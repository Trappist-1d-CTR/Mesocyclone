using UnityEngine;
using UnityEngine.EventSystems;
using Unity.Collections;
using UnityEngine.UI;
using UnityEngine.InputSystem;

// normally i'd use Unity's own custom cursor API, but for custom cursor animation/logic it doesn't work that well...
// so that's why i use Image

namespace Mesocyclone
{
    /// <summary>
    /// Handles the cursor appearance in-game.
    /// </summary>
    public class CursorHandler : MonoBehaviour
    {
        [HideInInspector]
        public static CursorHandler main { get; private set; }

        [Header("References")]
        [field: SerializeField]
        public Canvas canvas { get; private set; }
        private RectTransform rectTransform;

        [Header("Scaling")]
        [SerializeField, ReadOnly, Min(0f), Tooltip("The scale of the cursor when idle.")]
        private float _normalScale = 1f;
        public virtual float normalScale
        {
            get { return _normalScale; }
            private set { _normalScale = Mathf.Max(value, 0f); } // minimum value is 0
        }
        [SerializeField, Min(0f), Tooltip("The scale of the cursor when selecting a uGUI element.")]
        private float _growthScale = 1.5f;
        public virtual float growthScale
        {
            get { return _growthScale; }
            private set { _growthScale = Mathf.Max(value, 0f); } // same here
        }

        [Header("Interpolation")]
        [SerializeField, Min(0f), Tooltip("How long (in seconds) it takes for the cursor to go from idle size to active size.")]
        private float _growthDuration = 0.25f; // in seconds
        public float growthDuration
        {
            get { return _growthDuration; }
            private set { _growthDuration = Mathf.Max(value, 0f); } // and here
        }
        [SerializeField, Tooltip("The interpolation of how the cursor grows.")]
        protected AnimationCurve growthCurve = AnimationCurve.Linear(0f, 0f, 1f, 1f); // modify in the inspector

        [Header("Misc")]
        [SerializeField, ReadOnly] private bool wasOverUI = false;
        private Vector3 startScale; // scale at the moment the transition began
        private Vector3 targetScale; // scale at the end of the transition
        private float transitionTimer;

        // automatically create a Game Object and self-attach
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void Initialize()
        {
            GameObject canvasGO = new GameObject("Cursor Canvas");
            Canvas canvas = canvasGO.AddComponent<Canvas>();
            canvas.sortingOrder = 30001;
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvasGO.AddComponent<CanvasScaler>();
            canvasGO.AddComponent<GraphicRaycaster>();
            DontDestroyOnLoad(canvasGO);

            GameObject cursorGO = new GameObject("Cursor Handler", typeof(RectTransform), typeof(Image));
            cursorGO.transform.SetParent(canvasGO.transform, false);
            cursorGO.GetComponent<Image>().raycastTarget = false;
            cursorGO.GetComponent<Image>().sprite = Resources.Load<Sprite>("Cursor/MCursor");
            cursorGO.GetComponent<RectTransform>().pivot = new(0, 1);
            cursorGO.GetComponent<RectTransform>().sizeDelta = new(25, 25);
            cursorGO.AddComponent<CursorHandler>();
        }

        private void Awake()
        {
            main ??= this;

            if (gameObject.GetComponent<RectTransform>() == null)
                rectTransform = gameObject.AddComponent<RectTransform>();
            else
                rectTransform = gameObject.GetComponent<RectTransform>();

            if (canvas is null)
                canvas = GetComponentInParent<Canvas>();
            
            startScale = Vector3.one * normalScale;
            targetScale = Vector3.one * normalScale;
            rectTransform.localScale = startScale;

            // hide OS cursor
            Cursor.visible = false;
            Cursor.lockState = CursorLockMode.None;
        }

        private void Update()
        {
            FollowMouse();
            UpdateHoverState();
            ApplyScale();
        }

        private void OnEnable()
        {
            main ??= this;
        }

        private void OnDisable()
        {
            main = null!;

            // restore OS cursor
            Cursor.visible = true;
        }

        private void OnDestroy()
        {
            main = null!;
        }

        protected virtual void FollowMouse()
        {
            Vector2 mousePosition = Mouse.current.position.ReadValue();

            if (canvas.renderMode is RenderMode.ScreenSpaceOverlay)
            {
                // in overlay mode, screen position maps to canvas pos
                rectTransform.position = mousePosition;
            }
            else
            {
                // convert properly in camera or world space
                RectTransformUtility.ScreenPointToLocalPointInRectangle
                (
                    canvas.transform as RectTransform,
                    mousePosition,
                    canvas.worldCamera,
                    out Vector2 localPoint
                );

                rectTransform.localPosition = localPoint;
            }
        }

        protected virtual void UpdateHoverState()
        {
            // true if it is over any uGUI element*
            bool isOverUI = EventSystem.current != null && EventSystem.current.IsPointerOverGameObject();

            // transition when hover state changes
            if (isOverUI != wasOverUI)
            {
                startScale = rectTransform.localScale;
                targetScale = Vector3.one * (isOverUI ? growthScale : normalScale);
                transitionTimer = 0f;
                wasOverUI = isOverUI;
            }
        }

        protected virtual void ApplyScale()
        {
            transitionTimer += Time.deltaTime;

            float normalizedTime = Mathf.Clamp01(transitionTimer / growthDuration);
            float curveValue = growthCurve.Evaluate(normalizedTime);

            rectTransform.localScale = Vector3.LerpUnclamped(startScale, targetScale, curveValue);
        } 
    }
}
