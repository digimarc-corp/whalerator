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