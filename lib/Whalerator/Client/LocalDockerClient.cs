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

using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using Whalerator.Content;
using Whalerator.Data;
using Whalerator.Model;

namespace Whalerator.Client
{
    public class LocalDockerClient : IDockerClient
    {
        public LocalDockerClient(IAufsFilter filter, ILayerExtractor extractor)
        {
            this.filter = filter;
            this.extractor = extractor;
        }

        private readonly IAufsFilter filter;
        private readonly ILayerExtractor extractor;

        public string RegistryRoot { get; set; }
        string repositoriesRoot => Path.Combine(RegistryRoot, "docker/registry/v2/repositories");
        string blobsRoot => Path.Combine(RegistryRoot, "docker/registry/v2/blobs");
        private const string tagsFolder = "_manifests/tags";

        ImageConfig GetImageConfig(string repository, string digest)
        {
            var result = GetBlob(repository, digest);
            var json = new StreamReader(result).ReadToEnd();
            return JsonConvert.DeserializeObject<ImageConfig>(json);
        }

        public Stream GetBlob(string repository, string digest) =>
            new FileStream(Path.Combine(blobsRoot, digest.ToDigestPath(), "data"), FileMode.Open, FileAccess.Read, FileShare.ReadWrite);

        public void DeleteImage(string repository, string digest)
        {
            throw new NotImplementedException();
        }

        public void DeleteRepository(string repository)
        {
            throw new NotImplementedException();
        }

        public Stream GetFile(Layer layer, string path) => extractor.ExtractFile(Path.Combine(blobsRoot, layer.Digest.ToDigestPath(), "data"), path);

        public ImageSet GetImageSet(string repository, string tag)
        {
            string digest;
            if (tag.StartsWith("sha256:"))
            {
                digest = tag;
            }
            else
            {
                var repoRoot = Path.Combine(repositoriesRoot, repository);
                var tagLink = Path.Combine(repoRoot, tagsFolder, tag, "current/link");
                digest = File.ReadAllText(tagLink).Trim();
            }

            var manifestPath = Path.Combine(blobsRoot, digest.ToDigestPath(), "data");
            var manifest = File.ReadAllText(manifestPath);
            var mediaType = (string)JObject.Parse(manifest)["mediaType"];

            ImageSet imageSet;

            // In docker-land, a fat manifest is just a bunch of regular manifests bundled together under a single digest
            // In whalerator-land, there are no non-fat manifests, just fat manifests with a single image
            if (mediaType.StartsWith("application/vnd.docker.distribution.manifest.list.v2"))
            {
                var images = new List<Image>();
                var fatManifest = JsonConvert.DeserializeObject<FatManifest>(manifest);
                foreach (var subManifest in fatManifest.Manifests)
                {
                    images.Add(GetImageSet(repository, subManifest.Digest).Images.First());
                }

                imageSet = new ImageSet
                {
                    Date = images.SelectMany(i => i.History.Select(h => h.Created)).Max(),
                    Images = images,
                    Platforms = images.Select(i => i.Platform),
                    SetDigest = digest,
                };
            }
            else if (mediaType.StartsWith("application/vnd.docker.distribution.manifest.v2"))
            {
                var thinManifest = JsonConvert.DeserializeObject<ManifestV2>(manifest);
                var config = GetImageConfig(repository, thinManifest.Config.Digest);
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
                image.Layers = thinManifest.Layers.Select(l => l.ToLayer());

                imageSet = new ImageSet
                {
                    Date = image.History.Max(h => h.Created),
                    Images = new[] { image },
                    Platforms = new[] { image.Platform },
                    SetDigest = image.Digest
                };
            }
            else
            {
                throw new Exception($"Cannot build image set from mediatype '{mediaType}'");
            }

            return imageSet;
        }

        public IEnumerable<LayerIndex> GetIndexes(Image image, string targetPath) => filter.FilterLayers(GetLayers(image), targetPath);

        /// <summary>
        /// Extracts raw file indexes from each layer in an image, working from the top down.
        /// </summary>
        /// <param name="image"></param>
        /// <param name="maxDepth">Maximum layers down to search. If 0, searches all layers</param>
        /// <returns></returns>
        public IEnumerable<LayerIndex> GetLayers(Image image, int maxDepth = 0)
        {
            var layers = image.Layers.Reverse();
            var depth = 1;
            foreach (var layer in layers)
            {
                var layerStream = GetLayerArchive(layer.Digest);
                yield return new LayerIndex
                {
                    Depth = depth++,
                    Digest = layer.Digest,
                    Files = extractor.ExtractFiles(layerStream).ToList()
                };

                if (maxDepth > 0 && depth > maxDepth) { break; }
            }
        }

        public LayerProxyInfo GetLayerProxyInfo(string repository, Layer layer, IEnumerable<(string External, string Internal)> aliases)
        {
            throw new NotImplementedException();
        }

        public Permissions GetPermissions(string repository)
        {
            throw new NotImplementedException();
        }

        public IEnumerable<Model.Repository> GetRepositories() => GetRepositories(repositoriesRoot)
            .Select(r => r.Replace('\\', '/'))
            .Select(r => new Model.Repository
            {
                Name = r,
                Tags = GetTags(r).Count(),
                Permissions = Permissions.Pull
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
            var repoRoot = Path.Combine(repositoriesRoot, repository);
            var tagsRoot = Path.Combine(repoRoot, tagsFolder);

            var list = new List<string>();
            foreach (var t in Directory.EnumerateDirectories(tagsRoot))
            {
                list.Add(new DirectoryInfo(t).Name);
            }

            return list;
        }

        public Stream GetLayerArchive(string digest) => new FileStream(Path.Combine(blobsRoot, digest.ToDigestPath(), "data"), FileMode.Open, FileAccess.Read, FileShare.Read);

        public Layer GetLayer(string layerDigest)
        {
            var info = new FileInfo(Path.Combine(blobsRoot, layerDigest.ToDigestPath(), "data"));
            return new Layer
            {
                Digest = layerDigest,
                Size = info.Length
            };
        }
    }
}
