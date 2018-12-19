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

namespace Whalerator
{
    public interface ICache<T> where T : class
    {
        bool Exists(string key);
        bool TryGet(string key, out T value);
        void Set(string key, T value);
        void Set(string key, T value, TimeSpan? ttl);

        /// <summary>
        /// Represents a basic threadsafe locking mechanism. TakeLock will block until it can return a disposable Lock object, or throw if timeout is exceeded.
        /// Lock will be valid for lockTime, and can be extended with the Extend() method. To release the lock, dispose the Lock object (or use a using block).
        /// Not guaranteed safe, but good enough for basic concurrency management.
        /// </summary>
        /// <param name="key"></param>
        /// <param name="lockTime"></param>
        /// <param name="timeout"></param>
        /// <returns></returns>
        Lock TakeLock(string key, TimeSpan lockTime, TimeSpan timeout);
    }
}