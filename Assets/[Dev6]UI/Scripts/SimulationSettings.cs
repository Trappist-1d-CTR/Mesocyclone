using UnityEngine;

namespace Mesocyclone
{
    public static class SimulationSettings
    {
        #region Values
        public static float Brightness;
        public static float Contrast;
        #endregion

        #region Save/Load Settings
        public static void Default()
        {
            Brightness = -0.1f;
            Contrast = 0;
        }

        public static void Load()
        {
            if (PlayerPrefs.HasKey("Brightness"))
            {
                Brightness = PlayerPrefs.GetFloat("Brightness");
                Contrast = PlayerPrefs.GetFloat("Contrast");
            }
            else
            {
                Default();
            }
        }

        public static void Save()
        {
            PlayerPrefs.SetFloat("Brightness", Brightness);
            PlayerPrefs.SetFloat("Contrast", Contrast);

            PlayerPrefs.Save();
        }
        #endregion

        #region Apply Settings

        #endregion
    }
}