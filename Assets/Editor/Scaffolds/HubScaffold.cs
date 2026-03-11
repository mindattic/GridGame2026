using UnityEngine;
using UnityEngine.UI;
using UnityEditor;
using TMPro;
using Scripts.Hub;

/// <summary>
/// HUBSCAFFOLD - Editor tool to scaffold the Hub scene.
///
/// SCENE HIERARCHY (from SceneHierarchies.txt):
/// ```
/// EventSystem ................ EventSystem + StandaloneInputModule
/// HubManager ................. HubManager
/// Canvas ..................... Canvas(mode=Overlay) + CanvasScaler + GraphicRaycaster
///   ├── ContentPanel {stretch, sz=(0,-80) pos=(0,-40)} CanvasRenderer + Image
///   │   ├── PartyPanel {stretch}   CanvasRenderer + Image + PartySectionController + TiltParallax
///   │   │   ├── RosterList {stretch} VerticalLayoutGroup + HorizontalLayoutGroup
///   │   │   ├── PartyList {stretch}  VerticalLayoutGroup + HorizontalLayoutGroup
///   │   │   ├── GoldLabel {stretch}  TextMeshProUGUI
///   │   │   └── DetailLabel {stretch} TextMeshProUGUI
///   │   ├── ShopPanel {stretch}    CanvasRenderer + Image + ShopSectionController
///   │   │   ├── ItemList {stretch}   VerticalLayoutGroup + HorizontalLayoutGroup
///   │   │   ├── BuyTab {center, sz=(140,48)} Image + Button + Label
///   │   │   ├── SellTab {center, sz=(140,48)} Image + Button + Label
///   │   │   ├── GoldLabel {stretch}  TextMeshProUGUI
///   │   │   └── DetailLabel {stretch} TextMeshProUGUI
///   │   ├── MedicalPanel {stretch} CanvasRenderer + Image + MedicalSectionController
///   │   │   ├── HeroList {stretch}   VerticalLayoutGroup + HorizontalLayoutGroup
///   │   │   ├── GoldLabel {stretch}  TextMeshProUGUI
///   │   │   └── DetailLabel {stretch} TextMeshProUGUI
///   │   ├── ResidencePanel {stretch} CanvasRenderer + Image + ResidenceSectionController
///   │   │   ├── ActionList {stretch} VerticalLayoutGroup + HorizontalLayoutGroup
///   │   │   ├── RecruitList {stretch} VerticalLayoutGroup + HorizontalLayoutGroup
///   │   │   ├── GoldLabel {stretch}  TextMeshProUGUI
///   │   │   └── DetailLabel {stretch} TextMeshProUGUI
///   │   └── BlacksmithPanel {stretch} CanvasRenderer + Image + BlacksmithSectionController
///   │       ├── ItemList {stretch}    VerticalLayoutGroup + HorizontalLayoutGroup
///   │       ├── BuyTab/SellTab/CraftTab/RepairTab/SalvageTab {center, sz=(140,48)} Button+Label
///   │       ├── GoldLabel {stretch}   TextMeshProUGUI
///   │       └── DetailLabel {stretch}  TextMeshProUGUI
///   ├── CutoutOverlay {stretch}
///   ├── Output {a=(0.5,0.5) sz=(1000,1000) pos=(0,1000)} TextMeshProUGUI (debug)
///   ├── NavBar {a=(0,1...1,1) sz=(0,80) pos=(0,-258) pv=(0.5,1)} Image + HorizontalLayoutGroup
///   │   ├── PartyButton {sz=(200,60)} Image + Button + Text child
///   │   ├── ShopButton {sz=(200,60)}
///   │   ├── MedicalButton {sz=(200,60)}
///   │   ├── ResidenceButton {sz=(200,60)}
///   │   ├── BlacksmithButton {sz=(200,60)}
///   │   ├── OverworldButton {sz=(200,60)}
///   │   └── BattleButton {sz=(200,60)}
///   └── FadeOverlay {stretch} Image + FadeOverlayInstance
/// ```
///
/// NOTE: Section panel children (ItemList, GoldLabel, etc.) are also scaffolded by
/// HubSceneValidator (Tools → Hub Scene → Validate &amp; Scaffold Panels).
/// NavBar button labels use "Text" child name (not "Label").
///
/// SCENE FLOW: Overworld → Hub → (sections) → Overworld / Game
///
/// RELATED FILES: HubManager.cs, HubSceneValidator.cs, GameObjectHelper.Hub,
///   ShopSectionController.cs, PartySectionController.cs, etc.
/// </summary>
public static class HubScaffold
{
    private const string SceneName = "Hub";

    private static readonly string[] NavButtons = {
        "PartyButton", "ShopButton", "MedicalButton", "ResidenceButton",
        "BlacksmithButton", "OverworldButton", "BattleButton"
    };

    private static readonly string[] SectionPanels = {
        "PartyPanel", "ShopPanel", "MedicalPanel", "ResidencePanel", "BlacksmithPanel"
    };

