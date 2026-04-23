using Scripts.Canvas;
using Scripts.Data.Actor;
using Scripts.Data.Skills;
using Scripts.Effects;
using Scripts.Factories;
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
using UnityEngine;

namespace Scripts.Data.Items
{
/// <summary>Item rarity tiers affecting value and drop rates.
/// <para>Ordering is load-bearing: lower = worse, so <c>(int)ItemRarity.Epic &gt; (int)ItemRarity.Rare</c>.
/// Junk sits below Common and is reserved for filler drops farmers vendor immediately.</para>
/// <para>Colors follow the WoW-style convention (gray/white/green/blue/purple/orange) and are
/// exposed via <see cref="ItemRarityColors"/> so on-map pickups, hub rows, and combat text all
/// agree on a single source of truth.</para></summary>
public enum ItemRarity
{
    Junk = -1,
    Common = 0,
    Uncommon = 1,
    Rare = 2,
    Epic = 3,
    Legendary = 4
}

/// <summary>WoW-style rarity color palette shared between UI (hub rows) and on-map pickups.</summary>
public static class ItemRarityColors
{
    // Hex anchors — keep in sync with HubItemRowFactory.RarityColor.
    public static readonly Color Junk      = new Color(0.616f, 0.616f, 0.616f, 1f); // #9d9d9d
    public static readonly Color Common    = new Color(1.000f, 1.000f, 1.000f, 1f); // #ffffff
    public static readonly Color Uncommon  = new Color(0.118f, 1.000f, 0.000f, 1f); // #1eff00
    public static readonly Color Rare      = new Color(0.000f, 0.439f, 0.867f, 1f); // #0070dd
    public static readonly Color Epic      = new Color(0.639f, 0.208f, 0.933f, 1f); // #a335ee
    public static readonly Color Legendary = new Color(1.000f, 0.502f, 0.000f, 1f); // #ff8000

    public static Color Get(ItemRarity rarity) => rarity switch
    {
        ItemRarity.Junk      => Junk,
        ItemRarity.Uncommon  => Uncommon,
        ItemRarity.Rare      => Rare,
        ItemRarity.Epic      => Epic,
        ItemRarity.Legendary => Legendary,
        _                    => Common,
    };
}
}
