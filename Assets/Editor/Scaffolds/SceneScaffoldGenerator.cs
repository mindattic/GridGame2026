using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using UnityEditor;
using UnityEngine;

/// <summary>
/// SCENESCAFFOLDGENERATOR - Reads a .unity scene file and generates a deep-clone
/// scaffold C# file that can recreate every GameObject, component, and property.
///
/// PURPOSE:
/// This is the "Save" half of the bidirectional scaffold system:
///   - Update Scaffold (Save): Scene → YAML → Generator → {Scene}Scaffold.cs
///   - Clear &amp; Recreate (Load): {Scene}Scaffold.cs → Editor → Scene
///
/// The generated scaffold overwrites the existing {Scene}Scaffold.cs file,
/// producing a complete deep-clone that captures every serialized property.
///
/// USAGE:
///   Tools › Scenes › {SceneName} › Update Scaffold
///   or: Tools › Scenes › Update Active Scene Scaffold
///
/// RELATED FILES:
/// - SceneScaffoldHelper.cs: Shared helpers (LoadSprite, LoadFont, WireOnClick)
/// - All *Scaffold.cs files: Generated output consumed by "Clear &amp; Recreate"
/// </summary>
public static class SceneScaffoldGenerator
{
    // ===================== Script GUID → Type Name =====================

    static readonly Dictionary<string, string> BuiltinScriptMap = new Dictionary<string, string>
    {
        { "fe87c0e1cc204ed48ad3b37840f39efc", "Image" },
        { "4e29b1a8efbd4b44bb3f3716e73f07ff", "Button" },
        { "f4688fdb7df04437aeb418b961361dc5", "TextMeshProUGUI" },
        { "31a19414c41e5ae4aae2af33fee712f6", "ScrollRect" },
        { "1aa08ab6e0800fa44ae55d278d1423e3", "Mask" },
        { "2a4db7a114972834c8e4117be1d82ba3", "Scrollbar" },
        { "dc42784cf147c0c48a680349fa168899", "CanvasScaler" },
        { "0cd44c1031e13a943bb63640046fad76", "GraphicRaycaster" },
        { "59f8146938fff824cb5fd77236b75775", "VerticalLayoutGroup" },
        { "3245ec927659c4140ac4f8d17403cc18", "ContentSizeFitter" },
        { "4f231c4fb786f3946a6b90b886c48677", "EventSystem" },
        { "76c392e42b5098c458856cdf6ecaaaa1", "StandaloneInputModule" },
        { "30649d3a9faa99c48a7b1166b86bf2a0", "HorizontalLayoutGroup" },
    };

    // ===================== Data Structures =====================

    class GOData
    {
        public string FileID;
        public string Name;
        public int Layer;
        public bool IsActive;
        public List<string> ComponentFileIDs = new List<string>();
        public string ParentGoFileID;
        public List<string> ChildGoFileIDs = new List<string>();
    }

    class RTData
    {
        public float AncMinX, AncMinY, AncMaxX, AncMaxY;
        public float PosX, PosY, SzX, SzY, PvtX, PvtY;
    }

    class ComponentData
    {
        public string FileID;
        public string GoFileID;
        public string TypeID;
        public string ScriptGUID;
        public string ScriptName;
        public string RawYAML;
    }

    class OnClickData
    {
        public string ButtonGoFileID;
        public string TargetTypeName;
        public string MethodName;
        public string TargetGoFileID;
    }

    // ===================== Per-Scene Menu Items (Save) =====================

    [MenuItem("Tools/Scenes/Credits/Save")]
    static void Save_Credits() => GenerateForScene("Credits");
    [MenuItem("Tools/Scenes/Title Screen/Save")]
    static void Save_TitleScreen() => GenerateForScene("TitleScreen");
    [MenuItem("Tools/Scenes/Profile Select/Save")]
    static void Save_ProfileSelect() => GenerateForScene("ProfileSelect");
    [MenuItem("Tools/Scenes/Profile Create/Save")]
    static void Save_ProfileCreate() => GenerateForScene("ProfileCreate");
    [MenuItem("Tools/Scenes/Save File Select/Save")]
    static void Save_SaveFileSelect() => GenerateForScene("SaveFileSelect");
    [MenuItem("Tools/Scenes/Stage Select/Save")]
    static void Save_StageSelect() => GenerateForScene("StageSelect");
    [MenuItem("Tools/Scenes/Loading Screen/Save")]
    static void Save_LoadingScreen() => GenerateForScene("LoadingScreen");
    [MenuItem("Tools/Scenes/Settings/Save")]
    static void Save_Settings() => GenerateForScene("Settings");
    [MenuItem("Tools/Scenes/Hub/Save")]
    static void Save_Hub() => GenerateForScene("Hub");
    [MenuItem("Tools/Scenes/Post Battle Screen/Save")]
    static void Save_PostBattleScreen() => GenerateForScene("PostBattleScreen");
    [MenuItem("Tools/Scenes/Party Manager/Save")]
    static void Save_PartyManager() => GenerateForScene("PartyManager");
    [MenuItem("Tools/Scenes/Splash Screen/Save")]
    static void Save_SplashScreen() => GenerateForScene("SplashScreen");

    // ===================== Main Entry =====================

    static void GenerateForScene(string sceneName)
    {
        var scenePath = $"Assets/Scenes/{sceneName}.unity";
        var fullPath = Path.Combine(Application.dataPath, "..", scenePath);
        if (!File.Exists(fullPath))
        {
            EditorUtility.DisplayDialog("Save",
                $"Scene file not found:\n{scenePath}\n\nSave the scene first.", "OK");
            return;
        }

        if (!EditorUtility.DisplayDialog("Save",
            $"Overwrite {sceneName}Scaffold.cs with the current scene state?\n\n" +
            $"This will replace the existing scaffold file.",
            "Save", "Cancel"))
            return;

        var text = File.ReadAllText(fullPath);

        // Parse
        var allBlocks = ParseAllBlocks(text);
        var gameObjects = ParseGameObjects(text);
        var rtDataMap = new Dictionary<string, RTData>();
        BuildHierarchy(text, gameObjects, rtDataMap);
        var components = ParseComponents(text, allBlocks);
        ResolveScriptNames(components, fullPath);

        // Parse onClick events
        var onClicks = ParseOnClicks(text, components, gameObjects);

        // Find roots
        var roots = gameObjects.Values
            .Where(g => g.ParentGoFileID == null)
            .OrderBy(g => g.Name)
            .ToList();

        // Build variable name map (clean names with collision counter)
        var varNames = BuildVarNames(gameObjects);

        // Collect custom using namespaces from resolved script names
        var customNamespaces = CollectNamespaces(components, fullPath);

        // Generate code
        var code = GenerateCode(sceneName, roots, gameObjects, rtDataMap,
            components, onClicks, varNames, customNamespaces);

        // Write output — overwrites the hand-written scaffold
        var outputPath = $"Assets/Editor/Scaffolds/{sceneName}Scaffold.cs";
        File.WriteAllText(outputPath, code);
        AssetDatabase.Refresh();

        Debug.Log($"[SceneScaffoldGenerator] Saved {outputPath} " +
            $"({gameObjects.Count} GOs, {components.Count} components, {onClicks.Count} onClick events)");
        EditorUtility.DisplayDialog("Save",
            $"Scaffold saved for '{sceneName}'.\n\n" +
            $"GameObjects: {gameObjects.Count}\n" +
            $"Components: {components.Count}\n" +
            $"onClick events: {onClicks.Count}\n\n" +
            $"Output: {outputPath}",
            "OK");
    }

