using System.Collections.Generic;
using System.Diagnostics;
using UnityEngine;

namespace Mesocyclone
{
    // same here
    public sealed class AirCellBehavior : MonoBehaviour
    {
        #region Variables

        #region Bool Settings
        public bool FollowDrone;
        public bool AirCellObjects;
        public bool TerrainAtSeaLevel;
        public bool InterpolationWithTerrain;
        #endregion

        #region Cell Fundamentals
        private AirCell AirCellTest;
        private List<AirCell> AirCellGroup = new();
        private List<GameObject> CellObjectGroup = new();

        public GameObject CubeObject;
        #endregion

        #region Default Values
        public Vector3 CenterTest;
        public Vector2 AirCellsBounds;
        public List<Vector3> AirCellStartingGrid;
        public float MoleTest;
        public float TempTest;
        public Vector3 VelTest;
        public float AverageLocalTemp;
        public Vector3 AverageLocalWind;
        public int CellGroupNumber;
        #endregion

        #region Simulation Testing
        //public float[] InterpolatedValues = new float[6];
        public Vector3 DronePosition;
        public float CdTest;
        public float DistanceScale;
        public float GravityScale;
        public float SetTimeScale;
        public static float TimeScale;
        #endregion

        #region Locational Info
        public float LocalLatitude;
        public int iLocal;
        public float AmbientHeat;
        public AnimationCurve AmbientalHeat;
        #endregion

        #region Reference Objects
        private AGlobalValues C;
        #endregion

        #region Script Optimization Memory
        private float[] StaticPressureSOM;
        private float[] TempSOM;
        private float[] prevStatVolumeSOM;
        private float[] DynVolumeSOM;
        private float[] prevDynVolumeSOM;
        private List<Vector3> CellsRepulsionSOM;
        #endregion

        public bool CellsInstantiated = false;

        #endregion

        // int iy = 0;

        private void Awake()
        {
            // GameObject reference
            GameObject controller = GameObject.FindGameObjectWithTag("GameController");
            if (controller == null)
            {
                UnityEngine.Debug.LogError("No GameObject with tag GameController");
                return;
            }

            // component reference
            C = controller.GetComponent<AGlobalValues>();
            if (C == null)
            {
                UnityEngine.Debug.LogError("No 'AGlobalValues' Component attached to controller object");
                return;
            }

            TimeScale = SetTimeScale;

            CellsRepulsionSOM = new List<Vector3>();
            CellsInstantiated = false;

            InverseDistanceWeighting.FollowDrone = FollowDrone;
        }

