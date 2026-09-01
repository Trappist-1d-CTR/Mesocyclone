using UnityEngine;
using Unity.Collections;

// normally i'd use Unity's own custom cursor API, but for custom cursor animation/logic it doesn't work that well...
// so that's why i use Image

namespace Mesocyclone
{
    /// <summary>
    /// Singleton that handles the custom cursor appearance in-game.
    /// </summary>
    public class CursorHandler : MonoBehaviour
    {
        // singleton
        public static CursorHandler main { get; private set; }

        // Resources/Cursor
        [field: SerializeField, ReadOnly, Tooltip("The texture of the cursor.")]
        public Texture2D cursorTexture { get; protected set; }

        [field: SerializeField, ReadOnly, Tooltip("The path to the texture of the cursor.\nType for textures.")]
        public string cursorPath { get; protected set; }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void Initialize()
        {
            if (main is null)
            {
                GameObject cursorHandler = new("CursorHandler");
                main = cursorHandler.AddComponent<CursorHandler>();
                DontDestroyOnLoad(cursorHandler);
            }
        }

        private void Awake()
        {
            main ??= this;

            cursorPath = "Cursor/MCursor";
            cursorTexture = Resources.Load<Texture2D>(cursorPath);
        }

        private void OnEnable()
        {
            main ??= this;

            Cursor.SetCursor(cursorTexture, Vector2.zero, CursorMode.Auto); // assuming auto is the correct one
            Logger.Log($"A custom cursor has been set!\n{cursorPath}");
        }

        private void OnDisable()
        {
            main = null!;

            Cursor.SetCursor(null, Vector2.zero, CursorMode.Auto);
        }

        private void OnDestroy()
        {
            main = null!;

            Cursor.SetCursor(null, Vector2.zero, CursorMode.Auto);
        }
    }
}
