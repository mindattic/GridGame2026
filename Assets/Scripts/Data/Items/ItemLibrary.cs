using System.Collections.Generic;
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

namespace Scripts.Data.Items
{
/// <summary>
/// ITEMLIBRARY - Central registry for all item definitions.
/// 
/// PURPOSE:
/// Provides lookup and enumeration of item definitions
/// populated from static data classes.
/// 
/// USAGE:
/// ```csharp
/// var potion = ItemLibrary.Get("healing_potion_basic");
/// foreach (var item in ItemLibrary.All()) { ... }
/// ```
/// 
/// RELATED FILES:
/// - ItemDefinition.cs: Item data structure
/// - ItemData_Healing.cs: Consumable definitions
/// - ItemData_Equipment.cs: Equipment definitions
/// - PlayerInventory.cs: Item ownership
/// </summary>
public static class ItemLibrary
{
    private static Dictionary<string, ItemDefinition> items = new Dictionary<string, ItemDefinition>();
    private static bool initialized;

    /// <summary>Ensures library is populated once.</summary>
    private static void Ensure()
    {
        if (initialized) return;
        initialized = true;

        // Legacy items
        Register(ItemData_Healing.BasicHealingPotion);
        Register(ItemData_Healing.ManaPotion);
        Register(ItemData_Equipment.RustySword);
        Register(ItemData_Equipment.LeatherArmor);

        // Expanded consumables
        Register(ItemData_Consumables.HiPotion);
        Register(ItemData_Consumables.XPotion);
        Register(ItemData_Consumables.Ether);
        Register(ItemData_Consumables.HiEther);
        Register(ItemData_Consumables.PhoenixDown);
        Register(ItemData_Consumables.Antidote);
        Register(ItemData_Consumables.EyeDrops);
        Register(ItemData_Consumables.Remedy);
        Register(ItemData_Consumables.SmokeBomb);
        Register(ItemData_Consumables.Tent);

        // Weapons
        Register(ItemData_Weapons.IronSword);
        Register(ItemData_Weapons.SteelSword);
        Register(ItemData_Weapons.MysticStaff);
        Register(ItemData_Weapons.HunterBow);
        Register(ItemData_Weapons.WarHammer);
        Register(ItemData_Weapons.BronzeDagger);
        Register(ItemData_Weapons.FlameTongue);
        Register(ItemData_Weapons.CrystalWand);
        Register(ItemData_Weapons.SerpentSpear);
        Register(ItemData_Weapons.RuneAxe);
        Register(ItemData_Weapons.ShadowBlade);
        Register(ItemData_Weapons.StarfallMace);

        // Armor / Helmets / Boots
        Register(ItemData_Armor.ChainMail);
        Register(ItemData_Armor.PlateArmor);
        Register(ItemData_Armor.IronHelm);
        Register(ItemData_Armor.LeatherBoots);
        Register(ItemData_Armor.SteelGreaves);
        Register(ItemData_Armor.PaddedVest);
        Register(ItemData_Armor.MageRobes);
        Register(ItemData_Armor.DragonscaleArmor);
        Register(ItemData_Armor.SteelHelm);
        Register(ItemData_Armor.WizardHat);
        Register(ItemData_Armor.HornedHelm);
        Register(ItemData_Armor.WindRunners);
        Register(ItemData_Armor.IronSabatons);

        // Relics
        Register(ItemData_Relics.CopperRing);
        Register(ItemData_Relics.SilverRing);
        Register(ItemData_Relics.BoneAmulet);
        Register(ItemData_Relics.JadeAmulet);
        Register(ItemData_Relics.GoldRing);
        Register(ItemData_Relics.BloodstoneRing);
        Register(ItemData_Relics.PhantomBand);
        Register(ItemData_Relics.IronTalisman);
        Register(ItemData_Relics.SunfireAmulet);
        Register(ItemData_Relics.CrownOfStars);

        // Crafting Materials
        Register(ItemData_CraftingMaterials.IronOre);
        Register(ItemData_CraftingMaterials.Leather);
        Register(ItemData_CraftingMaterials.Cloth);
        Register(ItemData_CraftingMaterials.WoodPlank);
        Register(ItemData_CraftingMaterials.ArcaneDust);
        Register(ItemData_CraftingMaterials.SlimeGel);
        Register(ItemData_CraftingMaterials.WolfPelt);
        Register(ItemData_CraftingMaterials.UndeadBone);
        Register(ItemData_CraftingMaterials.TrollHide);
        Register(ItemData_CraftingMaterials.DemonShard);

        // Auto-assign salvage components to equipment that has none defined
        AssignDefaultSalvageComponents();
    }

