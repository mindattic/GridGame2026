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
/// RESIDENCESECTIONCONTROLLER - Hub inn/tavern section.
/// 
/// PURPOSE:
/// The Residence acts as the party's home base between battles.
/// Provides rest, recruitment, and hero lore browsing.
/// 
/// FEATURES:
/// - Rest: Heal all heroes to full HP for free (once per visit)
/// - Recruit: Browse available heroes from the full roster
/// - Lore: View hero backstories and trivia
/// - Trophy display (decoration stubs for future)
/// 
/// LAYOUT:
/// ```
/// ┌──────────────────────────────────────────────────────┐
/// │  Residence                           Gold: 1200      │
/// ├────────────────────────┬─────────────────────────────┤
/// │  [Rest at Inn — Free]  │  Hero Lore                  │
/// │                        │                             │
/// │  Available Recruits:   │  "Paladin"                  │
/// │  [Icon] Monk  Lv.3     │  The stalwart defender of   │
/// │  [Icon] Thief Lv.1     │  the realm, wielding holy   │
/// │  [Icon] Sage  Lv.2     │  power against the undead.  │
/// │                        │                             │
/// │  Recruited Heroes:     │  Trivia:                    │
/// │  [Icon] Paladin Lv.5   │  - Trained at the Order     │
/// │  [Icon] Archer  Lv.3   │  - Fears spiders            │
/// └────────────────────────┴─────────────────────────────┘
/// ```
/// 
/// RELATED FILES:
/// - HubManager.cs: Hub scene controller
/// - ActorLibrary.cs: Hero data and lore
/// - ProfileHelper.cs: Roster/party persistence
/// </summary>
public class ResidenceSectionController : MonoBehaviour
{
    private HubManager hub;
    private bool hasRestedThisVisit;
    private CharacterClass selectedHero;

    // Runtime UI references
    private RectTransform actionListContainer;
    private RectTransform recruitListContainer;
    private TextMeshProUGUI goldLabel;
    private TextMeshProUGUI detailLabel;

    private PlayerInventory Inventory => hub?.SharedInventory;

    /// <summary>Initializes the section.</summary>
    public void Initialize(HubManager hubManager)
    {
        hub = hubManager;
        ResolveUI();
        HubVendorFactory.Build(GetComponent<RectTransform>(), HubVendorFactory.ResidenceTheme);
    }

    /// <summary>Called when activated.</summary>
    public void OnActivated()
    {
        selectedHero = CharacterClass.None;
        RefreshActionList();
        RefreshRecruitList();
        RefreshGoldDisplay();
        RefreshDetail();
    }

    /// <summary>Resolves UI references from scene hierarchy.</summary>
    private void ResolveUI()
    {
        var rt = GetComponent<RectTransform>();
        if (rt == null) return;

        actionListContainer = rt.Find("ActionList")?.GetComponent<RectTransform>();
        recruitListContainer = rt.Find("RecruitList")?.GetComponent<RectTransform>();
        goldLabel = rt.Find("GoldLabel")?.GetComponent<TextMeshProUGUI>();
        detailLabel = rt.Find("DetailLabel")?.GetComponent<TextMeshProUGUI>();
    }

    // ===================== Action List =====================

