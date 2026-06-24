// Mary by Alex G hits hard
// script in which i discovered UnityEngine.Color(R, G, B, A) is measured from 0 - 1 rather than 0 - 255, i am utterly dissapointed. I'm moving over to MonoGame now...

using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using TMPro;

/// <summary>
/// Personal debugger for the game
/// more accurately named Debug, but Unity already took that name sooooo
/// 
/// <para>Class is partial, so you can update it for all your modding needs</para>
/// </summary>
public static partial class MCDebug // partial meaning u can just continue writing MCDebug from anywhere. Underrated asf feature. Thank me later modders
{
    // volatile makes it so threads don't cache it
    static volatile bool _isRunning = false;

    /// <summary>
    /// True if the program (technically the Debugger) is running
    /// </summary>
    public static bool isRunning => _isRunning; 

    public static bool devTools { get; private set; } = false;
    public static bool allowDevTools { get; private set; } = true; // relevant once modding API is established

    public static void Instantiate()
    {
        _isRunning = true;

        Application.quitting += OnQuit;
    }

    public static void Update()
    {
        // no way anyone actually accidentally hits this key
        if (Input.GetKeyDown(KeyCode.B))
            devTools = !devTools;
    }

    static void OnQuit() => _isRunning = false;
}


/// <summary>
/// Just runs the Debugger and integrated it into Unity's Player Loop, since it's a static class
/// </summary>
internal sealed class DebugRunner : MonoBehaviour
{
    // you do not have a permit sir
    private DebugRunner() { }

    static bool initialized;

    [SerializeField, Tooltip("Interval in which the FPS timer updates, modify this to your liking")]
    float fpsUpdateInterval = 1.35f;
    float fpsTimer;

    GameObject debugCanvasObj;
    Canvas debugCanvas;

    GameObject fpsTextObj;
    TextMeshProUGUI fpsText;
    RectTransform fpsTextRect;

    GameObject devToolsTextObj;
    TextMeshProUGUI devToolsText;
    RectTransform devToolsTextRect;

    DebugWindow fpsWindow;

    // Graphs
    GameObject fpsGraphObj;
    FPSGraph fpsGraph;

    static GameObject eventSystem; // IDragHandler logic stuff


    [RuntimeInitializeOnLoadMethod] // screw you unity
    static void Init()
    {
        if (initialized) return;
        initialized = true;

        MCDebug.Instantiate();

        GameObject go = new("Debugger & Profiler");
        DontDestroyOnLoad(go);
        go.AddComponent<DebugRunner>();

        // make sure an Event System doesn't already exist in the scene
        if (FindObjectOfType<EventSystem>() == null)
        {
            // Create the event system
            eventSystem = new GameObject("Event System");
            eventSystem.AddComponent<EventSystem>();
            eventSystem.AddComponent<StandaloneInputModule>();
            DontDestroyOnLoad(eventSystem);
        }
        else
            eventSystem = FindObjectOfType<EventSystem>();
    }

