using Unity.Collections;
using Unity.Entities;
using Unity.Burst;
using Unity.Mathematics;
using Unity.Transforms;
using Mesocyclone.Data;

namespace Mesocyclone.MesoDOTS
{
    [BurstCompile]
    public partial struct AirCellInitializationSystem : ISystem
    {
        private RefRW<AirCellSimulation> sim;
        private RefRW<AirCellBehaviourFlags> flags;
        private AirCellGroup group;
        private RefRW<AirCellBounds> bounds;
        private float b;
        private float h;

        private bool GotSingleton;

        public void OnCreate(ref SystemState state)
        {
            GotSingleton = false;

            state.RequireForUpdate<AirCellNeedsInitialization>();
            state.RequireForUpdate<AirCellGroup>();
        }

        public void OnUpdate(ref SystemState state)
        {
            UnityEngine.Debug.Log("IsUpdating");

            foreach (var item in SystemAPI.Query<RefRW<AirCellSimulation>>())
            { sim = item; }
            foreach (var item in SystemAPI.Query<RefRW<AirCellBehaviourFlags>>())
            { flags = item; }
            foreach (var item in SystemAPI.Query<RefRW<AirCellBounds>>())
            { bounds = item; }

            if (!GotSingleton)
            {
                group = SystemAPI.GetSingleton<AirCellGroup>();
                GotSingleton = true;
            }

            b = bounds.ValueRO.Value.x;
            h = bounds.ValueRO.Value.y;
            sim.ValueRW.MoleTest = GlobalData.Data.Gale.AtmPressure * 1000000f * h / (GlobalData.Data.Gale.Radius * GlobalData.Data.Gale.SurfTemp * group.CellGroupNumber);

            EntityCommandBuffer ECB = new(Allocator.Persistent);

            foreach
            (
                var (transform, cell, entity) in
                SystemAPI
                .Query<RefRW<LocalTransform>, RefRW<AirCell>>()
                .WithAll<AirCellNeedsInitialization>()
                .WithEntityAccess()
            )
            {
                UnityEngine.Debug.Log("Is Initializing");

                if (flags.ValueRO.AirCellObjects)
                {
                    #region Instantiate Air Cell Objects

                    transform.ValueRW.Rotation = quaternion.identity;
                    transform.ValueRW = transform.ValueRO;

                    /*float3 InstantiateLocation = new float3
                    {
                        x = (5f * b / 12f) * ((cell.ValueRO.ID % 3) - 1),
                        y = ((2f * h / 7f) * (cell.ValueRO.ID / 9)) + (3f * b / 14f),
                        z = (5f * b / 12f) * (((cell.ValueRO.ID / 3) % 3) - 1)
                    };*/

                    #endregion

                    UnityEngine.Debug.Log("Completed 1 Initialization");
                }

                Unity.Mathematics.Random RandomValue = Unity.Mathematics.Random.CreateFromIndex(1);

                cell.ValueRW.CellCenter = transform.ValueRO.Position;
                cell.ValueRW.Moles = sim.ValueRO.MoleTest;
                cell.ValueRW.Temperature = sim.ValueRO.TempTest + (((RandomValue.NextFloat() * 2f) - 1f) * 25f);
                cell.ValueRW.Velocity = sim.ValueRO.VelTest + (((RandomValue.NextFloat3() * 2f) - 1f) * 10f);
                cell.ValueRW = cell.ValueRO;

                ECB.RemoveComponent<AirCellNeedsInitialization>(entity);
            }

            // idek what this does
            ECB.Playback(state.EntityManager);
            ECB.Dispose();
        }
    }
}