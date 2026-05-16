using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// SCAFFOLDDRIFTCHECKER - Captures canonical per-scene signatures and flags drift.
/// <para>PURPOSE: Builders are the authoritative source for scene content. Once a scene's
/// canonical signature is committed under Documentation/Builders/Drift/, any later change
/// to the scene file that isn't reflected in the builder will diff against the snapshot.</para>
/// <para>SIGNATURE FORMAT: One line per object. "[Name]" denotes a GameObject at a given
/// indent depth; indented below it are its components, one per line, in stable alphabetical
/// order. Only structural fields that builders typically set are recorded — noise like
/// fileIDs, GUIDs, and most internal Unity state is excluded.</para>
/// <para>USAGE: Regenerate snapshots when builders legitimately change; Verify on every
/// builder run / commit to catch drift. Both paths are routed through CliEntryPoints.</para>
/// <para>RELATED FILES: CliEntryPoints.cs, SceneBuilderHelper.cs, Documentation/Builders/Drift/</para>
/// </summary>
public static class BuilderDriftChecker
{
    public const string SnapshotRoot = "Documentation/Builders/Drift";

    // ===================== Public API =====================

    /// <summary>
    /// For each scene, opens it and writes Documentation/Builders/Drift/&lt;Scene&gt;.snapshot.txt.
    /// Returns the number of scenes written successfully.
    /// </summary>
    public static int Regenerate(IEnumerable<string> scenes)
    {
        Directory.CreateDirectory(SnapshotRoot);
        int ok = 0;
        foreach (var scene in scenes)
        {
            if (!OpenSceneSafe(scene)) continue;
            File.WriteAllText(SnapshotPath(scene), BuildSignature(scene));
            Debug.Log($"[BuilderDrift] Wrote snapshot: {SnapshotPath(scene)}");
            ok++;
        }
        return ok;
    }

    /// <summary>
    /// For each scene, opens it, computes signature, and diffs against the committed snapshot.
    /// Returns the number of scenes with drift (0 = clean). Missing snapshots count as drift.
    /// </summary>
    public static int Verify(IEnumerable<string> scenes)
    {
        int drifted = 0;
        foreach (var scene in scenes)
        {
            if (!OpenSceneSafe(scene)) { drifted++; continue; }

            var current = BuildSignature(scene);
            var path = SnapshotPath(scene);
            if (!File.Exists(path))
            {
                Debug.LogError($"[BuilderDrift] {scene}: no snapshot at {path}. Run RegenerateBuilderSnapshots to create.");
                drifted++;
                continue;
            }
            var committed = File.ReadAllText(path);
            if (!NormalizedEqual(current, committed))
            {
                drifted++;
                var diff = FirstDiff(committed, current);
                Debug.LogError($"[BuilderDrift] {scene}: DRIFT detected.\n  First diff at line {diff.line}:\n    committed: {diff.a}\n    current  : {diff.b}");

                // Write the current signature next to the snapshot so the user can inspect full diff.
                var actualPath = path.Replace(".snapshot.txt", ".actual.txt");
                File.WriteAllText(actualPath, current);
                Debug.LogError($"[BuilderDrift] Full current signature written to {actualPath} for inspection.");
            }
            else
            {
                Debug.Log($"[BuilderDrift] {scene}: OK.");
            }
        }

        if (drifted == 0)
            Debug.Log($"[BuilderDrift] All scenes clean.");
        else
            Debug.LogError($"[BuilderDrift] FAIL — {drifted} scene(s) drifted from their committed snapshots.");
        return drifted;
    }

    // ===================== Scene Loading =====================

    private static bool OpenSceneSafe(string sceneName)
    {
        var path = $"Assets/Scenes/{sceneName}.unity";
        if (!File.Exists(path))
        {
            Debug.LogError($"[BuilderDrift] Scene file missing: {path}");
            return false;
        }
        EditorSceneManager.OpenScene(path);
        return true;
    }

    private static string SnapshotPath(string sceneName)
        => Path.Combine(SnapshotRoot, $"{sceneName}.snapshot.txt").Replace('\\', '/');

    // ===================== Signature Builder =====================

    private static string BuildSignature(string sceneName)
    {
        var sb = new StringBuilder();
        sb.Append("# Builder snapshot for scene: ").Append(sceneName).Append('\n');
        sb.Append("# Regenerate via CliEntryPoints.RegenerateBuilderSnapshots.\n");
        sb.Append("# Deterministic — diff this file to see scene drift.\n");
        sb.Append('\n');

        var scene = UnityEngine.SceneManagement.SceneManager.GetActiveScene();
        var roots = scene.GetRootGameObjects();
        Array.Sort(roots, (a, b) => string.CompareOrdinal(a.name, b.name));
        foreach (var go in roots)
            WriteGameObject(sb, go, 0);
        return sb.ToString();
    }

