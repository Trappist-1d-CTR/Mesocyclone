using System;
using UnityEngine;

// base class for all performance overloads

/// <summary>
/// last resort for humanity
/// </summary>
public class PerformanceOverloadException : Exception
{
    public PerformanceOverloadException() : base("Game has overwhelmed all device resources, crash staged")
    {
        Debug.LogError(base.Message);
    }
}