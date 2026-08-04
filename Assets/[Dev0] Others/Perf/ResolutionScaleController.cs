using GameState = DynamicGameStateHandler.GameState; // alias this piece of shit

using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Mesocyclone
{
    /// <summary>
    /// Controls rendered *camera* resolution depending on the game state
    /// </summary>
    public class ResolutionScaleController : MonoBehaviour
    {
        [SerializeField] private Camera targetCamera;

        [SerializeField]
        [Range(0.1f, 2.5f)]
        [Tooltip("How long it takes to transition over to the new resolution. Modify to your liking")]
        private float transitionDuration = 0.6f; // modify to what's best

        [SerializeField] // wished Unity's serializer could serialize tuples & properties :(
        [Tooltip("The different resolution scales the game goes through depending on the current DynamicGameStateHandler.gameState(*)\nModify these to your liking.\nNOTE: These go through in order from GameState.Standard to GameState.Aggressive")]
        private float[] resolutionScales = new float[]
        {
            1.0f,
            0.85f,
            0.7f,
            0.5f,
            0.35f
        };

        private Coroutine routine;

        private void Awake()
        {
            if (!TryGetComponent(out targetCamera))
                throw new Exception("RenderScaleController's attached GameObject is not a Camera!");

            targetCamera.allowDynamicResolution = true;
            DynamicGameStateHandler.OnGameStateChanged += HandleStateChanged;
        }

        private void OnDestroy()
        {
            DynamicGameStateHandler.OnGameStateChanged -= HandleStateChanged;
        }

        private void HandleStateChanged(GameState oldState, GameState newState)
        {
            float targetScale = ScaleForState(newState);
            if (routine != null) StopCoroutine(routine);
            routine = StartCoroutine(TransitionScale(targetScale));
        }

        // holy shit having to juggle between Godot and Unity API's is so confusing
        // i have no idea why this comment is specifically here
        private IEnumerator TransitionScale(float targetScale)
        {
            float startScale = ScalableBufferManager.widthScaleFactor; // most random asf Unity API, it's almost like it was made for this very moment
            float t = 0f;

            while (t < transitionDuration)
            {
                t += Time.unscaledDeltaTime;

                // i believe this is like, a bezier curve??
                float eased = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(t / transitionDuration));
                // linear interpolation
                float scale = Mathf.Lerp(startScale, targetScale, eased);
                ScalableBufferManager.ResizeBuffers(scale, scale); // this is probably expensive as fuck, but using shaders won't do anything, so this is my only option. Unless i'm stupid as fuck (count 51)
                yield return null;
            }
            routine = null;
        }

        // i purposely avoid switch expressions because their syntax is bullshit
        // it doesn't make any sense and it's just hard to memorize
        private static float ScaleForState(GameState state) => state switch
        {
            GameState.Standard => resolutionScales[0],
            GameState.Tuned => resolutionScales[1],
            GameState.Restricted => resolutionScales[2],
            GameState.Limited => resolutionScales[3],
            GameState.Aggressive => resolutionScales[4],
            _ => resolutionScales[0],
        };
    }
}