    void Awake()
    {
        // Debug Canvas
        debugCanvasObj = new GameObject("Debug Canvas");
        debugCanvasObj.transform.SetParent(transform);
        debugCanvas = debugCanvasObj.AddComponent<Canvas>();
        debugCanvas.renderMode = RenderMode.ScreenSpaceOverlay; // render ontop of everything
        debugCanvasObj.AddComponent<CanvasScaler>();
        debugCanvasObj.AddComponent<GraphicRaycaster>();

        // FPS Text
        fpsTextObj = new GameObject("FPS Counter");
        fpsTextObj.transform.SetParent(debugCanvasObj.transform);
        fpsText = fpsTextObj.AddComponent<TextMeshProUGUI>();
        fpsText.fontSize = 24; // make sure to tweak this to your liking
        fpsText.alignment = TextAlignmentOptions.TopLeft;

        // position FPS Counter to the top left corner
        fpsTextRect = fpsTextObj.GetComponent<RectTransform>();
        fpsTextRect.anchorMin = new Vector2(0, 1);
        fpsTextRect.anchorMax = new Vector2(0, 1);
        fpsTextRect.pivot = new Vector2(0, 1);
        fpsTextRect.anchoredPosition = new Vector2(10, -10);
        fpsTextRect.sizeDelta = new Vector2(200, 50);

        // :sparkles: style :sparkles:
        var fpsFont = Resources.Load<TMP_FontAsset>("Fonts & Materials/XoloniumBold-xKZO");
        if (fpsFont == null)
        {
            Debug.LogWarning
            (
                $"XoloniumBold-xKZO.asset not been able to load @ DebugRunner.fpsText.font\nResource: 'Resources/Fonts & Materials/XoloniumBold-xKZO.asset'\n \nPlausable issue is that the font is located in 'Assets/TextMesh Pro/*Resources*/[...]'\nRather than simply 'Assets/*Resources*/'\nSince unity lookups any folder named 'Resources'"
                // technically DebugRunner.fpsFont is referencing it, but just to make things clearer
            );
        }
        else fpsText.font = fpsFont;

        fpsText.fontMaterial.SetFloat(ShaderUtilities.ID_OutlineWidth, 0.2f); // 0 - 1
        fpsText.color = Color.white;

        // DevTools text
        devToolsTextObj = new GameObject("DevTools Txt");
        devToolsTextObj.transform.SetParent(debugCanvasObj.transform);
        devToolsText = devToolsTextObj.AddComponent<TextMeshProUGUI>();
        devToolsText.fontSize = 24;
        devToolsText.alignment = TextAlignmentOptions.Center;

        // position DevTools text
        devToolsTextRect = devToolsTextObj.GetComponent<RectTransform>();
        devToolsTextRect.anchorMin = new Vector2(0.5f, 1);
        devToolsTextRect.anchorMax = new Vector2(0.5f, 1);
        devToolsTextRect.pivot = new Vector2(0.5f, 1);
        devToolsTextRect.anchoredPosition = new Vector2(0, -10);
        devToolsTextRect.sizeDelta = new Vector2(200, 50);

        // :sparkles: style (sequel) :sparkles:
        var devToolsFont = Resources.Load<TMP_FontAsset>("Fonts & Materials/XoloniumBold-xKZO");
        if (devToolsFont == null)
        {
            Debug.LogWarning
            (
                $"XoloniumBold-xKZO.asset not been able to load @ DebugRunner.devToolsText.font\nResource: 'Resources/Fonts & Materials/XoloniumBold-xKZO.asset'\n \nPlausable issue is that the font is located in 'Assets/TextMesh Pro/*Resources*/[...]'\nRather than simply 'Assets/*Resources*/'\nSince unity lookups any folder named 'Resources'"
                // same situation here
            );
        }
        else devToolsText.font = devToolsFont;

        devToolsText.fontMaterial.SetFloat(ShaderUtilities.ID_OutlineWidth, 0.2f); // 0 - 1
        devToolsText.color = new Color(240f / 255f, 240f / 255f, 0f / 255f);
    }

    void Update()
    {
        fpsTextObj.SetActive(MCDebug.devTools);
        devToolsTextObj.SetActive(MCDebug.devTools);

        MCDebug.Update();

        if (!MCDebug.devTools) return; // none of this matters if devtools is off

        if (Input.GetKeyDown(KeyCode.N))
        {
            if (fpsWindow == null)
                SpawnFPSWindow();
        }

        fpsTimer += Time.deltaTime;

        if (fpsTimer >= fpsUpdateInterval)
        {
            fpsTimer = 0;
            int fps = Mathf.RoundToInt(1f / Time.smoothDeltaTime); // output reciprocal, since DeltaTime is just the interval between frames
            fpsText.text = $"{fps} FPS"; // TODO: eventually make this toggable in settings
            devToolsText.text = $"DevTools Enabled";
            fpsGraph?.AddSample(fps);
        }
    }

