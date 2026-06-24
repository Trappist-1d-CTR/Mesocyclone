using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public sealed class FPSGraph : DebugGraph
{
    Queue<float> samples = new();
    readonly int maxSamples = 100;
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

    protected override void OnRectTransformDimensionChange()
    {
        base.OnRectTransformDimensionsChange();
        SetVerticesDirty();
    }
}