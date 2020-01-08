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

namespace Whalerator.WebAPI
{
    public class RegistryFactory : IRegistryFactory
    {
        public RegistryFactory(RegistrySettings settings, ILogger<Registry> logger)
        {
            Settings = settings;
            Logger = logger;
        }

        public IRegistry GetRegistry(RegistryCredentials credentials)
        {
            credentials.Registry = credentials.Registry.ToLowerInvariant();
            if (Registry.DockerHubAliases.Contains(credentials.Registry))
            {
                credentials.Registry = Registry.DockerHub;
            }

            return new Registry(credentials, Settings, Logger);
        }

        public RegistrySettings Settings { get; }
        public ILogger<Registry> Logger { get; }
    }
}
