using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Mesocyclone;

namespace Mesocyclone.UI
{
    public class AirDataComputer : Tickable
    {
        #region Variables

        private Rigidbody DroneBody;
        private DroneControls DroneScript;

        #region Data
        public float a = 0; //Altitude
        public float h = 0; //Height
        public float prev_a = Mathf.Infinity;
        public float delta_a;

        public float v = 0; //Speed / Velocity
        public float prev_v = Mathf.Infinity;
        public float delta_v;

        public Quaternion Attitude;
        public float p = 0; //Pitch
        public float r = 0; //Roll
        public float y = 0; //Yaw

        public Vector3 deltaPRY;

        public float mt = 0; //Main Thrust
        public bool htog = false; //Hover Toggled
        public float htg = 0; //Hover Target
        public float ht = 0; //Hover Thrust
        public int hm = 1; //Hover Mode
        public int sas = 1; //SAS Mode

        public enum Status
        {
            Performing,
            Ready,
            Unavailable
        }
        public Status flipstate;
        #endregion

        #region Displays

        public RectTransform ASAE;

        public Transform AttitudeIndicator;
        public TextMeshProUGUI Airspeed;
        public RectTransform Acceleration;
        public TextMeshProUGUI AccValue;
        public TextMeshProUGUI Altitude;
        public TextMeshProUGUI RadarHeight;
        public RectTransform VerticalSpeed;
        public TextMeshProUGUI VertSpeedValue;
        public Slider MainThrust;
        public Slider HoverTarget;
        public Slider HoverThrust;
        public TextMeshProUGUI SASMode;
        public Image[] HoverMode;
        public Image HoverToggled;
        public Image FLIPStatus;
        public TextMeshProUGUI[] Performance;

        #endregion

        #region Customization

        public Color BaseColor;

        #endregion

        #region Performance
        private int FPS, PhFPS;
        private float t = 0;
        #endregion

        #endregion

        private void Start()
        {
            #region Get Components and Vectors

            DroneBody = transform.GetComponentInParent<Rigidbody>();
            DroneScript = transform.GetComponentInParent<DroneControls>();

            #endregion

            SetUIBaseColor();
        }

        public override void FixedTick()
        {
            #region Calculate Data

            _ = Physics.Raycast(origin: DroneBody.position, direction: Vector3.down, hitInfo: out RaycastHit hit, layerMask: LayerMask.GetMask("Default", "Terrain"), maxDistance: 5e5f);
            h = hit.distance;

            if (prev_a == Mathf.Infinity)
            {
                prev_a = a = DroneBody.position.y;
            }
            else
            {
                a = DroneBody.position.y;
                delta_a = (a - prev_a) / Time.fixedDeltaTime;
                prev_a = a;
            }

            if (prev_v == Mathf.Infinity)
            {
                prev_v = v = DroneBody.linearVelocity.magnitude;
            }
            else
            {
                v = DroneBody.linearVelocity.magnitude;
                delta_v = (v - prev_v) / Time.fixedDeltaTime;
                prev_v = v;
            }

            deltaPRY = DroneBody.angularVelocity * Mathf.Rad2Deg;

            Attitude = DroneBody.rotation;

            p = (DroneBody.rotation.eulerAngles.z > 180 ? -1 : 1) * Mathf.Rad2Deg * Mathf.Asin(Vector3.Project(DroneBody.transform.right, Vector3.up).magnitude);
            r = (DroneBody.rotation.eulerAngles.x > 180 ? -1 : 1) * Vector3.Angle(Vector3.ProjectOnPlane(DroneBody.transform.up, DroneBody.transform.right), Vector3.ProjectOnPlane(Vector3.up, DroneBody.transform.right));
            y = Vector3.SignedAngle(Vector3.right, Vector3.ProjectOnPlane(DroneBody.transform.right, Vector3.up), Vector3.up);

            #endregion
        }

