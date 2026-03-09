using UnityEngine;
using UnityEngine.UI;
using UnityEditor;
using TMPro;

/// <summary>
/// TITLESCREENSCAFFOLD - Editor tool to scaffold the TitleScreen scene.
///
/// PURPOSE:
/// Programmatically creates every GameObject in the TitleScreen scene
/// with exact component configuration matching the authoritative scene file.
///
/// SCENE HIERARCHY (from SceneHierarchies.txt):
/// ```
/// Main Camera ................. Camera(ortho, size=5, depth=-1) + AudioListener
/// EventSystem ................. EventSystem + StandaloneInputModule
/// TitleScreenManager .......... TitleScreenManager (MonoBehaviour)
/// Canvas [L=5] ................ Canvas(mode=Overlay) + CanvasScaler + GraphicRaycaster + CanvasRenderer + Image
///   ├── CutoutOverlay {stretch} CutoutOverlay component
///   │   ├── Top {top-anchor, h=130} CanvasRenderer + Image
///   │   │   ├── LeftPane     (left third)
///   │   │   ├── CenterPane   (center third)
///   │   │   └── RightPane    (right third)
///   │   └── Bottom [OFF] {bottom-anchor, h=94} CanvasRenderer + Image
///   ├── Panel {a=(0,0.5...1,0.5) sz=(0,600)} VerticalLayoutGroup
///   │   ├── Backdrop [OFF] {center, sz=(600,600)} CanvasRenderer + Image
///   │   ├── ContinueButton {sz=(512,128)} CanvasRenderer + Image + Button
///   │   │   └── Label {stretch} CanvasRenderer + TextMeshProUGUI
///   │   ├── LoadGameButton {sz=(512,128)} CanvasRenderer + Image + Button
///   │   │   └── Label {stretch}
///   │   ├── SettingsButton {sz=(512,128)} CanvasRenderer + Image + Button
///   │   │   └── Label {stretch}
///   │   ├── CreditsButton {sz=(512,128)} CanvasRenderer + Image + Button
///   │   │   └── Label {stretch}
///   │   ├── EndlessModeButton {sz=(512,128)} CanvasRenderer + Image + Button
///   │   │   └── Label {stretch}
///   │   └── PartyManagerButton {sz=(512,128)} CanvasRenderer + Image + Button
///   │       └── Label {stretch}
///   ├── ProfileButton {a=(0.5,0) sz=(64,64) pos=(0,110)} CanvasRenderer + Image + Button
///   │   └── Label {a=(0.5,1) sz=(64,0) pos=(0,-100)}
///   └── FadeOverlay {stretch} CanvasRenderer + Image + FadeOverlayInstance
/// ```
///
/// SCENE FLOW: SplashScreen → TitleScreen → ProfileSelect / Game / Settings / etc.
///
/// RELATED FILES:
///   - TitleScreenManager.cs, GameObjectHelper.TitleScreen
///   - ProfileHelper.cs, SceneHelper.cs
/// </summary>
public static class TitleScreenScaffold
{
    private const string SceneName = "TitleScreen";

    private static readonly (string name, string label)[] MenuButtons = {
        ("ContinueButton", "Continue"),
        ("LoadGameButton", "Load Game"),
        ("SettingsButton", "Settings"),
        ("CreditsButton", "Credits"),
        ("EndlessModeButton", "Endless Mode"),
        ("PartyManagerButton", "Party Manager"),
    };

