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

        public Slider FOVSlider;
        public TextMeshProUGUI FOVValue;
        public TMP_Dropdown FPSCapDropdown;
        public Toggle VSyncToggle;

        public TMP_InputField ResolutionXInputField;
        public TMP_InputField ResolutionYInputField;
        public Toggle FullscreenToggle;
        public Slider BrightnessSlider;
        public TextMeshProUGUI BrightnessValue;
        public Slider ContrastSlider;
        public TextMeshProUGUI ContrastValue;
        public Slider GammaSlider;
        public TextMeshProUGUI GammaValue;
        public TMP_Dropdown AntiAliasingDropdown;
        public TMP_Dropdown AnisotropicFilteringDropdown;

        #endregion

        void Start()
        {
            //SimulationSettings.Default();
            SimulationSettings.Load();

            #region Initialize Settings UI

            FOVSlider.value = SimulationSettings.FOV;
            FOVValue.text = SimulationSettings.FOV.ToString();
            UnityEngine.Debug.Log(SimulationSettings.FPSCap +  " ; " + SimulationSettings.FPSCap / 10);
            FPSCapDropdown.value = SimulationSettings.FPSCap / 10;
            VSyncToggle.isOn = SimulationSettings.VSync;

            ResolutionXInputField.text = SimulationSettings.ResolutionX.ToString();
            ResolutionYInputField.text = SimulationSettings.ResolutionY.ToString();
            FullscreenToggle.isOn = SimulationSettings.Fullscreen;
            BrightnessSlider.value = SimulationSettings.Brightness;
            BrightnessValue.text = (Mathf.Round(SimulationSettings.Brightness * 20f) / 20f).ToString();
            ContrastSlider.value = SimulationSettings.Contrast;
            ContrastValue.text = (Mathf.Round(SimulationSettings.Contrast * 10f) / 10f).ToString();
            GammaSlider.value = SimulationSettings.Gamma;
            GammaValue.text = (Mathf.Round(SimulationSettings.Contrast * 10f) / 10f).ToString();
            AntiAliasingDropdown.value = SimulationSettings.AntiAliasing;
            AnisotropicFilteringDropdown.value = SimulationSettings.AnisotropicFiltering;

            #endregion

            #region Set Settings

            SetFPSCap();
            SetVSync();
            SetResolution();
            SetBrightness();
            SetContrast();
            SetGamma();
            SetAntiAliasing();
            SetAnisotropicFiltering();

            #endregion
        }

        #region Settings Functions
        public void SetFOV(float Value)
        {
            SimulationSettings.FOV = Value;
            SimulationSettings.Save();
        }

        public void SetFPSCap(int Value)
        {
            SimulationSettings.FPSCap = (Value == 0) ? 1 : 10 * Value;
            SimulationSettings.Save();

            SetFPSCap();
        }

        public void SetFPSCap()
        {
            Application.targetFrameRate = SimulationSettings.FPSCap;
        }

        public void SetVSync(bool Value)
        {
            SimulationSettings.VSync = Value;
            SimulationSettings.Save();

            SetVSync();
        }

        public void SetVSync()
        {
            QualitySettings.vSyncCount = SimulationSettings.VSync ? 0 : 1;
        }

        public void SetResolutionX(string Value)
        {
            SimulationSettings.ResolutionX = int.Parse(Value);
            SimulationSettings.Save();
        }

        public void SetResolutionY(string Value)
        {
            SimulationSettings.ResolutionY = int.Parse(Value);
            SimulationSettings.Save();
        }

        public void SetResolution()
        {
            Screen.SetResolution(SimulationSettings.ResolutionX, SimulationSettings.ResolutionY, SimulationSettings.Fullscreen);
        }

        public void SetFullscreen(bool Value)
        {
            SimulationSettings.Fullscreen = Value;
            SimulationSettings.Save();

            SetFullscreen();
        }

        public void SetFullscreen()
        {
            Screen.fullScreen = SimulationSettings.Fullscreen;
        }

        public void SetBrightness(float Value)
        {
            SimulationSettings.Brightness = Value;
            SimulationSettings.Save();
            
            BrightnessValue.text = (Mathf.Round(Value * 20f) / 20f).ToString();
            SetBrightness();
        }

        public void SetBrightness()
        {
            ColorAdjustments ColorAdj;
            if (VolumeSettings.profile.TryGet(out ColorAdj))
            {
                ColorAdj.postExposure.value = 1.5f + SimulationSettings.Brightness;
            }
        }

        public void SetContrast(float Value)
        {
            SimulationSettings.Contrast = Value;
            SimulationSettings.Save();
            
            ContrastValue.text = (Mathf.Round(Value * 10f) / 10f).ToString();
            SetContrast();
        }

        public void SetContrast()
        {
            ColorAdjustments ColorAdj;
            if (VolumeSettings.profile.TryGet(out ColorAdj))
            {
                ColorAdj.contrast.value = SimulationSettings.Contrast;
            }
        }

        public void SetGamma(float Value)
        {
            SimulationSettings.Gamma = Value;
            SimulationSettings.Save();

            GammaValue.text = (Mathf.Round(Value * 10f) / 10f).ToString();
            SetGamma();
        }

        public void SetGamma()
        {
            LiftGammaGain LGG;
            if (VolumeSettings.profile.TryGet(out LGG))
            {
                LGG.gamma.Override(new(1, 1, 1, SimulationSettings.Gamma));
            }
        }

        public void SetAntiAliasing(int Value)
        {
            SimulationSettings.AntiAliasing = Value;
            SimulationSettings.Save();

            SetAntiAliasing();
        }

        public void SetAntiAliasing()
        {
            QualitySettings.antiAliasing = SimulationSettings.AntiAliasing;
        }

        public void SetAnisotropicFiltering(int Value)
        {
            SimulationSettings.AnisotropicFiltering = Value;
            SimulationSettings.Save();

            SetAnisotropicFiltering();
        }

        public void SetAnisotropicFiltering()
        {
            QualitySettings.anisotropicFiltering = (AnisotropicFiltering)(SimulationSettings.AnisotropicFiltering);
        }
        #endregion
    }
}