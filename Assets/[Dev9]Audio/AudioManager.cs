using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

#nullable enable

/// <summary>
/// Class that handles all the audio
/// </summary>
public sealed class AudioManager : MonoBehaviour
{
    #region Variables

    static AudioManager _instance;
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
    class PoolEntry
    {
        public GameObject GO;
        public AudioSource Source;
        public AudioLowPassFilter Filter;
    }

    List<PoolEntry> Pool = new();

    [SerializeField] byte _poolSize = 32;
    public byte PoolSize
    {
        get => _poolSize;
        set
        {
            _poolSize = (byte)Mathf.Clamp(value, 0, MaxPoolSize);
            if (_poolSize > 32)
            {
                Debug.LogWarning
                (
                    $"Audio Pool size ({_poolSize}) exceeds Unity's default voice limit of 32!!"
                );
            }
        }
    }

    public byte MaxPoolSize = 32;

    [SerializeField] List<Process> RepeatingClips = new();
    
    [SerializeField] Process? InterruptingRepeatingClip;

    [SerializeField] AudioListener Listener;
    [SerializeField] LayerMask OcclusionMask;

    #endregion


    #region Functions

    #region Init

    private AudioManager() {  }

    void Awake()
    {
        if (_instance != null && _instance != this)
        {
            Destroy(gameObject);
            return;
        }
        
        _instance = this;
        DontDestroyOnLoad(gameObject);
        Listener = FindObjectOfType<AudioListener>();
        OcclusionMask = LayerMask.GetMask("Terrain");
    }

    /// <summary>
    /// Creates a new child GO with it's own AudioSource and lowpass filter
    /// </summary>
    PoolEntry AddEntryToPool()
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

    PoolEntry? GetFreeEntry()
    {
        foreach (var Entry in Pool)
            if (!Entry.Source.isPlaying) return Entry;
        
        if (Pool.Count < PoolSize)
            return AddEntryToPool();
        
        #if UNITY_EDITOR

        bool Increase = UnityEditor.EditorUtility.DisplayDialog
        (
            "Audio Pool Full",
            $"The audio pool is full ({Pool.Count} / {PoolSize})!!\nIncrement pool size by 1 to fit space?",
            "Ye", "Nah"
        );

        if (Increase)
        {
            MaxPoolSize++;
            PoolSize++;
            return AddEntryToPool();
        }

        #endif

        Debug.LogWarning("Audio Pool is full!! No new sources will play until one is free");
        return null;
    }

    #endregion


    #region Play Functions

    public void Play(AudioClip Clip, // Audio Clip (MP3, WAV, OGG, etc.) to play
                    Vector3 Position = default, // Position in space
                    bool Is2D = true, // does the sound play globally? (makes 3D related arguments futile)
                    float Volume = 1f, // volume : 0 - 1
                    float MinDistance = 1f, // range in which the volume is max
                    float MaxDistance = 5f, // range in which you can no longer hear the source
                    AudioRolloffMode RolloffMode = AudioRolloffMode.Linear // how the sound rolls off from MinDistance to MaxDistance
                    )
    {
        PoolEntry Entry = GetFreeEntry();
        if (Entry == null) return;

        ConfigureEntry(Entry, Position, Is2D, Volume, MinDistance, MaxDistance, RolloffMode);
        Entry.Source.PlayOneShot(Clip);
    }

    public void Play(AudioClip Clip, // Audio Clip (MP3, WAV, OGG, etc.) to play
                    float MinPitch, // minimum pitch to choose from
                    float MaxPitch, // maximum pitch to choose from
                    Vector3 Position = default, // Position in space
                    bool Is2D = true, // does the sound play globally? (makes 3D related arguments futile)
                    float Volume = 1f, // volume : 0 - 1
                    float MinDistance = 1f, // range in which the volume is max
                    float MaxDistance = 5f, // range in which you can no longer hear the source
                    AudioRolloffMode RolloffMode = AudioRolloffMode.Linear // how the sound rolls off from MinDistance to MaxDistance
                    )
    {
        PoolEntry Entry = GetFreeEntry();
        if (Entry == null) return;

        ConfigureEntry(Entry, Position, Is2D, Volume, MinDistance, MaxDistance, RolloffMode);
        Entry.Source.pitch = UnityEngine.Random.Range(MinPitch, MaxPitch);
        Entry.Source.PlayOneShot(Clip);
    }

