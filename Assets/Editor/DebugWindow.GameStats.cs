using Scripts.Helpers;
using Scripts.Helpers;
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
    // Draws focused actor, input mode, current turn, and sequence details.
    // Safe during scene switches: avoids HasFocusedActor and null-guards all managers.
    private void RenderGameStats()
    {
        GUILayout.BeginHorizontal();

        var selected = (g.ActorManager != null && g.Actors.SelectedActor != null) ? g.Actors.SelectedActor.characterClass : CharacterClass.None;
        GUILayout.Label($"Focused Actor: {selected}", GUILayout.Width(Screen.width *0.25f));

        var mode = g.InputManager != null ? g.InputManager.InputMode : InputMode.None;
        GUILayout.Label($"Input Mode: {mode}", GUILayout.Width(Screen.width *0.25f));

        var turnText = g.TurnManager != null ? (g.TurnManager.IsHeroTurn ? "Player" : "Opponent") : "-";
        GUILayout.Label($"Current Turn: {turnText}", GUILayout.Width(Screen.width *0.25f));

        GUILayout.EndHorizontal();
        GUILayout.Space(10);
        //GUILayout.BeginHorizontal();
        //var sequenceDetails = g.SequenceManager.GetDetails() ?? "-";
        //GUILayout.Label(sequenceDetails, GUILayout.Width(Screen.width * 0.25f));
        //GUILayout.EndHorizontal();
    }
}