    /// <summary>Refreshes the top action buttons (rest, etc.).</summary>
    private void RefreshActionList()
    {
        ClearContainer(actionListContainer);
        if (actionListContainer == null) return;

        // Rest at inn
        var restGo = HubItemRowFactory.Create(actionListContainer);
        if (!hasRestedThisVisit)
        {
            HubItemRowFactory.SetLabel(restGo, "Rest at the Inn");
            HubItemRowFactory.SetSubLabel(restGo, "Restore all heroes to full HP (free, once per visit)");
            HubItemRowFactory.SetLabelColor(restGo, new Color(0.5f, 1f, 0.7f));
            var restBtn = restGo.GetComponent<Button>();
            if (restBtn != null) restBtn.onClick.AddListener(() =>
            {
                RestAtInn();
                RefreshActionList();
            });
        }
        else
        {
            HubItemRowFactory.SetLabel(restGo, "Already Rested");
            HubItemRowFactory.SetSubLabel(restGo, "<color=#88CC88>Party is well-rested</color>");
            HubItemRowFactory.SetLabelColor(restGo, new Color(0.5f, 0.6f, 0.5f));
            var restBtn = restGo.GetComponent<Button>();
            if (restBtn != null) restBtn.interactable = false;
        }

        // Party summary header
        var headerGo = HubItemRowFactory.Create(actionListContainer);
        var party = ProfileHelper.CurrentProfile?.CurrentSave?.Party?.Members;
        int partyCount = party?.Count ?? 0;
        HubItemRowFactory.SetLabel(headerGo, $"Current Party ({partyCount}/4)");
        HubItemRowFactory.SetSubLabel(headerGo, "Tap a hero below to view lore");
        HubItemRowFactory.SetLabelColor(headerGo, new Color(0.7f, 0.7f, 0.8f));
        var headerBtn = headerGo.GetComponent<Button>();
        if (headerBtn != null) headerBtn.interactable = false;

        // Show current party members with tap-to-view-lore
        if (party != null)
        {
            foreach (var member in party)
            {
                var cc = member.CharacterClass;
                if (cc == CharacterClass.None) continue;

                var derived = ExperienceHelper.DeriveFromTotalXP(Mathf.Max(0, member.TotalXP));
                int level = Mathf.Max(1, derived.level);

                var go = HubItemRowFactory.Create(actionListContainer);
                var heroData = ActorLibrary.Get(cc);
                if (heroData?.Portrait != null)
                    HubItemRowFactory.SetIconSprite(go, heroData.Portrait);
                else
                    HubItemRowFactory.SetIconColor(go, new Color(0.5f, 0.4f, 0.3f));

                HubItemRowFactory.SetLabel(go, $"{heroData?.CharacterName ?? cc.ToString()}  Lv.{level}");
                HubItemRowFactory.SetSubLabel(go, TruncateDescription(heroData?.Description));
                HubItemRowFactory.SetSelected(go, cc == selectedHero);

                var capturedCC = cc;
                var btn = go.GetComponent<Button>();
                if (btn != null) btn.onClick.AddListener(() =>
                {
                    selectedHero = capturedCC;
                    RefreshActionList();
                    RefreshDetail();
                });
            }
        }
    }

    // ===================== Recruit List =====================

    /// <summary>Refreshes the recruitable hero list (roster members not in party).</summary>
    private void RefreshRecruitList()
    {
        ClearContainer(recruitListContainer);
        if (recruitListContainer == null) return;

        var save = ProfileHelper.CurrentProfile?.CurrentSave;
        if (save?.Roster?.Members == null) return;

        var partySet = new HashSet<CharacterClass>(
            save.Party?.Members?.Select(m => m.CharacterClass) ?? Enumerable.Empty<CharacterClass>());

        // Header
        int availableCount = save.Roster.Members.Count(m => !partySet.Contains(m.CharacterClass) && m.CharacterClass != CharacterClass.None);
        if (availableCount > 0)
        {
            var headerGo = HubItemRowFactory.Create(recruitListContainer);
            HubItemRowFactory.SetLabel(headerGo, $"Available Recruits ({availableCount})");
            HubItemRowFactory.SetSubLabel(headerGo, "Heroes waiting at the inn");
            HubItemRowFactory.SetLabelColor(headerGo, new Color(0.8f, 0.75f, 0.55f));
            var hdrBtn = headerGo.GetComponent<Button>();
            if (hdrBtn != null) hdrBtn.interactable = false;
        }

        foreach (var member in save.Roster.Members)
        {
            var cc = member.CharacterClass;
            if (cc == CharacterClass.None) continue;
            if (partySet.Contains(cc)) continue;

            var derived = ExperienceHelper.DeriveFromTotalXP(Mathf.Max(0, member.TotalXP));
            int level = Mathf.Max(1, derived.level);

            var go = HubItemRowFactory.Create(recruitListContainer);

            var heroData = ActorLibrary.Get(cc);
            if (heroData?.Portrait != null)
                HubItemRowFactory.SetIconSprite(go, heroData.Portrait);
            else
                HubItemRowFactory.SetIconColor(go, new Color(0.4f, 0.4f, 0.5f));

            HubItemRowFactory.SetLabel(go, $"{heroData?.CharacterName ?? cc.ToString()}  Lv.{level}");

            bool partyFull = (save.Party?.Members?.Count ?? 0) >= 4;
            HubItemRowFactory.SetSubLabel(go, partyFull ? "Party is full" : "Tap to add to party");

            var capturedCC = cc;
            var btn = go.GetComponent<Button>();
            if (btn != null)
            {
                btn.interactable = !partyFull;
                btn.onClick.AddListener(() =>
                {
                    AddToParty(capturedCC);
                    RefreshActionList();
                    RefreshRecruitList();
                    RefreshDetail();
                });
            }
        }
    }

    // ===================== Hero Lore Detail =====================

