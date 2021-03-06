﻿/*
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

        public bool CacheLocalData { get; set; }

        public CachedDockerClient(ServiceConfig config, IDockerClient innerClient, ICacheFactory cacheFactory, IAuthHandler auth) : base(config, auth)
        {
            this.innerClient = innerClient;
            this.cacheFactory = cacheFactory;
        }

        Task<T> ExecAsync<T>(string scope, string key, Func<Task<T>> func) where T : class => cacheFactory.Get<T>().ExecAsync(scope, key, AuthHandler, func);

        #region fully cached methods

        public IEnumerable<Model.Repository> GetRepositories() =>
            ExecAsync(AuthHandler.CatalogScope(), CatalogKey(), () => Task.FromResult(innerClient.GetRepositories())).Result;

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

        #endregion

        #region locally cached methods
        // these methods are normally cheap to run from disk, but in case of a high-latency filesystem (looking at you, Azure File) we can still cache them

        public Task<ImageSet> GetImageSetAsync(string repository, string tag) => CacheLocalData ?
            ExecAsync(AuthHandler.RepoPullScope(repository), RepoImageSetKey(repository, tag), () => innerClient.GetImageSetAsync(repository, tag)) :
            innerClient.GetImageSetAsync(repository, tag);

        public Task<Layer> GetLayerAsync(string repository, string layerDigest) => CacheLocalData ?
            ExecAsync(AuthHandler.RepoPullScope(repository), ObjectDigestKey(layerDigest), () => innerClient.GetLayerAsync(repository, layerDigest)) :
            innerClient.GetLayerAsync(repository, layerDigest);

        public Task<ImageConfig> GetImageConfigAsync(string repository, string digest) => CacheLocalData ?
            ExecAsync(AuthHandler.RepoPullScope(repository), ObjectDigestKey(digest), () => innerClient.GetImageConfigAsync(repository, digest)) :
            innerClient.GetImageConfigAsync(repository, digest);

        #endregion

        #region passthrough methods

        public IEnumerable<LayerIndex> GetIndexes(string repository, Image image, params string[] targets) => innerClient.GetIndexes(repository, image, targets);

        public Task<Stream> GetFileAsync(string repository, Layer layer, string path) => innerClient.GetFileAsync(repository, layer, path);

        public Task<Stream> GetLayerArchiveAsync(string repository, string layerDigest) => innerClient.GetLayerArchiveAsync(repository, layerDigest);

        #endregion
    }
}
