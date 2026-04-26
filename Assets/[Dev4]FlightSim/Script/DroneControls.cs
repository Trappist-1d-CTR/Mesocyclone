using System.IO;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

public class DroneControls : MonoBehaviour
{
    #region Variables

    #region Drone Data
    [System.Serializable]
    public class MainStats
    {
        public string Name;
        public float Mass;
        public bool YesWind;
        public float CenterMassX;
        public float CenterMassY;
        public float CenterMassZ;
        public float DroneVolume;
        public int DroneParts;
        public float FrontArea;
        public float BrakeArea;
        public float BottomArea;
        public float SideArea;
        public float StallAngle;
        public float MaxInducedAoA;
        public float LiftCurveSlope;
        public float FrontCd;
        public float AirBrakesCd;
        public float BottomCd;
        public float SideCd;
        public float SideBrakesCd;
        public float ThrustAcceleration;
        public float FullThrustTime;
        public float ImpulseAcceleration;
        public float ImpulseMaxCharge;
        public float ImpulseRecharge;
        public float ImpulseStartupTime;
        public float ImpulseShutdownTime;
        public float HoverMaxThrust;
        public float HoverJerk;
        public float LandingSpeed;
        public float ReactionWheelsTorqueJerk;
    }

    [System.Serializable]
    public class MainDroneStats
    {
        public MainStats[] DroneBodyStats;
    }

    [System.Serializable]
    public class PartStats
    {
        public GameObject PartObject;
        public GameObject PartObjectb;
        public int ID;
        public int IDb;
        public string Name;
        public float CenterMassX;
        public float CenterMassY;
        public float CenterMassZ;
        public float Area;
        public float Chord;
        public float ControlAngle;
    }

    [System.Serializable]
    public class DronePartsList
    {
        public PartStats[] DronePartStats;
    }

    [System.Serializable]
    public class DroneObject
    {
        public MainDroneStats MainBody;
        public DronePartsList Parts;
    }

    public string DroneDataTxt;
    public DroneObject NetLinker;
    #endregion

    #region Engines Control Data
    public float InputMargin;

    public float Thrust;

    public float ImpulseCharge;
    public bool ImpulseActive;
    public float ImpulseTimer;
    public float LastImpulseBurn;
    public float ImpulseThreshold;
    public float ImpulseInputErrorTimer;
    
    public enum HoverModeType : int
    {
        MaxThrust = 1,
        SpeedTarget = 2,
        Descent = 3,
        Gravity = 4,
        /* half bread: what the fuck is 5??? */
        Landing = 6
    }
    public HoverModeType HoverMode;

    public float HoverThrust;
    public float HoverTarget;
    public bool HoverAuto;
    public bool HoverActive;
    public float HoverTimer;
    public int HoverMaxAngle;
    public float[] HoverTargetSpeed;
    public float HoverInputErrorTimer;
    #endregion

    #region Physics Engine Values
    private Vector3 PhysicsPosition;
    private Vector3 PhysicsVelocity;
    private Vector3 PhysicsAcceleration;

    private Quaternion PhysicsRotation;
    private Vector3 PhysicsAngVelocity;
    public Vector3 PhysicsTorque;

    public float DynamicPressure;
    public float ReferenceDynPressure;
    private float PrevDynamicPressure;
    #endregion

    #region Input Controls System (ICS)  
    public InputMap InputControl;

    // SAS = Stability Assist System
    public enum SASModeType : byte
    {
        Disabled = 0,
        Heli = 1,
        Plane = 2
    }
    public SASModeType SASMode; 
    
    private bool[] InputValues;
    private Vector3 CurrentAngles;
    public Vector3 ReactionWheelsDefaultTorque;
    private float[] ReactionWheelsTargetTorque;
    private float[] ReactionWheelsTorque;
    public float[] ReactionWheelsJerk;

    public float TorqueStabilizationJerk;
    private float[] ReactionWheelsTorqueStabilization;

    public float[] ControlSurfaceMaxAngles;
    public float[] ControlSurfaceTargetAngle;
    public float[] ControlSurfaceAngles;
    public float[] ControlSurfaceAngularSpeed;

    public float PitchSpeed;
    public float PitchNeutral;

    public float AileronsDefaultAngle;
    public float RollJerk;

    public float YawSpeed;

    private float[] NeutralControlsTimer;
    #endregion

    #region Atmospherical Physics
    public Vector3 Wind;
    public Vector3 AirSpeed;
    #endregion

    #region GameObject Components
    private Rigidbody DronePhysics;
    private BoxCollider DroneCollider;
    #endregion

    #region LiftDragTorque Coefficients
    private float[] AspectRatio;
    private AnimationCurve[] LiftAoA;
    private AnimationCurve[] InducedDragAoA;
    private AnimationCurve[] TorqueAoA;
    #endregion

    #region Reference Scripts
    private AGlobalValues C;
    public AirCellBehavior Air;
    private AirDataComputer DataComputer;
    #endregion

    #region Center Of Mass Correction

    public bool CenterOfLiftFound = false;
    private Vector3 VectorA;
    public Vector3 CenterOfLift;
    public Vector3 CenterOfMass;
    private double B;

    #endregion

    #region Physics Visualizer

    public enum VisualizationModeType : int
    {
        Off = 0,
        Forces = 1,
        LiftDrag = 2,
        Torque = 3
    }
    public VisualizationModeType VisualizationMode;

    private Vector3 TotThrust;
    private Vector3 TotDrag;
    private Vector3 TotLift;
    private Vector3 TotWeight;

    public bool AirChamberTest = false;
    public bool SteadyFlightTest = false;
    public float DesiredSteadySpeed;

    #endregion

    #region Wind Visualizer
    public bool WindParticlesEnabled;
    public ParticleSystem WindParticles;
    public float ParticlesOriginDistance;
    #endregion

    #region Script Optimization Memory
    private Vector3 Memory;
    private Vector3 ResetPosition;
    #endregion

    #region Data Output
    [System.Serializable]
    public class DataTable
    {
        public List<Vector3> TableElements;
    }

    private int DataTimeTick;
    public int DataTimeRate;

    public enum DataGatherStageType : int
    {
        Idle = 0,
        Gathering = 1,
        Saving = 2,
        Done = 3
    }
    public DataGatherStageType DataGatherStage;
    
    public DataTable OutputData;

    private float TimeAtGatheringStart;
    #endregion

    #endregion

    void Start()
    {
        #region Get Components and Script Reference

        DronePhysics = gameObject.GetComponent<Rigidbody>();
        DroneCollider = gameObject.GetComponent<BoxCollider>();
        C = GameObject.FindGameObjectWithTag("GameController").GetComponent<AGlobalValues>();
        DataComputer = gameObject.GetComponentInChildren<AirDataComputer>(false);

        #endregion

        #region Read Drone Data
        DroneDataTxt = System.IO.File.ReadAllText(Application.streamingAssetsPath + "/DroneData/DroneProperties.json");

        NetLinker = new DroneObject { MainBody = JsonUtility.FromJson<MainDroneStats>(DroneDataTxt), Parts = JsonUtility.FromJson<DronePartsList>(DroneDataTxt) };

        for (int i = 0; i < NetLinker.Parts.DronePartStats.Length; i++)
        {
            NetLinker.Parts.DronePartStats[i].PartObject = transform.Find("Corrective").GetChild(NetLinker.Parts.DronePartStats[i].ID).gameObject;
            if (NetLinker.Parts.DronePartStats[i].IDb != 0)
            {
                if (transform.Find("Corrective").childCount > NetLinker.Parts.DronePartStats[i].IDb)
                {
                    NetLinker.Parts.DronePartStats[i].PartObjectb = transform.Find("Corrective").GetChild(NetLinker.Parts.DronePartStats[i].IDb).gameObject;
                }
                else
                {
                    NetLinker.Parts.DronePartStats[i].PartObjectb = NetLinker.Parts.DronePartStats[i].PartObject;
                    //Debug.Log("PartObject with ID " + NetLinker.Parts.DronePartStats[i].IDb + " not found.");
                }
            }
        }

        DronePhysics.mass = NetLinker.MainBody.DroneBodyStats[0].Mass;
        #endregion

        #region Starting Setup

        //Enables inputs
        InputControl = new();
        InputControl.Enable();

        //Setup CenterOfMass from json drone data
        CenterOfMass = DronePhysics.centerOfMass = new Vector3(NetLinker.MainBody.DroneBodyStats[0].CenterMassX, NetLinker.MainBody.DroneBodyStats[0].CenterMassY, NetLinker.MainBody.DroneBodyStats[0].CenterMassZ);

        //Initialize animation curves for following info
        AspectRatio = new float[NetLinker.Parts.DronePartStats.Length];
        LiftAoA = new AnimationCurve[NetLinker.Parts.DronePartStats.Length];
        InducedDragAoA = new AnimationCurve[NetLinker.Parts.DronePartStats.Length];
        TorqueAoA = new AnimationCurve[NetLinker.Parts.DronePartStats.Length];

        //Initialize float3 for the ICS
        ControlSurfaceTargetAngle = new float[3] { PitchNeutral, 0, 0 };
        ReactionWheelsTorque = new float[3] { 0, 0, 0 };
        ReactionWheelsTorqueStabilization = new float[3] { 0, 0, 0 };
        ControlSurfaceAngles = new float[3] { 0, 0, 0 };
        NeutralControlsTimer = new float[3] { 0, 0, 0 };

        //Assign AspectRatio values
        for (int i = 0; i < AspectRatio.Length; i++)
        {
            AspectRatio[i] = NetLinker.Parts.DronePartStats[i].Chord * NetLinker.Parts.DronePartStats[i].Chord / NetLinker.Parts.DronePartStats[i].Area;
        }
        //Calculate FlightCoefficients values
        for (int i = 0; i < NetLinker.Parts.DronePartStats.Length; i++)
        {
            CalculateFlightCoefficients(i);
        }

        //Setup elevator objects rotation
        NetLinker.Parts.DronePartStats[7].PartObject.transform.localRotation = Quaternion.Euler(90, 0, 0);
        NetLinker.Parts.DronePartStats[7].PartObjectb.transform.localRotation = Quaternion.Euler(90, 0, 0);

        //Setup misc arrays
        InputValues = new bool[6] { false, false, false, false, false, false };
        ReactionWheelsTargetTorque = new float[3] { 0, 0, 0 };

        //Calculate ReferenceDynamicPressure
        ReferenceDynPressure = (float)C.GaleAtmD * 400f;

        //Setup ResetPosition to position at Start()
        ResetPosition = DronePhysics.position;

        //Setup Steady Flight Test if enabled
        if (SteadyFlightTest)
        {
            DroneCollider.enabled = false;
            DronePhysics.linearVelocity = AirSpeed;
        }

        #endregion

        #region Data Output Setup
        DataGatherStage = DataGatherStageType.Idle;
        OutputData = new();
        DataTimeTick = 0;
        #endregion

        #region Steady Flight Setup

        if (SteadyFlightTest)
        {
            PhysicsVelocity = DronePhysics.linearVelocity = Vector3.right * DesiredSteadySpeed;
            Thrust = 1;
        }

        #endregion
    }

