using System;
using UnityEngine;

namespace Mesocyclone
{
    /// <summary>
    /// summon him to die instantly
    /// </summary>
    public class Joar : Exception
    {
        public Joar() : base("fuck you.")
        {
            // awwww, why you comment out :(
            // UnityEngine.Debug.LogError(base.Message);

            UnityEngine.Debug.LogException(this);

            #if DEV
                UnityEditor.EditorApplication.isPaused = true;
            #endif
        }
    }
}