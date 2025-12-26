using Assets.Helper;
using UnityEditor;
using UnityEngine;
using scene = Assets.Helpers.SceneHelper;

public partial class DebugWindow
{
    // RenderScenes draws buttons to switch between different game scenes.
    private void RenderScenes()
    {
        GUILayout.BeginHorizontal();
        GUILayout.Label("SceneOptions", EditorStyles.boldLabel, GUILayout.Width(Screen.width));
        GUILayout.EndHorizontal();

        bool isClicked;

        GUILayout.BeginHorizontal();
        isClicked = GUILayout.Button("SplashScreen Screen", GUILayout.Width(Screen.width * Increment.Percent25));
        if (isClicked)
            scene.Fade.ToSplashScreen();

        isClicked = GUILayout.Button("TitleScreen Screen", GUILayout.Width(Screen.width * Increment.Percent25));
        if (isClicked)
            scene.Fade.ToTitleScreen();

        isClicked = GUILayout.Button("Settings", GUILayout.Width(Screen.width * Increment.Percent25));
        if (isClicked)
            scene.Fade.ToSettings();

        isClicked = GUILayout.Button("Stage Select", GUILayout.Width(Screen.width * Increment.Percent25));
        if (isClicked)
            scene.Fade.ToStageSelect();

        GUILayout.EndHorizontal();
        GUILayout.BeginHorizontal();

        isClicked = GUILayout.Button("Load Profile", GUILayout.Width(Screen.width * Increment.Percent25));
        if (isClicked)
            scene.Fade.ToProfileSelect();

        isClicked = GUILayout.Button("Load Save", GUILayout.Width(Screen.width * Increment.Percent25));
        if (isClicked)
            scene.Fade.ToSaveFileSelect();

        isClicked = GUILayout.Button("Overworld", GUILayout.Width(Screen.width * Increment.Percent25));
        if (isClicked)
            scene.Fade.ToOverworld();

        isClicked = GUILayout.Button("Game", GUILayout.Width(Screen.width * Increment.Percent25));
        if (isClicked)
            scene.Fade.ToGame();

        GUILayout.EndHorizontal();
        GUILayout.Space(10);
    }
}
