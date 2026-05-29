using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using UnityEditor;
using TMPro;

/// <summary>
/// BESTIARYBUILDER - The "Bestiary" scene: a swipe-navigable encyclopedia of every actor in
/// <c>ActorLibrary</c>. Header / page indicator / portrait / stats / abilities / lore.
///
/// <para>Scene file is auto-created by <see cref="SceneBuilderHelper.OpenScene"/> if missing —
/// no need to pre-add a blank <c>Bestiary.unity</c>. <see cref="BuilderAutoRebuild"/> picks this
/// up by reflection.</para>
/// </summary>
public static class BestiaryBuilder
{
    private const string SceneName = "Bestiary";
    private const string Font_Attic  = "Assets/Fonts/Attic.asset";
    private const string Font_Avenir = "Assets/Fonts/Avenir.asset";

    public static void Build()
    {
        if (!SceneBuilderHelper.OpenScene(SceneName)) return;
        SceneBuilderHelper.ClearAllRootObjectsSilent();

        var atticFont = SceneBuilderHelper.LoadFont(Font_Attic);
        var bodyFont  = SceneBuilderHelper.LoadFont(Font_Avenir);

        // ── EventSystem ──
        var es = new GameObject("EventSystem", typeof(EventSystem), typeof(StandaloneInputModule));
        Undo.RegisterCreatedObjectUndo(es, "Create EventSystem");

        // ── Camera (UI-only Bestiary scene) ──
        var camGO = new GameObject("Main Camera", typeof(Camera));
        camGO.tag = "MainCamera";
        var cam = camGO.GetComponent<Camera>();
        cam.clearFlags = CameraClearFlags.SolidColor;
        cam.backgroundColor = new Color(0.06f, 0.08f, 0.14f);
        cam.orthographic = true;
        // Fix #8: UI-only scene — exclude world geometry layers so nothing accidentally renders.
        cam.cullingMask = 1 << LayerMask.NameToLayer("UI");
        Undo.RegisterCreatedObjectUndo(camGO, "Create Main Camera");

        // ── Canvas ──
        var canvasGO = new GameObject("Canvas", typeof(RectTransform), typeof(UnityEngine.Canvas),
            typeof(CanvasScaler), typeof(GraphicRaycaster));
        canvasGO.layer = 5;
        var canvas = canvasGO.GetComponent<UnityEngine.Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        var scaler = canvasGO.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1170f, 2532f);
        scaler.matchWidthOrHeight = 0.5f;
        Undo.RegisterCreatedObjectUndo(canvasGO, "Create Canvas");
        var canvasRT = canvasGO.GetComponent<RectTransform>();

        // ── Background ──
        var bgGO = NewUI("Background", canvasRT, anchorMin: Vector2.zero, anchorMax: Vector2.one, addImage: true);
        bgGO.GetComponent<Image>().color = new Color(0.06f, 0.08f, 0.14f, 1f);

        // ── Title ──
        var titleGO = NewUI("Title", canvasRT,
            anchorMin: new Vector2(0f, 1f), anchorMax: new Vector2(1f, 1f),
            pivot: new Vector2(0.5f, 1f), sizeDelta: new Vector2(0f, 120f), anchoredPos: new Vector2(0f, -16f));
        var titleTMP = AddTMP(titleGO, atticFont, fontSize: 64, color: new Color(1f, 0.85f, 0.4f),
            align: TextAlignmentOptions.Center, text: "BESTIARY");

        // ── Page indicator ──
        var pageGO = NewUI("PageLabel", canvasRT,
            anchorMin: new Vector2(0.5f, 1f), anchorMax: new Vector2(0.5f, 1f),
            pivot: new Vector2(0.5f, 1f), sizeDelta: new Vector2(400f, 40f), anchoredPos: new Vector2(0f, -150f));
        var pageTMP = AddTMP(pageGO, bodyFont, fontSize: 28, color: new Color(0.9f, 0.9f, 0.95f),
            align: TextAlignmentOptions.Center, text: "1 / 1");

