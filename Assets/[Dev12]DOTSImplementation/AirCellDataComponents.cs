using System.ComponentModel;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;

// this class contains all the data components for the air cell physics system

namespace Mesocyclone.MesoDOTS
{
    #region ECS Data Components

    // base component for all air cells
    public struct AirCell : IComponentData
    {
        public int ID;
        public float3 CellCenter;
        public float Moles;
        public float Temperature;
        public float3 Velocity;
        public float3 Acceleration;
    }

    // the geometry / dimensions of the air cell
    public struct AirCellGeometry : IComponentData
    {
        public float CellStaticVolume;
        public float CellCircleArea;
        public float CellRadius;
        public float CellHeight;
    }

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
        [NativeDisableParallelForRestriction]
        public NativeArray<float> StaticPressure;
        [NativeDisableParallelForRestriction]
        public NativeArray<float> Temp;
        [NativeDisableParallelForRestriction]
        public NativeArray<float> PrevStatVolume;
        [NativeDisableParallelForRestriction]
        public NativeArray<float> DynVolume;
        [NativeDisableParallelForRestriction]
        public NativeArray<float> PrevDynVolume;
        [NativeDisableParallelForRestriction]
        public NativeList<float3> CellRepulsion;
    }

    // simulation of air cells
    [EditorBrowsable(EditorBrowsableState.Advanced)]
    public struct AirCellSimulation : IComponentData
    {
        public float TimeScale;
        public float3 DronePosition;
        public float CdTest;
        public float MoleTest;
        public float TempTest;
        public float3 VelTest;
        public float3 CenterTest;
        //public Entity Prefab; // the prefab entity to be used for all air cells
    }

    [EditorBrowsable(EditorBrowsableState.Advanced)]
    public struct AirCellGroup : IComponentData
    {
        public int CellGroupNumber;
    }

    #endregion
}
