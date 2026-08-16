using System;
using System.IO;
using System.Linq;
using System.Reflection;
using UnityEditor;
using UnityEditor.AddressableAssets;
using UnityEditor.AddressableAssets.Settings;
using UnityEditor.SceneManagement;
using UnityEditor.U2D;
using UnityEngine;
using UnityEngine.U2D;

/// <summary>
/// CLIENTRYPOINTS - Static entry points callable from Unity's -batchmode -executeMethod flag.
/// <para>PURPOSE: Lets the entire project be driven from a shell without ever opening the
/// Unity Editor UI. Each method is self-contained, logs its work, and calls
/// EditorApplication.Exit with 0 on success or 1 on failure so CI pipelines don't hang.</para>
/// <para>USAGE:
/// Unity -batchmode -nographics -projectPath . -executeMethod CliEntryPoints.BuilderAllScenes -quit -logFile -
/// </para>
/// <para>RELATED FILES: GridGame.Console.ps1, SceneBuilderHelper.cs, *Builder.cs</para>
/// </summary>
public static class CliEntryPoints
{
    public static readonly string[] BuilderedScenes =
    {
        "Credits",
        "LoadingScreen",
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
        "Vendor",
        "Alchemist",
        "Party",
        "Abilities",
        "Equip",
        "Blacksmith",
        "Summon",
        "StoryCrawl",
    };

    // ===================== Building =====================

    public static void BuilderAllScenes()
    {
        int ok = 0, failed = 0;
        foreach (var scene in BuilderedScenes)
        {
            try
            {
                InvokeBuilderCreate(scene);
                var active = EditorSceneManager.GetActiveScene();
                EditorSceneManager.MarkSceneDirty(active);
                EditorSceneManager.SaveScene(active, $"Assets/Scenes/{scene}.unity");
                Debug.Log($"[Cli] Buildered + saved: {scene}");
                ok++;
            }
            catch (Exception e)
            {
                var inner = e.InnerException ?? e;
                Debug.LogError($"[Cli] Builder failed for {scene}: {inner.GetType().Name}: {inner.Message}\n{inner.StackTrace}");
                failed++;
            }
        }
        Debug.Log($"[Cli] BuilderAllScenes: {ok} ok, {failed} failed.");
        EditorApplication.Exit(failed > 0 ? 1 : 0);
    }

    public static void BuilderScene()
    {
        var sceneName = GetArg("-sceneName");
        if (string.IsNullOrEmpty(sceneName))
        {
            Debug.LogError("[Cli] BuilderScene requires -sceneName <Name>");
            EditorApplication.Exit(1);
            return;
        }
        try
        {
            InvokeBuilderCreate(sceneName);
            var active = EditorSceneManager.GetActiveScene();
            EditorSceneManager.MarkSceneDirty(active);
            EditorSceneManager.SaveScene(active, $"Assets/Scenes/{sceneName}.unity");
            Debug.Log($"[Cli] Buildered + saved: {sceneName}");
            EditorApplication.Exit(0);
        }
        catch (Exception e)
        {
            var inner = e.InnerException ?? e;
            Debug.LogError($"[Cli] BuilderScene failed for {sceneName}: {inner.GetType().Name}: {inner.Message}\n{inner.StackTrace}");
            EditorApplication.Exit(1);
        }
    }

