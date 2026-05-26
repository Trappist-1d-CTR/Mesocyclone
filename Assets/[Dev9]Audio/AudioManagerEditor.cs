using System;
using System.Collections.Generic;
using UnityEngine;

#if UNITY_EDITOR

using UnityEditor;

#nullable enable // in case this isn't disabled

#region Editor Utility

[CustomEditor(typeof(AudioManager))]
public class AudioManagerEditor : Editor
{
    public override void OnInspectorGUI()
    {
        base.DrawDefaultInspector();

        AudioManager audioManager = (AudioManager)base.target;

        if (GUILayout.Button("Play"))
            AudioManagerPlayWindow.Open(false);

        if (GUILayout.Button("Play Repeating"))
            AudioManagerPlayWindow.Open(true);

        if (GUILayout.Button("Stop Repeating"))
            AudioManagerStopRepeatingWindow.Open();

        if (GUILayout.Button("Stop All Repeating"))
            audioManager.StopAllRepeating();

        if (GUILayout.Button("Pause All"))
            audioManager.PauseAll();

        if (GUILayout.Button("UnPause All"))
            audioManager.UnPauseAll();

        if (GUILayout.Button("Interrupt"))
            AudioManagerInterruptWindow.Open(false);
        
        if (GUILayout.Button("Repeating Interrupt"))
            AudioManagerInterruptWindow.Open(true);

        if (GUILayout.Button("Stop Repeating Interrupt"))
            AudioManager.Instance.StopRepeatingInterrupt();

        if (GUILayout.Button("Set Pool Size"))
            AudioManagerSetPoolSizeWindow.Open();
    }
}

#endregion


#region Play Options Window

public class AudioManagerPlayWindow : EditorWindow
{
    #region Variables

    bool isRepeating;

    AudioClip clip = null!; // `null!` is a keyword that's equivalent to `null` but doesn't make the compiler complain
    float minPitch = 0.9f;
    float maxPitch = 1.1f;
    Vector3 position = default; // Vector3.zero
    bool is2D = true;
    float volume = 1f;
    float minDistance = 15f;
    float maxDistance = 100f;
    AudioRolloffMode rolloffMode = AudioRolloffMode.Linear;

    bool pitchBypass = false;
    bool amplify = false;

    #endregion


    #region Funcions

    public static void Open(bool isRepeating)
    {
        var window = GetWindow<AudioManagerPlayWindow>();
        window.isRepeating = isRepeating;
        window.titleContent = new GUIContent
        (
            isRepeating ? "Audio Manager : PlayRepeating()" : "Audio Manager : Play()",
            EditorGUIUtility.IconContent("AudioSource Icon").image
        );
    }

    void OnGUI()
    {
        clip = (AudioClip)EditorGUILayout.ObjectField("Audio Clip", clip, typeof(AudioClip), false);

        GUILayout.Space(10);

        pitchBypass = EditorGUILayout.Toggle("Pitch Bypass", pitchBypass);

        GUILayout.Space(5);

        if (pitchBypass)
        {
            minPitch = EditorGUILayout.FloatField("Min Pitch", minPitch);
            maxPitch = EditorGUILayout.FloatField("Max Pitch", maxPitch);

            if (minPitch < 0.1 || maxPitch < 0.1)
                EditorGUILayout.HelpBox("Don't you dare.", MessageType.Error);
        }
        else
        {
            minPitch = EditorGUILayout.Slider("Min Pitch", minPitch, 0.1f, 1f);
            maxPitch = EditorGUILayout.Slider("Max Pitch", maxPitch, 1f, 2f);
        }

        GUILayout.Space(10);

        position = EditorGUILayout.Vector3Field("Position", position);

        GUILayout.Space(10);

        is2D = EditorGUILayout.Toggle("2D", is2D);

        GUILayout.Space(10);

        amplify = EditorGUILayout.Toggle("Amplify", amplify);

        GUILayout.Space(5);

        if (amplify)
            volume = EditorGUILayout.FloatField("Volume", volume);
        else volume = EditorGUILayout.Slider("Volume", volume, 0f, 1f);

        if (volume > 1f)
            EditorGUILayout.HelpBox("Volume above 1 can cause distortion. Use with caution.", MessageType.Warning);
        
        GUILayout.Space(10);

        minDistance = EditorGUILayout.FloatField("Min Distance (m)", minDistance);
        maxDistance = EditorGUILayout.FloatField("Max Distance (m)", maxDistance);

        GUILayout.Space(10);

        rolloffMode = (AudioRolloffMode)EditorGUILayout.EnumPopup("Rolloff Mode", rolloffMode);

        // ---

        GUILayout.FlexibleSpace(); // push down

        EditorGUILayout.BeginHorizontal();
        GUILayout.FlexibleSpace(); // push right

        EditorGUILayout.BeginDisabledGroup(clip == null);

        if (GUILayout.Button("Play"))
        {
            if (!isRepeating)
            {
                AudioManager.Instance.Play
                (
                    clip, minPitch, maxPitch, position, is2D, volume, minDistance, maxDistance, rolloffMode
                );
            }

            else
            {
                Process process = AudioManager.Instance.PlayRepeating
                (
                    clip, minPitch, maxPitch, position, is2D, volume, minDistance, maxDistance, rolloffMode
                );
            }
        }

        EditorGUILayout.EndDisabledGroup();
        EditorGUILayout.EndHorizontal();
    }

