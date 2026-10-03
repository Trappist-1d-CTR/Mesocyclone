/*using System;
using Unity.Mathematics;
using Unity.Collections;
using Unity.Burst;
using Unity.Entities;

// fuck you
//#pragma warning disable CA2211
//wut?... not getting any CA2211 errors btw </3

namespace Mesocyclone.MesoDOTS
{
    [BurstCompile]
    // the DOTS compatable version
    public struct InverseDistanceWeighting : IComponentData
    {
        public static InverseDistanceWeighting instance;
        public float R;
        public float3 Query;
        public NativeArray<float> SUM_wu;
        public float SUM_w;
        public bool FollowDrone;
        public NativeArray<float> Values;
        public bool LockValues = false;

        public InverseDistanceWeighting()
        {
            R = 25000;
            Query = 0;
            SUM_wu = new(6, Allocator.Persistent);
            SUM_w = 0;
            FollowDrone = false;
            Values = new(6, Allocator.Persistent);
            LockValues = false;
            instance = this;
        }

        [BurstCompile]
        public void DronePosition(in float3 position)
        {
            Query = FollowDrone ? new float3(0, position.y, 0) : position;
        }

        [BurstCompile]
        public void BeginInterpolation(bool followDrone)
        {
            FollowDrone = followDrone;

            SUM_wu[0] = 0f;
            SUM_wu[1] = 0f;
            SUM_wu[2] = 0f;
            SUM_wu[3] = 0f;
            SUM_wu[4] = 0f;
            SUM_wu[5] = 0f;

            SUM_w = 0;
            LockValues = false;
        }

        [BurstCompile]
        public void InterpolationStep(in float3 xi, in NativeArray<float> u)
        {
            if (R is 0)
                throw new InvalidOperationException("Interpolation step attempt with no radius");

            if (!LockValues)
            {
                float d = math.distance(Query, xi);

                if (d is 0)
                {
                    //Values = new(u.Length, Allocator.Persistent, NativeArrayOptions.ClearMemory);  wait, why do you even need this if you just set it to 'u' immediately after?
                    for (int i = 0; i < u.Length; i++)
                        Values[i] = u[i];
                    LockValues = true;

                    return;
                }

                float w = math.pow(math.max(R - d, 0) / (R * d), 2) + 1;

                SUM_w += w;

                for (int i = 0; i < SUM_wu.Length; i++)
                {
                    SUM_wu[i] += w * u[i];
                }
            }
        }

        [BurstCompile]
        public void BroadcastInterpolation(bool terrainAlreadyInterpolated)
        {
            if (SUM_w is 0)
            {
                return;
            }

            if (!LockValues)
            {
                NativeArray<float> vals = new(SUM_wu.Length, Allocator.Temp);

                UnityEngine.Debug.Log($"{SUM_wu[0]} ; {SUM_w}");

                for (int i = 0; i < SUM_wu.Length; i++)
                    vals[i] = SUM_wu[i] / SUM_w;

                if (!terrainAlreadyInterpolated && Query.y < 10)
                {
                    vals[0] *= Query.y / 10f;
                    vals[1] *= Query.y / 10f;
                    vals[2] *= Query.y / 10f;
                }

                for (int i = 0; i < vals.Length; i++)
                    Values[i] = vals[i];
            }
        }

        [BurstCompile]
        public void GetClosestCell(in NativeArray<float> u)
        {
            if (!LockValues)
            {
                for (int i = 0; i < u.Length; i++)
                    Values[i] = u[i];
            }
        }
    }
}
*/