using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading.Tasks;
using Whalerator.Config;
using Whalerator.Data;
using Whalerator.Model;

namespace Whalerator.Client
{
    public class LocalDistributionClient : DistributionClientBase
    {
        public LocalDistributionClient(ServiceConfig config)
        {
            Config = config;
        }

        public override string Host { get; set; } = Registry.DockerHub;
        public ServiceConfig Config { get; }

        string registryRoot => Config.RegistryRoot;
        string repositoriesRoot => Path.Combine(registryRoot, "docker/registry/v2/repositories");
        string blobsRoot => Path.Combine(registryRoot, "docker/registry/v2/blobs");

        string DigestToPath(string digest)
        {
            var parts = digest.Split(':');
            if (parts.Length != 2) { throw new FormatException("Could not parse as a digest string"); }
            else
            {
                return string.Join("/", parts[0], parts[1].Substring(0, 2), parts[1]);
            }
        }

        public override Task DeleteImage(string repository, string digest)
        {
            throw new NotImplementedException();
        }

        public override Task<Stream> GetBlobAsync(string repository, string digest)
        {
            var blobPath = Path.Combine(blobsRoot, DigestToPath(digest), "data");
            return Task.FromResult((Stream)new FileStream(blobPath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite));
        }

        public override Task<(Uri path, AuthenticationHeaderValue auth)> GetBlobPathAndAuthorizationAsync(string repository, string digest)
        {
            throw new NotImplementedException();
        }

        public override Task<ImageSet> GetImageSet(string repository, string tag)
        {
            string digest;
            if (tag.StartsWith("sha256:"))
            {
                digest = tag;
            }
            else
            {

                var repoRoot = Path.Combine(repositoriesRoot, repository);
                var tagLink = Path.Combine(repoRoot, "_manifests/tags", tag, "current/link");
                digest = File.ReadAllText(tagLink).Trim();
            }

            var manifestPath = Path.Combine(blobsRoot, DigestToPath(digest), "data");
            var manifest = File.ReadAllText(manifestPath);
            var mediaType = (string)(JObject.Parse(manifest)["mediaType"]);

            // In docker-land, a fat manifest is just a bunch of regular manifests bundled together under a single digest
            // In whalerator-land, there are no non-fat manifests, just fat manifests with a single image
            if (mediaType.StartsWith("application/vnd.docker.distribution.manifest.list.v2"))
            {
                var images = new List<Image>();
                var fatManifest = JsonConvert.DeserializeObject<FatManifest>(manifest);
                foreach (var subManifest in fatManifest.Manifests)
                {
                    images.Add(GetImageSet(repository, subManifest.Digest).Result.Images.First());
                }

                return Task.FromResult(new ImageSet
                {
                    Date = images.SelectMany(i => i.History.Select(h => h.Created)).Max(),
                    Images = images,
                    Platforms = images.Select(i => i.Platform),
                    SetDigest = digest,
                });
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

                return Task.FromResult(new ImageSet
                {
                    Date = image.History.Max(h => h.Created),
                    Images = new[] { image },
                    Platforms = new[] { image.Platform },
                    SetDigest = image.Digest
                });
            }
            else
            {
                throw new Exception($"Cannot build image set from mediatype '{mediaType}'");
            }
        }

        public override Task<RepositoryList> GetRepositoriesAsync() =>
            Task.FromResult(new RepositoryList { Repositories = GetRepositories(repositoriesRoot).Select(r => r.Replace('\\', '/')) });


        private IEnumerable<string> GetRepositories(string path)
        {
            foreach (var d in Directory.EnumerateDirectories(path))
            {
                if (new DirectoryInfo(d).Name.StartsWith("_"))
                {
                    continue;
                }
                else if (Directory.Exists(Path.Combine(d, "_manifests/tags")))
                {
                    if (Directory.EnumerateDirectories(Path.Combine(d, "_manifests/tags")).Count() > 0)
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

        public override Task<TagList> GetTagsAsync(string repository)
        {
            var repoRoot = Path.Combine(repositoriesRoot, repository);
            var tagsRoot = Path.Combine(repoRoot, "_manifests/tags");

            var list = new List<string>();
            foreach (var t in Directory.EnumerateDirectories(tagsRoot))
            {
                list.Add(new DirectoryInfo(t).Name);
            }

            return Task.FromResult(new TagList { Tags = list });
        }
    }
}
