// i fucking hate dots

using System;
using System.Diagnostics;
using System.Linq;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Collections;
using Unity.Physics;
using Unity.Transforms;
using Unity.Burst;
using Unity.Jobs;
using UnityEngine.Jobs;
using Mesocyclone.Data;

// systems for the behaviour of air cell entities

namespace Mesocyclone.MesoDOTS
{
    [RequireMatchingQueriesForUpdate]
    [UpdateInGroup(typeof(FixedStepSimulationSystemGroup))] // make it every fixed time step
    public partial struct AirCellManager : ISystem
    {
        private ComponentLookup<AirCell> _airCellLookup;
        private ComponentLookup<AirCellGeometry> _geoLookup;
        private EntityQuery AirCellQuery;

        private PhysicsWorldSingleton physicsWorld;
        public InverseDistanceWeighting interpolation;
        private RefRO<AirCellSimulation> sim;
        private RefRW<AirCellLocalEnvironment> env;
        private RefRO<AirCellBehaviourFlags> flags;
        private RefRO<AirCellBounds> bounds;
        private AirCellGroup group;
        private RefRW<AirCellOptimization> som;
        private AirCellBuffer buffer;
        private bool GotSingletons;

        public void OnCreate(ref SystemState state)
        {
            GotSingletons = false;

            //Get Lookups
            _airCellLookup = state.GetComponentLookup<AirCell>();
            _geoLookup = state.GetComponentLookup<AirCellGeometry>();

            //Get Singletons
            interpolation = new(true);

            //Get Air Cell Query
            AirCellQuery = state.GetEntityQuery(ComponentType.ReadWrite<LocalTransform>(), ComponentType.ReadWrite<AirCell>(), ComponentType.ReadWrite<AirCellGeometry>());

            // system only starts updating if there's an entity with this component
            state.RequireForUpdate<AirCell>();
            state.RequireForUpdate<AirCellGroup>();
            state.RequireForUpdate<AirCellOptimization>();
            state.RequireForUpdate<AirCellBuffer>();
            state.RequireForUpdate<PhysicsWorldSingleton>();
        }

        public void OnUpdate(ref SystemState state)
        {
            foreach (var item in SystemAPI.Query<RefRO<AirCellSimulation>>())
            { sim = item; }
            foreach (var item in SystemAPI.Query<RefRW<AirCellLocalEnvironment>>())
            { env = item; }
            foreach (var item in SystemAPI.Query<RefRO<AirCellBehaviourFlags>>())
            { flags = item; }
            foreach (var item in SystemAPI.Query<RefRO<AirCellBounds>>())
            { bounds = item; }

            if (!GotSingletons)
            {
                group = SystemAPI.GetSingleton<AirCellGroup>();
                som = SystemAPI.GetSingletonRW<AirCellOptimization>();
                buffer = SystemAPI.GetSingleton<AirCellBuffer>();
                physicsWorld = SystemAPI.GetSingleton<PhysicsWorldSingleton>();
                GotSingletons = true;
            }

            float dt = SystemAPI.Time.DeltaTime * sim.ValueRO.TimeScale;

            _airCellLookup.Update(ref state);
            _geoLookup.Update(ref state);

            env.ValueRW = new() { AverageLocalTemp = 0, AverageLocalWind = float3.zero, AmbientHeat = 0 };

            #region Schedule and Complete Jobs

            state.Dependency = new AirCellValuesSetupJob
            {
                FixedDeltaTime = dt,
                group = group,
                env = env,
                som = som,
                AirCellLookup = _airCellLookup,
                GeoLookup = _geoLookup
            }.ScheduleParallel(AirCellQuery, state.Dependency);

            state.Dependency.Complete();

            state.Dependency = new AirCellPhysics1Job
            {
                FixedDeltaTime = dt,
                sim = sim,
                group = group,
                env = env,
                som = som,
                physicsWorld = physicsWorld,
                flags = flags,
                bounds = bounds,
                AirCellLookup = _airCellLookup,
                GeoLookup = _geoLookup
            }.ScheduleParallel(AirCellQuery, state.Dependency);

            state.Dependency.Complete();

            state.Dependency = new AirCellRepulsionPhysicsJob
            {
                FixedDeltaTime = dt,
                som = som,
                AirCellLookup = _airCellLookup,
                GeoLookup = _geoLookup,
                buffer = buffer.Buffer
            }.Schedule(som.ValueRO.CellRepulsion.Length, 15, state.Dependency);

            state.Dependency.Complete();

            state.Dependency = new AirCellPhysics2Job
            {
                FixedDeltaTime = dt,
                som = som,
                flags = flags,
                interp = interpolation,
                AirCellLookup = _airCellLookup,
                GeoLookup = _geoLookup
            }.ScheduleParallel(AirCellQuery, state.Dependency);

            state.Dependency.Complete();

            state.Dependency = new AirCellInterpolationJob
            {
                FixedDeltaTime = dt,
                sim = sim,
                group = group,
                env = env,
                flags = flags,
                interp = interpolation,
                AirCellLookup = _airCellLookup,
                GeoLookup = _geoLookup,
                buffer = buffer.Buffer
            }.Schedule(state.Dependency);

            state.Dependency.Complete();

            #endregion
        }


