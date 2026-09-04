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
    [BurstCompile]
    public partial struct AirCellSpawnSystem : ISystem
    {
        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<AirCellInNeedOfSpawnPlz>();
        }

        public void OnUpdate(ref SystemState state)
        {
            var ECB = new EntityCommandBuffer(Allocator.Temp); // 2 lazy to manually dispose this aslwell

            foreach
            (
                // my condolences.
                var (config, group, bounds, entity)
                in
                SystemAPI.Query
                <
                    RefRW<AirCellSimulationConfig>,
                    RefRO<AirCellGroup>,
                    RefRO<AirCellBounds>
                >()
                .WithAll<AirCellInNeedOfSpawnPlz>()
                .WithEntityAccess()
            )
            {
                ECB.RemoveComponent<AirCellInNeedOfSpawnPlz>(entity);
            }

            ECB.Playback(state.EntityManager);
        }
    }
}