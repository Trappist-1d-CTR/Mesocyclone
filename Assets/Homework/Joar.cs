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
            UnityEngine.Debug.LogError(base.Message);
        }
    }
}