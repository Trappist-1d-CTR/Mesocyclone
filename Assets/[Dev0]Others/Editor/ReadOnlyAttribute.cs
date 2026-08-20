using System;
using System.Diagnostics;
using UnityEngine;

namespace Mesocyclone
{
    /// <summary>
    /// Marks a field as read-only in the Unity Inspector
    /// </summary>
    [Conditional("DEV")]
    public sealed class ReadOnlyAttribute : PropertyAttribute { }
}