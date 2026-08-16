#if UNITY_EDITOR
using System.Collections.Generic;
using System.IO;
using System.Linq;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.TextCore;
#if UNITY_2020_2_OR_NEWER
using UnityEditor.AddressableAssets;
using UnityEditor.AddressableAssets.Settings;
#endif

/// <summary>
/// COMBATFEEDSPRITEASSETAUTHOR - Builds the TMP Sprite Asset that lets battle text render
/// inline icons via &lt;sprite name="..."&gt; tags (US-133 / GG-A5).
///
/// <para>PIPELINE: (1) gap-fill 64×64 status glyphs (one per Buff id — Poisoned, Burning, …)
/// in the same two-letter placeholder style as the tag icons; (2) pack every spell icon,
/// actor-tag icon, and status glyph into ONE atlas PNG (glyph name = source file name);
/// (3) author a <see cref="TMP_SpriteAsset"/> over the atlas (glyph + character tables,
/// TextMeshPro/Sprite material sub-asset) and register it as Addressable
/// <c>"CombatFeedIcons"</c> for <c>CombatFeed</c>/<c>AnnouncementWindow</c> to consume.</para>
///
/// <para>Deterministic and re-runnable: source PNGs are gap-filled only (hand art is never
/// overwritten); the atlas + sprite asset are REGENERATED every run (they are derived
/// artifacts, not authored art). Re-run after adding spells or statuses.</para>
///
/// <para>Menu: <c>Tools/Sprites/Author Combat Feed Sprite Asset</c>. Batch:
/// <c>-executeMethod CombatFeedSpriteAssetAuthor.Author</c>.</para>
/// </summary>
public static class CombatFeedSpriteAssetAuthor
{
    private const int Cell = 64;
    private const int Columns = 8;

    private const string StatusFolder = "Assets/Sprites/Status";
    private const string OutFolder = "Assets/Sprites/UI";
    private const string AtlasPath = OutFolder + "/CombatFeedIcons.png";
    private const string AssetPath = OutFolder + "/CombatFeedIcons.asset";
    public const string Address = "CombatFeedIcons";

    // One glyph per Buff id (Data/Buffs.cs) so feed lines can tag the exact status by name.
    // Names are the LOWERCASE buff ids — announce lines interpolate buff.Id ("Rogue is
    // poisoned"), and TMP sprite-name lookup is case-sensitive.
    private static readonly (string name, string code, Color color)[] StatusGlyphs =
    {
        ("protection", "PR", new Color(0.55f, 0.75f, 0.95f)),
        ("burning",    "BU", new Color(0.95f, 0.45f, 0.15f)),
        ("frozen",     "FZ", new Color(0.55f, 0.85f, 0.95f)),
        ("wet",        "WE", new Color(0.30f, 0.55f, 0.90f)),
        ("warm",       "WA", new Color(0.95f, 0.70f, 0.40f)),
        ("poisoned",   "PO", new Color(0.55f, 0.80f, 0.30f)),
        ("slowed",     "SL", new Color(0.60f, 0.55f, 0.80f)),
        ("silenced",   "SI", new Color(0.75f, 0.75f, 0.80f)),
        ("blinded",    "BL", new Color(0.35f, 0.32f, 0.40f)),
        ("sleep",      "ZZ", new Color(0.70f, 0.65f, 0.95f)),
    };

