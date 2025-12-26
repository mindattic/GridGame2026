using Assets.Helper;
using UnityEditor;
using UnityEngine;
using g = Assets.Helpers.GameHelper;

public partial class DebugWindow
{
    // RenderLevels renders buttons for stage control: Load, Previous, and None.
    private void RenderLevels()
    {
        GUILayout.BeginHorizontal();
        GUILayout.Label("Stage Options", EditorStyles.boldLabel, GUILayout.Width(Screen.width));
        GUILayout.EndHorizontal();

        GUILayout.BeginHorizontal();

        if (GUILayout.Button("Restart", GUILayout.Width(Screen.width * Increment.Percent33)))
            OnReloadStageClick();

        //if (GUILayout.Button("< Previous", GUILayout.Width(Screen.thumbnailScaleX * Constants.percent33)))
        //    OnPreviousStageClick();

        //if (GUILayout.Button("None >", GUILayout.Width(Screen.thumbnailScaleX * Constants.percent33)))
        //    OnNextStageClick();

        GUILayout.EndHorizontal();
        GUILayout.Space(10);
    }

    // Stage control methods:
    // Reloads the CurrentProfile stage.
    private void OnReloadStageClick() => g.StageManager.RestartStage();
    // Moves to the previous stage.
    //private void OnPreviousStageClick() => g.StageManager.Previous();
    // Moves to the next stage.
    //private void OnNextStageClick() => g.StageManager.None();
}
