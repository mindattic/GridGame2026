//using UnityEngine;
//using UnityEditor;
//using UnityEditor.SceneManagement;
//using UnityEngine.SceneManagement;
//using System.Text;
//using System.IO;
//using System.Collections.Generic;
//using TMPro;
//using UnityEngine.UI;
//using UnityEngine.Rendering;
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
///// SCENEANALYZER - Generates human-readable scene hierarchy documentation.
///// 
///// PURPOSE:
///// Creates markdown documentation of Unity scenes that LLMs can interpret,
///// since .scene YAML files are not human-readable and contain GUIDs.
///// 
///// OUTPUT:
///// - Hierarchical structure of all GameObjects
///// - Components on each object with key properties
///// - Transform positions/scales
///// - References to prefabs, sprites, materials
///// 
///// USAGE:
///// Tools → Analyze Current Scene
///// Tools → Analyze All Scenes
///// </summary>
//public class SceneAnalyzer : EditorWindow
//{
//    private bool includeInactive = true;
//    private bool includeTransforms = true;
//    private bool includeRenderers = true;
//    private bool includeUI = true;
//    private bool includeScripts = true;
//    private int maxDepth = 20;

//    [MenuItem("Tools/Scene Analyzer")]
//    /// <summary>Show windows this component.</summary>
//    public static void ShowWindow()
//    {
//        GetWindow<SceneAnalyzer>("Scene Analyzer");
//    }

//    [MenuItem("Tools/Analyze Current Scene")]
//    /// <summary>Analyze current scene menu.</summary>
//    public static void AnalyzeCurrentSceneMenu()
//    {
//        var scene = SceneManager.GetActiveScene();
//        AnalyzeScene(scene, true, true, true, true, true, 20);
//    }

//    /// <summary>Draws immediate-mode GUI elements.</summary>
//    private void OnGUI()
//    {
//        EditorGUILayout.LabelField("Scene Analyzer", EditorStyles.boldLabel);
//        EditorGUILayout.Space();

//        includeInactive = EditorGUILayout.Toggle("Include Inactive Objects", includeInactive);
//        includeTransforms = EditorGUILayout.Toggle("Include Transform Details", includeTransforms);
//        includeRenderers = EditorGUILayout.Toggle("Include Renderers", includeRenderers);
//        includeUI = EditorGUILayout.Toggle("Include UI Components", includeUI);
//        includeScripts = EditorGUILayout.Toggle("Include Custom Scripts", includeScripts);
//        maxDepth = EditorGUILayout.IntSlider("Max Depth", maxDepth, 1, 50);

//        EditorGUILayout.Space();

//        if (GUILayout.Button("Analyze Current Scene", GUILayout.Height(30)))
//        {
//            var scene = SceneManager.GetActiveScene();
//            AnalyzeScene(scene, includeInactive, includeTransforms, includeRenderers, includeUI, includeScripts, maxDepth);
//        }

//        EditorGUILayout.Space();

//        if (GUILayout.Button("Analyze All Build Scenes", GUILayout.Height(30)))
//        {
//            AnalyzeAllScenes();
//        }
//    }

//    /// <summary>Analyze all scenes.</summary>
//    private void AnalyzeAllScenes()
//    {
//        var scenes = EditorBuildSettings.scenes;
//        foreach (var sceneSettings in scenes)
//        {
//            if (!sceneSettings.enabled) continue;

//            var scene = EditorSceneManager.OpenScene(sceneSettings.path, OpenSceneMode.Single);
//            AnalyzeScene(scene, includeInactive, includeTransforms, includeRenderers, includeUI, includeScripts, maxDepth);
//        }
//        Debug.Log($"Analyzed {scenes.Length} scenes");
//    }

//    /// <summary>Analyzes the scene hierarchy and writes a markdown file to Assets/Documentation/Scenes/.</summary>
//    public static void AnalyzeScene(Scene scene, bool includeInactive, bool includeTransforms, 
//        bool includeRenderers, bool includeUI, bool includeScripts, int maxDepth,
//        bool revealInFinder = true)
//    {
//        var sb = new StringBuilder();
        
//        sb.AppendLine($"# Scene: {scene.name}");
//        sb.AppendLine($"Path: `{scene.path}`");
//        sb.AppendLine($"Build Index: {scene.buildIndex}");
//        sb.AppendLine($"Root Object Count: {scene.rootCount}");
//        sb.AppendLine();
//        sb.AppendLine("---");
//        sb.AppendLine();
//        sb.AppendLine("## Hierarchy");
//        sb.AppendLine();
//        sb.AppendLine("```");

//        var rootObjects = scene.GetRootGameObjects();
//        foreach (var root in rootObjects)
//        {
//            AnalyzeGameObject(root, 0, sb, includeInactive, includeTransforms, 
//                includeRenderers, includeUI, includeScripts, maxDepth);
//        }

