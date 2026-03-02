using UnityEngine;
using UnityEditor;
using System.Text;
using System.Collections.Generic;
using Scripts.Factories;
using Scripts.Libraries;
using TMPro;
using Scripts.Canvas;
using Scripts.Data.Actor;
using Scripts.Data.Items;
using Scripts.Data.Skills;
using Scripts.Effects;
using Scripts.Helpers;
using Scripts.Hub;
using Scripts.Instances;
using Scripts.Instances.Actor;
using Scripts.Instances.Board;
using Scripts.Instances.SynergyLine;
using Scripts.Inventory;
using Scripts.Managers;
using Scripts.Models;
using Scripts.Models.Actor;
using Scripts.Overworld;
using Scripts.Sequences;
using Scripts.Serialization;
using Scripts.Utilities;

/// <summary>
/// Debug tool to compare Prefab vs Factory actors side-by-side.
/// Shows exactly what's different between them.
/// </summary>
public class ActorFactoryDebugger : EditorWindow
{
    private GameObject prefabInstance;
    private GameObject factoryInstance;
    private Vector2 scrollPos;
    private string comparisonResult = "";

    [MenuItem("Tools/Debug Actor Factory")]
    public static void ShowWindow()
    {
        GetWindow<ActorFactoryDebugger>("Actor Factory Debugger");
    }

    private void OnGUI()
    {
        EditorGUILayout.LabelField("Actor Factory vs Prefab Comparison", EditorStyles.boldLabel);
        EditorGUILayout.Space();

        if (GUILayout.Button("Spawn Both & Compare", GUILayout.Height(30)))
        {
            SpawnAndCompare();
        }

        EditorGUILayout.Space();

        if (GUILayout.Button("Clean Up Spawned Objects"))
        {
            CleanUp();
        }

        EditorGUILayout.Space();
        EditorGUILayout.LabelField("Comparison Results:", EditorStyles.boldLabel);

        scrollPos = EditorGUILayout.BeginScrollView(scrollPos);
        EditorGUILayout.TextArea(comparisonResult, GUILayout.ExpandHeight(true));
        EditorGUILayout.EndScrollView();
    }

    private void SpawnAndCompare()
    {
        CleanUp();

        // Reset material library to ensure fresh load
        MaterialLibrary.Reset();

        var sb = new StringBuilder();
        sb.AppendLine("=== SPAWNING ACTORS ===\n");

        // Spawn prefab instance
        var prefab = PrefabLibrary.Prefabs["ActorPrefab"];
        if (prefab == null)
        {
            sb.AppendLine("ERROR: Could not load ActorPrefab from PrefabLibrary");
            comparisonResult = sb.ToString();
            return;
        }

        prefabInstance = Instantiate(prefab, new Vector3(-2, 0, 0), Quaternion.identity);
        prefabInstance.name = "DEBUG_Prefab";
        sb.AppendLine("✓ Spawned Prefab at (-2, 0, 0)");

        // Spawn factory instance
        factoryInstance = ActorFactory.Create(null);
        factoryInstance.transform.position = new Vector3(2, 0, 0);
        factoryInstance.name = "DEBUG_Factory";
        sb.AppendLine("✓ Spawned Factory at (2, 0, 0)");

        sb.AppendLine("\n=== COMPARING ===\n");

        // Compare root
        CompareGameObjects(prefabInstance, factoryInstance, "", sb);

        comparisonResult = sb.ToString();
        Debug.Log(comparisonResult);
    }

