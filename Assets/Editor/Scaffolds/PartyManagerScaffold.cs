using UnityEngine;
using UnityEngine.UI;
using UnityEditor;
using TMPro;

/// <summary>
/// PARTYMANAGERSCAFFOLD - Editor tool to scaffold the PartyManager scene.
///
/// SCENE HIERARCHY (from SceneHierarchies.txt):
/// ```
/// Main Camera ................ Camera(ortho, size=5, depth=-1) + AudioListener
/// EventSystem ................ EventSystem + StandaloneInputModule
/// Background [OFF] ........... SpriteRenderer (starts inactive)
/// PartyManager ............... PartyManager (MonoBehaviour)
/// Canvas [L=5] ............... Canvas(mode=Overlay) + CanvasScaler + GraphicRaycaster + CanvasRenderer + Image
///   ├── CutoutOverlay {stretch}
///   ├── Title {a=(0.5,1) pos=(0,-1300)} TextMeshProUGUI
///   ├── RosterCarousel {a=(0,0...1,0) sz=(0,1000) pos=(0,1000) pv=(0.5,1)} Image
///   │   └── Panel {stretch} Image
///   ├── StatsDisplay {a=(0.5,0.5) sz=(400,400) pos=(-256,600)} ScalableControl
///   │   ├── (9-slice frame: Background, Top, Bottom, Left, Right, corners)
///   │   └── Panel {a=(0.5,0.5) pos=(-32,153)} Image
///   │       ├── HP {a=(0,1) pos=(0,-50)}  — Label + Value + Bar (Back/Fill/Front[OFF])
///   │       ├── STR {a=(0,1) pos=(0,-100)}
///   │       ├── VIT {a=(0,1) pos=(0,-150)}
///   │       ├── AGI {a=(0,1) pos=(0,-200)}
///   │       ├── SPD/WIS/STA/INT {a=(0,1) pos=(0,-250)}
///   │       ├── LCK {a=(0,1) pos=(0,-300)}
///   │       └── LVL {a=(0,1) pos=(0,0)}
///   ├── EquipmentDisplay {a=(0.5,0.5) sz=(400,400) pos=(256,600)} ScalableControl
///   │   ├── (9-slice frame)
///   │   └── Panel {a=(0,1) pos=(-32,0)} Image
///   │       ├── Weapons {a=(0,1) sz=(214,128) pos=(48,-128)} — Label + Dropdown
///   │       └── Armor {a=(0,1) sz=(214,128) pos=(48,-215)} — Label + Dropdown
///   ├── AddRemovePartyMemberButton {a=(0.5,0.5) sz=(560,128)} Image + Button + ScalableControl
///   │   ├── (9-slice frame)
///   │   └── Label {a=(inset)}
///   ├── PartyMemberCountLabel {a=(1,1) sz=(120,120) pos=(-70.5,-162.1)} TextMeshProUGUI
///   ├── TestTooltip {a=(0.5,0.5) sz=(160,30) pos=(0,137.53)} Image + Button
///   │   └── Text (TMP) {stretch}
///   ├── BackButton {a=(0,1) sz=(128,64) pos=(137.63,-171.1)} Image + Button
///   │   └── Label {a=(0.5,0.5)}
///   └── FadeOverlay {stretch} Image + FadeOverlayInstance
/// ```
///
/// STAT BAR PATTERN (repeated for each stat):
///   StatName {a=(0,1) pos=(0,yOffset)}
///     ├── Label {a=(0,1) sz=(100,32) pos=(-100,-2)} TextMeshProUGUI
///     ├── Value {a=(0,1) sz=(100,32) pos=(165,0)} TextMeshProUGUI
///     └── Bar {a=(0,1) sz=(100,32)} CanvasRenderer
///         ├── Back {a=(0,0.5) sz=(200,32) pv=(0,0.5)} Image
///         ├── Fill {a=(0,0.5) sz=(200,32) pv=(0,0.5)} Image
///         └── Front [OFF] {a=(0,0.5) sz=(200,32) pos=(0,16) pv=(0,0.5)} Image
///
/// SCENE FLOW: TitleScreen → PartyManager → TitleScreen
///
/// RELATED FILES: PartyManager.cs, GameObjectHelper.PartyManager,
///   HeroLoadout.cs, ActorStats.cs, ScalableControl.cs
/// </summary>
public static class PartyManagerScaffold
{
    private const string SceneName = "PartyManager";

