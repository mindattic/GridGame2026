using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using UnityEditor;
using UnityEngine;

/// <summary>
/// SERIALIZEDFIELDBAN - Guardrail that prevents new [SerializeField] usages from creeping in.
/// <para>PURPOSE: The project is being migrated off Inspector authoring. Every currently-known
/// [SerializeField] is frozen in an allowlist committed next to this file. New attributes that
/// aren't in the allowlist make Check() fail. As Phase 1 eliminates fields, the allowlist
/// shrinks via Regenerate().</para>
/// <para>INVARIANT: The allowlist is the upper bound on allowed fields, not a floor. Entries
/// that no longer exist in code are dropped on Regenerate but are not errors on Check.</para>
/// <para>USAGE: Called by CliEntryPoints.CheckSerializedFieldBan and
/// CliEntryPoints.RegenerateSerializedFieldAllowlist, driven from GridGame.ps1.</para>
/// <para>RELATED FILES: CliEntryPoints.cs, Assets/Editor/SerializedFieldAllowlist.txt</para>
/// </summary>
public static class SerializedFieldBan
{
    public const string ScanRoot = "Assets/Scripts";
    public const string AllowlistPath = "Assets/Editor/SerializedFieldAllowlist.txt";

    // Entry format: "relative/path/File.cs:fieldName" — sorted, unique, one per line.
    public readonly struct Entry : IComparable<Entry>, IEquatable<Entry>
    {
        public readonly string Path;
        public readonly string Field;
        public Entry(string path, string field) { Path = path; Field = field; }
        public override string ToString() => $"{Path}:{Field}";
        public int CompareTo(Entry other)
        {
            int c = string.CompareOrdinal(Path, other.Path);
            return c != 0 ? c : string.CompareOrdinal(Field, other.Field);
        }
        public bool Equals(Entry other) => Path == other.Path && Field == other.Field;
        public override bool Equals(object obj) => obj is Entry e && Equals(e);
        public override int GetHashCode() => (Path, Field).GetHashCode();
    }

    // ===================== Public API =====================

    /// <summary>Scans Assets/Scripts for every [SerializeField] field. Returns sorted, de-duplicated entries.</summary>
    public static List<Entry> ScanCurrent()
    {
        var results = new HashSet<Entry>();
        if (!Directory.Exists(ScanRoot))
        {
            Debug.LogError($"[SerializedFieldBan] Scan root missing: {ScanRoot}");
            return new List<Entry>();
        }

        foreach (var file in Directory.EnumerateFiles(ScanRoot, "*.cs", SearchOption.AllDirectories))
        {
            var rel = ToUnixPath(MakeRelative(file));
            foreach (var field in ExtractFields(File.ReadAllLines(file)))
                results.Add(new Entry(rel, field));
        }

        var list = results.ToList();
        list.Sort();
        return list;
    }

    /// <summary>Reads the committed allowlist. Returns empty list if the file doesn't exist.</summary>
    public static List<Entry> ReadAllowlist()
    {
        var list = new List<Entry>();
        if (!File.Exists(AllowlistPath)) return list;

        foreach (var raw in File.ReadAllLines(AllowlistPath))
        {
            var line = raw.Trim();
            if (line.Length == 0 || line.StartsWith("#")) continue;
            int colon = line.LastIndexOf(':');
            if (colon <= 0 || colon >= line.Length - 1) continue;
            list.Add(new Entry(line.Substring(0, colon), line.Substring(colon + 1)));
        }
        list.Sort();
        return list;
    }

