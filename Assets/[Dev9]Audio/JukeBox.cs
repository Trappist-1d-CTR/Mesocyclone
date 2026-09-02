using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.Extensions;
using FMODUnity;
using FMOD.Studio;
using Mesocyclone.Security.Critical;

namespace Mesocyclone.MesoMod // yknow; FMOD, MesoFMOD, MesoMod? This isn't the modding API btw, that's BepInEx's job
{
    /// <summary>
    /// New and improved music manager! :D
    /// </summary>
    public class JukeBox : MonoBehaviour // kickass name
    {
        #region Variables

        public static JukeBox Instance { get; private set; }

        public Bus UIBus;
        public Bus WorldBus;
        public Bus MusicBus;

        public int Playing;

        #endregion

        #region Saul

         // say hello to saul, take good care of him
        private static SteamAudioListener _saul;

        public static SteamAudioListener Saul
        {
            get { return _saul; }
            set
            {
                if (_saul is not null && value is null)
                    throw new SaulIsMissingOrDead(); // saul is not safe
                else
                    _saul = value; // saul is safe
            }
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void EnsureListener()
        {
            if (Saul is not null) return; // saul is safe

            Camera camera = Camera.main;

            if (camera is null)
            {
                UnityEngine.Debug.LogWarning("JukeBox: no main camera detected for audio listening!!");
                return;
            }

            camera.gameObject.GetOrAddComponent<FMODUnity.StudioListener>();
            Saul = camera.gameObject.GetOrAddComponent<SteamAudioListener>();
        }

        #endregion

        #region Music Manager

        public enum TrackType : byte
        {
            Concept,
            MainMenu,
            Game,
            Threat,
            Special
        }

        [Serializable]
        public struct MusicTrack
        {
            public int Volume;
            public int TrackIndex;
            public TrackType Type;
            public string TrackName;
            public string Artist; // shouldnt this be like a constant or smth because there only is one artist -.- // unless i start making music for this game aswell :3 // COFFEE, YOU HAVE COMPETITION!!!
        }

        public enum MusicState : byte
        {
            Idle,
            IsStopping,
            IsStarting,
            IsSwitching,
            IsPlaying
        }

        public static class MusicManager
        {
            public static MusicState State;
            public static int PlayingTrack;
            public static int SelectedTrack;
            public static int TrackListLength;
            public static MusicTrack[] Tracks;
            public static string[] Situation;

            #region Functions
            public static void Stop()
            {
                if (State is not MusicState.Idle)
                {
                    State = MusicState.IsStopping;
                    MusicBus.stopAllEvents(FMOD.Studio.STOP_MODE.IMMEDIATE); // holy FMOD naming conventions are so fucked. This isn't C++ my guy
                    PlayingTrack = -1;
                    State = MusicState.Idle;
                }
            }
            public static void Start()
            {
                if (State is not (MusicState.IsPlaying or MusicState.IsSwitching))
                {
                    State = MusicState.IsStarting;
                    RuntimeManager.PlayOneShot("event:/Music/" + Tracks[SelectedTrack].Artist + "/" + Tracks[SelectedTrack].TrackName);
                    PlayingTrack = SelectedTrack;
                    State = MusicState.IsPlaying;
                }
            }
            public static void Switch()
            {
                if (State is not (MusicState.Idle or MusicState.IsStopping))
                {
                    State = MusicState.IsSwitching;
                    MusicBus.stopAllEvents(FMOD.Studio.STOP_MODE.IMMEDIATE);
                    RuntimeManager.PlayOneShot("event:/Music/" + Tracks[SelectedTrack].Artist + "/" + Tracks[SelectedTrack].TrackName);
                    PlayingTrack = SelectedTrack;
                    State = MusicState.IsPlaying;
                }
            }

            public static void Assessment()
            {
                switch (Situation[0])
                {
                    case "MainMenu":
                        PickTrack(TrackType.MainMenu);
                        break;

                    case "DemoDevelopment":
                        PickTrack(TrackType.Game);
                        break;

                    default:
                        PickTrack(TrackType.Concept);
                        break;
                }
            }
            public static void PickTrack(TrackType type)
            {
                if (PlayingTrack != -1 && Tracks[PlayingTrack].Type == type) return;

                List<MusicTrack> pickList = new();

                foreach (MusicTrack item in Tracks)
                {
                    if (item.Type == type)
                        pickList.Add(item);
                }
                SelectedTrack = pickList[UnityEngine.Random.Range(0, pickList.Count)].TrackIndex;

                if (PlayingTrack == -1) Start();
                else Switch();
            }

            #endregion
        }
        private static string MusicDataTxt;

        #endregion

        private void Start()
        {
            #region Init
            if (Instance == null)
            {
                Instance = this;
                DontDestroyOnLoad(gameObject);
            }
            else DestroyImmediate(gameObject);
            #endregion

            #region Get Busses
            UIBus = RuntimeManager.GetBus("Bus:/UI");
            WorldBus = RuntimeManager.GetBus("Bus:/World");
            MusicBus = RuntimeManager.GetBus("Bus:/Music");
            #endregion

            #region Get Music Tracks
            int DebugStage = 0;

            try
            {
                MusicDataTxt = System.IO.File.ReadAllText(Application.streamingAssetsPath + "/DroneData/MusicPlaylist.json");

                string[] data = MusicDataTxt.Split(new string[] { "{", ";", "}" }, StringSplitOptions.RemoveEmptyEntries);

                DebugStage++;

                MusicManager.State = 0;
                MusicManager.PlayingTrack = -1;
                MusicManager.SelectedTrack = -1;
                MusicManager.TrackListLength = int.Parse(data[0]);
                MusicManager.Tracks = new MusicTrack[MusicManager.TrackListLength];
                MusicManager.Situation = new string[1] { "" };

                DebugStage++;

                for (int i = 0; i < MusicManager.TrackListLength; i++)
                {
                    string[] trackData = data[i + 1].Split(new string[] { "," }, StringSplitOptions.RemoveEmptyEntries);
                    MusicManager.Tracks[i] = new()
                    {
                        Volume = int.Parse(trackData[0]),
                        TrackIndex = int.Parse(trackData[1]),
                        Type = (TrackType)int.Parse(trackData[2]),
                        TrackName = trackData[3],
                        Artist = trackData[4]
                    };

                    DebugStage++;
                }
            }
            catch
            {
                UnityEngine.Debug.LogError("Unable to load music: " + DebugStage);
            }
            #endregion
        }

        private void OnDestroy()
        {
            Instance = null;
        }

        private void Update()
        {
            if (MusicManager.Situation[0] != SceneManager.GetActiveScene().name)
            {
                MusicManager.Situation[0] = SceneManager.GetActiveScene().name;
                MusicManager.Assessment();
            }

            Playing = MusicManager.PlayingTrack;
        }

        #region Pause/Resume
        public static void PauseTime(bool paused)
        {
            _ = WorldBus.setPaused(paused);
            _ = MusicBus.setPaused(paused);
        }
        #endregion

        public static class UI
        {
            #region Play UI SFX 
            public static void PlayClick()
            {
                RuntimeManager.PlayOneShot("event:/UI/Click");
            }

            public static void PlayNotification()
            {
                RuntimeManager.PlayOneShot("event:/UI/Notification");
            }

            public static void PlayLinking()
            {
                RuntimeManager.PlayOneShot("event:/UI/Linking");
            }
            #endregion
        }

        public static class Collision
        {
            #region Play Collision SFX
            public static void PlayHangarCover(Vector3 pos)
            {
                RuntimeManager.PlayOneShot("event:/Collision/HangarCover", pos);
            }

            public static void PlayDroneTerrain(GameObject drone)
            {
                RuntimeManager.PlayOneShotAttached("event:/Collision/DroneTerrain", drone);
            }
            #endregion
        }
    }
}
