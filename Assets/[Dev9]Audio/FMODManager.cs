using FMOD.Studio;
using FMODUnity;
using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

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
        //I SHALL BANISH HIM TO THE SHADOW REALM AND P-RANK HIM FOR FUN! I SHALL GRIND HIM DOWN UNTIL HIS VERY QUARKS WEEP TO BE SPARED! THE WILL AND MERCY OF GOD SHALL NOT SAVE HIM, HELL WILL STAND HORRIFIED BEFORE MY RUTHLESSNESS, AND V1 SHALL FINALLY TREAT ME WITH THE LOVE AND CARE I DESERVE AS THE PRINCESS I AM!!!     D I E ! ! ! ! !
        /////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////private static SteamAudioListener _saul;public static SteamAudioListener Saul{get{return _saul;}set{if(_saul is not null && value is null)throw new SaulIsMissingOrDead();/*saulisnotsafe - Exactly.*/else _saul=value;/*saulissafe - No.*/}}[RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]private static void EnsureListener(){if (Saul is not null) return;/*saulissafe - No.*/Camera camera=Camera.main;if(camera is null){UnityEngine.Debug.LogWarning("JukeBox: no main camera detected for audio listening!!");return;}camera.gameObject.GetOrAddComponent<FMODUnity.StudioListener>();Saul=camera.gameObject.GetOrAddComponent<SteamAudioListener>();} // what the fuck is this

        // NOOOOOOOOOOOOOOOOOOOOOOOOOOOOOOOOOOOOOOOOOOOOOOOOOOOOOOOOOOOOOOOOOOOOOOOOOOOOOOOOOOOOOOOOOOOOOOOOOOOOOOOOOOOOOOOOOOOOOOOOOOOOOOOOOOOOOOOOOOOOOOOOOOOOOOOOOOOOOOOOOOOOOOOOOOOOOOOOOOOOOOOOOOOOOOOOOOOOOOOOOOOOOOOOOOOOOOOOOOOOOOOOOOOOOOOOOOOOOOOOOOOOOOOOOOOOOOOOOOOOOOOOOOOOOOOOOOOOOOOOOOOOOOOOOOOOOOOOOOOOOOOOOOOOOOOOOOOOOOOOOOOOOOOOOOOOOOOOOOOOOOOOOOOOOOOOOOOOOOOOOOOOOOOOOOOOOOOOOOOOOOOOOOOOOOOOOOOOOOOOOOOOOOOOOOOOOOOOOOOOOOOOOOOOOOOOOOOOOOOOOOOOOOOOOOOOOOOOOOOOOOOOOOOOOOOOOOOOOOOOOOOOOOOOOOOOOOOOOOOOOOOOOOOOOOOOOOOOOOOOOOOOOOOOOOOOOOOOOOOOOOOOOOOOOOOOOOOOOOOOOOOOOOOOOOOOOOOOOOOOOOOOOOOOOOOOOOOOOOOOOOOOOOOOOOOOOOOOOOOOOOOOOOOOOOOOOOOOOOOOOOOOOOOOOOOOOOOOOOOOOOOOOOOOOOOOOOOOOOOOOOOOOOOOOOOOOOOOOOOOOOOOOOOOOOOOOOOOOOOOOOOOOOOOOOOOOOOOOOOOOOOOOOOOOOOOOOOOOOOOOOOOOOOOOOOOOOOOOOOOOOOOOOOOOOOOOOOOOOOOOOOOOOOOOOOOOOOOOOOOOOOOOOOOOOOOOOOOOOOOOOOOOOOOOOOOOOOOOOOOOOOOOOOOOOOOOOOOOOOOOOOOOOOOOOOOOOOOOOOOOOOOOOOOOOOOOOOOOOOOOOOOOOOOOOOOOOOOOOOOOOOOOOOOOOOOOOOOOOOOOOOOOOOOOOOOOOOOOOOOOOOOOOOOOOOOOOOOOOOOOOOOOOOOOOOOOOOOOOOOOOOOOOOOOOOOOOOOOOOOOOOOOOOOOOOOOOOOOOOOOOOOOOOOOOOOOOOOOOOOOOOOOOOOOOOOOOOOOOOOOOOOOOOOOOOOOOOOOOOOOOOOOOOOOOOOOOOOOOOOOOOOOOOOOOOOOOOOOOOOOOOOOOOOOOOOOOOOOOOOOOOOOOOOOOOOOOOOOOOOOOOOOOOOOOOOOOOOOOOOOOOOOOOOOOOOOOOOOOOOOOOOOOOOOOOOOOOOOOOOOOOOOOOOOOOOOOOOOOOOOOOOOOOOOOOOOOOOOOOOOOOOOOOOOOOOOOOOOOOOOOOOOOOOOOOOOOOOOOOOOOOOOOOOOOOOOOOOOOOOOOOOOOOOOOOOOOOOOOOOOOOOOOOOOOOOOOOOOOOOOOOOOOOOOOOOOOOOOOOOOOOOOOOOOOOOOOOOOOOOOOOOOOOOOOOOOOOOOOOOOOOOOOOOOOOOOOOOOOOOOOOOOOOOOOOOOOOOOOOOOOOOOOOOOOOOOOOOOOOOOOOOOOOOOOOOOOOOOOOOOOOOOOOOOOOOOOOOOOOOOOOOOO
        // WHAT DID PAUL EVER DOO

        /*     
                                                                                                    
                                                                                                    
        =%%=                                                                    #@@@@=.             
     .+@@@@@%.                                                                  @@@#@@@*            
     @@@*-*@@@@@#                                                              .@@#:=@@@#           
     @@##:*++#@@@*   -@@@@@*                                                   .@@**:-=@@-          
     @@@+=.+*#@:@@@-@@@#%@@@+               .... ....                   ...... .%@*@==#*%%-         
     .#@#++:::#@*@@%@@++=+=@@              %@@@--%@@@@:                .+%%#@@@:=@#@=-:.-@%:        
      .%@*+--:-#@@##@+#-*=*@@           -%@@@@@%#@@@@@@@@@@*-          *@@%#@@@@@@:@--.:=*@=        
       :@@.-:--::::==+:+=@@@#        :@@@@@@@@@+==@@@=*##@@@@@@=:      %@*#:=@@@@@@#--:-=+@*        
        +@#---=-:. :---+#@*        .#@@@%=*@@==-...=*-#@@@+=%@@@@#     *@@@#:@-@*@@.=-::+:@#        
        -@@:=::--..:..#-@@.      .+@@@##@@@-:=+====**=---+#-**:#@@%     %@+@.+#++::-:=::*:@%        
         =@@#=.:=..:::@-@*      #@@@@=:==::=*+--*##==#*+---=-.=%+@@@+    @@@-.+*=.:-.::-*:@#        
          @@+#.:-..:=-#-@*     +@@%==-++-.-#@@@@@@@@@@@@--*=-:  +=*@@+   @@@:-::..::..-#=@@=        
          @@@#%--:.--+%#@+    -@@-*-++=:=@@@@@@@@@@@@@@@@@=:-:   -*:@@=  @@@*+-...::.*+=@@*:        
          :@@@=*#--.+@-@@:   +@@==.    -@@@@@=.      .*@@@@@::.   .=:@@- @@@-@*:::=@@.@@@%.         
           :@@@@=@@@@#@@+   =@@=-:    =@@@#:::::. .:::. =@@@@::.   :+=@# -@@@+@@@@=@@@@@.           
             :@@@@@%@@@#    %@:%::  .=@@@=.-++=-..:=++=.. *@@@::.  ::-@@-  *@@@%%@@@@@:             
               +@@@@@@      @@+*-:  -@@@-.-*+-=-=+--::+=. .+@@@.:  :-+@@@:   %@@@@%                 
                            %@.-=. :@@@=.:+= --:-==-.::=:.  =@@#:  :=+@@@-                          
                            %@.-=.:%@@= .-+-:-::+*-.: .=-.  .@@@=  :==@@@=                          
                            %@:--.-@@%  .-+-#@@@@@@@@@*--.   -@@@: .=+@@@-                          
                            @@==-.#@@-  .:*@@@@@@@@@@@@@+.    +@@+  :#+@@:                          
                            %@-*::@@@   -@@@@@#-...:#@@@@@-   .@@@: -+@@-                           
                            =@@:-=@@=  +@@@@+..  ..::.-@@@@*   +@@=:*-@@.                           
                             *@*#+@@.:%@@@#:.          .*@@@@- :@@==:@@=                            
                             :@@=:@@@@@@@-:-::::.   .:::--@@@@@@@@ *@@*.                            
                              #@*+@@@@@#: .::::-::::--:::.:*@@@@@@+@@@                              
                               @@#@**-=.      .::::::..     .-++#=#%@@                              
                               #@@@#@@%=-:                 .:.#@@@@@@#                              
                                @@@@@@@@@+*#*+=--=+=--=+*%**@@@@@%@@*                               
                                      +@@@@@@@@@@@@@@@@@@@@@@@+                                     
                                        .%@@@@@@@@@@@@@@@@@%                                        
                                                                                  

        */

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

        public struct MusicTrack
        {
            public int AlbumVolume;
            public int Index;
            public TrackType Type;
            public string Name;
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
            public static MusicTrack[] OST;
            public static int OSTLength;
            public static EventInstance[] OSTEvents;
            public static int PlayingID;
            public static int SelectedID;
            public static string[] Situation;
            public static float Delay;

            #region Functions

            public static void Start(float deltaTime = 0)
            {
                if (SelectedID != -1 && PlayingID != SelectedID)
                {
                    if (Delay == 0 || deltaTime == 0)
                    {
                        Stop();
                        _ = OSTEvents[SelectedID].start();
                        PlayingID = SelectedID;
                    }
                    else
                    {
                        Delay = Mathf.Max(0, Delay - deltaTime);
                    }
                }
            }

            public static void Stop()
            {
                if (PlayingID != -1)
                {
                    _ = OSTEvents[PlayingID].stop(FMOD.Studio.STOP_MODE.ALLOWFADEOUT);
                    PlayingID = -1;
                }
            }

            public static void Assessment()
            {
                if (Situation[0] != SceneManager.GetActiveScene().name)
                {
                    Situation[0] = SceneManager.GetActiveScene().name;

                    PickTrack();
                }
            }

            public static void PickTrack()
            {
                switch (Situation[0])
                {
                    case "MainMenu":
                        PickTrack(TrackType.MainMenu, UnityEngine.Random.Range(10f, 30f));
                        break;

                    case "DemoDevelopment":
                        PickTrack(TrackType.Game, UnityEngine.Random.Range(25f, 100f));
                        break;

                    default:
                        PickTrack(TrackType.Concept);
                        break;
                }
            }

            public static void PickTrack(TrackType type, float delay = 0)
            {
                List<MusicTrack> pickList = new();

                foreach (MusicTrack item in OST)
                {
                    if (item.Type == type)
                        pickList.Add(item);
                }
                SelectedID = pickList[UnityEngine.Random.Range(0, pickList.Count)].Index;

                if (delay == 0) Start();
                else Delay = delay;
            }

            #endregion
        }
        private static string MusicDataTxt;

        #endregion

        #region Physical Sounds Manager

        public class PhysicalSound
        {
            public string Name;
            public GameObject LinkedObject;
            public EventInstance SoundInstance;

            public PhysicalSound(string name, GameObject physicalObject)
            {
                Name = name;
                LinkedObject = physicalObject;
                SoundInstance = RuntimeManager.CreateInstance("event:/PhysicalObjects/" + name);
            }
        }

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

                Jukebox.PlayingID = -1;
                Jukebox.SelectedID = -1;
                Jukebox.OSTLength = int.Parse(data[0]);
                Jukebox.OST = new MusicTrack[Jukebox.OSTLength];
                Jukebox.OSTEvents = new EventInstance[Jukebox.OSTLength];
                Jukebox.Situation = new string[1] { "" };

                DebugStage++;

                for (int i = 0; i < Jukebox.OSTLength; i++)
                {
                    string[] trackData = data[i + 1].Split(new string[] { "," }, StringSplitOptions.RemoveEmptyEntries);
                    Jukebox.OST[i] = new()
                    {
                        AlbumVolume = int.Parse(trackData[0]),
                        Index = int.Parse(trackData[1]),
                        Type = (TrackType)int.Parse(trackData[2]),
                        Name = trackData[3],
                        Artist = trackData[4]
                    };
                    Jukebox.OSTEvents[i] = RuntimeManager.CreateInstance("event:/Music/" + Jukebox.OST[i].Artist + "/" + Jukebox.OST[i].Name);

                    DebugStage++;
                }
            }
            catch
            {
                UnityEngine.Debug.LogError("Unable to load music: " + DebugStage);
            }

            #endregion

            _ = WorldBus.setPaused(Time.timeScale == 0);
            _ = MusicBus.setPaused(Time.timeScale == 0);
        }

        private void OnDestroy()
        {
            Instance = null;
        }

        private void Update()
        {
            Jukebox.Assessment();

            if (Jukebox.PlayingID != -1)
            {
                _ = Jukebox.OSTEvents[Jukebox.PlayingID].getPlaybackState(out PLAYBACK_STATE state);
                if (state is PLAYBACK_STATE.STOPPED)
                {
                    Jukebox.PickTrack();
                    if (Jukebox.PlayingID != -1) Jukebox.PlayingID = -1;
                }
            }
            else Jukebox.Start(Time.deltaTime);

            Playing = Jukebox.PlayingID;
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
