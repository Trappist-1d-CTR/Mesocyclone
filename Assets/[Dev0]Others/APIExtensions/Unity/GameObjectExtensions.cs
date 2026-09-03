using System;
using UnityEngine;

namespace Mesocyclone
{
    /// <summary>
    /// Extension methods for <a href="https://docs.unity3d.com/6000.3/Documentation/ScriptReference/GameObject.html">GameObject</a>
    /// </summary>
    public static class GameObjectExtensions
    {
        /// <summary>
        /// Gets a component from the GameObject, or adds it if it doesn't exist.
        /// </summary>
        /// <typeparam name="T">The type of component to get or add.</typeparam>
        /// <returns>A component of type T.</returns>
        public static T GetOrAddComponent<T>(this GameObject gameObject) where T : Component
        {
            return gameObject.TryGetComponent<T>(out var component) ? component : gameObject.AddComponent<T>();
        }
    }
}