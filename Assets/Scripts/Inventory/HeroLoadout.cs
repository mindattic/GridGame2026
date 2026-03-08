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

    /// <summary>Equipped abilities (skills + consumable items). Max 5 slots.</summary>
    public List<Ability> EquippedAbilities = new List<Ability>();

    /// <summary>Maximum number of ability slots per hero.</summary>
    public const int MaxAbilitySlots = 5;

    // Slot-based equipment
    public Dictionary<EquipmentSlot, ItemDefinition> EquippedSlots = new Dictionary<EquipmentSlot, ItemDefinition>();

    /// <summary>Gets the item in a specific slot, or null.</summary>
    public ItemDefinition GetEquipped(EquipmentSlot slot)
    {
        EquippedSlots.TryGetValue(slot, out var item);
        return item;
    }

    /// <summary>Adds an ability to the loadout if there is room.</summary>
    public bool AddAbility(Ability ability)
    {
        if (ability == null || EquippedAbilities.Count >= MaxAbilitySlots) return false;
        EquippedAbilities.Add(ability);
        return true;
    }

    /// <summary>Removes an ability by index.</summary>
    public void RemoveAbility(int index)
    {
        if (index >= 0 && index < EquippedAbilities.Count)
            EquippedAbilities.RemoveAt(index);
    }

    /// <summary>Gets all active abilities (non-passive, non-reactive).</summary>
    public List<Ability> GetActiveAbilities()
    {
        var result = new List<Ability>();
        foreach (var a in EquippedAbilities)
            if (a != null && a.IsActive) result.Add(a);
        return result;
    }

    /// <summary>Equips an item into its slot, returning the previously equipped item (or null).</summary>
    public ItemDefinition Equip(ItemDefinition item)
    {
        if (item == null || item.Slot == EquipmentSlot.None) return null;

        // Relic items: find the first open relic slot, or replace in Relic1 if all full
        if (EquipmentSlotHelper.IsRelicSlot(item.Slot))
        {
            return EquipRelic(item);
        }

        EquippedSlots.TryGetValue(item.Slot, out var previous);
        EquippedSlots[item.Slot] = item;
        return previous;
    }

    /// <summary>Equips a relic into a specific slot, returning the previously equipped item.</summary>
    public ItemDefinition EquipToSlot(ItemDefinition item, EquipmentSlot targetSlot)
    {
        if (item == null) return null;
        EquippedSlots.TryGetValue(targetSlot, out var previous);
        EquippedSlots[targetSlot] = item;
        return previous;
    }

    /// <summary>Finds the first open relic slot and equips the item there.</summary>
    private ItemDefinition EquipRelic(ItemDefinition item)
    {
        var relicSlots = new[] { EquipmentSlot.Relic1, EquipmentSlot.Relic2, EquipmentSlot.Relic3 };
        foreach (var slot in relicSlots)
        {
            if (!EquippedSlots.ContainsKey(slot) || EquippedSlots[slot] == null)
            {
                EquippedSlots[slot] = item;
                return null;
            }
        }
        // All full — replace Relic1
        EquippedSlots.TryGetValue(EquipmentSlot.Relic1, out var previous);
        EquippedSlots[EquipmentSlot.Relic1] = item;
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
        TryEquipFromIdToSlot(save.Relic1Id, EquipmentSlot.Relic1);
        TryEquipFromIdToSlot(save.Relic2Id, EquipmentSlot.Relic2);
        TryEquipFromIdToSlot(save.Relic3Id, EquipmentSlot.Relic3);
    }

    /// <summary>Exports equipment to save data.</summary>
    public HeroEquipmentSave ToSave()
    {
        return new HeroEquipmentSave
        {
            CharacterClass = CharacterClass,
            WeaponId = GetEquipped(EquipmentSlot.Weapon)?.Id,
            ArmorId = GetEquipped(EquipmentSlot.Armor)?.Id,
            Relic1Id = GetEquipped(EquipmentSlot.Relic1)?.Id,
            Relic2Id = GetEquipped(EquipmentSlot.Relic2)?.Id,
            Relic3Id = GetEquipped(EquipmentSlot.Relic3)?.Id,
        };
    }

    private void TryEquipFromId(string itemId)
    {
        if (string.IsNullOrEmpty(itemId)) return;
        var def = ItemLibrary.Get(itemId);
        if (def != null) Equip(def);
    }

    private void TryEquipFromIdToSlot(string itemId, EquipmentSlot targetSlot)
    {
        if (string.IsNullOrEmpty(itemId)) return;
        var def = ItemLibrary.Get(itemId);
        if (def != null) EquippedSlots[targetSlot] = def;
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