    void SpawnFPSWindow()
    {
        fpsGraphObj = new GameObject("FPS Graph");
        fpsGraphObj.transform.SetParent(debugCanvasObj.transform);
        fpsGraph = fpsGraphObj.AddComponent<FPSGraph>();
        _ = fpsGraphObj.AddComponent<CanvasRenderer>();
        fpsGraph.color = new Color(0f, 1f, 0f); // green

        fpsWindow = new DebugWindow(debugCanvasObj.transform, "FPS", fpsGraph);
    }
}

public sealed class DebugWindow
{
    public string title;
    public bool isMinimized { get; private set; }

    GameObject root;
    GameObject header;
    GameObject content;
    GameObject resizeHandle;

    TextMeshProUGUI titleText;
    Button minimizeButton;
    Button closeButton;

    RectTransform rootRect;
    RectTransform contentRect;

    DebugGraph graph;

    const float headerHeight = 24f;
    float minWidth;
    float minHeight;
    float maxWidth;
    float maxHeight;

    Color headerColor;
    Color contentColor;

    public DebugWindow
    (
        Transform canvasParent,
        string title,
        DebugGraph graph = null,
        float minWidth = 200f,
        float minHeight = 100f,
        float maxWidth = 1200f,
        float maxHeight = 600f,
        Color? headerColor = null,
        Color? contentColor = null
    )
    {
        this.title = title;
        this.graph = graph;
        this.minWidth = minWidth;
        this.minHeight = minHeight;
        this.headerColor = headerColor ?? new Color(0.2f, 0.2f, 0.2f); // gray
        this.contentColor = contentColor ?? new Color(0.085f, 0.085f, 0.085f); // darker gray

        // Root
        root = new GameObject($"{title}");
        root.transform.SetParent(canvasParent);
        rootRect = root.AddComponent<RectTransform>();
        rootRect.sizeDelta = new Vector2(300, 200);
        root.AddComponent<CanvasGroup>();

        // header
        header = new GameObject("Header");
        header.transform.SetParent(root.transform);
        Image headerBg = header.AddComponent<Image>();
        headerBg.color = this.headerColor;
        RectTransform headerRect = header.GetComponent<RectTransform>();
        headerRect.anchorMin = new Vector2(0, 1);
        headerRect.anchorMax = new Vector2(1, 1);
        headerRect.pivot = new Vector2(0, 1);
        headerRect.offsetMin = new Vector2(0, -headerHeight);
        headerRect.offsetMax = Vector2.zero;

        // Title
        GameObject titleObj = new("Title");
        titleObj.transform.SetParent(header.transform);
        titleText = titleObj.AddComponent<TextMeshProUGUI>();
        titleText.text = title;
        titleText.font = Resources.Load<TMP_FontAsset>("Fonts & Materials/XoloniumBold-xKZO");
        titleText.fontSize = 20;
        titleText.alignment = TextAlignmentOptions.MidlineLeft;
        RectTransform titleRect = titleObj.GetComponent<RectTransform>();
        titleRect.anchorMin = Vector2.zero;
        titleRect.anchorMax = Vector2.one;
        titleRect.offsetMin = new Vector2(6, 0);
        titleRect.offsetMax = new Vector2(-60, 60);

        // minimize button
        minimizeButton = CreateHeaderButton("─", new Vector2(-30, 0), header.transform); // TODO, implement mesh and texture integration
        minimizeButton.onClick.AddListener(ToggleMinimize);

        // Close button
        closeButton = CreateHeaderButton("✕", new Vector2(-6, 0), header.transform);
        closeButton.onClick.AddListener(Close);

        // make window draggable
        DragHandler drag = header.AddComponent<DragHandler>();
        drag.target = rootRect;

        // content
        content = new GameObject("Content");
        content.transform.SetParent(root.transform);
        Image contentBg = content.AddComponent<Image>();
        contentBg.color = this.contentColor;
        contentRect = content.GetComponent<RectTransform>();
        contentRect.anchorMin = Vector2.zero;
        contentRect.anchorMax = Vector2.one;
        contentRect.offsetMin = new Vector2(0, 0);
        contentRect.offsetMax = new Vector2(0, -headerHeight);

        // graph
        if (graph != null)
        {
            graph.transform.SetParent(content.transform);
            RectTransform graphRect = graph.GetComponent<RectTransform>();
            graphRect.anchorMin = Vector2.zero;
            graphRect.anchorMax = Vector2.one;
            graphRect.offsetMin = new Vector2(4, 4);
            graphRect.offsetMax = new Vector2(-4, -4);
        }

        // resize handle
        resizeHandle = new GameObject("Resize Handle");
        resizeHandle.transform.SetParent(root.transform);
        RectTransform handleRect = resizeHandle.AddComponent<RectTransform>();
        handleRect.anchorMin = Vector2.zero;
        handleRect.anchorMax = Vector2.zero;
        handleRect.pivot = Vector2.zero;
        handleRect.sizeDelta = new Vector2(16, 16);
        handleRect.anchoredPosition = Vector2.zero;
        resizeHandle.AddComponent<Image>().color = new Color(1, 1, 1, 0.3f);

        ResizeHandler resize = resizeHandle.AddComponent<ResizeHandler>();
        resize.target = rootRect;
        resize.minSize = new Vector2(minWidth, minHeight);
        resize.maxSize = new Vector2(maxWidth, maxHeight);
    }

