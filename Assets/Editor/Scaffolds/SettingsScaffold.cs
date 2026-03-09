using UnityEngine;
using UnityEngine.UI;
using UnityEditor;
using TMPro;

/// <summary>
/// SETTINGSSCAFFOLD - Editor tool to scaffold the Settings scene.
///
/// SCENE HIERARCHY (from SceneHierarchies.txt):
/// ```
/// Main Camera ................ Camera(ortho, size=5, depth=-1) + AudioListener
/// EventSystem ................ EventSystem + StandaloneInputModule
/// SettingsManager ............ SettingsManager
/// Canvas [L=5] ............... Canvas(mode=Overlay) + CanvasScaler + GraphicRaycaster + CanvasRenderer + Image
///   ├── CutoutOverlay {stretch}
///   ├── Title {a=(0.5,1) pos=(0,-128)} TextMeshProUGUI
///   ├── ScrollView {stretch, sz=(0,-512)} — Viewport/Content/Scrollbars
///   ├── DefaultsButton {a=(0.5,0.5) sz=(128,64) pos=(-467,-969)} Image + Button
///   │   └── Text (TMP) {stretch} TextMeshProUGUI
///   ├── SaveButton {a=(0.5,0.5) sz=(128,64) pos=(460,-969)} Image + Button
///   │   └── Text (TMP) {stretch} TextMeshProUGUI
///   ├── BackButton {a=(0,1) sz=(200,64) pos=(120,-200)} Image + Button
///   │   └── Label {a=(0.5,0.5) sz=(64,64)}
///   └── FadeOverlay {stretch} Image + FadeOverlayInstance
/// ```
///
/// SCENE FLOW: TitleScreen → Settings → TitleScreen
///
/// RELATED FILES: SettingsManager.cs, GameObjectHelper.Settings
/// </summary>
public static class SettingsScaffold
{
    private const string SceneName = "Settings";

    //[MenuItem("Tools/Scenes/Settings/Create Scaffolding")]
    public static void CreateScaffolding()
    {
        if (!ScaffoldHelper.OpenScene(SceneName)) return;
        int created = 0;
        int found = 0;

        ScaffoldHelper.EnsureCamera("Main Camera", ref created, ref found);
        ScaffoldHelper.EnsureEventSystem(ref created, ref found);
        ScaffoldHelper.EnsureEmptyGameObject("SettingsManager", ref created, ref found);

        var canvas = ScaffoldHelper.EnsureCanvas("Canvas", ref created, ref found);
        if (canvas != null)
        {
            ScaffoldHelper.EnsureCutoutOverlay(canvas, ref created, ref found);
            ScaffoldHelper.EnsureTitle(canvas, "Settings", ref created, ref found);
            ScaffoldHelper.EnsureScrollView(canvas, ref created, ref found);

            // DefaultsButton — bottom-left area
            var defaults = ScaffoldHelper.EnsureButton(canvas, "DefaultsButton", "Defaults", ref created, ref found);
            if (defaults != null)
            {
                defaults.anchorMin = defaults.anchorMax = new Vector2(0.5f, 0.5f);
                defaults.sizeDelta = new Vector2(128f, 64f);
                defaults.anchoredPosition = new Vector2(-467.31f, -968.7f);
            }

            // SaveButton — bottom-right area
            var save = ScaffoldHelper.EnsureButton(canvas, "SaveButton", "Save", ref created, ref found);
            if (save != null)
            {
                save.anchorMin = save.anchorMax = new Vector2(0.5f, 0.5f);
                save.sizeDelta = new Vector2(128f, 64f);
                save.anchoredPosition = new Vector2(460.2f, -968.7f);
            }

            ScaffoldHelper.EnsureBackButton(canvas, ref created, ref found);
            ScaffoldHelper.EnsureFadeOverlay(canvas, ref created, ref found);
        }

        ScaffoldHelper.LogResults(SceneName, created, found);
    }

    //[MenuItem("Tools/Scenes/Settings/Clear Scene")]
    public static void ClearScene()
    {
        if (!ScaffoldHelper.OpenScene(SceneName)) return;
        ScaffoldHelper.ClearAllRootObjects();
    }

    [MenuItem("Tools/Scenes/Settings/Clear && Recreate")]
    public static void ClearAndRecreate()
    {
        if (!ScaffoldHelper.OpenScene(SceneName)) return;
        ScaffoldHelper.ClearAllRootObjectsSilent();
        CreateScaffolding();
    }
}