    //[MenuItem("Tools/Scenes/Title Screen/Create Scaffolding")]
    public static void CreateScaffolding()
    {
        if (!ScaffoldHelper.OpenScene(SceneName)) return;
        int created = 0;
        int found = 0;

        ScaffoldHelper.EnsureCamera("Main Camera", ref created, ref found);
        ScaffoldHelper.EnsureEventSystem(ref created, ref found);
        ScaffoldHelper.EnsureEmptyGameObject("TitleScreenManager", ref created, ref found);

        var canvas = ScaffoldHelper.EnsureCanvas("Canvas", ref created, ref found);
        if (canvas != null)
        {
            // CutoutOverlay
            ScaffoldHelper.EnsureCutoutOverlay(canvas, ref created, ref found);

            // Panel — vertically centered, 600px tall, VerticalLayoutGroup
            var panel = ScaffoldHelper.EnsureRectChild(canvas, "Panel", ref created, ref found);
            if (panel != null)
            {
                panel.anchorMin = new Vector2(0f, 0.5f);
                panel.anchorMax = new Vector2(1f, 0.5f);
                panel.sizeDelta = new Vector2(0f, 600f);
                panel.anchoredPosition = Vector2.zero;
                if (panel.GetComponent<VerticalLayoutGroup>() == null)
                {
                    var vlg = panel.gameObject.AddComponent<VerticalLayoutGroup>();
                    vlg.childAlignment = TextAnchor.MiddleCenter;
                    vlg.childControlWidth = false;
                    vlg.childControlHeight = false;
                    vlg.childForceExpandWidth = false;
                    vlg.childForceExpandHeight = false;
                    vlg.spacing = 8f;
                }

                // Backdrop (inactive decorative background)
                var backdrop = ScaffoldHelper.EnsureImage(panel, "Backdrop", false, ref created, ref found);
                if (backdrop != null)
                {
                    backdrop.anchorMin = backdrop.anchorMax = new Vector2(0.5f, 0.5f);
                    backdrop.sizeDelta = new Vector2(600f, 600f);
                    backdrop.gameObject.SetActive(false);
                }

                // Menu buttons — 512×128 each
                foreach (var (btnName, label) in MenuButtons)
                    CreateMenuButton(panel, btnName, label, ref created, ref found);
            }

            // ProfileButton — bottom-center
            var profile = ScaffoldHelper.EnsureButton(canvas, "ProfileButton", "Profile", ref created, ref found);
            if (profile != null)
            {
                profile.anchorMin = profile.anchorMax = new Vector2(0.5f, 0f);
                profile.sizeDelta = new Vector2(64f, 64f);
                profile.anchoredPosition = new Vector2(0f, 110f);
                // Label positioned above
                var lbl = profile.Find("Label");
                if (lbl != null)
                {
                    var lblRT = lbl.GetComponent<RectTransform>();
                    lblRT.anchorMin = lblRT.anchorMax = new Vector2(0.5f, 1f);
                    lblRT.sizeDelta = new Vector2(64f, 0f);
                    lblRT.anchoredPosition = new Vector2(0f, -100f);
                }
            }

            // FadeOverlay
            ScaffoldHelper.EnsureFadeOverlay(canvas, ref created, ref found);
        }

        ScaffoldHelper.LogResults(SceneName, created, found);
    }

    private static void CreateMenuButton(RectTransform parent, string name, string label, ref int created, ref int found)
    {
        var existing = parent.Find(name);
        if (existing != null) { found++; return; }

        var go = new GameObject(name);
        go.layer = LayerMask.NameToLayer("UI");
        var rt = go.AddComponent<RectTransform>();
        rt.SetParent(parent, false);
        rt.anchorMin = rt.anchorMax = Vector2.zero;
        rt.sizeDelta = new Vector2(512f, 128f);

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
        labelRT.anchorMin = Vector2.zero;
        labelRT.anchorMax = Vector2.one;
        labelRT.offsetMin = Vector2.zero;
        labelRT.offsetMax = Vector2.zero;
        labelGO.AddComponent<CanvasRenderer>();
        var tmp = labelGO.AddComponent<TextMeshProUGUI>();
        tmp.text = label;
        tmp.fontSize = 32;
        tmp.color = Color.white;
        tmp.alignment = TextAlignmentOptions.Center;
        tmp.raycastTarget = false;

        Undo.RegisterCreatedObjectUndo(go, $"Create {name}");
        created++;
    }

    //[MenuItem("Tools/Scenes/Title Screen/Clear Scene")]
    public static void ClearScene()
    {
        if (!ScaffoldHelper.OpenScene(SceneName)) return;
        ScaffoldHelper.ClearAllRootObjects();
    }

    [MenuItem("Tools/Scenes/Title Screen/Clear && Recreate")]
    public static void ClearAndRecreate()
    {
        if (!ScaffoldHelper.OpenScene(SceneName)) return;
        ScaffoldHelper.ClearAllRootObjectsSilent();
        CreateScaffolding();
    }
}