    // ===================== Variable Name Builder =====================

    /// <summary>Builds a map of GO fileID → clean C# variable name with collision handling.</summary>
    static Dictionary<string, string> BuildVarNames(Dictionary<string, GOData> gameObjects)
    {
        var result = new Dictionary<string, string>();
        var usedNames = new Dictionary<string, int>();
        foreach (var go in gameObjects.Values)
        {
            var baseName = SanitizeId(go.Name);
            if (usedNames.ContainsKey(baseName))
            {
                usedNames[baseName]++;
                result[go.FileID] = baseName + usedNames[baseName];
            }
            else
            {
                usedNames[baseName] = 1;
                result[go.FileID] = baseName;
            }
        }
        return result;
    }

    // ===================== Namespace Collection =====================

    static HashSet<string> CollectNamespaces(List<ComponentData> components, string scenePath)
    {
        var namespaces = new HashSet<string>();
        var assetsDir = Path.Combine(Path.GetDirectoryName(scenePath), "..", "Assets", "Scripts");
        if (!Directory.Exists(assetsDir)) return namespaces;

        foreach (var cd in components)
        {
            if (cd.ScriptName == null || BuiltinScriptMap.ContainsValue(cd.ScriptName)) continue;
            // Search for the script file to find its namespace
            foreach (var csFile in Directory.GetFiles(assetsDir, $"{cd.ScriptName}.cs", SearchOption.AllDirectories))
            {
                try
                {
                    var code = File.ReadAllText(csFile);
                    var nsMatch = Regex.Match(code, @"namespace\s+([\w.]+)");
                    if (nsMatch.Success)
                        namespaces.Add(nsMatch.Groups[1].Value);
                }
                catch { }
                break;
            }
        }
        return namespaces;
    }

    // ===================== onClick Parsing =====================

    static List<OnClickData> ParseOnClicks(string text, List<ComponentData> components,
        Dictionary<string, GOData> gameObjects)
    {
        var result = new List<OnClickData>();

        // Map component fileID → GO fileID
        var compToGo = new Dictionary<string, string>();
        foreach (var cd in components)
            if (cd.GoFileID != null)
                compToGo[cd.FileID] = cd.GoFileID;

        // Find button components with onClick
        foreach (var cd in components)
        {
            if (cd.ScriptName != "Button") continue;
            var yaml = cd.RawYAML;

            var methodMatch = Regex.Match(yaml, @"m_MethodName:\s+(\S+)");
            var targetMatch = Regex.Match(yaml, @"m_TargetAssemblyTypeName:\s+(\w+),");
            var targetFileIDMatch = Regex.Match(yaml, @"m_Target:\s*\{fileID:\s*(\d+)");
            var callStateMatch = Regex.Match(yaml, @"m_CallState:\s*(\d+)");

            if (!methodMatch.Success || !targetMatch.Success || !targetFileIDMatch.Success) continue;
            if (callStateMatch.Success && callStateMatch.Groups[1].Value == "0") continue; // Off

            var targetCompFileID = targetFileIDMatch.Groups[1].Value;
            string targetGoFileID = null;
            if (compToGo.ContainsKey(targetCompFileID))
                targetGoFileID = compToGo[targetCompFileID];

            result.Add(new OnClickData
            {
                ButtonGoFileID = cd.GoFileID,
                TargetTypeName = targetMatch.Groups[1].Value,
                MethodName = methodMatch.Groups[1].Value,
                TargetGoFileID = targetGoFileID
            });
        }
        return result;
    }

    // ===================== Code Generation =====================

