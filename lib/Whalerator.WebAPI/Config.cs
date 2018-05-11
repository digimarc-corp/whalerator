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
            public List<string> Filenames { get; set; } = new List<string>() { "readme.md" };
        }

        public SecurityConfig Security { get; set; }
        public class SecurityConfig
        {
            public string PrivateKey { get; set; }
        }

        public CatalogConfig Catalog { get; set; }
        public class CatalogConfig
        {
            public string Registry { get; set; }
            public List<string> Repositories { get; set; }
            public CatalogUser User { get; set; }
            public class CatalogUser
            {
                public string Username { get; set; }
                public string Password { get; set; }
            }
        }
    }
}