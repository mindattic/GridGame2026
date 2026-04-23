using System;
using System.IO;
using System.Linq;
using System.Reflection;
using UnityEditor;
using UnityEditor.AddressableAssets;
using UnityEditor.AddressableAssets.Settings;
using UnityEditor.SceneManagement;
using UnityEngine;

/// <summary>
/// CLIENTRYPOINTS - Static entry points callable from Unity's -batchmode -executeMethod flag.
/// <para>PURPOSE: Lets the entire project be driven from a shell without ever opening the
/// Unity Editor UI. Each method is self-contained, logs its work, and calls
/// EditorApplication.Exit with 0 on success or 1 on failure so CI pipelines don't hang.</para>
/// <para>USAGE:
/// Unity -batchmode -nographics -projectPath . -executeMethod CliEntryPoints.ScaffoldAllScenes -quit -logFile -
/// </para>
/// <para>RELATED FILES: GridGame.Console.ps1, SceneScaffoldHelper.cs, *Scaffold.cs</para>
/// </summary>
public static class CliEntryPoints
{
    private static readonly string[] ScaffoldedScenes =
    {
        "Credits",
        "Hub",
        "LoadingScreen",
        "PartyManager",
        "PostBattleScreen",
        "ProfileCreate",
        "ProfileSelect",
        "SaveFileSelect",
        "Settings",
        "SplashScreen",
        "StageSelect",
        "TitleScreen",
        "Game",
        "Overworld",
    };

    // ===================== Scaffolding =====================

    public static void ScaffoldAllScenes()
    {
        int ok = 0, failed = 0;
        foreach (var scene in ScaffoldedScenes)
        {
            try
            {
                InvokeScaffoldCreate(scene);
                var active = EditorSceneManager.GetActiveScene();
                EditorSceneManager.MarkSceneDirty(active);
                EditorSceneManager.SaveScene(active, $"Assets/Scenes/{scene}.unity");
                Debug.Log($"[Cli] Scaffolded + saved: {scene}");
                ok++;
            }
            catch (Exception e)
            {
                var inner = e.InnerException ?? e;
                Debug.LogError($"[Cli] Scaffold failed for {scene}: {inner.GetType().Name}: {inner.Message}\n{inner.StackTrace}");
                failed++;
            }
        }
        Debug.Log($"[Cli] ScaffoldAllScenes: {ok} ok, {failed} failed.");
        EditorApplication.Exit(failed > 0 ? 1 : 0);
    }

    public static void ScaffoldScene()
    {
        var sceneName = GetArg("-sceneName");
        if (string.IsNullOrEmpty(sceneName))
        {
            Debug.LogError("[Cli] ScaffoldScene requires -sceneName <Name>");
            EditorApplication.Exit(1);
            return;
        }
        try
        {
            InvokeScaffoldCreate(sceneName);
            var active = EditorSceneManager.GetActiveScene();
            EditorSceneManager.MarkSceneDirty(active);
            EditorSceneManager.SaveScene(active, $"Assets/Scenes/{sceneName}.unity");
            Debug.Log($"[Cli] Scaffolded + saved: {sceneName}");
            EditorApplication.Exit(0);
        }
        catch (Exception e)
        {
            var inner = e.InnerException ?? e;
            Debug.LogError($"[Cli] ScaffoldScene failed for {sceneName}: {inner.GetType().Name}: {inner.Message}\n{inner.StackTrace}");
            EditorApplication.Exit(1);
        }
    }

