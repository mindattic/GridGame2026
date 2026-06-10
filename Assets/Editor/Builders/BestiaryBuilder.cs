using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using UnityEditor;
using TMPro;
using Scripts.Hub;

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
    private const string Font_Attic  = SceneBuilderHelper.FontPaths.Attic;
    private const string Font_Body   = SceneBuilderHelper.FontPaths.Outfit; // body = Outfit game-wide

    public static void Build()
    {
        try { BuildInternal(); }
        catch (System.Exception ex)
        {
            // BuilderAutoRebuild wraps this in TargetInvocationException and logs only the outer
            // .Message — surface the full stack + inner here so we can actually see the cause.
            Debug.LogError($"[BestiaryBuilder] Build threw: {ex}");
            throw;
        }
    }

    private static void BuildInternal()
    {
        if (!SceneBuilderHelper.OpenScene(SceneName)) return;
        SceneBuilderHelper.ClearAllRootObjectsSilent();

        var atticFont = SceneBuilderHelper.LoadFont(Font_Attic);
        var bodyFont  = SceneBuilderHelper.LoadFont(Font_Body);

        // ── EventSystem ──
        var es = new GameObject("EventSystem", typeof(EventSystem), typeof(StandaloneInputModule));
        Undo.RegisterCreatedObjectUndo(es, "Create EventSystem");

        // ── Camera (UI-only Bestiary scene) ──
        var camGO = new GameObject("Main Camera", typeof(Camera));
        camGO.tag = "MainCamera";
        var cam = camGO.GetComponent<Camera>();
        cam.clearFlags = CameraClearFlags.SolidColor;
        cam.backgroundColor = HubTheme.HeaderBg * 0.6f;
        cam.orthographic = true;
        // UI-only scene — exclude world geometry. Defensive: if "UI" layer doesn't exist in the
        // project, fall back to "everything" so the camera at least renders something.
        int uiLayer = LayerMask.NameToLayer("UI");
        cam.cullingMask = uiLayer >= 0 ? (1 << uiLayer) : ~0;
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
        bgGO.GetComponent<Image>().color = HubTheme.PanelBg;

        // ── Header (the game-wide standard bar; BestiaryView.TitleLabel = the bar's title TMP) ──
        var header = UiKit.Header(canvasRT, "Bestiary");
        var titleTMP = header.Find("Title").GetComponent<TextMeshProUGUI>();

        // ── Page indicator (in the header, right-aligned) ──
        var pageRT = UiKit.HeaderRightLabel(header, "PageLabel", "1 / 1");
        var pageTMP = pageRT.GetComponent<TextMeshProUGUI>();
        pageTMP.font = bodyFont;
        pageTMP.fontSize = 28;
        pageTMP.color = HubTheme.TextMuted;

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
        var classTMP = AddTMP(classGO, bodyFont, fontSize: 24, color: HubTheme.TextMuted,
            align: TextAlignmentOptions.Center, text: "");

        // ── Stats panel (left side, mid-bottom) ──
        var statsGO = NewUI("Stats", canvasRT,
            anchorMin: new Vector2(0f, 0f), anchorMax: new Vector2(0.5f, 0f),
            pivot: new Vector2(0.5f, 0f), sizeDelta: new Vector2(-40f, 400f), anchoredPos: new Vector2(0f, 280f));
        var statsTMP = AddTMP(statsGO, bodyFont, fontSize: 26, color: HubTheme.TextLight,
            align: TextAlignmentOptions.TopLeft, text: "");

        // ── Abilities panel (right side, mid-bottom) ──
        var abilitiesGO = NewUI("Abilities", canvasRT,
            anchorMin: new Vector2(0.5f, 0f), anchorMax: new Vector2(1f, 0f),
            pivot: new Vector2(0.5f, 0f), sizeDelta: new Vector2(-40f, 400f), anchoredPos: new Vector2(0f, 280f));
        var abilitiesTMP = AddTMP(abilitiesGO, bodyFont, fontSize: 26, color: HubTheme.Accent,
            align: TextAlignmentOptions.TopLeft, text: "");

        // ── Lore (bottom strip) ──
        var loreGO = NewUI("Lore", canvasRT,
            anchorMin: new Vector2(0f, 0f), anchorMax: new Vector2(1f, 0f),
            pivot: new Vector2(0.5f, 0f), sizeDelta: new Vector2(-80f, 240f), anchoredPos: new Vector2(0f, 30f));
        var loreTMP = AddTMP(loreGO, bodyFont, fontSize: 22, color: HubTheme.TextMuted,
            align: TextAlignmentOptions.TopLeft, text: "");
        loreTMP.enableWordWrapping = true;

        // ── Prev / Next buttons ──
        var prev = AddNavButton(canvasRT, "Prev", new Vector2(0f, 0.5f), new Vector2(64f, 64f), atticFont, "<");
        var next = AddNavButton(canvasRT, "Next", new Vector2(1f, 0.5f), new Vector2(-64f, 64f), atticFont, ">");

        // ── Back button — the game-wide standard (bottom-left "← Back"). Wired AFTER
        // BestiaryView is created so we can target view.OnBackButtonClicked (a
        // UnityEngine.Object method) instead of a lambda — persistent UnityEvent listeners
        // can't target closure classes.
        var back = UiKit.BackButton(canvasRT, "Back").GetComponent<Button>();

        // ── BestiaryView controller ──
        var viewGO = new GameObject("BestiaryView", typeof(Scripts.Canvas.BestiaryView));
        viewGO.transform.SetParent(canvasGO.transform, false);
        var view = viewGO.GetComponent<Scripts.Canvas.BestiaryView>();

        SceneBuilderHelper.WireOnClick(back,
            new UnityEngine.Events.UnityAction(view.OnBackButtonClicked));
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
        go.GetComponent<Image>().color = HubTheme.NavIdle;
        var navBtn = go.GetComponent<Button>();
        navBtn.transition = Selectable.Transition.ColorTint;
        navBtn.colors = HubTheme.ButtonColors;

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
