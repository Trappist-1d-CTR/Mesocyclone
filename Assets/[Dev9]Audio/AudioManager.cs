using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Mesocyclone;
using Mesocyclone.Debug; // System.Diagnostics.Process exists...
using UnityEditor;

#nullable enable // i hate this

namespace Mesocyclone.Sound
{
    /// <summary>
    /// Class that handles all the audio
    /// </summary>
    public sealed class AudioManager : Tickable
    {
        #region Variables

        public static AudioManager? _instance;
        public static AudioManager Instance
        {
            get
            {
                if (_instance == null)
                {
                    GameObject AudioManagerGO = new("Audio Manager");
                    _instance = AudioManagerGO.AddComponent<AudioManager>();
                    DontDestroyOnLoad(AudioManagerGO);
                }
                return _instance;
            }

            private set => _instance = value;
        }

        [Serializable]
        private class PoolEntry
        {
            public GameObject? GO;
            public AudioSource? Source;
            public AudioLowPassFilter? Filter;
        }

        [Header("Pool Stuff")]
        [Tooltip("Collection (List<>) of all the nested AudioSource Game Objects")]
        [SerializeField]
        private List<PoolEntry> Pool = new();

        [Space(18)]
        [Tooltip("Controls the max amount of AudioSource Game Objects that can exist in the Pool")]
        [Range(0, 255)]
        [SerializeField] byte _poolSize = 32;
        private byte PoolSize
        {
            get => _poolSize;
            set
            {
                _poolSize = (byte)Mathf.Clamp(value, 0, MaxPoolSize);
                if (_poolSize > 32)
                {
                    UnityEngine.Debug.LogWarning
                    (
                        $"Audio Pool size ({_poolSize}) exceeds Unity's default voice limit of 32!!"
                    );
                }
            }
        }

        [Space(10)]
        [Tooltip("The limit PoolSize limit caps to")]
        [Range(0, 255)]
        public byte MaxPoolSize = 32;

        [Header("Process Stuff")]
        [Tooltip("Collectiom (List<>) of Processes repeating clips")]
        public List<Mesocyclone.Debug.Process> RepeatingClips = new();

        [Space(10)]
        [Tooltip("Process that's currently interrupting all other sounds")]
        [SerializeField]
        private Mesocyclone.Debug.Process? InterruptingRepeatingClip;

        [Header("Audio Listener & Raycasting")]
        [Tooltip("Reference to any objects with the Audio Listener component")]
        [SerializeField]
        private AudioListener Listener;

        [Tooltip("Current Layer the rays shoot out from and detect collision")]
        [SerializeField]
        private LayerMask OcclusionMask;

        [Tooltip("Controls how many walls the raycast has to go through before max muffling\nTweak this to whatever you feel is right")]
        [Range(0, 255)]
        [SerializeField]
        private byte _maxWalls = 32; // controls how many "walls" the raycast has the go through before max muffling // tweak this to your liking
        private int MaxWalls // no idea why this isn't a byte
        {
            get { return _maxWalls; }
            set { _maxWalls = (byte)Mathf.Clamp(value, 0, 255); }
        }

        #endregion


        #region Functions

        #region Init


        void Start()
        {
            if (_instance != null && _instance != this)
            {
                Destroy(gameObject);
                return;
            }

            _instance = this;
            DontDestroyOnLoad(gameObject);
            Listener = FindFirstObjectByType<AudioListener>();
            OcclusionMask = LayerMask.GetMask("Terrain");
        }

        /// <summary>
        /// Creates a new child GO with it's own AudioSource and lowpass filter
        /// </summary>
        private PoolEntry AddEntryToPool()
        {
            GameObject _Child = new($"Audio Source {Pool.Count}");
            _Child.transform.SetParent(transform);

            AudioSource _Source = _Child.AddComponent<AudioSource>();
            AudioLowPassFilter _Filter = _Child.AddComponent<AudioLowPassFilter>();

            PoolEntry Entry = new()
            {
                GO = _Child,
                Source = _Source,
                Filter = _Filter
            };
            Pool.Add(Entry);
            return Entry;
        }

        #endregion


        #region Pool Functions

