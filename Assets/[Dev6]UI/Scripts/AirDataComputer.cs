using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class AirDataComputer : MonoBehaviour
{
    #region Variables

    private Rigidbody DroneBody;
    private DroneControls DroneScript;

    #region Data
    public float h = 0; //Height
    public float prev_h = Mathf.Infinity;
    public float delta_h;

    public float v = 0; //Speed / Velocity
    public float prev_v = Mathf.Infinity;
    public float delta_v;

    public float p = 0; //Pitch
    public float r = 0; //Roll
    public float y = 0; //Yaw

    public Vector3 deltaPRY;

    public float mt = 0; //Main Thrust
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
    public TextMeshProUGUI[] SAO;
    public Slider MainThrust;
    public Slider HoverMode;
    public TextMeshProUGUI SASMode;
    public Image FLIPStatus;
    public TextMeshProUGUI[] Performance;
    #endregion

    #region Performance
    int FPS, PhFPS;
    float t = 0;
    #endregion

    #endregion

    void Start()
    {
        #region Get Components and Vectors

        DroneBody = transform.GetComponentInParent<Rigidbody>();
        DroneScript = transform.GetComponentInParent<DroneControls>();

        #endregion
    }

    private void FixedUpdate()
    {
        #region Calculate Data
        if (prev_h == Mathf.Infinity)
        {
            prev_h = h = DroneBody.position.y;
        }
        else
        {
            h = DroneBody.position.y;
            delta_h = (h - prev_h) / Time.fixedDeltaTime;
            prev_h = h;
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

        p = DroneBody.rotation.eulerAngles.z;
        if (p > 180) p -= 360;
        r = (DroneBody.rotation.eulerAngles.x > 180 ? -1 : 1) * Vector3.Angle(Vector3.ProjectOnPlane(DroneBody.transform.up, DroneBody.transform.right), Vector3.ProjectOnPlane(Vector3.up, DroneBody.transform.right));
        y = DroneBody.rotation.eulerAngles.y;

        mt = DroneScript.Thrust;

        hm = (int)DroneScript.HoverMode;

        sas = (int)DroneScript.SASMode;

        flipstate = DroneScript.FLIPPerforming ? Status.Performing :
            DroneScript.FLIPCharge == DroneScript.NetLinker.MainBody.DroneBodyStats[0].FLIPMaxCharge ? Status.Ready : Status.Unavailable;
        #endregion

        #region Display Data
        SAO[0].text = h.ToString("n2");
        SAO[1].text = delta_h.ToString("n2");
        SAO[2].text = v.ToString("n2");
        SAO[3].text = delta_v.ToString("n2");
        SAO[4].text = p.ToString("n1");
        SAO[5].text = r.ToString("n1");
        SAO[6].text = y.ToString("n1");
        MainThrust.value = mt;
        SASMode.text = sas switch { 1 => "Heli", 2 => "Plane", _ => "None" };
        HoverMode.value = 1 - ((hm - 1) / 4f);
        FLIPStatus.color = flipstate switch { Status.Performing => Color.red, Status.Ready => Color.green, _ => Color.gray5 };
        #endregion
    }

    private void Update()
    {
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
        }

        #endregion
    }
}