    private static void InvokeScaffoldCreate(string sceneName)
    {
        var typeName = sceneName + "Scaffold";
        var type = AppDomain.CurrentDomain.GetAssemblies()
            .SelectMany(a => SafeGetTypes(a))
            .FirstOrDefault(t => t.Name == typeName);
        if (type == null)
            throw new Exception($"Type {typeName} not found. Does Assets/Editor/Scaffolds/{typeName}.cs exist?");

        var method = type.GetMethod("CreateScaffolding", BindingFlags.Public | BindingFlags.Static);
        if (method == null)
            throw new Exception($"{typeName}.CreateScaffolding() not found.");

        // Clear-and-recreate: ensures the scaffold output is canonical (no stale objects).
        if (!SceneScaffoldHelper.OpenScene(sceneName))
            throw new Exception($"Could not open scene {sceneName}.");
        SceneScaffoldHelper.ClearAllRootObjectsSilent();
        method.Invoke(null, null);
    }

    private static Type[] SafeGetTypes(Assembly a)
    {
        try { return a.GetTypes(); }
        catch (ReflectionTypeLoadException e) { return e.Types.Where(t => t != null).ToArray(); }
    }

    // ===================== Documentation =====================

    public static void GenerateDocs()
    {
        try
        {
            var type = AppDomain.CurrentDomain.GetAssemblies()
                .SelectMany(a => SafeGetTypes(a))
                .FirstOrDefault(t => t.Name == "DocumentationGenerator");
            if (type == null)
            {
                Debug.LogError("[Cli] DocumentationGenerator not found.");
                EditorApplication.Exit(1);
                return;
            }
            // Try a conventional entry point; fall back to any public static void method named Generate*.
            var method = type.GetMethod("GenerateAll", BindingFlags.Public | BindingFlags.Static)
                      ?? type.GetMethod("Generate", BindingFlags.Public | BindingFlags.Static)
                      ?? type.GetMethods(BindingFlags.Public | BindingFlags.Static)
                             .FirstOrDefault(m => m.Name.StartsWith("Generate") && m.GetParameters().Length == 0);
            if (method == null)
            {
                Debug.LogError("[Cli] No DocumentationGenerator.Generate* method found.");
                EditorApplication.Exit(1);
                return;
            }
            method.Invoke(null, null);
            Debug.Log($"[Cli] Docs generated via {type.Name}.{method.Name}.");
            EditorApplication.Exit(0);
        }
        catch (Exception e)
        {
            Debug.LogError($"[Cli] GenerateDocs failed: {e.Message}\n{e.StackTrace}");
            EditorApplication.Exit(1);
        }
    }

    // ===================== Tests =====================

    public static void RunEditTests() => RunTests(TestModeEnum.Edit);
    public static void RunPlayTests() => RunTests(TestModeEnum.Play);

    private enum TestModeEnum { Edit, Play }

    private static void RunTests(TestModeEnum mode)
    {
        // TestRunnerApi lives in com.unity.test-framework; load via reflection so this file
        // compiles even when the package isn't installed yet.
        try
        {
            var apiType = AppDomain.CurrentDomain.GetAssemblies()
                .SelectMany(a => SafeGetTypes(a))
                .FirstOrDefault(t => t.FullName == "UnityEditor.TestTools.TestRunner.Api.TestRunnerApi");
            if (apiType == null)
            {
                Debug.LogError("[Cli] TestRunnerApi not found. Install com.unity.test-framework.");
                EditorApplication.Exit(1);
                return;
            }
            Debug.LogWarning("[Cli] TestRunnerApi reflection path is a stub. " +
                "Wire concrete TestRunnerApi.Execute with ExecutionSettings when tests are authored.");
            EditorApplication.Exit(0);
        }
        catch (Exception e)
        {
            Debug.LogError($"[Cli] Run{mode}Tests failed: {e.Message}\n{e.StackTrace}");
            EditorApplication.Exit(1);
        }
    }

    // ===================== SerializedField Ban (Phase 0 guardrail) =====================

    /// <summary>
    /// Scans Assets/Scripts for [SerializeField] fields and fails if any are not present
    /// in Assets/Editor/SerializedFieldAllowlist.txt. Exit 0 = clean, 1 = new offenders detected.
    /// </summary>
    public static void CheckSerializedFieldBan()
    {
        try
        {
            var offenders = SerializedFieldBan.Check();
            EditorApplication.Exit(offenders > 0 ? 1 : 0);
        }
        catch (Exception e)
        {
            Debug.LogError($"[Cli] CheckSerializedFieldBan failed: {e.Message}\n{e.StackTrace}");
            EditorApplication.Exit(1);
        }
    }