        private void FixedUpdate()
        {
            // if the current time scale is 0, we don't want to run the simulation, as it will cause errors and unnecessary calculations
            if (Time.timeScale == 0f) return;

            if (!CellsInstantiated)
            {
                if (C.SetupComplete)
                {
                    #region Instantiate Air Cells
                    float l = Mathf.Floor(Mathf.Pow(CellGroupNumber, (1f / 3f)));
                    float b = AirCellsBounds.x;
                    float h = AirCellsBounds.y;
                    MoleTest = C.GaleAtm * 1000000f * h / (C.R * C.GaleAvgTemp * CellGroupNumber);
                    for (int i = 0; i < CellGroupNumber; i++)
                    {
                        Vector3 InstantiateLocation = Vector3.zero;

                        InstantiateLocation.x = (5 * b / 12) * ((i % 3) - 1);
                        InstantiateLocation.y = ((2 * h / 7) * ((int)i / (int)9)) + (3 * b / 14);
                        InstantiateLocation.z = (5 * b / 12) * ((((int)i / (int)3) % 3) - 1);

                        //InstantiateLocation.x = ((b / 4) * (i % 4)) - (3 * b / 8);
                        //InstantiateLocation.y = ((b / 4) * ((int)i / (int)16)) + (b / 8);
                        //InstantiateLocation.z = ((b / 4) * (((int)i / (int)4) % 4)) - (3 * b / 8);

                        /*if (i < Mathf.Pow(l, 3))
                        {
                            InstantiateLocation = CenterTest + new Vector3(-(b / 2f) + (b / (l * 2f)) + (b * (i % l) / l), 200f + ((h - 400f) * (Mathf.Floor(i / (l * l)) / l)), (-b / 2f) + (b / (l * 2f)) + (b * Mathf.Floor((i % (l * l)) / l) / l));
                        }
                        else
                        {
                            InstantiateLocation = CenterTest + new Vector3(Random.Range(-((b / 2) + 50), (b / 2) - 50), b - 50, Random.Range(-((b / 2) + 50), (b / 2) - 50));
                        }*/

                        if (AirCellObjects) CellObjectGroup.Add(Instantiate(CubeObject, Vector3.zero, new Quaternion(), gameObject.transform));
                        AirCellGroup.Add(new(InstantiateLocation, MoleTest, TempTest + Random.Range(-25f, 25f), VelTest + new Vector3(Random.Range(-10f, 10f), Random.Range(-10f, 10f), Random.Range(-10f, 10f)), C.StiffK));
                        if (AirCellObjects) CellObjectGroup[i].transform.position = AirCellGroup[i].CellCenter;
                    }
                    #endregion

                    #region SOMs Setup
                    prevStatVolumeSOM = new float[CellGroupNumber];
                    DynVolumeSOM = new float[CellGroupNumber];
                    prevDynVolumeSOM = new float[CellGroupNumber];

                    StaticPressureSOM = new float[CellGroupNumber];
                    TempSOM = new float[CellGroupNumber];

                    for (int i = 0; i < CellGroupNumber; i++)
                    {
                        TempSOM[i] = AirCellGroup[i].Temperature;
                    }
                    #endregion

                    #region iLocal Calculation
                    iLocal = (int)System.MathF.Round(((LocalLatitude / (180 / C.EOS_N)) - (90 / C.EOS_N)));
                    iLocal = (iLocal >= 0) ? ((iLocal < C.EOS_N) ? iLocal : C.EOS_N) : 0;
                    #endregion

                    CellsInstantiated = true;
                }
            }
            else
            {
                // iy++;

                /* if (iy == 100)
                {
                    Debug.Log("");
                    iy = 0;
                }
                */

                float dt = Time.fixedDeltaTime * TimeScale;

                #region Average Local Values
                AverageLocalTemp = 0;
                for (int i = 0; i < CellGroupNumber; i++)
                {
                    AverageLocalTemp += AirCellGroup[i].Temperature / CellGroupNumber;
                }

                AverageLocalWind = Vector3.zero;
                for (int i = 0; i < CellGroupNumber; i++)
                {
                    AverageLocalWind += AirCellGroup[i].Velocity / CellGroupNumber;
                }
                #endregion

                #region Values Setup

                for (int i = 0; i < CellGroupNumber; i++)
                {
                    //AirCellGroup[i].Accumulator += dt;
                    //float interval = 1f / AirCellGroup[i].TickRate;

                    //while (AirCellGroup[i].Accumulator >= interval) { }

                    if (i == 0)
                    {
                        //Debug.Log("");
                        //Debug.Log("Velocity: " + AirCellGroup[i].Velocity);
                        //Debug.Log("Acceleration: " + AirCellGroup[i].Acceleration);
                    }

                    DebugEverything(i);

                    #region Calculate Static Pressure
                    StaticPressureSOM[i] = C.StaticPressureAtHeight(AirCellGroup[i].CellCenter.y);
                    #endregion

                    DebugEverything(i);

                    // double mem; // ...he glazes afar into the distance, as he realizes he is amongst the only double left...
                    float mem; // nevermind

                    #region Insolation
                    AirCellGroup[i].Temperature = TempSOM[i];
                    AirCellGroup[i].Temperature += mem = C.GlobalInsolation.Evaluate(LocalLatitude) * C.ClearSkyTransparency.Evaluate(AirCellGroup[i].CellCenter.y) * AirCellGroup[i].CellCircleArea / (C.GaleAtmCp * C.GaleAtmMM * AirCellGroup[i].Moles) * dt;
                    #endregion
                    //if (i == 0) Debug.Log("Insolation: " + mem);

                    DebugEverything(i);

                    #region Diffusion
                    AirCellGroup[i].Temperature += (C.EOS_Diffusion[iLocal] / C.EOS_AtmC) * dt;
                    #endregion
                    //if (i == 0) Debug.Log("Diffusion: " + (C.EOS_Diffusion[0] / C.EOS_AtmC));

                    DebugEverything(i);

                    #region Radiative Heating/Cooling
                    mem = (C.GreekS * C.AtmSpecificEmissivity * ((2f * AirCellGroup[i].CellCircleArea) + (2f * UnityEngine.Mathf.PI * AirCellGroup[i].CellRadius * AirCellGroup[i].CellHeight)) * System.MathF.Pow(AirCellGroup[i].Temperature, 4f) * dt);

                    /* 
                    if (i == 0)
                        Debug.Log("Radiative Cooling: " + (mem / (AirCellGroup[i].Moles * C.GaleAtmMM * C.EOS_AtmC)));
                    AirCellGroup[i].Temperature -= mem / (AirCellGroup[i].Moles * C.GaleAtmMM * C.EOS_AtmC);
                    */

                    #endregion

                    DebugEverything(i);

                    //AirCellGroup[i].Accumulator -= interval;
                }

                //   Debug.Log("Temperature: " + AirCellGroup[0].Temperature);

                #endregion

                #region Air Cell Physics
                for (int i = 0; i < CellGroupNumber; i++)
                {
                    /*
                    if (AirCellGroup[i].Velocity.y >= 10)
                    {
                        Debug.Log("Rapid Vertical Velocity [" + i + "] ; Temp = " + AirCellGroup[i].Temperature);
                    }*/

                    DebugEverything(i);

                    #region Calculate Static Volume
                    AirCellGroup[i].SetSizeV(AirCellGroup[i].Moles * C.R * AirCellGroup[i].Temperature / StaticPressureSOM[i]);
                    if (prevStatVolumeSOM[i] == 0) prevStatVolumeSOM[i] = AirCellGroup[i].CellStaticVolume;
                    DynVolumeSOM[i] = AirCellGroup[i].CellStaticVolume;
                    #endregion

                    #region Static Adiabatic Temperature Changes
                    AirCellGroup[i].Temperature *= System.MathF.Pow(prevStatVolumeSOM[i] / AirCellGroup[i].CellStaticVolume, C.R / C.MolarHeatCapacity);
                    prevStatVolumeSOM[i] = AirCellGroup[i].CellStaticVolume;
                    /*
                    if (TempSOM[i] - AirCellGroup[i].Temperature > 10)
                    {
                        UnityEngine.Debug.Log("Heavy Abiatic Temperature Change [" + i + "] ; SOM = " + TempSOM[i] + " ; Temp = " + AirCellGroup[i].Temperature);
                    }*/

                    TempSOM[i] = AirCellGroup[i].Temperature;
                    #endregion

                    DebugEverything(i);

                    #region Air Cell Terrain Repulsion
                    if (TerrainAtSeaLevel && AirCellGroup[i].CellCenter.y < AirCellGroup[i].CellHeight / 2)
                    {
                        DynVolumeSOM[i] *= 0.5f + (AirCellGroup[i].CellCenter.y / AirCellGroup[i].CellHeight);
                        AirCellGroup[i].PerformAcceleration((StaticPressureSOM[i] * AirCellGroup[i].CellCircleArea * (System.MathF.Pow(AirCellGroup[i].CellStaticVolume / DynVolumeSOM[i], (1f + (C.MolarHeatCapacity / C.R))) - 1f) / (AirCellGroup[i].Moles * C.GaleAtmMM)) * Vector3.up);

                        //if (TempSOM[i] - AirCellGroup[i].Temperature > 1) Debug.Log("Heavy Terrain Rep. Temperature Change [" + i + "] ; SOM = " + TempSOM[i] + " ; Temp = " + AirCellGroup[i].Temperature);
                        //TempSOM[i] = AirCellGroup[i].Temperature;
                        //   Debug.Log((StaticPressureSOM[i] * AirCellGroup[i].CellCircleArea * (System.MathF.Pow(AirCellGroup[i].CellVolume / prevVolumeSOM[i], (1f + (C.MolarHeatCapacity / C.R))) - 1f) / (AirCellGroup[i].Moles * C.GaleAtmMM)));
                    }
                    #endregion

                    DebugEverything(i);

                    #region Perform Gravity and Buoyancy
                    AirCellGroup[i].PerformAcceleration((C.GaleG * ((AirCellGroup[i].Temperature / AverageLocalTemp) - 1f)) * Vector3.up);
                    #endregion

                    DebugEverything(i);

                    #region Perform Air Cell Drag

                    AirCellGroup[i].PerformAcceleration(CdTest * Mathf.Pow(AirCellGroup[i].Velocity.y - AverageLocalWind.y, 2f) / (2f * AirCellGroup[i].CellHeight) * -Vector3.Project(AirCellGroup[i].Velocity - AverageLocalWind, Vector3.up).normalized);
                    AirCellGroup[i].PerformAcceleration(CdTest * Vector3.ProjectOnPlane(AirCellGroup[i].Velocity - AverageLocalWind, Vector3.up).sqrMagnitude / (4f * AirCellGroup[i].CellRadius) * -Vector3.ProjectOnPlane(AirCellGroup[i].Velocity - AverageLocalWind, Vector3.up).normalized);

                    #endregion

                    DebugEverything(i);

                    #region Check for Terrain Collision - to do: improve with bouncing
                    if (AirCellGroup[i].CellCenter.y <= (-AirCellGroup[i].CellHeight / 2f))
                    {
                        AirCellGroup[i].CellCenter = new Vector3(AirCellGroup[i].CellCenter.x, -AirCellGroup[i].CellHeight / 2f + 0.2f, AirCellGroup[i].CellCenter.z);
                    }
                    #endregion

                    DebugEverything(i);

                    #region Keep Within Boundaries - note: for testing purposes

                    if (Mathf.Abs(AirCellGroup[i].CellCenter.x) >= (AirCellsBounds.x / 2) + 0.1f)
                    {
                        AirCellGroup[i].Velocity += new Vector3(Mathf.Sign(AirCellGroup[i].CellCenter.x) * -50f * dt, 0f, 0f); //Vector3.Scale(AirCellGroup[i].Velocity, new Vector3(-0.1f, 1f, 1f));
                                                                                                                               //AirCellGroup[i].CellCenter = new Vector3(AirCellGroup[i].CellCenter.x > 0 ? 500f : -500f, AirCellGroup[i].CellCenter.y, AirCellGroup[i].CellCenter.z);
                    }

                    if (Mathf.Abs(AirCellGroup[i].CellCenter.z) >= (AirCellsBounds.x / 2))
                    {
                        AirCellGroup[i].Velocity += new Vector3(0, 0, Mathf.Sign(AirCellGroup[i].CellCenter.z) * -50f * dt);
                        //AirCellGroup[i].CellCenter = new Vector3(AirCellGroup[i].CellCenter.x, AirCellGroup[i].CellCenter.y, AirCellGroup[i].CellCenter.z > 0 ? 500f : -500f);
                    }

                    if (AirCellGroup[i].CellCenter.y >= AirCellsBounds.y + 0.1)
                    {
                        AirCellGroup[i].Velocity += new Vector3(0, Mathf.Sign(AirCellGroup[i].CellCenter.y) * -50 * dt, 0);
                        //AirCellGroup[i].CellCenter = new Vector3(AirCellGroup[i].CellCenter.x, C.SimulationHeight, AirCellGroup[i].CellCenter.z);
                    }

                    #endregion

                    DebugEverything(i);
                }
                #endregion

                #region Inter-Cell Repulsion
                CellsRepulsionSOM.Clear();
                float d, r1, r2, d1, d2, A, h;

                for (int i = 0; i < CellGroupNumber; i++)
                {
                    for (int i2 = i + 1; i2 < CellGroupNumber; i2++)
                    {
                        #region Check For and Calculate Overlaps
                        d = Vector2.Distance(new Vector2(AirCellGroup[i].CellCenter.x, AirCellGroup[i].CellCenter.z),
                                             new Vector2(AirCellGroup[i2].CellCenter.x, AirCellGroup[i2].CellCenter.z));
                        d = SafeValue(d); // prevent division by zero

                        if ((h = System.MathF.Abs(AirCellGroup[i].CellCenter.y - AirCellGroup[i2].CellCenter.y)) < ((AirCellGroup[i].CellHeight + AirCellGroup[i2].CellHeight) / 2f)
                            && d < (AirCellGroup[i].CellRadius + AirCellGroup[i2].CellRadius))
                        {
                            #region Cell Overlap Calculations

                            if (AirCellGroup[i].CellRadius >= AirCellGroup[i2].CellRadius)
                            {
                                r1 = AirCellGroup[i].CellRadius;
                                r2 = AirCellGroup[i2].CellRadius;
                            }
                            else
                            {
                                r1 = AirCellGroup[i2].CellRadius;
                                r2 = AirCellGroup[i].CellRadius;
                            }

                            h = System.MathF.Abs(h - ((AirCellGroup[i].CellHeight + AirCellGroup[i2].CellHeight) / 2f));

                            d1 = (Mathf.Pow(r1, 2) - Mathf.Pow(r2, 2) + Mathf.Pow(d, 2)) / (2 * d);
                            d2 = d - d1;

                            A = (Mathf.Pow(r1, 2f) * Mathf.Acos(d1 / r1)) - (d1 * Mathf.Sqrt(Mathf.Pow(r1, 2) - Mathf.Pow(d1, 2f))) + (Mathf.Pow(r2, 2f) * Mathf.Acos(d2 / r2)) - (d2 * Mathf.Sqrt(Mathf.Pow(r2, 2f) - Mathf.Pow(d2, 2f)));

                            #endregion

                            if (h * A > 0 && DynVolumeSOM[i] > 0 && DynVolumeSOM[i2] > 0)
                            {
                                DynVolumeSOM[i] = System.MathF.Max(0, DynVolumeSOM[i] - A * h / 2f);
                                DynVolumeSOM[i2] = System.MathF.Max(0, DynVolumeSOM[i2] - A * h / 2f);

                                CellsRepulsionSOM.Add(new Vector3(i, i2, A * h));
                            }
                            else
                            {
                                //Debug.Log("ah");
                            }

                            DebugEverything(i);
                            DebugEverything(i2);
                        }
                        #endregion
                    }
                }

                #region Repulsion Physics

                // GODDAMN ASTRAA, LINE SKIP YOUR CODE I CANT READ TS T-T
                for (int i = 0; i < CellsRepulsionSOM.Count; i++)
                {
                    AirCellGroup[(int)CellsRepulsionSOM[i].x].PerformAcceleration((StaticPressureSOM[(int)CellsRepulsionSOM[i].x] * System.MathF.Pow(CellsRepulsionSOM[i].z, (2f / 3f)) * (System.MathF.Pow(AirCellGroup[(int)CellsRepulsionSOM[i].x].CellStaticVolume / DynVolumeSOM[(int)CellsRepulsionSOM[i].x], (1f + (C.MolarHeatCapacity / C.R))) - 1f) / (AirCellGroup[(int)CellsRepulsionSOM[i].x].Moles * C.GaleAtmMM)) * (AirCellGroup[(int)CellsRepulsionSOM[i].x].CellCenter - AirCellGroup[(int)CellsRepulsionSOM[i].y].CellCenter).normalized);

                    AirCellGroup[(int)CellsRepulsionSOM[i].y].PerformAcceleration((StaticPressureSOM[(int)CellsRepulsionSOM[i].y] * System.MathF.Pow(CellsRepulsionSOM[i].z, (2f / 3f)) * (System.MathF.Pow(AirCellGroup[(int)CellsRepulsionSOM[i].y].CellStaticVolume / DynVolumeSOM[(int)CellsRepulsionSOM[i].y], (1f + (C.MolarHeatCapacity / C.R))) - 1f) / (AirCellGroup[(int)CellsRepulsionSOM[i].y].Moles * C.GaleAtmMM)) * (AirCellGroup[(int)CellsRepulsionSOM[i].y].CellCenter - AirCellGroup[(int)CellsRepulsionSOM[i].x].CellCenter).normalized);
                }
                #endregion

                #endregion

                #region Cell-Terrain Repulsion

                if (!TerrainAtSeaLevel)
                {
                    for (int i = 0; i < CellGroupNumber; i++)
                    {
                        float maxD = Mathf.Sqrt(Mathf.Pow(AirCellGroup[i].CellRadius, 2) + Mathf.Pow(AirCellGroup[i].CellHeight / 2f, 2f));
                        RaycastHit hit;
                        if (Physics.Raycast(AirCellGroup[i].CellCenter + (Vector3.up * AirCellGroup[i].CellHeight / 2), Vector3.down, out hit, maxD, 1 << 3))
                        {
                            float d3 = Mathf.Abs(hit.barycentricCoordinate.y - AirCellGroup[i].CellCenter.y);
                            if (d3 < AirCellGroup[i].CellHeight / 2f)
                            {
                                DynVolumeSOM[i] *= 0.5f + (d3 / AirCellGroup[i].CellHeight);
                                AirCellGroup[i].PerformAcceleration((StaticPressureSOM[i] * AirCellGroup[i].CellCircleArea * (System.MathF.Pow(AirCellGroup[i].CellStaticVolume / DynVolumeSOM[i], (1f + (C.MolarHeatCapacity / C.R))) - 1f) / (AirCellGroup[i].Moles * C.GaleAtmMM)) * Vector3.up);
                            }
                        }
                    }
                }

                #endregion

                #region Perform Physics
                for (int i = 0; i < CellGroupNumber; i++)
                {
                    DebugEverything(i);

                    #region Dynamic Abiatic Temperature Change
                    DynVolumeSOM[i] = SafeValue(DynVolumeSOM[i]);
                    if (prevDynVolumeSOM[i] == 0) prevDynVolumeSOM[i] = DynVolumeSOM[i];
                    AirCellGroup[i].Temperature *= System.MathF.Pow(prevDynVolumeSOM[i] / DynVolumeSOM[i], C.R / C.MolarHeatCapacity);
                    prevDynVolumeSOM[i] = DynVolumeSOM[i];
                    #endregion

                    AirCellGroup[i].PerformVelocity();

                    DebugEverything(i);

                    //To visualize the Cells
                    if (AirCellObjects) CellObjectGroup[i].transform.position = AirCellGroup[i].CellCenter;

                    //For interpolation
                    if (InverseDistanceWeighting.Indexes.Contains(i))
                    {
                        if (Vector3.Distance(InverseDistanceWeighting.Query, AirCellGroup[i].CellCenter) > InverseDistanceWeighting.R)
                        {
                            InverseDistanceWeighting.Remove(i);
                        }
                    }
                    else if (Vector3.Distance(InverseDistanceWeighting.Query, AirCellGroup[i].CellCenter) <= InverseDistanceWeighting.R)
                    {
                        InverseDistanceWeighting.Add(i);
                    }

                    /*
                    if (i == 0)
                    {
                        Debug.Log("");
                        Debug.Log("Velocity: " + AirCellGroup[i].Velocity);
                        Debug.Log("Acceleration: " + AirCellGroup[i].Acceleration);
                    }
                    */

                    DebugEverything(i);
                }
                #endregion

                #region Interpolation

                InverseDistanceWeighting.BeginInterp();

                if (InterpolationWithTerrain)
                {
                    InverseDistanceWeighting.InterpolationStep(FollowDrone ? Vector3.zero : new Vector3(InverseDistanceWeighting.Query.x, 0, InverseDistanceWeighting.Query.z),
                    new float[6]
                    {
                    0f, 0f, 0f, MoleTest, AverageLocalTemp, 0f
                    });
                }

                foreach (int i in InverseDistanceWeighting.Indexes)
                {
                    InverseDistanceWeighting.InterpolationStep(AirCellGroup[i].CellCenter,
                        new float[6] { AirCellGroup[i].Velocity.x * TimeScale, AirCellGroup[i].Velocity.y * TimeScale, AirCellGroup[i].Velocity.z * TimeScale, AirCellGroup[i].Moles,
                    AirCellGroup[i].Temperature, 0 /* Dynamic Volume Should Supposedly Go Here */ });
                }

                InverseDistanceWeighting.BroadcastInterp(InterpolationWithTerrain);

                #region Natural Neighbour Interpolation Attempt
                /* 
                Vertex3[] CellVertexes = new Vertex3[CellGroupNumber];
                //Interpolation Values
                if (CellGroupNumber > 4)
                {
                    CellVertexes[i] = new(AirCellGroup[i].CellCenter.x, AirCellGroup[i].CellCenter.y, AirCellGroup[i].CellCenter.z, new float[6]
                        { AirCellGroup[i].Velocity.x, AirCellGroup[i].Velocity.y, AirCellGroup[i].Velocity.z, AirCellGroup[i].Moles, AirCellGroup[i].Temperature, AirCellGroup[i].CellDynamicVolume });
                }
                InterpolatedValues = InterpolateAirCells.GetValues(CellVertexes, new(DronePosition.x, DronePosition.y, DronePosition.z, 0));
                //   Debug.Log(InterpolatedValues.Length);
                   Debug.Log("Interpolated values at Drone Position: \n   Velocity = { " + InterpolatedValues[0] + ", " + InterpolatedValues[1] + ", " + InterpolatedValues[2] + " }\n   " +
                       "Moles = " + InterpolatedValues[3] + "\n   Temperature = " + InterpolatedValues[4] + "\n   Dynamic Volume = " + InterpolatedValues[5]);
                */
                #endregion
                #endregion
            }
        }

