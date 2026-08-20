using System;

// TODO: To be implemented later

namespace Mesocyclone
{
    /// <summary>
    /// The base interface for object pools.
    /// </summary>
    public interface IPool<T>
    {
        /// <summary>
        /// The total amount of instances in the pool.
        /// </summary>
        int count { get; }

        /// <summary>
        /// The amount of active instances in the pool.
        /// </summary>
        int countActive { get; }

        /// <summary>
        /// THe amount of inactive/idle instances in the pool
        /// </summary>
        int countInactive { get; }

        /// <summary>
        /// Takes an instance out of the pool.
        /// </summary>
        T Get();

        /// <summary>
        /// Returns an instance to the pool for reuse
        /// </summary>
        void Release(T item);

        /// <summary>
        /// Destroys/Discards every pooled instance
        /// </summary>
        void Clear();
    }
}