    private static readonly (string name, float yPos)[] Stats = {
        ("HP",  -50f),
        ("STR", -100f),
        ("VIT", -150f),
        ("AGI", -200f),
        ("SPD", -250f),
        ("STA", -250f),
        ("INT", -250f),
        ("WIS", -250f),
        ("LCK", -300f),
        ("LVL", 0f),
    };

    [MenuItem("Tools/Scenes/Party Manager/Create Scaffolding")]
    public static void CreateScaffolding()
    {
        if (!SceneScaffoldHelper.OpenScene(SceneName)) return;
        int created = 0;
        int found = 0;

        SceneScaffoldHelper.EnsureCamera("Main Camera", ref created, ref found);
        SceneScaffoldHelper.EnsureEventSystem(ref created, ref found);

        // Background (SpriteRenderer, starts inactive)
        var bgGO = GameObject.Find("Background");
        if (bgGO != null) { found++; }
        else
        {
            bgGO = new GameObject("Background");
            bgGO.AddComponent<SpriteRenderer>();
            bgGO.SetActive(false);
            Undo.RegisterCreatedObjectUndo(bgGO, "Create Background");
            created++;
        }

        SceneScaffoldHelper.EnsureEmptyGameObject("PartyManager", ref created, ref found);

        var canvas = SceneScaffoldHelper.EnsureCanvas("Canvas", ref created, ref found);
        if (canvas != null)
        {
            SceneScaffoldHelper.EnsureCutoutOverlay(canvas, ref created, ref found);

            // Title — offset far down
            var title = SceneScaffoldHelper.EnsureTitle(canvas, "Party Manager", ref created, ref found);
            if (title != null) title.anchoredPosition = new Vector2(0f, -1300f);

            // RosterCarousel — bottom-anchored, 1000px tall
            var carousel = SceneScaffoldHelper.EnsureImage(canvas, "RosterCarousel", false, ref created, ref found);
            if (carousel != null)
            {
                carousel.anchorMin = Vector2.zero;
                carousel.anchorMax = new Vector2(1f, 0f);
                carousel.pivot = new Vector2(0.5f, 1f);
                carousel.sizeDelta = new Vector2(0f, 1000f);
                carousel.anchoredPosition = new Vector2(0f, 1000f);
                var panel = SceneScaffoldHelper.EnsureImage(carousel, "Panel", true, ref created, ref found);
            }

            // StatsDisplay
            var statsDisplay = SceneScaffoldHelper.EnsureRectChild(canvas, "StatsDisplay", ref created, ref found);
            if (statsDisplay != null)
            {
                statsDisplay.anchorMin = statsDisplay.anchorMax = new Vector2(0.5f, 0.5f);
                statsDisplay.sizeDelta = new Vector2(400f, 400f);
                statsDisplay.anchoredPosition = new Vector2(-256f, 600f);
                if (statsDisplay.GetComponent<CanvasRenderer>() == null) statsDisplay.gameObject.AddComponent<CanvasRenderer>();
                SceneScaffoldHelper.EnsureNineSliceFrame(statsDisplay, ref created, ref found);
                var statsPanel = SceneScaffoldHelper.EnsureImage(statsDisplay, "Panel", false, ref created, ref found);
                if (statsPanel != null)
                {
                    statsPanel.anchorMin = statsPanel.anchorMax = new Vector2(0.5f, 0.5f);
                    statsPanel.anchoredPosition = new Vector2(-32f, 153.09f);
                    statsPanel.sizeDelta = Vector2.zero;
                    foreach (var (statName, yPos) in Stats)
                        CreateStatRow(statsPanel, statName, yPos, ref created, ref found);
                }
            }

            // EquipmentDisplay
            var equipDisplay = SceneScaffoldHelper.EnsureRectChild(canvas, "EquipmentDisplay", ref created, ref found);
            if (equipDisplay != null)
            {
                equipDisplay.anchorMin = equipDisplay.anchorMax = new Vector2(0.5f, 0.5f);
                equipDisplay.sizeDelta = new Vector2(400f, 400f);
                equipDisplay.anchoredPosition = new Vector2(256f, 600f);
                if (equipDisplay.GetComponent<CanvasRenderer>() == null) equipDisplay.gameObject.AddComponent<CanvasRenderer>();
                SceneScaffoldHelper.EnsureNineSliceFrame(equipDisplay, ref created, ref found);
                var equipPanel = SceneScaffoldHelper.EnsureImage(equipDisplay, "Panel", false, ref created, ref found);
                if (equipPanel != null)
                {
                    equipPanel.anchorMin = equipPanel.anchorMax = new Vector2(0f, 1f);
                    equipPanel.anchoredPosition = new Vector2(-32f, 0f);
                    equipPanel.sizeDelta = Vector2.zero;
                    CreateEquipSlot(equipPanel, "Weapons", -128f, ref created, ref found);
                    CreateEquipSlot(equipPanel, "Armor", -215f, ref created, ref found);
                }
            }

            // AddRemovePartyMemberButton — 560×128, center
            var addRemove = SceneScaffoldHelper.EnsureButton(canvas, "AddRemovePartyMemberButton", "Add to Party", ref created, ref found);
            if (addRemove != null)
            {
                addRemove.anchorMin = addRemove.anchorMax = new Vector2(0.5f, 0.5f);
                addRemove.sizeDelta = new Vector2(560f, 128f);
                addRemove.anchoredPosition = Vector2.zero;
                SceneScaffoldHelper.EnsureNineSliceFrame(addRemove, ref created, ref found);
            }

            // PartyMemberCountLabel — top-right
            var countLabel = SceneScaffoldHelper.EnsureLabel(canvas, "PartyMemberCountLabel", "Party: 0/4", ref created, ref found);
            if (countLabel != null)
            {
                countLabel.anchorMin = countLabel.anchorMax = new Vector2(1f, 1f);
                countLabel.sizeDelta = new Vector2(120f, 120f);
                countLabel.anchoredPosition = new Vector2(-70.5f, -162.1f);
            }

            // TestTooltip — debug
            var tooltip = SceneScaffoldHelper.EnsureButton(canvas, "TestTooltip", "", ref created, ref found);
            if (tooltip != null)
            {
                tooltip.anchorMin = tooltip.anchorMax = new Vector2(0.5f, 0.5f);
                tooltip.sizeDelta = new Vector2(160f, 30f);
                tooltip.anchoredPosition = new Vector2(0f, 137.53f);
            }

            // BackButton — 128×64 at top-left
            var back = SceneScaffoldHelper.EnsureButton(canvas, "BackButton", "Back", ref created, ref found);
            if (back != null)
            {
                back.anchorMin = back.anchorMax = new Vector2(0f, 1f);
                back.sizeDelta = new Vector2(128f, 64f);
                back.anchoredPosition = new Vector2(137.63f, -171.1f);
            }

            SceneScaffoldHelper.EnsureFadeOverlay(canvas, ref created, ref found);
        }

        SceneScaffoldHelper.LogResults(SceneName, created, found);
    }

