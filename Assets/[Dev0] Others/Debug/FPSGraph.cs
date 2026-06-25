// A lot of graphs are based off of this, so.... i'm just hoping this works before heven trying it ;-;
// minimal playesting yields greater results

 
// using System.Collections;

// a formal message to Trappist-1d:
// because microsoft devs are retarded, System.Collections.Generic and System.Collections are actually completely different*
// Collections.Generic contains literally every collection type you'd actually use
// and regular Collections is a separate namespace which contains collections specified towards people who are mental

// Now, why are very little if not no .NET namespaces actually nested?
// don't ask me
// ask bill gates

// goodnight.

using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public sealed class FPSGraph : DebugGraph
{
    Queue<float> samples = new();
    readonly byte maxSamples = 106; // enough samples to hold 1m 30s worth of data
    float cap;

    // FPS text
    GameObject fpsTextObj;
    TextMeshProUGUI fpsText;

    void Awake()
    {
        cap = (float)Screen.currentResolution.refreshRateRatio.value;

        fpsTextObj = new GameObject("FPS Text");
        fpsTextObj.transform.SetParent(transform); // parents to the graph

        fpsText = fpsTextObj.AddComponent<TextMeshProUGUI>();

        RectTransform rect = fpsTextObj.GetComponent<RectTransform>();
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = new Vector2(1, 0);
        rect.pivot = new Vector2(0.5f, 0);
        rect.anchoredPosition = new Vector2(0, 4);
        rect.sizeDelta = new Vector2(0, 20);
    }

    public void AddSample(float fps)
    {
        samples.Enqueue(fps);
        if (samples.Count > maxSamples) samples.Dequeue();
        SetVerticesDirty(); // tells unity to redraw

        fpsText.text = $"{fps} FPS";
    }

    protected override void Draw(VertexHelper vh)
    {
        float[] array = samples.ToArray();
        float width = rectTransform.rect.width;
        float height = rectTransform.rect.height;

        // offsets to fix alignment issues
        float offsetX = -width / 2f;
        float offsetY = -height / 2f;

        // TODO: so far the max Y is clamped and just flattens out the FPS; implement clipping to let Y be as high as it wants, just don't draw any of those extra pixels
        for (int i = 0; i < array.Length - 1; i++)
        {
            float x1 = (i / (float)maxSamples) * width + offsetX;
            float x2 = ((i + 1) / (float)maxSamples) * width + offsetX;
            float y1 = Mathf.Clamp01(array[i] / cap) * height + offsetY;
            float y2 = Mathf.Clamp01(array[i + 1] / cap) * height + offsetY;

            vh.AddUIVertexQuad
            (
                new UIVertex[]
                {
                    new() { position = new Vector3(x1, y1 - 1), color = base.color },
                    new() { position = new Vector3(x1, y1 + 1), color = base.color },
                    new() { position = new Vector3(x2, y2 + 1), color = base.color },
                    new() { position = new Vector3(x2, y2 - 1), color = base.color }     
                }
            );
        }
    }

    protected override void OnRectTransformDimensionsChange()
    {
        base.OnRectTransformDimensionsChange();
        SetVerticesDirty();
    }
}
