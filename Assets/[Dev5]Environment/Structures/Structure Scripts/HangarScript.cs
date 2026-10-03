using FMOD.Studio;
using FMODUnity;
using Mesocyclone;
using Mesocyclone.MesoMod;
using UnityEngine;

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

    private EventInstance CentrifugeSound;

    public enum HangarSituations
    {
        Standby,
        Closing,
        Sheltered,
        Launching
    }
    public HangarSituations HangarState;

    #endregion

    private void Start()
    {
        PlatformShelteredPos = new(0, 0.5f, 0);
        AnimationTimer = -1;

        if (HangarState == HangarSituations.Sheltered)
            GameObject.FindGameObjectWithTag("Player").SendMessage("InHangar", gameObject);

        CoverHinge = transform.GetComponentInChildren<HingeJoint>();

        CentrifugeSound = RuntimeManager.CreateInstance("event:/PhysicalObjects/HangarCentrifuge");
        _ = CentrifugeSound.set3DAttributes(RuntimeUtils.To3DAttributes(gameObject));
    }

    public override void FixedTick()
    {
        #region Platform-Cover Animations

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

            case HangarSituations.Standby:
                break;
            case HangarSituations.Sheltered:
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
            }
        }
        else if (float.IsNaN(CoverHinge.angle) || CoverHinge.angle >= -90)
        {
            Cover.AddRelativeTorque(CoverTorqueForce * Vector3.Cross(Vector3.forward, Vector3.up));
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

        #region Centrifuge Sound

        _ = CentrifugeSound.set3DAttributes(RuntimeUtils.To3DAttributes(gameObject));

        _ = CentrifugeSound.getPlaybackState(out PLAYBACK_STATE state);
        _ = CentrifugeSound.getTimelinePosition(out int pos);

        if (HangarState is HangarSituations.Closing or HangarSituations.Sheltered)
        {
            if (CoverHinge.angle <= -90)
            {
                if (state is not PLAYBACK_STATE.STARTING and not PLAYBACK_STATE.PLAYING and not PLAYBACK_STATE.SUSTAINING)
                { _ = CentrifugeSound.start(); }

                if (!WaitForCoverHit) WaitForCoverHit = true;
            }
            else if ((state is PLAYBACK_STATE.STARTING or PLAYBACK_STATE.PLAYING or PLAYBACK_STATE.SUSTAINING) && pos > 3 * 48000 && pos < 18 * 48000)
            {
                _ = CentrifugeSound.setTimelinePosition(Mathf.RoundToInt(18.01f * 48000));
            }
        }
        else if (float.IsNaN(CoverHinge.angle) || CoverHinge.angle >= -90)
        {
            if (state is not PLAYBACK_STATE.STARTING and not PLAYBACK_STATE.PLAYING and not PLAYBACK_STATE.SUSTAINING)
            { _ = CentrifugeSound.start(); }

            if (!WaitForCoverHit) WaitForCoverHit = true;
        }
        else if ((state is PLAYBACK_STATE.STARTING or PLAYBACK_STATE.PLAYING or PLAYBACK_STATE.SUSTAINING) && pos > 3 * 48000 && pos < 18 * 48000)
        {
            _ = CentrifugeSound.setTimelinePosition(Mathf.RoundToInt(18.01f * 48000));
        }

        #endregion
    }

    #region Cover Collision Sound

    private void CoverHit()
    {
        FMODManager.Collision.PlayHangarCover(Cover.worldCenterOfMass);
    }

    #endregion

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
