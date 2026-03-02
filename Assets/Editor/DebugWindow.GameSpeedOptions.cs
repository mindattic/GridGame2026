using Scripts.Helpers;
using UnityEditor;
using UnityEngine;
using g = Scripts.Helpers.GameHelper;
using s = Scripts.Helpers.SettingsHelper;
using Scripts.Canvas;
using Scripts.Data.Actor;
using Scripts.Data.Items;
using Scripts.Data.Skills;
using Scripts.Effects;
using Scripts.Factories;
using Scripts.Hub;
using Scripts.Instances;
using Scripts.Instances.Actor;
using Scripts.Instances.Board;
using Scripts.Instances.SynergyLine;
using Scripts.Inventory;
using Scripts.Libraries;
using Scripts.Managers;
using Scripts.Models;
using Scripts.Models.Actor;
using Scripts.Overworld;
using Scripts.Sequences;
using Scripts.Serialization;
using Scripts.Utilities;

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
