using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using UnityEditor;
using TMPro;
using Scripts.Canvas;
using Scripts.Managers;
using Scripts.Instances;
using Scripts.Hub;

/// <summary>
/// CAFFOLDHELPER - Shared utilities for scene scaffold editor tools.
///
/// PURPOSE:
/// Provides reusable building blocks used by all scene scaffold scripts
/// (TitleScreenScaffold, SplashScreenScaffold, etc.) to programmatically
/// create or validate GameObjects in a scene. Each method is idempotent —
/// it skips creation if the object already exists.
///
/// PATTERN:
/// Every Ensure* method follows the same contract:
///   1. Search for an existing object by name (under parent if given)
///   2. If found, increment `found` counter and return it
///   3. If missing, create it with correct components, register Undo,
///      increment `created` counter, and return it
///
/// COMMON OBJECTS CREATED:
/// - Camera: Orthographic, depth -1, solid black background
/// - EventSystem: EventSystem + StandaloneInputModule
/// - Canvas: Canvas (ScreenSpaceOverlay) + CanvasScaler (ScaleWithScreenSize 1920x1080)
///           + GraphicRaycaster
/// - FadeOverlay: Full-screen black Image (last sibling, for SceneHelper fade transitions)
/// - CutoutOverlay: Decorative frame with Top (LeftPane/CenterPane/RightPane) + Bottom
/// - ScrollView: ScrollRect + Viewport/Content + Vertical/Horizontal Scrollbars
/// - Button: Image + Button + Label (TMP child)
/// - Label: TMP text element
/// - Image: UI Image element
/// - NineSliceFrame: 9 border Images (Top, Bottom, Left, Right, corners, Background)
///
/// USAGE:
/// ```csharp
/// int created = 0, found = 0;
/// SceneScaffoldHelper.EnsureCamera("Main Camera", ref created, ref found);
/// var canvas = SceneScaffoldHelper.EnsureCanvas("Canvas", ref created, ref found);
/// SceneScaffoldHelper.EnsureFadeOverlay(canvas, ref created, ref found);
/// SceneScaffoldHelper.LogResults("MyScene", created, found);
/// ```
///
/// RELATED FILES:
/// - SplashScreenScaffold.cs, TitleScreenScaffold.cs, etc. (consumers)
/// - HubSceneValidator.cs: Similar pattern for Hub section panel children
/// - GameObjectHelper.cs: Path constants these objects must match
/// </summary>
public static class SceneScaffoldHelper
{
    // ===================== Root-Level Objects =====================

    /// <summary>Creates an orthographic Camera if it does not already exist.</summary>
    public static void EnsureCamera(string name, ref int created, ref int found)
    {
        var existing = GameObject.Find(name);
        if (existing != null) { found++; return; }

        var go = new GameObject(name);
        var cam = go.AddComponent<Camera>();
        cam.orthographic = true;
        cam.orthographicSize = 5f;
        cam.depth = -1;
        cam.clearFlags = CameraClearFlags.SolidColor;
        cam.backgroundColor = Color.black;
        go.AddComponent<AudioListener>();
        Undo.RegisterCreatedObjectUndo(go, $"Create {name}");
        created++;
    }

    /// <summary>Creates an EventSystem + StandaloneInputModule if none exists.</summary>
    public static void EnsureEventSystem(ref int created, ref int found)
    {
        var existing = Object.FindObjectOfType<EventSystem>();
        if (existing != null) { found++; return; }

        var go = new GameObject("EventSystem");
        go.AddComponent<EventSystem>();
        go.AddComponent<StandaloneInputModule>();
        Undo.RegisterCreatedObjectUndo(go, "Create EventSystem");
        created++;
    }

    /// <summary>Creates a plain empty GameObject. Returns it so callers can add components.</summary>
    public static GameObject EnsureEmptyGameObject(string name, ref int created, ref int found)
    {
        var existing = GameObject.Find(name);
        if (existing != null) { found++; return existing; }

        var go = new GameObject(name);
        Undo.RegisterCreatedObjectUndo(go, $"Create {name}");
        created++;
        return go;
    }