        #region Physics Functions

        [BurstCompile]
        public static void PerformVelocity(ref AirCell cell, float deltaTime)
        {
            cell.CellCenter += cell.Velocity * deltaTime;
        }
        [BurstCompile]
        public static void PerformAcceleration(ref AirCell cell, float3 acc, float deltaTime)
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
    }

    [BurstCompile]
    public partial struct AirCellValuesSetupJob : IJobEntity
    {
        public float FixedDeltaTime;
        public AirCellGroup group;
        public RefRW<AirCellLocalEnvironment> env;
        public RefRW<AirCellOptimization> som;

        public ComponentLookup<AirCell> AirCellLookup;
        public ComponentLookup<AirCellGeometry> GeoLookup;

        [BurstCompile]
        private void Execute
        (
            // ref is for Reading and Writing
            // in is for reading-only
            ref LocalTransform _transform,
            ref AirCell cell,
            ref AirCellGeometry geo,
            in DynamicBuffer<AirCellGroupMember> buffer
        )
        {
            // only lads with true ball know this is not the original
            //double mem; // ...he glazes afar into the distance, as he realizes he is amongst the only double left...
            float mem; // nevermind

            #region Values Setup

            #region Average Local Values

            env.ValueRW.AverageLocalTemp += cell.Temperature / group.CellGroupNumber;
            env.ValueRW.AverageLocalWind += cell.Velocity / group.CellGroupNumber;

            #endregion

            DebugEverything(cell.ID, in buffer, in AirCellLookup, in GeoLookup);

            #region Calculate Static Pressure
            som.ValueRW.StaticPressure[cell.ID] = GlobalCalc.StaticPressureAtHeight(cell.CellCenter.y);
            #endregion

            DebugEverything(cell.ID, in buffer, in AirCellLookup, in GeoLookup);

            #region Insolation
            cell.Temperature = som.ValueRO.Temp[cell.ID];
            cell.Temperature += mem = GlobalData.Data.Gale.Insolation * geo.CellCircleArea / (GlobalData.Data.AtmHeatCp * GlobalData.Data.Gale.AtmMM * cell.Moles) * FixedDeltaTime;
            #endregion

            DebugEverything(cell.ID, in buffer, in AirCellLookup, in GeoLookup);

            #region Radiative Cooling
            mem = -GlobalData.Const.StefBoltz * GlobalData.Data.AtmSpecificEmissivity * ((2f * geo.CellCircleArea) + (2f * math.PI * geo.CellRadius * geo.CellHeight)) * System.MathF.Pow(cell.Temperature, 4f) * FixedDeltaTime;
            //cell.Temperature += mem;
            #endregion

            DebugEverything(cell.ID, in buffer, in AirCellLookup, in GeoLookup);

            #region Calculate Static Volume

            AirCellManager.SetSizeV(ref geo, cell.Moles * GlobalData.Const.R * cell.Temperature / som.ValueRO.StaticPressure[cell.ID]);
            if (som.ValueRO.PrevStatVolume[cell.ID] == 0)
            {
                som.ValueRW.PrevStatVolume[cell.ID] = geo.CellStaticVolume;
            }
            som.ValueRW.DynVolume[cell.ID] = geo.CellStaticVolume;

            #endregion

            DebugEverything(cell.ID, in buffer, in AirCellLookup, in GeoLookup);

            #region Static Adiabatic Temperature Changes

            cell.Temperature *= math.pow(som.ValueRO.PrevStatVolume[cell.ID] / geo.CellStaticVolume, GlobalData.Const.R / GlobalData.Data.MolarHeatCapacity);
            som.ValueRW.PrevStatVolume[cell.ID] = geo.CellStaticVolume;
            /*
            if (math.abs(som.Temp[cell.ID] - cell.Temperature) > 10)
            {
                UnityEngine.Debug.Log("Heavy Abiatic Temperature Change [" + cell.ID + "] ; SOM = " + som.Temp[cell.ID] + " ; Temp = " + cell.Temperature);
            }*/

            som.ValueRW.Temp[cell.ID] = cell.Temperature;

            #endregion

            DebugEverything(cell.ID, in buffer, in AirCellLookup, in GeoLookup);

            #endregion
        }

