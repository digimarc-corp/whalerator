using Microsoft.Extensions.Caching.Memory;
using Newtonsoft.Json;
using StackExchange.Redis;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Whalerator.Support
{
    public class MemCache<T> : ICache<T> where T : class
    {
        private IMemoryCache _MemCache;

        public TimeSpan _Ttl { get; }

        public MemCache(IMemoryCache memCache, TimeSpan cacheTtl)
        {
            _MemCache = memCache;
            _Ttl = cacheTtl;
        }

        public bool TryGet(string key, out T value)
        {
            string json;
            if (_MemCache.TryGetValue(key, out json))
            {
                value = JsonConvert.DeserializeObject<T>(json);
                return true;
            }
            else value = null;
            return false;
        }

        public void Set(string key, T value)
        {
            var json = JsonConvert.SerializeObject(value);
            _MemCache.Set(key, json, _Ttl);
        }
    }
}
