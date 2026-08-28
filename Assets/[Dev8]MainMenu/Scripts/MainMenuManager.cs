using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using UnityEngine.EventSystems;
using TMPro;
using Mesocyclone.Sound;


namespace Mesocyclone
{
    public class MainMenuManager : MonoBehaviour
    {
        #region Variables

        #region Menu Navigation Values
        public int MenuPosition;
        #endregion

        #region Panels
        public List<GameObject> MenuPanels;
        #endregion

        #region Loading Objects
        public float ImageRotationSpeed;
        public TextMeshProUGUI LoadingText;
        public Image LoadingImage;
        public Image LoadingBar;
        #endregion

        public EventSystem MenuEventSystem;

        #endregion

        void Start()
        {
            SimulationSettings.Load();

            SelectPanel(0);
        }

        #region Panels Functions
        public void ResetPanels()
        {
            for (int i = 0; i < MenuPanels.Count; i++)
            {
                MenuPanels[i].SetActive(false);
            }
        }

        public void Panel()
        {
            ResetPanels();
            if (!MenuPanels[MenuPosition].activeSelf)
            {
                MenuPanels[MenuPosition].SetActive(true);

                if (MenuPosition == 1)
                {
                    _ = StartCoroutine(LoadGameAsync());
                }
                else
                {
                    MenuEventSystem.SetSelectedGameObject(MenuPanels[MenuPosition].GetComponentInChildren<Button>(false).gameObject);
                }
            }
        }

        public void SelectPanel(int ID)
        {
            MenuPosition = ID;
            Panel();
        }
        #endregion

        public void SoundClick()
        {
            AudioClip audioclip = Resources.Load<AudioClip>("SFX/GeneralUI/Click");
            AudioManager.Instance.Play(audioclip, MinPitch: 1f, MaxPitch: 1.01f, Volume: 0.6f);
        }

        public void QuitGame()
        {
            Application.Quit();
        }

        IEnumerator LoadGameAsync()
        {
            AsyncOperation Operation = SceneManager.LoadSceneAsync("DemoDevelopment", LoadSceneMode.Single);

            while (!Operation.isDone)
            {
                LoadingText.text = "Loading... (" + (Mathf.Round(Operation.progress / 0.0009f) / 10f) + "%)";
                LoadingImage.rectTransform.rotation = Quaternion.Euler(0, 0, Time.time * ImageRotationSpeed);
                LoadingBar.fillAmount = Mathf.Clamp01(Operation.progress / 0.9f);

                yield return null;
            }
        }
    }
}