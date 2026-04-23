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
    /// SALVAGESECTION - Break equipment down into raw materials.
    /// <para>PURPOSE: Closes the loot loop. Every weapon, armor piece, and relic carries
    /// <see cref="ItemDefinition.SalvageComponents"/> (auto-assigned by ItemLibrary) describing what
    /// materials disassembly yields. Unlike selling (which gives gold), salvage gives materials —
    /// the player chooses between liquid gold and the specific inputs needed for the next upgrade.
    /// Items handed to the Blacksmith (pending upgrade) are hidden to prevent ghost-salvage of gear
    /// the smith is still holding.</para>
    /// <para>FLOW: Pick an item → confirm → inventory swap is instant (unlike Blacksmith/Alchemist
    /// which are time-gated). Salvage is a craft-in-reverse; the smith returns the parts across the
    /// counter without delay.</para>
    /// <para>RELATED FILES: SalvageHelper.cs, ItemDefinition.cs, ItemLibrary.cs, CraftJobHelper.cs</para>
    /// </summary>
    public class SalvageSection : HubSection
    {
        private ItemDefinition selected;

        protected override void OnActivated()
        {
            var confirm = FindButton("ConfirmButton");
            Wire(confirm, ConfirmPressed);
        }

        public override void Refresh()
        {
            var list = FindList("ItemList/Viewport/Content");
            if (list == null) return;
            ClearList(list);

            foreach (var entry in EligibleItems())
                AddRow(list, entry);

            UpdateDetail();
            UpdateConfirmButton();
        }

        private IEnumerable<PlayerInventory.Entry> EligibleItems()
        {
            return Hub.Inventory.All()
                .Where(e => SalvageHelper.IsSalvageable(e)
                    && !CraftJobHelper.IsHeldByAnyVendor(e.Definition.Id))
                .OrderByDescending(e => (int)e.Definition.Rarity)
                .ThenBy(e => e.Definition.DisplayName);
        }

        private void AddRow(Transform list, PlayerInventory.Entry entry)
        {
            var def = entry.Definition;
            var row = HubItemRowFactory.Create(list);
            HubItemRowFactory.SetIcon(row, def);
            HubItemRowFactory.SetLabel(row, entry.Count > 1 ? $"{def.DisplayName}  ×{entry.Count}" : def.DisplayName);
            HubItemRowFactory.SetLabelColor(row, HubItemRowFactory.RarityColor(def.Rarity));
            HubItemRowFactory.SetSubLabel(row, $"<color=#CCBB77>→ {SalvageHelper.FormatYield(def)}</color>");

            var captured = def;
            row.GetComponent<Button>().onClick.AddListener(() => { selected = captured; Refresh(); });
            HubItemRowFactory.SetSelected(row, selected != null && selected.Id == def.Id);
        }

        private void ConfirmPressed()
        {
            if (selected == null) return;
            if (!Hub.Inventory.Contains(selected.Id, 1)) { selected = null; Refresh(); return; }

            string itemName = selected.DisplayName;
            string yield = SalvageHelper.FormatYield(selected);
            if (!SalvageHelper.Salvage(selected.Id, Hub.Inventory))
            {
                HubToast.Show($"Couldn't salvage {itemName}.");
                return;
            }
            HubToast.Show($"Salvaged {itemName} → {yield}");

            // If we exhausted the last copy, clear the selection so the UI doesn't get stuck.
            if (!Hub.Inventory.Contains(selected.Id, 1)) selected = null;
            Hub.PersistAndRefresh();
        }

        private void UpdateConfirmButton()
        {
            var btn = FindButton("ConfirmButton");
            if (btn == null) return;
            var label = btn.GetComponentInChildren<TextMeshProUGUI>();

            if (selected == null)
            {
                btn.interactable = false;
                if (label != null) label.text = "Select an item";
                return;
            }

            int owned = Hub.Inventory.CountOf(selected.Id);
            if (owned <= 0)
            {
                btn.interactable = false;
                if (label != null) label.text = "Out of stock";
                return;
            }

            btn.interactable = true;
            if (label != null) label.text = "Salvage";
        }

        private void UpdateDetail()
        {
            var detail = FindLabel(GameObjectHelper.Hub.DetailLabel);
            if (detail == null) return;

            if (selected == null)
            {
                int eligible = EligibleItems().Count();
                if (eligible == 0)
                {
                    detail.text = "<b>Salvage</b>\nNo equipment available to break down.\n\nSalvage trades unused gear for raw materials — more valuable than selling when you need specific ingredients for an upgrade.";
                }
                else
                {
                    detail.text = $"<b>Salvage</b>\n{eligible} item{(eligible == 1 ? "" : "s")} eligible for breakdown.\n\nSelect a weapon, armor piece, or relic to see what materials you'd recover.";
                }
                return;
            }

            int owned = Hub.Inventory.CountOf(selected.Id);
            int sellValue = selected.ComputedSellValue;
            var sb = new System.Text.StringBuilder();
            sb.AppendLine($"<b>{selected.DisplayName}</b>");
            if (!string.IsNullOrEmpty(selected.Description)) sb.AppendLine(selected.Description);
            sb.AppendLine();
            sb.AppendLine($"You own: <b>{owned}</b>");
            sb.AppendLine($"Sell value: {HubTheme.FormatGold(sellValue)}");
            sb.AppendLine();
            sb.AppendLine("<color=#CCBB77><b>Salvage yields:</b></color>");
            sb.AppendLine(SalvageHelper.FormatYield(selected));
            detail.text = sb.ToString();
        }
    }
}