        #region Debug

        [BurstCompile]
        [Conditional("DEV")]
        public void DebugEverything
        (
            int i,
            in DynamicBuffer<AirCellGroupMember> buffer,
            in ComponentLookup<AirCell> airCellLookup,
            in ComponentLookup<AirCellGeometry> geoLookup
        )
        {
            Entity member = buffer[i].Value;

            if
            (
                airCellLookup.TryGetComponent(member, out AirCell c)
                &&
                geoLookup.TryGetComponent(member, out AirCellGeometry geo)
            )
            {
                float3 vel = c.Velocity;

                if (!float.IsFinite(c.CellCenter.x) || !float.IsFinite(c.CellCenter.y) || !float.IsFinite(c.CellCenter.z))
                    UnityEngine.Debug.LogError($"NaN Position\ni = {i}");

                if (!float.IsFinite(vel.x) || !float.IsFinite(vel.y) || !float.IsFinite(vel.z))
                    UnityEngine.Debug.LogError($"Nan Cell Velocity\ni = {i}");

                if (!float.IsFinite(c.Temperature))
                    UnityEngine.Debug.LogError($"NaN Cell Temperature\ni = {i}");

                if (!float.IsFinite(geo.CellStaticVolume))
                    UnityEngine.Debug.LogError($"NaN Cell Volume\ni = {i}");

                if (som.ValueRO.PrevStatVolume[i] <= 0 && c.CellCenter.y < geo.CellHeight / 2f)
                    UnityEngine.Debug.LogError($"Negative/Null PrevStatVolume\ni = {i}");

                if (c.CellCenter.y <= -geo.CellHeight / 2f)
                    UnityEngine.Debug.LogError($"ACDDC - Air Cell Digging Down to China\ni = {i}");
            }
        }

        [BurstCompile]
        private float SafeValue(float value)
        {
            return math.max(value, 1e-2f);
        }

        #endregion
    }

    [BurstCompile]
    public partial struct AirCellPhysics1Job : IJobEntity
    {
        public float FixedDeltaTime;
        public RefRO<AirCellSimulation> sim;
        public AirCellGroup group;
        public RefRW<AirCellLocalEnvironment> env;
        public RefRW<AirCellOptimization> som;
        public PhysicsWorldSingleton physicsWorld;
        public RefRO<AirCellBehaviourFlags> flags;
        public RefRO<AirCellBounds> bounds;

