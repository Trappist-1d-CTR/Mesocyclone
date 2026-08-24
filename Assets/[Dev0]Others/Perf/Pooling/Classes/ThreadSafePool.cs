using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading;

// Last time i checked Unity's mono roslyn compiler can only compile C# 9 code... how tf do lock statements compile?!?!??!!??

namespace Mesocyclone
{
    /// <summary>
    /// Same behaviour as <see cref="Pool{T}"/>, but safe to call Get/Release from multiple threads
    /// <br/><br/>
    /// Useful for things like background stuff and networking
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public class ThreadSafePool<T> : IPool<T>
    {
        protected readonly Stack<T> inactive = new();
        protected readonly Func<T> createFunction;
        protected readonly Action<T> onGet;
        protected readonly Action<T> onRelease;
        protected readonly Action<T> onClear;
        public uint maxSize { get; protected set; }
        protected readonly object gate = new();

        private int _countActive;

        public int count { get { return countActive + countInactive; } }
        public int countInactive { get { lock (gate) return inactive.Count; } protected set { _countActive = value; } }
        public int countActive { get { lock (gate) return _countActive; } protected set { _countActive = value; } }

        public ThreadSafePool
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
            T item;

            lock (gate)
            {
                item = inactive.Count > 0 ? inactive.Pop() : default;
                
                if (item is null)
                    _countActive++;
            }

            if (item is null)
                createFunction();

            onGet?.Invoke(item);
            return item;
        }

        public void Release(T item)
        {
            onRelease?.Invoke(item);

            lock (gate)
            {
                _countActive--;

                if (inactive.Count >= (int)maxSize)
                {
                    onClear?.Invoke(item);
                    return;
                }
                inactive.Push(item);
            }
        }

        public void Clear()
        {
            lock (gate)
            {
                foreach (T item in inactive)
                    onClear?.Invoke(item);
                
                inactive.Clear();
                countActive = 0;
            }
        }

        public void Prewarn(int count)
        {
            for (int i = 0; i < count; i++)
            {
                T item = createFunction();
                onRelease?.Invoke(item);

                lock (gate)
                {
                    if (inactive.Count >= (int)maxSize)
                    {
                        onClear?.Invoke(item);
                        break;
                    }

                    inactive.Push(item);
                }
            }
        }
    }
}