    private void CompareGameObjects(GameObject prefab, GameObject factory, string indent, StringBuilder sb)
    {
        // Compare basic properties
        if (prefab.name.Replace("DEBUG_Prefab", "") != factory.name.Replace("DEBUG_Factory", "").Replace("ActorPrefab", ""))
        {
            // Names will differ, skip
        }

        if (prefab.layer != factory.layer)
            sb.AppendLine($"{indent}[{prefab.name}] LAYER DIFF: Prefab={prefab.layer}, Factory={factory.layer}");

        if (prefab.activeSelf != factory.activeSelf)
            sb.AppendLine($"{indent}[{prefab.name}] ACTIVE DIFF: Prefab={prefab.activeSelf}, Factory={factory.activeSelf}");

        // Compare transforms
        CompareTransforms(prefab.transform, factory.transform, indent, sb);

        // Compare SpriteRenderers
        var prefabSR = prefab.GetComponent<SpriteRenderer>();
        var factorySR = factory.GetComponent<SpriteRenderer>();
        if (prefabSR != null || factorySR != null)
        {
            CompareSpriteRenderers(prefabSR, factorySR, prefab.name, indent, sb);
        }

        // Compare SpriteMasks
        var prefabMask = prefab.GetComponent<SpriteMask>();
        var factoryMask = factory.GetComponent<SpriteMask>();
        if (prefabMask != null || factoryMask != null)
        {
            CompareSpriteMasks(prefabMask, factoryMask, prefab.name, indent, sb);
        }

        // Compare TextMeshPro
        var prefabTMP = prefab.GetComponent<TextMeshPro>();
        var factoryTMP = factory.GetComponent<TextMeshPro>();
        if (prefabTMP != null || factoryTMP != null)
        {
            CompareTMP(prefabTMP, factoryTMP, prefab.name, indent, sb);
        }

        // Recursively compare children by name
        var prefabChildren = new Dictionary<string, Transform>();
        var factoryChildren = new Dictionary<string, Transform>();

        for (int i = 0; i < prefab.transform.childCount; i++)
            prefabChildren[prefab.transform.GetChild(i).name] = prefab.transform.GetChild(i);

        for (int i = 0; i < factory.transform.childCount; i++)
            factoryChildren[factory.transform.GetChild(i).name] = factory.transform.GetChild(i);

        // Check for missing children
        foreach (var kvp in prefabChildren)
        {
            if (!factoryChildren.ContainsKey(kvp.Key))
                sb.AppendLine($"{indent}  MISSING IN FACTORY: {kvp.Key}");
        }

        foreach (var kvp in factoryChildren)
        {
            if (!prefabChildren.ContainsKey(kvp.Key))
                sb.AppendLine($"{indent}  EXTRA IN FACTORY: {kvp.Key}");
        }

        // Compare matching children
        foreach (var kvp in prefabChildren)
        {
            if (factoryChildren.TryGetValue(kvp.Key, out var factoryChild))
            {
                CompareGameObjects(kvp.Value.gameObject, factoryChild.gameObject, indent + "  ", sb);
            }
        }
    }

    private void CompareTransforms(Transform p, Transform f, string indent, StringBuilder sb)
    {
        if (!ApproxEqual(p.localPosition, f.localPosition))
            sb.AppendLine($"{indent}[{p.name}] POSITION DIFF: Prefab={p.localPosition}, Factory={f.localPosition}");

        if (!ApproxEqual(p.localScale, f.localScale))
            sb.AppendLine($"{indent}[{p.name}] SCALE DIFF: Prefab={p.localScale}, Factory={f.localScale}");

        if (!ApproxEqual(p.localEulerAngles, f.localEulerAngles))
            sb.AppendLine($"{indent}[{p.name}] ROTATION DIFF: Prefab={p.localEulerAngles}, Factory={f.localEulerAngles}");
    }

    private void CompareSpriteRenderers(SpriteRenderer p, SpriteRenderer f, string name, string indent, StringBuilder sb)
    {
        if (p == null) { sb.AppendLine($"{indent}[{name}] SR MISSING IN PREFAB"); return; }
        if (f == null) { sb.AppendLine($"{indent}[{name}] SR MISSING IN FACTORY"); return; }

        string pSprite = p.sprite != null ? p.sprite.name : "null";
        string fSprite = f.sprite != null ? f.sprite.name : "null";
        if (pSprite != fSprite)
            sb.AppendLine($"{indent}[{name}] SPRITE DIFF: Prefab={pSprite}, Factory={fSprite}");

        if (!ApproxEqual(p.color, f.color))
            sb.AppendLine($"{indent}[{name}] COLOR DIFF: Prefab={p.color}, Factory={f.color}");

        if (p.sortingLayerName != f.sortingLayerName)
            sb.AppendLine($"{indent}[{name}] SORTING_LAYER DIFF: Prefab={p.sortingLayerName}, Factory={f.sortingLayerName}");

        if (p.sortingOrder != f.sortingOrder)
            sb.AppendLine($"{indent}[{name}] SORTING_ORDER DIFF: Prefab={p.sortingOrder}, Factory={f.sortingOrder}");

        if (p.drawMode != f.drawMode)
            sb.AppendLine($"{indent}[{name}] DRAW_MODE DIFF: Prefab={p.drawMode}, Factory={f.drawMode}");

        if (p.drawMode == SpriteDrawMode.Sliced && f.drawMode == SpriteDrawMode.Sliced)
        {
            if (!ApproxEqual(p.size, f.size))
                sb.AppendLine($"{indent}[{name}] SIZE DIFF: Prefab={p.size}, Factory={f.size}");
        }

        if (p.maskInteraction != f.maskInteraction)
            sb.AppendLine($"{indent}[{name}] MASK_INTERACTION DIFF: Prefab={p.maskInteraction}, Factory={f.maskInteraction}");

        string pMat = p.sharedMaterial != null ? p.sharedMaterial.name : "null";
        string fMat = f.sharedMaterial != null ? f.sharedMaterial.name : "null";
        if (pMat != fMat)
            sb.AppendLine($"{indent}[{name}] MATERIAL DIFF: Prefab={pMat}, Factory={fMat}");
    }

