using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Scripts.Data.Items;
using Scripts.Inventory;
using Scripts.Libraries;

namespace Scripts.Helpers
{
    /// <summary>
    /// SALVAGEHELPER - Instant breakdown of equipment into raw materials.
    /// <para>PURPOSE: Shared entry point for <see cref="Scripts.Hub.Sections.SalvageSection"/>.
    /// Unlike Blacksmith upgrades which take real time, salvage is immediate — the smith hands
    /// the disassembled parts back across the counter. This is the loop that gives unused gear
    /// a second life: rather than selling a sub-optimal weapon for gold, the player can recover
    /// the materials and invest them in a better upgrade path.</para>
    /// <para>RULES:
    /// <list type="bullet">
    /// <item>Only <see cref="ItemDefinition.CanSalvage"/> items are eligible — equipment with at
    /// least one <see cref="SalvageComponent"/>.</item>
    /// <item>Items currently equipped on any hero are filtered out by the UI (enforced at the
    /// section level — see <c>SalvageSection.EligibleItems()</c>).</item>
    /// <item>Items currently held by the Blacksmith (pending upgrade) are filtered out so we
    /// never salvage gear the smith still has.</item>
    /// </list></para>
    /// <para>RELATED FILES: ItemDefinition.cs (SalvageComponents), ItemLibrary.cs (AssignDefaultSalvageComponents),
    /// SalvageSection.cs, CraftJobHelper.cs</para>
    /// </summary>
    public static class SalvageHelper
    {
        /// <summary>True if this inventory entry points at an item that can be broken down.</summary>
        public static bool IsSalvageable(PlayerInventory.Entry entry)
        {
            if (entry == null || entry.Definition == null || entry.Count <= 0) return false;
            return entry.Definition.CanSalvage;
        }

        /// <summary>Runs a single salvage: removes one copy of <paramref name="itemId"/> and credits
        /// the breakdown materials to the inventory. Returns true on success.</summary>
        public static bool Salvage(string itemId, PlayerInventory inv)
        {
            if (string.IsNullOrEmpty(itemId) || inv == null) return false;
            var def = ItemLibrary.Get(itemId);
            if (def == null || !def.CanSalvage) return false;
            if (!inv.Contains(itemId, 1)) return false;

            if (!inv.Remove(itemId, 1)) return false;

            foreach (var comp in def.SalvageComponents)
            {
                if (comp == null || string.IsNullOrEmpty(comp.MaterialId) || comp.Count <= 0) continue;
                var matDef = ItemLibrary.Get(comp.MaterialId);
                if (matDef == null)
                {
                    Debug.LogWarning($"[SalvageHelper] Unknown material '{comp.MaterialId}' referenced by '{itemId}' — skipped.");
                    continue;
                }
                inv.Add(matDef, comp.Count);
            }
            return true;
        }

        /// <summary>Human-readable summary of what one salvage of this item yields, e.g.
        /// "2× Iron Ore, 1× Wood Plank". Returns "—" if the item has no breakdown.</summary>
        public static string FormatYield(ItemDefinition def)
        {
            if (def == null || def.SalvageComponents == null || def.SalvageComponents.Count == 0) return "—";
            var parts = new List<string>(def.SalvageComponents.Count);
            foreach (var comp in def.SalvageComponents)
            {
                if (comp == null || string.IsNullOrEmpty(comp.MaterialId) || comp.Count <= 0) continue;
                var matDef = ItemLibrary.Get(comp.MaterialId);
                string name = matDef != null ? matDef.DisplayName : comp.MaterialId;
                parts.Add($"{comp.Count}× {name}");
            }
            return parts.Count == 0 ? "—" : string.Join(", ", parts);
        }
    }
}
