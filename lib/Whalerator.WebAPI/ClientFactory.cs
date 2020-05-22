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
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Whalerator.Client;
using Whalerator.Content;

namespace Whalerator.WebAPI
{
    public class ClientFactory : IClientFactory
    {
        private readonly IAufsFilter indexer;
        private readonly ILayerExtractor extractor;
        public RegistrySettings Settings { get; }
        public ILogger<Registry> Logger { get; }

        public ClientFactory(RegistrySettings settings, ILogger<Registry> logger, IAufsFilter indexer, ILayerExtractor extractor)
        {
            Settings = settings;
            Logger = logger;
            this.indexer = indexer;
            this.extractor = extractor;
        }

        public IDockerClient GetClient(RegistryCredentials credentials)
        {
            credentials.Registry = credentials.Registry.ToLowerInvariant();
            if (Registry.DockerHubAliases.Contains(credentials.Registry))
            {
                credentials.Registry = Registry.DockerHub;
            }


            if (string.IsNullOrEmpty(Settings.RegistryRoot))
            {
                var service = RestService.For<IDockerDistribution>(HostToEndpoint(credentials.Registry));
                var localClient = new LocalDockerClient(indexer, extractor) { RegistryRoot = Settings.LayerCache };
#warning i hate this
                var remoteClient =  new RemoteDockerClient(credentials, service, localClient);
                localClient.RecurseClient = remoteClient;

                return remoteClient;
            }
            else
            {
                return new LocalDockerClient(indexer, extractor) { RegistryRoot = Settings.RegistryRoot };
            }
        }

        #region Host Parsing

        // Docker uses some nonstandard names for Docker Hub
        public const string DockerHub = "registry-1.docker.io";
        public static HashSet<string> DockerHubAliases = new HashSet<string> {
            "docker.io",
            "hub.docker.io",
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

        public static string HostToEndpoint(string host)
        {
            host = DeAliasDockerHub(host);

            string scheme = null;
            // if the supplied hostname is missing the scheme, add one
            if (!hostWithScheme.IsMatch(host))
            {
                // if the hostname appears to include a port, assume plain http, otherwise assume https
                scheme = hostWithPort.IsMatch(host) ? "http://" : "https://";
            }

            return $"{scheme}{host.TrimEnd('/')}/v2";
        }

        #endregion
    }
}