    #endregion
}

#region Stop Repeating Options Window

public class AudioManagerStopRepeatingWindow : EditorWindow
{
    public static void Open()
    {
        var window = GetWindow<AudioManagerStopRepeatingWindow>();
        window.titleContent = new GUIContent
        (
            "Audio Manager : StopRepeating()",
            EditorGUIUtility.IconContent("AudioSource Icon").image
        );
    }
    
    void OnGUI()
    {
        foreach (var process in AudioManager.Instance.RepeatingClips)
        {
            if (GUILayout.Button("Stop"))
                AudioManager.Instance.StopRepeating(process);
        }
    }
}

#endregion

#region Interrupt Options Window

public class AudioManagerInterruptWindow : EditorWindow
{
    #region Variables

    bool isRepeating;

    AudioClip clip = null!; // `null!` is a keyword that's equivalent to `null` but doesn't make the compiler complain
    float minPitch = 0.9f;
    float maxPitch = 1.1f;
    Vector3 position = default; // Vector3.zero
    bool is2D = true;
    float volume = 1f;
    float minDistance = 15f;
    float maxDistance = 100f;
    AudioRolloffMode rolloffMode = AudioRolloffMode.Linear;

    bool pitchBypass = false;
    bool amplify = false;

    #endregion

    #region Functions

    public static void Open(bool isRepeating)
    {
        var window = GetWindow<AudioManagerInterruptWindow>();
        window.isRepeating = isRepeating;
        window.titleContent = new GUIContent
        (
            isRepeating ? "Audio Manager : InterruptAllSourcesWithRepeating()" : "Audio Manager : InterruptAllSources()",
            EditorGUIUtility.IconContent("AudioSource Icon").image
        );
    }

    void OnGUI()
    {
        clip = (AudioClip)EditorGUILayout.ObjectField("Audio Clip", clip, typeof(AudioClip), false);

        GUILayout.Space(10);

        pitchBypass = EditorGUILayout.Toggle("Pitch Bypass", pitchBypass);

        GUILayout.Space(5);

        if (pitchBypass)
        {
            minPitch = EditorGUILayout.FloatField("Min Pitch", minPitch);
            maxPitch = EditorGUILayout.FloatField("Max Pitch", maxPitch);

            if (minPitch < 0.1 || maxPitch < 0.1)
                EditorGUILayout.HelpBox("Don't you dare.", MessageType.Error);
        }
        else
        {
            minPitch = EditorGUILayout.Slider("Min Pitch", minPitch, 0.1f, 1f);
            maxPitch = EditorGUILayout.Slider("Max Pitch", maxPitch, 1f, 2f);
        }

        GUILayout.Space(10);

        position = EditorGUILayout.Vector3Field("Position", position);

        GUILayout.Space(10);

        is2D = EditorGUILayout.Toggle("2D", is2D);

        GUILayout.Space(10);

        amplify = EditorGUILayout.Toggle("Amplify", amplify);

        GUILayout.Space(5);

        if (amplify)
            volume = EditorGUILayout.FloatField("Volume", volume);
        else volume = EditorGUILayout.Slider("Volume", volume, 0f, 1f);

        if (volume > 1f)
            EditorGUILayout.HelpBox("Volume above 1 can cause distortion. Use with caution.", MessageType.Warning);
        
        GUILayout.Space(10);

        minDistance = EditorGUILayout.FloatField("Min Distance (m)", minDistance);
        maxDistance = EditorGUILayout.FloatField("Max Distance (m)", maxDistance);

        GUILayout.Space(10);

        rolloffMode = (AudioRolloffMode)EditorGUILayout.EnumPopup("Rolloff Mode", rolloffMode);

        // ---

        GUILayout.FlexibleSpace(); // push down

        EditorGUILayout.BeginHorizontal();
        GUILayout.FlexibleSpace(); // push right 

        EditorGUILayout.BeginDisabledGroup(clip == null);

        if (GUILayout.Button("Interrupt"))
        {
            if (!isRepeating)
            {
                AudioManager.Instance.InterruptAllSources
                (
                    clip, minPitch, maxPitch, position, is2D, volume, minDistance, maxDistance, rolloffMode
                );
            }

            else
            {
                AudioManager.Instance.InterruptAllSourcesWithRepeating
                (
                    clip, minPitch, maxPitch, position, is2D, volume, minDistance, maxDistance, rolloffMode
                );
            }
        }

        EditorGUILayout.EndDisabledGroup();
        EditorGUILayout.EndHorizontal();
    }

    #endregion
}

#endregion

#region Set Pool Size Window

public class AudioManagerSetPoolSizeWindow : EditorWindow
{
    byte maxPoolSize;

    void OnEnable() => maxPoolSize = AudioManager.Instance.MaxPoolSize;

    public static void Open()
    {
        var window = GetWindow<AudioManagerSetPoolSizeWindow>();
        window.titleContent = new GUIContent
        (
            "Audio Manager : SetPoolSize()",
            EditorGUIUtility.IconContent("AudioSource Icon").image
        );
    }
    
    void OnGUI()
    {
        maxPoolSize = (byte)EditorGUILayout.IntField("New Max Pool Size", maxPoolSize);
        
        if (GUILayout.Button("Set"))
            AudioManager.Instance.SetPoolSize(maxPoolSize);
    }
}

#endregion

#endregion

#nullable disable

#endif