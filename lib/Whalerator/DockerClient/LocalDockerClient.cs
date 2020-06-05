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

using ICSharpCode.SharpZipLib;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Whalerator.Client;
using Whalerator.Config;
using Whalerator.Content;
using Whalerator.Data;
using Whalerator.Model;

namespace Whalerator.DockerClient
{
    public class LocalDockerClient : DockerClientBase, ILocalDockerClient
    {
        public LocalDockerClient(ServiceConfig config, IAufsFilter filter, ILayerExtractor extractor, IAuthHandler auth, ILogger<LocalDockerClient> logger) : base(config, auth)
        {
            this.filter = filter;
            this.extractor = extractor;
            this.logger = logger;
            RecurseClient = this;
        }

        private readonly IAufsFilter filter;
        private readonly ILayerExtractor extractor;
        private readonly ILogger<LocalDockerClient> logger;

        /// <summary>
        /// When making recursive/chained calls to the API, they will be funnelled through this instance.
        /// </summary>
        public IDockerClient RecurseClient { get; set; }

        public string RegistryRoot { get; set; }
        string repositoriesRoot => Path.Combine(RegistryRoot, "docker/registry/v2/repositories");
        string blobsRoot => Path.Combine(RegistryRoot, "docker/registry/v2/blobs");
        private const string tagsFolder = "_manifests/tags";

        public static HashSet<string> SupportedManifestTypes { get; } = new HashSet<string>(new[] {
            ManifestV1MediaType,
            ManifestV2MediaType,
            ManifestListV2MediaType,
            ManifestV1MediaType + "+json",
            ManifestV2MediaType + "+json",
            ManifestListV2MediaType + "+json"
        });

        public const string ManifestV2MediaType = "application/vnd.docker.distribution.manifest.v2";
        public const string ManifestListV2MediaType = "application/vnd.docker.distribution.manifest.list.v2";
        public const string ManifestV1MediaType = "application/vnd.docker.distribution.manifest.v1+json";

        public string BlobPath(string digest) => Path.Combine(blobsRoot, digest.ToDigestPath(), "data");
        public string RepoPath(string repository) => Path.Combine(repositoriesRoot, repository);
        public string TagPath(string repository, string tag) => Path.Combine(RepoPath(repository), tagsFolder, tag);

        private string TagLinkPath(string repository, string tag) => Path.Combine(TagPath(repository, tag), "current/link");

        public async Task<ImageConfig> GetImageConfigAsync(string repository, string digest)
        {
            using (var stream = await GetBlobAsync(repository, digest))
            using (var sr = new StreamReader(stream))
            {
                var json = await sr.ReadToEndAsync();
                return JsonConvert.DeserializeObject<ImageConfig>(json);
            }
        }

        public Task<Stream> GetBlobAsync(string repository, string digest) => Task.FromResult<Stream>(
            new FileStream(Path.Combine(blobsRoot, digest.ToDigestPath(), "data"), FileMode.Open, FileAccess.Read, FileShare.ReadWrite));

        public Task DeleteImageAsync(string repository, string digest)
        {
            var tagDigests = GetTags(repository)
                .Select(t => (tag: t, digest: GetTagDigestAsync(repository, t).Result))
                .Where(t => t.digest.Equals(digest))
                .ToList();
            tagDigests.ForEach(t => Directory.Delete(TagPath(repository, t.tag), true));
            return Task.CompletedTask;
        }

        public Task DeleteRepositoryAsync(string repository)
        {
            Directory.Delete(RepoPath(repository), recursive: true);
            return Task.CompletedTask;
        }

        public Task<Stream> GetFileAsync(string repository, Layer layer, string path) => extractor.ExtractFileAsync(Path.Combine(blobsRoot, layer.Digest.ToDigestPath(), "data"), path);

        public async Task<ImageSet> GetImageSetAsync(string repository, string tag)
        {
            string digest;
            if (tag.IsDigest())
            {
                digest = tag;
            }
            else
            {
                digest = await GetTagDigestAsync(repository, tag);
            }
            string manifestPath = BlobPath(digest);
            string manifest = await ReadFileAsync(manifestPath);

            try
            {
                var mediaType = (string)JObject.Parse(manifest)["mediaType"];

                // In docker-land, a fat manifest is just a bunch of regular manifests bundled together under a single digest
                // In whalerator-land, there are no non-fat manifests, just fat manifests with a single image

                return mediaType switch
                {
                    string m when m.StartsWith(ManifestV2MediaType) => await ParseV2ThinManifest(repository, digest, manifest),
                    string m when m.StartsWith(ManifestListV2MediaType) => await ParseV2FatManifest(repository, digest, manifest),
                    _ => ParseV1Manifest(digest, manifest)
                };
            }
            catch
            {
                logger.LogError($"Failed to parse manifest {digest}. Manifest may be corrupt or an unsupported format.");
                throw new RegistryException("The manifest was unparseable.");
            }
        }

