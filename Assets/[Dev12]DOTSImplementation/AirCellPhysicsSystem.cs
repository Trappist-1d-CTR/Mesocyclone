using Mesocyclone.Data;
using System.Diagnostics;
using System.Drawing.Printing;
using Unity.Burst;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Entities;
using Unity.Entities.UniversalDelegates;
using Unity.Jobs;
using Unity.Mathematics;
using Unity.Physics;
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

        public static readonly AirCellGroup group = new() { CellGroupNumber = 100 };
        public static readonly AirCellBounds bounds = new() { Value = new(10000, 2000) };
        public static readonly AirCellSimulation sim = new()
        {
            TimeScale = 1,
            DronePosition = float3.zero,
            CdTest = 0.6f,
            MoleTest = GlobalData.Data.Gale.AtmPressure * 1000000f * bounds.Value.y / (GlobalData.Data.Gale.Radius * GlobalData.Data.Gale.SurfTemp * group.CellGroupNumber),
            TempTest = 1500,
            VelTest = float3.zero,
            CenterTest = float3.zero
        };
        public static readonly AirCellBehaviourFlags flags = new()
        {
            AirCellsVisible = true,
            FollowDrone = true,
            InterpolationWithTerrain = true,
            TerrainAtSeaLevel = false
        };

        #endregion

        #region Interpolation

        public InterpolationIDW interpolation;
        Entity _entity;

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
                    /*x = (5f * bounds.Value.x / 12f) * ((i % 3) - 1),
                    y = ((2f * bounds.Value.y / 7f) * (i / 9)) + (3f * bounds.Value.x / 14f),
                    z = (5f * bounds.Value.x / 12f) * (((i / 3) % 3) - 1)*/
                    x = UnityEngine.Random.Range(-bounds.Value.x, bounds.Value.x),
                    y = UnityEngine.Random.Range(1, bounds.Value.y),
                    z = UnityEngine.Random.Range(-bounds.Value.x, bounds.Value.x)
                };

                Unity.Mathematics.Random RandomValue = Unity.Mathematics.Random.CreateFromIndex(1);

                cell.CellCenter = InstantiateLocation;
                //UnityEngine.Debug.Log($"Cell Instantiation Position: {InstantiateLocation}\nCell Index: {i}");
                cell.Moles = sim.MoleTest;
                cell.Temperature = sim.TempTest + (((RandomValue.NextFloat() * 2f) - 1f) * 25f);
                cell.Velocity = sim.VelTest + (((RandomValue.NextFloat3() * 2f) - 1f) * 10f);

                CellList[i] = cell;
                GeometryList[i] = new();
            }

            #endregion

            #region Get Interpolation

            interpolation = new()
            {
                R = 10000,
                Query = 0,
                Values = new(6, Allocator.Persistent)
            };
            _entity = state.EntityManager.CreateEntity(typeof(InterpolationValues));

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

            interpolation.Values.Dispose();

            #endregion
        }

        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            #region Starting Update Setup

            //UnityEngine.Debug.Log($"{interpolation.Query}");

            env = new() { AverageLocalTemp = 0, AverageLocalWind = 0 };
            for (int i = 0; i < group.CellGroupNumber; i++)
            {
                env.AverageLocalTemp += CellList[i].Temperature / group.CellGroupNumber;
                env.AverageLocalWind += CellList[i].Velocity / group.CellGroupNumber;
            }
            som.CellRepulsion.Clear();

            float dt = SystemAPI.Time.DeltaTime * sim.TimeScale;

            #endregion

            #region Schedule and Complete Jobs

            interpolation.Query = SystemAPI.GetSingleton<InterpolationQuery>().Query;

            //UnityEngine.Debug.Log($"Initial: {CellList[0].Velocity}");

            AirCellValuesSetupJob SetupJob = new()
            {
                FixedDeltaTime = dt,
                inst = this
            };
            state.Dependency = SetupJob.Schedule(group.CellGroupNumber, 5, state.Dependency);

            //UnityEngine.Debug.Log("Starting Setup Job");
            state.Dependency.Complete();
            SetStructValues(SetupJob.inst);
            //UnityEngine.Debug.Log("Finished Setup Job");

            som = SetupJob.inst.som;

            //UnityEngine.Debug.Log($"Starting Setup: {CellList[0].Velocity}");

            AirCellPhysics1Job Physics1Job = new()
            {
                FixedDeltaTime = dt,
                inst = this
            };
            state.Dependency = Physics1Job.Schedule(group.CellGroupNumber, 5, state.Dependency);

            //UnityEngine.Debug.Log("Starting Physics 1 Job");
            state.Dependency.Complete();
            SetStructValues(Physics1Job.inst);
            //UnityEngine.Debug.Log("Finished Physics 1 Job");

            //UnityEngine.Debug.Log($"Physics 1: {CellList[0].Velocity}");

            AirCellTerrainRepulsionJob TerrainRepulsionJob = new()
            {
                FixedDeltaTime = dt,
                physicsWorld = SystemAPI.GetSingleton<PhysicsWorldSingleton>(),
                inst = this
            };
            state.Dependency = TerrainRepulsionJob.Schedule(state.Dependency);

            //UnityEngine.Debug.Log("Starting Terrain Repulsion Job");
            state.Dependency.Complete();
            SetStructValues(TerrainRepulsionJob.inst);
            //UnityEngine.Debug.Log("Finished Terrain Repulsion Job");

            //UnityEngine.Debug.Log($"Terrain Repulsion: {CellList[0].Velocity}");

            AirCellRepulsionPhysicsJob PhysicsRepulsionJob = new()
            {
                FixedDeltaTime = dt,
                ROCellList = new NativeArray<AirCell>(CellList, Allocator.TempJob),
                ROGeoList = new NativeArray<AirCellGeometry>(GeometryList, Allocator.TempJob),
                inst = this
            };
            state.Dependency = PhysicsRepulsionJob.Schedule(group.CellGroupNumber, 5, state.Dependency);

            //UnityEngine.Debug.Log("Starting Physics Repulsion Job");
            state.Dependency.Complete();
            PhysicsRepulsionJob.ROCellList.Dispose();
            PhysicsRepulsionJob.ROGeoList.Dispose();
            SetStructValues(PhysicsRepulsionJob.inst);
            //UnityEngine.Debug.Log("Finished Physics Repulsion Job");

            //UnityEngine.Debug.Log($"Physics Repulsion: {CellList[0].Velocity}");

            AirCellPhysics2Job Physics2Job = new()
            {
                FixedDeltaTime = dt,
                inst = this
            };
            state.Dependency = Physics2Job.Schedule(group.CellGroupNumber, 5, state.Dependency);

            //UnityEngine.Debug.Log("Starting Physics 2 Job");
            state.Dependency.Complete();
            SetStructValues(Physics2Job.inst);
            //UnityEngine.Debug.Log("Finished Physics 2 Job");

            //UnityEngine.Debug.Log($"Physics 2 (final): {CellList[0].Velocity}");

            AirCellInterpolationJob InterpolationJob = new()
            {
                FixedDeltaTime = dt,
                inst = this
            };
            state.Dependency = InterpolationJob.Schedule(state.Dependency);

            //UnityEngine.Debug.Log("Starting Interpolation Job");
            state.Dependency.Complete();
            SetStructValues(InterpolationJob.inst);
            //UnityEngine.Debug.Log("Finished Interpolation Job");

            SystemAPI.SetSingleton<InterpolationValues>(new() { Values = interpolation.Values });

            #endregion
        }

        [BurstCompile]
        public void SetStructValues(AirCellPhysicsSystem RefInstance)
        {
            CellList = RefInstance.CellList;
            GeometryList = RefInstance.GeometryList;

            env = RefInstance.env;
            som = RefInstance.som;

            interpolation = RefInstance.interpolation;
        }

        public void SetQuery(float3 pos)
        {
            interpolation.Query = pos;
        }

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

            //UnityEngine.Debug.Log($"Cell {i} with center {c.CellCenter}");

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
            [NativeDisableUnsafePtrRestriction]
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

                #region Calculate Static Pressure
                inst.som.StaticPressure[index] = GlobalCalc.StaticPressureAtHeight(cell.CellCenter.y);
                #endregion

                inst.DebugEverything(index);

                #region Insolation
                if (inst.som.Temp[cell.ID] == 0) inst.som.Temp[cell.ID] = cell.Temperature;
                else cell.Temperature = inst.som.Temp[cell.ID];
                cell.Temperature += mem = GlobalData.Data.Gale.Insolation * geo.CellCircleArea / (GlobalData.Data.AtmHeatCp * GlobalData.Data.Gale.AtmMM * cell.Moles) * FixedDeltaTime;
                #endregion

                inst.DebugEverything(index);

                #region Radiative Cooling
                mem = -GlobalData.Const.StefBoltz * GlobalData.Data.AtmSpecificEmissivity * ((2f * geo.CellCircleArea) + (2f * math.PI * geo.CellRadius * geo.CellHeight)) * math.pow(cell.Temperature, 4f) * FixedDeltaTime;
                //cell.Temperature += mem;
                #endregion

                inst.DebugEverything(index);

                #region Calculate Static Volume

                geo.SetSizeV(cell.Moles * GlobalData.Const.R * cell.Temperature / inst.som.StaticPressure[index]);
                if (inst.som.PrevStatVolume[index] == 0)
                {
                    inst.som.PrevStatVolume[index] = geo.CellStaticVolume;
                }
                inst.som.DynVolume[index] = geo.CellStaticVolume;

                #endregion

                inst.DebugEverything(index);

                #region Static Adiabatic Temperature Changes

                cell.Temperature *= math.pow(inst.som.PrevStatVolume[index] / geo.CellStaticVolume, GlobalData.Const.R / GlobalData.Data.MolarHeatCapacity);
                inst.som.PrevStatVolume[index] = geo.CellStaticVolume;
                /*
                if (math.abs(inst.som.Temp[index - cell.Temperature) > 10)
                {
                    UnityEngine.Debug.Log("Heavy Abiatic Temperature Change [" + cell.ID + "] ; inst.som = " + inst.som.Temp[index] + " ; Temp = " + cell.Temperature);
                }*/

                inst.som.Temp[index] = cell.Temperature;

                #endregion

                inst.DebugEverything(index);

                #endregion

                inst.CellList[index] = cell;
                inst.GeometryList[index] = geo;
            }
        }

        [BurstCompile]
        public partial struct AirCellPhysics1Job : IJobParallelFor
        {
            public float FixedDeltaTime;
            [NativeDisableUnsafePtrRestriction]
            public AirCellPhysicsSystem inst;

            [BurstCompile]
            public void Execute(int index)
            {
                AirCell cell = inst.CellList[index];
                AirCellGeometry geo = inst.GeometryList[index];

                #region Air Cell Physics

                #region Perform Gravity and Buoyancy
                cell.PerformAcceleration(new float3(0, GlobalData.Data.Gale.SurfGravity * ((cell.Temperature / inst.env.AverageLocalTemp) - 1f), 0), FixedDeltaTime);
                #endregion

                #region Perform Air Cell Drag
                cell.PerformAcceleration(-math.sign(cell.Velocity.x) * new float3(sim.CdTest * math.pow(cell.Velocity.x - inst.env.AverageLocalWind.x, 2f) / (4f * geo.CellRadius),
                    -math.sign(cell.Velocity.y) * sim.CdTest * math.pow(cell.Velocity.y - inst.env.AverageLocalWind.y, 2f) / (2f * geo.CellHeight),
                    -math.sign(cell.Velocity.z) * sim.CdTest * math.pow(cell.Velocity.z - inst.env.AverageLocalWind.z, 2f) / (4f * geo.CellRadius)), FixedDeltaTime);
                #endregion

                #region Check for Terrain Collision - to do: improve with bouncing
                if (cell.CellCenter.y <= (-geo.CellHeight / 2f))
                {
                    cell.CellCenter = new float3(cell.CellCenter.x, (-geo.CellHeight / 2f) + 0.2f, cell.CellCenter.z);
                }
                #endregion

                #region Keep Within Boundaries - note: for testing purposes

                if (math.abs(cell.CellCenter.x) >= bounds.Value.x + 0.1f)
                {
                    cell.Velocity += new float3(-math.sign(cell.CellCenter.x) * 50f * FixedDeltaTime, 0f, 0f);
                }

                if (math.abs(cell.CellCenter.z) >= bounds.Value.x + 0.1f)
                {
                    cell.Velocity += new float3(0, 0, -math.sign(cell.CellCenter.z) * 50f * FixedDeltaTime);
                }

                if (cell.CellCenter.y >= bounds.Value.y + 0.1)
                {
                    cell.Velocity += new float3(0, -math.sign(cell.CellCenter.y) * 50 * FixedDeltaTime, 0);
                }

                #endregion

                #region Air Cell Terrain Repulsion

                if (flags.TerrainAtSeaLevel && cell.CellCenter.y < geo.CellHeight / 2f)
                {
                    inst.som.DynVolume[index] *= 0.5f + (cell.CellCenter.y / geo.CellHeight);
                    cell.PerformAcceleration(new float3(0, inst.som.StaticPressure[index] * geo.CellCircleArea * (math.pow(geo.CellStaticVolume / inst.som.DynVolume[index],
                        1f + (GlobalData.Data.MolarHeatCapacity / GlobalData.Const.R)) - 1f) / (cell.Moles * GlobalData.Data.Gale.AtmMM), 0), FixedDeltaTime);
                }
                #endregion

                #endregion

                inst.CellList[index] = cell;
                inst.GeometryList[index] = geo;
            }
        }

        [BurstCompile]
        public partial struct AirCellTerrainRepulsionJob : IJob
        {
            public float FixedDeltaTime;
            [ReadOnly]
            public PhysicsWorldSingleton physicsWorld;
            [NativeDisableUnsafePtrRestriction]
            public AirCellPhysicsSystem inst;

            [BurstCompile]
            public void Execute()
            {
                for (int index = 0; index < group.CellGroupNumber; index++)
                {
                    AirCell cell = inst.CellList[index];
                    AirCellGeometry geo = inst.GeometryList[index];

                    #region Cell-Terrain Repulsion

                    inst.DebugEverything(index);

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
                                inst.som.DynVolume[index] *= 0.5f + (d3 / geo.CellHeight);
                                cell.PerformAcceleration(new float3(0, inst.som.StaticPressure[index] * geo.CellCircleArea * (math.pow(geo.CellStaticVolume / inst.som.DynVolume[index],
                                    1f + (GlobalData.Data.MolarHeatCapacity / GlobalData.Const.R)) - 1f) / (cell.Moles * GlobalData.Data.Gale.AtmMM), 0), FixedDeltaTime);
                            }
                        }
                    }

                    inst.DebugEverything(index);

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
            [ReadOnly]
            public NativeArray<AirCell> ROCellList;
            [ReadOnly]
            public NativeArray<AirCellGeometry> ROGeoList;

            [NativeDisableUnsafePtrRestriction]
            public AirCellPhysicsSystem inst;

            [BurstCompile]
            public void Execute(int index)
            {
                for (int i2 = 0; i2 < group.CellGroupNumber; i2++)
                {
                    if (index != i2)
                    {
                        AirCell cell = inst.CellList[index];
                        AirCell cell2 = ROCellList[i2];
                        AirCellGeometry geo = inst.GeometryList[index];
                        AirCellGeometry geo2 = ROGeoList[i2];

                        #region Calculate Inter-Cell Repulsion Forces

                        float d, r1, r2, d1, d2, A, h;

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

                            if (h * A > 0 && inst.som.DynVolume[index] > 0 && inst.som.DynVolume[i2] > 0)
                            {
                                inst.som.DynVolume[index] = SafeValue(inst.som.DynVolume[index] - (A * h / 2f));

                                #region Repulsion Physics

                                float mag = inst.som.StaticPressure[index] * math.pow(A * h, 2f / 3f) * (math.pow(geo.CellStaticVolume / inst.som.DynVolume[index],
                                        1f + (GlobalData.Data.MolarHeatCapacity / GlobalData.Const.R)) - 1f) / (cell.Moles * GlobalData.Data.Gale.AtmMM);

                                cell.PerformAcceleration(mag * math.normalize(cell.CellCenter - cell2.CellCenter), FixedDeltaTime);

                                #endregion
                            }
                            else
                            {
                                inst.som.DynVolume[index] = SafeValue(inst.som.DynVolume[index]);
                            }

                            inst.DebugEverything(index);
                        }

                        #endregion

                        inst.CellList[index] = cell;
                        inst.GeometryList[index] = geo;
                    }
                }
            }
        }

        #region Old Cell Repulsion Physics Job
        /*
        [BurstCompile]
        public partial struct AirCellRepulsionPhysicsJob : IJob
        {
            public float FixedDeltaTime;

            [NativeDisableUnsafePtrRestriction]
            public AirCellPhysicsSystem inst;

            [BurstCompile]
            public void Execute()
            {
                for (int index = 0; index < group.CellGroupNumber; index++)
                {
                    AirCell cell = inst.CellList[index];
                    AirCellGeometry geo = inst.GeometryList[index];

                    #region Calculate Inter-Cell Repulsion Forces

                    float d, r1, r2, d1, d2, A, h;

                    for (int i2 = index + 1; i2 < group.CellGroupNumber; i2++)
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

                            if (h * A > 0 && inst.som.DynVolume[index] > 0 && inst.som.DynVolume[i2] > 0)
                            {
                                inst.som.DynVolume[index] = SafeValue(inst.som.DynVolume[index] - (A * h / 2f));
                                inst.som.DynVolume[i2] = SafeValue(inst.som.DynVolume[i2] - (A * h / 2f));

                                inst.som.CellRepulsion.Add(new float3(index, i2, A * h));
                            }
                            else
                            {
                                inst.som.DynVolume[index] = SafeValue(inst.som.DynVolume[index]);
                                inst.som.DynVolume[i2] = SafeValue(inst.som.DynVolume[i2]);
                            }

                            inst.DebugEverything(index);
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

                for (int index = 0; index < inst.som.CellRepulsion.Length; index++)
                {
                    float3 Repulsion = inst.som.CellRepulsion[index];

                    int i1 = (int)Repulsion.x;
                    int i2 = (int)Repulsion.y;

                    AirCell repulCell = inst.CellList[i1];
                    AirCell repul2Cell = inst.CellList[i2];
                    AirCellGeometry repulGeo = inst.GeometryList[i1];
                    AirCellGeometry repul2Geo = inst.GeometryList[i2];

                    #region Repulsion Physics

                    float mag = inst.som.StaticPressure[i1] * math.pow(Repulsion.z, 2f / 3f) * (math.pow(repulGeo.CellStaticVolume / inst.som.DynVolume[i1],
                            1f + (GlobalData.Data.MolarHeatCapacity / GlobalData.Const.R)) - 1f) / (repulCell.Moles * GlobalData.Data.Gale.AtmMM);

                    inst.PerformAcceleration(repulCell.ID, mag * math.normalize(repulCell.CellCenter - repul2Cell.CellCenter), FixedDeltaTime);

                    mag = inst.som.StaticPressure[i2] * math.pow(Repulsion.z, 2f / 3f) * (math.pow(repul2Geo.CellStaticVolume / inst.som.DynVolume[i2],
                        1f + (GlobalData.Data.MolarHeatCapacity / GlobalData.Const.R)) - 1f) / (repul2Cell.Moles * GlobalData.Data.Gale.AtmMM);

                    inst.PerformAcceleration(repul2Cell.ID, mag * math.normalize(repul2Cell.CellCenter - repulCell.CellCenter), FixedDeltaTime);

                    #endregion

                    inst.CellList[i1] = repulCell;
                    inst.CellList[i2] = repul2Cell;
                    inst.GeometryList[i1] = repulGeo;
                    inst.GeometryList[i2] = repul2Geo;
                }
            }
        }
        */
        #endregion

        [BurstCompile]
        public partial struct AirCellPhysics2Job : IJobParallelFor
        {
            public float FixedDeltaTime;
            [NativeDisableUnsafePtrRestriction]
            public AirCellPhysicsSystem inst;

            [BurstCompile]
            public void Execute(int index)
            {
                AirCell cell = inst.CellList[index];

                #region Perform Physics

                inst.DebugEverything(index);

                #region Dynamic Abiatic Temperature Change

                inst.som.DynVolume[index] = SafeValue(inst.som.DynVolume[index]);
                if (inst.som.PrevDynVolume[index] == 0) inst.som.PrevDynVolume[index] = inst.som.DynVolume[index];
                cell.Temperature *= math.pow(inst.som.PrevDynVolume[index] / inst.som.DynVolume[index], GlobalData.Const.R / GlobalData.Data.MolarHeatCapacity);
                inst.som.PrevDynVolume = inst.som.DynVolume;

                #endregion

                cell.PerformVelocity(FixedDeltaTime);

                //UnityEngine.Debug.Log($"Cell position: {cell.CellCenter}\nCell index: {index}");

                inst.DebugEverything(index);

                //To visualize position
                if (flags.AirCellsVisible)
                {
                    UnityEngine.Debug.DrawLine(UnityEngine.Vector3.zero,
                        new UnityEngine.Vector3(cell.CellCenter.x, cell.CellCenter.y, cell.CellCenter.z),
                        UnityEngine.Color.blueViolet, math.EPSILON);
                    //UnityEngine.Debug.Log($"Air Cell {index} at position {cell.CellCenter}");
                }

                /*
                if (i == 0)
                {
                    UnityEngine.Debug.Log("");
                    UnityEngine.Debug.Log("Velocity: " + cell.Velocity);
                    UnityEngine.Debug.Log("Acceleration: " + cell.Acceleration);
                }*/

                inst.DebugEverything(index);

                #endregion

                inst.CellList[index] = cell;
            }
        }

        [BurstCompile]
        public partial struct AirCellInterpolationJob : IJob
        {
            public float FixedDeltaTime;
            [NativeDisableUnsafePtrRestriction]
            public AirCellPhysicsSystem inst;

            [BurstCompile]
            public void Execute()
            {
                #region Interpolation

                NativeArray<float> Sum_wu = new(6, Allocator.Temp);
                for (int i = 0; i < 6; i++)
                { Sum_wu[i] = 0; }
                float Sum_w = 0;

                float d, wi;

                if (flags.InterpolationWithTerrain)
                {
                    d = math.abs(inst.interpolation.Query.y);
                    wi = math.square(math.max(0, inst.interpolation.R - d) / (inst.interpolation.R * d));

                    Sum_wu[0] += 0;
                    Sum_wu[1] += 0;
                    Sum_wu[2] += 0;
                    Sum_wu[3] += wi * sim.MoleTest;
                    Sum_wu[4] += wi * inst.env.AverageLocalTemp;
                    Sum_w += wi;
                }

                for (int i = 0; i < group.CellGroupNumber; i++)
                {
                    if (!flags.FollowDrone)
                        d = math.distance(inst.interpolation.Query, inst.CellList[i].CellCenter);
                    else
                        d = math.distance(new(0, inst.interpolation.Query.y, 0), inst.CellList[i].CellCenter);

                    if (d == 0)
                    {
                        inst.interpolation.Values[0] = inst.CellList[i].Velocity.x;
                        inst.interpolation.Values[1] = inst.CellList[i].Velocity.y;
                        inst.interpolation.Values[2] = inst.CellList[i].Velocity.z;
                        inst.interpolation.Values[3] = inst.CellList[i].Moles;
                        inst.interpolation.Values[4] = inst.CellList[i].Temperature;
                        inst.interpolation.Values[5] = 0; /*Dynamic Volume Should Supposedly Go Here Once We Find A Use For It(TM)*/

                        return;
                    }
                    else
                    {
                        //float wi = math.pow(d, -2);
                        wi = math.square(math.max(0, inst.interpolation.R - d) / (inst.interpolation.R * d));

                        Sum_wu[0] += wi * inst.CellList[i].Velocity.x;
                        Sum_wu[1] += wi * inst.CellList[i].Velocity.y;
                        Sum_wu[2] += wi * inst.CellList[i].Velocity.z;
                        Sum_wu[3] += wi * inst.CellList[i].Moles;
                        Sum_wu[4] += wi * inst.CellList[i].Temperature;
                        Sum_w += wi;
                    }
                }

                for (int i = 0; i < 6; i++)
                {
                    inst.interpolation.Values[i] = Sum_wu[i] / Sum_w;
                }

                #endregion
            }
        }

        #endregion
    }
}