    /// <summary>
    /// Ensures a MonoBehaviour of type T is attached to the given GameObject.
    /// Idempotent — skips if already present. Used after EnsureEmptyGameObject
    /// to attach scene manager scripts (e.g. CreditsManager, HubManager).
    /// </summary>
    public static T EnsureScript<T>(GameObject go) where T : MonoBehaviour
    {
        if (go == null) return null;
        var existing = go.GetComponent<T>();
        if (existing != null) return existing;
        return go.AddComponent<T>();
    }

    // ===================== Canvas =====================

    /// <summary>
    /// Creates a ScreenSpaceOverlay Canvas with CanvasScaler (ScaleWithScreenSize
    /// 1920×1080, match 0.5), GraphicRaycaster, CanvasRenderer, and background Image.
    /// Matches the actual scene pattern where Canvas always has a background Image.
    /// Returns the Canvas RectTransform.
    /// </summary>
    public static RectTransform EnsureCanvas(string name, ref int created, ref int found)
    {
        var existing = GameObject.Find(name);
        if (existing != null)
        {
            found++;
            return existing.GetComponent<RectTransform>();
        }

        var go = new GameObject(name);
        go.layer = LayerMask.NameToLayer("UI");

        var canvas = go.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 0;

        var scaler = go.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);
        scaler.matchWidthOrHeight = 0.5f;

        go.AddComponent<GraphicRaycaster>();
        go.AddComponent<CanvasRenderer>();

        var bgImg = go.AddComponent<Image>();
        bgImg.color = Color.black;
        bgImg.raycastTarget = true;

        Undo.RegisterCreatedObjectUndo(go, $"Create {name}");
        created++;

