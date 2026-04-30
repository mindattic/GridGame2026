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
    /// BLACKSMITHSECTION - Pay-per-repair restoration of damaged equipment.
    /// <para>PURPOSE: Lists every damaged piece of equipment the player owns — weapons & armor
    /// equipped on roster heroes plus any damaged stacks sitting in inventory. Each row shows
    /// current/max durability, the item's lifetime repair count, and the gold cost to restore
    /// it to factory state. The cost climbs by ~1.6× per prior repair so that after roughly the
    /// 3rd visit a fresh purchase or battlefield drop becomes the better deal — exactly the
    /// FireEmblem economics the player is trying to optimise around.</para>
    /// <para>RELATED FILES: WeaponDurabilityHelper.cs (cost formula + decay), HeroLoadout.cs,
    /// PlayerInventory.cs, EquipSection.cs.</para>
    /// </summary>
    public class BlacksmithSection : HubSection
    {
        private const string ItemListPath = "ItemList/Viewport/Content";

        private RepairTarget selected;

        protected override void OnActivated()
        {
            // Single shared button at the bottom of the panel — repair every damaged piece the
            // player can afford in one click. Standard JRPG smith UX.
            Wire(FindButton("RepairAllButton"), RepairAllAffordable);
        }

        public override void Refresh()
        {
            RefreshList();
            RefreshDetail();
            RefreshRepairAllButton();
        }

        private void RefreshList()
        {
            var list = FindList(ItemListPath);
            if (list == null) return;
            ClearList(list);

            var targets = CollectDamagedTargets().ToList();
            if (targets.Count == 0)
            {
                var row = HubItemRowFactory.Create(list);
                HubItemRowFactory.SetLabel(row, "<color=#88CC88>All equipment in pristine condition.</color>");
                HubItemRowFactory.SetSubLabel(row, "Come back after a few battles.");
                row.GetComponent<Button>().interactable = false;
                return;
            }

            foreach (var t in targets)
            {
                var row = HubItemRowFactory.Create(list);
                HubItemRowFactory.SetIcon(row, t.Item);
                int cost = WeaponDurabilityHelper.RepairCost(t.Item, t.CurrentDurability, t.RepairCount);
                bool affordable = Hub.Inventory.Gold >= cost;
                bool uneconomical = WeaponDurabilityHelper.IsUneconomical(t.Item, t.CurrentDurability, t.RepairCount);

                string ownerLabel = t.IsEquipped ? $"{t.Owner}'s equipped" : "in inventory";
                HubItemRowFactory.SetLabel(row, $"{t.Item.DisplayName}  <color=#888>({ownerLabel})</color>");
                string durColor = t.CurrentDurability < t.Item.Durability * 0.3f ? "#DD5555"
                                : t.CurrentDurability < t.Item.Durability * 0.6f ? "#DDBB22"
                                : "#88CC88";
                string costColor = !affordable ? "#DD5555" : uneconomical ? "#DDBB22" : "#FFFFFF";
                string warning = uneconomical ? "  <color=#DDBB22>(replace it instead?)</color>" : string.Empty;
                HubItemRowFactory.SetSubLabel(row,
                    $"<color={durColor}>{t.CurrentDurability}/{t.Item.Durability}</color>  •  Repairs: {t.RepairCount}  •  <color={costColor}>{HubTheme.FormatGold(cost)}</color>{warning}");
                HubItemRowFactory.SetLabelColor(row, HubItemRowFactory.RarityColor(t.Item.Rarity));

                var captured = t;
                row.GetComponent<Button>().onClick.AddListener(() => { selected = captured; Refresh(); });
                HubItemRowFactory.SetSelected(row, selected != null && selected.Equals(t));
            }
        }

        private void RefreshDetail()
        {
            var detail = FindLabel(GameObjectHelper.Hub.DetailLabel);
            var confirm = FindButton("ConfirmButton");
            if (detail == null) return;
            if (selected == null)
            {
                detail.text = "<b>Blacksmith</b>\nSelect a damaged piece on the left to see the repair quote.\n\n"
                            + "<color=#888>Each repair costs more than the last — eventually you're better off buying a replacement.</color>";
                if (confirm != null) confirm.interactable = false;
                return;
            }

            int cost = WeaponDurabilityHelper.RepairCost(selected.Item, selected.CurrentDurability, selected.RepairCount);
            bool affordable = Hub.Inventory.Gold >= cost;
            bool uneconomical = WeaponDurabilityHelper.IsUneconomical(selected.Item, selected.CurrentDurability, selected.RepairCount);

            var sb = new System.Text.StringBuilder();
            sb.AppendLine($"<b>{selected.Item.DisplayName}</b>");
            if (!string.IsNullOrEmpty(selected.Item.Description)) sb.AppendLine(selected.Item.Description);
            sb.AppendLine();
            sb.AppendLine(selected.IsEquipped ? $"Equipped to: <b>{selected.Owner}</b>" : "Stored in inventory");
            sb.AppendLine($"Durability: <b>{selected.CurrentDurability}/{selected.Item.Durability}</b>");
            sb.AppendLine($"Times repaired: <b>{selected.RepairCount}</b>  (next quote inflates by 1.6×)");
            sb.AppendLine();
            sb.AppendLine($"Repair cost: <b>{HubTheme.FormatGold(cost)}</b>");
            sb.AppendLine($"New replacement cost: {HubTheme.FormatGold(selected.Item.BaseCost)}");
            if (uneconomical)
                sb.AppendLine("<color=#DDBB22>⚠ Repair now exceeds the price of a fresh copy. Consider replacing it.</color>");
            if (!affordable)
                sb.AppendLine("<color=#DD5555>Not enough gold.</color>");
            detail.text = sb.ToString();

            if (confirm != null)
            {
                confirm.interactable = affordable;
                Wire(confirm, ExecuteRepair);
                var label = confirm.GetComponentInChildren<TMP_Text>();
                if (label != null) label.text = $"Repair  ({HubTheme.FormatGold(cost)})";
            }
        }

        /// <summary>Loops the damaged-target list and repairs every entry the player can still
        /// afford, in cheapest-first order. Stops when gold runs out, mirroring how a real smith
        /// would only do as much work as the gold pile pays for.</summary>
        private void RepairAllAffordable()
        {
            int repaired = 0;
            int totalSpent = 0;

            // Snapshot current targets sorted cheapest-first so we never blow the budget on a
            // single Legendary repair while several Common pieces remain damaged.
            var targets = CollectDamagedTargets()
                .Select(t => new { Target = t, Cost = WeaponDurabilityHelper.RepairCost(t.Item, t.CurrentDurability, t.RepairCount) })
                .OrderBy(x => x.Cost)
                .ToList();

            foreach (var t in targets)
            {
                if (Hub.Inventory.Gold < t.Cost) continue;
                Hub.Inventory.Gold -= t.Cost;
                totalSpent += t.Cost;
                repaired++;

                if (t.Target.IsEquipped)
                {
                    var loadout = Hub.Loadout.Get(t.Target.Owner);
                    loadout.SlotDurability[t.Target.Slot] = t.Target.Item.Durability;
                    loadout.SlotRepairCount[t.Target.Slot] = t.Target.RepairCount + 1;
                }
                else
                {
                    var entry = Hub.Inventory.GetEntry(t.Target.Item.Id);
                    if (entry != null)
                    {
                        entry.CurrentDurability = t.Target.Item.Durability;
                        entry.RepairCount = t.Target.RepairCount + 1;
                    }
                }
            }

            if (repaired == 0)
                HubToast.Show("Nothing to repair, or not enough gold.");
            else
                HubToast.Show($"Repaired {repaired} piece{(repaired == 1 ? "" : "s")} for {HubTheme.FormatGold(totalSpent)}.");

            selected = null;
            Hub.PersistAndRefresh();
        }

        private void RefreshRepairAllButton()
        {
            var btn = FindButton("RepairAllButton");
            if (btn == null) return;
            int affordableCost = 0;
            int affordableCount = 0;
            int gold = Hub.Inventory.Gold;
            // Simulate cheapest-first to compute how many pieces fit in the gold pile.
            var costs = CollectDamagedTargets()
                .Select(t => WeaponDurabilityHelper.RepairCost(t.Item, t.CurrentDurability, t.RepairCount))
                .OrderBy(c => c)
                .ToList();
            foreach (var c in costs)
            {
                if (affordableCost + c > gold) break;
                affordableCost += c;
                affordableCount++;
            }
            btn.interactable = affordableCount > 0;
            var label = btn.GetComponentInChildren<TMP_Text>();
            if (label != null)
            {
                if (affordableCount == 0) label.text = "Repair All";
                else label.text = $"Repair All  ({affordableCount} for {HubTheme.FormatGold(affordableCost)})";
            }
        }

        private void ExecuteRepair()
        {
            if (selected == null) return;
            int cost = WeaponDurabilityHelper.RepairCost(selected.Item, selected.CurrentDurability, selected.RepairCount);
            if (Hub.Inventory.Gold < cost) { HubToast.Show("Not enough gold to repair."); return; }
            Hub.Inventory.Gold -= cost;

            if (selected.IsEquipped)
            {
                var loadout = Hub.Loadout.Get(selected.Owner);
                loadout.SlotDurability[selected.Slot] = selected.Item.Durability;
                loadout.SlotRepairCount[selected.Slot] = selected.RepairCount + 1;
            }
            else
            {
                var entry = Hub.Inventory.GetEntry(selected.Item.Id);
                if (entry != null)
                {
                    entry.CurrentDurability = selected.Item.Durability;
                    entry.RepairCount = selected.RepairCount + 1;
                }
            }

            HubToast.Show($"Repaired {selected.Item.DisplayName} for {HubTheme.FormatGold(cost)}.");
            selected = null;
            Hub.PersistAndRefresh();
        }

        // ---- Target collection ----

        private IEnumerable<RepairTarget> CollectDamagedTargets()
        {
            // Equipped pieces — iterate every roster member's loadout (party + reserve).
            var roster = ProfileHelper.CurrentProfile?.CurrentSave?.Roster?.Members;
            if (roster != null)
            {
                foreach (var member in roster)
                {
                    var loadout = Hub.Loadout.Get(member.CharacterClass);
                    foreach (var kvp in loadout.EquippedSlots)
                    {
                        var item = kvp.Value;
                        if (item == null || item.Durability <= 0) continue;
                        int dur = loadout.GetDurability(kvp.Key);
                        if (dur >= item.Durability) continue; // pristine
                        yield return new RepairTarget
                        {
                            Item = item,
                            CurrentDurability = dur,
                            RepairCount = loadout.GetRepairCount(kvp.Key),
                            IsEquipped = true,
                            Owner = member.CharacterClass,
                            Slot = kvp.Key,
                        };
                    }
                }
            }

            // Damaged inventory stacks.
            foreach (var entry in Hub.Inventory.All())
            {
                var item = entry.Definition;
                if (item == null || item.Durability <= 0) continue;
                if (entry.CurrentDurability >= item.Durability) continue;
                yield return new RepairTarget
                {
                    Item = item,
                    CurrentDurability = entry.CurrentDurability,
                    RepairCount = entry.RepairCount,
                    IsEquipped = false,
                    Owner = CharacterClass.None,
                    Slot = EquipmentSlot.None,
                };
            }
        }

        private class RepairTarget
        {
            public ItemDefinition Item;
            public int CurrentDurability;
            public int RepairCount;
            public bool IsEquipped;
            public CharacterClass Owner;
            public EquipmentSlot Slot;

            public override bool Equals(object obj)
            {
                if (!(obj is RepairTarget other)) return false;
                return Item?.Id == other.Item?.Id
                    && IsEquipped == other.IsEquipped
                    && Owner == other.Owner
                    && Slot == other.Slot;
            }
            public override int GetHashCode()
                => System.HashCode.Combine(Item?.Id, IsEquipped, Owner, Slot);
        }
    }
}
