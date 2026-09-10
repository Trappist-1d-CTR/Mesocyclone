using System;
using System.Collections.Generic;

// props to the rain world MSC developers for coming up with this genius thing

namespace Mesocyclone.Modding
{
    /// <summary>
    /// A modular and modding-friendly version of the enum.
    /// <br/><br/>
    /// <b>NOTE:</b> If you believe the enum shouldn't really be relevant in any modding application and/or be static, then it's usually better to use regular enum's as it is <i>much</i> less verbose 
    /// </summary>
    /// <remarks>
    /// Here's a tutorial for how to use extenums:
    /// <br/><br/>
    /// <tutorial>
    /// How you'd traditionally define an enum: <br/>
    /// <code>
    /// public enum Test
    /// {
    ///     Hello, World
    /// }
    /// </code>
    /// <br/><br/>
    /// And here's how you'd define an ExtEnum equivalent: <br/>
    /// <code>
    /// public class Test : ExtEnum&lt;Test&gt;
    /// {
    ///     public static readonly Test Hello = new("Hello", true);
    ///     public static readonly Test World = new("World", true);
    /// 
    ///     public Test(string value, bool register = false) : base(value, register) { }
    /// }
    /// </code>
    /// <br/><br/>
    /// Now because of issues that are on a technical, compilation issue, somewhere early in your code you have to call: <br/>
    /// <c>ExtEnumInitializer.InitTypes&lt;MyExtEnum&gt;();</c> <br/>
    /// To actually cache it into memory and wtv.
    /// <br/><br/>
    /// And here's how to add entries to an existing enum: <br/>
    /// <code>
    /// public static class MyModdedEnum
    /// {
    ///     public static CreatureTemplate.Type NewCreature;
    ///     public static CreatureTemplate.Type AnotherCreature;
    /// 
    ///     public static void RegisterValues()
    ///     {
    ///         NewCreature = new CreatureTemplate.Type("NewCreature", true);
    ///         AnotherCreature = new CreatureTemplate.Type("AnotherCreature, true);
    ///     }
    /// 
    ///     public static void UnregisterValues()
    ///     {
    ///         if (NewCreature is not null)
    ///         {
    ///             NewCreature.Unregister();
    ///             NewCreature = null;
    ///         }
    ///         if (AnotherCreature is not null)
    ///         {
    ///             AnotherCreature.Unregister();
    ///             AnotherCreature = null;
    ///         }
    ///     }
    /// }
    /// </code>
    /// <br/><br/><br/><br/>
    /// <i>Oh and if you're curios, ExtEnum is an abbrevation of Extended Enumerator, if that wasn't already obvious.</i>
    /// </tutorial>
    /// </remarks>
    /// <typeparam name="T">Your derived class. Self-referencing, e.g: <c>class Test : ExtEnum&lt;Test&gt;</c></typeparam>
    public abstract class ExtEnum<T> : Object where T : ExtEnum<T> // make it so the inheriting class has to fill in itself as a generic... almost
    {
        /// <summary>
        /// The string identifier of this entry.
        /// </summary>
        public readonly string value;

        /// <summary>
        /// The position of this entry inside <see cref="values"/>. Reassigned on unregister.
        /// </summary>
        public int index { get; private set; } = -1;

        public static List<T> values { get; } = new();

        protected ExtEnum(string value, bool register = false)
        {
            this.value = value;
            if (register)
                Register();
        }

        public void Register()
        {
            if (index is not -1) // if it's already registered, don't do anything
                return;

            foreach (T existing in values)
            {
                if (existing.value == value)
                    throw new InvalidOperationException($"ExtEnum entry \"{value}\" us already registered for type {typeof(T).Name}. " + "Two mods (or your own code) are colliding on the same name.");
            }

            index = values.Count;
            values.Add((T)this);
        }

        public void Unregister()
        {
            if (index is -1) // if it's not registered don't do anything
                return;
            
            values.RemoveAt(index);
            for (int i = 0; i < values.Count; i++)
                values[i].index = i;
            
            index = -1;
        }

        /// <summary>
        /// Finds an entry by name, or null if none is found registered.
        /// </summary>
        /// <returns></returns>
        public static T Find(string value)
        {
            foreach (T entry in values)
                if (entry.value == value)
                    return entry;
            
            return null;
        }

        public override string ToString()
        {
            return value;
        }

        public override bool Equals(object obj)
        {
            return obj is ExtEnum<T> other && other.value == value;
        }

        public override int GetHashCode()
        {
            return value.GetHashCode();
        }

        public static bool operator ==(ExtEnum<T> left, ExtEnum<T> right)
        {
            if (ReferenceEquals(left, right))
                return true;

            if (left is null || right is null)
                return false;
            
            return left.value == right.value;
        }

        public static bool operator !=(ExtEnum<T> left, ExtEnum<T> right)
        {
            return !(left == right);
        }
    }
}