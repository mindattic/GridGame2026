using TMPro;
using UnityEngine;
using UnityEngine.UI;
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
using Scripts.Libraries;
using Scripts.Managers;
using Scripts.Models;
using Scripts.Models.Actor;
using Scripts.Overworld;
using Scripts.Sequences;
using Scripts.Serialization;
using Scripts.Utilities;

namespace Scripts.Factories
{
/// <summary>
/// HUBVENDORFACTORY - Creates themed vendor displays for Hub sections.
/// 
/// PURPOSE:
/// Builds a vendor portrait image and a colored panel background
/// for each Hub section, giving each vendor a unique identity.
/// If a proper portrait sprite exists (from ActorLibrary), it is used;
/// otherwise a colored placeholder silhouette is generated.
/// 
/// VENDOR IDENTITIES:
/// - Shop: Merchant (Courier or Tinkerer) — warm gold/amber bg
/// - Blacksmith: Smith (Engineer or Machinist) — fiery orange/brown bg
/// - Training: Trainer (Mannequin) — cool blue/purple bg
/// - Medical: Healer (Cleric or Sister) — soft green/white bg
/// - Equip: Armorer (Knight or ShieldMaiden) — steel blue/gray bg
/// - Party: Commander (Paladin) — deep red/maroon bg
/// - Residence: Innkeeper (PandaGirl) — warm wood/tan bg
/// 
/// CREATED HIERARCHY (appended to section panel):
/// ```
/// VendorOverlay (child of panel root)
/// ├── VendorBackground (Image — gradient fill)
/// ├── VendorPortrait (Image — character portrait)
/// └── VendorNameplate (TMP — vendor title text)
/// ```
/// 
/// RELATED FILES:
/// - HubManager.cs: Section panel roots
/// - ActorLibrary.cs: Portrait sprites
/// - PlaceholderIconFactory.cs: Placeholder generation patterns
/// </summary>
public static class HubVendorFactory
{
    /// <summary>Vendor theme definition.</summary>
    public struct VendorTheme
    {
        public string VendorName;
        public CharacterClass PortraitClass;
        public Color BackgroundTop;
        public Color BackgroundBottom;
        public Color NameplateColor;
    }

    // ===================== Theme Presets =====================

    public static readonly VendorTheme ShopTheme = new VendorTheme
    {
        VendorName = "General Store",
        PortraitClass = CharacterClass.Courier,
        BackgroundTop = new Color(0.35f, 0.28f, 0.12f, 0.90f),
        BackgroundBottom = new Color(0.20f, 0.15f, 0.08f, 0.95f),
        NameplateColor = new Color(1f, 0.85f, 0.4f),
    };

    public static readonly VendorTheme BlacksmithTheme = new VendorTheme
    {
        VendorName = "Blacksmith",
        PortraitClass = CharacterClass.Engineer,
        BackgroundTop = new Color(0.38f, 0.20f, 0.10f, 0.90f),
        BackgroundBottom = new Color(0.22f, 0.10f, 0.05f, 0.95f),
        NameplateColor = new Color(1f, 0.55f, 0.2f),
    };

    public static readonly VendorTheme TrainingTheme = new VendorTheme
    {
        VendorName = "Training Hall",
        PortraitClass = CharacterClass.Mannequin,
        BackgroundTop = new Color(0.15f, 0.18f, 0.35f, 0.90f),
        BackgroundBottom = new Color(0.08f, 0.10f, 0.22f, 0.95f),
        NameplateColor = new Color(0.6f, 0.7f, 1f),
    };

    public static readonly VendorTheme MedicalTheme = new VendorTheme
    {
        VendorName = "Medical Tent",
        PortraitClass = CharacterClass.Cleric,
        BackgroundTop = new Color(0.12f, 0.30f, 0.18f, 0.90f),
        BackgroundBottom = new Color(0.06f, 0.18f, 0.10f, 0.95f),
        NameplateColor = new Color(0.5f, 1f, 0.6f),
    };

    public static readonly VendorTheme EquipTheme = new VendorTheme
    {
        VendorName = "Armory",
        PortraitClass = CharacterClass.Knight,
        BackgroundTop = new Color(0.20f, 0.25f, 0.32f, 0.90f),
        BackgroundBottom = new Color(0.10f, 0.14f, 0.20f, 0.95f),
        NameplateColor = new Color(0.7f, 0.8f, 0.95f),
    };

    public static readonly VendorTheme PartyTheme = new VendorTheme
    {
        VendorName = "Camp",
        PortraitClass = CharacterClass.Paladin,
        BackgroundTop = new Color(0.30f, 0.12f, 0.14f, 0.90f),
        BackgroundBottom = new Color(0.18f, 0.06f, 0.08f, 0.95f),
        NameplateColor = new Color(1f, 0.65f, 0.65f),
    };

    public static readonly VendorTheme ResidenceTheme = new VendorTheme
    {
        VendorName = "Residence",
        PortraitClass = CharacterClass.PandaGirl,
        BackgroundTop = new Color(0.32f, 0.25f, 0.18f, 0.90f),
        BackgroundBottom = new Color(0.18f, 0.14f, 0.10f, 0.95f),
        NameplateColor = new Color(0.95f, 0.85f, 0.65f),
    };

    // ===================== Builder =====================

