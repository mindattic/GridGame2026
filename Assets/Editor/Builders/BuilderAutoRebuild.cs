using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// BUILDERAUTOREBUILD - Watches <c>Assets/Editor/Builders/*Builder.cs</c> for mtime changes and
/// rebuilds the matching <c>Assets/Scenes/{Name}.unity</c> after each domain reload.
/// <para>DIRECTION: Strictly Builder → Scene. The reverse (Scene → Builder) requires a human/LLM
/// to translate YAML to C# and is intentionally not automated; edits made directly to a .unity in
/// the Editor are not synced back.</para>
/// <para>FIRST RUN: On editor launch with no cache, current mtimes are recorded silently. Nothing
/// is rebuilt — the cache is only used to detect deltas going forward.</para>
/// <para>PLAY MODE: Rebuilds are deferred until edit mode resumes.</para>
/// <para>ACTIVE SCENE: If a rebuild target matches the currently-loaded scene, the scene is reloaded
/// in place. Any in-editor changes to that scene are lost (builders are the source of truth).</para>
/// </summary>
[InitializeOnLoad]
public static class BuilderAutoRebuild
{
    private const string BuildersFolder = "Assets/Editor/Builders";
    private const string ScenesFolder   = "Assets/Scenes";
    private const string CachePath      = "Library/BuilderMTimes.json";

    // Files in BuildersFolder that aren't scene builders (helpers, watchers, codegen).
    private static readonly HashSet<string> NotASceneBuilder = new HashSet<string>
    {
        "SceneBuilderHelper",
        "VendorNavBarBuilder",
        "BuilderAutoRebuild",
    };

    [Serializable] private class Entry { public string name; public long mtimeTicks; }
    [Serializable] private class Cache { public List<Entry> entries = new List<Entry>(); }

    static BuilderAutoRebuild()
    {
        EditorApplication.delayCall += Tick;
    }

    private static void Tick()
    {
        try { RunPending(); }
        catch (Exception e) { Debug.LogError($"[BuilderAutoRebuild] {e.GetType().Name}: {e.Message}\n{e.StackTrace}"); }
    }

    [MenuItem("Tools/Scenes/Rebuild All")]
    public static void RebuildAllMenu()
    {
        if (!EditorUtility.DisplayDialog("Rebuild All Scenes",
            "Rebuild every Assets/Scenes/*.unity from its matching builder?\n\n" +
            "Any unsaved scene changes will be lost.",
            "Rebuild", "Cancel"))
            return;

        var targets = ScanBuilderMTimes().Keys
            .Where(name => !NotASceneBuilder.Contains(name))
            .Select(StripBuilderSuffix)
            .Where(scene => File.Exists($"{ScenesFolder}/{scene}.unity"))
            .ToList();

        RebuildScenes(targets);
        SaveCache(ScanBuilderMTimes());
    }

