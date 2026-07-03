using UnityEngine;

namespace MCCustom
{
    /// <summary>
    /// Base class for debug graphs
    /// <para>duh</para>
    /// </summary>
    public abstract class DebugGraph
    {
        protected readonly string label;
        protected readonly Rect anchorRect; // dimensions of the chroma
        
        static Texture2D backgroundTexture;
        static Color backgroundColor { get; }
            = new Color(0.1f, 0.1f, 0.1f, 0.75f); // slightly transparent dark grey

        /// <summary>
        /// Constructs MCCustom.DebugGraph
        /// </summary>
        /// <param name="label">name of the Debug Graph</param>
        /// <param name="anchorRect">Rectangle which defines the dimensions of the chroma</param>
        protected DebugGraph(string label, Rect anchorRect)
        {
            this.label = label;
            this.anchorRect = anchorRect;

            if (backgroundTexture == null)
            {
                backgroundTexture = new Texture2D(1, 1);
                backgroundTexture.SetPixel(0, 0, Color.white);
                backgroundTexture.Apply();
            }

            DebugGraphManager.Register(this);
        }

        // virtual in case you wanna get fancy ig
        public virtual void OnGUI()
        {
            // to prevent multiple Debug.LogWarning calls
            bool checkFont = true;

            Color prev = GUI.color;
            GUI.color = backgroundColor;
            GUI.DrawTexture(anchorRect, backgroundTexture);
            GUI.color = prev;

            if (!string.IsNullOrEmpty(label))
            {
                GUIStyle style = new(GUI.skin.label)
                {
                    font = Resources.Load("Fonts & Materials/Xolonium-pn4D"),
                    fontSize = 24,
                    fontStyle = FontStyle.Bold,
                };

                if (style.font != Resources.Load("Fonts & Materials/Xolonium-pn4D") && checkFont)
                {
                    Debug.LogWarning
                    (
                        // REMINDER: Update ln during Debug.cs changes
                        $"Font 'Xolonium-pn4D' unable to be loaded\n@: MCCustom.DebugGraph.OnGUI().style.font (DebugGraph.cs : ln 54)\nDirectory: Assets/TextMesh Pro/Resources/Fonts & Materials/Xolonium-pn4D.asset\n \nCause may be because Unity looks at all folders named 'Resources'\nwhich could trip it up"
                    );

                    checkFont = false; // prevent this from re-calling
                } 

                GUI.Label(new Rect(anchorRect.x + 4, anchorRect.y + 2, anchorRect.width - 8, 16), label, style);
            }

            DrawGraph(GetContentRect());
        }

        protected abstract void DrawGraph(Rect contentRect);

        Rect GetContentRect()
        {
            // inset for border/header
            // REMINDER: hopefully these values work
            // P.S: idk what i'm doing
            // i have no fucking clue atp
            // money does buy happiness
            // and sanity
            // ...to a certain extent...

            // plz ignore this rant
            // TODO: remove rant once i have a laptop
            // this set of comments is reserved for all of the people who've made it
            // this far into the rabbit hole of the mesocyclone git history

            // get help

            return new Rect(anchorRect.x + 4, anchorRect.y + 20, anchorRect.width - 8, anchorRect.height - 24);

            // sincere intergalactarian apologies
        }
    }
}