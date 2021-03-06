﻿/*
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
using System.Threading.Tasks;
using Whalerator.Client;

namespace Whalerator
{
    public interface ICache<T> where T : class
    {
        public TimeSpan Ttl { get; set; }

        /// <summary>
        /// Returns a cached result for a Func<typeparamref name="T"/>, or executes the function and caches the new result.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="scope">Security scope to be passed to the IAuthHandler</param>
        /// <param name="key"></param>
        /// <param name="ttl"></param>
        /// <param name="authHandler"></param>
        /// <param name="func"></param>
        /// <returns></returns>
        Task<T> ExecAsync(string scope, string key, TimeSpan ttl, IAuthHandler authHandler, Func<Task<T>> func);

        /// <summary>
        /// Returns a cached result for a Func<typeparamref name="T"/>, or executes the function and caches the new result.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="scope">Security scope to be passed to the IAuthHandler</param>
        /// <param name="key"></param>
        /// <param name="authHandler"></param>
        /// <param name="func"></param>
        /// <returns></returns>
        Task<T> ExecAsync(string scope, string key, IAuthHandler authHandler, Func<Task<T>> func);


        Task<bool> ExistsAsync(string key);
        Task<T> GetAsync(string key);

        /// <summary>
        /// Stores an object in cache, using an explicit ttl
        /// </summary>
        /// <param name="key"></param>
        /// <param name="value"></param>
        /// <param name="ttl"></param>
        Task SetAsync(string key, T value, TimeSpan ttl);

        /// <summary>
        /// Stores an object in cache, using the default ttl
        /// </summary>
        /// <param name="key"></param>
        /// <param name="value"></param>
        /// <param name="ttl"></param>
        Task SetAsync(string key, T value);

        /// <summary>
        /// Represents a basic threadsafe locking mechanism. TakeLock will block until it can return a disposable Lock object, or throw if timeout is exceeded.
        /// Lock will be valid for lockTime, and can be extended with the Extend() method. To release the lock, dispose the Lock object (or use a using block).
        /// Not guaranteed safe, but good enough for basic concurrency management.
        /// </summary>
        /// <param name="key"></param>
        /// <param name="lockTime"></param>
        /// <param name="timeout"></param>
        /// <returns></returns>
        Task<Lock> TakeLockAsync(string key, TimeSpan lockTime, TimeSpan timeout);

        /// <summary>
        /// Returns true if a key could be found and deleted, otherwise false
        /// </summary>
        /// <param name="key"></param>
        /// <returns></returns>
        Task<bool> TryDeleteAsync(string key);
    }
}