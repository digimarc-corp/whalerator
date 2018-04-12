using ICSharpCode.SharpZipLib.GZip;
using ICSharpCode.SharpZipLib.Tar;
using Whalerator.Client;
using Whalerator.Data;
using Whalerator.Model;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Security.Cryptography;
using System.Text;

namespace Whalerator
{
    public class Registry : IRegistry
    {
        // Docker uses some nonstandard names for Docker Hub
        public const string DockerHub = "registry-1.docker.io";
        public static HashSet<string> DockerHubAliases = new HashSet<string> {
            "docker.io",
            "hub.docker.io",
            "registry.docker.io",
            "registry-1.docker.io"
        };

        public static string HostToEndpoint(string host, string resource = null)
        {
            var sb = new StringBuilder();
            if (host.Contains("://"))
            {
                sb.Append(host.TrimEnd('/') + "/v2/");
            }
            else
            {
                sb.Append($"https://{host.TrimEnd('/')}/v2/");
            }
            sb.Append(resource ?? string.Empty);

            return sb.ToString();
        }

        public string LayerCache { get; set; }

        IDistributionClient DistributionAPI { get; set; }

        public Registry(string host = DockerHub, string username = null, string password = null, ICacheFactory cacheFactory = null)
        {
            var tokenSource = new AuthHandler(cacheFactory.Get<Client.Authorization>());
            tokenSource.Login(host, username, password);
            DistributionAPI = new DistributionClient(tokenSource, cacheFactory) { Host = host };
        }

        public Registry(IDistributionClient distribution)
        {
            DistributionAPI = distribution;
        }

        public (byte[] data, string layerDigest) FindFile(string repository, Image image, string search, bool ignoreCase = true)
        {
            var searchParams = search.Patherate();

            //search layers in reverse order, from newest to oldest
            var layers = image.Layers.Reverse().ToList();

            foreach (var layer in layers)
            {
                var files = GetFiles(repository, layer);
                foreach (var file in files)
                {
                    if (file.Matches(searchParams.searchPath, ignoreCase))
                    {
                        return (GetFile(repository, layer, searchParams.searchPath, ignoreCase), layer.Digest);
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

        public byte[] GetFile(string repository, Layer layer, string path, bool ignoreCase = true)
        {
            using (var stream = GetLayer(repository, layer))
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

        public IEnumerable<string> GetFiles(string repository, Layer layer)
        {
            var files = new List<string>();
            using (var stream = GetLayer(repository, layer))
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

        public Stream GetLayer(string repository, Layer layer)
        {
            if (!string.IsNullOrEmpty(LayerCache))
            {
                // registry path is ignored for caching purposes; only digest matters
                var digestPath = layer.Digest.Replace(':', Path.DirectorySeparatorChar);
                var cachedFile = Path.Combine(LayerCache, digestPath);
                var digestFolder = Path.GetDirectoryName(cachedFile);
                if (!Directory.Exists(digestFolder)) { Directory.CreateDirectory(digestFolder); }

                if (File.Exists(cachedFile) && ValidateLayer(cachedFile, layer.Digest))
                {
                    return new FileStream(cachedFile, FileMode.Open, FileAccess.Read, FileShare.Read);
                }
                else
                {
                    using (var stream = new FileStream(cachedFile, FileMode.Create, FileAccess.ReadWrite, FileShare.None))
                    {
                        DownloadLayer(repository, layer, stream);
                    }
                    if (ValidateLayer(cachedFile, layer.Digest))
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
                DownloadLayer(repository, layer, stream);
                stream.Seek(0, SeekOrigin.Begin);
                return stream;
            }
        }

        private bool ValidateLayer(string filename, string digest)
        {
            using (var stream = new FileStream(filename, FileMode.Open, FileAccess.Read, FileShare.Read))
            {
                return ValidateLayer(stream, digest);
            }
        }

        private bool ValidateLayer(Stream stream, string digest)
        {
            var parts = digest.Split(':').ToList();
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

        private void DownloadLayer(string repository, Layer layer, Stream stream)
        {
            if (layer is ForeignLayer) { FetchForeignLayer((ForeignLayer)layer, stream); }
            else { FetchLayer(repository, layer, stream); }
        }

        private void FetchForeignLayer(ForeignLayer layer, Stream buffer)
        {
            using (var client = new HttpClient() { Timeout = new TimeSpan(0, 10, 0) })
            {
                var result = client.GetAsync(layer.Urls.First()).Result;
                if (result.StatusCode == HttpStatusCode.OK)
                {
                    result.Content.CopyToAsync(buffer).Wait();
                }
                else
                {
                    throw new Exception($"Failed to get layer data at {layer.Urls.First()}: {result.StatusCode} {result.ReasonPhrase}");
                }
            }
        }

        private void FetchLayer(string repository, Layer layer, Stream buffer)
        {
            var result = DistributionAPI.GetBlobAsync(repository, layer.Digest).Result;
            result.Content.CopyToAsync(buffer).Wait();
        }

        public IEnumerable<Image> GetImages(string repository, string tag)
        {
            var response = DistributionAPI.GetV2Manifest(repository, tag).Result;

            var images = new List<Image>();
            if (response.Content.Headers.ContentType.MediaType == "application/vnd.docker.distribution.manifest.list.v2+json")
            {
                var json = response.Content.ReadAsStringAsync().Result;
                var fatManifest = JsonConvert.DeserializeObject<FatManifest>(json);
                foreach (var subManifest in fatManifest.Manifests)
                {
                    images.AddRange(GetImages(repository, subManifest.Digest));
                }
            }
            else if (response.Content.Headers.ContentType.MediaType == "application/vnd.docker.distribution.manifest.v2+json")
            {
                var json = response.Content.ReadAsStringAsync().Result;
                var manifest = JsonConvert.DeserializeObject<ManifestV2>(json);
                var config = GetImageConfig(repository, manifest.Config.Digest);
                var image = new Image
                {
                    History = config.History.Select(h => new Model.History { Command = new[] { h.Created_By }, Created = h.Created }),
                    Layers = manifest.Layers.Select(l => l.ToLayer()),
                    ManifestDigest = response.Headers.First(h => h.Key.ToLowerInvariant() == "docker-content-digest").Value.First(),
                    Platform = new Platform
                    {
                        Architecture = config.Architecture,
                        OS = config.OS,
                        OSVerion = config.OSVersion
                    }
                };
                image.Layers = manifest.Layers.Select(l => l.ToLayer());

                images.Add(image);
            }
            else
            {
                throw new Exception($"Cannot build image set from mediatype '{response.Content.Headers.ContentType.MediaType}'");
            }

            return images;
        }

        private ImageConfig GetImageConfig(string repository, string digest)
        {
            var result = DistributionAPI.GetBlobAsync(repository, digest).Result;
            var json = result.Content.ReadAsStringAsync().Result;
            return JsonConvert.DeserializeObject<ImageConfig>(json);
        }

        public IEnumerable<string> GetTags(string repository)
        {
            return DistributionAPI.GetTagsAsync(repository).Result.Tags;
        }

        public IEnumerable<string> GetRepositories()
        {
            return DistributionAPI.GetRepositoriesAsync().Result.Repositories;
        }
    }
}