    private static void RunPending()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode || EditorApplication.isPlaying)
        {
            EditorApplication.playModeStateChanged -= OnPlayModeChanged;
            EditorApplication.playModeStateChanged += OnPlayModeChanged;
            return;
        }

        var cache   = LoadCache();
        var current = ScanBuilderMTimes();

        // First run on a fresh checkout / cleared Library — just record and exit.
        if (cache.entries.Count == 0)
        {
            SaveCache(current);
            return;
        }

        var changedBuilders = current
            .Where(kvp => !cache.entries.Any(e => e.name == kvp.Key && e.mtimeTicks == kvp.Value))
            .Select(kvp => kvp.Key)
            .ToList();

        if (changedBuilders.Count == 0) return;

        var targets = changedBuilders
            .Where(name => !NotASceneBuilder.Contains(name))
            .Select(StripBuilderSuffix)
            .Where(scene => File.Exists($"{ScenesFolder}/{scene}.unity"))
            .ToList();

        if (targets.Count == 0)
        {
            SaveCache(current);
            return;
        }

        Debug.Log($"[BuilderAutoRebuild] Rebuilding {targets.Count} scene(s): {string.Join(", ", targets)}");
        RebuildScenes(targets);
        SaveCache(current);
    }

    private static void OnPlayModeChanged(PlayModeStateChange state)
    {
        if (state == PlayModeStateChange.EnteredEditMode)
        {
            EditorApplication.playModeStateChanged -= OnPlayModeChanged;
            EditorApplication.delayCall += Tick;
        }
    }

    private static void RebuildScenes(IList<string> sceneNames)
    {
        var originalActivePath = EditorSceneManager.GetActiveScene().path;
        int ok = 0, failed = 0;
        int total = sceneNames.Count;
        bool showBar = !Application.isBatchMode;

        try
        {
            for (int i = 0; i < total; i++)
            {
                var sceneName = sceneNames[i];
                int pct = (int)((float)i / total * 100f);
                Debug.Log($"[BuilderAutoRebuild] ({i + 1}/{total}, {pct}%) Rebuilding {sceneName}.unity ...");
                if (showBar)
                    EditorUtility.DisplayProgressBar("Rebuilding Scenes",
                        $"({i + 1}/{total}) {sceneName}",
                        (float)i / total);

                try
                {
                    RebuildScene(sceneName);
                    ok++;
                }
                catch (Exception e)
                {
                    Debug.LogError($"[BuilderAutoRebuild] {sceneName} failed: {e.GetType().Name}: {e.Message}");
                    failed++;
                }
            }
        }
        finally
        {
            if (showBar) EditorUtility.ClearProgressBar();
        }

        // Restore originally-active scene if it still exists and differs from what we ended on.
        if (!string.IsNullOrEmpty(originalActivePath) && File.Exists(originalActivePath))
        {
            var activeNow = EditorSceneManager.GetActiveScene();
            if (activeNow.path != originalActivePath)
                EditorSceneManager.OpenScene(originalActivePath);
        }

        Debug.Log($"[BuilderAutoRebuild] Done. 100% — {ok} ok, {failed} failed.");
    }

    private static void RebuildScene(string sceneName)
    {
        string path = $"{ScenesFolder}/{sceneName}.unity";
        var current = EditorSceneManager.GetActiveScene();
        if (current.path != path)
            EditorSceneManager.OpenScene(path);

        // Clear root objects silently — no Undo grouping, no dialog.
        var roots = SceneManager.GetActiveScene().GetRootGameObjects();
        foreach (var go in roots)
            UnityEngine.Object.DestroyImmediate(go);

        var builderType = AppDomain.CurrentDomain.GetAssemblies()
            .SelectMany(SafeGetTypes)
            .FirstOrDefault(t => t.Name == sceneName + "Builder");
        if (builderType == null)
            throw new Exception($"{sceneName}Builder type not found");

        var build = builderType.GetMethod("Build", BindingFlags.Public | BindingFlags.Static);
        if (build == null)
            throw new Exception($"{sceneName}Builder.Build() not found");

        build.Invoke(null, null);

        var active = EditorSceneManager.GetActiveScene();
        EditorSceneManager.MarkSceneDirty(active);
        EditorSceneManager.SaveScene(active, path);
        Debug.Log($"[BuilderAutoRebuild] Rebuilt {sceneName}.unity");
    }

    private static string StripBuilderSuffix(string name) =>
        name.EndsWith("Builder") ? name.Substring(0, name.Length - "Builder".Length) : name;

    private static Dictionary<string, long> ScanBuilderMTimes()
    {
        var result = new Dictionary<string, long>();
        if (!Directory.Exists(BuildersFolder)) return result;
        foreach (var path in Directory.GetFiles(BuildersFolder, "*Builder.cs", SearchOption.TopDirectoryOnly))
        {
            var name = Path.GetFileNameWithoutExtension(path);
            result[name] = new FileInfo(path).LastWriteTimeUtc.Ticks;
        }
        return result;
    }

    private static Cache LoadCache()
    {
        if (!File.Exists(CachePath)) return new Cache();
        try { return JsonUtility.FromJson<Cache>(File.ReadAllText(CachePath)) ?? new Cache(); }
        catch { return new Cache(); }
    }

    private static void SaveCache(Dictionary<string, long> mtimes)
    {
        var c = new Cache
        {
            entries = mtimes
                .Select(kvp => new Entry { name = kvp.Key, mtimeTicks = kvp.Value })
                .ToList()
        };
        Directory.CreateDirectory(Path.GetDirectoryName(CachePath));
        File.WriteAllText(CachePath, JsonUtility.ToJson(c, true));
    }

    private static Type[] SafeGetTypes(Assembly a)
    {
        try { return a.GetTypes(); }
        catch (ReflectionTypeLoadException e) { return e.Types.Where(t => t != null).ToArray(); }
    }
}
