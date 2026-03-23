using System;
using System.Collections;
using System.Collections.Generic;

namespace MonoGameEngine.Utils.Collections;

public class ObjectPool<T> : IEnumerable<T>
{
    private readonly List<T> _pool;
    private readonly Func<T> _onCreate;
    private readonly Action<T> _onGet;
    private readonly Action<T> _onRelease;
    private readonly Action<T> _onDispose;
    private readonly int _maxSize;

    public ObjectPool(Func<T> onCreate, Action<T> onGet = null, Action<T> onRelease = null, Action<T> onDispose = null, int initialSize = 10, int maxSize = 100)
    {
        _pool = [];
        _onCreate = onCreate;
        _onGet = onGet;
        _onRelease = onRelease;
        _onDispose = onDispose;
        _maxSize = maxSize;

        for (int i = 0; i < initialSize; i++)
        {
            _pool.Add(_onCreate());
        }
    }

    public T Get()
    {
        if (_pool.Count == 0)
        {
            _pool.Add(_onCreate());
        }

        T item = _pool[0];
        _pool.RemoveAt(0);

        _onGet?.Invoke(item);
        return item;
    }

    public void Release(T item)
    {
        _onRelease?.Invoke(item);
        if (_pool.Count < _maxSize)
        {
            _pool.Add(item);
        }
        else
        {
            _onDispose?.Invoke(item);
        }
    }

    public IEnumerator<T> GetEnumerator()
    {
        return _pool.GetEnumerator();
    }

    IEnumerator IEnumerable.GetEnumerator()
    {
        return GetEnumerator();
    }
}