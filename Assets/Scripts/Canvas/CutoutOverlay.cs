using UnityEngine;
using UnityEngine.UI;
using Assets.Helper;

/// <summary>
/// CUTOUTOVERLAY - Safe area cutout masking overlay.
/// 
/// PURPOSE:
/// Creates UI panels that mask the screen edges to respect
/// device safe areas (notches, rounded corners, etc.).
/// 
/// STRUCTURE:
/// ```
/// ┌──────────────────────────────┐
/// │          [Top]               │ ← Masks top safe area
/// ├─────┬────────────────┬───────┤
/// │Left │   CenterPane   │ Right │ ← Side masks
/// │Pane │   (visible)    │ Pane  │
/// ├─────┴────────────────┴───────┤
/// │         [Bottom]             │ ← Masks bottom safe area
/// └──────────────────────────────┘
/// ```
/// 
/// AUTO-ADAPTATION:
/// - Monitors safe area changes
/// - Updates on screen size/orientation change
/// - Respects canvas scale factor
/// 
/// HIERARCHY:
/// - topRoot: Top masking panel
/// - leftPane/centerPane/rightPane: Middle row
/// - bottomRoot: Bottom masking panel
/// 
/// RELATED FILES:
/// - GameObjectHelper.cs: Reference paths
/// - CanvasHelper.cs: Canvas access
/// </summary>
[DisallowMultipleComponent]
//[ExecuteAlways]
[RequireComponent(typeof(RectTransform))]
public sealed class CutoutOverlay : MonoBehaviour
{
    [Header("Hierarchy (auto-assigned if null)")]
    [SerializeField] private RectTransform topRoot;        // "Top" container with Image
    [SerializeField] private RectTransform leftPane;       // "LeftPane" under Top
    [SerializeField] private RectTransform centerPane;     // "CenterPane" under Top
    [SerializeField] private RectTransform rightPane;      // "RightPane" under Top
    [SerializeField] private RectTransform bottomRoot;     // "Bottom" container with Image

    private RectTransform _rect;
    private Canvas _rootCanvas;

    private Rect _lastSafe = Rect.zero;
    private Vector2Int _lastSize = Vector2Int.zero;
    private ScreenOrientation _lastOrientation = ScreenOrientation.AutoRotation;
    private float _lastScale = -1f;

    // ---------------------------------------------------------------------
    // Lifecycle
    // ---------------------------------------------------------------------

    private void Awake()
    {
        // Cache core components
        _rect = GetComponent<RectTransform>();
        _rootCanvas = GetComponentInParent<Canvas>();

        // Assign serialized references using helper if available
        // These paths are defined in GameObjectHelper.Game.CutoutOverlay
        // Fallback to auto-create if any are missing at runtime
        try
        {
            if (topRoot == null) topRoot = GameObjectHelper.Game.CutoutOverlay.TopRoot;
            if (leftPane == null) leftPane = GameObjectHelper.Game.CutoutOverlay.LeftPaneRect;
            if (centerPane == null) centerPane = GameObjectHelper.Game.CutoutOverlay.CenterPaneRect;
            if (rightPane == null) rightPane = GameObjectHelper.Game.CutoutOverlay.RightPaneRect;
            if (bottomRoot == null) bottomRoot = GameObjectHelper.Game.CutoutOverlay.BottomRoot;
        }
        catch
        {
            // In case objects are not present by path, create/assign locally
            AutoAssignOrCreateChildren();
        }
    }

    private void OnEnable()
    {
        if (_rect == null)
        {
            _rect = GetComponent<RectTransform>();
        }
        if (_rootCanvas == null)
        {
            _rootCanvas = GetComponentInParent<Canvas>();
        }

        if (topRoot == null || leftPane == null || centerPane == null || rightPane == null || bottomRoot == null)
        {
            AutoAssignOrCreateChildren();
        }

        Apply(force: true);
    }

    private void Update()
    {
#if UNITY_EDITOR
        if (!Application.isPlaying)
        {
            Apply(force: true);
            return;
        }
#endif
        float s = CanvasScale();
        if (Screen.safeArea != _lastSafe ||
            Screen.width != _lastSize.x ||
            Screen.height != _lastSize.y ||
            Screen.orientation != _lastOrientation ||
            !Mathf.Approximately(s, _lastScale))
        {
            Apply(force: false);
        }
    }

    // ---------------------------------------------------------------------
    // Public API
    // ---------------------------------------------------------------------

    /// <summary>
    /// Pane accessors for scene code that needs to add children at runtime.
    /// </summary>
    public RectTransform LeftPane => leftPane;
    public RectTransform CenterPane => centerPane;
    public RectTransform RightPane => rightPane;

