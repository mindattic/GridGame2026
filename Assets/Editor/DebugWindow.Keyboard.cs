using Scripts.Helpers;
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
    // RenderKeyboard draws UI buttons that simulate keyboard arrow keys.
    private void RenderKeyboard()
    {
        GUILayout.BeginVertical();
        GUILayout.BeginHorizontal();
        GUILayout.Label("Keyboard");
        GUILayout.EndHorizontal();

        bool isClicked;

        // Render "Up" arrow in the center.
        GUILayout.BeginHorizontal();
        GUILayout.Space(38); // Space to center the button.
        isClicked = GUILayout.Button("\u2191", GUILayout.Width(32), GUILayout.Height(32));
        if (isClicked)
            OnKeyUp();
        GUILayout.Space(38);
        GUILayout.EndHorizontal();

        // Render "Left", "Down", and "Right" arrows.
        GUILayout.BeginHorizontal();
        isClicked = GUILayout.Button("\u2190", GUILayout.Width(32), GUILayout.Height(32));
        if (isClicked)
            OnKeyLeft();

        isClicked = GUILayout.Button("\u2193", GUILayout.Width(32), GUILayout.Height(32));
        if (isClicked)
            OnKeyDown();

        isClicked = GUILayout.Button("\u2192", GUILayout.Width(32), GUILayout.Height(32));
        if (isClicked)
            OnKeyRight();

        GUILayout.EndHorizontal();
        GUILayout.EndVertical();

        GUILayout.Space(10);
        Repaint(); // Force a UI update.
    }

    // Keyboard control methods for actor Move.
    private void OnKeyUp()
    {
        if (!g.Actors.HasSelectedActor) return;
        g.Actors.SelectedActor.TeleportToward(Vector2Int.down);
    }

    private void OnKeyDown()
    {
        if (!g.Actors.HasSelectedActor) return;
        g.Actors.SelectedActor.TeleportToward(Vector2Int.up);
    }

    private void OnKeyLeft()
    {
        if (!g.Actors.HasSelectedActor) return;
        g.Actors.SelectedActor.TeleportToward(Vector2Int.left);
    }

    private void OnKeyRight()
    {
        if (!g.Actors.HasSelectedActor) return;
        g.Actors.SelectedActor.TeleportToward(Vector2Int.right);
    }
}
