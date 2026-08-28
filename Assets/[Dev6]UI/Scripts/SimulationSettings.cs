using UnityEngine;

namespace Mesocyclone
{
    public static class SimulationSettings
    {
        #region Values
        public static float FOV;
        public static int FPSCap;
        public static bool VSync;

        public static int ResolutionX;
        public static int ResolutionY;
        public static bool Fullscreen;
        public static float Brightness;
        public static float Contrast;
        public static float Gamma;
        public static int AntiAliasing;
        public static int AnisotropicFiltering;

        public static bool InvertedPitch;
        public static float CameraSensitivity;
        #endregion

        #region Save/Load Settings
        public static void Default()
        {
            FOV = 37;
            FPSCap = 60;
            VSync = true;

            ResolutionX = 1920;
            ResolutionY = 1080;
            Fullscreen = true;
            Brightness = 0f;
            Contrast = 0f;
            Gamma = 0f;
            AntiAliasing = 1;
            AnisotropicFiltering = 0;

            InvertedPitch = true;
            CameraSensitivity = 0.1f;

            Save();
        }

        public static void Load()
        {
            try
            {
                FOV = PlayerPrefs.GetFloat("FOV");
                FPSCap = PlayerPrefs.GetInt("FPSCap");
                VSync = PlayerPrefs.GetInt("VSync") == 1;

                ResolutionX = PlayerPrefs.GetInt("ResolutionX");
                ResolutionY = PlayerPrefs.GetInt("ResolutionY");
                Fullscreen = PlayerPrefs.GetInt("Fullscreen") == 1;
                Brightness = PlayerPrefs.GetFloat("Brightness");
                Contrast = PlayerPrefs.GetFloat("Contrast");
                Gamma = Contrast = PlayerPrefs.GetFloat("Gamma");
                AntiAliasing = PlayerPrefs.GetInt("AntiAliasing");
                AnisotropicFiltering = PlayerPrefs.GetInt("AnisotropicFiltering");

                InvertedPitch = PlayerPrefs.GetInt("InvertedPitch") == 1;
                CameraSensitivity = PlayerPrefs.GetFloat("CameraSensitivity");
            }
            catch
            {
                Default();
            }
        }

        public static void Save()
        {
            PlayerPrefs.SetFloat("FOV", FOV);
            PlayerPrefs.SetInt("FPSCap", FPSCap);
            PlayerPrefs.SetInt("VSync", VSync ? 1 : 0);

            PlayerPrefs.SetInt("ResolutionX", ResolutionX);
            PlayerPrefs.SetInt("ResolutionY", ResolutionY);
            PlayerPrefs.SetInt("Fullscreen", Fullscreen ? 1 : 0);
            PlayerPrefs.SetFloat("Brightness", Brightness);
            PlayerPrefs.SetFloat("Contrast", Contrast);
            PlayerPrefs.SetFloat("Gamma", Gamma);
            PlayerPrefs.SetInt("AntiAliasing", AntiAliasing);
            PlayerPrefs.SetInt("AnisotropicFiltering", AnisotropicFiltering);

            PlayerPrefs.SetInt("InvertedPitch", InvertedPitch ? 1 : 0);
            PlayerPrefs.SetFloat("CameraSensitivity", CameraSensitivity);

            PlayerPrefs.Save();
        }
        #endregion

        #region Apply Settings

        #endregion
    }
}