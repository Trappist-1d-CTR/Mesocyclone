using System;
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
        public float R = 1000;
        public float3 Query = float3.zero;
        public NativeArray<float> SUM_wu = new(6, Allocator.Persistent, NativeArrayOptions.ClearMemory);
        public float SUM_w = 0;
        public bool FollowDrone = false;
        public NativeArray<float> Values = new(6, Allocator.Persistent, NativeArrayOptions.ClearMemory);
        private bool LockValues = false;

        public InverseDistanceWeighting() { }

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
                float3 diff = Query - xi;
                float d = math.sqrt(math.square(diff.x) + math.square(diff.y) + math.square(diff.z));

                if (d is 0)
                {
                    //Values = new(u.Length, Allocator.Persistent, NativeArrayOptions.ClearMemory);  wait, why do you even need this if you just set it to 'u' immediately after?
                    Values = new(u, Allocator.Temp);
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

        [BurstCompile]
        public bool BroadcastInterpolation(bool terrainAlreadyInterpolated)
        {
            if (SUM_w is 0)
            {
                return false;
            }

            if (!LockValues)
            {
                NativeArray<float> vals = new(SUM_wu.Length, Allocator.Temp);

                for (int i = 0; i < SUM_wu.Length; i++)
                    vals[i] = SUM_wu[i] / SUM_w;

                if (!terrainAlreadyInterpolated && Query.y < 10)
                {
                    vals[0] *= Query.y / 10f;
                    vals[1] *= Query.y / 10f;
                    vals[2] *= Query.y / 10f;
                }

                Values = vals;
            }

            return true;
        }

        [BurstCompile]
        public void GetClosestCell(in NativeArray<float> u)
        {
            if (!LockValues)
            {
                Values = new(u, Allocator.Temp);
            }
        }
    }
}