//        sb.AppendLine("```");
//        sb.AppendLine();

//        // Summary section
//        sb.AppendLine("## Summary");
//        sb.AppendLine();
        
//        var allObjects = new List<GameObject>();
//        foreach (var root in rootObjects)
//        {
//            CollectAllObjects(root, allObjects, includeInactive);
//        }

//        var managers = new List<string>();
//        var canvasObjects = new List<string>();
//        var cameras = new List<string>();

//        foreach (var obj in allObjects)
//        {
//            if (obj.name.EndsWith("Manager"))
//                managers.Add(obj.name);
//            if (obj.GetComponent<Canvas>() != null)
//                canvasObjects.Add(obj.name);
//            if (obj.GetComponent<Camera>() != null)
//                cameras.Add(obj.name);
//        }

//        sb.AppendLine($"- **Total GameObjects**: {allObjects.Count}");
//        sb.AppendLine($"- **Managers**: {string.Join(", ", managers)}");
//        sb.AppendLine($"- **Cameras**: {string.Join(", ", cameras)}");
//        sb.AppendLine($"- **Canvases**: {string.Join(", ", canvasObjects)}");

//        // Save to file
//        var outputDir = "Assets/Documentation/Scenes";
//        if (!Directory.Exists(outputDir))
//            Directory.CreateDirectory(outputDir);

//        var outputPath = $"{outputDir}/{scene.name}_Hierarchy.md";
//        File.WriteAllText(outputPath, sb.ToString());
//        AssetDatabase.Refresh();

//        Debug.Log($"Scene analysis saved to: {outputPath}");
//        if (revealInFinder)
//            EditorUtility.RevealInFinder(outputPath);
//    }

//    /// <summary>Collect all objects.</summary>
//    private static void CollectAllObjects(GameObject obj, List<GameObject> list, bool includeInactive)
//    {
//        if (!includeInactive && !obj.activeInHierarchy) return;
        
//        list.Add(obj);
//        foreach (Transform child in obj.transform)
//        {
//            CollectAllObjects(child.gameObject, list, includeInactive);
//        }
//    }

//    /// <summary>Analyze game object.</summary>
//    private static void AnalyzeGameObject(GameObject obj, int depth, StringBuilder sb,
//        bool includeInactive, bool includeTransforms, bool includeRenderers, 
//        bool includeUI, bool includeScripts, int maxDepth)
//    {
//        if (depth > maxDepth) return;
//        if (!includeInactive && !obj.activeInHierarchy) return;

//        var indent = new string(' ', depth * 2);
//        var activeMarker = obj.activeSelf ? "" : " [INACTIVE]";
//        var prefabMarker = PrefabUtility.IsPartOfPrefabInstance(obj) ? " (Prefab)" : "";

//        sb.AppendLine($"{indent}[{obj.name}]{activeMarker}{prefabMarker}");

//        // Components
//        var components = obj.GetComponents<Component>();
//        foreach (var comp in components)
//        {
//            if (comp == null) continue;
            
//            var compIndent = indent + "  ";
//            var compType = comp.GetType();

//            // Skip Transform unless requested
//            if (compType == typeof(Transform) || compType == typeof(RectTransform))
//            {
//                if (includeTransforms)
//                {
//                    var t = comp as Transform;
//                    if (t.localPosition != Vector3.zero || t.localScale != Vector3.one || t.localEulerAngles != Vector3.zero)
//                    {
//                        sb.AppendLine($"{compIndent}Transform: pos={FormatVector3(t.localPosition)}, scale={FormatVector3(t.localScale)}");
//                    }
//                }
//                continue;
//            }

//            // Renderers
//            if (includeRenderers)
//            {
//                if (comp is SpriteRenderer sr)
//                {
//                    var spriteName = sr.sprite != null ? sr.sprite.name : "null";
//                    sb.AppendLine($"{compIndent}SpriteRenderer: sprite={spriteName}, order={sr.sortingOrder}");
//                    continue;
//                }
//                if (comp is MeshRenderer mr)
//                {
//                    sb.AppendLine($"{compIndent}MeshRenderer: materials={mr.sharedMaterials.Length}");
//                    continue;
//                }
//            }

//            // UI Components
//            if (includeUI)
//            {
//                if (comp is Canvas canvas)
//                {
//                    sb.AppendLine($"{compIndent}Canvas: renderMode={canvas.renderMode}, sortOrder={canvas.sortingOrder}");
//                    continue;
//                }
//                if (comp is Image img)
//                {
//                    var spriteName = img.sprite != null ? img.sprite.name : "null";
//                    sb.AppendLine($"{compIndent}Image: sprite={spriteName}");
//                    continue;
//                }
//                if (comp is TextMeshProUGUI tmpUI)
//                {
//                    var preview = tmpUI.text?.Length > 30 ? tmpUI.text.Substring(0, 30) + "..." : tmpUI.text;
//                    sb.AppendLine($"{compIndent}TextMeshProUGUI: \"{preview}\"");
//                    continue;
//                }
//                if (comp is Button btn)
//                {
//                    sb.AppendLine($"{compIndent}Button: interactable={btn.interactable}");
//                    continue;
//                }
//            }

