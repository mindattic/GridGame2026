using UnityEngine;
using UnityEngine.UI;
using UnityEditor;
using TMPro;
using Scripts.Instances;
using Scripts.Utilities;

/// <summary>
/// LOADINGSCREENSCAFFOLD - Editor tool to scaffold the LoadingScreen scene.
///
/// SCENE HIERARCHY (from SceneHierarchies.txt):
/// ```
/// EventSystem ................ EventSystem + StandaloneInputModule
/// LoadingScreen [L=5] ........ LoadingScreenManager (positioned off-screen)
///   {a=(0.5,0.5) sz=(100,100) pos=(585,1266)}
/// SceneLoader ................ SceneLoader (MonoBehaviour)
/// Canvas [L=5] ............... Canvas(mode=Overlay) + CanvasScaler + GraphicRaycaster
///   ├── ProgressPanel {stretch} CanvasRenderer + CanvasGroup
///   │   ├── ProgressLabel {a=(0.5,0.5) sz=(200,50) pos=(0,-50)} TextMeshProUGUI
///   │   └── ProgressBar {a=(0.5,0.5) sz=(1000,64)} Slider
///   │       ├── Background {a=(0,0.25...1,0.75)} Image
///   │       ├── Fill Area {a=(0,0.25...1,0.75) sz=(-20,0) pos=(-5,0)}
///   │       │   └── Fill {a=(0,0) sz=(10,0)} Image
///   │       └── Handle Slide Area {stretch, sz=(-20,0)}
///   │           └── Handle {a=(0,0) sz=(20,0)} Image
///   ├── LoreText {a=(0.5,0...0.5,1) sz=(1024,*)} TextMeshProUGUI + LoadingScreenLore
///   ├── FadePanel {stretch} CanvasRenderer + Image + CanvasGroup
///   └── FadeOverlay {stretch} CanvasRenderer + Image + FadeOverlayInstance
/// ```
///
/// NOTE: This scene does NOT have a standard Camera — it relies on Canvas overlay.
/// The LoadingScreen GO holds the manager. SceneLoader handles async scene loading.
/// Canvas does NOT have background Image (no CanvasRenderer on Canvas itself).
///
/// SCENE FLOW: StageSelect → LoadingScreen → Game
///
/// RELATED FILES: LoadingScreenManager.cs, LoadingScreenLore.cs, SceneLoader.cs
/// </summary>
public static class LoadingScreenScaffold
{
    private const string SceneName = "LoadingScreen";

