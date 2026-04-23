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
    /// TRAININGSECTION - Teach heroes new abilities for gold.
    /// <para>PURPOSE: Two-column hero / training picker. Left column lists party heroes; selecting
    /// one loads the right column with <see cref="TrainingLibrary.ForHero"/> — trainings whose
    /// level + tag requirements the hero meets. Learned trainings persist to
    /// <see cref="TrainingSaveData"/>.</para>
    /// <para>RELATED FILES: HubManager.cs, TrainingLibrary.cs, TrainingDefinition.cs</para>
    /// </summary>
    public class TrainingSection : HubSection
    {
        private CharacterClass selectedHero = CharacterClass.None;
        private TrainingDefinition selectedTraining;

        protected override void OnActivated()
        {
            if (selectedHero == CharacterClass.None) selectedHero = FirstHero();
            Wire(FindButton("ConfirmButton"), Learn);
        }

        public override void Refresh()
        {
            RefreshHeroList();
            RefreshTrainingList();
            RefreshDetail();
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

            foreach (var member in party)
            {
                var row = HubItemRowFactory.Create(list);
                var (level, _) = ExperienceHelper.DeriveFromTotalXP(member.TotalXP);
                HubItemRowFactory.SetLabel(row, member.CharacterClass.ToString());
                HubItemRowFactory.SetSubLabel(row, $"Lv {level}");
                HubItemRowFactory.SetIconColor(row, ClassTint(member.CharacterClass));
                var captured = member.CharacterClass;
                row.GetComponent<Button>().onClick.AddListener(() => { selectedHero = captured; selectedTraining = null; Refresh(); });
                HubItemRowFactory.SetSelected(row, member.CharacterClass == selectedHero);
            }
        }

        private void RefreshTrainingList()
        {
            var list = FindList(GameObjectHelper.Hub.TrainingList + "/Viewport/Content");
            if (list == null) return;
            ClearList(list);
            if (selectedHero == CharacterClass.None) return;

            var level = HeroLevel(selectedHero);
            var training = ProfileHelper.CurrentProfile?.CurrentSave?.Training;

            foreach (var def in TrainingLibrary.ForHero(selectedHero, level))
            {
                var row = HubItemRowFactory.Create(list);
                HubItemRowFactory.SetLabel(row, def.DisplayName);
                bool known = training != null && training.HasLearned(selectedHero, def.Id);
                bool canAfford = Hub.Inventory.Gold >= def.GoldCost;
                string costText = HubTheme.FormatGold(def.GoldCost);
                string status = known ? "<color=#88CC88>Learned</color>"
                               : HubTheme.ColorByAffordable(costText, canAfford);
                HubItemRowFactory.SetSubLabel(row, status);
                var captured = def;
                row.GetComponent<Button>().onClick.AddListener(() => { if (!known) { selectedTraining = captured; Refresh(); } });
                HubItemRowFactory.SetSelected(row, selectedTraining != null && selectedTraining.Id == def.Id);
            }
        }

        private void RefreshDetail()
        {
            var detail = FindLabel(GameObjectHelper.Hub.DetailLabel);
            if (detail == null) return;
            if (selectedHero == CharacterClass.None)
            {
                detail.text = "<b>Training Hall</b>\nPick a hero to see what the master can teach them.";
                return;
            }
            if (selectedTraining == null)
            {
                detail.text = $"<b>{selectedHero}</b>\nSelect a training to learn.";
                return;
            }
            detail.text = $"<b>{selectedTraining.DisplayName}</b>\n"
                        + $"{selectedTraining.Description}\n\n"
                        + $"Cost: {HubTheme.FormatGold(selectedTraining.GoldCost)}\n"
                        + $"Min level: {selectedTraining.MinLevel}";
        }

        private void Learn()
        {
            if (selectedHero == CharacterClass.None || selectedTraining == null) return;
            if (Hub.Inventory.Gold < selectedTraining.GoldCost) return;
            var save = ProfileHelper.CurrentProfile?.CurrentSave;
            if (save == null) return;
            if (save.Training == null) save.Training = new TrainingSaveData();
            if (save.Training.HasLearned(selectedHero, selectedTraining.Id)) return;
            var entry = save.Training.GetOrCreate(selectedHero);
            entry.LearnedTrainingIds.Add(selectedTraining.Id);
            Hub.Inventory.Gold -= selectedTraining.GoldCost;
            selectedTraining = null;
            Hub.PersistAndRefresh();
        }

        private int HeroLevel(CharacterClass cc)
        {
            var pair = ProfileHelper.CurrentProfile?.CurrentSave?.Party?.Members?.FirstOrDefault(m => m.CharacterClass == cc);
            if (pair == null) return 1;
            var (level, _) = ExperienceHelper.DeriveFromTotalXP(pair.TotalXP);
            return level;
        }

        private static Color ClassTint(CharacterClass cc)
        {
            int h = cc.ToString().GetHashCode();
            float hue = Mathf.Repeat(h * 0.000173f, 1f);
            return Color.HSVToRGB(hue, 0.5f, 0.8f);
        }
    }
}
