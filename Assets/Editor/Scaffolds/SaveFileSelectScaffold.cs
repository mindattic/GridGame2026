using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Events;
using UnityEditor;
using TMPro;
using Scripts.Managers;

/// <summary>
/// SAVEFILESELECTSCAFFOLD - Editor tool to scaffold the SaveFileSelect scene.
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
public static class SaveFileSelectScaffold
{
    private const string SceneName = "SaveFileSelect";

    //[MenuItem("Tools/Scenes/Save File Select/Create Scaffolding")]
    public static void CreateScaffolding()
    {
        if (!SceneScaffoldHelper.OpenScene(SceneName)) return;
        int created = 0;
        int found = 0;

        SceneScaffoldHelper.EnsureCamera("Main Camera", ref created, ref found);
        SceneScaffoldHelper.EnsureEventSystem(ref created, ref found);
        var mgr = SceneScaffoldHelper.EnsureEmptyGameObject("SaveFileSelectManager", ref created, ref found);
        SceneScaffoldHelper.EnsureScript<SaveFileSelectManager>(mgr);

        var canvas = SceneScaffoldHelper.EnsureCanvas("Canvas", ref created, ref found);
        if (canvas != null)
        {
            SceneScaffoldHelper.EnsureCutoutOverlay(canvas, ref created, ref found);
            SceneScaffoldHelper.EnsureTitle(canvas, "Load Game", ref created, ref found);
            SceneScaffoldHelper.EnsureScrollView(canvas, ref created, ref found);
            SceneScaffoldHelper.EnsureBackButton(canvas, ref created, ref found);

            // Wire BackButton → SaveFileSelectManager.OnBackButtonClicked
            var backBtn = canvas.Find("BackButton")?.GetComponent<Button>();
            var saveFileSelectManager = mgr.GetComponent<SaveFileSelectManager>();
            if (backBtn != null && saveFileSelectManager != null)
                SceneScaffoldHelper.WireOnClick(backBtn, new UnityAction(saveFileSelectManager.OnBackButtonClicked));

            SceneScaffoldHelper.EnsureFadeOverlay(canvas, ref created, ref found);
        }

        SceneScaffoldHelper.LogResults(SceneName, created, found);
    }

    //[MenuItem("Tools/Scenes/Save File Select/Clear Scene")]
    public static void ClearScene()
    {
        if (!SceneScaffoldHelper.OpenScene(SceneName)) return;
        SceneScaffoldHelper.ClearAllRootObjects();
    }

    [MenuItem("Tools/Scenes/Save File Select/Load")]
    public static void Load()
    {
        if (!EditorUtility.DisplayDialog("Load",
            "Clear the SaveFileSelect scene and recreate all GameObjects from the scaffold?\n\n" +
            "Any unsaved scene changes will be lost.",
            "Load", "Cancel"))
            return;
        if (!SceneScaffoldHelper.OpenScene(SceneName)) return;
        SceneScaffoldHelper.ClearAllRootObjectsSilent();
        CreateScaffolding();
    }
}
