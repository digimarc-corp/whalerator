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
        private IMemoryCache memCache;

        public MemCache(IMemoryCache memCache)
        {
            this.memCache = memCache;
        }

        /// <inheritdoc/>
        public bool TryGet(string key, out T value)
        {
            string json;
            if (memCache.TryGetValue(key, out json))
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
                memCache.Set(key, value, lockTime);
                var check = memCache.Get<string>(key);
                return check == value;
            }
        }

        void LockRelease(string key, string value)
        {
            var check = memCache.Get<string>(key);
            if (check == value) { memCache.Remove(key); }
        }

        bool LockExtend(string key, string value, TimeSpan time)
        {
            var check = memCache.Get<string>(key);
            if (check == value)
            {
                memCache.Set(key, value, time);
                check = memCache.Get<string>(key);
                return check == value;
            }
            else
            {
                return false;
            }
        }

        /// <inheritdoc/>
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

        /// <inheritdoc/>
        public void Set(string key, T value) => Set(key, value, null);

        /// <inheritdoc/>
        public bool Exists(string key) => memCache.TryGetValue(key, out var discard);

        /// <inheritdoc/>
        public void Set(string key, T value, TimeSpan? ttl)
        {
            var json = JsonConvert.SerializeObject(value);
            if (ttl == null) { memCache.Set(key, json); }
            else { memCache.Set(key, json, (TimeSpan)ttl); }
        }

        /// <inheritdoc/>
        public bool TryDelete(string key)
        {
            if (Exists(key))
            {
                memCache.Remove(key);
                return true;
            }
            else
            {
                return false;
            }
        }
    }
}
