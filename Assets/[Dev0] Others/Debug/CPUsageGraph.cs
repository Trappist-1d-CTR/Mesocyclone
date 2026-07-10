using System;
using System.Diagnostics;
using UnityEngine;

namespace MCCustom
{
    // TODO: literally all the same as FPSGraph
    // TODO: implement functionality for mac and linux

    /// <summary>
    /// Graph that displays the current overall CPU Usage, and how much the game is taking up
    /// </summary>
    public sealed class CPUUsageGraph : DebugGraph
    {
        static Material lineMaterial;

        readonly float[] overallSamples = new float[120]; // used for the graph of overall CPU Usage
        readonly float[] processSamples = new float[120]; // used for the graph of Usage the game uses
        int head;

        TimeSpan lastCPUTime;
        float lastSampleTime;

        public CPUUsageGraph() : base("CPU %", new Rect(Screen.width - 154, Screen.height - 138, 144, 128))
        {
            lastCPUTime = MCDebug.process.TotalProcessorTime;
            lastSampleTime = Time.realtimeSinceStartup;
        }

        public void Sample()
        {
            // TODO: multi-platform implementation
            #if ONWINDOWS
                float overall = MCDebug.w_cpuCounter.NextValue(); // 0 - 100
            #else
                float overall = 0f; // placeholder
            #endif

            TimeSpan now = MCDebug.process.TotalProcessorTime;
            float wallDelta = Time.realtimeSinceStartup - lastSampleTime;
            float cpuDelta = (float)(now - lastCPUTime).TotalSeconds;
            
            // Credit to Astraa for teaching me this
            float processPct = wallDelta > 0f
                ? Mathf.Clamp01(cpuDelta / (wallDelta * SystemInfo.processorCount)) * 100f
                : 0f;
            
            lastCPUTime = now;
            lastSampleTime = Time.realtimeSinceStartup;

            overallSamples[head % overallSamples.Length] = overall;
            processSamples[head % processSamples.Length] = processPct;
            head++; // that's what he said
        }

        // i love using highly highly deprecated unity API's :D
        protected override void DrawGraph(Rect r)
        {
            if (lineMaterial == null)
                lineMaterial = new Material(Shader.Find("Hidden/Internal-Colored"));
            
            const float max = 100f; // CPU Usage can only go up to 100%, duh

            lineMaterial.SetPass(0);
            GL.PushMatrix();
            GL.LoadPixelMatrix();

                                                      // blue
            DrawSeries(overallSamples, r, max, new Color(0, 0, 240f / 255f));

                                                      // cyan
            DrawSeries(processSamples, r, max, new Color(0, 240f / 255f, 240f / 255f));


            GL.PopMatrix();
        }

        void DrawSeries(float[] samples, Rect r, float max, Color color)
        {
            GL.Begin(GL.LINES);
            GL.Color(color);

            for (int i = 1; i < samples.Length; i++)
            {
                float x0 = r.x + r.width * (i - 1) / (samples.Length - 1);
                float x1 = r.x + r.width * i / (samples.Length - 1);
                float y0 = r.yMax - r.height * Mathf.Clamp01(samples[(head + i - 1) % samples.Length] / max);
                float y1 = r.yMax - r.height * Mathf.Clamp01(samples[(head + i) % samples.Length] / max);

                GL.Vertex3(x0, y0, 0);
                GL.Vertex3(x1, y1, 0);
            }

            GL.End();
        }
    }

    /*
    public sealed class CPUTempGraph : DebugGraph
    {
        // scrapped
        // acquiring CPU temperature would require a kernel driver
        // that acquires permission to read the CPU MSR data / communicate with the motherboards controller chip
        // and return the monitored temperature through an API (as an extern field or smth)
        // which is ofc both beyond my programming capabilities
        // and extremely impractical and unnecessary
        // just use CPU-Z
    }
    */
}