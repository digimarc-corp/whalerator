using StackExchange.Redis;
using System;
using System.Collections.Generic;
using System.Text;

namespace Whalerator.Support
{
    public class RedCacheFactory : ICacheFactory
    {
        public IConnectionMultiplexer Mux { get; set; }
        public int Db { get; set; }
        public TimeSpan Ttl { get; set; }

        public ICache<T> Get<T>() where T : class
        {
            return new RedCache<T>(Mux, Db, Ttl);
        }
    }
}
