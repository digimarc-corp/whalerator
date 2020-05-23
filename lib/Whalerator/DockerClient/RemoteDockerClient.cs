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

using Org.BouncyCastle.Utilities;
using Refit;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using Whalerator.Client;
using Whalerator.Content;
using Whalerator.Model;

namespace Whalerator.DockerClient
{
    public class RemoteDockerClient : DockerClientBase, IDockerClient
    {
        private readonly IAuthHandler auth;
        private readonly IDockerDistribution dockerDistribution;
        private readonly ILocalDockerClient localClient;

        public RemoteDockerClient(IAuthHandler auth, IDockerDistribution dockerDistribution, ILocalDockerClient localClient) : base(auth)
        {
            this.auth = auth;
            this.dockerDistribution = dockerDistribution;
            this.localClient = localClient;
        }

        IDockerDistribution api => RestService.For<IDockerDistribution>(auth.RegistryEndpoint);

        private void LockFile(string path, Action action, CancellationToken token)
        {
            var key = Guid.NewGuid().ToString();
            var @lock = path + ".lock";
            while (File.Exists(@lock))
            {
                Thread.Sleep(500);
                token.ThrowIfCancellationRequested();
            }

            Directory.CreateDirectory(Path.GetDirectoryName(path));
            File.WriteAllText(@lock, key);
            var readKey = File.ReadAllText(@lock);
            if (readKey == key)
            {
                try
                {
                    action();
                }
                catch
                {
                    throw;
                }
                finally
                {
                    File.Delete(@lock);
                }
            }
            else
            {
                throw new Exception($"Could not get lock for {path}");
            }
        }

        private void FetchBlob(string repository, string layerDigest, string scope)
        {
            var path = localClient.BlobPath(layerDigest);
            var tokenSource = new CancellationTokenSource();
            tokenSource.CancelAfter(TimeSpan.FromMinutes(1));
            if (!File.Exists(path))
            {
                LockFile(path, () =>
                {
                    if (!File.Exists(path))
                    {
                        var result = dockerDistribution.GetStreamBlob(repository, layerDigest, scope).Result;
                        Directory.CreateDirectory(Path.GetDirectoryName(path));
                        using (var stream = new FileStream(path, FileMode.CreateNew, FileAccess.Write, FileShare.None))
                        {
                            result.CopyTo(stream);
                        }
                    }
                }, tokenSource.Token);
            }
        }

        public void DeleteImage(string repository, string imageDigest) => _ =
            dockerDistribution.DeleteManifest(repository, imageDigest, AuthHandler.RepoAdminScope(repository)).Result;


        public void DeleteRepository(string repository)
        {
            // there is no official Docker Distribution API to delete an entire repo. Instead, we
            // delete all tags from the repo via the API, then remove the (now empty) local folder

            var scope = AuthHandler.RepoAdminScope(repository);

            if (AuthHandler.Authorize(scope))
            {
                var key = RepoTagsKey(repository);

                // coerce to list before deleting to avoid deleting a tag out from under ourselves                
                var imageDigests = GetTags(repository).Select(t => GetTagDigest(repository, t)).Distinct().ToList();
                if (imageDigests.Any(d => d == null))
                {
                    //Logger.LogError($"Encountered at least one unreadable tag in {repository}, aborting");
                    return;
                }

                foreach (var digest in imageDigests)
                {
                    DeleteImage(repository, digest);
                }

                localClient.DeleteRepository(repository);
            }
            else
            {
                throw new UnauthorizedAccessException();
            }
        }

        public Stream GetFile(string repository, Layer layer, string path)
        {
            FetchBlob(repository, layer.Digest, AuthHandler.RepoPullScope(repository));

            return localClient.GetFile(repository, layer, path);
        }

        public ImageSet GetImageSet(string repository, string tag)
        {
            string digest = tag.IsDigest() ? tag : GetTagDigest(repository, tag);

            var path = localClient.BlobPath(digest);
            var tokenSource = new CancellationTokenSource();
            tokenSource.CancelAfter(TimeSpan.FromMinutes(1));

            if (!File.Exists(path))
            {
                LockFile(path, () =>
                {
                    if (!File.Exists(path))
                    {
                        var result = dockerDistribution.GetManifest(repository, digest, AuthHandler.RepoPullScope(repository)).Result;
                        Directory.CreateDirectory(Path.GetDirectoryName(path));
                        File.WriteAllText(path, result);
                    }
                }, tokenSource.Token);
            }

            return localClient.GetImageSet(repository, digest);
        }

        public Data.ImageConfig GetImageConfig(string repository, string digest)
        {
            var path = localClient.BlobPath(digest);
            var tokenSource = new CancellationTokenSource();
            tokenSource.CancelAfter(TimeSpan.FromMinutes(1));
            if (!File.Exists(path))
            {
                LockFile(path, () =>
                {
                    if (!File.Exists(path))
                    {
                        var result = dockerDistribution.GetStringBlob(repository, digest, AuthHandler.RepoPullScope(repository)).Result;
                        Directory.CreateDirectory(Path.GetDirectoryName(path));
                        File.WriteAllText(path, result);
                    }
                }, tokenSource.Token);
            }

            return localClient.GetImageConfig(repository, digest);
        }

        public IEnumerable<LayerIndex> GetIndexes(string repository, Image image, string target = null) => localClient.GetIndexes(repository, image, target);

        public Layer GetLayer(string repository, string layerDigest)
        {
            FetchBlob(repository, layerDigest, AuthHandler.RepoPullScope(repository));

            return localClient.GetLayer(repository, layerDigest);
        }

        public Stream GetLayerArchive(string repository, string layerDigest)
        {
            FetchBlob(repository, layerDigest, AuthHandler.RepoPullScope(repository));

            return localClient.GetLayerArchive(repository, layerDigest);
        }

        public string GetTagDigest(string repository, string tag)
        {
            var result = dockerDistribution.GetTagDigest(repository, tag, AuthHandler.RepoPullScope(repository)).Result;
            return result.Headers.First(h => h.Key.Equals("Docker-Content-Digest")).Value.First();
        }

        public IEnumerable<Repository> GetRepositories() => dockerDistribution.GetRepositoryListAsync(AuthHandler.CatalogScope())
            .Result.Repositories.Select(r => new Repository
            {
                Name = r,
                Tags = GetTags(r)?.Count() ?? 0,
                Permissions = GetPermissions(r)
            });

        public IEnumerable<string> GetTags(string repository)
        {
            try
            {
                return dockerDistribution.GetTagListAsync(repository, AuthHandler.RepoPullScope(repository)).Result.Tags;
            }
            catch (AggregateException ex)
            {
                if (ex.InnerExceptions.OfType<ApiException>().Any(e => e.StatusCode == System.Net.HttpStatusCode.NotFound))
                {
                    return new string[0];
                }
                else
                {
                    throw;
                }
            }
        }
    }
}