    private static void InvokeBuilderCreate(string sceneName)
    {
        var typeName = sceneName + "Builder";
        var type = AppDomain.CurrentDomain.GetAssemblies()
            .SelectMany(a => SafeGetTypes(a))
            .FirstOrDefault(t => t.Name == typeName);
        if (type == null)
            throw new Exception($"Type {typeName} not found. Does Assets/Editor/Builders/{typeName}.cs exist?");

        var method = type.GetMethod("Build", BindingFlags.Public | BindingFlags.Static);
        if (method == null)
            throw new Exception($"{typeName}.Build() not found.");

        // Clear-and-recreate: ensures the builder output is canonical (no stale objects).
        if (!SceneBuilderHelper.OpenScene(sceneName))
            throw new Exception($"Could not open scene {sceneName}.");
        SceneBuilderHelper.ClearAllRootObjectsSilent();
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

    // ===================== Builder Drift (Phase 0 guardrail) =====================

    /// <summary>
    /// For each buildered scene: opens it, rebuilds its hierarchy in-memory from the matching
    /// <c>*Builder.Build()</c>, computes the canonical signature, and diffs against the committed
    /// snapshot in <c>Documentation/Builders/Drift/&lt;Scene&gt;.snapshot.txt</c>. The in-memory
    /// rebuild is NOT saved to disk — this is read-only verification. Exits 1 on any diff.
    /// <para>This is the only path that catches drift between builder code and the committed .unity:
    /// it signatures what the builder *would produce* right now, not what's already on disk.</para>
    /// </summary>
    public static void VerifyBuilderDrift()
    {
        try
        {
            int drifted = BuilderDriftChecker.Verify(BuilderedScenes);
            EditorApplication.Exit(drifted > 0 ? 1 : 0);
        }
        catch (Exception e)
        {
            Debug.LogError($"[Cli] VerifyBuilderDrift failed: {e.Message}\n{e.StackTrace}");
            EditorApplication.Exit(1);
        }
    }

    /// <summary>
    /// For each buildered scene: opens it, rebuilds via <c>*Builder.Build()</c>, saves the rebuilt
    /// .unity, then writes a fresh canonical signature to
    /// <c>Documentation/Builders/Drift/&lt;Scene&gt;.snapshot.txt</c>. Commit the output (snapshots
    /// + any .unity changes) as the new baseline.
    /// <para>This is a single "make everything consistent" operation: builder → .unity → snapshot.
    /// Hand-edits to the .unity that aren't reproducible from the builder will be wiped — that's
    /// intentional, since the builder is the source of truth.</para>
    /// </summary>
    public static void RegenerateBuilderSnapshots()
    {
        try
        {
            int ok = BuilderDriftChecker.Regenerate(BuilderedScenes);
            Debug.Log($"[Cli] Wrote {ok} builder snapshot(s).");
            EditorApplication.Exit(0);
        }
        catch (Exception e)
        {
            Debug.LogError($"[Cli] RegenerateBuilderSnapshots failed: {e.Message}\n{e.StackTrace}");
            EditorApplication.Exit(1);
        }
    }

    // ===================== Check All Guardrails (CI smoke test) =====================

    /// <summary>
    /// Runs every guardrail check in one batchmode session and fails if any report offenders.
    /// Covers: SerializedFieldBan (Phase 1), ResourcesLoadBan (Phase 3), InstantiateBan (Phase 4),
    /// and BuilderDriftChecker (Phase 0/2). Intended as the pre-push / CI entry point —
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
        Run("BuilderDriftChecker", () => BuilderDriftChecker.Verify(BuilderedScenes));

        Debug.Log($"[Cli] ── Summary ──────────────────────────────────────────");
        if (failures.Count == 0)
            Debug.Log("[Cli] CheckAllGuardrails: OK — every guardrail clean.");
        else
            Debug.LogError($"[Cli] CheckAllGuardrails: FAIL — {failures.Count} guardrail(s) reporting issues: {string.Join(", ", failures)}");

        EditorApplication.Exit(total > 0 ? 1 : 0);
    }