    Button CreateHeaderButton(string label, Vector2 anchoredPos, Transform parent)
    {
        GameObject obj = new(label);
        obj.transform.SetParent(parent);
        Image bg = obj.AddComponent<Image>();
        bg.color = Color.white;
        Button button = obj.AddComponent<Button>();
        RectTransform rect = obj.GetComponent<RectTransform>();
        rect.anchorMin = new Vector2(1, 1);
        rect.anchorMax = new Vector2(1, 1);
        rect.pivot = new Vector2(1, 1);
        rect.sizeDelta = new Vector2(20, headerHeight);
        rect.anchoredPosition = anchoredPos;

        GameObject labelObj = new("Label");
        labelObj.transform.SetParent(obj.transform);
        TextMeshProUGUI text = labelObj.AddComponent<TextMeshProUGUI>();
        text.text = label;
        text.fontSize = 12f;
        text.alignment = TextAlignmentOptions.Center;
        RectTransform labelRect = labelObj.GetComponent<RectTransform>();
        labelRect.anchorMin = Vector2.zero;
        labelRect.anchorMax = Vector2.one;
        labelRect.offsetMin = labelRect.offsetMax = Vector2.zero;

        return button;
    }

    void ToggleMinimize()
    {
        isMinimized = !isMinimized;
        content.SetActive(!isMinimized);
        resizeHandle.SetActive(!isMinimized);
    }

    void Close()
    {
        Object.Destroy(root);
    }
}

// handles dragging the window
internal class DragHandler : MonoBehaviour, IDragHandler
{
    public RectTransform target;

    public void OnDrag(PointerEventData e) =>
        target.anchoredPosition += e.delta; 
}

// handles corner resizing
internal class ResizeHandler : MonoBehaviour, IDragHandler
{
    public RectTransform target;
    public Vector2 minSize;
    public Vector2 maxSize;

    public void OnDrag(PointerEventData e)
    {
        Vector2 delta = new(e.delta.x, -e.delta.y);
        Vector2 newSize = target.sizeDelta + delta;
        target.sizeDelta = new Vector2
        (
            Mathf.Clamp(newSize.x, minSize.x, maxSize.x),
            Mathf.Clamp(newSize.y, minSize.y, maxSize.y)
        );
    }
} 

// actual graph with content and elements in the Debug Window
public abstract class DebugGraph : Graphic
{
    protected sealed override void OnPopulateMesh(VertexHelper vh)
    {
        vh.Clear();
        Draw(vh);
    }

    protected abstract void Draw(VertexHelper vh);
}