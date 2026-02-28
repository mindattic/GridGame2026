using UnityEngine;
using UnityEditor;
using System.Text;
using TMPro;

/// <summary>
/// Editor tool to analyze a prefab and output all component values.
/// Use this to extract exact values needed to replicate a prefab in code.
/// 
/// USAGE:
/// 1. Select a prefab in the Project window
/// 2. Go to Tools > Analyze Selected Prefab
/// 3. Check the Console for output
/// </summary>
public class PrefabAnalyzer : EditorWindow
{
    [MenuItem("Tools/Analyze Selected Prefab")]
    public static void AnalyzeSelectedPrefab()
    {
        var selected = Selection.activeGameObject;
        if (selected == null)
        {
            Debug.LogError("Please select a prefab in the Project window first.");
            return;
        }

        var sb = new StringBuilder();
        sb.AppendLine("=== PREFAB ANALYSIS ===");
        sb.AppendLine($"Prefab Name: {selected.name}");
        sb.AppendLine($"Layer: {selected.layer} ({LayerMask.LayerToName(selected.layer)})");
        sb.AppendLine($"Tag: {selected.tag}");
        sb.AppendLine();

        AnalyzeGameObject(selected, sb, 0);

        Debug.Log(sb.ToString());
        
        // Also save to file for easier reading
        var path = "Assets/Editor/PrefabAnalysis_Output.txt";
        System.IO.File.WriteAllText(path, sb.ToString());
        AssetDatabase.Refresh();
        Debug.Log($"Analysis saved to: {path}");
    }

    private static void AnalyzeGameObject(GameObject go, StringBuilder sb, int depth)
    {
        var indent = new string(' ', depth * 2);
        
        sb.AppendLine($"{indent}[{go.name}] Active={go.activeSelf}, Layer={go.layer}");

        // Transform
        var t = go.transform;
        if (t.localPosition != Vector3.zero || t.localRotation != Quaternion.identity || t.localScale != Vector3.one)
        {
            sb.AppendLine($"{indent}  Transform:");
            if (t.localPosition != Vector3.zero)
                sb.AppendLine($"{indent}    localPosition: new Vector3({t.localPosition.x}f, {t.localPosition.y}f, {t.localPosition.z}f)");
            if (t.localRotation != Quaternion.identity)
                sb.AppendLine($"{indent}    localRotation: Quaternion.Euler({t.localEulerAngles.x}f, {t.localEulerAngles.y}f, {t.localEulerAngles.z}f)");
            if (t.localScale != Vector3.one)
                sb.AppendLine($"{indent}    localScale: new Vector3({t.localScale.x}f, {t.localScale.y}f, {t.localScale.z}f)");
        }

        // SpriteRenderer
        var sr = go.GetComponent<SpriteRenderer>();
        if (sr != null)
        {
            sb.AppendLine($"{indent}  SpriteRenderer:");
            sb.AppendLine($"{indent}    sprite: {(sr.sprite != null ? sr.sprite.name : "null")}");
            sb.AppendLine($"{indent}    color: new Color({sr.color.r}f, {sr.color.g}f, {sr.color.b}f, {sr.color.a}f)");
            sb.AppendLine($"{indent}    material: {(sr.sharedMaterial != null ? sr.sharedMaterial.name : "null")}");
            sb.AppendLine($"{indent}    sortingLayerName: \"{sr.sortingLayerName}\"");
            sb.AppendLine($"{indent}    sortingOrder: {sr.sortingOrder}");
            sb.AppendLine($"{indent}    drawMode: SpriteDrawMode.{sr.drawMode}");
            if (sr.drawMode != SpriteDrawMode.Simple)
                sb.AppendLine($"{indent}    size: new Vector2({sr.size.x}f, {sr.size.y}f)");
            sb.AppendLine($"{indent}    maskInteraction: SpriteMaskInteraction.{sr.maskInteraction}");
            sb.AppendLine($"{indent}    spriteSortPoint: SpriteSortPoint.{sr.spriteSortPoint}");
            sb.AppendLine($"{indent}    flipX: {sr.flipX.ToString().ToLower()}, flipY: {sr.flipY.ToString().ToLower()}");
        }

        // SpriteMask
        var mask = go.GetComponent<SpriteMask>();
        if (mask != null)
        {
            sb.AppendLine($"{indent}  SpriteMask:");
            sb.AppendLine($"{indent}    sprite: {(mask.sprite != null ? mask.sprite.name : "null")}");
            sb.AppendLine($"{indent}    alphaCutoff: {mask.alphaCutoff}f");
            sb.AppendLine($"{indent}    isCustomRangeActive: {mask.isCustomRangeActive.ToString().ToLower()}");
            sb.AppendLine($"{indent}    frontSortingLayerID: {mask.frontSortingLayerID}");
            sb.AppendLine($"{indent}    frontSortingOrder: {mask.frontSortingOrder}");
            sb.AppendLine($"{indent}    backSortingLayerID: {mask.backSortingLayerID}");
            sb.AppendLine($"{indent}    backSortingOrder: {mask.backSortingOrder}");
        }

        // TextMeshPro
        var tmp = go.GetComponent<TextMeshPro>();
        if (tmp != null)
        {
            sb.AppendLine($"{indent}  TextMeshPro:");
            sb.AppendLine($"{indent}    font: {(tmp.font != null ? tmp.font.name : "null")}");
            sb.AppendLine($"{indent}    fontSize: {tmp.fontSize}f");
            sb.AppendLine($"{indent}    color: new Color({tmp.color.r}f, {tmp.color.g}f, {tmp.color.b}f, {tmp.color.a}f)");
            sb.AppendLine($"{indent}    alignment: TextAlignmentOptions.{tmp.alignment}");
            sb.AppendLine($"{indent}    text: \"{tmp.text}\"");
            sb.AppendLine($"{indent}    sortingLayerID: {tmp.sortingLayerID}");
            sb.AppendLine($"{indent}    sortingOrder: {tmp.sortingOrder}");
            sb.AppendLine($"{indent}    enabled: {tmp.enabled.ToString().ToLower()}");
        }

        // RectTransform
        var rt = go.GetComponent<RectTransform>();
        if (rt != null && tmp != null) // Only show for TMP objects
        {
            sb.AppendLine($"{indent}  RectTransform:");
            sb.AppendLine($"{indent}    anchorMin: new Vector2({rt.anchorMin.x}f, {rt.anchorMin.y}f)");
            sb.AppendLine($"{indent}    anchorMax: new Vector2({rt.anchorMax.x}f, {rt.anchorMax.y}f)");
            sb.AppendLine($"{indent}    pivot: new Vector2({rt.pivot.x}f, {rt.pivot.y}f)");
            sb.AppendLine($"{indent}    sizeDelta: new Vector2({rt.sizeDelta.x}f, {rt.sizeDelta.y}f)");
        }

        // SortingGroup
        var sg = go.GetComponent<UnityEngine.Rendering.SortingGroup>();
        if (sg != null)
        {
            sb.AppendLine($"{indent}  SortingGroup:");
            sb.AppendLine($"{indent}    sortingLayerName: \"{sg.sortingLayerName}\"");
            sb.AppendLine($"{indent}    sortingOrder: {sg.sortingOrder}");
        }

        // MonoBehaviours (custom components)
        var behaviours = go.GetComponents<MonoBehaviour>();
        foreach (var b in behaviours)
        {
            if (b == null) continue;
            var typeName = b.GetType().Name;
            // Skip common Unity components
            if (typeName == "TextMeshPro" || typeName == "TextMeshProUGUI") continue;
            
            sb.AppendLine($"{indent}  MonoBehaviour: {typeName}");
            
            // Try to extract serialized fields
            var so = new SerializedObject(b);
            var prop = so.GetIterator();
            prop.Next(true); // Skip script reference
            while (prop.NextVisible(false))
            {
                if (prop.name == "m_Script") continue;
                sb.AppendLine($"{indent}    {prop.name}: {GetPropertyValue(prop)}");
            }
        }

        sb.AppendLine();

        // Recurse into children
        for (int i = 0; i < go.transform.childCount; i++)
        {
            AnalyzeGameObject(go.transform.GetChild(i).gameObject, sb, depth + 1);
        }
    }

