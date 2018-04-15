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
using System.Threading.Tasks;

namespace Whalerator
{
    public class Registry : IRegistry
    {

        #region statics

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
            if (DockerHubAliases.Contains(host.ToLowerInvariant())) { host = DockerHub; }

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

        #endregion

        IDistributionClient DistributionAPI { get; set; }
        ICacheFactory CacheFactory { get; set; }
        IAuthHandler AuthHandler { get; set; }
        public string LayerCache { get; set; }
        string Host { get; set; }

        #region ctors

        public Registry(string host = DockerHub, string username = null, string password = null, ICacheFactory cacheFactory = null)
        {
            AuthHandler = new AuthHandler(cacheFactory?.Get<Client.Authorization>());
            AuthHandler.Login(host, username, password);
            DistributionAPI = new DistributionClient(AuthHandler) { Host = host };
            CacheFactory = cacheFactory;
        }

        public Registry(IDistributionClient distribution, ICacheFactory cacheFactory, IAuthHandler authHandler)
        {
            DistributionAPI = distribution;
            CacheFactory = cacheFactory;
            AuthHandler = authHandler;
        }

        #endregion

        #region public methods

        public IEnumerable<string> GetRepositories()
        {
            var key = $"{Host}:repos";
            var scope = "registry:catalog:*";

            return GetCached(scope, key, () => DistributionAPI.GetRepositoriesAsync().Result.Repositories);
        }

        public IEnumerable<string> GetTags(string repository)
        {
            var key = $"{Host}:tags:{repository}";
            var scope = $"repository:{repository}:pull";

            return GetCached(scope, key, () => DistributionAPI.GetTagsAsync(repository).Result.Tags);
        }

        public IEnumerable<Image> GetImages(string repository, string tag)
        {
            var key = $"{Host}:{repository}:{tag}:images";
            var scope = $"repository:{repository}:pull";

            return GetCached(scope, key, () => DistributionAPI.GetImages(repository, tag).Result);
        }

        #endregion

        private T GetCached<T>(string scope, string key, Func<T> func) where T : class
        {
            var cache = CacheFactory?.Get<T>();

            T result;
            if (AuthHandler.Authorize(scope))
            {
                if (cache != null && cache.TryGet(key, out result))
                {
                    return result;
                }
                else
                {
                    result = func();
                    cache?.Set(key, result);
                    return result;
                }
            }
            else
            {
                throw new AuthenticationException("The request could not be authorized.");
            }
        }

        public Layer FindFile(string repository, Image image, string search, bool ignoreCase = true)
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
                        return layer;
                    }
                    else
                    {
                        if (file.Matches(searchParams.fileWhiteout, ignoreCase))
                        {
                            //found a whiteout entry for the file
                            break;
                        }

                        if (file.Matches(searchParams.pathWhiteout, ignoreCase))
                        {
                            //found a opaque point for the whole path
                            break;
                        }
                    }
                }
            }

            return null;
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
    }
}
