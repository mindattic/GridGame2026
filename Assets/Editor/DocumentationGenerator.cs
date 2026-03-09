using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine.SceneManagement;
using System.IO;
using System.Text;
using Scripts.Canvas;
using Scripts.Data.Actor;
using Scripts.Data.Items;
using Scripts.Data.Skills;
using Scripts.Effects;
using Scripts.Factories;
using Scripts.Helpers;
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

/// <summary>
/// DOCUMENTATIONGENERATOR - Master tool to generate all LLM-readable documentation.
/// 
/// Generates:
/// - Project settings (tags, layers, sorting layers)
/// - Addressables configuration
/// - All scene hierarchies
/// - ScriptableObject data
/// 
/// Run before sharing project context with an LLM.
/// </summary>
public class DocumentationGenerator : EditorWindow
{
    [MenuItem("Tools/Generate All Documentation")]
    /// <summary>Generate all.</summary>
    public static void GenerateAll()
    {
        var sb = new StringBuilder();
        sb.AppendLine("=== DOCUMENTATION GENERATION ===\n");

        //// 1. Project Settings
        //sb.AppendLine("Generating: Project Settings...");
        //ProjectSettingsAnalyzer.AnalyzeProjectSettings();

        //// 2. Addressables
        //sb.AppendLine("Generating: Addressables...");
        //try
        //{
        //    AddressablesAnalyzer.AnalyzeAddressables();
        //}
        //catch (System.Exception e)
        //{
        //    sb.AppendLine($"  Warning: {e.Message}");
        //}

        // 3. Current Scene - call menu item directly
        sb.AppendLine("Generating: Current Scene...");
        EditorApplication.ExecuteMenuItem("Tools/Analyze Current Scene");

        sb.AppendLine("\n=== COMPLETE ===");
        sb.AppendLine("Documentation saved to: Assets/Documentation/");

        Debug.Log(sb.ToString());

        // Open documentation folder
        EditorUtility.RevealInFinder("Assets/Documentation/");
    }

    //[MenuItem("Tools/Documentation/Generate Project Settings")]
    ///// <summary>Gen project settings.</summary>
    //public static void GenProjectSettings()
    //{
    //    ProjectSettingsAnalyzer.AnalyzeProjectSettings();
    //}

    //[MenuItem("Tools/Documentation/Generate Addressables")]
    ///// <summary>Gen addressables.</summary>
    //public static void GenAddressables()
    //{
    //    AddressablesAnalyzer.AnalyzeAddressables();
    //}

    //[MenuItem("Tools/Documentation/Generate Current Scene")]
    ///// <summary>Gen current scene.</summary>
    //public static void GenCurrentScene()
    //{
    //    EditorApplication.ExecuteMenuItem("Tools/Analyze Current Scene");
    //}

    //[MenuItem("Tools/Documentation/Generate All Scenes")]
    ///// <summary>Gen all scenes.</summary>
    //public static void GenAllScenes()
    //{
    //    var scenes = EditorBuildSettings.scenes;
    //    var originalScene = SceneManager.GetActiveScene().path;

    //    foreach (var sceneSettings in scenes)
    //    {
    //        if (!sceneSettings.enabled) continue;

    //        EditorSceneManager.OpenScene(sceneSettings.path, OpenSceneMode.Single);
    //        EditorApplication.ExecuteMenuItem("Tools/Analyze Current Scene");
    //    }

    //    // Restore original scene
    //    if (!string.IsNullOrEmpty(originalScene))
    //    {
    //        EditorSceneManager.OpenScene(originalScene);
    //    }

    //    Debug.Log($"Generated documentation for {scenes.Length} scenes");
    //}
}
