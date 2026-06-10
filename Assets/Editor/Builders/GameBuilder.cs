using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Events;
using UnityEngine.EventSystems;
using UnityEditor;
using TMPro;

public static class GameBuilder
{
    private const string SceneName = "Game";

    // Sprite asset paths
    private const string Sprite_action_bar_1 = "Assets/Sprites/ActionBar/action-bar-1.png";
    private const string Sprite_action_bar_back_1 = "Assets/Sprites/ActionBar/action-bar-back-1.png";
    private const string Sprite_Mannequin = "Assets/Sprites/Actor/Portraits/Mannequin.png";
    private const string Sprite_Black32x32 = "Assets/Sprites/Black32x32.png";
    private const string Sprite_Coin = "Assets/Sprites/Coin.png";
    private const string Sprite_ArrowLeft = "Assets/Sprites/GUI/ArrowLeft.png";
    private const string Sprite_ArrowRight = "Assets/Sprites/GUI/ArrowRight.png";
    private const string Sprite_Back_1024x256 = "Assets/Sprites/GUI/Back.1024x256.png";
    private const string Sprite_Back_512x128 = "Assets/Sprites/GUI/Back.512x128.png";
    private const string Sprite_Button_128x64 = "Assets/Sprites/GUI/Button.128x64.png";
    private const string Sprite_Cancel = "Assets/Sprites/GUI/Cancel.png";
    private const string Sprite_Confirm = "Assets/Sprites/GUI/Confirm.png";
    private const string Sprite_Button_Bottom = "Assets/Sprites/GUI/ScalableButton/Button.Bottom.png";
    private const string Sprite_Pause = "Assets/Sprites/Pause.png";
    private const string Sprite_TitleBar = "Assets/Sprites/TitleBar.png";
    private const string Sprite_Transparent32x32 = "Assets/Sprites/Transparent32x32.png";

    // ── 15-row HUD layout grid (canvas 1170×2532; each row ~169px tall) ──
    // Row 1: Money (right) | Row 2: Timeline | Row 3: ActionTitle
    // Rows 4–12: 6×8 Board (world-space, camera-framed, not a canvas element)
    // Row 13: 6-slot ability bar | Row 14: 12-orb mana line | Row 15: Character card
    private const float Hud_CanvasHeight    = 2532f;
    private const float Hud_RowHeight       = Hud_CanvasHeight / 15f;  // ≈168.8
    private const float Hud_Row1Y_FromTop   = -Hud_RowHeight * 0.5f;   // ≈-84
    private const float Hud_Row2Y_FromTop   = -Hud_RowHeight * 1.5f;   // ≈-253
    private const float Hud_Row3Y_FromTop   = -Hud_RowHeight * 2.5f;   // ≈-422
    private const float Hud_Row13Y_FromBot  =  Hud_RowHeight * 2.5f;   // ≈ 422
    private const float Hud_Row14Y_FromBot  =  Hud_RowHeight * 1.5f;   // ≈ 253
    private const float Hud_Row15Y_FromBot  =  Hud_RowHeight * 0.5f;   // ≈  84
    // Center-pivot canvas: convert "from top" to canvas-center-relative Y for elements anchored
    // at (0.5,0.5) — Y_center = (CanvasHeight/2) + fromTop  (fromTop is already negative).
    private const float Hud_Row2Y_Centered  = Hud_CanvasHeight * 0.5f + Hud_Row2Y_FromTop; // ≈1013

    // Font asset paths — two-font system: Attic = display, Outfit = body (UiFonts.cs).
    // The legacy Avenir / LiberationSans HUD stragglers were unified 2026-06-09 (US-123).
    private const string Font_Attic = "Assets/Fonts/Attic.asset";
    private const string Font_Body = "Assets/Fonts/Outfit.asset";

