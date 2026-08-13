using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using UnityEngine;
using Unity.Collections;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.Utilities;

// TODO: Implement CPU/GPU usage checks
// TODO: Implement more than just resolution changes

// moral of the story:
// NEVER make a simulation game in Unity unless it's Unity 5 or smth
// or you're writing your own engine ontop of unity in which there's only one MonoBehaviour
// i.e: Futile framework (i needa work on a fork of that)
// if we just made this godot i probably would not need to write this T-T

namespace Mesocyclone
{
    /// <summary>
    /// for your safety, do not look at the code
    /// </summary>
    public sealed partial class DynamicGameStateHandler : Tickable
    {
        #region Resource Checking

        // 0 = :D
        // 1 = :)
        // 2+ = :(
        [
            // i think this syntax works, don't @ me
            SerializeField,
            Tooltip("Represents how much resources the game is using\nIf it's ~0 then awesome!\nIf it's between 0.0000001 and 1.9999999, fine ig, negligible\nIf it's more than 2, RUN, I AM COMING FOR YOU"),
            ReadOnly
        ]
        private static float _performanceBudget = 0f;
        public static float performanceBudget
        {
            // no one loves you

            // *the acts performed are purely for entertainment purposes and should not be re-inacted at home, between family members (that of, parents, siblings, and/or cousins), or partners of mileaging parties*
            // *to further reinstate this, the getter method is highly used between programmers, and this stunt is purely for shock value, rather than to be compared with realistic sources*
            get { return _performanceBudget; }
            private set
            {
                //UnityEngine.Debug.Log("Performance value: " + value);
                /*if (value > 100)
                {
                    // reason i exception check this is bcz i'm not sure if logging an error actually throws an exception or not, or, just, wtv it does
                    // but this also acts as an excuse to call Joar()
                    try
                    {
                        UnityEngine.Debug.LogWarning("Don't");
                        //throw new InvalidOperationException("I SAID DONT");
                    }
                    catch
                    {
                        // you will suffer
                        throw new Joar();
                    }
                }*/

                _performanceBudget = Mathf.Max(value, 0);

                if (_performanceBudget > 100)
                {
                    UnityEngine.Debug.LogWarning("Warning: performance budget (resources used by PC) VERY high");
                    //UnityEngine.Debug.Log("WHAT THE FUCK IS HAPPENING YOUR PC IS GOING TO EMPLODE");
                    return;
                }
            }
        }

        /// <summary>
        /// The type of state the game is in performance/LOD-wise
        /// </summary>
        [Serializable]
        public enum GameState : byte // can only include 256 definitions, way more than enough
        {
            // ignore how shitty these names are

            /// <summary>
            /// Regular gameplay
            /// </summary>
            Standard,

            /// <summary>
            /// Makes minor adjustments
            /// </summary>/
            Tuned,

            /// <summary>
            /// Makes more, major adjustments
            /// </summary>
            Restricted,

            /// <summary>
            /// Area where graphics gets shitty
            /// </summary>
            Limited,

            /// <summary>
            /// Area where gameplay gets shitty
            /// </summary>
            Aggressive,

            /// <summary>
            /// Just freeze everthing, moslty used for idle states
            /// </summary>
            Frozen,

            /// <summary>
            /// Drives the game to crash so your PC doesn't emplode with it
            /// </summary>
            Crash
        }
        private static GameState _gameState; // gotta use a backing field since this is C# 9 :/
        public static GameState gameState
        {
            get { return _gameState; }
            internal set
            {
                _gameState = value;

                switch (_gameState)
                {
                    case GameState.Standard: // this case is literally useless
                        break;
                    case GameState.Tuned:
                        break;
                    case GameState.Restricted:
                        break;
                    case GameState.Limited:
                        break;
                    case GameState.Aggressive:
                        break;
                    case GameState.Frozen:
                        break;
                    case GameState.Crash:
                        PerformanceOverloadException.Call(); // drive the game to crash with an exception ; exceptions don't usually cause a crash for some reason, so the exception just simply quits the program
                }
            }
        }


        // twuning
        [SerializeField, Tooltip("Self explanatory"), ReadOnly]
        private float targetFrameTimeMs = 16.6f; // people are fucking idiots and don't understand this is delta time