    /// <summary>Refreshes the detail panel with hero lore.</summary>
    private void RefreshDetail()
    {
        if (detailLabel == null) return;

        if (selectedHero == CharacterClass.None)
        {
            detailLabel.text = "Select a hero to view their story";
            return;
        }

        var actorData = ActorLibrary.Get(selectedHero);
        if (actorData == null)
        {
            detailLabel.text = $"{selectedHero} — No data available";
            return;
        }

        var save = ProfileHelper.CurrentProfile?.CurrentSave;
        var member = save?.Party?.Members?.FirstOrDefault(m => m.CharacterClass == selectedHero)
                  ?? save?.Roster?.Members?.FirstOrDefault(m => m.CharacterClass == selectedHero);

        var derived = member != null
            ? ExperienceHelper.DeriveFromTotalXP(Mathf.Max(0, member.TotalXP))
            : (level: 1, currentXP: 0);
        int level = Mathf.Max(1, derived.level);

        var lines = new List<string>();
        lines.Add($"<b>{actorData.CharacterName ?? selectedHero.ToString()}</b>  Level {level}");
        lines.Add("");

        // Description / Lore
        if (!string.IsNullOrEmpty(actorData.Description))
            lines.Add($"<i>{actorData.Description}</i>");
        if (!string.IsNullOrEmpty(actorData.Lore))
        {
            lines.Add("");
            lines.Add(actorData.Lore);
        }

        // Expectations
        if (!string.IsNullOrEmpty(actorData.Expectations))
        {
            lines.Add("");
            lines.Add($"<color=#CCAA66>Role: {actorData.Expectations}</color>");
        }

        // Trivia
        if (actorData.Trivia != null && actorData.Trivia.Count > 0)
        {
            lines.Add("");
            lines.Add("<color=#88AACC>Trivia:</color>");
            foreach (var t in actorData.Trivia)
                lines.Add($"  • {t}");
        }

        // Stats overview
        var stats = actorData.GetStats(level);
        if (stats != null)
        {
            lines.Add("");
            float hp = Formulas.Health(stats);
            lines.Add($"<color=#FF8888>HP {hp:F0}</color>  STR {stats.Strength:F0}  VIT {stats.Vitality:F0}  AGI {stats.Agility:F0}");
            lines.Add($"SPD {stats.Speed:F0}  INT {stats.Intelligence:F0}  WIS {stats.Wisdom:F0}  LCK {stats.Luck:F0}");
        }

        // Abilities
        if (actorData.Abilities != null && actorData.Abilities.Count > 0)
        {
            lines.Add("");
            lines.Add("<color=#AACCFF>Abilities:</color>");
            foreach (var ab in actorData.Abilities)
                lines.Add($"  • {ab.name ?? "Unknown"}");
        }

        // Learned training skills
        var trainingData = save?.Training?.GetOrCreate(selectedHero);
        if (trainingData?.LearnedTrainingIds != null && trainingData.LearnedTrainingIds.Count > 0)
        {
            lines.Add("");
            lines.Add("<color=#88CC88>Trained Skills:</color>");
            foreach (var tid in trainingData.LearnedTrainingIds)
            {
                var td = TrainingLibrary.Get(tid);
                lines.Add($"  • {td?.DisplayName ?? tid}");
            }
        }

        detailLabel.text = string.Join("\n", lines);
    }

    // ===================== Actions =====================

    /// <summary>Rests at the inn, healing all party members.</summary>
    private void RestAtInn()
    {
        hasRestedThisVisit = true;
        // Medical controller handles actual HP — this is a flag for the hub visit
    }

    /// <summary>Adds a hero to the party.</summary>
    private void AddToParty(CharacterClass hero)
    {
        var save = ProfileHelper.CurrentProfile?.CurrentSave;
        if (save?.Party?.Members == null) return;
        if (save.Party.Members.Count >= 4) return;
        if (save.Party.Members.Any(m => m.CharacterClass == hero)) return;

        var rosterEntry = save.Roster?.Members?.FirstOrDefault(m => m.CharacterClass == hero);
        if (rosterEntry == null) return;

        save.Party.Members.Add(new CharacterLevelPair(hero, rosterEntry.TotalXP));
    }

    // ===================== Helpers =====================

    private string TruncateDescription(string desc)
    {
        if (string.IsNullOrEmpty(desc)) return "";
        return desc.Length > 60 ? desc.Substring(0, 57) + "..." : desc;
    }

    private void RefreshGoldDisplay()
    {
        if (goldLabel != null && Inventory != null)
            goldLabel.text = $"Gold: {Inventory.Gold}";
    }

    private void ClearContainer(RectTransform container)
    {
        if (container == null) return;
        for (int i = container.childCount - 1; i >= 0; i--)
            Destroy(container.GetChild(i).gameObject);
    }
}

}
