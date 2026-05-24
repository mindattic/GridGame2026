using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// BUILDERDRIFTCHECKER - Captures canonical per-scene signatures and flags drift between
/// what a scene's builder produces and what the .unity on disk contains.
/// <para>SOURCE OF TRUTH: Builders are authoritative. By default Verify() and Regenerate()
/// invoke each scene's builder in-memory before signaturing, so the signature reflects what
/// the builder *would produce* — not what someone may have hand-edited into the .unity.
/// The legacy "signature from .unity on disk" path is available via <c>fromBuilders: false</c>
/// for debugging the checker itself; production callers should use the default.</para>
/// <para>SIGNATURE FORMAT (v2): A header line "# signature_version: 2" followed by one line per
/// object. "[Name @index]" denotes a GameObject (index appears only under LayoutGroup parents
/// where sibling order is visually meaningful). Indented below it are its components, one per
/// line, in stable alphabetical order. Unknown MonoBehaviours emit their [SerializeField] +
/// public field values via reflection so changes to project-owned components are visible. Only
/// structural fields that builders typically set are recorded — fileIDs, GUIDs, and most
/// internal Unity state is excluded.</para>
/// <para>COMPARISON: Lines starting with '#' are treated as comments and stripped before equality
/// check, so renaming the header doesn't create false drift.</para>
/// <para>ARTIFACTS: On drift, writes <c>&lt;Scene&gt;.actual.txt</c> (current signature) next to
/// the committed snapshot. On clean verify the artifact is removed. Diff output lists *all*
/// divergent lines, not just the first.</para>
/// <para>USAGE: Regenerate snapshots when builders legitimately change; Verify on every
/// builder run / commit to catch drift. Both paths are routed through CliEntryPoints.</para>
/// <para>RELATED FILES: CliEntryPoints.cs, SceneBuilderHelper.cs, BuilderAutoRebuild.cs,
/// Documentation/Builders/Drift/</para>
/// </summary>
public static class BuilderDriftChecker
{
    public const string SnapshotRoot = "Documentation/Builders/Drift";
    public const int SignatureVersion = 2;

    // ===================== Public API =====================

    /// <summary>
    /// For each scene, opens it, invokes its builder in-memory (when fromBuilders=true), saves
    /// the rebuilt .unity, and writes <c>Documentation/Builders/Drift/&lt;Scene&gt;.snapshot.txt</c>.
    /// Returns the number of scenes written successfully.
    /// </summary>
    /// <param name="fromBuilders">When true (default), rebuilds each scene from its <c>*Builder.Build()</c>
    /// before signaturing — closes the "hand-edit + bless as canonical" loophole. When false,
    /// signatures whatever the .unity on disk contains (debug-only).</param>
    public static int Regenerate(IEnumerable<string> scenes, bool fromBuilders = true)
    {
        Directory.CreateDirectory(SnapshotRoot);
        int ok = 0;
        foreach (var scene in scenes)
        {
            if (!OpenSceneSafe(scene)) continue;

            if (fromBuilders)
            {
                try { RebuildActiveSceneInMemory(scene); }
                catch (Exception e)
                {
                    Debug.LogError($"[BuilderDrift] {scene}: builder invocation failed: {e.GetType().Name}: {e.Message}");
                    continue;
                }
                var active = EditorSceneManager.GetActiveScene();
                EditorSceneManager.MarkSceneDirty(active);
                EditorSceneManager.SaveScene(active, $"Assets/Scenes/{scene}.unity");
            }

            File.WriteAllText(SnapshotPath(scene), BuildSignature(scene));
            Debug.Log($"[BuilderDrift] Wrote snapshot: {SnapshotPath(scene)}");
            ok++;
        }
        return ok;
    }