    private static void WriteGameObject(StringBuilder sb, GameObject go, int depth)
    {
        Indent(sb, depth);
        sb.Append('[').Append(go.name).Append(']');
        if (!go.activeSelf) sb.Append(" (inactive)");
        var layerName = LayerMask.LayerToName(go.layer);
        if (!string.IsNullOrEmpty(layerName) && layerName != "Default")
            sb.Append(" layer=").Append(layerName);
        if (go.tag != "Untagged") sb.Append(" tag=").Append(go.tag);
        sb.Append('\n');

        // Components — stable alphabetical order by type name. Skip the Transform written
        // in-line with positional data; we fold its data into its component line instead.
        var components = go.GetComponents<Component>();
        var lines = new List<string>(components.Length);
        foreach (var c in components)
        {
            if (c == null) { lines.Add("    <missing script>"); continue; }
            var line = DescribeComponent(c);
            if (!string.IsNullOrEmpty(line)) lines.Add(line);
        }
        lines.Sort(StringComparer.Ordinal);
        foreach (var line in lines)
        {
            Indent(sb, depth);
            sb.Append("  ").Append(line).Append('\n');
        }

        // Children — sorted by name, then by sibling index as tiebreak.
        var children = new List<Transform>(go.transform.childCount);
        for (int i = 0; i < go.transform.childCount; i++)
            children.Add(go.transform.GetChild(i));
        children.Sort((a, b) =>
        {
            int c = string.CompareOrdinal(a.name, b.name);
            return c != 0 ? c : a.GetSiblingIndex().CompareTo(b.GetSiblingIndex());
        });
        foreach (var child in children)
            WriteGameObject(sb, child.gameObject, depth + 1);
    }

    private static void Indent(StringBuilder sb, int depth)
    {
        for (int i = 0; i < depth; i++) sb.Append("  ");
    }

    // ===================== Component Descriptions =====================

    // Describes a component as a stable, one-line signature. Returns null for components
    // that should be completely omitted. Unknown MonoBehaviours get their type name only.
    private static string DescribeComponent(Component c)
    {
        switch (c)
        {
            case RectTransform rt: return DescribeRect(rt);
            case Transform t: return DescribeTransform(t);
            case Camera cam: return DescribeCamera(cam);
            case Canvas canvas: return DescribeCanvas(canvas);
            case CanvasScaler cs: return DescribeCanvasScaler(cs);
            case GraphicRaycaster _: return "GraphicRaycaster";
            case CanvasRenderer _: return "CanvasRenderer";
            case AudioListener _: return "AudioListener";
            case Mask m: return $"Mask: showGraphic={m.showMaskGraphic}";
            case Image img: return DescribeImage(img);
            case RawImage ri: return DescribeRawImage(ri);
            case Button _: return "Button";
            case Scrollbar sb: return $"Scrollbar: dir={sb.direction}";
            case ScrollRect _: return "ScrollRect";
            case VerticalLayoutGroup vl: return $"VerticalLayoutGroup: spacing={F(vl.spacing)}, align={vl.childAlignment}";
            case HorizontalLayoutGroup hl: return $"HorizontalLayoutGroup: spacing={F(hl.spacing)}, align={hl.childAlignment}";
            case GridLayoutGroup gl: return $"GridLayoutGroup: cell={V2(gl.cellSize)}, spacing={V2(gl.spacing)}";
            case ContentSizeFitter csf: return $"ContentSizeFitter: h={csf.horizontalFit}, v={csf.verticalFit}";
            case LayoutElement le: return $"LayoutElement: pref=({F(le.preferredWidth)},{F(le.preferredHeight)}), flex=({F(le.flexibleWidth)},{F(le.flexibleHeight)})";
            case TextMeshProUGUI tmp: return DescribeTMP(tmp);
            case TMP_Text tmt: return DescribeTMPText(tmt);
            case SpriteRenderer sr: return DescribeSpriteRenderer(sr);
            case MeshRenderer mr: return $"MeshRenderer: enabled={mr.enabled}";
            case MeshFilter mf: return $"MeshFilter: mesh={AssetName(mf.sharedMesh)}";
            case ParticleSystem _: return "ParticleSystem";
            case Collider col: return $"{col.GetType().Name}: enabled={col.enabled}, trigger={col.isTrigger}";
            case Rigidbody rb: return $"Rigidbody: mass={F(rb.mass)}, useGravity={rb.useGravity}, kinematic={rb.isKinematic}";
            case Rigidbody2D rb2: return $"Rigidbody2D: mass={F(rb2.mass)}, gravity={F(rb2.gravityScale)}";
            case MonoBehaviour mb: return mb.GetType().Name;
            default: return c.GetType().Name;
        }
    }

