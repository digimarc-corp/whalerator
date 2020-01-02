/*
   Copyright 2018 Digimarc, Inc

   Licensed under the Apache License, Version 2.0 (the "License");
   you may not use this file except in compliance with the License.
   You may obtain a copy of the License at

       http://www.apache.org/licenses/LICENSE-2.0

   Unless required by applicable law or agreed to in writing, software
   distributed under the License is distributed on an "AS IS" BASIS,
   WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
   See the License for the specific language governing permissions and
   limitations under the License.

   SPDX-License-Identifier: Apache-2.0
*/

using System;
using System.Collections.Generic;
using System.Text;

namespace Whalerator
{
    /// <summary>
    /// Minimal cache implementation based on Dictionary<T>. Ignores Ttl. For testing use only.
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public class DictCache<T> : ICache<T> where T : class
    {
        public Dictionary<string, T> Cache { get; set; } = new Dictionary<string, T>();

        public TimeSpan VolatileTtl { get; set; }

        public TimeSpan? StaticTtl { get; set; } = null;

        public bool Exists(string key) => Cache.ContainsKey(key);
        public void Set(string key, T value) => Cache[key] = value;

        public void Set(string key, T value, TimeSpan? ttl) => Set(key, value);

        public void Set(string key, T value, bool isVolatile) => Set(key, value);

        public Lock TakeLock(string key, TimeSpan lockTime, TimeSpan timeout) => throw new NotImplementedException();

        public bool TryDelete(string key)
        {
            if (Exists(key))
            {
                Cache.Remove(key);
                return true;
            }
            else
            {
                return false;
            }
        }

        public bool TryGet(string key, out T value)
        {
            var exists = Exists(key);
            value = exists ? Cache[key] : null;
            return exists;
        }
    }
}