    /// <summary>
    /// Overwrites the allowlist with the current scan results. Run after intentionally
    /// removing [SerializeField] fields (Phase 1+) or — rarely — after explicitly approving a new one.
    /// </summary>
    public static void RegenerateSerializedFieldAllowlist()
    {
        try
        {
            var count = SerializedFieldBan.Regenerate();
            Debug.Log($"[Cli] Allowlist regenerated with {count} entr{(count == 1 ? "y" : "ies")}.");
            EditorApplication.Exit(0);
        }
        catch (Exception e)
        {
            Debug.LogError($"[Cli] RegenerateSerializedFieldAllowlist failed: {e.Message}\n{e.StackTrace}");
            EditorApplication.Exit(1);
        }
    }

    // ===================== Resources.Load Ban (Phase 3 guardrail) =====================

    /// <summary>
    /// Scans Assets/Scripts for Resources.Load calls and fails if any file is not present
    /// in Assets/Editor/ResourcesLoadAllowlist.txt. Exit 0 = clean, 1 = new offenders detected.
    /// </summary>
    public static void CheckResourcesLoadBan()
    {
        try
        {
            var offenders = ResourcesLoadBan.Check();
            EditorApplication.Exit(offenders > 0 ? 1 : 0);
        }
        catch (Exception e)
        {
            Debug.LogError($"[Cli] CheckResourcesLoadBan failed: {e.Message}\n{e.StackTrace}");
            EditorApplication.Exit(1);
        }
    }

    /// <summary>
    /// Overwrites the allowlist with the current scan results. Run after migrating a
    /// file's Resources.Load calls to AssetHelper.LoadAsset (Phase 3+).
    /// </summary>
    public static void RegenerateResourcesLoadAllowlist()
    {
        try
        {
            var count = ResourcesLoadBan.Regenerate();
            Debug.Log($"[Cli] ResourcesLoad allowlist regenerated with {count} entr{(count == 1 ? "y" : "ies")}.");
            EditorApplication.Exit(0);
        }
        catch (Exception e)
        {
            Debug.LogError($"[Cli] RegenerateResourcesLoadAllowlist failed: {e.Message}\n{e.StackTrace}");
            EditorApplication.Exit(1);
        }
    }

    // ===================== Instantiate Ban (Phase 4 guardrail) =====================

    /// <summary>
    /// Scans Assets/Scripts for Instantiate call-sites outside *Factory.cs files and fails
    /// if any file is not present in Assets/Editor/InstantiateAllowlist.txt.
    /// Exit 0 = clean, 1 = new offenders detected.
    /// </summary>
    public static void CheckInstantiateBan()
    {
        try
        {
            var offenders = InstantiateBan.Check();
            EditorApplication.Exit(offenders > 0 ? 1 : 0);
        }
        catch (Exception e)
        {
            Debug.LogError($"[Cli] CheckInstantiateBan failed: {e.Message}\n{e.StackTrace}");
            EditorApplication.Exit(1);
        }
    }

    /// <summary>
    /// Overwrites the allowlist with the current scan results. Run after migrating a
    /// file's Instantiate calls into a *Factory.cs (Phase 4+).
    /// </summary>
    public static void RegenerateInstantiateAllowlist()
    {
        try
        {
            var count = InstantiateBan.Regenerate();
            Debug.Log($"[Cli] Instantiate allowlist regenerated with {count} entr{(count == 1 ? "y" : "ies")}.");
            EditorApplication.Exit(0);
        }
        catch (Exception e)
        {
            Debug.LogError($"[Cli] RegenerateInstantiateAllowlist failed: {e.Message}\n{e.StackTrace}");
            EditorApplication.Exit(1);
        }
    }

