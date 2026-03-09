using UnityEngine;
using UnityEngine.UI;
using UnityEditor;
using TMPro;

/// <summary>
/// PROFILECREATESCAFFOLD - Editor tool to scaffold the ProfileCreate scene.
///
/// SCENE HIERARCHY (from SceneHierarchies.txt):
/// ```
/// Main Camera ................ Camera(ortho, size=5, depth=-1) + AudioListener
/// EventSystem ................ EventSystem + StandaloneInputModule
/// ProfileCreateManager ....... ProfileCreateManager
/// Canvas [L=5] ............... Canvas(mode=Overlay) + CanvasScaler + GraphicRaycaster + CanvasRenderer + Image
///   ├── CutoutOverlay {stretch} CutoutOverlay
///   │   ├── Top {top, h=130}    Image  (LeftPane / CenterPane / RightPane)
///   │   └── Bottom [OFF] {bottom, h=94}
///   ├── BackButton [OFF] {a=(0.5,0.5) sz=(48,48) pos=(-435,1220)} Image + Button
///   │   └── Text (TMP) {stretch} TextMeshProUGUI
///   └── FadeOverlay {stretch}    Image + FadeOverlayInstance
/// ```
///
/// NOTE: This is a minimal scene. The on-screen keyboard (GameObjectHelper.KeyboardDialog)
/// is managed by a separate Keyboard prefab or dialog system, not scaffolded here.
/// The BackButton starts inactive and uses "Text (TMP)" as its label child name.
///
/// SCENE FLOW: TitleScreen → ProfileCreate → (name entered) → TitleScreen
///
/// RELATED FILES: ProfileCreateManager.cs, GameObjectHelper.KeyboardDialog
/// </summary>
public static class ProfileCreateScaffold
{
    private const string SceneName = "ProfileCreate";

    //[MenuItem("Tools/Scenes/Profile Create/Create Scaffolding")]
    public static void CreateScaffolding()
    {
        if (!ScaffoldHelper.OpenScene(SceneName)) return;
        int created = 0;
        int found = 0;

        ScaffoldHelper.EnsureCamera("Main Camera", ref created, ref found);
        ScaffoldHelper.EnsureEventSystem(ref created, ref found);
        ScaffoldHelper.EnsureEmptyGameObject("ProfileCreateManager", ref created, ref found);

        var canvas = ScaffoldHelper.EnsureCanvas("Canvas", ref created, ref found);
        if (canvas != null)
        {
            ScaffoldHelper.EnsureCutoutOverlay(canvas, ref created, ref found);

            // BackButton — starts inactive, centered with offset
            var backBtn = ScaffoldHelper.EnsureButton(canvas, "BackButton", "Back", ref created, ref found);
            if (backBtn != null)
            {
                backBtn.anchorMin = backBtn.anchorMax = new Vector2(0.5f, 0.5f);
                backBtn.sizeDelta = new Vector2(48f, 48f);
                backBtn.anchoredPosition = new Vector2(-435f, 1220f);
                backBtn.gameObject.SetActive(false);
            }

            ScaffoldHelper.EnsureFadeOverlay(canvas, ref created, ref found);
        }

        ScaffoldHelper.LogResults(SceneName, created, found);
    }

    //[MenuItem("Tools/Scenes/Profile Create/Clear Scene")]
    public static void ClearScene()
    {
        if (!ScaffoldHelper.OpenScene(SceneName)) return;
        ScaffoldHelper.ClearAllRootObjects();
    }

    [MenuItem("Tools/Scenes/Profile Create/Clear && Recreate")]
    public static void ClearAndRecreate()
    {
        if (!ScaffoldHelper.OpenScene(SceneName)) return;
        ScaffoldHelper.ClearAllRootObjectsSilent();
        CreateScaffolding();
    }
}