        private PoolEntry? GetFreeEntry()
        {
            foreach (var Entry in Pool)
                if (Entry.Source != null && !Entry.Source.isPlaying) return Entry;

            if (Pool.Count < PoolSize)
                return AddEntryToPool();

            #if UNITY_EDITOR

                bool Increase = UnityEditor.EditorUtility.DisplayDialog
                (
                    "Audio Pool Full",
                    $"The audio pool is full ({Pool.Count} / {PoolSize})!!\nIncrement pool size by 1 to fit space?",
                    "Sure", "Nah"
                );

                if (Increase)
                {
                    MaxPoolSize++;
                    PoolSize++;
                    return AddEntryToPool();
                }
                else
                {
                    bool AllGood = UnityEditor.EditorUtility.DisplayDialog
                    (
                        "Pool Size Kept",
                        $"You chose not to increment pool size, is everything good?",
                        "Affirmative", "STOP EVERYTHING FOR ENTROPY'S SAKE-"
                    );

                    if (!AllGood)
                    {
                        UnityEngine.Debug.Log("Oke :3");
                        DestroyImmediate(Instance.gameObject);
                        throw new Joar();
                    }
                }

            #endif

            UnityEngine.Debug.LogWarning("Audio Pool is full!! No new sources will play until one is free");
            return null;
        }

        #endregion


        #region Play Functions

        public void Play
        (
            AudioClip Clip, // Audio Clip (MP3, WAV, OGG, etc.) to play
            Vector3 Position = default, // Position in space
            bool Is2D = true, // does the sound play globally? (makes 3D related arguments futile)
            float Volume = 1f, // volume : 0 - 1
            float MinDistance = 15f, // range in which the volume is max
            float MaxDistance = 100f, // range in which you can no longer hear the source
            AudioRolloffMode RolloffMode = AudioRolloffMode.Linear // how the sound rolls off from MinDistance to MaxDistance
        )
        {
            PoolEntry? Entry = GetFreeEntry();
            if (Entry == null) return;

            ConfigureEntry(Entry, Position, Is2D, Volume, MinDistance, MaxDistance, RolloffMode);
            if (Entry.Source != null) Entry.Source.PlayOneShot(Clip);
        }

        public void Play
        (
            AudioClip Clip, // Audio Clip (MP3, WAV, OGG, etc.) to play
            float MinPitch, // minimum pitch to choose from
            float MaxPitch, // maximum pitch to choose from
            Vector3 Position = default, // Position in space
            bool Is2D = true, // does the sound play globally? (makes 3D related arguments futile)
            float Volume = 1f, // volume : 0 - 1
            float MinDistance = 15f, // range in which the volume is max
            float MaxDistance = 100f, // range in which you can no longer hear the source
            AudioRolloffMode RolloffMode = AudioRolloffMode.Linear // how the sound rolls off from MinDistance to MaxDistance
        )
        {
            PoolEntry? Entry = GetFreeEntry();
            if (Entry == null) return;

            ConfigureEntry(Entry, Position, Is2D, Volume, MinDistance, MaxDistance, RolloffMode);
            if (Entry.Source != null) Entry.Source.pitch = UnityEngine.Random.Range(MinPitch, MaxPitch);
            if (Entry.Source != null) Entry.Source.PlayOneShot(Clip);
        }

        private void ConfigureEntry
        (
            PoolEntry Entry,
            Vector3 Position,
            bool Is2D,
            float Volume,
            float MinDistance,
            float MaxDistance,
            AudioRolloffMode RolloffMode
        )
        {
            if (Entry.GO != null) Entry.GO.transform.position = Position;
            else throw new Joar();
            if (Entry.Source != null)
            {
                Entry.Source.spatialBlend = Is2D ? 0f : 1f;
                Entry.Source.minDistance = MinDistance;
                Entry.Source.maxDistance = MaxDistance;
                Entry.Source.rolloffMode = RolloffMode;
                Entry.Source.volume = Volume;
                Entry.Source.pitch = 1f;
            }
            else throw new Joar();
        }

        #endregion


        #region Play Repeating Functions

        public Mesocyclone.Debug.Process PlayRepeating
        (
            AudioClip Clip, // Audio Clip (MP3, WAV, OGG, etc.) to play
            Vector3 Position = default, // Position in space
            bool Is2D = true, // does the sound play globally? (makes 3D related arguments futile)
            float Volume = 1f, // volume : 0 - 1
            float MinDistance = 1f, // range in which the volume is max
            float MaxDistance = 5f, // range in which you can no longer hear the source
            AudioRolloffMode RolloffMode = AudioRolloffMode.Linear // how the sound rolls off from MinDistance to MaxDistance
        )
        {
            Mesocyclone.Debug.Process _Process = new(() => PlayRepeatingRoutine(Clip, Position, Is2D, Volume, MinDistance, MaxDistance, RolloffMode));
            RepeatingClips.Add(_Process);
            _Process.Start();
            return _Process;
        }

