using Assets.Helper;
using System.Linq;
using UnityEditor;
using UnityEngine;
using g = Assets.Helpers.GameHelper;

public partial class DebugWindow
{
    // RenderSpawnOptions renders buttons to spawn various enemy types.
    private void RenderSpawnOptions()
    {
        GUILayout.BeginHorizontal();
        GUILayout.Label("Spawn Options", EditorStyles.boldLabel, GUILayout.Width(Screen.width));
        GUILayout.EndHorizontal();

        bool isClicked;
        GUILayout.BeginHorizontal();

        isClicked = GUILayout.Button("Slime", GUILayout.Width(Screen.width * Increment.Percent25));
        if (isClicked)
            g.DebugManager.SpawnSlime();

        isClicked = GUILayout.Button("Bat", GUILayout.Width(Screen.width * Increment.Percent25));
        if (isClicked)
            g.DebugManager.SpawnBat();

        isClicked = GUILayout.Button("Scorpion", GUILayout.Width(Screen.width * Increment.Percent25));
        if (isClicked)
            g.DebugManager.SpawnScorpion();

        isClicked = GUILayout.Button("Yeti", GUILayout.Width(Screen.width * Increment.Percent25));
        if (isClicked)
            g.DebugManager.SpawnYeti();

        GUILayout.EndHorizontal();
        GUILayout.BeginHorizontal();

        isClicked = GUILayout.Button("Soldier", GUILayout.Width(Screen.width * Increment.Percent25));
        if (isClicked)
            g.DebugManager.SpawnSoldier();

        GUILayout.EndHorizontal();
        GUILayout.Space(10);
    }

}
