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

        #region locking
        bool LockTake(string key, string value, TimeSpan lockTime)
        {
            if (Exists(key)) { return false; }
            else
            {
                _MemCache.Set(key, value, lockTime);
                var check = _MemCache.Get<string>(key);
                return check == value;
            }
        }

        void LockRelease(string key, string value)
        {
            var check = _MemCache.Get<string>(key);
            if (check == value) { _MemCache.Remove(key); }
        }

        bool LockExtend(string key, string value, TimeSpan time)
        {
            var check = _MemCache.Get<string>(key);
            if (check == value)
            {
                _MemCache.Set(key, value, time);
                check = _MemCache.Get<string>(key);
                return check == value;
            }
            else
            {
                return false;
            }
        }

        public Lock TakeLock(string key, TimeSpan lockTime, TimeSpan lockTimeout)
        {
            var value = Guid.NewGuid().ToString();
            var start = DateTime.UtcNow;
            while (!LockTake(key, value, lockTime))
            {
                if (DateTime.Now - start > lockTimeout) { throw new TimeoutException($"Could not get a lock for {key} in the allotted time."); }
                System.Threading.Thread.Sleep(500);
            }
            return new Lock(
                releaseAction: () => { LockRelease(key, value); },
                extendAction: (time) => { return LockExtend(key, value, time); }
            );
        }
        #endregion

        public void Set(string key, T value) => Set(key, value, _Ttl);

        public bool Exists(string key) => _MemCache.TryGetValue(key, out var discard);

        public void Set(string key, T value, TimeSpan? ttl)
        {
            var json = JsonConvert.SerializeObject(value);
            if (ttl == null) { _MemCache.Set(key, json); }
            else { _MemCache.Set(key, json, (TimeSpan)ttl); }
        }
    }
}
