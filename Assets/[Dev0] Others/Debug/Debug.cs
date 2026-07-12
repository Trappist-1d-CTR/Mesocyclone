#if UNITY_STANDALONE_WIN || UNITY_EDITOR_WIN
    #define ONWINDOWS
#endif

#if UNITY_STANDALONE_MAC || UNITY_EDITOR_MAC
    #define ONMAC
#endif

#if UNITY_STANDALONE_LINUX || UNITY_EDITOR_LINUX
    #define ONLINUX
#endif

using System.Diagnostics;
using UnityEngine;


namespace MCCustom
{
    /// <summary>
    /// Used for debugging and profiling aspects of the game
    /// </summary>
    public static partial class MCDebug
    {
        // volatile makes it so threads don't cache it
        // *why tf can't i have volatile properties*
        static volatile bool _isRunning = false;

        /// <summary>
        /// returns true if the game is running.
        /// returns false if not
        /// <para>duh</para>
        /// </summary>
        public static bool isRunning => _isRunning;

        public static bool devTools { get; private set; } 
            = false;

        static bool allowDevTools { get; set; } 
            = false; // TODO: redundant until Modding API comes out


        // have to implement this for each OS because everyone hates each other
        // like it wasn't already obvious by our mentally more or less intolerable disgust between one another
        // that causes us to fabricate weapons  which manipulate steel and powder to explode people into a bajillion pieces
        // and bombs of mass-destruction which split the very atoms that is matter
        // just because one person had bad vibes
        // #RANT

        #if ONWINDOWS

        public static PerformanceCounter w_cpuCounter { get; private set; } = new("Processor", "% Processor Time", "_Total");
        public static float w_usage { get; private set; }

        #endif


        #if ONMAC

        // TODO :

        #endif


        #if ONLINUX

        // TODO :

        #endif
        

        /// <summary>
        /// Process of the game
        /// </summary>
        public static System.Diagnostics.Process process { get; private set; } 
            = System.Diagnostics.Process.GetCurrentProcess();
        // already have a Process class in the games code, so have to be explicit
        // unless i'm just fucking stupid


        /// <summary>
        /// total elapsed time the game has been running in frames
        /// <para>not very useful information. You should just reference Time.frameCount</para>
        /// </summary>
        public static ulong totalElapsedGameTime { get; private set; }

        /// <summary>
        /// total elapsed time the game has been running in milliseconds
        /// <para>TODO: implement this in debug screen</para>
        /// </summary>
        public static float totalElapsedGameTimeInMilliseconds { get; private set; }

        /// <summary>
        /// total elapsed time the game has been running in seconds
        /// <para>TODO: implement this in debug screen</para>
        /// </summary>
        public static float totalElapsedGameTimeInSeconds { get; private set; }

        /// <summary>
        /// total elapsed time the game has been running in minutes
        /// <para>TODO: implement this in debug screen</para>
        /// </summary>
        public static float totalElapsedGameTimeInMinutes { get; private set; }

        /// <summary>
        /// total elapsed time the game has been running in days
        /// <para>TODO: implement this for a warning. I mean who's running the game for 24 hours T-T</para>
        /// </summary>
        public static float totalElapsedGameTimeInDays { get; private set; }


        public static void Init()
        {
            _isRunning = true;

            Application.quitting += OnQuit;
        }

        public static void Update()
        {
            if (allowDevTools)
            {
                if (Input.GetKeyDown(KeyCode.B))
                    devTools = !devTools;
            }

            totalElapsedGameTime = (ulong)Time.frameCount; // just reference Time.frameCount lol
            totalElapsedGameTimeInMilliseconds = (float)process.TotalProcessorTime.TotalMilliseconds;
            totalElapsedGameTimeInSeconds = (float)process.TotalProcessorTime.TotalSeconds;
            totalElapsedGameTimeInMinutes = (float)process.TotalProcessorTime.TotalMinutes;
            totalElapsedGameTimeInDays = (float)process.TotalProcessorTime.TotalDays;
        }

        internal static void OnQuit()
        {
            _isRunning = false;
        }

        // seriously don't wanna bother with game objects and TMP
        public static void OnGUI()
        {
            // to prevent multiple Debug.Log calls
            bool checkDevToolsFont = true;

            if (devTools)
            {
                GUIStyleState norm = new();
                norm.textColor = Color.yellow;

                GUIStyle style = new(GUI.skin.label)
                {
                    normal = norm,
                    font = Resources.Load("Fonts & Materials/Xolonium-pn4D") as Font,
                    fontSize = 24,
                    fontStyle = FontStyle.Bold // not even sure this works on a custom font : might actually be the issue if the font doesn't load
                };

                if (style.font != Resources.Load("Fonts & Materials/Xolonium-pn4D") && checkDevToolsFont)
                {
                    UnityEngine.Debug.LogWarning
                    (
                        // REMINDER: Update ln during Debug.cs changes
                        $"Font 'Xolonium-pn4D' unable to be loaded\n@: MCCustom.MCDebug.OnGUI().style.font (Debug.cs : ln 163)\nDirectory: Assets/TextMesh Pro/Resources/Fonts & Materials/Xolonium-pn4D.asset\n \nCause may be because Unity looks at all folders named 'Resources'\nwhich could trip it up"
                    );

                    checkDevToolsFont = false; // prevent this from re-calling
                } 

                GUI.Label(new Rect(10, 10, Screen.width - 10, Screen.height - 10), "DevTools Enabled");
            }
        }
    }


    /// <summary>
    /// Handles MCDebug and integrates it into the Unity player loop
    /// </summary>
    internal sealed class MCDebugHandler : MonoBehaviour
    {
        static GameObject go;

        [RuntimeInitializeOnLoadMethod] // screw you unity
        static void Init()
        {
            MCDebug.Init();

            go = new GameObject("Debug Handler");
            _ = go.AddComponent<MCDebugHandler>();
            DontDestroyOnLoad(go);
        }

        void Awake() // nevermind
        {

        }

        void Update() // runs every frame, obvi
        {
            MCDebug.Update();
        }

        void OnGUI() => MCDebug.OnGUI();

        void OnDestroy()
        {
            Application.quitting -= MCDebug.OnQuit;
        }
    }
}