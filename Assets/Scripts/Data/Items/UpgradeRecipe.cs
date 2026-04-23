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
    /// UPGRADERECIPE - Describes a +N upgrade for an equipment item.
    /// <para>PURPOSE: Blacksmith consumes materials + gold to transform an owned
    /// <see cref="ItemDefinition"/> into its +1 / +2 / +3 variant. Upgraded items are new
    /// <see cref="ItemDefinition"/>s (DisplayName "Iron Sword +1") registered in
    /// <see cref="ItemLibrary"/> so they round-trip through save and equip flows naturally.</para>
    /// <para>RELATED FILES: UpgradeLibrary.cs, BlacksmithSection.cs, ItemLibrary.cs</para>
    /// </summary>
    public class UpgradeRecipe
    {
        public ItemDefinition From;                                   // base item, e.g. IronSword
        public ItemDefinition To;                                     // result item, e.g. IronSword+1
        public int GoldCost;
        public List<(string itemId, int count)> Materials = new List<(string, int)>();

        public bool CanUpgrade(PlayerInventory inv)
        {
            if (inv == null || From == null || To == null) return false;
            if (inv.Gold < GoldCost) return false;
            if (!inv.Contains(From.Id, 1)) return false;
            foreach (var m in Materials)
                if (!inv.Contains(m.itemId, m.count)) return false;
            return true;
        }

        /// <summary>Consumes the base item + materials + gold, adds the upgraded item. Returns true on success.</summary>
        public bool Execute(PlayerInventory inv)
        {
            if (!CanUpgrade(inv)) return false;
            inv.Gold -= GoldCost;
            inv.Remove(From.Id, 1);
            foreach (var m in Materials) inv.Remove(m.itemId, m.count);
            inv.Add(To, 1);
            return true;
        }
    }
}