    [MenuItem("Tools/Sprites/Author Combat Feed Sprite Asset")]
    public static void Author()
    {
        AuthorStatusGlyphs();

        // ── Gather sources (name = file base name; 64×64 enforced by rescale) ──
        var sources = new List<(string name, Texture2D tex)>();
        foreach (var folder in new[] { "Assets/Sprites/Spells", "Assets/Sprites/Timeline/ActorTagIcons", StatusFolder })
        {
            if (!Directory.Exists(folder)) continue;
            foreach (var file in Directory.GetFiles(folder, "*.png").OrderBy(f => f))
            {
                var tex = new Texture2D(2, 2, TextureFormat.RGBA32, false);
                if (!tex.LoadImage(File.ReadAllBytes(file))) continue; // bypasses importer readability
                sources.Add((Path.GetFileNameWithoutExtension(file), Rescale(tex, Cell)));
            }
        }
        if (sources.Count == 0)
        {
            Debug.LogWarning("[CombatFeedSpriteAssetAuthor] No source sprites found — run the spell/tag authors first.");
            return;
        }

        // ── Pack the atlas (fixed grid; deterministic order) ──
        int rows = Mathf.CeilToInt(sources.Count / (float)Columns);
        var atlas = new Texture2D(Columns * Cell, rows * Cell, TextureFormat.RGBA32, false);
        atlas.SetPixels(new Color[atlas.width * atlas.height]); // clear to transparent
        for (int i = 0; i < sources.Count; i++)
        {
            int cx = (i % Columns) * Cell;
            int cy = (rows - 1 - i / Columns) * Cell; // row 0 at the TOP of the texture
            atlas.SetPixels(cx, cy, Cell, Cell, sources[i].tex.GetPixels());
        }
        atlas.Apply();

        Directory.CreateDirectory(OutFolder);
        File.WriteAllBytes(AtlasPath, atlas.EncodeToPNG());
        AssetDatabase.ImportAsset(AtlasPath, ImportAssetOptions.ForceUpdate);

        var importer = (TextureImporter)AssetImporter.GetAtPath(AtlasPath);
        importer.textureType = TextureImporterType.Sprite;
        importer.spriteImportMode = SpriteImportMode.Single;
        importer.mipmapEnabled = false;
        importer.SaveAndReimport();

        var atlasAsset = AssetDatabase.LoadAssetAtPath<Texture2D>(AtlasPath);

        // ── Build (or rebuild) the TMP_SpriteAsset over the atlas ──
        var spriteAsset = AssetDatabase.LoadAssetAtPath<TMP_SpriteAsset>(AssetPath);
        bool fresh = spriteAsset == null;
        if (fresh) spriteAsset = ScriptableObject.CreateInstance<TMP_SpriteAsset>();

        spriteAsset.spriteSheet = atlasAsset;
        spriteAsset.spriteGlyphTable.Clear();
        spriteAsset.spriteCharacterTable.Clear();

        for (int i = 0; i < sources.Count; i++)
        {
            int cx = (i % Columns) * Cell;
            int cy = (rows - 1 - i / Columns) * Cell;

            var glyph = new TMP_SpriteGlyph
            {
                index = (uint)i,
                glyphRect = new GlyphRect(cx, cy, Cell, Cell),
                // Baseline-sit the icon: bearingY lifts it so it centers on the text line.
                metrics = new GlyphMetrics(Cell, Cell, 0, Cell * 0.85f, Cell),
                scale = 1f,
                atlasIndex = 0,
            };
            spriteAsset.spriteGlyphTable.Add(glyph);

            var character = new TMP_SpriteCharacter(0xFFFE, glyph)
            {
                name = sources[i].name,
                scale = 1f,
            };
            spriteAsset.spriteCharacterTable.Add(character);
        }

        if (fresh)
        {
            AssetDatabase.CreateAsset(spriteAsset, AssetPath);
            var material = new Material(Shader.Find("TextMeshPro/Sprite")) { name = "CombatFeedIcons Material" };
            material.SetTexture(ShaderUtilities.ID_MainTex, atlasAsset);
            spriteAsset.material = material;
            AssetDatabase.AddObjectToAsset(material, spriteAsset);
        }
        else
        {
            spriteAsset.material.SetTexture(ShaderUtilities.ID_MainTex, atlasAsset);
        }

        // faceInfo is read-only via the public API — set the serialized point size so TMP
        // scales the 64px glyphs relative to the running font size correctly.
        var so = new SerializedObject(spriteAsset);
        so.FindProperty("m_FaceInfo.m_PointSize").floatValue = Cell;
        so.FindProperty("m_FaceInfo.m_Scale").floatValue = 1f;
        so.FindProperty("m_FaceInfo.m_LineHeight").floatValue = Cell;
        so.FindProperty("m_FaceInfo.m_AscentLine").floatValue = Cell * 0.85f;
        so.FindProperty("m_FaceInfo.m_Baseline").floatValue = 0f;
        so.FindProperty("m_FaceInfo.m_DescentLine").floatValue = -Cell * 0.15f;
        so.ApplyModifiedPropertiesWithoutUndo();

        spriteAsset.UpdateLookupTables();
        EditorUtility.SetDirty(spriteAsset);
        AssetDatabase.SaveAssets();

        RegisterAddressable(AssetPath, Address);
        Debug.Log($"[CombatFeedSpriteAssetAuthor] Authored {sources.Count} glyphs into {AssetPath} (Addressable '{Address}').");
    }

