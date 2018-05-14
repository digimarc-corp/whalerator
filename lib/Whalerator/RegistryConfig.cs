using System;
using System.Collections.Generic;
using System.Text;
using Whalerator.Client;

namespace Whalerator
{
    public class RegistryConfig
    {
        public IDistributionFactory DistributionFactory { get; set; } = new DistributionFactory();
        public ICacheFactory CacheFactory { get; set; }
        public IAuthHandler AuthHandler { get; set; }

        /// <summary>
        /// Optional handler used only for fetching the initial catalog list
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

        public TimeSpan VolatileTtl { get; set; } = new TimeSpan(0, 20, 0);
        public TimeSpan? StaticTtl { get; set; } = null;
    }
}
