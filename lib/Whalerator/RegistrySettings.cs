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
using System.Text;
using Whalerator.Client;

namespace Whalerator
{
    public class RegistrySettings
    {
        public Func<string, IAuthHandler, IDistributionClient> DistributionFactory { get; set; } = (host, handler) => new DistributionClient(handler) { Host = host };
        public ICacheFactory CacheFactory { get; set; }
        public IAuthHandler AuthHandler { get; set; }

        /// <summary>
        /// Optional handler used only for fetching the initial catalog list. Important for scenarios where users may not have full catalog access, but should
        /// still be able to view specific repos that they have access too.
        /// </summary>
        public IAuthHandler CatalogAuthHandler { get; set; }

        /// <summary>
        /// Optional list of repositories that will always be checked for authorization, even if no catalog access is available.
        /// </summary>
        public List<string> StaticRepos { get; set; }

        /// <summary>
        /// Optional list of repositories that will always be hidden, even if the user has access to it otherwise. Takes precedence over StaticRepos.
        /// </summary>
        public List<string> HiddenRepos { get; set; }

        public string LayerCache { get; set; }

        public TimeSpan? StaticTtl { get; set; } = null;
        public TimeSpan VolatileTtl { get; set; }
    }
}
