using UnityEngine;
using UnityEditor;
using UnityEditor.Animations;
using System.Text;
using System.IO;
using System.Linq;
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
/// ANIMATORANALYZER - Documents Animator Controllers for LLM context.
/// 
/// Extracts:
/// - States and their motion clips
/// - Parameters
/// - Transitions and conditions
/// - Layers
/// </summary>
public class AnimatorAnalyzer : Editor
{
    [MenuItem("Tools/Analyze Selected Animator Controller")]
    /// <summary>Analyze selected.</summary>
    public static void AnalyzeSelected()
    {
        var selected = Selection.activeObject as AnimatorController;
        if (selected == null)
        {
            Debug.LogWarning("Select an Animator Controller in the Project window.");
            return;
        }

        AnalyzeController(selected);
    }

    /// <summary>Analyze controller.</summary>
    public static void AnalyzeController(AnimatorController controller)
    {
        var sb = new StringBuilder();

        sb.AppendLine($"# Animator Controller: {controller.name}");
        sb.AppendLine();
        sb.AppendLine($"Path: `{AssetDatabase.GetAssetPath(controller)}`");
        sb.AppendLine();
        sb.AppendLine("---");
        sb.AppendLine();

        // Parameters
        sb.AppendLine("## Parameters");
        sb.AppendLine();
        if (controller.parameters.Length == 0)
        {
            sb.AppendLine("*No parameters*");
        }
        else
        {
            sb.AppendLine("| Name | Type | Default |");
            sb.AppendLine("|------|------|---------|");
            foreach (var param in controller.parameters)
            {
                var defaultVal = param.type switch
                {
                    AnimatorControllerParameterType.Bool => param.defaultBool.ToString().ToLower(),
                    AnimatorControllerParameterType.Int => param.defaultInt.ToString(),
                    AnimatorControllerParameterType.Float => param.defaultFloat.ToString("F2"),
                    AnimatorControllerParameterType.Trigger => "trigger",
                    _ => "?"
                };
                sb.AppendLine($"| `{param.name}` | {param.type} | {defaultVal} |");
            }
        }
        sb.AppendLine();

        // Layers
        for (int i = 0; i < controller.layers.Length; i++)
        {
            var layer = controller.layers[i];
            sb.AppendLine($"## Layer {i}: {layer.name}");
            sb.AppendLine();
            sb.AppendLine($"- **Weight**: {layer.defaultWeight:F2}");
            sb.AppendLine($"- **Blending**: {layer.blendingMode}");
            sb.AppendLine();

            var stateMachine = layer.stateMachine;

            // States
            sb.AppendLine("### States");
            sb.AppendLine();
            sb.AppendLine("```");
            foreach (var state in stateMachine.states)
            {
                var s = state.state;
                var motionName = s.motion != null ? s.motion.name : "null";
                var defaultMarker = (stateMachine.defaultState == s) ? " [DEFAULT]" : "";
                sb.AppendLine($"- {s.name}{defaultMarker}");
                sb.AppendLine($"    Motion: {motionName}");
                sb.AppendLine($"    Speed: {s.speed:F2}");
            }
            sb.AppendLine("```");
            sb.AppendLine();

            // Transitions
            sb.AppendLine("### Transitions");
            sb.AppendLine();
            sb.AppendLine("```");
            foreach (var state in stateMachine.states)
            {
                var s = state.state;
                foreach (var transition in s.transitions)
                {
                    var destName = transition.destinationState != null 
                        ? transition.destinationState.name 
                        : (transition.isExit ? "Exit" : "?");
                    
                    sb.Append($"{s.name} --> {destName}");
                    
                    if (transition.conditions.Length > 0)
                    {
                        var conditions = string.Join(" && ", transition.conditions.Select(c => 
                            $"{c.parameter} {c.mode} {c.threshold}"));
                        sb.Append($" [{conditions}]");
                    }
                    sb.AppendLine();
                }
            }

            // Any State transitions
            foreach (var transition in stateMachine.anyStateTransitions)
            {
                var destName = transition.destinationState != null 
                    ? transition.destinationState.name : "?";
                sb.Append($"AnyState --> {destName}");
                if (transition.conditions.Length > 0)
                {
                    var conditions = string.Join(" && ", transition.conditions.Select(c => 
                        $"{c.parameter} {c.mode} {c.threshold}"));
                    sb.Append($" [{conditions}]");
                }
                sb.AppendLine();
            }
            sb.AppendLine("```");
            sb.AppendLine();
        }

        // Save
        var outputDir = "Assets/Documentation/Animators";
        if (!Directory.Exists(outputDir))
            Directory.CreateDirectory(outputDir);

        var outputPath = $"{outputDir}/{controller.name}_Analysis.md";
        File.WriteAllText(outputPath, sb.ToString());
        AssetDatabase.Refresh();

        Debug.Log($"Animator analysis saved to: {outputPath}");
        EditorUtility.RevealInFinder(outputPath);
    }
}
