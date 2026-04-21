using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using UnityEditor;
using UnityEngine;

/// <summary>
/// RESOURCESLOADBAN - Phase 3 guardrail that prevents new Resources.Load usages from creeping in.
/// <para>PURPOSE: Phase 3 routes runtime asset loading through Addressables via
/// AssetHelper.LoadAsset&lt;T&gt;(address). Existing Resources.Load call-sites are frozen
/// in an allowlist committed next to this file. New occurrences make Check() fail.
/// As MapPropIO prefabs eventually move to Addressables, the allowlist shrinks via
/// Regenerate().</para>
/// <para>INVARIANT: The allowlist is the upper bound, not a floor. Entries that no
/// longer exist in code are dropped on Regenerate but are not errors on Check.</para>
/// <para>USAGE: Called by CliEntryPoints.CheckResourcesLoadBan and
/// CliEntryPoints.RegenerateResourcesLoadAllowlist, driven from GridGame.Console.ps1.</para>
/// <para>RELATED FILES: CliEntryPoints.cs, Assets/Editor/ResourcesLoadAllowlist.txt, SerializedFieldBan.cs</para>
/// </summary>
public static class ResourcesLoadBan
{
    public const string ScanRoot = "Assets/Scripts";
    public const string AllowlistPath = "Assets/Editor/ResourcesLoadAllowlist.txt";

    // Matches Resources.Load, Resources.LoadAsync, and Resources.LoadAll.
    // Lead-in \b rejects prefixes like MyResources.Load or `.Resources.Load` inside strings.
    private static readonly Regex ResourcesLoadRx = new Regex(
        @"\bResources\.Load(All|Async)?\s*(<|\()", RegexOptions.Compiled);

    // Entry format: "relative/path/File.cs" — file-level granularity. One call-site per line
    // would be brittle (line numbers shift on every edit). File-level means adding a second
    // Resources.Load in an already-allowed file is OK; introducing the first one in a new
    // file is flagged. Combined with the existing no-Inspector rule, this is the effective
    // upper bound on legacy asset loading.
    public readonly struct Entry : IComparable<Entry>, IEquatable<Entry>
    {
        public readonly string Path;
        public Entry(string path) { Path = path; }
        public override string ToString() => Path;
        public int CompareTo(Entry other) => string.CompareOrdinal(Path, other.Path);
        public bool Equals(Entry other) => Path == other.Path;
        public override bool Equals(object obj) => obj is Entry e && Equals(e);
        public override int GetHashCode() => Path?.GetHashCode() ?? 0;
    }

    // ===================== Public API =====================

    /// <summary>Scans Assets/Scripts for every file containing a Resources.Load call. Returns sorted, unique paths.</summary>
    public static List<Entry> ScanCurrent()
    {
        var results = new HashSet<Entry>();
        if (!Directory.Exists(ScanRoot))
        {
            Debug.LogError($"[ResourcesLoadBan] Scan root missing: {ScanRoot}");
            return new List<Entry>();
        }

        foreach (var file in Directory.EnumerateFiles(ScanRoot, "*.cs", SearchOption.AllDirectories))
        {
            var lines = File.ReadAllLines(file);
            if (FileHasResourcesLoad(lines))
                results.Add(new Entry(ToUnixPath(MakeRelative(file))));
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
            list.Add(new Entry(line));
        }
        list.Sort();
        return list;
    }

    /// <summary>Overwrites the allowlist on disk with the given entries, sorted and de-duplicated.</summary>
    public static void WriteAllowlist(IEnumerable<Entry> entries)
    {
        var sorted = new SortedSet<Entry>(entries).ToList();
        var sb = new StringBuilder();
        sb.AppendLine("# ResourcesLoadAllowlist — committed frozen set of files containing Resources.Load calls.");
        sb.AppendLine("# Format: relative/path/File.cs (one per line, sorted).");
        sb.AppendLine("# Do not hand-edit. Regenerate via CliEntryPoints.RegenerateResourcesLoadAllowlist");
        sb.AppendLine("# (after migrating a file's Resources.Load calls to AssetHelper.LoadAsset).");
        foreach (var e in sorted) sb.AppendLine(e.ToString());

        var dir = Path.GetDirectoryName(AllowlistPath);
        if (!string.IsNullOrEmpty(dir)) Directory.CreateDirectory(dir);
        File.WriteAllText(AllowlistPath, sb.ToString());
    }

    /// <summary>
    /// Compares current scan against allowlist. Logs offenders (new files not in allowlist).
    /// Returns offender count. Zero = clean.
    /// </summary>
    public static int Check()
    {
        var current = ScanCurrent();
        var allowed = new HashSet<Entry>(ReadAllowlist());

        var offenders = current.Where(e => !allowed.Contains(e)).ToList();
        var missing = allowed.Where(a => !current.Contains(a)).ToList();

        Debug.Log($"[ResourcesLoadBan] Scanned: {current.Count} file(s) with Resources.Load. Allowlist: {allowed.Count}.");

        if (missing.Count > 0)
        {
            Debug.Log($"[ResourcesLoadBan] {missing.Count} allowlist entr{(missing.Count == 1 ? "y" : "ies")} no longer contain Resources.Load (OK — shrinking is good). Run Regenerate to trim.");
            foreach (var m in missing.Take(10))
                Debug.Log($"    shrink: {m}");
        }

        if (offenders.Count == 0)
        {
            Debug.Log("[ResourcesLoadBan] OK — no new Resources.Load call-sites detected. Route runtime asset loads through AssetHelper.LoadAsset<T>(address).");
            return 0;
        }

        Debug.LogError($"[ResourcesLoadBan] FAIL — {offenders.Count} new file(s) contain Resources.Load:");
        foreach (var o in offenders)
            Debug.LogError($"    NEW: {o}");
        Debug.LogError("[ResourcesLoadBan] Phase 3 routes runtime asset loads through Addressables. " +
                       "Replace Resources.Load<T>(path) with AssetHelper.LoadAsset<T>(address) " +
                       "after registering the asset in Window > Asset Management > Addressables > Groups.");
        return offenders.Count;
    }

    /// <summary>Overwrites the allowlist with the current scan. Returns the new entry count.</summary>
    public static int Regenerate()
    {
        var current = ScanCurrent();
        WriteAllowlist(current);
        Debug.Log($"[ResourcesLoadBan] Regenerated allowlist: {current.Count} entr{(current.Count == 1 ? "y" : "ies")} written to {AllowlistPath}");
        return current.Count;
    }

    // ===================== Parser =====================

    /// <summary>
    /// Returns true if the file contains at least one Resources.Load call that is not
    /// inside a // line comment. (Multi-line /* */ comments are rare in this codebase;
    /// the comment-awareness matches SerializedFieldBan's pragmatic line-scan approach.)
    /// </summary>
    private static bool FileHasResourcesLoad(string[] lines)
    {
        foreach (var line in lines)
        {
            var match = ResourcesLoadRx.Match(line);
            if (!match.Success) continue;

            int commentStart = FindLineCommentStart(line);
            if (commentStart >= 0 && match.Index >= commentStart) continue;

            return true;
        }
        return false;
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
