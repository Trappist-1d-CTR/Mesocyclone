using System.Collections;
using System.Collections.Generic;
using FMODUnity;
using FMOD.Studio;

namespace Mesocyclone.FMOD
{
    public static class FMODManager
    {
        #region Variables
        public static Bus UIBus;
        public static Bus WorldBus;
        public static Bus MusicBus;
        #endregion

        #region Setup
        public static void GetBusses()
        {
            UIBus = RuntimeManager.GetBus("Bus:/UI");
            WorldBus = RuntimeManager.GetBus("Bus:/World");
            MusicBus = RuntimeManager.GetBus("Bus:/Music");
        }
        #endregion

        #region Pause/Resume
        public static void PauseTime(bool paused)
        {
            _ = WorldBus.setPaused(paused);
            _ = MusicBus.setPaused(paused);
        }
        #endregion

        #region Play UI SFX
        public static void SFX_Click()
        {
            RuntimeManager.PlayOneShot("event:/UI/Click");
        }

        public static void SFX_Notification()
        {
            RuntimeManager.PlayOneShot("event:/UI/Notification");
        }

        public static void SFX_Linking()
        {
            RuntimeManager.PlayOneShot("event:/UI/Linking");
        }
        #endregion
    }
}
