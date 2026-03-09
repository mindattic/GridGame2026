using UnityEngine;
using UnityEngine.UI;
using UnityEditor;
using TMPro;

/// <summary>
/// PROFILESELECTSCAFFOLD - Editor tool to scaffold the ProfileSelect scene.
///
/// SCENE HIERARCHY (from SceneHierarchies.txt):
/// ```
/// Main Camera ................ Camera(ortho, size=5, depth=-1) + AudioListener
/// EventSystem ................ EventSystem + StandaloneInputModule
/// ProfileSelectManager ....... ProfileSelectManager
/// Canvas [L=5] ............... Canvas(mode=Overlay) + CanvasScaler + GraphicRaycaster + CanvasRenderer + Image
///   ├── CutoutOverlay {stretch}  CutoutOverlay
///   │   ├── Top {top, h=130}     Image
///   │   │   ├── LeftPane / CenterPane / RightPane
///   │   └── Bottom [OFF] {bottom, h=94}
///   ├── Title {a=(0.5,1) pos=(0,-128)} TextMeshProUGUI
///   ├── ScrollView {stretch, sz=(0,-512)} Image + ScrollRect
///   │   ├── Viewport {stretch, sz=(-17,0) pv=(0,1)} Image + Mask + ScrollRect
///   │   │   └── Content {a=(0,1...1,1) pv=(0,1)} VerticalLayoutGroup + HorizontalLayoutGroup
///   │   ├── Scrollbar Vertical + Scrollbar Horizontal
///   ├── BackButton {a=(0,1) sz=(200,64) pos=(120,-200)} Image + Button
///   │   └── Label {a=(0.5,0.5) sz=(64,64)}
///   └── FadeOverlay {stretch} Image + FadeOverlayInstance
/// ```
///
/// SCENE FLOW: TitleScreen → ProfileSelect → (pick profile) → TitleScreen
///
/// RELATED FILES: ProfileSelectManager.cs, GameObjectHelper.ProfileSelect
/// </summary>
public static class ProfileSelectScaffold
{
    private const string SceneName = "ProfileSelect";

    //[MenuItem("Tools/Scenes/Profile Select/Create Scaffolding")]
    public static void CreateScaffolding()
    {
        if (!ScaffoldHelper.OpenScene(SceneName)) return;
        int created = 0;
        int found = 0;

        ScaffoldHelper.EnsureCamera("Main Camera", ref created, ref found);
        ScaffoldHelper.EnsureEventSystem(ref created, ref found);
        ScaffoldHelper.EnsureEmptyGameObject("ProfileSelectManager", ref created, ref found);

        var canvas = ScaffoldHelper.EnsureCanvas("Canvas", ref created, ref found);
        if (canvas != null)
        {
            ScaffoldHelper.EnsureCutoutOverlay(canvas, ref created, ref found);
            ScaffoldHelper.EnsureTitle(canvas, "Select Profile", ref created, ref found);
            ScaffoldHelper.EnsureScrollView(canvas, ref created, ref found);
            ScaffoldHelper.EnsureBackButton(canvas, ref created, ref found);
            ScaffoldHelper.EnsureFadeOverlay(canvas, ref created, ref found);
        }

        ScaffoldHelper.LogResults(SceneName, created, found);
    }

    //[MenuItem("Tools/Scenes/Profile Select/Clear Scene")]
    public static void ClearScene()
    {
        if (!ScaffoldHelper.OpenScene(SceneName)) return;
        ScaffoldHelper.ClearAllRootObjects();
    }

    [MenuItem("Tools/Scenes/Profile Select/Clear && Recreate")]
    public static void ClearAndRecreate()
    {
        if (!ScaffoldHelper.OpenScene(SceneName)) return;
        ScaffoldHelper.ClearAllRootObjectsSilent();
        CreateScaffolding();
    }
}