    /// <summary>
    /// For each scene, opens it, invokes its builder in-memory (when fromBuilders=true), computes
    /// the signature, and diffs against the committed snapshot. Returns the number of scenes with
    /// drift (0 = clean). Missing snapshots count as drift. On drift, writes
    /// <c>&lt;Scene&gt;.actual.txt</c>; on pass, removes any prior .actual.txt for the scene so the
    /// working tree stays clean.
    /// </summary>
    /// <param name="fromBuilders">When true (default), rebuilds each scene from its
    /// <c>*Builder.Build()</c> before signaturing — the only path that catches drift between
    /// builder code and the committed .unity. The in-memory rebuild is NOT saved to disk.
    /// When false, signatures the .unity on disk as-is (debug-only).</param>
    public static int Verify(IEnumerable<string> scenes, bool fromBuilders = true)
    {
        int drifted = 0;
        foreach (var scene in scenes)
        {
            // Clean up artifacts from prior runs before this scene's verify — passing scenes
            // self-clean, drifting ones rewrite a fresh artifact below.
            DeleteIfExists(ActualPath(scene));
            DeleteIfExists(DiffPath(scene));

            if (!OpenSceneSafe(scene)) { drifted++; continue; }

            if (fromBuilders)
            {
                try { RebuildActiveSceneInMemory(scene); }
                catch (Exception e)
                {
                    Debug.LogError($"[BuilderDrift] {scene}: builder invocation failed: {e.GetType().Name}: {e.Message}");
                    drifted++;
                    continue;
                }
            }

            var current = BuildSignature(scene);
            var snapshotPath = SnapshotPath(scene);
            if (!File.Exists(snapshotPath))
            {
                Debug.LogError($"[BuilderDrift] {scene}: no snapshot at {snapshotPath}. Run RegenerateBuilderSnapshots to create.");
                drifted++;
                continue;
            }
            var committed = File.ReadAllText(snapshotPath);
            if (!NormalizedEqual(current, committed))
            {
                drifted++;
                ReportDrift(scene, committed, current);
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

    /// <summary>
    /// Builds the signature for whatever scene is currently active. Public so other tools
    /// (e.g. BuilderAutoRebuild's post-rebuild verification) can compare without going through
    /// the full Verify loop.
    /// </summary>
    public static string SignatureOfActiveScene()
    {
        var scene = UnityEngine.SceneManagement.SceneManager.GetActiveScene();
        return BuildSignature(scene.name);
    }

    /// <summary>Reads the committed snapshot for a scene, or returns null if missing.</summary>
    public static string ReadCommittedSnapshot(string sceneName)
    {
        var path = SnapshotPath(sceneName);
        return File.Exists(path) ? File.ReadAllText(path) : null;
    }

    /// <summary>Strips comments + normalizes EOL and compares two signature strings.</summary>
    public static bool SignaturesEqual(string a, string b) => NormalizedEqual(a, b);

    // ===================== Scene Loading & Rebuild =====================

    private static bool OpenSceneSafe(string sceneName)
    {
        var path = $"Assets/Scenes/{sceneName}.unity";
        if (!File.Exists(path))
        {
            Debug.LogError($"[BuilderDrift] Scene file missing: {path}");
            return false;
        }
        // OpenSceneMode.Single discards any previously-loaded scene's in-memory state. In
        // batchmode this is silent; in interactive mode it relies on the caller (CliEntryPoints)
        // running in a context where that's acceptable.
        EditorSceneManager.OpenScene(path, OpenSceneMode.Single);
        return true;
    }

    private static void RebuildActiveSceneInMemory(string sceneName)
    {
        // Clear all roots, then invoke <SceneName>Builder.Build(). Identical to the flow in
        // BuilderAutoRebuild.RebuildScene minus the SaveScene step, so Verify() can rebuild
        // without touching disk.
        var roots = UnityEngine.SceneManagement.SceneManager.GetActiveScene().GetRootGameObjects();
        foreach (var go in roots) UnityEngine.Object.DestroyImmediate(go);

        var builderType = AppDomain.CurrentDomain.GetAssemblies()
            .SelectMany(SafeGetTypes)
            .FirstOrDefault(t => t.Name == sceneName + "Builder");
        if (builderType == null)
            throw new Exception($"{sceneName}Builder type not found");

        var build = builderType.GetMethod("Build", BindingFlags.Public | BindingFlags.Static);
        if (build == null)
            throw new Exception($"{sceneName}Builder.Build() not found");

        build.Invoke(null, null);
    }

    private static IEnumerable<Type> SafeGetTypes(Assembly a)
    {
        try { return a.GetTypes(); }
        catch (ReflectionTypeLoadException e) { return e.Types.Where(t => t != null); }
    }

    private static string SnapshotPath(string sceneName)
        => Path.Combine(SnapshotRoot, $"{sceneName}.snapshot.txt").Replace('\\', '/');

    private static string ActualPath(string sceneName)
        => Path.Combine(SnapshotRoot, $"{sceneName}.actual.txt").Replace('\\', '/');

    private static string DiffPath(string sceneName)
        => Path.Combine(SnapshotRoot, $"{sceneName}.diff.txt").Replace('\\', '/');

    private static void DeleteIfExists(string path)
    {
        if (File.Exists(path)) File.Delete(path);
    }

    // ===================== Signature Builder =====================

    private static string BuildSignature(string sceneName)
    {
        var sb = new StringBuilder();
        sb.Append("# signature_version: ").Append(SignatureVersion).Append('\n');
        sb.Append("# scene: ").Append(sceneName).Append('\n');
        sb.Append("# regenerate via: CliEntryPoints.RegenerateBuilderSnapshots\n");
        sb.Append("# comments (lines starting with #) are stripped before comparison\n");
        sb.Append('\n');

        var scene = UnityEngine.SceneManagement.SceneManager.GetActiveScene();
        var roots = scene.GetRootGameObjects();
        Array.Sort(roots, (a, b) => string.CompareOrdinal(a.name, b.name));
        // Root level has no LayoutGroup parent, so sort-by-name is fine here.
        foreach (var go in roots)
            WriteGameObject(sb, go, depth: 0, parentHasLayoutGroup: false, siblingIndex: -1);
        return sb.ToString();
    }

    private static void WriteGameObject(StringBuilder sb, GameObject go, int depth, bool parentHasLayoutGroup, int siblingIndex)
    {
        Indent(sb, depth);
        sb.Append('[').Append(go.name);
        // Include sibling index when the parent uses a LayoutGroup so row reordering is visible
        // to drift. Without this, sorting children alphabetically erases meaningful order.
        if (parentHasLayoutGroup && siblingIndex >= 0)
            sb.Append(" @").Append(siblingIndex);
        sb.Append(']');
        if (!go.activeSelf) sb.Append(" (inactive)");
        var layerName = LayerMask.LayerToName(go.layer);
        if (!string.IsNullOrEmpty(layerName) && layerName != "Default")
            sb.Append(" layer=").Append(layerName);
        if (go.tag != "Untagged") sb.Append(" tag=").Append(go.tag);
        sb.Append('\n');

        // Components — stable alphabetical order by signature string.
        var components = go.GetComponents<Component>();
        var lines = new List<string>(components.Length);
        foreach (var c in components)
        {
            if (c == null) { lines.Add("<missing script>"); continue; }
            var line = DescribeComponent(c);
            if (!string.IsNullOrEmpty(line)) lines.Add(line);
        }
        lines.Sort(StringComparer.Ordinal);
        foreach (var line in lines)
        {
            Indent(sb, depth);
            sb.Append("  ").Append(line).Append('\n');
        }

        // Children — alphabetical for layout-agnostic parents, sibling-index for LayoutGroup parents.
        bool thisHasLayoutGroup = go.GetComponent<LayoutGroup>() != null;
        var children = new List<Transform>(go.transform.childCount);
        for (int i = 0; i < go.transform.childCount; i++)
            children.Add(go.transform.GetChild(i));

        if (thisHasLayoutGroup)
        {
            children.Sort((a, b) => a.GetSiblingIndex().CompareTo(b.GetSiblingIndex()));
        }
        else
        {
            children.Sort((a, b) =>
            {
                int c = string.CompareOrdinal(a.name, b.name);
                return c != 0 ? c : a.GetSiblingIndex().CompareTo(b.GetSiblingIndex());
            });
        }

        foreach (var child in children)
        {
            WriteGameObject(sb, child.gameObject, depth + 1,
                parentHasLayoutGroup: thisHasLayoutGroup,
                siblingIndex: thisHasLayoutGroup ? child.GetSiblingIndex() : -1);
        }
    }

    private static void Indent(StringBuilder sb, int depth)
    {
        for (int i = 0; i < depth; i++) sb.Append("  ");
    }

    // ===================== Component Descriptions =====================

    // Describes a component as a stable, one-line signature. Returns null for components
    // that should be completely omitted. Unknown MonoBehaviours emit reflected [SerializeField]
    // + public field values so changes to project-owned components are visible to drift.
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
            case MonoBehaviour mb: return DescribeMonoBehaviourViaReflection(mb);
            default: return c.GetType().Name;
        }
    }

    private static string DescribeTransform(Transform t)
    {
        // localRotation as quaternion avoids the 359.999 ≈ 0.001 textual aliasing of euler angles.
        var r = t.localRotation;
        return $"Transform: pos={V3(t.localPosition)}, rotQ=({F(r.x)},{F(r.y)},{F(r.z)},{F(r.w)}), scale={V3(t.localScale)}";
    }

    private static string DescribeRect(RectTransform rt)
    {
        var r = rt.localRotation;
        return $"RectTransform: anchor=({V2(rt.anchorMin)})-({V2(rt.anchorMax)}), pos={V2(rt.anchoredPosition)}, size={V2(rt.sizeDelta)}, pivot={V2(rt.pivot)}, rotQ=({F(r.x)},{F(r.y)},{F(r.z)},{F(r.w)}), scale={V3(rt.localScale)}";
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

    // Reflects over a project-owned MonoBehaviour and emits `field=value` pairs for any
    // [SerializeField]-tagged or public instance field whose value is formattable. Skips
    // fields whose type yields noise (cross-scene references, fileID-bearing references).
    // This makes project components first-class drift sources — without this, the descriptor
    // switch silently swallows every change to project code.
    private static string DescribeMonoBehaviourViaReflection(MonoBehaviour mb)
    {
        var type = mb.GetType();
        var fields = type
            .GetFields(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance)
            .Where(f => !f.IsStatic)
            .Where(f => !f.IsDefined(typeof(NonSerializedAttribute), inherit: false))
            .Where(f => f.IsPublic || f.IsDefined(typeof(SerializeField), inherit: false))
            .OrderBy(f => f.Name, StringComparer.Ordinal)
            .ToList();

        if (fields.Count == 0) return type.Name;

        var parts = new List<string>(fields.Count);
        foreach (var f in fields)
        {
            object value;
            try { value = f.GetValue(mb); }
            catch { continue; }
            var formatted = FormatReflectedValue(value);
            if (formatted == null) continue;
            parts.Add($"{f.Name}={formatted}");
        }

        return parts.Count == 0 ? type.Name : $"{type.Name}: {string.Join(", ", parts)}";
    }

    private static string FormatReflectedValue(object v)
    {
        if (v == null) return "<null>";
        switch (v)
        {
            case bool b: return b ? "True" : "False";
            case string s: return Str(s);
            case float f: return F(f);
            case double d: return ((float)d).ToString("R", CultureInfo.InvariantCulture);
            case int i: return i.ToString(CultureInfo.InvariantCulture);
            case long l: return l.ToString(CultureInfo.InvariantCulture);
            case Enum e: return e.ToString();
            case Vector2 v2: return $"({V2(v2)})";
            case Vector3 v3: return $"({V3(v3)})";
            case Vector4 v4: return $"({F(v4.x)},{F(v4.y)},{F(v4.z)},{F(v4.w)})";
            case Quaternion q: return $"({F(q.x)},{F(q.y)},{F(q.z)},{F(q.w)})";
            case Color c: return Hex(c);
            case Color32 c32: return Hex(c32);
            case Rect r: return $"({F(r.x)},{F(r.y)},{F(r.width)},{F(r.height)})";
            case LayerMask lm: return lm.value.ToString(CultureInfo.InvariantCulture);
            case GameObject go: return $"→{(go == null ? "<null>" : go.name)}";
            case Component comp: return $"→{(comp == null ? "<null>" : comp.name)}";
            case UnityEngine.Object obj: return $"→{(obj == null ? "<null>" : obj.name)}";
        }
        if (v is IList list)
        {
            // Bound noise — emit count plus first few formatted elements.
            const int peek = 3;
            var head = new List<string>(peek);
            for (int i = 0; i < list.Count && i < peek; i++)
            {
                var item = FormatReflectedValue(list[i]);
                head.Add(item ?? "<skip>");
            }
            var suffix = list.Count > peek ? ",…" : "";
            return $"[count={list.Count}: {string.Join(",", head)}{suffix}]";
        }
        // Unknown type — skip rather than emit noise like `System.RuntimeType: …`.
        return null;
    }

    // ===================== Formatters =====================

    // "R" = round-trip — preserves full precision so sub-1e-4 changes don't vanish from the signature.
    private static string F(float f) => f.ToString("R", CultureInfo.InvariantCulture);
    private static string V2(Vector2 v) => $"{F(v.x)},{F(v.y)}";
    private static string V3(Vector3 v) => $"{F(v.x)},{F(v.y)},{F(v.z)}";

    private static string Hex(Color c)
    {
        var c32 = (Color32)c;
        return $"#{c32.r:X2}{c32.g:X2}{c32.b:X2}{c32.a:X2}";
    }

    private static string Hex(Color32 c32)
    {
        return $"#{c32.r:X2}{c32.g:X2}{c32.b:X2}{c32.a:X2}";
    }

    private static string Str(string s)
    {
        if (s == null) return "null";
        // Cap long text to keep snapshots readable; diff still catches changes up to the cap.
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

    // Newline-normalize, strip comment lines (^#), and equal-compare. Keeps Windows/Unix EOL
    // differences AND header renames from flagging drift.
    private static bool NormalizedEqual(string a, string b)
    {
        return Normalize(a) == Normalize(b);
    }

    private static string Normalize(string s)
    {
        if (s == null) return "";
        var lf = s.Replace("\r\n", "\n").Replace("\r", "\n");
        var lines = lf.Split('\n');
        var sb = new StringBuilder(lf.Length);
        foreach (var line in lines)
        {
            // Strip lines starting with # (after optional leading whitespace). These are
            // informational headers — renaming/restructuring them must NEVER create drift.
            var trimmed = line.TrimStart();
            if (trimmed.StartsWith("#")) continue;
            sb.Append(line).Append('\n');
        }
        return sb.ToString();
    }

    private static void ReportDrift(string scene, string committed, string current)
    {
        var actualPath = ActualPath(scene);
        var diffPath = DiffPath(scene);

        File.WriteAllText(actualPath, current);

        var (count, summaryLines, unified) = ComputeDiff(committed, current);
        File.WriteAllText(diffPath, unified);

        var summary = string.Join("\n    ", summaryLines);
        Debug.LogError(
            $"[BuilderDrift] {scene}: DRIFT — {count} differing line(s).\n" +
            $"    {summary}\n" +
            $"  Full current signature: {actualPath}\n" +
            $"  Full diff:              {diffPath}\n" +
            $"  Inspect with:           git diff --no-index \"{SnapshotPath(scene)}\" \"{actualPath}\"");
    }

    // Returns (differing-line count, first/last N summary lines for the log, full unified-style diff text).
    // The summary list shows up to the first 5 and last 5 differing lines with their line numbers.
    private static (int count, List<string> summary, string unified) ComputeDiff(string committed, string current)
    {
        var aLines = Normalize(committed).Split('\n');
        var bLines = Normalize(current).Split('\n');
        int max = Math.Max(aLines.Length, bLines.Length);

        var diffs = new List<(int line, string a, string b)>();
        for (int i = 0; i < max; i++)
        {
            string aLine = i < aLines.Length ? aLines[i] : "<eof>";
            string bLine = i < bLines.Length ? bLines[i] : "<eof>";
            if (aLine != bLine)
                diffs.Add((i + 1, aLine, bLine));
        }

        const int head = 5;
        const int tail = 5;
        var summary = new List<string>();
        for (int i = 0; i < Math.Min(head, diffs.Count); i++)
        {
            var d = diffs[i];
            summary.Add($"L{d.line,4}: - {d.a}");
            summary.Add($"      + {d.b}");
        }
        if (diffs.Count > head + tail)
            summary.Add($"… {diffs.Count - head - tail} more diff(s) elided; see .diff.txt for full output …");
        for (int i = Math.Max(diffs.Count - tail, head); i < diffs.Count; i++)
        {
            var d = diffs[i];
            summary.Add($"L{d.line,4}: - {d.a}");
            summary.Add($"      + {d.b}");
        }

        var sb = new StringBuilder();
        sb.Append("# unified-style diff: '-' = committed snapshot, '+' = current signature\n");
        sb.Append("# ").Append(diffs.Count).Append(" differing line(s)\n");
        foreach (var d in diffs)
        {
            sb.Append("L").Append(d.line).Append(":\n");
            sb.Append("- ").Append(d.a).Append('\n');
            sb.Append("+ ").Append(d.b).Append('\n');
        }
        return (diffs.Count, summary, sb.ToString());
    }
}
