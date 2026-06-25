using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

/* TODO:
    Implement CPU Usa
*/

/// excuse my shitty XML

/// <summary>
/// Literally in the name
///
/// <para>Contains the graphics and display for the CPU Usage graph in the devtools</para>
/// <para>which unsuprisingly displays the current usage of the CPU</para>
/// <para>and the CPU usage the game process is taking up</para>
/// <para>Since task manager is stupid and annoying</para>
/// </summary>
public sealed class CPUUsageGraph : DebugGraph
{
    // Overall CPU Usage
    Queue<float> samples = new();
    const byte maxSamples = 106;
    byte cap = 100;

    public void AddSample(float cpuUsage)
    {
        samples.Enqueue(cpuUsage);
        if (samples.Count > maxSamples) samples.Dequeue();
        SetVerticesDirty();
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

    protected override void OnRectTransformDimensionsChange()
    {
        base.OnRectTransformDimensionsChange();
        SetVerticesDirty();
    }
}

/// <summary>
/// 
/// </summary>
public sealed class CPUTempGraph : DebugGraph
{
    // TODO
}
