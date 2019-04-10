/*
   Copyright 2018 Digimarc, Inc

   Licensed under the Apache License, Version 2.0 (the "License");
   you may not use this file except in compliance with the License.
   You may obtain a copy of the License at

       http://www.apache.org/licenses/LICENSE-2.0

   Unless required by applicable law or agreed to in writing, software
   distributed under the License is distributed on an "AS IS" BASIS,
   WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
   See the License for the specific language governing permissions and
   limitations under the License.

   SPDX-License-Identifier: Apache-2.0
*/

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
using System.Text.RegularExpressions;

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

        static Regex _HostWithScheme = new Regex(@"\w+:\/\/.+", RegexOptions.Compiled);
        static Regex _HostWithPort = new Regex(@".+:\d+$", RegexOptions.Compiled);

        public static string HostToEndpoint(string host, string resource = null)
        {
            if (DockerHubAliases.Contains(host.ToLowerInvariant())) { host = DockerHub; }

            var sb = new StringBuilder();
            // if the supplied hostname appears to include a scheme, preserve it and just add the resource path
            if (!_HostWithScheme.IsMatch(host))
            {
                // if the hostname appears to include a port, assume plain http, otherwise assume https
                if (_HostWithPort.IsMatch(host)) { sb.Append("http://"); }
                else { sb.Append("https://"); }
            }
            sb.Append(host.TrimEnd('/'));
            sb.Append("/v2/");
            sb.Append(resource ?? string.Empty);

            return sb.ToString();
        }

        #endregion

        public RegistrySettings Settings { get; set; }

        IDistributionClient DistributionClient { get; set; }
        IDistributionClient CatalogClient { get; set; }

        #region ctors

        public Registry(RegistryCredentials credentials, RegistrySettings config)
        {
            Settings = config;
            Settings.AuthHandler.Login(credentials.Registry, credentials.Username, credentials.Password);
            DistributionClient = Settings.DistributionFactory(credentials.Registry, Settings.AuthHandler);
            CatalogClient = Settings.DistributionFactory(credentials.Registry, Settings.CatalogAuthHandler ?? Settings.AuthHandler);
        }

        #endregion

        #region scope keys
        string CatalogScope() => "registry:catalog:*";
        string RepoPullScope(string repository) => $"repository:{repository}:pull";
        string RepoPushScope(string repository) => $"repository:{repository}:push";
        string RepoAdminScope(string repository) => $"repository:{repository}:*";
        #endregion

        #region cache keys
        string RepoListKey() => $"volatile:{CatalogClient.Host}:repos";
        string RepoTagsKey(string repository) => $"volatile:{DistributionClient.Host}:repos:{repository}:tags";
        string ImageDigestKey(string digest) => $"static:image:{digest}";
        string ImageTagKey(string repository, string tag) => $"volatile:{DistributionClient.Host}:repos:{repository}:tags:{tag}:images";
        string ImageFilesKey(Image image, int maxDepth) => $"static:image:{image.Digest}:files:{maxDepth}";
        string ImageLockKey(Image image) => $"static:image:{image.Digest}:lock";
        string LayerFilesKey(Layer layer) => $"static:layer:{layer.Digest}:files";
        string LayerFileKey(Layer layer, string path) => $"static:layer:{layer.Digest}:file:{path}";
        string ImageSearchKey(Image image, string search, int maxDepth) => $"static:image:{image.Digest}:find:{maxDepth}:{search}";
        #endregion

        #region cached methods

        /// <summary>
        /// Gets a list of pullable repositories from the registry.
        /// </summary>
        /// <param name="empties">If set, will return empty repositories only. Permissions checks still apply, but hidden/static repos are ignored.</param>
        /// <returns></returns>
        public IEnumerable<Repository> GetRepositories(bool empties = false)
        {
            var key = RepoListKey();
            var scope = CatalogScope();

            List<string> repoNames;
            try
            {
                repoNames = GetCached(scope, key, true, Settings.CatalogAuthHandler ?? Settings.AuthHandler, () => CatalogClient.GetRepositoriesAsync().Result.Repositories).ToList();
            }
            catch
            {
                repoNames = new List<string>();
            }

            // add statics, and remove hidden
            if (!empties && Settings.StaticRepos != null) { repoNames.AddRange(Settings.StaticRepos); }
            if (!empties && Settings.HiddenRepos != null) { repoNames.RemoveAll(r => Settings.HiddenRepos.Any(h => h.ToLowerInvariant() == r.ToLowerInvariant())); }

            // must check and filter permissions before trying to count tags
            var repositories = repoNames.Distinct().Select(n => new Repository
            {
                Name = n,
                Permissions = Settings.AuthHandler.GetPermissions(n)                
            }).Where(r => r.Permissions >= Permissions.Pull).ToList();
            repositories.ForEach(r => r.Tags = GetTags(r.Name)?.Count() ?? 0);

            return empties ? repositories.Where(r => r.Tags == 0) : repositories.Where(r => r.Tags > 0);            
        }

        public IEnumerable<string> GetTags(string repository)
        {
            string key = RepoTagsKey(repository);
            var scope = $"repository:{repository}:pull";

            return GetCached(scope, key, true, () => DistributionClient.GetTagsAsync(repository).Result.Tags);
        }

        /*
         * The whole image vs image set thing is very confusing and probably needs to be broken up further to make it clear what is happening.
         * For now, if this method is called with the digest of a "regular" single-platform image, it will return an ImageSet with a single image
         * and a SetDigest copied from that image.
         * If it is called with the digest of a "fat" manifest list, it will return an ImageSet with multiple images with valid single-image digests
         * and a unique manifest list digest.
         */
        public ImageSet GetImageSet(string repository, string tag, bool isDigest)
        {
            var key = isDigest ? ImageDigestKey(tag) : ImageTagKey(repository, tag);
            string scope = RepoPullScope(repository);

            return GetCached(scope, key, !isDigest, () => DistributionClient.GetImageSet(repository, tag).Result);
        }

        /// <summary>
        /// Deletes an image from the repository. The digest must correspond to either a manifest list, or a "regular" v2 manifest - not a tag or submanifest or v1 manifest.
        /// </summary>
        /// <param name="digest"></param>
        public void DeleteImage(string repository, string digest)
        {
            var scope = RepoAdminScope(repository);

            if (Settings.AuthHandler.Authorize(scope))
            {
                // on delete, we leave the static image data in cache, just in case another repo is using this image
                // we do clear the tags list for the repo, since there is no good way to know which tags this delete will effect
                var key = RepoTagsKey(repository);
                var cache = Settings.CacheFactory?.Get<string>();

                DistributionClient.DeleteImage(repository, digest).Wait();
                cache.TryDelete(key);
            }
            else
            {
                throw new UnauthorizedAccessException();
            }
        }

        /// <summary>
        /// Deletes all images and tags within a repository. Final cleanup of the actual repo folder on the registry server must currently be done manually.
        /// </summary>
        /// <param name="repository"></param>
        public void DeleteRepository(string repository)
        {
            var scope = RepoAdminScope(repository);

            if (Settings.AuthHandler.Authorize(scope))
            {
                var key = RepoTagsKey(repository);
                var cache = Settings.CacheFactory?.Get<string>();

                // coerce to list before deleting to avoid deleting a tag out from under ourselves                
                var imageDigests = GetTags(repository).Select(t => GetImageSet(repository, t, false).SetDigest).Distinct().ToList();

                foreach (var digest in imageDigests)
                {
                    DeleteImage(repository, digest);
                }

                cache.Set(key, null);
            }
            else
            {
                throw new UnauthorizedAccessException();
            }
        }

        public IEnumerable<ImageFile> GetImageFiles(string repository, Image image, int maxDepth)
        {
            string key = ImageFilesKey(image, maxDepth);
            string lockKey = ImageLockKey(image);
            var scope = RepoPullScope(repository);

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
            string key = LayerFilesKey(layer);
            var scope = RepoPullScope(repository);

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
            string key = LayerFileKey(layer, path);
            var scope = RepoPullScope(repository);

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

        public Layer FindFile(string repository, Image image, string search, int maxDepth, bool ignoreCase = true)
        {
            string key = ImageSearchKey(image, search, maxDepth);
            var scope = RepoPullScope(repository);
            return GetCached(scope, key, false, () =>
            {

                var searchParams = search.Patherate();

                //search layers in reverse order, from newest to oldest
                var layers = image.Layers.Reverse().ToList();

                var depth = 0;
                foreach (var layer in layers)
                {
                    depth++;
                    if (maxDepth > 0 && depth > maxDepth) { break; }
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

        public LayerProxyInfo GetLayerProxyInfo(string repository, Layer layer)
        {
            var proxyInfo = DistributionClient.GetBlobPathAndAuthorizationAsync(repository, layer.Digest).Result;
            return new LayerProxyInfo
            {
                LayerAuthorization = $"{proxyInfo.auth?.Scheme} {proxyInfo.auth?.Parameter}",
                LayerUrl = proxyInfo.path.ToString()
            };
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

        public Permissions GetPermissions(string repository)
        {
            if (Settings.AuthHandler.Authorize(RepoAdminScope(repository))) { return Permissions.Admin; }
            else if (Settings.AuthHandler.Authorize(RepoPushScope(repository))) { return Permissions.Push; }
            else if (Settings.AuthHandler.Authorize(RepoPullScope(repository))) { return Permissions.Pull; }
            else { return Permissions.None; }
        }
    }
}
