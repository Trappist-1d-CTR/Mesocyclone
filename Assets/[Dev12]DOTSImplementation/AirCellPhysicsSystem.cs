using Mesocyclone.Data;
using System.Diagnostics;
using Unity.Burst;
using Unity.Collections;
//using Unity.Collections.LowLevel.Unsafe;
using Unity.Entities;
using Unity.Jobs;
using Unity.Mathematics;
using Unity.Physics;
using UnityEngine.Jobs;

//using UnityEngine;
using UnityEngine.Jobs;

namespace Mesocyclone.MesoDOTS
{
    [BurstCompile]
    [RequireMatchingQueriesForUpdate]
    [UpdateInGroup(typeof(FixedStepSimulationSystemGroup))] // make it every fixed time step
    public partial struct AirCellPhysicsSystem : ISystem
    {
        #region Component Data

        #region Air Cell Components
        public NativeArray<AirCell> CellList;
        public NativeArray<AirCellGeometry> GeometryList;
        #endregion

        #region Singleton Data
        public AirCellOptimization som;
        public AirCellLocalEnvironment env;

        public static readonly AirCellGroup group = new() { CellGroupNumber = 32 };
        public static readonly AirCellBounds bounds = new() { Value = new(1000, 1000) };
        public static readonly AirCellSimulation sim = new()
        {
            TimeScale = 1,
            DronePosition = float3.zero,
            CdTest = 1,
            MoleTest = GlobalData.Data.Gale.AtmPressure * 1000000f * bounds.Value.y / (GlobalData.Data.Gale.Radius * GlobalData.Data.Gale.SurfTemp * group.CellGroupNumber),
            TempTest = 1500,
            VelTest = float3.zero,
            CenterTest = float3.zero
        };
        public static readonly AirCellBehaviourFlags flags = new()
        {
            AirCellObjects = false,
            FollowDrone = false,
            InterpolationWithTerrain = false,
            TerrainAtSeaLevel = true
        };
        #endregion

        #region Interpolation
        public InverseDistanceWeighting interpolation;
        public NativeArray<bool> IsInInterpolationRange;
        public NativeArray<float> InterpolationResult;
        #endregion

        #region Other
        public PhysicsWorldSingleton physicsWorld;
        #endregion

        #endregion

        public void OnCreate(ref SystemState state)
        {
            #region Get and Setup SOM

            som = new()
            {
                StaticPressure = new(group.CellGroupNumber, Allocator.Persistent),
                PrevStatVolume = new(group.CellGroupNumber, Allocator.Persistent),
                DynVolume = new(group.CellGroupNumber, Allocator.Persistent),
                PrevDynVolume = new(group.CellGroupNumber, Allocator.Persistent),
                Temp = new(group.CellGroupNumber, Allocator.Persistent),
                CellRepulsion = new(Allocator.Persistent)
            };

            #endregion

            #region Get Lists

            CellList = new NativeArray<AirCell>(group.CellGroupNumber, Allocator.Persistent);
            GeometryList = new NativeArray<AirCellGeometry>(group.CellGroupNumber, Allocator.Persistent);
            for (int i = 0; i < group.CellGroupNumber; i++)
            {
                AirCell cell = new()
                { ID = i };

                float3 InstantiateLocation = new()
                {
                    x = (5f * bounds.Value.x / 12f) * ((CellList[i].ID % 3) - 1),
                    y = ((2f * bounds.Value.y / 7f) * (CellList[i].ID / 9)) + (3f * bounds.Value.x / 14f),
                    z = (5f * bounds.Value.x / 12f) * (((CellList[i].ID / 3) % 3) - 1)
                };

                Unity.Mathematics.Random RandomValue = Unity.Mathematics.Random.CreateFromIndex(1);

                cell.CellCenter = InstantiateLocation;
                cell.Moles = sim.MoleTest;
                cell.Temperature = sim.TempTest + (((RandomValue.NextFloat() * 2f) - 1f) * 25f);
                cell.Velocity = sim.VelTest + (((RandomValue.NextFloat3() * 2f) - 1f) * 10f);

                CellList[i] = cell;
                GeometryList[i] = new();
            }

            #endregion

            #region Get Interpolation

            interpolation = new();

            #endregion
        }