    private void CompareSpriteMasks(SpriteMask p, SpriteMask f, string name, string indent, StringBuilder sb)
    {
        if (p == null) { sb.AppendLine($"{indent}[{name}] MASK MISSING IN PREFAB"); return; }
        if (f == null) { sb.AppendLine($"{indent}[{name}] MASK MISSING IN FACTORY"); return; }

        string pSprite = p.sprite != null ? p.sprite.name : "null";
        string fSprite = f.sprite != null ? f.sprite.name : "null";
        if (pSprite != fSprite)
            sb.AppendLine($"{indent}[{name}] MASK_SPRITE DIFF: Prefab={pSprite}, Factory={fSprite}");

        if (!Mathf.Approximately(p.alphaCutoff, f.alphaCutoff))
            sb.AppendLine($"{indent}[{name}] ALPHA_CUTOFF DIFF: Prefab={p.alphaCutoff}, Factory={f.alphaCutoff}");
    }

    private void CompareTMP(TextMeshPro p, TextMeshPro f, string name, string indent, StringBuilder sb)
    {
        if (p == null) { sb.AppendLine($"{indent}[{name}] TMP MISSING IN PREFAB"); return; }
        if (f == null) { sb.AppendLine($"{indent}[{name}] TMP MISSING IN FACTORY"); return; }

        string pFont = p.font != null ? p.font.name : "null";
        string fFont = f.font != null ? f.font.name : "null";
        if (pFont != fFont)
            sb.AppendLine($"{indent}[{name}] FONT DIFF: Prefab={pFont}, Factory={fFont}");

        if (!Mathf.Approximately(p.fontSize, f.fontSize))
            sb.AppendLine($"{indent}[{name}] FONT_SIZE DIFF: Prefab={p.fontSize}, Factory={f.fontSize}");

        if (p.alignment != f.alignment)
            sb.AppendLine($"{indent}[{name}] ALIGNMENT DIFF: Prefab={p.alignment}, Factory={f.alignment}");

        if (p.sortingOrder != f.sortingOrder)
            sb.AppendLine($"{indent}[{name}] TMP_SORTING_ORDER DIFF: Prefab={p.sortingOrder}, Factory={f.sortingOrder}");
    }

    private bool ApproxEqual(Vector3 a, Vector3 b, float tolerance = 0.001f)
    {
        return Mathf.Abs(a.x - b.x) < tolerance &&
               Mathf.Abs(a.y - b.y) < tolerance &&
               Mathf.Abs(a.z - b.z) < tolerance;
    }

    private bool ApproxEqual(Vector2 a, Vector2 b, float tolerance = 0.001f)
    {
        return Mathf.Abs(a.x - b.x) < tolerance &&
               Mathf.Abs(a.y - b.y) < tolerance;
    }

    private bool ApproxEqual(Color a, Color b, float tolerance = 0.01f)
    {
        return Mathf.Abs(a.r - b.r) < tolerance &&
               Mathf.Abs(a.g - b.g) < tolerance &&
               Mathf.Abs(a.b - b.b) < tolerance &&
               Mathf.Abs(a.a - b.a) < tolerance;
    }

    private void CleanUp()
    {
        if (prefabInstance != null) DestroyImmediate(prefabInstance);
        if (factoryInstance != null) DestroyImmediate(factoryInstance);
        prefabInstance = null;
        factoryInstance = null;
    }

    private void OnDestroy()
    {
        CleanUp();
    }
}
