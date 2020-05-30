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
using System.Linq;
using System.Threading.Tasks;

namespace Whalerator.Config
{
    public class ServiceConfig
    {
        public string RedisCache { get; set; }
        public int CacheTtl { get; set; } = 3600;

        public string IndexFolder { get; set; }

        public List<Theme> Themes { get; set; } = new List<Theme>();

        public bool Vulnerabilities { get; set; }

        public int MaxSearchDepth { get; set; }

        public List<string> Documents { get; set; } = new List<string>();

        public bool CaseInsensitive { get; set; }

        public int AuthTokenLifetime { get; set; } = 2419200;
        public string AuthTokenKey { get; set; }

        public string Registry { get; set; }
        public string RegistryRoot { get; set; }
        public string RegistryUser { get; set; }
        public string RegistryPassword { get; set; }
        public string RegistryCache { get; set; }
        public List<RegistryAlias> RegistryAliases { get; set; } = new List<RegistryAlias>();
        
        /// <summary>
        /// Allows internal clients to ignore RegistryAliases, while wtill applying them to external services like Clair. Primarily for dev use, ex. running whalerator
        /// services locally while Registry and Clair are in a docker-compose env.
        /// </summary>
        public bool IgnoreInternalAlias { get; set; }
        public bool AutoLogin { get; set; }

        public List<string> Repositories { get; set; } = new List<string>();
        public List<string> HiddenRepositories { get; set; } = new List<string>();

        public bool IndexWorker { get; set; }
        public bool ClairWorker { get; set; }
        public string ClairApi { get; set; }
    }
}