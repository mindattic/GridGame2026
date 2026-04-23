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
    /// PARTYSECTION - Roster management (move heroes in/out of the active party).
    /// <para>PURPOSE: Two-column view — roster on the left, active party on the right. Clicking a
    /// roster hero adds them to the party; clicking a party hero removes them (party can never
    /// drop to zero). Detail label shows the selected hero's level, class, and equipped gear.</para>
    /// <para>RELATED FILES: HubManager.cs, ProfileHelper.cs, ActorLibrary.cs</para>
    /// </summary>
    public class PartySection : HubSection
    {
        private const int MaxPartySize = 4;
        private CharacterClass selected = CharacterClass.None;

        public override void Refresh()
        {
            RefreshRoster();
            RefreshParty();
            RefreshDetail();
        }

        private void RefreshRoster()
        {
            var list = FindList(GameObjectHelper.Hub.RosterList + "/Viewport/Content");
            if (list == null) return;
            ClearList(list);
            var save = ProfileHelper.CurrentProfile?.CurrentSave;
            if (save?.Roster?.Members == null) return;
            var inParty = save.Party?.Members?.Select(m => m.CharacterClass).ToHashSet() ?? new System.Collections.Generic.HashSet<CharacterClass>();

            foreach (var member in save.Roster.Members)
            {
                if (inParty.Contains(member.CharacterClass)) continue;
                var row = HubItemRowFactory.Create(list);
                var (level, _) = ExperienceHelper.DeriveFromTotalXP(member.TotalXP);
                HubItemRowFactory.SetLabel(row, member.CharacterClass.ToString());
                HubItemRowFactory.SetSubLabel(row, $"Lv {level}");
                HubItemRowFactory.SetIconColor(row, ClassTint(member.CharacterClass));
                var captured = member.CharacterClass;
                row.GetComponent<Button>().onClick.AddListener(() => AddHero(captured));
                HubItemRowFactory.SetSelected(row, member.CharacterClass == selected);
            }
        }

        private void RefreshParty()
        {
            var list = FindList(GameObjectHelper.Hub.PartyList + "/Viewport/Content");
            if (list == null) return;
            ClearList(list);
            var save = ProfileHelper.CurrentProfile?.CurrentSave;
            if (save?.Party?.Members == null) return;

            foreach (var member in save.Party.Members)
            {
                var row = HubItemRowFactory.Create(list);
                var (level, _) = ExperienceHelper.DeriveFromTotalXP(member.TotalXP);
                HubItemRowFactory.SetLabel(row, member.CharacterClass.ToString());
                HubItemRowFactory.SetSubLabel(row, $"Lv {level} — tap to remove");
                HubItemRowFactory.SetIconColor(row, ClassTint(member.CharacterClass));
                var captured = member.CharacterClass;
                row.GetComponent<Button>().onClick.AddListener(() => RemoveHero(captured));
                HubItemRowFactory.SetSelected(row, member.CharacterClass == selected);
            }
        }

        private void RefreshDetail()
        {
            var detail = FindLabel(GameObjectHelper.Hub.DetailLabel);
            if (detail == null) return;
            if (selected == CharacterClass.None)
            {
                detail.text = "<b>Party</b>\nSelect a hero to view details.\nMax party size: " + MaxPartySize + ".";
                return;
            }
            var data = ActorLibrary.Get(selected);
            var pair = ProfileHelper.CurrentProfile?.CurrentSave?.Roster?.Members?.FirstOrDefault(m => m.CharacterClass == selected);
            if (data == null || pair == null) { detail.text = selected.ToString(); return; }
            var (level, currentXP) = ExperienceHelper.DeriveFromTotalXP(pair.TotalXP);
            var stats = data.GetStats(level);
            detail.text = $"<b>{selected}</b>  Lv {level}\n"
                + $"HP {stats.MaxHP:0}  AP {stats.MaxAP:0}\n"
                + $"STR {stats.Strength:0}  VIT {stats.Vitality:0}  AGI {stats.Agility:0}\n"
                + $"INT {stats.Intelligence:0}  WIS {stats.Wisdom:0}  LCK {stats.Luck:0}\n"
                + $"XP to next: {ExperienceHelper.NextLevel(level) - currentXP}";
        }

        private void AddHero(CharacterClass cc)
        {
            selected = cc;
            var party = ProfileHelper.CurrentProfile?.CurrentSave?.Party?.Members;
            if (party == null) { Refresh(); return; }
            if (party.Count >= MaxPartySize) { Refresh(); return; }
            ProfileHelper.AddToParty(cc);
            Hub.PersistAndRefresh();
        }

        private void RemoveHero(CharacterClass cc)
        {
            selected = cc;
            var party = ProfileHelper.CurrentProfile?.CurrentSave?.Party?.Members;
            if (party == null || party.Count <= 1) { Refresh(); return; }
            ProfileHelper.RemoveFromParty(cc);
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