    private void FixedUpdate()
    {
        if (!CenterOfLiftFound)
        {
            VectorA = Vector3.zero;
            B = 0;
        }

        #region Measurement Values Reset

        TotThrust = Vector3.zero;
        TotDrag = Vector3.zero;
        TotLift = Vector3.zero;
        TotWeight = Vector3.zero;

        #endregion

        #region Physics Setup

        PhysicsVelocity = DronePhysics.linearVelocity;
        InverseDistanceWeighting.Query = PhysicsPosition = DronePhysics.position;
        if (Air != null) Air.DronePosition = PhysicsPosition;
        PhysicsAcceleration = TotWeight = ((float)C.GaleG * Vector3.down) + (NetLinker.MainBody.DroneBodyStats[0].DroneVolume * (float)C.GaleAtmD * Vector3.up / DronePhysics.mass);
        //   Debug.Log("Starting Acceleration: " + PhysicsAcceleration);
        PhysicsRotation = DronePhysics.rotation;
        PhysicsAngVelocity = DronePhysics.angularVelocity;
        PhysicsTorque = Vector3.zero;
        DronePhysics.angularDamping = 3f;
        if (DronePhysics.centerOfMass != CenterOfMass)
        {
            DronePhysics.centerOfMass = CenterOfMass;
        }

        //Wind Vector: Tailwind is a Positive X while Headwind is a Negative X ; Climbing is a Negative Y while Descending is a Positive Y
        if (!AirChamberTest && !SteadyFlightTest && InverseDistanceWeighting.Values != null)
            Wind = (NetLinker.MainBody.DroneBodyStats[0].YesWind && Time.time > 2) ? new Vector3(InverseDistanceWeighting.Values[0], InverseDistanceWeighting.Values[1], InverseDistanceWeighting.Values[2]) : Vector3.zero;
        else if (!AirChamberTest)
            Wind = Vector3.zero;

        #endregion

        #region Main Engine Thrust

        Memory = transform.right;

        //Main Thrust
        if (InputControl.FlightControls.Thrust.inProgress)
        {
            Thrust += InputControl.FlightControls.Thrust.ReadValue<float>() * Time.fixedDeltaTime / NetLinker.MainBody.DroneBodyStats[0].FullThrustTime;
            Thrust = Mathf.Clamp01(Thrust);
        }

        PhysicsAcceleration += TotThrust = Thrust * NetLinker.MainBody.DroneBodyStats[0].ThrustAcceleration * Memory;

        #endregion

        #region Impulse Drive

        if (InputControl.FlightControls.ImpulseThrust.inProgress)
        {
            if (ImpulseInputErrorTimer != 0) ImpulseInputErrorTimer = 0;
            ImpulseTimer += Time.fixedDeltaTime;

            if (!ImpulseActive && ImpulseTimer >= NetLinker.MainBody.DroneBodyStats[0].ImpulseStartupTime && ImpulseCharge > ImpulseThreshold)
            {
                ImpulseActive = true;
            }
        }
        else if (ImpulseInputErrorTimer < NetLinker.MainBody.DroneBodyStats[0].ImpulseShutdownTime && LastImpulseBurn != 0)
        {
            ImpulseTimer += Time.fixedDeltaTime;
            ImpulseInputErrorTimer += Time.fixedDeltaTime;
        }
        else
        {
            if (ImpulseActive)
            {
                ImpulseTimer += Time.fixedDeltaTime;

                if ((ImpulseTimer - LastImpulseBurn) >= NetLinker.MainBody.DroneBodyStats[0].ImpulseShutdownTime) ImpulseActive = false;
            }
            else if (ImpulseTimer != 0) ImpulseTimer = 0;

            if (LastImpulseBurn != 0)
            {
                ImpulseThreshold = (NetLinker.MainBody.DroneBodyStats[0].ImpulseStartupTime / 3) + (LastImpulseBurn / NetLinker.MainBody.DroneBodyStats[0].ImpulseMaxCharge);
                ImpulseTimer = 0;
                LastImpulseBurn = 0;
            }
        }

        if (ImpulseActive)
        {
            PhysicsAcceleration += NetLinker.MainBody.DroneBodyStats[0].ImpulseAcceleration * Memory;
            TotThrust += NetLinker.MainBody.DroneBodyStats[0].ImpulseAcceleration * Memory;
            ImpulseCharge -= Time.fixedDeltaTime;
            LastImpulseBurn += Time.fixedDeltaTime;

            if (Thrust != 0) Thrust = 0;

            if (ImpulseCharge < 0)
            {
                ImpulseActive = false;

                if (LastImpulseBurn != 0)
                {
                    ImpulseThreshold = 0.5f + (LastImpulseBurn / NetLinker.MainBody.DroneBodyStats[0].ImpulseMaxCharge);
                    ImpulseTimer = 0;
                    LastImpulseBurn = 0;
                }
            }
        }
        else if (ImpulseCharge != NetLinker.MainBody.DroneBodyStats[0].ImpulseMaxCharge)
        {
            ImpulseCharge += Time.fixedDeltaTime * NetLinker.MainBody.DroneBodyStats[0].ImpulseRecharge;
            ImpulseCharge = Mathf.Clamp(ImpulseCharge, 0, NetLinker.MainBody.DroneBodyStats[0].ImpulseMaxCharge);
        }

        #endregion

        #region Hovering
        
        Memory = transform.up;

        #region Hover Controls

        InputControl.FlightControls.MouseClick.performed += ToggleAutoMode;
        InputControl.FlightControls.ToggleHoverMode.performed += SwitchModes;
        

        if (InputControl.FlightControls.Hovering.inProgress)
        {
            if (HoverInputErrorTimer != 0) HoverInputErrorTimer = 0;

            HoverTimer += Time.fixedDeltaTime;

            if (!HoverActive && HoverTimer > InputMargin)
            {
                HoverActive = true;
            }
        }
        else if (HoverInputErrorTimer < InputMargin)
        {
            HoverTimer += Time.fixedDeltaTime;
            HoverInputErrorTimer += Time.fixedDeltaTime;
        }
        else
        {
            if (HoverTimer != 0) HoverTimer = 0;
            if (HoverActive) HoverActive = false;
        }

        #endregion

        #region Hover Modes and Execution

        if (HoverActive)
        {
            float mem = (2 * NetLinker.MainBody.DroneBodyStats[0].HoverJerk) / (NetLinker.MainBody.DroneBodyStats[0].HoverMaxThrust - (float)C.GaleG);

            switch (HoverMode)
            {
                case HoverModeType.MaxThrust:
                    HoverTarget = NetLinker.MainBody.DroneBodyStats[0].HoverMaxThrust;
                    break;

                case HoverModeType.SpeedTarget:
                    if (DronePhysics.linearVelocity.y <= -16.77 + HoverTargetSpeed[0])
                    {
                        HoverTarget = NetLinker.MainBody.DroneBodyStats[0].HoverMaxThrust;
                    }
                    else if (DronePhysics.linearVelocity.y < HoverTargetSpeed[0])
                    {
                        HoverTarget = NetLinker.MainBody.DroneBodyStats[0].HoverMaxThrust + (mem * (DronePhysics.linearVelocity.y + HoverTargetSpeed[0]));
                    }
                    else
                    {
                        HoverTarget = 0;
                    }
                    break;

                case HoverModeType.Descent:
                    if (DronePhysics.linearVelocity.y <= -16.77)
                    {
                        HoverTarget = NetLinker.MainBody.DroneBodyStats[0].HoverMaxThrust;
                    }
                    else if (DronePhysics.linearVelocity.y < 0)
                    {
                        HoverTarget = NetLinker.MainBody.DroneBodyStats[0].HoverMaxThrust + (mem * DronePhysics.linearVelocity.y);
                    }
                    else
                    {
                        HoverTarget = 0;
                    }
                    break;

                case HoverModeType.Gravity:
                    HoverTarget = (float)C.GaleG;
                    break;
/*
                case 5:
                    if (DronePhysics.velocity.y <= -16.77 + HoverTargetSpeed[1])
                    {
                        HoverTarget = NetLinker.MainBody.DroneBodyStats[0].HoverMaxThrust;
                    }
                    else if (DronePhysics.velocity.y < HoverTargetSpeed[1])
                    {
                        HoverTarget = NetLinker.MainBody.DroneBodyStats[0].HoverMaxThrust + (mem * (DronePhysics.velocity.y + HoverTargetSpeed[1]));
                    }
                    else
                    {
                        HoverTarget = 0;
                    }
                    break;*/

                case HoverModeType.Landing:
                    if (DronePhysics.linearVelocity.y <= -16.77 + (DronePhysics.position.y > 10 ? -3 : NetLinker.MainBody.DroneBodyStats[0].LandingSpeed))
                    {
                        HoverTarget = NetLinker.MainBody.DroneBodyStats[0].HoverMaxThrust;
                    }
                    else if (DronePhysics.linearVelocity.y < (DronePhysics.position.y > 10 ? -3 : NetLinker.MainBody.DroneBodyStats[0].LandingSpeed))
                    {
                        HoverTarget = NetLinker.MainBody.DroneBodyStats[0].HoverMaxThrust + (mem * (DronePhysics.linearVelocity.y - NetLinker.MainBody.DroneBodyStats[0].LandingSpeed));
                    }
                    else
                    {
                        HoverTarget = 0;
                    }
                    break;

                default:

                    break;
            }

            if (Mathf.Abs(Vector3.Angle(Memory, Vector3.up)) >= HoverMaxAngle)
            {
                HoverTarget = 0;
            }
            else
            {
                HoverTarget /= Mathf.Cos(Mathf.Deg2Rad * Mathf.Abs(Vector3.Angle(Memory, Vector3.up)));
            }

            HoverTarget = Mathf.Clamp(HoverTarget, 0, NetLinker.MainBody.DroneBodyStats[0].HoverMaxThrust);

            if (HoverThrust != HoverTarget)
            {
                if (Mathf.Abs(HoverThrust - HoverTarget) < NetLinker.MainBody.DroneBodyStats[0].HoverJerk * Time.fixedDeltaTime)
                {
                    HoverThrust = HoverTarget;
                }
                else
                {
                    HoverThrust += Mathf.Sign(HoverTarget - HoverThrust) * NetLinker.MainBody.DroneBodyStats[0].HoverJerk * Time.fixedDeltaTime;
                }
            }
        }
        else if (HoverThrust != 0)
        {
            HoverThrust -= NetLinker.MainBody.DroneBodyStats[0].HoverJerk * Time.fixedDeltaTime;
            if (HoverThrust < 0) HoverThrust = 0;
        }

        PhysicsAcceleration += HoverThrust * Memory;
        TotThrust += HoverThrust * Memory;

        #endregion

        #endregion

        #region Dev Commands
        InputControl.Dev.ResetDrone.performed += ResetDrone;
        #endregion


        #region Lift - Induced Drag - Torque

        Vector3 CM = DronePhysics.worldCenterOfMass;

        Vector3 Check;
        Vector3 L;
        Transform t;
        float AoA;
        for (int i = 0; i < NetLinker.Parts.DronePartStats.Length; i++)
        {
            L = new Vector3(NetLinker.Parts.DronePartStats[i].CenterMassX, NetLinker.Parts.DronePartStats[i].CenterMassY, NetLinker.Parts.DronePartStats[i].CenterMassZ);
            t = NetLinker.Parts.DronePartStats[i].PartObject.transform;
            AirSpeed = PhysicsVelocity - Wind; //+ Vector3.Cross(DronePhysics.angularVelocity, L - (PhysicsPosition + DronePhysics.centerOfMass)); 
            AirSpeed = Vector3.ProjectOnPlane(AirSpeed, t.forward);
            AoA = -Mathf.Rad2Deg * Mathf.Atan2(Vector3.Dot(AirSpeed, t.up), Vector3.Dot(AirSpeed, t.right));

            //if (i == 0) RefAirSpeed1 = AirSpeed.magnitude;
            //else if (i == 7) RefAirSpeed7 = AirSpeed.magnitude;

            #region Lift
            Memory = t.up;
            PhysicsAcceleration += Check = 0.5f * (float)C.GaleAtmD * LiftAoA[i].Evaluate(AoA) * NetLinker.Parts.DronePartStats[i].Area * AirSpeed.sqrMagnitude * Memory / DronePhysics.mass;
            TotLift += Check;
            //Debug.Log(AoA + " ; " + Check + " ; (" + Vector3.Dot(AirSpeed, t.up) + ", " + Vector3.Dot(AirSpeed, t.right) + ")");

            if (VisualizationMode == VisualizationModeType.LiftDrag)
                Debug.DrawLine(t.position, t.position + (DronePhysics.mass * Check / 20), Color.green, 1 / Time.renderedFrameCount);
            #endregion

            #region Induced Drag
            Memory = -AirSpeed.normalized;
            PhysicsAcceleration += Check = 0.5f * (float)C.GaleAtmD * InducedDragAoA[i].Evaluate(AoA) * NetLinker.Parts.DronePartStats[i].Area * AirSpeed.sqrMagnitude * Memory / DronePhysics.mass;
            TotDrag += Check;

            if (VisualizationMode == VisualizationModeType.LiftDrag)
                Debug.DrawLine(t.position, t.position + (DronePhysics.mass * Check / 20), Color.red, 1 / Time.renderedFrameCount);
            #endregion

            #region Torque

            Memory = 0.5f * (float)C.GaleAtmD * TorqueAoA[i].Evaluate(AoA) * NetLinker.Parts.DronePartStats[i].Area * AirSpeed.sqrMagnitude * ((i != 7) ? Vector3.up : Vector3.forward);
            PhysicsTorque += Check = Vector3.Cross(L - DronePhysics.centerOfMass, Memory);

            //if (i == 0) Debug.Log("i=" + i + " ; AoA " + AoA + " => " + Check.z);

            if (VisualizationMode == VisualizationModeType.Torque)
                Debug.DrawLine(t.position, t.position + (DronePhysics.mass * Memory / 100), Color.blue, 1 / Time.renderedFrameCount);
            #endregion

            #region CenterOfLift Calculation
            if (NetLinker.Parts.DronePartStats[i].ID != 8)
            {
                //PhysicsTorque += Vector3.Cross(Memory, t.TransformDirection(L - CenterOfLift));
                VectorA += L * NetLinker.Parts.DronePartStats[i].Area;
                B += NetLinker.Parts.DronePartStats[i].Area;
            }
            #endregion


            if (NetLinker.Parts.DronePartStats[i].IDb != 0)
            {
                L = new Vector3(L.x, L.y, -L.z);
                t = NetLinker.Parts.DronePartStats[i].PartObjectb.transform;
                AirSpeed = PhysicsVelocity - Wind; //+ Vector3.Cross(DronePhysics.angularVelocity, L - (PhysicsPosition + DronePhysics.centerOfMass));
                AirSpeed = Vector3.ProjectOnPlane(AirSpeed, t.forward);
                AoA = -Mathf.Rad2Deg * Mathf.Atan2(Vector3.Dot(AirSpeed, t.up), Vector3.Dot(AirSpeed, t.right));

                #region Lift
                Memory = t.up;
                PhysicsAcceleration += Check = 0.5f * (float)C.GaleAtmD * LiftAoA[i].Evaluate(AoA) * NetLinker.Parts.DronePartStats[i].Area * AirSpeed.sqrMagnitude * Memory / DronePhysics.mass;
                TotLift += Check;

                if (VisualizationMode == VisualizationModeType.LiftDrag)
                    Debug.DrawLine(CM + (DronePhysics.rotation * L), CM + (DronePhysics.rotation * L) + (DronePhysics.mass * Check / 20), Color.green, 1 / Time.renderedFrameCount);
                #endregion

                #region Induced Drag
                Memory = -AirSpeed.normalized;
                PhysicsAcceleration += Check = 0.5f * (float)C.GaleAtmD * InducedDragAoA[i].Evaluate(AoA) * NetLinker.Parts.DronePartStats[i].Area * AirSpeed.sqrMagnitude * Memory / DronePhysics.mass;
                TotDrag += Check;

                if (VisualizationMode == VisualizationModeType.LiftDrag)
                    Debug.DrawLine(t.position, t.position + (DronePhysics.mass * Check / 20), Color.red, 1 / Time.renderedFrameCount);
                #endregion

                #region Torque
                Memory = 0.5f * (float)C.GaleAtmD * TorqueAoA[i].Evaluate(AoA) * NetLinker.Parts.DronePartStats[i].Area * AirSpeed.sqrMagnitude * ((i != 7) ? Vector3.up : Vector3.forward);
                PhysicsTorque += Check = Vector3.Cross(L - DronePhysics.centerOfMass, Memory);

                //if (i == 0) Debug.Log("i=" + i + " ; AoA " + AoA + " => " + Check.z);

                if (VisualizationMode == VisualizationModeType.Torque)
                    Debug.DrawLine(t.position, t.position + (DronePhysics.mass * Memory / 100), Color.blue, 1 / Time.renderedFrameCount);
                #endregion

                #region CenterOfLift Calculation
                if (NetLinker.Parts.DronePartStats[i].ID != 8)
                {
                    //PhysicsTorque += Vector3.Cross(Check, t.TransformDirection(L - CenterOfLift));
                    VectorA += L * NetLinker.Parts.DronePartStats[i].Area;
                    B += NetLinker.Parts.DronePartStats[i].Area;
                }
                #endregion
            }
        }

        #endregion

        #region Input Controls System (ICS)

        CurrentAngles = new Vector3(DataComputer.p, DataComputer.r, DataComputer.y);
        DynamicPressure = (float)C.DensityAtHeight(PhysicsPosition.y) * AirSpeed.sqrMagnitude;

        #region Toggle Input
        InputControl.FlightControls.ToggleSASMode.performed += ToggleSASModes;
        #endregion

        #region Orientation Input

        InputValues[0] = InputControl.ControlSurfaces.PitchUp.IsPressed();
        InputValues[1] = InputControl.ControlSurfaces.PitchDown.IsPressed();
        InputValues[2] = InputControl.ControlSurfaces.RollClock.IsPressed();
        InputValues[3] = InputControl.ControlSurfaces.RollCounterClock.IsPressed();
        InputValues[4] = InputControl.ControlSurfaces.YawRight.IsPressed();
        InputValues[5] = InputControl.ControlSurfaces.YawLeft.IsPressed();

        #endregion

        switch (SASMode)
        {
            case SASModeType.Disabled:
                break;

            case SASModeType.Heli: // Hover-Heli mode
                #region Hover-Heli SAS

                #region Normalize Control Surfaces
                if (ControlSurfaceTargetAngle[0] != PitchNeutral) ControlSurfaceTargetAngle[0] = PitchNeutral;
                for (int i = 1; i < 3; i++)
                {
                    if (ControlSurfaceTargetAngle[i] != 0) ControlSurfaceTargetAngle[i] = 0;
                }
                #endregion

                #region Pitch

                if (InputValues[0] ^ InputValues[1])
                {
                    ReactionWheelsTargetTorque[0] = (InputValues[0] ? 1 : -1) * ReactionWheelsDefaultTorque.x;
                }
                else
                {
                    ReactionWheelsTargetTorque[0] = 0;
                }

                #endregion

                #region Roll

                if (InputValues[2] ^ InputValues[3])
                {
                    ReactionWheelsTargetTorque[1] = (InputValues[2] ? 1 : -1) * ReactionWheelsDefaultTorque.y;
                }
                else
                {
                    ReactionWheelsTargetTorque[1] = 0;
                }

                #endregion

                #region Yaw

                if (InputValues[4] ^ InputValues[5])
                {
                    ReactionWheelsTargetTorque[2] = (InputValues[4] ? 1 : -1) * ReactionWheelsDefaultTorque.z;
                }
                else
                {
                    ReactionWheelsTargetTorque[2] = 0;
                }

                #endregion

                #region Undesired Torque Stabilization
                float catchException;
                Vector3 T = -PhysicsTorque;
                //Debug.Log(PhysicsTorque.magnitude.ToString() + " ; " + (PhysicsTorque.magnitude - T.magnitude).ToString());

                for (int i = 0; i < 3; i++)
                {
                    catchException = ReactionWheelsTorqueStabilization[i];
                    if (Mathf.Abs((i == 0 ? T.x : (i == 1 ? T.y : T.z)) - ReactionWheelsTorqueStabilization[i]) < TorqueStabilizationJerk * Time.fixedDeltaTime)
                    {
                        ReactionWheelsTorqueStabilization[i] = i == 0 ? T.x : (i == 1 ? T.y : T.z);
                    }
                    else
                    {
                        ReactionWheelsTorqueStabilization[i] += Mathf.Sign((i == 0 ? T.x : (i == 1 ? T.y : T.z)) - ReactionWheelsTorqueStabilization[i]) * TorqueStabilizationJerk * Time.fixedDeltaTime;
                    }

                    if (Mathf.Abs(ReactionWheelsTorqueStabilization[i] - catchException) > 2 * TorqueStabilizationJerk * Time.fixedDeltaTime)
                    {
                        throw new System.Exception("Impossible Torque Stabilization Jerk: " + Mathf.Abs(ReactionWheelsTorqueStabilization[i] - catchException));
                    }
                }

                #endregion

                #region Torque to Targets

                for (int i = 0; i < 3; i++)
                {
                    if (ReactionWheelsTorque[i] != ReactionWheelsTargetTorque[i])
                    {
                        if (Mathf.Abs(ReactionWheelsTargetTorque[i] - ReactionWheelsTorque[i]) < ReactionWheelsJerk[i] * Time.fixedDeltaTime)
                            ReactionWheelsTorque[i] = ReactionWheelsTargetTorque[i];
                        else
                            ReactionWheelsTorque[i] += Mathf.Sign(ReactionWheelsTargetTorque[i] - ReactionWheelsTorque[i]) * ReactionWheelsJerk[i] * Time.fixedDeltaTime;
                    }
                }

                #endregion

                #endregion
                break;

            case SASModeType.Plane: // Plane mode
                #region Check Requirements
                if (DynamicPressure < 400)
                {
                    SASMode = SASModeType.Heli;
                    break;
                }
                #endregion

                #region Remove Undesired Torque Stabilization
                
                for (int i = 0; i < 3; i++)
                {
                    if (Mathf.Abs(ReactionWheelsTorqueStabilization[i]) < TorqueStabilizationJerk * Time.fixedDeltaTime)
                    {
                        ReactionWheelsTorqueStabilization[i] = 0;
                    }
                    else
                    {
                        ReactionWheelsTorqueStabilization[i] += -Mathf.Sign(ReactionWheelsTorqueStabilization[i]) * TorqueStabilizationJerk * Time.fixedDeltaTime;
                    }
                }
                
                #endregion

                #region Plane SAS

                #region Pitch

                if (InputValues[0] || InputValues[1])
                {
                    if (InputValues[0] && InputValues[1])
                    {
                        ControlSurfaceTargetAngle[0] = PitchNeutral;
                        if (NeutralControlsTimer[0] != InputMargin) NeutralControlsTimer[0] = InputMargin;
                    }
                    else
                    {
                        if (NeutralControlsTimer[0] == 0)
                        {
                            ControlSurfaceTargetAngle[0] += (InputValues[0] ? 1 : -1) * PitchSpeed * Time.fixedDeltaTime;
                            ControlSurfaceTargetAngle[0] = Mathf.Clamp(ControlSurfaceTargetAngle[0], -ControlSurfaceMaxAngles[0], ControlSurfaceMaxAngles[0]);
                        }
                        else NeutralControlsTimer[0] = Mathf.Min(0, NeutralControlsTimer[0] - Time.fixedDeltaTime);
                    }
                }

                #endregion

                #region Roll

                if (InputValues[2] ^ InputValues[3])
                {
                    if (Mathf.Abs(ControlSurfaceTargetAngle[1]) != AileronsDefaultAngle)
                    {
                        ControlSurfaceTargetAngle[1] = (InputValues[2] ? 1 : -1) * AileronsDefaultAngle * Mathf.Sqrt(ReferenceDynPressure / DynamicPressure);
                        //Adjustement above obtained from collecting data from a more proper testing scenario.
                    }
                }
                else
                {
                    if (ControlSurfaceTargetAngle[1] != 0)
                        ControlSurfaceTargetAngle[1] = 0;
                }

                #endregion

                #region Yaw

                if (InputValues[4] ^ InputValues[5])
                {
                    if (NeutralControlsTimer[2] == 0)
                    {
                        ControlSurfaceTargetAngle[2] += (InputValues[4] ? 1 : -1) * YawSpeed * Time.fixedDeltaTime;
                        ControlSurfaceTargetAngle[2] = Mathf.Clamp(ControlSurfaceTargetAngle[2], -ControlSurfaceMaxAngles[2], ControlSurfaceMaxAngles[2]);
                    }
                    else NeutralControlsTimer[2] = Mathf.Min(0, NeutralControlsTimer[2] - Time.fixedDeltaTime);
                }
                else if (InputValues[4] && InputValues[5])
                {
                    if (ControlSurfaceTargetAngle[2] != 0) ControlSurfaceTargetAngle[2] = 0;
                    if (NeutralControlsTimer[2] != InputMargin) NeutralControlsTimer[2] = InputMargin;
                }

                #endregion

                #endregion
                break;

            default: //to please the IntelliSense
                SASMode = SASModeType.Disabled;
                break;
        }

        #region Control Surfaces to Targets

        for (int i = 0; i < 3; i++)
        {
            if (ControlSurfaceAngles[i] != ControlSurfaceTargetAngle[i])
            {
                if (Mathf.Abs(ControlSurfaceTargetAngle[i] - ControlSurfaceAngles[i]) < ControlSurfaceAngularSpeed[i] * Time.fixedDeltaTime)
                {
                    ControlSurfaceAngles[i] = ControlSurfaceTargetAngle[i];
                }
                else
                {
                    ControlSurfaceAngles[i] += Mathf.Sign(ControlSurfaceTargetAngle[i] - ControlSurfaceAngles[i]) * ControlSurfaceAngularSpeed[i] * Time.fixedDeltaTime;
                }
            }
        }

        //Canards
        NetLinker.Parts.DronePartStats[0].PartObject.transform.localRotation = Quaternion.Euler(0, 0, ControlSurfaceAngles[0]);
        //Ailerons
        NetLinker.Parts.DronePartStats[4].PartObject.transform.localRotation = Quaternion.Euler(0, 0, ControlSurfaceAngles[1]);
        NetLinker.Parts.DronePartStats[4].PartObjectb.transform.localRotation = Quaternion.Euler(0, 0, -ControlSurfaceAngles[1]);
        NetLinker.Parts.DronePartStats[5].PartObject.transform.localRotation = Quaternion.Euler(0, 0, ControlSurfaceAngles[1]);
        NetLinker.Parts.DronePartStats[5].PartObjectb.transform.localRotation = Quaternion.Euler(0, 0, -ControlSurfaceAngles[1]);
        //Elevators
        NetLinker.Parts.DronePartStats[7].PartObject.transform.localRotation = Quaternion.Euler(90, -ControlSurfaceAngles[2], 0);
        NetLinker.Parts.DronePartStats[7].PartObjectb.transform.localRotation = Quaternion.Euler(90, -ControlSurfaceAngles[2], 0);

        #endregion

        #endregion

        #region Gather Data to Output
        /*
        if (InputControl.ControlSurfaces.PitchUp.IsPressed() && DataGatherStage == DataGatherStageType.Idle)
        {
            DataGatherStage = DataGatherStageType.Gathering;
            TimeAtGatheringStart = Time.time;
            OutputData.TableElements = new();
        }
        else if (ControlSurfaceTargetAngle[1] == 0 && DataGatherStage == DataGatherStageType.Gathering)
        {
            DataGatherStage = DataGatherStageType.Saving;
        }

        switch (DataGatherStage)
        {
            case DataGatherStageType.Gathering:
                if (!InputControl.ControlSurfaces.PitchUp.IsPressed())
                {
                    //Debug.Log(Time.time - TimeAtGatheringStart + " ; " + PhysicsTorque.z);
                    DataGatherStage = DataGatherStageType.Saving;
                }
                
                if (DataTimeTick >= DataTimeRate)
                {
                    OutputData.TableElements.Add(new(ControlSurfaceAngles[0], PhysicsTorque.z));
                    DataTimeTick -= DataTimeRate;
                }
                DataTimeTick++;
                
                break;

            case DataGatherStageType.Saving:
                File.WriteAllText(Application.streamingAssetsPath + "/OutputData.json", JsonUtility.ToJson(OutputData, true));
                DataGatherStage = DataGatherStageType.Done;
                break;

            default:
                break;
        }
        */
        #endregion

        PrevDynamicPressure = DynamicPressure;
        AirSpeed = -PhysicsVelocity + Wind;

        #region Drag Physics

        Memory = transform.right;
        MainStats s = NetLinker.MainBody.DroneBodyStats[0];

        //Forward Drag
        PhysicsAcceleration += Check = 0.5f * (float)C.GaleAtmD * (InputControl.FlightControls.AirBrakes.IsPressed() ? s.AirBrakesCd : s.FrontCd) * s.FrontArea * Mathf.Pow(Vector3.Dot(AirSpeed, Memory), 2) * Mathf.Sign(Vector3.Dot(AirSpeed, Memory)) * Memory / DronePhysics.mass;
        TotDrag += Check;
        //   Debug.Log("Wind: " + AirSpeed + "; Forward Drag: " + Check);

        Memory = transform.up;

        //Vertical Drag
        PhysicsAcceleration += Check = 0.5f * (float)C.GaleAtmD * s.BottomCd * s.BottomArea * Mathf.Pow(Vector3.Dot(AirSpeed, Memory), 2) * Mathf.Sign(Vector3.Dot(AirSpeed, Memory)) * Memory / DronePhysics.mass;
        TotDrag += Check;
        //   Debug.Log("Vertical Drag: " + Check);

        Memory = transform.forward;

        //Side Drag
        PhysicsAcceleration += Check = 0.5f * (float)C.GaleAtmD * (InputControl.FlightControls.AirBrakes.IsPressed() ? s.SideBrakesCd : s.SideCd) * (s.SideArea - (InputControl.FlightControls.AirBrakes.IsPressed() ? NetLinker.Parts.DronePartStats[7].Area : 0)) * Mathf.Pow(Vector3.Dot(AirSpeed, Memory), 2) * Mathf.Sign(Vector3.Dot(AirSpeed, Memory)) * Memory / DronePhysics.mass;
        TotDrag += Check;
        //   Debug.Log("Side Drag: " + Check);

        #endregion


        #region APPLY PHYSICS

        PhysicsTorque += new Vector3(ReactionWheelsTorqueStabilization[0], ReactionWheelsTorqueStabilization[1], ReactionWheelsTorqueStabilization[2]);

        if (!AirChamberTest)
        {
            //PhysicsVelocity += PhysicsAcceleration * Time.fixedDeltaTime / DronePhysics.mass;
            DronePhysics.AddForce(PhysicsAcceleration, ForceMode.Acceleration);
            DronePhysics.AddRelativeTorque(PhysicsTorque, ForceMode.Force);
            DronePhysics.AddRelativeTorque(new(-ReactionWheelsTorque[1], ReactionWheelsTorque[2], ReactionWheelsTorque[0]), ForceMode.Acceleration);
            //Debug.Log(PhysicsTorque);
        }
        else
        {
            DronePhysics.position = PhysicsPosition = Vector3.up;
            DronePhysics.linearVelocity = PhysicsVelocity = Vector3.zero;
            DronePhysics.AddRelativeTorque(PhysicsTorque, ForceMode.Force);
            //Debug.Log(PhysicsTorque);
        }

        #endregion


        #region Find CoF

        if (!CenterOfLiftFound)
        {
            CenterOfLift = VectorA / (float)B;
            //Debug.Log("Center of Lift: " + CenterOfLift);
            CenterOfLiftFound = true;
        }
        
        #endregion
    }

