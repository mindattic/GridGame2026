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
    /// <summary>Elemental affinity a weapon can be enchanted with.</summary>
    public enum Element
    {
        Flame,
        Frost,
        Spark,
        Shadow,
    }

    /// <summary>
    /// ENCHANTRECIPE - Describes an elemental enchantment recipe for a weapon.
    /// <para>PURPOSE: The Enchanter consumes a base weapon + one elemental essence +
    /// a small amount of arcane dust + gold to produce an enchanted variant. Enchanted
    /// weapons are new <see cref="ItemDefinition"/>s (DisplayName "Iron Sword (Flame)")
    /// registered in <see cref="ItemLibrary"/> so they flow through save, inventory,
    /// and equip naturally — the same pattern used by <see cref="UpgradeRecipe"/>.</para>
    /// <para>RELATED FILES: EnchantLibrary.cs, EnchantSection.cs, ItemData_Essences.cs, ItemLibrary.cs</para>
    /// </summary>
    public class EnchantRecipe
    {
        public ItemDefinition From;       // base weapon (e.g., IronSword)
        public ItemDefinition To;         // enchanted result (e.g., IronSword_flame)
        public Element Affinity;
        public int GoldCost;
        public List<(string itemId, int count)> Materials = new List<(string, int)>();

        public bool CanEnchant(PlayerInventory inv)
        {
            if (inv == null || From == null || To == null) return false;
            if (inv.Gold < GoldCost) return false;
            if (!inv.Contains(From.Id, 1)) return false;
            foreach (var m in Materials)
                if (!inv.Contains(m.itemId, m.count)) return false;
            return true;
        }

        /// <summary>Synchronous variant: consumes inputs and adds the enchanted item immediately.
        /// Typically unused in favor of <c>CraftJobHelper.StartEnchant</c> which time-gates the result.</summary>
        public bool Execute(PlayerInventory inv)
        {
            if (!CanEnchant(inv)) return false;
            inv.Gold -= GoldCost;
            inv.Remove(From.Id, 1);
            foreach (var m in Materials) inv.Remove(m.itemId, m.count);
            inv.Add(To, 1);
            return true;
        }
    }
}
