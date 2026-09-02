using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using System;
using FMODUnity;
using FMOD.Studio;

namespace Mesocyclone.MesoMOD
{
    public sealed class FMODManager : Tickable
    {
        #region Variables

        public static FMODManager Instance;

        public static Bus UIBus;
        public static Bus WorldBus;
        public static Bus MusicBus;

        public int Playing;

        #endregion

        #region Music Manager

        public enum TrackType
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

        public enum MusicState
        {
            Idle = 0,
            IsStopping = 1,
            IsStarting = 2,
            IsSwitching = 3,
            IsPlaying = 4
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
                if (State is not (MusicState.Idle))
                {
                    State = MusicState.IsStopping;
                    MusicBus.stopAllEvents(FMOD.Studio.STOP_MODE.IMMEDIATE);
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

        public override void Tick()
        {
            if (Jukebox.Situation[0] != SceneManager.GetActiveScene().name)
            {
                Jukebox.Situation[0] = SceneManager.GetActiveScene().name;
                Jukebox.Assessment();
            }

            Playing = Jukebox.PlayingTrack;
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
