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

namespace Whalerator.WebAPI
{
    public class Config
    {
        public CacheConfig Cache { get; set; }
        public class CacheConfig
        {
            public string Redis { get; set; }
            public string LayerCache { get; set; }
            public int VolatileTtl { get; set; } = 900; // 15 minutes
            public int StaticTtl { get; set; } = 0;
        }

        public SearchConfig Search { get; set; }
        public class SearchConfig
        {
            public int? Depth { get; set; } = 4;
            public List<string> Filelists { get; set; }
        }

        public SecurityConfig Security { get; set; }
        public class SecurityConfig
        {
            public long TokenLifetime { get; set; }
            public string PrivateKey { get; set; }
        }

        public CatalogConfig Catalog { get; set; }
        public class CatalogConfig
        {
            public string Registry { get; set; }
            public List<string> Repositories { get; set; }
            public List<string> Hidden { get; set; }
            public CatalogUser User { get; set; }
            public class CatalogUser
            {
                public string Username { get; set; }
                public string Password { get; set; }
                public int PollingInterval { get; set; } = 0;
            }
        }
    }
}