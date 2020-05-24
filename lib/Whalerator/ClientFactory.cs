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
        private readonly IAuthHandler auth;

        public ClientFactory(ServiceConfig config, ILoggerFactory loggerFactory, IAufsFilter indexer, ILayerExtractor extractor, ICacheFactory cacheFactory, IAuthHandler auth)
        {
            this.config = config;
            this.loggerFactory = loggerFactory;
            logger = loggerFactory.CreateLogger<ClientFactory>();
            this.indexer = indexer;
            this.extractor = extractor;
            this.cacheFactory = cacheFactory;
            this.auth = auth;
        }

        public IDockerClient GetClient(RegistryCredentials credentials)
        {
            credentials.Registry = credentials.Registry.ToLowerInvariant();
            if (DockerHubAliases.Contains(credentials.Registry))
            {
                credentials.Registry = DockerHub;
            }

            auth.Login(credentials.Registry, credentials.Username, credentials.Password);

            if (string.IsNullOrEmpty(config.RegistryRoot))
            {
                async Task<string> GetToken(HttpRequestMessage message)
                {
                    var scope = message.Headers.First(h => h.Key.Equals("X-Docker-Scope")).Value.First();
                    var token = auth.Authorize(scope) ? auth.GetAuthorization(scope).Parameter : null;

                    return token;
                }

#warning i hate this

                var httpClient = new HttpClient(new AuthenticatedParameterizedHttpClientHandler(GetToken)) { BaseAddress = new Uri(HostToEndpoint(credentials.Registry)) };
                var service = RestService.For<IDockerDistribution>(httpClient);
                var localClient = new LocalDockerClient(config, indexer, extractor, auth, loggerFactory.CreateLogger<LocalDockerClient>()) { RegistryRoot = config.RegistryCache, Host = credentials.Registry };
                var remoteClient = new RemoteDockerClient(config, auth, service, localClient) { Host = credentials.Registry };
                localClient.RecurseClient = remoteClient;

                var cachedClient = new CachedDockerClient(config, remoteClient, cacheFactory, auth) { Host = credentials.Registry };

                return cachedClient;
            }
            else
            {
                return new LocalDockerClient(config, indexer, extractor, auth, loggerFactory.CreateLogger<LocalDockerClient>()) { RegistryRoot = config.RegistryRoot };
            }
        }

        #region Host Parsing

        // Docker uses some nonstandard names for Docker Hub
        public const string DockerHub = "registry-1.docker.io";
        public static HashSet<string> DockerHubAliases = new HashSet<string> {
            "docker.io",
            "hub.docker.io",
            "hub.docker.com",
            "registry.docker.io",
            "registry-1.docker.io"
        };

        // when working anonymously against docker hub, it's helpful to know the Realm and Service ahead of time
        public const string DockerRealm = "https://auth.docker.io/token";
        public const string DockerService = "registry.docker.io";

        // If this is a Docker hub alias, replace it with the canonical registry name.
        public static string DeAliasDockerHub(string host) =>
            DockerHubAliases.Contains(host.ToLowerInvariant()) ? DockerHub : host;

        static Regex hostWithScheme = new Regex(@"\w+:\/\/.+", RegexOptions.Compiled);
        static Regex hostWithPort = new Regex(@".+:\d+$", RegexOptions.Compiled);

        public static string HostToEndpoint(string host, string resource = null)
        {
            host = DeAliasDockerHub(host);

            string scheme = null;
            // if the supplied hostname is missing the scheme, add one
            if (!hostWithScheme.IsMatch(host))
            {
                // if the hostname appears to include a port, assume plain http, otherwise assume https
                scheme = hostWithPort.IsMatch(host) ? "http://" : "https://";
            }

            return $"{scheme}{host.TrimEnd('/')}/v2{(string.IsNullOrEmpty(resource) ? "" : "/")}";
        }

        #endregion
    }
}