        [SerializeField, Tooltip("Resource check interval in seconds"), Range(1, 30)]
        private float _checkInterval = 5; // look at the damn tooltip for info
        private float checkInterval
        {
            // another victim
            get { return _checkInterval; }
            set { _checkInterval = Mathf.Max(value, 1E-2f); }
        }

        [SerializeField, Tooltip("Me neither.")]
        private uint consecutiveChecksToConfirm = 3;
        // guys, hear me out, but unsigned integers are so fucking underrated. Like first of all it uses no more memory, since it just replace all the negative values with more positive values, and also like there are SO MANY times where we don't want an integer to go negative, so unsigned solves that aswell, adding less boilerplate since you don't have to make a fucking property every single time

        // i love tuples
        // this is just a container of thresholds and states
        private static readonly (float ratioThreshold, GameState state)[] thresholds = new[]
        {
            (200f, GameState.Crash),
            (150f, GameState.Frozen),
            (100f, GameState.Aggressive),
            (60f, GameState.Limited),
            (30f, GameState.Restricted),
            (10f, GameState.Tuned),
            (0f, GameState.Standard)
        };

        private float emaFrameTimeMs;
        private float timer;
        private GameState pendingState;
        private int pendStreak;

        public static event Action<GameState, GameState> OnGameStateChanged;

        private static InputMap DebugControls;

        [field: SerializeField]
        public bool shouldEvaluate { get; private set; } = true;

        [field: SerializeField]
        public bool manualOverrideActive { get; private set; } = false;

        // create GO and attach after the game scene has finished loading
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void Init()
        {
            GameObject dgsh = new("Dynamic Game State Handler");
            DontDestroyOnLoad(dgsh);
            _ = dgsh.AddComponent<DynamicGameStateHandler>();
        }

        
        private void Start()
        {
            __Awake();

            emaFrameTimeMs = targetFrameTimeMs;
            pendingState = gameState;

            DebugControls = new();
            DebugControls.Enable();
            DebugControls.Dev.AssignGameState.performed += GameStateReassign;

            //shouldEvaluate = false; // change this
        }

        private void OnDestroy()
        {
            InputSystem.onAnyButtonPress.Call(currentAction => stopwatch.Restart());
            DebugControls.Dev.AssignGameState.performed -= GameStateReassign;
            DebugControls.Disable();

            CheckAnyButtons.Dispose();
        }

        #region Dev Utils

        private void GameStateReassign(InputAction.CallbackContext obj)
        {
            UnityEngine.Debug.Log(obj.ReadValue<float>());

            manualOverrideActive = true;
            switch (obj.ReadValue<float>())
            {
                case 0.1f:
                    SetGameState(GameState.Standard);
                    break;

                case 0.2f:
                    SetGameState(GameState.Tuned);
                    break;

                case 0.3f:
                    SetGameState(GameState.Restricted);
                    break;

                case 0.4f:
                    SetGameState(GameState.Limited);
                    break;

                case 0.5f:
                    SetGameState(GameState.Aggressive);
                    break;

                case 0.6f:
                    SetGameState(GameState.Frozen);
                    break;

                case 0.7f:
                    SetGameState(GameState.Crash);
                    break;

                default:
                    manualOverrideActive = false;
                    break;
            }
        }
        #endregion

        public override void Tick()
        {
            if (shouldEvaluate) _Update();

            float dtMs = Time.unscaledDeltaTime * 1000f;
            const float emaAlpha = 0.1f;
            emaFrameTimeMs = Mathf.Lerp(emaFrameTimeMs, dtMs, emaAlpha);

            timer += Time.unscaledDeltaTime;
            if (timer < checkInterval) return;
            timer = 0f;

            if (shouldEvaluate) Evaluate();
        }

