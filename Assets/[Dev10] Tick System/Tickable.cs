using System;
using UnityEngine;

/// <summary>
/// Base class for a tickable MonoBehaviour
/// <para>Made for dynamic tick rates, instead of a global Time.fixedDeltaTime</para>
/// </summary>
public abstract class Tickable : MonoBehaviour
{
    public float TickRate { get; set; }
        = 50f; // set this to what's best

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
    { }

    /// <summary>
    /// FixedTick method called every fixed tick, based on the TickRate and FixedAccumulator
    /// <para>Useful for physics based tickables</para>
    /// </summary>
    public virtual void FixedTick()
    { }

    /// <summary>
    /// Implement logic to adjust TickRate based on performance metrics or other criteria
    /// <param name="minDistance">The minimum distance at which the tick rate should be adjusted</param>
    /// <param name="maxDistance">The maximum distance at which the tick rate should be adjusted</param>
    /// <param name="target">The target GameObject to consider for distance</param>
    /// <para>Override this method in derived classes to implement custom logic for dynamic tick rate adjustment based on distance or other factors.</para>
    /// </summary>
    protected virtual void ImplementDynamicTickRateBasedOnDistance(float minDistance, float maxDistance, GameObject target = FindGameObjectWithTag("Camera"))
    {
        if (Physics.Raycast(transform.position, (target.transform.position - transform.position).normalized, out RaycastHit hitInfo, maxDistance))
        {
            float distance = hitInfo.distance;

            if (distance < minDistance)
            {
                TickRate = 20f; // normal tick rate for close objects
            }
            else if (distance < maxDistance)
            {
                TickRate = Mathf.Lerp(0f, TickRate, 1f - (distance - minDistance) / (maxDistance - minDistance)); // Lower tick rate based on distance
            }
            else
            {
                TickRate = 20f; // Low tick rate for distant objects
            }
        }
        else
        {
            TickRate = 20f; // Default tick rate if no object is hit
        }
    }
}