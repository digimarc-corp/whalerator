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
using System.Text;
using Whalerator.Client;
using Whalerator.Content;
using Whalerator.Data;
using Whalerator.Model;

namespace Whalerator.DockerClient
{
    public class CachedDockerClient : DockerClientBase, IDockerClient
    {
        private readonly IDockerClient innerClient;
        private readonly ICacheFactory cacheFactory;

        public CachedDockerClient(IDockerClient innerClient, ICacheFactory cacheFactory, IAuthHandler auth) : base(auth)
        {
            this.innerClient = innerClient;
            this.cacheFactory = cacheFactory;
        }

        T Exec<T>(string scope, string key, Func<T> func) where T : class => cacheFactory.Get<T>().Exec(scope, key, AuthHandler, func);

        public LayerProxyInfo GetLayerProxyInfo(string repository, Layer layer, IEnumerable<(string External, string Internal)> aliases) => throw new NotImplementedException();

        public IEnumerable<Model.Repository> GetRepositories() =>
            Exec(AuthHandler.CatalogScope(), CatalogKey("bob"), () => innerClient.GetRepositories());

        public string GetTagDigest(string repository, string tag) =>
            Exec(AuthHandler.RepoPullScope(repository), RepoTagDigestKey(repository, tag), () => innerClient.GetTagDigest(repository, tag));

        public IEnumerable<string> GetTags(string repository) =>
            Exec(AuthHandler.RepoPullScope(repository), RepoTagsKey(repository), () => innerClient.GetTags(repository));

        #region passthrough methods

        public void DeleteImage(string repository, string imageDigest) => innerClient.DeleteImage(repository, imageDigest);

        public void DeleteRepository(string repository) => innerClient.DeleteRepository(repository);

        public Stream GetFile(string repository, Layer layer, string path) => innerClient.GetFile(repository, layer, path);

        public ImageConfig GetImageConfig(string repository, string digest) => innerClient.GetImageConfig(repository, digest);

        public ImageSet GetImageSet(string repository, string tag) => innerClient.GetImageSet(repository, tag);

        public IEnumerable<LayerIndex> GetIndexes(string repository, Image image, string target = null) => innerClient.GetIndexes(repository, image, target);

        public Layer GetLayer(string repository, string layerDigest) => innerClient.GetLayer(repository, layerDigest);

        public Stream GetLayerArchive(string repository, string layerDigest) => innerClient.GetLayerArchive(repository, layerDigest);

        #endregion
    }
}