        return go.GetComponent<RectTransform>();
    }

    // ===================== Common UI Patterns =====================

    /// <summary>Creates a full-screen black Image named "FadeOverlay" as the last child.
    /// Used by SceneHelper.FadeIn/FadeOut for scene transitions.</summary>
    public static void EnsureFadeOverlay(RectTransform canvas, ref int created, ref int found)
    {
        if (canvas == null) return;
        var existing = canvas.Find("FadeOverlay");
        if (existing != null) { found++; return; }

        var go = new GameObject("FadeOverlay");
        go.layer = LayerMask.NameToLayer("UI");
        var rt = go.AddComponent<RectTransform>();
        rt.SetParent(canvas, false);
        StretchFill(rt);

        go.AddComponent<CanvasRenderer>();
        var img = go.AddComponent<Image>();
        img.color = Color.black;
        img.raycastTarget = true;

        rt.SetAsLastSibling();
        Undo.RegisterCreatedObjectUndo(go, "Create FadeOverlay");
        created++;

        // Attach FadeOverlayInstance script
        go.AddComponent<FadeOverlayInstance>();
    }

    /// <summary>
    /// Creates the CutoutOverlay decorative frame used by most scenes.
    /// Exact anchoring matches real scene data:
    ///   CutoutOverlay: stretch-fill, CutoutOverlay component
    ///   Top: anchored to top edge, 130px tall, Image
    ///     LeftPane: left third
    ///     CenterPane: center third
    ///     RightPane: right third
    ///   Bottom: anchored to bottom edge, 94px tall, Image, starts INACTIVE
    /// </summary>
    public static void EnsureCutoutOverlay(RectTransform canvas, ref int created, ref int found)
    {
        if (canvas == null) return;
        var cutout = EnsureRectChild(canvas, "CutoutOverlay", ref created, ref found);
        if (cutout == null) return;

        // Attach CutoutOverlay MonoBehaviour
        EnsureScript<CutoutOverlay>(cutout.gameObject);

        // Top bar — anchored to top of parent, 130px tall
        var top = FindOrCreateUI(cutout, "Top", ref created, ref found);
        if (top != null)
        {
            var topRT = top.GetComponent<RectTransform>();
            topRT.anchorMin = new Vector2(0f, 1f);
            topRT.anchorMax = Vector2.one;
            topRT.pivot = new Vector2(0.5f, 1f);
            topRT.sizeDelta = new Vector2(0f, 130.15384f);
            topRT.anchoredPosition = Vector2.zero;

            // LeftPane — left third
            var lp = FindOrCreateRect(top.GetComponent<RectTransform>(), "LeftPane", ref created, ref found);
            if (lp != null) { lp.anchorMin = new Vector2(0f, 0f); lp.anchorMax = new Vector2(0.3333333f, 1f); lp.pivot = new Vector2(0f, 0.5f); lp.sizeDelta = Vector2.zero; lp.anchoredPosition = Vector2.zero; }

            // CenterPane — center third
            var cp = FindOrCreateRect(top.GetComponent<RectTransform>(), "CenterPane", ref created, ref found);
            if (cp != null) { cp.anchorMin = new Vector2(0.3333333f, 0f); cp.anchorMax = new Vector2(0.6666667f, 1f); cp.sizeDelta = Vector2.zero; cp.anchoredPosition = Vector2.zero; }

            // RightPane — right third
            var rp = FindOrCreateRect(top.GetComponent<RectTransform>(), "RightPane", ref created, ref found);
            if (rp != null) { rp.anchorMin = new Vector2(0.6666667f, 0f); rp.anchorMax = new Vector2(1f, 1f); rp.pivot = new Vector2(1f, 0.5f); rp.sizeDelta = Vector2.zero; rp.anchoredPosition = Vector2.zero; }
        }

        // Bottom bar — anchored to bottom, 94px tall, starts inactive
        var bottom = FindOrCreateUI(cutout, "Bottom", ref created, ref found);
        if (bottom != null)
        {
            var btmRT = bottom.GetComponent<RectTransform>();
            btmRT.anchorMin = Vector2.zero;
            btmRT.anchorMax = new Vector2(1f, 0f);
            btmRT.pivot = new Vector2(0.5f, 0f);
            btmRT.sizeDelta = new Vector2(0f, 94.15384f);
            btmRT.anchoredPosition = Vector2.zero;
            bottom.SetActive(false);
        }
    }

    /// <summary>
    /// Creates a TMP title label anchored to top-center at y=-128, matching real scene data.
    /// Used by ProfileSelect, SaveFileSelect, StageSelect, Settings, Credits scenes.
    /// </summary>
    public static RectTransform EnsureTitle(RectTransform parent, string text, ref int created, ref int found)
    {
        if (parent == null) return null;
        var existing = parent.Find("Title");
        if (existing != null) { found++; return existing.GetComponent<RectTransform>(); }

        var go = new GameObject("Title");
        go.layer = LayerMask.NameToLayer("UI");
        var rt = go.AddComponent<RectTransform>();
        rt.SetParent(parent, false);
        rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 1f);
        rt.pivot = new Vector2(0.5f, 0.5f);
        rt.sizeDelta = Vector2.zero;
        rt.anchoredPosition = new Vector2(0f, -128f);

        go.AddComponent<CanvasRenderer>();
        var tmp = go.AddComponent<TextMeshProUGUI>();
        tmp.text = text;
        tmp.fontSize = 36;
        tmp.color = Color.white;
        tmp.alignment = TextAlignmentOptions.Center;
        tmp.raycastTarget = false;

        Undo.RegisterCreatedObjectUndo(go, "Create Title");
        created++;
        return rt;
    }

    /// <summary>
    /// Creates a BackButton anchored top-left at (120, -200) with 200×64 size
    /// and a Label child. Matches real scene data from ProfileSelect, SaveFileSelect,
    /// StageSelect, Settings, Credits scenes.
    /// </summary>
    public static RectTransform EnsureBackButton(RectTransform parent, ref int created, ref int found)
    {
        if (parent == null) return null;
        var existing = parent.Find("BackButton");
        if (existing != null) { found++; return existing.GetComponent<RectTransform>(); }

        var go = new GameObject("BackButton");
        go.layer = LayerMask.NameToLayer("UI");
        var rt = go.AddComponent<RectTransform>();
        rt.SetParent(parent, false);
        rt.anchorMin = rt.anchorMax = new Vector2(0f, 1f);
        rt.pivot = new Vector2(0.5f, 0.5f);
        rt.sizeDelta = new Vector2(200f, 64f);
        rt.anchoredPosition = new Vector2(120f, -200f);

        go.AddComponent<CanvasRenderer>();
        var img = go.AddComponent<Image>();
        img.color = new Color(0.2f, 0.2f, 0.2f, 0.8f);
        img.raycastTarget = true;
        var btn = go.AddComponent<Button>();
        btn.targetGraphic = img;

        var labelGO = new GameObject("Label");
        labelGO.layer = LayerMask.NameToLayer("UI");
        var labelRT = labelGO.AddComponent<RectTransform>();
        labelRT.SetParent(rt, false);
        labelRT.anchorMin = labelRT.anchorMax = new Vector2(0.5f, 0.5f);
        labelRT.sizeDelta = new Vector2(64f, 64f);
        labelRT.anchoredPosition = Vector2.zero;
        labelGO.AddComponent<CanvasRenderer>();
        var tmp = labelGO.AddComponent<TextMeshProUGUI>();
        tmp.text = "Back";
        tmp.fontSize = 24;
        tmp.color = Color.white;
        tmp.alignment = TextAlignmentOptions.Center;
        tmp.raycastTarget = false;

        Undo.RegisterCreatedObjectUndo(go, "Create BackButton");
        created++;
        return rt;
    }

    /// <summary>
    /// Creates a standard ScrollView matching real scene data.
    /// Root: stretch with sizeDelta=(0,-512) to leave room for title/buttons.
    /// Contains Viewport (with Mask + ScrollRect), Content (VerticalLayoutGroup + ContentSizeFitter),
    /// and Vertical + Horizontal scrollbars with correct anchoring.
    /// </summary>
    public static RectTransform EnsureScrollView(RectTransform parent, ref int created, ref int found)
    {
        if (parent == null) return null;
        var existing = parent.Find("ScrollView");
        if (existing != null) { found++; return existing.GetComponent<RectTransform>(); }

        // Root — stretch with vertical inset
        var svGO = new GameObject("ScrollView");
        svGO.layer = LayerMask.NameToLayer("UI");
        var svRT = svGO.AddComponent<RectTransform>();
        svRT.SetParent(parent, false);
        svRT.anchorMin = Vector2.zero;
        svRT.anchorMax = Vector2.one;
        svRT.sizeDelta = new Vector2(0f, -512f);
        svRT.anchoredPosition = Vector2.zero;

        svGO.AddComponent<CanvasRenderer>();
        var svImg = svGO.AddComponent<Image>();
        svImg.color = new Color(0.1f, 0.1f, 0.12f, 0.5f);
        svImg.raycastTarget = true;

        // Viewport — stretch with -17px right for scrollbar, Mask + ScrollRect
        var vpGO = new GameObject("Viewport");
        vpGO.layer = LayerMask.NameToLayer("UI");
        var vpRT = vpGO.AddComponent<RectTransform>();
        vpRT.SetParent(svRT, false);
        vpRT.anchorMin = Vector2.zero;
        vpRT.anchorMax = Vector2.one;
        vpRT.sizeDelta = new Vector2(-17f, 0f);
        vpRT.pivot = new Vector2(0f, 1f);
        vpRT.anchoredPosition = Vector2.zero;
        vpGO.AddComponent<CanvasRenderer>();
        var vpImg = vpGO.AddComponent<Image>();
        vpImg.raycastTarget = true;
        var mask = vpGO.AddComponent<Mask>();
        mask.showMaskGraphic = false;
        var scrollRect = vpGO.AddComponent<ScrollRect>();

        // Content — anchored to top, VerticalLayoutGroup + ContentSizeFitter
        var contentGO = new GameObject("Content");
        contentGO.layer = LayerMask.NameToLayer("UI");
        var contentRT = contentGO.AddComponent<RectTransform>();
        contentRT.SetParent(vpRT, false);
        contentRT.anchorMin = new Vector2(0f, 1f);
        contentRT.anchorMax = Vector2.one;
        contentRT.pivot = new Vector2(0f, 1f);
        contentRT.sizeDelta = Vector2.zero;
        contentRT.anchoredPosition = Vector2.zero;
        var vlg = contentGO.AddComponent<VerticalLayoutGroup>();
        vlg.childAlignment = TextAnchor.UpperLeft;
        vlg.childControlWidth = true;
        vlg.childControlHeight = false;
        vlg.childForceExpandWidth = true;
        vlg.childForceExpandHeight = false;
        vlg.spacing = 4f;
        var csf = contentGO.AddComponent<ContentSizeFitter>();
        csf.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

        scrollRect.viewport = vpRT;
        scrollRect.content = contentRT;

        // Vertical scrollbar — right edge
        var vBar = CreateScrollbar(svRT, "Scrollbar Vertical", true);
        scrollRect.verticalScrollbar = vBar.GetComponent<Scrollbar>();
        scrollRect.verticalScrollbarVisibility = ScrollRect.ScrollbarVisibility.AutoHideAndExpandViewport;

        // Horizontal scrollbar — bottom edge
        var hBar = CreateScrollbar(svRT, "Scrollbar Horizontal", false);
        scrollRect.horizontalScrollbar = hBar.GetComponent<Scrollbar>();
        scrollRect.horizontalScrollbarVisibility = ScrollRect.ScrollbarVisibility.AutoHideAndExpandViewport;

        Undo.RegisterCreatedObjectUndo(svGO, "Create ScrollView");
        created++;
        return svRT;
    }

    /// <summary>
    /// Creates the 9-slice border frame children used by the PartyManager scene
    /// for StatsDisplay, EquipmentDisplay, and button frames.
    /// Children: Background, Top, Bottom, Left, Right, TopLeft, TopRight, BottomLeft, BottomRight.
    /// </summary>
    public static void EnsureNineSliceFrame(RectTransform parent, ref int created, ref int found)
    {
        if (parent == null) return;
        foreach (var name in new[] { "Background", "Top", "Bottom", "Left", "Right", "TopLeft", "TopRight", "BottomLeft", "BottomRight" })
            EnsureImage(parent, name, true, ref created, ref found);
    }

    // ===================== Primitive Children =====================

    /// <summary>Creates a RectTransform-only child (no visual components).</summary>
    public static RectTransform EnsureRectChild(RectTransform parent, string name, ref int created, ref int found)
    {
        if (parent == null) return null;
        var existing = parent.Find(name);
        if (existing != null) { found++; return existing.GetComponent<RectTransform>(); }

        var go = new GameObject(name);
        go.layer = LayerMask.NameToLayer("UI");
        var rt = go.AddComponent<RectTransform>();
        rt.SetParent(parent, false);
        StretchFill(rt);
        Undo.RegisterCreatedObjectUndo(go, $"Create {name}");
        created++;
        return rt;
    }

    /// <summary>Creates an Image child. If stretch is true, fills the parent.</summary>
    public static RectTransform EnsureImage(RectTransform parent, string name, bool stretch, ref int created, ref int found)
    {
        if (parent == null) return null;
        var existing = parent.Find(name);
        if (existing != null) { found++; return existing.GetComponent<RectTransform>(); }

        var go = new GameObject(name);
        go.layer = LayerMask.NameToLayer("UI");
        var rt = go.AddComponent<RectTransform>();
        rt.SetParent(parent, false);
        if (stretch) StretchFill(rt);

        go.AddComponent<CanvasRenderer>();
        var img = go.AddComponent<Image>();
        img.color = Color.white;
        img.raycastTarget = false;

        Undo.RegisterCreatedObjectUndo(go, $"Create {name}");
        created++;
        return rt;
    }

    /// <summary>Creates a Button with an Image background and a TMP Label child.</summary>
    public static RectTransform EnsureButton(RectTransform parent, string name, string label, ref int created, ref int found)
    {
        if (parent == null) return null;
        var existing = parent.Find(name);
        if (existing != null) { found++; return existing.GetComponent<RectTransform>(); }

        var go = new GameObject(name);
        go.layer = LayerMask.NameToLayer("UI");
        var rt = go.AddComponent<RectTransform>();
        rt.SetParent(parent, false);
        rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0.5f);
        rt.sizeDelta = new Vector2(200f, 52f);

        go.AddComponent<CanvasRenderer>();
        var img = go.AddComponent<Image>();
        img.color = new Color(0.2f, 0.2f, 0.2f, 0.8f);
        img.raycastTarget = true;

        var btn = go.AddComponent<Button>();
        btn.targetGraphic = img;

        // Label child
        var labelGO = new GameObject("Label");
        labelGO.layer = LayerMask.NameToLayer("UI");
        var labelRT = labelGO.AddComponent<RectTransform>();
        labelRT.SetParent(rt, false);
        StretchFill(labelRT);
        labelGO.AddComponent<CanvasRenderer>();
        var tmp = labelGO.AddComponent<TextMeshProUGUI>();
        tmp.text = label;
        tmp.fontSize = 24;
        tmp.color = Color.white;
        tmp.alignment = TextAlignmentOptions.Center;
        tmp.raycastTarget = false;

        Undo.RegisterCreatedObjectUndo(go, $"Create {name}");
        created++;
        return rt;
    }

    /// <summary>Creates a TMP label child.</summary>
    public static RectTransform EnsureLabel(RectTransform parent, string name, string text, ref int created, ref int found)
    {
        if (parent == null) return null;
        var existing = parent.Find(name);
        if (existing != null) { found++; return existing.GetComponent<RectTransform>(); }

        var go = new GameObject(name);
        go.layer = LayerMask.NameToLayer("UI");
        var rt = go.AddComponent<RectTransform>();
        rt.SetParent(parent, false);
        StretchFill(rt);

        go.AddComponent<CanvasRenderer>();
        var tmp = go.AddComponent<TextMeshProUGUI>();
        tmp.text = text;
        tmp.fontSize = 24;
        tmp.color = Color.white;
        tmp.alignment = TextAlignmentOptions.TopLeft;
        tmp.enableWordWrapping = true;
        tmp.raycastTarget = false;

        Undo.RegisterCreatedObjectUndo(go, $"Create {name}");
        created++;
        return rt;
    }

    // ===================== Scene Management =====================

    /// <summary>
    /// Opens the specified scene in the editor before scaffolding.
    /// If the scene file doesn't exist, creates a new empty one.
    /// Prompts to save unsaved changes in the current scene.
    /// Returns false if the user cancels (caller should abort).
    /// </summary>
    public static bool OpenScene(string sceneName)
    {
        string path = $"Assets/Scenes/{sceneName}.unity";

        // Already on the right scene?
        var current = UnityEditor.SceneManagement.EditorSceneManager.GetActiveScene();
        if (current.name == sceneName && current.isLoaded)
            return true;

        // Prompt to save if the current scene has unsaved changes
        if (current.isDirty)
        {
            if (!UnityEditor.SceneManagement.EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
                return false;
        }

        // Open existing scene or create a new empty one
        if (System.IO.File.Exists(path))
        {
            UnityEditor.SceneManagement.EditorSceneManager.OpenScene(path);
        }
        else
        {
            var newScene = UnityEditor.SceneManagement.EditorSceneManager.NewScene(
                UnityEditor.SceneManagement.NewSceneSetup.EmptyScene,
                UnityEditor.SceneManagement.NewSceneMode.Single);
            UnityEditor.SceneManagement.EditorSceneManager.SaveScene(newScene, path);
            Debug.Log($"[SceneScaffold] Created new scene: {path}");
        }

        return true;
    }

    /// <summary>
    /// Destroys ALL root GameObjects in the active scene without a
    /// confirmation dialog. Used by "Clear &amp; Recreate" operations
    /// where the intent is already confirmed by clicking the menu item.
    /// </summary>
    public static void ClearAllRootObjectsSilent()
    {
        var scene = UnityEngine.SceneManagement.SceneManager.GetActiveScene();
        var roots = scene.GetRootGameObjects();
        Undo.SetCurrentGroupName("Clear Scene");
        foreach (var go in roots)
            Undo.DestroyObjectImmediate(go);
        Debug.Log($"[SceneScaffold] Cleared {roots.Length} root object(s) from '{scene.name}'.");
    }

    /// <summary>
    /// Destroys ALL root GameObjects in the active scene.
    /// Used by "Clear Scene" menu items to wipe before re-scaffolding.
    /// Registers Undo so the operation is reversible.
    /// </summary>
    public static void ClearAllRootObjects()
    {
        var scene = UnityEngine.SceneManagement.SceneManager.GetActiveScene();
        var roots = scene.GetRootGameObjects();

        if (roots.Length == 0)
        {
            EditorUtility.DisplayDialog("Clear Scene", "Scene is already empty.", "OK");
            return;
        }

        if (!EditorUtility.DisplayDialog("Clear Scene",
            $"This will destroy {roots.Length} root object(s) in '{scene.name}'.\n\nThis is undoable (Ctrl+Z).\n\nContinue?",
            "Clear", "Cancel"))
            return;

        Undo.SetCurrentGroupName("Clear Scene");
        foreach (var go in roots)
            Undo.DestroyObjectImmediate(go);

        Debug.Log($"[SceneScaffold] Cleared {roots.Length} root object(s) from '{scene.name}'.");
    }

    /// <summary>Logs results and shows a dialog summarizing what was created/found.</summary>
    public static void LogResults(string sceneName, int created, int found)
    {
        Debug.Log($"[{sceneName}Scaffold] Done. {found} already existed, {created} created.");
        if (created > 0)
            EditorUtility.DisplayDialog($"{sceneName} Scaffold",
                $"Created {created} missing object(s).\n{found} already existed.\n\nRemember to save the scene.",
                "OK");
        else
            EditorUtility.DisplayDialog($"{sceneName} Scaffold",
                $"All {found} required objects already exist.\nNo changes needed.",
                "OK");
    }

    // ===================== Internal Helpers =====================

    /// <summary>Sets a RectTransform to stretch-fill its parent.</summary>
    private static void StretchFill(RectTransform rt)
    {
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.offsetMin = Vector2.zero;
        rt.offsetMax = Vector2.zero;
    }

    /// <summary>Finds existing or creates a UI child with CanvasRenderer + Image.</summary>
    private static GameObject FindOrCreateUI(RectTransform parent, string name, ref int created, ref int found)
    {
        if (parent == null) return null;
        var existing = parent.Find(name);
        if (existing != null) { found++; return existing.gameObject; }

        var go = new GameObject(name);
        go.layer = LayerMask.NameToLayer("UI");
        var rt = go.AddComponent<RectTransform>();
        rt.SetParent(parent, false);
        go.AddComponent<CanvasRenderer>();
        var img = go.AddComponent<Image>();
        img.color = Color.white;
        img.raycastTarget = false;
        Undo.RegisterCreatedObjectUndo(go, $"Create {name}");
        created++;
        return go;
    }

    /// <summary>Finds existing or creates a plain RectTransform child. Returns the RectTransform.</summary>
    private static RectTransform FindOrCreateRect(RectTransform parent, string name, ref int created, ref int found)
    {
        if (parent == null) return null;
        var existing = parent.Find(name);
        if (existing != null) { found++; return existing.GetComponent<RectTransform>(); }

        var go = new GameObject(name);
        go.layer = LayerMask.NameToLayer("UI");
        var rt = go.AddComponent<RectTransform>();
        rt.SetParent(parent, false);
        Undo.RegisterCreatedObjectUndo(go, $"Create {name}");
        created++;
        return rt;
    }

    /// <summary>Creates a scrollbar with Sliding Area and Handle.</summary>
    private static RectTransform CreateScrollbar(RectTransform parent, string name, bool vertical)
    {
        var go = new GameObject(name);
        go.layer = LayerMask.NameToLayer("UI");
        var rt = go.AddComponent<RectTransform>();
        rt.SetParent(parent, false);

        // Position scrollbar at edge
        if (vertical)
        {
            rt.anchorMin = new Vector2(1f, 0f);
            rt.anchorMax = Vector2.one;
            rt.pivot = Vector2.one;
            rt.sizeDelta = new Vector2(20f, 0f);
            rt.offsetMin = new Vector2(-20f, 0f);
            rt.offsetMax = Vector2.zero;
        }
        else
        {
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = new Vector2(1f, 0f);
            rt.pivot = Vector2.zero;
            rt.sizeDelta = new Vector2(0f, 20f);
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = new Vector2(0f, 20f);
        }

        go.AddComponent<CanvasRenderer>();
        var bgImg = go.AddComponent<Image>();
        bgImg.color = new Color(0.15f, 0.15f, 0.18f, 0.5f);

        var scrollbar = go.AddComponent<Scrollbar>();
        scrollbar.direction = vertical ? Scrollbar.Direction.BottomToTop : Scrollbar.Direction.LeftToRight;

        // Sliding Area
        var saGO = new GameObject("Sliding Area");
        saGO.layer = LayerMask.NameToLayer("UI");
        var saRT = saGO.AddComponent<RectTransform>();
        saRT.SetParent(rt, false);
        StretchFill(saRT);

        // Handle
        var hGO = new GameObject("Handle");
        hGO.layer = LayerMask.NameToLayer("UI");
        var hRT = hGO.AddComponent<RectTransform>();
        hRT.SetParent(saRT, false);
        StretchFill(hRT);
        hGO.AddComponent<CanvasRenderer>();
        var hImg = hGO.AddComponent<Image>();
        hImg.color = new Color(0.4f, 0.4f, 0.45f, 0.8f);

        scrollbar.handleRect = hRT;
        scrollbar.targetGraphic = hImg;

        return rt;
    }
}
