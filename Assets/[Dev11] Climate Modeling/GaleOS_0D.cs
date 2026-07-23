using System.Collections.Generic;
using System.Collections;
using UnityEngine;

namespace Mesocyclone
{
    public class GaleOS_0D : MonoBehaviour
    {
        #region Variables

        private AGlobalValues GlobalValues;

        public float A;
        public float Greenhouse;
        public float C;

        public float t;
        public float tMultiplier;
        public float S;

        public float Ei;
        public float Ee;
        public float T;

        private float dt;

        #endregion

        // Start is called once before the first execution of Update after the MonoBehaviour is created
        void Start()
        {
            GlobalValues = GameObject.FindGameObjectWithTag("GameController").GetComponent<AGlobalValues>();
            t = 0;
            dt = tMultiplier * Time.fixedDeltaTime;
        }

        // Update is called once per frame
        void FixedUpdate()
        {
            if (dt != tMultiplier * Time.fixedDeltaTime) dt = tMultiplier * Time.fixedDeltaTime;

            t += dt;
            S = GlobalValues.CalculateStaticInsolation(t);

            Ei = Mathf.PI * Mathf.Pow(GlobalValues.GaleR, 2) * S * (1 - A);
            Ee = 4 * Mathf.PI * Mathf.Pow(GlobalValues.GaleR, 2) * GlobalValues.GreekS * Mathf.Pow(T, 4) * (1 - Greenhouse);

            T += (Ei - Ee) / (Mathf.PI * Mathf.Pow(GlobalValues.GaleR, 2) * S) / C * dt;
        }
    }
}