        public Mesocyclone.Debug.Process PlayRepeating
        (
            AudioClip Clip, // Audio Clip (MP3, WAV, OGG, etc.) to play
            float MinPitch,
            float MaxPitch,
            Vector3 Position = default, // Position in space
            bool Is2D = true, // does the sound play globally? (makes 3D related arguments futile)
            float Volume = 1f, // volume : 0 - 1
            float MinDistance = 1f, // range in which the volume is max
            float MaxDistance = 5f, // range in which you can no longer hear the source
            AudioRolloffMode RolloffMode = AudioRolloffMode.Linear // how the sound rolls off from MinDistance to MaxDistance
        )
        {
            Mesocyclone.Debug.Process _Process = new(() => PlayRepeatingRoutine(Clip, MinPitch, MaxPitch, Position, Is2D, Volume, MinDistance, MaxDistance, RolloffMode));
            RepeatingClips.Add(_Process);
            _Process.Start();
            return _Process;
        }

        #endregion


        #region Stop Repeating

        public void StopRepeating(Mesocyclone.Debug.Process process) // sry 4 the out of place naming, don't wanna encounter naming conflicts ;-;
        {
            process.Stop();
            RepeatingClips.Remove(process);
        }

        public void StopAllRepeating()
        {
            foreach (var Clip in RepeatingClips)
                Clip.Stop();
            RepeatingClips.Clear();
        }

        #endregion


        #region Pause & Interrupt

        public void PauseAll()
        {
            foreach (var Entry in Pool)
                if (Entry.Source != null) Entry.Source.Pause();

            foreach (var Clip in RepeatingClips)
                Clip.Stop();
        }

        public void UnPauseAll()
        {
            foreach (var Entry in Pool)
                if (Entry.Source != null) Entry.Source.UnPause();

            foreach (var Clip in RepeatingClips)
                Clip.Start();
        }

        public void InterruptAllSources
        (
            AudioClip Clip, // Audio Clip (MP3, WAV, OGG, etc.) to play
            Vector3 Position = default, // Position in space
            bool Is2D = true, // does the sound play globally? (makes 3D related arguments futile)
            float Volume = 1f, // volume : 0 - 1
            float MinDistance = 1f, // range in which the volume is max
            float MaxDistance = 5f, // range in which you can no longer hear the source
            AudioRolloffMode RolloffMode = AudioRolloffMode.Linear // how the sound rolls off from MinDistance to MaxDistance
        )
        {
            PauseAll();
            Play(Clip, Position, Is2D, Volume, MinDistance, MaxDistance, RolloffMode);
            Invoke(nameof(UnPauseAll), Clip.length);
        }

        public void InterruptAllSources
        (
            AudioClip Clip, // Audio Clip (MP3, WAV, OGG, etc.) to play
            float MinPitch,
            float MaxPitch,
            Vector3 Position = default, // Position in space
            bool Is2D = true, // does the sound play globally? (makes 3D related arguments futile)
            float Volume = 1f, // volume : 0 - 1
            float MinDistance = 1f, // range in which the volume is max
            float MaxDistance = 5f, // range in which you can no longer hear the source
            AudioRolloffMode RolloffMode = AudioRolloffMode.Linear // how the sound rolls off from MinDistance to MaxDistance
        )
        {
            PauseAll();
            Play(Clip, MinPitch, MaxPitch, Position, Is2D, Volume, MinDistance, MaxDistance, RolloffMode);
            Invoke(nameof(UnPauseAll), Clip.length);
        }

        public void InterruptAllSourcesWithRepeating
        (
            AudioClip Clip, // Audio Clip (MP3, WAV, OGG, etc.) to play
            Vector3 Position = default, // Position in space
            bool Is2D = true, // does the sound play globally? (makes 3D related arguments futile)
            float Volume = 1f, // volume : 0 - 1
            float MinDistance = 1f, // range in which the volume is max
            float MaxDistance = 5f, // range in which you can no longer hear the source
            AudioRolloffMode RolloffMode = AudioRolloffMode.Linear // how the sound rolls off from MinDistance to MaxDistance
        )
        {
            PauseAll();
            InterruptingRepeatingClip = PlayRepeating(Clip, Position, Is2D, Volume, MinDistance, MaxDistance, RolloffMode);
        }

