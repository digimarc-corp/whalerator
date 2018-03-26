using ICSharpCode.SharpZipLib.GZip;
using ICSharpCode.SharpZipLib.Tar;
using MobyInspector.Data;
using Newtonsoft.Json;
using Refit;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Security.Cryptography;
using System.Text;

namespace MobyInspector
{
    public class Registry : IRegistry
    {
        public ITokenSource TokenSource { get; set; }
        public Uri RegistryEndpoint { get; set; }
        public string LayerCache { get; set; }
        public int Retries { get; set; } = 3;
        public IPatherator Patherator { get; set; }

        public Registry()
        {
            TokenSource = new AnonTokenSource();
            Patherator = new Patherator();
        }

        public Registry(ITokenSource tokenSource, IPatherator patherator)
        {
            TokenSource = tokenSource;
            Patherator = patherator;
        }

        public (byte[] data, string layerDigest) Find(string search, string repository, string tag, Platform platform, bool ignoreCase = true)
        {
            var searchParams = Patherator.Parse(search);

            var manifest = GetManifest(repository, tag, platform);

            //search layers in reverse order, from newest to oldest
            var layers = manifest.Layers.Reverse().ToList();

            foreach (var layer in layers)
            {
                var files = GetLayerFiles(repository, layer);
                foreach (var file in files)
                {
                    if (file.Matches(searchParams.searchPath, ignoreCase))
                    {
                        return (GetLayerFile(repository, layer, searchParams.searchPath, ignoreCase), layer.Digest);
                    }
                    else
                    {
                        if (file.Matches(searchParams.fileWhiteout, ignoreCase))
                        {
                            //found a whiteout entry for the file
                            return (null, null);
                        }

                        if (file.Matches(searchParams.pathWhiteout, ignoreCase))
                        {
                            //found a opaque point for the whole path
                            return (null, null);
                        }
                    }
                }
            }

            return (null, null);
        }

        public byte[] GetLayerFile(string repository, MediaDigest digest, string path, bool ignoreCase = true)
        {
            using (var stream = GetLayer(repository, digest))
            {
                var temp = Path.GetTempFileName();
                try
                {
                    using (var gunzipped = new FileStream(temp, FileMode.Create, FileAccess.ReadWrite, FileShare.None))
                    {
                        using (var gzipStream = new GZipInputStream(stream)) { gzipStream.CopyTo(gunzipped); }
                        gunzipped.Seek(0, SeekOrigin.Begin);
                        using (var tarStream = new TarInputStream(gunzipped))
                        {
                            var entry = tarStream.GetNextEntry();
                            while (entry != null)
                            {
                                //if we're ignoring case and there are multiple possible matches, we just return the first one we find
                                if (entry.Name.Matches(path, ignoreCase)) { break; }
                                entry = tarStream.GetNextEntry();
                            }

                            if (entry == null || entry.IsDirectory)
                            {
                                throw new FileNotFoundException();
                            }
                            else
                            {
                                var bytes = new byte[entry.Size];
                                tarStream.Read(bytes, 0, (int)entry.Size);
                                return bytes;
                            }
                        }
                    }
                }
                catch
                {
                    throw;
                }
                finally
                {
                    File.Delete(temp);
                }
            }
        }

        public IEnumerable<string> GetLayerFiles(string repository, MediaDigest digest)
        {
            var files = new List<string>();
            using (var stream = GetLayer(repository, digest))
            {
                var temp = Path.GetTempFileName();
                try
                {
                    using (var gunzipped = new FileStream(temp, FileMode.Create, FileAccess.ReadWrite, FileShare.None))
                    {
                        using (var gzipStream = new GZipInputStream(stream)) { gzipStream.CopyTo(gunzipped); }
                        gunzipped.Seek(0, SeekOrigin.Begin);
                        using (var tarStream = new TarInputStream(gunzipped))
                        {
                            var entry = tarStream.GetNextEntry();
                            while (entry != null)
                            {
                                files.Add(entry.Name);
                                entry = tarStream.GetNextEntry();
                            }
                        }
                    }
                }
                catch
                {
                    throw;
                }
                finally
                {
                    File.Delete(temp);
                }
            }
            return files;
        }