//            // Cameras
//            if (comp is Camera cam)
//            {
//                sb.AppendLine($"{compIndent}Camera: orthographic={cam.orthographic}, depth={cam.depth}");
//                continue;
//            }

//            // Audio
//            if (comp is AudioSource audio)
//            {
//                var clipName = audio.clip != null ? audio.clip.name : "null";
//                sb.AppendLine($"{compIndent}AudioSource: clip={clipName}");
//                continue;
//            }

//            // Sorting Group
//            if (comp is SortingGroup sg)
//            {
//                sb.AppendLine($"{compIndent}SortingGroup: layer={sg.sortingLayerName}, order={sg.sortingOrder}");
//                continue;
//            }

//            // Custom Scripts
//            if (includeScripts && !compType.Namespace?.StartsWith("UnityEngine") == true 
//                && !compType.Namespace?.StartsWith("TMPro") == true)
//            {
//                sb.AppendLine($"{compIndent}{compType.Name}");
//            }
//        }

//        // Recurse children
//        foreach (Transform child in obj.transform)
//        {
//            AnalyzeGameObject(child.gameObject, depth + 1, sb, includeInactive, 
//                includeTransforms, includeRenderers, includeUI, includeScripts, maxDepth);
//        }
//    }

//    /// <summary>Format vector3.</summary>
//    private static string FormatVector3(Vector3 v)
//    {
//        if (v == Vector3.zero) return "(0,0,0)";
//        if (v == Vector3.one) return "(1,1,1)";
//        return $"({v.x:F2},{v.y:F2},{v.z:F2})";
//    }
//}

///// <summary>
///// SCENEANALYZERAUTOSAVE - Automatically regenerates scene hierarchy
///// documentation whenever a scene is saved in the editor.
/////
///// PURPOSE:
///// Keeps Assets/Documentation/Scenes/ always up-to-date without manual
///// intervention. Hooks into EditorSceneManager.sceneSaved which fires
///// after every Ctrl+S or File → Save Scene.
/////
///// TOGGLE:
///// Menu: Tools → Auto-Generate Scene Docs (persists via EditorPrefs).
/////
///// RELATED FILES:
///// - SceneAnalyzer: The analysis engine this calls
///// - Assets/Documentation/Scenes/: Output directory
///// </summary>
//[InitializeOnLoad]
//public static class SceneAnalyzerAutoSave
//{
//    private const string PrefKey = "SceneAnalyzerAutoSave_Enabled";
//    private const string MenuPath = "Tools/Auto-Generate Scene Docs";

//    /// <summary>Subscribes to the scene-saved event on editor load.</summary>
//    static SceneAnalyzerAutoSave()
//    {
//        EditorSceneManager.sceneSaved -= OnSceneSaved;
//        EditorSceneManager.sceneSaved += OnSceneSaved;
//    }

//    /// <summary>Returns whether the auto-generation toggle is enabled.</summary>
//    private static bool IsEnabled
//    {
//        get => EditorPrefs.GetBool(PrefKey, true);
//        set => EditorPrefs.SetBool(PrefKey, value);
//    }

//    /// <summary>Menu toggle for enabling/disabling auto-generation.</summary>
//    [MenuItem(MenuPath, priority = 200)]
//    private static void ToggleAutoGenerate()
//    {
//        IsEnabled = !IsEnabled;
//        Debug.Log($"[SceneAnalyzerAutoSave] Auto-generate scene docs: {(IsEnabled ? "ON" : "OFF")}");
//    }

//    /// <summary>Adds a checkmark to the menu item when enabled.</summary>
//    [MenuItem(MenuPath, true)]
//    private static bool ToggleAutoGenerateValidate()
//    {
//        Menu.SetChecked(MenuPath, IsEnabled);
//        return true;
//    }

//    /// <summary>Called by Unity after a scene is saved. Regenerates the hierarchy markdown.</summary>
//    private static void OnSceneSaved(Scene scene)
//    {
//        if (!IsEnabled)
//            return;

//        if (string.IsNullOrEmpty(scene.path))
//            return;

//        SceneAnalyzer.AnalyzeScene(
//            scene,
//            includeInactive: true,
//            includeTransforms: true,
//            includeRenderers: true,
//            includeUI: true,
//            includeScripts: true,
//            maxDepth: 20,
//            revealInFinder: false
//        );

//        Debug.Log($"[SceneAnalyzerAutoSave] Regenerated: Assets/Documentation/Scenes/{scene.name}_Hierarchy.md");
//    }
//}