    [System.Obsolete]
    private void Update()
    {
        #region Physics Visualizer
        Vector3 CM = DronePhysics.worldCenterOfMass;

        switch (VisualizationMode)
        {
            case VisualizationModeType.Off:
                break;

            case VisualizationModeType.Forces:
                Debug.DrawLine(CM, CM + (PhysicsVelocity / 20), Color.yellow, 1 / Time.renderedFrameCount);

                Debug.DrawLine(CM, CM + (DronePhysics.mass * TotThrust / 20), Color.blue, 1 / Time.renderedFrameCount);
                Debug.DrawLine(CM, CM + (DronePhysics.mass * TotDrag / 20), Color.red, 1 / Time.renderedFrameCount);
                Debug.DrawLine(CM, CM + (DronePhysics.mass * TotLift / 20), Color.green, 1 / Time.renderedFrameCount);
                Debug.DrawLine(CM, CM + (DronePhysics.mass * TotWeight / 20), Color.grey, 1 / Time.renderedFrameCount);
                break;

            case VisualizationModeType.LiftDrag:
                //Check Lift and Induced Drag code regions
                break;

            case VisualizationModeType.Torque:
                //Check Torque code regions

                Debug.DrawLine(CM - (DronePhysics.rotation * new Vector3(0, 0.2f)), CM + (DronePhysics.rotation * new Vector3(0, 0.2f)), Color.yellow, 1 / Time.renderedFrameCount);
                Debug.DrawLine(CM - (DronePhysics.rotation * new Vector3(0, 0, 0.2f)), CM + (DronePhysics.rotation * new Vector3(0, 0, 0.2f)), Color.yellow, 1 / Time.renderedFrameCount);

                Debug.DrawLine(PhysicsPosition + (DronePhysics.rotation * CenterOfLift) - (DronePhysics.rotation * new Vector3(0, 0.2f)), PhysicsPosition + (DronePhysics.rotation * CenterOfLift) + (DronePhysics.rotation * new Vector3(0, 0.2f)), Color.cyan, 1 / Time.renderedFrameCount);
                Debug.DrawLine(PhysicsPosition + (DronePhysics.rotation * CenterOfLift) - (DronePhysics.rotation * new Vector3(0, 0, 0.2f)), PhysicsPosition + (DronePhysics.rotation * CenterOfLift) + (DronePhysics.rotation * new Vector3(0, 0, 0.2f)), Color.cyan, 1 / Time.renderedFrameCount);
                break;
        }
        #endregion

        #region Wind Visualizer

        if (WindParticlesEnabled)
        {
            AirSpeed = PhysicsVelocity - Wind;
            if (AirSpeed.magnitude >= 1f)
            {
                if (!WindParticles.enableEmission) WindParticles.enableEmission = true;

                WindParticles.transform.position = CM + (ParticlesOriginDistance * (AirSpeed.magnitude / 25f) * AirSpeed.normalized);
                Vector3 forwardTarget = -Vector3.ProjectOnPlane((AirSpeed.normalized.y < 0.999f) ? Vector3.up : Vector3.forward, AirSpeed);
                WindParticles.transform.rotation = Quaternion.LookRotation(forwardTarget, Quaternion.AngleAxis(90, AirSpeed) * forwardTarget);
                WindParticles.startLifetime = 5f;

                ParticleSystem.VelocityOverLifetimeModule ParticlesVel = WindParticles.velocityOverLifetime;
                ParticlesVel.xMultiplier = AirSpeed.magnitude;
            }
            else if (WindParticles.enableEmission)
                WindParticles.enableEmission = false;
        }

        #endregion
    }

