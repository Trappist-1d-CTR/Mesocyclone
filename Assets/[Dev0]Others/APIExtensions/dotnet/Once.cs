using System;

namespace Mesocyclone
{
    /// <summary>
    /// Represents a value that can only be set once.
    /// <br/><br/>
    /// Fixes the problem of not being able to use readonly nicely in unity, since you usually don't use constructors.
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public class Once<T> where T : class, new()
    {
        public T Value { get; private set; } = null!;

        public T Assign(T value)
        {
            if (Value is not null)
                throw new InvalidOperationException("Value has already been assigned.");

            Value = value ?? throw new ArgumentNullException(nameof(value));
            return Value;
        }
    }

    /// <summary>
    /// Represents a value that can only be set once.
    /// <br/><br/>
    /// Fixes the problem of not being able to use readonly nicely in unity, since you usually don't use constructors.
    /// <br/><br/>
    /// This version is for structs, which are value types and cannot be null. It uses a nullable type to allow for the "not assigned" state.
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public struct NativeOnce<T> where T : struct
    {
        public T? Value { get; private set; }

        public T Assign(T value)
        {
            if (Value is not null)
                throw new InvalidOperationException("Value has already been assigned.");

            Value = value;
            return (T)Value;
        }
    }
}
