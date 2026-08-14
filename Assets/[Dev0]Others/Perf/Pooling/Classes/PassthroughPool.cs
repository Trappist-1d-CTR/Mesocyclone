using System;
using System.Collections;
using System.Collections.Generic;

namespace Mesocyclone
{
    /// <summary>
    /// An object pool however it always just creates stuff and never actually pools. Which sounds stupid but could be used for fallbacking, switches, and the like.
    /// </summary>
    public class PassthroughPool<T> : IPool<T>
    {
        protected readonly Func<T> createFunction;
        protected readonly Action<T> onClear;
        
        public int countActive { get { return 0; } }
        public int countInactive { get; protected set; }
        public int count { get { return countActive; } }

        public PassthroughPool(Func<T> createFunction, Action<T> onClear = null)
        {
            this.createFunction = createFunction;
            this.onClear = onClear;
        }

        public T Get()
        {
            countActive++;
            return createFunction();
        }

        public void Release(T item)
        {
            countActive--;
            onClear?.Invoke(item);
        }

        public void Clear()
        { }
    }
}