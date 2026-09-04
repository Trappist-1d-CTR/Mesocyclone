/*

// would be amazing if we had global using declerations in unity -.-

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
    #region Components

    /// <summary>
    /// Entity component which contains all the base variables for air cells
    /// <br/><br/>
    /// These values are touched pretty much all the time
    /// </summary>
    public struct AirCell : IComponentData
    {
        public float3 CellCenter;
        public float Moles;
        public float Temperature;
        public float3 Velocity;
        public float3 Acceleration;
    }

    /// <summary>
    /// Entity component for air cell geometry; self-explanatory
    /// </summary>
    public struct AirCellGeometry : IComponentData
    {
        public float CellStaticVolume;
        public float CellCircleArea;
        public float CellRadius;
        public float CellHeight;
    }

    /// <summary>
    /// Entity component for air cell stiffness; how resistant it is to deformation
    /// </summary>
    public struct AirCellStiffness : IComponentData
    {
        public NativeOnce<float> Value;
    }

    /// <summary>
    /// Entity component for special air cell behaviour flags (booleans)
    /// </summary>
    public struct AirCellBehaviourFlags : IComponentData
    {
        // technically entities but tomato tamato
        public bool AirCellObjects;
        public bool TerrainAtSeaLevel; // ironic
        public bool InterpolationWithTerrain;
        public bool CellsInstantiated;
    }

    /// <summary>
    /// Entity component that acts as a sort of "toggle" to whether it should follow the drone or not
    /// </summary>
    public struct FollowDrone : IComponentData, IEnableableComponent // WithAll<FollowDrone>;
    { }

    public struct AirCellGroup : IComponentData
    {
        public AirCellGroupMember AirCellGroup;
        public NativeList<Entity> AirCellObjectGroup;
        public int CellGroupNumber;
    }

    /// <summary>
    /// Basically a native array for Entities (air cells in this case)
    /// <br/><br/>
    /// Idk what this even does some random guy said this works so i'm trusting him
    /// </summary>
    [InternalBufferCapacity(30)] // i believe this is how many air cells can exist
    public struct AirCellGroupMember : IBufferElementData
    {
        public Entity Value;
    }

    /// <summary>
    /// Entity component for certain data on the environment
    /// </summary>
    public struct AirCellLocalEnvironment : IComponentData
    {
        public float AverageLocalTemp;
        public float3 AverageLocalWind;
        public float LocalLatitude;
        public float AmbientHeat;
        //public BlobAssetReferece<BlobCurve> AmbientalHeat; // thanks astraa
    }

    public struct AirCellMemoryOptimization
    {
        public NativeArray<float> StaticPressureSOM;
        public NativeArray<float> TempSOM;
        public NativeArray<float> PrevStatVolumeSOM;
        public NativeArray<float> DynVolumeSOM;
        public NativeArray<float> PrevDynVolumeSOM;
        public NativeList<float3> CellRepulsionSOM;
    }

    /// <summary>
    /// Self explanatory
    /// </summary>
    public struct AirCellBounds : IComponentData
    {
        public float2 Value;
    }

    [EditorBrowsable(EditorBrowsableState.Advanced)]
    public struct AirCellSimulationConfig : IComponentData
    {
        public float3 DronePosition;
        public float CdTest;
        public float TimeScale;
        public float DistanceScale;
        public float GravityScale;
        public NativeList<float3> AirCellStartingGrid;
        public float MoleTest;
        public float TempTest;
        public float3 VelTest;
        public float3 CenterTest;
        public Entity CubeEntity;
    }

    /// <summary>
    /// Entity component attached for the moment the air cell needs to be spawned. Which gets seen by the AirCellSpawnSystem and actually spawns in the air cell :]
    /// </summary>
    public struct AirCellInNeedOfSpawnPlz : IComponentData
    { }

    #endregion



    #region American Authors

    /// <summary>
    /// The author for all aircell related components & bakers
    /// </summary>
    [EditorBrowsable(EditorBrowsableState.Advanced)]
    public class AirCellAuthoring : MonoBehaviour
    {
        [Header("Main")]
        public Vector3 CellCenter;
        public float Moles;
        public float Temperature;
        public Vector3 Velocity;
        public Vector3 Acceleration;

        [Header("Geometry")]
        public float CellStaticVolume;
        public float CellCircleArea;
        public float CellRadius;
        public float CellHeight;

        [Header("Stiffness")]
        public NativeOnce<float> StiffnessConstant;

        [Header("Behaviour Flags")]
        public bool AirCellObjects;
        public bool TerrainAtSeaLevel; // ironic 2
        public bool InterpolationWithTerrain;
        public bool CellsInstantiated;

        [Header("Cell Group")]
        public int CellGroupNumber;

        [Header("Environment")]
        public float AverageLocalTemp;
        public Vector3 AverageLocalWind;
        public float LocalLatitude;
        public float AmbientHeat;

        [Header("Bounds")]
        public Vector2 AirCellBounds;

        [Header("Simulation")]
        public float TimeScale;
        public float DistanceScale;
        public float GravityScale;
        public GameObject CubeEnity;

        [EditorBrowsable(EditorBrowsableState.Never)]
        public class Baker : Baker<AirCellAuthoring>
        {
            public override void Bake(AirCellAuthoring authoring)
            {
                Entity entity = GetEntity(TransformUsageFlags.Dynamic);

                // base values
                authoring.CellCenter = default;
                authoring.Moles = 1000;
                authoring.Temperature = 300;
                authoring.Velocity = default;
                authoring.StiffnessConstant.Assign(0.5f);

                authoring.TimeScale = 1f;
                authoring.CellsInstantiated = false;

                AddComponent(entity, new AirCell
                {
                   CellCenter = authoring.CellCenter,
                   Moles = authoring.Moles,
                   Temperature = authoring.Temperature,
                   Velocity = authoring.Velocity,
                   Acceleration = authoring.Acceleration,
                });

                AddComponent(entity, new AirCellGeometry
                {
                    CellCircleArea = authoring.CellCircleArea,
                    CellHeight = authoring.CellHeight,
                    CellRadius = authoring.CellRadius,
                    CellStaticVolume = authoring.CellStaticVolume
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
                    CellsInstantiated = authoring.CellsInstantiated
                });

                AddComponent(entity, new FollowDrone());
                SetComponentEnabled<FollowDrone>(entity, true);

                AddComponent(entity, new AirCellGroup
                {
                    CellGroupNumber = authoring.CellGroupNumber
                });

                AddBuffer<AirCellGroupMember>(entity);

                AddComponent(entity, new AirCellLocalEnvironment
                {
                    AverageLocalTemp = authoring.AverageLocalTemp,
                    AverageLocalWind = authoring.AverageLocalWind,
                    LocalLatitude = authoring.LocalLatitude,
                    AmbientHeat = authoring.AmbientHeat
                });

                AddComponent(entity, new AirCellBounds
                {
                    Value = authoring.AirCellBounds
                });

                DependsOn(authoring.CubeEnity);
                Entity cube = GetEntity(authoring.CubeEnity, TransformUsageFlags.Dynamic);

                AddComponent(entity, new AirCellSimulationConfig
                {
                    TimeScale = authoring.TimeScale,
                    DistanceScale = authoring.DistanceScale,
                    GravityScale = authoring.GravityScale,
                    CubeEntity = cube
                });

                AddComponent(entity, new AirCellInNeedOfSpawnPlz());

                // ye so apperently order matters here
                // one of my least favorite things abt programming -.-
                InverseDistanceWeighting.FollowDrone = IsComponentEnabled<FollowDrone>(entity);
            }
        }
    }

    [Obsolete]
    public class AirCellAuthoring : MonoBehaviour
    {
        public AnimationCurve AmbientalHeat;

        // intialize values
        private void Awake()
        {
            CellCenter = default;
            Moles = 1000;
            Temperature = 300;
            Velocity = default;
            StiffnessConstant.Assign(0.5f);
        }

        [EditorBrowsable(EditorBrowsableState.Never)]
        public class AirCellDataBaker : Baker<AirCellDataAuthoring>
        {
            public override void Bake(AirCellDataAuthoring authoring)
            {
                Entity airCell = GetEntity(TransformUsageFlags.ManualOverride);

                using BlobBuilder builder = new(Allocator.Temp);

                // ngl, despite all my years programing, i have no clue what this is
                ref BlobCurve root = ref builder.ConstructRoot<BlobCurve>();

                const int sampleCount = 32; // how many key frames are in the AC. Tune to however
                BlobBuilderArray<float2> array = builder.Allocate(ref root.KeyFrames, sampleCount);

                for (int i = 0; i < sampleCount; i++)
                {
                    float t = (float)i / (sampleCount - 1);
                    float time = math.lerp(0f, 1f, t); // assumes the curve "bounds" (ig) is [0,1]
                    array[i] = new float2(time, authoring.AmbientalHeat.Evaluate(time));
                }

                BlobCurve blob = builder.CreateBlobAssetReference<BlobCurve>(Allocator.Persistent);

                AddBlobAsset(ref blob, out _);

                AddComponent(airCell, new AirCellData
                {
                    CellCenter = authoring.CellCenter,
                    Moles = authoring.Moles,
                    Temperature = authoring.Temperature,
                    Velocity = authoring.Velocity,
                    Acceleration = authoring.Acceleration,
                    CellStaticVolume = authoring.CellStaticVolume,
                    CellCircleArea = authoring.CellCircleArea,
                    CellRadius = authoring.CellRadius,
                    CellHeight = authoring.CellHeight,
                    StiffnessConstant = authoring.StiffnessConstant
                });
            }
        }
    }


    #endregion
}
*/