    #region Toggle SAS Modes

    private void ToggleSASModes(InputAction.CallbackContext obj)
    {
        SASMode = SASMode switch
        {
            SASModeType.Disabled => SASModeType.Heli,
            SASModeType.Heli => SASModeType.Plane,
            SASModeType.Plane => SASModeType.Disabled,
            _ => SASModeType.Disabled
        };
        InputControl.FlightControls.ToggleSASMode.performed -= ToggleSASModes;
        return;
    }

    #endregion

    #region Hover Modes Control
    private void ToggleAutoMode(InputAction.CallbackContext obj)
    {
        HoverAuto = !HoverAuto;
        InputControl.FlightControls.MouseClick.performed -= ToggleAutoMode;
        return;
    }

    private void SwitchModes(InputAction.CallbackContext obj)
    {
        int mode = (int)HoverMode;
        mode += InputControl.FlightControls.ToggleHoverMode.ReadValue<float>() > 0 ? (mode == 6 ? -2 : -1) : (mode == 4 ? 2 : 1);
        mode = Mathf.Clamp(mode, 1, 6);
        HoverMode = (HoverModeType)mode;
        InputControl.FlightControls.ToggleHoverMode.performed -= SwitchModes;
        return;
    }
    #endregion

    #region Reset Drone Position, Velocity, and Engines
    private void ResetDrone(InputAction.CallbackContext obj)
    {
        InverseDistanceWeighting.Query = PhysicsPosition = DronePhysics.position = ResetPosition;
        PhysicsVelocity = DronePhysics.linearVelocity = Vector3.zero;
        if (Air != null) Air.DronePosition = PhysicsPosition;
        PhysicsAcceleration = TotWeight = ((float)C.GaleG * Vector3.down) + (NetLinker.MainBody.DroneBodyStats[0].DroneVolume * (float)C.GaleAtmD * Vector3.up / DronePhysics.mass);
        PhysicsRotation = DronePhysics.rotation = Quaternion.Euler(0f, 0f, 0f);
        PhysicsAngVelocity = DronePhysics.angularVelocity = Vector3.zero;
        PhysicsTorque = Vector3.zero;

        Thrust = 0;        HoverThrust = 0; HoverTarget = 0;        ImpulseCharge = 0;

        //Wind Vector: Tailwind is a Positive X while Headwind is a Negative X ; Climbing is a Negative Y while Descending is a Positive Y
        if (!AirChamberTest && InverseDistanceWeighting.Values != null)
            Wind = (NetLinker.MainBody.DroneBodyStats[0].YesWind && Time.time > 2) ? new Vector3(InverseDistanceWeighting.Values[0], InverseDistanceWeighting.Values[1], InverseDistanceWeighting.Values[2]) : Vector3.zero;

        InputControl.Dev.ResetDrone.performed -= ResetDrone;
        return;
    }
    #endregion