    static string GenerateCode(string sceneName, List<GOData> roots,
        Dictionary<string, GOData> allGOs, Dictionary<string, RTData> rtDataMap,
        List<ComponentData> components, List<OnClickData> onClicks,
        Dictionary<string, string> varNames, HashSet<string> customNamespaces)
    {
        // Build component lookup: goFileID → ordered list
        var compsByGO = new Dictionary<string, List<ComponentData>>();
        foreach (var cd in components)
        {
            if (cd.GoFileID == null) continue;
            if (!compsByGO.ContainsKey(cd.GoFileID))
                compsByGO[cd.GoFileID] = new List<ComponentData>();
            compsByGO[cd.GoFileID].Add(cd);
        }

        // Sort components per GO by their YAML order
        foreach (var goId in compsByGO.Keys.ToList())
        {
            if (allGOs.ContainsKey(goId))
            {
                var order = allGOs[goId].ComponentFileIDs;
                compsByGO[goId] = compsByGO[goId]
                    .OrderBy(c => { var idx = order.IndexOf(c.FileID); return idx >= 0 ? idx : 999; })
                    .ToList();
            }
        }

        var spriteMap = BuildSpriteMap(components);
        var fontMap = BuildFontMap(components);

        var sb = new StringBuilder();

        // Header
        sb.AppendLine("// =============================================================================");
        sb.AppendLine($"// AUTO-GENERATED by SceneScaffoldGenerator from {sceneName}.unity");
        sb.AppendLine($"// Generated: {DateTime.Now:yyyy-MM-dd HH:mm:ss}");
        sb.AppendLine("//");
        sb.AppendLine("// Save:  Tools > Scenes > {SceneName} > Save   (scene → scaffold)");
        sb.AppendLine("// Load:  Tools > Scenes > {SceneName} > Load   (scaffold → scene)");
        sb.AppendLine("// =============================================================================");
        sb.AppendLine();
        sb.AppendLine("using UnityEngine;");
        sb.AppendLine("using UnityEngine.UI;");
        sb.AppendLine("using UnityEngine.Events;");
        sb.AppendLine("using UnityEngine.EventSystems;");
        sb.AppendLine("using UnityEditor;");
        sb.AppendLine("using TMPro;");
        sb.AppendLine();
        foreach (var ns in customNamespaces.OrderBy(n => n))
            sb.AppendLine($"using {ns};");
        if (customNamespaces.Count > 0) sb.AppendLine();

        sb.AppendLine($"public static class {sceneName}Scaffold");
        sb.AppendLine("{");
        sb.AppendLine($"    private const string SceneName = \"{sceneName}\";");
        sb.AppendLine();

        // Sprite/font constants
        if (spriteMap.Count > 0)
        {
            sb.AppendLine("    // Sprite asset paths");
            foreach (var kvp in spriteMap.OrderBy(k => k.Value))
                sb.AppendLine($"    private const string Sprite_{SanitizeId(Path.GetFileNameWithoutExtension(kvp.Value))} = \"{kvp.Value}\";");
            sb.AppendLine();
        }
        if (fontMap.Count > 0)
        {
            sb.AppendLine("    // Font asset paths");
            foreach (var kvp in fontMap.OrderBy(k => k.Value))
                sb.AppendLine($"    private const string Font_{SanitizeId(Path.GetFileNameWithoutExtension(kvp.Value))} = \"{kvp.Value}\";");
            sb.AppendLine();
        }

        // Menu item names use display names (spaces, not scene names)
        var menuPath = GetMenuPath(sceneName);

        // Load
        sb.AppendLine($"    [MenuItem(\"Tools/Scenes/{menuPath}/Load\")]");
        sb.AppendLine("    public static void Load()");
        sb.AppendLine("    {");
        sb.AppendLine("        if (!EditorUtility.DisplayDialog(\"Load\",");
        sb.AppendLine($"            \"Clear the {sceneName} scene and recreate all GameObjects from the scaffold?\\n\\n\" +");
        sb.AppendLine("            \"Any unsaved scene changes will be lost.\",");
        sb.AppendLine("            \"Load\", \"Cancel\"))");
        sb.AppendLine("            return;");
        sb.AppendLine("        if (!SceneScaffoldHelper.OpenScene(SceneName)) return;");
        sb.AppendLine("        SceneScaffoldHelper.ClearAllRootObjectsSilent();");
        sb.AppendLine("        CreateScaffolding();");
        sb.AppendLine("    }");
        sb.AppendLine();

        // ClearScene
        sb.AppendLine("    public static void ClearScene()");
        sb.AppendLine("    {");
        sb.AppendLine("        if (!SceneScaffoldHelper.OpenScene(SceneName)) return;");
        sb.AppendLine("        SceneScaffoldHelper.ClearAllRootObjects();");
        sb.AppendLine("    }");
        sb.AppendLine();

        // CreateScaffolding
        sb.AppendLine("    public static void CreateScaffolding()");
        sb.AppendLine("    {");
        sb.AppendLine("        if (!SceneScaffoldHelper.OpenScene(SceneName)) return;");
        sb.AppendLine();

        // Generate each root and subtree
        foreach (var root in roots)
        {
            GenerateGO(sb, root, allGOs, rtDataMap, compsByGO, spriteMap, fontMap, varNames, "        ", null);
            sb.AppendLine();
        }

        // Post-pass: wire ScrollRect cross-references
        GenerateScrollRectWiring(sb, allGOs, compsByGO, varNames, "        ");

        // Post-pass: wire Scrollbar handleRect + targetGraphic
        GenerateScrollbarWiring(sb, allGOs, compsByGO, varNames, "        ");

        // Post-pass: wire onClick events
        if (onClicks.Count > 0)
        {
            sb.AppendLine("        // --- onClick event wiring ---");
            foreach (var oc in onClicks)
            {
                if (oc.ButtonGoFileID == null || oc.TargetGoFileID == null) continue;
                if (!varNames.ContainsKey(oc.ButtonGoFileID) || !varNames.ContainsKey(oc.TargetGoFileID)) continue;

                var btnVar = varNames[oc.ButtonGoFileID];
                var targetVar = varNames[oc.TargetGoFileID];
                sb.AppendLine($"        SceneScaffoldHelper.WireOnClick(");
                sb.AppendLine($"            go_{btnVar}.GetComponent<Button>(),");
                sb.AppendLine($"            new UnityAction(go_{targetVar}.GetComponent<{oc.TargetTypeName}>().{oc.MethodName}));");
            }
            sb.AppendLine();
        }

        sb.AppendLine("        UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(");
        sb.AppendLine("            UnityEditor.SceneManagement.EditorSceneManager.GetActiveScene());");
        sb.AppendLine("    }");
        sb.AppendLine("}");

        return sb.ToString();
    }

    // ===================== GO Generation =====================

    static void GenerateGO(StringBuilder sb, GOData go, Dictionary<string, GOData> allGOs,
        Dictionary<string, RTData> rtDataMap, Dictionary<string, List<ComponentData>> compsByGO,
        Dictionary<string, string> spriteMap, Dictionary<string, string> fontMap,
        Dictionary<string, string> varNames, string indent, string parentVar)
    {
        var varName = varNames[go.FileID];
        var goVar = $"go_{varName}";

        sb.AppendLine($"{indent}// --- {go.Name} ---");
        sb.AppendLine($"{indent}var {goVar} = new GameObject(\"{EscapeString(go.Name)}\");");

        if (go.Layer != 0)
            sb.AppendLine($"{indent}{goVar}.layer = {go.Layer};");

        if (parentVar != null)
        {
            if (rtDataMap.ContainsKey(go.FileID))
            {
                var rt = rtDataMap[go.FileID];
                sb.AppendLine($"{indent}var rt_{varName} = {goVar}.AddComponent<RectTransform>();");
                sb.AppendLine($"{indent}rt_{varName}.SetParent({parentVar}.GetComponent<RectTransform>(), false);");
                sb.AppendLine($"{indent}rt_{varName}.anchorMin = new Vector2({F(rt.AncMinX)}, {F(rt.AncMinY)});");
                sb.AppendLine($"{indent}rt_{varName}.anchorMax = new Vector2({F(rt.AncMaxX)}, {F(rt.AncMaxY)});");
                sb.AppendLine($"{indent}rt_{varName}.pivot = new Vector2({F(rt.PvtX)}, {F(rt.PvtY)});");
                sb.AppendLine($"{indent}rt_{varName}.sizeDelta = new Vector2({F(rt.SzX)}, {F(rt.SzY)});");
                sb.AppendLine($"{indent}rt_{varName}.anchoredPosition = new Vector2({F(rt.PosX)}, {F(rt.PosY)});");
            }
            else
            {
                sb.AppendLine($"{indent}{goVar}.transform.SetParent({parentVar}.transform, false);");
            }
        }
        else if (rtDataMap.ContainsKey(go.FileID))
        {
            sb.AppendLine($"{indent}{goVar}.AddComponent<RectTransform>();");
        }

        // Components in YAML order
        if (compsByGO.ContainsKey(go.FileID))
        {
            foreach (var cd in compsByGO[go.FileID])
                GenerateComponent(sb, cd, goVar, varName, spriteMap, fontMap, indent);
        }

        if (!go.IsActive)
            sb.AppendLine($"{indent}{goVar}.SetActive(false);");

        sb.AppendLine($"{indent}Undo.RegisterCreatedObjectUndo({goVar}, \"Create {EscapeString(go.Name)}\");");

        // Children
        foreach (var childId in go.ChildGoFileIDs)
        {
            if (allGOs.ContainsKey(childId))
            {
                sb.AppendLine();
                GenerateGO(sb, allGOs[childId], allGOs, rtDataMap, compsByGO, spriteMap, fontMap, varNames, indent, goVar);
            }
        }
    }

