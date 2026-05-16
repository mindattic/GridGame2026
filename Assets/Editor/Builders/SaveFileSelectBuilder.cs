using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Events;
using UnityEditor;
using TMPro;
using Scripts.Managers;

/// <summary>
/// SAVEFILESELECTSCAFFOLD - Editor tool to builder the SaveFileSelect scene.
///
/// SCENE HIERARCHY (from SceneHierarchies.txt):
/// ```
/// Main Camera ................ Camera(ortho, size=5, depth=-1) + AudioListener
/// EventSystem ................ EventSystem + StandaloneInputModule
/// SaveFileSelectManager ...... SaveFileSelectManager
/// Canvas [L=5] ............... Canvas(mode=Overlay) + CanvasScaler + GraphicRaycaster + CanvasRenderer + Image
///   ├── CutoutOverlay {stretch}
///   ├── Title {a=(0.5,1) pos=(0,-128)} TextMeshProUGUI
///   ├── ScrollView {stretch, sz=(0,-512)} — with Viewport/Content/Scrollbars
///   ├── BackButton {a=(0,1) sz=(200,64) pos=(120,-200)} Image + Button
///   │   └── Label {a=(0.5,0.5) sz=(64,64)}
///   └── FadeOverlay {stretch} Image + FadeOverlayInstance
/// ```
///
/// SCENE FLOW: TitleScreen → SaveFileSelect → (pick save) → Game
///
/// RELATED FILES: SaveFileSelectManager.cs, ProfileHelper.cs
/// </summary>
public static class SaveFileSelectBuilder
{
    private const string SceneName = "SaveFileSelect";

    //[MenuItem("Tools/Scenes/Save File Select/Create Building")]
    public static void Build()
    {
        if (!SceneBuilderHelper.OpenScene(SceneName)) return;
        int created = 0;
        int found = 0;

        SceneBuilderHelper.EnsureCamera("Main Camera", ref created, ref found);
        SceneBuilderHelper.EnsureEventSystem(ref created, ref found);
        var mgr = SceneBuilderHelper.EnsureEmptyGameObject("SaveFileSelectManager", ref created, ref found);
        SceneBuilderHelper.EnsureScript<SaveFileSelectManager>(mgr);

        var canvas = SceneBuilderHelper.EnsureCanvas("Canvas", ref created, ref found);
        if (canvas != null)
        {
            SceneBuilderHelper.EnsureCutoutOverlay(canvas, ref created, ref found);
            SceneBuilderHelper.EnsureTitle(canvas, "Load Game", ref created, ref found);
            SceneBuilderHelper.EnsureScrollView(canvas, ref created, ref found);
            SceneBuilderHelper.EnsureBackButton(canvas, ref created, ref found);

            // Wire BackButton → SaveFileSelectManager.OnBackButtonClicked
            var backBtn = canvas.Find("BackButton")?.GetComponent<Button>();
            var saveFileSelectManager = mgr.GetComponent<SaveFileSelectManager>();
            if (backBtn != null && saveFileSelectManager != null)
                SceneBuilderHelper.WireOnClick(backBtn, new UnityAction(saveFileSelectManager.OnBackButtonClicked));

            SceneBuilderHelper.EnsureFadeOverlay(canvas, ref created, ref found);
        }

        SceneBuilderHelper.LogResults(SceneName, created, found);
    }

}