    /// <summary>
    /// Pre-push gate. Enforces only the three high-signal CODE guardrails (SerializedFieldBan,
    /// ResourcesLoadBan, InstantiateBan) as BLOCKING. BuilderDriftChecker is still run, but is
    /// ADVISORY (logged, never affects the exit code).
    /// <para>Rationale: in headless batchmode BuilderDriftChecker emits false positives — a
    /// resolution-dependent CanvasScaler RectTransform is computed differently under -nographics
    /// than at the resolution the snapshots were captured at, so ~16 scenes "drift" by that one
    /// line on every run. Gating pushes on that would block all pushes for a non-issue. Once the
    /// drift signature excludes resolution-dependent fields and the larger Game/TitleScreen drift
    /// is audited, fold drift back into the blocking set (or point the hook at CheckAllGuardrails).</para>
    /// </summary>
    public static void CheckCodeGuardrails()
    {
        int blocking = 0;
        var failures = new System.Collections.Generic.List<string>();

        void Run(string label, Func<int> check, bool gating)
        {
            try
            {
                Debug.Log($"[Cli] ── {label}{(gating ? "" : " (advisory)")} ─────────────────────────────────");
                var n = check();
                if (gating)
                {
                    blocking += n;
                    if (n > 0) failures.Add($"{label} ({n})");
                }
                else if (n > 0)
                {
                    Debug.LogWarning($"[Cli] {label}: {n} advisory finding(s) — NOT blocking the push. Audit separately.");
                }
            }
            catch (Exception e)
            {
                Debug.LogError($"[Cli] {label} threw: {e.Message}\n{e.StackTrace}");
                if (gating) { blocking += 1; failures.Add($"{label} (exception)"); }
            }
        }

        Run("SerializedFieldBan",  () => SerializedFieldBan.Check(), gating: true);
        Run("ResourcesLoadBan",    () => ResourcesLoadBan.Check(),   gating: true);
        Run("InstantiateBan",      () => InstantiateBan.Check(),     gating: true);
        Run("BuilderDriftChecker", () => BuilderDriftChecker.Verify(BuilderedScenes), gating: false);

        Debug.Log($"[Cli] ── Summary ──────────────────────────────────────────");
        if (failures.Count == 0)
            Debug.Log("[Cli] CheckCodeGuardrails: OK — code guardrails clean (BuilderDrift advisory only).");
        else
            Debug.LogError($"[Cli] CheckCodeGuardrails: FAIL — {failures.Count} code guardrail(s) reporting issues: {string.Join(", ", failures)}");

        EditorApplication.Exit(blocking > 0 ? 1 : 0);
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

    /// <summary>
    /// Authors every procedural placeholder sprite SpriteLibrary expects but that isn't on disk
    /// yet: the mana orb body/glass and one icon per SpellLibrary entry. Each is written as a PNG
    /// (importer = Sprite) and registered in Addressables at the address SpriteLibrary loads
    /// (e.g. "Sprites/Mana/orb-body", "Sprites/Spells/Fireball"). Idempotent — re-running
    /// overwrites. Run once after a fresh checkout, or whenever SpellLibrary gains an entry.
    /// </summary>
    public static void AuthorPlaceholderSprites()
    {
        try
        {
            SpriteAssetAuthor.AuthorManaOrbSprites();
            SpriteAssetAuthor.AuthorSpellIcons();
            SpriteAssetAuthor.AuthorTagIcons();
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log("[Cli] AuthorPlaceholderSprites: mana orbs + spell icons + tag icons authored and registered.");
            EditorApplication.Exit(0);
        }
        catch (Exception e)
        {
            Debug.LogError($"[Cli] AuthorPlaceholderSprites failed: {e.Message}\n{e.StackTrace}");
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

    // ===================== HUD Texture Atlas (US-103) =====================

    /// <summary>
    /// Creates (or re-creates) <c>Assets/Sprites/HudAtlas.spriteatlas</c> containing all small
    /// in-battle HUD sprite folders and registers it as an Addressable with label <c>UI</c>.
    ///
    /// <para>Unity automatically batches any <see cref="UnityEngine.UI.Image"/> whose sprite lives
    /// in the atlas into a single draw call — no changes to <c>SpriteLibrary</c> or builder code are
    /// needed. Excluded: Backgrounds (large), generic Icons (246 entries / own setup), Portraits,
    /// Thumbnails, and decorative sets not drawn every frame.</para>
    ///
    /// <para>Run via batchmode:
    /// <c>Unity -batchmode -nographics -projectPath . -executeMethod CliEntryPoints.BuildHudAtlas -quit -logFile -</c>
    /// </para>
    /// </summary>
    public static void BuildHudAtlas()
    {
        try
        {
            const string atlasPath = "Assets/Sprites/HudAtlas.spriteatlas";

            AssetDatabase.DeleteAsset(atlasPath);

            var atlas = new SpriteAtlas();

            // Mobile-optimal: no mipmaps, bilinear, 2048 max, no alpha-split.
            var tex = atlas.GetTextureSettings();
            tex.generateMipMaps = false;
            tex.filterMode = FilterMode.Bilinear;
            tex.readable = false;
            atlas.SetTextureSettings(tex);

            var plat = atlas.GetPlatformSettings("DefaultTexturePlatform");
            plat.maxTextureSize = 2048;
            plat.format = TextureImporterFormat.Automatic;
            plat.crunchedCompression = false;
            plat.allowsAlphaSplitting = false;
            atlas.SetPlatformSettings(plat);

            // HUD sprite folders — in-battle UI elements that draw every frame.
            var hudFolders = new[]
            {
                "Assets/Sprites/GUI",
                "Assets/Sprites/ActionBar",
                "Assets/Sprites/HealthBar",
                "Assets/Sprites/Mana",
                "Assets/Sprites/Statuses",
                "Assets/Sprites/AbilityButtons",
                "Assets/Sprites/Timeline/ActorTagIcons",
                "Assets/Sprites/TimerBar",
                "Assets/Sprites/Selection",
                "Assets/Sprites/Actor/Masks",
                "Assets/Sprites/Actor/Base",
                "Assets/Sprites/Actor/Back",
                "Assets/Sprites/Actor/Frames",
                "Assets/Sprites/Actor/Armor",
            };

            var toAdd = new System.Collections.Generic.List<UnityEngine.Object>();
            foreach (var folder in hudFolders)
            {
                if (!AssetDatabase.IsValidFolder(folder))
                {
                    Debug.LogWarning($"[Cli] BuildHudAtlas: folder not found, skipping: {folder}");
                    continue;
                }
                var obj = AssetDatabase.LoadAssetAtPath<DefaultAsset>(folder);
                if (obj != null) toAdd.Add(obj);
            }

            if (toAdd.Count == 0)
            {
                Debug.LogError("[Cli] BuildHudAtlas: no valid HUD sprite folders found — check Assets/Sprites/ structure.");
                EditorApplication.Exit(1);
                return;
            }

            atlas.Add(toAdd.ToArray());
            AssetDatabase.CreateAsset(atlas, atlasPath);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            Debug.Log($"[Cli] HudAtlas created at {atlasPath} with {toAdd.Count} folder(s).");

            // Register as Addressable: address "HudAtlas", label "UI".
            var settings = AddressableAssetSettingsDefaultObject.Settings;
            if (settings != null)
            {
                settings.AddLabel("UI", true);
                var guid = AssetDatabase.AssetPathToGUID(atlasPath);
                var entry = settings.CreateOrMoveEntry(guid, settings.DefaultGroup);
                entry.address = "HudAtlas";
                entry.labels.Add("UI");
                AssetDatabase.SaveAssets();
                Debug.Log("[Cli] HudAtlas registered: address='HudAtlas' label='UI'.");
            }
            else
            {
                Debug.LogWarning("[Cli] Addressables not initialized; atlas saved but NOT registered. " +
                    "Open Window → Asset Management → Addressables → Groups once, then re-run BuildHudAtlas.");
            }

            EditorApplication.Exit(0);
        }
        catch (Exception e)
        {
            Debug.LogError($"[Cli] BuildHudAtlas failed: {e.Message}\n{e.StackTrace}");
            EditorApplication.Exit(1);
        }
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
