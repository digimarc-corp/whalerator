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
using System.Text;

namespace MobyInspector
{
    public class Registry : IRegistry
    {
        public ITokenSource TokenSource { get; set; }
        public Uri RegistryEndpoint { get; set; }
        public string LayerCache { get; set; }
        public int Retries { get; set; } = 3;

        public byte[] GetLayerFile(string repository, string digest, string path, bool ignoreCase = true)
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
                                if (entry.Name == path || (ignoreCase && entry.Name.ToLowerInvariant() == path.ToLowerInvariant())) { break; }
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

        public IEnumerable<string> GetLayerFiles(string repository, string digest)
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

        public Stream GetLayer(string repository, string digest)
        {
            if (!string.IsNullOrEmpty(LayerCache))
            {
                // registry path is ignored for caching purposes; only digest matters
                var digestPath = digest.Replace(':', Path.DirectorySeparatorChar);
                var cachedFile = Path.Combine(LayerCache, digestPath);
                var digestFolder = Path.GetDirectoryName(cachedFile);
                if (!Directory.Exists(digestFolder)) { Directory.CreateDirectory(digestFolder); }
                if (!File.Exists(cachedFile))
                {
                    using (var stream = new FileStream(cachedFile, FileMode.CreateNew, FileAccess.ReadWrite, FileShare.None))
                    {
                        FetchLayer(repository, digest, stream);
                    }
                }
                return new FileStream(cachedFile, FileMode.Open, FileAccess.Read, FileShare.Read);
            }
            else
            {
                var stream = new MemoryStream();
                FetchLayer(repository, digest, stream);
                stream.Seek(0, SeekOrigin.Begin);
                return stream;
            }
        }

        private void FetchLayer(string repostiory, string digest, Stream buffer) => FetchLayer(repostiory, digest, buffer, retries: Retries);

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
                    using (var redirectClient = new HttpClient() { Timeout = new TimeSpan(0, 10, 0) })
                    {
                        var redirectResult = redirectClient.GetAsync(result.Headers.Location).Result;
                        if (redirectResult.StatusCode == HttpStatusCode.OK)
                        {
                            redirectResult.Content.CopyToAsync(buffer).Wait();
                        }
                        else
                        {
                            throw new Exception($"Failed to get layer data on redirect: {redirectResult.StatusCode} {redirectResult.ReasonPhrase}");
                        }
                    }
                }
                else
                {
                    throw new Exception($"Failed to get layer: {result.StatusCode} {result.ReasonPhrase}");
                }
            }
        }

        public Manifest GetManifest(string repository, string tag) => GetManifest(repository, tag, retries: Retries);

        private Manifest GetManifest(string repository, string tag, int retries, string token = null)
        {
            var distributionApi = RestService.For<IDistribution>(RegistryEndpoint.ToString());

            var response = token == null ? distributionApi.GetManifest(repository, tag).Result : distributionApi.GetManifest(repository, tag, $"Bearer {token}").Result;

            if (response.StatusCode == HttpStatusCode.OK)
            {
                return JsonConvert.DeserializeObject<Manifest>(response.Content.ReadAsStringAsync().Result);
            }
            else if (response.StatusCode == HttpStatusCode.Unauthorized)
            {
                var challenge = response.Headers.WwwAuthenticate.First(h => h.Scheme == "Bearer");

                token = TokenSource.GetToken(challenge);
                return GetManifest(repository, tag, retries - 1, token);
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
