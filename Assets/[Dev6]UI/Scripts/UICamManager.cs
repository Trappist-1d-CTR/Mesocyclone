using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.InputSystem;
using UnityEngine.EventSystems;
using TMPro;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;
using Unity.Cinemachine;
using Mesocyclone.Debug; // System.Diagnostics.Process exists...
using Mesocyclone.UI.Feedbacking;
using Mesocyclone.FMOD;

namespace Mesocyclone.UI
{
    public class UICamManager : Tickable
    {
        #region Variables

        public Rigidbody DroneBody;

        #region Camera & UI
        private InputMap InputControl;

        private CinemachineBrain CamBrain;
        public Transform CamTarget;
        public CinemachineCamera ThirdCamera;
        private CinemachinePanTilt ThirdCamPanTilt;
        public CinemachineCamera FirstCamera;

        private Vector2 CameraRotation;
        private Vector3 localCamRot;
        public float CamDistance;
        public float MinCamDistance;
        public float MaxCamDistance;

        public Canvas UICanvas;
        public ButtonEventSystem BackgrES;
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
        public int NotifSelectedMessage;
        public TextMeshProUGUI NotifMessageIndex;
        public Image NotifOutline;
        public Color NotifNewMsgColor;
        #endregion

        #region Audio
        [SerializeField] AudioListener audioListener;
        #endregion

        public EventSystem UIEventSystem;

        #endregion

        public Process soundTest;

        void Start()
        {
            #region Get Components and Values

            DroneBody = transform.GetComponentInParent<Rigidbody>();
            CamBrain = GetComponent<CinemachineBrain>();
            ThirdCamPanTilt = ThirdCamera.GetComponent<CinemachinePanTilt>();

            CamDistance = ThirdCamera.GetComponent<CinemachineThirdPersonFollow>().CameraDistance;
            localCamRot = Vector3.zero;
            CameraRotation = Vector3.zero;

            #endregion

            #region Check and Set Time Scale
            if (Time.timeScale != 1) Time.timeScale = 1;
            #endregion

            #region Disable Pause Menus
            ToggleMenu(-1);
            #endregion

            #region Camera Controls

            InputControl = new();
            InputControl.Enable();

            #endregion

            #region Misc Setups
            MET = 0;
            NotifSelectedMessage = 1;
            NotifAnimTimer = -1;
            SimulationSettings.Load();
            #endregion
        }

        private void OnDestroy()
        {
            #region Disable Camera Controls

            InputControl.UIControls.CamResetPivot.performed += ResetPivot;

            InputControl.Disable();

            #endregion
        }