    public static void CreateScaffolding()
    {
        if (!SceneScaffoldHelper.OpenScene(SceneName)) return;
        int created = 0;
        int found = 0;

        SceneScaffoldHelper.EnsureEventSystem(ref created, ref found);

        // LoadingScreen manager GO (off-screen, holds LoadingScreenManager)
        var loadingGO = SceneScaffoldHelper.EnsureEmptyGameObject("LoadingScreen", ref created, ref found);
        SceneScaffoldHelper.EnsureScript<LoadingScreenManager>(loadingGO);
        var sceneLoaderGO = SceneScaffoldHelper.EnsureEmptyGameObject("SceneLoader", ref created, ref found);
        SceneScaffoldHelper.EnsureScript<SceneLoader>(sceneLoaderGO);

        // Canvas — no background Image in this scene
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
            // ProgressPanel — stretch, CanvasGroup for fade
            var progressPanel = SceneScaffoldHelper.EnsureRectChild(canvas, "ProgressPanel", ref created, ref found);
            if (progressPanel != null)
            {
                if (progressPanel.GetComponent<CanvasRenderer>() == null) progressPanel.gameObject.AddComponent<CanvasRenderer>();
                if (progressPanel.GetComponent<CanvasGroup>() == null) progressPanel.gameObject.AddComponent<CanvasGroup>();

                // ProgressLabel
                var pLabel = SceneScaffoldHelper.EnsureLabel(progressPanel, "ProgressLabel", "Loading...", ref created, ref found);
                if (pLabel != null)
                {
                    pLabel.anchorMin = pLabel.anchorMax = new Vector2(0.5f, 0.5f);
                    pLabel.sizeDelta = new Vector2(200f, 50f);
                    pLabel.anchoredPosition = new Vector2(0f, -50f);
                }

                // ProgressBar — Slider with fill/handle
                var pBar = SceneScaffoldHelper.EnsureRectChild(progressPanel, "ProgressBar", ref created, ref found);
                if (pBar != null)
                {
                    pBar.anchorMin = pBar.anchorMax = new Vector2(0.5f, 0.5f);
                    pBar.sizeDelta = new Vector2(1000f, 64f);
                    pBar.anchoredPosition = Vector2.zero;

                    var bg = SceneScaffoldHelper.EnsureImage(pBar, "Background", false, ref created, ref found);
                    if (bg != null) { bg.anchorMin = new Vector2(0f, 0.25f); bg.anchorMax = new Vector2(1f, 0.75f); bg.sizeDelta = Vector2.zero; bg.anchoredPosition = Vector2.zero; }

                    var fillArea = SceneScaffoldHelper.EnsureRectChild(pBar, "Fill Area", ref created, ref found);
                    if (fillArea != null)
                    {
                        fillArea.anchorMin = new Vector2(0f, 0.25f);
                        fillArea.anchorMax = new Vector2(1f, 0.75f);
                        fillArea.sizeDelta = new Vector2(-20f, 0f);
                        fillArea.anchoredPosition = new Vector2(-5f, 0f);
                        var fill = SceneScaffoldHelper.EnsureImage(fillArea, "Fill", false, ref created, ref found);
                        if (fill != null) { fill.anchorMin = fill.anchorMax = Vector2.zero; fill.sizeDelta = new Vector2(10f, 0f); }
                    }

                    var handleArea = SceneScaffoldHelper.EnsureRectChild(pBar, "Handle Slide Area", ref created, ref found);
                    if (handleArea != null)
                    {
                        handleArea.anchorMin = Vector2.zero; handleArea.anchorMax = Vector2.one;
                        handleArea.sizeDelta = new Vector2(-20f, 0f); handleArea.anchoredPosition = Vector2.zero;
                        var handle = SceneScaffoldHelper.EnsureImage(handleArea, "Handle", false, ref created, ref found);
                        if (handle != null) { handle.anchorMin = handle.anchorMax = Vector2.zero; handle.sizeDelta = new Vector2(20f, 0f); }
                    }
                }
            }

            // LoreText
            var lore = SceneScaffoldHelper.EnsureLabel(canvas, "LoreText", "", ref created, ref found);
            if (lore != null)
            {
                lore.anchorMin = new Vector2(0.5f, 0f);
                lore.anchorMax = new Vector2(0.5f, 1f);
                lore.sizeDelta = new Vector2(1024f, -2506f);
                lore.anchoredPosition = new Vector2(0f, 499.92f);
            }

            // FadePanel — stretch, Image + CanvasGroup
            var fadePanel = SceneScaffoldHelper.EnsureImage(canvas, "FadePanel", true, ref created, ref found);
            if (fadePanel != null && fadePanel.GetComponent<CanvasGroup>() == null)
                fadePanel.gameObject.AddComponent<CanvasGroup>();

            // FadeOverlay
            SceneScaffoldHelper.EnsureFadeOverlay(canvas, ref created, ref found);
        }

        SceneScaffoldHelper.LogResults(SceneName, created, found);
    }

    public static void ClearScene()
    {
        if (!SceneScaffoldHelper.OpenScene(SceneName)) return;
        SceneScaffoldHelper.ClearAllRootObjects();
    }

    [MenuItem("Tools/Scenes/Loading Screen/Load")]
    public static void Load()
    {
        if (!EditorUtility.DisplayDialog("Load",
            "Clear the LoadingScreen scene and recreate all GameObjects from the scaffold?\n\n" +
            "Any unsaved scene changes will be lost.",
            "Load", "Cancel"))
            return;
        if (!SceneScaffoldHelper.OpenScene(SceneName)) return;
        SceneScaffoldHelper.ClearAllRootObjectsSilent();
        CreateScaffolding();
    }
}
