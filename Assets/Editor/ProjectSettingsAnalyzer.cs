using UnityEngine;
using UnityEditor;
using System.Text;
using System.IO;
using System.Linq;
using System.Collections.Generic;
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
/// PROJECTSETTINGSANALYZER - Documents Unity project settings for LLM context.
/// 
/// Extracts human-readable information from:
/// - Sorting Layers
/// - Tags
/// - Layers
/// - Input Axes
/// </summary>
public class ProjectSettingsAnalyzer : Editor
{
    [MenuItem("Tools/Analyze Project Settings")]
    public static void AnalyzeProjectSettings()
    {
        var sb = new StringBuilder();

        sb.AppendLine("# Project Settings Documentation");
        sb.AppendLine();
        sb.AppendLine("Auto-generated documentation of Unity project settings.");
        sb.AppendLine();
        sb.AppendLine("---");
        sb.AppendLine();

        // Sorting Layers
        sb.AppendLine("## Sorting Layers");
        sb.AppendLine();
        sb.AppendLine("Used for 2D sprite rendering order.");
        sb.AppendLine();
        sb.AppendLine("| Index | Name |");
        sb.AppendLine("|-------|------|");
        foreach (var layer in SortingLayer.layers)
        {
            sb.AppendLine($"| {layer.id} | `{layer.name}` |");
        }
        sb.AppendLine();

        // Tags
        sb.AppendLine("## Tags");
        sb.AppendLine();
        sb.AppendLine("GameObject identification tags.");
        sb.AppendLine();
        sb.AppendLine("```");
        foreach (var tag in UnityEditorInternal.InternalEditorUtility.tags)
        {
            sb.AppendLine($"- {tag}");
        }
        sb.AppendLine("```");
        sb.AppendLine();

        // Layers
        sb.AppendLine("## Layers");
        sb.AppendLine();
        sb.AppendLine("Physics and rendering layers.");
        sb.AppendLine();
        sb.AppendLine("| Index | Name |");
        sb.AppendLine("|-------|------|");
        for (int i = 0; i < 32; i++)
        {
            var name = LayerMask.LayerToName(i);
            if (!string.IsNullOrEmpty(name))
            {
                sb.AppendLine($"| {i} | `{name}` |");
            }
        }
        sb.AppendLine();

        // Build Scenes
        sb.AppendLine("## Build Scenes");
        sb.AppendLine();
        sb.AppendLine("Scenes included in build (in order).");
        sb.AppendLine();
        sb.AppendLine("| Index | Scene Path | Enabled |");
        sb.AppendLine("|-------|------------|---------|");
        var scenes = EditorBuildSettings.scenes;
        for (int i = 0; i < scenes.Length; i++)
        {
            var scene = scenes[i];
            var sceneName = Path.GetFileNameWithoutExtension(scene.path);
            sb.AppendLine($"| {i} | `{scene.path}` | {(scene.enabled ? "✓" : "✗")} |");
        }
        sb.AppendLine();

        // Quality Settings
        sb.AppendLine("## Quality Levels");
        sb.AppendLine();
        sb.AppendLine("```");
        var qualityNames = QualitySettings.names;
        for (int i = 0; i < qualityNames.Length; i++)
        {
            var marker = (i == QualitySettings.GetQualityLevel()) ? " (current)" : "";
            sb.AppendLine($"- [{i}] {qualityNames[i]}{marker}");
        }
        sb.AppendLine("```");
        sb.AppendLine();

        // Save
        var outputDir = "Assets/Documentation";
        if (!Directory.Exists(outputDir))
            Directory.CreateDirectory(outputDir);

        var outputPath = $"{outputDir}/ProjectSettings.md";
        File.WriteAllText(outputPath, sb.ToString());
        AssetDatabase.Refresh();

        Debug.Log($"Project settings documented at: {outputPath}");
        EditorUtility.RevealInFinder(outputPath);
    }
}
