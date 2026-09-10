// i fucking hate dots

using System;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using UnityEngine;
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
    [UpdateInGroup(typeof(FixedStepSimulationSystemGroup))] // make it every fixed time step
    public partial struct AirCellManager : ISystem
    {
        private ComponentLookup<AirCell> _airCellLookup;
        private ComponentLookup<AirCellGeometry> _geoLookup;
        private ComponentLookup<AirCellOptimization> _somLookup;

        public void OnCreate(ref SystemState state)
        {
            _airCellLookup = state.GetComponentLookup<AirCell>();
            _geoLookup = state.GetComponentLookup<AirCellGeometry>();
            _somLookup = state.GetComponentLookup<AirCellOptimization>();

            _ = new InverseDistanceWeighting();

            // system only starts updating if there's an entity with this component
            state.RequireForUpdate<AirCell>();
        }

        public void OnUpdate(ref SystemState state)
        {
            var sim = SystemAPI.GetSingletonRW<AirCellSimulation>();
            float dt = SystemAPI.Time.DeltaTime * sim.ValueRO.TimeScale;

            _airCellLookup.Update(ref state);

            state.Dependency = new AirCellUpdateJob
            {
                FixedDeltaTime = dt,
                sim = sim.ValueRO,
                AirCellLookup = _airCellLookup,
                GeoLookup = _geoLookup,
                SOMLookup = _somLookup
            }.ScheduleParallel(state.Dependency);
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
        public static void AccelerationAlongVelocity(ref AirCell cell, float acc, float deltaTime)
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

    // actual update logic for the air cells
    [BurstCompile]
    public partial struct AirCellUpdateJob : IJobEntity
    {
        public float FixedDeltaTime;
        public AirCellSimulation sim;

        public ComponentLookup<AirCell> AirCellLookup;
        public ComponentLookup<AirCellGeometry> GeoLookup;
        public ComponentLookup<AirCellOptimization> SOMLookup;

        [BurstCompile]
        private void Execute
        (
            // ref is for Reading and Writing
            // in is for reading-only
            ref LocalTransform transform,
            ref AirCell cell,
            ref AirCellGeometry geo,
            ref AirCellSimulation sim,
            ref AirCellGroup group,
            ref AirCellLocalEnvironment env,
            ref AirCellOptimization som,
            in PhysicsWorldSingleton physicsWorld,
            in AirCellBehaviourFlags flags,
            in AirCellBounds bounds,
            in DynamicBuffer<AirCellGroupMember> buffer
        )
        {
            if (sim.TimeScale == 0f)
                return;
            
            env.AverageLocalTemp = 0;
            env.AverageLocalWind = float3.zero;

            // we do all this cuz we need to scan the surrounding entities with air cell components
            for (int i = 0; i < group.CellGroupNumber; i++)
            {
                Entity member = buffer[i].Value;

                if (AirCellLookup.TryGetComponent(member, out AirCell memberCell))
                {
                    // only lads with true ball know this is not the original
                    //double mem; // ...he glazes afar into the distance, as he realizes he is amongst the only double left...
                    float mem; // nevermind

                    #region Average Local Values

                    env.AverageLocalTemp += memberCell.Temperature / group.CellGroupNumber;
                    env.AverageLocalWind += memberCell.Temperature / group.CellGroupNumber;

                    #endregion

                    #region Values Setup

                    DebugEverything(i, in buffer, in AirCellLookup, in GeoLookup, in SOMLookup);

                    #region Calculate Static Pressure
                    som.StaticPressure[i] = GlobalCalc.StaticPressureAtHeight(memberCell.CellCenter.y);
                    #endregion

                    DebugEverything(i, in buffer, in AirCellLookup, in GeoLookup, in SOMLookup);

                    #region Insolation
                    memberCell.Temperature = som.Temp[i];
                    memberCell.Temperature += mem = GlobalData.Data.Gale.Insolation * geo.CellCircleArea / (GlobalData.Data.AtmHeatCp * GlobalData.Data.Gale.AtmMM * memberCell.Moles) * FixedDeltaTime;
                    #endregion

                    DebugEverything(i, in buffer, in AirCellLookup, in GeoLookup, in SOMLookup);

                    #region Radiative Heating/Cooling
                    mem = -GlobalData.Const.StefBoltz * GlobalData.Data.AtmSpecificEmissivity * ((2f * geo.CellCircleArea) + (2f * math.PI * geo.CellRadius * geo.CellHeight)) * System.MathF.Pow(memberCell.Temperature - env.AverageLocalTemp, 4f) * FixedDeltaTime;
                    memberCell.Temperature += mem;
                    #endregion

                    DebugEverything(i, in buffer, in AirCellLookup, in GeoLookup, in SOMLookup);

                    #endregion

                    #region Air Cell Physics

                    #region Calculate Static Volume
                    AirCellManager.SetSizeV(ref geo, memberCell.Moles * GlobalData.Const.R * memberCell.Temperature / som.StaticPressure[i]);
                    if (som.PrevStatVolume[i] == 0) som.PrevStatVolume[i] = geo.CellStaticVolume;
                    som.DynVolume[i] = geo.CellStaticVolume;
                    #endregion

                    #region Static Adiabatic Temperature Changes
                    memberCell.Temperature *= math.pow(som.PrevStatVolume[i] / geo.CellStaticVolume, GlobalData.Const.R / GlobalData.Data.MolarHeatCapacity);
                    som.PrevStatVolume[i] = geo.CellStaticVolume;
                    /*
                    if (math.abs(som.Temp[i] - memberCell.Temperature) > 10)
                    {
                        UnityEngine.Debug.Log("Heavy Abiatic Temperature Change [" + i + "] ; SOM = " + TempSOM[i] + " ; Temp = " + AirCellGroup[i].Temperature);
                    }*/

                    som.Temp[i] = memberCell.Temperature;
                    #endregion

                    DebugEverything(i, in buffer, in AirCellLookup, in GeoLookup, in SOMLookup);

                    #region Air Cell Terrain Repulsion
                    if (flags.TerrainAtSeaLevel && memberCell.CellCenter.y < geo.CellHeight / 2f)
                    {
                        som.DynVolume[i] *= 0.5f + (memberCell.CellCenter.y / geo.CellHeight);
                        AirCellManager.PerformAcceleration(ref memberCell, new float3(0, som.StaticPressure[i] * geo.CellCircleArea * (math.pow(geo.CellStaticVolume / som.DynVolume[i], (1f + (GlobalData.Data.MolarHeatCapacity / GlobalData.Const.R))) - 1f) / (memberCell.Moles * GlobalData.Data.Gale.AtmMM), 0), FixedDeltaTime);
                    }
                    #endregion

                    DebugEverything(i, in buffer, in AirCellLookup, in GeoLookup, in SOMLookup);

                    #region Perform Gravity and Buoyancy
                    AirCellManager.PerformAcceleration(ref memberCell, new float3(0, GlobalData.Data.Gale.SurfGravity * ((memberCell.Temperature / env.AverageLocalTemp) - 1f), 0), FixedDeltaTime);
                    #endregion

                    DebugEverything(i, in buffer, in AirCellLookup, in GeoLookup, in SOMLookup);

                    #region Perform Air Cell Drag
                    AirCellManager.PerformAcceleration(ref memberCell, new float3(sim.CdTest * math.pow(memberCell.Velocity.x - env.AverageLocalWind.x, 2f) / (4f * geo.CellRadius), sim.CdTest * math.pow(memberCell.Velocity.y - env.AverageLocalWind.y, 2f) / (2f * geo.CellHeight), sim.CdTest * math.pow(memberCell.Velocity.z - env.AverageLocalWind.z, 2f) / (4f * geo.CellRadius)), FixedDeltaTime);
                    #endregion

                    DebugEverything(i, in buffer, in AirCellLookup, in GeoLookup, in SOMLookup);

                    #region Check for Terrain Collision - to do: improve with bouncing
                    if (memberCell.CellCenter.y <= (-geo.CellHeight / 2f))
                    {
                        memberCell.CellCenter = new float3(memberCell.CellCenter.x, -geo.CellHeight / 2f + 0.2f, memberCell.CellCenter.z);
                    }
                    #endregion

                    DebugEverything(i, in buffer, in AirCellLookup, in GeoLookup, in SOMLookup);

                    #region Keep Within Boundaries - note: for testing purposes

                    if (math.abs(memberCell.CellCenter.x) >= (bounds.Value.x / 2) + 0.1f)
                    {
                        memberCell.Velocity += new float3(-math.sign(memberCell.CellCenter.x) * 50f * FixedDeltaTime, 0f, 0f);
                    }

                    if (math.abs(memberCell.CellCenter.z) >= (bounds.Value.x / 2))
                    {
                        memberCell.Velocity += new float3(0, 0, -math.sign(memberCell.CellCenter.z) * 50f * FixedDeltaTime);
                    }

                    if (memberCell.CellCenter.y >= bounds.Value.y + 0.1)
                    {
                        memberCell.Velocity += new float3(0, -math.sign(memberCell.CellCenter.y) * 50 * FixedDeltaTime, 0);
                    }

                    #endregion

                    DebugEverything(i, in buffer, in AirCellLookup, in GeoLookup, in SOMLookup);

                    #endregion

                    #region Inter-Cell Repulsion
                    som.CellRepulsion = new NativeArray<float3>();
                    float d, r1, r2, d1, d2, A, h;

                    for (int i2 = i + 1; i2 < group.CellGroupNumber; i2++)
                    {
                        Entity member2 = buffer[i2].Value;
                        if (AirCellLookup.TryGetComponent(member2, out AirCell member2Cell) && GeoLookup.TryGetComponent(member2, out AirCellGeometry geo2))
                        {
                            #region Check For and Calculate Overlaps
                            d = SafeValue(math.sqrt(math.square(memberCell.CellCenter.x - member2Cell.CellCenter.x) +
                                math.square(memberCell.CellCenter.z - member2Cell.CellCenter.z)));

                            if ((h = math.abs(memberCell.CellCenter.y - member2Cell.CellCenter.y)) < (geo.CellHeight + geo2.CellHeight) / 2f && 
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

                                if (h * A > 0 && som.DynVolume[i] > 0 && som.DynVolume[i2] > 0)
                                {
                                    som.DynVolume[i] = SafeValue(som.DynVolume[i] - (A * h / 2f));
                                    som.DynVolume[i2] = SafeValue(som.DynVolume[i2] - (A * h / 2f));

                                    som.CellRepulsion = new NativeArray<float3>(array: som.CellRepulsion.Concat(new NativeArray<float3>(new float3[1] { new float3(i, i2, A * h) }, Allocator.TempJob)).ToArray(), Allocator.TempJob);
                                }
                                else
                                {
                                    som.DynVolume[i] = SafeValue(som.DynVolume[i]);
                                    som.DynVolume[i2] = SafeValue(som.DynVolume[i2]);
                                }

                                DebugEverything(i, in buffer, in AirCellLookup, in GeoLookup, in SOMLookup);
                                DebugEverything(i2, in buffer, in AirCellLookup, in GeoLookup, in SOMLookup);
                            }
                            #endregion
                        }
                    }

                    #endregion
                }

                #region Repulsion Physics

                foreach (float3 Repulsion in som.CellRepulsion)
                {
                    int i1 = (int)Repulsion.x;
                    int i2 = (int)Repulsion.y;

                    if (AirCellLookup.TryGetComponent(buffer[i1].Value, out AirCell repulCell) && AirCellLookup.TryGetComponent(buffer[i2].Value, out AirCell repul2Cell) && GeoLookup.TryGetComponent(buffer[i1].Value, out AirCellGeometry repulGeo) && GeoLookup.TryGetComponent(buffer[i2].Value, out AirCellGeometry repul2Geo))
                    {
                        float mag = (som.StaticPressure[i1] * math.pow(som.CellRepulsion[i1].z, 2f / 3f) *
                            (math.pow(repulGeo.CellStaticVolume / som.DynVolume[i1], 1f + (GlobalData.Data.MolarHeatCapacity / GlobalData.Const.R)) - 1f) /
                            (repulCell.Moles * GlobalData.Data.Gale.AtmMM));
                        float norm = math.sqrt(math.square(repulCell.CellCenter.x - repul2Cell.CellCenter.x) + math.square(repulCell.CellCenter.y - repul2Cell.CellCenter.y) + math.square(repulCell.CellCenter.z - repul2Cell.CellCenter.z));

                        AirCellManager.PerformAcceleration(ref repulCell, mag * ((repulCell.CellCenter - repul2Cell.CellCenter) / norm), FixedDeltaTime);

                        mag = (som.StaticPressure[i2] * math.pow(som.CellRepulsion[i2].z, 2f / 3f) *
                            (math.pow(repul2Geo.CellStaticVolume / som.DynVolume[i2], 1f + (GlobalData.Data.MolarHeatCapacity / GlobalData.Const.R)) - 1f) /
                            (repul2Cell.Moles * GlobalData.Data.Gale.AtmMM));
                        
                        AirCellManager.PerformAcceleration(ref repul2Cell, mag * ((repul2Cell.CellCenter - repulCell.CellCenter) / norm), FixedDeltaTime);
                    }
                }

                #endregion

                if (AirCellLookup.TryGetComponent(member, out memberCell))
                {
                    #region Cell-Terrain Repulsion

                    if (!flags.TerrainAtSeaLevel)
                    {
                        float maxD = math.sqrt(math.pow(geo.CellRadius, 2) + math.pow(geo.CellHeight / 2f, 2f));

                        RaycastInput ray = new RaycastInput();
                        ray.Start = memberCell.CellCenter + new float3(0, geo.CellHeight / 2f, 0);
                        ray.End = ray.Start - new float3(0, maxD, 0);
                        ray.Filter = new CollisionFilter();
                        ray.Filter.CollidesWith = 1 << 3;
                        Unity.Physics.RaycastHit hit;
                        if (physicsWorld.CastRay(ray, out hit))
                        {
                            float d3 = math.abs(hit.Position.y - memberCell.CellCenter.y);
                            if (d3 < geo.CellHeight / 2f)
                            {
                                som.DynVolume[i] *= 0.5f + (d3 / geo.CellHeight);
                                AirCellManager.PerformAcceleration(ref memberCell, new float3(0, som.StaticPressure[i] * geo.CellCircleArea * (math.pow(geo.CellStaticVolume / som.DynVolume[i], (1f + (GlobalData.Data.MolarHeatCapacity / GlobalData.Const.R))) - 1f) / (memberCell.Moles * GlobalData.Data.Gale.AtmMM), 0), FixedDeltaTime);
                            }
                        }
                    }

                    #endregion

                    #region Perform Physics

                    DebugEverything(i, in buffer, in AirCellLookup, in GeoLookup, in SOMLookup);

                    #region Dynamic Abiatic Temperature Change

                    som.DynVolume[i] = SafeValue(som.DynVolume[i]);
                    if (som.PrevDynVolume[i] == 0) som.PrevDynVolume[i] = som.DynVolume[i];
                    memberCell.Temperature *= math.pow(som.PrevDynVolume[i] / som.DynVolume[i], GlobalData.Const.R / GlobalData.Data.MolarHeatCapacity);
                    som.PrevDynVolume = som.DynVolume;

                    #endregion

                    AirCellManager.PerformVelocity(ref memberCell, FixedDeltaTime);

                    DebugEverything(i, in buffer, in AirCellLookup, in GeoLookup, in SOMLookup);

                    //To visualize the Air Cells
                    if (flags.AirCellObjects) transform.Position = memberCell.CellCenter;

                    //For interpolation
                    if (InverseDistanceWeighting.Instance.Indices.Contains<int>(i))
                    {
                        if (math.sqrt(math.square(InverseDistanceWeighting.Instance.Query.x - memberCell.CellCenter.x) +
                            math.square(InverseDistanceWeighting.Instance.Query.y - memberCell.CellCenter.y) +
                            math.square(InverseDistanceWeighting.Instance.Query.z - memberCell.CellCenter.z)) > InverseDistanceWeighting.Instance.R)
                            InverseDistanceWeighting.Instance.Remove(i);
                    }
                    else if (math.sqrt(math.square(InverseDistanceWeighting.Instance.Query.x - memberCell.CellCenter.x) +
                            math.square(InverseDistanceWeighting.Instance.Query.y - memberCell.CellCenter.y) +
                            math.square(InverseDistanceWeighting.Instance.Query.z - memberCell.CellCenter.z)) <= InverseDistanceWeighting.Instance.R)
                        InverseDistanceWeighting.Instance.Add(i);

                    /*
                    if (i == 0)
                    {
                        UnityEngine.Debug.Log("");
                        UnityEngine.Debug.Log("Velocity: " + memberCell.Velocity);
                        UnityEngine.Debug.Log("Acceleration: " + memberCell.Acceleration);
                    }*/

                    DebugEverything(i, in buffer, in AirCellLookup, in GeoLookup, in SOMLookup);

                    #endregion
                }
            }

            #region Interpolation

            InverseDistanceWeighting.Instance.BeginInterpolation(flags.FollowDrone);

            if (flags.InterpolationWithTerrain)
            {
                InverseDistanceWeighting.Instance.InterpolationStep(flags.FollowDrone ? float3.zero : new float3(InverseDistanceWeighting.Instance.Query.x, 0, InverseDistanceWeighting.Instance.Query.z),
                    new NativeArray<float>(new float[6]
                    {
                        0f, 0f, 0f, sim.MoleTest, env.AverageLocalTemp, 0f
                    }, Allocator.TempJob));
            }

            foreach (int ii in InverseDistanceWeighting.Instance.Indices)
            {
                Entity member = buffer[ii].Value;
                if (AirCellLookup.TryGetComponent(member, out AirCell interpCell))
                {
                    InverseDistanceWeighting.Instance.InterpolationStep(interpCell.CellCenter,
                        new NativeArray<float>(new float[6] { interpCell.Velocity.x * sim.TimeScale, interpCell.Velocity.y * sim.TimeScale,
                        interpCell.Velocity.z * sim.TimeScale, interpCell.Moles, interpCell.Temperature, 0 /* Dynamic Volume Should Supposedly Go Here But Still Haven't Found A Use For It (TM) */ }, Allocator.TempJob));
                }
            }

            if (!InverseDistanceWeighting.Instance.BroadcastInterpolation(flags.InterpolationWithTerrain))
            {
                int minI = 0;
                float minD = math.INFINITY;

                for (int i = 0; i < group.CellGroupNumber; i++)
                {
                    Entity interpMember = buffer[i].Value;
                    if (AirCellLookup.TryGetComponent(interpMember, out AirCell interpCell))
                    {
                        if (minD > math.sqrt(math.square(InverseDistanceWeighting.Instance.Query.x - interpCell.CellCenter.x) +
                            math.square(InverseDistanceWeighting.Instance.Query.y - interpCell.CellCenter.y) +
                            math.square(InverseDistanceWeighting.Instance.Query.z - interpCell.CellCenter.z)))
                        {
                            minD = math.sqrt(math.square(InverseDistanceWeighting.Instance.Query.x - interpCell.CellCenter.x) +
                            math.square(InverseDistanceWeighting.Instance.Query.y - interpCell.CellCenter.y) +
                            math.square(InverseDistanceWeighting.Instance.Query.z - interpCell.CellCenter.z));
                            minI = i;
                        }
                    }
                }

                Entity closestMember = buffer[minI].Value;
                if (AirCellLookup.TryGetComponent(closestMember, out AirCell closestCell))
                    InverseDistanceWeighting.Instance.GetClosestCell(new NativeArray<float>(new float[6] { closestCell.Velocity.x * sim.TimeScale,
                    closestCell.Velocity.y * sim.TimeScale, closestCell.Velocity.z * sim.TimeScale, closestCell.Moles, closestCell.Temperature, 0 /* Dynamic Volume Should Supposedly Go Here But Still Haven't Found A Use For It (TM) */ }, Allocator.TempJob));
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
            in ComponentLookup<AirCellGeometry> geoLookup,
            in ComponentLookup<AirCellOptimization> somLookup
        )
        {
            Entity member = buffer[i].Value;

            if
            (
                airCellLookup.TryGetComponent(member, out AirCell c)
                &&
                geoLookup.TryGetComponent(member, out AirCellGeometry geo)
                &&
                somLookup.TryGetComponent(member, out AirCellOptimization som)
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

                if (som.PrevStatVolume[i] <= 0 && c.CellCenter.y < geo.CellHeight / 2f)
                    UnityEngine.Debug.LogError($"Negative/Null PrevStatVolume\ni = {i}");
                
                if (c.CellCenter.y <= -geo.CellHeight / 2f)
                    UnityEngine.Debug.LogError($"ACDDC - Air Cell Digging Down to China\ni = {i}");
            }
        }

        [BurstCompile]
        float SafeValue(float value)
        {
            return math.max(value, 1e-2f);
        }

        #endregion
    }
}