    /// <summary>
    /// Auto-assigns salvage components to equipment items that don't have
    /// manually defined salvage data. Components are determined by slot,
    /// rarity, and cost — producing a sensible material breakdown.
    /// </summary>
    private static void AssignDefaultSalvageComponents()
    {
        foreach (var item in items.Values)
        {
            if (item.Type != ItemType.Equipment) continue;
            if (item.SalvageComponents != null && item.SalvageComponents.Count > 0) continue;

            item.SalvageComponents = new List<SalvageComponent>();
            int tier = RarityTier(item.Rarity);

            switch (item.Slot)
            {
                case EquipmentSlot.Weapon:
                    item.SalvageComponents.Add(new SalvageComponent("mat_iron_ore", 1 + tier));
                    if (tier >= 1) item.SalvageComponents.Add(new SalvageComponent("mat_wood_plank", 1));
                    if (tier >= 2) item.SalvageComponents.Add(new SalvageComponent("mat_arcane_dust", tier - 1));
                    break;

                case EquipmentSlot.Armor:
                    item.SalvageComponents.Add(new SalvageComponent("mat_iron_ore", 1 + tier));
                    item.SalvageComponents.Add(new SalvageComponent("mat_leather", 1));
                    if (tier >= 2) item.SalvageComponents.Add(new SalvageComponent("mat_cloth", tier));
                    break;

                case EquipmentSlot.Relic1:
                case EquipmentSlot.Relic2:
                case EquipmentSlot.Relic3:
                    item.SalvageComponents.Add(new SalvageComponent("mat_arcane_dust", 1 + tier));
                    if (tier >= 1) item.SalvageComponents.Add(new SalvageComponent("mat_leather", 1));
                    if (tier >= 2) item.SalvageComponents.Add(new SalvageComponent("mat_undead_bone", 1));
                    break;
            }
        }
    }

    private static int RarityTier(ItemRarity rarity)
    {
        switch (rarity)
        {
            case ItemRarity.Common: return 0;
            case ItemRarity.Uncommon: return 1;
            case ItemRarity.Rare: return 2;
            case ItemRarity.Epic: return 3;
            case ItemRarity.Legendary: return 4;
            default: return 0;
        }
    }

    /// <summary>Registers an item definition.</summary>
    private static void Register(ItemDefinition def)
    {
        if (def == null || string.IsNullOrEmpty(def.Id)) return;
        if (!items.ContainsKey(def.Id)) items.Add(def.Id, def);
    }

    /// <summary>Gets an item by Id or null if not found.</summary>
    public static ItemDefinition Get(string id)
    {
        Ensure();
        if (string.IsNullOrEmpty(id)) return null;
        items.TryGetValue(id, out var def);
        return def;
    }

    /// <summary>Enumerates all item definitions.</summary>
    public static IEnumerable<ItemDefinition> All()
    {
        Ensure();
        return items.Values;
    }

    /// <summary>Enumerates items filtered by equipment slot.</summary>
    public static IEnumerable<ItemDefinition> BySlot(EquipmentSlot slot)
    {
        Ensure();
        foreach (var item in items.Values)
        {
            if (item.Slot == slot) yield return item;
        }
    }

    /// <summary>Enumerates items filtered by type.</summary>
    public static IEnumerable<ItemDefinition> ByType(ItemType type)
    {
        Ensure();
        foreach (var item in items.Values)
        {
            if (item.Type == type) yield return item;
        }
    }

    /// <summary>Enumerates vendor-purchasable crafting materials.</summary>
    public static IEnumerable<ItemDefinition> VendorMaterials()
    {
        Ensure();
        foreach (var item in items.Values)
        {
            if (item.Type == ItemType.CraftingMaterial && item.BaseCost <= 30)
                yield return item;
        }
    }
}

}
