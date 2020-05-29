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

using Newtonsoft.Json;
using StackExchange.Redis;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Whalerator.Client;

namespace Whalerator.Support
{
    public class RedCache<T> : ICache<T> where T : class
    {
        private IConnectionMultiplexer mux;

        public RedCache(IConnectionMultiplexer redisMux)
        {
            mux = redisMux;
        }

        public TimeSpan Ttl { get; set; } = TimeSpan.FromHours(1);

        /// <inheritdoc/>
        public Task<T> ExecAsync(string scope, string key, IAuthHandler authHandler, Func<Task<T>> func) =>
            ExecAsync(scope, key, Ttl, authHandler, func);

        /// <inheritdoc/>
        public async Task<T> ExecAsync(string scope, string key, TimeSpan ttl, IAuthHandler authHandler, Func<Task<T>> func)
        {
            T result;
            if (await authHandler.AuthorizeAsync(scope))
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
        public Task<bool> ExistsAsync(string key) => mux.GetDatabase().KeyExistsAsync(key);

        /// <inheritdoc/>
        public Task SetAsync(string key, T value, TimeSpan ttl)
        {
            var json = JsonConvert.SerializeObject(value);
            return mux.GetDatabase().StringSetAsync(key, json, ttl);
        }

        /// <inheritdoc/>
        public Task SetAsync(string key, T value) => SetAsync(key, value, Ttl);

        /// <inheritdoc/>
        public async Task<Lock> TakeLockAsync(string key, TimeSpan lockTime, TimeSpan lockTimeout)
        {
            var value = Guid.NewGuid().ToString();
            var db = mux.GetDatabase();
            var start = DateTime.UtcNow;
            while (!await db.LockTakeAsync(key, value, lockTime))
            {
                if (DateTime.Now - start > lockTimeout) { throw new TimeoutException($"Could not get a lock for {key} in the allotted time."); }
                System.Threading.Thread.Sleep(500);
            }
            return new Lock(
                releaseAction: () => db.LockReleaseAsync(key, value),
                extendAction: (time) => db.LockExtendAsync(key, value, time)
            );
        }

        /// <inheritdoc/>
        public Task<bool> TryDeleteAsync(string key) => mux.GetDatabase().KeyDeleteAsync(key);

        /// <inheritdoc/>
        public async Task<T> GetAsync(string key)
        {
            var json = await mux.GetDatabase().StringGetAsync(key);
            return json.IsNullOrEmpty ? null : JsonConvert.DeserializeObject<T>(json);
        }
    }
}
