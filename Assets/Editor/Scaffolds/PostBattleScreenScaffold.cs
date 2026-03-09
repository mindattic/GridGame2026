using UnityEngine;
using UnityEngine.UI;
using UnityEditor;
using TMPro;
using Scripts.Managers;
using Scripts.Instances;

/// <summary>
/// POSTBATTLESCREENSCAFFOLD - Editor tool to scaffold the PostBattleScreen scene.
///
/// SCENE HIERARCHY (from SceneHierarchies.txt):
/// ```
/// Main Camera ................ Camera(ortho, size=5, depth=-1) + AudioListener
/// EventSystem ................ EventSystem + StandaloneInputModule
/// Background [OFF] ........... SpriteRenderer + BackgroundInstance (starts inactive)
/// PostBattleManager .......... PostBattleManager
/// Canvas [L=5] ............... Canvas(mode=Overlay) + CanvasScaler + GraphicRaycaster
///   ├── Background {stretch, sz=(840,1818)} CanvasRenderer + Image
///   ├── Title {a=(0,1...1,1) sz=(0,64) pos=(0,-200) pv=(0.5,1)} TextMeshProUGUI
///   ├── ScrollView {stretch, sz=(0,-512) pos=(0,-256) pv=(0.5,1)}
///   │   ├── Viewport / Content / Scrollbars
///   ├── BottomBar {a=(0,0...1,0) sz=(840,110) pos=(0,225)} CanvasRenderer + Image
///   │   └── NextButton {a=(1,0.5) sz=(260,64) pos=(-40,0) pv=(1,0.5)} Button + Image
///   │       └── Label {stretch} TextMeshProUGUI
///   ├── CutoutOverlay {stretch}
///   └── FadeOverlay {stretch} Image + FadeOverlayInstance
/// ```
///
/// NOTE: Canvas here does NOT have CanvasRenderer + Image on itself.
/// Background is a separate GO (SpriteRenderer, inactive).
/// A Canvas child named "Background" provides the UI background.
///
/// SCENE FLOW: Game → PostBattleScreen → Hub / StageSelect
///
/// RELATED FILES: PostBattleManager.cs, GameObjectHelper.PostBattleScreen
/// </summary>
public static class PostBattleScreenScaffold
{
    private const string SceneName = "PostBattleScreen";

    public static void CreateScaffolding()
    {
        if (!SceneScaffoldHelper.OpenScene(SceneName)) return;
        int created = 0;
        int found = 0;

        SceneScaffoldHelper.EnsureCamera("Main Camera", ref created, ref found);
        SceneScaffoldHelper.EnsureEventSystem(ref created, ref found);

        // Background GO (SpriteRenderer, starts inactive)
        var bgGO = GameObject.Find("Background");
        if (bgGO != null) { found++; }
        else
        {
            bgGO = new GameObject("Background");
            bgGO.AddComponent<SpriteRenderer>();
            bgGO.AddComponent<BackgroundInstance>();
            bgGO.SetActive(false);
            Undo.RegisterCreatedObjectUndo(bgGO, "Create Background");
            created++;
        }

        var mgr = SceneScaffoldHelper.EnsureEmptyGameObject("PostBattleManager", ref created, ref found);
        SceneScaffoldHelper.EnsureScript<PostBattleManager>(mgr);

        // Canvas — no background image on Canvas itself in this scene
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
            // Background (UI) — canvas child with Image
            var uiBg = SceneScaffoldHelper.EnsureImage(canvas, "Background", false, ref created, ref found);
            if (uiBg != null)
            {
                uiBg.anchorMin = Vector2.zero; uiBg.anchorMax = Vector2.one;
                uiBg.sizeDelta = new Vector2(840f, 1817.8464f);
                uiBg.anchoredPosition = Vector2.zero;
            }

            // Title — anchored to top, full width, 64px tall
            var title = SceneScaffoldHelper.EnsureLabel(canvas, "Title", "Battle Results", ref created, ref found);
            if (title != null)
            {
                title.anchorMin = new Vector2(0f, 1f); title.anchorMax = new Vector2(1f, 1f);
                title.pivot = new Vector2(0.5f, 1f);
                title.sizeDelta = new Vector2(0f, 64f);
                title.anchoredPosition = new Vector2(0f, -200f);
            }

            // ScrollView
            var sv = SceneScaffoldHelper.EnsureScrollView(canvas, ref created, ref found);
            if (sv != null)
            {
                sv.pivot = new Vector2(0.5f, 1f);
                sv.anchoredPosition = new Vector2(0f, -256f);
            }

            // BottomBar with NextButton
            var bottomBar = SceneScaffoldHelper.EnsureImage(canvas, "BottomBar", false, ref created, ref found);
            if (bottomBar != null)
            {
                bottomBar.anchorMin = Vector2.zero;
                bottomBar.anchorMax = new Vector2(1f, 0f);
                bottomBar.sizeDelta = new Vector2(840f, 110f);
                bottomBar.anchoredPosition = new Vector2(0f, 225f);

                var next = SceneScaffoldHelper.EnsureButton(bottomBar, "NextButton", "Next", ref created, ref found);
                if (next != null)
                {
                    next.anchorMin = next.anchorMax = new Vector2(1f, 0.5f);
                    next.pivot = new Vector2(1f, 0.5f);
                    next.sizeDelta = new Vector2(260f, 64f);
                    next.anchoredPosition = new Vector2(-40f, 0f);
                }
            }

            SceneScaffoldHelper.EnsureCutoutOverlay(canvas, ref created, ref found);
            SceneScaffoldHelper.EnsureFadeOverlay(canvas, ref created, ref found);
        }

        SceneScaffoldHelper.LogResults(SceneName, created, found);
    }

    public static void ClearScene()
    {
        if (!SceneScaffoldHelper.OpenScene(SceneName)) return;
        SceneScaffoldHelper.ClearAllRootObjects();
    }

    [MenuItem("Tools/Scenes/Post Battle Screen/Clear && Recreate")]
    public static void ClearAndRecreate()
    {
        if (!SceneScaffoldHelper.OpenScene(SceneName)) return;
        SceneScaffoldHelper.ClearAllRootObjectsSilent();
        CreateScaffolding();
    }
}
