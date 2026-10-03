using System;
using System.Collections.Generic;
using System.Reflection;
using Scripts.Data;
using Scripts.Data.Items;
using Scripts.Helpers;
using Scripts.Models;

namespace Scripts.Services
{
    /// <summary>
    /// COMBATLOADOUTS - Decides what the Row-13 <see cref="Scripts.Canvas.AbilityBar"/> shows for a
    /// hero: the player's saved per-hero bar first, the class preset second.
    ///
    /// <para>SOURCE ORDER: (1) <c>HeroEquipmentSave.AbilityBarSlots</c> — the bar the player edits in
    /// the Abilities scene — when it holds at least one filled slot; (2) the class preset
    /// <see cref="HeroLoadouts.For"/>. Slot positions are kept (slot 3 in the Abilities scene is slot 3
    /// in combat).</para>
    ///
    /// <para>SLOT MAPPING: an item slot becomes a per-slot consumable stack linked to its
    /// <see cref="ItemDefinition"/> (so <c>OnUseSpellName</c> items such as Sleep Dart still route
    /// through their spell); its charges are the owned count capped at <see cref="ItemDefinition.MaxStack"/>.
    /// A named slot resolves against the <see cref="ManaAbilities"/> catalog by name. A weapon slot,
    /// or a name/item the catalog doesn't know, renders empty.</para>
    ///
    /// <para>CACHE: resolved bars are cached per class for one battle so item charges survive
    /// re-selecting a hero. <see cref="Scripts.Canvas.AbilityBar"/> clears it when the bar is built.</para>
    ///
    /// <para>CONSUMPTION: items are single-use consumables. An item slot built from the saved bar
    /// draws its charges from the inventory, so <see cref="TryUseItem(ManaAbility)"/> spends one
    /// charge AND removes one of that item from the save's inventory. Preset item slots
    /// (<see cref="HeroLoadouts"/>) are not backed by the inventory and only spend a charge.</para>
    /// </summary>
    public static class CombatLoadouts
    {
        /// <summary>Slots on the combat bar — the hard unlock max (§4.7).</summary>
        public const int SlotCount = AbilitySlotProgression.MaxSlots;

        private static readonly Dictionary<CharacterClass, IReadOnlyList<ManaAbility>> cache =
            new Dictionary<CharacterClass, IReadOnlyList<ManaAbility>>();

        /// <summary>Item slots whose charges came from the inventory (reference identity).</summary>
        private static readonly HashSet<ManaAbility> inventoryBacked = new HashSet<ManaAbility>();

        /// <summary>Forget every resolved bar (call at battle start).</summary>
        public static void ResetForBattle()
        {
            cache.Clear();
            inventoryBacked.Clear();
        }

        /// <summary>True when <paramref name="item"/> is a saved-bar item slot drawn from the inventory.</summary>
        public static bool IsInventoryBacked(ManaAbility item) => item != null && inventoryBacked.Contains(item);

        /// <summary>Use one charge of an item slot against the live save's inventory.</summary>
        public static bool TryUseItem(ManaAbility item)
            => TryUseItem(item, ProfileHelper.CurrentProfile?.CurrentSave?.Inventory);

        /// <summary>Spend one charge of <paramref name="item"/>; when the slot is inventory-backed, also
        /// remove one of its item from <paramref name="inventory"/>. False (nothing changes) when the
        /// slot is empty.</summary>
        public static bool TryUseItem(ManaAbility item, InventorySaveData inventory)
        {
            if (item == null || !item.TryConsumeCharge()) return false;
            if (IsInventoryBacked(item) && inventory?.Items != null)
            {
                for (int i = 0; i < inventory.Items.Count; i++)
                {
                    var e = inventory.Items[i];
                    if (e == null || e.ItemId != item.SourceItemId || e.Count <= 0) continue;
                    e.Count--;
                    if (e.Count <= 0) inventory.Items.RemoveAt(i);
                    break;
                }
            }
            return true;
        }

        /// <summary>Debug-only: replace the bar a class shows for the rest of this battle.</summary>
        public static void SetBattleOverride(CharacterClass characterClass, IReadOnlyList<ManaAbility> loadout)
        {
            cache[characterClass] = loadout;
        }

