// i fucking hate dots

using Mesocyclone.Data;
using System;
using System.Diagnostics;
using System.Linq;
using Unity.Burst;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Entities;
using Unity.Jobs;
using Unity.Mathematics;
using Unity.Physics;
using Unity.Transforms;
using Unity.VisualScripting;
using UnityEngine.Jobs;

// systems for the behaviour of air cell entities

namespace Mesocyclone.MesoDOTS
{
    [BurstCompile]
    [RequireMatchingQueriesForUpdate]
    [UpdateInGroup(typeof(FixedStepSimulationSystemGroup))] // make it every fixed time step
    public partial struct AirCellManager : ISystem
    {
        private ComponentLookup<LocalTransform> _transformLookup;
        private ComponentLookup<AirCell> _airCellLookup;
        private ComponentLookup<AirCellGeometry> _geoLookup;
        private ComponentLookup<AirCellNeedsInitialization> _initLookup;

        private EntityArchetype AirCellArchetype;

        public NativeArray<LocalTransform> AirTransformList;
        public NativeArray<AirCell> AirCellList;
        public NativeArray<AirCellGeometry> AirGeometryList;
        public NativeArray<Entity> AirCellEntities;

        public NativeList<int> InterpolationIndices;
        public bool GotLists;
        public bool GotAllAirCells;

        private PhysicsWorldSingleton physicsWorld;
        public InverseDistanceWeighting interpolation;
        private RefRO<AirCellSimulation> sim;
        private AirCellLocalEnvironment env;
        private RefRO<AirCellBehaviourFlags> flags;
        private RefRO<AirCellBounds> bounds;
        private AirCellGroup group;
        private AirCellOptimization som;
        private bool GotSingletons;

        public void OnCreate(ref SystemState state)
        {
            UnityEngine.Debug.Log("Air cell behavior creation");

            GotLists = false;
            GotAllAirCells = false;
            GotSingletons = false;

            //Setup Interpolation
            interpolation = new(true);
            InterpolationIndices = new(Allocator.Persistent);

            //Set Air Cell Archetype
            AirCellArchetype = state.EntityManager.CreateArchetype(typeof(LocalTransform), typeof(AirCell),
                typeof(AirCellGeometry), typeof(AirCellNeedsInitialization));

            // system only starts updating if there's an entity with this component
            //state.RequireForUpdate<AirCell>();
            state.RequireForUpdate<AirCellGroup>();
            state.RequireForUpdate<AirCellOptimization>();
            state.RequireForUpdate<PhysicsWorldSingleton>();
        }

        public void OnDestroy(ref SystemState state)
        {
            AirTransformList.Dispose();
            AirCellList.Dispose();
            AirGeometryList.Dispose();
            AirCellEntities.Dispose();
            InterpolationIndices.Dispose();
        }

        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            #region Get Data Components and Singletons

            foreach (var item in SystemAPI.Query<RefRO<AirCellSimulation>>())
            { sim = item; }
            foreach (var item in SystemAPI.Query<RefRW<AirCellLocalEnvironment>>())
            { env = item.ValueRW; }
            foreach (var item in SystemAPI.Query<RefRO<AirCellBehaviourFlags>>())
            { flags = item; }
            foreach (var item in SystemAPI.Query<RefRO<AirCellBounds>>())
            { bounds = item; }

            if (!GotSingletons)
            {
                group = SystemAPI.GetSingleton<AirCellGroup>();
                som = SystemAPI.GetSingleton<AirCellOptimization>();
                //buffer = SystemAPI.GetSingleton<AirCellBuffer>();
                GotSingletons = true;
            }
            physicsWorld = SystemAPI.GetSingleton<PhysicsWorldSingleton>();

            #endregion

            #region Get Lists and Air Cell Entities

            if (!GotLists)
            {
                AirTransformList = new(group.CellGroupNumber, Allocator.Persistent);
                AirCellList = new(group.CellGroupNumber, Allocator.Persistent);
                AirGeometryList = new(group.CellGroupNumber, Allocator.Persistent);

                //UnityEngine.Debug.Log("Lists have been generated");
                GotLists = true;

                AirCellEntities = state.EntityManager.CreateEntity(AirCellArchetype, group.CellGroupNumber, Allocator.Persistent);
                UnityEngine.Debug.Log("The air cell entities have been created");
            }

