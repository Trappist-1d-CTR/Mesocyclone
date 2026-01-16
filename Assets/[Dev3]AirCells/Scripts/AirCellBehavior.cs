using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class AirCellBehavior : MonoBehaviour
{
    #region Variables

    #region Cell Fundamentals
    private AirCell AirCellTest;
    private List<AirCell> AirCellGroup = new();
    private List<GameObject> CellObjectGroup = new();
        
    public GameObject CubeObject;
    #endregion

    #region Default Test Values
    public Vector3 CenterTest;
    public double MoleTest;
    public double TempTest;
    public Vector3 VelTest;
    //public float CdTest;
    public double AverageLocalTemp;
    public Vector3 AverageLocalWind;
    public int CellGroupNumber;
    #endregion

    #region Simulation Testing
    //public double[] InterpolatedValues = new double[6];
    public Vector3 DronePosition;
    public float CdTest;
    public float DistanceScale;
    public float GravityScale;
    #endregion

    #region Locational Info
    public double LocalLatitude;
    public int iLocal;
    public double AmbientHeat;
    public AnimationCurve AmbientalHeat;
    #endregion

    #region Reference Scripts
    private AGlobalValues C;
    #endregion

    #region Script Optimization Memory
    private double[] StaticPressureSOM;
    private double[] TempSOM;
    private double[] prevStatVolumeSOM;
    private double[] DynVolumeSOM;
    private double[] prevDynVolumeSOM;
    private List<Vector3> CellsRepulsionSOM;
    #endregion

    #endregion

    // int iy = 0;

    private void Awake()
    {
        // GameObject reference
        GameObject controller = GameObject.FindGameObjectWithTag("GameController");
        if (controller == null)
        {
            Debug.LogError('No GameObject with tag "GameController"');
            return;
        }

        // component reference
        C = controller.GetComponent<AGlobalValues>();
        if (C == null)
        {
            Debug.LogError("No 'AGlobalValues' Component attached to controller object");
            return;
        }

        CellsRepulsionSOM = new List<Vector3>();

        #region Instantiate Air Cells
        float l = Mathf.Floor(Mathf.Pow(CellGroupNumber, (float)(1f / 3f)));
        for (int i = 0; i < CellGroupNumber; i++)
        {
            Vector3 InstantiateLocation;

            MoleTest = C.GaleAtm * 1000000f * C.SimulationHeight / (C.R * C.GaleAvgTemp * CellGroupNumber);
            if (i < Mathf.Pow(l, 3))
            {
                InstantiateLocation = CenterTest + new Vector3(-500 + (1000f / (l * 2)) + (1000f * (i % l) / l), 100 + ((float)(C.SimulationHeight - 200) * (Mathf.Floor(i / (l * l)) / l)), -500 + (1000f / (l * 2)) + (1000f * Mathf.Floor((i % (l * l)) / l) / l));
            }
            else
            {
                InstantiateLocation = CenterTest + new Vector3(Random.Range(-499f, 499f), 999f, Random.Range(-499f, 499f));
            }

            CellObjectGroup.Add(Instantiate(CubeObject, Vector3.zero, new Quaternion(), gameObject.transform));
            AirCellGroup.Add(new(InstantiateLocation, MoleTest, TempTest + Random.Range(-25f, 25f), VelTest + new Vector3(Random.Range(-10f, 10f), Random.Range(-10f, 10f), Random.Range(-10f, 10f)), C.StiffK));
            CellObjectGroup[i].transform.position = AirCellGroup[i].CellCenter;
        }
        #endregion

        #region SOMs Setup
        prevStatVolumeSOM = new double[CellGroupNumber];
        DynVolumeSOM = new double[CellGroupNumber];
        prevDynVolumeSOM = new double[CellGroupNumber];

        StaticPressureSOM = new double[CellGroupNumber];
        TempSOM = new double[CellGroupNumber];

        for (int i = 0; i < CellGroupNumber; i++)
        {
            TempSOM[i] = AirCellGroup[i].Temperature;
        }
        #endregion

        #region iLocal Calculation
        iLocal = (int)System.Math.Round((double)((LocalLatitude / (180 / C.EOS_N)) - (90 / C.EOS_N)));
        iLocal = (iLocal >= 0) ? ((iLocal < C.EOS_N) ? iLocal : C.EOS_N) : 0;
        #endregion
    }

    private void FixedUpdate()
    {
        // iy++;

        /* if (iy == 100)
        {
            Debug.Log("");
            iy = 0;
        }
        */
        
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

            double mem;

            #region Insolation
            AirCellGroup[i].Temperature = TempSOM[i];
            AirCellGroup[i].Temperature += mem = C.GlobalInsolation.Evaluate((float)LocalLatitude) * C.ClearSkyTransparency.Evaluate((float)AirCellGroup[i].CellCenter.y) * AirCellGroup[i].CellCircleArea / (C.GaleAtmCp * C.GaleAtmMM * AirCellGroup[i].Moles) * Time.fixedDeltaTime;
            #endregion
            //if (i == 0) Debug.Log("Insolation: " + mem);

            DebugEverything(i);

            #region Diffusion
            AirCellGroup[i].Temperature += (C.EOS_Diffusion[iLocal] / C.EOS_AtmC) * Time.fixedDeltaTime;
            #endregion
            //if (i == 0) Debug.Log("Diffusion: " + (C.EOS_Diffusion[0] / C.EOS_AtmC));

            DebugEverything(i);

            #region Radiative Heating/Cooling
            mem = C.GreekS * C.AtmSpecificEmissivity * ((2 * AirCellGroup[i].CellCircleArea) + (2 * System.Math.PI * AirCellGroup[i].CellRadius * AirCellGroup[i].CellHeight)) * System.Math.Pow(AirCellGroup[i].Temperature, 4) * Time.fixedDeltaTime;
            //if (i == 0)   Debug.Log("Radiative Cooling: " + (mem / (AirCellGroup[i].Moles * C.GaleAtmMM * C.EOS_AtmC)));
            AirCellGroup[i].Temperature -= mem / (AirCellGroup[i].Moles * C.GaleAtmMM * C.EOS_AtmC);
            #endregion

            DebugEverything(i);
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
            AirCellGroup[i].Temperature *= System.Math.Pow(prevStatVolumeSOM[i] / AirCellGroup[i].CellStaticVolume, C.R / C.MolarHeatCapacity);
            prevStatVolumeSOM[i] = AirCellGroup[i].CellStaticVolume;

            if (TempSOM[i] - AirCellGroup[i].Temperature > 10)
            {
                Debug.Log("Heavy Abiatic Temperature Change [" + i + "] ; SOM = " + TempSOM[i] + " ; Temp = " + AirCellGroup[i].Temperature);
            }

            TempSOM[i] = AirCellGroup[i].Temperature;
            #endregion

            DebugEverything(i);

            #region Air Cell Terrain Repulsion
            if (AirCellGroup[i].CellCenter.y < AirCellGroup[i].CellHeight / 2)
            {
                DynVolumeSOM[i] *= 0.5 + (AirCellGroup[i].CellCenter.y / AirCellGroup[i].CellHeight);
                AirCellGroup[i].PerformAcceleration((float)(StaticPressureSOM[i] * AirCellGroup[i].CellCircleArea * (System.Math.Pow(AirCellGroup[i].CellStaticVolume / DynVolumeSOM[i], (double)((double)1.0 + (C.MolarHeatCapacity / C.R))) - 1) / (AirCellGroup[i].Moles * C.GaleAtmMM)) * Vector3.up);

                //if (TempSOM[i] - AirCellGroup[i].Temperature > 1) Debug.Log("Heavy Terrain Rep. Temperature Change [" + i + "] ; SOM = " + TempSOM[i] + " ; Temp = " + AirCellGroup[i].Temperature);
                //TempSOM[i] = AirCellGroup[i].Temperature;
                //   Debug.Log((float)(StaticPressureSOM[i] * AirCellGroup[i].CellCircleArea * (System.Math.Pow(AirCellGroup[i].CellVolume / prevVolumeSOM[i], (double)((double)1.0 + (C.MolarHeatCapacity / C.R))) - 1) / (AirCellGroup[i].Moles * C.GaleAtmMM)));
            }
            #endregion

            DebugEverything(i);

            #region Perform Gravity and Buoyancy
            AirCellGroup[i].PerformAcceleration((float)(C.GaleG * ((AirCellGroup[i].Temperature / AverageLocalTemp) - 1.0)) * Vector3.up);
            #endregion

            DebugEverything(i);

            #region Perform Air Cell Drag
            
            AirCellGroup[i].PerformAcceleration(CdTest * Mathf.Pow(AirCellGroup[i].Velocity.y - AverageLocalWind.y, 2) / (2 * (float)AirCellGroup[i].CellHeight) * -Vector3.Project(AirCellGroup[i].Velocity - AverageLocalWind, Vector3.up).normalized);
            AirCellGroup[i].PerformAcceleration(CdTest * Vector3.ProjectOnPlane(AirCellGroup[i].Velocity - AverageLocalWind, Vector3.up).sqrMagnitude / (4 * (float)AirCellGroup[i].CellRadius) * -Vector3.ProjectOnPlane(AirCellGroup[i].Velocity - AverageLocalWind, Vector3.up).normalized);
            
            #endregion

            DebugEverything(i);

            #region Check for Terrain Collision - to do: improve with bouncing
            if (AirCellGroup[i].CellCenter.y <= (-AirCellGroup[i].CellHeight / 2))
            {
                AirCellGroup[i].CellCenter = new Vector3(AirCellGroup[i].CellCenter.x, (float)-AirCellGroup[i].CellHeight / 2 + 0.2f, AirCellGroup[i].CellCenter.z);
            }
            #endregion

            DebugEverything(i);

            #region Keep Within Boundaries - note: for testing purposes

            if (Mathf.Abs(AirCellGroup[i].CellCenter.x) >= 500.1)
            {
                AirCellGroup[i].Velocity += new Vector3(Mathf.Sign(AirCellGroup[i].CellCenter.x) * -50 * Time.fixedDeltaTime, 0, 0); //Vector3.Scale(AirCellGroup[i].Velocity, new Vector3(-0.1f, 1, 1));
                //AirCellGroup[i].CellCenter = new Vector3(AirCellGroup[i].CellCenter.x > 0 ? 500 : -500, AirCellGroup[i].CellCenter.y, AirCellGroup[i].CellCenter.z);
            }

            if (Mathf.Abs(AirCellGroup[i].CellCenter.z) >= 500.1)
            {
                AirCellGroup[i].Velocity += new Vector3(0, 0, Mathf.Sign(AirCellGroup[i].CellCenter.z) * -50 * Time.fixedDeltaTime);
                //AirCellGroup[i].CellCenter = new Vector3(AirCellGroup[i].CellCenter.x, AirCellGroup[i].CellCenter.y, AirCellGroup[i].CellCenter.z > 0 ? 500 : -500);
            }

            if (AirCellGroup[i].CellCenter.y >= C.SimulationHeight + 0.1)
            {
                AirCellGroup[i].Velocity += new Vector3(0, Mathf.Sign(AirCellGroup[i].CellCenter.y) * -50 * Time.fixedDeltaTime, 0);
                //AirCellGroup[i].CellCenter = new Vector3(AirCellGroup[i].CellCenter.x, (float)C.SimulationHeight, AirCellGroup[i].CellCenter.z);
            }

            #endregion

            DebugEverything(i);
        }
        #endregion

        #region Inter-Cell Behavior
        CellsRepulsionSOM.Clear();
        float d, r1, r2, d1, d2, A, h;

        for (int i = 0; i < CellGroupNumber; i++)
        {
            for (int i2 = i + 1; i2 < CellGroupNumber; i2++)
            {
                #region Check For and Calculate Overlaps
                if ((h = System.Math.Abs(AirCellGroup[i].CellCenter.y - AirCellGroup[i2].CellCenter.y)) < ((AirCellGroup[i].CellHeight + AirCellGroup[i2].CellHeight) / 2)
                    && (d = Vector2.Distance(new Vector2(AirCellGroup[i].CellCenter.x, AirCellGroup[i].CellCenter.z), new Vector2(AirCellGroup[i2].CellCenter.x, AirCellGroup[i2].CellCenter.z))) < (AirCellGroup[i].CellRadius + AirCellGroup[i2].CellRadius))
                {
                    #region Cell Overlap Calculations

                    if (AirCellGroup[i].CellRadius >= AirCellGroup[i2].CellRadius)
                    {
                        r1 = (float)AirCellGroup[i].CellRadius;
                        r2 = (float)AirCellGroup[i2].CellRadius;
                    }
                    else
                    {
                        r1 = (float)AirCellGroup[i2].CellRadius;
                        r2 = (float)AirCellGroup[i].CellRadius;
                    }

                    h = (float)System.Math.Abs(h - ((AirCellGroup[i].CellHeight + AirCellGroup[i2].CellHeight) / 2));

                    d1 = (Mathf.Pow(r1, 2) - Mathf.Pow(r2, 2) + Mathf.Pow(d, 2)) / (2 * d);
                    d2 = d - d1;

                    A = (Mathf.Pow(r1, 2) * Mathf.Acos(d1 / r1)) - (d1 * Mathf.Sqrt(Mathf.Pow(r1, 2) - Mathf.Pow(d1, 2))) + (Mathf.Pow(r2, 2) * Mathf.Acos(d2 / r2)) - (d2 * Mathf.Sqrt(Mathf.Pow(r2, 2) - Mathf.Pow(d2, 2)));

                    #endregion

                    if (h * A > 0 && DynVolumeSOM[i] > 0 && DynVolumeSOM[i2] > 0)
                    {
                        DynVolumeSOM[i] = System.Math.Max(0, DynVolumeSOM[i] - A * h / 2);
                        DynVolumeSOM[i2] = System.Math.Max(0, DynVolumeSOM[i2] - A * h / 2);

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
        for (int i = 0; i < CellsRepulsionSOM.Count; i++)
        {
            AirCellGroup[(int)CellsRepulsionSOM[i].x].PerformAcceleration((float)(StaticPressureSOM[(int)CellsRepulsionSOM[i].x] * System.Math.Pow(CellsRepulsionSOM[i].z, (double)((double)2.0 / (double)3.0)) * (System.Math.Pow(AirCellGroup[(int)CellsRepulsionSOM[i].x].CellStaticVolume / DynVolumeSOM[(int)CellsRepulsionSOM[i].x], (double)((double)1.0 + (C.MolarHeatCapacity / C.R))) - 1) / (AirCellGroup[(int)CellsRepulsionSOM[i].x].Moles * C.GaleAtmMM)) * (AirCellGroup[(int)CellsRepulsionSOM[i].x].CellCenter - AirCellGroup[(int)CellsRepulsionSOM[i].y].CellCenter).normalized);
            AirCellGroup[(int)CellsRepulsionSOM[i].y].PerformAcceleration((float)(StaticPressureSOM[(int)CellsRepulsionSOM[i].y] * System.Math.Pow(CellsRepulsionSOM[i].z, (double)((double)2.0 / (double)3.0)) * (System.Math.Pow(AirCellGroup[(int)CellsRepulsionSOM[i].y].CellStaticVolume / DynVolumeSOM[(int)CellsRepulsionSOM[i].y], (double)((double)1.0 + (C.MolarHeatCapacity / C.R))) - 1) / (AirCellGroup[(int)CellsRepulsionSOM[i].y].Moles * C.GaleAtmMM)) * (AirCellGroup[(int)CellsRepulsionSOM[i].y].CellCenter - AirCellGroup[(int)CellsRepulsionSOM[i].x].CellCenter).normalized);
        }
        #endregion

        #endregion

        #region Perform Physics
        for (int i = 0; i < CellGroupNumber; i++)
        {
            DebugEverything(i);

            #region Dynamic Abiatic Temperature Change
            if (prevDynVolumeSOM[i] == 0) prevDynVolumeSOM[i] = DynVolumeSOM[i];
            AirCellGroup[i].Temperature *= System.Math.Pow(prevDynVolumeSOM[i] / DynVolumeSOM[i], C.R / C.MolarHeatCapacity);
            prevDynVolumeSOM[i] = DynVolumeSOM[i];
            #endregion

            AirCellGroup[i].PerformVelocity();

            //To visualize the Cells
            CellObjectGroup[i].transform.position = AirCellGroup[i].CellCenter;

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

        InverseDistanceWeighting.InterpolationStep(new Vector3(InverseDistanceWeighting.Query.x, 0, InverseDistanceWeighting.Query.z),
            new float[6] { 0, 0, 0, (float)MoleTest, (float)AverageLocalTemp, 0 });
        foreach (int i in InverseDistanceWeighting.Indexes)
        {
            InverseDistanceWeighting.InterpolationStep(AirCellGroup[i].CellCenter,
                new float[6] { AirCellGroup[i].Velocity.x, AirCellGroup[i].Velocity.y, AirCellGroup[i].Velocity.z, (float)AirCellGroup[i].Moles,
                    (float)AirCellGroup[i].Temperature, 0 /* Dynamic Volume Should Supposedly Go Here */ });
        }

        InverseDistanceWeighting.BroadcastInterp();

        #region Natural Neighbour Interpolation Attempt
        /* 
        Vertex3[] CellVertexes = new Vertex3[CellGroupNumber];
        //Interpolation Values
        if (CellGroupNumber > 4)
        {
            CellVertexes[i] = new(AirCellGroup[i].CellCenter.x, AirCellGroup[i].CellCenter.y, AirCellGroup[i].CellCenter.z, new double[6]
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

    public void DebugEverything(int i)
    {
        if (!float.IsFinite((float)AirCellGroup[i].CellStaticVolume))
            Debug.LogError("NaN Cell Volume ; i = " + i);

        if (!float.IsFinite((float)AirCellGroup[i].Temperature))
            Debug.LogError("NaN Temperature ; i = " + i);

        if (prevStatVolumeSOM[i] <= 0 && AirCellGroup[i].CellCenter.y < AirCellGroup[i].CellHeight / 2)
            Debug.LogError("Negative/Null prevVolumeSOM ; i = " + i);

        if (AirCellGroup[i].CellCenter.y <= -AirCellGroup[i].CellHeight / 2)
            Debug.LogError("ACDDC - Air Cell Digging Down to China ; i = " + i);

        if (!float.IsFinite(AirCellGroup[i].CellCenter.x) || !float.IsFinite(AirCellGroup[i].CellCenter.y) || !float.IsFinite(AirCellGroup[i].CellCenter.z))
            Debug.LogError("NaN Position ; i = " + i);
    }
}


#region Discarded
/*
 * for (int i = 0; i < 1; i++)
{
//Force from Other Cells
AirCellGroup[i].PerformAcceleration((float)(C.R * ((AirCellGroup[i].Temperature + AirCellGroup[i + 1].Temperature) / 2) / (C.GaleAtmMM * (AirCellGroup[i].CellCenter.y / DistanceScale))) * (AirCellGroup[i].CellCenter - AirCellGroup[i + 1].CellCenter).normalized * DistanceScale);
AirCellGroup[i + 1].PerformAcceleration((float)(C.R * ((AirCellGroup[i].Temperature + AirCellGroup[i + 1].Temperature) / 2) / (C.GaleAtmMM * (AirCellGroup[i].CellCenter.y / DistanceScale))) * (AirCellGroup[i + 1].CellCenter - AirCellGroup[i].CellCenter).normalized * DistanceScale);
}

#region Cell Behavior Simulation
//Force from Terrain (y = 0)
//(float)(C.R * AirCellGroup[i].Temperature / (C.GaleAtmMM * AirCellGroup[i].CellCenter.y / DistanceScale));
//double P1 = AirCellGroup[i].Moles * C.R * AirCellGroup[i].Temperature / (AirCellGroup[i].CellCenter.y / DistanceScale);
//double P2 = C.CalculateStaticPressure(AirCellGroup[i].CellCenter.y);
AirCellGroup[i].PerformAcceleration((float)(C.R * AirCellGroup[i].Temperature / (C.GaleAtmMM * AirCellGroup[i].CellCenter.y / DistanceScale)) * DistanceScale * Vector3.up);

//Gravity
AirCellGroup[i].PerformAcceleration((float)C.GaleG * DistanceScale * GravityScale * Vector3.down);
#endregion
#region Simulation Testing (non realistic)
//Force from Ceiling (y = 1000)
//AirCellGroup[i].PerformAcceleration((float)(C.R * AirCellGroup[i].Temperature / (C.GaleAtmMM * ((1000 - AirCellGroup[i].CellCenter.y) / DistanceScale))) * DistanceScale * Vector3.down);

//Force from walls (x = ± 1000, y = ± 1000)
AirCellGroup[i].PerformAcceleration((float)(C.R * AirCellGroup[i].Temperature / (C.GaleAtmMM * ((1000 - Mathf.Abs(AirCellGroup[i].CellCenter.x)) / DistanceScale))) * (AirCellGroup[i].CellCenter.x > 0 ? 1 : -1) * DistanceScale * Vector3.left);
AirCellGroup[i].PerformAcceleration((float)(C.R * AirCellGroup[i].Temperature / (C.GaleAtmMM * ((1000 - Mathf.Abs(AirCellGroup[i].CellCenter.z)) / DistanceScale))) * (AirCellGroup[i].CellCenter.z > 0 ? 1 : -1) * DistanceScale * Vector3.back);

//Background Friction
AirCellGroup[i].AccAlongVelocity(-0.2f * AirCellGroup[i].Velocity.magnitude * DistanceScale);
#endregion
*/
#endregion