        // ── Portrait ──
        var portraitGO = NewUI("Portrait", canvasRT,
            anchorMin: new Vector2(0.5f, 0.5f), anchorMax: new Vector2(0.5f, 0.5f),
            pivot: new Vector2(0.5f, 0.5f), sizeDelta: new Vector2(640f, 800f), anchoredPos: new Vector2(0f, 350f),
            addImage: true);
        var portraitImg = portraitGO.GetComponent<Image>();
        portraitImg.preserveAspect = true;
        portraitImg.color = Color.white;

        // ── Name + Class ──
        var nameGO = NewUI("Name", canvasRT,
            anchorMin: new Vector2(0f, 0.5f), anchorMax: new Vector2(1f, 0.5f),
            pivot: new Vector2(0.5f, 0.5f), sizeDelta: new Vector2(0f, 80f), anchoredPos: new Vector2(0f, -120f));
        var nameTMP = AddTMP(nameGO, atticFont, fontSize: 56, color: Color.white,
            align: TextAlignmentOptions.Center, text: "");

        var classGO = NewUI("Class", canvasRT,
            anchorMin: new Vector2(0f, 0.5f), anchorMax: new Vector2(1f, 0.5f),
            pivot: new Vector2(0.5f, 0.5f), sizeDelta: new Vector2(0f, 36f), anchoredPos: new Vector2(0f, -180f));
        var classTMP = AddTMP(classGO, bodyFont, fontSize: 24, color: new Color(0.8f, 0.9f, 1f),
            align: TextAlignmentOptions.Center, text: "");

        // ── Stats panel (left side, mid-bottom) ──
        var statsGO = NewUI("Stats", canvasRT,
            anchorMin: new Vector2(0f, 0f), anchorMax: new Vector2(0.5f, 0f),
            pivot: new Vector2(0.5f, 0f), sizeDelta: new Vector2(-40f, 400f), anchoredPos: new Vector2(0f, 280f));
        var statsTMP = AddTMP(statsGO, bodyFont, fontSize: 26, color: new Color(0.85f, 0.95f, 0.85f),
            align: TextAlignmentOptions.TopLeft, text: "");

        // ── Abilities panel (right side, mid-bottom) ──
        var abilitiesGO = NewUI("Abilities", canvasRT,
            anchorMin: new Vector2(0.5f, 0f), anchorMax: new Vector2(1f, 0f),
            pivot: new Vector2(0.5f, 0f), sizeDelta: new Vector2(-40f, 400f), anchoredPos: new Vector2(0f, 280f));
        var abilitiesTMP = AddTMP(abilitiesGO, bodyFont, fontSize: 26, color: new Color(1f, 0.92f, 0.7f),
            align: TextAlignmentOptions.TopLeft, text: "");

        // ── Lore (bottom strip) ──
        var loreGO = NewUI("Lore", canvasRT,
            anchorMin: new Vector2(0f, 0f), anchorMax: new Vector2(1f, 0f),
            pivot: new Vector2(0.5f, 0f), sizeDelta: new Vector2(-80f, 240f), anchoredPos: new Vector2(0f, 30f));
        var loreTMP = AddTMP(loreGO, bodyFont, fontSize: 22, color: new Color(0.85f, 0.85f, 0.85f),
            align: TextAlignmentOptions.TopLeft, text: "");
        loreTMP.enableWordWrapping = true;

        // ── Prev / Next buttons ──
        var prev = AddNavButton(canvasRT, "Prev", new Vector2(0f, 0.5f), new Vector2(64f, 64f), atticFont, "<");
        var next = AddNavButton(canvasRT, "Next", new Vector2(1f, 0.5f), new Vector2(-64f, 64f), atticFont, ">");

        // ── Back button (returns to TitleScreen) ──
        var back = AddNavButton(canvasRT, "Back", new Vector2(0f, 1f), new Vector2(64f, -64f), atticFont, "←");
        SceneBuilderHelper.WireOnClick(back,
            new UnityEngine.Events.UnityAction(() => Scripts.Helpers.SceneHelper.Fade.ToTitleScreen()));