    public static void Build()
    {
        if (!SceneBuilderHelper.OpenScene(SceneName)) return;

        // Clear existing roots first so the rebuild is canonical and warning-free
        // (matches every other builder + CliEntryPoints; US-002 — kills the
        // "object already exists" warning spam that hides real Console errors).
        SceneBuilderHelper.ClearAllRootObjectsSilent();

        // --- Background ---
        var go_Background = new GameObject("Background");
        go_Background.layer = 5;
        go_Background.AddComponent<SpriteRenderer>();
        go_Background.AddComponent<Scripts.Instances.BackgroundInstance>();
        Undo.RegisterCreatedObjectUndo(go_Background, "Create Background");

        // --- Board ---
        var go_Board = new GameObject("Board");
        go_Board.layer = 6;
        go_Board.AddComponent<Scripts.Instances.Board.BoardInstance>();
        Undo.RegisterCreatedObjectUndo(go_Board, "Create Board");

        // --- BoardOverlay ---
        var go_BoardOverlay = new GameObject("BoardOverlay");
        go_BoardOverlay.transform.SetParent(go_Board.transform, false);
        go_BoardOverlay.AddComponent<Scripts.Instances.Board.BoardOverlay>();
        Undo.RegisterCreatedObjectUndo(go_BoardOverlay, "Create BoardOverlay");

        // --- Canvas ---
        var go_Canvas = new GameObject("Canvas");
        go_Canvas.layer = 5;
        go_Canvas.AddComponent<RectTransform>();
        var canvas_Canvas = go_Canvas.AddComponent<Canvas>();
        canvas_Canvas.renderMode = (RenderMode)0;
        go_Canvas.AddComponent<GraphicRaycaster>();
        var scaler_Canvas = go_Canvas.AddComponent<CanvasScaler>();
        scaler_Canvas.uiScaleMode = (CanvasScaler.ScaleMode)0;
        scaler_Canvas.referenceResolution = new Vector2(0f, 0f);
        scaler_Canvas.matchWidthOrHeight = 0f;
        go_Canvas.AddComponent<CanvasRenderer>();
        Undo.RegisterCreatedObjectUndo(go_Canvas, "Create Canvas");

        // --- PauseButton ---
        var go_PauseButton = new GameObject("PauseButton");
        go_PauseButton.layer = 5;
        var rt_PauseButton = go_PauseButton.AddComponent<RectTransform>();
        rt_PauseButton.SetParent(go_Canvas.GetComponent<RectTransform>(), false);
        rt_PauseButton.anchorMin = new Vector2(1f, 0.5f);
        rt_PauseButton.anchorMax = new Vector2(1f, 0.5f);
        rt_PauseButton.pivot = new Vector2(0.5f, 0.5f);
        rt_PauseButton.sizeDelta = new Vector2(48f, 48f);
        rt_PauseButton.anchoredPosition = new Vector2(-100f, 1100f);
        go_PauseButton.AddComponent<CanvasRenderer>();
        var img_PauseButton = go_PauseButton.AddComponent<Image>();
        img_PauseButton.sprite = SceneBuilderHelper.LoadSprite(Sprite_Pause);
        img_PauseButton.color = new Color(1f, 1f, 1f, 1f);
        img_PauseButton.raycastTarget = true;
        var btn_PauseButton = go_PauseButton.AddComponent<Button>();
        btn_PauseButton.navigation = new Navigation { mode = (Navigation.Mode)3 };
        btn_PauseButton.targetGraphic = go_PauseButton.GetComponent<Image>();
        Undo.RegisterCreatedObjectUndo(go_PauseButton, "Create PauseButton");

        // --- Label ---
        var go_Label5 = new GameObject("Label");
        go_Label5.layer = 5;
        var rt_Label5 = go_Label5.AddComponent<RectTransform>();
        rt_Label5.SetParent(go_PauseButton.GetComponent<RectTransform>(), false);
        rt_Label5.anchorMin = new Vector2(0f, 0f);
        rt_Label5.anchorMax = new Vector2(1f, 1f);
        rt_Label5.pivot = new Vector2(0.5f, 0.5f);
        rt_Label5.sizeDelta = new Vector2(0f, 0f);
        rt_Label5.anchoredPosition = new Vector2(0f, 0f);
        go_Label5.AddComponent<CanvasRenderer>();
        var tmp_Label5 = go_Label5.AddComponent<TextMeshProUGUI>();
        tmp_Label5.font = SceneBuilderHelper.LoadFont(Font_Body);
        tmp_Label5.text = ""; // pause icon is the sprite; the legacy garbage label text is gone
        tmp_Label5.fontSize = 24f;
        tmp_Label5.color = new Color(0.1960784f, 0.1960784f, 0.1960784f, 1f);
        tmp_Label5.alignment = (TextAlignmentOptions)514;
        tmp_Label5.enableWordWrapping = true;
        tmp_Label5.raycastTarget = true;
        Undo.RegisterCreatedObjectUndo(go_Label5, "Create Label");

        // --- CutoutOverlay ---
        var go_CutoutOverlay = new GameObject("CutoutOverlay");
        go_CutoutOverlay.layer = 5;
        var rt_CutoutOverlay = go_CutoutOverlay.AddComponent<RectTransform>();
        rt_CutoutOverlay.SetParent(go_Canvas.GetComponent<RectTransform>(), false);
        rt_CutoutOverlay.anchorMin = new Vector2(0f, 0f);
        rt_CutoutOverlay.anchorMax = new Vector2(1f, 1f);
        rt_CutoutOverlay.pivot = new Vector2(0.5f, 0.5f);
        rt_CutoutOverlay.sizeDelta = new Vector2(0f, 0f);
        rt_CutoutOverlay.anchoredPosition = new Vector2(0f, 0f);
        go_CutoutOverlay.AddComponent<Scripts.Canvas.CutoutOverlay>();
        Undo.RegisterCreatedObjectUndo(go_CutoutOverlay, "Create CutoutOverlay");

        // --- Bottom ---
        var go_Bottom = new GameObject("Bottom");
        var rt_Bottom = go_Bottom.AddComponent<RectTransform>();
        rt_Bottom.SetParent(go_CutoutOverlay.GetComponent<RectTransform>(), false);
        rt_Bottom.anchorMin = new Vector2(0f, 0f);
        rt_Bottom.anchorMax = new Vector2(1f, 0f);
        rt_Bottom.pivot = new Vector2(0.5f, 0f);
        rt_Bottom.sizeDelta = new Vector2(0f, 94.15384f);
        rt_Bottom.anchoredPosition = new Vector2(0f, 0f);
        go_Bottom.AddComponent<CanvasRenderer>();
        var img_Bottom = go_Bottom.AddComponent<Image>();
        img_Bottom.sprite = SceneBuilderHelper.LoadSprite(Sprite_Black32x32);
        img_Bottom.color = new Color(1f, 1f, 1f, 1f);
        img_Bottom.raycastTarget = false;
        go_Bottom.SetActive(false);
        Undo.RegisterCreatedObjectUndo(go_Bottom, "Create Bottom");

        // --- Top ---
        var go_Top = new GameObject("Top");
        var rt_Top = go_Top.AddComponent<RectTransform>();
        rt_Top.SetParent(go_CutoutOverlay.GetComponent<RectTransform>(), false);
        rt_Top.anchorMin = new Vector2(0f, 1f);
        rt_Top.anchorMax = new Vector2(1f, 1f);
        rt_Top.pivot = new Vector2(0.5f, 1f);
        rt_Top.sizeDelta = new Vector2(0f, 130.1538f);
        rt_Top.anchoredPosition = new Vector2(0f, 0f);
        go_Top.AddComponent<CanvasRenderer>();
        var img_Top = go_Top.AddComponent<Image>();
        img_Top.sprite = SceneBuilderHelper.LoadSprite(Sprite_Black32x32);
        img_Top.color = new Color(1f, 1f, 1f, 1f);
        img_Top.raycastTarget = false;
        Undo.RegisterCreatedObjectUndo(go_Top, "Create Top");

        // --- RightPane ---
        var go_RightPane = new GameObject("RightPane");
        var rt_RightPane = go_RightPane.AddComponent<RectTransform>();
        rt_RightPane.SetParent(go_Top.GetComponent<RectTransform>(), false);
        rt_RightPane.anchorMin = new Vector2(0.6666667f, 0f);
        rt_RightPane.anchorMax = new Vector2(1f, 1f);
        rt_RightPane.pivot = new Vector2(1f, 0.5f);
        rt_RightPane.sizeDelta = new Vector2(0f, 0f);
        rt_RightPane.anchoredPosition = new Vector2(0f, 0f);
        var hlg_RightPane = go_RightPane.AddComponent<HorizontalLayoutGroup>();
        hlg_RightPane.spacing = 0f;
        hlg_RightPane.childAlignment = (TextAnchor)4;
        hlg_RightPane.childControlWidth = false;
        hlg_RightPane.childControlHeight = false;
        hlg_RightPane.childForceExpandWidth = true;
        hlg_RightPane.childForceExpandHeight = true;
        Undo.RegisterCreatedObjectUndo(go_RightPane, "Create RightPane");

        // --- CenterPane ---
        var go_CenterPane = new GameObject("CenterPane");
        var rt_CenterPane = go_CenterPane.AddComponent<RectTransform>();
        rt_CenterPane.SetParent(go_Top.GetComponent<RectTransform>(), false);
        rt_CenterPane.anchorMin = new Vector2(0.3333333f, 0f);
        rt_CenterPane.anchorMax = new Vector2(0.6666667f, 1f);
        rt_CenterPane.pivot = new Vector2(0.5f, 0.5f);
        rt_CenterPane.sizeDelta = new Vector2(0f, -7.629395E-06f);
        rt_CenterPane.anchoredPosition = new Vector2(0f, 0f);
        var hlg_CenterPane = go_CenterPane.AddComponent<HorizontalLayoutGroup>();
        hlg_CenterPane.spacing = 0f;
        hlg_CenterPane.childAlignment = (TextAnchor)4;
        hlg_CenterPane.childControlWidth = false;
        hlg_CenterPane.childControlHeight = false;
        hlg_CenterPane.childForceExpandWidth = true;
        hlg_CenterPane.childForceExpandHeight = true;
        Undo.RegisterCreatedObjectUndo(go_CenterPane, "Create CenterPane");

        // --- LeftPane ---
        var go_LeftPane = new GameObject("LeftPane");
        var rt_LeftPane = go_LeftPane.AddComponent<RectTransform>();
        rt_LeftPane.SetParent(go_Top.GetComponent<RectTransform>(), false);
        rt_LeftPane.anchorMin = new Vector2(0f, 0f);
        rt_LeftPane.anchorMax = new Vector2(0.3333333f, 1f);
        rt_LeftPane.pivot = new Vector2(0f, 0.5f);
        rt_LeftPane.sizeDelta = new Vector2(0f, 0f);
        rt_LeftPane.anchoredPosition = new Vector2(0f, 0f);
        var hlg_LeftPane = go_LeftPane.AddComponent<HorizontalLayoutGroup>();
        hlg_LeftPane.spacing = 0f;
        hlg_LeftPane.childAlignment = (TextAnchor)4;
        hlg_LeftPane.childControlWidth = false;
        hlg_LeftPane.childControlHeight = false;
        hlg_LeftPane.childForceExpandWidth = true;
        hlg_LeftPane.childForceExpandHeight = true;
        Undo.RegisterCreatedObjectUndo(go_LeftPane, "Create LeftPane");

        // --- FadeOverlay ---
        var go_FadeOverlay = new GameObject("FadeOverlay");
        go_FadeOverlay.layer = 5;
        var rt_FadeOverlay = go_FadeOverlay.AddComponent<RectTransform>();
        rt_FadeOverlay.SetParent(go_Canvas.GetComponent<RectTransform>(), false);
        rt_FadeOverlay.anchorMin = new Vector2(0f, 0f);
        rt_FadeOverlay.anchorMax = new Vector2(1f, 1f);
        rt_FadeOverlay.pivot = new Vector2(0.5f, 0.5f);
        rt_FadeOverlay.sizeDelta = new Vector2(-1f, -1f);
        rt_FadeOverlay.anchoredPosition = new Vector2(-0.5f, 0.5f);
        go_FadeOverlay.AddComponent<CanvasRenderer>();
        var img_FadeOverlay = go_FadeOverlay.AddComponent<Image>();
        img_FadeOverlay.sprite = SceneBuilderHelper.LoadSprite(Sprite_Black32x32);
        img_FadeOverlay.color = new Color(0f, 0f, 0f, 0f);
        img_FadeOverlay.raycastTarget = false;
        go_FadeOverlay.AddComponent<Scripts.Canvas.FadeOverlayInstance>();
        Undo.RegisterCreatedObjectUndo(go_FadeOverlay, "Create FadeOverlay");

        // --- Announcements ---
        var go_Announcements = new GameObject("Announcements");
        go_Announcements.layer = 5;
        var rt_Announcements = go_Announcements.AddComponent<RectTransform>();
        rt_Announcements.SetParent(go_Canvas.GetComponent<RectTransform>(), false);
        rt_Announcements.anchorMin = new Vector2(0.5f, 0.5f);
        rt_Announcements.anchorMax = new Vector2(0.5f, 0.5f);
        rt_Announcements.pivot = new Vector2(0.5f, 0.5f);
        rt_Announcements.sizeDelta = new Vector2(100f, 100f);
        rt_Announcements.anchoredPosition = new Vector2(0f, 0f);
        Undo.RegisterCreatedObjectUndo(go_Announcements, "Create Announcements");

        // --- VictoryAnnouncement ---
        var go_VictoryAnnouncement = new GameObject("VictoryAnnouncement");
        go_VictoryAnnouncement.layer = 5;
        var rt_VictoryAnnouncement = go_VictoryAnnouncement.AddComponent<RectTransform>();
        rt_VictoryAnnouncement.SetParent(go_Announcements.GetComponent<RectTransform>(), false);
        rt_VictoryAnnouncement.anchorMin = new Vector2(0.5f, 0.5f);
        rt_VictoryAnnouncement.anchorMax = new Vector2(0.5f, 0.5f);
        rt_VictoryAnnouncement.pivot = new Vector2(0.5f, 0.5f);
        rt_VictoryAnnouncement.sizeDelta = new Vector2(0f, 0f);
        rt_VictoryAnnouncement.anchoredPosition = new Vector2(0f, 0f);
        go_VictoryAnnouncement.AddComponent<CanvasRenderer>();
        go_VictoryAnnouncement.AddComponent<Scripts.Canvas.VictoryAnnouncement>();
        Undo.RegisterCreatedObjectUndo(go_VictoryAnnouncement, "Create VictoryAnnouncement");

        // --- Front ---
        var go_Front = new GameObject("Front");
        go_Front.layer = 5;
        var rt_Front = go_Front.AddComponent<RectTransform>();
        rt_Front.SetParent(go_VictoryAnnouncement.GetComponent<RectTransform>(), false);
        rt_Front.anchorMin = new Vector2(0.5f, 0.5f);
        rt_Front.anchorMax = new Vector2(0.5f, 0.5f);
        rt_Front.pivot = new Vector2(0.5f, 0.5f);
        rt_Front.sizeDelta = new Vector2(0f, 0f);
        rt_Front.anchoredPosition = new Vector2(0f, 0f);
        go_Front.AddComponent<CanvasRenderer>();
        var tmp_Front = go_Front.AddComponent<TextMeshProUGUI>();
        tmp_Front.font = SceneBuilderHelper.LoadFont(Font_Attic);
        tmp_Front.text = "Victory!";
        tmp_Front.fontSize = 128f;
        tmp_Front.color = new Color(1f, 1f, 1f, 0f);
        tmp_Front.alignment = (TextAlignmentOptions)514;
        tmp_Front.enableWordWrapping = false;
        tmp_Front.raycastTarget = true;
        Undo.RegisterCreatedObjectUndo(go_Front, "Create Front");

        // --- Back ---
        var go_Back3 = new GameObject("Back");
        go_Back3.layer = 5;
        var rt_Back3 = go_Back3.AddComponent<RectTransform>();
        rt_Back3.SetParent(go_VictoryAnnouncement.GetComponent<RectTransform>(), false);
        rt_Back3.anchorMin = new Vector2(0.5f, 0.5f);
        rt_Back3.anchorMax = new Vector2(0.5f, 0.5f);
        rt_Back3.pivot = new Vector2(0.5f, 0.5f);
        rt_Back3.sizeDelta = new Vector2(0f, 0f);
        rt_Back3.anchoredPosition = new Vector2(0f, 8f);
        go_Back3.AddComponent<CanvasRenderer>();
        var tmp_Back3 = go_Back3.AddComponent<TextMeshProUGUI>();
        tmp_Back3.font = SceneBuilderHelper.LoadFont(Font_Attic);
        tmp_Back3.text = "Victory!";
        tmp_Back3.fontSize = 128f;
        tmp_Back3.color = new Color(1f, 1f, 1f, 0f);
        tmp_Back3.alignment = (TextAlignmentOptions)514;
        tmp_Back3.enableWordWrapping = false;
        tmp_Back3.raycastTarget = true;
        Undo.RegisterCreatedObjectUndo(go_Back3, "Create Back");

        // --- Image ---
        var go_Image3 = new GameObject("Image");
        go_Image3.layer = 5;
        var rt_Image3 = go_Image3.AddComponent<RectTransform>();
        rt_Image3.SetParent(go_VictoryAnnouncement.GetComponent<RectTransform>(), false);
        rt_Image3.anchorMin = new Vector2(0.5f, 0.5f);
        rt_Image3.anchorMax = new Vector2(0.5f, 0.5f);
        rt_Image3.pivot = new Vector2(0.5f, 0.5f);
        rt_Image3.sizeDelta = new Vector2(161.95f, 261.7f);
        rt_Image3.anchoredPosition = new Vector2(0f, 0f);
        go_Image3.AddComponent<CanvasRenderer>();
        var img_Image3 = go_Image3.AddComponent<Image>();
        img_Image3.sprite = SceneBuilderHelper.LoadSprite(Sprite_Transparent32x32);
        img_Image3.color = new Color(1f, 1f, 1f, 0f);
        img_Image3.raycastTarget = true;
        go_Image3.AddComponent<Scripts.Canvas.ScrollingImage>();
        Undo.RegisterCreatedObjectUndo(go_Image3, "Create Image");

        // --- DefeatAnnouncement ---
        var go_DefeatAnnouncement = new GameObject("DefeatAnnouncement");
        go_DefeatAnnouncement.layer = 5;
        var rt_DefeatAnnouncement = go_DefeatAnnouncement.AddComponent<RectTransform>();
        rt_DefeatAnnouncement.SetParent(go_Announcements.GetComponent<RectTransform>(), false);
        rt_DefeatAnnouncement.anchorMin = new Vector2(0.5f, 0.5f);
        rt_DefeatAnnouncement.anchorMax = new Vector2(0.5f, 0.5f);
        rt_DefeatAnnouncement.pivot = new Vector2(0.5f, 0.5f);
        rt_DefeatAnnouncement.sizeDelta = new Vector2(0f, 0f);
        rt_DefeatAnnouncement.anchoredPosition = new Vector2(0f, 0f);
        go_DefeatAnnouncement.AddComponent<CanvasRenderer>();
        go_DefeatAnnouncement.AddComponent<Scripts.Canvas.DefeatAnnouncement>();
        Undo.RegisterCreatedObjectUndo(go_DefeatAnnouncement, "Create DefeatAnnouncement");

        // --- Image ---
        var go_Image2 = new GameObject("Image");
        go_Image2.layer = 5;
        var rt_Image2 = go_Image2.AddComponent<RectTransform>();
        rt_Image2.SetParent(go_DefeatAnnouncement.GetComponent<RectTransform>(), false);
        rt_Image2.anchorMin = new Vector2(0.5f, 0.5f);
        rt_Image2.anchorMax = new Vector2(0.5f, 0.5f);
        rt_Image2.pivot = new Vector2(0.5f, 0.5f);
        rt_Image2.sizeDelta = new Vector2(161.95f, 261.7f);
        rt_Image2.anchoredPosition = new Vector2(0f, 0f);
        go_Image2.AddComponent<CanvasRenderer>();
        var img_Image2 = go_Image2.AddComponent<Image>();
        img_Image2.sprite = SceneBuilderHelper.LoadSprite(Sprite_Transparent32x32);
        img_Image2.color = new Color(1f, 1f, 1f, 0f);
        img_Image2.raycastTarget = true;
        go_Image2.AddComponent<Scripts.Canvas.ScrollingImage>();
        Undo.RegisterCreatedObjectUndo(go_Image2, "Create Image");

        // --- Front ---
        var go_Front3 = new GameObject("Front");
        go_Front3.layer = 5;
        var rt_Front3 = go_Front3.AddComponent<RectTransform>();
        rt_Front3.SetParent(go_DefeatAnnouncement.GetComponent<RectTransform>(), false);
        rt_Front3.anchorMin = new Vector2(0.5f, 0.5f);
        rt_Front3.anchorMax = new Vector2(0.5f, 0.5f);
        rt_Front3.pivot = new Vector2(0.5f, 0.5f);
        rt_Front3.sizeDelta = new Vector2(0f, 0f);
        rt_Front3.anchoredPosition = new Vector2(0f, 0f);
        go_Front3.AddComponent<CanvasRenderer>();
        var tmp_Front3 = go_Front3.AddComponent<TextMeshProUGUI>();
        tmp_Front3.font = SceneBuilderHelper.LoadFont(Font_Attic);
        tmp_Front3.text = "Defeat";
        tmp_Front3.fontSize = 128f;
        tmp_Front3.color = new Color(1f, 1f, 1f, 0f);
        tmp_Front3.alignment = (TextAlignmentOptions)514;
        tmp_Front3.enableWordWrapping = false;
        tmp_Front3.raycastTarget = true;
        Undo.RegisterCreatedObjectUndo(go_Front3, "Create Front");

        // --- Back ---
        var go_Back5 = new GameObject("Back");
        go_Back5.layer = 5;
        var rt_Back5 = go_Back5.AddComponent<RectTransform>();
        rt_Back5.SetParent(go_DefeatAnnouncement.GetComponent<RectTransform>(), false);
        rt_Back5.anchorMin = new Vector2(0.5f, 0.5f);
        rt_Back5.anchorMax = new Vector2(0.5f, 0.5f);
        rt_Back5.pivot = new Vector2(0.5f, 0.5f);
        rt_Back5.sizeDelta = new Vector2(0f, 0f);
        rt_Back5.anchoredPosition = new Vector2(0f, 8f);
        go_Back5.AddComponent<CanvasRenderer>();
        var tmp_Back5 = go_Back5.AddComponent<TextMeshProUGUI>();
        tmp_Back5.font = SceneBuilderHelper.LoadFont(Font_Attic);
        tmp_Back5.text = "Defeat";
        tmp_Back5.fontSize = 128f;
        tmp_Back5.color = new Color(1f, 1f, 1f, 0f);
        tmp_Back5.alignment = (TextAlignmentOptions)514;
        tmp_Back5.enableWordWrapping = false;
        tmp_Back5.raycastTarget = true;
        Undo.RegisterCreatedObjectUndo(go_Back5, "Create Back");

        // --- WaveAnnouncement ---
        var go_WaveAnnouncement = new GameObject("WaveAnnouncement");
        go_WaveAnnouncement.layer = 5;
        var rt_WaveAnnouncement = go_WaveAnnouncement.AddComponent<RectTransform>();
        rt_WaveAnnouncement.SetParent(go_Announcements.GetComponent<RectTransform>(), false);
        rt_WaveAnnouncement.anchorMin = new Vector2(0.5f, 0.5f);
        rt_WaveAnnouncement.anchorMax = new Vector2(0.5f, 0.5f);
        rt_WaveAnnouncement.pivot = new Vector2(0.5f, 0.5f);
        rt_WaveAnnouncement.sizeDelta = new Vector2(0f, 0f);
        rt_WaveAnnouncement.anchoredPosition = new Vector2(0f, 0f);
        go_WaveAnnouncement.AddComponent<CanvasRenderer>();
        go_WaveAnnouncement.AddComponent<Scripts.Canvas.WaveAnnouncement>();
        Undo.RegisterCreatedObjectUndo(go_WaveAnnouncement, "Create WaveAnnouncement");

        // --- Back ---
        var go_Back = new GameObject("Back");
        go_Back.layer = 5;
        var rt_Back = go_Back.AddComponent<RectTransform>();
        rt_Back.SetParent(go_WaveAnnouncement.GetComponent<RectTransform>(), false);
        rt_Back.anchorMin = new Vector2(0.5f, 0.5f);
        rt_Back.anchorMax = new Vector2(0.5f, 0.5f);
        rt_Back.pivot = new Vector2(0.5f, 0.5f);
        rt_Back.sizeDelta = new Vector2(0f, 0f);
        rt_Back.anchoredPosition = new Vector2(0f, 8f);
        go_Back.AddComponent<CanvasRenderer>();
        var tmp_Back = go_Back.AddComponent<TextMeshProUGUI>();
        tmp_Back.font = SceneBuilderHelper.LoadFont(Font_Attic);
        tmp_Back.text = "Wave 1/\\u221E";
        tmp_Back.fontSize = 128f;
        tmp_Back.color = new Color(1f, 1f, 1f, 0f);
        tmp_Back.alignment = (TextAlignmentOptions)514;
        tmp_Back.enableWordWrapping = false;
        tmp_Back.raycastTarget = true;
        Undo.RegisterCreatedObjectUndo(go_Back, "Create Back");

        // --- Image ---
        var go_Image = new GameObject("Image");
        go_Image.layer = 5;
        var rt_Image = go_Image.AddComponent<RectTransform>();
        rt_Image.SetParent(go_WaveAnnouncement.GetComponent<RectTransform>(), false);
        rt_Image.anchorMin = new Vector2(0.5f, 0.5f);
        rt_Image.anchorMax = new Vector2(0.5f, 0.5f);
        rt_Image.pivot = new Vector2(0.5f, 0.5f);
        rt_Image.sizeDelta = new Vector2(161.95f, 261.7f);
        rt_Image.anchoredPosition = new Vector2(0f, 0f);
        go_Image.AddComponent<CanvasRenderer>();
        var img_Image = go_Image.AddComponent<Image>();
        img_Image.sprite = SceneBuilderHelper.LoadSprite(Sprite_Transparent32x32);
        img_Image.color = new Color(1f, 1f, 1f, 0f);
        img_Image.raycastTarget = true;
        go_Image.AddComponent<Scripts.Canvas.ScrollingImage>();
        Undo.RegisterCreatedObjectUndo(go_Image, "Create Image");

        // --- Front ---
        var go_Front2 = new GameObject("Front");
        go_Front2.layer = 5;
        var rt_Front2 = go_Front2.AddComponent<RectTransform>();
        rt_Front2.SetParent(go_WaveAnnouncement.GetComponent<RectTransform>(), false);
        rt_Front2.anchorMin = new Vector2(0.5f, 0.5f);
        rt_Front2.anchorMax = new Vector2(0.5f, 0.5f);
        rt_Front2.pivot = new Vector2(0.5f, 0.5f);
        rt_Front2.sizeDelta = new Vector2(0f, 0f);
        rt_Front2.anchoredPosition = new Vector2(0f, 0f);
        go_Front2.AddComponent<CanvasRenderer>();
        var tmp_Front2 = go_Front2.AddComponent<TextMeshProUGUI>();
        tmp_Front2.font = SceneBuilderHelper.LoadFont(Font_Attic);
        tmp_Front2.text = "Wave 1/\\u221E";
        tmp_Front2.fontSize = 128f;
        tmp_Front2.color = new Color(1f, 1f, 1f, 0f);
        tmp_Front2.alignment = (TextAlignmentOptions)514;
        tmp_Front2.enableWordWrapping = false;
        tmp_Front2.raycastTarget = true;
        Undo.RegisterCreatedObjectUndo(go_Front2, "Create Front");

        // --- ActorPanel (HUD Row 15: tabbed character panel — Stats / Equipment / Lore). The
        //     tab bar + content panels are built at runtime by Scripts.Canvas.ActorPanel, so this
        //     is just the full-width, bottom-anchored 256-tall root + the component. ---
        var go_ActorPanel = new GameObject("ActorPanel");
        go_ActorPanel.layer = 5;
        var rt_ActorPanel = go_ActorPanel.AddComponent<RectTransform>();
        rt_ActorPanel.SetParent(go_Canvas.GetComponent<RectTransform>(), false);
        rt_ActorPanel.anchorMin = new Vector2(0f, 0f);
        rt_ActorPanel.anchorMax = new Vector2(1f, 0f);
        rt_ActorPanel.pivot = new Vector2(0.5f, 0f);
        rt_ActorPanel.sizeDelta = new Vector2(0f, 256f);
        rt_ActorPanel.anchoredPosition = new Vector2(0f, 0f);
        go_ActorPanel.AddComponent<CanvasRenderer>();
        go_ActorPanel.AddComponent<Scripts.Canvas.ActorPanel>();
        Undo.RegisterCreatedObjectUndo(go_ActorPanel, "Create ActorPanel");

        // --- AbilityButtonContainer (Row 13: 6-slot ability bar, pulled out of Card) ---
        // Pre-Phase-B this lived inside the Card; now it's a direct Canvas child sitting on Row 13
        // (above the orb line, above the character card).
        var go_AbilityButtonContainer = new GameObject("AbilityButtonContainer");
        go_AbilityButtonContainer.layer = 5;
        var rt_AbilityButtonContainer = go_AbilityButtonContainer.AddComponent<RectTransform>();
        rt_AbilityButtonContainer.SetParent(go_Canvas.GetComponent<RectTransform>(), false);
        rt_AbilityButtonContainer.anchorMin = new Vector2(0f, 0f);
        rt_AbilityButtonContainer.anchorMax = new Vector2(1f, 0f);
        rt_AbilityButtonContainer.pivot = new Vector2(0.5f, 0.5f);
        rt_AbilityButtonContainer.sizeDelta = new Vector2(0f, Hud_RowHeight);
        rt_AbilityButtonContainer.anchoredPosition = new Vector2(0f, Hud_Row13Y_FromBot);
        go_AbilityButtonContainer.AddComponent<CanvasRenderer>();
        var img_AbilityButtonContainer = go_AbilityButtonContainer.AddComponent<Image>();
        img_AbilityButtonContainer.sprite = SceneBuilderHelper.LoadSprite(Sprite_action_bar_1);
        img_AbilityButtonContainer.color = new Color(1f, 1f, 1f, 0.2352941f);
        img_AbilityButtonContainer.raycastTarget = true;
        var hlg_AbilityButtonContainer = go_AbilityButtonContainer.AddComponent<HorizontalLayoutGroup>();
        hlg_AbilityButtonContainer.spacing = 0f;
        hlg_AbilityButtonContainer.childAlignment = (TextAnchor)3;
        hlg_AbilityButtonContainer.childControlWidth = false;
        hlg_AbilityButtonContainer.childControlHeight = false;
        hlg_AbilityButtonContainer.childForceExpandWidth = true;
        hlg_AbilityButtonContainer.childForceExpandHeight = true;
        Undo.RegisterCreatedObjectUndo(go_AbilityButtonContainer, "Create AbilityButtonContainer");

        // (Legacy ActorCard children — ArrowRight / Text / Backdrop / Details / ArrowLeft /
        //  Portrait / Title — removed. Scripts.Canvas.ActorPanel now builds its tab bar and the
        //  Stats / Equipment / Lore content panels at runtime, so the scene root carries none of them.)

        // --- Pointer ---
        var go_Pointer = new GameObject("Pointer");
        go_Pointer.layer = 5;
        var rt_Pointer = go_Pointer.AddComponent<RectTransform>();
        rt_Pointer.SetParent(go_Canvas.GetComponent<RectTransform>(), false);
        rt_Pointer.anchorMin = new Vector2(0.5f, 0.5f);
        rt_Pointer.anchorMax = new Vector2(0.5f, 0.5f);
        rt_Pointer.pivot = new Vector2(0.5f, 0.5f);
        rt_Pointer.sizeDelta = new Vector2(100f, 100f);
        rt_Pointer.anchoredPosition = new Vector2(0f, 0f);
        go_Pointer.AddComponent<CanvasRenderer>();
        var canvasGroup_Pointer = go_Pointer.AddComponent<CanvasGroup>();
        go_Pointer.AddComponent<Scripts.Managers.PointerManager>();
        Undo.RegisterCreatedObjectUndo(go_Pointer, "Create Pointer");

        // --- AbilityCastConfirm ---
        var go_AbilityCastConfirm = new GameObject("AbilityCastConfirm");
        go_AbilityCastConfirm.layer = 5;
        var rt_AbilityCastConfirm = go_AbilityCastConfirm.AddComponent<RectTransform>();
        rt_AbilityCastConfirm.SetParent(go_Canvas.GetComponent<RectTransform>(), false);
        rt_AbilityCastConfirm.anchorMin = new Vector2(0.5f, 0.5f);
        rt_AbilityCastConfirm.anchorMax = new Vector2(0.5f, 0.5f);
        rt_AbilityCastConfirm.pivot = new Vector2(0.5f, 0.5f);
        rt_AbilityCastConfirm.sizeDelta = new Vector2(512f, 128f);
        rt_AbilityCastConfirm.anchoredPosition = new Vector2(0f, 0f);
        go_AbilityCastConfirm.AddComponent<CanvasRenderer>();
        go_AbilityCastConfirm.AddComponent<Scripts.Canvas.AbilityCastConfirm>();
        var canvasGroup_AbilityCastConfirm = go_AbilityCastConfirm.AddComponent<CanvasGroup>();
        var img_AbilityCastConfirm = go_AbilityCastConfirm.AddComponent<Image>();
        img_AbilityCastConfirm.sprite = SceneBuilderHelper.LoadSprite(Sprite_Back_512x128);
        img_AbilityCastConfirm.color = new Color(1f, 1f, 1f, 1f);
        img_AbilityCastConfirm.raycastTarget = true;
        Undo.RegisterCreatedObjectUndo(go_AbilityCastConfirm, "Create AbilityCastConfirm");

        // --- CancelButton ---
        var go_CancelButton = new GameObject("CancelButton");
        go_CancelButton.layer = 5;
        var rt_CancelButton = go_CancelButton.AddComponent<RectTransform>();
        rt_CancelButton.SetParent(go_AbilityCastConfirm.GetComponent<RectTransform>(), false);
        rt_CancelButton.anchorMin = new Vector2(0f, 0.5f);
        rt_CancelButton.anchorMax = new Vector2(0f, 0.5f);
        rt_CancelButton.pivot = new Vector2(0.5f, 0.5f);
        rt_CancelButton.sizeDelta = new Vector2(64f, 64f);
        rt_CancelButton.anchoredPosition = new Vector2(391.36f, 0f);
        go_CancelButton.AddComponent<CanvasRenderer>();
        var img_CancelButton = go_CancelButton.AddComponent<Image>();
        img_CancelButton.sprite = SceneBuilderHelper.LoadSprite(Sprite_Cancel);
        img_CancelButton.color = new Color(1f, 1f, 1f, 1f);
        img_CancelButton.raycastTarget = true;
        var btn_CancelButton = go_CancelButton.AddComponent<Button>();
        btn_CancelButton.navigation = new Navigation { mode = (Navigation.Mode)3 };
        btn_CancelButton.targetGraphic = go_CancelButton.GetComponent<Image>();
        Undo.RegisterCreatedObjectUndo(go_CancelButton, "Create CancelButton");

        // --- Label ---
        var go_Label3 = new GameObject("Label");
        go_Label3.layer = 5;
        var rt_Label3 = go_Label3.AddComponent<RectTransform>();
        rt_Label3.SetParent(go_CancelButton.GetComponent<RectTransform>(), false);
        rt_Label3.anchorMin = new Vector2(0f, 0f);
        rt_Label3.anchorMax = new Vector2(1f, 1f);
        rt_Label3.pivot = new Vector2(0.5f, 0.5f);
        rt_Label3.sizeDelta = new Vector2(0f, 0f);
        rt_Label3.anchoredPosition = new Vector2(0f, 0f);
        go_Label3.AddComponent<CanvasRenderer>();
        var tmp_Label3 = go_Label3.AddComponent<TextMeshProUGUI>();
        tmp_Label3.font = SceneBuilderHelper.LoadFont(Font_Body);
        tmp_Label3.text = "X";
        tmp_Label3.fontSize = 24f;
        tmp_Label3.color = new Color(1f, 1f, 1f, 1f);
        tmp_Label3.alignment = (TextAlignmentOptions)514;
        tmp_Label3.enableWordWrapping = true;
        tmp_Label3.raycastTarget = true;
        Undo.RegisterCreatedObjectUndo(go_Label3, "Create Label");

        // --- CastButton ---
        var go_CastButton = new GameObject("CastButton");
        go_CastButton.layer = 5;
        var rt_CastButton = go_CastButton.AddComponent<RectTransform>();
        rt_CastButton.SetParent(go_AbilityCastConfirm.GetComponent<RectTransform>(), false);
        rt_CastButton.anchorMin = new Vector2(0f, 0.5f);
        rt_CastButton.anchorMax = new Vector2(0f, 0.5f);
        rt_CastButton.pivot = new Vector2(0.5f, 0.5f);
        rt_CastButton.sizeDelta = new Vector2(64f, 64f);
        rt_CastButton.anchoredPosition = new Vector2(454.9f, 0f);
        go_CastButton.AddComponent<CanvasRenderer>();
        var img_CastButton = go_CastButton.AddComponent<Image>();
        img_CastButton.sprite = SceneBuilderHelper.LoadSprite(Sprite_Confirm);
        img_CastButton.color = new Color(1f, 1f, 1f, 1f);
        img_CastButton.raycastTarget = true;
        var btn_CastButton = go_CastButton.AddComponent<Button>();
        btn_CastButton.navigation = new Navigation { mode = (Navigation.Mode)3 };
        btn_CastButton.targetGraphic = go_CastButton.GetComponent<Image>();
        Undo.RegisterCreatedObjectUndo(go_CastButton, "Create CastButton");

        // --- Label ---
        var go_Label6 = new GameObject("Label");
        go_Label6.layer = 5;
        var rt_Label6 = go_Label6.AddComponent<RectTransform>();
        rt_Label6.SetParent(go_CastButton.GetComponent<RectTransform>(), false);
        rt_Label6.anchorMin = new Vector2(0f, 0f);
        rt_Label6.anchorMax = new Vector2(1f, 1f);
        rt_Label6.pivot = new Vector2(0.5f, 0.5f);
        rt_Label6.sizeDelta = new Vector2(0f, 0f);
        rt_Label6.anchoredPosition = new Vector2(0f, 0f);
        go_Label6.AddComponent<CanvasRenderer>();
        var tmp_Label6 = go_Label6.AddComponent<TextMeshProUGUI>();
        tmp_Label6.font = SceneBuilderHelper.LoadFont(Font_Body);
        tmp_Label6.text = "Ok";
        tmp_Label6.fontSize = 24f;
        tmp_Label6.color = new Color(1f, 1f, 1f, 1f);
        tmp_Label6.alignment = (TextAlignmentOptions)514;
        tmp_Label6.enableWordWrapping = true;
        tmp_Label6.raycastTarget = true;
        Undo.RegisterCreatedObjectUndo(go_Label6, "Create Label");

        // --- Label ---
        var go_Label4 = new GameObject("Label");
        go_Label4.layer = 5;
        var rt_Label4 = go_Label4.AddComponent<RectTransform>();
        rt_Label4.SetParent(go_AbilityCastConfirm.GetComponent<RectTransform>(), false);
        rt_Label4.anchorMin = new Vector2(0f, 0.5f);
        rt_Label4.anchorMax = new Vector2(0f, 0.5f);
        rt_Label4.pivot = new Vector2(0.5f, 0.5f);
        rt_Label4.sizeDelta = new Vector2(0f, 64f);
        rt_Label4.anchoredPosition = new Vector2(64f, 0f);
        go_Label4.AddComponent<CanvasRenderer>();
        var tmp_Label4 = go_Label4.AddComponent<TextMeshProUGUI>();
        tmp_Label4.font = SceneBuilderHelper.LoadFont(Font_Attic);
        tmp_Label4.text = "Cast Heal";
        tmp_Label4.fontSize = 32f;
        tmp_Label4.color = new Color(1f, 1f, 1f, 1f);
        tmp_Label4.alignment = (TextAlignmentOptions)513;
        tmp_Label4.enableWordWrapping = false;
        tmp_Label4.raycastTarget = true;
        Undo.RegisterCreatedObjectUndo(go_Label4, "Create Label");

        // --- ActionTitle (Row 3: dedicated action banner under timeline, FF6-style) ---
        var go_ActionTitle = new GameObject("ActionTitle");
        go_ActionTitle.layer = 5;
        var rt_ActionTitle = go_ActionTitle.AddComponent<RectTransform>();
        rt_ActionTitle.SetParent(go_Canvas.GetComponent<RectTransform>(), false);
        // Anchored top, pivot centered — sits at Row 3 (≈422px below canvas top).
        rt_ActionTitle.anchorMin = new Vector2(0.5f, 1f);
        rt_ActionTitle.anchorMax = new Vector2(0.5f, 1f);
        rt_ActionTitle.pivot = new Vector2(0.5f, 0.5f);
        rt_ActionTitle.sizeDelta = new Vector2(900f, 130f);
        rt_ActionTitle.anchoredPosition = new Vector2(0f, Hud_Row3Y_FromTop);
        go_ActionTitle.AddComponent<CanvasRenderer>();
        var img_ActionTitle = go_ActionTitle.AddComponent<Image>();
        img_ActionTitle.sprite = SceneBuilderHelper.LoadSprite(Sprite_TitleBar);
        img_ActionTitle.color = new Color(1f, 1f, 1f, 1f);
        img_ActionTitle.raycastTarget = false;
        go_ActionTitle.AddComponent<Scripts.Canvas.ActionTitle>();
        var canvasGroup_ActionTitle = go_ActionTitle.AddComponent<CanvasGroup>();
        Undo.RegisterCreatedObjectUndo(go_ActionTitle, "Create ActionTitle");

        // --- Label ---
        var go_Label = new GameObject("Label");
        go_Label.layer = 5;
        var rt_Label = go_Label.AddComponent<RectTransform>();
        rt_Label.SetParent(go_ActionTitle.GetComponent<RectTransform>(), false);
        rt_Label.anchorMin = new Vector2(0.5f, 0.5f);
        rt_Label.anchorMax = new Vector2(0.5f, 0.5f);
        rt_Label.pivot = new Vector2(0.5f, 0.5f);
        rt_Label.sizeDelta = new Vector2(0f, 64f);
        rt_Label.anchoredPosition = new Vector2(0f, 0f);
        go_Label.AddComponent<CanvasRenderer>();
        var tmp_Label = go_Label.AddComponent<TextMeshProUGUI>();
        tmp_Label.font = SceneBuilderHelper.LoadFont(Font_Attic);
        tmp_Label.text = "Test";
        tmp_Label.fontSize = 32f;
        tmp_Label.color = new Color(1f, 1f, 1f, 1f);
        tmp_Label.alignment = (TextAlignmentOptions)514;
        tmp_Label.enableWordWrapping = false;
        tmp_Label.raycastTarget = true;
        Undo.RegisterCreatedObjectUndo(go_Label, "Create Label");

        // --- ManaPool, HeroBar, EnemyBar, BankButton: REMOVED ---
        // The legacy fill-bar UI + Bank button are gone. The new HUD pieces (12-orb
        // mana line + Shield button) are runtime-spawned by ManaPoolManager.Start via
        // ManaOrbLineFactory.Create and ShieldButtonFactory.Create.
        // Pincer-completion orb drops via ManaOrbFactory.Drop (see PincerAttackManager).
        // --- Clock ---
        var go_Clock = new GameObject("Clock");
        var rt_Clock = go_Clock.AddComponent<RectTransform>();
        rt_Clock.SetParent(go_Canvas.GetComponent<RectTransform>(), false);
        rt_Clock.anchorMin = new Vector2(0f, 0.5f);
        rt_Clock.anchorMax = new Vector2(0f, 0.5f);
        rt_Clock.pivot = new Vector2(0f, 1f);
        rt_Clock.sizeDelta = new Vector2(280f, 32f);
        rt_Clock.anchoredPosition = new Vector2(0f, 1100f);
        go_Clock.AddComponent<CanvasRenderer>();
        var tmp_Clock = go_Clock.AddComponent<TextMeshProUGUI>();
        tmp_Clock.font = SceneBuilderHelper.LoadFont(Font_Body);
        tmp_Clock.text = "10:41 AM";
        tmp_Clock.fontSize = 24f;
        tmp_Clock.color = new Color(1f, 1f, 1f, 1f);
        tmp_Clock.alignment = (TextAlignmentOptions)514;
        tmp_Clock.enableWordWrapping = false;
        tmp_Clock.raycastTarget = false;
        go_Clock.AddComponent<Scripts.Canvas.Clock>();
        Undo.RegisterCreatedObjectUndo(go_Clock, "Create Clock");

        // --- Portraits ---
        var go_Portraits = new GameObject("Portraits");
        go_Portraits.layer = 5;
        var rt_Portraits = go_Portraits.AddComponent<RectTransform>();
        rt_Portraits.SetParent(go_Canvas.GetComponent<RectTransform>(), false);
        rt_Portraits.anchorMin = new Vector2(0.5f, 0.5f);
        rt_Portraits.anchorMax = new Vector2(0.5f, 0.5f);
        rt_Portraits.pivot = new Vector2(0.5f, 0.5f);
        rt_Portraits.sizeDelta = new Vector2(100f, 100f);
        rt_Portraits.anchoredPosition = new Vector2(0f, 0f);
        Undo.RegisterCreatedObjectUndo(go_Portraits, "Create Portraits");

        // --- CoinCounter ---
        var go_CoinCounter = new GameObject("CoinCounter");
        go_CoinCounter.layer = 5;
        var rt_CoinCounter = go_CoinCounter.AddComponent<RectTransform>();
        rt_CoinCounter.SetParent(go_Canvas.GetComponent<RectTransform>(), false);
        rt_CoinCounter.anchorMin = new Vector2(0f, 1f);
        rt_CoinCounter.anchorMax = new Vector2(1f, 1f);
        rt_CoinCounter.pivot = new Vector2(1f, 0.5f);
        rt_CoinCounter.sizeDelta = new Vector2(-980f, 100f);
        rt_CoinCounter.anchoredPosition = new Vector2(-402.37f, -1200f);
        go_CoinCounter.AddComponent<Scripts.Canvas.CoinCounter>();
        Undo.RegisterCreatedObjectUndo(go_CoinCounter, "Create CoinCounter");

        // --- Glow ---
        var go_Glow = new GameObject("Glow");
        go_Glow.layer = 12;
        var rt_Glow = go_Glow.AddComponent<RectTransform>();
        rt_Glow.SetParent(go_CoinCounter.GetComponent<RectTransform>(), false);
        rt_Glow.anchorMin = new Vector2(0.5f, 0.5f);
        rt_Glow.anchorMax = new Vector2(0.5f, 0.5f);
        rt_Glow.pivot = new Vector2(0.5f, 0.5f);
        rt_Glow.sizeDelta = new Vector2(100f, 100f);
        rt_Glow.anchoredPosition = new Vector2(218.79f, 1023.68f);
        go_Glow.AddComponent<CanvasRenderer>();
        var img_Glow = go_Glow.AddComponent<Image>();
        img_Glow.sprite = SceneBuilderHelper.LoadSprite(Sprite_Coin);
        img_Glow.color = new Color(1f, 1f, 1f, 1f);
        img_Glow.raycastTarget = true;
        Undo.RegisterCreatedObjectUndo(go_Glow, "Create Glow");

        // --- Value ---
        var go_Value = new GameObject("Value");
        go_Value.layer = 12;
        var rt_Value = go_Value.AddComponent<RectTransform>();
        rt_Value.SetParent(go_CoinCounter.GetComponent<RectTransform>(), false);
        rt_Value.anchorMin = new Vector2(0.5f, 0.5f);
        rt_Value.anchorMax = new Vector2(0.5f, 0.5f);
        rt_Value.pivot = new Vector2(0.5f, 0.5f);
        rt_Value.sizeDelta = new Vector2(20f, 5f);
        rt_Value.anchoredPosition = new Vector2(243.79f, 1053.65f);
        go_Value.AddComponent<CanvasRenderer>();
        var tmp_Value = go_Value.AddComponent<TextMeshProUGUI>();
        tmp_Value.font = SceneBuilderHelper.LoadFont(Font_Attic);
        tmp_Value.text = "0000000";
        tmp_Value.fontSize = 48f;
        tmp_Value.color = new Color(1f, 1f, 1f, 1f);
        tmp_Value.alignment = (TextAlignmentOptions)257;
        tmp_Value.enableWordWrapping = false;
        tmp_Value.raycastTarget = true;
        Undo.RegisterCreatedObjectUndo(go_Value, "Create Value");

        // --- Icon ---
        var go_Icon = new GameObject("Icon");
        go_Icon.layer = 12;
        var rt_Icon = go_Icon.AddComponent<RectTransform>();
        rt_Icon.SetParent(go_CoinCounter.GetComponent<RectTransform>(), false);
        rt_Icon.anchorMin = new Vector2(0.5f, 0.5f);
        rt_Icon.anchorMax = new Vector2(0.5f, 0.5f);
        rt_Icon.pivot = new Vector2(0.5f, 0.5f);
        rt_Icon.sizeDelta = new Vector2(100f, 100f);
        rt_Icon.anchoredPosition = new Vector2(218.79f, 1023.68f);
        go_Icon.AddComponent<CanvasRenderer>();
        var img_Icon = go_Icon.AddComponent<Image>();
        img_Icon.sprite = SceneBuilderHelper.LoadSprite(Sprite_Coin);
        img_Icon.color = new Color(1f, 0.8745098f, 0f, 1f);
        img_Icon.raycastTarget = true;
        Undo.RegisterCreatedObjectUndo(go_Icon, "Create Icon");

        // --- TimelineBar (Row 2: horizontal load strip) ---
        var go_TimelineBar = new GameObject("TimelineBar");
        var rt_TimelineBar = go_TimelineBar.AddComponent<RectTransform>();
        rt_TimelineBar.SetParent(go_Canvas.GetComponent<RectTransform>(), false);
        rt_TimelineBar.anchorMin = new Vector2(0.5f, 0.5f);
        rt_TimelineBar.anchorMax = new Vector2(0.5f, 0.5f);
        rt_TimelineBar.pivot = new Vector2(0.5f, 0.5f);
        rt_TimelineBar.sizeDelta = new Vector2(1050f, 8f);
        rt_TimelineBar.anchoredPosition = new Vector2(0f, Hud_Row2Y_Centered);
        go_TimelineBar.AddComponent<Scripts.Canvas.TimelineBarInstance>();
        Undo.RegisterCreatedObjectUndo(go_TimelineBar, "Create TimelineBar");

        // --- Tags ---
        var go_Tags = new GameObject("Tags");
        var rt_Tags = go_Tags.AddComponent<RectTransform>();
        rt_Tags.SetParent(go_TimelineBar.GetComponent<RectTransform>(), false);
        rt_Tags.anchorMin = new Vector2(0.5f, 0.5f);
        rt_Tags.anchorMax = new Vector2(0.5f, 0.5f);
        rt_Tags.pivot = new Vector2(0.5f, 0.5f);
        rt_Tags.sizeDelta = new Vector2(100f, 100f);
        rt_Tags.anchoredPosition = new Vector2(0f, 16f);
        Undo.RegisterCreatedObjectUndo(go_Tags, "Create Tags");

        // --- Right ---
        var go_Right = new GameObject("Right");
        var rt_Right = go_Right.AddComponent<RectTransform>();
        rt_Right.SetParent(go_TimelineBar.GetComponent<RectTransform>(), false);
        rt_Right.anchorMin = new Vector2(1f, 0.5f);
        rt_Right.anchorMax = new Vector2(1f, 0.5f);
        rt_Right.pivot = new Vector2(1f, 0.5f);
        rt_Right.sizeDelta = new Vector2(2f, 16f);
        rt_Right.anchoredPosition = new Vector2(0f, 0f);
        go_Right.AddComponent<CanvasRenderer>();
        var img_Right = go_Right.AddComponent<Image>();
        img_Right.color = new Color(0.5490196f, 0.7882354f, 0.1411765f, 1f);
        img_Right.raycastTarget = true;
        Undo.RegisterCreatedObjectUndo(go_Right, "Create Right");

        // --- Left ---
        var go_Left = new GameObject("Left");
        var rt_Left = go_Left.AddComponent<RectTransform>();
        rt_Left.SetParent(go_TimelineBar.GetComponent<RectTransform>(), false);
        rt_Left.anchorMin = new Vector2(0f, 0.5f);
        rt_Left.anchorMax = new Vector2(0f, 0.5f);
        rt_Left.pivot = new Vector2(0f, 0.5f);
        rt_Left.sizeDelta = new Vector2(2f, 16f);
        rt_Left.anchoredPosition = new Vector2(0f, 0f);
        go_Left.AddComponent<CanvasRenderer>();
        var img_Left = go_Left.AddComponent<Image>();
        img_Left.color = new Color(1f, 0f, 0f, 1f);
        img_Left.raycastTarget = true;
        Undo.RegisterCreatedObjectUndo(go_Left, "Create Left");

        // --- Line ---
        var go_Line = new GameObject("Line");
        var rt_Line = go_Line.AddComponent<RectTransform>();
        rt_Line.SetParent(go_TimelineBar.GetComponent<RectTransform>(), false);
        rt_Line.anchorMin = new Vector2(0f, 0.5f);
        rt_Line.anchorMax = new Vector2(1f, 0.5f);
        rt_Line.pivot = new Vector2(0.5f, 0.5f);
        rt_Line.sizeDelta = new Vector2(0f, 2f);
        rt_Line.anchoredPosition = new Vector2(0f, 0f);
        go_Line.AddComponent<CanvasRenderer>();
        var img_Line = go_Line.AddComponent<Image>();
        img_Line.color = new Color(1f, 1f, 1f, 1f);
        img_Line.raycastTarget = true;
        Undo.RegisterCreatedObjectUndo(go_Line, "Create Line");

        // --- PauseMenu ---
        var go_PauseMenu = Scripts.Factories.PauseMenuFactory.Create(
            go_Canvas.GetComponent<RectTransform>(),
            SceneBuilderHelper.LoadSprite(Sprite_Black32x32));
        Undo.RegisterCreatedObjectUndo(go_PauseMenu, "Create PauseMenu");

        // --- ParallaxBackground ---
        var go_ParallaxBackground = new GameObject("ParallaxBackground");
        go_ParallaxBackground.layer = 5;
        var rt_ParallaxBackground = go_ParallaxBackground.AddComponent<RectTransform>();
        rt_ParallaxBackground.SetParent(go_PauseMenu.GetComponent<RectTransform>(), false);
        rt_ParallaxBackground.anchorMin = new Vector2(0f, 0f);
        rt_ParallaxBackground.anchorMax = new Vector2(1f, 1f);
        rt_ParallaxBackground.pivot = new Vector2(0.5f, 0.5f);
        rt_ParallaxBackground.sizeDelta = new Vector2(0f, 0f);
        rt_ParallaxBackground.anchoredPosition = new Vector2(0f, 0f);
        Undo.RegisterCreatedObjectUndo(go_ParallaxBackground, "Create ParallaxBackground");

        // --- Slide1 ---
        var go_Slide1 = new GameObject("Slide1");
        go_Slide1.layer = 5;
        var rt_Slide1 = go_Slide1.AddComponent<RectTransform>();
        rt_Slide1.SetParent(go_ParallaxBackground.GetComponent<RectTransform>(), false);
        rt_Slide1.anchorMin = new Vector2(0f, 0f);
        rt_Slide1.anchorMax = new Vector2(1f, 1f);
        rt_Slide1.pivot = new Vector2(0.5f, 1f);
        rt_Slide1.sizeDelta = new Vector2(0f, 0f);
        rt_Slide1.anchoredPosition = new Vector2(0f, 0f);
        go_Slide1.AddComponent<CanvasRenderer>();
        // TODO: unresolved script GUID=1344c3c82d62a2a41a3576d8abb8e3ea — component skipped.
        go_Slide1.AddComponent<Scripts.Canvas.ScrollingRawImage>();
        Undo.RegisterCreatedObjectUndo(go_Slide1, "Create Slide1");

        // --- Slide3 ---
        var go_Slide3 = new GameObject("Slide3");
        go_Slide3.layer = 5;
        var rt_Slide3 = go_Slide3.AddComponent<RectTransform>();
        rt_Slide3.SetParent(go_ParallaxBackground.GetComponent<RectTransform>(), false);
        rt_Slide3.anchorMin = new Vector2(0f, 0f);
        rt_Slide3.anchorMax = new Vector2(1f, 1f);
        rt_Slide3.pivot = new Vector2(0.5f, 0f);
        rt_Slide3.sizeDelta = new Vector2(0f, 0f);
        rt_Slide3.anchoredPosition = new Vector2(0f, 0f);
        go_Slide3.AddComponent<CanvasRenderer>();
        // TODO: unresolved script GUID=1344c3c82d62a2a41a3576d8abb8e3ea — component skipped.
        go_Slide3.AddComponent<Scripts.Canvas.ScrollingRawImage>();
        Undo.RegisterCreatedObjectUndo(go_Slide3, "Create Slide3");

        // --- Slide2 ---
        var go_Slide2 = new GameObject("Slide2");
        go_Slide2.layer = 5;
        var rt_Slide2 = go_Slide2.AddComponent<RectTransform>();
        rt_Slide2.SetParent(go_ParallaxBackground.GetComponent<RectTransform>(), false);
        rt_Slide2.anchorMin = new Vector2(0f, 0f);
        rt_Slide2.anchorMax = new Vector2(1f, 1f);
        rt_Slide2.pivot = new Vector2(0.5f, 0f);
        rt_Slide2.sizeDelta = new Vector2(0f, 0f);
        rt_Slide2.anchoredPosition = new Vector2(0f, 0f);
        go_Slide2.AddComponent<CanvasRenderer>();
        // TODO: unresolved script GUID=1344c3c82d62a2a41a3576d8abb8e3ea — component skipped.
        go_Slide2.AddComponent<Scripts.Canvas.ScrollingRawImage>();
        Undo.RegisterCreatedObjectUndo(go_Slide2, "Create Slide2");

        // --- Inner ---
        var go_Inner = new GameObject("Inner");
        go_Inner.layer = 5;
        var rt_Inner = go_Inner.AddComponent<RectTransform>();
        rt_Inner.SetParent(go_PauseMenu.GetComponent<RectTransform>(), false);
        rt_Inner.anchorMin = new Vector2(0.5f, 0.5f);
        rt_Inner.anchorMax = new Vector2(0.5f, 0.5f);
        rt_Inner.pivot = new Vector2(0.5f, 0.5f);
        rt_Inner.sizeDelta = new Vector2(800f, 600f);
        rt_Inner.anchoredPosition = new Vector2(0f, 300f);
        var vlg_Inner = go_Inner.AddComponent<VerticalLayoutGroup>();
        vlg_Inner.spacing = 10f;
        vlg_Inner.childAlignment = (TextAnchor)4;
        vlg_Inner.childControlWidth = false;
        vlg_Inner.childControlHeight = false;
        vlg_Inner.childForceExpandWidth = true;
        vlg_Inner.childForceExpandHeight = true;
        Undo.RegisterCreatedObjectUndo(go_Inner, "Create Inner");

        // --- SectionOptions ---
        var go_SectionOptions = new GameObject("SectionOptions");
        go_SectionOptions.layer = 5;
        var rt_SectionOptions = go_SectionOptions.AddComponent<RectTransform>();
        rt_SectionOptions.SetParent(go_Inner.GetComponent<RectTransform>(), false);
        rt_SectionOptions.anchorMin = new Vector2(0f, 0f);
        rt_SectionOptions.anchorMax = new Vector2(0f, 0f);
        rt_SectionOptions.pivot = new Vector2(0.5f, 0.5f);
        rt_SectionOptions.sizeDelta = new Vector2(1024f, 100f);
        rt_SectionOptions.anchoredPosition = new Vector2(0f, 0f);
        go_SectionOptions.AddComponent<CanvasRenderer>();
        var img_SectionOptions = go_SectionOptions.AddComponent<Image>();
        img_SectionOptions.color = new Color(0f, 0f, 0f, 0f);
        img_SectionOptions.raycastTarget = true;
        Undo.RegisterCreatedObjectUndo(go_SectionOptions, "Create SectionOptions");

        // --- Image ---
        var go_Image4 = new GameObject("Image");
        go_Image4.layer = 5;
        var rt_Image4 = go_Image4.AddComponent<RectTransform>();
        rt_Image4.SetParent(go_SectionOptions.GetComponent<RectTransform>(), false);
        rt_Image4.anchorMin = new Vector2(0.5f, 0.5f);
        rt_Image4.anchorMax = new Vector2(0.5f, 0.5f);
        rt_Image4.pivot = new Vector2(0.5f, 0.5f);
        rt_Image4.sizeDelta = new Vector2(1024f, 50f);
        rt_Image4.anchoredPosition = new Vector2(0f, 0f);
        go_Image4.AddComponent<CanvasRenderer>();
        var img_Image4 = go_Image4.AddComponent<Image>();
        img_Image4.sprite = SceneBuilderHelper.LoadSprite(Sprite_Button_Bottom);
        img_Image4.color = new Color(1f, 1f, 1f, 1f);
        img_Image4.raycastTarget = true;
        Undo.RegisterCreatedObjectUndo(go_Image4, "Create Image");

        // --- Label ---
        var go_Label10 = new GameObject("Label");
        go_Label10.layer = 5;
        var rt_Label10 = go_Label10.AddComponent<RectTransform>();
        rt_Label10.SetParent(go_SectionOptions.GetComponent<RectTransform>(), false);
        rt_Label10.anchorMin = new Vector2(0f, 0.5f);
        rt_Label10.anchorMax = new Vector2(0f, 0.5f);
        rt_Label10.pivot = new Vector2(0.5f, 0.5f);
        rt_Label10.sizeDelta = new Vector2(200f, 75f);
        rt_Label10.anchoredPosition = new Vector2(125f, 24f);
        go_Label10.AddComponent<CanvasRenderer>();
        var tmp_Label10 = go_Label10.AddComponent<TextMeshProUGUI>();
        tmp_Label10.font = SceneBuilderHelper.LoadFont(Font_Attic);
        tmp_Label10.text = "'Options";
        tmp_Label10.fontSize = 36f;
        tmp_Label10.color = new Color(1f, 1f, 1f, 1f);
        tmp_Label10.alignment = (TextAlignmentOptions)513;
        tmp_Label10.enableWordWrapping = false;
        tmp_Label10.raycastTarget = true;
        Undo.RegisterCreatedObjectUndo(go_Label10, "Create Label");

        // --- StageSelectButton ---
        var go_StageSelectButton = new GameObject("StageSelectButton");
        go_StageSelectButton.layer = 5;
        var rt_StageSelectButton = go_StageSelectButton.AddComponent<RectTransform>();
        rt_StageSelectButton.SetParent(go_Inner.GetComponent<RectTransform>(), false);
        rt_StageSelectButton.anchorMin = new Vector2(0f, 0f);
        rt_StageSelectButton.anchorMax = new Vector2(0f, 0f);
        rt_StageSelectButton.pivot = new Vector2(0.5f, 0.5f);
        rt_StageSelectButton.sizeDelta = new Vector2(512f, 128f);
        rt_StageSelectButton.anchoredPosition = new Vector2(0f, 0f);
        go_StageSelectButton.AddComponent<CanvasRenderer>();
        var img_StageSelectButton = go_StageSelectButton.AddComponent<Image>();
        img_StageSelectButton.sprite = SceneBuilderHelper.LoadSprite(Sprite_Back_512x128);
        img_StageSelectButton.color = new Color(1f, 1f, 1f, 1f);
        img_StageSelectButton.raycastTarget = true;
        var btn_StageSelectButton = go_StageSelectButton.AddComponent<Button>();
        btn_StageSelectButton.navigation = new Navigation { mode = (Navigation.Mode)3 };
        btn_StageSelectButton.targetGraphic = go_StageSelectButton.GetComponent<Image>();
        // TODO: unresolved script GUID=306cc8c2b49d7114eaa3623786fc2126 — component skipped.
        Undo.RegisterCreatedObjectUndo(go_StageSelectButton, "Create StageSelectButton");

        // --- Label ---
        var go_Label9 = new GameObject("Label");
        go_Label9.layer = 5;
        var rt_Label9 = go_Label9.AddComponent<RectTransform>();
        rt_Label9.SetParent(go_StageSelectButton.GetComponent<RectTransform>(), false);
        rt_Label9.anchorMin = new Vector2(0.5f, 0.5f);
        rt_Label9.anchorMax = new Vector2(0.5f, 0.5f);
        rt_Label9.pivot = new Vector2(0.5f, 0.5f);
        rt_Label9.sizeDelta = new Vector2(0f, 128f);
        rt_Label9.anchoredPosition = new Vector2(0f, 0f);
        go_Label9.AddComponent<CanvasRenderer>();
        var tmp_Label9 = go_Label9.AddComponent<TextMeshProUGUI>();
        tmp_Label9.font = SceneBuilderHelper.LoadFont(Font_Attic);
        tmp_Label9.text = "Stage Select";
        tmp_Label9.fontSize = 64f;
        tmp_Label9.color = new Color(1f, 1f, 1f, 1f);
        tmp_Label9.alignment = (TextAlignmentOptions)514;
        tmp_Label9.enableWordWrapping = false;
        tmp_Label9.raycastTarget = true;
        Undo.RegisterCreatedObjectUndo(go_Label9, "Create Label");

        // --- QuitButton ---
        var go_QuitButton = new GameObject("QuitButton");
        go_QuitButton.layer = 5;
        var rt_QuitButton = go_QuitButton.AddComponent<RectTransform>();
        rt_QuitButton.SetParent(go_Inner.GetComponent<RectTransform>(), false);
        rt_QuitButton.anchorMin = new Vector2(0f, 0f);
        rt_QuitButton.anchorMax = new Vector2(0f, 0f);
        rt_QuitButton.pivot = new Vector2(0.5f, 0.5f);
        rt_QuitButton.sizeDelta = new Vector2(512f, 128f);
        rt_QuitButton.anchoredPosition = new Vector2(0f, 0f);
        go_QuitButton.AddComponent<CanvasRenderer>();
        var img_QuitButton = go_QuitButton.AddComponent<Image>();
        img_QuitButton.sprite = SceneBuilderHelper.LoadSprite(Sprite_Back_512x128);
        img_QuitButton.color = new Color(1f, 1f, 1f, 1f);
        img_QuitButton.raycastTarget = true;
        var btn_QuitButton = go_QuitButton.AddComponent<Button>();
        btn_QuitButton.navigation = new Navigation { mode = (Navigation.Mode)3 };
        btn_QuitButton.targetGraphic = go_QuitButton.GetComponent<Image>();
        // TODO: unresolved script GUID=306cc8c2b49d7114eaa3623786fc2126 — component skipped.
        Undo.RegisterCreatedObjectUndo(go_QuitButton, "Create QuitButton");

        // --- Label ---
        var go_Label16 = new GameObject("Label");
        go_Label16.layer = 5;
        var rt_Label16 = go_Label16.AddComponent<RectTransform>();
        rt_Label16.SetParent(go_QuitButton.GetComponent<RectTransform>(), false);
        rt_Label16.anchorMin = new Vector2(0.5f, 0.5f);
        rt_Label16.anchorMax = new Vector2(0.5f, 0.5f);
        rt_Label16.pivot = new Vector2(0.5f, 0.5f);
        rt_Label16.sizeDelta = new Vector2(0f, 128f);
        rt_Label16.anchoredPosition = new Vector2(0f, 0f);
        go_Label16.AddComponent<CanvasRenderer>();
        var tmp_Label16 = go_Label16.AddComponent<TextMeshProUGUI>();
        tmp_Label16.font = SceneBuilderHelper.LoadFont(Font_Attic);
        tmp_Label16.text = "'Quit";
        tmp_Label16.fontSize = 64f;
        tmp_Label16.color = new Color(1f, 1f, 1f, 1f);
        tmp_Label16.alignment = (TextAlignmentOptions)514;
        tmp_Label16.enableWordWrapping = false;
        tmp_Label16.raycastTarget = true;
        Undo.RegisterCreatedObjectUndo(go_Label16, "Create Label");

        // --- SettingsButton ---
        var go_SettingsButton = new GameObject("SettingsButton");
        go_SettingsButton.layer = 5;
        var rt_SettingsButton = go_SettingsButton.AddComponent<RectTransform>();
        rt_SettingsButton.SetParent(go_Inner.GetComponent<RectTransform>(), false);
        rt_SettingsButton.anchorMin = new Vector2(0f, 0f);
        rt_SettingsButton.anchorMax = new Vector2(0f, 0f);
        rt_SettingsButton.pivot = new Vector2(0.5f, 0.5f);
        rt_SettingsButton.sizeDelta = new Vector2(512f, 128f);
        rt_SettingsButton.anchoredPosition = new Vector2(0f, 0f);
        go_SettingsButton.AddComponent<CanvasRenderer>();
        var img_SettingsButton = go_SettingsButton.AddComponent<Image>();
        img_SettingsButton.sprite = SceneBuilderHelper.LoadSprite(Sprite_Back_512x128);
        img_SettingsButton.color = new Color(1f, 1f, 1f, 1f);
        img_SettingsButton.raycastTarget = true;
        var btn_SettingsButton = go_SettingsButton.AddComponent<Button>();
        btn_SettingsButton.navigation = new Navigation { mode = (Navigation.Mode)3 };
        btn_SettingsButton.targetGraphic = go_SettingsButton.GetComponent<Image>();
        // TODO: unresolved script GUID=306cc8c2b49d7114eaa3623786fc2126 — component skipped.
        Undo.RegisterCreatedObjectUndo(go_SettingsButton, "Create SettingsButton");

        // --- Label ---
        var go_Label13 = new GameObject("Label");
        go_Label13.layer = 5;
        var rt_Label13 = go_Label13.AddComponent<RectTransform>();
        rt_Label13.SetParent(go_SettingsButton.GetComponent<RectTransform>(), false);
        rt_Label13.anchorMin = new Vector2(0.5f, 0.5f);
        rt_Label13.anchorMax = new Vector2(0.5f, 0.5f);
        rt_Label13.pivot = new Vector2(0.5f, 0.5f);
        rt_Label13.sizeDelta = new Vector2(0f, 128f);
        rt_Label13.anchoredPosition = new Vector2(0f, 0f);
        go_Label13.AddComponent<CanvasRenderer>();
        var tmp_Label13 = go_Label13.AddComponent<TextMeshProUGUI>();
        tmp_Label13.font = SceneBuilderHelper.LoadFont(Font_Attic);
        tmp_Label13.text = "'Settings";
        tmp_Label13.fontSize = 64f;
        tmp_Label13.color = new Color(1f, 1f, 1f, 1f);
        tmp_Label13.alignment = (TextAlignmentOptions)514;
        tmp_Label13.enableWordWrapping = false;
        tmp_Label13.raycastTarget = true;
        Undo.RegisterCreatedObjectUndo(go_Label13, "Create Label");

        // --- ResumeButton ---
        var go_ResumeButton = new GameObject("ResumeButton");
        go_ResumeButton.layer = 5;
        var rt_ResumeButton = go_ResumeButton.AddComponent<RectTransform>();
        rt_ResumeButton.SetParent(go_Inner.GetComponent<RectTransform>(), false);
        rt_ResumeButton.anchorMin = new Vector2(0f, 0f);
        rt_ResumeButton.anchorMax = new Vector2(0f, 0f);
        rt_ResumeButton.pivot = new Vector2(0.5f, 0.5f);
        rt_ResumeButton.sizeDelta = new Vector2(512f, 128f);
        rt_ResumeButton.anchoredPosition = new Vector2(0f, 0f);
        go_ResumeButton.AddComponent<CanvasRenderer>();
        var img_ResumeButton = go_ResumeButton.AddComponent<Image>();
        img_ResumeButton.sprite = SceneBuilderHelper.LoadSprite(Sprite_Back_512x128);
        img_ResumeButton.color = new Color(1f, 1f, 1f, 1f);
        img_ResumeButton.raycastTarget = true;
        var btn_ResumeButton = go_ResumeButton.AddComponent<Button>();
        btn_ResumeButton.navigation = new Navigation { mode = (Navigation.Mode)3 };
        btn_ResumeButton.targetGraphic = go_ResumeButton.GetComponent<Image>();
        // TODO: unresolved script GUID=306cc8c2b49d7114eaa3623786fc2126 — component skipped.
        Undo.RegisterCreatedObjectUndo(go_ResumeButton, "Create ResumeButton");

        // --- Label ---
        var go_Label14 = new GameObject("Label");
        go_Label14.layer = 5;
        var rt_Label14 = go_Label14.AddComponent<RectTransform>();
        rt_Label14.SetParent(go_ResumeButton.GetComponent<RectTransform>(), false);
        rt_Label14.anchorMin = new Vector2(0.5f, 0.5f);
        rt_Label14.anchorMax = new Vector2(0.5f, 0.5f);
        rt_Label14.pivot = new Vector2(0.5f, 0.5f);
        rt_Label14.sizeDelta = new Vector2(0f, 128f);
        rt_Label14.anchoredPosition = new Vector2(0f, 0f);
        go_Label14.AddComponent<CanvasRenderer>();
        var tmp_Label14 = go_Label14.AddComponent<TextMeshProUGUI>();
        tmp_Label14.font = SceneBuilderHelper.LoadFont(Font_Attic);
        tmp_Label14.text = "Resume";
        tmp_Label14.fontSize = 64f;
        tmp_Label14.color = new Color(1f, 1f, 1f, 1f);
        tmp_Label14.alignment = (TextAlignmentOptions)514;
        tmp_Label14.enableWordWrapping = false;
        tmp_Label14.raycastTarget = true;
        Undo.RegisterCreatedObjectUndo(go_Label14, "Create Label");

        // --- RunAwayButton ---
        var go_RunAwayButton = new GameObject("RunAwayButton");
        go_RunAwayButton.layer = 5;
        var rt_RunAwayButton = go_RunAwayButton.AddComponent<RectTransform>();
        rt_RunAwayButton.SetParent(go_Inner.GetComponent<RectTransform>(), false);
        rt_RunAwayButton.anchorMin = new Vector2(0f, 0f);
        rt_RunAwayButton.anchorMax = new Vector2(0f, 0f);
        rt_RunAwayButton.pivot = new Vector2(0.5f, 0.5f);
        rt_RunAwayButton.sizeDelta = new Vector2(512f, 128f);
        rt_RunAwayButton.anchoredPosition = new Vector2(0f, 0f);
        go_RunAwayButton.AddComponent<CanvasRenderer>();
        var img_RunAwayButton = go_RunAwayButton.AddComponent<Image>();
        img_RunAwayButton.sprite = SceneBuilderHelper.LoadSprite(Sprite_Back_512x128);
        img_RunAwayButton.color = new Color(1f, 1f, 1f, 1f);
        img_RunAwayButton.raycastTarget = true;
        var btn_RunAwayButton = go_RunAwayButton.AddComponent<Button>();
        btn_RunAwayButton.navigation = new Navigation { mode = (Navigation.Mode)3 };
        btn_RunAwayButton.targetGraphic = go_RunAwayButton.GetComponent<Image>();
        // TODO: unresolved script GUID=306cc8c2b49d7114eaa3623786fc2126 — component skipped.
        Undo.RegisterCreatedObjectUndo(go_RunAwayButton, "Create RunAwayButton");

        // --- Label ---
        var go_Label11 = new GameObject("Label");
        go_Label11.layer = 5;
        var rt_Label11 = go_Label11.AddComponent<RectTransform>();
        rt_Label11.SetParent(go_RunAwayButton.GetComponent<RectTransform>(), false);
        rt_Label11.anchorMin = new Vector2(0.5f, 0.5f);
        rt_Label11.anchorMax = new Vector2(0.5f, 0.5f);
        rt_Label11.pivot = new Vector2(0.5f, 0.5f);
        rt_Label11.sizeDelta = new Vector2(0f, 128f);
        rt_Label11.anchoredPosition = new Vector2(0f, 0f);
        go_Label11.AddComponent<CanvasRenderer>();
        var tmp_Label11 = go_Label11.AddComponent<TextMeshProUGUI>();
        tmp_Label11.font = SceneBuilderHelper.LoadFont(Font_Attic);
        tmp_Label11.text = "Run Away";
        tmp_Label11.fontSize = 64f;
        tmp_Label11.color = new Color(1f, 1f, 1f, 1f);
        tmp_Label11.alignment = (TextAlignmentOptions)514;
        tmp_Label11.enableWordWrapping = false;
        tmp_Label11.raycastTarget = true;
        Undo.RegisterCreatedObjectUndo(go_Label11, "Create Label");

        // --- PartyManagerButton ---
        var go_PartyManagerButton = new GameObject("PartyManagerButton");
        go_PartyManagerButton.layer = 5;
        var rt_PartyManagerButton = go_PartyManagerButton.AddComponent<RectTransform>();
        rt_PartyManagerButton.SetParent(go_Inner.GetComponent<RectTransform>(), false);
        rt_PartyManagerButton.anchorMin = new Vector2(0f, 0f);
        rt_PartyManagerButton.anchorMax = new Vector2(0f, 0f);
        rt_PartyManagerButton.pivot = new Vector2(0.5f, 0.5f);
        rt_PartyManagerButton.sizeDelta = new Vector2(512f, 128f);
        rt_PartyManagerButton.anchoredPosition = new Vector2(0f, 0f);
        go_PartyManagerButton.AddComponent<CanvasRenderer>();
        var img_PartyManagerButton = go_PartyManagerButton.AddComponent<Image>();
        img_PartyManagerButton.sprite = SceneBuilderHelper.LoadSprite(Sprite_Back_512x128);
        img_PartyManagerButton.color = new Color(1f, 1f, 1f, 1f);
        img_PartyManagerButton.raycastTarget = true;
        var btn_PartyManagerButton = go_PartyManagerButton.AddComponent<Button>();
        btn_PartyManagerButton.navigation = new Navigation { mode = (Navigation.Mode)3 };
        btn_PartyManagerButton.targetGraphic = go_PartyManagerButton.GetComponent<Image>();
        // TODO: unresolved script GUID=306cc8c2b49d7114eaa3623786fc2126 — component skipped.
        Undo.RegisterCreatedObjectUndo(go_PartyManagerButton, "Create PartyManagerButton");

        // --- Label ---
        var go_Label12 = new GameObject("Label");
        go_Label12.layer = 5;
        var rt_Label12 = go_Label12.AddComponent<RectTransform>();
        rt_Label12.SetParent(go_PartyManagerButton.GetComponent<RectTransform>(), false);
        rt_Label12.anchorMin = new Vector2(0.5f, 0.5f);
        rt_Label12.anchorMax = new Vector2(0.5f, 0.5f);
        rt_Label12.pivot = new Vector2(0.5f, 0.5f);
        rt_Label12.sizeDelta = new Vector2(0f, 128f);
        rt_Label12.anchoredPosition = new Vector2(0f, 0f);
        go_Label12.AddComponent<CanvasRenderer>();
        var tmp_Label12 = go_Label12.AddComponent<TextMeshProUGUI>();
        tmp_Label12.font = SceneBuilderHelper.LoadFont(Font_Attic);
        tmp_Label12.text = "Party";
        tmp_Label12.fontSize = 64f;
        tmp_Label12.color = new Color(1f, 1f, 1f, 1f);
        tmp_Label12.alignment = (TextAlignmentOptions)514;
        tmp_Label12.enableWordWrapping = false;
        tmp_Label12.raycastTarget = true;
        Undo.RegisterCreatedObjectUndo(go_Label12, "Create Label");

        // --- RestartStageButton ---
        var go_RestartStageButton = new GameObject("RestartStageButton");
        go_RestartStageButton.layer = 5;
        var rt_RestartStageButton = go_RestartStageButton.AddComponent<RectTransform>();
        rt_RestartStageButton.SetParent(go_Inner.GetComponent<RectTransform>(), false);
        rt_RestartStageButton.anchorMin = new Vector2(0f, 0f);
        rt_RestartStageButton.anchorMax = new Vector2(0f, 0f);
        rt_RestartStageButton.pivot = new Vector2(0.5f, 0.5f);
        rt_RestartStageButton.sizeDelta = new Vector2(512f, 128f);
        rt_RestartStageButton.anchoredPosition = new Vector2(0f, 0f);
        go_RestartStageButton.AddComponent<CanvasRenderer>();
        var img_RestartStageButton = go_RestartStageButton.AddComponent<Image>();
        img_RestartStageButton.sprite = SceneBuilderHelper.LoadSprite(Sprite_Back_512x128);
        img_RestartStageButton.color = new Color(1f, 1f, 1f, 1f);
        img_RestartStageButton.raycastTarget = true;
        var btn_RestartStageButton = go_RestartStageButton.AddComponent<Button>();
        btn_RestartStageButton.navigation = new Navigation { mode = (Navigation.Mode)3 };
        btn_RestartStageButton.targetGraphic = go_RestartStageButton.GetComponent<Image>();
        // TODO: unresolved script GUID=306cc8c2b49d7114eaa3623786fc2126 — component skipped.
        Undo.RegisterCreatedObjectUndo(go_RestartStageButton, "Create RestartStageButton");

        // --- Label ---
        var go_Label15 = new GameObject("Label");
        go_Label15.layer = 5;
        var rt_Label15 = go_Label15.AddComponent<RectTransform>();
        rt_Label15.SetParent(go_RestartStageButton.GetComponent<RectTransform>(), false);
        rt_Label15.anchorMin = new Vector2(0.5f, 0.5f);
        rt_Label15.anchorMax = new Vector2(0.5f, 0.5f);
        rt_Label15.pivot = new Vector2(0.5f, 0.5f);
        rt_Label15.sizeDelta = new Vector2(0f, 128f);
        rt_Label15.anchoredPosition = new Vector2(0f, 0f);
        go_Label15.AddComponent<CanvasRenderer>();
        var tmp_Label15 = go_Label15.AddComponent<TextMeshProUGUI>();
        tmp_Label15.font = SceneBuilderHelper.LoadFont(Font_Attic);
        tmp_Label15.text = "Restart Stage";
        tmp_Label15.fontSize = 64f;
        tmp_Label15.color = new Color(1f, 1f, 1f, 1f);
        tmp_Label15.alignment = (TextAlignmentOptions)514;
        tmp_Label15.enableWordWrapping = false;
        tmp_Label15.raycastTarget = true;
        Undo.RegisterCreatedObjectUndo(go_Label15, "Create Label");

        // --- CreateSaveGameButton ---
        var go_CreateSaveGameButton = new GameObject("CreateSaveGameButton");
        go_CreateSaveGameButton.layer = 5;
        var rt_CreateSaveGameButton = go_CreateSaveGameButton.AddComponent<RectTransform>();
        rt_CreateSaveGameButton.SetParent(go_Inner.GetComponent<RectTransform>(), false);
        rt_CreateSaveGameButton.anchorMin = new Vector2(0f, 0f);
        rt_CreateSaveGameButton.anchorMax = new Vector2(0f, 0f);
        rt_CreateSaveGameButton.pivot = new Vector2(0.5f, 0.5f);
        rt_CreateSaveGameButton.sizeDelta = new Vector2(512f, 128f);
        rt_CreateSaveGameButton.anchoredPosition = new Vector2(0f, 0f);
        go_CreateSaveGameButton.AddComponent<CanvasRenderer>();
        var img_CreateSaveGameButton = go_CreateSaveGameButton.AddComponent<Image>();
        img_CreateSaveGameButton.sprite = SceneBuilderHelper.LoadSprite(Sprite_Back_512x128);
        img_CreateSaveGameButton.color = new Color(1f, 1f, 1f, 1f);
        img_CreateSaveGameButton.raycastTarget = true;
        var btn_CreateSaveGameButton = go_CreateSaveGameButton.AddComponent<Button>();
        btn_CreateSaveGameButton.navigation = new Navigation { mode = (Navigation.Mode)3 };
        btn_CreateSaveGameButton.targetGraphic = go_CreateSaveGameButton.GetComponent<Image>();
        // TODO: unresolved script GUID=306cc8c2b49d7114eaa3623786fc2126 — component skipped.
        Undo.RegisterCreatedObjectUndo(go_CreateSaveGameButton, "Create CreateSaveGameButton");

        // --- Label ---
        var go_Label17 = new GameObject("Label");
        go_Label17.layer = 5;
        var rt_Label17 = go_Label17.AddComponent<RectTransform>();
        rt_Label17.SetParent(go_CreateSaveGameButton.GetComponent<RectTransform>(), false);
        rt_Label17.anchorMin = new Vector2(0.5f, 0.5f);
        rt_Label17.anchorMax = new Vector2(0.5f, 0.5f);
        rt_Label17.pivot = new Vector2(0.5f, 0.5f);
        rt_Label17.sizeDelta = new Vector2(0f, 128f);
        rt_Label17.anchoredPosition = new Vector2(0f, 0f);
        go_Label17.AddComponent<CanvasRenderer>();
        var tmp_Label17 = go_Label17.AddComponent<TextMeshProUGUI>();
        tmp_Label17.font = SceneBuilderHelper.LoadFont(Font_Attic);
        tmp_Label17.text = "New Save";
        tmp_Label17.fontSize = 64f;
        tmp_Label17.color = new Color(1f, 1f, 1f, 1f);
        tmp_Label17.alignment = (TextAlignmentOptions)514;
        tmp_Label17.enableWordWrapping = false;
        tmp_Label17.raycastTarget = true;
        Undo.RegisterCreatedObjectUndo(go_Label17, "Create Label");

        // --- QuickSaveGameButton ---
        var go_QuickSaveGameButton = new GameObject("QuickSaveGameButton");
        go_QuickSaveGameButton.layer = 5;
        var rt_QuickSaveGameButton = go_QuickSaveGameButton.AddComponent<RectTransform>();
        rt_QuickSaveGameButton.SetParent(go_Inner.GetComponent<RectTransform>(), false);
        rt_QuickSaveGameButton.anchorMin = new Vector2(0f, 0f);
        rt_QuickSaveGameButton.anchorMax = new Vector2(0f, 0f);
        rt_QuickSaveGameButton.pivot = new Vector2(0.5f, 0.5f);
        rt_QuickSaveGameButton.sizeDelta = new Vector2(512f, 128f);
        rt_QuickSaveGameButton.anchoredPosition = new Vector2(0f, 0f);
        go_QuickSaveGameButton.AddComponent<CanvasRenderer>();
        var img_QuickSaveGameButton = go_QuickSaveGameButton.AddComponent<Image>();
        img_QuickSaveGameButton.sprite = SceneBuilderHelper.LoadSprite(Sprite_Back_512x128);
        img_QuickSaveGameButton.color = new Color(1f, 1f, 1f, 1f);
        img_QuickSaveGameButton.raycastTarget = true;
        var btn_QuickSaveGameButton = go_QuickSaveGameButton.AddComponent<Button>();
        btn_QuickSaveGameButton.navigation = new Navigation { mode = (Navigation.Mode)3 };
        btn_QuickSaveGameButton.targetGraphic = go_QuickSaveGameButton.GetComponent<Image>();
        // TODO: unresolved script GUID=306cc8c2b49d7114eaa3623786fc2126 — component skipped.
        Undo.RegisterCreatedObjectUndo(go_QuickSaveGameButton, "Create QuickSaveGameButton");

        // --- Label ---
        var go_Label8 = new GameObject("Label");
        go_Label8.layer = 5;
        var rt_Label8 = go_Label8.AddComponent<RectTransform>();
        rt_Label8.SetParent(go_QuickSaveGameButton.GetComponent<RectTransform>(), false);
        rt_Label8.anchorMin = new Vector2(0.5f, 0.5f);
        rt_Label8.anchorMax = new Vector2(0.5f, 0.5f);
        rt_Label8.pivot = new Vector2(0.5f, 0.5f);
        rt_Label8.sizeDelta = new Vector2(0f, 128f);
        rt_Label8.anchoredPosition = new Vector2(0f, 0f);
        go_Label8.AddComponent<CanvasRenderer>();
        var tmp_Label8 = go_Label8.AddComponent<TextMeshProUGUI>();
        tmp_Label8.font = SceneBuilderHelper.LoadFont(Font_Attic);
        tmp_Label8.text = "Quick Save";
        tmp_Label8.fontSize = 64f;
        tmp_Label8.color = new Color(1f, 1f, 1f, 1f);
        tmp_Label8.alignment = (TextAlignmentOptions)514;
        tmp_Label8.enableWordWrapping = false;
        tmp_Label8.raycastTarget = true;
        Undo.RegisterCreatedObjectUndo(go_Label8, "Create Label");

        // --- SectionGameplay ---
        var go_SectionGameplay = new GameObject("SectionGameplay");
        go_SectionGameplay.layer = 5;
        var rt_SectionGameplay = go_SectionGameplay.AddComponent<RectTransform>();
        rt_SectionGameplay.SetParent(go_Inner.GetComponent<RectTransform>(), false);
        rt_SectionGameplay.anchorMin = new Vector2(0f, 0f);
        rt_SectionGameplay.anchorMax = new Vector2(0f, 0f);
        rt_SectionGameplay.pivot = new Vector2(0.5f, 0.5f);
        rt_SectionGameplay.sizeDelta = new Vector2(1024f, 100f);
        rt_SectionGameplay.anchoredPosition = new Vector2(0f, 0f);
        go_SectionGameplay.AddComponent<CanvasRenderer>();
        var img_SectionGameplay = go_SectionGameplay.AddComponent<Image>();
        img_SectionGameplay.color = new Color(0f, 0f, 0f, 0f);
        img_SectionGameplay.raycastTarget = true;
        Undo.RegisterCreatedObjectUndo(go_SectionGameplay, "Create SectionGameplay");

        // --- Label ---
        var go_Label7 = new GameObject("Label");
        go_Label7.layer = 5;
        var rt_Label7 = go_Label7.AddComponent<RectTransform>();
        rt_Label7.SetParent(go_SectionGameplay.GetComponent<RectTransform>(), false);
        rt_Label7.anchorMin = new Vector2(0f, 0.5f);
        rt_Label7.anchorMax = new Vector2(0f, 0.5f);
        rt_Label7.pivot = new Vector2(0.5f, 0.5f);
        rt_Label7.sizeDelta = new Vector2(200f, 75f);
        rt_Label7.anchoredPosition = new Vector2(125f, 24f);
        go_Label7.AddComponent<CanvasRenderer>();
        var tmp_Label7 = go_Label7.AddComponent<TextMeshProUGUI>();
        tmp_Label7.font = SceneBuilderHelper.LoadFont(Font_Attic);
        tmp_Label7.text = "'Gameplay";
        tmp_Label7.fontSize = 36f;
        tmp_Label7.color = new Color(1f, 1f, 1f, 1f);
        tmp_Label7.alignment = (TextAlignmentOptions)513;
        tmp_Label7.enableWordWrapping = false;
        tmp_Label7.raycastTarget = true;
        Undo.RegisterCreatedObjectUndo(go_Label7, "Create Label");

        // --- Image ---
        var go_Image5 = new GameObject("Image");
        go_Image5.layer = 5;
        var rt_Image5 = go_Image5.AddComponent<RectTransform>();
        rt_Image5.SetParent(go_SectionGameplay.GetComponent<RectTransform>(), false);
        rt_Image5.anchorMin = new Vector2(0.5f, 0.5f);
        rt_Image5.anchorMax = new Vector2(0.5f, 0.5f);
        rt_Image5.pivot = new Vector2(0.5f, 0.5f);
        rt_Image5.sizeDelta = new Vector2(1024f, 50f);
        rt_Image5.anchoredPosition = new Vector2(0f, 0f);
        go_Image5.AddComponent<CanvasRenderer>();
        var img_Image5 = go_Image5.AddComponent<Image>();
        img_Image5.sprite = SceneBuilderHelper.LoadSprite(Sprite_Button_Bottom);
        img_Image5.color = new Color(1f, 1f, 1f, 1f);
        img_Image5.raycastTarget = true;
        Undo.RegisterCreatedObjectUndo(go_Image5, "Create Image");

        // --- Canvas3D ---
        var go_Canvas3D = new GameObject("Canvas3D");
        go_Canvas3D.AddComponent<RectTransform>();
        var canvas_Canvas3D = go_Canvas3D.AddComponent<Canvas>();
        canvas_Canvas3D.renderMode = (RenderMode)2;
        canvas_Canvas3D.sortingOrder = 1;
        go_Canvas3D.AddComponent<GraphicRaycaster>();
        var scaler_Canvas3D = go_Canvas3D.AddComponent<CanvasScaler>();
        scaler_Canvas3D.uiScaleMode = (CanvasScaler.ScaleMode)0;
        scaler_Canvas3D.referenceResolution = new Vector2(0f, 0f);
        scaler_Canvas3D.matchWidthOrHeight = 0f;
        Undo.RegisterCreatedObjectUndo(go_Canvas3D, "Create Canvas3D");

        // --- Effects ---
        var go_Effects = new GameObject("Effects");
        Undo.RegisterCreatedObjectUndo(go_Effects, "Create Effects");

        // --- EventSystem ---
        var go_EventSystem = new GameObject("EventSystem");
        go_EventSystem.AddComponent<StandaloneInputModule>();
        go_EventSystem.AddComponent<EventSystem>();
        Undo.RegisterCreatedObjectUndo(go_EventSystem, "Create EventSystem");

        // --- FocusIndicator ---
        var go_FocusIndicator = new GameObject("FocusIndicator");
        go_FocusIndicator.transform.SetParent(go_Board.transform, false);
        go_FocusIndicator.layer = 10;
        go_FocusIndicator.AddComponent<Scripts.Instances.Board.FocusIndicator>();
        Undo.RegisterCreatedObjectUndo(go_FocusIndicator, "Create FocusIndicator");

        // --- Game ---
        var go_Game = new GameObject("Game");
        // GameManager expects sources[0]=sound, sources[1]=music. Matches Game_Hierarchy.md.
        go_Game.AddComponent<AudioSource>();
        go_Game.AddComponent<AudioSource>();
        go_Game.AddComponent<Scripts.Managers.CameraManager>();
        go_Game.AddComponent<Scripts.Managers.TurnManager>();
        go_Game.AddComponent<Scripts.Managers.InputManager>();
        go_Game.AddComponent<Scripts.Managers.GameManager>();
        go_Game.AddComponent<Scripts.Managers.StageManager>();
        go_Game.AddComponent<Scripts.Managers.BoardManager>();
        go_Game.AddComponent<Scripts.Managers.ActorManager>();
        go_Game.AddComponent<Scripts.Managers.SupportLineManager>();
        go_Game.AddComponent<Scripts.Managers.AttackLineManager>();
        go_Game.AddComponent<Scripts.Managers.CombatTextManager>();
        go_Game.AddComponent<Scripts.Managers.GhostManager>();
        go_Game.AddComponent<Scripts.Managers.SelectionManager>();
        go_Game.AddComponent<Scripts.Managers.HeroManager>();
        go_Game.AddComponent<Scripts.Managers.EnemyManager>();
        go_Game.AddComponent<Scripts.Managers.TileManager>();
        go_Game.AddComponent<Scripts.Managers.FootstepManager>();
        go_Game.AddComponent<Scripts.Managers.AudioManager>();
        go_Game.AddComponent<Scripts.Managers.VisualEffectManager>();
        go_Game.AddComponent<Scripts.Managers.CoinManager>();
        go_Game.AddComponent<Scripts.Managers.ItemPickupManager>();
        go_Game.AddComponent<Scripts.Managers.DebugManager>();
        go_Game.AddComponent<Scripts.Managers.ConsoleManager>();
        go_Game.AddComponent<Scripts.Managers.LogManager>();
        go_Game.AddComponent<Scripts.Managers.DottedLineManager>();
        go_Game.AddComponent<Scripts.Managers.ProjectileManager>();
        go_Game.AddComponent<Scripts.Managers.SequenceManager>();
        go_Game.AddComponent<Scripts.Managers.PincerAttackManager>();
        go_Game.AddComponent<Scripts.Managers.SortingManager>();
        go_Game.AddComponent<Scripts.Managers.TargetLineManager>();
        go_Game.AddComponent<Scripts.Managers.AbilityButtonManager>();
        go_Game.AddComponent<Scripts.Managers.SynergyLineManager>();
        go_Game.AddComponent<Scripts.Managers.AbilityManager>();
        go_Game.AddComponent<Scripts.Managers.ManaPoolManager>();
        go_Game.AddComponent<Scripts.Managers.PortraitManager>();
        Undo.RegisterCreatedObjectUndo(go_Game, "Create Game");

        // --- Main Camera ---
        var go_Main_Camera = new GameObject("Main Camera");
        go_Main_Camera.tag = "MainCamera";
        go_Main_Camera.transform.position = new Vector3(0f, 0f, -10f);
        var cam_Main_Camera = go_Main_Camera.AddComponent<Camera>();
        cam_Main_Camera.orthographic = true;
        cam_Main_Camera.orthographicSize = 5f;
        cam_Main_Camera.depth = -1f;
        cam_Main_Camera.clearFlags = (CameraClearFlags)2;
        cam_Main_Camera.backgroundColor = new Color(0.1921569f, 0.3019608f, 0.4745098f, 0f);
        go_Main_Camera.AddComponent<AudioListener>();
        go_Main_Camera.AddComponent<Scripts.Managers.CameraManager>();
        var urp_Main_Camera = go_Main_Camera.AddComponent<UnityEngine.Rendering.Universal.UniversalAdditionalCameraData>();
        urp_Main_Camera.renderType = UnityEngine.Rendering.Universal.CameraRenderType.Base;
        Undo.RegisterCreatedObjectUndo(go_Main_Camera, "Create Main Camera");

        // --- Overlay Camera ---
        var go_Overlay_Camera = new GameObject("Overlay Camera");
        go_Overlay_Camera.transform.position = new Vector3(0f, 0f, -10f);
        var cam_Overlay_Camera = go_Overlay_Camera.AddComponent<Camera>();
        cam_Overlay_Camera.orthographic = true;
        cam_Overlay_Camera.orthographicSize = 5f;
        cam_Overlay_Camera.depth = 1f;
        cam_Overlay_Camera.clearFlags = (CameraClearFlags)4;
        cam_Overlay_Camera.backgroundColor = new Color(0.1921569f, 0.3019608f, 0.4745098f, 0f);
        var urp_Overlay_Camera = go_Overlay_Camera.AddComponent<UnityEngine.Rendering.Universal.UniversalAdditionalCameraData>();
        urp_Overlay_Camera.renderType = UnityEngine.Rendering.Universal.CameraRenderType.Overlay;
        urp_Main_Camera.cameraStack.Add(cam_Overlay_Camera);
        Undo.RegisterCreatedObjectUndo(go_Overlay_Camera, "Create Overlay Camera");

        // --- PostProcessing ---
        var go_PostProcessing = new GameObject("PostProcessing");
        // TODO: unresolved script GUID=172515602e62fb746b5d573b38a5fe58 — component skipped.
        Undo.RegisterCreatedObjectUndo(go_PostProcessing, "Create PostProcessing");

        // --- TargetModeOverlay ---
        var go_TargetModeOverlay = new GameObject("TargetModeOverlay");
        go_TargetModeOverlay.transform.SetParent(go_Board.transform, false);
        go_TargetModeOverlay.AddComponent<CanvasRenderer>();
        go_TargetModeOverlay.AddComponent<Scripts.Canvas.TargetModeOverlay>();
        Undo.RegisterCreatedObjectUndo(go_TargetModeOverlay, "Create TargetModeOverlay");

        // --- onClick event wiring ---
        SceneBuilderHelper.WireOnClick(
            go_CancelButton.GetComponent<Button>(),
            new UnityAction(go_Game.GetComponent<Scripts.Managers.InputManager>().OnCancelButtonClickedEvent));
        SceneBuilderHelper.WireOnClick(
            go_CastButton.GetComponent<Button>(),
            new UnityAction(go_Game.GetComponent<Scripts.Managers.AbilityManager>().OnCastButtonClicked));
        // (Hero-cycle arrows + tab buttons now live inside ActorPanel and wire their own onClick
        //  in ActorPanel.BuildUi — no scene-side wiring needed.)
        // PHASE B: BankButton onClick removed — the legacy button is gone (see ManaPool removal
        // block above). The new Shield button is runtime-spawned and wires its own click in
        // ShieldButtonFactory (which calls BuffSystem.ApplyToAllHeroes(Protection) +
        // ManaPoolManager.OnBankButtonClicked() for the timeline auto-skip side of the flow).
        SceneBuilderHelper.WireOnClick(
            go_QuickSaveGameButton.GetComponent<Button>(),
            new UnityAction(go_PauseMenu.GetComponent<Scripts.Managers.PauseMenu>().OnQuickSaveGameButtonClicked));
        SceneBuilderHelper.WireOnClick(
            go_StageSelectButton.GetComponent<Button>(),
            new UnityAction(go_PauseMenu.GetComponent<Scripts.Managers.PauseMenu>().OnStageSelectButtonClicked));
        SceneBuilderHelper.WireOnClick(
            go_CreateSaveGameButton.GetComponent<Button>(),
            new UnityAction(go_PauseMenu.GetComponent<Scripts.Managers.PauseMenu>().OnCreateSaveGameButtonClicked));
        SceneBuilderHelper.WireOnClick(
            go_RunAwayButton.GetComponent<Button>(),
            new UnityAction(go_PauseMenu.GetComponent<Scripts.Managers.PauseMenu>().OnRunAwayClicked));
        SceneBuilderHelper.WireOnClick(
            go_SettingsButton.GetComponent<Button>(),
            new UnityAction(go_PauseMenu.GetComponent<Scripts.Managers.PauseMenu>().OnSettingsButtonClicked));
        SceneBuilderHelper.WireOnClick(
            go_RestartStageButton.GetComponent<Button>(),
            new UnityAction(go_PauseMenu.GetComponent<Scripts.Managers.PauseMenu>().OnRestartStageButtonClicked));
        SceneBuilderHelper.WireOnClick(
            go_ResumeButton.GetComponent<Button>(),
            new UnityAction(go_PauseMenu.GetComponent<Scripts.Managers.PauseMenu>().OnResumeButtonClicked));
        // TODO: unresolved onClick — go_QuitButton → PauseMenu.OnTitleScreenButtonClicked
        SceneBuilderHelper.WireOnClick(
            go_PartyManagerButton.GetComponent<Button>(),
            new UnityAction(go_PauseMenu.GetComponent<Scripts.Managers.PauseMenu>().OnPartyManagerButtonClicked));

        UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(
            UnityEditor.SceneManagement.EditorSceneManager.GetActiveScene());

        TryAddAltRunner();
    }

