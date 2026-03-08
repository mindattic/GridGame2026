using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
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

namespace Scripts.Hub
{
/// <summary>
/// TRAININGSECTIONCONTROLLER - Hub ability training section.
/// 
/// PURPOSE:
/// Allows heroes to learn new abilities by spending gold.
/// Heroes select a party member, then browse available
/// training options filtered by tags and level.
/// 
/// FLOW:
/// 1. Select a hero from party
/// 2. View available training filtered by hero tags/level
/// 3. Pay gold to learn
/// 4. Skill added to hero's learned list (persisted)
/// 
/// RELATED FILES:
/// - HubManager.cs: Hub scene controller
/// - TrainingLibrary.cs: Training registry
/// - TrainingDefinition.cs: Training data
/// - SkillData_Training.cs: Trainable skills
/// </summary>
public class TrainingSectionController : MonoBehaviour
{
    private HubManager hub;
    private CharacterClass selectedHero;
    private List<TrainingDefinition> availableTrainings = new List<TrainingDefinition>();

    // Runtime UI references
    private RectTransform heroListContainer;
    private RectTransform trainingListContainer;
    private TextMeshProUGUI goldLabel;
    private TextMeshProUGUI detailLabel;
    private Image vendorPortrait;

    /// <summary>Initializes the controller.</summary>
    public void Initialize(HubManager hubManager)
    {
        hub = hubManager;
        ResolveUI();
        HubVendorFactory.Build(GetComponent<RectTransform>(), HubVendorFactory.TrainingTheme);
    }

    /// <summary>Called when section becomes active.</summary>
    public void OnActivated()
    {
        RefreshHeroList();
        RefreshGoldDisplay();
    }

    /// <summary>Resolves UI references from scene hierarchy.</summary>
    private void ResolveUI()
    {
        var rt = GetComponent<RectTransform>();
        if (rt == null) return;

        heroListContainer = rt.Find("HeroList")?.GetComponent<RectTransform>();
        trainingListContainer = rt.Find("TrainingList")?.GetComponent<RectTransform>();
        goldLabel = rt.Find("GoldLabel")?.GetComponent<TextMeshProUGUI>();
        detailLabel = rt.Find("DetailLabel")?.GetComponent<TextMeshProUGUI>();
        vendorPortrait = rt.Find("VendorPortrait")?.GetComponent<Image>();

        // Set vendor to Mannequin placeholder
        if (vendorPortrait != null)
        {
            var mannequin = ActorLibrary.Get(CharacterClass.Mannequin);
            if (mannequin?.Portrait != null) vendorPortrait.sprite = mannequin.Portrait;
        }
    }

    /// <summary>Selects a hero and refreshes training list.</summary>
    public void SelectHero(CharacterClass hero)
    {
        selectedHero = hero;
        RefreshTrainingList();
    }

    /// <summary>Attempts to purchase a training for the selected hero.</summary>
    public bool Train(string trainingId)
    {
        if (hub == null) return false;
        var inv = hub.SharedInventory;
        var save = ProfileHelper.CurrentProfile?.CurrentSave;
        if (inv == null || save == null) return false;

        var training = TrainingLibrary.Get(trainingId);
        if (training == null) return false;

        // Already learned?
        if (save.Training != null && save.Training.HasLearned(selectedHero, trainingId)) return false;

        // Can afford?
        if (inv.Gold < training.GoldCost) return false;

        inv.Gold -= training.GoldCost;

        // Persist learning
        if (save.Training == null) save.Training = new TrainingSaveData();
        var heroTraining = save.Training.GetOrCreate(selectedHero);
        heroTraining.LearnedTrainingIds.Add(trainingId);

        RefreshTrainingList();
        RefreshGoldDisplay();
        return true;
    }