    // ===================== Scaffold Drift (Phase 0 guardrail) =====================

    /// <summary>
    /// Opens each scaffolded scene, walks its hierarchy, and emits a canonical signature text file.
    /// Compares against the committed snapshot in Documentation/Scaffolds/Drift/. Exits 1 on any diff.
    /// </summary>
    public static void VerifyScaffoldDrift()
    {
        try
        {
            int drifted = ScaffoldDriftChecker.Verify(ScaffoldedScenes);
            EditorApplication.Exit(drifted > 0 ? 1 : 0);
        }
        catch (Exception e)
        {
            Debug.LogError($"[Cli] VerifyScaffoldDrift failed: {e.Message}\n{e.StackTrace}");
            EditorApplication.Exit(1);
        }
    }

    /// <summary>
    /// Opens each scaffolded scene and writes a fresh canonical signature to
    /// Documentation/Scaffolds/Drift/&lt;Scene&gt;.snapshot.txt. Commit the output as the new baseline.
    /// </summary>
    public static void RegenerateScaffoldSnapshots()
    {
        try
        {
            int ok = ScaffoldDriftChecker.Regenerate(ScaffoldedScenes);
            Debug.Log($"[Cli] Wrote {ok} scaffold snapshot(s).");
            EditorApplication.Exit(0);
        }
        catch (Exception e)
        {
            Debug.LogError($"[Cli] RegenerateScaffoldSnapshots failed: {e.Message}\n{e.StackTrace}");
            EditorApplication.Exit(1);
        }
    }

    // ===================== Scaffold Save (scene → code) =====================

    /// <summary>
    /// Batchmode-friendly wrapper around SceneScaffoldGenerator.GenerateForScene.
    /// Accepts one or more -scene args (e.g. -scene Game -scene Overworld) or falls
    /// back to every entry in ScaffoldedScenes. Exits 1 if any scene fails to save.
    /// </summary>
    public static void SaveSceneScaffolds()
    {
        try
        {
            var scenes = GetArgs("-scene");
            if (scenes.Length == 0) scenes = ScaffoldedScenes;
            int ok = 0, failed = 0;
            foreach (var scene in scenes)
            {
                if (SceneScaffoldGenerator.GenerateForScene(scene, interactive: false))
                {
                    Debug.Log($"[Cli] Saved scaffold: {scene}");
                    ok++;
                }
                else
                {
                    failed++;
                }
            }
            Debug.Log($"[Cli] SaveSceneScaffolds: ok={ok} failed={failed}");
            EditorApplication.Exit(failed > 0 ? 1 : 0);
        }
        catch (Exception e)
        {
            Debug.LogError($"[Cli] SaveSceneScaffolds failed: {e.Message}\n{e.StackTrace}");
            EditorApplication.Exit(1);
        }
    }

    // ===================== Check All Guardrails (CI smoke test) =====================

    /// <summary>
    /// Runs every guardrail check in one batchmode session and fails if any report offenders.
    /// Covers: SerializedFieldBan (Phase 1), ResourcesLoadBan (Phase 3), InstantiateBan (Phase 4),
    /// and ScaffoldDriftChecker (Phase 0/2). Intended as the pre-push / CI entry point —
    /// four separate Unity launches collapse to one Editor warm-up.
    /// </summary>
    public static void CheckAllGuardrails()
    {
        int total = 0;
        var failures = new System.Collections.Generic.List<string>();

        void Run(string label, Func<int> check)
        {
            try
            {
                Debug.Log($"[Cli] ── {label} ─────────────────────────────────");
                var n = check();
                total += n;
                if (n > 0) failures.Add($"{label} ({n})");
            }
            catch (Exception e)
            {
                Debug.LogError($"[Cli] {label} threw: {e.Message}\n{e.StackTrace}");
                total += 1;
                failures.Add($"{label} (exception)");
            }
        }

        Run("SerializedFieldBan",  () => SerializedFieldBan.Check());
        Run("ResourcesLoadBan",    () => ResourcesLoadBan.Check());
        Run("InstantiateBan",      () => InstantiateBan.Check());
        Run("ScaffoldDriftChecker", () => ScaffoldDriftChecker.Verify(ScaffoldedScenes));

        Debug.Log($"[Cli] ── Summary ──────────────────────────────────────────");
        if (failures.Count == 0)
            Debug.Log("[Cli] CheckAllGuardrails: OK — every guardrail clean.");
        else
            Debug.LogError($"[Cli] CheckAllGuardrails: FAIL — {failures.Count} guardrail(s) reporting issues: {string.Join(", ", failures)}");

        EditorApplication.Exit(total > 0 ? 1 : 0);
    }