    /// <summary>If the AltTester SDK is installed (com.alttester.sdk), drop an AltRunner
    /// GameObject into the scene so PlayMode tests can connect via the AltDriver WebSocket
    /// bridge. Uses reflection so this file has zero compile-time dependency on the package
    /// — when the SDK is absent, the lookup returns null and the method silently no-ops.
    /// Re-running Checkout on Game.scene with AltTester installed re-adds the runner.</summary>
    private static void TryAddAltRunner()
    {
        const string AltRunnerFullName = "AltTester.AltTesterUnitySDK.Commands.AltRunner";

        System.Type altRunnerType = null;
        foreach (var asm in System.AppDomain.CurrentDomain.GetAssemblies())
        {
            try
            {
                altRunnerType = asm.GetType(AltRunnerFullName, throwOnError: false);
                if (altRunnerType != null) break;
            }
            catch { /* skip assemblies that can't be reflected */ }
        }
        if (altRunnerType == null) return;  // SDK not installed — silently skip.

        // Skip if already present (idempotent).
        var existing = GameObject.Find("AltRunner");
        if (existing != null) return;

        var go = new GameObject("AltRunner");
        go.AddComponent(altRunnerType);
        // Runtime guard: destroys the GameObject in non-development builds so AltTester's
        // WebSocket server never opens in production. See AltTesterGuard.cs for why this
        // can't be an Editor-time check.
        go.AddComponent<Scripts.Helpers.AltTesterGuard>();
        UnityEditor.Undo.RegisterCreatedObjectUndo(go, "Create AltRunner");
        UnityEngine.Debug.Log("[GameBuilder] AltTester detected — AltRunner added to Game.unity for PlayMode test bridge.");
    }
}
