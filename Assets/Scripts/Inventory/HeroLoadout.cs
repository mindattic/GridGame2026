using System.Collections.Generic;
using Scripts.Helpers;
using Scripts.Canvas;
using Scripts.Data.Actor;
using Scripts.Data.Items;
using Scripts.Data.Skills;
using Scripts.Effects;
using Scripts.Factories;
using Scripts.Hub;
using Scripts.Instances;
using Scripts.Instances.Actor;
using Scripts.Instances.Board;
using Scripts.Instances.SynergyLine;
using Scripts.Libraries;
using Scripts.Managers;
using Scripts.Models;
using Scripts.Models.Actor;
using Scripts.Overworld;
using Scripts.Sequences;
using Scripts.Serialization;
using Scripts.Utilities;

namespace Scripts.Inventory
{
/// <summary>
/// HEROLOADOUT - Individual hero's equipment and skills.
/// 
/// PURPOSE:
/// Tracks equipped skills, consumable items, and slot-based
/// equipment for a single hero character.
/// 
/// SLOTS:
/// - Skills: Equipped active/passive abilities
/// - Items: Consumable items for battle
/// - Equipment: Slot-based (Weapon, Armor, Helmet, Boots, Ring, Amulet)
/// 
/// RELATED FILES:
/// - PartyLoadout: Groups all hero loadouts
/// - PlayerInventory.cs: Item ownership
/// - SkillDefinition.cs: Skill data
/// - HeroEquipmentSave: Save structure
/// </summary>
public class HeroLoadout
{
    public CharacterClass CharacterClass;
    public List<SkillDefinition> Skills = new List<SkillDefinition>();
    public List<ItemDefinition> Items = new List<ItemDefinition>();
    public List<ItemDefinition> Equipment = new List<ItemDefinition>();

    // Slot-based equipment
    public Dictionary<EquipmentSlot, ItemDefinition> EquippedSlots = new Dictionary<EquipmentSlot, ItemDefinition>();

    /// <summary>Gets the item in a specific slot, or null.</summary>
    public ItemDefinition GetEquipped(EquipmentSlot slot)
    {
        EquippedSlots.TryGetValue(slot, out var item);
        return item;
    }

    /// <summary>Equips an item into its slot, returning the previously equipped item (or null).</summary>
    public ItemDefinition Equip(ItemDefinition item)
    {
        if (item == null || item.Slot == EquipmentSlot.None) return null;
        EquippedSlots.TryGetValue(item.Slot, out var previous);
        EquippedSlots[item.Slot] = item;
        return previous;
    }

    /// <summary>Unequips the item in the given slot, returning it.</summary>
    public ItemDefinition Unequip(EquipmentSlot slot)
    {
        if (!EquippedSlots.TryGetValue(slot, out var item)) return null;
        EquippedSlots.Remove(slot);
        return item;
    }

    /// <summary>Loads equipment from save data.</summary>
    public void LoadFromSave(HeroEquipmentSave save)
    {
        if (save == null) return;
        TryEquipFromId(save.WeaponId);
        TryEquipFromId(save.ArmorId);
        TryEquipFromId(save.HelmetId);
        TryEquipFromId(save.BootsId);
        TryEquipFromId(save.RingId);
        TryEquipFromId(save.AmuletId);
    }

    /// <summary>Exports equipment to save data.</summary>
    public HeroEquipmentSave ToSave()
    {
        return new HeroEquipmentSave
        {
            CharacterClass = CharacterClass,
            WeaponId = GetEquipped(EquipmentSlot.Weapon)?.Id,
            ArmorId = GetEquipped(EquipmentSlot.Armor)?.Id,
            HelmetId = GetEquipped(EquipmentSlot.Helmet)?.Id,
            BootsId = GetEquipped(EquipmentSlot.Boots)?.Id,
            RingId = GetEquipped(EquipmentSlot.Ring)?.Id,
            AmuletId = GetEquipped(EquipmentSlot.Amulet)?.Id,
        };
    }

    private void TryEquipFromId(string itemId)
    {
        if (string.IsNullOrEmpty(itemId)) return;
        var def = ItemLibrary.Get(itemId);
        if (def != null) Equip(def);
    }
}

/// <summary>
/// PARTYLOADOUT - All hero loadouts for active party.
/// 
/// PURPOSE:
/// Groups HeroLoadout instances for each party member
/// and provides accessor methods.
/// </summary>
public class PartyLoadout
{
    public Dictionary<CharacterClass, HeroLoadout> HeroLoadouts = new Dictionary<CharacterClass, HeroLoadout>();

    /// <summary>Gets or creates the loadout for a hero.</summary>
    public HeroLoadout Get(CharacterClass hero)
    {
        if (!HeroLoadouts.TryGetValue(hero, out var loadout))
        {
            loadout = new HeroLoadout { CharacterClass = hero };
            HeroLoadouts[hero] = loadout;
        }
        return loadout;
    }

    /// <summary>Loads all equipment from save data.</summary>
    public void LoadFromSave(EquipmentSaveData save)
    {
        if (save?.Heroes == null) return;
        foreach (var heroSave in save.Heroes)
        {
            var loadout = Get(heroSave.CharacterClass);
            loadout.LoadFromSave(heroSave);
        }
    }

    /// <summary>Exports all equipment to save data.</summary>
    public EquipmentSaveData ToSave()
    {
        var save = new EquipmentSaveData();
        foreach (var kvp in HeroLoadouts)
        {
            save.Heroes.Add(kvp.Value.ToSave());
        }
        return save;
    }
}

}