        public ComponentLookup<AirCell> AirCellLookup;
        public ComponentLookup<AirCellGeometry> GeoLookup;

        [BurstCompile]
        private void Execute
        (
            // ref is for Reading and Writing
            // in is for reading-only
            ref LocalTransform _transform,
            ref AirCell cell,
            ref AirCellGeometry geo,
            in DynamicBuffer<AirCellGroupMember> buffer
        )
        {
            #region Air Cell Physics

            DebugEverything(cell.ID, in buffer, in AirCellLookup, in GeoLookup);

            #region Air Cell Terrain Repulsion
            if (flags.ValueRO.TerrainAtSeaLevel && cell.CellCenter.y < geo.CellHeight / 2f)
            {
                som.ValueRW.DynVolume[cell.ID] *= 0.5f + (cell.CellCenter.y / geo.CellHeight);
                AirCellManager.PerformAcceleration(ref cell, new float3(0, som.ValueRO.StaticPressure[cell.ID] * geo.CellCircleArea * (math.pow(geo.CellStaticVolume / som.ValueRO.DynVolume[cell.ID], 1f + (GlobalData.Data.MolarHeatCapacity / GlobalData.Const.R)) - 1f) / (cell.Moles * GlobalData.Data.Gale.AtmMM), 0), FixedDeltaTime);
            }
            #endregion

            DebugEverything(cell.ID, in buffer, in AirCellLookup, in GeoLookup);

            #region Perform Gravity and Buoyancy
            AirCellManager.PerformAcceleration(ref cell, new float3(0, GlobalData.Data.Gale.SurfGravity * ((cell.Temperature / env.ValueRO.AverageLocalTemp) - 1f), 0), FixedDeltaTime);
            #endregion

            DebugEverything(cell.ID, in buffer, in AirCellLookup, in GeoLookup);

            #region Perform Air Cell Drag
            AirCellManager.PerformAcceleration(ref cell, new float3(sim.ValueRO.CdTest * math.pow(cell.Velocity.x - env.ValueRO.AverageLocalWind.x, 2f) / (4f * geo.CellRadius),
                sim.ValueRO.CdTest * math.pow(cell.Velocity.y - env.ValueRO.AverageLocalWind.y, 2f) / (2f * geo.CellHeight),
                sim.ValueRO.CdTest * math.pow(cell.Velocity.z - env.ValueRO.AverageLocalWind.z, 2f) / (4f * geo.CellRadius)), FixedDeltaTime);
            #endregion

            DebugEverything(cell.ID, in buffer, in AirCellLookup, in GeoLookup);

            #region Check for Terrain Collision - to do: improve with bouncing
            if (cell.CellCenter.y <= (-geo.CellHeight / 2f))
            {
                cell.CellCenter = new float3(cell.CellCenter.x, (-geo.CellHeight / 2f) + 0.2f, cell.CellCenter.z);
            }
            #endregion

            DebugEverything(cell.ID, in buffer, in AirCellLookup, in GeoLookup);

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

            DebugEverything(cell.ID, in buffer, in AirCellLookup, in GeoLookup);

            #region Cell-Terrain Repulsion

            if (!flags.ValueRO.TerrainAtSeaLevel)
            {
                float maxD = math.sqrt(math.square(geo.CellRadius) + math.square(geo.CellHeight / 2f));

                RaycastInput ray = new();
                ray.Start = cell.CellCenter + new float3(0, geo.CellHeight / 2f, 0);
                ray.End = ray.Start - new float3(0, maxD, 0);
                ray.Filter = new();
                ray.Filter.CollidesWith = 1 << 3;
                if (physicsWorld.CastRay(ray, out Unity.Physics.RaycastHit hit))
                {
                    float d3 = math.abs(hit.Position.y - cell.CellCenter.y);
                    if (d3 < geo.CellHeight / 2f)
                    {
                        som.ValueRW.DynVolume[cell.ID] *= 0.5f + (d3 / geo.CellHeight);
                        AirCellManager.PerformAcceleration(ref cell, new float3(0, som.ValueRO.StaticPressure[cell.ID] * geo.CellCircleArea * (math.pow(geo.CellStaticVolume / som.ValueRO.DynVolume[cell.ID], (1f + (GlobalData.Data.MolarHeatCapacity / GlobalData.Const.R))) - 1f) / (cell.Moles * GlobalData.Data.Gale.AtmMM), 0), FixedDeltaTime);
                    }
                }
            }

            #endregion

            DebugEverything(cell.ID, in buffer, in AirCellLookup, in GeoLookup);

            #endregion

            #region Calculate Inter-Cell Repulsion Forces

            som.ValueRW.CellRepulsion.Clear();
            float d, r1, r2, d1, d2, A, h;

            for (int i2 = cell.ID + 1; i2 < group.CellGroupNumber; i2++)
            {
                Entity member2 = buffer[i2].Value;
                if (AirCellLookup.TryGetComponent(member2, out AirCell cell2) && GeoLookup.TryGetComponent(member2, out AirCellGeometry geo2))
                {
                    #region Check For and Calculate Overlaps
                    d = SafeValue(math.sqrt(math.square(cell.CellCenter.x - cell2.CellCenter.x) +
                        math.square(cell.CellCenter.z - cell2.CellCenter.z)));

                    if ((h = math.abs(cell.CellCenter.y - cell2.CellCenter.y)) < (geo.CellHeight + geo2.CellHeight) / 2f &&
                        d < (geo.CellRadius + geo2.CellRadius))
                    {
                        #region Cell Overlap Calculations

                        r1 = math.max(geo.CellRadius, geo2.CellRadius);
                        r2 = math.min(geo.CellRadius, geo2.CellRadius);

                        h = math.abs(h - ((geo.CellHeight + geo2.CellHeight) / 2f));

                        d1 = (math.square(r1) - math.square(r2) + math.square(d)) / (2 * d);
                        d2 = d - d1;

                        A = (math.square(r1) * math.acos(d1 / r1)) - (d1 * math.sqrt(math.square(r1) - math.square(d1))) +
                            (math.square(r2) * math.acos(d2 / r2)) - (d2 * math.sqrt(math.square(r2) - math.square(d2)));

                        #endregion

                        if (h * A > 0 && som.ValueRO.DynVolume[cell.ID] > 0 && som.ValueRO.DynVolume[i2] > 0)
                        {
                            som.ValueRW.DynVolume[cell.ID] = SafeValue(som.ValueRO.DynVolume[cell.ID] - (A * h / 2f));
                            som.ValueRW.DynVolume[i2] = SafeValue(som.ValueRO.DynVolume[i2] - (A * h / 2f));

                            som.ValueRW.CellRepulsion.Add(new float3(cell.ID, i2, A * h));
                        }
                        else
                        {
                            som.ValueRW.DynVolume[cell.ID] = SafeValue(som.ValueRO.DynVolume[cell.ID]);
                            som.ValueRW.DynVolume[i2] = SafeValue(som.ValueRO.DynVolume[i2]);
                        }

                        DebugEverything(cell.ID, in buffer, in AirCellLookup, in GeoLookup);
                        DebugEverything(i2, in buffer, in AirCellLookup, in GeoLookup);
                    }
                    #endregion
                }
            }

            #endregion
        }

