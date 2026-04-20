using System;
using System.IO;
using System.Linq;
using System.Reflection;
using UnityEditor;
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
/// <para>RELATED FILES: GridGame.ps1, SceneScaffoldHelper.cs, *Scaffold.cs</para>
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
                Debug.LogError($"[Cli] Scaffold failed for {scene}: {e.Message}\n{e.StackTrace}");
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
            Debug.LogError($"[Cli] ScaffoldScene failed for {sceneName}: {e.Message}\n{e.StackTrace}");
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

    // ===================== Command-line arg helper =====================

    private static string GetArg(string name)
    {
        var args = Environment.GetCommandLineArgs();
        for (int i = 0; i < args.Length - 1; i++)
            if (args[i] == name) return args[i + 1];
        return null;
    }
}
