#if UNITY_EDITOR
using System.IO;
using UnityEditor;
using UnityEngine;
#if UNITY_2020_2_OR_NEWER
using UnityEditor.AddressableAssets;
using UnityEditor.AddressableAssets.Settings;
#endif

/// <summary>
/// SPRITEASSETAUTHOR - Editor-only tool that procedurally builds 64×64 sprite PNGs, drops them
/// into <c>Assets/Sprites/...</c>, sets the TextureImporter to Sprite, and adds them to the
/// project's default Addressables group so <c>AssetHelper.LoadAsset&lt;Sprite&gt;(address)</c>
/// finds them just like every other in-project sprite.
///
/// <para>The project's standard sprite pipeline is PNG-on-disk + Addressables — code-only
/// procedural sprites bypass that and lose the swap-out workflow. This tool keeps the procedural
/// generator in code (deterministic + regeneratable) but the output is a real asset.</para>
///
/// <para>Menu:</para>
/// <list type="bullet">
///   <item><c>Tools/Sprites/Author Mana Orb Sprites</c> — orb-body (radial gradient) + orb-glass (highlight).</item>
///   <item><c>Tools/Sprites/Author Spell Icons</c> — one 64×64 placeholder per <c>SpellLibrary</c> entry.</item>
/// </list>
/// </summary>
public static class SpriteAssetAuthor
{
    private const int OrbSize  = 256;   // crisp gradient
    private const int IconSize = 64;    // user-spec icon size

    private const string ManaFolder  = "Assets/Sprites/Mana";
    private const string SpellFolder = "Assets/Sprites/Spells";

    // ── Menu entries ──────────────────────────────────────────────

    [MenuItem("Tools/Sprites/Author Mana Orb Sprites")]
    public static void AuthorManaOrbSprites()
    {
        EnsureFolder(ManaFolder);
        SavePngAndRegister(BuildOrbBody(),  $"{ManaFolder}/orb-body.png");
        SavePngAndRegister(BuildOrbGlass(), $"{ManaFolder}/orb-glass.png");
        Debug.Log("[SpriteAssetAuthor] Mana orb sprites authored. Add registration lines to Libraries/SpriteLibrary.cs if not yet present.");
    }

    [MenuItem("Tools/Sprites/Author Spell Icons (Placeholders)")]
    public static void AuthorSpellIcons()
    {
        EnsureFolder(SpellFolder);
        foreach (var spell in Scripts.Data.SpellLibrary.All)
        {
            if (spell == null || spell.Ability == null) continue;
            var tex = BuildSpellIcon(spell);
            SavePngAndRegister(tex, $"{SpellFolder}/{spell.Ability.Name}.png");
        }
        Debug.Log("[SpriteAssetAuthor] Spell placeholder icons authored. Real art: drop a 64×64 PNG with the same name to replace.");
    }

    // ── Procedural texture builders ───────────────────────────────

    /// <summary>Radial gradient: bright at center, fades to transparent at the edge.
    /// Mid-zone slightly darker to give a 3D-sphere read. Image.color tints it.</summary>
    private static Texture2D BuildOrbBody()
    {
        var tex = new Texture2D(OrbSize, OrbSize, TextureFormat.RGBA32, false);
        float r = OrbSize * 0.5f;
        var pixels = new Color[OrbSize * OrbSize];
        for (int y = 0; y < OrbSize; y++)
        for (int x = 0; x < OrbSize; x++)
        {
            float dx = x - r + 0.5f, dy = y - r + 0.5f;
            float d = Mathf.Sqrt(dx * dx + dy * dy);
            float t = Mathf.Clamp01(d / r);                 // 0=center, 1=edge
            float core = 1f - Mathf.Pow(t, 1.6f);            // brighter near center
            float edge = Mathf.SmoothStep(1f, 0f, (d - (r - 1f)) / 1.5f); // AA edge mask
            float lum = Mathf.Lerp(0.6f, 1.0f, core);        // mid 60% → core 100%
            pixels[y * OrbSize + x] = new Color(lum, lum, lum, core * edge);
        }
        tex.SetPixels(pixels);
        tex.Apply();
        return tex;
    }