        public override void Tick()
        {
            #region Calculate Data

            float hmt = DroneScript.NetLinker.MainBody.DroneBodyStats[0].HoverMaxThrust;

            mt = DroneScript.Thrust;
            htog = DroneScript.HoverActive;
            htg = htog ? DroneScript.HoverTarget / hmt : 0;
            ht = DroneScript.HoverThrust / hmt;

            hm = (int)DroneScript.HoverMode;

            sas = (int)DroneScript.SASMode;

            flipstate = DroneScript.FLIPPerforming ? Status.Performing :
                DroneScript.FLIPCharge == DroneScript.NetLinker.MainBody.DroneBodyStats[0].FLIPMaxCharge ? Status.Ready : Status.Unavailable;

            #endregion

            #region Display Data

            AttitudeIndicator.rotation = Quaternion.Euler(0, 0, 0);
            AttitudeIndicator.Rotate(Vector3.forward, p);
            AttitudeIndicator.Rotate(Vector3.right, -r);
            AttitudeIndicator.Rotate(Vector3.up, y);
            Debug.Log(p + " ; " + r + " ; " + y + "\n" + AttitudeIndicator.rotation.eulerAngles);

            Airspeed.text = v < 10 ? v.ToString("0.0") : v.ToString("###0");
            Altitude.text = a < 1000 ? (10 * Mathf.Round(a / 10f)).ToString("##0") : a < 10000
                ? (a / 1000f).ToString("0.00") + "k"
                : (a / 1000f).ToString("00.0") + "k";
            RadarHeight.text = h.ToString("f0");

            AccValue.text = Mathf.Abs(delta_v) < 1 ? delta_v.ToString("0.0") : delta_v.ToString("##0");
            VertSpeedValue.text = Mathf.Abs(delta_a) < 1 ? delta_a.ToString("0.0") : delta_a.ToString("##0");

            float pos_acc = Mathf.Sign(delta_v) * ((Mathf.Abs(delta_v) / 20f) / ((Mathf.Abs(delta_v) / 20f) + 1));
            float pos_vs = Mathf.Sign(delta_a) * ((Mathf.Abs(delta_a) / 20f) / ((Mathf.Abs(delta_a) / 20f) + 1));

            Acceleration.anchoredPosition = pos_acc < 0 ? new Vector2(1, 121 - (120 * -pos_acc))
                : new Vector2(1 + (71.373f * pos_acc), 121 + (71.408f * pos_acc));

            VerticalSpeed.anchoredPosition = pos_vs < 0 ? new Vector2(-1, 121 - (120 * -pos_vs))
                : new Vector2(-1 - (71.373f * pos_vs), 121 + (71.408f * pos_vs));

            MainThrust.value = mt;
            HoverTarget.value = htg;
            HoverThrust.value = ht;

            SASMode.text = sas switch { 1 => "Heli", 2 => "Plane", _ => "None" };
            HoverToggled.color = BaseColor * (htog ? 1 : 0.15f);
            for (int i = 1; i < HoverMode.Length + 1; i++)
            {
                HoverMode[i - 1].color = BaseColor * (hm == i ? 1 : 0.15f);
            }
            FLIPStatus.color = flipstate switch { Status.Performing => Color.red, Status.Ready => Color.green, _ => Color.gray5 };

            #endregion

            #region Performance Stats

            t += Time.unscaledDeltaTime;
            if (t - Mathf.Floor(t) >= 0.1f)
            {
                t += 0.9f;

                FPS += Mathf.RoundToInt(1f / Time.unscaledDeltaTime);
                PhFPS += Mathf.RoundToInt(1f / Time.fixedUnscaledDeltaTime);
            }
            if (t >= 10)
            {
                t -= 10;
                FPS /= 10;
                PhFPS /= 10;

                Performance[0].text = "FPS: " + FPS.ToString("n0");
                Performance[1].text = "PhFPS: " + PhFPS.ToString("n0");
                Performance[2].text = "PerfB: " + DynamicGameStateHandler.performanceBudget.ToString("G3");
            }

            #endregion
        }

        public void SetUIBaseColor()
        {
            foreach (var image in ASAE.GetComponentsInChildren<Image>(true))
            {
                image.color = BaseColor;
            }
            AttitudeIndicator.GetComponent<MeshRenderer>().material.color = BaseColor;
        }
    }
}
