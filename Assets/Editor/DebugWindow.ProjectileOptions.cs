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
    /// <summary>
    /// Renders projectile debug controls.
    /// Includes existing spawn options and Fireball Bezier curve tests.
    /// </summary>
    private void RenderProjectileOptions()
    {
        GUILayout.BeginHorizontal();
        GUILayout.Label("Projectile Options", EditorStyles.boldLabel, GUILayout.Width(Screen.width));
        GUILayout.EndHorizontal();


        // Row 1
        RenderButtonRow(
            ("Heal", () => g.DebugManager.Heal()),
            ("Fireball", () => g.DebugManager.Fireball()),
            ("Homing Spiral", () => g.DebugManager.HomingSpiral())
        );


        GUILayout.Space(10);
    }

}
