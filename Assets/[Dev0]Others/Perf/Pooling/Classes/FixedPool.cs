using System;
using System.Collections;
using System.Collections.Generic;
using Unity.Collections;

namespace Mesocyclone
{
    /// <summary>
    /// Object pool where it never grows past it's prewarmed size
    /// </summary>
    public class FixedPool<T> : IPool<T>
    {
        protected readonly Stack<T> inactive = new();
        protected readonly Func<T> createFunction;
        protected readonly Action<T> onGet;
        protected readonly Action<T> onRelease;
        protected readonly Action<T> onClear;
        public uint maxSize { get; protected set; }

        public int count { get { return countActive + countInactive; } } // just add the 2 lol
        public int countActive { get; protected set; }
        public int countInactive { get { return inactive.Count; } }

        public FixedPool
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
            if (inactive.Count is 0)
                throw new InvalidOperationException("Fixed Pool exhausted.\nMust increase capacity to continue adding items");
            
            T item = inactive.Pop();
            countActive++;
            onGet?.Invoke(item);
            return item;
        }

        public void Release(T item)
        {
            // if the pool is full, discard the instance
            if (inactive.Count >= (int)maxSize)
            {
                onClear?.Invoke(item);
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
                onClear?.Invoke(item);
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