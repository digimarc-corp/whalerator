﻿using System;
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
            if (Registry.DockerHubAliases.Contains(name))
            {
                name = Registry.DockerHub;
            }

            return new Registry(name, username, password, _CacheFactory);
        }

        private ICacheFactory _CacheFactory;
    }
}