    /// <summary>
    /// Builds or refreshes the vendor overlay on a section panel.
    /// Safe to call multiple times — checks for existing overlay first.
    /// </summary>
    public static void Build(RectTransform panel, VendorTheme theme)
    {
        if (panel == null) return;

        // Check if already built
        var existing = panel.Find("VendorOverlay");
        if (existing != null) return;

        // === VendorOverlay container (non-interactive, behind content) ===
        var overlay = new GameObject("VendorOverlay");
        overlay.layer = LayerMask.NameToLayer("UI");
        var overlayRT = overlay.AddComponent<RectTransform>();
        overlayRT.SetParent(panel, false);
        overlayRT.anchorMin = Vector2.zero;
        overlayRT.anchorMax = Vector2.one;
        overlayRT.offsetMin = Vector2.zero;
        overlayRT.offsetMax = Vector2.zero;
        // Send to back so content renders on top
        overlayRT.SetAsFirstSibling();

        // === Background gradient ===
        var bg = new GameObject("VendorBackground");
        bg.layer = LayerMask.NameToLayer("UI");
        var bgRT = bg.AddComponent<RectTransform>();
        bgRT.SetParent(overlayRT, false);
        bgRT.anchorMin = Vector2.zero;
        bgRT.anchorMax = Vector2.one;
        bgRT.offsetMin = Vector2.zero;
        bgRT.offsetMax = Vector2.zero;
        bg.AddComponent<CanvasRenderer>();
        var bgImg = bg.AddComponent<Image>();
        bgImg.raycastTarget = false;

        // Generate a vertical gradient texture
        var gradTex = GenerateGradient(theme.BackgroundTop, theme.BackgroundBottom);
        bgImg.sprite = Sprite.Create(gradTex, new Rect(0, 0, 4, 64), new Vector2(0.5f, 0.5f));
        bgImg.type = Image.Type.Sliced;

        // === Portrait (right-aligned, partially transparent) ===
        var portrait = new GameObject("VendorPortrait");
        portrait.layer = LayerMask.NameToLayer("UI");
        var porRT = portrait.AddComponent<RectTransform>();
        porRT.SetParent(overlayRT, false);
        // Position: right half, vertically centered, large
        porRT.anchorMin = new Vector2(0.55f, 0.0f);
        porRT.anchorMax = new Vector2(1.0f, 1.0f);
        porRT.offsetMin = new Vector2(0f, 10f);
        porRT.offsetMax = new Vector2(-10f, -10f);
        portrait.AddComponent<CanvasRenderer>();
        var porImg = portrait.AddComponent<Image>();
        porImg.raycastTarget = false;
        porImg.preserveAspect = true;

        // Try to load the character portrait
        var actorData = ActorLibrary.Get(theme.PortraitClass);
        if (actorData?.Portrait != null)
        {
            porImg.sprite = actorData.Portrait;
            porImg.color = new Color(1f, 1f, 1f, 0.15f); // ghostly silhouette
        }
        else
        {
            // Generate a colored placeholder silhouette
            porImg.sprite = GenerateSilhouette(theme.NameplateColor);
            porImg.color = new Color(1f, 1f, 1f, 0.12f);
        }

        // === Vendor Name plate (top-left) ===
        var nameplate = new GameObject("VendorNameplate");
        nameplate.layer = LayerMask.NameToLayer("UI");
        var npRT = nameplate.AddComponent<RectTransform>();
        npRT.SetParent(overlayRT, false);
        npRT.anchorMin = new Vector2(0f, 1f);
        npRT.anchorMax = new Vector2(0.5f, 1f);
        npRT.pivot = new Vector2(0f, 1f);
        npRT.offsetMin = new Vector2(12f, -44f);
        npRT.offsetMax = new Vector2(0f, -8f);
        nameplate.AddComponent<CanvasRenderer>();
        var npTmp = nameplate.AddComponent<TextMeshProUGUI>();
        npTmp.text = theme.VendorName;
        npTmp.fontSize = 32;
        npTmp.fontStyle = FontStyles.Bold;
        npTmp.color = theme.NameplateColor;
        npTmp.alignment = TextAlignmentOptions.BottomLeft;
        npTmp.enableWordWrapping = false;
        npTmp.raycastTarget = false;
    }

    // ===================== Texture Helpers =====================

    /// <summary>Generates a small vertical gradient texture.</summary>
    private static Texture2D GenerateGradient(Color top, Color bottom)
    {
        int w = 4, h = 64;
        var tex = new Texture2D(w, h, TextureFormat.RGBA32, false);
        tex.filterMode = FilterMode.Bilinear;
        tex.wrapMode = TextureWrapMode.Clamp;

        var pixels = new Color[w * h];
        for (int y = 0; y < h; y++)
        {
            float t = (float)y / (h - 1);
            var c = Color.Lerp(bottom, top, t);
            for (int x = 0; x < w; x++)
                pixels[y * w + x] = c;
        }
        tex.SetPixels(pixels);
        tex.Apply();
        return tex;
    }

    /// <summary>Generates a simple colored circle silhouette as placeholder portrait.</summary>
    private static Sprite GenerateSilhouette(Color tint)
    {
        int s = 128;
        var tex = new Texture2D(s, s, TextureFormat.RGBA32, false);
        tex.filterMode = FilterMode.Bilinear;
        var pixels = new Color[s * s];
        float half = s * 0.5f;

        for (int y = 0; y < s; y++)
        {
            for (int x = 0; x < s; x++)
            {
                float dx = x - half + 0.5f;
                float dy = y - half + 0.5f;
                float dist = Mathf.Sqrt(dx * dx + dy * dy);

                if (dist < half - 2f)
                {
                    float fade = 1f - (dist / half);
                    pixels[y * s + x] = new Color(tint.r, tint.g, tint.b, fade * 0.6f);
                }
                else
                {
                    pixels[y * s + x] = Color.clear;
                }
            }
        }

        tex.SetPixels(pixels);
        tex.Apply();
        return Sprite.Create(tex, new Rect(0, 0, s, s), new Vector2(0.5f, 0.5f));
    }
}

}
