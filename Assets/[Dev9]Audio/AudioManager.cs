using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Singleton that handles all the Audio
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
                GameObject audioManager = new("Audio Manager");
                _instance = audioManager.AddComponent<AudioManager>();
                DontDestroyOnLoad(audioManager);
            }
            return _instance;
        }

        private set => _instance = value;
    }

    List<AudioSource> pool = new();

    [SerializeField] byte _poolSize = 32;
    public byte poolSize
    {
        get => _poolSize;
        set
        {
            _poolSize = Mathf.Clamp(value, 0, maxPoolSize);
            if (_poolSize > 32)
            {
                Debug.LogWarning
                (
                    $"Audio Pool size ({_poolSize}) exceeds Unity's default voice limit of 32!!"
                );
            }
        }
    }

    public byte maxPoolSize = 32;

    List<Process> repeatingProcesses = new();

    #nullable enable // in case this isn't disabled
    Process? interruptProcess;
    #nullable disable

    AudioListener listener;

    Dictionary<AudioSource, AudioLowPassFilter> filters = new();

    LayerMask occlusionMask;

    #endregion


    #region Functions

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

        for (int i = 0; i < poolSize; i++)
        {
            AudioSource source = gameObject.AddComponent<AudioSource>();
            AudioLowPassFilter filter = source.gameObject.AddComponent<AudioLowPassFilter>();
            filters[source] = filter;
            pool.Add(source);
        }

        listener = FindObjectOfType<AudioListener>();

        occlusionMask = LayerMask.GetMask("Terrain");
    }

    AudioSource GetFreeSource()
    {
        foreach (var source in pool)
        {
            if (!source.isPlaying) return source;
        }

        if (pool.Count < poolSize)
        {
            AudioSource source = gameObject.AddComponent<AudioSource>();
            AudioLowPassFilter filter = source.gameObject.AddComponent<AudioLowPassFilter>();
            filters[source] = filter;
            pool.Add(source);
            return source;
        }

        #if UNITY_EDITOR

        bool increase = UnityEditor.EditorUtility.DisplayDialog
        (
            "Audio Manager: Audio Pool Full",
            $"The audio pool is full ({pool.Count} / {poolSize}) !!!\nIncrease pool size by 1 to fit audio source?",
            "Ye", "Nah"
        );

        if (increase)
        {
            poolSize++;
            AudioSource source = gameObject.AddComponent<AudioSource>();
            AudioLowPassFilter filter = source.gameObject.AddComponent<AudioLowPassFilter>();
            filters[source] = filter;
            pool.Add(source);
            return source;
        }

        #endif

        Debug.LogWarning
        (
            $"Audio Pool size is full!!\nWon't instantiate any new audio sources until there's space left"
        );
        return null;
    }

    #region Play Functions

    public void Play(AudioClip audioClip, Vector3 position = Vector3.zero, bool is2D = true, float volume = 1f, float minDistance = 1f, float maxDistance = 5f, AudioRolloffMode rolloffMode = AudioRolloffMode.Linear, bool reversed = false)
    {
        #nullable enable // in case this isn't disabled
        AudioSource? source = GetFreeSource();
        #nullable disable
        if (source == null) return;

        float spatialBlend = is2D ? 0f : 1f;

        AudioClip clip = audioClip;
        if (reversed) clip = Reverse(clip);

        source.transform.position = position;
        source.spatialBlend = spatialBlend;
        source.minDistance = minDistance;
        source.maxDistance = maxDistance;
        source.rolloffMode = rolloffMode;
        source.volume = volume;

        source.PlayOneShot(clip);
    }

    public void Play(AudioClip audioClip, float minPitch, float maxPitch, Vector3 position = Vector3.zero, bool is2D = true, float volume = 1f, float minDistance = 1f, float maxDistance = 5f, AudioRolloffMode rolloffMode = AudioRolloffMode.Linear, bool reversed = false)
    {
        #nullable enable // in case this isn't disabled
        AudioSource? source = GetFreeSource();
        #nullable disable
        if (source == null) return;

        float spatialBlend = is2D ? 0f : 1f;

        AudioClip clip = audioClip;
        if (reversed) clip = Reverse(clip);

        source.transform.position = position;
        source.spatialBlend = spatialBlend;
        source.minDistance = minDistance;
        source.maxDistance = maxDistance;
        source.rolloffMode = rolloffMode;
        source.volume = volume;
        source.pitch = Random.Range(minPitch, maxPitch);

        source.PlayOneShot(clip);
    }

    #endregion

    #region Play Repeating Functions

    public Process PlayRepeating(AudioClip audioClip, Vector3 position = Vector3.zero, bool is2D = true, float volume = 1f, float minDistance = 1f, float maxDistance = 5f, AudioRolloffMode rolloffMode = AudioRolloffMode.Linear, bool reversed = false)
    {
        Process process = new(() => PlayRepeatingRoutine(audioClip, position, is2D, volume, minDistance, maxDistance, rolloffMode, reversed));
        repeatingProcesses.Add(process);
        process.Start();
        return process;
    }

    public Process PlayRepeating(AudioClip audioClip, float minPitch, float maxPitch, Vector3 position = Vector3.zero, bool is2D = true, float volume = 1f, float minDistance = 1f, float maxDistance = 5f, AudioRolloffMode rolloffMode = AudioRolloffMode.Linear, bool reversed = false)
    {
        Process process = new(() => PlayRepeatingRoutine(audioClip, minPitch, maxPitch, position, is2D, volume, minDistance, maxDistance, rolloffMode, reversed));
        repeatingProcesses.Add(process);
        process.Start();
        return process;
    }

    #endregion

    #region Stop Repeating Function

    public void StopRepeating(Process process)
    {
        process.Stop();
        repeatingProcesses.Remove(process);
    }

    public void StopAllRepeating()
    {
        foreach (var process in repeatingProcesses)
            process.Stop();
        
        repeatingProcesses.Clear();
    }

    #endregion


    #region Pause Functions

    public void PauseAll()
    {
        foreach (var source in pool)
            source.Pause();

        foreach (var process in repeatingProcesses)
            process.Stop();
    }

    public void UnPauseAll()
    {
        foreach (var source in pool)
            source.UnPause();

        foreach (var process in repeatingProcesses)
            process.Start();
    }

    public void InterruptAllSources(AudioClip audioClip, Vector3 position = Vector3.zero, bool is2D = true, float volume = 1f, float minDistance = 1f, float maxDistance = 5f, AudioRolloffMode rolloffMode = AudioRolloffMode.Linear, bool reversed = false)
    {
        PauseAll();
        Play(audioClip, position, is2D, volume, minDistance, maxDistance, rolloffMode, reversed);
    }

    public void InterruptAllSources(AudioClip audioClip, float minPitch, float maxPitch, Vector3 position = Vector3.zero, bool is2D = true, float volume = 1f, float minDistance = 1f, float maxDistance = 5f, AudioRolloffMode rolloffMode = AudioRolloffMode.Linear, bool reversed = false)
    {
        PauseAll();
        Play(audioClip, minPitch, maxPitch, position, is2D, volume, minDistance, maxDistance, rolloffMode, reversed);
        Invoke("UnPauseAll", audioClip.length);
    }

    public void InterruptAllSourcesWithRepeating(AudioClip audioClip, Vector3 position = Vector3.zero, bool is2D = true, float volume = 1f, float minDistance = 1f, float maxDistance = 5f, AudioRolloffMode rolloffMode = AudioRolloffMode.Linear, bool reversed = false)
    {
        PauseAll();
        interruptProcess = PlayRepeating(audioClip, position, is2D, volume, minDistance, maxDistance, rolloffMode, reversed);
    }

    public void InterruptAllSourcesWithRepeating(AudioClip audioClip, float minPitch, float maxPitch, Vector3 position = Vector3.zero, bool is2D = true, float volume = 1f, float minDistance = 1f, float maxDistance = 5f, AudioRolloffMode rolloffMode = AudioRolloffMode.Linear, bool reversed = false)
    {
        PauseAll();
        interruptProcess = PlayRepeating(audioClip, minPitch, maxPitch, position, is2D, volume, minDistance, maxDistance, rolloffMode, reversed);
    }

    public void StopRepeatedInterrupt()
    {
        interruptProcess?.Stop();
        interruptProcess = null;
        UnPauseAll();
    }

    #endregion


    #region Play Repeating Routines

    public IEnumerator PlayRepeatingRoutine(AudioClip audioClip, Vector3 position = Vector3.zero, bool is2D = true, float volume = 1f, float minDistance = 1f, float maxDistance = 5f, AudioRolloffMode rolloffMode = AudioRolloffMode.Linear, bool reversed = false)
    {
        while (true)
        {
            Play(audioClip, position, is2D, volume, minDistance, maxDistance, rolloffMode, reversed);

            yield return new WaitForSeconds(audioClip.length);
        }
    }

    public IEnumerator PlayRepeatingRoutine(AudioClip audioClip, float minPitch, float maxPitch, Vector3 position = Vector3.zero, bool is2D = true, float volume = 1f, float minDistance = 1f, float maxDistance = 5f, AudioRolloffMode rolloffMode = AudioRolloffMode.Linear, bool reversed = false)
    {
        while (true)
        {
            Play(audioClip, minPitch, maxPitch, position, is2D, volume, minDistance, maxDistance, rolloffMode, reversed);

            yield return new WaitForSeconds(audioClip.length);
        }
    }

    #endregion

    public void SetPoolSize(byte newSize) => poolSize = newSize;

    void OnDestroy() => Instance = null;

    AudioClip Reverse(AudioClip audioClip)
    {
        float[] samples = new float[audioClip.samples * audioClip.channels];
        audioClip.GetData(samples, 0);

        System.Array.Reverse(samples);

        AudioClip newClip = AudioClip.Create
        (
            $"{audioClip.name}_reversed",
            audioClip.samples,
            audioClip.channels,
            audioClip.frequency,
            false
        );

        newClip.SetData(samples, 0);
        return newClip;
    }

    #region Update

    void Update()
    {
        foreach (var source in pool)
        {
            if (!source.isPlaying) continue;

            AudioLowPassFilter filter = filters[source];
            float occlusion = GetOcclusion(source);
            filter.cutoffFrequency = Mathf.Lerp(22000f, 10f, occlusion);
        }
    }

    #endregion

    #region Raycasting

    float GetOcclusion(AudioSource source)
    {
        Vector3 direction = listener.transform.position - source.transform.position;
        float distance = direction.magnitude;

        if (Physics.Raycast(source.transform.position, direction.normalized, distance, occlusionMask))
            return 1f;
        
        return 0f;
    }

    #endregion

    #endregion
}