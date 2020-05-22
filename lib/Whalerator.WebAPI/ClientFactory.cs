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
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Whalerator.Client;
using Whalerator.Content;

namespace Whalerator.WebAPI
{
    public class ClientFactory : IClientFactory
    {
        private readonly IAufsFilter indexer;
        private readonly ILayerExtractor extractor;

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
                throw new NotImplementedException();
            }
            else
            {
                return new LocalDockerClient(indexer, extractor) { RegistryRoot = Settings.RegistryRoot };
            }
        }

        public RegistrySettings Settings { get; }
        public ILogger<Registry> Logger { get; }
    }
}
