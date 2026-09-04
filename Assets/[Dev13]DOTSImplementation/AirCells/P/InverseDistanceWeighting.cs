using System;
using UnityEngine;
using Unity.Mathematics;
using Unity.Collections;

namespace Mesocyclone
{
    // the DOTS compatable version
    public static class InverseDistanceWeighting
    {
        public static float R = 1000;
        public static float3 Query;
        public static NativeList<int> Indices;
        public static int LastIndex;

        public static NativeArray<float> SUM_wu;
        public static float SUM_w;

        public static bool FollowDrone;

        public static NativeArray<float> Values { get; private set; }
        private static bool LockValues;

        public static void IntializeIndicesNative()
        {
            Indices = new(Allocator.Temp); // way too lazy to manually dispose of this
        }

        public static void DronePosition(float3 position)
        {
            Query = FollowDrone ? new float3(0, position.y, 0) : position;
        }

        public static void Add(int index)
        {
            Indices.Add(index);
        }

        public static void Remove(int index)
        {
            if (Indices.Count is 1)
            {
                LastIndex = index;
            }
            Indices.Remove(index);
        }

        public static void BeginInterpolation()
        {
            SUM_wu = new(6, Allocator.Temp, NativeArrayOptions.ClearMemory);
            SUM_wu[0] = 0f;
            SUM_wu[1] = 0f;
            SUM_wu[2] = 0f;
            SUM_wu[3] = 0f;
            SUM_wu[4] = 0f;
            SUM_wu[5] = 0f; 

            SUM_w = 0;
            LockValues = false;
        }

        public static void InterpolationStep(float3 xi, NativeArray<float> u)
        {
            if (R is 0)
                throw new InvalidOperationException("Interpolation step attempt with no radius");
            
            if (!LockValues)
            {
                float d = float3.Distance(Query, xi);

                if (d is 0)
                {
                    Values = new(Allocator.Temp, NativeArrayOptions.ClearMemory);
                    Values = u;

                    LockValues = true;

                    return;
                }

                float w = math.pow(math.max(R - d, 0) / (R * d), 2);

                SUM_w += w;

                for (int i = 0; i < SUM_wu.Length; i++)
                {
                    SUM_wu[i] += w * u[i];
                }
            }
        }

        public static bool BroadcastInterpolation(bool terrainAlreadyInterpolated)
        {
            if (SUM_w is 0)
                return false;
            
            if (!LockValues)
            {
                Values = new(SUM_wu.Length, Allocator.Temp, NativeArrayOptions.ClearMemory);

                for (int i = 0; i < SUM_wu.Length; i++)
                    Values[i] = SUM_wu[i] / SUM_w;
                
                if (!terrainAlreadyInterpolated && Query.y < 10)
                {
                    Values[0] *= Query.y / 10f;
                    Values[1] *= Query.y / 10f;
                    Values[3] *= Query.y / 10f;
                }
            }

            return true;
        }

        public static void GetClosestCell(NativeArray<float> u)
        {
            if (!LockValues)
                Values = u;
        }
    }
}