        private void Evaluate()
        {
            try
            {
                float frameRatio = emaFrameTimeMs / targetFrameTimeMs;
                float memRatio = GetMemoryPressureRatio();
                float ratio = Mathf.Max(frameRatio, memRatio);
                performanceBudget = ratio;

                GameState candidate = GameState.Standard;
                foreach (var (threshold, state) in thresholds)
                {
                    if (ratio >= threshold)
                    {
                        candidate = state;
                        break;
                    }
                }

                if (candidate == pendingState)
                {
                    pendStreak++;
                }
                else
                {
                    pendingState = candidate;
                    pendStreak = 1;
                }

                if (!manualOverrideActive && pendStreak >= consecutiveChecksToConfirm && candidate != gameState)
                {
                    SetGameState(candidate);
                }
            }
            catch
            {
                // DIE
                throw new Joar();
            }
            finally
            {
                // MUHAHAHAHAHAHHAHHA
                // Astraa must be going fucking insane >:3

                #if DEV
                    Console.Beep();
                #endif
            }

            try
            {

            }
            catch (PerformanceOverloadException)
            {
                // just to give extra warning to the user that their PC is suffering
                Console.Beep();
            }
        }

        private void SetGameState(GameState newState)
        {
            if (newState == _gameState) return; // don't do anything if we're just re-assigning the same value

            GameState oldState = _gameState;
            _gameState = newState;
            OnGameStateChanged?.Invoke(oldState, newState);
        }

        private static float GetMemoryPressureRatio()
        {
            long usedBytes = GC.GetTotalMemory(false); // using this instead of UnityEngine.Profiler, since all profiler references are broken in release builds. Another reason why i hate unity
            long budgetBytes = (long)UnityEngine.SystemInfo.systemMemorySize * 1024L * 1024L / 4L; // 2026-07-19 @ 22:59 / 10:59 years old when i realized Int16 and Int64 have suffixes ;-;
            return (float)usedBytes / budgetBytes;
        }

        #endregion



        #region Idle Checking

        private IDisposable CheckAnyButtons;

        // this IDE is such bullshit
        // when i was half asleep coding the first iteration of this i fucking wrote "stopwatch.Elapsed.TotalMinutes(0.5)"
        // first of all, that math is wrong, my apolagies to everyone reading this
        // secondly, visual studio code didn't say shit
        // VSC, what the fuck do you not understand that TimeSpan.TotalSeconds is a property
        // a property
        // i'm surprised people use you for C# development other than ppl like me who can't afford a PC
        // being able to run your comically large as fuck .NET IDE,
        // sure you're lightweight
        // but your sugar daddy C# extensions are piles of shit that barely work
        // tell me why IntelliSense only activates once in a blue moon
        // "oH mY BaD, fOr iNtELlIsENsE tO WoRk wE NeEd tO FiNd yOuR sOlUTiOn/.csproj"
        // dumbass, first of all even when a solution and/or .csproj does exist
        // your lardass still doesn't summon IntelliSense
        // secondly what the hell is so important about those that requires an AI to help me write my own code (in fact what's nice is that I.S is not even AI)
        // heck even signing in with my MS acc (kill me) for "Visual Studio benefits" shit doesn't happen
        // go kill yourself
        // i kill my chromebook more and more everyday just to run this

        Stopwatch stopwatch = Stopwatch.StartNew();

        private static readonly (float minutes, GameState state)[] idleThresholds = new[]
        {
            (150f, GameState.Frozen),
            (100f, GameState.Aggressive),
            (60f, GameState.Limited),
            (40f, GameState.Restricted),
            (20f, GameState.Tuned),
            (0f, GameState.Standard)
        };

        private void _Update()
        {
            double minutesIdle = stopwatch.Elapsed.TotalMinutes;

            foreach (var (minutes, state) in idleThresholds)
            {
                if (minutesIdle >= minutes)
                {
                    // check if the value is not the same so that we don't unecessarily spam gameState reassigns
                    if (!manualOverrideActive && state != gameState)
                    {
                        SetGameState(state);
                    }
                    break;
                }
            }
        }

        #endregion




        #region Actual Handling Now

        public ResolutionScaleController RSC;

        private void __Awake()
        {
            //Idle check activation
            CheckAnyButtons = InputSystem.onAnyButtonPress.Call(currentAction => stopwatch.Restart());

            OnGameStateChanged += ((GameState oldState, GameState newState) =>
            {
                UnityEngine.Debug.Log($"Game State is being changed from {oldState} to {newState}");
            });

            RSC = FindFirstObjectByType<Camera>().gameObject.AddComponent<ResolutionScaleController>();
        }

        #endregion
    }
}
