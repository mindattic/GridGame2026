using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Events;
using UnityEditor;
using TMPro;
using Scripts.Managers;

/// <summary>
/// SETTINGSSCAFFOLD - Editor tool to builder the Settings scene.
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
public static class SettingsBuilder
{
    private const string SceneName = "Settings";

    //[MenuItem("Tools/Scenes/Settings/Create Building")]
    public static void Build()
    {
        if (!SceneBuilderHelper.OpenScene(SceneName)) return;
        int created = 0;
        int found = 0;

        SceneBuilderHelper.EnsureCamera("Main Camera", ref created, ref found);
        SceneBuilderHelper.EnsureEventSystem(ref created, ref found);
        var mgr = SceneBuilderHelper.EnsureEmptyGameObject("SettingsManager", ref created, ref found);
        SceneBuilderHelper.EnsureScript<SettingsManager>(mgr);

        var canvas = SceneBuilderHelper.EnsureCanvas("Canvas", ref created, ref found);
        if (canvas != null)
        {
            SceneBuilderHelper.EnsureTitle(canvas, "Settings", ref created, ref found);
            SceneBuilderHelper.EnsureScrollView(canvas, ref created, ref found);

            // DefaultsButton — bottom-left area (kit Secondary; was a Button128 sprite)
            var defaults = UiKit.Button(canvas, "DefaultsButton", "Defaults", UiKit.UiButtonStyle.Secondary, 24f);
            if (defaults != null)
            {
                defaults.anchorMin = defaults.anchorMax = new Vector2(0.5f, 0.5f);
                defaults.sizeDelta = new Vector2(160f, 64f);
                defaults.anchoredPosition = new Vector2(-467.31f, -968.7f);
            }

            // SaveButton — bottom-right area (kit Primary: the screen's commit action)
            var save = UiKit.Button(canvas, "SaveButton", "Save", UiKit.UiButtonStyle.Primary, 24f);
            if (save != null)
            {
                save.anchorMin = save.anchorMax = new Vector2(0.5f, 0.5f);
                save.sizeDelta = new Vector2(160f, 64f);
                save.anchoredPosition = new Vector2(460.2f, -968.7f);
            }

            SceneBuilderHelper.EnsureBackButton(canvas, ref created, ref found);
            SceneBuilderHelper.EnsureFadeOverlay(canvas, ref created, ref found);

            // Wire onClick events
            var settingsManager = mgr.GetComponent<SettingsManager>();
            if (settingsManager != null)
            {
                var defaultsBtn = canvas.Find("DefaultsButton")?.GetComponent<Button>();
                if (defaultsBtn != null)
                    SceneBuilderHelper.WireOnClick(defaultsBtn, new UnityAction(settingsManager.OnDefaultsButtonClick));

                var saveBtn = canvas.Find("SaveButton")?.GetComponent<Button>();
                if (saveBtn != null)
                    SceneBuilderHelper.WireOnClick(saveBtn, new UnityAction(settingsManager.OnSaveButtonClicked));

                var backBtn = canvas.Find("BackButton")?.GetComponent<Button>();
                if (backBtn != null)
                    SceneBuilderHelper.WireOnClick(backBtn, new UnityAction(settingsManager.OnBackButtonClicked));
            }
        }

        SceneBuilderHelper.LogResults(SceneName, created, found);
    }

}