        // conditional makes it so every call for this function is skipped, so i dont have to spam preprocessors
        [System.Diagnostics.Conditional("UNITY_EDITOR")]
        public void DebugEverything(int i)
        {
            Vector3 vel = AirCellGroup[i].Velocity;

            // Just checks the X Y or Z values to see if the cell velocity is NaN. Much cheaper than full square root calculations via magnitude checking
            if (!float.IsFinite(vel.x) || !float.IsFinite(vel.y) || !float.IsFinite(vel.z))
                UnityEngine.Debug.LogError("NaN Cell Velocity ; i = " + i);

            if (!float.IsFinite(AirCellGroup[i].CellStaticVolume))
                UnityEngine.Debug.LogError("NaN Cell Volume ; i = " + i);

            if (!float.IsFinite(AirCellGroup[i].Temperature))
                UnityEngine.Debug.LogError("NaN Temperature ; i = " + i);

            if (prevStatVolumeSOM[i] <= 0 && AirCellGroup[i].CellCenter.y < AirCellGroup[i].CellHeight / 2)
                UnityEngine.Debug.LogError("Negative/Null prevVolumeSOM ; i = " + i);

            if (AirCellGroup[i].CellCenter.y <= -AirCellGroup[i].CellHeight / 2)
                UnityEngine.Debug.LogError("ACDDC - Air Cell Digging Down to China ; i = " + i);

            if (!float.IsFinite(AirCellGroup[i].CellCenter.x) || !float.IsFinite(AirCellGroup[i].CellCenter.y) || !float.IsFinite(AirCellGroup[i].CellCenter.z))
                UnityEngine.Debug.LogError("NaN Position ; i = " + i);
        }