        #region Debug

        [BurstCompile]
        [Conditional("DEV")]
        public void DebugEverything
        (
            int i,
            in DynamicBuffer<AirCellGroupMember> buffer,
            in ComponentLookup<AirCell> airCellLookup,
            in ComponentLookup<AirCellGeometry> geoLookup
        )
        {
            Entity member = buffer[i].Value;

            if
            (
                airCellLookup.TryGetComponent(member, out AirCell c)
                &&
                geoLookup.TryGetComponent(member, out AirCellGeometry geo)
            )
            {
                float3 vel = c.Velocity;

                if (!float.IsFinite(c.CellCenter.x) || !float.IsFinite(c.CellCenter.y) || !float.IsFinite(c.CellCenter.z))
                    UnityEngine.Debug.LogError($"NaN Position\ni = {i}");

                if (!float.IsFinite(vel.x) || !float.IsFinite(vel.y) || !float.IsFinite(vel.z))
                    UnityEngine.Debug.LogError($"Nan Cell Velocity\ni = {i}");

                if (!float.IsFinite(c.Temperature))
                    UnityEngine.Debug.LogError($"NaN Cell Temperature\ni = {i}");

                if (!float.IsFinite(geo.CellStaticVolume))
                    UnityEngine.Debug.LogError($"NaN Cell Volume\ni = {i}");

                if (som.ValueRO.PrevStatVolume[i] <= 0 && c.CellCenter.y < geo.CellHeight / 2f)
                    UnityEngine.Debug.LogError($"Negative/Null PrevStatVolume\ni = {i}");

                if (c.CellCenter.y <= -geo.CellHeight / 2f)
                    UnityEngine.Debug.LogError($"ACDDC - Air Cell Digging Down to China\ni = {i}");
            }
        }