        private async Task<ImageSet> ParseV2ThinManifest(string repository, string digest, string manifest)
        {
            var thinManifest = JsonConvert.DeserializeObject<ManifestV2>(manifest);
            var config = await RecurseClient.GetImageConfigAsync(repository, thinManifest.Config.Digest);
            if (config == null) { throw new NotFoundException("The requested manifest does not exist in the registry."); }
            var image = new Image
            {
                History = config.History.Select(h => Model.History.From(h)),
                Layers = thinManifest.Layers.Select(l => l.ToLayer()),
                Digest = digest,
                Platform = new Platform
                {
                    Architecture = config.Architecture,
                    OS = config.OS,
                    OSVerion = config.OSVersion
                }
            };

            return new ImageSet
            {
                Date = image.History.Max(h => h.Created),
                Images = new[] { image },
                Platforms = new[] { image.Platform },
                SetDigest = image.Digest
            };
        }

        private async Task<ImageSet> ParseV2FatManifest(string repository, string digest, string manifest)
        {
            var images = new List<Image>();
            var fatManifest = JsonConvert.DeserializeObject<FatManifest>(manifest);
            foreach (var subManifest in fatManifest.Manifests)
            {
                images.Add((await RecurseClient.GetImageSetAsync(repository, subManifest.Digest)).Images.First());
            }

            return new ImageSet
            {
                Date = images.SelectMany(i => i.History.Select(h => h.Created)).Max(),
                Images = images,
                Platforms = images.Select(i => i.Platform),
                SetDigest = digest,
            };
        }

        private static ImageSet ParseV1Manifest(string digest, string manifest)
        {
            var v1manifest = JsonConvert.DeserializeObject<ManifestV1>(manifest);
            var image = new Image
            {
                History = v1manifest.History.Select(h => h.Parsed.ToHistory()),
                Layers = v1manifest.FsLayers.Select(l => l.ToLayer()),
                Digest = digest,
                Platform = new Platform
                {
                    Architecture = v1manifest.Architecture,
                    OS = "linux",
                    OSVerion = "unknown"
                }
            };

            try
            {
                return new ImageSet
                {
                    Date = image.History.Max(h => h.Created),
                    Images = new[] { image },
                    Platforms = new[] { image.Platform },
                    SetDigest = image.Digest
                };
            }
            catch (Exception)
            {
                return null;
            }
        }

        public IEnumerable<LayerIndex> GetIndexes(string repository, Image image, params string[] targets) =>
            filter.FilterLayers(GetRawIndexesAsync(repository, image), targets).ToEnumerable();

        /// <summary>
        /// Extracts raw file indexes from each layer in an image, working from the top down.
        /// </summary>
        /// <param name="image"></param>
        /// <param name="maxDepth">Maximum layers down to search. If 0, searches all layers</param>
        /// <returns></returns>
        async IAsyncEnumerable<LayerIndex> GetRawIndexesAsync(string repository, Image image, int maxDepth = 0)
        {
            var layers = image.Layers.Reverse();
            var depth = 1;
            foreach (var layer in layers)
            {
                List<string> files;
                using (var layerStream = await RecurseClient.GetLayerArchiveAsync(repository, layer.Digest))
                {
                    try
                    {
                        files = extractor.ExtractFiles(layerStream).ToList();
                    }
                    catch (SharpZipBaseException ex)
                    {
                        logger.LogError("Encountered corrupt layer archive, halting index.", ex);
                        yield break;
                    }
                }

                yield return new LayerIndex
                {
                    Depth = depth++,
                    Digest = layer.Digest,
                    Files = files
                };

                if (maxDepth > 0 && depth > maxDepth) { break; }
            }
        }

        public IEnumerable<Model.Repository> GetRepositories() => GetRepositories(repositoriesRoot)
            .Select(r => r.Replace('\\', '/'))
            .Select(r => new Model.Repository
            {
                Name = r,
                Tags = GetTags(r).Count(),
                Permissions = GetPermissionsAsync(r).Result
            });

        private IEnumerable<string> GetRepositories(string path)
        {
            foreach (var d in Directory.EnumerateDirectories(path))
            {
                if (new DirectoryInfo(d).Name.StartsWith("_"))
                {
                    continue;
                }
                else if (Directory.Exists(Path.Combine(d, tagsFolder)))
                {
                    if (Directory.EnumerateDirectories(Path.Combine(d, tagsFolder)).Count() > 0)
                    {
                        yield return new DirectoryInfo(d).Name;
                    }
                }
                else
                {
                    foreach (var sd in GetRepositories(d))
                    {
                        yield return Path.Combine(new DirectoryInfo(d).Name, sd);
                    }
                }
            }
        }

        public IEnumerable<string> GetTags(string repository)
        {
            var repoRoot = RepoPath(repository);
            var tagsRoot = Path.Combine(repoRoot, tagsFolder);

            var list = new List<string>();
            foreach (var t in Directory.EnumerateDirectories(tagsRoot))
            {
                list.Add(new DirectoryInfo(t).Name);
            }

            return list;
        }

        public Task<Stream> GetLayerArchiveAsync(string repository, string digest) => Task.FromResult<Stream>(
            new FileStream(BlobPath(digest), FileMode.Open, FileAccess.Read, FileShare.Read));

        public Task<Layer> GetLayerAsync(string repository, string layerDigest) =>
            Task.FromResult(new Layer
            {
                Digest = layerDigest,
                Size = new FileInfo(BlobPath(layerDigest)).Length
            });

        public async Task<string> GetTagDigestAsync(string repository, string tag) =>
            (await ReadFileAsync(TagLinkPath(repository, tag))).Trim();
    }
}