        float SafeValue(float value)
        {
            return Mathf.Max(value, 1e-2f);
        }
    }


    #region Discarded
    /*
     * for (int i = 0; i < 1; i++)
    {
    //Force from Other Cells
    AirCellGroup[i].PerformAcceleration((C.R * ((AirCellGroup[i].Temperature + AirCellGroup[i + 1].Temperature) / 2) / (C.GaleAtmMM * (AirCellGroup[i].CellCenter.y / DistanceScale))) * (AirCellGroup[i].CellCenter - AirCellGroup[i + 1].CellCenter).normalized * DistanceScale);
    AirCellGroup[i + 1].PerformAcceleration((C.R * ((AirCellGroup[i].Temperature + AirCellGroup[i + 1].Temperature) / 2) / (C.GaleAtmMM * (AirCellGroup[i].CellCenter.y / DistanceScale))) * (AirCellGroup[i + 1].CellCenter - AirCellGroup[i].CellCenter).normalized * DistanceScale);
    }

    #region Cell Behavior Simulation
    //Force from Terrain (y = 0)
    //(C.R * AirCellGroup[i].Temperature / (C.GaleAtmMM * AirCellGroup[i].CellCenter.y / DistanceScale));
    //float P1 = AirCellGroup[i].Moles * C.R * AirCellGroup[i].Temperature / (AirCellGroup[i].CellCenter.y / DistanceScale);
    //float P2 = C.CalculateStaticPressure(AirCellGroup[i].CellCenter.y);
    AirCellGroup[i].PerformAcceleration((C.R * AirCellGroup[i].Temperature / (C.GaleAtmMM * AirCellGroup[i].CellCenter.y / DistanceScale)) * DistanceScale * Vector3.up);

    //Gravity
    AirCellGroup[i].PerformAcceleration(C.GaleG * DistanceScale * GravityScale * Vector3.down);
    #endregion
    #region Simulation Testing (non realistic)
    //Force from Ceiling (y = 1000)
    //AirCellGroup[i].PerformAcceleration((C.R * AirCellGroup[i].Temperature / (C.GaleAtmMM * ((1000 - AirCellGroup[i].CellCenter.y) / DistanceScale))) * DistanceScale * Vector3.down);

    //Force from walls (x = ± 1000, y = ± 1000)
    AirCellGroup[i].PerformAcceleration((C.R * AirCellGroup[i].Temperature / (C.GaleAtmMM * ((1000 - Mathf.Abs(AirCellGroup[i].CellCenter.x)) / DistanceScale))) * (AirCellGroup[i].CellCenter.x > 0 ? 1 : -1) * DistanceScale * Vector3.left);
    AirCellGroup[i].PerformAcceleration((C.R * AirCellGroup[i].Temperature / (C.GaleAtmMM * ((1000 - Mathf.Abs(AirCellGroup[i].CellCenter.z)) / DistanceScale))) * (AirCellGroup[i].CellCenter.z > 0 ? 1 : -1) * DistanceScale * Vector3.back);

    //Background Friction
    AirCellGroup[i].AccAlongVelocity(-0.2f * AirCellGroup[i].Velocity.magnitude * DistanceScale);
    #endregion
    */
    #endregion
}