using Assets.Helper;
using UnityEditor;
using UnityEngine;
using g = Assets.Helpers.GameHelper;
using s = Assets.Helpers.SettingsHelper;

public partial class DebugWindow
{
    // RenderGameSpeed renders a dropdown to select the game speed and an Apply button.
    private void RenderGameSpeedOptions()
    {
        GUILayout.BeginHorizontal();
        GUILayout.Label("Game Speed", GUILayout.Width(Screen.width * 0.25f));
        selectedGameFocus = (GameSpeedOption)EditorGUILayout.EnumPopup(selectedGameFocus, GUILayout.Width(Screen.width * 0.5f));
        if (GUILayout.Button("Apply", GUILayout.Width(Screen.width * 0.25f)))
            OnGameSpeedChange();
        GUILayout.EndHorizontal();
        GUILayout.Space(10);
    }

    // OnGameSpeedChange adjusts the game speed based on the selected option.
    private void OnGameSpeedChange()
    {
        switch (selectedGameFocus)
        {
            case GameSpeedOption.Paused:
                s.GameSpeed = 0f;
                break;
            case GameSpeedOption.Percent25:
                s.GameSpeed = 0.25f;
                break;
            case GameSpeedOption.Percent50:
                s.GameSpeed = 0.5f;
                break;
            case GameSpeedOption.Normal:
                s.GameSpeed = 1f;
                break;
            case GameSpeedOption.Percent125:
                s.GameSpeed = 1.25f;
                break;
            case GameSpeedOption.Percent150:
                s.GameSpeed = 1.5f;
                break;
        }
    }
}