    public void CalculateFlightCoefficients(int i)
    {
        float memmoi, CN, CT, CM, AoA;
        //float mem1, mem2, mem2s, mem2c, mem3;
        LiftAoA[i] = new();
        InducedDragAoA[i] = new();
        TorqueAoA[i] = new();
        float iStep = 2;

        PartStats p = NetLinker.Parts.DronePartStats[i];
        MainStats d = NetLinker.MainBody.DroneBodyStats[0];

        #region Lift - Low AoAs
        memmoi = d.LiftCurveSlope * (AspectRatio[i] / (AspectRatio[i] + (2 * (AspectRatio[i] + 4) / (AspectRatio[i] + 2))));
        
        _ = LiftAoA[i].AddKey(new(0, 0, memmoi, memmoi));
        _ = LiftAoA[i].AddKey(new(180, 0, memmoi, 0));
        _ = LiftAoA[i].AddKey(new(-180, 0, 0, memmoi));
        _ = LiftAoA[i].AddKey(new(d.StallAngle, d.StallAngle * memmoi, memmoi, 0));
        _ = LiftAoA[i].AddKey(new(-d.StallAngle, -d.StallAngle * memmoi, 0, memmoi));
        _ = LiftAoA[i].AddKey(new(180 - d.StallAngle, -d.StallAngle * memmoi, 0, memmoi));
        _ = LiftAoA[i].AddKey(new(-180 + d.StallAngle, d.StallAngle * memmoi, memmoi, 0));
        #endregion

        #region Induced Drag - Low AoAs
        AoA = Mathf.Deg2Rad * (d.StallAngle - (d.StallAngle * memmoi / (Mathf.PI * AspectRatio[i])));
        CT = d.FrontCd * Mathf.Cos(AoA);
        CN = ((d.StallAngle * memmoi) + (CT * Mathf.Sin(AoA))) / Mathf.Cos(AoA);

        _ = InducedDragAoA[i].AddKey(new(0, 0, 0, 0));
        _ = InducedDragAoA[i].AddKey(new(180, 0, 0, 0));
        _ = InducedDragAoA[i].AddKey(new(-180, 0, 0, 0));
        _ = InducedDragAoA[i].AddKey(d.StallAngle, (CN * Mathf.Sin(AoA)) + (CT * Mathf.Cos(AoA)));
        _ = InducedDragAoA[i].AddKey(180 - d.StallAngle, (CN * Mathf.Sin(AoA)) + (CT * Mathf.Cos(AoA)));
        _ = InducedDragAoA[i].AddKey(-d.StallAngle, (CN * Mathf.Sin(AoA)) + (CT * Mathf.Cos(AoA)));
        _ = InducedDragAoA[i].AddKey(-180 + d.StallAngle, (CN * Mathf.Sin(AoA)) + (CT * Mathf.Cos(AoA)));

        for (int i2 = 0; i2 < d.StallAngle / iStep; i2++)
        {
            AoA = Mathf.Deg2Rad * ((i2 * iStep) - ((i2 * iStep) * memmoi / (Mathf.PI * AspectRatio[i])));
            CT = NetLinker.MainBody.DroneBodyStats[0].FrontCd * Mathf.Cos(AoA);
            CN = (((i2 * iStep) * memmoi) + (CT * Mathf.Sin(AoA))) / Mathf.Cos(AoA);

            _ = InducedDragAoA[i].AddKey(i2 * iStep, (CN * Mathf.Sin(AoA)) + (CT * Mathf.Cos(AoA)));
            _ = InducedDragAoA[i].AddKey(-i2 * iStep, (CN * Mathf.Sin(AoA)) + (CT * Mathf.Cos(AoA)));
            _ = InducedDragAoA[i].AddKey(180 - (i2 * iStep), (CN * Mathf.Sin(AoA)) + (CT * Mathf.Cos(AoA)));
            _ = InducedDragAoA[i].AddKey(-180 + (i2 * iStep), (CN * Mathf.Sin(AoA)) + (CT * Mathf.Cos(AoA)));
        }
        #endregion

        #region Torque - Low AoAs

        _ = TorqueAoA[i].AddKey(new(0, 0));
        _ = TorqueAoA[i].AddKey(new(180, 0));
        _ = TorqueAoA[i].AddKey(new(-180, 0));

        for (int i2 = 0; i2 < d.StallAngle / iStep; i2++)
        {
            AoA = Mathf.Deg2Rad * ((i2 * iStep) - ((i2 * iStep) * memmoi / (Mathf.PI * AspectRatio[i])));
            CT = d.FrontCd * Mathf.Cos(AoA);
            CN = (((i2 * iStep) * memmoi) + (CT * Mathf.Sin(AoA))) / Mathf.Cos(AoA);
            CM = CN * (float)(0.25 - (0.175 * (1 - (2 * AoA / Mathf.PI))));

            _ = TorqueAoA[i].AddKey(i2 * iStep, CM);
            _ = TorqueAoA[i].AddKey(-i2 * iStep, -CM);
            _ = TorqueAoA[i].AddKey(180 - (i2 * iStep), CM);
            _ = TorqueAoA[i].AddKey(-180 + (i2 * iStep), -CM);
        }

        AoA = Mathf.Deg2Rad * (d.StallAngle - (d.StallAngle * memmoi / (Mathf.PI * AspectRatio[i])));
        CT = d.FrontCd * Mathf.Cos(AoA);
        CN = ((d.StallAngle * memmoi) + (CT * Mathf.Sin(AoA))) / Mathf.Cos(AoA);
        CM = 5 * CN * (float)(0.25 - (0.175 * (1 - (2 * AoA / Mathf.PI))));

        _ = TorqueAoA[i].AddKey(d.StallAngle, CM);
        _ = TorqueAoA[i].AddKey(-d.StallAngle, -CM);
        _ = TorqueAoA[i].AddKey(180 - d.StallAngle, CM);
        _ = TorqueAoA[i].AddKey(-180 + d.StallAngle, -CM);

        #endregion

        for (int i2 = 0; i2 < (180 - (2 * (d.StallAngle + 10))) / iStep; i2++)
        {
            AoA = (d.StallAngle + 10) + (i2 * iStep) - 180;
            memmoi = -Mathf.Abs(Mathf.Deg2Rad * (Mathf.Abs(AoA) - 0)); //(-d.MaxInducedAoA * Mathf.Abs(AoA) / (90 - (d.StallAngle + 10)))

            CN = d.BottomCd * Mathf.Sin(memmoi) * ((1f / (0.56f + (0.44f * Mathf.Sin(memmoi)))) - (0.41f * (1 - Mathf.Exp(-17f / AspectRatio[i]))));
            CT = 0.5f * d.FrontCd * Mathf.Cos(memmoi);

            #region Lift - High AoAs
            _ = LiftAoA[i].AddKey(new(d.StallAngle + 10 + (i2 * iStep), CN * Mathf.Cos(memmoi) - (CT * Mathf.Sin(memmoi))));
            _ = LiftAoA[i].AddKey(new(d.StallAngle + 10 + (i2 * iStep) - 180, CN * Mathf.Cos(memmoi) - (CT * Mathf.Sin(memmoi))));
            #endregion

            #region Induced Drag - High AoAs
            _ = InducedDragAoA[i].AddKey(d.StallAngle + 10 + (i2 * iStep), (CN * Mathf.Sin(memmoi)) + (CT * Mathf.Cos(memmoi)));
            _ = InducedDragAoA[i].AddKey(-(d.StallAngle + 10 + (i2 * iStep)), (CN * Mathf.Sin(memmoi)) + (CT * Mathf.Cos(memmoi)));
            #endregion

            #region Torque - High AoAs
            CM = CN * (float)(0.25 - (0.175 * (1 - (2 * AoA / Mathf.PI))));

            _ = TorqueAoA[i].AddKey(d.StallAngle + 10 + (i2 * iStep), CM);
            _ = TorqueAoA[i].AddKey(-(d.StallAngle + 10 + (i2 * iStep)), -CM);
            #endregion
        }

        #region Smoothing Tangents

        for (int i2 = 2; i2 < LiftAoA[i].length - 2; i2++)
        {
            if (Mathf.Abs(LiftAoA[i].keys[i2].time) != 0 && Mathf.Abs(LiftAoA[i].keys[i2].time) != d.StallAngle)
            {
                LiftAoA[i].SmoothTangents(i2, 1);
            }
        }

        #endregion
    }
}