    private static string GetPropertyValue(SerializedProperty prop)
    {
        switch (prop.propertyType)
        {
            case SerializedPropertyType.Integer:
                return prop.intValue.ToString();
            case SerializedPropertyType.Boolean:
                return prop.boolValue.ToString().ToLower();
            case SerializedPropertyType.Float:
                return $"{prop.floatValue}f";
            case SerializedPropertyType.String:
                return $"\"{prop.stringValue}\"";
            case SerializedPropertyType.Color:
                var c = prop.colorValue;
                return $"new Color({c.r}f, {c.g}f, {c.b}f, {c.a}f)";
            case SerializedPropertyType.Vector2:
                var v2 = prop.vector2Value;
                return $"new Vector2({v2.x}f, {v2.y}f)";
            case SerializedPropertyType.Vector3:
                var v3 = prop.vector3Value;
                return $"new Vector3({v3.x}f, {v3.y}f, {v3.z}f)";
            case SerializedPropertyType.ObjectReference:
                return prop.objectReferenceValue != null ? prop.objectReferenceValue.name : "null";
            case SerializedPropertyType.AnimationCurve:
                var curve = prop.animationCurveValue;
                if (curve == null || curve.length == 0) return "new AnimationCurve()";
                var curveStr = new StringBuilder("new AnimationCurve(");
                for (int i = 0; i < curve.length; i++)
                {
                    var key = curve[i];
                    if (i > 0) curveStr.Append(", ");
                    curveStr.Append($"new Keyframe({key.time}f, {key.value}f, {key.inTangent}f, {key.outTangent}f)");
                }
                curveStr.Append(")");
                return curveStr.ToString();
            default:
                return $"[{prop.propertyType}]";
        }
    }
}
