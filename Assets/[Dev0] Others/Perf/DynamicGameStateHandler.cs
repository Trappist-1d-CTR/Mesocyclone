using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using UnityEngine;

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
    public sealed partial class DynamicGameStateHandler : MonoBehaviour
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
        static float _performanceBudget = 0f;
        public static float performanceBudget
        {
            // no one loves you

            // *the acts performed are purely for entertainment purposes and should not be re-inacted at home, between family members (that of, parents, siblings, and/or cousins), or partners of mileaging parties*
            // *to further reinstate this, the getter method is highly used between programmers, and this stunt is purely for shock value, rather than to be compared with realistic sources*
            get => _performanceBudget;
            private set
            {
                if (value > 5)
                {
                    // reason i exception check this is bcz i'm not sure if logging an error actually throws an exception or not, or, just, wtv it does
                    // but this also acts as an excuse to call Joar()
                    try
                    {
                        Debug.LogError("Don't");
                        throw new InvalidOperationException("I SAID DONT");
                    }
                    catch
                    {
                        // you will suffer
                        throw new Joar();
                    }
                }

                _performanceBudget = Mathf.Max(value, 0);

                if (_performanceBudget > 5)
                {
                    Debug.Log("WHAT THE FUCK IS HAPPENING YOUR PC IS GOING TO EMPLODE");
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
            Standard, // regular gameplay
            Tuned, // makes minor adjustments
            Restricted, // makes more major adjustments
            Limited, // Area where graphics gets shitty
            Aggressive, // Area where gameplay gets shitty
            Frozen, // Just freeze everything, mostly used for idle states
            Crash // drives the game to crash so your PC doesn't emplode with it
        }
        private static GameState _gameState;
        public static GameState gameState
        {
            get => _gameState;
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
                        throw new PerformanceOverloadException(); // drive the game to crash with an exception
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
            get => _checkInterval;
            set => _checkInterval = Mathf.Max(value, 1E-2f);
        }

        [SerializeField, Tooltip("Me neither.")]
        private uint consecutiveChecksToConfirm = 3;
        // guys, hear me out, but unsigned integers are so fucking underrated. Like first of all it uses no more memory, since it just replace all the negative values with more positive values, and also like there are SO MANY times where we don't want an integer to go negative, so unsigned solves that aswell, adding less boilerplate since you don't have to make a fucking property every single time


        // i love tuples
        // this is just a container of thresholds and states
        private static readonly (float ratioThreshold, GameState state)[] thresholds = new[]
        {
        (4f, GameState.Crash),
        (3.5f, GameState.Frozen),
        (3f, GameState.Aggressive),
        (2.5f, GameState.Limited),
        (1.95f, GameState.Restricted),
        (1.05f, GameState.Tuned),
        (0f, GameState.Standard)
    };


        private float emaFrameTimeMs;
        private float timer;
        private GameState pendingState;
        private int pendStreak;

        public static event Action<GameState, GameState> OnGameStateChanged;


        // create GO and attach after the game scene has finished loading
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void Init()
        {
            GameObject dgsh = new("Dynamic Game State Handler");
            DontDestroyOnLoad(dgsh);
            _ = dgsh.AddComponent<DynamicGameStateHandler>();
        }

        private void Awake()
        {
            __Awake();

            emaFrameTimeMs = targetFrameTimeMs;
            pendingState = gameState;
        }

        private void Update()
        {
            _Update();
            ImmediateGameStateReassignCheck();

            float dtMs = Time.unscaledDeltaTime * 1000f;
            const float emaAlpha = 0.1f;
            emaFrameTimeMs = Mathf.Lerp(emaFrameTimeMs, dtMs, emaAlpha);

            timer += Time.unscaledDeltaTime;
            if (timer < checkInterval) return;
            timer = 0f;

            Evaluate();
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

                if (pendStreak >= consecutiveChecksToConfirm && candidate != gameState)
                {
                    SetGameState(candidate);
                }
            }
            catch
            {
                // DIE
                throw new Joar();
            }
            catch (PerformanceOverloadException)
            {
                // just to give extra warning to the user that their PC is suffering
                Console.Beep();
            }
            finally
            {
                // MUHAHAHAHAHAHHAHHA
                // Astraa must be going fucking insane >:3

                #if DEV
                    Console.Beep();
                #endif
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
        (4.5f, GameState.Frozen),
        (3.2f, GameState.Aggressive),
        (2f, GameState.Limited),
        (1f, GameState.Restricted),
        (0.5f, GameState.Tuned),
        (0f, GameState.Standard)
    };

        private void _Update()
        {
            if (Input.anyKeyDown)
                stopwatch.Restart();

            double minutesIdle = stopwatch.Elapsed.TotalMinutes;

            foreach (var (minutes, state) in idleThresholds)
            {
                if (minutesIdle >= minutes)
                {
                    // check if the value is not the same so that we don't unecessarily spam gameState reassigns
                    if (state != gameState)
                    {
                        SetGameState(state);
                    }
                    break;
                }
            }
        }

        #endregion




        #region Actual Handling Now

        public RenderScaleController RSC;

        private void __Awake()
        {
            OnGameStateChanged += ((GameState oldState, GameState newState) =>
            {
                Debug.Log($"Game State is being changed from {oldState} to {newState}");
            });

            RSC = Camera.main.AddComponent<ResolutionScaleController>();
        }

        #endregion




        #region Dev Utils

        [Conditional("DEV")]
        private void ImmediateGameStateReassignCheck()
        {
            if (Input.GetKey(KeyCode.I))
                SetGameState(GameState.Standard);
            else if (Input.GetKey(KeyCode.J))
                SetGameState(GameState.Tuned);
            else if (Input.GetKey(KeyCode.K))
                SetGameState(GameState.Restricted);
            else if (Input.GetKey(KeyCode.L))
                SetGameState(GameState.Limited);
            else if (Input.GetKey(KeyCode.Semicolon))
                SetGameState(GameState.Aggressive);
            else if (Input.GetKey(KeyCode.Quote) || Input.GetKey(KeyCode.BackQuote))
                SetGameState(GameState.Frozen);
            else if (Input.GetKey(KeyCode.Backslash))
                SetGameState(GameState.Crash);
        }

        #endregion
    }
}