#if UNITY_EDITOR
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using Scripts.Data.Config;

/// <summary>
/// STARTSCENEAUTHORITY - Applies StartSceneConfig.StartScene to Unity's editor state.
/// <para>PURPOSE: StartSceneConfig.StartScene is the code-authored source of truth
/// for "which scene the game starts in". Unity has two separate surfaces that
/// need to agree with it:
/// <list type="bullet">
/// <item><description><c>EditorSceneManager.playModeStartScene</c> — forces Play Mode
/// to boot into the chosen scene regardless of which scene is currently open in the
/// editor. This is the editor-convenience half.</description></item>
/// <item><description><c>EditorBuildSettings.scenes[0]</c> — the entry a built standalone
/// player launches into. This is the shipping half. If the chosen scene isn't in the
/// list it's added at index 0; if it's already there it's moved to index 0.</description></item>
/// </list>
/// </para>
/// <para>LIFECYCLE: <c>[InitializeOnLoad]</c> runs the static constructor on every
/// domain reload (script compile, editor open, etc.), so editing
/// StartSceneConfig.cs — e.g. via GridGame.Console.ps1 Option 20 — automatically
/// re-syncs the editor state without a menu click. Idempotent and cheap; early-outs
/// when the scene can't be resolved.</para>
/// <para>RELATED FILES: StartSceneConfig.cs, GridGame.Console.ps1, CliEntryPoints.cs</para>
/// </summary>
[InitializeOnLoad]
public static class StartSceneAuthority
{
    public const string ScenesRoot = "Assets/Scenes";

    static StartSceneAuthority()
    {
        // Defer to first editor tick so AssetDatabase is definitely ready.
        EditorApplication.delayCall += Apply;
    }

    /// <summary>
    /// Re-applies StartSceneConfig.StartScene to playModeStartScene and
    /// EditorBuildSettings.scenes[0]. Safe to call repeatedly.
    /// </summary>
    public static void Apply()
    {
        var sceneName = StartSceneConfig.StartScene;
        if (string.IsNullOrEmpty(sceneName))
        {
            Debug.LogWarning("[StartSceneAuthority] StartSceneConfig.StartScene is empty — skipping apply.");
            return;
        }

        var path = FindScenePath(sceneName);
        if (string.IsNullOrEmpty(path))
        {
            Debug.LogWarning($"[StartSceneAuthority] Scene '{sceneName}' not found under {ScenesRoot}/.");
            return;
        }

        ApplyPlayModeStartScene(path, sceneName);
        ApplyBuildSettingsOrder(path, sceneName);
    }

    /// <summary>Sets EditorSceneManager.playModeStartScene if it's not already pointing at path.</summary>
    private static void ApplyPlayModeStartScene(string path, string sceneName)
    {
        var asset = AssetDatabase.LoadAssetAtPath<SceneAsset>(path);
        if (asset == null)
        {
            Debug.LogWarning($"[StartSceneAuthority] Could not load SceneAsset at {path}.");
            return;
        }

        if (EditorSceneManager.playModeStartScene == asset) return;

        EditorSceneManager.playModeStartScene = asset;
        Debug.Log($"[StartSceneAuthority] playModeStartScene → {sceneName}");
    }

    /// <summary>Ensures EditorBuildSettings.scenes[0] is the chosen path; inserts it if missing.</summary>
    private static void ApplyBuildSettingsOrder(string path, string sceneName)
    {
        var scenes = EditorBuildSettings.scenes.ToList();
        int idx = scenes.FindIndex(s => s.path == path);

        if (idx == 0) return; // already at head

        if (idx > 0)
        {
            var entry = scenes[idx];
            scenes.RemoveAt(idx);
            scenes.Insert(0, entry);
        }
        else
        {
            scenes.Insert(0, new EditorBuildSettingsScene(path, true));
        }

        EditorBuildSettings.scenes = scenes.ToArray();
        Debug.Log($"[StartSceneAuthority] EditorBuildSettings.scenes[0] → {sceneName}");
    }

    /// <summary>Returns the first Assets/Scenes/**/sceneName.unity path, or null.</summary>
    private static string FindScenePath(string sceneName)
    {
        if (!Directory.Exists(ScenesRoot)) return null;

        foreach (var file in Directory.EnumerateFiles(ScenesRoot, "*.unity", SearchOption.AllDirectories))
        {
            var fileName = Path.GetFileNameWithoutExtension(file);
            if (string.Equals(fileName, sceneName, System.StringComparison.Ordinal))
                return file.Replace('\\', '/');
        }
        return null;
    }
}
#endif
