using UnityEngine;

namespace MCCustom
{
    public sealed class FPSGraph : DebugGraph
    {
        static Material lineMaterial;

        readonly float[] samples = new float[120];
        int head;

        public FPSGraph() : base("FPS", new Rect(10, 10, 144, 64))
        { }

        // this is all just a 'that's what she said' joke
        public void Sample(float deltaTime)
        {
            // modulus breaks my brain
            samples[head % samples.Length] = 1f / deltaTime;
            head++;
        }

        // unity hates me even more
        protected override void DrawGraph(Rect r)
        {
            if (lineMaterial == null)
                lineMaterial = new Material(Shader.Find("Hidden/Internal-Colored"));

            const float max = 120f;

            lineMaterial.SetPass(0);
            GL.PushMatrix();
            GL.LoadPixelMatrix();
            GL.Begin(GL.LINES);
            GL.Color(new Color(0, 240f / 255f, 0)); // green

            // REMINDER: guessing positions rn [insert fire emojis here]
            // fix later
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
            GL.PopMatrix();
        }
    }
}