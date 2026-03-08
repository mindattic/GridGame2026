using UnityEngine;
using Scripts.Data.Items;
using Scripts.Canvas;
using Scripts.Data.Actor;
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
/// PLACEHOLDERICONFACTORY - Generates colored placeholder icons at runtime.
/// 
/// PURPOSE:
/// Creates small Sprite icons for items that don't have hand-drawn art yet.
/// Each icon is a colored square with a shape indicator baked in:
/// - Swords (Weapon) = diamond
/// - Shield (Armor/Helmet/Boots) = circle
/// - Ring/Amulet = small circle
/// - Potion (Consumable) = triangle
/// - Gem (CraftingMaterial) = hexagon
/// - Scroll (QuestItem) = rectangle
/// Colors are tinted by rarity and type.
/// 
/// CACHING:
/// Icons are generated once per (Type, Slot, Rarity) combo and cached.
/// 
/// RELATED FILES:
/// - HubItemRowFactory.cs: Displays icons in rows
/// - ItemDefinition.cs: Provides type/slot/rarity
/// </summary>
public static class PlaceholderIconFactory
{
    private const int IconSize = 64;
    private static readonly System.Collections.Generic.Dictionary<string, Sprite> cache
        = new System.Collections.Generic.Dictionary<string, Sprite>();

    /// <summary>Gets or creates a placeholder icon for an item definition.</summary>
    public static Sprite GetIcon(ItemDefinition item)
    {
        if (item == null) return GetFallback();

        string key = $"{item.Type}_{item.Slot}_{item.Rarity}";
        if (cache.TryGetValue(key, out var cached)) return cached;

        var baseColor = GetBaseColor(item.Type, item.Slot);
        var rarityTint = GetRarityTint(item.Rarity);
        var finalColor = Color.Lerp(baseColor, rarityTint, 0.35f);
        var borderColor = rarityTint;

        var tex = GenerateIcon(finalColor, borderColor, GetShape(item.Type, item.Slot));
        var sprite = Sprite.Create(tex, new Rect(0, 0, IconSize, IconSize), new Vector2(0.5f, 0.5f), 100f);
        sprite.name = $"Icon_{key}";
        cache[key] = sprite;
        return sprite;
    }

    /// <summary>Gets a generic fallback icon (gray square).</summary>
    public static Sprite GetFallback()
    {
        const string key = "__fallback";
        if (cache.TryGetValue(key, out var cached)) return cached;

        var tex = GenerateIcon(new Color(0.3f, 0.3f, 0.35f), new Color(0.5f, 0.5f, 0.5f), IconShape.Square);
        var sprite = Sprite.Create(tex, new Rect(0, 0, IconSize, IconSize), new Vector2(0.5f, 0.5f), 100f);
        sprite.name = "Icon_Fallback";
        cache[key] = sprite;
        return sprite;
    }

    /// <summary>Gets a gold coin icon for currency display.</summary>
    public static Sprite GetGoldIcon()
    {
        const string key = "__gold";
        if (cache.TryGetValue(key, out var cached)) return cached;

        var tex = GenerateIcon(new Color(1f, 0.82f, 0.2f), new Color(0.85f, 0.65f, 0.1f), IconShape.Circle);
        var sprite = Sprite.Create(tex, new Rect(0, 0, IconSize, IconSize), new Vector2(0.5f, 0.5f), 100f);
        sprite.name = "Icon_Gold";
        cache[key] = sprite;
        return sprite;
    }

    // ===================== Shape Types =====================

    private enum IconShape { Square, Diamond, Circle, SmallCircle, Triangle, Hexagon }

    private static IconShape GetShape(ItemType type, EquipmentSlot slot)
    {
        switch (type)
        {
            case ItemType.Equipment:
                switch (slot)
                {
                    case EquipmentSlot.Weapon: return IconShape.Diamond;
                    case EquipmentSlot.Armor: return IconShape.Circle;
                    case EquipmentSlot.Relic1:
                    case EquipmentSlot.Relic2:
                    case EquipmentSlot.Relic3: return IconShape.SmallCircle;
                    default: return IconShape.Square;
                }
            case ItemType.Consumable: return IconShape.Triangle;
            case ItemType.CraftingMaterial: return IconShape.Hexagon;
            case ItemType.QuestItem: return IconShape.Square;
            default: return IconShape.Square;
        }
    }

