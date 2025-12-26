using Assets.Helper;
using System.Linq;
using UnityEditor;
using UnityEngine;
using g = Assets.Helpers.GameHelper;

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