    private static string DescribeTransform(Transform t)
    {
        return $"Transform: pos={V3(t.localPosition)}, rot={V3(t.localEulerAngles)}, scale={V3(t.localScale)}";
    }

    private static string DescribeRect(RectTransform rt)
    {
        return $"RectTransform: anchor=({V2(rt.anchorMin)})-({V2(rt.anchorMax)}), pos={V2(rt.anchoredPosition)}, size={V2(rt.sizeDelta)}, pivot={V2(rt.pivot)}";
    }

    private static string DescribeCamera(Camera cam)
    {
        return $"Camera: ortho={cam.orthographic}, size={F(cam.orthographicSize)}, fov={F(cam.fieldOfView)}, depth={F(cam.depth)}, clear={cam.clearFlags}, bg={Hex(cam.backgroundColor)}";
    }

    private static string DescribeCanvas(Canvas canvas)
    {
        return $"Canvas: mode={canvas.renderMode}, order={canvas.sortingOrder}, layer={canvas.sortingLayerName}";
    }

    private static string DescribeCanvasScaler(CanvasScaler cs)
    {
        return $"CanvasScaler: mode={cs.uiScaleMode}, ref={V2(cs.referenceResolution)}, match={F(cs.matchWidthOrHeight)}";
    }

    private static string DescribeImage(Image img)
    {
        return $"Image: sprite={AssetName(img.sprite)}, color={Hex(img.color)}, type={img.type}, raycast={img.raycastTarget}";
    }

    private static string DescribeRawImage(RawImage ri)
    {
        return $"RawImage: tex={AssetName(ri.texture)}, color={Hex(ri.color)}, raycast={ri.raycastTarget}";
    }

    private static string DescribeTMP(TextMeshProUGUI tmp)
    {
        return $"TextMeshProUGUI: font={AssetName(tmp.font)}, text={Str(tmp.text)}, size={F(tmp.fontSize)}, color={Hex(tmp.color)}, align={tmp.alignment}, raycast={tmp.raycastTarget}";
    }

    private static string DescribeTMPText(TMP_Text tmt)
    {
        return $"TMP_Text: font={AssetName(tmt.font)}, text={Str(tmt.text)}, size={F(tmt.fontSize)}, color={Hex(tmt.color)}, align={tmt.alignment}";
    }

    private static string DescribeSpriteRenderer(SpriteRenderer sr)
    {
        return $"SpriteRenderer: sprite={AssetName(sr.sprite)}, color={Hex(sr.color)}, order={sr.sortingOrder}, layer={sr.sortingLayerName}";
    }

    // ===================== Formatters =====================

    private static string F(float f) => f.ToString("0.####", CultureInfo.InvariantCulture);
    private static string V2(Vector2 v) => $"{F(v.x)},{F(v.y)}";
    private static string V3(Vector3 v) => $"{F(v.x)},{F(v.y)},{F(v.z)}";

    private static string Hex(Color c)
    {
        var c32 = (Color32)c;
        return $"#{c32.r:X2}{c32.g:X2}{c32.b:X2}{c32.a:X2}";
    }

    private static string Str(string s)
    {
        if (s == null) return "null";
        // Cap long text to keep snapshots readable; diff still catches changes.
        const int cap = 80;
        var trimmed = s.Length > cap ? s.Substring(0, cap) + "…" : s;
        return "'" + trimmed.Replace("\n", "\\n").Replace("'", "\\'") + "'";
    }

    private static string AssetName(UnityEngine.Object obj)
    {
        if (obj == null) return "<null>";
        return obj.name;
    }

    // ===================== Diff =====================

    // Newline-normalize then equal-compare. Keeps Windows/Unix EOL differences from flagging drift.
    private static bool NormalizedEqual(string a, string b)
    {
        return Normalize(a) == Normalize(b);
    }

    private static string Normalize(string s) => s.Replace("\r\n", "\n").Replace("\r", "\n");

    private static (int line, string a, string b) FirstDiff(string committed, string current)
    {
        var aLines = Normalize(committed).Split('\n');
        var bLines = Normalize(current).Split('\n');
        int n = Math.Min(aLines.Length, bLines.Length);
        for (int i = 0; i < n; i++)
        {
            if (aLines[i] != bLines[i])
                return (i + 1, aLines[i], bLines[i]);
        }
        if (aLines.Length != bLines.Length)
            return (n + 1, aLines.Length > n ? aLines[n] : "<eof>", bLines.Length > n ? bLines[n] : "<eof>");
        return (0, "<identical>", "<identical>");
    }
}
