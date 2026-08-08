using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Mesocyclone;

public class HangarScript : Tickable
{
    #region Variables

    public Rigidbody Platform;
    public Rigidbody Cover;

    private Vector3 PlatformShelteredPos;

    public float PlatformExtensionHeight;
    public float ClosingTime;
    public float LaunchingTime;
    public float CoverTorqueForce;

    public float AnimationTimer;

    public enum HangarSituations
    {
        Standby,
        Closing,
        Sheltered,
        Launching
    }
    public HangarSituations HangarState;

    #endregion

    void Start()
    {
        PlatformShelteredPos = new(0, 0.5f, 0);
        AnimationTimer = -1;

        if (HangarState == HangarSituations.Sheltered)
            GameObject.FindGameObjectWithTag("Player").SendMessage("InHangar", gameObject);
    }

    public override void FixedTick()
    {
        #region Platform and Cover animations

        switch (HangarState)
        {
            case HangarSituations.Closing:
                Platform.MovePosition(Platform.transform.parent.position +
                    ((PlatformShelteredPos + (PlatformExtensionHeight * (1.0f - (AnimationTimer / ClosingTime)) * Vector3.up)) * Platform.transform.lossyScale.y));

                if (AnimationTimer >= ClosingTime)
                {
                    Platform.MovePosition(Platform.transform.parent.position +
                        (PlatformShelteredPos * Platform.transform.lossyScale.y));
                    AnimationTimer = -1;
                    HangarState = HangarSituations.Sheltered;
                    GameObject.FindGameObjectWithTag("Player").SendMessage("InHangar", gameObject);
                }
                break;

            case HangarSituations.Launching:
                Platform.MovePosition(Platform.transform.parent.position + 
                    ((PlatformShelteredPos + (PlatformExtensionHeight * (AnimationTimer / ClosingTime) * Vector3.up)) * Platform.transform.lossyScale.y));
                
                if (AnimationTimer >= LaunchingTime)
                {
                    Platform.MovePosition(Platform.transform.parent.position + 
                        ((PlatformShelteredPos + (PlatformExtensionHeight * Vector3.up)) * Platform.transform.lossyScale.y));
                    AnimationTimer = -1;
                    HangarState = HangarSituations.Standby;
                }
                break;

            default:
                if (AnimationTimer != -1)
                    AnimationTimer = -1;
                break;
        }

        if (HangarState is HangarSituations.Closing or HangarSituations.Sheltered)
        {
            if (Cover.rotation.eulerAngles.z <= 90)
                Cover.AddRelativeTorque(-CoverTorqueForce * Vector3.Cross(Vector3.forward, Vector3.up));
        }
        else if (Cover.rotation.eulerAngles.z >= 90)
        {
            Cover.AddRelativeTorque(CoverTorqueForce * Vector3.Cross(Vector3.forward, Vector3.up));
        }


        if (AnimationTimer != -1)
        {
            AnimationTimer += Time.fixedDeltaTime;
        }

        #endregion
    }

    public override void Tick() { return; }

    public void ShelterHangar()
    {
        if (HangarState == HangarSituations.Standby)
        {
            AnimationTimer = 0;
            HangarState = HangarSituations.Closing;
        }
    }

    public void LaunchHangar()
    {
        if (HangarState == HangarSituations.Sheltered)
        {
            AnimationTimer = 0;
            HangarState = HangarSituations.Launching;
        }
    }
}