    /// <summary>Glassy white highlight overlay — soft crescent in the upper-left, faint rim.
    /// Always white; renders unmultiplied on top of the colored body to add the "sphere" read.</summary>
    private static Texture2D BuildOrbGlass()
    {
        var tex = new Texture2D(OrbSize, OrbSize, TextureFormat.RGBA32, false);
        float r = OrbSize * 0.5f;
        var pixels = new Color[OrbSize * OrbSize];

        // Highlight: small white spot centered upper-left inside the orb.
        Vector2 hl = new Vector2(r - r * 0.35f, r + r * 0.40f);
        float hlRadius = r * 0.32f;

        for (int y = 0; y < OrbSize; y++)
        for (int x = 0; x < OrbSize; x++)
        {
            float dx = x - r + 0.5f, dy = y - r + 0.5f;
            float dCenter = Mathf.Sqrt(dx * dx + dy * dy);
            if (dCenter > r) { pixels[y * OrbSize + x] = new Color(1f, 1f, 1f, 0f); continue; }

            // Main highlight
            float hDx = x - hl.x, hDy = y - hl.y;
            float hd = Mathf.Sqrt(hDx * hDx + hDy * hDy);
            float hAlpha = Mathf.Clamp01(1f - hd / hlRadius);
            hAlpha = Mathf.Pow(hAlpha, 1.5f) * 0.85f;

            // Faint rim — adds depth around the edge.
            float rim = Mathf.Clamp01((dCenter - r * 0.85f) / (r * 0.15f));
            rim = Mathf.SmoothStep(0f, 1f, rim) * 0.18f;

            // AA mask at the outer edge.
            float edgeMask = Mathf.SmoothStep(1f, 0f, (dCenter - (r - 1f)) / 1.5f);

            float a = Mathf.Clamp01(hAlpha + rim) * edgeMask;
            pixels[y * OrbSize + x] = new Color(1f, 1f, 1f, a);
        }
        tex.SetPixels(pixels);
        tex.Apply();
        return tex;
    }

    /// <summary>64×64 placeholder spell icon — colored gradient disk + bright ring + black initial.
    /// Real art replaces by overwriting the PNG; address stays the same.</summary>
    private static Texture2D BuildSpellIcon(Scripts.Models.SpellDefinition spell)
    {
        var tex = new Texture2D(IconSize, IconSize, TextureFormat.RGBA32, false);
        float r = IconSize * 0.5f;
        Color body = ColorForSpell(spell);
        var pixels = new Color[IconSize * IconSize];

        for (int y = 0; y < IconSize; y++)
        for (int x = 0; x < IconSize; x++)
        {
            float dx = x - r + 0.5f, dy = y - r + 0.5f;
            float d = Mathf.Sqrt(dx * dx + dy * dy);
            if (d > r) { pixels[y * IconSize + x] = new Color(0f, 0f, 0f, 0f); continue; }

            // Bright outer ring (1.5px).
            if (d >= r - 2f)
            {
                float a = Mathf.SmoothStep(1f, 0f, (d - (r - 1f)) / 1.5f);
                pixels[y * IconSize + x] = new Color(1f, 1f, 1f, 0.9f * a);
                continue;
            }

            // Radial gradient from a brighter version of body color → darker.
            float t = d / r;
            Color c = Color.Lerp(LightenedColor(body, 0.35f), DarkenedColor(body, 0.25f), t);
            c.a = 1f;
            pixels[y * IconSize + x] = c;
        }
        // Initial-letter overlay (5×7 pixel font, black, centered).
        char initial = char.ToUpper(spell.Ability.Name[0]);
        DrawCharCentered(pixels, IconSize, IconSize, initial, Color.black, scale: 4);
        tex.SetPixels(pixels);
        tex.Apply();
        return tex;
    }

