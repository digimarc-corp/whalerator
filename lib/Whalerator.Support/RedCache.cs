using StackExchange.Redis;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Whalerator.Support
{
    /*
    public class RedCache : ICache<int>
    {
        private IConnectionMultiplexer _Mux;
        private int _Db;
        private TimeSpan _Ttl;

        public RedCache(IConnectionMultiplexer redisMux, int db, TimeSpan ttl)
        {
            _Mux = redisMux;
            _Db = db;
            _Ttl = ttl;
        }

        public void Set(string key, int value)
        {
            _Mux.GetDatabase(_Db).StringSet(key, value, _Ttl);
        }

        public bool TryGet(string key, out int value)
        {
            try
            {
                var redValue = _Mux.GetDatabase(_Db).StringGet(key);
                value = (int)redValue; //null RedisValue casts to default(int)
                return redValue.IsNull ? false : true;
            }
            catch
            {
                value = default(int);
                return false;
            }
        }
    }*/
}