    // ===================== Cross-Reference Wiring =====================

    static void GenerateScrollRectWiring(StringBuilder sb, Dictionary<string, GOData> allGOs,
        Dictionary<string, List<ComponentData>> compsByGO, Dictionary<string, string> varNames, string indent)
    {
        bool headerWritten = false;
        foreach (var kvp in compsByGO)
        {
            foreach (var cd in kvp.Value)
            {
                if (cd.ScriptName != "ScrollRect") continue;

                if (!headerWritten) { sb.AppendLine($"{indent}// --- ScrollRect cross-references ---"); headerWritten = true; }

                var goName = varNames.ContainsKey(cd.GoFileID) ? varNames[cd.GoFileID] : null;
                if (goName == null) continue;

                var vpFileID = ExtractFileID(cd.RawYAML, "m_Viewport");
                var contentFileID = ExtractFileID(cd.RawYAML, "m_Content");
                var vBarFileID = ExtractFileID(cd.RawYAML, "m_VerticalScrollbar");
                var hBarFileID = ExtractFileID(cd.RawYAML, "m_HorizontalScrollbar");

                var srVar = $"go_{goName}.GetComponent<ScrollRect>()";

                // Map component fileIDs to GO fileIDs via RectTransform
                if (vpFileID != null) EmitRTCrossRef(sb, srVar, "viewport", vpFileID, allGOs, varNames, indent);
                if (contentFileID != null) EmitRTCrossRef(sb, srVar, "content", contentFileID, allGOs, varNames, indent);

                // Scrollbar refs point to MonoBehaviour component fileIDs
                if (vBarFileID != null) EmitCompCrossRef(sb, srVar, "verticalScrollbar", vBarFileID, "Scrollbar", compsByGO, varNames, indent);
                if (hBarFileID != null) EmitCompCrossRef(sb, srVar, "horizontalScrollbar", hBarFileID, "Scrollbar", compsByGO, varNames, indent);

                var vVis = ExtractInt(cd.RawYAML, "m_VerticalScrollbarVisibility", 0);
                var hVis = ExtractInt(cd.RawYAML, "m_HorizontalScrollbarVisibility", 0);
                if (vVis != 0) sb.AppendLine($"{indent}{srVar}.verticalScrollbarVisibility = (ScrollRect.ScrollbarVisibility){vVis};");
                if (hVis != 0) sb.AppendLine($"{indent}{srVar}.horizontalScrollbarVisibility = (ScrollRect.ScrollbarVisibility){hVis};");
            }
        }
        if (headerWritten) sb.AppendLine();
    }

    static void GenerateScrollbarWiring(StringBuilder sb, Dictionary<string, GOData> allGOs,
        Dictionary<string, List<ComponentData>> compsByGO, Dictionary<string, string> varNames, string indent)
    {
        bool headerWritten = false;
        foreach (var kvp in compsByGO)
        {
            foreach (var cd in kvp.Value)
            {
                if (cd.ScriptName != "Scrollbar") continue;

                var goName = varNames.ContainsKey(cd.GoFileID) ? varNames[cd.GoFileID] : null;
                if (goName == null) continue;

                var handleFileID = ExtractFileID(cd.RawYAML, "m_HandleRect");
                if (handleFileID == null) continue;

                if (!headerWritten) { sb.AppendLine($"{indent}// --- Scrollbar handle wiring ---"); headerWritten = true; }

                var sbVar = $"go_{goName}.GetComponent<Scrollbar>()";
                EmitRTCrossRef(sb, sbVar, "handleRect", handleFileID, allGOs, varNames, indent);

                // targetGraphic is the handle's Image
                var handleGoFileID = FindGoForRT(handleFileID, allGOs);
                if (handleGoFileID != null && varNames.ContainsKey(handleGoFileID))
                    sb.AppendLine($"{indent}{sbVar}.targetGraphic = go_{varNames[handleGoFileID]}.GetComponent<Image>();");
            }
        }
        if (headerWritten) sb.AppendLine();
    }

    static void EmitRTCrossRef(StringBuilder sb, string srVar, string property, string rtFileID,
        Dictionary<string, GOData> allGOs, Dictionary<string, string> varNames, string indent)
    {
        var goFileID = FindGoForRT(rtFileID, allGOs);
        if (goFileID != null && varNames.ContainsKey(goFileID))
            sb.AppendLine($"{indent}{srVar}.{property} = go_{varNames[goFileID]}.GetComponent<RectTransform>();");
    }

    static void EmitCompCrossRef(StringBuilder sb, string srVar, string property, string compFileID,
        string compType, Dictionary<string, List<ComponentData>> compsByGO,
        Dictionary<string, string> varNames, string indent)
    {
        // Find the GO that owns this component fileID
        foreach (var kvp in compsByGO)
        {
            foreach (var cd in kvp.Value)
            {
                if (cd.FileID == compFileID && varNames.ContainsKey(kvp.Key))
                {
                    sb.AppendLine($"{indent}{srVar}.{property} = go_{varNames[kvp.Key]}.GetComponent<{compType}>();");
                    return;
                }
            }
        }
    }

    // ===================== Component Generation =====================