            if (!GotAllAirCells)
            {
                _initLookup = state.GetComponentLookup<AirCellNeedsInitialization>();

                for (int i = 0; i < AirCellEntities.Length; i++)
                {
                    UnityEngine.Debug.Log("iteration");
                    if (_initLookup.TryGetComponent(AirCellEntities[i], out _))
                    {
                        UnityEngine.Debug.Log("Cell needs initialization");
                        GotAllAirCells = false;
                        return;
                    }
                    else
                    {
                        _transformLookup = state.GetComponentLookup<LocalTransform>();
                        _airCellLookup = state.GetComponentLookup<AirCell>();
                        _geoLookup = state.GetComponentLookup<AirCellGeometry>();
                        if (_transformLookup.TryGetComponent(AirCellEntities[i], out LocalTransform t) && _airCellLookup.TryGetComponent(AirCellEntities[i], out AirCell c) && _geoLookup.TryGetComponent(AirCellEntities[i], out AirCellGeometry g))
                        {
                            AirTransformList[i] = t;
                            AirCellList[i] = c;
                            AirGeometryList[i] = g;
                        }
                        else
                        {
                            throw new Joar();
                        }
                        UnityEngine.Debug.Log("Air cell added to list");

                        GotAllAirCells = true;
                    }
                }
            }

            #endregion

            if (AirCellList.Length != group.CellGroupNumber)
            {
                UnityEngine.Debug.Log("Air cell list is not as long as it should be");
            }
            else
            {
                float dt = SystemAPI.Time.DeltaTime * sim.ValueRO.TimeScale;

                env = new() { AverageLocalTemp = 0, AverageLocalWind = float3.zero, AmbientHeat = 0 };
                som.CellRepulsion.Clear();
                SetData();

                #region Schedule and Complete Jobs

                AirCellValuesSetupJob SetupJob = new()
                {
                    FixedDeltaTime = dt,
                    group = group,
                    env = env,
                    som = som,
                    cells = AirCellList,
                    geos = AirGeometryList
                };
                state.Dependency = SetupJob.Schedule(group.CellGroupNumber, 5, state.Dependency);

                state.Dependency.Complete();
                SetData();

                AirCellPhysics1Job Physics1Job = new()
                {
                    FixedDeltaTime = dt,
                    sim = sim,
                    group = group,
                    env = env,
                    som = som,
                    flags = flags,
                    bounds = bounds,
                    cells = AirCellList,
                    geos = AirGeometryList
                };
                state.Dependency = Physics1Job.Schedule(group.CellGroupNumber, 5, state.Dependency);

                state.Dependency.Complete();
                SetData();

                AirCellTerrainRepulsionJob TerrainRepulsionJob = new()
                {
                    FixedDeltaTime = dt,
                    group = group,
                    env = env,
                    som = som,
                    flags = flags,
                    cells = AirCellList,
                    geos = AirGeometryList,
                    physicsWorld = physicsWorld,
                };
                state.Dependency = TerrainRepulsionJob.Schedule(state.Dependency);

                state.Dependency.Complete();
                SetData();

                AirCellRepulsionPhysicsJob PhysicsRepulsionJob = new()
                {
                    FixedDeltaTime = dt,
                    somCellRepulsion = som.CellRepulsion,
                    somStaticPressure = som.StaticPressure,
                    somDynVolume = som.DynVolume,
                    cells = AirCellList,
                    geos = AirGeometryList
                };
                state.Dependency = PhysicsRepulsionJob.Schedule(som.CellRepulsion.Length, 15, state.Dependency);

                state.Dependency.Complete();
                SetData();

                AirCellPhysics2Job Physics2Job = new()
                {
                    FixedDeltaTime = dt,
                    som = som,
                    flags = flags,
                    interp = interpolation,
                    interpIndices = InterpolationIndices,
                    transforms = AirTransformList,
                    cells = AirCellList,
                    geos = AirGeometryList
                };
                state.Dependency = Physics2Job.Schedule(group.CellGroupNumber, 5, state.Dependency);

                state.Dependency.Complete();
                SetData();

                AirCellInterpolationJob InterpolationJob = new()
                {
                    FixedDeltaTime = dt,
                    sim = sim,
                    group = group,
                    env = env,
                    flags = flags,
                    interp = interpolation,
                    interpIndices = InterpolationIndices,
                    cells = AirCellList,
                    geos = AirGeometryList
                };
                state.Dependency = InterpolationJob.Schedule(state.Dependency);

                state.Dependency.Complete();
                SetData();

                #endregion
            }
        }


        #region Physics Functions