    /// <summary>Creates a stat row: Label + Value + Bar (Back/Fill/Front[OFF]).</summary>
    private static void CreateStatRow(RectTransform parent, string statName, float yPos, ref int created, ref int found)
    {
        var existing = parent.Find(statName);
        if (existing != null) { found++; return; }

        var go = new GameObject(statName);
        go.layer = LayerMask.NameToLayer("UI");
        var rt = go.AddComponent<RectTransform>();
        rt.SetParent(parent, false);
        rt.anchorMin = rt.anchorMax = new Vector2(0f, 1f);
        rt.sizeDelta = Vector2.zero;
        rt.anchoredPosition = new Vector2(0f, yPos);

        // Label
        CreateTMPChild(rt, "Label", statName, new Vector2(-100f, -2f), new Vector2(100f, 32f));
        // Value
        CreateTMPChild(rt, "Value", "0", new Vector2(165f, 0f), new Vector2(100f, 32f));
        // Bar
        var barGO = new GameObject("Bar");
        barGO.layer = LayerMask.NameToLayer("UI");
        var barRT = barGO.AddComponent<RectTransform>();
        barRT.SetParent(rt, false);
        barRT.anchorMin = barRT.anchorMax = new Vector2(0f, 1f);
        barRT.sizeDelta = new Vector2(100f, 32f);
        barRT.anchoredPosition = Vector2.zero;
        barGO.AddComponent<CanvasRenderer>();

        CreateBarImage(barRT, "Back", Vector2.zero);
        CreateBarImage(barRT, "Fill", Vector2.zero);
        var front = CreateBarImage(barRT, "Front", new Vector2(0f, 16f));
        if (front != null) front.gameObject.SetActive(false);

        Undo.RegisterCreatedObjectUndo(go, $"Create {statName}");
        created++;
    }

