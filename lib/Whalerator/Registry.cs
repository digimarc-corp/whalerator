using ICSharpCode.SharpZipLib.GZip;
using ICSharpCode.SharpZipLib.Tar;
using Whalerator.Client;
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
using Microsoft.Extensions.Logging;

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

        public RegistryConfig Settings { get; set; }

        IDistributionClient DistributionClient { get; set; }
        IDistributionClient CatalogClient { get; set; }

        #region ctors

        public Registry(RegistryCredentials credentials, RegistryConfig config)
        {
            Settings = config;
            Settings.AuthHandler.Login(credentials.Registry, credentials.Username, credentials.Password);
            DistributionClient = Settings.DistributionFactory.GetClient(credentials.Registry, Settings.AuthHandler);
            CatalogClient = Settings.DistributionFactory.GetClient(credentials.Registry, Settings.CatalogAuthHandler ?? Settings.AuthHandler);
        }

        #endregion

        #region cached methods

        public IEnumerable<Repository> GetRepositories()
        {
            var key = $"volatile:{CatalogClient.Host}:repos";
            var scope = "registry:catalog:*";

            List<string> repositories;
            try
            {
                repositories = GetCached(scope, key, true, Settings.CatalogAuthHandler ?? Settings.AuthHandler, () => CatalogClient.GetRepositoriesAsync().Result.Repositories).ToList();
            }
            catch
            {
                repositories = new List<string>();
            }

            // add statics, and remove hidden
            if (Settings.StaticRepos != null) { repositories.AddRange(Settings.StaticRepos); }
            if (Settings.HiddenRepos != null) { repositories.RemoveAll(r => Settings.HiddenRepos.Any(h => h.ToLowerInvariant() == r.ToLowerInvariant())); }

            return repositories.Where(r => Settings.AuthHandler.Authorize($"repository:{r}:pull")).Select(r => new Repository
            {
                Name = r,
                //Push = Settings.AuthHandler.Authorize($"repository:{r}:push"),
                //Delete = Settings.AuthHandler.Authorize($"repository:{r}:*"),
                Tags = GetTags(r).Count()
            });
        }

        public IEnumerable<string> GetTags(string repository)
        {
            var key = $"volatile:{DistributionClient.Host}:tags:{repository}";
            var scope = $"repository:{repository}:pull";

            return GetCached(scope, key, true, () => DistributionClient.GetTagsAsync(repository).Result.Tags);
        }

        public IEnumerable<Image> GetImages(string repository, string tag)
        {
            var key = $"volatile:{DistributionClient.Host}:repos:{repository}:{tag}:images";
            var scope = $"repository:{repository}:pull";

            return GetCached(scope, key, true, () => DistributionClient.GetImages(repository, tag).Result);
        }

        public IEnumerable<ImageFile> GetImageFiles(string repository, Image image, int maxDepth)
        {
            var key = $"static:image:{image.Digest}:files:{maxDepth}";
            var lockKey = $"lock:static:image:{image.Digest}:files:{maxDepth}";
            var scope = $"repository:{repository}:pull";

            return GetCached(scope, key, false, () =>
            {
                using (var lockObj = Settings.CacheFactory.Get<string>().TakeLock(lockKey, new TimeSpan(0, 5, 0), new TimeSpan(0, 5, 0)))
                {
                    var files = new List<ImageFile>();

                    var layers = image.Layers.Reverse();
                    var depth = 1;
                    foreach (var layer in layers)
                    {
                        var layerFiles = GetFiles(repository, layer);
                        files.AddRange(layerFiles.Where(f => !string.IsNullOrWhiteSpace(f)).Select(f => new ImageFile
                        {
                            //Layer = layer.Digest,
                            LayerDepth = depth,
                            Path = f
                        }));
                        depth++;
                        if (depth > maxDepth) { break; }
                    }

                    return files.OrderBy(f => f.Path).ToList();
                }
            });
        }

        public IEnumerable<string> GetFiles(string repository, Layer layer)
        {
            var key = $"static:layer:{layer.Digest}:files";
            var scope = $"repository:{repository}:pull";

            return GetCached(scope, key, false, () =>
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
            });
        }

        public byte[] GetFile(string repository, Layer layer, string path, bool ignoreCase = true)
        {
            var key = $"static:layer:{layer.Digest}:file:{path}";
            var scope = $"repository:{repository}:pull";

            return GetCached(scope, key, false, () =>
            {

                var searchParams = path.Patherate();

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
                                    if (entry.Name.Matches(searchParams.searchPath, ignoreCase)) { break; }
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
            });
        }


        #endregion

        private T GetCached<T>(string scope, string key, bool isVolatile, Func<T> func) where T : class =>
            GetCached<T>(scope, key, isVolatile, Settings.AuthHandler, func);

        private T GetCached<T>(string scope, string key, bool isVolatile, IAuthHandler authHandler, Func<T> func) where T : class
        {
            var cache = Settings.CacheFactory?.Get<T>();

            T result;
            if (authHandler.Authorize(scope))
            {
                if (cache != null && cache.TryGet(key, out result))
                {
#if DEBUG
                    Console.WriteLine($"Cache hit {key}");
#endif
                    return result;
                }
                else
                {
#if DEBUG
                    Console.WriteLine($"Cache miss {key}");
#endif
                    result = func();
                    cache?.Set(key, result, isVolatile ? Settings.VolatileTtl : Settings.StaticTtl);
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


        public Stream GetLayer(string repository, Layer layer)
        {
            if (!string.IsNullOrEmpty(Settings.LayerCache))
            {
                // registry path is ignored for caching purposes; only digest matters
                var digestPath = layer.Digest.Replace(':', Path.DirectorySeparatorChar);
                var cachedFile = Path.Combine(Settings.LayerCache, digestPath);
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
            var result = DistributionClient.GetBlobAsync(repository, layer.Digest).Result;
            result.Content.CopyToAsync(buffer).Wait();
        }
    }
}
