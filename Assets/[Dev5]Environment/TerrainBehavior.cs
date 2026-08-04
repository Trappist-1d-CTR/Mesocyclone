using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Mesocyclone
{
    public class TerrainBehavior : MonoBehaviour
    {
        #region Variables

        #region Terrain Properties
        public float Area;
        public float Temperature;
        public float Moles;
        public float MolarWeight = 0.0401f;
        public float MassPerSquareMeter;
        public float Albedo;
        public float Emissivity;
        public float HeatCapacity;
        public float HeatInertia;
        public float LocalLatitude;
        #endregion

        #region Reference Scripts
        private AGlobalValues C;
        #endregion

        #endregion

        void Awake()
        {
            //Get Script Reference
            C = GameObject.FindGameObjectWithTag("GameController").GetComponent<AGlobalValues>();
        }

        void FixedUpdate()
        {
            Temperature += C.CalculateStaticInsolation(LocalLatitude) * (1 - Albedo) * C.TransparencyAtHeight(0) / (HeatCapacity * MassPerSquareMeter) * Time.fixedDeltaTime;

            // To do here: Radiative Cooling

            // To do in cell behavior: Higher Insolation from Terrain Albedo; Heating from Terrain; Conduction with Terrain
        }
    }

}