        public void OnDestroy(ref SystemState state)
        {
            #region Dispose Lists

            CellList.Dispose();
            GeometryList.Dispose();

            som.StaticPressure.Dispose();
            som.PrevStatVolume.Dispose();
            som.DynVolume.Dispose();
            som.PrevDynVolume.Dispose();
            som.Temp.Dispose();
            som.CellRepulsion.Dispose();

            InterpolationResult.Dispose();

            #endregion
        }

        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            #region Starting Update Setup
            
            env = new() { AverageLocalTemp = 0, AverageLocalWind = 0 };
            som.CellRepulsion.Clear();
            physicsWorld = SystemAPI.GetSingleton<PhysicsWorldSingleton>();

            float dt = SystemAPI.Time.DeltaTime * sim.TimeScale;

            #endregion

            #region Schedule and Complete Jobs

            AirCellValuesSetupJob SetupJob = new()
            {
                FixedDeltaTime = dt,
                inst = this
            };
            state.Dependency = SetupJob.Schedule(group.CellGroupNumber, 5, state.Dependency);

            state.Dependency.Complete();
            SetStructValues(SetupJob.inst);

            som = SetupJob.inst.som;

            AirCellPhysics1Job Physics1Job = new()
            {
                FixedDeltaTime = dt,
                inst = this
            };
            state.Dependency = Physics1Job.Schedule(group.CellGroupNumber, 5, state.Dependency);

            state.Dependency.Complete();
            SetStructValues(Physics1Job.inst);

            AirCellTerrainRepulsionJob TerrainRepulsionJob = new()
            {
                FixedDeltaTime = dt,
                physicsWorld = physicsWorld,
                inst = this
            };
            state.Dependency = TerrainRepulsionJob.Schedule(state.Dependency);

            state.Dependency.Complete();
            SetStructValues(TerrainRepulsionJob.inst);

            AirCellRepulsionPhysicsJob PhysicsRepulsionJob = new()
            {
                FixedDeltaTime = dt,
                somCellRepulsion = som.CellRepulsion,
                somStaticPressure = som.StaticPressure,
                somDynVolume = som.DynVolume,
                inst = this
            };
            state.Dependency = PhysicsRepulsionJob.Schedule(som.CellRepulsion.Length, 15, state.Dependency);

            state.Dependency.Complete();
            SetStructValues(PhysicsRepulsionJob.inst);

            UnityEngine.Debug.Log($"Pos before Physics2: {CellList[0].CellCenter}");

            AirCellPhysics2Job Physics2Job = new()
            {
                FixedDeltaTime = dt,
                inst = this
            };
            state.Dependency = Physics2Job.Schedule(group.CellGroupNumber, 5, state.Dependency);

            state.Dependency.Complete();
            SetStructValues(Physics2Job.inst);

            UnityEngine.Debug.Log($"Pos after Physics2: {CellList[0].CellCenter}");

            AirCellInterpolationJob InterpolationJob = new()
            {
                FixedDeltaTime = dt,
                interp = interpolation,
                inst = this
            };
            state.Dependency = InterpolationJob.Schedule(state.Dependency);

            state.Dependency.Complete();
            SetStructValues(InterpolationJob.inst);

            #endregion

            InterpolationResult.Dispose();
            InterpolationResult = new(interpolation.Values, Allocator.Persistent);
        }

        [BurstCompile]
        public void SetStructValues(AirCellPhysicsSystem RefInstance)
        {
            CellList = RefInstance.CellList;
            GeometryList = RefInstance.GeometryList;

            env = RefInstance.env;
            som = RefInstance.som;

            IsInInterpolationRange = RefInstance.IsInInterpolationRange;
            InterpolationResult = RefInstance.InterpolationResult;
        }

        #region Cell and Geometry Functions

