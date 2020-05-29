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
using System.Threading.Tasks;
using Whalerator.Client;

namespace Whalerator
{
    /// <summary>
    /// Minimal cache implementation based on Dictionary<T>. Ignores Ttl. For testing use only.
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public class DictCache<T> : ICache<T> where T : class
    {
        public Dictionary<string, T> Cache { get; set; } = new Dictionary<string, T>();

        public TimeSpan Ttl { get; set; } = TimeSpan.FromHours(1);

        public Task<T> ExecAsync(string scope, string key, IAuthHandler authHandler, Func<Task<T>> func) => ExecAsync(scope, key, Ttl, authHandler, func);
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

        public Task<bool> ExistsAsync(string key) => Task.FromResult(Cache.ContainsKey(key));
        public Task SetAsync(string key, T value)
        {
            Cache[key] = value;
            return Task.CompletedTask;
        }

        public Task SetAsync(string key, T value, TimeSpan ttl) => SetAsync(key, value);

        public Task<Lock> TakeLockAsync(string key, TimeSpan lockTime, TimeSpan timeout) => throw new NotImplementedException();

        public async Task<bool> TryDeleteAsync(string key)
        {
            if (await ExistsAsync(key))
            {
                Cache.Remove(key);
                return true;
            }
            else
            {
                return false;
            }
        }

        public Task<T> GetAsync(string key) => Task.FromResult(Cache[key]);
    }
}
