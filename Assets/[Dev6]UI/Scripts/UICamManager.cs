using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using TMPro;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.SceneManagement;

public class UICamManager : MonoBehaviour
{
    #region Variables

    private Rigidbody DroneBody;

    #region Camera & UI
    public Camera Cam;
    private Vector2 CameraRotation;
    private Vector3 localCamRot;
    private Vector3 CameraVector;
    private float CameraScale;
    public float CamCollisionRadius;

    public Canvas UICanvas;
    public ButtonEventSystem BackgrES;

    public float MinCamScale;
    public float MaxCamScale;
    #endregion

    #region Camera Effects
    public Volume PostProcessing;
    public AnimationCurve FOVFromSpeed;
    #endregion

    #region Pause Menus
    public GameObject PausePanel;
    public GameObject PauseMenu;
    public GameObject SettingsMenu;
    public GameObject FeedbackMenu;

    public GameObject FeedbackSent;
    public Button[] FeedbackAssessmentButtons;
    public GameObject[] FeedbackAssessmentTexts;
    public TextMeshProUGUI OtherInfo;
    #endregion

    #region Notifications
    public float MET;
    public float NotifAnimTimer;
    public TextMeshProUGUI NotifTime;
    public TextMeshProUGUI NotifThumbnail;
    public Image NotifOutline;
    public Color NotifNewMsgColor;
    #endregion

    #region Audio
    [SerializeField] AudioListener audioListener;
    #endregion

    #endregion

    public Process soundTest;

    void Start()
    {
        #region Get Components and Vectors

        DroneBody = transform.GetComponentInParent<Rigidbody>();

        Cam = gameObject.GetComponent<Camera>();
        localCamRot = Vector3.zero;
        CameraRotation = Vector3.zero;
        CameraVector = transform.localPosition;

        #endregion

        #region Disable Pause Menus
        ToggleMenu(-1);
        #endregion

        MET = 0;
        CameraScale = 1;
        NotifAnimTimer = -1;
        Application.targetFrameRate = -1;

        #region Initialize AudioListener

        if (gameObject.GetComponent<AudioListener>() == null)
            audioListener = gameObject.AddComponent<AudioListener>();
        else audioListener = gameObject.GetComponent<AudioListener>();

        #endregion
    }

    private void FixedUpdate()
    {
        #region Camera Controls
        Vector3 ScaledCamVector = CameraVector * CameraScale;

        if (BackgrES.PointerOverElement)
        {
            if (ButtonEventSystem.PointerDown(1))
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
            else if (ButtonEventSystem.PointerDown(0, 1))
            {
                // Camera scaling
                CameraScale += -0.0008f * ButtonEventSystem.PointerDeltaPos.y;
                CameraScale = Mathf.Clamp(CameraScale, MinCamScale, MaxCamScale);
            }
            else if (ButtonEventSystem.PointerDown(2))
            {
                // Camera local rotation
                localCamRot = new(localCamRot.x + (-0.1f * ButtonEventSystem.PointerDeltaPos.y), localCamRot.y + ((Mathf.Abs(localCamRot.x) >= 90 ? -0.1f : 0.1f) * ButtonEventSystem.PointerDeltaPos.x));

                if (Mathf.Abs(localCamRot.x) > 180)
                {
                    localCamRot.x = Mathf.Sign(localCamRot.x) * (Mathf.Abs(localCamRot.x) - 360);
                }
                if (Mathf.Abs(localCamRot.y) >= 360)
                {
                    localCamRot.y -= Mathf.Sign(localCamRot.y) * 360;
                }
            }
            else if (ButtonEventSystem.PointerDown(1, 2))
            {
                localCamRot = Vector3.zero;
            }
        }

        transform.localRotation = Quaternion.Euler(6 + CameraRotation.x + localCamRot.x, 90 + CameraRotation.y + localCamRot.y, 0);
        transform.localPosition = CamDistance(Quaternion.AngleAxis(CameraRotation.y, Vector3.up) * (Quaternion.AngleAxis(CameraRotation.x, Vector3.back) * ScaledCamVector));
        #endregion

        MET += Time.fixedDeltaTime;
    }

