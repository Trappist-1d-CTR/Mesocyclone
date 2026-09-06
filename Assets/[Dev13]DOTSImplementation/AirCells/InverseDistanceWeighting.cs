using System;
using UnityEngine;
using Unity.Mathematics;
using Unity.Collections;
using Unity.Burst;

// fuck you
//#pragma warning disable CA2211
//wut?... not getting any CA2211 errors btw </3

namespace Mesocyclone.MesoDOTS
{
    [BurstCompile]
    // the DOTS compatable version
    public class InverseDistanceWeighting
    {
        public static InverseDistanceWeighting Instance { get; private set; }
        public InverseDistanceWeighting()
        {
            Instance = this;
        }

        public float R = 1000;
        public float3 Query;
        public readonly NativeList<int> Indices = new(Allocator.Persistent); // wayy too lazy to manually dispose of this  Astraa: NUH UH!

        public NativeArray<float> SUM_wu = new(6, Allocator.Persistent, NativeArrayOptions.ClearMemory);
        public float SUM_w;

        public bool FollowDrone;

        public NativeArray<float> Values { get; private set; }
        private bool LockValues;

        [BurstCompile]
        public void IntializeIndicesNative()
        {
            
        }

        [BurstCompile]
        public void DronePosition(in float3 position)
        {
            Query = FollowDrone ? new float3(0, position.y, 0) : position;
        }

        [BurstCompile]
        public void Add(int index)
        {
            Indices.Add(index);
        }

        [BurstCompile]
        public void Remove(int index)
        {
            Indices.RemoveAt(index);
        }

        [BurstCompile]
        public void BeginInterpolation()
        {
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

        [BurstCompile]
        public bool BroadcastInterpolation(bool terrainAlreadyInterpolated)
        {
            if (SUM_w is 0)
                return false;
            
            if (!LockValues)
            {
                NativeArray<float> vals = new(SUM_wu.Length, Allocator.Persistent, NativeArrayOptions.ClearMemory);

                for (int i = 0; i < SUM_wu.Length; i++)
                    vals[i] = SUM_wu[i] / SUM_w;
                
                if (!terrainAlreadyInterpolated && Query.y < 10)
                {
                    vals[0] *= Query.y / 10f;
                    vals[1] *= Query.y / 10f;
                    vals[2] *= Query.y / 10f;
                }

                Values = vals;
                vals.Dispose();
            }

            return true;
        }

        [BurstCompile]
        public void GetClosestCell(in NativeArray<float> u)
        {
            if (!LockValues)
                Values = u;
        }
    }
}

#pragma warning restore CA2211
