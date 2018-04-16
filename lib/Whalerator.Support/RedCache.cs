using Newtonsoft.Json;
using StackExchange.Redis;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Whalerator.Support
{
    public class RedCache<T> : ICache<T> where T : class
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

        public bool Exists(string key)
        {
            return _Mux.GetDatabase(_Db).KeyExists(key);
        }

        public void Set(string key, T value)
        {
            var json = JsonConvert.SerializeObject(value);
            _Mux.GetDatabase(_Db).StringSet(key, json, _Ttl);
        }

        public bool TryGet(string key, out T value)
        {
            try
            {
                var json = _Mux.GetDatabase(_Db).StringGet(key);
                value = json.IsNullOrEmpty ? null : JsonConvert.DeserializeObject<T>(json);
            }
            catch
            {
                value = null;
            }
            return value == null ? false : true;
        }
    }
}
