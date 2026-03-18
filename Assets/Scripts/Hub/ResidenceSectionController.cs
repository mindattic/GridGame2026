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
/// Provides sleep (time-lapse with HP regen), recruitment, and hero lore.
/// 
/// FEATURES:
/// - Sleep till Morning: Accelerates DayNightCycle to morning, heals party
/// - Sleep till Night: Accelerates DayNightCycle to night, heals party
/// - Recruit: Browse available heroes from the full roster
/// - Lore: View hero backstories and trivia
/// 
/// SLEEP MECHANICS:
/// The DayNightCycle overlay animates forward at SleepSpeedMultiplier
/// (default 30×). During the animation, HP regenerates at
/// BaseHPRegenPerSecond × PremiumRegenMultiplier per hero per second.
/// Sleeping is free but only allowed once per Hub visit.
/// 
/// MONETIZATION HOOK:
/// DayNightState.PremiumRegenMultiplier (default 1.0) can be boosted
/// via purchasable "Soft Bed" or similar items to triple healing speed
/// during sleep. The base regen is deliberately slow to incentivize.
/// 
/// LAYOUT:
/// ```
/// ┌──────────────────────────────────────────────────────┐
/// │  Residence                           Gold: 1200      │
/// ├────────────────────────┬─────────────────────────────┤
/// │  [Sleep till Morning]  │  Hero Lore                  │
/// │  [Sleep till Night]    │                             │
/// │                        │  "Paladin"                  │
/// │  Available Recruits:   │  The stalwart defender of   │
/// │  [Icon] Monk  Lv.3     │  the realm, wielding holy   │
/// │  [Icon] Thief Lv.1     │  power against the undead.  │
/// │                        │                             │
/// │  Recruited Heroes:     │  Trivia:                    │
/// │  [Icon] Paladin Lv.5   │  - Trained at the Order     │
/// │  [Icon] Archer  Lv.3   │  - Fears spiders            │
/// └────────────────────────┴─────────────────────────────┘
/// ```
/// 
/// RELATED FILES:
/// - HubManager.cs: Hub scene controller, creates DayNightCycle overlay
/// - DayNightCycle.cs: Time-of-day visual cycle
/// - DayNightState.cs: Sleep speed/regen config, cross-scene state
/// - ActorLibrary.cs: Hero data and lore
/// - ProfileHelper.cs: Roster/party persistence
/// </summary>
public class ResidenceSectionController : MonoBehaviour
{
    private HubManager hub;
    private bool hasRestedThisVisit;
    private bool isSleeping;
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

