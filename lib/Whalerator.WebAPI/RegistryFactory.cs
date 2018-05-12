using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Whalerator.WebAPI
{
    public class RegistryFactory : IRegistryFactory
    {
        public RegistryFactory(RegistryConfig settings)
        {
            Settings = settings;
        }

        public IRegistry GetRegistry(RegistryCredentials credentials)
        {
            credentials.Registry = credentials.Registry.ToLowerInvariant();
            if (Registry.DockerHubAliases.Contains(credentials.Registry))
            {
                credentials.Registry = Registry.DockerHub;
            }

            return new Registry(credentials, Settings);
        }

        private ICacheFactory _CacheFactory;

        public RegistryConfig Settings { get; }
    }
}
