using System;
using Unity.Entities;
using Unity.Burst;
using Unity.Mathematics;
using Unity.Jobs;
using UnityEngine.Jobs;

namespace Mesocyclone
{
    [BurstCompile]
    public partial struct AirCellInitializationSystem : ISystem
    {
        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<AirCellNeedsInitialization>();
        }

        public void OnUpdate(ref SystemState state)
        {
            EntityCommandBuffer ECB = new(Allocator.Temp);
            AirCellSimulation sim = SystemAPI.GetSingletonRW<AirCellSimulation>();

            foreach
            (
                var (cell, flags, group, bounds, transform, som, entity) in
                SystemAPI
                .Query<RefRO<AirCell>, RefRO<AirCellBehaviourFlags>, RefRO<AirCellGroup>, RefRO<AirCellBounds>, RefRW<LocalTransform>, RefRW<AirCellOptimization>>()
                .WithAll<AirCellNeedsInitialization>()
                .WithEntityAccess()
            )
            {
                if (flags.ValueRO.AirCellObjects)
                {
                    #region SOM setup

                    // kill me
                    som.ValueRW.PrevStatVolume = new(group.ValueRO.CellGroupNumber, Allocator.Persistent, NativeArrayOptions.ClearMemory);
                    som.ValueRW.DynVolume = new(group.ValueRO.CellGroupNumber, Allocator.Persistent, NativeArrayOptions.ClearMemory);
                    som.ValueRW.PrevDynVolume = new(group.ValueRO.CellGroupNumber, Allocator.Persistent, NativeArrayOptions.ClearMemory);
                    som.ValueRW.StaticPressure = new(group.ValueRO.CellGroupNumber, Allocator.Persistent, NativeArrayOptions.ClearMemory);
                    som.ValueRW.Temp = new(group.ValueRO.CellGroupNumber, Allocator.Persistent, NativeArrayOptions.ClearMemory);

                    #endregion

                    #region Instantiate Air Cells

                    DynamicBuffer<AirCellGroupMember> buffer = SystemAPI.GetBuffer<AirCellGroupMember>(entity);

                    // iterate through every cell in the buffer
                    for (int i = 0; i < group.ValueRO.CellGroupNumber; i++)
                    {
                        Entity c = ECB.Instantiate(sim.Prefab);
                        buffer.Add(new AirCellGroupMember
                        {
                            Value = c
                        });

                        transform.ValueRW.Rotation = quaternion.identity;

                        float b = bounds.ValueRO.Value.x;
                        float h = bounds.ValueRO.Value.y;

                        sim.MoleTest = GlobalData.Data.Gale.AtmPressure * 1000000f * h / (GlobalData.Data.Gale.Radius * GlobalData.Data.Gale.SurfTemp * group.ValueRO.CellGroupNumber);

                        float3 InstantiateLocation = new float3
                        {
                            x = (5f * b / 12f) * ((i % 3) - 1),
                            y = ((2f * h / 7f) * ((int)i / (int)9f)) + (3f * b / 14f),
                            z = (5f * b / 12f) * ((((int)i / (int)3f) % 3) - 1)
                        };

                        ECB.SetComponent(c, LocalTransform.FromPosition(InstantiateLocation));

                        som.ValueRW.Temp[i] = cell.ValueRO.Temperature;
                    }

                    #endregion
                }

                ECB.RemoveComponent<AirCellNeedsInitialization>(entity);
            }

            // idek what this does
            ECB.Playback(state.EntityManager);
        }
    }
}