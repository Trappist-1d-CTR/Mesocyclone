using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using TMPro;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

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

    #region Settings
    public Volume VolumeSettings;

    public Slider BrightnessSlider;
    public TextMeshProUGUI BrightnessValue;
    public Slider ContrastSlider;
    public TextMeshProUGUI ContrastValue;
    #endregion

    #endregion
    
    void Start()
    {
        SimulationSettings.Load();

        #region Initialize Settings
        BrightnessSlider.value = SimulationSettings.Brightness;
        BrightnessValue.text = (Mathf.Round(SimulationSettings.Brightness * 20f) / 20f).ToString();
        ContrastSlider.value = SimulationSettings.Contrast;
        ContrastValue.text = (Mathf.Round(SimulationSettings.Contrast * 10f) / 10f).ToString();
        #endregion

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
        }
    }

    public void SelectPanel(int ID)
    {
        MenuPosition = ID;
        Panel();
    }
    #endregion

    #region Settings Functions
    public void SetBrightness(float Value)
    {
        ColorAdjustments ColorAdj;

        SimulationSettings.Brightness = Value;
        SimulationSettings.Save();
        if (VolumeSettings.profile.TryGet(out ColorAdj))
        {
            ColorAdj.postExposure.value = SimulationSettings.Brightness;
        }
        BrightnessValue.text = (Mathf.Round(Value * 20f) / 20f).ToString();
    }

    public void SetContrast(float Value)
    {
        ColorAdjustments ColorAdj;

        SimulationSettings.Contrast = Value;
        SimulationSettings.Save();
        if (VolumeSettings.profile.TryGet(out ColorAdj))
        {
            ColorAdj.contrast.value = SimulationSettings.Contrast;
        }
        ContrastValue.text = (Mathf.Round(Value * 10f) / 10f).ToString();
    }
    #endregion

    public void QuitGame()
    {
        Application.Quit();
    }

    IEnumerator LoadGameAsync()
    {
        AsyncOperation Operation = SceneManager.LoadSceneAsync("EnvironmentDevelopment", LoadSceneMode.Single);

        while (!Operation.isDone)
        {
            LoadingText.text = "Loading... (" + (Mathf.Round(Operation.progress / 0.0009f) / 10f) + "%)";
            LoadingImage.rectTransform.rotation = Quaternion.Euler(0, 0, Time.time * ImageRotationSpeed);
            LoadingBar.fillAmount = Mathf.Clamp01(Operation.progress / 0.9f);

            yield return null;
        }
    }
}