        [BurstCompile]
        private float SafeValue(float value)
        {
            return math.max(value, 1e-2f);
        }

        #endregion
    }

    [BurstCompile]
    public partial struct AirCellRepulsionPhysicsJob : IJobParallelFor
    {
        public float FixedDeltaTime;
        public RefRW<AirCellOptimization> som;

        public ComponentLookup<AirCell> AirCellLookup;
        public ComponentLookup<AirCellGeometry> GeoLookup;
        public NativeList<AirCellGroupMember> buffer;

        [BurstCompile]
        public void Execute(int index)
        {
            #region Repulsion Physics

            float3 Repulsion = som.ValueRO.CellRepulsion[index];

            int i1 = (int)Repulsion.x;
            int i2 = (int)Repulsion.y;

            if (AirCellLookup.TryGetComponent(buffer[i1].Value, out AirCell repulCell) && AirCellLookup.TryGetComponent(buffer[i2].Value, out AirCell repul2Cell) && GeoLookup.TryGetComponent(buffer[i1].Value, out AirCellGeometry repulGeo) && GeoLookup.TryGetComponent(buffer[i2].Value, out AirCellGeometry repul2Geo))
            {
                float mag = som.ValueRO.StaticPressure[i1] * math.pow(Repulsion.z, 2f / 3f) * (math.pow(repulGeo.CellStaticVolume / som.ValueRO.DynVolume[i1],
                    1f + (GlobalData.Data.MolarHeatCapacity / GlobalData.Const.R)) - 1f) / (repulCell.Moles * GlobalData.Data.Gale.AtmMM);

                AirCellManager.PerformAcceleration(ref repulCell, mag * math.normalize(repulCell.CellCenter - repul2Cell.CellCenter), FixedDeltaTime);

                mag = som.ValueRO.StaticPressure[i2] * math.pow(Repulsion.z, 2f / 3f) * (math.pow(repul2Geo.CellStaticVolume / som.ValueRO.DynVolume[i2],
                    1f + (GlobalData.Data.MolarHeatCapacity / GlobalData.Const.R)) - 1f) / (repul2Cell.Moles * GlobalData.Data.Gale.AtmMM);

                AirCellManager.PerformAcceleration(ref repul2Cell, mag * math.normalize(repul2Cell.CellCenter - repulCell.CellCenter), FixedDeltaTime);
            }

            #endregion
        }

        #region Debug

