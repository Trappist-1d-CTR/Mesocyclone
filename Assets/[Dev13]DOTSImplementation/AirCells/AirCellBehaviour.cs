using System;
using System.ComponentModel;
using UnityEngine;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Burst;
using Unity.Jobs;
using Mesocyclone.Data;

namespace Mesocyclone
{
    /// <summary>
    /// Handles the base behaviour of all air cell entities
    /// </summary>
    [BurstCompile]
    public partial struct AirCellBehaviour : ISystem
    {
        [BurstCompile]
        public void OnCreate(ref SystemState state)
        {
            // doesn't call update if there is no entities with an AirCell component
            // since systems are constantly querying
            state.RequireForUpdate<AirCell>();
        }

        // utilize the AirCellUpdateJob for updating
        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            state.Dependency = new AirCellUpdateJob
            {
                DeltaTime = SystemAPI.Time.DeltaTime
            }.ScheduleParallel(state.Dependency);
        }

        [BurstCompile]
        public void OnDestroy(ref SystemState state)
        {
            
        }
    }

    [EditorBrowsable(EditorBrowsableState.Never), BurstCompile, UpdateInGroup(typeof(FixedStepSimulationSystemGroup))] // who thought this was a good idea
    public partial struct AirCellFixedBehaviour : ISystem
    {
        [BurstCompile]
        public void OnCreate(ref SystemState state)
        {
            // same here
            state.RequireForUpdate<AirCell>();
        }

        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            state.Dependency = new AirCellFixedUpdateJob
            {
                FixedDeltaTime = SystemAPI.Time.FixedDeltaTime
            }.ScheduleParallel(state.Dependency);
        }

        [BurstCompile]
        public void OnDestroy(ref SystemState state)
        {
            
        }
    }

    /// <summary>
    /// Handles per-frame updating logic for each air cell entity
    /// </summary>
    [BurstCompile]
    public partial struct AirCellUpdateJob : IJobEntity
    {
        public float DeltaTime;

        private void Execute(ref AirCellData cell)
        {
            
        }
    }

    /// <summary>
    /// Handles per-tick updating logic for each air cell entity
    /// </summary>
    [BurstCompile]
    public partial struct AirCellFixedUpdateJob : IJobEntity
    {
        public float FixedDeltaTime;

        private void Execute(ref AirCellData cell)
        {
            
        }
    }
}