    private void Update()
    {
        #region FOV from Speed

        if (FOVFromSpeed.keys.Length != 0)
        {
            Cam.fieldOfView = FOVFromSpeed.Evaluate(DroneBody.linearVelocity.magnitude);
        }

        #endregion

        #region Handle Notifications
        int hour = Mathf.FloorToInt(MET / 3600);
        int minute = Mathf.FloorToInt((MET % 3600) / 60);
        int second = Mathf.FloorToInt(MET % 60);
        NotifTime.text = ((hour > 9 ? "" : "0") + hour) + ":" + ((minute > 9 ? "" : "0") + minute) + ":" + ((second > 9 ? "" : "0") + second);

        if (NotifAnimTimer == -1)
        {
            if (NotifierSystem.PiorityMessageList.Count != 0)
            {
                NotifAnimTimer = 0;
            }
        }
        else
        {
            if (NotifAnimTimer == 0)
            {
                NotifThumbnail.text = /*"[" + NotifierSystem.PiorityMessageList[0].MET + "] : " + */NotifierSystem.PiorityMessageList[0].msg;
                NotifOutline.color = NotifNewMsgColor;
            }
            else if (NotifAnimTimer < NotifierSystem.PiorityMessageList[0].duration)
            {

            }
            else
            {
                if (NotifOutline.color.a != 0) NotifOutline.color = new(0, 0, 0, 0);

                NotifierSystem.PiorityMessageList.RemoveAt(0);
                NotifAnimTimer = -1;
            }

            //Debug.Log(NotifierSystem.PiorityMessageList.Count + " ; " + NotifAnimTimer);
            
            if (NotifAnimTimer != -1)
                NotifAnimTimer += Time.deltaTime;
        }
        #endregion

        #region Select Chosen Assessment Button
        for (int i = 0; i < 4; i++)
        {
            FeedbackAssessmentButtons[i].interactable = (4 - i) != (int)FeedbackSystem.FeedbackVariable.Assessment;
        }
        #endregion
    }

    private Vector3 CamDistance(Vector3 Vector)
    {
        RaycastHit HitInfo;
        if (Physics.SphereCast(transform.parent.position, CamCollisionRadius, transform.parent.rotation * Vector, out HitInfo, Vector.magnitude, -1 ^ LayerMask.GetMask("NetLinker")))
        {
            return Vector.normalized * HitInfo.distance;
        }
        else
        {
            return Vector;
        }
    }

    #region Pause Menus Controls

    public void SoundTest()
    {
        AudioClip audioclip = Resources.Load<AudioClip>("SFX/Click");
        soundTest = AudioManager.Instance.PlayRepeating(audioclip, MinPitch: 1f, MaxPitch: 1f, MinDistance: 100f, MaxDistance: 1000f, Volume: Mathf.Infinity, Is2D: false, Position: 30 * Vector3.up, RolloffMode: AudioRolloffMode.Logarithmic);
    }

    public void SoundClick()
    {
        AudioClip audioclip = Resources.Load<AudioClip>("SFX/Click");
        AudioManager.Instance.Play(audioclip, MinPitch: 1f, MaxPitch: 1.01f, Volume: 0.7f);
    }

    public void EscapeUI()
    {
        SoundClick();
        OtherInfo.text = Application.version + " ; " + DroneBody.position + " ; " + System.DateTime.Today.ToShortDateString();

        if (PauseMenu.activeInHierarchy || FeedbackMenu.activeInHierarchy || SettingsMenu.activeInHierarchy)
        {
            ToggleMenu(-1);
            Time.timeScale = 1;
        }
        else
        {
            ToggleMenu(0);
            Time.timeScale = 0;
        }
    }

    public void ToggleMenu(int idx)
    {
        PausePanel.SetActive(idx is 0 or 4);
        PauseMenu.SetActive(idx is 0 or 4);
        SettingsMenu.SetActive(idx == 1);
        FeedbackMenu.SetActive(idx == 2);
        FeedbackSent.SetActive(idx == 4);
    }

    public void QuitToMenu()
    {
        SceneManager.LoadScene(0, LoadSceneMode.Single);
    }
    #endregion

    #region Feedback
    public void FeedbackUpdate(int value) => FeedbackSystem.SetFeedback(value);
    public void FeedbackUpdate(string value) => FeedbackSystem.SetFeedback(value);
    public void FeedbackUpdate(bool value)
    {
        FeedbackSystem.SetFeedback(value);

        for (int i = 0; i < 8; i++)
        {
            FeedbackAssessmentTexts[i].SetActive(i % 2 == (value ? 1 : 0));
        }
    }
    public void ButtonSendFeedback() => _ = StartCoroutine(SendFeedback());
    private IEnumerator SendFeedback()
    {
        FeedbackSystem.SetFeedback(DroneBody.position);

        UICanvas.enabled = false;
        yield return new WaitForEndOfFrame();

        string path = Application.streamingAssetsPath + "/Screenshots/FeedbackThumbnail.png";
        System.IO.File.Delete(path);
        ScreenCapture.CaptureScreenshot(path);
        yield return new WaitForEndOfFrame();

        UICanvas.enabled = true;

        FeedbackSystem.SendMail(path);
        ToggleMenu(4);
    }
    #endregion
}
