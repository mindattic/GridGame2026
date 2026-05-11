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
    /// VENDORSECTION - Buy raw materials / basic consumables, sell anything.
    /// <para>PURPOSE: Entry vendor for loot loops — materials (IronOre, Leather, Cloth, ArcaneDust,
    /// Wood) fuel the Alchemist and Blacksmith. Sell-mode dumps loot for gold. Two tabs (Buy/Sell)
    /// toggle which list is visible.</para>
    /// <para>RELATED FILES: HubManager.cs, HubItemRowFactory.cs, ItemLibrary.cs</para>
    /// </summary>
    public class VendorSection : HubSection
    {
        private enum Mode { Buy, Sell }
        private enum Category { All, Equipment, Consumables, Materials }
        private Mode mode = Mode.Buy;
        private Category category = Category.All;
        private ItemDefinition selected;

        protected override void OnActivated()
        {
            Wire(FindButton(GameObjectHelper.Hub.BuyTab), () => { mode = Mode.Buy; selected = null; Refresh(); });
            Wire(FindButton(GameObjectHelper.Hub.SellTab), () => { mode = Mode.Sell; selected = null; Refresh(); });
            Wire(FindButton(GameObjectHelper.Hub.FilterAll),   () => { category = Category.All;         selected = null; Refresh(); });
            Wire(FindButton(GameObjectHelper.Hub.FilterEquip), () => { category = Category.Equipment;   selected = null; Refresh(); });
            Wire(FindButton(GameObjectHelper.Hub.FilterCons),  () => { category = Category.Consumables; selected = null; Refresh(); });
            Wire(FindButton(GameObjectHelper.Hub.FilterMats),  () => { category = Category.Materials;   selected = null; Refresh(); });
            var confirm = FindButton("ConfirmButton");
            Wire(confirm, ConfirmTransaction);
        }

        public override void Refresh()
        {
            var list = FindList("ItemList/Viewport/Content");
            if (list == null) return;
            ClearList(list);

            UpdateTabTints();

            var items = (mode == Mode.Buy ? BuyCatalogue() : SellCatalogue()).Where(PassesCategoryFilter);
            foreach (var item in items)
            {
                var row = HubItemRowFactory.Create(list);
                HubItemRowFactory.SetIcon(row, item);
                HubItemRowFactory.SetLabel(row, item.DisplayName);
                int price = mode == Mode.Buy ? item.BaseCost : item.ComputedSellValue;
                int owned = Hub.Inventory.CountOf(item.Id);
                string owns = owned > 0 ? $"  [owned: {owned}]" : "";
                string priceText = HubTheme.FormatGold(price);
                bool canAfford = mode == Mode.Buy ? Hub.Inventory.Gold >= price : owned > 0;
                HubItemRowFactory.SetSubLabel(row, $"{HubTheme.ColorByAffordable(priceText, canAfford)}{owns}");
                HubItemRowFactory.SetLabelColor(row, HubItemRowFactory.RarityColor(item.Rarity));
                var captured = item;
                row.GetComponent<Button>().onClick.AddListener(() => { selected = captured; Refresh(); });
                HubItemRowFactory.SetSelected(row, selected != null && selected.Id == item.Id);
            }
            UpdateDetail();
        }

        private IEnumerable<ItemDefinition> BuyCatalogue()
        {
            // Materials at entry-level prices; basic healing potion available from the jump.
            foreach (var mat in ItemLibrary.VendorMaterials()) yield return mat;
            var basicPotion = ItemLibrary.Get("healing_potion_basic");
            if (basicPotion != null) yield return basicPotion;
        }

        private IEnumerable<ItemDefinition> SellCatalogue()
        {
            return Hub.Inventory.All()
                .Select(e => e.Definition)
                .Where(d => d != null && d.ComputedSellValue > 0
                    && !CraftJobHelper.IsHeldByAnyVendor(d.Id))
                .OrderByDescending(d => (int)d.Rarity)
                .ThenBy(d => d.DisplayName);
        }

        private bool PassesCategoryFilter(ItemDefinition d)
        {
            if (d == null) return false;
            return category switch
            {
                Category.All => true,
                Category.Equipment => d.Type == ItemType.Equipment,
                Category.Consumables => d.Type == ItemType.Consumable,
                Category.Materials => d.Type == ItemType.CraftingMaterial,
                _ => true,
            };
        }

        private void UpdateTabTints()
        {
            // Mode row
            TintTab(FindButton(GameObjectHelper.Hub.BuyTab),  mode == Mode.Buy);
            TintTab(FindButton(GameObjectHelper.Hub.SellTab), mode == Mode.Sell);
            // Filter row
            TintTab(FindButton(GameObjectHelper.Hub.FilterAll),   category == Category.All);
            TintTab(FindButton(GameObjectHelper.Hub.FilterEquip), category == Category.Equipment);
            TintTab(FindButton(GameObjectHelper.Hub.FilterCons),  category == Category.Consumables);
            TintTab(FindButton(GameObjectHelper.Hub.FilterMats),  category == Category.Materials);
        }

        private static void TintTab(Button btn, bool active)
        {
            if (btn == null) return;
            var img = btn.GetComponent<Image>();
            if (img == null) return;
            img.color = active ? HubTheme.NavActive : HubTheme.NavIdle;
        }

        private void UpdateDetail()
        {
            var detail = FindLabel(GameObjectHelper.Hub.DetailLabel);
            if (detail == null) return;
            if (selected == null)
            {
                detail.text = mode == Mode.Buy
                    ? "<b>Merchant</b>\nBrowse to buy materials and basic supplies."
                    : "<b>Merchant</b>\nBrowse to sell unused items.";
                return;
            }
            int price = mode == Mode.Buy ? selected.BaseCost : selected.ComputedSellValue;
            string verb = mode == Mode.Buy ? "Buy" : "Sell";
            detail.text = $"<b>{selected.DisplayName}</b>\n{selected.Description}\n\n{verb}: {HubTheme.FormatGold(price)}";
        }

        private void ConfirmTransaction()
        {
            if (selected == null) return;
            if (mode == Mode.Buy)
            {
                if (Hub.Inventory.Gold < selected.BaseCost) return;
                if (!Hub.Inventory.Add(selected, 1)) return;
                Hub.Inventory.Gold -= selected.BaseCost;
            }
            else
            {
                if (!Hub.Inventory.Contains(selected.Id, 1)) return;
                Hub.Inventory.Remove(selected.Id, 1);
                Hub.Inventory.Gold += selected.ComputedSellValue;
            }
            Hub.PersistAndRefresh();
        }
    }
}
