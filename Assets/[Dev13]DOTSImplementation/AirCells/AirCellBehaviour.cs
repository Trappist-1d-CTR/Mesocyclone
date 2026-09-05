// i fucking hate dots

using System;
using System.ComponentModel;
using System.Diagnostics;
using UnityEngine;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Collections;
using Unity.Transforms;
using Unity.Burst;
using Unity.Jobs;
using UnityEngine.Jobs;
using Mesocyclone.Data;

// systems for the behaviour of air cell entities

namespace Mesocyclone
{
    [BurstCompile]
    [UpdateInGroup(typeof(FixedStepSimulationSystemGroup))] // make it every fixed time step
    public partial struct AirCellBehaviour : ISystem
    {
        private ComponentLookup<Aircell> _airCellLookup;
        private ComponentLookup<AirCellGeometry> _geoLookup;
        private ComponentLookup<AirCellOptimization> _somLookup;

        public void OnCreate(ref SystemState state)
        {
            _airCellLookup = state.GetComponentLookup<AirCell>();
            _geoLookup = state.GetComponentLookup<AirCellGeometry>();
            _somLookup = state.GetComponentLookup<AirCellOptimization>();

            // system only starts updating if there's an entity with this component
            state.RequireForUpdate<AirCell>();
        }

        public void OnUpdate(ref SystemState state)
        {
            var sim = SystemAPI.GetSingletonRW<AirCellSimulation>();
            float dt = SystemAPI.Time.DeltaTime * sim.TimeScale;

            _airCellLookup.Update(ref state);

            state.Dependency = new AirCellUpdateJob
            {
                FixedDeltaTime = dt,
                sim = sim,
                AirCellLookup = _airCellLookup,
                GeoLookup = _geoLookup,
                SOMLookup = _somLookup
            }.ScheduleParallel(state.Dependency);
        }

        #region Physics Functions

        public static void PerformVelocity(ref AirCell cell, float deltaTime)
        {
            cell.CellCenter += cell.Velocity * deltaTime;
        }

        public static void PerformAcceleration(ref AirCell cell, float3 acc, float deltaTime)
        {
            cell.Acceleration = acc;
            cell.Velocity += cell.Acceleration * deltaTime;
        }

        public static void AccelerationAlongVelocity(ref AirCell cell, float acc, float deltaTime)
        {
            if (math.lengthsq(cell.Velocity) > 1E-10f)
            {
                cell.Acceleration = math.normalize(cell.Velocity);
                cell.Velocity += cell.Acceleration * deltaTime;
            }
        }

        #endregion


        #region Volume Functions

        // no geo?
        [BustCompile]
        public static void SetSizeV(ref AirCellGeometry geo, float v)
        {
            geo.CellStaticVolume = v;
            geo.CellHeight = math.pow(v, 1f / 3f);
            geo.CellCircleArea = v / geo.CellHeight;
            geo.CellRadius = math.sqrt(geo.CellCircleArea / math.PI);
        }

        [BustCompile]
        public static void SetSizeVL(ref AirCellGeometry geo, float v, float l)
        {
            geo.CellStaticVolume = v;
            geo.CellHeight = l;
            geo.CellCircleArea = v / l;
            geo.CellRadius = math.sqrt(geo.CellCircleArea / math.PI);
        }

        [BustCompile]
        public static void SetSizeRL(ref AirCellGeometry geo, float r, float l)
        {
            geo.CellRadius = r;
            geo.CellHeight = l;
            geo.CellCircleArea = math.pow(r, 2) * math.PI;
            geo.CellStaticVolume = geo.CellCircleArea * l;
        }

        #endregion
    }

    // actual update logic for the air cells
    [BurstCompile]
    public partial struct AirCellUpdateJob : IJobEntity
    {
        public float FixedDeltaTime;
        public AirCellSimulation sim;

        public ComponentLookup<AirCell> AirCellLookup;
        public ComponentLookup<AirCellGeometry> GeoLookup;
        public ComponentLookup<AirCellOptimization> SOMLookup;

