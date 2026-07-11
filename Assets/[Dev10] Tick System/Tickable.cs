using System;
using UnityEngine;

/// <summary>
/// Base class for a tickable MonoBehaviour
/// <para>Made for dynamic tick rates, instead of a global Time.fixedDeltaTime</para>
/// </summary>
public abstract class Tickable : MonoBehaviour
{
    [HideInInspector]
    public static readonly float DefaultTickRate = 50f; // default tick rate for all tickables, can be changed in the future if needed

    [SerializeField, Tooltip($"The rate at which this tickable should be updated.\nSet to 0 to disable ticking.")]
    float tickRate = 50f; // set this to what's best
    public float TickRate
    {
        get => tickRate;
        set
        {
            if (value < 0f)
                throw new ArgumentOutOfRangeException(nameof(value), "TickRate cannot be negative.");
            tickRate = value;
        }
    }

    public float Accumulator { get; set; }
    public float FixedAccumulator { get; set; }

    bool isRegistered = false;

    protected virtual void Awake()
    {
        if (!isRegistered)
        {
            TickSystem.Instance.RegisterTickable(this);
            isRegistered = true;
        }
    }

    protected virtual void OnEnable()
    {
        if (!isRegistered)
        {
            TickSystem.Instance.RegisterTickable(this);
            isRegistered = true;
        }
    }

    protected virtual void OnDisable()
    {
        if (isRegistered)
        {
            TickSystem.Instance.UnregisterTickable(this);
            isRegistered = false;
        }
    }

    /// <summary>
    /// Tick method called every tick, based on the TickRate and Accumulator
    /// </summary>
    public virtual void Tick()
    {
        if (TickRate <= 0f) return;

        // always make sure to call base.Tick() before any other logic in derived classes to ensure proper tick handling
    }

    /// <summary>
    /// FixedTick method called every fixed tick, based on the TickRate and FixedAccumulator
    /// <para>Useful for physics based tickables</para>
    /// </summary>
    public virtual void FixedTick()
    {
        if (TickRate <= 0f) return;
        // always make sure to call base.FixedTick() before any other logic in derived classes to ensure proper fixed tick handling
    }

    /// <summary>
    /// Implement logic to adjust TickRate based on performance metrics or other criteria
    /// <para>Override this method in derived classes to implement custom logic for dynamic tick rate adjustment based on distance or other factors.</para>
    /// <param name="minDistance">The minimum distance at which the tick rate should be adjusted</param>
    /// <param name="maxDistance">The maximum distance at which the tick rate should be adjusted</param>
    /// <param name="target">The target GameObject to consider for distance, defaults to the main camera</param>
    /// </summary>
    protected virtual void ImplementDynamicTickRateBasedOnDistance(float minDistance, float maxDistance, GameObject target = null)
    {
        target ??= Camera.main?.gameObject;

        if (Physics.Raycast(transform.position, (target.transform.position - transform.position).normalized, out RaycastHit hitInfo, maxDistance))
        {
            float distance = hitInfo.distance;

            if (distance < minDistance)
            {
                TickRate = 50f; // normal tick rate for close objects
            }
            else if (distance < maxDistance)
            {
                TickRate = Mathf.Lerp(0f, 50f, 1f - (distance - minDistance) / (maxDistance - minDistance)); // Lower tick rate based on distance
            }
            else
            {
                TickRate = 0f; // No ticks for objects beyond max distance
            }
        }
        else
        {
            TickRate = 50f; // Default tick rate if no object is hit
        }
    }
}