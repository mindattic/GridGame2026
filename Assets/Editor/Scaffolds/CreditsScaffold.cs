using UnityEngine;
using UnityEngine.UI;
using UnityEditor;
using TMPro;
using Scripts.Managers;

/// <summary>
/// CREDITSSCAFFOLD - Editor tool to scaffold the Credits scene.
///
/// SCENE HIERARCHY (from SceneHierarchies.txt):
/// ```
/// Main Camera ................ Camera(ortho, size=5, depth=-1) + AudioListener
/// EventSystem ................ EventSystem + StandaloneInputModule
/// CreditsManager ............. CreditsManager
/// Canvas [L=5] ............... Canvas(mode=Overlay) + CanvasScaler + GraphicRaycaster + CanvasRenderer + Image
///   ├── CutoutOverlay {stretch}
///   ├── Title {a=(0.5,1) pos=(0,-128)} TextMeshProUGUI
///   ├── ScrollView {a=(0,0...1,1) sz=(0,-512) pos=(0,-256) pv=(0.5,1)}
///   │   ├── Viewport {stretch, sz=(-17,0) pv=(0,1)} Image + Mask + ScrollRect
///   │   │   └── Content {a=(0,1...1,1) pv=(0,1)} VerticalLayoutGroup
///   │   │       └── Textarea {a=(0,1) sz=(1100,0) pos=(531.5,-150)} TextMeshProUGUI
///   │   ├── Scrollbar Vertical / Horizontal
///   ├── BackButton {a=(0,1) sz=(200,64) pos=(120,-200)} Image + Button
///   │   └── Label {a=(0.5,0.5) sz=(64,64)}
///   └── FadeOverlay {stretch} Image + FadeOverlayInstance
/// ```
///
/// NOTE: Credits has a Textarea child inside ScrollView/Viewport/Content for the
/// actual credits text. The ScrollView is offset with pivot=(0.5,1) and pos=(0,-256).
///
/// SCENE FLOW: TitleScreen → Credits → TitleScreen
///
/// RELATED FILES: CreditsManager.cs, GameObjectHelper.Credits
/// </summary>
public static class CreditsScaffold
{
    private const string SceneName = "Credits";

    public static void CreateScaffolding()
    {
        if (!SceneScaffoldHelper.OpenScene(SceneName)) return;
        int created = 0;
        int found = 0;

        SceneScaffoldHelper.EnsureCamera("Main Camera", ref created, ref found);
        SceneScaffoldHelper.EnsureEventSystem(ref created, ref found);
        var mgr = SceneScaffoldHelper.EnsureEmptyGameObject("CreditsManager", ref created, ref found);
        SceneScaffoldHelper.EnsureScript<CreditsManager>(mgr);

        var canvas = SceneScaffoldHelper.EnsureCanvas("Canvas", ref created, ref found);
        if (canvas != null)
        {
            SceneScaffoldHelper.EnsureCutoutOverlay(canvas, ref created, ref found);
            SceneScaffoldHelper.EnsureTitle(canvas, "Credits", ref created, ref found);

            // ScrollView — slightly offset for credits layout
            var sv = SceneScaffoldHelper.EnsureScrollView(canvas, ref created, ref found);
            if (sv != null)
            {
                sv.pivot = new Vector2(0.5f, 1f);
                sv.anchoredPosition = new Vector2(0f, -256f);

                // Textarea inside Content
                var viewport = sv.Find("Viewport");
                if (viewport != null)
                {
                    var content = viewport.Find("Content");
                    if (content != null)
                    {
                        var textarea = SceneScaffoldHelper.EnsureLabel(
                            content.GetComponent<RectTransform>(), "Textarea", "", ref created, ref found);
                        if (textarea != null)
                        {
                            textarea.anchorMin = textarea.anchorMax = new Vector2(0f, 1f);
                            textarea.sizeDelta = new Vector2(1100f, 0f);
                            textarea.anchoredPosition = new Vector2(531.5f, -150f);
                        }
                    }
                }
            }

            SceneScaffoldHelper.EnsureBackButton(canvas, ref created, ref found);
            SceneScaffoldHelper.EnsureFadeOverlay(canvas, ref created, ref found);
        }

        SceneScaffoldHelper.LogResults(SceneName, created, found);
    }

    public static void ClearScene()
    {
        if (!SceneScaffoldHelper.OpenScene(SceneName)) return;
        SceneScaffoldHelper.ClearAllRootObjects();
    }

    [MenuItem("Tools/Scenes/Credits/Clear && Recreate")]
    public static void ClearAndRecreate()
    {
        if (!SceneScaffoldHelper.OpenScene(SceneName)) return;
        SceneScaffoldHelper.ClearAllRootObjectsSilent();
        CreateScaffolding();
    }
}
