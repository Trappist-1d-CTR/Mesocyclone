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
        public float HoverHeight;
        public float ThrustAcceleration;
        public float FullThrustTime;
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
    public float Thrust;
    #endregion

    #region Physics Engine Values
    private Vector3 PhysicsPosition;
    private Vector3 PhysicsVelocity;
    private Vector3 PhysicsAcceleration;

    private Quaternion PhysicsRotation;
    /*private Vector3 PhysicsTorque;*/
    #endregion

    #region Atmospherical Physics
    public Vector3 Wind;
    public Vector3 AirSpeed;
    #endregion

    #region Object Components
    private Rigidbody DronePhysics;
    private BoxCollider DroneCollider;
    #endregion

    #region Drone Parts
    public AnimationCurve TransitionCurve;
    #endregion

    #region Lift Coefficients
    public float[] AspectRatio;
    public AnimationCurve[] LiftAoA;
    public AnimationCurve[] InducedDragAoA;
    #endregion

    #region Reference Scripts
    private AGlobalValues C;
    public AirCellBehavior Air;
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
                NetLinker.Parts.DronePartStats[i].PartObjectb = transform.Find("Corrective").GetChild(NetLinker.Parts.DronePartStats[i].IDb).gameObject;
            }
        }

        DronePhysics.mass = NetLinker.MainBody.DroneBodyStats[0].Mass;
        #endregion

        #region Starting Setup

        InputControl = new();
        InputControl.Enable();

        PhysicsPosition = Vector3.up;
        DronePhysics.centerOfMass = new Vector3(NetLinker.MainBody.DroneBodyStats[0].CenterMassX, NetLinker.MainBody.DroneBodyStats[0].CenterMassY, NetLinker.MainBody.DroneBodyStats[0].CenterMassZ);

        AspectRatio = new float[NetLinker.Parts.DronePartStats.Length];
        LiftAoA = new AnimationCurve[NetLinker.Parts.DronePartStats.Length];
        InducedDragAoA = new AnimationCurve[NetLinker.Parts.DronePartStats.Length];

        for (int i = 0; i < AspectRatio.Length; i++)
        {
            AspectRatio[i] = NetLinker.Parts.DronePartStats[i].Chord * NetLinker.Parts.DronePartStats[i].Chord / NetLinker.Parts.DronePartStats[i].Area;
        }

        for (int i = 0; i < NetLinker.Parts.DronePartStats.Length; i++)
        {
            CalculateFlightCoefficients(i);
        }
        #endregion
    }

    private void FixedUpdate()
    {
        #region Physics Setup

        PhysicsVelocity = DronePhysics.velocity;
        Air.DronePosition = PhysicsPosition = DronePhysics.position;
        PhysicsAcceleration = ((float)C.GaleG * Vector3.down) + (NetLinker.MainBody.DroneBodyStats[0].DroneVolume * (float)C.GaleAtmD / DronePhysics.mass * Vector3.up);
        PhysicsRotation = DronePhysics.rotation;
        DronePhysics.angularDrag = 0.2f;

        Wind = (NetLinker.MainBody.DroneBodyStats[0].YesWind && Time.time > 2) ? new Vector3((float)Air.InterpolatedValues[0], (float)Air.InterpolatedValues[1], (float)Air.InterpolatedValues[2]) : Vector3.zero;

        #endregion

        #region Main Engine Thrust

        //Main Thrust
        if (InputControl.FlightControls.Thrust.IsPressed())
        {
            Thrust += InputControl.FlightControls.Thrust.ReadValue<float>() * Time.fixedDeltaTime / NetLinker.MainBody.DroneBodyStats[0].FullThrustTime;
            Thrust = Mathf.Clamp01(Thrust);
        }

        Memory = Quaternion.Inverse(PhysicsRotation) * transform.forward;
        PhysicsAcceleration += Thrust * NetLinker.MainBody.DroneBodyStats[0].ThrustAcceleration * Memory;

        #endregion

        #region Hovering

        if (InputControl.FlightControls.Hovering.IsPressed())
        {
            if (PhysicsPosition.y > -1)
            {
                if (PhysicsPosition.y <= NetLinker.MainBody.DroneBodyStats[0].HoverHeight)
                {
                    PhysicsAcceleration += (2 - (PhysicsPosition.y / NetLinker.MainBody.DroneBodyStats[0].HoverHeight)) * (float)C.GaleG * Vector3.up;
                }
                else if (PhysicsVelocity.y < 0)
                {
                    PhysicsAcceleration += (float)C.GaleG * 2 * Vector3.up;
                }
                else
                {
                    PhysicsAcceleration += (float)C.GaleG * 1.3f * Vector3.up;
                }
            }

            DronePhysics.angularDrag = 0.9f;
        }

        #endregion

        #region Control Canards

        if (InputControl.FlightControls.Pitch.IsPressed())
        {
            NetLinker.Parts.DronePartStats[0].PartObject.transform.localRotation = Quaternion.Euler(NetLinker.Parts.DronePartStats[0].ControlAngle * InputControl.FlightControls.Pitch.ReadValue<float>(), 0, 0);

            DronePhysics.AddRelativeTorque(-0.15f * DronePhysics.mass * InputControl.FlightControls.Pitch.ReadValue<float>(), 0, 0);
        }
        else
        {
            NetLinker.Parts.DronePartStats[0].PartObject.transform.localRotation = Quaternion.Euler(0, 0, 0);
        }

        #endregion

        #region Control Ailerons

        if (InputControl.FlightControls.Roll.IsPressed())
        {
            NetLinker.Parts.DronePartStats[5].PartObject.transform.localRotation = Quaternion.Euler(-NetLinker.Parts.DronePartStats[5].ControlAngle * InputControl.FlightControls.Roll.ReadValue<float>(), 0, 0);
            NetLinker.Parts.DronePartStats[5].PartObjectb.transform.localRotation = Quaternion.Euler(NetLinker.Parts.DronePartStats[5].ControlAngle * InputControl.FlightControls.Roll.ReadValue<float>(), 0, 0);

            DronePhysics.AddRelativeTorque(0, 0, 0.03f * DronePhysics.mass * InputControl.FlightControls.Roll.ReadValue<float>());
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
            NetLinker.Parts.DronePartStats[7].PartObject.transform.localRotation = Quaternion.Euler(-NetLinker.Parts.DronePartStats[7].ControlAngle * InputControl.FlightControls.Yaw.ReadValue<float>(), 90, 0);
            NetLinker.Parts.DronePartStats[7].PartObjectb.transform.localRotation = Quaternion.Euler(-NetLinker.Parts.DronePartStats[7].ControlAngle * InputControl.FlightControls.Yaw.ReadValue<float>(), 90, 0);

            DronePhysics.AddRelativeTorque(0, -0.15f * DronePhysics.mass * InputControl.FlightControls.Yaw.ReadValue<float>(), 0);
        }
        else
        {
            NetLinker.Parts.DronePartStats[7].PartObject.transform.localRotation = Quaternion.Euler(0, 90, 0);
            NetLinker.Parts.DronePartStats[7].PartObjectb.transform.localRotation = Quaternion.Euler(0, 90, 0);
        }

        #endregion

        Vector3 Check;
        float AoA;
        for (int i = 0; i < NetLinker.Parts.DronePartStats.Length; i++)
        {
            #region Lift, Induced Drag, Torque
            AirSpeed = PhysicsVelocity - Wind + Vector3.Cross(DronePhysics.angularVelocity, new Vector3(NetLinker.Parts.DronePartStats[i].CenterMassX, NetLinker.Parts.DronePartStats[i].CenterMassY, NetLinker.Parts.DronePartStats[i].CenterMassZ) - (PhysicsPosition + DronePhysics.centerOfMass));
            Transform t = NetLinker.Parts.DronePartStats[i].PartObject.transform;

            Memory = t.up;
            AirSpeed = Vector3.ProjectOnPlane(AirSpeed, Memory);

            AoA = Mathf.Rad2Deg * Mathf.Atan2(Vector3.Dot(AirSpeed, t.up), Vector3.Dot(AirSpeed, t.forward));
            //   Debug.Log("AoA: " + AoA);

            #region Lift Force
            PhysicsAcceleration += Check = 0.5f * (float)C.GaleAtmD * LiftAoA[i].Evaluate(AoA) * NetLinker.Parts.DronePartStats[i].Area * AirSpeed.sqrMagnitude * Memory / DronePhysics.mass;
            //   Debug.Log("i = " + i + " : " + Check + " ; " + Memory);
            #endregion

            Memory = -NetLinker.Parts.DronePartStats[i].PartObject.transform.forward;

            #region Induced Drag Force
            PhysicsAcceleration += Check = 0.5f * (float)C.GaleAtmD * InducedDragAoA[i].Evaluate(AoA) * NetLinker.Parts.DronePartStats[i].Area * AirSpeed.sqrMagnitude * Memory / DronePhysics.mass;
            //    Debug.Log("i = " + i + " : " + Check + " ; " + Memory);
            #endregion

            #region Torque

            #endregion

            if (NetLinker.Parts.DronePartStats[i].IDb != 0)
            {
                AirSpeed = PhysicsVelocity - Wind + Vector3.Cross(DronePhysics.angularVelocity, new Vector3(NetLinker.Parts.DronePartStats[i].CenterMassX, NetLinker.Parts.DronePartStats[i].CenterMassY, -NetLinker.Parts.DronePartStats[i].CenterMassZ) - (PhysicsPosition + DronePhysics.centerOfMass));
                t = NetLinker.Parts.DronePartStats[i].PartObjectb.transform;

                Memory = t.up;
                AirSpeed = Vector3.ProjectOnPlane(AirSpeed, Memory);

                AoA = Mathf.Rad2Deg * Mathf.Atan2(Vector3.Dot(AirSpeed, t.up), Vector3.Dot(AirSpeed, t.forward));
                //   Debug.Log("AoA: " + AoA);

                #region Lift Force
                PhysicsAcceleration += Check = 0.5f * (float)C.GaleAtmD * LiftAoA[i].Evaluate(AoA) * NetLinker.Parts.DronePartStats[i].Area * AirSpeed.sqrMagnitude * Memory / DronePhysics.mass;
                //   Debug.Log("i = " + i + " : " + Check + " ; " + Memory);
                #endregion

                Memory = -NetLinker.Parts.DronePartStats[i].PartObject.transform.forward;

                #region Induced Drag Force
                PhysicsAcceleration += Check = 0.5f * (float)C.GaleAtmD * InducedDragAoA[i].Evaluate(AoA) * NetLinker.Parts.DronePartStats[i].Area * AirSpeed.sqrMagnitude * Memory / DronePhysics.mass;
                //    Debug.Log("i = " + i + " : " + Check + " ; " + Memory);
                #endregion

                #region Torque

                #endregion
            }
            #endregion
        }

        AirSpeed = PhysicsVelocity - Wind;

        if (Wind.magnitude > 1)
        {
            Debug.Log("Offc: " + Wind);
        }

        #region Drag Physics

        Memory = Quaternion.Inverse(PhysicsRotation) * transform.forward;
        MainStats s = NetLinker.MainBody.DroneBodyStats[0];

        //Forward Drag
        PhysicsAcceleration += Check = -0.5f * (float)C.GaleAtmD * (InputControl.FlightControls.AirBrakes.IsPressed() ? s.AirBrakesCd : s.FrontCd) * s.FrontArea * Mathf.Pow(Vector3.Dot(AirSpeed, transform.forward), 2) * (Vector3.Dot(AirSpeed, transform.forward) > 0 ? 1 : -1) * Memory / DronePhysics.mass;
        //   Debug.Log("Wind: " + Wind + "; Forward Drag: " + Check);

        Memory = Quaternion.Inverse(PhysicsRotation) * transform.up;

        //Vertical Drag
        PhysicsAcceleration += Check = -0.5f * (float)C.GaleAtmD * s.BottomCd * s.BottomArea * Mathf.Pow(Vector3.Dot(AirSpeed, transform.up), 2) * (Vector3.Dot(AirSpeed, transform.up) > 0 ? 1 : -1) * Memory / DronePhysics.mass;
           Debug.Log("Wind: " + Wind + "; Vertical Drag: " + Check);

        Memory = Quaternion.Inverse(PhysicsRotation) * transform.right;

        //Side Drag
        PhysicsAcceleration += Check = -0.5f * (float)C.GaleAtmD * (InputControl.FlightControls.AirBrakes.IsPressed() ? s.SideBrakesCd : s.SideCd) * (s.SideArea - (InputControl.FlightControls.AirBrakes.IsPressed() ? NetLinker.Parts.DronePartStats[7].Area : 0)) * Mathf.Pow(Vector3.Dot(AirSpeed, transform.right), 2) * (Vector3.Dot(AirSpeed, transform.right) > 0 ? 1 : -1) * Memory / DronePhysics.mass;
        //   Debug.Log("Wind: " + Wind + "; Side Drag: " + Check);

        #endregion


        //   Debug.Log(PhysicsVelocity + " ; " + DronePhysics.velocity);

        DronePhysics.AddRelativeForce(PhysicsAcceleration, ForceMode.Acceleration);
        //DronePhysics.AddRelativeTorque(PhysicsTorque);
    }

    public void CalculateFlightCoefficients(int i)
    {
        float memmoi, CN, CT, AoA;
        //float mem1, mem2, mem2s, mem2c, mem3;
        LiftAoA[i] = new();
        InducedDragAoA[i] = new();
        float iStep = 1;

        PartStats p = NetLinker.Parts.DronePartStats[i];
        MainStats d = NetLinker.MainBody.DroneBodyStats[0];

        #region Lift - Low AoAs
        memmoi = d.LiftCurveSlope * (AspectRatio[i] / (AspectRatio[i] + (2 * (AspectRatio[i] + 4) / (AspectRatio[i] + 2))));
        
        _ = LiftAoA[i].AddKey(new(0, 0, memmoi, memmoi));
        _ = LiftAoA[i].AddKey(new(180, 0, -memmoi, -memmoi));
        _ = LiftAoA[i].AddKey(new(-180, 0, -memmoi, -memmoi));
        _ = LiftAoA[i].AddKey(new(d.StallAngle, d.StallAngle * memmoi, memmoi, 0));
        _ = LiftAoA[i].AddKey(new(-d.StallAngle, -d.StallAngle * memmoi, 0, memmoi));
        _ = LiftAoA[i].AddKey(new(180 - d.StallAngle, -d.StallAngle * memmoi, memmoi, 0));
        _ = LiftAoA[i].AddKey(new(-180 + d.StallAngle, d.StallAngle * memmoi, 0, memmoi));
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

        for (int i2 = 0; i2 < (180 - (2 * (d.StallAngle + 10))) / iStep; i2++)
        {
            AoA = (d.StallAngle + 10) + (i2 * iStep) - 180;
            memmoi = -Mathf.Abs(Mathf.Deg2Rad * (Mathf.Abs(AoA) - (-d.MaxInducedAoA * Mathf.Abs(AoA) / (90 - (d.StallAngle + 10)))));
            
            CN = d.BottomCd * Mathf.Sin(memmoi) * ((1f / (0.56f + (0.44f * Mathf.Sin(memmoi)))) - (0.41f * (1 - Mathf.Exp(-17f / AspectRatio[i]))));
            CT = (1f / 2f) * d.FrontCd * Mathf.Cos(memmoi);

            #region Lift - High AoAs
            _ = LiftAoA[i].AddKey(new(d.StallAngle + 10 + (i2 * iStep), CN * Mathf.Cos(memmoi) - (CT * Mathf.Sin(memmoi))/*, mem3, mem3*/));
            _ = LiftAoA[i].AddKey(new(d.StallAngle + 10 + (i2 * iStep) - 180, CN * Mathf.Cos(memmoi) - (CT * Mathf.Sin(memmoi))/*, mem3, mem3*/));
            #endregion

            #region Induced Drag - High AoAs
            _ = InducedDragAoA[i].AddKey(d.StallAngle + 10 + (i2 * iStep), (CN * Mathf.Sin(memmoi)) + (CT * Mathf.Cos(memmoi)));
            _ = InducedDragAoA[i].AddKey(-(d.StallAngle + 10 + (i2 * iStep)), (CN * Mathf.Sin(memmoi)) + (CT * Mathf.Cos(memmoi)));
            #endregion
        }

        #region Smoothing Tangents

        for (int i2 = 0; i2 < LiftAoA[i].length; i2++)
        {
            LiftAoA[i].SmoothTangents(i2, 1);
        }

        for (int i2 = 0; i2 < InducedDragAoA[i].length; i2++)
        {
            if (Mathf.Abs(InducedDragAoA[i].keys[i2].time) != 180 && Mathf.Abs(InducedDragAoA[i].keys[i2].time) != d.StallAngle && Mathf.Abs(InducedDragAoA[i].keys[i2].time) != (180 - d.StallAngle) && InducedDragAoA[i].keys[i2].time != 0)
            {
                InducedDragAoA[i].SmoothTangents(i2, 1);
            }
        }

        #endregion
    }
}
