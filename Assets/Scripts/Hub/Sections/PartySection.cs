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
    /// PARTYSECTION - Single rich list of every unlocked hero with inline Add/Remove buttons.
    /// <para>VISUAL LAYOUT:
    /// <code>
    /// ┌────────────────────────────────────────┬──────────────────────┐
    /// │ ▌ [Portrait] Paladin Lv 5      [Remove]│  Selected hero       │
    /// │   HP 80  STR 20  VIT 22  AGI 11        │  - portrait          │
    /// │   AP 120 INT 8   WIS 9   LCK 11        │  - equipment         │
    /// ├────────────────────────────────────────┤  - XP / next level   │
    /// │ ▌ [Portrait] Cleric Lv 4        [Remove]│                     │
    /// │   ...                                  │  [ Manage Equipment ]│
    /// ├────────────────────────────────────────┤                      │
    /// │   [Portrait] RedNinja Lv 1        [Add] │                     │
    /// │   ...                                  │                      │
    /// └────────────────────────────────────────┴──────────────────────┘
    /// </code>
    /// Green left-edge accent indicates the hero is in the active party. Tapping a row body
    /// selects the hero (driving the right pane); the per-row action button toggles party
    /// membership without changing selection.</para>
    /// <para>RELATED FILES: HeroCardFactory.cs, EquipSection.cs, ProfileHelper.cs</para>
    /// </summary>
    public class PartySection : HubSection
    {
        private const int MaxPartySize = 4;
        private CharacterClass selected = CharacterClass.None;
        private const string EquipBtnName = "ManageEquipmentButton";

        protected override void OnActivated()
        {
            Wire(FindButton(EquipBtnName), ManageEquipment);

            // Pick up a hero handed off from EquipSection's Back button so the player keeps context.
            if (Hub != null && Hub.PendingEquipHero != CharacterClass.None)
            {
                selected = Hub.PendingEquipHero;
                Hub.PendingEquipHero = CharacterClass.None;
            }
        }

        public override void Refresh()
        {
            RefreshHeroList();
            RefreshDetail();
            RefreshActionButtons();
        }

        private void RefreshHeroList()
        {
            var list = FindList(GameObjectHelper.Hub.RosterList + "/Viewport/Content");
            if (list == null)
            {
                Debug.LogError("[PartySection] RosterList/Viewport/Content not found — scaffold out of date?");
                return;
            }
            ClearList(list);
            var save = ProfileHelper.CurrentProfile?.CurrentSave;
            if (save == null)
            {
                Debug.LogWarning("[PartySection] CurrentSave is null.");
                return;
            }
            if (save.Roster?.Members == null || save.Roster.Members.Count == 0)
            {
                Debug.LogWarning("[PartySection] save.Roster.Members is null/empty — nothing to render.");
                return;
            }
            var partyIds = save.Party?.Members?.Select(m => m.CharacterClass).ToHashSet() ?? new System.Collections.Generic.HashSet<CharacterClass>();
            int partyCount = save.Party?.Members?.Count ?? 0;

            // Sort: party members first (in their party order), then reserve heroes alphabetically.
            var rosterOrdered = save.Roster.Members
                .OrderByDescending(m => partyIds.Contains(m.CharacterClass))
                .ThenBy(m => m.CharacterClass.ToString())
                .ToList();

            int rendered = 0;
            int skipped = 0;
            foreach (var member in rosterOrdered)
            {
                try
                {
                    var data = ActorLibrary.Get(member.CharacterClass);
                    var (level, _) = ExperienceHelper.DeriveFromTotalXP(member.TotalXP);
                    bool inParty = partyIds.Contains(member.CharacterClass);

                    var card = HeroCardFactory.Create(list);
                    if (card == null) { skipped++; continue; }

                    // Even if ActorData is missing for this class, render a stub row so the player
                    // can still see what's in the save and act on it.
                    if (data != null)
                    {
                        var stats = data.GetStats(Mathf.Max(1, level));
                        HeroCardFactory.SetPortrait(card, data.Portrait, ClassTint(member.CharacterClass));
                        HeroCardFactory.SetStats(card, stats);
                    }
                    else
                    {
                        HeroCardFactory.SetPortrait(card, null, ClassTint(member.CharacterClass));
                    }
                    HeroCardFactory.SetName(card, member.CharacterClass.ToString(), level);
                    HeroCardFactory.SetInParty(card, inParty);
                    HeroCardFactory.SetSelected(card, member.CharacterClass == selected);

                    var captured = member.CharacterClass;
                    HeroCardFactory.OnRowClick(card, () => SelectHero(captured));

                    if (inParty)
                    {
                        bool canRemove = partyCount > 1;
                        HeroCardFactory.SetAction(card, "Remove", canRemove, () => RemoveFromParty(captured));
                    }
                    else
                    {
                        bool canAdd = partyCount < MaxPartySize;
                        HeroCardFactory.SetAction(card, "Add", canAdd, () => AddToParty(captured));
                    }
                    rendered++;
                }
                catch (System.Exception ex)
                {
                    Debug.LogError($"[PartySection] Failed to render row for {member.CharacterClass}: {ex.Message}\n{ex.StackTrace}");
                    skipped++;
                }
            }
            Debug.Log($"[PartySection] Roster rendered: {rendered}/{rosterOrdered.Count} (skipped {skipped}).");
        }

        private void RefreshDetail()
        {
            var detail = FindLabel(GameObjectHelper.Hub.DetailLabel);
            if (detail == null) return;
            if (selected == CharacterClass.None)
            {
                detail.text = "<b>Party</b>\nSelect any hero on the left to view their full profile.\n\n"
                            + $"<color=#888>Up to {MaxPartySize} heroes can join the active party.</color>";
                return;
            }

            var data = ActorLibrary.Get(selected);
            var pair = ProfileHelper.CurrentProfile?.CurrentSave?.Roster?.Members?.FirstOrDefault(m => m.CharacterClass == selected);
            if (data == null || pair == null) { detail.text = selected.ToString(); return; }
            var (level, currentXP) = ExperienceHelper.DeriveFromTotalXP(pair.TotalXP);
            var stats = data.GetStats(level);
            var loadout = Hub.Loadout.Get(selected);
            var bonus = Formulas.ComputeEquipmentBonus(loadout);

            string weaponLine = WeaponLine(loadout);
            string armorLine  = SlotLine(loadout, EquipmentSlot.Armor,  "Armor");
            string relic1     = SlotLine(loadout, EquipmentSlot.Relic1, "Relic 1");
            string relic2     = SlotLine(loadout, EquipmentSlot.Relic2, "Relic 2");
            string relic3     = SlotLine(loadout, EquipmentSlot.Relic3, "Relic 3");

            detail.text = $"<b><size=32>{selected}</size></b>  <color=#888>Lv {level}</color>\n\n"
                + $"<b>Stats (with equipment)</b>\n"
                + $"HP {stats.MaxHP:0}  AP {stats.MaxAP:0}\n"
                + $"STR {stats.Strength:0}<color=#55DD55>+{bonus.Strength:0}</color>  "
                + $"VIT {stats.Vitality:0}<color=#55DD55>+{bonus.Vitality:0}</color>  "
                + $"AGI {stats.Agility:0}<color=#55DD55>+{bonus.Agility:0}</color>\n"
                + $"INT {stats.Intelligence:0}<color=#55DD55>+{bonus.Intelligence:0}</color>  "
                + $"WIS {stats.Wisdom:0}<color=#55DD55>+{bonus.Wisdom:0}</color>  "
                + $"LCK {stats.Luck:0}<color=#55DD55>+{bonus.Luck:0}</color>\n\n"
                + $"<b>Equipment</b>\n"
                + weaponLine + "\n"
                + armorLine + "\n"
                + relic1 + "\n"
                + relic2 + "\n"
                + relic3 + "\n\n"
                + $"<color=#888>XP to next: {ExperienceHelper.NextLevel(level) - currentXP}</color>";
        }

        private string WeaponLine(HeroLoadout loadout)
        {
            var weapon = loadout.GetEquipped(EquipmentSlot.Weapon);
            if (weapon == null) return "Weapon: <color=#888>(none — unarmed)</color>";
            string damageType = WeaponTypeHelper.IsMagical(weapon.WeaponType) ? "<color=#7799FF>Magical</color>" : "<color=#FF9966>Physical</color>";
            string line = $"Weapon: {weapon.DisplayName}  ({damageType})";
            if (weapon.Durability > 0)
                line += $"  {DurabilityChip(loadout.GetDurability(EquipmentSlot.Weapon), weapon.Durability)}";
            return line;
        }

        private static string SlotLine(HeroLoadout loadout, EquipmentSlot slot, string label)
        {
            var item = loadout.GetEquipped(slot);
            if (item == null) return $"{label}: <color=#888>(none)</color>";
            string line = $"{label}: {item.DisplayName}";
            if (item.Durability > 0)
                line += $"  {DurabilityChip(loadout.GetDurability(slot), item.Durability)}";
            return line;
        }

        private static string DurabilityChip(int current, int max)
        {
            string color = current < max * 0.3f ? "#DD5555"
                         : current < max * 0.6f ? "#DDBB22"
                         : "#88CC88";
            return $"<color={color}>{current}/{max}</color>";
        }

        private void RefreshActionButtons()
        {
            var equipBtn = FindButton(EquipBtnName);
            if (equipBtn != null) equipBtn.interactable = selected != CharacterClass.None;
        }

        // ---- Actions ----

        private void SelectHero(CharacterClass cc)
        {
            selected = cc;
            Refresh();
        }

        private void AddToParty(CharacterClass cc)
        {
            var party = ProfileHelper.CurrentProfile?.CurrentSave?.Party?.Members;
            if (party == null) return;
            if (party.Any(m => m.CharacterClass == cc)) return;
            if (party.Count >= MaxPartySize) { HubToast.Show($"Party is full ({MaxPartySize} max)."); return; }
            ProfileHelper.AddToParty(cc);
            HubToast.Show($"Added {cc} to the party.");
            selected = cc;
            Hub.PersistAndRefresh();
        }

        private void RemoveFromParty(CharacterClass cc)
        {
            var party = ProfileHelper.CurrentProfile?.CurrentSave?.Party?.Members;
            if (party == null || party.Count <= 1) { HubToast.Show("Need at least one hero in the party."); return; }
            if (!party.Any(m => m.CharacterClass == cc)) return;
            ProfileHelper.RemoveFromParty(cc);
            HubToast.Show($"{cc} returned to reserve.");
            selected = cc;
            Hub.PersistAndRefresh();
        }

        private void ManageEquipment()
        {
            if (selected == CharacterClass.None) return;
            Hub.PendingEquipHero = selected;
            Hub.Show<EquipSection>();
        }

        private static Color ClassTint(CharacterClass cc)
        {
            int h = cc.ToString().GetHashCode();
            float hue = Mathf.Repeat(h * 0.000173f, 1f);
            return Color.HSVToRGB(hue, 0.5f, 0.8f);
        }
    }
}
