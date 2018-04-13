using System;
using System.Collections.Generic;
using System.Text;

namespace Whalerator
{
    /// <summary>
    /// Minimal cache implementation based on Dictionary<T>
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public class DictCache<T> : ICache<T> where T : class
    {
        public Dictionary<string, T> Cache { get; set; } = new Dictionary<string, T>();
        public bool Exists(string key) => Cache.ContainsKey(key);
        public void Set(string key, T value) => Cache[key] = value;
        public bool TryGet(string key, out T value)
        {
            var exists = Exists(key);
            value = exists ? Cache[key] : null;
            return exists;
        }
    }
}