    /// <summary>Refreshes the hero selection list.</summary>
    private void RefreshHeroList()
    {
        if (heroListContainer == null) return;
        ClearChildren(heroListContainer);

        var party = ProfileHelper.CurrentProfile?.CurrentSave?.Party?.Members;
        if (party == null) return;

        foreach (var member in party)
        {
            var cc = member.CharacterClass;
            var go = HubItemRowFactory.Create(heroListContainer);

            // Hero portrait icon
            var heroData = ActorLibrary.Get(cc);
            if (heroData?.Portrait != null)
                HubItemRowFactory.SetIconSprite(go, heroData.Portrait);
            else
                HubItemRowFactory.SetIconColor(go, new Color(0.3f, 0.3f, 0.6f));

            var derived = ExperienceHelper.DeriveFromTotalXP(Mathf.Max(0, member.TotalXP));
            int level = Mathf.Max(1, derived.level);
            HubItemRowFactory.SetLabel(go, $"{cc}  Lv.{level}");

            // Count learned skills
            var save = ProfileHelper.CurrentProfile?.CurrentSave;
            int learnedCount = save?.Training?.GetOrCreate(cc)?.LearnedTrainingIds?.Count ?? 0;
            HubItemRowFactory.SetSubLabel(go, $"{learnedCount} skills learned");

            var btn = go.GetComponent<Button>();
            if (btn != null) btn.onClick.AddListener(() => SelectHero(cc));
        }

        // Auto-select first
        if (party.Count > 0) SelectHero(party[0].CharacterClass);
    }

    /// <summary>Refreshes training options for selected hero.</summary>
    private void RefreshTrainingList()
    {
        if (trainingListContainer == null) return;
        ClearChildren(trainingListContainer);

        var save = ProfileHelper.CurrentProfile?.CurrentSave;
        var member = save?.Party?.Members?.FirstOrDefault(m => m.CharacterClass == selectedHero);
        var derivedXP = ExperienceHelper.DeriveFromTotalXP(Mathf.Max(0, member?.TotalXP ?? 0));
        int level = Mathf.Max(1, derivedXP.level);

        availableTrainings = TrainingLibrary.ForHero(selectedHero, level).ToList();

        foreach (var t in availableTrainings)
        {
            bool learned = save?.Training != null && save.Training.HasLearned(selectedHero, t.Id);
            var go = HubItemRowFactory.Create(trainingListContainer);

            if (learned)
            {
                HubItemRowFactory.SetLabel(go, $"{t.DisplayName}");
                HubItemRowFactory.SetSubLabel(go, "<color=#88CC88>Learned</color>");
                HubItemRowFactory.SetLabelColor(go, new Color(0.5f, 0.6f, 0.5f));
            }
            else
            {
                bool canAfford = hub?.SharedInventory != null && hub.SharedInventory.Gold >= t.GoldCost;
                HubItemRowFactory.SetLabel(go, $"{t.DisplayName} — {t.GoldCost}g");
                HubItemRowFactory.SetSubLabel(go, !string.IsNullOrEmpty(t.Description) ? t.Description : $"Requires Lv.{t.MinLevel}");
                HubItemRowFactory.SetLabelColor(go, canAfford ? Color.white : new Color(0.7f, 0.5f, 0.5f));
            }

            var btn = go.GetComponent<Button>();
            if (btn != null)
            {
                btn.interactable = !learned;
                var id = t.Id;
                btn.onClick.AddListener(() => Train(id));
            }
        }

        if (detailLabel != null)
            detailLabel.text = $"Training for: {selectedHero}";
    }

    /// <summary>Refreshes gold display.</summary>
    private void RefreshGoldDisplay()
    {
        if (goldLabel != null && hub?.SharedInventory != null)
            goldLabel.text = $"Gold: {hub.SharedInventory.Gold}";
    }

    /// <summary>Clears all child GameObjects of a container.</summary>
    private void ClearChildren(RectTransform container)
    {
        for (int i = container.childCount - 1; i >= 0; i--)
            Destroy(container.GetChild(i).gameObject);
    }
}

}
