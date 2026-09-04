using System;
using System.ComponentModel;
using UnityEngine;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Burst;
using Unity.Jobs;
using Unity.Transforms;
using Mesocyclone.Data;

namespace Mesocyclone
{
    #region Behaviours

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

    // who thought this was a good idea
    [EditorBrowsable(EditorBrowsableState.Never)]
    [BurstCompile]
    [UpdateInGroup(typeof(FixedStepSimulationSystemGroup))]
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
            // holy i am not fully typing that type
            var ECBSystem = SystemAPI.GetSingleton<EndSimulationEntityCommandBufferSystem.Singleton>();
            EntityCommandBuffer ECB = ECBSystem.CreateCommandBuffer(state.WorldUnmanaged);

            state.Dependency = new AirCellFixedUpdateJob
            {
                // no clue why there is no TimeData equivalent
                FixedDeltaTime = SystemAPI.Time.DeltaTime, // apperently it doesn't matter even if it's fixed update
                Ecb = ECB.AsParallelWriter()
            }.ScheduleParallel(state.Dependency);
        }

        [BurstCompile]
        public void OnDestroy(ref SystemState state)
        {
            
        }
    }

    #endregion



    #region Update Jobs

    /// <summary>
    /// Handles per-frame updating logic for each air cell entity
    /// </summary>
    [BurstCompile, EditorBrowsable(EditorBrowsableState.Advanced)]
    public partial struct AirCellUpdateJob : IJobEntity
    {
        public float DeltaTime;

        private void Execute(ref AirCell cell)
        {
            
        }
    }

    /// <summary>
    /// Handles per-tick updating logic for each air cell entity
    /// </summary>
    [BurstCompile, EditorBrowsable(EditorBrowsableState.Advanced)]
    public partial struct AirCellFixedUpdateJob : IJobEntity
    {
        public float FixedDeltaTime;
        public EntityCommandBuffer.ParallelWriter ECB;

        private void Execute
        (
            [ChunkIndexQuery] int chunkIndex,
            ref AirCellSimulationConfig simConfig,
            ref AirCellGroup group,
            ref LocalTransform transform,
            in AirCellBehaviourFlags flags,
            in AirCellBounds bounds
        )
        {

            if (UnityEngine.Time.timeScale is 0f)
                return;
            
            if (!flags.CellsInstantiated)
            {
                #region Instantiate Air Cells

                float l = math.floor(math.pow(group.CellGroupNumber));
                float b = bounds.Value.x;
                float h = bounds.Value.y;

                simConfig.MoleTest = GlobalData.Data.Gale.AtmPressure * 1000000f * h / (GlobalData.Data.Gale.Radius * GlobalData.Data.Gale.SurfTemp * group.CellGroupNumber);

                Entity cube = ECB.Instantiate(chunkIndex, simConfig.CubeEnity);
                transform.Position = float3.zero;

                for (int i = 0; i < group.CellGroupNumber; i++)
                {
                    float3 InstantiateLocation = float3.zero;

                    InstantiateLocation.x = (5f * b / 12f) * ((i & 3) - 1);
                    InstantiateLocation.y = ((2f * h / 7f) * ((int)i / (int)9f)) + (3f * b / 14f);
                    InstantiateLocation.z = (5f * b / 12f) * ((((int)i / (int)3f) % 3) - 1);

                    if (flags.AirCellObjects)
                        group.AirCellObjectGroup.Add(cube);
                    
                }

                #endregion
            }
        }
    }

    #endregion
}