    static void GenerateComponent(StringBuilder sb, ComponentData cd, string goVar, string varName,
        Dictionary<string, string> spriteMap, Dictionary<string, string> fontMap, string indent)
    {
        switch (cd.ScriptName)
        {
            case "Canvas":         GenerateCanvas(sb, cd, goVar, indent); break;
            case "CanvasScaler":   GenerateCanvasScaler(sb, cd, goVar, indent); break;
            case "CanvasRenderer": sb.AppendLine($"{indent}{goVar}.AddComponent<CanvasRenderer>();"); break;
            case "GraphicRaycaster": sb.AppendLine($"{indent}{goVar}.AddComponent<GraphicRaycaster>();"); break;
            case "CanvasGroup":    GenerateCanvasGroup(sb, cd, goVar, indent); break;
            case "Image":          GenerateImage(sb, cd, goVar, varName, spriteMap, indent); break;
            case "Button":         GenerateButton(sb, cd, goVar, varName, indent); break;
            case "TextMeshProUGUI": GenerateTMP(sb, cd, goVar, varName, fontMap, indent); break;
            case "Scrollbar":      GenerateScrollbar(sb, cd, goVar, varName, indent); break;
            case "ScrollRect":     sb.AppendLine($"{indent}{goVar}.AddComponent<ScrollRect>();"); break;
            case "Mask":           GenerateMask(sb, cd, goVar, indent); break;
            case "VerticalLayoutGroup":   GenerateLayoutGroup(sb, cd, goVar, indent, "VerticalLayoutGroup", "vlg"); break;
            case "HorizontalLayoutGroup": GenerateLayoutGroup(sb, cd, goVar, indent, "HorizontalLayoutGroup", "hlg"); break;
            case "ContentSizeFitter":     GenerateCSF(sb, cd, goVar, indent); break;
            case "Camera":         GenerateCamera(sb, cd, goVar, indent); break;
            case "AudioListener":  sb.AppendLine($"{indent}{goVar}.AddComponent<AudioListener>();"); break;
            case "EventSystem":    sb.AppendLine($"{indent}{goVar}.AddComponent<EventSystem>();"); break;
            case "StandaloneInputModule": sb.AppendLine($"{indent}{goVar}.AddComponent<StandaloneInputModule>();"); break;
            default:
                sb.AppendLine($"{indent}{goVar}.AddComponent<{cd.ScriptName}>();");
                break;
        }
    }

    static void GenerateCanvas(StringBuilder sb, ComponentData cd, string goVar, string indent)
    {
        var renderMode = ExtractInt(cd.RawYAML, "m_RenderMode", 0);
        var sortOrder = ExtractInt(cd.RawYAML, "m_SortingOrder", 0);
        sb.AppendLine($"{indent}var canvas = {goVar}.AddComponent<Canvas>();");
        sb.AppendLine($"{indent}canvas.renderMode = (RenderMode){renderMode};");
        if (sortOrder != 0)
            sb.AppendLine($"{indent}canvas.sortingOrder = {sortOrder};");
    }

    static void GenerateCanvasScaler(StringBuilder sb, ComponentData cd, string goVar, string indent)
    {
        var mode = ExtractInt(cd.RawYAML, "m_UiScaleMode", 0);
        ParseVector2(cd.RawYAML, "m_ReferenceResolution", out var rx, out var ry);
        var match = ExtractFloat(cd.RawYAML, "m_MatchWidthOrHeight", 0f);
        sb.AppendLine($"{indent}var scaler = {goVar}.AddComponent<CanvasScaler>();");
        sb.AppendLine($"{indent}scaler.uiScaleMode = (CanvasScaler.ScaleMode){mode};");
        sb.AppendLine($"{indent}scaler.referenceResolution = new Vector2({F(rx)}, {F(ry)});");
        sb.AppendLine($"{indent}scaler.matchWidthOrHeight = {F(match)};");
    }

    static void GenerateCanvasGroup(StringBuilder sb, ComponentData cd, string goVar, string indent)
    {
        var alpha = ExtractFloat(cd.RawYAML, "m_Alpha", 1f);
        var interactable = ExtractInt(cd.RawYAML, "m_Interactable", 1);
        var blocksRaycasts = ExtractInt(cd.RawYAML, "m_BlocksRaycasts", 1);
        sb.AppendLine($"{indent}var canvasGroup = {goVar}.AddComponent<CanvasGroup>();");
        if (alpha != 1f) sb.AppendLine($"{indent}canvasGroup.alpha = {F(alpha)};");
        if (interactable != 1) sb.AppendLine($"{indent}canvasGroup.interactable = false;");
        if (blocksRaycasts != 1) sb.AppendLine($"{indent}canvasGroup.blocksRaycasts = false;");
    }

    static void GenerateImage(StringBuilder sb, ComponentData cd, string goVar, string varName,
        Dictionary<string, string> spriteMap, string indent)
    {
        var imgVar = $"img_{varName}";
        sb.AppendLine($"{indent}var {imgVar} = {goVar}.AddComponent<Image>();");

        var spriteGuid = ExtractSpriteGUID(cd.RawYAML);
        if (spriteGuid != null && spriteMap.ContainsKey(spriteGuid))
        {
            var constName = $"Sprite_{SanitizeId(Path.GetFileNameWithoutExtension(spriteMap[spriteGuid]))}";
            sb.AppendLine($"{indent}{imgVar}.sprite = SceneScaffoldHelper.LoadSprite({constName});");
        }
        else if (spriteGuid != null && spriteGuid.StartsWith("0000000000000000"))
        {
            sb.AppendLine($"{indent}{imgVar}.sprite = SceneScaffoldHelper.LoadBuiltinSprite();");
        }

        ExtractColor(cd.RawYAML, "m_Color", out var cr, out var cg, out var cb, out var ca);
        sb.AppendLine($"{indent}{imgVar}.color = new Color({F(cr)}, {F(cg)}, {F(cb)}, {F(ca)});");

        var imgType = ExtractInt(cd.RawYAML, "m_Type", 0);
        if (imgType != 0)
            sb.AppendLine($"{indent}{imgVar}.type = (Image.Type){imgType};");

        var raycast = ExtractInt(cd.RawYAML, "m_RaycastTarget", 1);
        sb.AppendLine($"{indent}{imgVar}.raycastTarget = {(raycast == 1 ? "true" : "false")};");
    }

    static void GenerateButton(StringBuilder sb, ComponentData cd, string goVar, string varName, string indent)
    {
        sb.AppendLine($"{indent}var btn_{varName} = {goVar}.AddComponent<Button>();");
        var navMatch = Regex.Match(cd.RawYAML, @"m_Navigation:\s*\r?\n\s*m_Mode:\s*(\d+)");
        var navMode = navMatch.Success ? int.Parse(navMatch.Groups[1].Value) : 3;
        sb.AppendLine($"{indent}btn_{varName}.navigation = new Navigation {{ mode = (Navigation.Mode){navMode} }};");
        sb.AppendLine($"{indent}btn_{varName}.targetGraphic = {goVar}.GetComponent<Image>();");
    }