        public override void Tick()
        {
            #region Camera Controls
            Vector2 ConsoleCamInput = InputControl.UIControls.MoveCam.ReadValue<Vector2>();

            if (ConsoleCamInput != Vector2.zero)
            {
                if (InputControl.UIControls.CamZoomMod.IsPressed())
                {
                    // Camera scaling
                    CamDistance += -0.16f * SimulationSettings.CameraSensitivity * ConsoleCamInput.y;
                    CamDistance = Mathf.Clamp(CamDistance, MinCamDistance, MaxCamDistance);

                    ThirdCamera.GetComponent<CinemachineThirdPersonFollow>().CameraDistance = CamDistance;
                }
                else if (InputControl.UIControls.CamPivotMod.IsPressed())
                {
                    // Camera local rotation
                    localCamRot = new(localCamRot.x + (10 * -SimulationSettings.CameraSensitivity * ConsoleCamInput.y), localCamRot.y + (10 * (Mathf.Abs(localCamRot.x) >= 90 ? -1f : 1f) * SimulationSettings.CameraSensitivity * ConsoleCamInput.x));

                    if (Mathf.Abs(localCamRot.x) > 180)
                    {
                        localCamRot.x = Mathf.Sign(localCamRot.x) * (Mathf.Abs(localCamRot.x) - 360);
                    }
                    if (Mathf.Abs(localCamRot.y) >= 360)
                    {
                        localCamRot.y -= Mathf.Sign(localCamRot.y) * 360;
                    }
                }
                else
                {
                    // Up/down rotation                                                                                     // Rotation around normal
                    CameraRotation = new(CameraRotation.x + (10 * -SimulationSettings.CameraSensitivity * ConsoleCamInput.y), CameraRotation.y + (10 * (Mathf.Abs(CameraRotation.x) >= 90 ? -1f : 1f) * SimulationSettings.CameraSensitivity * ConsoleCamInput.x));

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
            else if (BackgrES.PointerOverElement)
            {
                if (ButtonEventSystem.PointerDown(1))
                {
                    // Up/down rotation                                                                                                     // Rotation around normal
                    CameraRotation = new(CameraRotation.x + (-SimulationSettings.CameraSensitivity * ButtonEventSystem.PointerDeltaPos.y), CameraRotation.y + ((Mathf.Abs(CameraRotation.x) >= 90 ? -1f : 1f) * SimulationSettings.CameraSensitivity * ButtonEventSystem.PointerDeltaPos.x));

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
                    CamDistance += -0.016f * SimulationSettings.CameraSensitivity * ButtonEventSystem.PointerDeltaPos.y;
                    CamDistance = Mathf.Clamp(CamDistance, MinCamDistance, MaxCamDistance);

                    ThirdCamera.GetComponent<CinemachineThirdPersonFollow>().CameraDistance = CamDistance;
                }
                else if (ButtonEventSystem.PointerDown(2))
                {
                    // Camera local rotation
                    localCamRot = new(localCamRot.x + (-SimulationSettings.CameraSensitivity * ButtonEventSystem.PointerDeltaPos.y), localCamRot.y + ((Mathf.Abs(localCamRot.x) >= 90 ? -1f : 1f) * SimulationSettings.CameraSensitivity * ButtonEventSystem.PointerDeltaPos.x));

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
            #endregion

            #region Camera FOV

            if (FOVFromSpeed.keys.Length != 0 && ThirdCamera.Lens.FieldOfView != SimulationSettings.FOV + FOVFromSpeed.Evaluate(DroneBody.linearVelocity.magnitude))
            {
                SetFOV(SimulationSettings.FOV + FOVFromSpeed.Evaluate(DroneBody.linearVelocity.magnitude));
            }

            #endregion

            #region Handle Notifications

            #region MET
            /*int hour = Mathf.FloorToInt(MET / 3600);
            int minute = Mathf.FloorToInt((MET % 3600) / 60);
            int second = Mathf.FloorToInt(MET % 60);
            NotifTime.text = ((hour > 9 ? "" : "0") + hour) + ":" + ((minute > 9 ? "" : "0") + minute) + ":" + ((second > 9 ? "" : "0") + second);*/

            NotifTime.text = "•";
            int METInt = Mathf.FloorToInt(MET);
            int index = 0;
            while (METInt != 0)
            {
                NotifTime.text = (METInt % ((index % 4) + 2)).ToString() + NotifTime.text;
                METInt = Mathf.FloorToInt(METInt / ((index % 4) + 2));
                index++;
            }
            #endregion

            NotifMessageIndex.text = NotifSelectedMessage.ToString() + "/" + NotifierSystem.MainMessageList.Count.ToString();

            if (NotifAnimTimer == -1)
            {
                if (NotifierSystem.PiorityMessageList.Count != 0)
                {
                    NotifAnimTimer = 0;
                }

                if (NotifierSystem.MainMessageList.Count >= 1)
                    NotifThumbnail.text = NotifierSystem.MainMessageList[NotifSelectedMessage - 1].msg;
            }
            else
            {
                NotifSelectedMessage = NotifierSystem.MainMessageList.Count - NotifierSystem.PiorityMessageList.Count + 1;

                if (NotifAnimTimer == 0)
                {
                    NotifThumbnail.text = /*"[" + NotifierSystem.PiorityMessageList[0].MET + "] : " + */NotifierSystem.PiorityMessageList[0].msg;
                    NotifOutline.color = NotifNewMsgColor;
                    SFX_Notification();
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

            MET += Time.deltaTime;
        }

        public override void FixedTick()
        {
            #region Set Camera Rotations
            CamTarget.localRotation = Quaternion.Euler(CameraRotation.x, CameraRotation.y + 90, 0);
            CamTarget.rotation = Quaternion.Euler(CamTarget.rotation.eulerAngles.x, CamTarget.rotation.eulerAngles.y, 0);
            ThirdCamPanTilt.TiltAxis.Value = ThirdCamPanTilt.TiltAxis.Center + localCamRot.x;
            ThirdCamPanTilt.PanAxis.Value = ThirdCamPanTilt.PanAxis.Center + localCamRot.y;
            #endregion
        }

        private void ResetPivot(InputAction.CallbackContext obj)
        {
            localCamRot = Vector3.zero;
        }

        public void NotifChangeSelectedMessage(string type)
        {
            if (NotifAnimTimer == -1)
            {
                switch (type)
                {
                    case "prev":
                        NotifSelectedMessage--;
                        break;

                    case "next":
                        NotifSelectedMessage++;
                        break;

                    case "last":
                        NotifSelectedMessage = NotifierSystem.MainMessageList.Count;
                        break;
                }

                NotifSelectedMessage = Mathf.Clamp(NotifSelectedMessage, 1, NotifierSystem.MainMessageList.Count);
            }
        }

        #region Set Camera Settings

        private void SetFOV(float FOV)
        {
            ThirdCamera.Lens.FieldOfView = FOV;
            FirstCamera.Lens.FieldOfView = FOV;
        }

        #endregion

        #region Play UI SFX
        public static void SFX_Click()
        {
            FMODManager.UI.Click();
        }

        public static void SFX_Notification()
        {
            FMODManager.UI.Notification();
        }

        public static void SFX_Linking()
        {
            FMODManager.UI.Linking();
        }
        #endregion

        #region Pause Menus Controls

        public void EscapeUI()
        {
            SFX_Click();
            if (DroneBody == null)
                DroneBody = transform.GetComponentInParent<Rigidbody>();
            OtherInfo.text = Application.version + " ; " + DroneBody.position + " ; " + System.DateTime.Today.ToShortDateString();

            if (PauseMenu.activeInHierarchy || FeedbackMenu.activeInHierarchy || SettingsMenu.activeInHierarchy)
            {
                ToggleMenu(-1);
                Time.timeScale = 1;
                FMODManager.PauseTime(false);

                transform.parent.SendMessage("PauseSFX", false);
                GameObject.FindGameObjectWithTag("ArtificialStructure").BroadcastMessage("PauseSFX", false);

                UIEventSystem.SetSelectedGameObject(null);
            }
            else
            {
                ToggleMenu(0);
                Time.timeScale = 0;
                FMODManager.PauseTime(true);

                transform.parent.SendMessage("PauseSFX", true);
                GameObject.FindGameObjectWithTag("ArtificialStructure").BroadcastMessage("PauseSFX", true);

                UIEventSystem.SetSelectedGameObject(UIEventSystem.transform.parent.GetComponentInChildren<Button>(false).gameObject);
            }
        }

        public void ToggleMenu(int idx)
        {
            PausePanel.SetActive(idx is 0 or 4);
            PauseMenu.SetActive(idx is 0 or 4);
            SettingsMenu.SetActive(idx == 1);
            FeedbackMenu.SetActive(idx == 2);
            FeedbackSent.SetActive(idx == 4);

            UIEventSystem.SetSelectedGameObject(UIEventSystem.transform.parent.GetComponentInChildren<Button>(false).gameObject);
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
}
