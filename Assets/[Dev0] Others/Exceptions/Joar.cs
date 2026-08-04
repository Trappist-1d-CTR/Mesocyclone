using System;
using UnityEngine;

// fallback to this whenever you're unsure on what exception to use
// or are just too lazy

namespace Mesocyclone
{
    /// <summary>
    /// summon him to die instantly
    /// </summary>
    public class Joar : Exception
    {
        public Joar() : base("fuck you")
        {
            Debug.LogError(base.Message);
        }
    }
}