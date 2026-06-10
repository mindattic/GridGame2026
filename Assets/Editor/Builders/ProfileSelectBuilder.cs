using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Events;
using UnityEditor;
using TMPro;
using Scripts.Managers;

/// <summary>
/// PROFILESELECTSCAFFOLD - Editor tool to builder the ProfileSelect scene.
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
public static class ProfileSelectBuilder
{
    private const string SceneName = "ProfileSelect";

    //[MenuItem("Tools/Scenes/Profile Select/Create Building")]
    public static void Build()
    {
        if (!SceneBuilderHelper.OpenScene(SceneName)) return;
        int created = 0;
        int found = 0;

        SceneBuilderHelper.EnsureCamera("Main Camera", ref created, ref found);
        SceneBuilderHelper.EnsureEventSystem(ref created, ref found);
        var mgr = SceneBuilderHelper.EnsureEmptyGameObject("ProfileSelectManager", ref created, ref found);
        SceneBuilderHelper.EnsureScript<ProfileSelectManager>(mgr);

        var canvas = SceneBuilderHelper.EnsureCanvas("Canvas", ref created, ref found);
        if (canvas != null)
        {
            SceneBuilderHelper.EnsureTitle(canvas, "Select Profile", ref created, ref found);
            SceneBuilderHelper.EnsureScrollView(canvas, ref created, ref found);
            SceneBuilderHelper.EnsureBackButton(canvas, ref created, ref found);

            // Wire BackButton → ProfileSelectManager.OnBackButtonClicked
            var backBtn = canvas.Find("BackButton")?.GetComponent<Button>();
            var profileSelectManager = mgr.GetComponent<ProfileSelectManager>();
            if (backBtn != null && profileSelectManager != null)
                SceneBuilderHelper.WireOnClick(backBtn, new UnityAction(profileSelectManager.OnBackButtonClicked));

            SceneBuilderHelper.EnsureFadeOverlay(canvas, ref created, ref found);
        }

        SceneBuilderHelper.LogResults(SceneName, created, found);
    }

}
