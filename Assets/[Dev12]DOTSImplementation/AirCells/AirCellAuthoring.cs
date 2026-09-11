using System.Collections.Generic;
using System;
using System.ComponentModel;
using UnityEngine;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Collections;
using Mesocyclone.Data;

// this class contains all the components for an air cell
// alongside the authoring component that prefab has attached to automatically bind
// all the ECS components

namespace Mesocyclone.MesoDOTS
{
    #region ECS Component Data

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

    // physical property that defines how resistant it is to deformation
    public struct AirCellStiffness : IComponentData
    {
        public float Value;
    }

    // self explanatory
    public struct AirCellBehaviourFlags : IComponentData
    {
        public bool AirCellObjects;
        public bool TerrainAtSeaLevel;
        public bool InterpolationWithTerrain;
        public bool FollowDrone;
    }

    public struct AirCellGroup : IComponentData
    {
        public int CellGroupNumber;
    }

    // entity sampled from the simulation singleton prefab
    [InternalBufferCapacity(30)] // arbitrary value
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

    // flag that marks that the cell needs to be initialized
    // pretty obvious
    public struct AirCellNeedsInitialization : IComponentData
    { }


    // singleton holding values for the general
    // simulation of air cells
    [EditorBrowsable(EditorBrowsableState.Advanced)]
    public struct AirCellSimulation : IComponentData
    {
        public float TimeScale;
        public float DistanceScale;
        public float GravityScale;
        public float3 DronePosition;
        public float CdTest;
        public NativeArray<float3> StartingGrid;
        public float MoleTest;
        public float TempTest;
        public float3 VelTest;
        public float3 CenterTest;
        public Entity Prefab; // the prefab entity to be used for all air cells
    }

    #endregion



    #region Authoring Component

    // the actual component attached to the GameObject prefab
    // which links all the ECS components on the entity-side
    [EditorBrowsable(EditorBrowsableState.Advanced)]
    public class AirCellAuthoring : MonoBehaviour
    {
        [Header("Main")]
        public int CellID;
        public Vector3 CellCenter;
        public float Moles = 1000f;
        public float Temperature = 300f;
        public Vector3 Velocity;
        public Vector3 Acceleration;

        [Header("Geometry")]
        public float CellStaticVolume;
        public float CellCircleArea;
        public float CellRadius;
        public float CellHeight;

        [Header("Stiffness")]
        public float StiffnessConstant = 0.5f;

        [Header("Behaviour Flags")]
        public bool AirCellObjects;
        public bool TerrainAtSeaLevel;
        public bool InterpolationWithTerrain;
        public bool FollowDrone;

        [Header("Grouping")]
        public int CellGroupNumber;

        [Header("Environment")]
        public float AverageLocalTemp;
        public float AverageLocalWind;
        public float LocalLatitude;
        public float AmbientHeat;

        [Header("Bounds")]
        public Vector2 AirCellBounds;

        [Header("Script Optimization")]
        public float[] StaticPressure;
        public float[] Temp;
        public float[] PrevStatVolume;
        public float[] DynVolume;
        public float[] PrevDynVolume;
        public NativeList<float3> CellRepulsion = new();


        #region Baker

        [EditorBrowsable(EditorBrowsableState.Never)]
        public class Baker : Baker<AirCellAuthoring>
        {
            public override void Bake(AirCellAuthoring authoring)
            {
                Entity entity = GetEntity(TransformUsageFlags.Dynamic);

                #region Struct Initialization on author cause for some fucking reason you can't assign default on decleration
                authoring.CellCenter = default;
                authoring.Velocity = default;
                authoring.Acceleration = default;
                #endregion

                AddComponent(entity, new AirCell
                {
                    ID = authoring.CellID,
                    CellCenter = authoring.CellCenter,
                    Moles = authoring.Moles,
                    Temperature = authoring.Temperature,
                    Velocity = authoring.Velocity,
                    Acceleration = authoring.Acceleration
                });

                AddComponent(entity, new AirCellGeometry
                {
                    CellStaticVolume = authoring.CellStaticVolume,
                    CellCircleArea = authoring.CellCircleArea,
                    CellRadius = authoring.CellRadius,
                    CellHeight = authoring.CellHeight
                });

                AddComponent(entity, new AirCellStiffness
                {
                    Value = authoring.StiffnessConstant
                });

                AddComponent(entity, new AirCellBehaviourFlags
                {
                    AirCellObjects = authoring.AirCellObjects,
                    TerrainAtSeaLevel = authoring.TerrainAtSeaLevel,
                    InterpolationWithTerrain = authoring.InterpolationWithTerrain,
                    FollowDrone = authoring.FollowDrone
                });

                AddComponent(entity, new AirCellGroup
                {
                    CellGroupNumber = authoring.CellGroupNumber
                });
                AddBuffer<AirCellGroupMember>(entity);

                AddComponent(entity, new AirCellLocalEnvironment
                {
                    AverageLocalTemp = authoring.AverageLocalTemp,
                    AverageLocalWind = authoring.AverageLocalWind,
                    AmbientHeat = authoring.AmbientHeat
                });

                AddComponent(entity, new AirCellOptimization
                {
                    StaticPressure = new NativeArray<float>(authoring.StaticPressure, Allocator.Temp),
                    Temp = new NativeArray<float>(authoring.Temp, Allocator.Temp),
                    PrevStatVolume = new NativeArray<float>(authoring.PrevStatVolume, Allocator.Temp),
                    DynVolume = new NativeArray<float>(authoring.DynVolume, Allocator.Temp),
                    PrevDynVolume = new NativeArray<float>(authoring.PrevDynVolume, Allocator.Temp),
                    CellRepulsion = authoring.CellRepulsion
                });

                AddComponent(entity, new AirCellBounds
                {
                    Value = authoring.AirCellBounds
                });

                AddComponent(entity, new AirCellNeedsInitialization());
            }
        }

        #endregion
    }

    #endregion
}