#region discarded control surfaces control
/*
        #region Control Canards

        if (InputControl.FlightControls.Pitch.IsPressed())
        {
            ControlSurfacePositivePriority[0] = InputControl.FlightControls.Pitch == 1;

            NetLinker.Parts.DronePartStats[0].PartObject.transform.localRotation = Quaternion.Euler(0, 0, ControlSurfaceAngle[0]);

            NetLinker.Parts.DronePartStats[0].PartObject.transform.localRotation = Quaternion.Euler(0, 0, NetLinker.Parts.DronePartStats[0].ControlAngle * InputControl.FlightControls.Pitch.ReadValue<float>());

            //DronePhysics.AddRelativeTorque(0, 0, 0.2f * DronePhysics.mass * InputControl.FlightControls.Pitch.ReadValue<float>());
        }

        #endregion
        
        #region Control Ailerons

        if (InputControl.FlightControls.Roll.IsPressed())
        {
            NetLinker.Parts.DronePartStats[5].PartObject.transform.localRotation = Quaternion.Euler(0, 0, -NetLinker.Parts.DronePartStats[5].ControlAngle * InputControl.FlightControls.Roll.ReadValue<float>());
            NetLinker.Parts.DronePartStats[5].PartObjectb.transform.localRotation = Quaternion.Euler(0, 0, NetLinker.Parts.DronePartStats[5].ControlAngle * InputControl.FlightControls.Roll.ReadValue<float>());

            DronePhysics.AddRelativeTorque(0.25f * DronePhysics.mass * InputControl.FlightControls.Roll.ReadValue<float>(), 0, 0);
        }
        else
        {
            NetLinker.Parts.DronePartStats[5].PartObject.transform.localRotation = Quaternion.Euler(0, 0, 0);
            NetLinker.Parts.DronePartStats[5].PartObjectb.transform.localRotation = Quaternion.Euler(0, 0, 0);
        }

        #endregion

        #region Control Rudder Tails

        if (InputControl.FlightControls.Yaw.IsPressed())
        {
            NetLinker.Parts.DronePartStats[7].PartObject.transform.localRotation = Quaternion.Euler(90, NetLinker.Parts.DronePartStats[7].ControlAngle * InputControl.FlightControls.Yaw.ReadValue<float>(), 0);
            NetLinker.Parts.DronePartStats[7].PartObjectb.transform.localRotation = Quaternion.Euler(-90, NetLinker.Parts.DronePartStats[7].ControlAngle * InputControl.FlightControls.Yaw.ReadValue<float>(), 0);

            DronePhysics.AddRelativeTorque(0, -0.2f * DronePhysics.mass * InputControl.FlightControls.Yaw.ReadValue<float>(), 0);
        }
        else
        {
            NetLinker.Parts.DronePartStats[7].PartObject.transform.localRotation = Quaternion.Euler(90, 0, 0);
            NetLinker.Parts.DronePartStats[7].PartObjectb.transform.localRotation = Quaternion.Euler(-90, 0, 0);
        }

        #endregion
        */
#endregion

#region discarded Lift, Induced Drag, Torque
/*
for (int i = 0; i < NetLinker.Parts.DronePartStats.Length; i++)
{
    L = new Vector3(NetLinker.Parts.DronePartStats[i].CenterMassX, NetLinker.Parts.DronePartStats[i].CenterMassY, NetLinker.Parts.DronePartStats[i].CenterMassZ);

    #region Lift, Induced Drag, Torque
    AirSpeed = PhysicsVelocity - Wind + Vector3.Cross(DronePhysics.angularVelocity, L - (PhysicsPosition + DronePhysics.centerOfMass));
    Transform t = NetLinker.Parts.DronePartStats[i].PartObject.transform;

    if (i != 7) Debug.DrawLine(DronePhysics.position, DronePhysics.position + t.up, Color.green, 0.01f);        //up = up
    if (i != 7) Debug.DrawLine(DronePhysics.position, DronePhysics.position + t.right, Color.red, 0.01f);       //forward = right

    AoA = AirSpeed.magnitude < 0.01f ? 0 : -Mathf.Rad2Deg * (Mathf.Atan2(Vector3.Dot(AirSpeed, t.up), Vector3.Dot(AirSpeed, t.right)) - ((Vector3.Dot(AirSpeed, t.up) > 0 && Vector3.Dot(AirSpeed, t.right) < 0) ? Mathf.PI : 0));
    AirSpeed = Vector3.ProjectOnPlane(AirSpeed, t.forward);
    if (i == 0) VectorB = AirSpeed;
    if (i != 7) Debug.Log("AoA: " + AoA);

    Memory = t.up;

    #region Lift Force
    PhysicsAcceleration += Check = 0.5f * (float)C.GaleAtmD * LiftAoA[i].Evaluate(AoA) * NetLinker.Parts.DronePartStats[i].Area * AirSpeed.sqrMagnitude * Memory / DronePhysics.mass;
    //   Debug.Log("i = " + i + " : " + Check + " ; " + Memory);
    //   Debug.DrawLine(DronePhysics.position + DronePhysics.centerOfMass, DronePhysics.position + DronePhysics.centerOfMass + Check, Color.magenta, 0.01f);
    if (Check.magnitude > 1000)
    {
        Debug.Log("Help");
    }
    #endregion

    Memory = -t.forward;

    #region Induced Drag Force
    //PhysicsAcceleration += Check = 0.5f * (float)C.GaleAtmD * InducedDragAoA[i].Evaluate(AoA) * NetLinker.Parts.DronePartStats[i].Area * AirSpeed.sqrMagnitude * Memory / DronePhysics.mass;
    //    Debug.Log("i = " + i + " : " + Check + " ; " + Memory);
    #endregion

    Memory = L.magnitude * Vector3.Cross(L, t.up).normalized;
    //
    #region Torque
    //PhysicsAngAcceleration += Check = 0.5f * (float)C.GaleAtmD * TorqueAoA[i].Evaluate(AoA) * NetLinker.Parts.DronePartStats[i].Area * AirSpeed.sqrMagnitude * Memory / DronePhysics.mass;
    if (Time.time > 10)
    {
    //    Debug.Log("i = " + i + " : " + Check + " ; " + Memory);
    }
    #endregion

    if (NetLinker.Parts.DronePartStats[i].IDb != 0)
    {
        L = new Vector3(L.x, L.y, -L.z);

        AirSpeed = PhysicsVelocity - Wind + Vector3.Cross(DronePhysics.angularVelocity, new Vector3(NetLinker.Parts.DronePartStats[i].CenterMassX, NetLinker.Parts.DronePartStats[i].CenterMassY, -NetLinker.Parts.DronePartStats[i].CenterMassZ) - (PhysicsPosition + DronePhysics.centerOfMass));
        t = NetLinker.Parts.DronePartStats[i].PartObjectb.transform;

        AoA = AirSpeed.magnitude < 0.01f ? 0 : -Mathf.Rad2Deg * (Mathf.Atan2(Vector3.Dot(AirSpeed, t.up), Vector3.Dot(AirSpeed, t.right)) - ((Vector3.Dot(AirSpeed, t.up) > 0 && Vector3.Dot(AirSpeed, t.right) < 0) ? Mathf.PI : 0));
        AirSpeed = Vector3.ProjectOnPlane(AirSpeed, t.up);
        if (i != 7) Debug.Log("AoA: " + AoA);

        Memory = t.up;

        #region Lift Force
        PhysicsAcceleration += Check = 0.5f * (float)C.GaleAtmD * LiftAoA[i].Evaluate(AoA) * NetLinker.Parts.DronePartStats[i].Area * AirSpeed.sqrMagnitude * Memory / DronePhysics.mass;
        //   Debug.Log("i = " + i + " : " + Check + " ; " + Memory);
        //   Debug.DrawLine(DronePhysics.position + DronePhysics.centerOfMass, DronePhysics.position + DronePhysics.centerOfMass + Check, Color.magenta, 0.01f);
        #endregion

        Memory = -t.forward;

        #region Induced Drag Force
        //PhysicsAcceleration += Check = 0.5f * (float)C.GaleAtmD * InducedDragAoA[i].Evaluate(AoA) * NetLinker.Parts.DronePartStats[i].Area * AirSpeed.sqrMagnitude * Memory / DronePhysics.mass;
        //    Debug.Log("i = " + i + " : " + Check + " ; " + Memory);
        #endregion

        Memory = L.magnitude * Vector3.Cross(L, t.up).normalized;

        #region Torque
        //PhysicsAngAcceleration += Check = 0.5f * (float)C.GaleAtmD * TorqueAoA[i].Evaluate(AoA) * NetLinker.Parts.DronePartStats[i].Area * AirSpeed.sqrMagnitude * Memory / DronePhysics.mass;
        //    Debug.Log("i = " + i + " : " + Check + " ; " + Memory);
        #endregion
    }
    #endregion
}*/
#endregion