    // ===================== Build =====================

    public static void BuildStandaloneWindows()
    {
        try
        {
            var outputDir = GetArg("-buildOutput") ?? "Build/Windows";
            Directory.CreateDirectory(outputDir);
            var scenes = EditorBuildSettings.scenes.Where(s => s.enabled).Select(s => s.path).ToArray();
            if (scenes.Length == 0)
            {
                Debug.LogError("[Cli] No scenes enabled in EditorBuildSettings.");
                EditorApplication.Exit(1);
                return;
            }
            var opts = new BuildPlayerOptions
            {
                scenes = scenes,
                locationPathName = Path.Combine(outputDir, "GridGame.exe"),
                target = BuildTarget.StandaloneWindows64,
                options = BuildOptions.None,
            };
            var report = BuildPipeline.BuildPlayer(opts);
            var result = report.summary.result;
            Debug.Log($"[Cli] Build: {result}, size={report.summary.totalSize}B, errors={report.summary.totalErrors}");
            EditorApplication.Exit(result == UnityEditor.Build.Reporting.BuildResult.Succeeded ? 0 : 1);
        }
        catch (Exception e)
        {
            Debug.LogError($"[Cli] BuildStandaloneWindows failed: {e.Message}\n{e.StackTrace}");
            EditorApplication.Exit(1);
        }
    }

    // ===================== Generated Sprite Assets =====================

