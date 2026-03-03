using Scripts.Helpers;
using Scripts.Libraries; // for TextStyleLibrary
using System.Collections.Generic;
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
using Scripts.Managers;
using Scripts.Models;
using Scripts.Models.Actor;
using Scripts.Overworld;
using Scripts.Sequences;
using Scripts.Serialization;
using Scripts.Utilities;

public partial class DebugWindow
{
    // RenderCheckboxes provides several toggles for various debug options.
    /// <summary>Render toggle options.</summary>
    private void RenderToggleOptions()
    {

        GUILayout.BeginHorizontal();
        GUILayout.Label("Toggle Options", EditorStyles.boldLabel, GUILayout.Width(Screen.width));
        GUILayout.EndHorizontal();

        GUILayout.BeginHorizontal();

        // Toggle to show or hide actor name tags.
        bool onCheckChanged = EditorGUILayout.Toggle("Show Actor Name?", g.DebugManager.showActorNameTag, GUILayout.Width(Screen.width * 0.25f));
        if (g.DebugManager.showActorNameTag != onCheckChanged)
        {
            g.DebugManager.showActorNameTag = onCheckChanged;
            g.Actors.All.ForEach(x => x.Render.SetNameTagEnabled(onCheckChanged));
        }

        // Toggle to show or hide actor frames.
        onCheckChanged = EditorGUILayout.Toggle("Show Actor Frame?", g.DebugManager.showActorFrame, GUILayout.Width(Screen.width * 0.25f));
        if (g.DebugManager.showActorFrame != onCheckChanged)
        {
            g.DebugManager.showActorFrame = onCheckChanged;
            g.Actors.All.ForEach(x => x.Render.SetFrameEnabled(onCheckChanged));
        }
    
        // Toggle for hero invincibility.
        onCheckChanged = EditorGUILayout.Toggle("Are Heroes Invincible?", g.DebugManager.isHeroInvincible, GUILayout.Width(Screen.width * 0.25f));
        if (g.DebugManager.isHeroInvincible != onCheckChanged)
            g.DebugManager.isHeroInvincible = onCheckChanged;

        GUILayout.EndHorizontal();
        GUILayout.BeginHorizontal();

        // Toggle for enemy invincibility.
        onCheckChanged = EditorGUILayout.Toggle("Are Enemies Invincible?", g.DebugManager.isEnemyInvincible, GUILayout.Width(Screen.width * 0.25f));
        if (g.DebugManager.isEnemyInvincible != onCheckChanged)
            g.DebugManager.isEnemyInvincible = onCheckChanged;

        // Toggle for infinite timer.
        onCheckChanged = EditorGUILayout.Toggle("Is Timer Infinite?", g.DebugManager.isTimerInfinite, GUILayout.Width(Screen.width * 0.25f));
        if (g.DebugManager.isTimerInfinite != onCheckChanged)
            g.DebugManager.isTimerInfinite = onCheckChanged;

        // Toggle for enemy stunned state.
        onCheckChanged = EditorGUILayout.Toggle("Is Opponent Stunned?", g.DebugManager.isEnemyStunned, GUILayout.Width(Screen.width * 0.25f));
        if (g.DebugManager.isEnemyStunned != onCheckChanged)
            g.DebugManager.isEnemyStunned = onCheckChanged;

        GUILayout.EndHorizontal();
        GUILayout.Space(10);
    }

}