    public static void CreateScaffolding()
    {
        if (!SceneScaffoldHelper.OpenScene(SceneName)) return;
        int created = 0;
        int found = 0;

        SceneScaffoldHelper.EnsureEventSystem(ref created, ref found);
        var mgr = SceneScaffoldHelper.EnsureEmptyGameObject("HubManager", ref created, ref found);
        SceneScaffoldHelper.EnsureScript<HubManager>(mgr);

        // Canvas — no background image on Canvas itself
        var canvasGO = GameObject.Find("Canvas");
        RectTransform canvas;
        if (canvasGO != null)
        {
            found++;
            canvas = canvasGO.GetComponent<RectTransform>();
        }
        else
        {
            canvasGO = new GameObject("Canvas");
            canvasGO.layer = LayerMask.NameToLayer("UI");
            var c = canvasGO.AddComponent<Canvas>();
            c.renderMode = RenderMode.ScreenSpaceOverlay;
            var scaler = canvasGO.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f);
            scaler.matchWidthOrHeight = 0.5f;
            canvasGO.AddComponent<GraphicRaycaster>();
            Undo.RegisterCreatedObjectUndo(canvasGO, "Create Canvas");
            created++;
            canvas = canvasGO.GetComponent<RectTransform>();
        }

        if (canvas != null)
        {
            // ContentPanel — stretch with 80px top inset for NavBar
            var content = SceneScaffoldHelper.EnsureImage(canvas, "ContentPanel", false, ref created, ref found);
            if (content != null)
            {
                content.anchorMin = Vector2.zero;
                content.anchorMax = Vector2.one;
                content.sizeDelta = new Vector2(0f, -80f);
                content.anchoredPosition = new Vector2(0f, -40f);

                // Section panels — stretch-fill inside ContentPanel
                foreach (var panel in SectionPanels)
                {
                    var p = SceneScaffoldHelper.EnsureImage(content, panel, true, ref created, ref found);
                    // Children (ItemList, GoldLabel, etc.) are created by HubSceneValidator
                }
            }

            SceneScaffoldHelper.EnsureCutoutOverlay(canvas, ref created, ref found);

            // Output — debug label, positioned off-screen above
            var output = SceneScaffoldHelper.EnsureLabel(canvas, "Output", "", ref created, ref found);
            if (output != null)
            {
                output.anchorMin = output.anchorMax = new Vector2(0.5f, 0.5f);
                output.sizeDelta = new Vector2(1000f, 1000f);
                output.anchoredPosition = new Vector2(0f, 1000f);
            }

            // NavBar — anchored to top, 80px tall, with HorizontalLayoutGroup
            var navBar = SceneScaffoldHelper.EnsureImage(canvas, "NavBar", false, ref created, ref found);
            if (navBar != null)
            {
                navBar.anchorMin = new Vector2(0f, 1f);
                navBar.anchorMax = Vector2.one;
                navBar.pivot = new Vector2(0.5f, 1f);
                navBar.sizeDelta = new Vector2(0f, 80f);
                navBar.anchoredPosition = new Vector2(0f, -257.67f);
                if (navBar.GetComponent<HorizontalLayoutGroup>() == null)
                {
                    var hlg = navBar.gameObject.AddComponent<HorizontalLayoutGroup>();
                    hlg.childAlignment = TextAnchor.MiddleCenter;
                    hlg.childControlWidth = false;
                    hlg.childControlHeight = false;
                    hlg.spacing = 4f;
                }

                foreach (var btnName in NavButtons)
                    CreateNavButton(navBar, btnName, ref created, ref found);
            }

            SceneScaffoldHelper.EnsureFadeOverlay(canvas, ref created, ref found);
        }

        SceneScaffoldHelper.LogResults(SceneName, created, found);
    }

    /// <summary>Creates a 200×60 nav button with "Text" child (Hub uses "Text" not "Label").</summary>
    private static void CreateNavButton(RectTransform parent, string name, ref int created, ref int found)
    {
        var existing = parent.Find(name);
        if (existing != null) { found++; return; }

        var go = new GameObject(name);
        go.layer = LayerMask.NameToLayer("UI");
        var rt = go.AddComponent<RectTransform>();
        rt.SetParent(parent, false);
        rt.anchorMin = rt.anchorMax = Vector2.zero;
        rt.sizeDelta = new Vector2(200f, 60f);

        go.AddComponent<CanvasRenderer>();
        var img = go.AddComponent<Image>();
        img.color = new Color(0.2f, 0.2f, 0.2f, 0.8f);
        img.raycastTarget = true;
        var btn = go.AddComponent<Button>();
        btn.targetGraphic = img;

        var labelGO = new GameObject("Text");
        labelGO.layer = LayerMask.NameToLayer("UI");
        var labelRT = labelGO.AddComponent<RectTransform>();
        labelRT.SetParent(rt, false);
        labelRT.anchorMin = Vector2.zero;
        labelRT.anchorMax = Vector2.one;
        labelRT.offsetMin = Vector2.zero;
        labelRT.offsetMax = Vector2.zero;
        labelGO.AddComponent<CanvasRenderer>();
        var tmp = labelGO.AddComponent<TextMeshProUGUI>();
        tmp.text = name.Replace("Button", "");
        tmp.fontSize = 20;
        tmp.color = Color.white;
        tmp.alignment = TextAlignmentOptions.Center;
        tmp.raycastTarget = false;

        Undo.RegisterCreatedObjectUndo(go, $"Create {name}");
        created++;
    }

    public static void ClearScene()
    {
        if (!SceneScaffoldHelper.OpenScene(SceneName)) return;
        SceneScaffoldHelper.ClearAllRootObjects();
    }

    [MenuItem("Tools/Scenes/Hub/Load")]
    public static void Load()
    {
        if (!EditorUtility.DisplayDialog("Load",
            "Clear the Hub scene and recreate all GameObjects from the scaffold?\n\n" +
            "Any unsaved scene changes will be lost.",
            "Load", "Cancel"))
            return;
        if (!SceneScaffoldHelper.OpenScene(SceneName)) return;
        SceneScaffoldHelper.ClearAllRootObjectsSilent();
        CreateScaffolding();
    }
}
