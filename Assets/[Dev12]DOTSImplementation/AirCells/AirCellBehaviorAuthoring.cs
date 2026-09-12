using System.ComponentModel;
using UnityEngine;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Collections;

namespace Mesocyclone.MesoDOTS
{
    #region ECS Component Data

    // self explanatory
    public struct AirCellBehaviourFlags : IComponentData
    {
        public bool AirCellObjects;
        public bool TerrainAtSeaLevel;
        public bool InterpolationWithTerrain;
        public bool FollowDrone;
    }

    // entity sampled from the simulation singleton prefab
    [InternalBufferCapacity(32)] // arbitrary value
    public struct AirCellGroupMember : IBufferElementData
    {
        public Entity Value;
    }

    // data on the local environment to read off of
    public struct AirCellLocalEnvironment : IComponentData
    {
        public float AverageLocalTemp;
        public float3 AverageLocalWind;
        public float AmbientHeat;
    }

    // the bounds the air cell is restricted to
    public struct AirCellBounds : IComponentData
    {
        public float2 Value;
    }

    // handles values which change stuff about the logic
    // for performance
    public struct AirCellOptimization : IComponentData
    {
        public NativeArray<float> StaticPressure;
        public NativeArray<float> Temp;
        public NativeArray<float> PrevStatVolume;
        public NativeArray<float> DynVolume;
        public NativeArray<float> PrevDynVolume;
        public NativeList<float3> CellRepulsion;
    }

    // simulation of air cells
    [EditorBrowsable(EditorBrowsableState.Advanced)]
    public struct AirCellSimulation : IComponentData
    {
        public float TimeScale;
        public float DistanceScale;
        public float GravityScale;
        public float3 DronePosition;
        public float CdTest;
        public float MoleTest;
        public float TempTest;
        public float3 VelTest;
        public float3 CenterTest;
        public Entity Prefab; // the prefab entity to be used for all air cells
    }

    [EditorBrowsable(EditorBrowsableState.Advanced)]
    public struct AirCellGroup : IComponentData
    {
        public int CellGroupNumber;
    }

    public struct AirCellBuffer : IComponentData
    {
        public NativeList<AirCellGroupMember> Buffer;
    }

    #endregion


    #region Singleton Components

    public partial struct SingletonInitSystem : ISystem
    {
        public void OnCreate(ref SystemState state)
        {
            int CellNumber = 32;

            _ = state.EntityManager.CreateSingleton<AirCellGroup>(new AirCellGroup
            {
                CellGroupNumber = CellNumber
            });

            _ = state.EntityManager.CreateSingleton<AirCellOptimization>(new AirCellOptimization
            {
                StaticPressure = new(CellNumber, Allocator.Persistent),
                PrevStatVolume = new(CellNumber, Allocator.Persistent),
                DynVolume = new(CellNumber, Allocator.Persistent),
                PrevDynVolume = new(CellNumber, Allocator.Persistent),
                Temp = new(CellNumber, Allocator.Persistent),
                CellRepulsion = new(Allocator.Persistent)
            });

            _ = state.EntityManager.CreateSingleton<AirCellBuffer>(new AirCellBuffer
            {
                Buffer = new(Allocator.Persistent)
            });

            UnityEngine.Debug.Log("Singletons Created");
        }
    }

    #endregion

    #region Authoring Component

    public class AirCellBehaviorAuthoring : MonoBehaviour
    {
        [Header("Behaviour Flags")]
        public bool AirCellObjects;
        public bool TerrainAtSeaLevel;
        public bool InterpolationWithTerrain;
        public bool FollowDrone;

        [Header("Environment")]
        public float AverageLocalTemp;
        public float AverageLocalWind;
        public float AmbientHeat;

        [Header("Simulation")]
        public float TimeScale = 1f;
        public float DistanceScale = 1f;
        public float GravityScale = 1f;
        public Vector3 DronePosition;
        public float CdTest;
        public float MoleTest;
        public float TempTest;
        public Vector3 VelTest;
        public Vector3 CenterTest;
        public GameObject Prefab;

        [Header("Bounds")]
        public Vector2 AirCellBounds;


        #region Baker