    // ===================== Colors =====================

    private static Color GetBaseColor(ItemType type, EquipmentSlot slot)
    {
        switch (type)
        {
            case ItemType.Equipment:
                switch (slot)
                {
                    case EquipmentSlot.Weapon: return new Color(0.75f, 0.35f, 0.30f);
                    case EquipmentSlot.Armor: return new Color(0.40f, 0.50f, 0.65f);
                    case EquipmentSlot.Relic1: return new Color(0.65f, 0.55f, 0.20f);
                    case EquipmentSlot.Relic2: return new Color(0.30f, 0.60f, 0.55f);
                    case EquipmentSlot.Relic3: return new Color(0.50f, 0.55f, 0.60f);
                    default: return new Color(0.5f, 0.5f, 0.5f);
                }
            case ItemType.Consumable: return new Color(0.30f, 0.70f, 0.35f);
            case ItemType.CraftingMaterial: return new Color(0.55f, 0.45f, 0.65f);
            case ItemType.QuestItem: return new Color(0.70f, 0.60f, 0.25f);
            default: return new Color(0.5f, 0.5f, 0.5f);
        }
    }

    private static Color GetRarityTint(ItemRarity rarity)
    {
        switch (rarity)
        {
            case ItemRarity.Common: return new Color(0.75f, 0.75f, 0.75f);
            case ItemRarity.Uncommon: return new Color(0.30f, 0.85f, 0.30f);
            case ItemRarity.Rare: return new Color(0.30f, 0.55f, 1.00f);
            case ItemRarity.Epic: return new Color(0.70f, 0.30f, 0.90f);
            case ItemRarity.Legendary: return new Color(1.00f, 0.65f, 0.00f);
            default: return Color.white;
        }
    }

    // ===================== Texture Generation =====================

    private static Texture2D GenerateIcon(Color fill, Color border, IconShape shape)
    {
        var tex = new Texture2D(IconSize, IconSize, TextureFormat.RGBA32, false);
        tex.filterMode = FilterMode.Bilinear;
        tex.wrapMode = TextureWrapMode.Clamp;

        var pixels = new Color[IconSize * IconSize];
        float half = IconSize * 0.5f;
        float outerR = half - 1f;
        float innerR = outerR - 3f;

        for (int y = 0; y < IconSize; y++)
        {
            for (int x = 0; x < IconSize; x++)
            {
                float dx = x - half + 0.5f;
                float dy = y - half + 0.5f;
                float dist;

                switch (shape)
                {
                    case IconShape.Diamond:
                        dist = Mathf.Abs(dx) + Mathf.Abs(dy);
                        break;
                    case IconShape.Circle:
                        dist = Mathf.Sqrt(dx * dx + dy * dy);
                        break;
                    case IconShape.SmallCircle:
                        dist = Mathf.Sqrt(dx * dx + dy * dy) * 1.4f;
                        break;
                    case IconShape.Triangle:
                        float ny = (dy + half) / IconSize;
                        float halfWidth = ny * half;
                        dist = (Mathf.Abs(dx) > halfWidth || ny < 0.1f)
                            ? outerR + 5f
                            : Mathf.Max(Mathf.Abs(dx) - halfWidth + outerR, Mathf.Abs(dy));
                        break;
                    case IconShape.Hexagon:
                        float ax = Mathf.Abs(dx);
                        float ay = Mathf.Abs(dy);
                        dist = Mathf.Max(ax + ay * 0.577f, ay);
                        break;
                    default: // Square
                        dist = Mathf.Max(Mathf.Abs(dx), Mathf.Abs(dy));
                        break;
                }

                Color c;
                if (dist > outerR)
                    c = Color.clear;
                else if (dist > innerR)
                    c = border;
                else
                {
                    // Slight gradient from center
                    float t = dist / innerR;
                    c = Color.Lerp(fill * 1.2f, fill * 0.8f, t);
                    c.a = 1f;
                }

                pixels[y * IconSize + x] = c;
            }
        }

        tex.SetPixels(pixels);
        tex.Apply();
        return tex;
    }
}

}