    /// <summary>Overwrites the allowlist on disk with the given entries, sorted and de-duplicated.</summary>
    public static void WriteAllowlist(IEnumerable<Entry> entries)
    {
        var sorted = new SortedSet<Entry>(entries).ToList();
        var sb = new StringBuilder();
        sb.AppendLine("# SerializedFieldAllowlist — committed frozen set of known [SerializeField] fields.");
        sb.AppendLine("# Format: relative/path/File.cs:fieldName (one per line, sorted).");
        sb.AppendLine("# Do not hand-edit. Regenerate via CliEntryPoints.RegenerateSerializedFieldAllowlist");
        sb.AppendLine("# (after Phase 1 removes a field, or after explicitly approving a new one).");
        foreach (var e in sorted) sb.AppendLine(e.ToString());

        var dir = Path.GetDirectoryName(AllowlistPath);
        if (!string.IsNullOrEmpty(dir)) Directory.CreateDirectory(dir);
        File.WriteAllText(AllowlistPath, sb.ToString());
    }

    /// <summary>
    /// Compares current scan against allowlist. Logs offenders (new entries not in allowlist).
    /// Returns offender count. Zero = clean.
    /// </summary>
    public static int Check()
    {
        var current = ScanCurrent();
        var allowed = new HashSet<Entry>(ReadAllowlist());

        var offenders = current.Where(e => !allowed.Contains(e)).ToList();
        var missing = allowed.Where(a => !current.Contains(a)).ToList();

        Debug.Log($"[SerializedFieldBan] Scanned: {current.Count} field(s). Allowlist: {allowed.Count}.");

        if (missing.Count > 0)
        {
            Debug.Log($"[SerializedFieldBan] {missing.Count} allowlist entr{(missing.Count == 1 ? "y" : "ies")} no longer in code (OK — shrinking is good). Run Regenerate to trim.");
            foreach (var m in missing.Take(10))
                Debug.Log($"    shrink: {m}");
        }

        if (offenders.Count == 0)
        {
            Debug.Log("[SerializedFieldBan] OK — no new [SerializeField] fields detected.");
            return 0;
        }

        Debug.LogError($"[SerializedFieldBan] FAIL — {offenders.Count} new [SerializeField] field(s) not in allowlist:");
        foreach (var o in offenders)
            Debug.LogError($"    NEW: {o}");
        Debug.LogError($"[SerializedFieldBan] To allow these explicitly, run RegenerateSerializedFieldAllowlist. " +
                       $"But the point of Phase 0 is to REFUSE new ones.");
        return offenders.Count;
    }

    /// <summary>Overwrites the allowlist with the current scan. Returns the new entry count.</summary>
    public static int Regenerate()
    {
        var current = ScanCurrent();
        WriteAllowlist(current);
        Debug.Log($"[SerializedFieldBan] Regenerated allowlist: {current.Count} entr{(current.Count == 1 ? "y" : "ies")} written to {AllowlistPath}");
        return current.Count;
    }

    // ===================== Parser =====================

    // Matches any [Attribute...] block. Non-greedy so nested [...] don't merge.
    private static readonly Regex AttributeBlockRx = new Regex(@"\[[^\]]*\]", RegexOptions.Compiled);
    // Trailing identifier before the terminator.
    private static readonly Regex TrailingIdentifierRx = new Regex(@"([A-Za-z_]\w*)\s*$", RegexOptions.Compiled);

    /// <summary>
    /// Extracts the field name(s) for each [SerializeField] attribute in the file.
    /// Handles both same-line attribute+declaration and attribute-above-declaration forms,
    /// plus combined attributes like [SerializeField, HideInInspector] and [SerializeField][Range(...)].
    /// Multi-field declarations (int a, b;) are not currently split — only the trailing name is captured.
    /// </summary>
    public static IEnumerable<string> ExtractFields(string[] lines)
    {
        for (int i = 0; i < lines.Length; i++)
        {
            if (!ContainsSerializeField(lines[i])) continue;

            // Accumulate lines until we reach a declaration terminator. The terminator is
            // `;` or `=` at top-level (outside of string literals or brackets).
            var acc = new StringBuilder(lines[i]);
            int j = i;
            while (!HasDeclTerminator(acc.ToString()))
            {
                j++;
                if (j >= lines.Length) break;
                acc.Append(' ').Append(lines[j]);
            }
            var full = acc.ToString();

            // Strip attribute blocks so the remaining text is just the field declaration.
            var stripped = AttributeBlockRx.Replace(full, " ");

            // Find first real terminator in the stripped text.
            int term = FindDeclTerminator(stripped);
            if (term < 0) continue;

            var before = stripped.Substring(0, term);
            var m = TrailingIdentifierRx.Match(before.TrimEnd());
            if (m.Success)
            {
                var name = m.Groups[1].Value;
                // Skip common type-name false positives. Field declarations end with an identifier
                // preceded by a type, so if the last token is a C# keyword we're clearly in a
                // malformed match and should skip.
                if (!IsCSharpKeyword(name))
                    yield return name;
            }

            i = j; // jump past the accumulated declaration
        }
    }

