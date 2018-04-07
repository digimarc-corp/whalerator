using Microsoft.Extensions.Caching.Memory;
using System;
using System.Collections.Generic;
using System.Text;

namespace Whalerator.Support
{
    public class MemCacheFactory : ICacheFactory
    {
        private IMemoryCache _Cache;
        private TimeSpan _Ttl;

        public MemCacheFactory(IMemoryCache cache, TimeSpan ttl)
        {
            _Cache = cache;
            _Ttl = ttl;
        }

        public ICache<T> Get<T>() where T : class
        {
            return new MemCache<T>(_Cache, _Ttl);
        }
    }
}
