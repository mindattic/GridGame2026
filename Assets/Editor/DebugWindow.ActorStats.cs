using Assets.Helper;
using System.Linq;
using UnityEditor;
using UnityEngine;
using g = Assets.Helpers.GameHelper;

public partial class DebugWindow
{
    // RenderActorStats displays a list of all hero and enemy actors with basic status info.
    private void RenderActorStats()
    {
        GUILayout.BeginHorizontal();
        GUILayout.Label("Stats", EditorStyles.boldLabel, GUILayout.Width(Screen.width));
        GUILayout.EndHorizontal();

        // Display hero Stats sorted by name.
        foreach (var x in g.Actors.Heroes.OrderBy(x => x.name))
        {
            GUILayout.BeginHorizontal();
            string stats = $"{x.name}, IsAlive? {x.IsAlive}, IsActive? {x.IsActive}";
            GUILayout.Label(stats, GUILayout.Width(Screen.width));
            GUILayout.EndHorizontal();
        }

        GUILayout.Space(10);

        // Display enemy Stats sorted by name.
        foreach (var x in g.Actors.Enemies.OrderBy(x => x.name))
        {
            GUILayout.BeginHorizontal();
            string stats = $"{x.name}, IsAlive? {x.IsAlive}, IsActive? {x.IsActive}";
            GUILayout.Label(stats, GUILayout.Width(Screen.width));
            GUILayout.EndHorizontal();
        }

        GUILayout.Space(10);
    }
}
