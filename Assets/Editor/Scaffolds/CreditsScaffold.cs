using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Events;
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

                // Vertical scrollbar handle — red accent color from scene YAML: rgba(1, 0, 0.2, 1)
                var vBarHandle = sv.Find("Scrollbar Vertical/Sliding Area/Handle");
                if (vBarHandle != null)
                {
                    var handleImg = vBarHandle.GetComponent<Image>();
                    if (handleImg != null)
                        handleImg.color = new Color(1f, 0f, 0.2f, 1f);
                }

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
                            textarea.anchorMin = textarea.anchorMax = Vector2.zero;
                            textarea.sizeDelta = new Vector2(1100f, 0f);
                            textarea.anchoredPosition = Vector2.zero;

                            // Override defaults: Consolas font, size 36, wrapping on
                            var textTMP = textarea.GetComponent<TextMeshProUGUI>();
                            if (textTMP != null)
                            {
                                textTMP.font = SceneScaffoldHelper.LoadFont(SceneScaffoldHelper.FontPaths.Consolas);
                                textTMP.fontSize = 36;
                                textTMP.alignment = TextAlignmentOptions.Top;
                                textTMP.enableWordWrapping = true;
                            }
                        }
                    }
                }
            }

            SceneScaffoldHelper.EnsureBackButton(canvas, ref created, ref found);

            // Wire BackButton → CreditsManager.OnBackButtonClicked
            var backBtn = canvas.Find("BackButton")?.GetComponent<Button>();
            var creditsManager = mgr.GetComponent<CreditsManager>();
            if (backBtn != null && creditsManager != null)
                SceneScaffoldHelper.WireOnClick(backBtn, new UnityAction(creditsManager.OnBackButtonClicked));

            SceneScaffoldHelper.EnsureFadeOverlay(canvas, ref created, ref found);
        }

        SceneScaffoldHelper.LogResults(SceneName, created, found);
    }

    public static void ClearScene()
    {
        if (!SceneScaffoldHelper.OpenScene(SceneName)) return;
        SceneScaffoldHelper.ClearAllRootObjects();
    }

    [MenuItem("Tools/Scenes/Credits/Load")]
    public static void Load()
    {
        if (!EditorUtility.DisplayDialog("Load",
            "Clear the Credits scene and recreate all GameObjects from the scaffold?\n\n" +
            "Any unsaved scene changes will be lost.",
            "Load", "Cancel"))
            return;
        if (!SceneScaffoldHelper.OpenScene(SceneName)) return;
        SceneScaffoldHelper.ClearAllRootObjectsSilent();
        CreateScaffolding();
    }
}
