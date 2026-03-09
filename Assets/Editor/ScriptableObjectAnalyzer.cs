//using UnityEngine;
//using UnityEditor;
//using System.Text;
//using System.IO;
//using System.Reflection;
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
///// SCRIPTABLEOBJECTANALYZER - Documents ScriptableObject assets for LLM context.
///// 
///// Extracts:
///// - All fields and their values
///// - Nested object references
///// - Array/List contents
///// </summary>
//public class ScriptableObjectAnalyzer : Editor
//{
//    [MenuItem("Tools/Analyze Selected ScriptableObjects")]
//    /// <summary>Analyze selected.</summary>
//    public static void AnalyzeSelected()
//    {
//        var selected = Selection.objects;
//        if (selected.Length == 0)
//        {
//            Debug.LogWarning("No objects selected. Select ScriptableObject assets in Project window.");
//            return;
//        }

//        var sb = new StringBuilder();
//        sb.AppendLine("# ScriptableObject Analysis");
//        sb.AppendLine();

//        int count = 0;
//        foreach (var obj in selected)
//        {
//            if (obj is ScriptableObject so)
//            {
//                AnalyzeScriptableObject(so, sb);
//                count++;
//            }
//        }

//        if (count == 0)
//        {
//            Debug.LogWarning("No ScriptableObjects in selection.");
//            return;
//        }

//        // Save
//        var outputDir = "Assets/Documentation/ScriptableObjects";
//        if (!Directory.Exists(outputDir))
//            Directory.CreateDirectory(outputDir);

//        var outputPath = $"{outputDir}/ScriptableObjects_Analysis.md";
//        File.WriteAllText(outputPath, sb.ToString());
//        AssetDatabase.Refresh();

//        Debug.Log($"Analyzed {count} ScriptableObjects. Saved to: {outputPath}");
//        EditorUtility.RevealInFinder(outputPath);
//    }

//    [MenuItem("Tools/Analyze All ScriptableObjects of Type...")]
//    /// <summary>Analyze all of type.</summary>
//    public static void AnalyzeAllOfType()
//    {
//        // Find all ScriptableObject types in the project
//        var types = new List<System.Type>();
//        foreach (var assembly in System.AppDomain.CurrentDomain.GetAssemblies())
//        {
//            try
//            {
//                foreach (var type in assembly.GetTypes())
//                {
//                    if (type.IsSubclassOf(typeof(ScriptableObject)) && !type.IsAbstract)
//                    {
//                        // Skip Unity built-in types
//                        if (type.Namespace?.StartsWith("UnityEngine") == true) continue;
//                        if (type.Namespace?.StartsWith("UnityEditor") == true) continue;
//                        types.Add(type);
//                    }
//                }
//            }
//            catch { }
//        }

//        // Create menu
//        var menu = new GenericMenu();
//        foreach (var type in types)
//        {
//            var typeCopy = type; // Capture for closure
//            menu.AddItem(new GUIContent(type.Name), false, () => AnalyzeAllOfSpecificType(typeCopy));
//        }
//        menu.ShowAsContext();
//    }

//    /// <summary>Analyze all of specific type.</summary>
//    private static void AnalyzeAllOfSpecificType(System.Type type)
//    {
//        var guids = AssetDatabase.FindAssets($"t:{type.Name}");
//        if (guids.Length == 0)
//        {
//            Debug.LogWarning($"No assets found of type {type.Name}");
//            return;
//        }

//        var sb = new StringBuilder();
//        sb.AppendLine($"# {type.Name} Assets");
//        sb.AppendLine();
//        sb.AppendLine($"Total: {guids.Length} assets");
//        sb.AppendLine();

//        foreach (var guid in guids)
//        {
//            var path = AssetDatabase.GUIDToAssetPath(guid);
//            var asset = AssetDatabase.LoadAssetAtPath<ScriptableObject>(path);
//            if (asset != null)
//            {
//                AnalyzeScriptableObject(asset, sb);
//            }
//        }

//        // Save
//        var outputDir = "Assets/Documentation/ScriptableObjects";
//        if (!Directory.Exists(outputDir))
//            Directory.CreateDirectory(outputDir);

//        var outputPath = $"{outputDir}/{type.Name}_All.md";
//        File.WriteAllText(outputPath, sb.ToString());
//        AssetDatabase.Refresh();

//        Debug.Log($"Analyzed {guids.Length} {type.Name} assets. Saved to: {outputPath}");
//        EditorUtility.RevealInFinder(outputPath);
//    }

//    /// <summary>Analyze scriptable object.</summary>
//    private static void AnalyzeScriptableObject(ScriptableObject so, StringBuilder sb)
//    {
//        var path = AssetDatabase.GetAssetPath(so);
        
//        sb.AppendLine($"## {so.name}");
//        sb.AppendLine();
//        sb.AppendLine($"- **Type**: `{so.GetType().FullName}`");
//        sb.AppendLine($"- **Path**: `{path}`");
//        sb.AppendLine();

//        // Get all serialized fields
//        var serializedObject = new SerializedObject(so);
//        var iterator = serializedObject.GetIterator();

//        sb.AppendLine("### Properties");
//        sb.AppendLine();
//        sb.AppendLine("```yaml");

//        bool enterChildren = true;
//        while (iterator.NextVisible(enterChildren))
//        {
//            enterChildren = false;

//            // Skip m_Script
//            if (iterator.name == "m_Script") continue;

//            var value = GetPropertyValue(iterator);
//            sb.AppendLine($"{iterator.name}: {value}");
//        }

//        sb.AppendLine("```");
//        sb.AppendLine();
//        sb.AppendLine("---");
//        sb.AppendLine();
//    }

//    /// <summary>Gets the property value.</summary>
//    private static string GetPropertyValue(SerializedProperty prop)
//    {
//        switch (prop.propertyType)
//        {
//            case SerializedPropertyType.Integer:
//                return prop.intValue.ToString();
//            case SerializedPropertyType.Boolean:
//                return prop.boolValue.ToString().ToLower();
//            case SerializedPropertyType.Float:
//                return prop.floatValue.ToString("F2");
//            case SerializedPropertyType.String:
//                return $"\"{prop.stringValue}\"";
//            case SerializedPropertyType.Enum:
//                return prop.enumNames[prop.enumValueIndex];
//            case SerializedPropertyType.ObjectReference:
//                return prop.objectReferenceValue != null ? prop.objectReferenceValue.name : "null";
//            case SerializedPropertyType.Vector2:
//                return $"({prop.vector2Value.x:F2}, {prop.vector2Value.y:F2})";
//            case SerializedPropertyType.Vector3:
//                return $"({prop.vector3Value.x:F2}, {prop.vector3Value.y:F2}, {prop.vector3Value.z:F2})";
//            case SerializedPropertyType.Vector2Int:
//                return $"({prop.vector2IntValue.x}, {prop.vector2IntValue.y})";
//            case SerializedPropertyType.Color:
//                var c = prop.colorValue;
//                return $"rgba({c.r:F2}, {c.g:F2}, {c.b:F2}, {c.a:F2})";
//            case SerializedPropertyType.ArraySize:
//                return $"[size: {prop.intValue}]";
//            case SerializedPropertyType.Generic:
//                if (prop.isArray)
//                    return $"[array: {prop.arraySize} elements]";
//                return "[object]";
//            default:
//                return $"[{prop.propertyType}]";
//        }
//    }
//}