    private static bool ContainsSerializeField(string line)
    {
        // Match [SerializeField] and [SerializeField, ...] and [SerializeField][...] forms.
        // Exclude property-targeted [field: SerializeField] only if we decide to (for now we treat
        // it the same; the captured identifier will be the auto-prop name which is fine).
        int idx = line.IndexOf("SerializeField", StringComparison.Ordinal);
        if (idx < 0) return false;
        // Require a preceding '[' somewhere on the line before the match.
        int bracket = line.LastIndexOf('[', idx);
        if (bracket < 0) return false;
        // Reject if the '[' sits inside a // single-line comment (including /// doc comments).
        int commentStart = FindLineCommentStart(line);
        if (commentStart >= 0 && bracket >= commentStart) return false;
        return true;
    }

    /// <summary>Returns the index of the first `//` outside string/char literals, or -1 if none.</summary>
    private static int FindLineCommentStart(string line)
    {
        bool inStr = false, inChar = false;
        for (int k = 0; k < line.Length - 1; k++)
        {
            char c = line[k];
            char prev = k > 0 ? line[k - 1] : '\0';
            if (inStr)
            {
                if (c == '"' && prev != '\\') inStr = false;
                continue;
            }
            if (inChar)
            {
                if (c == '\'' && prev != '\\') inChar = false;
                continue;
            }
            if (c == '"') { inStr = true; continue; }
            if (c == '\'') { inChar = true; continue; }
            if (c == '/' && line[k + 1] == '/') return k;
        }
        return -1;
    }

    private static bool HasDeclTerminator(string text) => FindDeclTerminator(text) >= 0;

    // Returns the index of `;` or `=` outside of any [...] bracket region and outside string/char literals.
    // This avoids terminating on attribute contents like [Tooltip("semicolons; in here")] or string defaults.
    private static int FindDeclTerminator(string text)
    {
        int depth = 0;
        bool inStr = false, inChar = false;
        char strDelim = '\0';
        for (int k = 0; k < text.Length; k++)
        {
            char c = text[k];
            char prev = k > 0 ? text[k - 1] : '\0';
            if (inStr)
            {
                if (c == strDelim && prev != '\\') inStr = false;
                continue;
            }
            if (inChar)
            {
                if (c == '\'' && prev != '\\') inChar = false;
                continue;
            }
            if (c == '"') { inStr = true; strDelim = '"'; continue; }
            if (c == '\'') { inChar = true; continue; }
            if (c == '[') depth++;
            else if (c == ']') depth = Math.Max(0, depth - 1);
            else if (depth == 0 && (c == ';' || c == '='))
                return k;
        }
        return -1;
    }

    private static bool IsCSharpKeyword(string name)
    {
        switch (name)
        {
            case "private": case "public": case "internal": case "protected":
            case "readonly": case "static": case "const": case "new":
            case "override": case "virtual": case "abstract": case "sealed":
            case "ref": case "in": case "out":
                return true;
            default:
                return false;
        }
    }

    private static string MakeRelative(string fullPath)
    {
        var projectRoot = Directory.GetCurrentDirectory();
        var full = Path.GetFullPath(fullPath);
        if (full.StartsWith(projectRoot, StringComparison.OrdinalIgnoreCase))
            return full.Substring(projectRoot.Length).TrimStart(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
        return fullPath;
    }

    private static string ToUnixPath(string path) => path.Replace('\\', '/');
}