        [BurstCompile]
        public void PerformVelocity(int ID, float deltaTime)
        {
            AirCell cell = CellList[ID];
            cell.CellCenter += cell.Velocity * deltaTime;
            CellList[ID] = cell;
        }
        [BurstCompile]
        public void PerformAcceleration(int ID, in float3 acc, float deltaTime)
        {
            AirCell cell = CellList[ID];
            cell.Acceleration = acc;
            cell.Velocity += cell.Acceleration * deltaTime;
            CellList[ID] = cell;
        }
        [BurstCompile]
        public void AccelerationAlongVelocity(int ID, float deltaTime)
        {
            AirCell cell = CellList[ID];
            if (math.lengthsq(cell.Velocity) > 1E-10f)
            {
                cell.Acceleration = math.normalize(cell.Velocity);
                cell.Velocity += cell.Acceleration * deltaTime;
                CellList[ID] = cell;
            }
        }

        // no geo?  we poor af frfr :broken_heart:
        [BurstCompile]
        public void SetSizeV(int ID, float v)
        {
            AirCellGeometry geo = GeometryList[ID];
            geo.CellStaticVolume = v;
            geo.CellHeight = math.pow(v, 1f / 3f);
            geo.CellCircleArea = v / geo.CellHeight;
            geo.CellRadius = math.sqrt(geo.CellCircleArea / math.PI);
            GeometryList[ID] = geo;
        }
        [BurstCompile]
        public void SetSizeVL(int ID, float v, float l)
        {
            AirCellGeometry geo = GeometryList[ID];
            geo.CellStaticVolume = v;
            geo.CellHeight = l;
            geo.CellCircleArea = v / l;
            geo.CellRadius = math.sqrt(geo.CellCircleArea / math.PI);
            GeometryList[ID] = geo;
        }
        [BurstCompile]
        public void SetSizeRL(int ID, float r, float l)
        {
            AirCellGeometry geo = GeometryList[ID];
            geo.CellRadius = r;
            geo.CellHeight = l;
            geo.CellCircleArea = math.pow(r, 2) * math.PI;
            geo.CellStaticVolume = geo.CellCircleArea * l;
            GeometryList[ID] = geo;
        }

        #endregion

        #region Debug and Safety Functions

        [BurstCompile]
        [Conditional("DEV")]
        public void DebugEverything
            (
                int i
            )
        {
            AirCell c = CellList[i];
            AirCellGeometry g = GeometryList[i];

            float3 vel = c.Velocity;

            if (!float.IsFinite(c.CellCenter.x) || !float.IsFinite(c.CellCenter.y) || !float.IsFinite(c.CellCenter.z))
                UnityEngine.Debug.LogError($"NaN Position\ni = {i}");

            if (!float.IsFinite(vel.x) || !float.IsFinite(vel.y) || !float.IsFinite(vel.z))
                UnityEngine.Debug.LogError($"Nan Cell Velocity\ni = {i}");

            if (!float.IsFinite(c.Temperature))
                UnityEngine.Debug.LogError($"NaN Cell Temperature\ni = {i}");

            if (!float.IsFinite(g.CellStaticVolume))
                UnityEngine.Debug.LogError($"NaN Cell Volume\ni = {i}");

            if (som.PrevStatVolume[i] <= 0 && c.CellCenter.y < g.CellHeight / 2f)
                UnityEngine.Debug.LogError($"Negative/Null PrevStatVolume\ni = {i}");

            if (c.CellCenter.y <= -g.CellHeight / 2f)
                UnityEngine.Debug.LogError($"ACDDC - Air Cell Digging Down to China\ni = {i}");
        }

        [BurstCompile]
        public static float SafeValue(float value)
        {
            return math.max(value, 1e-2f);
        }

        #endregion

        #region Burst Parallelized Jobs

        [BurstCompile]
        public partial struct AirCellValuesSetupJob : IJobParallelFor
        {
            public float FixedDeltaTime;
            public AirCellPhysicsSystem inst;

