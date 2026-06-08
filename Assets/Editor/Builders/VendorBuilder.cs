using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Events;
using UnityEngine.EventSystems;
using UnityEditor;
using TMPro;

public static class VendorBuilder
{
    private const string SceneName = "Vendor";

    // Sprite asset paths
    private const string Sprite_Black32x32 = "Assets/Sprites/Black32x32.png";
    private const string Sprite_GunMetal16x16 = "Assets/Sprites/GunMetal16x16.png";

    // Font asset paths
    private const string Font_Attic = "Assets/Fonts/Attic.asset";

    public static void Build()
    {
        if (!SceneBuilderHelper.OpenScene(SceneName)) return;

        // --- Canvas ---
        var go_Canvas = new GameObject("Canvas");
        go_Canvas.layer = 5;
        go_Canvas.AddComponent<RectTransform>();
        var canvas_Canvas = go_Canvas.AddComponent<Canvas>();
        canvas_Canvas.renderMode = (RenderMode)0;
        go_Canvas.AddComponent<GraphicRaycaster>();
        var scaler_Canvas = go_Canvas.AddComponent<CanvasScaler>();
        scaler_Canvas.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler_Canvas.referenceResolution = new Vector2(1170f, 2532f);
        scaler_Canvas.matchWidthOrHeight = 0.5f;
        go_Canvas.AddComponent<CanvasRenderer>();
        var img_Canvas = go_Canvas.AddComponent<Image>();
        img_Canvas.sprite = SceneBuilderHelper.LoadSprite(Sprite_GunMetal16x16);
        img_Canvas.color = new Color(0.06f, 0.08f, 0.14f, 0.92f);
        img_Canvas.raycastTarget = true;
        Undo.RegisterCreatedObjectUndo(go_Canvas, "Create Canvas");

        // --- Header ---
        var go_Header = new GameObject("Header");
        go_Header.layer = 5;
        var rt_Header = go_Header.AddComponent<RectTransform>();
        rt_Header.SetParent(go_Canvas.GetComponent<RectTransform>(), false);
        rt_Header.anchorMin = new Vector2(0f, 0.9f);
        rt_Header.anchorMax = new Vector2(1f, 1f);
        rt_Header.pivot = new Vector2(0.5f, 0.5f);
        rt_Header.sizeDelta = new Vector2(0f, 0f);
        rt_Header.anchoredPosition = new Vector2(0f, 0f);
        go_Header.AddComponent<CanvasRenderer>();
        var img_Header = go_Header.AddComponent<Image>();
        img_Header.color = new Color(0.1f, 0.14f, 0.24f, 1f);
        img_Header.raycastTarget = true;
        Undo.RegisterCreatedObjectUndo(go_Header, "Create Header");

        // --- TitleLabel ---
        var go_TitleLabel = new GameObject("TitleLabel");
        go_TitleLabel.layer = 5;
        var rt_TitleLabel = go_TitleLabel.AddComponent<RectTransform>();
        rt_TitleLabel.SetParent(go_Header.GetComponent<RectTransform>(), false);
        rt_TitleLabel.anchorMin = new Vector2(0f, 0f);
        rt_TitleLabel.anchorMax = new Vector2(1f, 1f);
        rt_TitleLabel.pivot = new Vector2(0.5f, 0.5f);
        rt_TitleLabel.sizeDelta = new Vector2(-128f, 0f);
        rt_TitleLabel.anchoredPosition = new Vector2(32f, 0f);
        go_TitleLabel.AddComponent<CanvasRenderer>();
        var tmp_TitleLabel = go_TitleLabel.AddComponent<TextMeshProUGUI>();
        tmp_TitleLabel.font = SceneBuilderHelper.LoadFont(Font_Attic);
        tmp_TitleLabel.text = "Merchant";
        tmp_TitleLabel.fontSize = 56f;
        tmp_TitleLabel.color = new Color(1f, 0.78f, 0.28f, 1f);
        tmp_TitleLabel.alignment = (TextAlignmentOptions)4097;
        tmp_TitleLabel.enableWordWrapping = true;
        tmp_TitleLabel.raycastTarget = false;
        Undo.RegisterCreatedObjectUndo(go_TitleLabel, "Create TitleLabel");

        // --- ModeBar ---
        var go_ModeBar = new GameObject("ModeBar");
        go_ModeBar.layer = 5;
        var rt_ModeBar = go_ModeBar.AddComponent<RectTransform>();
        rt_ModeBar.SetParent(go_Canvas.GetComponent<RectTransform>(), false);
        rt_ModeBar.anchorMin = new Vector2(0f, 0.8f);
        rt_ModeBar.anchorMax = new Vector2(1f, 0.9f);
        rt_ModeBar.pivot = new Vector2(0.5f, 0.5f);
        rt_ModeBar.sizeDelta = new Vector2(0f, 0f);
        rt_ModeBar.anchoredPosition = new Vector2(0f, 0f);
        go_ModeBar.AddComponent<CanvasRenderer>();
        var img_ModeBar = go_ModeBar.AddComponent<Image>();
        img_ModeBar.color = new Color(0f, 0f, 0f, 0f);
        img_ModeBar.raycastTarget = false;
        var hlg_ModeBar = go_ModeBar.AddComponent<HorizontalLayoutGroup>();
        hlg_ModeBar.spacing = 16f;
        hlg_ModeBar.childAlignment = (TextAnchor)4;
        hlg_ModeBar.childControlWidth = true;
        hlg_ModeBar.childControlHeight = true;
        hlg_ModeBar.childForceExpandWidth = true;
        hlg_ModeBar.childForceExpandHeight = true;
        Undo.RegisterCreatedObjectUndo(go_ModeBar, "Create ModeBar");

        // --- SellTabButton ---
        var go_SellTabButton = new GameObject("SellTabButton");
        go_SellTabButton.layer = 5;
        var rt_SellTabButton = go_SellTabButton.AddComponent<RectTransform>();
        rt_SellTabButton.SetParent(go_ModeBar.GetComponent<RectTransform>(), false);
        rt_SellTabButton.anchorMin = new Vector2(0f, 0f);
        rt_SellTabButton.anchorMax = new Vector2(0f, 0f);
        rt_SellTabButton.pivot = new Vector2(0.5f, 0.5f);
        rt_SellTabButton.sizeDelta = new Vector2(0f, 0f);
        rt_SellTabButton.anchoredPosition = new Vector2(0f, 0f);
        go_SellTabButton.AddComponent<CanvasRenderer>();
        var img_SellTabButton = go_SellTabButton.AddComponent<Image>();
        img_SellTabButton.color = new Color(0.14f, 0.18f, 0.28f, 1f);
        img_SellTabButton.raycastTarget = true;
        var btn_SellTabButton = go_SellTabButton.AddComponent<Button>();
        btn_SellTabButton.navigation = new Navigation { mode = (Navigation.Mode)3 };
        btn_SellTabButton.targetGraphic = go_SellTabButton.GetComponent<Image>();
        Undo.RegisterCreatedObjectUndo(go_SellTabButton, "Create SellTabButton");

        // --- Label ---
        var go_Label2 = new GameObject("Label");
        go_Label2.layer = 5;
        var rt_Label2 = go_Label2.AddComponent<RectTransform>();
        rt_Label2.SetParent(go_SellTabButton.GetComponent<RectTransform>(), false);
        rt_Label2.anchorMin = new Vector2(0f, 0f);
        rt_Label2.anchorMax = new Vector2(1f, 1f);
        rt_Label2.pivot = new Vector2(0.5f, 0.5f);
        rt_Label2.sizeDelta = new Vector2(0f, 0f);
        rt_Label2.anchoredPosition = new Vector2(0f, 0f);
        go_Label2.AddComponent<CanvasRenderer>();
        var tmp_Label2 = go_Label2.AddComponent<TextMeshProUGUI>();
        tmp_Label2.font = SceneBuilderHelper.LoadFont(Font_Attic);
        tmp_Label2.text = "Sell";
        tmp_Label2.fontSize = 30f;
        tmp_Label2.color = new Color(1f, 1f, 1f, 1f);
        tmp_Label2.alignment = (TextAlignmentOptions)514;
        tmp_Label2.enableWordWrapping = false;
        tmp_Label2.raycastTarget = false;
        Undo.RegisterCreatedObjectUndo(go_Label2, "Create Label");

        // --- BuyTabButton ---
        var go_BuyTabButton = new GameObject("BuyTabButton");
        go_BuyTabButton.layer = 5;
        var rt_BuyTabButton = go_BuyTabButton.AddComponent<RectTransform>();
        rt_BuyTabButton.SetParent(go_ModeBar.GetComponent<RectTransform>(), false);
        rt_BuyTabButton.anchorMin = new Vector2(0f, 0f);
        rt_BuyTabButton.anchorMax = new Vector2(0f, 0f);
        rt_BuyTabButton.pivot = new Vector2(0.5f, 0.5f);
        rt_BuyTabButton.sizeDelta = new Vector2(0f, 0f);
        rt_BuyTabButton.anchoredPosition = new Vector2(0f, 0f);
        go_BuyTabButton.AddComponent<CanvasRenderer>();
        var img_BuyTabButton = go_BuyTabButton.AddComponent<Image>();
        img_BuyTabButton.color = new Color(0.14f, 0.18f, 0.28f, 1f);
        img_BuyTabButton.raycastTarget = true;
        var btn_BuyTabButton = go_BuyTabButton.AddComponent<Button>();
        btn_BuyTabButton.navigation = new Navigation { mode = (Navigation.Mode)3 };
        btn_BuyTabButton.targetGraphic = go_BuyTabButton.GetComponent<Image>();
        Undo.RegisterCreatedObjectUndo(go_BuyTabButton, "Create BuyTabButton");

        // --- Label ---
        var go_Label6 = new GameObject("Label");
        go_Label6.layer = 5;
        var rt_Label6 = go_Label6.AddComponent<RectTransform>();
        rt_Label6.SetParent(go_BuyTabButton.GetComponent<RectTransform>(), false);
        rt_Label6.anchorMin = new Vector2(0f, 0f);
        rt_Label6.anchorMax = new Vector2(1f, 1f);
        rt_Label6.pivot = new Vector2(0.5f, 0.5f);
        rt_Label6.sizeDelta = new Vector2(0f, 0f);
        rt_Label6.anchoredPosition = new Vector2(0f, 0f);
        go_Label6.AddComponent<CanvasRenderer>();
        var tmp_Label6 = go_Label6.AddComponent<TextMeshProUGUI>();
        tmp_Label6.font = SceneBuilderHelper.LoadFont(Font_Attic);
        tmp_Label6.text = "Buy";
        tmp_Label6.fontSize = 30f;
        tmp_Label6.color = new Color(1f, 1f, 1f, 1f);
        tmp_Label6.alignment = (TextAlignmentOptions)514;
        tmp_Label6.enableWordWrapping = false;
        tmp_Label6.raycastTarget = false;
        Undo.RegisterCreatedObjectUndo(go_Label6, "Create Label");

        // --- FadeOverlay ---
        var go_FadeOverlay = new GameObject("FadeOverlay");
        go_FadeOverlay.layer = 5;
        var rt_FadeOverlay = go_FadeOverlay.AddComponent<RectTransform>();
        rt_FadeOverlay.SetParent(go_Canvas.GetComponent<RectTransform>(), false);
        rt_FadeOverlay.anchorMin = new Vector2(0f, 0f);
        rt_FadeOverlay.anchorMax = new Vector2(1f, 1f);
        rt_FadeOverlay.pivot = new Vector2(0.5f, 0.5f);
        rt_FadeOverlay.sizeDelta = new Vector2(0f, 0f);
        rt_FadeOverlay.anchoredPosition = new Vector2(0f, 0f);
        go_FadeOverlay.AddComponent<CanvasRenderer>();
        var img_FadeOverlay = go_FadeOverlay.AddComponent<Image>();
        img_FadeOverlay.sprite = SceneBuilderHelper.LoadSprite(Sprite_Black32x32);
        img_FadeOverlay.color = new Color(0f, 0f, 0f, 1f);
        img_FadeOverlay.raycastTarget = true;
        go_FadeOverlay.AddComponent<Scripts.Canvas.FadeOverlayInstance>();
        Undo.RegisterCreatedObjectUndo(go_FadeOverlay, "Create FadeOverlay");

        // --- List ---
        var go_List = new GameObject("List");
        go_List.layer = 5;
        var rt_List = go_List.AddComponent<RectTransform>();
        rt_List.SetParent(go_Canvas.GetComponent<RectTransform>(), false);
        rt_List.anchorMin = new Vector2(0f, 0.1f);
        rt_List.anchorMax = new Vector2(1f, 0.8f);
        rt_List.pivot = new Vector2(0.5f, 0.5f);
        rt_List.sizeDelta = new Vector2(-48f, -16f);
        rt_List.anchoredPosition = new Vector2(0f, 0f);
        go_List.AddComponent<CanvasRenderer>();
        var img_List = go_List.AddComponent<Image>();
        img_List.color = new Color(0f, 0f, 0f, 0.35f);
        img_List.raycastTarget = true;
        var sr_List = go_List.AddComponent<ScrollRect>();
        Undo.RegisterCreatedObjectUndo(go_List, "Create List");

        // --- Viewport ---
        var go_Viewport = new GameObject("Viewport");
        go_Viewport.layer = 5;
        var rt_Viewport = go_Viewport.AddComponent<RectTransform>();
        rt_Viewport.SetParent(go_List.GetComponent<RectTransform>(), false);
        rt_Viewport.anchorMin = new Vector2(0f, 0f);
        rt_Viewport.anchorMax = new Vector2(1f, 1f);
        rt_Viewport.pivot = new Vector2(0f, 1f);
        rt_Viewport.sizeDelta = new Vector2(0f, 0f);
        rt_Viewport.anchoredPosition = new Vector2(0f, 0f);
        go_Viewport.AddComponent<CanvasRenderer>();
        var img_Viewport = go_Viewport.AddComponent<Image>();
        img_Viewport.sprite = SceneBuilderHelper.LoadBuiltinSprite();
        img_Viewport.color = new Color(1f, 1f, 1f, 0.02f);
        img_Viewport.type = (Image.Type)1;
        img_Viewport.raycastTarget = true;
        var mask_Viewport = go_Viewport.AddComponent<Mask>();
        mask_Viewport.showMaskGraphic = true;
        Undo.RegisterCreatedObjectUndo(go_Viewport, "Create Viewport");

        // --- Content ---
        var go_Content = new GameObject("Content");
        go_Content.layer = 5;
        var rt_Content = go_Content.AddComponent<RectTransform>();
        rt_Content.SetParent(go_Viewport.GetComponent<RectTransform>(), false);
        rt_Content.anchorMin = new Vector2(0f, 1f);
        rt_Content.anchorMax = new Vector2(1f, 1f);
        rt_Content.pivot = new Vector2(0f, 1f);
        rt_Content.sizeDelta = new Vector2(0f, 0f);
        rt_Content.anchoredPosition = new Vector2(0f, 0f);
        var vlg_Content = go_Content.AddComponent<VerticalLayoutGroup>();
        vlg_Content.spacing = 4f;
        vlg_Content.childAlignment = (TextAnchor)0;
        vlg_Content.childControlWidth = true;
        vlg_Content.childControlHeight = false;
        vlg_Content.childForceExpandWidth = true;
        vlg_Content.childForceExpandHeight = false;
        var csf_Content = go_Content.AddComponent<ContentSizeFitter>();
        csf_Content.verticalFit = (ContentSizeFitter.FitMode)2;
        Undo.RegisterCreatedObjectUndo(go_Content, "Create Content");

        // --- FooterBar ---
        var go_FooterBar = new GameObject("FooterBar");
        go_FooterBar.layer = 5;
        var rt_FooterBar = go_FooterBar.AddComponent<RectTransform>();
        rt_FooterBar.SetParent(go_Canvas.GetComponent<RectTransform>(), false);
        rt_FooterBar.anchorMin = new Vector2(0f, 0f);
        rt_FooterBar.anchorMax = new Vector2(1f, 0.1f);
        rt_FooterBar.pivot = new Vector2(0.5f, 0.5f);
        rt_FooterBar.sizeDelta = new Vector2(0f, 0f);
        rt_FooterBar.anchoredPosition = new Vector2(0f, 0f);
        go_FooterBar.AddComponent<CanvasRenderer>();
        var img_FooterBar = go_FooterBar.AddComponent<Image>();
        img_FooterBar.color = new Color(0.1f, 0.14f, 0.24f, 1f);
        img_FooterBar.raycastTarget = true;
        Undo.RegisterCreatedObjectUndo(go_FooterBar, "Create FooterBar");

        // --- TotalLabel ---
        var go_TotalLabel = new GameObject("TotalLabel");
        go_TotalLabel.layer = 5;
        var rt_TotalLabel = go_TotalLabel.AddComponent<RectTransform>();
        rt_TotalLabel.SetParent(go_FooterBar.GetComponent<RectTransform>(), false);
        rt_TotalLabel.anchorMin = new Vector2(0f, 0f);
        rt_TotalLabel.anchorMax = new Vector2(0.6f, 1f);
        rt_TotalLabel.pivot = new Vector2(0.5f, 0.5f);
        rt_TotalLabel.sizeDelta = new Vector2(-24f, 0f);
        rt_TotalLabel.anchoredPosition = new Vector2(12f, 0f);
        go_TotalLabel.AddComponent<CanvasRenderer>();
        var tmp_TotalLabel = go_TotalLabel.AddComponent<TextMeshProUGUI>();
        tmp_TotalLabel.font = SceneBuilderHelper.LoadFont(Font_Attic);
        tmp_TotalLabel.text = "'Pay: 0g  |  Gold: 0g'";
        tmp_TotalLabel.fontSize = 32f;
        tmp_TotalLabel.color = new Color(1f, 1f, 1f, 1f);
        tmp_TotalLabel.alignment = (TextAlignmentOptions)4097;
        tmp_TotalLabel.enableWordWrapping = false;
        tmp_TotalLabel.raycastTarget = false;
        Undo.RegisterCreatedObjectUndo(go_TotalLabel, "Create TotalLabel");

        // --- ActionButton ---
        var go_ActionButton = new GameObject("ActionButton");
        go_ActionButton.layer = 5;
        var rt_ActionButton = go_ActionButton.AddComponent<RectTransform>();
        rt_ActionButton.SetParent(go_FooterBar.GetComponent<RectTransform>(), false);
        rt_ActionButton.anchorMin = new Vector2(0.6f, 0f);
        rt_ActionButton.anchorMax = new Vector2(1f, 1f);
        rt_ActionButton.pivot = new Vector2(0.5f, 0.5f);
        rt_ActionButton.sizeDelta = new Vector2(-40f, -24f);
        rt_ActionButton.anchoredPosition = new Vector2(-4f, 0f);
        go_ActionButton.AddComponent<CanvasRenderer>();
        var img_ActionButton = go_ActionButton.AddComponent<Image>();
        img_ActionButton.color = new Color(1f, 0.78f, 0.28f, 1f);
        img_ActionButton.raycastTarget = true;
        var btn_ActionButton = go_ActionButton.AddComponent<Button>();
        btn_ActionButton.navigation = new Navigation { mode = (Navigation.Mode)3 };
        btn_ActionButton.targetGraphic = go_ActionButton.GetComponent<Image>();
        Undo.RegisterCreatedObjectUndo(go_ActionButton, "Create ActionButton");

        // --- Label ---
        var go_Label10 = new GameObject("Label");
        go_Label10.layer = 5;
        var rt_Label10 = go_Label10.AddComponent<RectTransform>();
        rt_Label10.SetParent(go_ActionButton.GetComponent<RectTransform>(), false);
        rt_Label10.anchorMin = new Vector2(0f, 0f);
        rt_Label10.anchorMax = new Vector2(1f, 1f);
        rt_Label10.pivot = new Vector2(0.5f, 0.5f);
        rt_Label10.sizeDelta = new Vector2(0f, 0f);
        rt_Label10.anchoredPosition = new Vector2(0f, 0f);
        go_Label10.AddComponent<CanvasRenderer>();
        var tmp_Label10 = go_Label10.AddComponent<TextMeshProUGUI>();
        tmp_Label10.font = SceneBuilderHelper.LoadFont(Font_Attic);
        tmp_Label10.text = "Buy";
        tmp_Label10.fontSize = 36f;
        tmp_Label10.color = new Color(0f, 0f, 0f, 1f);
        tmp_Label10.alignment = (TextAlignmentOptions)514;
        tmp_Label10.enableWordWrapping = false;
        tmp_Label10.raycastTarget = false;
        Undo.RegisterCreatedObjectUndo(go_Label10, "Create Label");

        // --- VendorNavBar (Hamburger menu, upper-left) ---
        VendorNavBarBuilder.Build(go_Canvas.GetComponent<RectTransform>(), topInset: 0f, anchorLeft: true);

        // --- EventSystem ---
        var go_EventSystem = new GameObject("EventSystem");
        go_EventSystem.AddComponent<StandaloneInputModule>();
        go_EventSystem.AddComponent<EventSystem>();
        Undo.RegisterCreatedObjectUndo(go_EventSystem, "Create EventSystem");

        // --- Main Camera ---
        var go_Main_Camera = new GameObject("Main Camera");
        var cam_Main_Camera = go_Main_Camera.AddComponent<Camera>();
        cam_Main_Camera.orthographic = true;
        cam_Main_Camera.orthographicSize = 5f;
        cam_Main_Camera.depth = -1f;
        cam_Main_Camera.clearFlags = (CameraClearFlags)2;
        cam_Main_Camera.backgroundColor = new Color(0f, 0f, 0f, 1f);
        go_Main_Camera.AddComponent<AudioListener>();
        Undo.RegisterCreatedObjectUndo(go_Main_Camera, "Create Main Camera");

        // --- VendorManagerGO ---
        var go_VendorManagerGO = new GameObject("VendorManagerGO");
        go_VendorManagerGO.AddComponent<Scripts.Vendor.VendorManager>();
        Undo.RegisterCreatedObjectUndo(go_VendorManagerGO, "Create VendorManagerGO");

        // --- ScrollRect cross-references ---
        sr_List.viewport   = rt_Viewport;
        sr_List.content    = rt_Content;
        sr_List.vertical   = true;
        sr_List.horizontal = false;

        UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(
            UnityEditor.SceneManagement.EditorSceneManager.GetActiveScene());
    }
}
