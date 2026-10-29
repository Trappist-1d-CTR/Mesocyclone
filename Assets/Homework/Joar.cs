using System;

namespace Mesocyclone
{
    /// <summary>
    /// summon him to die instantly
    /// </summary>
    public class Joar : Exception
    {
        public Joar(string message = "") : base("fuck you.\n" + message)
        {
            // awwww, why you comment out :(
            // UnityEngine.Debug.LogError(base.Message);

            // UnityEngine.Debug.LogException(this);

#if DEV
                UnityEditor.EditorApplication.isPaused = true;
#endif
        }
    }
}