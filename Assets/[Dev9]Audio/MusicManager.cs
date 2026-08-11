using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using Mesocyclone;
using Mesocyclone.Debug; // System.Diagnostics.Process exists...

#nullable enable // i hate this

namespace Mesocyclone.Music
{
    /// <summary>
    /// Class that handles the in-game music
    /// </summary>
    public sealed class MusicManager : Tickable
    {
        #region Variables

        public static MusicManager? _instance;
        public static MusicManager Instance
        {
            get
            {
                if (_instance == null)
                {
                    GameObject MusicManagerGO = new("Music Manager");
                    _instance = MusicManagerGO.AddComponent<MusicManager>();
                    DontDestroyOnLoad(MusicManagerGO);
                }
                return _instance;
            }

            private set => _instance = value;
        }

        public string CurrentContext = "";
        public bool HasSeenStar;

        public AudioClip MenuMusic;
        public AudioClip GameAmbience;
        public AudioClip GameMusic;

        public string Music_Main_Playing = "";
        public Process? Ambience_Main;
        public AudioSource Ambience_StarSight = new();
        public Process? Music_Main;

        #endregion


        private MusicManager() { }

        private void Start()
        {
            #region Init
            if (_instance != null && _instance != this)
            {
                Destroy(gameObject);
                return;
            }

            _instance = this;
            DontDestroyOnLoad(gameObject);
            #endregion

            #region Setup and Components
            HasSeenStar = false;

            Ambience_StarSight = gameObject.AddComponent<AudioSource>();

            SceneManager.sceneLoaded += CheckScene;
            CheckScene(SceneManager.GetActiveScene(), LoadSceneMode.Single);

            Ambience_Main = AudioManager.Instance.PlayRepeating(GameAmbience);

            if (CurrentContext == "MainMenu")
            {
                Music_Main = AudioManager.Instance.PlayRepeating(MenuMusic);
                Music_Main_Playing = "MenuMusic";

                Ambience_Main.Stop();
            }
            else
            {
                Music_Main = AudioManager.Instance.PlayRepeating(GameMusic);
                Music_Main_Playing = "GameMusic";
            }
            #endregion
        }

        public override void Tick()
        {
            if (CurrentContext == "MainMenu")
            {
                if (Music_Main_Playing != "MenuMusic")
                {
                    AudioManager.Instance.StopRepeating(Music_Main);
                    Music_Main = AudioManager.Instance.PlayRepeating(MenuMusic);
                    Music_Main_Playing = "MenuMusic";
                }

                if (Ambience_Main.isRunning)
                {
                    Ambience_Main.Stop(true);
                }
            }
            else
            {
                if (Music_Main_Playing != "GameMusic")
                {
                    AudioManager.Instance.StopRepeating(Music_Main);
                    Music_Main = AudioManager.Instance.PlayRepeating(GameMusic);
                    Music_Main_Playing = "GameMusic";
                }

                if (!Ambience_Main.isRunning)
                {
                    Ambience_Main.Start(true);
                }
            }
        }

        public override void FixedTick() { return; }


        #region Functions

        #region Misc
        private static void CheckScene(Scene sceneStruct, LoadSceneMode loadMode)
        {
            switch (sceneStruct.name)
            {
                case "MainMenu":
                    Instance.CurrentContext = "MainMenu";
                    break;

                case "DemoDevelopment":
                    Instance.CurrentContext = "DemoGame";
                    break;

                default:
                    Instance.CurrentContext = "Unknown";
                    throw new Joar(); //no one's sins shall go unnoticed
            }
        }

        public static void StarSight()
        {
            if (!Instance.HasSeenStar)
            {
                Instance.HasSeenStar = true;
                Instance.Ambience_StarSight.PlayOneShot(Resources.Load<AudioClip>("SFX/Ambience/Tension"), 3f);
            }
        }
        #endregion

        #region OnDestroy

        void OnDestroy()
        {
            SceneManager.sceneLoaded -= CheckScene;
            Instance = null!;
        }

        #endregion

        #endregion
    }

#nullable disable
}