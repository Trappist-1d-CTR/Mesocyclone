using System;
using UnityEngine;

/// <summary>
/// Base class for a tickable object
/// <para>Made for dynamic tick rates, instead of a global Time.fixedDeltaTime</para>
/// </summary>
public abstract class Tickable : MonoBehaviour
{
    public float TickRate { get; set; }
    public float Accumulator { get; set; }
    public float FixedAccumulator { get; set; }

    /// <summary>
    /// Tick method called every tick, based on the TickRate and Accumulator
    /// </summary>
    public virtual void Tick()
    { }

    /// <summary>
    /// FixedTick method called every fixed tick, based on the TickRate and FixedAccumulator
    /// <para>Useful for physics based tickables</para>
    /// </summary>
    public virtual void FixedTick()
    { }
}