        public Stream GetLayer(string repository, MediaDigest media)
        {
            if (!string.IsNullOrEmpty(LayerCache))
            {
                // registry path is ignored for caching purposes; only digest matters
                var digestPath = media.Digest.Replace(':', Path.DirectorySeparatorChar);
                var cachedFile = Path.Combine(LayerCache, digestPath);
                var digestFolder = Path.GetDirectoryName(cachedFile);
                if (!Directory.Exists(digestFolder)) { Directory.CreateDirectory(digestFolder); }

                if (File.Exists(cachedFile) && ValidateLayer(cachedFile, media))
                {
                    return new FileStream(cachedFile, FileMode.Open, FileAccess.Read, FileShare.Read);
                }
                else
                {
                    using (var stream = new FileStream(cachedFile, FileMode.Create, FileAccess.ReadWrite, FileShare.None))
                    {
                        DownloadLayer(repository, media, stream);
                    }
                    if (ValidateLayer(cachedFile, media))
                    {
                        return new FileStream(cachedFile, FileMode.Open, FileAccess.Read, FileShare.Read);
                    }
                    else
                    {
                        throw new Exception("Downloaded layer does not match the requested digest/hash.");
                    }
                }
            }
            else
            {
                var stream = new MemoryStream();
                DownloadLayer(repository, media, stream);
                stream.Seek(0, SeekOrigin.Begin);
                return stream;
            }
        }

        private bool ValidateLayer(string filename, MediaDigest digest)
        {
            using (var stream = new FileStream(filename, FileMode.Open, FileAccess.Read, FileShare.Read))
            {
                return ValidateLayer(stream, digest);
            }
        }

        private bool ValidateLayer(Stream stream, MediaDigest digest)
        {
            var parts = digest.Digest.Split(':').ToList();
            if (parts.Count() != 2) { throw new ArgumentException("Could not parse digest string."); }
            else
            {
                var alg = parts[0];
                var checkHash = parts[1].ToLowerInvariant();
                HashAlgorithm hasher;
                switch (alg)
                {
                    case "sha256":
                        hasher = System.Security.Cryptography.SHA256.Create();
                        break;
                    default:
                        throw new ArgumentException($"Don't know how to calculated digest hash of type {alg}");
                }
                var calcHash = BitConverter.ToString(hasher.ComputeHash(stream)).Replace("-", string.Empty).ToLowerInvariant();
                return calcHash == checkHash;
            }
        }

        private void DownloadLayer(string repository, MediaDigest media, Stream stream)
        {
            if (media.MediaType == "application/vnd.docker.image.rootfs.diff.tar.gzip") { FetchLayer(repository, media.Digest, stream); }
            else if (media.MediaType == "application/vnd.docker.image.rootfs.foreign.diff.tar.gzip") { FetchForeignLayer(media.Urls.First(), stream); }
            else { throw new NotSupportedException($"Don't know how to fetch media of type '{media.MediaType}'"); }
        }

        private void FetchForeignLayer(string url, Stream buffer)
        {
            using (var client = new HttpClient() { Timeout = new TimeSpan(0, 10, 0) })
            {
                var result = client.GetAsync(url).Result;
                if (result.StatusCode == HttpStatusCode.OK)
                {
                    result.Content.CopyToAsync(buffer).Wait();
                }
                else
                {
                    throw new Exception($"Failed to get layer data at {url}: {result.StatusCode} {result.ReasonPhrase}");
                }
            }
        }

        private void FetchLayer(string repository, string digest, Stream buffer) => FetchLayer(repository, digest, buffer, retries: Retries);