            [BurstCompile]
            public void Execute(int index)
            {
                // only lads with true ball know this is not the original
                //double mem; // ...he glazes afar into the distance, as he realizes he is amongst the only double left...
                float mem; // nevermind

                AirCell cell = inst.CellList[index];
                AirCellGeometry geo = inst.GeometryList[index];

                #region Values Setup

                #region Average Local Values
                inst.env.AverageLocalTemp += cell.Temperature / group.CellGroupNumber;
                inst.env.AverageLocalWind += cell.Velocity / group.CellGroupNumber;
                #endregion

                UnityEngine.Debug.Log($"Avg local temp: {inst.env.AverageLocalTemp}");

                inst.DebugEverything(cell.ID);

                #region Calculate Static Pressure
                inst.som.StaticPressure[cell.ID] = GlobalCalc.StaticPressureAtHeight(cell.CellCenter.y);
                #endregion

                inst.DebugEverything(cell.ID);

                #region Insolation
                if (inst.som.Temp[cell.ID] == 0) inst.som.Temp[cell.ID] = cell.Temperature;
                else cell.Temperature = inst.som.Temp[cell.ID];
                cell.Temperature += mem = GlobalData.Data.Gale.Insolation * geo.CellCircleArea / (GlobalData.Data.AtmHeatCp * GlobalData.Data.Gale.AtmMM * cell.Moles) * FixedDeltaTime;
                #endregion

                inst.DebugEverything(cell.ID);

                #region Radiative Cooling
                mem = -GlobalData.Const.StefBoltz * GlobalData.Data.AtmSpecificEmissivity * ((2f * geo.CellCircleArea) + (2f * math.PI * geo.CellRadius * geo.CellHeight)) * math.pow(cell.Temperature, 4f) * FixedDeltaTime;
                //cell.Temperature += mem;
                #endregion

                inst.DebugEverything(cell.ID);

                #region Calculate Static Volume

                inst.SetSizeV(cell.ID, cell.Moles * GlobalData.Const.R * cell.Temperature / inst.som.StaticPressure[cell.ID]);
                if (inst.som.PrevStatVolume[cell.ID] == 0)
                {
                    inst.som.PrevStatVolume[cell.ID] = geo.CellStaticVolume;
                }
                inst.som.DynVolume[cell.ID] = geo.CellStaticVolume;

                #endregion

                inst.DebugEverything(cell.ID);

                #region Static Adiabatic Temperature Changes

                cell.Temperature *= math.pow(inst.som.PrevStatVolume[cell.ID] / geo.CellStaticVolume, GlobalData.Const.R / GlobalData.Data.MolarHeatCapacity);
                inst.som.PrevStatVolume[cell.ID] = geo.CellStaticVolume;
                /*
                if (math.abs(inst.som.Temp[cell.ID] - cell.Temperature) > 10)
                {
                    UnityEngine.Debug.Log("Heavy Abiatic Temperature Change [" + cell.ID + "] ; inst.som = " + inst.som.Temp[cell.ID] + " ; Temp = " + cell.Temperature);
                }*/

                inst.som.Temp[cell.ID] = cell.Temperature;

                #endregion

                inst.DebugEverything(cell.ID);

                #endregion

                inst.CellList[index] = cell;
                inst.GeometryList[index] = geo;
            }
        }

        [BurstCompile]
        public partial struct AirCellPhysics1Job : IJobParallelFor
        {
            public float FixedDeltaTime;
            public AirCellPhysicsSystem inst;

