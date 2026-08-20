using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using Mesocyclone.UI;

namespace Mesocyclone
{
    public sealed class GameSettingsManager : MonoBehaviour
    {
        #region Variables
        public Volume VolumeSettings;

        public Slider BrightnessSlider;
        public TextMeshProUGUI BrightnessValue;
        public Slider ContrastSlider;
        public TextMeshProUGUI ContrastValue;
        public Slider GammaSlider;
        public TextMeshProUGUI GammaValue;
        #endregion

        void Start()
        {
            SimulationSettings.Load();

            #region Initialize Settings
            BrightnessSlider.value = SimulationSettings.Brightness;
            BrightnessValue.text = (Mathf.Round(SimulationSettings.Brightness * 20f) / 20f).ToString();
            ContrastSlider.value = SimulationSettings.Contrast;
            ContrastValue.text = (Mathf.Round(SimulationSettings.Contrast * 10f) / 10f).ToString();
            GammaSlider.value = SimulationSettings.Gamma;
            GammaValue.text = (Mathf.Round(SimulationSettings.Contrast * 10f) / 10f).ToString();
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

        public void SetGamma(float Value)
        {
            LiftGammaGain LGG;

            SimulationSettings.Gamma = Value;
            SimulationSettings.Save();
            if (VolumeSettings.profile.TryGet(out LGG))
            {
                LGG.gamma.Override(new(1, 1, 1, Value));
            }
            GammaValue.text = (Mathf.Round(Value * 10f) / 10f).ToString();
        }
        #endregion
    }
}