        private void FetchLayer(string repository, string digest, Stream buffer, int retries, string token = null)
        {
            var handler = new HttpClientHandler() { AllowAutoRedirect = false };
            using (var client = new HttpClient(handler) { BaseAddress = RegistryEndpoint })
            {
                var distributionApi = RestService.For<IDistribution>(client);

                var result = token == null ? distributionApi.GetLayerAsync(repository, digest).Result : distributionApi.GetLayerAsync(repository, digest, $"Bearer {token}").Result;
                if (result.StatusCode == HttpStatusCode.OK)
                {
                    result.Content.CopyToAsync(buffer).Wait();
                }
                else if (result.StatusCode == System.Net.HttpStatusCode.Unauthorized)
                {
                    var challenge = result.Headers.WwwAuthenticate.First(h => h.Scheme == "Bearer");

                    token = TokenSource.GetToken(challenge);
                    FetchLayer(repository, digest, buffer, retries - 1, token);
                }
                else if (result.StatusCode == System.Net.HttpStatusCode.Redirect || result.StatusCode == System.Net.HttpStatusCode.RedirectKeepVerb)
                {
                    FetchForeignLayer(result.Headers.Location.ToString(), buffer);
                }
                else
                {
                    throw new Exception($"Failed to get layer: {result.StatusCode} {result.ReasonPhrase}");
                }
            }
        }

        public IEnumerable<Platform> GetPlatforms(string repository, string tag) => GetPlatforms(repository, tag, retries: Retries);

        private IEnumerable<Platform> GetPlatforms(string repository, string tag, int retries, string token = null)
        {
            var distributionApi = RestService.For<IDistribution>(RegistryEndpoint.ToString());

            HttpResponseMessage v1response, v2response;
            v2response = token == null ? distributionApi.GetV2Manifest(repository, tag).Result : distributionApi.GetV2Manifest(repository, tag, $"Bearer {token}").Result;

            if (v2response.StatusCode == HttpStatusCode.OK)
            {
                if (v2response.Content.Headers.ContentType.MediaType == "application/vnd.docker.distribution.manifest.list.v2+json")
                {
                    var manifest = JsonConvert.DeserializeObject<FatManifest>(v2response.Content.ReadAsStringAsync().Result);
                    return manifest.Manifests.Select(m => m.Platform).ToList();
                }
                else
                {
                    //we got back a single "skinny" V2 manifest, so we need to get the V1 manifest w/ history to get platform info
                    if (v2response.Content.Headers.ContentType.MediaType == "application/vnd.docker.distribution.manifest.v2+json")
                    {
                        v1response = token == null ? distributionApi.GetV1Manifest(repository, tag).Result : distributionApi.GetV1Manifest(repository, tag, $"Bearer {token}").Result;
                        if (v1response.StatusCode != HttpStatusCode.OK)
                        {
                            throw new Exception($"Failed to get V1 manifest from registry API: {v1response.StatusCode} {v1response.ReasonPhrase}");
                        }
                    }
                    else if (v2response.Content.Headers.ContentType.MediaType == "application/vnd.docker.distribution.manifest.v1+json")
                    {
                        v1response = v2response;
                    }
                    else
                    {
                        throw new NotSupportedException($"Don't know how to handle a manifest of type '{v2response.Content.Headers.ContentType.MediaType}'");
                    }

                    var manifest = JsonConvert.DeserializeObject<ManifestV1>(v1response.Content.ReadAsStringAsync().Result);
                    var history = manifest.History.Select(h => h.ToPlatform()).FirstOrDefault(p => p != null);
                    if (history != null) { return new[] { history }; }
                    else { return null; }
                }
            }
            else if (v2response.StatusCode == HttpStatusCode.Unauthorized)
            {
                var challenge = v2response.Headers.WwwAuthenticate.First(h => h.Scheme == "Bearer");

                token = TokenSource.GetToken(challenge);
                return GetPlatforms(repository, tag, retries - 1, token);
            }
            else
            {
                throw new Exception($"Failed to get manifest: {v2response.StatusCode} {v2response.ReasonPhrase}");
            }
        }