    static void GenerateTMP(StringBuilder sb, ComponentData cd, string goVar, string varName,
        Dictionary<string, string> fontMap, string indent)
    {
        var tmpVar = $"tmp_{varName}";
        sb.AppendLine($"{indent}var {tmpVar} = {goVar}.AddComponent<TextMeshProUGUI>();");

        var fontGuid = ExtractFontGUID(cd.RawYAML);
        if (fontGuid != null && fontMap.ContainsKey(fontGuid))
        {
            var constName = $"Font_{SanitizeId(Path.GetFileNameWithoutExtension(fontMap[fontGuid]))}";
            sb.AppendLine($"{indent}{tmpVar}.font = SceneScaffoldHelper.LoadFont({constName});");
        }

        var textContent = ExtractString(cd.RawYAML, "m_text");
        sb.AppendLine($"{indent}{tmpVar}.text = \"{EscapeString(textContent)}\";");

        var fontSize = ExtractFloat(cd.RawYAML, "m_fontSize", 24f);
        sb.AppendLine($"{indent}{tmpVar}.fontSize = {F(fontSize)};");

        ExtractColor(cd.RawYAML, "m_fontColor", out var cr, out var cg, out var cb, out var ca);
        sb.AppendLine($"{indent}{tmpVar}.color = new Color({F(cr)}, {F(cg)}, {F(cb)}, {F(ca)});");

        var hAlign = ExtractInt(cd.RawYAML, "m_HorizontalAlignment", 1);
        var vAlign = ExtractInt(cd.RawYAML, "m_VerticalAlignment", 256);
        sb.AppendLine($"{indent}{tmpVar}.alignment = (TextAlignmentOptions){hAlign + vAlign};");

        var wrapMode = ExtractInt(cd.RawYAML, "m_TextWrappingMode", 0);
        sb.AppendLine($"{indent}{tmpVar}.enableWordWrapping = {(wrapMode == 1 ? "true" : "false")};");

        var raycast = ExtractInt(cd.RawYAML, "m_RaycastTarget", 1);
        sb.AppendLine($"{indent}{tmpVar}.raycastTarget = {(raycast == 1 ? "true" : "false")};");
    }

    static void GenerateScrollbar(StringBuilder sb, ComponentData cd, string goVar, string varName, string indent)
    {
        var dir = ExtractInt(cd.RawYAML, "m_Direction", 0);
        sb.AppendLine($"{indent}var scrollbar_{varName} = {goVar}.AddComponent<Scrollbar>();");
        sb.AppendLine($"{indent}scrollbar_{varName}.direction = (Scrollbar.Direction){dir};");
    }

    static void GenerateMask(StringBuilder sb, ComponentData cd, string goVar, string indent)
    {
        var showGraphic = ExtractInt(cd.RawYAML, "m_ShowMaskGraphic", 1);
        sb.AppendLine($"{indent}var mask = {goVar}.AddComponent<Mask>();");
        sb.AppendLine($"{indent}mask.showMaskGraphic = {(showGraphic == 1 ? "true" : "false")};");
    }

    static void GenerateLayoutGroup(StringBuilder sb, ComponentData cd, string goVar, string indent,
        string typeName, string varPrefix)
    {
        var spacing = ExtractFloat(cd.RawYAML, "m_Spacing", 0f);
        var childAlign = ExtractInt(cd.RawYAML, "m_ChildAlignment", 0);
        var ctrlW = ExtractInt(cd.RawYAML, "m_ChildControlWidth", 0) == 1;
        var ctrlH = ExtractInt(cd.RawYAML, "m_ChildControlHeight", 0) == 1;
        var forceW = ExtractInt(cd.RawYAML, "m_ChildForceExpandWidth", 1) == 1;
        var forceH = ExtractInt(cd.RawYAML, "m_ChildForceExpandHeight", 1) == 1;

        sb.AppendLine($"{indent}var {varPrefix} = {goVar}.AddComponent<{typeName}>();");
        sb.AppendLine($"{indent}{varPrefix}.spacing = {F(spacing)};");
        sb.AppendLine($"{indent}{varPrefix}.childAlignment = (TextAnchor){childAlign};");
        sb.AppendLine($"{indent}{varPrefix}.childControlWidth = {(ctrlW ? "true" : "false")};");
        sb.AppendLine($"{indent}{varPrefix}.childControlHeight = {(ctrlH ? "true" : "false")};");
        sb.AppendLine($"{indent}{varPrefix}.childForceExpandWidth = {(forceW ? "true" : "false")};");
        sb.AppendLine($"{indent}{varPrefix}.childForceExpandHeight = {(forceH ? "true" : "false")};");
    }

    static void GenerateCSF(StringBuilder sb, ComponentData cd, string goVar, string indent)
    {
        var vFit = ExtractInt(cd.RawYAML, "m_VerticalFit", 0);
        var hFit = ExtractInt(cd.RawYAML, "m_HorizontalFit", 0);
        sb.AppendLine($"{indent}var csf = {goVar}.AddComponent<ContentSizeFitter>();");
        if (vFit != 0) sb.AppendLine($"{indent}csf.verticalFit = (ContentSizeFitter.FitMode){vFit};");
        if (hFit != 0) sb.AppendLine($"{indent}csf.horizontalFit = (ContentSizeFitter.FitMode){hFit};");
    }

    static void GenerateCamera(StringBuilder sb, ComponentData cd, string goVar, string indent)
    {
        var ortho = ExtractInt(cd.RawYAML, "orthographic", 0) == 1;
        var orthoSize = ExtractFloat(cd.RawYAML, "orthographic size", 5f);
        var depth = ExtractFloat(cd.RawYAML, "m_Depth", 0f);
        var clearFlags = ExtractInt(cd.RawYAML, "m_ClearFlags", 1);
        ExtractColor(cd.RawYAML, "m_BackGroundColor", out var cr, out var cg, out var cb, out var ca);

        sb.AppendLine($"{indent}var cam = {goVar}.AddComponent<Camera>();");
        sb.AppendLine($"{indent}cam.orthographic = {(ortho ? "true" : "false")};");
        sb.AppendLine($"{indent}cam.orthographicSize = {F(orthoSize)};");
        sb.AppendLine($"{indent}cam.depth = {F(depth)};");
        sb.AppendLine($"{indent}cam.clearFlags = (CameraClearFlags){clearFlags};");
        sb.AppendLine($"{indent}cam.backgroundColor = new Color({F(cr)}, {F(cg)}, {F(cb)}, {F(ca)});");
    }

    // ===================== Parsing =====================