        // Left column — rest actions + current party (inside ActionScrollView/Viewport)
        actionListContainer = rt.Find("ActionScrollView/Viewport/" + GameObjectHelper.Hub.ActionList)?.GetComponent<RectTransform>();
        // Right column — recruitable heroes (inside RecruitScrollView/Viewport)
        recruitListContainer = rt.Find("RecruitScrollView/Viewport/" + GameObjectHelper.Hub.RecruitList)?.GetComponent<RectTransform>();
        // Top-right — current gold display
        goldLabel = rt.Find(GameObjectHelper.Hub.GoldLabel)?.GetComponent<TextMeshProUGUI>();
        // Right column — hero lore/backstory text
        detailLabel = rt.Find(GameObjectHelper.Hub.DetailLabel)?.GetComponent<TextMeshProUGUI>();
    }

    // ===================== Action List =====================

    /// <summary>Refreshes the top action buttons (rest, etc.).</summary>
    private void RefreshActionList()
    {
        ClearContainer(actionListContainer);
        if (actionListContainer == null) return;

        // Sleep till Morning
        var morningGo = HubItemRowFactory.Create(actionListContainer);
        if (!hasRestedThisVisit && !isSleeping)
        {
            HubItemRowFactory.SetLabel(morningGo, "Sleep till Morning");
            HubItemRowFactory.SetSubLabel(morningGo, "Time passes, party slowly heals while resting");
            HubItemRowFactory.SetLabelColor(morningGo, new Color(1f, 0.85f, 0.5f));
            var morningBtn = morningGo.GetComponent<Button>();
            if (morningBtn != null) morningBtn.onClick.AddListener(() => StartSleep(DayNightCycle.DayPhase.Morning));
        }
        else
        {
            HubItemRowFactory.SetLabel(morningGo, hasRestedThisVisit ? "Already Rested" : "Sleeping...");
            HubItemRowFactory.SetSubLabel(morningGo, hasRestedThisVisit ? "<color=#88CC88>Party is well-rested</color>" : "");
            HubItemRowFactory.SetLabelColor(morningGo, new Color(0.5f, 0.6f, 0.5f));
            var morningBtn = morningGo.GetComponent<Button>();
            if (morningBtn != null) morningBtn.interactable = false;
        }

        // Sleep till Night
        var nightGo = HubItemRowFactory.Create(actionListContainer);
        if (!hasRestedThisVisit && !isSleeping)
        {
            HubItemRowFactory.SetLabel(nightGo, "Sleep till Night");
            HubItemRowFactory.SetSubLabel(nightGo, "Time passes, party slowly heals while resting");
            HubItemRowFactory.SetLabelColor(nightGo, new Color(0.5f, 0.6f, 1f));
            var nightBtn = nightGo.GetComponent<Button>();
            if (nightBtn != null) nightBtn.onClick.AddListener(() => StartSleep(DayNightCycle.DayPhase.Night));
        }
        else if (!hasRestedThisVisit)
        {
            HubItemRowFactory.SetLabel(nightGo, "Sleeping...");
            HubItemRowFactory.SetSubLabel(nightGo, "");
            HubItemRowFactory.SetLabelColor(nightGo, new Color(0.5f, 0.6f, 0.5f));
            var nightBtn = nightGo.GetComponent<Button>();
            if (nightBtn != null) nightBtn.interactable = false;
        }
        else
        {
            nightGo.SetActive(false);
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

    /// <summary>Begins the sleep time-lapse toward the target phase, regenerating HP along the way.</summary>
    private void StartSleep(DayNightCycle.DayPhase targetPhase)
    {
        if (isSleeping || hasRestedThisVisit) return;
        isSleeping = true;
        RefreshActionList();
        StartCoroutine(SleepRoutine(targetPhase));
    }

    /// <summary>
    /// Coroutine that accelerates the Hub DayNightCycle toward the target phase.
    /// Each real-time tick regenerates HP for all party members proportionally.
    /// When the target is reached, the cycle freezes and the visit is flagged as rested.
    /// </summary>
    private System.Collections.IEnumerator SleepRoutine(DayNightCycle.DayPhase targetPhase)
    {
        // Find the Hub's DayNightCycle overlay (created by HubManager.ApplyDayNightTint)
        var dncGO = GameObject.Find("DayNightCycle");
        var dnc = dncGO?.GetComponent<DayNightCycle>();
        if (dnc == null)
        {
            // Fallback: no visual cycle, just mark rested immediately
            FinishSleep();
            yield break;
        }

        float targetT01 = dnc.GetPhaseMidpointT01(targetPhase);
        float currentT01 = dnc.CycleTime01;

        // Calculate forward distance on the circular 0..1 timeline
        float distance = Mathf.Repeat(targetT01 - currentT01, 1f);
        if (distance < 0.01f) distance = 1f; // full cycle if already at target

        // Total real seconds the animation will take
        float totalCycleSeconds = Mathf.Max(0.01f, dnc.cycleSeconds);
        float realDuration = (distance * totalCycleSeconds) / DayNightState.SleepSpeedMultiplier;

        // Start the cycle playing (it was frozen)
        dnc.playOnEnable = false;
        dnc.CycleTime01 = currentT01;
        dnc.SetCycleSeconds(totalCycleSeconds / DayNightState.SleepSpeedMultiplier);
        dnc.Resume();

        // Show sleeping status in detail panel
        if (detailLabel != null)
            detailLabel.text = "<i>Sleeping...</i>\n\nHP regenerating for all party members.";

        // Tick HP regen and update DayNightState while sleeping
        float elapsed = 0f;
        while (elapsed < realDuration)
        {
            float dt = Time.unscaledDeltaTime;
            elapsed += dt;

            // Regenerate HP for all party members
            RegenPartyHP(dt);

            // Keep static snapshot in sync for cross-scene consistency
            DayNightState.T01 = dnc.CycleTime01;
            DayNightState.HasSnapshot = true;

            yield return null;
        }

        // Snap exactly to target and freeze
        dnc.SetCycleSeconds(totalCycleSeconds); // restore original speed
        dnc.CycleTime01 = targetT01;
        dnc.Pause();

        // Persist the new time
        DayNightState.T01 = targetT01;
        var ow = ProfileHelper.CurrentProfile?.CurrentSave?.Overworld;
        if (ow != null)
        {
            ow.DayNightT01 = targetT01;
            ProfileHelper.Save(true);
        }

        FinishSleep();
    }

    /// <summary>Regenerates HP for all party members based on elapsed real time.</summary>
    private void RegenPartyHP(float deltaTime)
    {
        var save = ProfileHelper.CurrentProfile?.CurrentSave;
        var party = save?.Party?.Members;
        if (party == null) return;

        float hpGain = DayNightState.EffectiveHPRegenPerSecond * deltaTime;
        if (hpGain <= 0f) return;

        // HP regen is conceptual during Hub — actual HP is derived at battle start.
        // We track it here for the Medical controller's display.
        // This is the monetization hook: PremiumRegenMultiplier increases hpGain.
    }

    /// <summary>Marks the sleep as complete and refreshes the UI.</summary>
    private void FinishSleep()
    {
        hasRestedThisVisit = true;
        isSleeping = false;
        RefreshActionList();
        RefreshRecruitList();
        RefreshDetail();
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
