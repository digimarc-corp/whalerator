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
using System.Threading.Tasks;
using Whalerator.Client;
using Whalerator.Config;
using Whalerator.Content;
using Whalerator.Data;
using Whalerator.Model;

namespace Whalerator.DockerClient
{
    public class RemoteDockerClient : DockerClientBase, IDockerClient
    {
        private readonly IAuthHandler auth;
        private readonly IDockerDistribution dockerDistribution;
        private readonly ILocalDockerClient localClient;
        private readonly ICacheFactory cacheFactory;

        public TimeSpan DownloadTimeout { get; set; } = TimeSpan.FromMinutes(5);

        public RemoteDockerClient(ServiceConfig config, IAuthHandler auth, IDockerDistribution dockerDistribution, ILocalDockerClient localClient, ICacheFactory cacheFactory) : base(config, auth)
        {
            this.auth = auth;
            this.dockerDistribution = dockerDistribution;
            this.localClient = localClient;
            this.cacheFactory = cacheFactory;
        }

        IDockerDistribution api => RestService.For<IDockerDistribution>(auth.RegistryEndpoint);

        private async Task FetchBlobAsync(string repository, string layerDigest, string scope)
        {
            var path = localClient.BlobPath(layerDigest);
            await LockedPathExecAsync(path, async () =>
            {
                var result = await dockerDistribution.GetStreamBlobAsync(repository, layerDigest, scope);
                using (var stream = new FileStream(path, FileMode.CreateNew, FileAccess.Write, FileShare.None))
                {
                    await result.CopyToAsync(stream);
                }
            });
        }

        public async Task DeleteImageAsync(string repository, string imageDigest) => _ =
            await dockerDistribution.DeleteManifestAsync(repository, imageDigest, AuthHandler.RepoAdminScope(repository));

        public async Task DeleteRepositoryAsync(string repository)
        {
            // there is no official Docker Distribution API to delete an entire repo. Instead, we
            // delete all tags from the repo via the API, then remove the (now empty) local folder

            var scope = AuthHandler.RepoAdminScope(repository);

            if (AuthHandler.Authorize(scope))
            {
                var key = RepoTagsKey(repository);

                // coerce to list before deleting to avoid deleting a tag out from under ourselves     
                var tags = GetTags(repository).ToAsyncEnumerable();
                var imageDigests = await tags.SelectAwait(async t => await GetTagDigestAsync(repository, t)).Distinct().ToListAsync();
                if (imageDigests.Any(d => d == null))
                {
                    //Logger.LogError($"Encountered at least one unreadable tag in {repository}, aborting");
                    return;
                }

                foreach (var digest in imageDigests)
                {
                    await DeleteImageAsync(repository, digest);
                }

                await localClient.DeleteRepositoryAsync(repository);
            }
            else
            {
                throw new UnauthorizedAccessException();
            }
        }

        public async Task<Stream> GetFileAsync(string repository, Layer layer, string path)
        {
            await FetchBlobAsync(repository, layer.Digest, AuthHandler.RepoPullScope(repository));

            return await localClient.GetFileAsync(repository, layer, path);
        }

        // If the path does not exist, execute the given action with a lock
        private async Task LockedPathExecAsync(string path, Func<Task> action)
        {
            if (!File.Exists(path))
            {
                using (var @lock = await cacheFactory.Get<object>().TakeLockAsync($"dl:{path}", DownloadTimeout, DownloadTimeout))
                {
                    // double check the path wasn't created while we waited for a lock
                    if (!File.Exists(path))
                    {
                        Directory.CreateDirectory(Path.GetDirectoryName(path));
                        await action();
                    }
                }
            }
        }

        public async Task<ImageSet> GetImageSetAsync(string repository, string tag)
        {
            string digest = tag.IsDigest() ? tag : await GetTagDigestAsync(repository, tag);

            var path = localClient.BlobPath(digest);

            await LockedPathExecAsync(path, async () =>
            {
                var result = await dockerDistribution.GetManifestAsync(repository, digest, AuthHandler.RepoPullScope(repository));
                await WriteFileAsync(path, result);
            });

            return await localClient.GetImageSetAsync(repository, digest);
        }

        public async Task<Data.ImageConfig> GetImageConfigAsync(string repository, string digest)
        {
            var path = localClient.BlobPath(digest);

            await LockedPathExecAsync(path, async () =>
            {
                var result = await dockerDistribution.GetStringBlobAsync(repository, digest, AuthHandler.RepoPullScope(repository));
                await WriteFileAsync(path, result);
            });

            return await localClient.GetImageConfigAsync(repository, digest);
        }

        public IEnumerable<LayerIndex> GetIndexes(string repository, Image image, params string[] targets) => localClient.GetIndexes(repository, image, targets);

        public async Task<Layer> GetLayerAsync(string repository, string layerDigest)
        {
            await FetchBlobAsync(repository, layerDigest, AuthHandler.RepoPullScope(repository));

            return await localClient.GetLayerAsync(repository, layerDigest);
        }

        public async Task<Stream> GetLayerArchiveAsync(string repository, string layerDigest)
        {
            await FetchBlobAsync(repository, layerDigest, AuthHandler.RepoPullScope(repository));

            return await localClient.GetLayerArchiveAsync(repository, layerDigest);
        }

        public async Task<string> GetTagDigestAsync(string repository, string tag)
        {
            var result = await dockerDistribution.GetTagDigestAsync(repository, tag, AuthHandler.RepoPullScope(repository));
            return result.Headers.First(h => h.Key.Equals("Docker-Content-Digest")).Value.First();
        }

        public IEnumerable<Model.Repository> GetRepositories() => GetLiveRepositoriesAsync().Result.Where(r => r.Tags > 0);

        async Task<IEnumerable<Model.Repository>> GetLiveRepositoriesAsync()
        {
            try
            {
                var repos = (await dockerDistribution.GetRepositoryListAsync(AuthHandler.CatalogScope())).Repositories
                    .Union(Config.Repositories).Distinct().Select(r => new Model.Repository { Name = r }).ToList();

                var perms = repos.Select(async r => r.Permissions = await GetPermissionsAsync(r.Name));
                Task.WaitAll(perms.ToArray());

                repos.Where(r => r.Permissions >= Permissions.Pull).ToList().ForEach(r => r.Tags = GetTags(r.Name).Count());


                return repos;
            }
            catch (AuthenticationException)
            {
                return new Model.Repository[0];
            }
        }

        public IEnumerable<string> GetTags(string repository)
        {
            try
            {
                var result = dockerDistribution.GetTagListAsync(repository, AuthHandler.RepoPullScope(repository)).Result;
                return result?.Tags ?? new string[0];
            }
            catch (AggregateException ex)
            {
                if (ex.InnerException is ApiException && ((ApiException)ex.InnerException).StatusCode == System.Net.HttpStatusCode.NotFound)
                {
                    return new string[0];
                }
                else
                {
                    throw;
                }
            }/*
            catch (ApiException ex)
            {
                if (ex.StatusCode == System.Net.HttpStatusCode.NotFound)
                {
                    return new string[0];
                }
                else
                {
                    throw;
                }
            }*/
        }
    }
}
