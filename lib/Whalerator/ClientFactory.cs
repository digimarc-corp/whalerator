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

using Microsoft.Extensions.Logging;
using Refit;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Whalerator.Client;
using Whalerator.Config;
using Whalerator.Content;
using Whalerator.DockerClient;

namespace Whalerator
{
    public class ClientFactory : IClientFactory
    {
        private readonly ServiceConfig config;
        private readonly ILoggerFactory loggerFactory;
        private readonly ILogger<ClientFactory> logger;
        private readonly IAufsFilter indexer;
        private readonly ILayerExtractor extractor;
        private readonly ICacheFactory cacheFactory;

        public ClientFactory(ServiceConfig config, ILoggerFactory loggerFactory, IAufsFilter indexer, ILayerExtractor extractor, ICacheFactory cacheFactory)
        {
            this.config = config;
            this.loggerFactory = loggerFactory;
            logger = loggerFactory.CreateLogger<ClientFactory>();
            this.indexer = indexer;
            this.extractor = extractor;
            this.cacheFactory = cacheFactory;
        }

        Func<HttpRequestMessage, Task<string>> ClientTokenCallback(IAuthHandler auth) => (message) =>
        {
            // the Refit interface must set this header for each request, so we know what kind of token to get here.
            var scope = message.Headers.First(h => h.Key.Equals("X-Docker-Scope")).Value.First();
            var token = auth.TokensRequired && auth.AuthorizeAsync(scope).Result ? auth.GetAuthorizationAsync(scope).Result?.Parameter : null;

            return Task.FromResult(token);
        };

        public IDockerClient GetClient(IAuthHandler auth)
        {
            var host = auth.GetRegistryHost(config.IgnoreInternalAlias);
            if (string.IsNullOrEmpty(config.RegistryRoot))
            {
                /* this is convoluted. The local client needs to call back to the remote client to fetch layers or other blobs as needed, but the remote client needs to call down 
                 * to the local client to actually load data, interperet manifests, etc. So, a circular dependency exists. Still waiting on an epiphany to make it cleaner.
                 */

                var httpClient = new HttpClient(new AuthenticatedParameterizedHttpClientHandler(ClientTokenCallback(auth))) { BaseAddress = new Uri(RegistryCredentials.HostToEndpoint(host)) };
                var service = RestService.For<IDockerDistribution>(httpClient);
                var localClient = new LocalDockerClient(config, indexer, extractor, auth, loggerFactory.CreateLogger<LocalDockerClient>()) { RegistryRoot = config.RegistryCache, Host = host };
                var remoteClient = new RemoteDockerClient(config, auth, service, localClient, cacheFactory) { Host = host };
                localClient.RecurseClient = remoteClient;

                var cachedClient = new CachedDockerClient(config, remoteClient, cacheFactory, auth) { Host = host, CacheLocalData = config.LocalCache };

                return cachedClient;
            }
            else if (config.LocalCache)
            {
                var localClient = new LocalDockerClient(config, indexer, extractor, auth, loggerFactory.CreateLogger<LocalDockerClient>()) { RegistryRoot = config.RegistryRoot };
                var cachedClient = new CachedDockerClient(config, localClient, cacheFactory, auth) { Host = host, CacheLocalData = config.LocalCache };
                return cachedClient;
            }
            else
            {
                return new LocalDockerClient(config, indexer, extractor, auth, loggerFactory.CreateLogger<LocalDockerClient>()) { RegistryRoot = config.RegistryRoot };
            }
        }
    }
}
