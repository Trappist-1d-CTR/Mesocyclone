using Unity.Collections;
using System;
using System.Linq;
using Unity.Entities;
using Unity.Burst;
using Unity.Mathematics;
using Unity.Transforms;
using Unity.Jobs;
using UnityEngine.Jobs;
using Mesocyclone.Data;

namespace Mesocyclone.MesoDOTS
{
    [BurstCompile]
    public partial struct AirCellInitializationSystem : ISystem
    {
        private RefRW<AirCellSimulation> sim;
        private RefRW<AirCellBehaviourFlags> flags;
        private RefRW<AirCellOptimization> som;
        private RefRW<AirCellGroup> group;
        private RefRW<AirCellBounds> bounds;
        private RefRW<AirCellBuffer> buffer;
        private float b;
        private float h;

        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<AirCellNeedsInitialization>();

            buffer = new();

            //Get Singletons
            sim = SystemAPI.Query<RefRW<AirCellSimulation>>().First();
            flags = SystemAPI.Query<RefRW<AirCellBehaviourFlags>>().First();
            som = SystemAPI.Query<RefRW<AirCellOptimization>>().First();
            group = SystemAPI.Query<RefRW<AirCellGroup>>().First();
            bounds = SystemAPI.Query<RefRW<AirCellBounds>>().First();
            buffer = SystemAPI.Query<RefRW<AirCellBuffer>>().First();

            #region Global setups

            som.ValueRW.PrevStatVolume = new(group.ValueRO.CellGroupNumber, Allocator.Persistent, NativeArrayOptions.ClearMemory);
            som.ValueRW.DynVolume = new(group.ValueRO.CellGroupNumber, Allocator.Persistent, NativeArrayOptions.ClearMemory);
            som.ValueRW.PrevDynVolume = new(group.ValueRO.CellGroupNumber, Allocator.Persistent, NativeArrayOptions.ClearMemory);
            som.ValueRW.StaticPressure = new(group.ValueRO.CellGroupNumber, Allocator.Persistent, NativeArrayOptions.ClearMemory);
            som.ValueRW.Temp = new(group.ValueRO.CellGroupNumber, Allocator.Persistent, NativeArrayOptions.ClearMemory);
            som.ValueRW = som.ValueRO;

            b = bounds.ValueRO.Value.x;
            h = bounds.ValueRO.Value.y;

            sim.ValueRW.MoleTest = GlobalData.Data.Gale.AtmPressure * 1000000f * h / (GlobalData.Data.Gale.Radius * GlobalData.Data.Gale.SurfTemp * group.ValueRO.CellGroupNumber);
            sim.ValueRW = sim.ValueRO;

            #endregion
        }

        public void OnUpdate(ref SystemState state)
        {
            EntityCommandBuffer ECB = new(Allocator.Temp);

            foreach
            (
                var (cell, transform, entity) in
                SystemAPI
                .Query<RefRW<AirCell>, RefRW<LocalTransform>>()
                .WithAll<AirCellNeedsInitialization>()
                .WithEntityAccess()
            )
            {
                if (flags.ValueRO.AirCellObjects)
                {
                    #region Instantiate Air Cell Objects

                    Entity c = ECB.Instantiate(sim.ValueRO.Prefab);
                    buffer.ValueRW.Buffer.Add(new AirCellGroupMember
                    {
                        Value = c
                    });

                    transform.ValueRW.Rotation = quaternion.identity;
                    transform.ValueRW = transform.ValueRO;

                    float3 InstantiateLocation = transform.ValueRO.Position;

                    /*float3 InstantiateLocation = new float3
                    {
                        x = (5f * b / 12f) * ((cell.ValueRO.ID % 3) - 1),
                        y = ((2f * h / 7f) * (cell.ValueRO.ID / 9)) + (3f * b / 14f),
                        z = (5f * b / 12f) * (((cell.ValueRO.ID / 3) % 3) - 1)
                    };*/

                    ECB.SetComponent(c, LocalTransform.FromPosition(InstantiateLocation));

                    #endregion
                }

                Unity.Mathematics.Random RandomValue = Unity.Mathematics.Random.CreateFromIndex(1);

                cell.ValueRW.CellCenter = transform.ValueRO.Position;
                cell.ValueRW.Moles = sim.ValueRO.MoleTest;
                cell.ValueRW.Temperature = sim.ValueRO.TempTest + (((RandomValue.NextFloat() * 2f) - 1f) * 25f);
                cell.ValueRW.Velocity = sim.ValueRO.VelTest + (((RandomValue.NextFloat3() * 2f) - 1f) * 10f);
                cell.ValueRW = cell.ValueRO;

                som.ValueRW.Temp[cell.ValueRO.ID] = cell.ValueRO.Temperature;
                som.ValueRW = som.ValueRO;

                ECB.RemoveComponent<AirCellNeedsInitialization>(entity);
            }

            // idek what this does
            ECB.Playback(state.EntityManager);
        }
    }
}