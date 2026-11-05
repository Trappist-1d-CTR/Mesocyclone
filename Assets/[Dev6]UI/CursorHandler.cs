using UnityEngine;
using System.ComponentModel;

namespace Mesocyclone
{
    // handles custom cursor stuff
    [EditorBrowsable(EditorBrowsableState.Never)] // don't show in IntelliSense
    public sealed class CursorHandler : MonoBehaviour
    {
        public static CursorHandler instance { get; private set; }

        [HideInInspector]
        public Texture2D cursorTexture;

        [HideInInspector]
        public string cursorPath;

        private void Awake()
        {
            DontDestroyOnLoad(gameObject);
        }
        
        private void Start()
        {
            cursorPath = DefaultCursorPath();
            cursorTexture = Resources.Load<Texture2D>(cursorPath);
        }

        private void OnEnable()
        {
            instance = this;

            if (cursorTexture != null)
            {
                Cursor.SetCursor(cursorTexture, Vector2.zero, CursorMode.Auto);
                print("Cursor applies succsessfully!");
            }
            else
            {
                UnityEngine.Debug.LogWarning(
                    $"Failed to set cursor!\n" +
                    $"{nameof(cursorTexture)} ({cursorPath}) is null"
                );
            }
        }

        private void OnDisable()
        {
            Cursor.SetCursor(null, Vector2.zero, CursorMode.Auto);

            instance = null;
        }

        private string DefaultCursorPath()
        {
            return "Cursor/cursor";
        }
    }
}