            [BurstCompile]
            public void Execute(int index)
            {
                AirCell cell = inst.CellList[index];
                AirCellGeometry geo = inst.GeometryList[index];

                #region Air Cell Physics

                UnityEngine.Debug.Log($"Avg local temp: {inst.env.AverageLocalTemp}");

                inst.DebugEverything(cell.ID);
                UnityEngine.Debug.Log($"Acceleration pre-gravity: {cell.Acceleration}");

                #region Perform Gravity and Buoyancy
                inst.PerformAcceleration(cell.ID, new float3(0, GlobalData.Data.Gale.SurfGravity * ((cell.Temperature / inst.env.AverageLocalTemp) - 1f), 0), FixedDeltaTime);
                #endregion

                UnityEngine.Debug.Log($"Acceleration post-gravity: {cell.Acceleration}");
                inst.DebugEverything(cell.ID);

                #region Perform Air Cell Drag
                inst.PerformAcceleration(cell.ID, new float3(sim.CdTest * math.pow(cell.Velocity.x - inst.env.AverageLocalWind.x, 2f) / (4f * geo.CellRadius),
                    sim.CdTest * math.pow(cell.Velocity.y - inst.env.AverageLocalWind.y, 2f) / (2f * geo.CellHeight),
                    sim.CdTest * math.pow(cell.Velocity.z - inst.env.AverageLocalWind.z, 2f) / (4f * geo.CellRadius)), FixedDeltaTime);
                #endregion

                inst.DebugEverything(cell.ID);

                #region Check for Terrain Collision - to do: improve with bouncing
                if (cell.CellCenter.y <= (-geo.CellHeight / 2f))
                {
                    cell.CellCenter = new float3(cell.CellCenter.x, (-geo.CellHeight / 2f) + 0.2f, cell.CellCenter.z);
                }
                #endregion

                inst.DebugEverything(cell.ID);

                #region Keep Within Boundaries - note: for testing purposes

                if (math.abs(cell.CellCenter.x) >= (bounds.Value.x / 2) + 0.1f)
                {
                    cell.Velocity += new float3(-math.sign(cell.CellCenter.x) * 50f * FixedDeltaTime, 0f, 0f);
                }

                if (math.abs(cell.CellCenter.z) >= (bounds.Value.x / 2))
                {
                    cell.Velocity += new float3(0, 0, -math.sign(cell.CellCenter.z) * 50f * FixedDeltaTime);
                }

                if (cell.CellCenter.y >= bounds.Value.y + 0.1)
                {
                    cell.Velocity += new float3(0, -math.sign(cell.CellCenter.y) * 50 * FixedDeltaTime, 0);
                }

                #endregion

                inst.DebugEverything(cell.ID);

                #region Air Cell Terrain Repulsion
                if (flags.TerrainAtSeaLevel && cell.CellCenter.y < geo.CellHeight / 2f)
                {
                    inst.som.DynVolume[cell.ID] *= 0.5f + (cell.CellCenter.y / geo.CellHeight);
                    inst.PerformAcceleration(cell.ID, new float3(0, inst.som.StaticPressure[cell.ID] * geo.CellCircleArea * (math.pow(geo.CellStaticVolume / inst.som.DynVolume[cell.ID],
                        1f + (GlobalData.Data.MolarHeatCapacity / GlobalData.Const.R)) - 1f) / (cell.Moles * GlobalData.Data.Gale.AtmMM), 0), FixedDeltaTime);
                }
                #endregion

                inst.DebugEverything(cell.ID);

                #endregion

                #region Calculate Inter-Cell Repulsion Forces

                float d, r1, r2, d1, d2, A, h;

                for (int i2 = cell.ID + 1; i2 < group.CellGroupNumber; i2++)
                {
                    AirCell cell2 = inst.CellList[i2];
                    AirCellGeometry geo2 = inst.GeometryList[i2];

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

                        if (h * A > 0 && inst.som.DynVolume[cell.ID] > 0 && inst.som.DynVolume[i2] > 0)
                        {
                            inst.som.DynVolume[cell.ID] = SafeValue(inst.som.DynVolume[cell.ID] - (A * h / 2f));
                            inst.som.DynVolume[i2] = SafeValue(inst.som.DynVolume[i2] - (A * h / 2f));

                            inst.som.CellRepulsion.Add(new float3(cell.ID, i2, A * h));
                        }
                        else
                        {
                            inst.som.DynVolume[cell.ID] = SafeValue(inst.som.DynVolume[cell.ID]);
                            inst.som.DynVolume[i2] = SafeValue(inst.som.DynVolume[i2]);
                        }

                        inst.DebugEverything(cell.ID);
                        inst.DebugEverything(i2);
                    }

                    #endregion

                    inst.CellList[i2] = cell2;
                    inst.GeometryList[i2] = geo2;
                }

                #endregion

