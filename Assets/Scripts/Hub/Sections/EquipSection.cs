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
    /// EQUIPSECTION - Three-column hero / slot / item picker.
    /// <para>PURPOSE: Pick a hero → pick an equipment slot → choose from inventory items that fit
    /// that slot. Unequipped items go back to the shared inventory. Stats label shows the
    /// hero's effective stat block with current loadout.</para>
    /// <para>RELATED FILES: HubManager.cs, HeroLoadout.cs, Formulas.cs</para>
    /// </summary>
    public class EquipSection : HubSection
    {
        private CharacterClass selectedHero = CharacterClass.None;
        private EquipmentSlot selectedSlot = EquipmentSlot.None;

        protected override void OnActivated()
        {
            if (selectedHero == CharacterClass.None) selectedHero = FirstHero();
        }

        public override void Refresh()
        {
            RefreshHeroList();
            RefreshSlotList();
            RefreshItemPicker();
            RefreshStats();
        }

        private CharacterClass FirstHero()
        {
            var party = ProfileHelper.CurrentProfile?.CurrentSave?.Party?.Members;
            if (party != null && party.Count > 0) return party[0].CharacterClass;
            return CharacterClass.None;
        }

        private void RefreshHeroList()
        {
            var list = FindList(GameObjectHelper.Hub.HeroList + "/Viewport/Content");
            if (list == null) return;
            ClearList(list);
            var party = ProfileHelper.CurrentProfile?.CurrentSave?.Party?.Members;
            if (party == null) return;

            foreach (var hero in party)
            {
                var row = HubItemRowFactory.Create(list);
                HubItemRowFactory.SetLabel(row, hero.CharacterClass.ToString());
                HubItemRowFactory.SetIconColor(row, ClassTint(hero.CharacterClass));
                var captured = hero.CharacterClass;
                row.GetComponent<Button>().onClick.AddListener(() => { selectedHero = captured; selectedSlot = EquipmentSlot.None; Refresh(); });
                HubItemRowFactory.SetSelected(row, hero.CharacterClass == selectedHero);
            }
        }

        private void RefreshSlotList()
        {
            var list = FindList(GameObjectHelper.Hub.SlotList + "/Viewport/Content");
            if (list == null) return;
            ClearList(list);
            if (selectedHero == CharacterClass.None) return;
            var loadout = Hub.Loadout.Get(selectedHero);

            foreach (EquipmentSlot slot in System.Enum.GetValues(typeof(EquipmentSlot)))
            {
                if (slot == EquipmentSlot.None) continue;
                var row = HubItemRowFactory.Create(list);
                HubItemRowFactory.SetLabel(row, slot.ToString());
                var equipped = loadout.GetEquipped(slot);
                HubItemRowFactory.SetSubLabel(row, equipped != null ? equipped.DisplayName : "<color=#888>(empty)</color>");
                if (equipped != null) HubItemRowFactory.SetIcon(row, equipped);
                var captured = slot;
                row.GetComponent<Button>().onClick.AddListener(() => { selectedSlot = captured; Refresh(); });
                HubItemRowFactory.SetSelected(row, slot == selectedSlot);
            }
        }

        private void RefreshItemPicker()
        {
            var list = FindList(GameObjectHelper.Hub.ItemPicker + "/Viewport/Content");
            if (list == null) return;
            ClearList(list);
            if (selectedHero == CharacterClass.None || selectedSlot == EquipmentSlot.None) return;
            var loadout = Hub.Loadout.Get(selectedHero);

            // Unequip row
            var unequip = HubItemRowFactory.Create(list);
            HubItemRowFactory.SetLabel(unequip, "— Unequip —");
            HubItemRowFactory.SetSubLabel(unequip, "Return the current item to inventory.");
            HubItemRowFactory.SetIconColor(unequip, HubTheme.TextDim);
            unequip.GetComponent<Button>().onClick.AddListener(Unequip);

            var currentlyEquipped = loadout.GetEquipped(selectedSlot);
            foreach (var entry in Hub.Inventory.BySlot(selectedSlot))
            {
                if (entry.Count <= 0) continue;
                var def = entry.Definition;
                var row = HubItemRowFactory.Create(list);
                HubItemRowFactory.SetIcon(row, def);
                HubItemRowFactory.SetLabel(row, def.DisplayName);
                // When a slot already has an item equipped, show per-stat deltas (+N green / -N red)
                // so the player can compare at a glance. Empty slot → raw stat summary.
                HubItemRowFactory.SetSubLabel(row,
                    currentlyEquipped != null ? StatDiffSummary(currentlyEquipped, def) : StatSummary(def));
                HubItemRowFactory.SetLabelColor(row, HubItemRowFactory.RarityColor(def.Rarity));
                var captured = def;
                row.GetComponent<Button>().onClick.AddListener(() => EquipItem(captured));
            }
        }

        private void RefreshStats()
        {
            var label = FindLabel(GameObjectHelper.Hub.StatsLabel);
            if (label == null) return;
            if (selectedHero == CharacterClass.None) { label.text = ""; return; }

            var loadout = Hub.Loadout.Get(selectedHero);
            var data = ActorLibrary.Get(selectedHero);
            if (data == null) { label.text = selectedHero.ToString(); return; }
            var stats = data.GetStats(1);
            var bonus = Formulas.ComputeEquipmentBonus(loadout);

            label.text = $"<b>{selectedHero}</b>\n"
                + $"STR {stats.Strength:0} +{bonus.Strength:0}\n"
                + $"VIT {stats.Vitality:0} +{bonus.Vitality:0}\n"
                + $"AGI {stats.Agility:0} +{bonus.Agility:0}\n"
                + $"INT {stats.Intelligence:0} +{bonus.Intelligence:0}\n"
                + $"WIS {stats.Wisdom:0} +{bonus.Wisdom:0}\n"
                + $"LCK {stats.Luck:0} +{bonus.Luck:0}";
        }

        private string StatSummary(ItemDefinition d)
        {
            var parts = new List<string>();
            if (d.Strength != 0) parts.Add($"STR{Signed(d.Strength)}");
            if (d.Vitality != 0) parts.Add($"VIT{Signed(d.Vitality)}");
            if (d.Agility != 0) parts.Add($"AGI{Signed(d.Agility)}");
            if (d.Intelligence != 0) parts.Add($"INT{Signed(d.Intelligence)}");
            if (d.Wisdom != 0) parts.Add($"WIS{Signed(d.Wisdom)}");
            if (d.Luck != 0) parts.Add($"LCK{Signed(d.Luck)}");
            return parts.Count > 0 ? string.Join(" ", parts) : "—";
        }

        private string Signed(float v) => v >= 0 ? $"+{v:0}" : $"{v:0}";

        /// <summary>Per-stat delta between the currently-equipped item and <paramref name="candidate"/>.
        /// Upgrades are green, downgrades red, equal stats omitted — so the row reads as a quick-glance
        /// compare instead of a raw dump of both stat blocs.</summary>
        private string StatDiffSummary(ItemDefinition current, ItemDefinition candidate)
        {
            var parts = new List<string>();
            AppendDiff(parts, "STR", current.Strength,     candidate.Strength);
            AppendDiff(parts, "VIT", current.Vitality,     candidate.Vitality);
            AppendDiff(parts, "AGI", current.Agility,      candidate.Agility);
            AppendDiff(parts, "INT", current.Intelligence, candidate.Intelligence);
            AppendDiff(parts, "WIS", current.Wisdom,       candidate.Wisdom);
            AppendDiff(parts, "LCK", current.Luck,         candidate.Luck);
            return parts.Count == 0 ? "<color=#888>= no change</color>" : string.Join(" ", parts);
        }

        private static void AppendDiff(List<string> parts, string label, float current, float candidate)
        {
            float delta = candidate - current;
            if (Mathf.Abs(delta) < 0.01f) return;
            string color = delta > 0 ? "#55DD55" : "#DD5555";
            string sign = delta > 0 ? "+" : "";
            parts.Add($"<color={color}>{sign}{delta:0} {label}</color>");
        }

        private void EquipItem(ItemDefinition item)
        {
            if (selectedHero == CharacterClass.None || item == null) return;
            var loadout = Hub.Loadout.Get(selectedHero);

            // Unequip current, return to inventory
            var current = loadout.GetEquipped(selectedSlot);
            if (current != null) Hub.Inventory.Add(current, 1);

            loadout.EquippedSlots[selectedSlot] = item;
            Hub.Inventory.Remove(item.Id, 1);
            Hub.PersistAndRefresh();
        }

        private void Unequip()
        {
            if (selectedHero == CharacterClass.None || selectedSlot == EquipmentSlot.None) return;
            var loadout = Hub.Loadout.Get(selectedHero);
            var current = loadout.GetEquipped(selectedSlot);
            if (current == null) return;
            Hub.Inventory.Add(current, 1);
            loadout.EquippedSlots.Remove(selectedSlot);
            Hub.PersistAndRefresh();
        }

        private static Color ClassTint(CharacterClass cc)
        {
            int h = cc.ToString().GetHashCode();
            float hue = Mathf.Repeat(h * 0.000173f, 1f);
            return Color.HSVToRGB(hue, 0.5f, 0.8f);
        }
    }
}
