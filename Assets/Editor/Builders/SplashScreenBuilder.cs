using UnityEngine;
using UnityEngine.UI;
using UnityEditor;
using TMPro;
using Scripts.Managers;

/// <summary>
/// SPLASHSCREENSCAFFOLD - Editor tool to builder the SplashScreen scene.
///
/// PURPOSE:
/// Programmatically creates every GameObject in the SplashScreen scene
/// with exact component configuration matching the authoritative scene file.
///
/// SCENE HIERARCHY (from SceneHierarchies.txt):
/// ```
/// Main Camera ................. Camera(ortho, size=5, depth=-1) + AudioListener
/// EventSystem ................. EventSystem + StandaloneInputModule
/// SplashScreenManager ......... SplashScreenManager (MonoBehaviour)
/// Canvas [L=5] ................ Canvas(mode=Camera) + CanvasScaler + GraphicRaycaster + CanvasRenderer + Image
///   ├── Full [L=5] {stretch} .. CanvasRenderer + RawImage (full-screen splash graphic)
///   ├── Window [L=5] .......... CanvasRenderer + Image + SwingingWindow
///   │   {a=(0.5,0.5) sz=(108.5,172.29) pos=(135,-431.4) pv=(1,0.5)}
///   ├── CanvasParticleEmitter . CanvasRenderer + Image + CanvasParticleEmitter
///   │   {a=(0.5,0.5) sz=(100,100) pos=(-500,0)}
///   └── FadeOverlay [L=5] ..... CanvasRenderer + Image + FadeOverlayInstance
/// ```
///
/// NOTE: Canvas uses Camera render mode (not Overlay) in this scene.
/// SwingingWindow, CanvasParticleEmitter, FadeOverlayInstance are custom MonoBehaviours
/// attached at runtime or manually after building.
///
/// SCENE FLOW: App Launch → SplashScreen → (wait 30s or tap) → TitleScreen
///
/// RELATED FILES:
///   - SplashScreenManager.cs, SwingingWindow.cs, CanvasParticleEmitter.cs
///   - FadeOverlayInstance.cs, CameraManager.cs
/// </summary>
public static class SplashScreenBuilder
{
    private const string SceneName = "SplashScreen";

    //[MenuItem("Tools/Scenes/Splash Screen/Create Building")]
    public static void Build()
    {
        if (!SceneBuilderHelper.OpenScene(SceneName)) return;
        int created = 0;
        int found = 0;

        // ── Root Objects ──
        SceneBuilderHelper.EnsureCamera("Main Camera", ref created, ref found);
        SceneBuilderHelper.EnsureEventSystem(ref created, ref found);
        var mgr = SceneBuilderHelper.EnsureEmptyGameObject("SplashScreenManager", ref created, ref found);
        SceneBuilderHelper.EnsureScript<SplashScreenManager>(mgr);

        // ── Canvas (Camera mode for this scene) ──
        var canvas = SceneBuilderHelper.EnsureCanvas("Canvas", ref created, ref found);
        if (canvas != null)
        {
            // Full — full-screen splash graphic (RawImage)
            var full = SceneBuilderHelper.EnsureRectChild(canvas, "Full", ref created, ref found);
            if (full != null)
            {
                full.anchorMin = Vector2.zero;
                full.anchorMax = Vector2.one;
                full.sizeDelta = new Vector2(-1f, -1f);
                full.anchoredPosition = new Vector2(-0.5f, 0.5f);
                if (full.GetComponent<CanvasRenderer>() == null) full.gameObject.AddComponent<CanvasRenderer>();
                if (full.GetComponent<RawImage>() == null) full.gameObject.AddComponent<RawImage>();
            }

            // Window — swinging animated window
            var window = SceneBuilderHelper.EnsureImage(canvas, "Window", false, ref created, ref found);
            if (window != null)
            {
                window.anchorMin = window.anchorMax = new Vector2(0.5f, 0.5f);
                window.pivot = new Vector2(1f, 0.5f);
                window.sizeDelta = new Vector2(108.5f, 172.29f);
                window.anchoredPosition = new Vector2(135f, -431.4f);
            }

            // CanvasParticleEmitter — particle overlay
            var emitter = SceneBuilderHelper.EnsureImage(canvas, "CanvasParticleEmitter", false, ref created, ref found);
            if (emitter != null)
            {
                emitter.anchorMin = emitter.anchorMax = new Vector2(0.5f, 0.5f);
                emitter.sizeDelta = new Vector2(100f, 100f);
                emitter.anchoredPosition = new Vector2(-500f, 0f);
            }

            // FadeOverlay
            SceneBuilderHelper.EnsureFadeOverlay(canvas, ref created, ref found);
        }

        SceneBuilderHelper.LogResults(SceneName, created, found);
    }

}
