using UnityEngine;
using Mesocyclone;
using Mesocyclone.MesoMod;

public class HangarScript : Tickable
{
    #region Variables

    public Rigidbody Platform;
    public Rigidbody Cover;

    private HingeJoint CoverHinge;

    private Vector3 PlatformShelteredPos;

    public float PlatformExtensionHeight;
    public float ClosingTime;
    public float LaunchingTime;
    public float CoverTorqueForce;

    public float AnimationTimer;

    private bool WaitForCoverHit;

    private AudioSource CentrifugeSFX;

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

        CentrifugeSFX = GetComponentInChildren<AudioSource>();
        CoverHinge = transform.GetComponentInChildren<HingeJoint>();
    }

    public override void FixedTick()
    {
        #region Platform-Cover Animations and Centrifuge SFX

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
            if (CoverHinge.angle <= -90)
            {
                Cover.AddRelativeTorque(-CoverTorqueForce * Vector3.Cross(Vector3.forward, Vector3.up));
                if (!CentrifugeSFX.isPlaying)
                    CentrifugeSFX.Play();
                else if (CentrifugeSFX.time > 18)
                    CentrifugeSFX.time = 1;

                if (!WaitForCoverHit) WaitForCoverHit = true;
            }
            else if (CentrifugeSFX.isPlaying && CentrifugeSFX.time > 3 && CentrifugeSFX.time < 18)
            {
                CentrifugeSFX.time = 18;
            }
        }
        else if (float.IsNaN(CoverHinge.angle) || CoverHinge.angle >= -90)
        {
            Cover.AddRelativeTorque(CoverTorqueForce * Vector3.Cross(Vector3.forward, Vector3.up));
            if (!CentrifugeSFX.isPlaying)
                CentrifugeSFX.Play();
            else if (CentrifugeSFX.time > 18)
                CentrifugeSFX.time = 1;

            if (!WaitForCoverHit) WaitForCoverHit = true;
        }
        else
        {
            if (CentrifugeSFX.isPlaying && CentrifugeSFX.time > 3 && CentrifugeSFX.time < 18)
            {
                CentrifugeSFX.time = 18;
            }
        }


        if (AnimationTimer != -1)
        {
            AnimationTimer += Time.fixedDeltaTime;
        }

        #endregion
    }

    public override void Tick()
    {
        #region Check For Cover Hit
        if (WaitForCoverHit && Cover.angularVelocity.magnitude < 0.01f && (CoverHinge.angle > -1f || CoverHinge.angle < -176f || float.IsNaN(CoverHinge.angle)))
        {
            WaitForCoverHit = false;
            //Debug.Log("Cover Hit!");
            CoverHit();
        }
        #endregion
    }

    #region Cover Collision Sound
    private void CoverHit()
    {
        JukeBox.Collision_HangarCover(Cover.worldCenterOfMass);
    }
    #endregion

    public void PauseSFX(bool ToPause)
    {
        if (ToPause)
        {
            CentrifugeSFX.Pause();
        }
        else
        {
            CentrifugeSFX.UnPause();
        }
    }

    #region Hangar Commands
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
    #endregion
}
