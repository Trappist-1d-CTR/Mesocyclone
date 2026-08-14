using System;
using System.Collections;
using System.Collections.Generic;

namespace Mesocyclone
{
    /// <summary>
    /// Just an object pool.
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public class Pool<T> : IPool<T>
    {
        protected readonly Stack<T> inactive = new();
        protected readonly Func<T> createFunction;
        protected readonly Action<T> onGet;
        protected readonly Action<T> onRelease;
        protected readonly Action<T> onClear;
        public readonly uint maxSize { get; protected set; }

        public int count { get { return countActive + countInactive; } } // just add the 2 lol
        public int countActive { get; protected set; }
        public int countInctive { get { return inactive.Count; } }

        public Pool
        (
            Func<T> createFunction,
            Action<T> onGet = null,
            Action<T> onRelease = null,
            Action<T> onClear = null,
            uint maxSize = 128
        )
        {
            this.createFunction = createFunction;
            this.onGet = onGet;
            this.onRelease = onRelease;
            this.onClear = onClear;
            this.maxSize = maxSize;
        }

        public T Get()
        {
            T item = inactive.Count > 0 ? inactive.Pop() : createFunction();
            countActive++;
            onGet?.Invoke();
            return item;
        }

        public void Release(T item)
        {
            // if the pool is full, discard the instance
            if (inactive.Count >= (int)maxSize)
            {
                onDestroy?.Invoke(item);
            }
            else
            {
                onRelease?.Invoke(item);
                inactive.Push(item);
            }
            countActive--;
        }

        public void Clear()
        {
            foreach (T item in inactive)
                onDestroy?.Invoke(item);
            inactive.Clear();
            countActive = 0;
        }

        public void Prewarm(int count)
        {
            for (int i = 0; i < count && inactive.Count < (int)maxSize; i++)
            {
                T item = createFunction();
                onRelease?.Invoke(item);
                inactive.Push(item);
            }
        }
    }
}