#region discarded Control Surfaces and SAS
/*
DynamicPressure = (float)C.GaleAtmD * RefAirSpeed1;

if (PrevDynamicPressure == 0)
    PrevDynamicPressure = DynamicPressure;

int threshold1 = 81;
int threshold2 = 225;

if (DynamicPressure >= threshold1) //Above 18 m/s
{
    #region Control Surfaces and Reaction Wheels Speed

    if (DynamicPressure >= threshold2) //Above 50 m/s
    {
        pctrl = CanardsCtrl * (50 * (float)C.GaleAtmD / DynamicPressure);
        rctrl = AileronsCtrl * (50 * (float)C.GaleAtmD / DynamicPressure);
        yctrl = ElevatorsCtrl * (50 * (float)C.GaleAtmD / DynamicPressure);
        //yctrl = YawCtrl * (((float)C.GaleAtmD / (float)C.GaleAtmD) * (900 / RefSqrAirSpeed7));
        //Debug.Log("Control Surfaces: " + pctrl + " ; " + rctrl + " ; " + yctrl + " .");

        if (ReactionWheelsPower != 0) ReactionWheelsPower = 0;
    }
    else
    {
        pctrl = CanardsCtrl;
        rctrl = AileronsCtrl;
        yctrl = ElevatorsCtrl;

        ReactionWheelsPower = 1 - Mathf.Pow((DynamicPressure - threshold1) / (threshold2 - threshold1), 1);
    }
    #endregion

    #region Pitch
    //Pitch Control Assist Augmentations:
    //  Higher controls at low canards angles (10 times at angles between -0.25 and 0.25);
    //  Reason: Low canards angles seem to have no effect on actual pitch, but begin to at high speeds
    if (Mathf.Abs(ControlSurfaceAngle[0]) <= 0.25)
    {
        pctrl *= 10;
    }

    bool csp = InputControl.ControlSurfaces.PitchUp.IsPressed();
    bool csn = InputControl.ControlSurfaces.PitchDown.IsPressed();
    if (csp && !csn)
    {
        ControlSurfaceAngle[0] += pctrl * Time.fixedDeltaTime;

        if (ReactionWheelsPower != 0)
            ReactionWheelsTorque[0] += PitchJerk * ReactionWheelsPower * Time.fixedDeltaTime;
    }
    else if (!csp && csn)
    {
        ControlSurfaceAngle[0] -= pctrl * Time.fixedDeltaTime;

        if (ReactionWheelsPower != 0)
            ReactionWheelsTorque[0] -= PitchJerk * ReactionWheelsPower * Time.fixedDeltaTime;
    }
    else if (csp && csn && (ControlSurfaceAngle[0] != 0 || ReactionWheelsTorque[0] != 0))
    {
        ControlSurfaceAngle[0] += (Mathf.Abs(ControlSurfaceAngle[0]) > pctrl / 5)
            ? Mathf.Sign(-ControlSurfaceAngle[0]) * pctrl * Time.fixedDeltaTime
            : -ControlSurfaceAngle[0];

        if (ReactionWheelsPower != 0)
            ReactionWheelsTorque[0] += (Mathf.Abs(ReactionWheelsTorque[0]) > PitchJerk * ReactionWheelsPower / 5)
                ? Mathf.Sign(-ReactionWheelsTorque[0]) * PitchJerk * ReactionWheelsPower * Time.fixedDeltaTime
                : -ReactionWheelsTorque[0];
    }


    if (csp || csn)
    {
        NetLinker.Parts.DronePartStats[0].PartObject.transform.localRotation = Quaternion.Euler(0, 0, ControlSurfaceAngle[0]);
    }
    else 
    {
        if (ControlSurfaceAngle[0] != 0 && Mathf.Abs(ControlSurfaceAngle[0]) < pctrl / 5)
        {
            ControlSurfaceAngle[0] = 0;
            NetLinker.Parts.DronePartStats[0].PartObject.transform.localRotation = Quaternion.Euler(0, 0, 0);
        }

        if (ReactionWheelsTorque[0] != 0 && Mathf.Abs(ReactionWheelsTorque[0]) < PitchJerk * ReactionWheelsPower / 5)
        {
            ReactionWheelsTorque[0] = 0;
        }
    }

    #endregion

    #region Roll
    //Roll Control Assist Augmentations:
    //  Lowered controls at high canards angle (progressive: 1/7 above or at 0.35 and 1/14 above or at 0.45);
    //  Higher controls on retraction to neutral speed (x2);

    if (Mathf.Abs(ControlSurfaceAngle[0]) >= 0.35f)
        rctrl /= 7;
    if (Mathf.Abs(ControlSurfaceAngle[0]) >= 0.45f)
        rctrl /= 2;

    csp = InputControl.ControlSurfaces.RollClock.IsPressed();
    csn = InputControl.ControlSurfaces.RollCounterClock.IsPressed();
    if (csp && !csn)
    {
        ControlSurfaceAngle[1] += rctrl * Time.fixedDeltaTime;

        if (ReactionWheelsPower != 0)
            ReactionWheelsTorque[1] += RollJerk * ReactionWheelsPower * Time.fixedDeltaTime;
    }
    else if (!csp && csn)
    {
        ControlSurfaceAngle[1] -= rctrl * Time.fixedDeltaTime;

        if (ReactionWheelsPower != 0)
            ReactionWheelsTorque[1] -= RollJerk * ReactionWheelsPower * Time.fixedDeltaTime;
    }
    else if (csp && csn && (ControlSurfaceAngle[1] != 0 || ReactionWheelsTorque[1] != 0))
    {
        ControlSurfaceAngle[1] += (Mathf.Abs(ControlSurfaceAngle[1]) > rctrl / 5)
            ? (ControlSurfaceAngle[1] < 0 ? 2 : -2) * rctrl * Time.fixedDeltaTime
            : -ControlSurfaceAngle[1];

        if (ReactionWheelsPower != 0)
            ReactionWheelsTorque[1] += (Mathf.Abs(ReactionWheelsTorque[1]) > RollJerk * ReactionWheelsPower / 5)
                ? (ReactionWheelsTorque[1] < 0 ? 2 : -2) * RollJerk * ReactionWheelsPower * Time.fixedDeltaTime
                : -ReactionWheelsTorque[1];
    }


    if (csp || csn)
    {
        NetLinker.Parts.DronePartStats[4].PartObject.transform.localRotation = Quaternion.Euler(0, 0, ControlSurfaceAngle[1] / 2);
        NetLinker.Parts.DronePartStats[4].PartObjectb.transform.localRotation = Quaternion.Euler(0, 0, -ControlSurfaceAngle[1] / 2);
        NetLinker.Parts.DronePartStats[5].PartObject.transform.localRotation = Quaternion.Euler(0, 0, ControlSurfaceAngle[1]);
        NetLinker.Parts.DronePartStats[5].PartObjectb.transform.localRotation = Quaternion.Euler(0, 0, -ControlSurfaceAngle[1]);
    }
    else
    {
        if (ControlSurfaceAngle[1] != 0 && Mathf.Abs(ControlSurfaceAngle[1]) < rctrl / 5)
        {
            ControlSurfaceAngle[1] = 0;
            NetLinker.Parts.DronePartStats[4].PartObject.transform.localRotation = Quaternion.Euler(0, 0, 0);
            NetLinker.Parts.DronePartStats[4].PartObjectb.transform.localRotation = Quaternion.Euler(0, 0, 0);
            NetLinker.Parts.DronePartStats[5].PartObject.transform.localRotation = Quaternion.Euler(0, 0, 0);
            NetLinker.Parts.DronePartStats[5].PartObjectb.transform.localRotation = Quaternion.Euler(0, 0, 0);
        }

        if (ReactionWheelsTorque[1] != 0 && Mathf.Abs(ReactionWheelsTorque[1]) < RollJerk * ReactionWheelsPower / 5)
        {
            ReactionWheelsTorque[1] = 0;
        }
    }

    #endregion

    #region Yaw

    csp = InputControl.ControlSurfaces.YawRight.IsPressed();
    csn = InputControl.ControlSurfaces.YawLeft.IsPressed();
    if (csp && !csn)
    {
        ControlSurfaceAngle[2] += yctrl * Time.fixedDeltaTime;

        if (ReactionWheelsPower != 0)
            ReactionWheelsTorque[2] += YawJerk * ReactionWheelsPower * Time.fixedDeltaTime;
    }
    else if (!csp && csn)
    {
        ControlSurfaceAngle[2] -= yctrl * Time.fixedDeltaTime;

        if (ReactionWheelsPower != 0)
            ReactionWheelsTorque[2] -= YawJerk * ReactionWheelsPower * Time.fixedDeltaTime;
    }
    else if (csp && csn && (ControlSurfaceAngle[2] != 0 || ReactionWheelsTorque[2] != 0))
    {
        ControlSurfaceAngle[2] += (Mathf.Abs(ControlSurfaceAngle[2]) > yctrl / 10)
            ? Mathf.Sign(-ControlSurfaceAngle[2]) * yctrl * Time.fixedDeltaTime
            : -ControlSurfaceAngle[2];

        if (ReactionWheelsPower != 0)
            ReactionWheelsTorque[2] += (Mathf.Abs(ReactionWheelsTorque[2]) > YawJerk * ReactionWheelsPower / 5)
                ? Mathf.Sign(-ReactionWheelsTorque[2]) * YawJerk * ReactionWheelsPower * Time.fixedDeltaTime
                : -ReactionWheelsTorque[2];
    }


    if (csp || csn)
    {
        NetLinker.Parts.DronePartStats[7].PartObject.transform.localRotation = Quaternion.Euler(90, -ControlSurfaceAngle[2], 0);
        NetLinker.Parts.DronePartStats[7].PartObjectb.transform.localRotation = Quaternion.Euler(90, -ControlSurfaceAngle[2], 0);
    }
    else
    {
        if (ControlSurfaceAngle[2] != 0 && Mathf.Abs(ControlSurfaceAngle[2]) < yctrl / 10)
        {
            ControlSurfaceAngle[2] = 0;
            NetLinker.Parts.DronePartStats[7].PartObject.transform.localRotation = Quaternion.Euler(90, 0, 0);
            NetLinker.Parts.DronePartStats[7].PartObjectb.transform.localRotation = Quaternion.Euler(90, 0, 0);
        }

        if (ReactionWheelsTorque[2] != 0 && Mathf.Abs(ReactionWheelsTorque[2]) < YawJerk * ReactionWheelsPower / 5)
        {
            ReactionWheelsTorque[2] = 0;
        }
    }

    #endregion
}
else
{
    #region Neutral Canards

    if (ControlSurfaceAngle[0] != 0)
    {
        ControlSurfaceAngle[0] += (Mathf.Abs(ControlSurfaceAngle[0]) > CanardsCtrl / 5)
            ? Mathf.Sign(-ControlSurfaceAngle[0]) * CanardsCtrl * Time.fixedDeltaTime
            : -ControlSurfaceAngle[0];

        NetLinker.Parts.DronePartStats[0].PartObject.transform.localRotation = Quaternion.Euler(0, 0, ControlSurfaceAngle[0]);
    }
    #endregion

    #region Neutral Ailerons

    if (ControlSurfaceAngle[1] != 0)
    {
        ControlSurfaceAngle[1] += (Mathf.Abs(ControlSurfaceAngle[1]) > AileronsCtrl / 5)
            ? Mathf.Sign(-ControlSurfaceAngle[1]) * AileronsCtrl * Time.fixedDeltaTime
            : -ControlSurfaceAngle[1];

        NetLinker.Parts.DronePartStats[4].PartObject.transform.localRotation = Quaternion.Euler(0, 0, ControlSurfaceAngle[1] / 2);
        NetLinker.Parts.DronePartStats[4].PartObjectb.transform.localRotation = Quaternion.Euler(0, 0, -ControlSurfaceAngle[1] / 2);
        NetLinker.Parts.DronePartStats[5].PartObject.transform.localRotation = Quaternion.Euler(0, 0, ControlSurfaceAngle[1]);
        NetLinker.Parts.DronePartStats[5].PartObjectb.transform.localRotation = Quaternion.Euler(0, 0, -ControlSurfaceAngle[1]);
    }
    #endregion

    #region Neutral Elevators

    if (ControlSurfaceAngle[2] != 0)
    {
        ControlSurfaceAngle[2] += (Mathf.Abs(ControlSurfaceAngle[2]) > ElevatorsCtrl / 5)
            ? Mathf.Sign(-ControlSurfaceAngle[2]) * ElevatorsCtrl * Time.fixedDeltaTime
            : -ControlSurfaceAngle[2];

        NetLinker.Parts.DronePartStats[7].PartObject.transform.localRotation = Quaternion.Euler(90, -ControlSurfaceAngle[2], 0);
        NetLinker.Parts.DronePartStats[7].PartObjectb.transform.localRotation = Quaternion.Euler(90, -ControlSurfaceAngle[2], 0);
    }
    #endregion

    #region Reaction Wheels

    if (ReactionWheelsPower != 1) ReactionWheelsPower = 1;

    #region Pitch
    bool csp = InputControl.ControlSurfaces.PitchUp.IsPressed();
    bool csn = InputControl.ControlSurfaces.PitchDown.IsPressed();
    if (csp && !csn)
    {
        ReactionWheelsTorque[0] += PitchJerk * Time.fixedDeltaTime;
    }
    else if (!csp && csn)
    {
        ReactionWheelsTorque[0] -= PitchJerk * Time.fixedDeltaTime;
    }
    else if (!(csp ^ csn) && ReactionWheelsTorque[0] != 0)
    {
        ReactionWheelsTorque[0] += (Mathf.Abs(ReactionWheelsTorque[0]) > PitchJerk / 5)
            ? (ReactionWheelsTorque[0] < 0 ? 3 : -3) * PitchJerk * Time.fixedDeltaTime
            : -ReactionWheelsTorque[0];
    }
    #endregion

    #region Roll
    csp = InputControl.ControlSurfaces.RollClock.IsPressed();
    csn = InputControl.ControlSurfaces.RollCounterClock.IsPressed();
    if (csp && !csn)
    {
        ReactionWheelsTorque[1] += RollJerk * Time.fixedDeltaTime;
    }
    else if (!csp && csn)
    {
        ReactionWheelsTorque[1] -= RollJerk * Time.fixedDeltaTime;
    }
    else if (!(csp ^ csn) && ReactionWheelsTorque[1] != 0)
    {
        ReactionWheelsTorque[1] += (Mathf.Abs(ReactionWheelsTorque[1]) > RollJerk / 5)
            ? (ReactionWheelsTorque[1] < 0 ? 6 : -6) * RollJerk * Time.fixedDeltaTime
            : -ReactionWheelsTorque[1];
    }
    #endregion

    #region Yaw

    csp = InputControl.ControlSurfaces.YawRight.IsPressed();
    csn = InputControl.ControlSurfaces.YawLeft.IsPressed();
    if (csp && !csn)
    {
        ReactionWheelsTorque[2] += YawJerk * Time.fixedDeltaTime;
    }
    else if (!csp && csn)
    {
        ReactionWheelsTorque[2] -= YawJerk * Time.fixedDeltaTime;
    }
    else if (!(csp ^ csn) && ReactionWheelsTorque[2] != 0)
    {
        ReactionWheelsTorque[2] += (Mathf.Abs(ReactionWheelsTorque[2]) > YawJerk / 5)
            ? (ReactionWheelsTorque[2] < 0 ? 3 : -3) * YawJerk * Time.fixedDeltaTime
            : -ReactionWheelsTorque[2];
    }

    #endregion

    #endregion

    #region SAS (unused)

    /*PhysicsTorque = new((Mathf.Abs(PhysicsTorque.x) > NetLinker.MainBody.DroneBodyStats[0].ReactionWheelsTorqueJerk) ? PhysicsTorque.x + (NetLinker.MainBody.DroneBodyStats[0].ReactionWheelsTorqueJerk * (PhysicsTorque.x > 0 ? -1 : 1)) : 0,
        (Mathf.Abs(PhysicsTorque.y) > NetLinker.MainBody.DroneBodyStats[0].ReactionWheelsTorqueJerk) ? PhysicsTorque.y + (NetLinker.MainBody.DroneBodyStats[0].ReactionWheelsTorqueJerk * (PhysicsTorque.y > 0 ? -1 : 1)) : 0,
        (Mathf.Abs(PhysicsTorque.z) > NetLinker.MainBody.DroneBodyStats[0].ReactionWheelsTorqueJerk) ? PhysicsTorque.z + (NetLinker.MainBody.DroneBodyStats[0].ReactionWheelsTorqueJerk * (PhysicsTorque.z > 0 ? -1 : 1)) : 0);
    */

