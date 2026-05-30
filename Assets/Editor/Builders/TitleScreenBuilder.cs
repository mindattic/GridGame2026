using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Events;
using UnityEditor;
using TMPro;
using Scripts.Managers;

/// <summary>
/// TITLESCREENSCAFFOLD - Editor tool to builder the TitleScreen scene.
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
public static class TitleScreenBuilder
{
    private const string SceneName = "TitleScreen";

    private static readonly (string name, string label)[] MenuButtons = {
        ("ContinueButton", "Continue"),
        ("LoadGameButton", "Load Game"),
        ("SettingsButton", "Settings"),
        ("CreditsButton", "Credits"),
        ("EndlessModeButton", "Endless Mode"),
        ("PartyManagerButton", "Party Manager"),
        ("BestiaryButton", "Bestiary"),
    };

    //[MenuItem("Tools/Scenes/Title Screen/Create Building")]
    public static void Build()
    {
        if (!SceneBuilderHelper.OpenScene(SceneName)) return;
        int created = 0;
        int found = 0;

        SceneBuilderHelper.EnsureCamera("Main Camera", ref created, ref found);
        SceneBuilderHelper.EnsureEventSystem(ref created, ref found);
        var mgr = SceneBuilderHelper.EnsureEmptyGameObject("TitleScreenManager", ref created, ref found);
        SceneBuilderHelper.EnsureScript<TitleScreenManager>(mgr);

        var canvas = SceneBuilderHelper.EnsureCanvas("Canvas", ref created, ref found);
        if (canvas != null)
        {
            // CutoutOverlay
            SceneBuilderHelper.EnsureCutoutOverlay(canvas, ref created, ref found);

            // Panel — vertically centered, 600px tall, VerticalLayoutGroup
            var panel = SceneBuilderHelper.EnsureRectChild(canvas, "Panel", ref created, ref found);
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
                var backdrop = SceneBuilderHelper.EnsureImage(panel, "Backdrop", false, ref created, ref found);
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
            var profile = SceneBuilderHelper.EnsureButton(canvas, "ProfileButton", "Profile", ref created, ref found, SceneBuilderHelper.SpritePaths.UserIcon);
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
            SceneBuilderHelper.EnsureFadeOverlay(canvas, ref created, ref found);

            // Wire onClick events
            var titleManager = mgr.GetComponent<TitleScreenManager>();
            if (titleManager != null)
            {
                var continueBtn = canvas.Find("Panel/ContinueButton")?.GetComponent<Button>();
                if (continueBtn != null)
                    SceneBuilderHelper.WireOnClick(continueBtn, new UnityAction(titleManager.OnContinueButtonClicked));

                var loadBtn = canvas.Find("Panel/LoadGameButton")?.GetComponent<Button>();
                if (loadBtn != null)
                    SceneBuilderHelper.WireOnClick(loadBtn, new UnityAction(titleManager.OnLoadGameButtonClicked));

                var settingsBtn = canvas.Find("Panel/SettingsButton")?.GetComponent<Button>();
                if (settingsBtn != null)
                    SceneBuilderHelper.WireOnClick(settingsBtn, new UnityAction(titleManager.OnSettingsButtonClicked));

                var creditsBtn = canvas.Find("Panel/CreditsButton")?.GetComponent<Button>();
                if (creditsBtn != null)
                    SceneBuilderHelper.WireOnClick(creditsBtn, new UnityAction(titleManager.OnCreditsButtonClicked));

                var endlessBtn = canvas.Find("Panel/EndlessModeButton")?.GetComponent<Button>();
                if (endlessBtn != null)
                    SceneBuilderHelper.WireOnClick(endlessBtn, new UnityAction(titleManager.OnEndlessModeClicked));

                var partyBtn = canvas.Find("Panel/PartyManagerButton")?.GetComponent<Button>();
                if (partyBtn != null)
                    SceneBuilderHelper.WireOnClick(partyBtn, new UnityAction(titleManager.OnPartyManagerClicked));

                var profileBtn = canvas.Find("ProfileButton")?.GetComponent<Button>();
                if (profileBtn != null)
                    SceneBuilderHelper.WireOnClick(profileBtn, new UnityAction(titleManager.OnChangeProfileButtonClicked));

                // Bestiary button — direct scene-load, no manager method needed.
                // Wrapped: any failure here was getting masked by BuilderAutoRebuild's outer
                // TargetInvocationException, with no inner-exception details surfaced.
                try
                {
                    var bestiaryBtn = canvas.Find("Panel/BestiaryButton")?.GetComponent<Button>();
                    if (bestiaryBtn != null)
                        SceneBuilderHelper.WireOnClick(bestiaryBtn,
                            new UnityAction(() => Scripts.Helpers.SceneHelper.Fade.ToBestiary()));
                }
                catch (System.Exception ex)
                {
                    Debug.LogError($"[TitleScreenBuilder] BestiaryButton wire-up failed: {ex}");
                    throw;
                }
            }
        }

        SceneBuilderHelper.LogResults(SceneName, created, found);
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
        img.sprite = SceneBuilderHelper.LoadSprite(SceneBuilderHelper.SpritePaths.Back512);
        img.color = Color.white;
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
        tmp.font = SceneBuilderHelper.LoadFont(SceneBuilderHelper.FontPaths.Attic);
        tmp.text = label;
        tmp.fontSize = 32;
        tmp.color = Color.white;
        tmp.alignment = TextAlignmentOptions.Center;
        tmp.enableWordWrapping = false;
        tmp.raycastTarget = true;

        Undo.RegisterCreatedObjectUndo(go, $"Create {name}");
        created++;
    }

}
