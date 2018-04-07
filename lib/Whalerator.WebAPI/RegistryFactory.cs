using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Whalerator.WebAPI
{
    public class RegistryFactory : IRegistryFactory
    {
        public RegistryFactory(ICacheFactory cacheFactory)
        {
            _CacheFactory = cacheFactory;
        }

        public IRegistry GetRegistry(string name, string username, string password)
        {
            name = name.ToLowerInvariant();
            if (DockerAliases.Contains(name))
            {
                name = "registry-1.docker.io";
            }

            return new Registry(name, username, password, _CacheFactory);
        }

        HashSet<string> DockerAliases = new HashSet<string> {
            "docker.io",
            "hub.docker.io",
            "registry.docker.io"
        };

        private ICacheFactory _CacheFactory;
    }
}
