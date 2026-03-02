using Scripts.Helpers;
using System.Linq;
using UnityEditor;
using UnityEngine;
using g = Scripts.Helpers.GameHelper;
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
