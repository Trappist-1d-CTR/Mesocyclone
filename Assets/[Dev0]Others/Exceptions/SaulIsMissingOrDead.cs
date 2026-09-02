using System;
using UnityEngine;

namespace Mesocyclone.Security.Critical
{
    public class SaulIsMissingOrDead : Exception
    {
        public SaulIsMissingOrDead() : base("SAUL IS MISSING OR DEAD WE ARE ALL FUCKED!!!")
        {
            UnityEngine.Debug.Log(base.Message);
            UnityEngine.Debug.LogException(this);
            throw new Joar();
        }
    }
}