        [BurstCompile]
        public static void PerformVelocity(ref AirCell cell, float deltaTime)
        {
            cell.CellCenter += cell.Velocity * deltaTime;
        }
        [BurstCompile]
        public static void PerformAcceleration(ref AirCell cell, in float3 acc, float deltaTime)
        {
            cell.Acceleration = acc;
            cell.Velocity += cell.Acceleration * deltaTime;
        }
        [BurstCompile]
        public static void AccelerationAlongVelocity(ref AirCell cell, float deltaTime)
        {
            if (math.lengthsq(cell.Velocity) > 1E-10f)
            {
                cell.Acceleration = math.normalize(cell.Velocity);
                cell.Velocity += cell.Acceleration * deltaTime;
            }
        }

        #endregion

        #region Volume Functions

        // no geo?  we poor af frfr :broken_heart:
        [BurstCompile]
        public static void SetSizeV(ref AirCellGeometry geo, float v)
        {
            geo.CellStaticVolume = v;
            geo.CellHeight = math.pow(v, 1f / 3f);
            geo.CellCircleArea = v / geo.CellHeight;
            geo.CellRadius = math.sqrt(geo.CellCircleArea / math.PI);
        }

        [BurstCompile]
        public static void SetSizeVL(ref AirCellGeometry geo, float v, float l)
        {
            geo.CellStaticVolume = v;
            geo.CellHeight = l;
            geo.CellCircleArea = v / l;
            geo.CellRadius = math.sqrt(geo.CellCircleArea / math.PI);
        }

        [BurstCompile]
        public static void SetSizeRL(ref AirCellGeometry geo, float r, float l)
        {
            geo.CellRadius = r;
            geo.CellHeight = l;
            geo.CellCircleArea = math.pow(r, 2) * math.PI;
            geo.CellStaticVolume = geo.CellCircleArea * l;
        }

        #endregion