    private static Color ColorForSpell(Scripts.Models.SpellDefinition spell)
    {
        // Reuse the orb palette for thematic consistency. Mana cost wins; fall back to DamageType.
        if (spell.Cost != null && spell.Cost.Costs.Count > 0)
            return Scripts.Canvas.ManaOrbLine.ColorFor(spell.Cost.Costs[0].Type);
        switch (spell.DamageType)
        {
            case Scripts.Models.DamageType.Fire:      return new Color(1.00f, 0.45f, 0.10f);
            case Scripts.Models.DamageType.Ice:       return new Color(0.55f, 0.85f, 1.00f);
            case Scripts.Models.DamageType.Lightning: return new Color(1.00f, 0.95f, 0.45f);
            case Scripts.Models.DamageType.Poison:    return new Color(0.50f, 0.85f, 0.40f);
            case Scripts.Models.DamageType.Holy:      return new Color(1.00f, 0.95f, 0.80f);
            case Scripts.Models.DamageType.Dark:      return new Color(0.40f, 0.30f, 0.55f);
            default:                                   return new Color(0.65f, 0.65f, 0.75f);
        }
    }

    private static Color LightenedColor(Color c, float amount) => Color.Lerp(c, Color.white, amount);
    private static Color DarkenedColor (Color c, float amount) => Color.Lerp(c, Color.black, amount);

    // ── Tiny 5×7 pixel font, scaled by `scale`. Covers A–Z capitals (lazy lookup). ──
    private static void DrawCharCentered(Color[] pixels, int w, int h, char ch, Color color, int scale)
    {
        var glyph = PixelFont.Get(ch);
        if (glyph == null) return;
        int gw = 5 * scale, gh = 7 * scale;
        int x0 = (w - gw) / 2, y0 = (h - gh) / 2;
        for (int gy = 0; gy < 7; gy++)
        {
            byte row = glyph[6 - gy]; // flip y so row 0 = top
            for (int gx = 0; gx < 5; gx++)
            {
                if ((row & (1 << (4 - gx))) == 0) continue;
                int px0 = x0 + gx * scale;
                int py0 = y0 + gy * scale;
                for (int sy = 0; sy < scale; sy++)
                for (int sx = 0; sx < scale; sx++)
                {
                    int px = px0 + sx, py = py0 + sy;
                    if (px < 0 || px >= w || py < 0 || py >= h) continue;
                    pixels[py * w + px] = color;
                }
            }
        }
    }

    // ── Asset save + Addressables registration ────────────────────

    private static void SavePngAndRegister(Texture2D tex, string assetPath)
    {
        File.WriteAllBytes(assetPath, tex.EncodeToPNG());
        Object.DestroyImmediate(tex);
        AssetDatabase.ImportAsset(assetPath, ImportAssetOptions.ForceSynchronousImport);

        var importer = (TextureImporter)AssetImporter.GetAtPath(assetPath);
        if (importer != null)
        {
            importer.textureType = TextureImporterType.Sprite;
            importer.mipmapEnabled = false;
            importer.alphaIsTransparency = true;
            importer.filterMode = FilterMode.Bilinear;
            importer.SaveAndReimport();
        }

        // Add to Addressables under the project sub-path as its address.
        string address = assetPath.Replace("Assets/", "").Replace(".png", "");
        AddToAddressables(assetPath, address);
    }

    private static void AddToAddressables(string assetPath, string address)
    {
#if UNITY_2020_2_OR_NEWER
        var settings = AddressableAssetSettingsDefaultObject.Settings;
        if (settings == null)
        {
            Debug.LogWarning("[SpriteAssetAuthor] AddressableAssetSettings missing — sprite saved but not added to Addressables. Open Window → Asset Management → Addressables → Groups to initialize.");
            return;
        }
        string guid = AssetDatabase.AssetPathToGUID(assetPath);
        if (string.IsNullOrEmpty(guid)) return;
        var entry = settings.CreateOrMoveEntry(guid, settings.DefaultGroup);
        entry.address = address;
        settings.SetDirty(AddressableAssetSettings.ModificationEvent.EntryModified, entry, true);
#else
        Debug.LogWarning("[SpriteAssetAuthor] Addressables API only available in Unity 2020.2+.");
#endif
    }

    private static void EnsureFolder(string assetPath)
    {
        if (AssetDatabase.IsValidFolder(assetPath)) return;
        var parts = assetPath.Split('/');
        string cur = parts[0];
        for (int i = 1; i < parts.Length; i++)
        {
            var next = $"{cur}/{parts[i]}";
            if (!AssetDatabase.IsValidFolder(next))
                AssetDatabase.CreateFolder(cur, parts[i]);
            cur = next;
        }
    }
}

