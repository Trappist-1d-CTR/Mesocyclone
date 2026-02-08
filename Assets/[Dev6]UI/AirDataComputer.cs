using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using TMPro;

public class AirDataComputer : MonoBehaviour
{
    #region Variables

    private Rigidbody DroneBody;
    private DroneControls DroneScript;

    #region Camera & UI
    public Vector2 CameraRotation;
    public Vector3 CameraVector;
    public ButtonEventSystem BackgrES;
    #endregion

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

    public float mt = 0; //Main Thrust
    public int hm = 1; //Hover Mode
    #endregion

    #region Displays
    public TextMeshProUGUI[] SAO;
    public Slider MainThrust;
    public Slider HoverMode;
    public TextMeshProUGUI[] Performance;
    #endregion

    #endregion

    // Start is called before the first frame update
    void Start()
    {
        DroneBody = transform.GetComponentInParent<Rigidbody>();
        DroneScript = transform.GetComponentInParent<DroneControls>();

        CameraRotation = Vector3.zero;
        CameraVector = transform.localPosition;
    }

    private void FixedUpdate()
    {
        #region Camera Controls
        if (BackgrES.PointerOverElement)
        {
            if (ButtonEventSystem.PointerClickButtons == 1)
            {
                                     // Up/down rotation                                                 // Rotation around normal
                CameraRotation = new(CameraRotation.x + (-0.1f * ButtonEventSystem.PointerDeltaPos.y), CameraRotation.y + ((Mathf.Abs(CameraRotation.x) >= 90 ? -0.1f : 0.1f) * ButtonEventSystem.PointerDeltaPos.x));

                if (Mathf.Abs(CameraRotation.x) > 180)
                {
                    CameraRotation.x = Mathf.Sign(CameraRotation.x) * (Mathf.Abs(CameraRotation.x) - 360);
                }
                if (Mathf.Abs(CameraRotation.y) >= 360)
                {
                    CameraRotation.y -= Mathf.Sign(CameraRotation.y) * 360;
                }
            }
        }

        transform.localRotation = Quaternion.Euler(6 + CameraRotation.x, 90 + CameraRotation.y, 0);
        transform.localPosition = Quaternion.AngleAxis(CameraRotation.y, Vector3.up) * (Quaternion.AngleAxis(CameraRotation.x, Vector3.back) * CameraVector);
        #endregion

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

        p = DroneBody.rotation.eulerAngles.z;
        if (p > 180) p -= 360;
        r = DroneBody.rotation.eulerAngles.x;
        if (r > 180) r -= 360;
        y = DroneBody.rotation.eulerAngles.y;

        mt = DroneScript.Thrust;

        hm = DroneScript.HoverMode;
        if (hm == 6) hm = 5;
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
        HoverMode.value = 1 - ((hm - 1) / 4f);
        #endregion


        #region Get and Display Performance
        Performance[0].text = "FPS: " + (1f / Time.unscaledDeltaTime).ToString("n0");
        Performance[1].text = "PhFPS: " + (1f / Time.fixedUnscaledDeltaTime).ToString("n0");
        #endregion
    }
}