    private static void CreateTMPChild(RectTransform parent, string name, string text, Vector2 pos, Vector2 size)
    {
        var go = new GameObject(name);
        go.layer = LayerMask.NameToLayer("UI");
        var rt = go.AddComponent<RectTransform>();
        rt.SetParent(parent, false);
        rt.anchorMin = rt.anchorMax = new Vector2(0f, 1f);
        rt.sizeDelta = size;
        rt.anchoredPosition = pos;
        go.AddComponent<CanvasRenderer>();
        var tmp = go.AddComponent<TextMeshProUGUI>();
        tmp.text = text;
        tmp.fontSize = 18;
        tmp.color = Color.white;
        tmp.alignment = TextAlignmentOptions.Left;
        tmp.raycastTarget = false;
    }

    private static RectTransform CreateBarImage(RectTransform parent, string name, Vector2 pos)
    {
        var go = new GameObject(name);
        go.layer = LayerMask.NameToLayer("UI");
        var rt = go.AddComponent<RectTransform>();
        rt.SetParent(parent, false);
        rt.anchorMin = rt.anchorMax = new Vector2(0f, 0.5f);
        rt.pivot = new Vector2(0f, 0.5f);
        rt.sizeDelta = new Vector2(200f, 32f);
        rt.anchoredPosition = pos;
        go.AddComponent<CanvasRenderer>();
        go.AddComponent<Image>();
        return rt;
    }

    private static void CreateEquipSlot(RectTransform parent, string name, float yPos, ref int created, ref int found)
    {
        var existing = parent.Find(name);
        if (existing != null) { found++; return; }

        var go = new GameObject(name);
        go.layer = LayerMask.NameToLayer("UI");
        var rt = go.AddComponent<RectTransform>();
        rt.SetParent(parent, false);
        rt.anchorMin = rt.anchorMax = new Vector2(0f, 1f);
        rt.pivot = new Vector2(0f, 0.5f);
        rt.sizeDelta = new Vector2(213.91f, 128f);
        rt.anchoredPosition = new Vector2(48f, yPos);
        go.AddComponent<CanvasRenderer>();

        CreateTMPChild(rt, "Label", name, new Vector2(105f, 12f), new Vector2(200f, 50f));

        // Dropdown placeholder (TMP_Dropdown is complex — stub with RectTransform)
        var ddGO = new GameObject("Dropdown");
        ddGO.layer = LayerMask.NameToLayer("UI");
        var ddRT = ddGO.AddComponent<RectTransform>();
        ddRT.SetParent(rt, false);
        ddRT.anchorMin = ddRT.anchorMax = new Vector2(0f, 1f);
        ddRT.pivot = new Vector2(0f, 0.5f);
        ddRT.sizeDelta = new Vector2(370f, 48f);
        ddRT.anchoredPosition = new Vector2(4f, -16f);
        ddGO.AddComponent<CanvasRenderer>();
        ddGO.AddComponent<Image>();

        Undo.RegisterCreatedObjectUndo(go, $"Create {name}");
        created++;
    }

    [MenuItem("Tools/Scenes/Party Manager/Clear Scene")]
    public static void ClearScene()
    {
        if (!SceneScaffoldHelper.OpenScene(SceneName)) return;
        SceneScaffoldHelper.ClearAllRootObjects();
    }

    [MenuItem("Tools/Scenes/Party Manager/Clear && Recreate")]
    public static void ClearAndRecreate()
    {
        if (!SceneScaffoldHelper.OpenScene(SceneName)) return;
        SceneScaffoldHelper.ClearAllRootObjectsSilent();
        CreateScaffolding();
    }
}
