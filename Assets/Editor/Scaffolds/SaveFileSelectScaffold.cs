using UnityEngine;
using UnityEngine.UI;
using UnityEditor;
using TMPro;

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
        if (!ScaffoldHelper.OpenScene(SceneName)) return;
        int created = 0;
        int found = 0;

        ScaffoldHelper.EnsureCamera("Main Camera", ref created, ref found);
        ScaffoldHelper.EnsureEventSystem(ref created, ref found);
        ScaffoldHelper.EnsureEmptyGameObject("SaveFileSelectManager", ref created, ref found);

        var canvas = ScaffoldHelper.EnsureCanvas("Canvas", ref created, ref found);
        if (canvas != null)
        {
            ScaffoldHelper.EnsureCutoutOverlay(canvas, ref created, ref found);
            ScaffoldHelper.EnsureTitle(canvas, "Load Game", ref created, ref found);
            ScaffoldHelper.EnsureScrollView(canvas, ref created, ref found);
            ScaffoldHelper.EnsureBackButton(canvas, ref created, ref found);
            ScaffoldHelper.EnsureFadeOverlay(canvas, ref created, ref found);
        }

        ScaffoldHelper.LogResults(SceneName, created, found);
    }

    //[MenuItem("Tools/Scenes/Save File Select/Clear Scene")]
    public static void ClearScene()
    {
        if (!ScaffoldHelper.OpenScene(SceneName)) return;
        ScaffoldHelper.ClearAllRootObjects();
    }

    [MenuItem("Tools/Scenes/Save File Select/Clear && Recreate")]
    public static void ClearAndRecreate()
    {
        if (!ScaffoldHelper.OpenScene(SceneName)) return;
        ScaffoldHelper.ClearAllRootObjectsSilent();
        CreateScaffolding();
    }
}
