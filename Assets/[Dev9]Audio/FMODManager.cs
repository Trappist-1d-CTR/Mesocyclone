using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using FMODUnity;
using FMOD.Studio;

namespace Mesocyclone.MesoMod // yknow; FMOD, MesoFMOD, MesoMod? This isn't the modding API btw, that's BepInEx's job
{
    /// <summary>
    /// New and improved music manager! :D
    /// </summary>
    public class FMODManager : MonoBehaviour // kickass name
    {
        #region Variables

        public static FMODManager Instance { get; private set; }

        public Bus UIBus;
        public Bus WorldBus;
        public Bus MusicBus;

        public int Playing;

        #endregion

        #region Saul

        // say hello to saul, take good care of him
        //I SHALL BANISH HIM TO THE SHADOW REALM AND P-RANK HIM FOR FUN! I SHALL GRIND HIM DOWN UNTIL HIS VERY QUARKS WEEP TO BE SPARED! THE WILL AND MERCY OF GOD SHALL NOT SAVE HIM, HELL WILL STAND HORRIFIED BEFORE MY RUTHLESSNESS, AND V1 SHALL FINALLY TREAT ME WITH THE LOVE AND CARE I DESERVE IT AS THE PRINCESS I AM!!!     D I E ! ! ! ! !
        /////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////private static SteamAudioListener _saul;public static SteamAudioListener Saul{get{return _saul;}set{if(_saul is not null && value is null)throw new SaulIsMissingOrDead();/*saulisnotsafe - Exactly.*/else _saul=value;/*saulissafe - No.*/}}[RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]private static void EnsureListener(){if (Saul is not null) return;/*saulissafe - No.*/Camera camera=Camera.main;if(camera is null){UnityEngine.Debug.LogWarning("JukeBox: no main camera detected for audio listening!!");return;}camera.gameObject.GetOrAddComponent<FMODUnity.StudioListener>();Saul=camera.gameObject.GetOrAddComponent<SteamAudioListener>();}

        #endregion

        #region Music Manager

        public enum TrackType : byte
        {
            Concept = 0,
            MainMenu = 1,
            Game = 2,
            Threat = 3,
            Special = 4
        }

        public struct MusicTrack
        {
            public int TrackVolume;
            public int TrackIndex;
            public TrackType TrackType;
            public string TrackName;
            public string Artist;
        }

        public enum MusicState : byte
        {
            Idle,
            IsStopping,
            IsStarting,
            IsSwitching,
            IsPlaying
        }

        public static class Jukebox
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
                    Instance.MusicBus.stopAllEvents(FMOD.Studio.STOP_MODE.IMMEDIATE); // holy FMOD naming conventions are so fucked. This isn't C++ my guy
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
                    Instance.MusicBus.stopAllEvents(FMOD.Studio.STOP_MODE.IMMEDIATE);
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
                if (PlayingTrack != -1 && Tracks[PlayingTrack].TrackType == type) return;

                List<MusicTrack> pickList = new();

                foreach (MusicTrack item in Tracks)
                {
                    if (item.TrackType == type)
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

                Jukebox.State = 0;
                Jukebox.PlayingTrack = -1;
                Jukebox.SelectedTrack = -1;
                Jukebox.TrackListLength = int.Parse(data[0]);
                Jukebox.Tracks = new MusicTrack[Jukebox.TrackListLength];
                Jukebox.Situation = new string[1] { "" };

                DebugStage++;

                for (int i = 0; i < Jukebox.TrackListLength; i++)
                {
                    string[] trackData = data[i + 1].Split(new string[] { "," }, StringSplitOptions.RemoveEmptyEntries);
                    Jukebox.Tracks[i] = new()
                    {
                        TrackVolume = int.Parse(trackData[0]),
                        TrackIndex = int.Parse(trackData[1]),
                        TrackType = (TrackType)int.Parse(trackData[2]),
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
            if (Jukebox.Situation[0] != SceneManager.GetActiveScene().name)
            {
                Jukebox.Situation[0] = SceneManager.GetActiveScene().name;
                Jukebox.Assessment();
            }

            Playing = Jukebox.PlayingTrack;
        }

        #region Pause/Resume
        public void PauseTime(bool paused)
        {
            _ = Instance.WorldBus.setPaused(paused);
            _ = Instance.MusicBus.setPaused(paused);
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

            public static void PlayWawa()
            {
                RuntimeManager.PlayOneShot("event:/UI/Wawa");
            }
            #endregion
        }

        public static class Collision
        {
            #region Play Collision SFX
            public static void PlayHangarCover(Vector3 pos)
            {
                RuntimeManager.PlayOneShot("event:/Collisions/HangarCover", pos);
            }
            
            public static void PlayDroneTerrain(GameObject drone)
            {
                RuntimeManager.PlayOneShotAttached("event:/Collisions/DroneTerrain", drone);
            }
            #endregion
        }
    }
}