        private void Execute
        (
            // ref is for Reading and Writing
            // in is for reading-only
            ref LocalTransform transform,
            ref AirCell cell,
            ref AirCellSimulation sim,
            ref AirCellGroup group,
            ref AirCellLocalEnvironment env,
            ref AirCellOptimization som,
            in AirCellBehaviourFlags flags,
            in AirCellBounds bounds,
            in DynamicBuffer<AirCellGroupMember> buffer
        )
        {
            if (sim.TimeScale == 0f)
                return;
            
            #region Average Local Values

            env.AverageLocalTemp = 0;
            env.AverageLocalWind = float3.zero;

            // we do all this cuz we need to scan the surrounding entities with air cell components
            for (int i = 0; i < group.CellGroupNumber; i++)
            {
                Entity member = buffer[i].Value;

                if (AirCellLookup.TryGetComponent(member, out AirCell memberCell))
                {
                    env.AverageLocalTemp += memberCell.Temperature / group.CellGroupNumber;
                    env.AverageLocalWind += memberCell.Temperature / group.CellGroupNumber;

                    #region Values Setup

                    DebugEverything(i, in buffer, in AirCellLookup, in GeoLookup, in SOMLookup);

                    #region Calculate Static Pressure
                    som.StaticPressure[i] = GlobalCalc.StaticPressureAtHeight(memberCell.CellCenter.y);
                    #endregion

                    DebugEverything(i, in buffer, in AirCellLookup, in GeoLookup, in SOMLookup);

                    // only lads with true ball know this is not the original

                    //double mem; // ...he glazes afar into the distance, as he realizes he is amongst the only double left...
                    float mem; // nevermind

                    #region Insolation
                    memberCell.Temperature = som.Temp[i];
                    memberCell.Temperature += mem = GlobalData.Data.Gale.Insolation.Evaluate();
                    #endregion

                    #endregion
                }
            }

            #endregion
        }

        #region Debug

        [BustCompile]
        [Conditional("DEV")]
        public void DebugEverything
        (
            int i,
            in DynamicBuffer<AirCellGroupMember> buffer,
            in ComponentLookup<AirCell> airCellLookup,
            in ComponentLookup<AirCellGeometry> geoLookup,
            in ComponentLookup<AirCellOptimization> somLookup
        )
        {
            Entity member = buffer[i].Value;

            if
            (
                airCellLookup.TryGetComponent(member, out AirCell c)
                &&
                geoLookup.TryGetComponent(member, out AirCellGeometry geo)
                &&
                somLookup.TryGetComponent(member, out AirCellOptimization som)
            )
            {
                float3 vel = c.Velocity;

                if (!float.IsFinite(c.CellCenter.x) || !float.IsFinite(c.CellCenter.y) || !float.IsFinite(c.CellCenter.z))
                    UnityEngine.Debug.LogError($"NaN Position\ni = {i}");

                if (!float.IsFinite(vel.x) || !float.IsFinite(vel.y) || !float.IsFinite(vel.z))
                    UnityEngine.Debug.LogError($"Nan Cell Velocity\ni = {i}");
                
                if (!float.IsFinite(c.Temperature))
                    UnityEngine.Debug.LogError($"NaN Cell Temperature\ni = {i}");
                
                if (!float.IsFinite(geo.CellStaticVolume))
                    UnityEngine.Debug.LogError($"NaN Cell Volume\ni = {i}");

                if (som.PrevStatVolume[i] <= 0 && c.CellCenter.y < geo.CellHeight / 2f)
                    UnityEngine.Debug.LogError($"Negative/Null PrevStatVolume\ni = {i}");
                
                if (c.CellCenter.y <= -geo.CellHeight / 2f);
                    UnityEngine.Debug.LogError($"ACDDC - Air Cell Digging Down to China\ni = {i}");
            }
        }

        #endregion
    }
}