        [EditorBrowsable(EditorBrowsableState.Never)]
        public class Baker : Baker<AirCellBehaviorAuthoring>
        {
            public override void Bake(AirCellBehaviorAuthoring authoring)
            {
                Entity entity = GetEntity(TransformUsageFlags.None);

                _ = DependsOn(authoring.Prefab);
                Entity prefab = GetEntity(authoring.Prefab, TransformUsageFlags.Dynamic);

                AddComponent(entity, new AirCellBehaviourFlags
                {
                    AirCellObjects = authoring.AirCellObjects,
                    TerrainAtSeaLevel = authoring.TerrainAtSeaLevel,
                    InterpolationWithTerrain = authoring.InterpolationWithTerrain,
                    FollowDrone = authoring.FollowDrone
                });

                _ = AddBuffer<AirCellGroupMember>(entity);

                AddComponent(entity, new AirCellLocalEnvironment
                {
                    AverageLocalTemp = authoring.AverageLocalTemp,
                    AverageLocalWind = authoring.AverageLocalWind,
                    AmbientHeat = authoring.AmbientHeat
                });

                AddComponent(entity, new AirCellSimulation
                {
                    TimeScale = authoring.TimeScale,
                    DistanceScale = authoring.DistanceScale,
                    GravityScale = authoring.GravityScale,
                    DronePosition = authoring.DronePosition,
                    CdTest = authoring.CdTest,
                    MoleTest = authoring.MoleTest,
                    TempTest = authoring.TempTest,
                    VelTest = authoring.VelTest,
                    CenterTest = authoring.CenterTest,
                    Prefab = prefab
                });

                AddComponent(entity, new AirCellBounds
                {
                    Value = authoring.AirCellBounds
                });

                /*
                var flags = SystemAPI.
                RW<AirCellBehaviourFlags>();
                flags.ValueRW = new()
                {
                    AirCellObjects = authoring.AirCellObjects,
                    TerrainAtSeaLevel = authoring.TerrainAtSeaLevel,
                    InterpolationWithTerrain = authoring.InterpolationWithTerrain,
                    FollowDrone = authoring.FollowDrone
                };

                var group = SystemAPI.GetSingletonRW<AirCellGroup>();
                group.ValueRW = new()
                {
                    CellGroupNumber = authoring.CellGroupNumber
                };

                var env = SystemAPI.GetSingletonRW<AirCellLocalEnvironment>();
                env.ValueRW = new()
                {
                    AverageLocalTemp = authoring.AverageLocalTemp,
                    AverageLocalWind = authoring.AverageLocalWind,
                    AmbientHeat = authoring.AmbientHeat
                };

                var sim = SystemAPI.GetSingletonRW<AirCellSimulation>();
                sim.ValueRW = new()
                {
                    TimeScale = authoring.TimeScale,
                    DistanceScale = authoring.DistanceScale,
                    GravityScale = authoring.GravityScale,
                    DronePosition = authoring.DronePosition,
                    CdTest = authoring.CdTest,
                    StartingGrid = new NativeArray<float3>(authoring.StartingGrid, Allocator.Temp),
                    MoleTest = authoring.MoleTest,
                    TempTest = authoring.TempTest,
                    VelTest = authoring.VelTest,
                    CenterTest = authoring.CenterTest,
                    Prefab = prefab
                };

                var bounds = SystemAPI.GetSingletonRW<AirCellBounds>();
                bounds.ValueRW = new()
                {
                    Value = authoring.AirCellBounds
                };

                var som = SystemAPI.GetSingletonRW<AirCellOptimization>();
                som.ValueRW = new()
                {
                    StaticPressure = new NativeArray<float>(authoring.StaticPressure, Allocator.Temp),
                    Temp = new NativeArray<float>(authoring.Temp, Allocator.Temp),
                    PrevStatVolume = new NativeArray<float>(authoring.PrevStatVolume, Allocator.Temp),
                    DynVolume = new NativeArray<float>(authoring.DynVolume, Allocator.Temp),
                    PrevDynVolume = new NativeArray<float>(authoring.PrevDynVolume, Allocator.Temp),
                    CellRepulsion = authoring.CellRepulsion
                };

                var buffer = SystemAPI.GetSingletonRW<AirCellBuffer>();
                buffer.ValueRW = new()
                {
                    Buffer = new()
                };
                */
            }
        }

        #endregion
    }

    #endregion


    /*public class SetupDataSingletons : SystemBase
    {
        protected override void OnCreate()
        {
            int NumberOfAirCells = 32;

            _ = World.EntityManager.CreateEntity(ComponentType.ReadOnly<AirCellGroup>());
            SystemAPI.SetSingleton(new AirCellGroup
            {
                CellGroupNumber = NumberOfAirCells
            });

            _ = World.EntityManager.CreateEntity(ComponentType.ReadOnly<AirCellBehaviourFlags>());
            SystemAPI.SetSingleton(new AirCellBehaviourFlags
            {
                AirCellObjects = true,
                TerrainAtSeaLevel = false,
                InterpolationWithTerrain = false,
                FollowDrone = true
            });

            _ = World.EntityManager.CreateEntity(ComponentType.ReadOnly<AirCellLocalEnvironment>());
            SystemAPI.SetSingleton(new AirCellLocalEnvironment
            {
                AverageLocalTemp = 0,
                AverageLocalWind = float3.zero,
                AmbientHeat = 0
            });

            _ = World.EntityManager.CreateEntity(ComponentType.ReadOnly<AirCellSimulation>());
            SystemAPI.SetSingleton(new AirCellSimulation
            {
                TimeScale = 1,
                DistanceScale = 1,
                GravityScale = 1,
                DronePosition = float3.zero,
                CdTest = 1,
                StartingGrid = new NativeArray<float3>(64, Allocator.Persistent),
                MoleTest = 100000,
                TempTest = 1500,
                VelTest = float3.zero,
                CenterTest = float3.zero
            });

            _ = World.EntityManager.CreateEntity(ComponentType.ReadOnly<AirCellBounds>());
            SystemAPI.SetSingleton(new AirCellBounds
            {
                Value = new(1000, 1000)
            });

            _ = World.EntityManager.CreateEntity(ComponentType.ReadOnly<AirCellOptimization>());
            SystemAPI.SetSingleton(new AirCellOptimization
            {
                StaticPressure = new NativeArray<float>(NumberOfAirCells, Allocator.Persistent),
                Temp = new NativeArray<float>(NumberOfAirCells, Allocator.Persistent),
                PrevStatVolume = new NativeArray<float>(NumberOfAirCells, Allocator.Persistent),
                DynVolume = new NativeArray<float>(NumberOfAirCells, Allocator.Persistent),
                PrevDynVolume = new NativeArray<float>(NumberOfAirCells, Allocator.Persistent),
                CellRepulsion = new NativeList<float3>(Allocator.Persistent)
            });

            _ = World.EntityManager.CreateEntity(ComponentType.ReadOnly<AirCellBuffer>());
            SystemAPI.SetSingleton(new AirCellBuffer
            {
                Buffer = new()
            });
        }

        protected override void OnUpdate()
        {

        }
    }*/
}