        [BurstCompile]
        [Conditional("DEV")]
        public void DebugEverything
        (
            int i,
            in DynamicBuffer<AirCellGroupMember> buffer,
            in ComponentLookup<AirCell> airCellLookup,
            in ComponentLookup<AirCellGeometry> geoLookup
        )
        {
            Entity member = buffer[i].Value;

            if
            (
                airCellLookup.TryGetComponent(member, out AirCell c)
                &&
                geoLookup.TryGetComponent(member, out AirCellGeometry geo)
            )
            {
                float3 vel = c.Velocity;

                if (!float.IsFinite(c.CellCenter.x) || !float.IsFinite(c.CellCenter.y) || !float.IsFinite(c.CellCenter.z))
                    UnityEngine.Debug.LogError($"NaN Position\ni = {i}");

                if (!float.IsFinite(vel.x) || !float.IsFinite(vel.y) || !float.IsFinite(vel.z))
                    UnityEngine.Debug.LogError($"Nan Cell Velocity\ni = {i}");

                if (!float.IsFinite(c.Temperature))
                    UnityEngine.Debug.LogError($"NaN Cell Temperature\ni = {i}");

                if (!float.IsFinite(geo.CellStaticVolume))
                    UnityEngine.Debug.LogError($"NaN Cell Volume\ni = {i}");

                if (som.ValueRW.PrevStatVolume[i] <= 0 && c.CellCenter.y < geo.CellHeight / 2f)
                    UnityEngine.Debug.LogError($"Negative/Null PrevStatVolume\ni = {i}");

                if (c.CellCenter.y <= -geo.CellHeight / 2f)
                    UnityEngine.Debug.LogError($"ACDDC - Air Cell Digging Down to China\ni = {i}");
            }
        }

        [BurstCompile]
        private float SafeValue(float value)
        {
            return math.max(value, 1e-2f);
        }

        #endregion
    }

    [BurstCompile]
    public partial struct AirCellPhysics2Job : IJobEntity
    {
        public float FixedDeltaTime;
        public RefRW<AirCellOptimization> som;
        public RefRO<AirCellBehaviourFlags> flags;
        public InverseDistanceWeighting interp;

        public ComponentLookup<AirCell> AirCellLookup;
        public ComponentLookup<AirCellGeometry> GeoLookup;

        [BurstCompile]
        private void Execute
        (
            // ref is for Reading and Writing
            // in is for reading-only
            ref LocalTransform transform,
            ref AirCell cell,
            ref AirCellGeometry _geo,
            in DynamicBuffer<AirCellGroupMember> buffer
        )
        {
            #region Perform Physics

            DebugEverything(cell.ID, in buffer, in AirCellLookup, in GeoLookup);

            #region Dynamic Abiatic Temperature Change

            som.ValueRW.DynVolume[cell.ID] = SafeValue(som.ValueRO.DynVolume[cell.ID]);
            if (som.ValueRO.PrevDynVolume[cell.ID] == 0) som.ValueRW.PrevDynVolume[cell.ID] = som.ValueRO.DynVolume[cell.ID];
            cell.Temperature *= math.pow(som.ValueRO.PrevDynVolume[cell.ID] / som.ValueRO.DynVolume[cell.ID], GlobalData.Const.R / GlobalData.Data.MolarHeatCapacity);
            som.ValueRW.PrevDynVolume = som.ValueRO.DynVolume;

            #endregion

            AirCellManager.PerformVelocity(ref cell, FixedDeltaTime);

            DebugEverything(cell.ID, in buffer, in AirCellLookup, in GeoLookup);

            //To visualize the Air Cells
            if (flags.ValueRO.AirCellObjects) transform.Position = cell.CellCenter;

            //For interpolation
            if (interp.Indices.Contains(cell.ID))
            {
                if (math.length(interp.Query - cell.CellCenter) > interp.R)
                    interp.Remove(cell.ID);
            }
            else if (math.length(interp.Query - cell.CellCenter) <= interp.R)
                interp.Add(cell.ID);

            /*
            if (i == 0)
            {
                UnityEngine.Debug.Log("");
                UnityEngine.Debug.Log("Velocity: " + cell.Velocity);
                UnityEngine.Debug.Log("Acceleration: " + cell.Acceleration);
            }*/

            DebugEverything(cell.ID, in buffer, in AirCellLookup, in GeoLookup);

            #endregion
        }