    /// <summary>
    /// Generates Assets/Sprites/ChevronScroll.png — a single-chevron-wide tileable strip (black
    /// stroke on transparent background) used by TimelineBarInstance as a UV-scrolling motion
    /// indicator. Registers the PNG as addressable under "Sprites/ChevronScroll" so SpriteLibrary
    /// can load it the same way as any hand-made sprite. Re-running the method overwrites the PNG
    /// and updates the addressable entry; swap with a designer-made replacement any time.
    /// </summary>
    public static void GenerateChevronScroll()
    {
        try
        {
            const string assetPath = "Assets/Sprites/ChevronScroll.png";
            const string address = "Sprites/ChevronScroll";
            const int W = 80;   // tile width (one chevron period)
            const int H = 40;   // tile height
            const float strokeHalfWidth = 4.5f; // 9px stroke
            const float aa = 1.0f;              // anti-alias edge width
            // Apex sits at ~75% of the tile — leaves breathing space between chevrons when tiled.
            var apex = new Vector2(W * 0.72f, H * 0.5f);
            var topLeft = new Vector2(0f, 0f);
            var bottomLeft = new Vector2(0f, H);
            // Next tile's chevron left-edge starts at x=W, so its segments project into our tile
            // as `(W, 0)→(W+apex.x, H/2)` and `(W, H)→(W+apex.x, H/2)`. Sample those too so the
            // stroke wraps cleanly across the tile seam.
            var nextTopLeft = new Vector2(W, 0f);
            var nextBottomLeft = new Vector2(W, H);
            var nextApex = new Vector2(W + apex.x, H * 0.5f);

            var pixels = new Color32[W * H];
            for (int y = 0; y < H; y++)
            {
                for (int x = 0; x < W; x++)
                {
                    var p = new Vector2(x + 0.5f, y + 0.5f);
                    float d = Mathf.Min(
                        DistanceToSegment(p, topLeft, apex),
                        DistanceToSegment(p, apex, bottomLeft),
                        DistanceToSegment(p, nextTopLeft, nextApex),
                        DistanceToSegment(p, nextApex, nextBottomLeft));
                    float coverage = 1f - Mathf.Clamp01((d - (strokeHalfWidth - aa)) / aa);
                    byte a = (byte)Mathf.RoundToInt(Mathf.Clamp01(coverage) * 255f);
                    pixels[y * W + x] = new Color32(0, 0, 0, a);
                }
            }

            var tex = new Texture2D(W, H, TextureFormat.RGBA32, mipChain: false);
            tex.SetPixels32(pixels);
            tex.Apply();
            var bytes = tex.EncodeToPNG();
            UnityEngine.Object.DestroyImmediate(tex);

            var dir = Path.GetDirectoryName(assetPath);
            if (!Directory.Exists(dir)) Directory.CreateDirectory(dir);
            File.WriteAllBytes(assetPath, bytes);
            AssetDatabase.ImportAsset(assetPath, ImportAssetOptions.ForceUpdate);

            var importer = AssetImporter.GetAtPath(assetPath) as TextureImporter;
            if (importer == null)
                throw new Exception($"Could not get TextureImporter for {assetPath}");
            importer.textureType = TextureImporterType.Sprite;
            importer.spriteImportMode = SpriteImportMode.Single;
            importer.wrapMode = TextureWrapMode.Repeat;
            importer.filterMode = FilterMode.Bilinear;
            importer.alphaIsTransparency = true;
            importer.mipmapEnabled = false;
            importer.SaveAndReimport();

            var settings = AddressableAssetSettingsDefaultObject.Settings;
            if (settings == null)
                throw new Exception("AddressableAssetSettings not found — project may be missing Addressables configuration.");
            var spritesGroup = settings.groups.FirstOrDefault(g => g != null && g.Name == "Sprites")
                               ?? settings.DefaultGroup;
            if (spritesGroup == null)
                throw new Exception("No 'Sprites' addressable group and no default group — cannot register entry.");
            var guid = AssetDatabase.AssetPathToGUID(assetPath);
            var entry = settings.CreateOrMoveEntry(guid, spritesGroup);
            entry.address = address;
            settings.SetDirty(AddressableAssetSettings.ModificationEvent.EntryMoved, entry, postEvent: true);
            AssetDatabase.SaveAssets();

            Debug.Log($"[Cli] GenerateChevronScroll: wrote {assetPath} ({W}x{H}) and registered '{address}' in group '{spritesGroup.Name}'.");
            EditorApplication.Exit(0);
        }
        catch (Exception e)
        {
            Debug.LogError($"[Cli] GenerateChevronScroll failed: {e.Message}\n{e.StackTrace}");
            EditorApplication.Exit(1);
        }
    }

    private static float DistanceToSegment(Vector2 p, Vector2 a, Vector2 b)
    {
        var ab = b - a;
        float lenSq = ab.sqrMagnitude;
        if (lenSq < 1e-6f) return Vector2.Distance(p, a);
        float t = Mathf.Clamp01(Vector2.Dot(p - a, ab) / lenSq);
        return Vector2.Distance(p, a + t * ab);
    }

    // ===================== Command-line arg helper =====================

    private static string GetArg(string name)
    {
        var args = Environment.GetCommandLineArgs();
        for (int i = 0; i < args.Length - 1; i++)
            if (args[i] == name) return args[i + 1];
        return null;
    }

    /// <summary>Returns every value following each occurrence of `name` in the command line.</summary>
    private static string[] GetArgs(string name)
    {
        var args = Environment.GetCommandLineArgs();
        var result = new System.Collections.Generic.List<string>();
        for (int i = 0; i < args.Length - 1; i++)
            if (args[i] == name) result.Add(args[i + 1]);
        return result.ToArray();
    }
}
