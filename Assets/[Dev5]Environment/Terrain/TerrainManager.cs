using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Mesocyclone.Unused;

namespace Mesocyclone
{
    public class TerrainManager : MonoBehaviour
    {
        #region Variables

        public static Vector2 TerrainOriginPoint = new(-10000, -10000);
        private TerrainData TerrData;
        private Vector2 Offset;
        private Vector2 TerrSize;
        private int TerrResolution;
        public static float NoiseScale = 9;
        public static float Amplitude = 15;
        public static float MaxHeight = 100;

        #endregion

        public static bool GenerateTerrain = false;

        void Start()
        {
            if (GenerateTerrain)
            {
                #region Get component and values

                TerrData = gameObject.GetComponent<TerrainCollider>().terrainData;
                TerrSize = new(TerrData.size.x, TerrData.size.z);
                TerrResolution = TerrData.heightmapResolution;
                Offset = new(Mathf.Round(gameObject.transform.position.x / TerrSize.x) * (TerrResolution - 1), Mathf.Round(gameObject.transform.position.z / TerrSize.y) * (TerrResolution - 1));
                Offset.x -= TerrainOriginPoint.x;
                Offset.y -= TerrainOriginPoint.y;

                #endregion

                #region Generate and apply HeightMap

                TerrData.size = new(TerrSize.x, MaxHeight, TerrSize.y);
                TerrData.SetHeights(0, 0, TerrainGenerator.GenerateSimplePerlinMap(TerrResolution, Offset, NoiseScale, Amplitude / MaxHeight));

                #endregion
            }
        }

        void Update()
        {

        }
    }
}