        public void InterruptAllSourcesWithRepeating
        (   
            AudioClip Clip, // Audio Clip (MP3, WAV, OGG, etc.) to play
            float MinPitch,
            float MaxPitch,
            Vector3 Position = default, // Position in space
            bool Is2D = true, // does the sound play globally? (makes 3D related arguments futile)
            float Volume = 1f, // volume : 0 - 1
            float MinDistance = 1f, // range in which the volume is max
            float MaxDistance = 5f, // range in which you can no longer hear the source
            AudioRolloffMode RolloffMode = AudioRolloffMode.Linear // how the sound rolls off from MinDistance to MaxDistance
        )
        {
            PauseAll();
            InterruptingRepeatingClip = PlayRepeating(Clip, MinPitch, MaxPitch, Position, Is2D, Volume, MinDistance, MaxDistance, RolloffMode);
        }

        public void StopRepeatingInterrupt()
        {
            InterruptingRepeatingClip?.Stop();
            InterruptingRepeatingClip = null;
            UnPauseAll();
        }

        #endregion


        #region Repeating Routines

        private IEnumerator PlayRepeatingRoutine
        (
            AudioClip Clip, // Audio Clip (MP3, WAV, OGG, etc.) to play
            Vector3 Position = default, // Position in space
            bool Is2D = true, // does the sound play globally? (makes 3D related arguments futile)
            float Volume = 1f, // volume : 0 - 1
            float MinDistance = 1f, // range in which the volume is max
            float MaxDistance = 5f, // range in which you can no longer hear the source
            AudioRolloffMode RolloffMode = AudioRolloffMode.Linear // how the sound rolls off from MinDistance to MaxDistance
        )
        {
            while (true)
            {
                Play(Clip, Position, Is2D, Volume, MinDistance, MaxDistance, RolloffMode);
                yield return new WaitForSeconds(Clip.length);
            }
        }

        private IEnumerator PlayRepeatingRoutine
        (
            AudioClip Clip, // Audio Clip (MP3, WAV, OGG, etc.) to play
            float MinPitch,
            float MaxPitch,
            Vector3 Position = default, // Position in space
            bool Is2D = true, // does the sound play globally? (makes 3D related arguments futile)
            float Volume = 1f, // volume : 0 - 1
            float MinDistance = 1f, // range in which the volume is max
            float MaxDistance = 5f, // range in which you can no longer hear the source
            AudioRolloffMode RolloffMode = AudioRolloffMode.Linear // how the sound rolls off from MinDistance to MaxDistance
        )
        {
            while (true)
            {
                Play(Clip, MinPitch, MaxPitch, Position, Is2D, Volume, MinDistance, MaxDistance, RolloffMode);
                yield return new WaitForSeconds(Clip.length);
            }
        }

        #endregion


        #region Update & Muffling

        public override void Tick()
        {
            if (Listener == null) return;

            foreach (var Entry in Pool)
            {
                if (Entry.Source != null && Entry.Filter != null)
                {
                    if (!Entry.Source.isPlaying) continue;

                    float Occlusion = GetOcclusion(Entry.Source);
                    Entry.Filter.cutoffFrequency = Mathf.Lerp(22000f, 10f, Occlusion);
                }
            }
        }

        private float GetOcclusion(AudioSource Source)
        {
            Vector3 ListenerPosition = Listener.transform.position;
            Vector3 RayOrigin = Source.transform.position;
            Vector3 Direction = (ListenerPosition - RayOrigin).normalized;
            float Remaining = Vector3.Distance(RayOrigin, ListenerPosition);

            float StepPerWall = 1f / MaxWalls;
            float Occlusion = 0f;

            for (int i = 0; i < MaxWalls; i++)
            {
                if (!Physics.Raycast(RayOrigin, Direction, out RaycastHit Hit, Remaining, OcclusionMask))
                    break;

                Occlusion += StepPerWall;
                Remaining -= Hit.distance + 1E-2f; // aka 0.01
                RayOrigin = Hit.point + Direction * 1E-2f; // aka 0.01

                if (Remaining <= 0f) break;
            }

            return Mathf.Clamp01(Occlusion); // clamp the value between 0 - 1 since this will be returned to be used for t in the Lerp function
        }

        #endregion


        #region Misc

        public void SetPoolSize(byte NewSize) => PoolSize = NewSize;

        void OnDestroy() => Instance = null!;

        #endregion

        #endregion
    }
}

#nullable disable