/// <summary>Minimal 5×7 bitmap font (caps only). Each byte row = top 5 bits = pixels.</summary>
internal static class PixelFont
{
    public static byte[] Get(char ch)
    {
        switch (char.ToUpper(ch))
        {
            case 'A': return new byte[] { 0b01110, 0b10001, 0b10001, 0b11111, 0b10001, 0b10001, 0b10001 };
            case 'B': return new byte[] { 0b11110, 0b10001, 0b10001, 0b11110, 0b10001, 0b10001, 0b11110 };
            case 'C': return new byte[] { 0b01110, 0b10001, 0b10000, 0b10000, 0b10000, 0b10001, 0b01110 };
            case 'D': return new byte[] { 0b11110, 0b10001, 0b10001, 0b10001, 0b10001, 0b10001, 0b11110 };
            case 'E': return new byte[] { 0b11111, 0b10000, 0b10000, 0b11110, 0b10000, 0b10000, 0b11111 };
            case 'F': return new byte[] { 0b11111, 0b10000, 0b10000, 0b11110, 0b10000, 0b10000, 0b10000 };
            case 'G': return new byte[] { 0b01110, 0b10001, 0b10000, 0b10111, 0b10001, 0b10001, 0b01110 };
            case 'H': return new byte[] { 0b10001, 0b10001, 0b10001, 0b11111, 0b10001, 0b10001, 0b10001 };
            case 'I': return new byte[] { 0b01110, 0b00100, 0b00100, 0b00100, 0b00100, 0b00100, 0b01110 };
            case 'J': return new byte[] { 0b00111, 0b00010, 0b00010, 0b00010, 0b00010, 0b10010, 0b01100 };
            case 'K': return new byte[] { 0b10001, 0b10010, 0b10100, 0b11000, 0b10100, 0b10010, 0b10001 };
            case 'L': return new byte[] { 0b10000, 0b10000, 0b10000, 0b10000, 0b10000, 0b10000, 0b11111 };
            case 'M': return new byte[] { 0b10001, 0b11011, 0b10101, 0b10101, 0b10001, 0b10001, 0b10001 };
            case 'N': return new byte[] { 0b10001, 0b11001, 0b10101, 0b10101, 0b10011, 0b10001, 0b10001 };
            case 'O': return new byte[] { 0b01110, 0b10001, 0b10001, 0b10001, 0b10001, 0b10001, 0b01110 };
            case 'P': return new byte[] { 0b11110, 0b10001, 0b10001, 0b11110, 0b10000, 0b10000, 0b10000 };
            case 'Q': return new byte[] { 0b01110, 0b10001, 0b10001, 0b10001, 0b10101, 0b10010, 0b01101 };
            case 'R': return new byte[] { 0b11110, 0b10001, 0b10001, 0b11110, 0b10100, 0b10010, 0b10001 };
            case 'S': return new byte[] { 0b01111, 0b10000, 0b10000, 0b01110, 0b00001, 0b00001, 0b11110 };
            case 'T': return new byte[] { 0b11111, 0b00100, 0b00100, 0b00100, 0b00100, 0b00100, 0b00100 };
            case 'U': return new byte[] { 0b10001, 0b10001, 0b10001, 0b10001, 0b10001, 0b10001, 0b01110 };
            case 'V': return new byte[] { 0b10001, 0b10001, 0b10001, 0b10001, 0b10001, 0b01010, 0b00100 };
            case 'W': return new byte[] { 0b10001, 0b10001, 0b10001, 0b10101, 0b10101, 0b11011, 0b10001 };
            case 'X': return new byte[] { 0b10001, 0b10001, 0b01010, 0b00100, 0b01010, 0b10001, 0b10001 };
            case 'Y': return new byte[] { 0b10001, 0b10001, 0b10001, 0b01010, 0b00100, 0b00100, 0b00100 };
            case 'Z': return new byte[] { 0b11111, 0b00001, 0b00010, 0b00100, 0b01000, 0b10000, 0b11111 };
            default:  return null;
        }
    }
}
#endif