                inst.CellList[index] = cell;
                inst.GeometryList[index] = geo;
            }
        }

        [BurstCompile]
        public partial struct AirCellTerrainRepulsionJob : IJob
        {
            public float FixedDeltaTime;
            public PhysicsWorldSingleton physicsWorld;
            public AirCellPhysicsSystem inst;

            [BurstCompile]
            public void Execute()
            {
                for (int index = 0; index < group.CellGroupNumber; index++)
                {
                    AirCell cell = inst.CellList[index];
                    AirCellGeometry geo = inst.GeometryList[index];

                    #region Cell-Terrain Repulsion

                    inst.DebugEverything(cell.ID);

                    if (!flags.TerrainAtSeaLevel)
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
                                inst.som.DynVolume[cell.ID] *= 0.5f + (d3 / geo.CellHeight);
                                inst.PerformAcceleration(cell.ID, new float3(0, inst.som.StaticPressure[cell.ID] * geo.CellCircleArea * (math.pow(geo.CellStaticVolume / inst.som.DynVolume[cell.ID],
                                    1f + (GlobalData.Data.MolarHeatCapacity / GlobalData.Const.R)) - 1f) / (cell.Moles * GlobalData.Data.Gale.AtmMM), 0), FixedDeltaTime);
                            }
                        }
                    }

                    inst.DebugEverything(cell.ID);

                    #endregion

                    inst.CellList[index] = cell;
                    inst.GeometryList[index] = geo;
                }
            }
        }

        [BurstCompile]
        public partial struct AirCellRepulsionPhysicsJob : IJobParallelFor
        {
            public float FixedDeltaTime;
            public AirCellPhysicsSystem inst;

            [ReadOnly]
            public NativeList<float3> somCellRepulsion;
            [ReadOnly]
            public NativeArray<float> somStaticPressure;
            [ReadOnly]
            public NativeArray<float> somDynVolume;

            [BurstCompile]
            public void Execute(int index)
            {
                float3 Repulsion = somCellRepulsion[index];

                int i1 = (int)Repulsion.x;
                int i2 = (int)Repulsion.y;

                AirCell repulCell = inst.CellList[i1];
                AirCell repul2Cell = inst.CellList[i2];
                AirCellGeometry repulGeo = inst.GeometryList[i1];
                AirCellGeometry repul2Geo = inst.GeometryList[i2];

                #region Repulsion Physics

                float mag = somStaticPressure[i1] * math.pow(Repulsion.z, 2f / 3f) * (math.pow(repulGeo.CellStaticVolume / somDynVolume[i1],
                        1f + (GlobalData.Data.MolarHeatCapacity / GlobalData.Const.R)) - 1f) / (repulCell.Moles * GlobalData.Data.Gale.AtmMM);

                inst.PerformAcceleration(repulCell.ID, mag * math.normalize(repulCell.CellCenter - repul2Cell.CellCenter), FixedDeltaTime);

                mag = somStaticPressure[i2] * math.pow(Repulsion.z, 2f / 3f) * (math.pow(repul2Geo.CellStaticVolume / somDynVolume[i2],
                    1f + (GlobalData.Data.MolarHeatCapacity / GlobalData.Const.R)) - 1f) / (repul2Cell.Moles * GlobalData.Data.Gale.AtmMM);

                inst.PerformAcceleration(repul2Cell.ID, mag * math.normalize(repul2Cell.CellCenter - repulCell.CellCenter), FixedDeltaTime);

                #endregion

                inst.CellList[i1] = repulCell;
                inst.CellList[i2] = repul2Cell;
                inst.GeometryList[i1] = repulGeo;
                inst.GeometryList[i2] = repul2Geo;
            }
        }

        [BurstCompile]
        public partial struct AirCellPhysics2Job : IJobParallelFor
        {
            public float FixedDeltaTime;
            [ReadOnly]
            public InverseDistanceWeighting interp;
            public AirCellPhysicsSystem inst;

            [BurstCompile]
            public void Execute(int index)
            {
                AirCell cell = inst.CellList[index];

                #region Perform Physics

                inst.DebugEverything(cell.ID);

                #region Dynamic Abiatic Temperature Change

                inst.som.DynVolume[cell.ID] = SafeValue(inst.som.DynVolume[cell.ID]);
                if (inst.som.PrevDynVolume[cell.ID] == 0) inst.som.PrevDynVolume[cell.ID] = inst.som.DynVolume[cell.ID];
                cell.Temperature *= math.pow(inst.som.PrevDynVolume[cell.ID] / inst.som.DynVolume[cell.ID], GlobalData.Const.R / GlobalData.Data.MolarHeatCapacity);
                inst.som.PrevDynVolume = inst.som.DynVolume;

                #endregion

                inst.PerformVelocity(cell.ID, FixedDeltaTime);

                UnityEngine.Debug.Log($"Cell position: {cell.CellCenter}\nCell index: {cell.ID}");

                inst.DebugEverything(cell.ID);

                //To visualize position
                if (flags.AirCellObjects)
                    UnityEngine.Debug.Log($"Air Cell {cell.ID} at position {cell.CellCenter}");

                //Check if Within Interpolation Range
                inst.IsInInterpolationRange[index] = math.length(interp.Query - cell.CellCenter) <= interp.R;

                /*
                if (i == 0)
                {
                    UnityEngine.Debug.Log("");
                    UnityEngine.Debug.Log("Velocity: " + cell.Velocity);
                    UnityEngine.Debug.Log("Acceleration: " + cell.Acceleration);
                }*/

                inst.DebugEverything(cell.ID);

                #endregion

                inst.CellList[index] = cell;
            }
        }

        [BurstCompile]
        public partial struct AirCellInterpolationJob : IJob
        {
            public float FixedDeltaTime;
            public InverseDistanceWeighting interp;
            public NativeArray<bool> isInInterp;
            public AirCellPhysicsSystem inst;

            [BurstCompile]
            public void Execute()
            {
                #region Interpolation

                NativeArray<float> v = new(6, Allocator.Temp);

                interp.BeginInterpolation(flags.FollowDrone);

                if (flags.InterpolationWithTerrain)
                {
                    v[0] = 0;
                    v[1] = 0;
                    v[2] = 0;
                    v[3] = sim.MoleTest;
                    v[4] = inst.env.AverageLocalTemp;
                    v[5] = 0; /* Dynamic Volume Should Supposedly Go Here But Still Haven't Found A Use For It (TM) */

                    interp.InterpolationStep(flags.FollowDrone ? float3.zero : new float3(interp.Query.x, 0, interp.Query.z), v);
                }

                for (int i = 0; i < group.CellGroupNumber; i++)
                {
                    if (isInInterp[i])
                    {
                        AirCell interpCell = inst.CellList[i];

                        v[0] = interpCell.Velocity.x * sim.TimeScale;
                        v[1] = interpCell.Velocity.y * sim.TimeScale;
                        v[2] = interpCell.Velocity.z * sim.TimeScale;
                        v[3] = interpCell.Moles;
                        v[4] = interpCell.Temperature;
                        v[5] = 0; /* Dynamic Volume Should Supposedly Go Here But Still Haven't Found A Use For It (TM) */

                        interp.InterpolationStep(interpCell.CellCenter, v);
                    }
                }

                if (!interp.BroadcastInterpolation(flags.InterpolationWithTerrain))
                {
                    int minI = 0;
                    float minD = math.INFINITY;

                    for (int i = 0; i < group.CellGroupNumber; i++)
                    {
                        AirCell interpCell = inst.CellList[i];

                        if (minD > math.length(interp.Query - interpCell.CellCenter))
                        {
                            minD = math.length(interp.Query - interpCell.CellCenter);
                            minI = i;
                        }
                    }

                    AirCell closestCell = inst.CellList[minI];

                    v[0] = closestCell.Velocity.x * sim.TimeScale;
                    v[1] = closestCell.Velocity.y * sim.TimeScale;
                    v[2] = closestCell.Velocity.z * sim.TimeScale;
                    v[3] = closestCell.Moles;
                    v[4] = closestCell.Temperature;
                    v[5] = 0; /* Dynamic Volume Should Supposedly Go Here But Still Haven't Found A Use For It (TM) */

                    interp.GetClosestCell(v);
                }

                #endregion
            }
        }

        #endregion
    }
}