//PhysicsTorque /= 15;
/*
#endregion
}


#region SAS - Undesired Torque Stabilization (unused)

        /*
        ReactionWheelsPower = 1;

        if (ReactionWheelsPower != 0)
        {
            Vector3 T = -PhysicsTorque * ReactionWheelsPower;

            StabilizingWheelTorque.x = (Mathf.Abs(T.x - StabilizingWheelTorque.x) > NetLinker.MainBody.DroneBodyStats[0].ReactionWheelsTorqueJerk * Time.fixedDeltaTime)
                ? StabilizingWheelTorque.x + (Mathf.Sign(T.x - StabilizingWheelTorque.x) * NetLinker.MainBody.DroneBodyStats[0].ReactionWheelsTorqueJerk * Time.fixedDeltaTime)
                : T.x;
            StabilizingWheelTorque.y = (Mathf.Abs(T.y - StabilizingWheelTorque.y) > NetLinker.MainBody.DroneBodyStats[0].ReactionWheelsTorqueJerk * Time.fixedDeltaTime)
                ? StabilizingWheelTorque.y + (Mathf.Sign(T.y - StabilizingWheelTorque.y) * NetLinker.MainBody.DroneBodyStats[0].ReactionWheelsTorqueJerk * Time.fixedDeltaTime)
                : T.y;
            StabilizingWheelTorque.z = (Mathf.Abs(T.z - StabilizingWheelTorque.z) > NetLinker.MainBody.DroneBodyStats[0].ReactionWheelsTorqueJerk * Time.fixedDeltaTime)
                ? StabilizingWheelTorque.z + (Mathf.Sign(T.z - StabilizingWheelTorque.z) * NetLinker.MainBody.DroneBodyStats[0].ReactionWheelsTorqueJerk * Time.fixedDeltaTime)
                : T.z;
        }
        else if (StabilizingWheelTorque != Vector3.zero)
        {
            StabilizingWheelTorque.x = (Mathf.Abs(StabilizingWheelTorque.x) > NetLinker.MainBody.DroneBodyStats[0].ReactionWheelsTorqueJerk * Time.fixedDeltaTime)
                ? StabilizingWheelTorque.x + (Mathf.Sign(StabilizingWheelTorque.x) * NetLinker.MainBody.DroneBodyStats[0].ReactionWheelsTorqueJerk * Time.fixedDeltaTime)
                : 0;
            StabilizingWheelTorque.y = (Mathf.Abs(StabilizingWheelTorque.y) > NetLinker.MainBody.DroneBodyStats[0].ReactionWheelsTorqueJerk * Time.fixedDeltaTime)
                ? StabilizingWheelTorque.y + (Mathf.Sign(StabilizingWheelTorque.y) * NetLinker.MainBody.DroneBodyStats[0].ReactionWheelsTorqueJerk * Time.fixedDeltaTime)
                : 0;
            StabilizingWheelTorque.z = (Mathf.Abs(StabilizingWheelTorque.z) > NetLinker.MainBody.DroneBodyStats[0].ReactionWheelsTorqueJerk * Time.fixedDeltaTime)
                ? StabilizingWheelTorque.z + (Mathf.Sign(StabilizingWheelTorque.z) * NetLinker.MainBody.DroneBodyStats[0].ReactionWheelsTorqueJerk * Time.fixedDeltaTime)
                : 0;
        }
#endregion

*/
#endregion
