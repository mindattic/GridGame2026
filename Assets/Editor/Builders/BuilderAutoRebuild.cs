using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Security.Cryptography;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// BUILDERAUTOREBUILD - Watches <c>Assets/Editor/Builders/*Builder.cs</c> for content changes and
/// rebuilds the matching <c>Assets/Scenes/{Name}.unity</c> after each domain reload.
/// <para>DIRECTION: Strictly Builder → Scene. The reverse (Scene → Builder) requires a human/LLM
/// to translate YAML to C# and is intentionally not automated; edits made directly to a .unity in
/// the Editor are not synced back.</para>
/// <para>CHANGE DETECTION: Uses content-SHA1 (not mtime) so a <c>git pull</c> that touches a builder
/// file without changing its bytes won't trigger a spurious rebuild. The cache survives across
/// machines because hashes depend on bytes, not local clock state.</para>
/// <para>FIRST RUN: On editor launch with no cache, current hashes are recorded silently. Nothing
/// is rebuilt — the cache is only used to detect deltas going forward.</para>
/// <para>PLAY MODE: Rebuilds are deferred until edit mode resumes.</para>
/// <para>SAVE PROMPT: If the currently-loaded scene is dirty, the user is prompted to save before
/// the destructive clear-and-recreate happens. Batchmode skips the prompt (nothing dirty there).</para>
/// <para>POST-REBUILD VERIFY: After each successful rebuild, the rebuilt scene's signature is
/// compared to its committed snapshot. Divergence logs a warning but does NOT auto-update the
/// snapshot — that's a deliberate signal to either regenerate snapshots (intentional builder change)
/// or fix the builder (regression).</para>
/// </summary>
[InitializeOnLoad]
public static class BuilderAutoRebuild
{
    private const string BuildersFolder = "Assets/Editor/Builders";
    private const string ScenesFolder   = "Assets/Scenes";
    private const string CachePath      = "Library/BuilderHashes.json";

    // Files in BuildersFolder that aren't scene builders (helpers, watchers, codegen).
    private static readonly HashSet<string> NotASceneBuilder = new HashSet<string>
    {
        "SceneBuilderHelper",
        "VendorNavBarBuilder",
        "BuilderAutoRebuild",
    };

    [Serializable] private class Entry { public string name; public string hash; }
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

        var targets = ScanBuilderHashes().Keys
            .Where(name => !NotASceneBuilder.Contains(name))
            .Select(StripBuilderSuffix)
            .Where(scene => File.Exists($"{ScenesFolder}/{scene}.unity"))
            .ToList();

        RebuildScenes(targets);
        SaveCache(ScanBuilderHashes());
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
        var current = ScanBuilderHashes();

        // First run on a fresh checkout / cleared Library — just record and exit.
        if (cache.entries.Count == 0)
        {
            SaveCache(current);
            return;
        }

        var changedBuilders = current
            .Where(kvp => !cache.entries.Any(e => e.name == kvp.Key && e.hash == kvp.Value))
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
        // Give the user a chance to save any dirty work before we wipe scenes. In batchmode no
        // scene is dirty so this is a no-op. In interactive mode they see one prompt covering
        // any modified scenes — better than silent destruction on every domain reload.
        if (!Application.isBatchMode)
        {
            EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo();
        }

        var originalActivePath = EditorSceneManager.GetActiveScene().path;
        int ok = 0, failed = 0, drifted = 0;
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
                    if (VerifyAgainstCommittedSnapshot(sceneName)) drifted++;
                    ok++;
                }
                catch (Exception e)
                {
                    // Unwrap TargetInvocationException so the real cause is visible. Also log
                    // ToString() (stack + inner chain) — was only logging .Message before, which
                    // hid the actual failure inside reflection invokes.
                    var root = e is System.Reflection.TargetInvocationException tie && tie.InnerException != null ? tie.InnerException : e;
                    Debug.LogError($"[BuilderAutoRebuild] {sceneName} failed: {root.GetType().Name}: {root.Message}\n{root}");
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

        if (drifted > 0)
        {
            Debug.LogWarning(
                $"[BuilderAutoRebuild] Done. 100% — {ok} ok, {failed} failed, {drifted} drifted from committed snapshot.\n" +
                "  Drifted scenes' rebuilds no longer match the committed signature — either the builder change\n" +
                "  was intentional (run CliEntryPoints.RegenerateBuilderSnapshots to bless the new state) or it's\n" +
                "  a regression (revert the builder edit).");
        }
        else
        {
            Debug.Log($"[BuilderAutoRebuild] Done. 100% — {ok} ok, {failed} failed.");
        }
    }

    // Returns true if the freshly-rebuilt scene's signature differs from the committed snapshot.
    // Logs a warning with the location of the snapshot for inspection; does NOT regenerate it.
    private static bool VerifyAgainstCommittedSnapshot(string sceneName)
    {
        try
        {
            var committed = BuilderDriftChecker.ReadCommittedSnapshot(sceneName);
            if (committed == null) return false; // no snapshot yet for this scene — not drift

            var current = BuilderDriftChecker.SignatureOfActiveScene();
            if (BuilderDriftChecker.SignaturesEqual(committed, current)) return false;

            Debug.LogWarning(
                $"[BuilderAutoRebuild] {sceneName}: rebuilt scene drifts from committed snapshot at " +
                $"Documentation/Builders/Drift/{sceneName}.snapshot.txt. " +
                $"Run CliEntryPoints.RegenerateBuilderSnapshots if the change is intentional.");
            return true;
        }
        catch (Exception e)
        {
            Debug.LogError($"[BuilderAutoRebuild] {sceneName}: post-rebuild signature check failed: {e.Message}");
            return false;
        }
    }

    private static void RebuildScene(string sceneName)
    {
        string path = $"{ScenesFolder}/{sceneName}.unity";
        var current = EditorSceneManager.GetActiveScene();
        if (current.path != path)
            EditorSceneManager.OpenScene(path);

        // Clear root objects silently — no Undo grouping, no dialog. (Save prompt happened
        // earlier in RebuildScenes.)
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

    // Content-SHA1 hashes — survives git checkouts that touch mtime without changing bytes.
    private static Dictionary<string, string> ScanBuilderHashes()
    {
        var result = new Dictionary<string, string>();
        if (!Directory.Exists(BuildersFolder)) return result;
        using (var sha = SHA1.Create())
        {
            foreach (var path in Directory.GetFiles(BuildersFolder, "*Builder.cs", SearchOption.TopDirectoryOnly))
            {
                var name = Path.GetFileNameWithoutExtension(path);
                var bytes = File.ReadAllBytes(path);
                var hash = BitConverter.ToString(sha.ComputeHash(bytes)).Replace("-", "");
                result[name] = hash;
            }
        }
        return result;
    }

    private static Cache LoadCache()
    {
        if (!File.Exists(CachePath)) return new Cache();
        try { return JsonUtility.FromJson<Cache>(File.ReadAllText(CachePath)) ?? new Cache(); }
        catch { return new Cache(); }
    }

    private static void SaveCache(Dictionary<string, string> hashes)
    {
        var c = new Cache
        {
            entries = hashes
                .Select(kvp => new Entry { name = kvp.Key, hash = kvp.Value })
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
