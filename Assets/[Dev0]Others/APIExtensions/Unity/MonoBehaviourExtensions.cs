using System;
using UnityEngine;

namespace Mesocyclone
{
    /// <summary>
    /// Extension methods for <a href="https://docs.unity3d.com/6000.3/Documentation/ScriptReference/MonoBehaviour.html">MonoBehaviour</a>
    /// </summary>
    public static class MonoBehaviourExtensions
    {
        /// <summary>
        /// Gets a component from the MonoBehaviour's GameObject, or adds it if it doesn't exist.
        /// </summary>
        /// <typeparam name="T">The type of component to get or add.</typeparam>
        /// <returns>A component of type T.</returns>
        public static T GetOrAddComponent<T>(this MonoBehaviour monoBehaviour) where T : Component
        {
            return monoBehaviour.gameObject.GetOrAddComponent<T>();
        }
    }
}