        public ManifestV2 GetManifest(string repository, string tag, Platform platform) => GetManifest(repository, tag, platform, retries: Retries);

        private ManifestV2 GetManifest(string repository, string tag, Platform platform, int retries, string token = null)
        {
            var distributionApi = RestService.For<IDistribution>(RegistryEndpoint.ToString());

            var response = token == null ? distributionApi.GetV2Manifest(repository, tag).Result : distributionApi.GetV2Manifest(repository, tag, $"Bearer {token}").Result;

            if (response.StatusCode == HttpStatusCode.OK)
            {
                if (response.Content.Headers.ContentType.MediaType == "application/vnd.docker.distribution.manifest.list.v2+json")
                {
                    var fatManifest = JsonConvert.DeserializeObject<FatManifest>(response.Content.ReadAsStringAsync().Result);
                    var subManifest = fatManifest.Manifests.Where(m => m.Platform == platform).FirstOrDefault();
                    return subManifest == null ? null : GetManifest(repository, subManifest.Digest, platform);
                }
                else if (response.Content.Headers.ContentType.MediaType == "application/vnd.docker.distribution.manifest.v2+json")
                {
                    return JsonConvert.DeserializeObject<ManifestV2>(response.Content.ReadAsStringAsync().Result);
                }
                else
                {
                    throw new NotImplementedException($"Can't handle manifest of type '{response.Content.Headers.ContentType.MediaType}'");
                }
            }
            else if (response.StatusCode == HttpStatusCode.Unauthorized)
            {
                var challenge = response.Headers.WwwAuthenticate.First(h => h.Scheme == "Bearer");

                token = TokenSource.GetToken(challenge);
                return GetManifest(repository, tag, platform, retries - 1, token);
            }
            else
            {
                throw new Exception($"Failed to get manifest: {response.StatusCode} {response.ReasonPhrase}");
            }

        }

        public IEnumerable<string> GetTags(string repository) => GetTags(repository, retries: Retries);

        private IEnumerable<string> GetTags(string repository, int retries, string token = null)
        {
            var distributionApi = RestService.For<IDistribution>(RegistryEndpoint.ToString());
            try
            {
                var repsonse = token == null ? distributionApi.GetTagsAsync(repository).Result : distributionApi.GetTagsAsync(repository, $"Bearer {token}").Result;
                return repsonse.Tags;
            }
            catch (AggregateException aggex)
            {
                if (aggex.InnerException is ApiException && retries > 0)
                {
                    var ex = aggex.InnerException as ApiException;

                    if (ex.StatusCode == System.Net.HttpStatusCode.Unauthorized)
                    {
                        var challenge = ex.Headers.WwwAuthenticate.First(h => h.Scheme == "Bearer");

                        token = TokenSource.GetToken(challenge);
                        return GetTags(repository, retries - 1, token);
                    }
                }

                throw;
            }
        }

        public IEnumerable<string> GetRepositories() => GetRepositories(retries: Retries);

        private IEnumerable<string> GetRepositories(int retries, string token = null)
        {
            var distributionApi = RestService.For<IDistribution>(RegistryEndpoint.ToString());
            try
            {
                var repsonse = token == null ? distributionApi.GetRepositoriesAsync().Result : distributionApi.GetRepositoriesAsync($"Bearer {token}").Result;
                return repsonse.Repositories;
            }
            catch (AggregateException aggex)
            {
                if (aggex.InnerException is ApiException && retries > 0)
                {
                    var ex = aggex.InnerException as ApiException;

                    if (ex.StatusCode == System.Net.HttpStatusCode.Unauthorized)
                    {
                        var challenge = ex.Headers.WwwAuthenticate.First(h => h.Scheme == "Bearer");

                        token = TokenSource.GetToken(challenge);
                        return GetRepositories(retries - 1, token);
                    }
                }

                throw;
            }
        }
    }
}