    /// <summary>
    /// Instantiates a prefab under LeftPane and stretches it if it is UI.
    /// </summary>
    public T AddToLeft<T>(T prefab) where T : Component
    {
        if (prefab == null || leftPane == null) return null;
        var inst = Instantiate(prefab, leftPane, worldPositionStays: false);
        StretchIfUI(inst.transform as RectTransform);
        return inst;
    }

    /// <summary>
    /// Instantiates a prefab under CenterPane and stretches it if it is UI.
    /// </summary>
    public T AddToCenter<T>(T prefab) where T : Component
    {
        if (prefab == null || centerPane == null) return null;
        var inst = Instantiate(prefab, centerPane, worldPositionStays: false);
        StretchIfUI(inst.transform as RectTransform);
        return inst;
    }

    /// <summary>
    /// Instantiates a prefab under RightPane and stretches it if it is UI.
    /// </summary>
    public T AddToRight<T>(T prefab) where T : Component
    {
        if (prefab == null || rightPane == null) return null;
        var inst = Instantiate(prefab, rightPane, worldPositionStays: false);
        StretchIfUI(inst.transform as RectTransform);
        return inst;
    }

    /// <summary>
    /// Instantiates a prefab GameObject under LeftPane.
    /// </summary>
    public GameObject AddToLeft(GameObject prefab)
    {
        if (prefab == null || leftPane == null) return null;
        var inst = Instantiate(prefab, leftPane, worldPositionStays: false);
        StretchIfUI(inst.transform as RectTransform);
        return inst;
    }

    /// <summary>
    /// Instantiates a prefab GameObject under CenterPane.
    /// </summary>
    public GameObject AddToCenter(GameObject prefab)
    {
        if (prefab == null || centerPane == null) return null;
        var inst = Instantiate(prefab, centerPane, worldPositionStays: false);
        StretchIfUI(inst.transform as RectTransform);
        return inst;
    }

    /// <summary>
    /// Instantiates a prefab GameObject under RightPane.
    /// </summary>
    public GameObject AddToRight(GameObject prefab)
    {
        if (prefab == null || rightPane == null) return null;
        var inst = Instantiate(prefab, rightPane, worldPositionStays: false);
        StretchIfUI(inst.transform as RectTransform);
        return inst;
    }

    // ---------------------------------------------------------------------
    // Core layout
    // ---------------------------------------------------------------------

    /// <summary>
    /// Positions Top from screen top down to safe-area top. Positions Bottom from
    /// screen bottom up to safe-area bottom. Panes are laid out inside Top.
    /// </summary>
    private void Apply(bool force)
    {
        if (_rect == null) return;

        Rect safe = Screen.safeArea;
        Vector2Int size = new Vector2Int(Screen.width, Screen.height);
        ScreenOrientation orient = Screen.orientation;
        float scale = CanvasScale();

        if (!force &&
            safe == _lastSafe &&
            size == _lastSize &&
            orient == _lastOrientation &&
            Mathf.Approximately(scale, _lastScale))
        {
            return;
        }

        _lastSafe = safe;
        _lastSize = size;
        _lastOrientation = orient;
        _lastScale = scale;

        EnsureRootAnchors();

        float topInsetPx = Mathf.Max(0f, size.y - safe.yMax);
        float bottomInsetPx = Mathf.Max(0f, safe.yMin);

        float topInsetUnits = PixelsToUnits(topInsetPx);
        float bottomInsetUnits = PixelsToUnits(bottomInsetPx);

        LayoutTopInset(topInsetUnits);
        LayoutBottomInset(bottomInsetUnits);
        LayoutPanes();
    }

    /// <summary>
    /// Sizes Top to exactly the unsafe top inset and pins it to the top edge.
    /// </summary>
    private void LayoutTopInset(float heightUnits)
    {
        if (topRoot == null) return;

        topRoot.anchorMin = new Vector2(0f, 1f);
        topRoot.anchorMax = new Vector2(1f, 1f);
        topRoot.pivot = new Vector2(0.5f, 1f);

        topRoot.anchoredPosition = Vector2.zero;
        topRoot.offsetMin = new Vector2(0f, -heightUnits);
        topRoot.offsetMax = Vector2.zero;

        var img = topRoot.GetComponent<Image>();
        if (img != null) img.enabled = heightUnits > 0.001f;
    }

    /// <summary>
    /// Sizes Bottom to exactly the unsafe bottom inset and pins it to the bottom edge.
    /// </summary>
    private void LayoutBottomInset(float heightUnits)
    {
        if (bottomRoot == null) return;

        bottomRoot.anchorMin = new Vector2(0f, 0f);
        bottomRoot.anchorMax = new Vector2(1f, 0f);
        bottomRoot.pivot = new Vector2(0.5f, 0f);

        bottomRoot.anchoredPosition = Vector2.zero;
        bottomRoot.offsetMin = Vector2.zero;
        bottomRoot.offsetMax = new Vector2(0f, heightUnits);

        var img = bottomRoot.GetComponent<Image>();
        if (img != null) img.enabled = heightUnits > 0.001f;
    }