        #region Other Functions
        [BurstCompile]
        public void SetData()
        {
            SystemAPI.GetSingletonRW<AirCellLocalEnvironment>().ValueRW = env;
            SystemAPI.GetSingletonRW<AirCellOptimization>().ValueRW = som;
        }
        #endregion
    }

    [BurstCompile]
    public partial struct AirCellValuesSetupJob : IJobParallelFor
    {
        public float FixedDeltaTime;

        public AirCellGroup group;
        public AirCellLocalEnvironment env;
        public AirCellOptimization som;

        public NativeArray<AirCell> cells;
        public NativeArray<AirCellGeometry> geos;

        [BurstCompile]
        public void Execute(int index)
        {
            // only lads with true ball know this is not the original
            //double mem; // ...he glazes afar into the distance, as he realizes he is amongst the only double left...
            float mem; // nevermind

            AirCell cell = cells[index];
            AirCellGeometry geo = geos[index];

            #region Values Setup

            #region Average Local Values

            env.AverageLocalTemp += cell.Temperature / group.CellGroupNumber;
            env.AverageLocalWind += cell.Velocity / group.CellGroupNumber;

            #endregion

            UnityEngine.Debug.Log("Test");
            DebugEverything(cell.ID, in cells, in geos);

            #region Calculate Static Pressure
            som.StaticPressure[cell.ID] = GlobalCalc.StaticPressureAtHeight(cell.CellCenter.y);
            #endregion

            DebugEverything(cell.ID, in cells, in geos);

            #region Insolation
            cell.Temperature = som.Temp[cell.ID];
            cell.Temperature += mem = GlobalData.Data.Gale.Insolation * geo.CellCircleArea / (GlobalData.Data.AtmHeatCp * GlobalData.Data.Gale.AtmMM * cell.Moles) * FixedDeltaTime;
            #endregion

            DebugEverything(cell.ID, in cells, in geos);

            #region Radiative Cooling
            mem = -GlobalData.Const.StefBoltz * GlobalData.Data.AtmSpecificEmissivity * ((2f * geo.CellCircleArea) + (2f * math.PI * geo.CellRadius * geo.CellHeight)) * math.pow(cell.Temperature, 4f) * FixedDeltaTime;
            //cell.Temperature += mem;
            #endregion

            DebugEverything(cell.ID, in cells, in geos);

            #region Calculate Static Volume

            AirCellManager.SetSizeV(ref geo, cell.Moles * GlobalData.Const.R * cell.Temperature / som.StaticPressure[cell.ID]);
            if (som.PrevStatVolume[cell.ID] == 0)
            {
                som.PrevStatVolume[cell.ID] = geo.CellStaticVolume;
            }
            som.DynVolume[cell.ID] = geo.CellStaticVolume;

            #endregion

            DebugEverything(cell.ID, in cells, in geos);

            #region Static Adiabatic Temperature Changes

            cell.Temperature *= math.pow(som.PrevStatVolume[cell.ID] / geo.CellStaticVolume, GlobalData.Const.R / GlobalData.Data.MolarHeatCapacity);
            som.PrevStatVolume[cell.ID] = geo.CellStaticVolume;
            /*
            if (math.abs(som.Temp[cell.ID] - cell.Temperature) > 10)
            {
                UnityEngine.Debug.Log("Heavy Abiatic Temperature Change [" + cell.ID + "] ; SOM = " + som.Temp[cell.ID] + " ; Temp = " + cell.Temperature);
            }*/

            som.Temp[cell.ID] = cell.Temperature;

            #endregion

            DebugEverything(cell.ID, in cells, in geos);

            #endregion
        }

        #region Debug

        [BurstCompile]
        [Conditional("DEV")]
        public void DebugEverything
        (
            int i,
            in NativeArray<AirCell> cells,
            in NativeArray<AirCellGeometry> geos
        )
        {
            AirCell c = cells[i];

            float3 vel = c.Velocity;

            if (!float.IsFinite(c.CellCenter.x) || !float.IsFinite(c.CellCenter.y) || !float.IsFinite(c.CellCenter.z))
                UnityEngine.Debug.LogError($"NaN Position\ni = {i}");

            if (!float.IsFinite(vel.x) || !float.IsFinite(vel.y) || !float.IsFinite(vel.z))
                UnityEngine.Debug.LogError($"Nan Cell Velocity\ni = {i}");

            if (!float.IsFinite(c.Temperature))
                UnityEngine.Debug.LogError($"NaN Cell Temperature\ni = {i}");

            if (!float.IsFinite(geos[i].CellStaticVolume))
                UnityEngine.Debug.LogError($"NaN Cell Volume\ni = {i}");

            if (som.PrevStatVolume[i] <= 0 && c.CellCenter.y < geos[i].CellHeight / 2f)
                UnityEngine.Debug.LogError($"Negative/Null PrevStatVolume\ni = {i}");

            if (c.CellCenter.y <= -geos[i].CellHeight / 2f)
                UnityEngine.Debug.LogError($"ACDDC - Air Cell Digging Down to China\ni = {i}");
        }

        [BurstCompile]
        private float SafeValue(float value)
        {
            return math.max(value, 1e-2f);
        }

        #endregion
    }

    [BurstCompile]
    public partial struct AirCellPhysics1Job : IJobParallelFor
    {
        public float FixedDeltaTime;

        [NativeDisableUnsafePtrRestriction]
        public RefRO<AirCellSimulation> sim;
        public AirCellGroup group;
        public AirCellLocalEnvironment env;
        public AirCellOptimization som;
        [NativeDisableUnsafePtrRestriction]
        public RefRO<AirCellBehaviourFlags> flags;
        [NativeDisableUnsafePtrRestriction]
        public RefRO<AirCellBounds> bounds;

        public NativeArray<AirCell> cells;
        public NativeArray<AirCellGeometry> geos;

        [BurstCompile]
        public void Execute(int index)
        {
            AirCell cell = cells[index];
            AirCellGeometry geo = geos[index];

            #region Air Cell Physics

            DebugEverything(cell.ID, in cells, in geos);

            #region Air Cell Terrain Repulsion
            if (flags.ValueRO.TerrainAtSeaLevel && cell.CellCenter.y < geo.CellHeight / 2f)
            {
                som.DynVolume[cell.ID] *= 0.5f + (cell.CellCenter.y / geo.CellHeight);
                AirCellManager.PerformAcceleration(ref cell, new float3(0, som.StaticPressure[cell.ID] * geo.CellCircleArea * (math.pow(geo.CellStaticVolume / som.DynVolume[cell.ID], 1f + (GlobalData.Data.MolarHeatCapacity / GlobalData.Const.R)) - 1f) / (cell.Moles * GlobalData.Data.Gale.AtmMM), 0), FixedDeltaTime);
            }
            #endregion

            DebugEverything(cell.ID, in cells, in geos);

            #region Perform Gravity and Buoyancy
            AirCellManager.PerformAcceleration(ref cell, new float3(0, GlobalData.Data.Gale.SurfGravity * ((cell.Temperature / env.AverageLocalTemp) - 1f), 0), FixedDeltaTime);
            #endregion

            DebugEverything(cell.ID, in cells, in geos);

            #region Perform Air Cell Drag
            AirCellManager.PerformAcceleration(ref cell, new float3(sim.ValueRO.CdTest * math.pow(cell.Velocity.x - env.AverageLocalWind.x, 2f) / (4f * geo.CellRadius),
                sim.ValueRO.CdTest * math.pow(cell.Velocity.y - env.AverageLocalWind.y, 2f) / (2f * geo.CellHeight),
                sim.ValueRO.CdTest * math.pow(cell.Velocity.z - env.AverageLocalWind.z, 2f) / (4f * geo.CellRadius)), FixedDeltaTime);
            #endregion

            DebugEverything(cell.ID, in cells, in geos);

            #region Check for Terrain Collision - to do: improve with bouncing
            if (cell.CellCenter.y <= (-geo.CellHeight / 2f))
            {
                cell.CellCenter = new float3(cell.CellCenter.x, (-geo.CellHeight / 2f) + 0.2f, cell.CellCenter.z);
            }
            #endregion

            DebugEverything(cell.ID, in cells, in geos);

            #region Keep Within Boundaries - note: for testing purposes

            if (math.abs(cell.CellCenter.x) >= (bounds.ValueRO.Value.x / 2) + 0.1f)
            {
                cell.Velocity += new float3(-math.sign(cell.CellCenter.x) * 50f * FixedDeltaTime, 0f, 0f);
            }

            if (math.abs(cell.CellCenter.z) >= (bounds.ValueRO.Value.x / 2))
            {
                cell.Velocity += new float3(0, 0, -math.sign(cell.CellCenter.z) * 50f * FixedDeltaTime);
            }

            if (cell.CellCenter.y >= bounds.ValueRO.Value.y + 0.1)
            {
                cell.Velocity += new float3(0, -math.sign(cell.CellCenter.y) * 50 * FixedDeltaTime, 0);
            }

            #endregion

            DebugEverything(cell.ID, in cells, in geos);

            #endregion

            #region Calculate Inter-Cell Repulsion Forces

            float d, r1, r2, d1, d2, A, h;

            for (int i2 = cell.ID + 1; i2 < group.CellGroupNumber; i2++)
            {
                AirCell cell2 = cells[i2];
                AirCellGeometry geo2 = geos[i2];

                #region Check For and Calculate Overlaps
                d = SafeValue(math.sqrt(math.square(cell.CellCenter.x - cell2.CellCenter.x) +
                    math.square(cell.CellCenter.z - cell2.CellCenter.z)));

                if ((h = math.abs(cell.CellCenter.y - cell2.CellCenter.y)) < (geo.CellHeight + geo2.CellHeight) / 2f &&
                    d < (geo.CellRadius + geo2.CellRadius))
                {
                    //Cell Overlap Calculations
                    r1 = math.max(geo.CellRadius, geo2.CellRadius);
                    r2 = math.min(geo.CellRadius, geo2.CellRadius);

                    h = math.abs(h - ((geo.CellHeight + geo2.CellHeight) / 2f));

                    d1 = (math.square(r1) - math.square(r2) + math.square(d)) / (2 * d);
                    d2 = d - d1;

                    A = (math.square(r1) * math.acos(d1 / r1)) - (d1 * math.sqrt(math.square(r1) - math.square(d1))) +
                        (math.square(r2) * math.acos(d2 / r2)) - (d2 * math.sqrt(math.square(r2) - math.square(d2)));

                    if (h * A > 0 && som.DynVolume[cell.ID] > 0 && som.DynVolume[i2] > 0)
                    {
                        som.DynVolume[cell.ID] = SafeValue(som.DynVolume[cell.ID] - (A * h / 2f));
                        som.DynVolume[i2] = SafeValue(som.DynVolume[i2] - (A * h / 2f));

                        som.CellRepulsion.Add(new float3(cell.ID, i2, A * h));
                    }
                    else
                    {
                        som.DynVolume[cell.ID] = SafeValue(som.DynVolume[cell.ID]);
                        som.DynVolume[i2] = SafeValue(som.DynVolume[i2]);
                    }

                    DebugEverything(cell.ID, in cells, in geos);
                    DebugEverything(i2, in cells, in geos);
                }
                #endregion
            }

            #endregion
        }

        #region Debug

        [BurstCompile]
        [Conditional("DEV")]
        public void DebugEverything
        (
            int i,
            in NativeArray<AirCell> cells,
            in NativeArray<AirCellGeometry> geos
        )
        {
            AirCell c = cells[i];

            float3 vel = c.Velocity;

            if (!float.IsFinite(c.CellCenter.x) || !float.IsFinite(c.CellCenter.y) || !float.IsFinite(c.CellCenter.z))
                UnityEngine.Debug.LogError($"NaN Position\ni = {i}");

            if (!float.IsFinite(vel.x) || !float.IsFinite(vel.y) || !float.IsFinite(vel.z))
                UnityEngine.Debug.LogError($"Nan Cell Velocity\ni = {i}");

            if (!float.IsFinite(c.Temperature))
                UnityEngine.Debug.LogError($"NaN Cell Temperature\ni = {i}");

            if (!float.IsFinite(geos[i].CellStaticVolume))
                UnityEngine.Debug.LogError($"NaN Cell Volume\ni = {i}");

            if (som.PrevStatVolume[i] <= 0 && c.CellCenter.y < geos[i].CellHeight / 2f)
                UnityEngine.Debug.LogError($"Negative/Null PrevStatVolume\ni = {i}");

            if (c.CellCenter.y <= -geos[i].CellHeight / 2f)
                UnityEngine.Debug.LogError($"ACDDC - Air Cell Digging Down to China\ni = {i}");
        }

        [BurstCompile]
        private float SafeValue(float value)
        {
            return math.max(value, 1e-2f);
        }

        #endregion
    }

    [BurstCompile]
    public partial struct AirCellTerrainRepulsionJob : IJob
    {
        public float FixedDeltaTime;

        public AirCellGroup group;
        public AirCellLocalEnvironment env;
        public AirCellOptimization som;
        [NativeDisableUnsafePtrRestriction]
        public RefRO<AirCellBehaviourFlags> flags;

        public NativeArray<AirCell> cells;
        public NativeArray<AirCellGeometry> geos;

        public PhysicsWorldSingleton physicsWorld;

        [BurstCompile]
        public void Execute()
        {
            for (int index = 0; index < group.CellGroupNumber; index++)
            {
                AirCell cell = cells[index];
                AirCellGeometry geo = geos[index];

                DebugEverything(cell.ID, in cells, in geos);

                #region Cell-Terrain Repulsion

                if (!flags.ValueRO.TerrainAtSeaLevel)
                {
                    float maxD = math.sqrt(math.square(geo.CellRadius) + math.square(geo.CellHeight / 2f));

                    RaycastInput ray = new()
                    {
                        Start = cell.CellCenter + new float3(0, geo.CellHeight / 2f, 0),
                        End = cell.CellCenter + new float3(0, (geo.CellHeight / 2f) - maxD, 0),
                        Filter = new() { CollidesWith = 1 << 3 }
                    };
                    if (physicsWorld.CastRay(ray, out Unity.Physics.RaycastHit hit))
                    {
                        float d3 = math.abs(hit.Position.y - cell.CellCenter.y);
                        if (d3 < geo.CellHeight / 2f)
                        {
                            som.DynVolume[cell.ID] *= 0.5f + (d3 / geo.CellHeight);
                            AirCellManager.PerformAcceleration(ref cell, new float3(0, som.StaticPressure[cell.ID] * geo.CellCircleArea * (math.pow(geo.CellStaticVolume / som.DynVolume[cell.ID], 1f + (GlobalData.Data.MolarHeatCapacity / GlobalData.Const.R)) - 1f) / (cell.Moles * GlobalData.Data.Gale.AtmMM), 0), FixedDeltaTime);
                        }
                    }
                }

                #endregion

                DebugEverything(cell.ID, in cells, in geos);
            }
        }

        #region Debug

        [BurstCompile]
        [Conditional("DEV")]
        public void DebugEverything
        (
            int i,
            in NativeArray<AirCell> cells,
            in NativeArray<AirCellGeometry> geos
        )
        {
            AirCell c = cells[i];

            float3 vel = c.Velocity;

            if (!float.IsFinite(c.CellCenter.x) || !float.IsFinite(c.CellCenter.y) || !float.IsFinite(c.CellCenter.z))
                UnityEngine.Debug.LogError($"NaN Position\ni = {i}");

            if (!float.IsFinite(vel.x) || !float.IsFinite(vel.y) || !float.IsFinite(vel.z))
                UnityEngine.Debug.LogError($"Nan Cell Velocity\ni = {i}");

            if (!float.IsFinite(c.Temperature))
                UnityEngine.Debug.LogError($"NaN Cell Temperature\ni = {i}");

            if (!float.IsFinite(geos[i].CellStaticVolume))
                UnityEngine.Debug.LogError($"NaN Cell Volume\ni = {i}");

            if (som.PrevStatVolume[i] <= 0 && c.CellCenter.y < geos[i].CellHeight / 2f)
                UnityEngine.Debug.LogError($"Negative/Null PrevStatVolume\ni = {i}");

            if (c.CellCenter.y <= -geos[i].CellHeight / 2f)
                UnityEngine.Debug.LogError($"ACDDC - Air Cell Digging Down to China\ni = {i}");
        }

        #endregion
    }

    [BurstCompile]
    public partial struct AirCellRepulsionPhysicsJob : IJobParallelFor
    {
        public float FixedDeltaTime;

        [ReadOnly]
        public NativeList<float3> somCellRepulsion;
        [ReadOnly]
        public NativeArray<float> somStaticPressure;
        [ReadOnly]
        public NativeArray<float> somDynVolume;

        public NativeArray<AirCell> cells;
        public NativeArray<AirCellGeometry> geos;

        [BurstCompile]
        public void Execute(int index)
        {
            #region Repulsion Physics

            float3 Repulsion = somCellRepulsion[index];

            int i1 = (int)Repulsion.x;
            int i2 = (int)Repulsion.y;

            AirCell repulCell = cells[i1];
            AirCell repul2Cell = cells[i2];
            AirCellGeometry repulGeo = geos[i1];
            AirCellGeometry repul2Geo = geos[i2];

            float mag = somStaticPressure[i1] * math.pow(Repulsion.z, 2f / 3f) * (math.pow(repulGeo.CellStaticVolume / somDynVolume[i1],
                    1f + (GlobalData.Data.MolarHeatCapacity / GlobalData.Const.R)) - 1f) / (repulCell.Moles * GlobalData.Data.Gale.AtmMM);

            AirCellManager.PerformAcceleration(ref repulCell, mag * math.normalize(repulCell.CellCenter - repul2Cell.CellCenter), FixedDeltaTime);

            mag = somStaticPressure[i2] * math.pow(Repulsion.z, 2f / 3f) * (math.pow(repul2Geo.CellStaticVolume / somDynVolume[i2],
                1f + (GlobalData.Data.MolarHeatCapacity / GlobalData.Const.R)) - 1f) / (repul2Cell.Moles * GlobalData.Data.Gale.AtmMM);

            AirCellManager.PerformAcceleration(ref repul2Cell, mag * math.normalize(repul2Cell.CellCenter - repulCell.CellCenter), FixedDeltaTime);

            #endregion
        }
    }

    [BurstCompile]
    public partial struct AirCellPhysics2Job : IJobParallelFor
    {
        public float FixedDeltaTime;

        public AirCellOptimization som;
        [NativeDisableUnsafePtrRestriction]
        public RefRO<AirCellBehaviourFlags> flags;
        public InverseDistanceWeighting interp;
        public NativeList<int> interpIndices;

        public NativeArray<LocalTransform> transforms;
        public NativeArray<AirCell> cells;
        public NativeArray<AirCellGeometry> geos;

        [BurstCompile]
        public void Execute(int index)
        {
            LocalTransform transform = transforms[index];
            AirCell cell = cells[index];

            #region Perform Physics

            DebugEverything(cell.ID, in cells, in geos);

            #region Dynamic Abiatic Temperature Change

            som.DynVolume[cell.ID] = SafeValue(som.DynVolume[cell.ID]);
            if (som.PrevDynVolume[cell.ID] == 0) som.PrevDynVolume[cell.ID] = som.DynVolume[cell.ID];
            cell.Temperature *= math.pow(som.PrevDynVolume[cell.ID] / som.DynVolume[cell.ID], GlobalData.Const.R / GlobalData.Data.MolarHeatCapacity);
            som.PrevDynVolume = som.DynVolume;

            #endregion

            AirCellManager.PerformVelocity(ref cell, FixedDeltaTime);

            DebugEverything(cell.ID, in cells, in geos);

            //To visualize the Air Cells
            if (flags.ValueRO.AirCellObjects) transform.Position = cell.CellCenter;

            //For interpolation
            if (interpIndices.Contains(cell.ID))
            {
                if (math.length(interp.Query - cell.CellCenter) > interp.R)
                {
                    int IndexToRemove = interpIndices.BinarySearch(cell.ID);
                    interpIndices.RemoveAt(IndexToRemove);
                }
            }
            else if (math.length(interp.Query - cell.CellCenter) <= interp.R)
                interpIndices.Add(cell.ID);

            /*
            if (i == 0)
            {
                UnityEngine.Debug.Log("");
                UnityEngine.Debug.Log("Velocity: " + cell.Velocity);
                UnityEngine.Debug.Log("Acceleration: " + cell.Acceleration);
            }*/

            DebugEverything(cell.ID, in cells, in geos);

            #endregion
        }

        #region Debug

        [BurstCompile]
        [Conditional("DEV")]
        public void DebugEverything
        (
            int i,
            in NativeArray<AirCell> cells,
            in NativeArray<AirCellGeometry> geos
        )
        {
            AirCell c = cells[i];

            float3 vel = c.Velocity;

            if (!float.IsFinite(c.CellCenter.x) || !float.IsFinite(c.CellCenter.y) || !float.IsFinite(c.CellCenter.z))
                UnityEngine.Debug.LogError($"NaN Position\ni = {i}");

            if (!float.IsFinite(vel.x) || !float.IsFinite(vel.y) || !float.IsFinite(vel.z))
                UnityEngine.Debug.LogError($"Nan Cell Velocity\ni = {i}");

            if (!float.IsFinite(c.Temperature))
                UnityEngine.Debug.LogError($"NaN Cell Temperature\ni = {i}");

            if (!float.IsFinite(geos[i].CellStaticVolume))
                UnityEngine.Debug.LogError($"NaN Cell Volume\ni = {i}");

            if (som.PrevStatVolume[i] <= 0 && c.CellCenter.y < geos[i].CellHeight / 2f)
                UnityEngine.Debug.LogError($"Negative/Null PrevStatVolume\ni = {i}");

            if (c.CellCenter.y <= -geos[i].CellHeight / 2f)
                UnityEngine.Debug.LogError($"ACDDC - Air Cell Digging Down to China\ni = {i}");
        }

        [BurstCompile]
        private float SafeValue(float value)
        {
            return math.max(value, 1e-2f);
        }

        #endregion
    }

    [BurstCompile]
    public partial struct AirCellInterpolationJob : IJob
    {
        public float FixedDeltaTime;

        [NativeDisableUnsafePtrRestriction]
        public RefRO<AirCellSimulation> sim;
        public AirCellGroup group;
        public AirCellLocalEnvironment env;
        [NativeDisableUnsafePtrRestriction]
        public RefRO<AirCellBehaviourFlags> flags;
        public InverseDistanceWeighting interp;
        public NativeList<int> interpIndices;

        public NativeArray<AirCell> cells;
        public NativeArray<AirCellGeometry> geos;

        [BurstCompile]
        public void Execute()
        {
            #region Interpolation

            interp.BeginInterpolation(flags.ValueRO.FollowDrone);

            if (flags.ValueRO.InterpolationWithTerrain)
            {
                NativeArray<float> v = new(6, Allocator.Persistent, NativeArrayOptions.ClearMemory);
                v[0] = 0;
                v[1] = 0;
                v[2] = 0;
                v[3] = sim.ValueRO.MoleTest;
                v[4] = env.AverageLocalTemp;
                v[5] = 0; /* Dynamic Volume Should Supposedly Go Here But Still Haven't Found A Use For It (TM) */

                interp.InterpolationStep(flags.ValueRO.FollowDrone ? float3.zero : new float3(interp.Query.x, 0, interp.Query.z), v);
                v.Dispose();
            }

            foreach (int i in interpIndices)
            {
                AirCell interpCell = cells[i];

                NativeArray<float> v = new(6, Allocator.Persistent, NativeArrayOptions.ClearMemory);
                v[0] = interpCell.Velocity.x * sim.ValueRO.TimeScale;
                v[1] = interpCell.Velocity.y * sim.ValueRO.TimeScale;
                v[2] = interpCell.Velocity.z * sim.ValueRO.TimeScale;
                v[3] = interpCell.Moles;
                v[4] = interpCell.Temperature;
                v[5] = 0; /* Dynamic Volume Should Supposedly Go Here But Still Haven't Found A Use For It (TM) */

                interp.InterpolationStep(interpCell.CellCenter, v);
                v.Dispose();
            }

            if (!interp.BroadcastInterpolation(flags.ValueRO.InterpolationWithTerrain))
            {
                int minI = 0;
                float minD = math.INFINITY;

                for (int i = 0; i < group.CellGroupNumber; i++)
                {
                    AirCell interpCell = cells[i];

                    if (minD > math.length(interp.Query - interpCell.CellCenter))
                    {
                        minD = math.length(interp.Query - interpCell.CellCenter);
                        minI = i;
                    }
                }

                AirCell closestCell = cells[minI];

                NativeArray<float> v = new(6, Allocator.Persistent, NativeArrayOptions.ClearMemory);
                v[0] = closestCell.Velocity.x * sim.ValueRO.TimeScale;
                v[1] = closestCell.Velocity.y * sim.ValueRO.TimeScale;
                v[2] = closestCell.Velocity.z * sim.ValueRO.TimeScale;
                v[3] = closestCell.Moles;
                v[4] = closestCell.Temperature;
                v[5] = 0; /* Dynamic Volume Should Supposedly Go Here But Still Haven't Found A Use For It (TM) */

                interp.GetClosestCell(v);
                v.Dispose();
            }
            #endregion
        }
    }
}