    static Dictionary<string, string> ParseAllBlocks(string text)
    {
        var blocks = new Dictionary<string, string>();
        foreach (Match m in Regex.Matches(text, @"--- !u!\d+ &(\d+)\r?\n\w+:.*?(?=\r?\n--- !u!|\z)", RegexOptions.Singleline))
            blocks[m.Groups[1].Value] = m.Value;
        return blocks;
    }

    static Dictionary<string, GOData> ParseGameObjects(string text)
    {
        var gos = new Dictionary<string, GOData>();
        foreach (Match m in Regex.Matches(text, @"--- !u!1 &(\d+)\r?\nGameObject:.*?(?=\r?\n--- !u!)", RegexOptions.Singleline))
        {
            var go = new GOData { FileID = m.Groups[1].Value };
            var v = m.Value;
            var nm = Regex.Match(v, @"m_Name:\s+(.+?)[\r\n]"); if (nm.Success) go.Name = nm.Groups[1].Value.Trim();
            var lm = Regex.Match(v, @"m_Layer:\s*(\d+)"); if (lm.Success) go.Layer = int.Parse(lm.Groups[1].Value);
            var am = Regex.Match(v, @"m_IsActive:\s*(\d)"); if (am.Success) go.IsActive = am.Groups[1].Value == "1";
            foreach (Match cm in Regex.Matches(v, @"component:\s*\{fileID:\s*(\d+)\}"))
                go.ComponentFileIDs.Add(cm.Groups[1].Value);
            gos[go.FileID] = go;
        }
        return gos;
    }

    static void BuildHierarchy(string text, Dictionary<string, GOData> gos, Dictionary<string, RTData> rtData)
    {
        var rtToGo = new Dictionary<string, string>();
        foreach (Match m in Regex.Matches(text, @"--- !u!224 &(\d+)\r?\nRectTransform:.*?(?=\r?\n--- !u!)", RegexOptions.Singleline))
        {
            var rtFileId = m.Groups[1].Value;
            var v = m.Value;
            var goId = ExtractFileID(v, "m_GameObject");
            if (goId != null) rtToGo[rtFileId] = goId;
            var fatherId = ExtractFileID(v, "m_Father");

            var rt = new RTData();
            ParseVector2(v, "m_AnchorMin", out rt.AncMinX, out rt.AncMinY);
            ParseVector2(v, "m_AnchorMax", out rt.AncMaxX, out rt.AncMaxY);
            ParseVector2(v, "m_AnchoredPosition", out rt.PosX, out rt.PosY);
            ParseVector2(v, "m_SizeDelta", out rt.SzX, out rt.SzY);
            ParseVector2(v, "m_Pivot", out rt.PvtX, out rt.PvtY);
            if (goId != null) rtData[goId] = rt;

            if (fatherId != null && fatherId != "0" && goId != null)
            {
                var parentGoId = rtToGo.ContainsKey(fatherId) ? rtToGo[fatherId] : null;
                if (parentGoId == null)
                {
                    var pm = Regex.Match(text, $@"--- !u!224 &{Regex.Escape(fatherId)}\r?\nRectTransform:.*?m_GameObject:\s*\{{fileID:\s*(\d+)", RegexOptions.Singleline);
                    if (pm.Success) { parentGoId = pm.Groups[1].Value; rtToGo[fatherId] = parentGoId; }
                }
                if (parentGoId != null && gos.ContainsKey(goId) && gos.ContainsKey(parentGoId))
                {
                    gos[goId].ParentGoFileID = parentGoId;
                    gos[parentGoId].ChildGoFileIDs.Add(goId);
                }
            }
        }
    }

    static List<ComponentData> ParseComponents(string text, Dictionary<string, string> allBlocks)
    {
        var comps = new List<ComponentData>();
        foreach (Match m in Regex.Matches(text, @"--- !u!114 &(\d+)\r?\nMonoBehaviour:.*?(?=\r?\n---)", RegexOptions.Singleline))
        {
            var cd = new ComponentData { FileID = m.Groups[1].Value, TypeID = "114", RawYAML = m.Value };
            cd.GoFileID = ExtractFileID(m.Value, "m_GameObject");
            var sm = Regex.Match(m.Value, @"m_Script:\s*\{[^}]*guid:\s*([a-f0-9]+)"); if (sm.Success) cd.ScriptGUID = sm.Groups[1].Value;
            comps.Add(cd);
        }
        foreach (Match m in Regex.Matches(text, @"--- !u!20 &(\d+)\r?\nCamera:.*?(?=\r?\n--- !u!)", RegexOptions.Singleline))
            comps.Add(new ComponentData { FileID = m.Groups[1].Value, TypeID = "20", ScriptName = "Camera", GoFileID = ExtractFileID(m.Value, "m_GameObject"), RawYAML = m.Value });
        foreach (Match m in Regex.Matches(text, @"--- !u!81 &(\d+)\r?\nAudioListener:.*?(?=\r?\n--- !u!)", RegexOptions.Singleline))
            comps.Add(new ComponentData { FileID = m.Groups[1].Value, TypeID = "81", ScriptName = "AudioListener", GoFileID = ExtractFileID(m.Value, "m_GameObject"), RawYAML = m.Value });
        foreach (Match m in Regex.Matches(text, @"--- !u!222 &(\d+)\r?\nCanvasRenderer:.*?(?=\r?\n--- !u!)", RegexOptions.Singleline))
            comps.Add(new ComponentData { FileID = m.Groups[1].Value, TypeID = "222", ScriptName = "CanvasRenderer", GoFileID = ExtractFileID(m.Value, "m_GameObject"), RawYAML = m.Value });
        foreach (Match m in Regex.Matches(text, @"--- !u!223 &(\d+)\r?\nCanvas:.*?(?=\r?\n--- !u!)", RegexOptions.Singleline))
            comps.Add(new ComponentData { FileID = m.Groups[1].Value, TypeID = "223", ScriptName = "Canvas", GoFileID = ExtractFileID(m.Value, "m_GameObject"), RawYAML = m.Value });
        foreach (Match m in Regex.Matches(text, @"--- !u!225 &(\d+)\r?\nCanvasGroup:.*?(?=\r?\n--- !u!)", RegexOptions.Singleline))
            comps.Add(new ComponentData { FileID = m.Groups[1].Value, TypeID = "225", ScriptName = "CanvasGroup", GoFileID = ExtractFileID(m.Value, "m_GameObject"), RawYAML = m.Value });
        return comps;
    }

