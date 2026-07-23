using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// Astraa, you should've told me you were making this ;-;

namespace Mesocyclone
{
    public static class TerrainGenerator
    {
        #region Simple Perlin Noise

        public static float[,] GenerateSimplePerlinMap(int Width, int Height, Vector2 Offset, float Scale, float AmplitudeMultiplier)
        {
            float[,] NoiseMap = new float[Width, Height];

            for (int x = 0; x < Width; x++)
            {
                for (int y = 0; y < Height; y++)
                {
                    NoiseMap[y, x] = Mathf.PerlinNoise((x + Offset.x) / Scale, (y + Offset.y) / Scale) * AmplitudeMultiplier;
                }
            }

            return NoiseMap;
        }

        public static float[,] GenerateSimplePerlinMap(int Resolution, Vector2 Offset, float Scale, float AmplitudeMultiplier) => GenerateSimplePerlinMap(Resolution, Resolution, Offset, Scale, AmplitudeMultiplier);

        #endregion
    }
}