        #region Debug

        [BurstCompile]
        [Conditional("DEV")]
        public void DebugEverything
        (
            int i,
            in DynamicBuffer<AirCellGroupMember> buffer,
            in ComponentLookup<AirCell> airCellLookup,
            in ComponentLookup<AirCellGeometry> geoLookup
        )
        {
            Entity member = buffer[i].Value;

            if
            (
                airCellLookup.TryGetComponent(member, out AirCell c)
                &&
                geoLookup.TryGetComponent(member, out AirCellGeometry geo)
            )
            {
                float3 vel = c.Velocity;

                if (!float.IsFinite(c.CellCenter.x) || !float.IsFinite(c.CellCenter.y) || !float.IsFinite(c.CellCenter.z))
                    UnityEngine.Debug.LogError($"NaN Position\ni = {i}");

                if (!float.IsFinite(vel.x) || !float.IsFinite(vel.y) || !float.IsFinite(vel.z))
                    UnityEngine.Debug.LogError($"Nan Cell Velocity\ni = {i}");

                if (!float.IsFinite(c.Temperature))
                    UnityEngine.Debug.LogError($"NaN Cell Temperature\ni = {i}");

                if (!float.IsFinite(geo.CellStaticVolume))
                    UnityEngine.Debug.LogError($"NaN Cell Volume\ni = {i}");

                if (som.ValueRO.PrevStatVolume[i] <= 0 && c.CellCenter.y < geo.CellHeight / 2f)
                    UnityEngine.Debug.LogError($"Negative/Null PrevStatVolume\ni = {i}");

                if (c.CellCenter.y <= -geo.CellHeight / 2f)
                    UnityEngine.Debug.LogError($"ACDDC - Air Cell Digging Down to China\ni = {i}");
            }
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
        public RefRO<AirCellSimulation> sim;
        public AirCellGroup group;
        public RefRW<AirCellLocalEnvironment> env;
        public RefRO<AirCellBehaviourFlags> flags;
        public InverseDistanceWeighting interp;

        public ComponentLookup<AirCell> AirCellLookup;
        public ComponentLookup<AirCellGeometry> GeoLookup;
        public NativeList<AirCellGroupMember> buffer;

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
                v[4] = env.ValueRO.AverageLocalTemp;
                v[5] = 0; /* Dynamic Volume Should Supposedly Go Here But Still Haven't Found A Use For It (TM) */

                interp.InterpolationStep(flags.ValueRO.FollowDrone ? float3.zero : new float3(interp.Query.x, 0, interp.Query.z), v);
                v.Dispose();
            }

            foreach (int i in interp.Indices)
            {
                Entity member = buffer[i].Value;
                if (AirCellLookup.TryGetComponent(member, out AirCell interpCell))
                {
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
            }

            if (!interp.BroadcastInterpolation(flags.ValueRO.InterpolationWithTerrain))
            {
                int minI = 0;
                float minD = math.INFINITY;

                for (int i = 0; i < group.CellGroupNumber; i++)
                {
                    Entity interpMember = buffer[i].Value;
                    if (AirCellLookup.TryGetComponent(interpMember, out AirCell interpCell))
                    {
                        if (minD > math.length(interp.Query - interpCell.CellCenter))
                        {
                            minD = math.length(interp.Query - interpCell.CellCenter);
                            minI = i;
                        }
                    }
                }

                Entity closestMember = buffer[minI].Value;
                if (AirCellLookup.TryGetComponent(closestMember, out AirCell closestCell))
                {
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
            }
            #endregion
        }
    }
}
