using System;
using System.Collections;
using System.Collections.Generic;

namespace Mesocyclone
{
    /// <summary>
    /// Dictionary for containing multiple object pools.
    /// </summary>
    public class KeyedPool<TKey, T> where T : IPool<T>
    {
        protected Dictionary<TKey, IPool<T>> _pools = new();
        public IReadOnlyDictionary<TKey, IPool<T>> pools { get { return _pools; } }

        protected readonly Func<TKey, IPool<T>> poolFactory;

        public KeyedPool(Func<TKey, IPool<T>> poolFactory)
        {
            this.poolFactory = poolFactory;
        }

        public T Get(TKey key)
        {
            if (!pools.TryGetValue(key, out var pool))
                _pools = (Dictionary<TKey, IPool<T>>)(pool = poolFactory(key));
            
            return pool.Get();
        }

        public void Release(TKey key, T item)
        {
            pools[key].Release(item);
        }
    }
}