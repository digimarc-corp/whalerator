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
    public class CachedDockerClient : DockerClientBase, IDockerClient
    {
        private readonly IDockerClient innerClient;
        private readonly ICacheFactory cacheFactory;

        public CachedDockerClient(ServiceConfig config, IDockerClient innerClient, ICacheFactory cacheFactory, IAuthHandler auth) : base(config, auth)
        {
            this.innerClient = innerClient;
            this.cacheFactory = cacheFactory;
        }

        Task<T> ExecAsync<T>(string scope, string key, Func<Task<T>> func) where T : class => cacheFactory.Get<T>().ExecAsync(scope, key, AuthHandler, func);

        public IEnumerable<Model.Repository> GetRepositories()
        {
            if (AuthHandler.Authorize(AuthHandler.CatalogScope()))
            {
                return ExecAsync(AuthHandler.CatalogScope(), CatalogKey(), () => Task.FromResult(innerClient.GetRepositories())).Result;
            }
            else
            {
                // the user doesn't have catalog permissions, but we can try to construct a synthetic catalog from configured static repos
                var repos = Config.Repositories?.Select(r => new Model.Repository { Name = r, });
                var perms = repos.Select(async r => r.Permissions = await GetPermissionsAsync(r.Name));
                Task.WaitAll(perms.ToArray());

                var tags = repos.Where(r => r.Permissions >= Permissions.Pull).Select(async r => r.Tags = GetTags(r.Name).Count());
                Task.WaitAll(tags.ToArray());

                return repos.Where(r => r.Tags > 0);
            }
        }

        public Task<string> GetTagDigestAsync(string repository, string tag) =>
            ExecAsync(AuthHandler.RepoPullScope(repository), RepoTagDigestKey(repository, tag), () => innerClient.GetTagDigestAsync(repository, tag));

        public IEnumerable<string> GetTags(string repository) =>
            ExecAsync(AuthHandler.RepoPullScope(repository), RepoTagsKey(repository), () => Task.FromResult(innerClient.GetTags(repository))).Result;

        public async Task DeleteImageAsync(string repository, string imageDigest)
        {
            // we're only deleting, so we don't care about a type arg
            var cache = cacheFactory.Get<object>();

            await cache.TryDeleteAsync(CatalogKey());
            await cache.TryDeleteAsync(RepoTagsKey(repository));

            await innerClient.DeleteImageAsync(repository, imageDigest);
        }

        public async Task DeleteRepositoryAsync(string repository)
        {
            // we're only deleting, so we don't care about a type arg
            var cache = cacheFactory.Get<object>();

            await cache.TryDeleteAsync(CatalogKey());
            await cache.TryDeleteAsync(RepoTagsKey(repository));

            await innerClient.DeleteRepositoryAsync(repository);
        }

        #region passthrough methods

        public Task<Stream> GetFileAsync(string repository, Layer layer, string path) => innerClient.GetFileAsync(repository, layer, path);

        public Task<ImageConfig> GetImageConfigAsync(string repository, string digest) => innerClient.GetImageConfigAsync(repository, digest);

        public Task<ImageSet> GetImageSetAsync(string repository, string tag) => innerClient.GetImageSetAsync(repository, tag);

        public IEnumerable<LayerIndex> GetIndexes(string repository, Image image, params string[] targets) => innerClient.GetIndexes(repository, image, targets);

        public Task<Layer> GetLayerAsync(string repository, string layerDigest) => innerClient.GetLayerAsync(repository, layerDigest);

        public Task<Stream> GetLayerArchiveAsync(string repository, string layerDigest) => innerClient.GetLayerArchiveAsync(repository, layerDigest);

        #endregion
    }
}
