//using UnityEngine;
//using UnityEditor;
//using UnityEditor.AddressableAssets;
//using UnityEditor.AddressableAssets.Settings;
//using System.Text;
//using System.IO;
//using System.Linq;
//using System.Collections.Generic;
//using Scripts.Canvas;
//using Scripts.Data.Actor;
//using Scripts.Data.Items;
//using Scripts.Data.Skills;
//using Scripts.Effects;
//using Scripts.Factories;
//using Scripts.Helpers;
//using Scripts.Hub;
//using Scripts.Instances;
//using Scripts.Instances.Actor;
//using Scripts.Instances.Board;
//using Scripts.Instances.SynergyLine;
//using Scripts.Inventory;
//using Scripts.Libraries;
//using Scripts.Managers;
//using Scripts.Models;
//using Scripts.Models.Actor;
//using Scripts.Overworld;
//using Scripts.Sequences;
//using Scripts.Serialization;
//using Scripts.Utilities;

///// <summary>
///// ADDRESSABLESANALYZER - Documents Addressables configuration for LLM context.
///// 
///// Extracts:
///// - All groups and their assets
///// - Asset addresses
///// - Labels
///// </summary>
//public class AddressablesAnalyzer : Editor
//{
//    [MenuItem("Tools/Analyze Addressables")]
//    /// <summary>Analyze addressables.</summary>
//    public static void AnalyzeAddressables()
//    {
//        var settings = AddressableAssetSettingsDefaultObject.Settings;
//        if (settings == null)
//        {
//            Debug.LogError("No Addressables settings found.");
//            return;
//        }

//        var sb = new StringBuilder();

//        sb.AppendLine("# Addressables Configuration");
//        sb.AppendLine();
//        sb.AppendLine("Auto-generated documentation of Addressable Asset System configuration.");
//        sb.AppendLine();
//        sb.AppendLine("---");
//        sb.AppendLine();

//        // Summary
//        var allEntries = new List<AddressableAssetEntry>();
//        foreach (var group in settings.groups)
//        {
//            if (group == null) continue;
//            allEntries.AddRange(group.entries);
//        }

//        sb.AppendLine("## Summary");
//        sb.AppendLine();
//        sb.AppendLine($"- **Groups**: {settings.groups.Count}");
//        sb.AppendLine($"- **Total Assets**: {allEntries.Count}");
//        sb.AppendLine($"- **Labels**: {string.Join(", ", settings.GetLabels().Select(l => $"`{l}`"))}");
//        sb.AppendLine();

//        // Groups
//        foreach (var group in settings.groups)
//        {
//            if (group == null) continue;

//            sb.AppendLine($"## Group: {group.Name}");
//            sb.AppendLine();
//            sb.AppendLine($"- **Entries**: {group.entries.Count}");
//            sb.AppendLine($"- **Read Only**: {group.ReadOnly}");
//            sb.AppendLine();

//            if (group.entries.Count > 0)
//            {
//                sb.AppendLine("### Assets");
//                sb.AppendLine();
//                sb.AppendLine("| Address | Asset Path | Labels |");
//                sb.AppendLine("|---------|------------|--------|");

//                foreach (var entry in group.entries.OrderBy(e => e.address))
//                {
//                    var labels = entry.labels.Count > 0 
//                        ? string.Join(", ", entry.labels) 
//                        : "-";
//                    sb.AppendLine($"| `{entry.address}` | `{entry.AssetPath}` | {labels} |");
//                }
//                sb.AppendLine();
//            }
//        }

//        // By Type Summary
//        sb.AppendLine("## Assets by Type");
//        sb.AppendLine();
        
//        var byType = allEntries
//            .GroupBy(e => e.MainAssetType?.Name ?? "Unknown")
//            .OrderByDescending(g => g.Count());

//        sb.AppendLine("| Type | Count |");
//        sb.AppendLine("|------|-------|");
//        foreach (var typeGroup in byType)
//        {
//            sb.AppendLine($"| {typeGroup.Key} | {typeGroup.Count()} |");
//        }
//        sb.AppendLine();

//        // Save
//        var outputDir = "Assets/Documentation";
//        if (!Directory.Exists(outputDir))
//            Directory.CreateDirectory(outputDir);

//        var outputPath = $"{outputDir}/Addressables.md";
//        File.WriteAllText(outputPath, sb.ToString());
//        AssetDatabase.Refresh();

//        Debug.Log($"Addressables analysis saved to: {outputPath}");
//        EditorUtility.RevealInFinder(outputPath);
//    }
//}
