using UnityEngine;
using UnityEngine.UI;
using UnityEditor;
using TMPro;
using Scripts.Managers;

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

    public static void CreateScaffolding()
    {
        if (!SceneScaffoldHelper.OpenScene(SceneName)) return;
        int created = 0;
        int found = 0;

        SceneScaffoldHelper.EnsureCamera("Main Camera", ref created, ref found);
        SceneScaffoldHelper.EnsureEventSystem(ref created, ref found);
        var mgr = SceneScaffoldHelper.EnsureEmptyGameObject("ProfileCreateManager", ref created, ref found);
        SceneScaffoldHelper.EnsureScript<ProfileCreateManager>(mgr);

        var canvas = SceneScaffoldHelper.EnsureCanvas("Canvas", ref created, ref found);
        if (canvas != null)
        {
            SceneScaffoldHelper.EnsureCutoutOverlay(canvas, ref created, ref found);

            // BackButton — starts inactive, centered with offset
            var backBtn = SceneScaffoldHelper.EnsureButton(canvas, "BackButton", "Back", ref created, ref found);
            if (backBtn != null)
            {
                backBtn.anchorMin = backBtn.anchorMax = new Vector2(0.5f, 0.5f);
                backBtn.sizeDelta = new Vector2(48f, 48f);
                backBtn.anchoredPosition = new Vector2(-435f, 1220f);
                backBtn.gameObject.SetActive(false);
            }

            SceneScaffoldHelper.EnsureFadeOverlay(canvas, ref created, ref found);
        }

        SceneScaffoldHelper.LogResults(SceneName, created, found);
    }

    public static void ClearScene()
    {
        if (!SceneScaffoldHelper.OpenScene(SceneName)) return;
        SceneScaffoldHelper.ClearAllRootObjects();
    }

    [MenuItem("Tools/Scenes/Profile Create/Clear && Recreate")]
    public static void ClearAndRecreate()
    {
        if (!SceneScaffoldHelper.OpenScene(SceneName)) return;
        SceneScaffoldHelper.ClearAllRootObjectsSilent();
        CreateScaffolding();
    }
}
