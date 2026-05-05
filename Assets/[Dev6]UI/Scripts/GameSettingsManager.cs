using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

public class GameSettingsManager : MonoBehaviour
{
    #region Variables
    public Volume VolumeSettings;

    public Slider BrightnessSlider;
    public TextMeshProUGUI BrightnessValue;
    public Slider ContrastSlider;
    public TextMeshProUGUI ContrastValue;
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
    }

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
}