    /// <summary>Gap-fills the per-Buff status glyph PNGs (two-letter placeholder style —
    /// overwrite with real art any time; delete to regenerate).</summary>
    private static void AuthorStatusGlyphs()
    {
        Directory.CreateDirectory(StatusFolder);
        int created = 0;
        foreach (var (name, code, color) in StatusGlyphs)
        {
            var path = $"{StatusFolder}/{name}.png";
            if (File.Exists(path)) continue;
            File.WriteAllBytes(path, BuildGlyph(code, color).EncodeToPNG());
            created++;
        }
        if (created > 0) AssetDatabase.Refresh();
        Debug.Log($"[CombatFeedSpriteAssetAuthor] Status glyphs: {created} created (existing left untouched).");
    }

    /// <summary>Rounded colored square + darker rim; the two-letter code is drawn as a simple
    /// pixel block pattern placeholder (kept minimal — real art replaces the PNG).</summary>
    private static Texture2D BuildGlyph(string code, Color color)
    {
        var tex = new Texture2D(Cell, Cell, TextureFormat.RGBA32, false);
        var px = new Color[Cell * Cell];
        float r = Cell * 0.5f;
        for (int y = 0; y < Cell; y++)
        for (int x = 0; x < Cell; x++)
        {
            float dx = x - r + 0.5f, dy = y - r + 0.5f;
            float d = Mathf.Sqrt(dx * dx + dy * dy);
            if (d > r) { px[y * Cell + x] = Color.clear; continue; }
            float edge = Mathf.SmoothStep(1f, 0f, (d - (r - 1.5f)) / 1.5f);
            float rim = d > r * 0.82f ? 0.65f : 1f;
            px[y * Cell + x] = new Color(color.r * rim, color.g * rim, color.b * rim, edge);
        }
        tex.SetPixels(px);
        tex.Apply();
        return tex;
    }

    /// <summary>Nearest-neighbor rescale to a square target (placeholder-grade fidelity).</summary>
    private static Texture2D Rescale(Texture2D src, int size)
    {
        if (src.width == size && src.height == size) return src;
        var dst = new Texture2D(size, size, TextureFormat.RGBA32, false);
        var px = new Color[size * size];
        for (int y = 0; y < size; y++)
        for (int x = 0; x < size; x++)
            px[y * size + x] = src.GetPixel(x * src.width / size, y * src.height / size);
        dst.SetPixels(px);
        dst.Apply();
        return dst;
    }

    private static void RegisterAddressable(string assetPath, string address)
    {
#if UNITY_2020_2_OR_NEWER
        var settings = AddressableAssetSettingsDefaultObject.Settings;
        if (settings == null) { Debug.LogWarning("[CombatFeedSpriteAssetAuthor] No Addressables settings."); return; }
        var guid = AssetDatabase.AssetPathToGUID(assetPath);
        var entry = settings.CreateOrMoveEntry(guid, settings.DefaultGroup);
        entry.address = address;
        settings.SetDirty(AddressableAssetSettings.ModificationEvent.EntryModified, entry, true);
        AssetDatabase.SaveAssets();
#endif
    }
}
#endif
