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
    /// EQUIPSECTION - Per-hero loadout editor with live stat preview.
    /// <para>VISUAL LAYOUT:
    /// <code>
    /// ┌──────────────────────┬────────────────────────────────────┐
    /// │ ┌──────┐ Cleric Lv 4 │ [Weapon][Armor][Relic 1][Relic 2]  │
    /// │ │ POR- │              ├────────────────────────────────────┤
    /// │ │ TRAIT│              │ ▌ Iron Sword            ✓          │
    /// │ │      │              │   +4 STR              [ Equip ]    │
    /// │ └──────┘              ├────────────────────────────────────┤
    /// │                        │ ▌ Mystic Staff   ✓ (currently)    │
    /// │ STR 31  +6 (staff)     │   +6 INT  +3 WIS    [ Equipped ]  │
    /// │ VIT 12                 ├────────────────────────────────────┤
    /// │ AGI 12                 │ ▌ Crystal Wand          ✓          │
    /// │ INT 18  +3 (staff)     │   ⚠ +2 INT  -3 WIS    [ Swap ]    │
    /// │                        │                                    │
    /// │ Weapon: Mystic Staff   │                                    │
    /// │ ✓ Magical                                                   │
    /// │ 80/120 durability                                            │
    /// │                                                              │
    /// │ ◄ Back to Party                                              │
    /// └──────────────────────┴────────────────────────────────────┘
    /// </code></para>
    /// <para>Tabs along the right top filter the item list to the active slot. Tapping a row
    /// equips the item; the left stats panel updates live so the player sees the new totals
    /// before deciding to keep them or swap again.</para>
    /// <para>RELATED FILES: HubManager.cs, HeroLoadout.cs, Formulas.cs, WeaponProficiencyHelper.cs,
    /// PlaceholderIconFactory.cs.</para>
    /// </summary>
    public class EquipSection : HubSection
    {
        private CharacterClass selectedHero = CharacterClass.None;
        private EquipmentSlot selectedSlot = EquipmentSlot.Weapon;

        protected override void OnActivated()
        {
            // Take whichever hero PartySection (or our own previous Back-roundtrip) put on deck.
            if (Hub != null && Hub.PendingEquipHero != CharacterClass.None)
            {
                selectedHero = Hub.PendingEquipHero;
                Hub.PendingEquipHero = CharacterClass.None;
            }
            if (selectedHero == CharacterClass.None) selectedHero = FirstRosterHero();

            Wire(FindButton("BackToPartyButton"), () =>
            {
                if (selectedHero != CharacterClass.None) Hub.PendingEquipHero = selectedHero;
                Hub.Show<PartySection>();
            });

            Wire(FindButton("SlotTab_Weapon"), () => { selectedSlot = EquipmentSlot.Weapon; Refresh(); });
            Wire(FindButton("SlotTab_Armor"),  () => { selectedSlot = EquipmentSlot.Armor;  Refresh(); });
            Wire(FindButton("SlotTab_Relic1"), () => { selectedSlot = EquipmentSlot.Relic1; Refresh(); });
            Wire(FindButton("SlotTab_Relic2"), () => { selectedSlot = EquipmentSlot.Relic2; Refresh(); });
            Wire(FindButton("SlotTab_Relic3"), () => { selectedSlot = EquipmentSlot.Relic3; Refresh(); });
        }

        public override void Refresh()
        {
            RefreshHeroHeader();
            RefreshTabs();
            RefreshStats();
            RefreshItemList();
        }

        private CharacterClass FirstRosterHero()
        {
            var roster = ProfileHelper.CurrentProfile?.CurrentSave?.Roster?.Members;
            return (roster != null && roster.Count > 0) ? roster[0].CharacterClass : CharacterClass.None;
        }

        // ---- Header (portrait + name + Lv) ----

        private void RefreshHeroHeader()
        {
            var portraitTr = transform.Find("HeroPortrait");
            var nameLabel = FindLabel("HeroNameLabel");

            if (selectedHero == CharacterClass.None)
            {
                if (nameLabel != null) nameLabel.text = "<color=#888>No hero selected</color>";
                return;
            }
            var data = ActorLibrary.Get(selectedHero);
            if (data == null) return;

            if (portraitTr != null)
            {
                var img = portraitTr.GetComponent<Image>();
                if (img != null)
                {
                    if (data.Portrait != null) { img.sprite = data.Portrait; img.color = Color.white; }
                    else { img.sprite = PlaceholderIconFactory.GetFallback(); img.color = ClassTint(selectedHero); }
                }
            }

            var rosterEntry = ProfileHelper.CurrentProfile?.CurrentSave?.Roster?.Members?
                .FirstOrDefault(m => m.CharacterClass == selectedHero);
            int level = 1;
            if (rosterEntry != null) (level, _) = ExperienceHelper.DeriveFromTotalXP(rosterEntry.TotalXP);
            if (nameLabel != null) nameLabel.text = $"<b>{selectedHero}</b>  <color=#888>Lv {level}</color>";
        }

        // ---- Slot tabs (highlight active) ----

        private void RefreshTabs()
        {
            HighlightTab("SlotTab_Weapon", selectedSlot == EquipmentSlot.Weapon);
            HighlightTab("SlotTab_Armor",  selectedSlot == EquipmentSlot.Armor);
            HighlightTab("SlotTab_Relic1", selectedSlot == EquipmentSlot.Relic1);
            HighlightTab("SlotTab_Relic2", selectedSlot == EquipmentSlot.Relic2);
            HighlightTab("SlotTab_Relic3", selectedSlot == EquipmentSlot.Relic3);
        }

        private void HighlightTab(string buttonName, bool active)
        {
            var btn = FindButton(buttonName);
            if (btn == null) return;
            var img = btn.GetComponent<Image>();
            if (img != null) img.color = active ? HubTheme.NavActive : HubTheme.NavIdle;
        }

        // ---- Stats panel (left column under header) ----

        private void RefreshStats()
        {
            var label = FindLabel(GameObjectHelper.Hub.StatsLabel);
            if (label == null) return;
            if (selectedHero == CharacterClass.None) { label.text = ""; return; }

            var loadout = Hub.Loadout.Get(selectedHero);
            var data = ActorLibrary.Get(selectedHero);
            if (data == null) { label.text = selectedHero.ToString(); return; }

            var rosterEntry = ProfileHelper.CurrentProfile?.CurrentSave?.Roster?.Members?
                .FirstOrDefault(m => m.CharacterClass == selectedHero);
            int level = 1;
            if (rosterEntry != null) (level, _) = ExperienceHelper.DeriveFromTotalXP(rosterEntry.TotalXP);
            var stats = data.GetStats(Mathf.Max(1, level));
            var bonus = Formulas.ComputeEquipmentBonus(loadout);

            var sb = new System.Text.StringBuilder();
            sb.AppendLine("<b>Stats</b>");
            sb.AppendLine(StatLine("STR", stats.Strength,     bonus.Strength));
            sb.AppendLine(StatLine("VIT", stats.Vitality,     bonus.Vitality));
            sb.AppendLine(StatLine("AGI", stats.Agility,      bonus.Agility));
            sb.AppendLine(StatLine("INT", stats.Intelligence, bonus.Intelligence));
            sb.AppendLine(StatLine("WIS", stats.Wisdom,       bonus.Wisdom));
            sb.AppendLine(StatLine("LCK", stats.Luck,         bonus.Luck));
            sb.AppendLine();

            // Currently-equipped weapon section.
            var weapon = loadout.GetEquipped(EquipmentSlot.Weapon);
            if (weapon == null)
            {
                sb.AppendLine("<color=#888>No weapon — unarmed.</color>");
            }
            else
            {
                string damageType = WeaponTypeHelper.IsMagical(weapon.WeaponType)
                    ? "<color=#7799FF>Magical</color>" : "<color=#FF9966>Physical</color>";
                var prof = WeaponProficiencyHelper.GetProficiency(selectedHero, weapon);
                sb.AppendLine($"<b>Weapon:</b> {weapon.DisplayName}");
                sb.AppendLine($"{damageType}  ({WeaponTypeHelper.DisplayName(weapon.WeaponType)})  {WeaponProficiencyHelper.Marker(prof)}");
                if (weapon.Durability > 0)
                {
                    int dur = loadout.GetDurability(EquipmentSlot.Weapon);
                    string durColor = dur < weapon.Durability * 0.3f ? "#DD5555"
                                    : dur < weapon.Durability * 0.6f ? "#DDBB22"
                                    : "#88CC88";
                    sb.AppendLine($"<color={durColor}>{dur}/{weapon.Durability} durability</color>");
                }
            }
            label.text = sb.ToString();
        }

        private static string StatLine(string name, float baseVal, float bonus)
        {
            if (Mathf.Abs(bonus) < 0.01f) return $"{name} {baseVal:0}";
            return $"{name} {baseVal:0}  <color=#55DD55>+{bonus:0}</color>";
        }

        // ---- Item list (right column) ----

        private void RefreshItemList()
        {
            var list = FindList(GameObjectHelper.Hub.ItemPicker + "/Viewport/Content");
            if (list == null) return;
            ClearList(list);
            if (selectedHero == CharacterClass.None) return;

            var loadout = Hub.Loadout.Get(selectedHero);
            var currentlyEquipped = loadout.GetEquipped(selectedSlot);

            // Currently-equipped row pinned at the top with an "Unequip" button.
            if (currentlyEquipped != null)
            {
                var equippedRow = HubItemRowFactory.Create(list);
                HubItemRowFactory.SetIcon(equippedRow, currentlyEquipped);
                HubItemRowFactory.SetLabel(equippedRow, $"{currentlyEquipped.DisplayName}  <color=#88CC88>(equipped)</color>");
                HubItemRowFactory.SetSubLabel(equippedRow, "Tap to unequip and return to inventory.");
                HubItemRowFactory.SetLabelColor(equippedRow, HubItemRowFactory.RarityColor(currentlyEquipped.Rarity));
                equippedRow.GetComponent<Button>().onClick.AddListener(Unequip);
            }

            foreach (var entry in Hub.Inventory.BySlot(selectedSlot))
            {
                if (entry.Count <= 0) continue;
                var def = entry.Definition;
                var row = HubItemRowFactory.Create(list);
                HubItemRowFactory.SetIcon(row, def);

                // Weapon-slot rows surface the proficiency marker.
                string nameText = def.DisplayName;
                WeaponProficiency prof = WeaponProficiency.Proficient;
                if (selectedSlot == EquipmentSlot.Weapon)
                {
                    prof = WeaponProficiencyHelper.GetProficiency(selectedHero, def);
                    nameText = $"{WeaponProficiencyHelper.Marker(prof)} {nameText}";
                }
                HubItemRowFactory.SetLabel(row, nameText);
                HubItemRowFactory.SetLabelColor(row, HubItemRowFactory.RarityColor(def.Rarity));

                string sub = currentlyEquipped != null
                    ? StatDiffSummary(currentlyEquipped, def)
                    : StatSummary(def);
                if (entry.Count > 1) sub = $"x{entry.Count}  •  " + sub;
                HubItemRowFactory.SetSubLabel(row, sub);

                var captured = def;
                var btn = row.GetComponent<Button>();
                if (selectedSlot == EquipmentSlot.Weapon && prof == WeaponProficiency.Forbidden)
                {
                    btn.interactable = false;
                }
                else
                {
                    btn.onClick.AddListener(() => EquipItem(captured));
                }
            }
        }

        private string StatSummary(ItemDefinition d)
        {
            var parts = new List<string>();
            AppendChip(parts, "STR", d.Strength);
            AppendChip(parts, "VIT", d.Vitality);
            AppendChip(parts, "AGI", d.Agility);
            AppendChip(parts, "INT", d.Intelligence);
            AppendChip(parts, "WIS", d.Wisdom);
            AppendChip(parts, "LCK", d.Luck);
            return parts.Count > 0 ? string.Join("  ", parts) : "<color=#888>no stat bonus</color>";
        }

        private string StatDiffSummary(ItemDefinition cur, ItemDefinition cand)
        {
            var parts = new List<string>();
            AppendDelta(parts, "STR", cur.Strength,     cand.Strength);
            AppendDelta(parts, "VIT", cur.Vitality,     cand.Vitality);
            AppendDelta(parts, "AGI", cur.Agility,      cand.Agility);
            AppendDelta(parts, "INT", cur.Intelligence, cand.Intelligence);
            AppendDelta(parts, "WIS", cur.Wisdom,       cand.Wisdom);
            AppendDelta(parts, "LCK", cur.Luck,         cand.Luck);
            return parts.Count == 0 ? "<color=#888>= no change</color>" : string.Join("  ", parts);
        }

        private static void AppendChip(List<string> parts, string label, float v)
        {
            if (Mathf.Abs(v) < 0.01f) return;
            string sign = v >= 0 ? "+" : "";
            parts.Add($"<color=#88CCFF>{sign}{v:0} {label}</color>");
        }

        private static void AppendDelta(List<string> parts, string label, float cur, float cand)
        {
            float d = cand - cur;
            if (Mathf.Abs(d) < 0.01f) return;
            string color = d > 0 ? "#55DD55" : "#DD5555";
            string sign  = d > 0 ? "+" : "";
            parts.Add($"<color={color}>{sign}{d:0} {label}</color>");
        }

        // ---- Equip / Unequip ----

        private void EquipItem(ItemDefinition item)
        {
            if (selectedHero == CharacterClass.None || item == null) return;

            if (!WeaponProficiencyHelper.CanEquip(selectedHero, item))
            {
                HubToast.Show($"{selectedHero} cannot wield a {WeaponTypeHelper.DisplayName(item.WeaponType)}.");
                return;
            }

            var loadout = Hub.Loadout.Get(selectedHero);

            var inboundEntry = Hub.Inventory.GetEntry(item.Id);
            int inboundDurability = inboundEntry?.CurrentDurability ?? item.Durability;
            int inboundRepairCount = inboundEntry?.RepairCount ?? 0;

            var current = loadout.GetEquipped(selectedSlot);
            if (current != null)
            {
                int outDurability  = loadout.GetDurability(selectedSlot);
                int outRepairCount = loadout.GetRepairCount(selectedSlot);
                ReturnEquipmentToInventory(current, outDurability, outRepairCount);
            }

            loadout.EquippedSlots[selectedSlot] = item;
            loadout.SlotDurability[selectedSlot] = inboundDurability;
            loadout.SlotRepairCount[selectedSlot] = inboundRepairCount;
            Hub.Inventory.Remove(item.Id, 1);

            if (item.Slot == EquipmentSlot.Weapon)
            {
                var prof = WeaponProficiencyHelper.GetProficiency(selectedHero, item);
                if (prof == WeaponProficiency.Poor)
                    HubToast.Show($"⚠ {selectedHero} is poorly suited to {WeaponTypeHelper.DisplayName(item.WeaponType)} — fights at reduced effectiveness.");
            }

            Hub.PersistAndRefresh();
        }

        private void Unequip()
        {
            if (selectedHero == CharacterClass.None) return;
            var loadout = Hub.Loadout.Get(selectedHero);
            var current = loadout.GetEquipped(selectedSlot);
            if (current == null) return;
            int durability  = loadout.GetDurability(selectedSlot);
            int repairCount = loadout.GetRepairCount(selectedSlot);
            ReturnEquipmentToInventory(current, durability, repairCount);
            loadout.EquippedSlots.Remove(selectedSlot);
            loadout.SlotDurability.Remove(selectedSlot);
            loadout.SlotRepairCount.Remove(selectedSlot);
            Hub.PersistAndRefresh();
        }

        private void ReturnEquipmentToInventory(ItemDefinition item, int durability, int repairCount)
        {
            var existing = Hub.Inventory.GetEntry(item.Id);
            if (existing == null)
            {
                Hub.Inventory.Add(item, 1);
                existing = Hub.Inventory.GetEntry(item.Id);
                if (existing != null)
                {
                    existing.CurrentDurability = durability > 0 ? durability : item.Durability;
                    existing.RepairCount = repairCount;
                }
            }
            else
            {
                Hub.Inventory.Add(item, 1);
                existing.CurrentDurability = Mathf.Min(existing.CurrentDurability, durability > 0 ? durability : item.Durability);
                existing.RepairCount = Mathf.Max(existing.RepairCount, repairCount);
            }
        }

        private static Color ClassTint(CharacterClass cc)
        {
            int h = cc.ToString().GetHashCode();
            float hue = Mathf.Repeat(h * 0.000173f, 1f);
            return Color.HSVToRGB(hue, 0.5f, 0.8f);
        }
    }
}
