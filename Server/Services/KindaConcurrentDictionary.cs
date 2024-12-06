
public class KindaConcurrentDictionary<T, U>()
{
    private readonly Dictionary<T, U> _dictionary = new();
    private readonly ReaderWriterLockSlim _lock = new(LockRecursionPolicy.NoRecursion);

    public bool TryGetValue(T key, out U value)
    {
        _lock.EnterReadLock();
        try
        {
            return _dictionary.TryGetValue(key, out value);
        }
        finally
        {
            _lock.ExitReadLock();
        }
    }

    public void AddOrUpdate(T key, U value)
    {
        _lock.EnterWriteLock();
        try
        {
            _dictionary[key] = value;
        }
        finally
        {
            _lock.ExitWriteLock();
        }
    }
}
