using UnityEngine;
using UnityEngine.UI;
using TMPro;

public sealed class FPSGraph : DebugGraph
{
    Queue<float> samples = new();
    readonly int maxSamples = 100;
    float cap;

    void Awake() => cap = (float)Screen.currentResolution.refreshRateRatio.value;

    public void AddSample(float fps)
    {
        samples.Enqueue(fps);
        if (samples.Count > maxSamples) samples.Dequeue();
        SetVerticesDirty(); // tells unity to redraw
    }

    protected override void Draw(VertexHelper vh)
    {
        float[] array = samples.ToArray();
        float width = rectTransform.rect.width;
        float height = rectTransform.rect.height;

        for (int i = 0; i < array.Length - 1; i++)
        {
            float x1 = (i / (float)maxSamples) * width;
            float x2 = ((i + 1) / (float)maxSamples) * width;
            float y1 = Mathf.Clamp01(array[i] / cap) * height;
            float y2 = Mathf.Clamp01(array[i + 1] / cap) * height;

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
}