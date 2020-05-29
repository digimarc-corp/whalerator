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
using Whalerator.Client;

namespace Whalerator.Support
{
    public class MemCache<T> : ICache<T> where T : class
    {
        private IMemoryCache memCache;

        public MemCache(IMemoryCache memCache)
        {
            this.memCache = memCache;
        }

        public TimeSpan Ttl { get; set; } = TimeSpan.FromHours(1);

        /// <inheritdoc/>
        public Task<T> ExecAsync(string scope, string key, IAuthHandler authHandler, Func<Task<T>> func) =>
            ExecAsync(scope, key, Ttl, authHandler, func);

        /// <inheritdoc/>
        public async Task<T> ExecAsync(string scope, string key, TimeSpan ttl, IAuthHandler authHandler, Func<Task<T>> func)
        {
            T result;
            if (authHandler.Authorize(scope))
            {
                if (await ExistsAsync(key))
                {
                    return await GetAsync(key);
                }
                else
                {
                    result = await func();
                    await SetAsync(key, result, ttl);
                    return result;
                }
            }
            else
            {
                throw new AuthenticationException("The request could not be authorized.");
            }
        }

        /// <inheritdoc/>
        public Task<T> GetAsync(string key)
        {
            string json = (string)memCache.Get(key);
            return Task.FromResult(JsonConvert.DeserializeObject<T>(json));
        }

        #region locking
        async Task<bool> LockTakeAsync(string key, string value, TimeSpan lockTime)
        {
            if (await ExistsAsync(key)) { return false; }
            else
            {
                memCache.Set(key, value, lockTime);
                var check = memCache.Get<string>(key);
                return check == value;
            }
        }

        Task LockReleaseAsync(string key, string value)
        {
            var check = memCache.Get<string>(key);
            if (check == value) { memCache.Remove(key); }
            return Task.CompletedTask;
        }

        Task<bool> LockExtend(string key, string value, TimeSpan time)
        {
            var check = memCache.Get<string>(key);
            if (check == value)
            {
                memCache.Set(key, value, time);
                check = memCache.Get<string>(key);
                return Task.FromResult(check == value);
            }
            else
            {
                return Task.FromResult(false);
            }
        }

        /// <inheritdoc/>
        public async Task<Lock> TakeLockAsync(string key, TimeSpan lockTime, TimeSpan lockTimeout)
        {
            var value = Guid.NewGuid().ToString();
            var start = DateTime.UtcNow;
            while (!await LockTakeAsync(key, value, lockTime))
            {
                if (DateTime.Now - start > lockTimeout) { throw new TimeoutException($"Could not get a lock for {key} in the allotted time."); }
                System.Threading.Thread.Sleep(500);
            }
            return new Lock(
                releaseAction: () => LockReleaseAsync(key, value),
                extendAction: (time) => LockExtend(key, value, time)
            );
        }
        #endregion

        /// <inheritdoc/>
        public async Task SetAsync(string key, T value) => await SetAsync(key, value, Ttl);

        /// <inheritdoc/>
        public Task<bool> ExistsAsync(string key) => Task.FromResult(memCache.TryGetValue(key, out var _));

        /// <inheritdoc/>
        public Task SetAsync(string key, T value, TimeSpan ttl)
        {
            var json = JsonConvert.SerializeObject(value);
            if (ttl == null) { memCache.Set(key, json); }
            else { memCache.Set(key, json, ttl); }

            return Task.CompletedTask;
        }

        /// <inheritdoc/>
        public async Task<bool> TryDeleteAsync(string key)
        {
            if (await ExistsAsync(key))
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
