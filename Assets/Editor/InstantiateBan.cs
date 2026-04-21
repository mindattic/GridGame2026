using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using UnityEditor;
using UnityEngine;

/// <summary>
/// INSTANTIATEBAN - Phase 4 guardrail that confines Object.Instantiate calls to Factory classes.
/// <para>PURPOSE: The project's convention is that every runtime GameObject is built by a
/// *Factory.cs class (ActorFactory, HubItemRowFactory, TileFactory, etc.). Scattered
/// Instantiate(prefab) calls in managers/instances/UI code tie runtime construction to
/// binary .prefab assets and defeat the code-authoring workflow. This scanner ensures
/// new Instantiate call-sites land in a Factory — or nowhere.</para>
/// <para>BEHAVIOR: Files ending in `Factory.cs` are excluded from the scan (they are the
/// sanctioned instantiators). Any other file under Assets/Scripts containing
/// Instantiate is flagged unless explicitly allowlisted. Legitimate legacy exceptions
/// (prefab-bound systems like MapPropEditor's prop loader, VisualEffectInstance's VFX
/// clone) are frozen on InstantiateAllowlist.txt; new offenders fail Check().</para>
/// <para>USAGE: CliEntryPoints.CheckInstantiateBan and
/// CliEntryPoints.RegenerateInstantiateAllowlist, driven from GridGame.ps1.</para>
/// <para>RELATED FILES: CliEntryPoints.cs, Assets/Editor/InstantiateAllowlist.txt,
/// SerializedFieldBan.cs, ResourcesLoadBan.cs</para>
/// </summary>
public static class InstantiateBan
{
    public const string ScanRoot = "Assets/Scripts";
    public const string AllowlistPath = "Assets/Editor/InstantiateAllowlist.txt";

    // Matches Instantiate(, Object.Instantiate(, UnityEngine.Object.Instantiate(, MonoBehaviour.Instantiate(.
    // The leading word-boundary is essential so "MyReInstantiate(" doesn't match.
    // The trailing ( ensures we only count calls, not identifier mentions ("Instantiate method").
    private static readonly Regex InstantiateCallRx = new Regex(
        @"\bInstantiate\s*\(", RegexOptions.Compiled);

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

    /// <summary>
    /// Scans Assets/Scripts for every non-Factory file containing an Instantiate call.
    /// Files ending in "Factory.cs" are skipped — they are the sanctioned instantiators.
    /// </summary>
    public static List<Entry> ScanCurrent()
    {
        var results = new HashSet<Entry>();
        if (!Directory.Exists(ScanRoot))
        {
            Debug.LogError($"[InstantiateBan] Scan root missing: {ScanRoot}");
            return new List<Entry>();
        }

        foreach (var file in Directory.EnumerateFiles(ScanRoot, "*.cs", SearchOption.AllDirectories))
        {
            if (IsFactoryFile(file)) continue;
            var lines = File.ReadAllLines(file);
            if (FileHasInstantiate(lines))
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
        sb.AppendLine("# InstantiateAllowlist — committed frozen set of non-Factory files that call Instantiate.");
        sb.AppendLine("# Format: relative/path/File.cs (one per line, sorted).");
        sb.AppendLine("# *Factory.cs files are auto-excluded from the scan — not listed here.");
        sb.AppendLine("# Do not hand-edit. Regenerate via CliEntryPoints.RegenerateInstantiateAllowlist");
        sb.AppendLine("# (after migrating a file's Instantiate calls into a Factory).");
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

        Debug.Log($"[InstantiateBan] Scanned: {current.Count} non-Factory file(s) with Instantiate. Allowlist: {allowed.Count}.");

        if (missing.Count > 0)
        {
            Debug.Log($"[InstantiateBan] {missing.Count} allowlist entr{(missing.Count == 1 ? "y" : "ies")} no longer contain Instantiate (OK — shrinking is good). Run Regenerate to trim.");
            foreach (var m in missing.Take(10))
                Debug.Log($"    shrink: {m}");
        }

        if (offenders.Count == 0)
        {
            Debug.Log("[InstantiateBan] OK — no new Instantiate call-sites outside Factory classes. Route runtime GameObject construction through a *Factory.cs.");
            return 0;
        }

        Debug.LogError($"[InstantiateBan] FAIL — {offenders.Count} new non-Factory file(s) contain Instantiate:");
        foreach (var o in offenders)
            Debug.LogError($"    NEW: {o}");
        Debug.LogError("[InstantiateBan] Phase 4 confines GameObject construction to Factory classes. " +
                       "Either move the Instantiate call into a new or existing *Factory.cs, " +
                       "or replace it with ActorFactory.Create(...) / HubItemRowFactory.Create(...) / etc.");
        return offenders.Count;
    }

    /// <summary>Overwrites the allowlist with the current scan. Returns the new entry count.</summary>
    public static int Regenerate()
    {
        var current = ScanCurrent();
        WriteAllowlist(current);
        Debug.Log($"[InstantiateBan] Regenerated allowlist: {current.Count} entr{(current.Count == 1 ? "y" : "ies")} written to {AllowlistPath}");
        return current.Count;
    }

    // ===================== Parser =====================

    /// <summary>
    /// Factories are the sanctioned instantiators. Any file whose name ends in
    /// "Factory.cs" is implicitly allowed; we don't emit them to the allowlist
    /// and they don't appear in Check offender counts.
    /// </summary>
    private static bool IsFactoryFile(string fullPath)
    {
        var name = Path.GetFileName(fullPath);
        return name.EndsWith("Factory.cs", StringComparison.Ordinal);
    }

    /// <summary>
    /// Returns true if the file contains at least one Instantiate() call that is
    /// not inside a // line comment. Comment-awareness matches SerializedFieldBan's
    /// pragmatic line-scan approach.
    /// </summary>
    private static bool FileHasInstantiate(string[] lines)
    {
        foreach (var line in lines)
        {
            var match = InstantiateCallRx.Match(line);
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