        // ── BestiaryView controller ──
        var viewGO = new GameObject("BestiaryView", typeof(Scripts.Canvas.BestiaryView));
        viewGO.transform.SetParent(canvasGO.transform, false);
        var view = viewGO.GetComponent<Scripts.Canvas.BestiaryView>();
        view.TitleLabel = titleTMP;
        view.PageLabel  = pageTMP;
        view.NameLabel  = nameTMP;
        view.ClassLabel = classTMP;
        view.PortraitImage = portraitImg;
        view.StatsBlock = statsTMP;
        view.AbilitiesBlock = abilitiesTMP;
        view.LoreBlock = loreTMP;
        view.PrevButton = prev;
        view.NextButton = next;
        Undo.RegisterCreatedObjectUndo(viewGO, "Create BestiaryView");

        UnityEditor.SceneManagement.EditorSceneManager.SaveScene(
            UnityEngine.SceneManagement.SceneManager.GetActiveScene());
    }

    // ── small helpers ──

    private static GameObject NewUI(string name, RectTransform parent,
        Vector2 anchorMin = default, Vector2 anchorMax = default,
        Vector2 pivot = default, Vector2 sizeDelta = default, Vector2 anchoredPos = default,
        bool addImage = false)
    {
        var go = addImage
            ? new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image))
            : new GameObject(name, typeof(RectTransform));
        go.layer = 5;
        var rt = (RectTransform)go.transform;
        rt.SetParent(parent, false);
        rt.anchorMin = anchorMin == default ? new Vector2(0.5f, 0.5f) : anchorMin;
        rt.anchorMax = anchorMax == default ? new Vector2(0.5f, 0.5f) : anchorMax;
        rt.pivot = pivot == default ? new Vector2(0.5f, 0.5f) : pivot;
        rt.sizeDelta = sizeDelta;
        rt.anchoredPosition = anchoredPos;
        Undo.RegisterCreatedObjectUndo(go, $"Create {name}");
        return go;
    }

    private static TMP_Text AddTMP(GameObject host, TMP_FontAsset font, int fontSize, Color color,
        TextAlignmentOptions align, string text)
    {
        var tmp = host.AddComponent<TextMeshProUGUI>();
        if (font != null) tmp.font = font;
        tmp.fontSize = fontSize;
        tmp.color = color;
        tmp.alignment = align;
        tmp.text = text;
        tmp.raycastTarget = false;
        return tmp;
    }

    private static Button AddNavButton(RectTransform canvasRT, string name, Vector2 anchor,
        Vector2 anchoredPos, TMP_FontAsset font, string label)
    {
        var go = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(Button));
        go.layer = 5;
        var rt = (RectTransform)go.transform;
        rt.SetParent(canvasRT, false);
        rt.anchorMin = anchor; rt.anchorMax = anchor;
        rt.pivot = new Vector2(0.5f, 0.5f);
        rt.sizeDelta = new Vector2(96f, 96f);
        rt.anchoredPosition = anchoredPos;
        go.GetComponent<Image>().color = new Color(0.15f, 0.20f, 0.30f, 0.95f);

        var labelGO = new GameObject("Label", typeof(RectTransform), typeof(CanvasRenderer));
        var lrt = (RectTransform)labelGO.transform;
        lrt.SetParent(go.transform, false);
        lrt.anchorMin = Vector2.zero; lrt.anchorMax = Vector2.one;
        lrt.offsetMin = Vector2.zero; lrt.offsetMax = Vector2.zero;
        var ltmp = labelGO.AddComponent<TextMeshProUGUI>();
        if (font != null) ltmp.font = font;
        ltmp.fontSize = 48;
        ltmp.color = Color.white;
        ltmp.alignment = TextAlignmentOptions.Center;
        ltmp.text = label;
        ltmp.raycastTarget = false;

        Undo.RegisterCreatedObjectUndo(go, $"Create {name}");
        return go.GetComponent<Button>();
    }
}
