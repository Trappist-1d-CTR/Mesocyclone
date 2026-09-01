// extremely original lol
//shut it-

using UnityEngine;
using UnityEngine.SceneManagement;
using Mesocyclone.Debug;

#nullable enable // i hate this

namespace Mesocyclone.Deprecated
{
    /// <summary>
    /// Class that handles the in-game music
    /// </summary>
    [Obsolete("Old Unity-based Music Manager")]
    public class MusicManager : Tickable // why was this tickable??
    {
        #region Variables

        public static MusicManager Instance { get; private set; }

        public string CurrentContext = "";
        public bool HasSeenStar;

        public AudioClip? MenuMusic;
        public AudioClip? GameAmbience;
        public AudioClip? GameMusic;

        public string Music_Main_Playing = "";
        public Process? Ambience_Main;
        public AudioSource Ambience_StarSight = new();
        public Process? Music_Main;

        #endregion


        private void Start()
        {
            DestroyImmediate(gameObject);

            #region Init
            /*if (Instance is not null and not this) // what a mouthful
            {
                Destroy(gameObject);
                return;
            }

            Instance = this;
            DontDestroyOnLoad(gameObject);*/
            #endregion

            #region Setup and Components
            HasSeenStar = false;

            Ambience_StarSight = gameObject.AddComponent<AudioSource>();

            SceneManager.sceneLoaded += CheckScene;
            CheckScene(SceneManager.GetActiveScene(), LoadSceneMode.Single);

            if (GameAmbience != null) Ambience_Main = AudioManager.Instance.PlayRepeating(GameAmbience);

            if (CurrentContext == "MainMenu" && MenuMusic != null && Ambience_Main != null)
            {
                Music_Main = AudioManager.Instance.PlayRepeating(MenuMusic);
                Music_Main_Playing = "MenuMusic";

                Ambience_Main.Stop();
            }
            else if (GameMusic != null)
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
                if (Music_Main_Playing != "MenuMusic" && Music_Main != null && MenuMusic != null)
                {
                    AudioManager.Instance.StopRepeating(Music_Main);
                    Music_Main = AudioManager.Instance.PlayRepeating(MenuMusic);
                    Music_Main_Playing = "MenuMusic";
                }

                if (Ambience_Main != null && Ambience_Main.isRunning)
                {
                    Ambience_Main.Stop(true);
                }
            }
            else
            {
                if (Music_Main_Playing != "GameMusic" && Music_Main != null && GameMusic != null)
                {
                    AudioManager.Instance.StopRepeating(Music_Main);
                    Music_Main = AudioManager.Instance.PlayRepeating(GameMusic);
                    Music_Main_Playing = "GameMusic";
                }

                if (Ambience_Main != null && !Ambience_Main.isRunning)
                {
                    Ambience_Main.Start(true);
                }
            }
        }

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

        private void OnEnable()
        {
            Instance ??= this;
        }

        private void OnDisable()
        {
            SceneManager.sceneLoaded -= CheckScene;
            Instance = null!;
        }

        private void OnDestroy()
        {
            SceneManager.sceneLoaded -= CheckScene;
            Instance = null!;
        }

        private void OnApplicationQuit()
        {
            SceneManager.sceneLoaded -= CheckScene;
            Instance = null!;
        }

        #endregion

        #endregion
    }

}

#nullable disable