    /// <summary>
    /// Lays out three panes inside Top as equal thirds: Left, Center, Right.
    /// </summary>
    private void LayoutPanes()
    {
        if (topRoot == null) return;

        // Left: 0.0 to 0.333
        if (leftPane != null)
        {
            leftPane.SetParent(topRoot, false);
            leftPane.anchorMin = new Vector2(0f, 0f);
            leftPane.anchorMax = new Vector2(1f / 3f, 1f);
            leftPane.pivot = new Vector2(0f, 0.5f);
            leftPane.offsetMin = Vector2.zero;
            leftPane.offsetMax = Vector2.zero;
        }

        // Center: 0.333 to 0.666
        if (centerPane != null)
        {
            centerPane.SetParent(topRoot, false);
            centerPane.anchorMin = new Vector2(1f / 3f, 0f);
            centerPane.anchorMax = new Vector2(2f / 3f, 1f);
            centerPane.pivot = new Vector2(0.5f, 0.5f);
            centerPane.offsetMin = Vector2.zero;
            centerPane.offsetMax = Vector2.zero;
        }

        // Right: 0.666 to 1.0
        if (rightPane != null)
        {
            rightPane.SetParent(topRoot, false);
            rightPane.anchorMin = new Vector2(2f / 3f, 0f);
            rightPane.anchorMax = new Vector2(1f, 1f);
            rightPane.pivot = new Vector2(1f, 0.5f);
            rightPane.offsetMin = Vector2.zero;
            rightPane.offsetMax = Vector2.zero;
        }
    }

    // ---------------------------------------------------------------------
    // Helpers
    // ---------------------------------------------------------------------

    /// <summary>
    /// Ensures the root RectTransform fills the canvas.
    /// </summary>
    private void EnsureRootAnchors()
    {
        if (_rect == null) return;
        _rect.anchorMin = new Vector2(0f, 0f);
        _rect.anchorMax = new Vector2(1f, 1f);
        _rect.pivot = new Vector2(0.5f, 0.5f);
        _rect.offsetMin = Vector2.zero;
        _rect.offsetMax = Vector2.zero;
    }

    /// <summary>
    /// Creates required children if missing and assigns references.
    /// </summary>
    private void AutoAssignOrCreateChildren()
    {
        if (topRoot == null) topRoot = FindOrCreate("Top");
        if (leftPane == null) leftPane = FindOrCreate("LeftPane", topRoot);
        if (centerPane == null) centerPane = FindOrCreate("CenterPane", topRoot);
        if (rightPane == null) rightPane = FindOrCreate("RightPane", topRoot);
        if (bottomRoot == null) bottomRoot = FindOrCreate("Bottom");
    }

    /// <summary>
    /// Finds a RectTransform child by name or creates it under the given parent.
    /// Adds an Image to Top and Bottom so the unsafe areas can be tinted.
    /// </summary>
    private RectTransform FindOrCreate(string name, RectTransform parentOverride = null)
    {
        Transform parent = parentOverride != null ? parentOverride : transform;
        var t = parent.Find(name);
        if (t != null)
        {
            var rtExisting = t as RectTransform;
            if (rtExisting != null) return rtExisting;
            return t.gameObject.AddComponent<RectTransform>();
        }

        var go = new GameObject(name, typeof(RectTransform));
        var rt = go.GetComponent<RectTransform>();
        rt.SetParent(parent, false);
        rt.anchorMin = new Vector2(0f, 0f);
        rt.anchorMax = new Vector2(1f, 1f);
        rt.pivot = new Vector2(0.5f, 0.5f);
        rt.offsetMin = Vector2.zero;
        rt.offsetMax = Vector2.zero;

        if (name == "Top" || name == "Bottom")
        {
            if (go.GetComponent<Image>() == null) go.AddComponent<Image>();
        }

        return rt;
    }

    /// <summary>
    /// Stretches a RectTransform to its parent if available.
    /// </summary>
    private void StretchIfUI(RectTransform rt)
    {
        if (rt == null) return;
        rt.anchorMin = new Vector2(0f, 0f);
        rt.anchorMax = new Vector2(1f, 1f);
        rt.pivot = new Vector2(0.5f, 0.5f);
        rt.offsetMin = Vector2.zero;
        rt.offsetMax = Vector2.zero;
    }

    /// <summary>
    /// Converts device pixels to canvas units based on the active canvas scale.
    /// </summary>
    private float PixelsToUnits(float pixels)
    {
        float s = CanvasScale();
        return s <= 0f ? pixels : pixels / s;
    }

    /// <summary>
    /// Returns the current Canvas scale factor or 1 if none.
    /// </summary>
    private float CanvasScale()
    {
        if (_rootCanvas == null) return 1f;
        return _rootCanvas.scaleFactor <= 0f ? 1f : _rootCanvas.scaleFactor;
    }
}
