using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using g = Scripts.Helpers.GameHelper;
using Scripts.Canvas;
using Scripts.Data.Actor;
using Scripts.Data.Items;
using Scripts.Data.Skills;
using Scripts.Effects;
using Scripts.Factories;
using Scripts.Helpers;
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

namespace Scripts.Hub.Sections
{
    /// <summary>
    /// INVENTORYSECTION - Read-only browser for every item the party owns.
    /// <para>PURPOSE: Four filter tabs (All / Equipment / Consumables / Materials) swap the list
    /// contents. Selecting a row shows the full description + stats + sell value in the detail
    /// label. No transactions happen here — buying/selling is the Vendor's job.</para>
    /// <para>RELATED FILES: HubManager.cs, PlayerInventory.cs, ItemDefinition.cs</para>
    /// </summary>
    public class InventorySection : HubSection
    {
        private enum Filter { All, Equipment, Consumable, Material }
        private Filter filter = Filter.All;
        private ItemDefinition selected;

        protected override void OnActivated()
        {
            Wire(FindButton(GameObjectHelper.Hub.FilterAll),   () => { filter = Filter.All;        selected = null; Refresh(); });
            Wire(FindButton(GameObjectHelper.Hub.FilterEquip), () => { filter = Filter.Equipment;  selected = null; Refresh(); });
            Wire(FindButton(GameObjectHelper.Hub.FilterCons),  () => { filter = Filter.Consumable; selected = null; Refresh(); });
            Wire(FindButton(GameObjectHelper.Hub.FilterMats),  () => { filter = Filter.Material;   selected = null; Refresh(); });
        }

        public override void Refresh()
        {
            var list = FindList("ItemList/Viewport/Content");
            if (list == null) return;
            ClearList(list);

            foreach (var entry in Entries())
            {
                var def = entry.Definition;
                var row = HubItemRowFactory.Create(list);
                HubItemRowFactory.SetIcon(row, def);
                HubItemRowFactory.SetLabel(row, def.DisplayName);
                HubItemRowFactory.SetSubLabel(row, $"x{entry.Count}  —  {TypeTag(def.Type)}");
                HubItemRowFactory.SetLabelColor(row, HubItemRowFactory.RarityColor(def.Rarity));
                var captured = def;
                row.GetComponent<Button>().onClick.AddListener(() => { selected = captured; Refresh(); });
                HubItemRowFactory.SetSelected(row, selected != null && selected.Id == def.Id);
            }
            UpdateDetail();
        }

        private IEnumerable<PlayerInventory.Entry> Entries()
        {
            var all = Hub.Inventory.All().OrderBy(e => e.Definition.DisplayName);
            switch (filter)
            {
                case Filter.Equipment:  return all.Where(e => e.Definition.Type == ItemType.Equipment);
                case Filter.Consumable: return all.Where(e => e.Definition.Type == ItemType.Consumable);
                case Filter.Material:   return all.Where(e => e.Definition.Type == ItemType.CraftingMaterial);
                default:                return all;
            }
        }

        private static string TypeTag(ItemType t) => t switch
        {
            ItemType.Equipment        => "Gear",
            ItemType.Consumable       => "Consumable",
            ItemType.CraftingMaterial => "Material",
            ItemType.QuestItem        => "Quest",
            _                         => "Misc"
        };

        private void UpdateDetail()
        {
            var detail = FindLabel(GameObjectHelper.Hub.DetailLabel);
            if (detail == null) return;
            if (selected == null)
            {
                detail.text = "<b>Inventory</b>\nSelect an item for details.";
                return;
            }
            var sb = new System.Text.StringBuilder();
            sb.AppendLine($"<b>{selected.DisplayName}</b>");
            if (!string.IsNullOrEmpty(selected.Description)) sb.AppendLine(selected.Description);
            sb.AppendLine();
            if (selected.Type == ItemType.Equipment)
            {
                sb.AppendLine($"Slot: {selected.Slot}");
                AddStat(sb, "STR", selected.Strength);
                AddStat(sb, "VIT", selected.Vitality);
                AddStat(sb, "AGI", selected.Agility);
                AddStat(sb, "INT", selected.Intelligence);
                AddStat(sb, "WIS", selected.Wisdom);
                AddStat(sb, "LCK", selected.Luck);
            }
            sb.Append($"Sell value: {HubTheme.FormatGold(selected.ComputedSellValue)}");
            detail.text = sb.ToString();
        }

        private static void AddStat(System.Text.StringBuilder sb, string label, float v)
        {
            if (v == 0) return;
            sb.AppendLine($"{label} {(v >= 0 ? "+" : "")}{v:0}");
        }
    }
}