    void ConfigureEntry(PoolEntry Entry, Vector3 Position, bool Is2D, float Volume, float MinDistance, float MaxDistance, AudioRolloffMode RolloffMode)
    {
        Entry.GO.transform.position = Position;
        Entry.Source.spatialBlend = Is2D ? 0f : 1f;
        Entry.Source.minDistance = MinDistance;
        Entry.Source.maxDistance = MaxDistance;
        Entry.Source.rolloffMode = RolloffMode;
        Entry.Source.volume = Volume;
        Entry.Source.pitch = 1f;
    }

    #endregion


    #region Play Repeating Functions

    public Process PlayRepeating(AudioClip Clip, // Audio Clip (MP3, WAV, OGG, etc.) to play
                    Vector3 Position = default, // Position in space
                    bool Is2D = true, // does the sound play globally? (makes 3D related arguments futile)
                    float Volume = 1f, // volume : 0 - 1
                    float MinDistance = 1f, // range in which the volume is max
                    float MaxDistance = 5f, // range in which you can no longer hear the source
                    AudioRolloffMode RolloffMode = AudioRolloffMode.Linear // how the sound rolls off from MinDistance to MaxDistance
                    )
    {
        Process _Process = new(() => PlayRepeatingRoutine(Clip, Position, Is2D, Volume, MinDistance, MaxDistance, RolloffMode));
        RepeatingClips.Add(_Process); 
        _Process.Start();
        return _Process;
    }

    public Process PlayRepeating(AudioClip Clip, // Audio Clip (MP3, WAV, OGG, etc.) to play
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
        Process _Process = new(() => PlayRepeatingRoutine(Clip, MinPitch, MaxPitch, Position, Is2D, Volume, MinDistance, MaxDistance, RolloffMode));
        RepeatingClips.Add(_Process); 
        _Process.Start();
        return _Process;
    }

    #endregion


    #region Stop Repeating

    public void StopRepeating(Process process) // sry 4 the out of place naming, don't wanna encounter naming conflicts ;-;
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
            Entry.Source.Pause();
        
        foreach (var Clip in RepeatingClips)
            Clip.Stop();
    }

    public void UnPauseAll()
    {
        foreach (var Entry in Pool)
            Entry.Source.UnPause();

        foreach (var Clip in RepeatingClips)
            Clip.Start();
    }

    public void InterruptAllSources(AudioClip Clip, // Audio Clip (MP3, WAV, OGG, etc.) to play
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

    public void InterruptAllSources(AudioClip Clip, // Audio Clip (MP3, WAV, OGG, etc.) to play
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

    public void InterruptAllSourcesWithRepeating(AudioClip Clip, // Audio Clip (MP3, WAV, OGG, etc.) to play
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

    public void InterruptAllSourcesWithRepeating(AudioClip Clip, // Audio Clip (MP3, WAV, OGG, etc.) to play
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

    IEnumerator PlayRepeatingRoutine(AudioClip Clip, // Audio Clip (MP3, WAV, OGG, etc.) to play
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

    IEnumerator PlayRepeatingRoutine(AudioClip Clip, // Audio Clip (MP3, WAV, OGG, etc.) to play
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

    void Update()
    {
        if (Listener == null) return;

        foreach (var Entry in Pool)
        {
            if (!Entry.Source.isPlaying) continue;

            float Occlusion = GetOcclusion(Entry.Source);
            Entry.Filter.cutoffFrequency = Mathf.Lerp(22000f, 10f, Occlusion);
        }
    }

    float GetOcclusion(AudioSource Source)
    {
        Vector3 Direction = Listener.transform.position - Source.transform.position;
        float Distance = Direction.magnitude;

        return Physics.Raycast(Source.transform.position, Direction.normalized, Distance, OcclusionMask)
            ? 1f : 0f;
    }

    #endregion


    #region Misc

    public void SetPoolSize(byte NewSize) => PoolSize = NewSize;

    void OnDestroy() => Instance = null;

    #endregion

    #endregion
}

#nullable disable