using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;
using Assets.Scripts.Libraries; // TextStyleLibrary
using g = Assets.Helpers.GameHelper;

public partial class DebugWindow
{
    // Cache input per style key
    private Dictionary<string, string> textStyleInputs = new Dictionary<string, string>();

    private void RenderTextStyles()
    {
        if (!Application.isPlaying || g.CombatTextManager == null)
            return;

        GUILayout.Space(6);
        GUILayout.BeginHorizontal();
        GUILayout.Label("Text Styles", EditorStyles.boldLabel, GUILayout.Width(Screen.width));
        GUILayout.EndHorizontal();

        var styles = TextStyleLibrary.TextStyles;
        if (styles == null || styles.Count == 0)
        {
            GUILayout.Label("No text styles loaded.");
            return;
        }

        foreach (var kvp in styles.OrderBy(k => k.Key))
        {
            string key = kvp.Key;
            if (!textStyleInputs.ContainsKey(key))
                textStyleInputs[key] = key; // seed example text

            GUILayout.BeginHorizontal();
            textStyleInputs[key] = EditorGUILayout.TextField(key, textStyleInputs[key], GUILayout.Width(Screen.width * 0.70f));
            if (GUILayout.Button("Spawn", GUILayout.Width(Screen.width * 0.28f)))
            {
                var actor = GetRandomPlayingActor();
                if (actor != null)
                {
                    var msg = string.IsNullOrEmpty(textStyleInputs[key]) ? key : textStyleInputs[key];
                    g.CombatTextManager.Spawn(msg, actor.Position, key);
                }
                else
                {
                    Debug.LogWarning("No active actors found to spawn text on.");
                }
            }
            GUILayout.EndHorizontal();
        }

        GUILayout.Space(6);
    }

    private static ActorInstance GetRandomPlayingActor()
    {
        var all = g.Actors.All;
        if (all == null) return null;
        var playing = all.Where(a => a != null && a.IsPlaying).ToList();
        if (playing.Count == 0) return null;
        int idx = UnityEngine.Random.Range(0, playing.Count);
        return playing[idx];
    }
}