        /// <summary>The bar for <paramref name="characterClass"/> in the live save (cached per battle).</summary>
        public static IReadOnlyList<ManaAbility> For(CharacterClass characterClass)
        {
            if (cache.TryGetValue(characterClass, out var cached) && cached != null) return cached;
            var save = ProfileHelper.CurrentProfile?.CurrentSave;
            var heroSave = save?.Equipment?.Heroes?.Find(h => h != null && h.CharacterClass == characterClass);
            var resolved = Resolve(characterClass, heroSave, save?.Inventory);
            cache[characterClass] = resolved;
            return resolved;
        }

        /// <summary>Pure resolution: the saved bar when it has any filled slot, else the class preset.</summary>
        public static IReadOnlyList<ManaAbility> Resolve(CharacterClass characterClass, HeroEquipmentSave heroSave, InventorySaveData inventory)
        {
            if (HasSavedBar(heroSave))
            {
                var bar = new ManaAbility[SlotCount];
                for (int i = 0; i < SlotCount && i < heroSave.AbilityBarSlots.Count; i++)
                    bar[i] = FromSlot(heroSave.AbilityBarSlots[i], inventory);
                return bar;
            }
            return HeroLoadouts.For(characterClass);
        }

        /// <summary>True when the hero's saved bar has at least one filled slot.</summary>
        public static bool HasSavedBar(HeroEquipmentSave heroSave)
        {
            if (heroSave?.AbilityBarSlots == null) return false;
            foreach (var slot in heroSave.AbilityBarSlots)
                if (slot != null && !slot.IsEmpty) return true;
            return false;
        }

        /// <summary>Map one saved slot to a combat-bar entry (null = empty slot).</summary>
        public static ManaAbility FromSlot(AbilityBarSlotSave slot, InventorySaveData inventory)
        {
            if (slot == null || slot.IsEmpty) return null;
            if (slot.IsItem)
            {
                var def = ItemLibrary.Get(slot.ItemId);
                if (def == null || !def.IsConsumable)
                {
                    UnityEngine.Debug.LogWarning($"[CombatLoadouts] Saved item '{slot.ItemId}' is not a known consumable — slot left empty.");
                    return null;
                }
                int stack = Math.Max(0, Math.Min(def.MaxStack, OwnedCount(inventory, def.Id)));
                var consumable = ManaAbilities.NewConsumable(def.DisplayName, stack, def.Id);
                inventoryBacked.Add(consumable);
                return consumable;
            }
            if (slot.IsAbility)
            {
                var ability = CatalogByName(slot.AbilityName);
                if (ability == null)
                    UnityEngine.Debug.LogWarning($"[CombatLoadouts] Saved ability '{slot.AbilityName}' has no combat-bar entry — slot left empty.");
                return ability;
            }
            // Weapon-swap slots have no combat-bar kind.
            return null;
        }

        private static int OwnedCount(InventorySaveData inventory, string itemId)
        {
            if (inventory?.Items == null) return 0;
            int total = 0;
            foreach (var e in inventory.Items)
                if (e != null && e.ItemId == itemId) total += e.Count;
            return total;
        }

        private static Dictionary<string, ManaAbility> catalog;

        /// <summary>Skill/Spell entries of <see cref="ManaAbilities"/> by name (case-insensitive).</summary>
        public static ManaAbility CatalogByName(string name)
        {
            if (string.IsNullOrEmpty(name)) return null;
            if (catalog == null)
            {
                catalog = new Dictionary<string, ManaAbility>(StringComparer.OrdinalIgnoreCase);
                foreach (var f in typeof(ManaAbilities).GetFields(BindingFlags.Public | BindingFlags.Static))
                {
                    if (f.FieldType != typeof(ManaAbility)) continue;
                    if (f.GetValue(null) is ManaAbility a && a.Kind != AbilityKind.Item && !catalog.ContainsKey(a.Name))
                        catalog[a.Name] = a;
                }
            }
            return catalog.TryGetValue(name, out var found) ? found : null;
        }
    }
}
