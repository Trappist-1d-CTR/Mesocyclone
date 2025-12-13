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

    public TextAsset DroneDataTxt;
    public DroneObject NetLinker;
    #endregion

    #region Flight Control Data
    public float InputMargin;

    public float[] ControlSurfaceAngle;

    public float Thrust;

    public float ImpulseCharge;
    public bool ImpulseActive;
    public float ImpulseTimer;
    public float LastImpulseBurn;
    public float ImpulseThreshold;
    public float ImpulseInputErrorTimer;

    public float HoverThrust;
    public float HoverTarget;
    public bool HoverAuto;
    public int HoverMode = 1;
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
    #endregion

    #region Atmospherical Physics
    public Vector3 Wind;
    public Vector3 AirSpeed;
    #endregion

    #region GameObject Components
    private Rigidbody DronePhysics;
    private BoxCollider DroneCollider;
    #endregion

    #region Lift Coefficients
    public float[] AspectRatio;
    public AnimationCurve[] LiftAoA;
    public AnimationCurve[] InducedDragAoA;
    public AnimationCurve[] TorqueAoA;
    #endregion

    #region Control Surfaces Coefficients
    private float RefSqrAirSpeed1;
    private float RefSqrAirSpeed7;

    public float PitchCtrl;
    public float RollCtrl;
    public float YawCtrl;
    #endregion

    #region Reference Scripts
    private AGlobalValues C;
    public AirCellBehavior Air;
    #endregion

    #region Center Of Mass Correction

    public bool CenterOfLiftFound = false;
    private Vector3 VectorA;
    public Vector3 CenterOfLift;
    public Vector3 CenterOfMass;
    private double B;

    #endregion

    #region Physics Visualizer

    public int VisualizationMode;

    private Vector3 TotThrust;
    private Vector3 TotDrag;
    private Vector3 TotLift;
    private Vector3 TotWeight;

    public bool AirChamberTest = false;

    #endregion

    #region Script Optimization Memory
    private Vector3 Memory;
    #endregion

    public InputMap InputControl;

    #endregion

    // Start is called before the first frame update
    void Start()
    {
        #region Get Components and Script Reference

        DronePhysics = gameObject.GetComponent<Rigidbody>();
        DroneCollider = gameObject.GetComponent<BoxCollider>();
        C = GameObject.FindGameObjectWithTag("GameController").GetComponent<AGlobalValues>();

        #endregion

        #region Read Drone Data
        NetLinker = new DroneObject { MainBody = JsonUtility.FromJson<MainDroneStats>(DroneDataTxt.text), Parts = JsonUtility.FromJson<DronePartsList>(DroneDataTxt.text) };

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
                    Debug.Log("PartObject with ID " + NetLinker.Parts.DronePartStats[i].IDb + " not found.");
                }
            }
        }

        DronePhysics.mass = NetLinker.MainBody.DroneBodyStats[0].Mass;
        #endregion

        #region Starting Setup

        InputControl = new();
        InputControl.Enable();

        PhysicsPosition = Vector3.up;
        CenterOfMass = DronePhysics.centerOfMass = new Vector3(NetLinker.MainBody.DroneBodyStats[0].CenterMassX, NetLinker.MainBody.DroneBodyStats[0].CenterMassY, NetLinker.MainBody.DroneBodyStats[0].CenterMassZ);

        AspectRatio = new float[NetLinker.Parts.DronePartStats.Length];
        LiftAoA = new AnimationCurve[NetLinker.Parts.DronePartStats.Length];
        InducedDragAoA = new AnimationCurve[NetLinker.Parts.DronePartStats.Length];
        TorqueAoA = new AnimationCurve[NetLinker.Parts.DronePartStats.Length];

        ControlSurfaceAngle = new float[3] { 0, 0, 0 };

        for (int i = 0; i < AspectRatio.Length; i++)
        {
            AspectRatio[i] = NetLinker.Parts.DronePartStats[i].Chord * NetLinker.Parts.DronePartStats[i].Chord / NetLinker.Parts.DronePartStats[i].Area;
        }

        for (int i = 0; i < NetLinker.Parts.DronePartStats.Length; i++)
        {
            CalculateFlightCoefficients(i);
        }

        NetLinker.Parts.DronePartStats[7].PartObject.transform.localRotation = Quaternion.Euler(90, 0, 0);
        NetLinker.Parts.DronePartStats[7].PartObjectb.transform.localRotation = Quaternion.Euler(90, 0, 0);

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

        PhysicsVelocity = DronePhysics.velocity;
        InverseDistanceWeighting.Query = PhysicsPosition = DronePhysics.position;
        if (Air != null) Air.DronePosition = PhysicsPosition;
        PhysicsAcceleration = TotWeight = ((float)C.GaleG * Vector3.down) + (NetLinker.MainBody.DroneBodyStats[0].DroneVolume * (float)C.GaleAtmD * Vector3.up / DronePhysics.mass);
        //   Debug.Log("Starting Acceleration: " + PhysicsAcceleration);
        PhysicsRotation = DronePhysics.rotation;
        PhysicsAngVelocity = DronePhysics.angularVelocity;
        PhysicsTorque = Vector3.zero;
        DronePhysics.angularDrag = 3f;

        //Wind Vector: Tailwind is a Positive X while Headwind is a Negative X ; Climbing is a Negative Y while Descending is a Positive Y
        if (!AirChamberTest && InverseDistanceWeighting.Values != null)
            Wind = (NetLinker.MainBody.DroneBodyStats[0].YesWind && Time.time > 2) ? new Vector3(InverseDistanceWeighting.Values[0], InverseDistanceWeighting.Values[1], InverseDistanceWeighting.Values[2]) : Vector3.zero;
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
        InputControl.FlightControls.MouseScroll.performed += SwitchModes;

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
                case 1:
                    HoverTarget = NetLinker.MainBody.DroneBodyStats[0].HoverMaxThrust;
                    break;

                case 2:
                    if (DronePhysics.velocity.y <= -16.77 + HoverTargetSpeed[0])
                    {
                        HoverTarget = NetLinker.MainBody.DroneBodyStats[0].HoverMaxThrust;
                    }
                    else if (DronePhysics.velocity.y < HoverTargetSpeed[0])
                    {
                        HoverTarget = NetLinker.MainBody.DroneBodyStats[0].HoverMaxThrust + (mem * (DronePhysics.velocity.y + HoverTargetSpeed[0]));
                    }
                    else
                    {
                        HoverTarget = 0;
                    }
                    break;

                case 3:
                    if (DronePhysics.velocity.y <= -16.77)
                    {
                        HoverTarget = NetLinker.MainBody.DroneBodyStats[0].HoverMaxThrust;
                    }
                    else if (DronePhysics.velocity.y < 0)
                    {
                        HoverTarget = NetLinker.MainBody.DroneBodyStats[0].HoverMaxThrust + (mem * DronePhysics.velocity.y);
                    }
                    else
                    {
                        HoverTarget = 0;
                    }
                    break;

                case 4:
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

                case 6:
                    if (DronePhysics.velocity.y <= -16.77 + (DronePhysics.position.y > 10 ? -3 : NetLinker.MainBody.DroneBodyStats[0].LandingSpeed))
                    {
                        HoverTarget = NetLinker.MainBody.DroneBodyStats[0].HoverMaxThrust;
                    }
                    else if (DronePhysics.velocity.y < (DronePhysics.position.y > 10 ? -3 : NetLinker.MainBody.DroneBodyStats[0].LandingSpeed))
                    {
                        HoverTarget = NetLinker.MainBody.DroneBodyStats[0].HoverMaxThrust + (mem * (DronePhysics.velocity.y - NetLinker.MainBody.DroneBodyStats[0].LandingSpeed));
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
                    HoverThrust += (HoverThrust < HoverTarget ? 1 : -1) * NetLinker.MainBody.DroneBodyStats[0].HoverJerk * Time.fixedDeltaTime;
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

            if (i == 0) RefSqrAirSpeed1 = AirSpeed.sqrMagnitude;
            else if (i == 7) RefSqrAirSpeed7 = AirSpeed.sqrMagnitude;

            #region Lift
            Memory = t.up;
            PhysicsAcceleration += Check = 0.5f * (float)C.GaleAtmD * LiftAoA[i].Evaluate(AoA) * NetLinker.Parts.DronePartStats[i].Area * AirSpeed.sqrMagnitude * Memory / DronePhysics.mass;
            TotLift += Check;
            //Debug.Log(AoA + " ; " + Check + " ; (" + Vector3.Dot(AirSpeed, t.up) + ", " + Vector3.Dot(AirSpeed, t.right) + ")");

            if (VisualizationMode == 2)
                Debug.DrawLine(t.position, t.position + (DronePhysics.mass * Check / 20), Color.green, 1 / Time.renderedFrameCount);
            #endregion

            #region Induced Drag
            Memory = -AirSpeed.normalized;
            PhysicsAcceleration += Check = 0.5f * (float)C.GaleAtmD * InducedDragAoA[i].Evaluate(AoA) * NetLinker.Parts.DronePartStats[i].Area * AirSpeed.sqrMagnitude * Memory / DronePhysics.mass;
            TotDrag += Check;

            if (VisualizationMode == 2)
                Debug.DrawLine(t.position, t.position + (DronePhysics.mass * Check / 20), Color.red, 1 / Time.renderedFrameCount);
            #endregion

            #region Torque
            Memory = 0.5f * (float)C.GaleAtmD * TorqueAoA[i].Evaluate(AoA) * NetLinker.Parts.DronePartStats[i].Area * AirSpeed.sqrMagnitude * ((i != 7) ? Vector3.up : Vector3.forward);
            PhysicsTorque += Check = Vector3.Cross(L - DronePhysics.centerOfMass, Memory);

            if (VisualizationMode == 3)
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

                if (VisualizationMode == 2)
                    Debug.DrawLine(CM + (DronePhysics.rotation * L), CM + (DronePhysics.rotation * L) + (DronePhysics.mass * Check / 20), Color.green, 1 / Time.renderedFrameCount);
                #endregion

                #region Induced Drag
                Memory = -AirSpeed.normalized;
                PhysicsAcceleration += Check = 0.5f * (float)C.GaleAtmD * InducedDragAoA[i].Evaluate(AoA) * NetLinker.Parts.DronePartStats[i].Area * AirSpeed.sqrMagnitude * Memory / DronePhysics.mass;
                TotDrag += Check;

                if (VisualizationMode == 2)
                    Debug.DrawLine(t.position, t.position + (DronePhysics.mass * Check / 20), Color.red, 1 / Time.renderedFrameCount);
                #endregion

                #region Torque
                Memory = 0.5f * (float)C.GaleAtmD * TorqueAoA[i].Evaluate(AoA) * NetLinker.Parts.DronePartStats[i].Area * AirSpeed.sqrMagnitude * ((i != 7) ? Vector3.up : Vector3.forward);
                PhysicsTorque += Check = Vector3.Cross(L - DronePhysics.centerOfMass, Memory);

                if (VisualizationMode == 3)
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

        #region Control Surfaces

        float pctrl = PitchCtrl * ((float)C.GaleAtmD / (float)C.GaleAtmD) * (900 / RefSqrAirSpeed1);
        float rctrl = RollCtrl * ((float)C.GaleAtmD / (float)C.GaleAtmD) * (900 / RefSqrAirSpeed1);
        float yctrl = YawCtrl * ((float)C.GaleAtmD / (float)C.GaleAtmD) * (900 / RefSqrAirSpeed7);
        //Debug.Log("Control Surfaces: " + pctrl + " ; " + rctrl + " ; " + yctrl + " .");

        #region Pitch

        bool csp = InputControl.ControlSurfaces.PitchUp.IsPressed();
        bool csn = InputControl.ControlSurfaces.PitchDown.IsPressed();
        if (csp && !csn)
        {
            ControlSurfaceAngle[0] += pctrl * Time.fixedDeltaTime;
        }
        else if (!csp && csn)
        {
            ControlSurfaceAngle[0] -= pctrl * Time.fixedDeltaTime;
        }
        else if (csp && csn)
        {
            ControlSurfaceAngle[0] += (Mathf.Abs(ControlSurfaceAngle[0]) > 1)
                ? (ControlSurfaceAngle[0] < 0 ? 1 : -1) * pctrl * Time.fixedDeltaTime
                : -ControlSurfaceAngle[0];
        }

        if (csp || csn)
        {
            NetLinker.Parts.DronePartStats[0].PartObject.transform.localRotation = Quaternion.Euler(0, 0, ControlSurfaceAngle[0]);
        }
        else if (Mathf.Abs(ControlSurfaceAngle[0]) < PitchCtrl / 5)
        {
            ControlSurfaceAngle[0] = 0;
            NetLinker.Parts.DronePartStats[0].PartObject.transform.localRotation = Quaternion.Euler(0, 0, 0);
        }

        #endregion

        #region Roll

        csp = InputControl.ControlSurfaces.RollClock.IsPressed();
        csn = InputControl.ControlSurfaces.RollCounterClock.IsPressed();
        if (csp && !csn)
        {
            ControlSurfaceAngle[1] += rctrl * Time.fixedDeltaTime;
        }
        else if (!csp && csn)
        {
            ControlSurfaceAngle[1] -= rctrl * Time.fixedDeltaTime;
        }
        else if (csp && csn)
        {
            ControlSurfaceAngle[1] += (Mathf.Abs(ControlSurfaceAngle[1]) > 1)
                ? (ControlSurfaceAngle[1] < 0 ? 1 : -1) * rctrl * Time.fixedDeltaTime
                : -ControlSurfaceAngle[1];
        }

        if (csp || csn)
        {
            NetLinker.Parts.DronePartStats[4].PartObject.transform.localRotation = Quaternion.Euler(0, 0, ControlSurfaceAngle[1] / 2);
            NetLinker.Parts.DronePartStats[4].PartObjectb.transform.localRotation = Quaternion.Euler(0, 0, -ControlSurfaceAngle[1] / 2);
            NetLinker.Parts.DronePartStats[5].PartObject.transform.localRotation = Quaternion.Euler(0, 0, ControlSurfaceAngle[1]);
            NetLinker.Parts.DronePartStats[5].PartObjectb.transform.localRotation = Quaternion.Euler(0, 0, -ControlSurfaceAngle[1]);
        }
        else if (Mathf.Abs(ControlSurfaceAngle[1]) < RollCtrl / 5)
        {
            ControlSurfaceAngle[1] = 0;
            NetLinker.Parts.DronePartStats[4].PartObject.transform.localRotation = Quaternion.Euler(0, 0, 0);
            NetLinker.Parts.DronePartStats[4].PartObjectb.transform.localRotation = Quaternion.Euler(0, 0, 0);
            NetLinker.Parts.DronePartStats[5].PartObject.transform.localRotation = Quaternion.Euler(0, 0, 0);
            NetLinker.Parts.DronePartStats[5].PartObjectb.transform.localRotation = Quaternion.Euler(0, 0, 0);
        }

        #endregion

        #region Yaw

        csp = InputControl.ControlSurfaces.YawRight.IsPressed();
        csn = InputControl.ControlSurfaces.YawLeft.IsPressed();
        if (csp && !csn)
        {
            ControlSurfaceAngle[2] += yctrl * Time.fixedDeltaTime;
        }
        else if (!csp && csn)
        {
            ControlSurfaceAngle[2] -= yctrl * Time.fixedDeltaTime;
        }
        else if (csp && csn)
        {
            ControlSurfaceAngle[2] += (Mathf.Abs(ControlSurfaceAngle[2]) > 1)
                ? (ControlSurfaceAngle[2] < 0 ? 1 : -1) * yctrl * Time.fixedDeltaTime
                : -ControlSurfaceAngle[2];
        }

        if (csp || csn)
        {
            NetLinker.Parts.DronePartStats[7].PartObject.transform.localRotation = Quaternion.Euler(90, -ControlSurfaceAngle[2], 0);
            NetLinker.Parts.DronePartStats[7].PartObjectb.transform.localRotation = Quaternion.Euler(90, -ControlSurfaceAngle[2], 0);
        }
        else if (Mathf.Abs(ControlSurfaceAngle[2]) < YawCtrl / 5)
        {
            ControlSurfaceAngle[2] = 0;
            NetLinker.Parts.DronePartStats[7].PartObject.transform.localRotation = Quaternion.Euler(90, 0, 0);
            NetLinker.Parts.DronePartStats[7].PartObjectb.transform.localRotation = Quaternion.Euler(90, 0, 0);
        }

        #endregion

        #endregion

        AirSpeed = -PhysicsVelocity + Wind;

        #region Drag Physics

        Memory = transform.right;
        MainStats s = NetLinker.MainBody.DroneBodyStats[0];

        //Forward Drag
        PhysicsAcceleration += Check = 0.5f * (float)C.GaleAtmD * (InputControl.FlightControls.AirBrakes.IsPressed() ? s.AirBrakesCd : s.FrontCd) * s.FrontArea * Mathf.Pow(Vector3.Dot(AirSpeed, Memory), 2) * (Vector3.Dot(AirSpeed, Memory) > 0 ? 1 : -1) * Memory / DronePhysics.mass;
        TotDrag += Check;
        //   Debug.Log("Wind: " + AirSpeed + "; Forward Drag: " + Check);

        Memory = transform.up;

        //Vertical Drag
        PhysicsAcceleration += Check = 0.5f * (float)C.GaleAtmD * s.BottomCd * s.BottomArea * Mathf.Pow(Vector3.Dot(AirSpeed, Memory), 2) * (Vector3.Dot(AirSpeed, Memory) > 0 ? 1 : -1) * Memory / DronePhysics.mass;
        TotDrag += Check;
        //   Debug.Log("Vertical Drag: " + Check);

        Memory = transform.forward;

        //Side Drag
        PhysicsAcceleration += Check = 0.5f * (float)C.GaleAtmD * (InputControl.FlightControls.AirBrakes.IsPressed() ? s.SideBrakesCd : s.SideCd) * (s.SideArea - (InputControl.FlightControls.AirBrakes.IsPressed() ? NetLinker.Parts.DronePartStats[7].Area : 0)) * Mathf.Pow(Vector3.Dot(AirSpeed, Memory), 2) * (Vector3.Dot(AirSpeed, Memory) > 0 ? 1 : -1) * Memory / DronePhysics.mass;
        TotDrag += Check;
        //   Debug.Log("Side Drag: " + Check);

        #endregion


        #region APPLY PHYSICS
        if (!AirChamberTest)
        {
            //PhysicsVelocity += PhysicsAcceleration * Time.fixedDeltaTime / DronePhysics.mass;
            DronePhysics.AddForce(PhysicsAcceleration, ForceMode.Acceleration);
            DronePhysics.AddRelativeTorque(PhysicsTorque);
            //Debug.Log(PhysicsTorque);

            //MAJOR ISSUE: Torque from wings behaves weird
        }
        else
        {
            DronePhysics.position = PhysicsPosition = Vector3.up;
            DronePhysics.velocity = PhysicsVelocity = Vector3.zero;
            DronePhysics.AddRelativeTorque(PhysicsTorque);
            //Debug.Log(PhysicsTorque);

            /*
            if (PhysicsTorque.magnitude < 0.01)
            {
                //Debug.Log("Stable Rotation");
            }*/
        }
        #endregion


        #region Match CoM with CoF
        
        if (!CenterOfLiftFound)
        {
            CenterOfLift = VectorA / (float)B;
            Debug.Log("Center of Lift: " + CenterOfLift);
            CenterOfLiftFound = true;
            DronePhysics.centerOfMass = CenterOfLift;
        }
        
        #endregion

        #region Physics Visualizer

        switch (VisualizationMode)
        {
            case 0:

                break;

            case 1:
                Debug.DrawLine(CM, CM + (PhysicsVelocity / 20), Color.yellow, 1 / Time.renderedFrameCount);

                Debug.DrawLine(CM, CM + (DronePhysics.mass * TotThrust / 20), Color.blue, 1 / Time.renderedFrameCount);
                Debug.DrawLine(CM, CM + (DronePhysics.mass * TotDrag / 20), Color.red, 1 / Time.renderedFrameCount);
                Debug.DrawLine(CM, CM + (DronePhysics.mass * TotLift / 20), Color.green, 1 / Time.renderedFrameCount);
                Debug.DrawLine(CM, CM + (DronePhysics.mass * TotWeight / 20), Color.grey, 1 / Time.renderedFrameCount);
                break;

            case 2:
                //Check Lift and Induced Drag code regions
                break;

            case 3:
                //Check Torque code regions

                Debug.DrawLine(CM - (DronePhysics.rotation * new Vector3(0, 0.2f)), CM + (DronePhysics.rotation * new Vector3(0, 0.2f)), Color.yellow, 1 / Time.renderedFrameCount);
                Debug.DrawLine(CM - (DronePhysics.rotation * new Vector3(0, 0, 0.2f)), CM + (DronePhysics.rotation * new Vector3(0, 0, 0.2f)), Color.yellow, 1 / Time.renderedFrameCount);
                
                Debug.DrawLine(PhysicsPosition + (DronePhysics.rotation * CenterOfLift) - (DronePhysics.rotation * new Vector3(0, 0.2f)), PhysicsPosition + (DronePhysics.rotation * CenterOfLift) + (DronePhysics.rotation * new Vector3(0, 0.2f)), Color.cyan, 1 / Time.renderedFrameCount);
                Debug.DrawLine(PhysicsPosition + (DronePhysics.rotation * CenterOfLift) - (DronePhysics.rotation * new Vector3(0, 0, 0.2f)), PhysicsPosition + (DronePhysics.rotation * CenterOfLift) + (DronePhysics.rotation * new Vector3(0, 0, 0.2f)), Color.cyan, 1 / Time.renderedFrameCount);
                break;
        }
        #endregion
    }

    #region Hover Modes Control
    private void ToggleAutoMode(InputAction.CallbackContext obj)
    {
        HoverAuto = !HoverAuto;
    }

    private void SwitchModes(InputAction.CallbackContext obj)
    {
        HoverMode += InputControl.FlightControls.MouseScroll.ReadValue<float>() > 0 ? (HoverMode == 6 ? -2 : -1) : (HoverMode == 4 ? 2 : 1);
        HoverMode = Mathf.Clamp(HoverMode, 1, 6);
    }
    #endregion

    public void CalculateFlightCoefficients(int i)
    {
        float memmoi, CN, CT, CM, AoA;
        //float mem1, mem2, mem2s, mem2c, mem3;
        LiftAoA[i] = new();
        InducedDragAoA[i] = new();
        TorqueAoA[i] = new();
        float iStep = 1;

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
            CN = ((d.StallAngle * memmoi) + (CT * Mathf.Sin(AoA))) / Mathf.Cos(AoA);

            _ = InducedDragAoA[i].AddKey(i2 * iStep, (CN * Mathf.Sin(AoA)) + (CT * Mathf.Cos(AoA)));
            _ = InducedDragAoA[i].AddKey(-i2 * iStep, (CN * Mathf.Sin(AoA)) + (CT * Mathf.Cos(AoA)));
            _ = InducedDragAoA[i].AddKey(180 - (i2 * iStep), (CN * Mathf.Sin(AoA)) + (CT * Mathf.Cos(AoA)));
            _ = InducedDragAoA[i].AddKey(-180 + (i2 * iStep), (CN * Mathf.Sin(AoA)) + (CT * Mathf.Cos(AoA)));
        }
        #endregion

        #region Torque - Low AoAs
        CM = -CN * (float)(0.25 - (0.175 * (1 - (2 * AoA / Mathf.PI))));

        _ = TorqueAoA[i].AddKey(new(0, 0));
        _ = TorqueAoA[i].AddKey(new(180, 0));
        _ = TorqueAoA[i].AddKey(new(-180, 0));
        _ = TorqueAoA[i].AddKey(d.StallAngle, CM);
        _ = TorqueAoA[i].AddKey(-d.StallAngle, -CM);

        CM = -CN * (float)(0.25 - (0.175 * (1 - (2 * (1 - (AoA / Mathf.PI))))));

        _ = TorqueAoA[i].AddKey(180 - d.StallAngle, CM);
        _ = TorqueAoA[i].AddKey(-180 + d.StallAngle, -CM);
        #endregion

        for (int i2 = 0; i2 < (180 - (2 * (d.StallAngle + 10))) / iStep; i2++)
        {
            AoA = (d.StallAngle + 10) + (i2 * iStep) - 180;
            memmoi = -Mathf.Abs(Mathf.Deg2Rad * (Mathf.Abs(AoA) - 0)); //(-d.MaxInducedAoA * Mathf.Abs(AoA) / (90 - (d.StallAngle + 10)))

            CN = d.BottomCd * Mathf.Sin(memmoi) * ((1f / (0.56f + (0.44f * Mathf.Sin(memmoi)))) - (0.41f * (1 - Mathf.Exp(-17f / AspectRatio[i]))));
            CT = (1f / 2f) * d.FrontCd * Mathf.Cos(memmoi);

            #region Lift - High AoAs
            _ = LiftAoA[i].AddKey(new(d.StallAngle + 10 + (i2 * iStep), CN * Mathf.Cos(memmoi) - (CT * Mathf.Sin(memmoi))));
            _ = LiftAoA[i].AddKey(new(d.StallAngle + 10 + (i2 * iStep) - 180, CN * Mathf.Cos(memmoi) - (CT * Mathf.Sin(memmoi))));
            #endregion

            #region Induced Drag - High AoAs
            _ = InducedDragAoA[i].AddKey(d.StallAngle + 10 + (i2 * iStep), (CN * Mathf.Sin(memmoi)) + (CT * Mathf.Cos(memmoi)));
            _ = InducedDragAoA[i].AddKey(-(d.StallAngle + 10 + (i2 * iStep)), (CN * Mathf.Sin(memmoi)) + (CT * Mathf.Cos(memmoi)));
            #endregion

            #region Torque - High AoAs
            CM = -CN * (float)(0.25 - (0.175 * (1 - (2 * AoA / Mathf.PI))));

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
        /*
        for (int i2 = 0; i2 < InducedDragAoA[i].length; i2++)
        {
            if (Mathf.Abs(InducedDragAoA[i].keys[i2].time) != 180 && Mathf.Abs(InducedDragAoA[i].keys[i2].time) != d.StallAngle && Mathf.Abs(InducedDragAoA[i].keys[i2].time) != (180 - d.StallAngle) && InducedDragAoA[i].keys[i2].time != 0)
            {
                InducedDragAoA[i].SmoothTangents(i2, 1);
            }
        }
        */
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