    static void ResolveScriptNames(List<ComponentData> comps, string scenePath)
    {
        var customScripts = new Dictionary<string, string>();
        var assetsDir = Path.Combine(Path.GetDirectoryName(scenePath), "..", "Assets");
        foreach (var meta in Directory.GetFiles(assetsDir, "*.cs.meta", SearchOption.AllDirectories))
        {
            try
            {
                var content = File.ReadAllText(meta);
                var gm = Regex.Match(content, @"guid:\s*([a-f0-9]{32})");
                if (gm.Success) customScripts[gm.Groups[1].Value] = Path.GetFileNameWithoutExtension(meta.Replace(".meta", ""));
            }
            catch { }
        }
        foreach (var cd in comps)
        {
            if (cd.ScriptGUID == null || cd.ScriptName != null) continue;
            if (BuiltinScriptMap.TryGetValue(cd.ScriptGUID, out var bn)) cd.ScriptName = bn;
            else if (customScripts.TryGetValue(cd.ScriptGUID, out var cn)) cd.ScriptName = cn;
            else cd.ScriptName = $"Unknown_{cd.ScriptGUID.Substring(0, 8)}";
        }
    }

    // ===================== Asset Map Builders =====================

    static Dictionary<string, string> BuildSpriteMap(List<ComponentData> components)
    {
        var guids = new HashSet<string>();
        foreach (var cd in components) { var g = ExtractSpriteGUID(cd.RawYAML); if (g != null && !g.StartsWith("0000000000000000")) guids.Add(g); }
        return ResolveAssetPaths(guids);
    }

    static Dictionary<string, string> BuildFontMap(List<ComponentData> components)
    {
        var guids = new HashSet<string>();
        foreach (var cd in components) { var g = ExtractFontGUID(cd.RawYAML); if (g != null) guids.Add(g); }
        return ResolveAssetPaths(guids);
    }

    static Dictionary<string, string> ResolveAssetPaths(HashSet<string> guids)
    {
        var map = new Dictionary<string, string>();
        if (guids.Count == 0) return map;
        var assetsDir = Application.dataPath;
        foreach (var meta in Directory.GetFiles(assetsDir, "*.meta", SearchOption.AllDirectories))
        {
            if (meta.Contains("[")) continue;
            try
            {
                var content = File.ReadAllText(meta);
                foreach (var guid in guids.ToList())
                {
                    if (content.Contains(guid))
                    {
                        map[guid] = "Assets" + meta.Replace(assetsDir, "").Replace(".meta", "").Replace('\\', '/');
                        guids.Remove(guid);
                    }
                }
            }
            catch { }
            if (guids.Count == 0) break;
        }
        return map;
    }

    // ===================== Helpers =====================

    static string FindGoForRT(string rtFileID, Dictionary<string, GOData> allGOs)
    {
        // RectTransform fileID is typically GO fileID + 1, but we can't rely on that.
        // Check all GOs for a component matching this fileID.
        foreach (var go in allGOs.Values)
            if (go.ComponentFileIDs.Contains(rtFileID))
                return go.FileID;
        return null;
    }

    static string GetMenuPath(string sceneName)
    {
        // Map scene file names to their menu display paths
        return sceneName switch
        {
            "TitleScreen" => "Title Screen",
            "ProfileSelect" => "Profile Select",
            "ProfileCreate" => "Profile Create",
            "SaveFileSelect" => "Save File Select",
            "StageSelect" => "Stage Select",
            "LoadingScreen" => "Loading Screen",
            "PostBattleScreen" => "Post Battle Screen",
            "PartyManager" => "Party Manager",
            "SplashScreen" => "Splash Screen",
            _ => sceneName
        };
    }

    static string ExtractFileID(string yaml, string fieldName)
    {
        var m = Regex.Match(yaml, fieldName + @":\s*\{fileID:\s*(\d+)");
        return m.Success && m.Groups[1].Value != "0" ? m.Groups[1].Value : null;
    }

    static int ExtractInt(string yaml, string fieldName, int def)
    {
        var m = Regex.Match(yaml, fieldName + @":\s*(-?\d+)");
        return m.Success ? int.Parse(m.Groups[1].Value) : def;
    }

    static float ExtractFloat(string yaml, string fieldName, float def)
    {
        var m = Regex.Match(yaml, fieldName + @":\s*([\d.e+-]+)");
        return m.Success && float.TryParse(m.Groups[1].Value, System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out var v) ? v : def;
    }

    static string ExtractString(string yaml, string fieldName)
    {
        var m = Regex.Match(yaml, fieldName + @":\s*(.+?)[\r\n]");
        return m.Success ? m.Groups[1].Value.Trim() : "";
    }

    static string ExtractSpriteGUID(string yaml)
    {
        var m = Regex.Match(yaml, @"m_Sprite:\s*\{fileID:\s*\d+,\s*guid:\s*([a-f0-9]{32})");
        return m.Success ? m.Groups[1].Value : null;
    }

    static string ExtractFontGUID(string yaml)
    {
        var m = Regex.Match(yaml, @"m_fontAsset:\s*\{fileID:\s*\d+,\s*guid:\s*([a-f0-9]{32})");
        return m.Success ? m.Groups[1].Value : null;
    }

    static void ExtractColor(string yaml, string fieldName, out float r, out float g, out float b, out float a)
    {
        r = 1f; g = 1f; b = 1f; a = 1f;
        var m = Regex.Match(yaml, fieldName + @":\s*\{r:\s*([\d.e+-]+),\s*g:\s*([\d.e+-]+),\s*b:\s*([\d.e+-]+),\s*a:\s*([\d.e+-]+)\}");
        if (!m.Success) return;
        float.TryParse(m.Groups[1].Value, System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out r);
        float.TryParse(m.Groups[2].Value, System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out g);
        float.TryParse(m.Groups[3].Value, System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out b);
        float.TryParse(m.Groups[4].Value, System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out a);
    }

    static void ParseVector2(string yaml, string fieldName, out float x, out float y)
    {
        x = 0f; y = 0f;
        var m = Regex.Match(yaml, fieldName + @":\s*\{x:\s*([\d.e+-]+),\s*y:\s*([\d.e+-]+)\}");
        if (!m.Success) return;
        float.TryParse(m.Groups[1].Value, System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out x);
        float.TryParse(m.Groups[2].Value, System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out y);
    }

    static string F(float v)
    {
        if (v == 0f) return "0f";
        if (v == 1f) return "1f";
        if (v == -1f) return "-1f";
        return v.ToString("G", System.Globalization.CultureInfo.InvariantCulture) + "f";
    }

    static string SanitizeId(string name) =>
        Regex.Replace(Regex.Replace(name, @"[^a-zA-Z0-9_]", "_"), @"__+", "_").Trim('_');

    static string EscapeString(string s) =>
        string.IsNullOrEmpty(s) ? "" : s.Replace("\\", "\\\\").Replace("\"", "\\\"").Replace("\n", "\\n").Replace("\r", "");
}
