namespace Whalerator
{
    public interface ICache<T> where T : class
    {
        bool TryGet(string key, out T value);
        void Set(string key, T value);
    }
}