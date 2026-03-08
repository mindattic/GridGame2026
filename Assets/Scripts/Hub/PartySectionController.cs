using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Scripts.Helpers;
using Scripts.Libraries;
using Scripts.Canvas;
using Scripts.Data.Actor;
using Scripts.Data.Items;
using Scripts.Data.Skills;
using Scripts.Effects;
using Scripts.Factories;
using Scripts.Instances;
using Scripts.Instances.Actor;
using Scripts.Instances.Board;
using Scripts.Instances.SynergyLine;
using Scripts.Inventory;
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
/// PARTYSECTIONCONTROLLER - Hub party management section.
/// 
/// PURPOSE:
/// Manages the party selection UI in the Hub, allowing players
/// to add/remove heroes from the active party, view hero stats,
/// and see per-hero equipment summaries.
/// 
/// LAYOUT:
/// ```
/// ┌───────────────┬───────────────────────────────┐
/// │  Roster       │  Active Party (max 4)         │
/// │  ┌─────────┐  │  ┌──────────────────────────┐ │
/// │  │ Monk    │→ │  │ Paladin  Lv5  STR 12 ... │ │
/// │  │ Thief   │→ │  │ Archer   Lv3  STR  8 ... │ │
/// │  └─────────┘  │  └──────────────────────────┘ │
/// │               │                               │
/// │               │  Selected: Paladin            │
/// │               │  STR 12  VIT 8  AGI 5  SPD 6 │
/// │               │  INT 3   WIS 4  LCK 5        │
/// │               │  Weapon: Iron Sword           │
/// │               │  Armor: Chain Mail            │
/// └───────────────┴───────────────────────────────┘
/// ```
/// 
/// RELATED FILES:
/// - HubManager.cs: Hub scene controller
/// - HeroLoadout.cs: Hero equipment/skills
/// - ProfileHelper.cs: Save data
/// </summary>
public class PartySectionController : MonoBehaviour
{
    private HubManager hub;
    private CharacterClass selectedHero;

    private const int MaxPartySize = 4;

    // Runtime UI references
    private RectTransform rosterContainer;
    private RectTransform partyContainer;
    private TextMeshProUGUI detailLabel;
    private TextMeshProUGUI goldLabel;

    /// <summary>Initializes this controller.</summary>
    public void Initialize(HubManager hubManager)
    {
        hub = hubManager;
        ResolveUI();
        HubVendorFactory.Build(GetComponent<RectTransform>(), HubVendorFactory.PartyTheme);
    }

    /// <summary>Called when section becomes active.</summary>
    public void OnActivated()
    {
        RefreshAll();
    }

    /// <summary>Resolves UI references from scene hierarchy.</summary>
    private void ResolveUI()
    {
        var rt = GetComponent<RectTransform>();
        if (rt == null) return;

        // Left column — scrollable list of roster heroes not yet in party
        rosterContainer = rt.Find(GameObjectHelper.Hub.RosterList)?.GetComponent<RectTransform>();
        // Right column — scrollable list of active party members (max 4)
        partyContainer = rt.Find(GameObjectHelper.Hub.PartyList)?.GetComponent<RectTransform>();
        // Right side — multi-line stat block for the selected hero
        detailLabel = rt.Find(GameObjectHelper.Hub.DetailLabel)?.GetComponent<TextMeshProUGUI>();
        // Top-right — current gold display
        goldLabel = rt.Find(GameObjectHelper.Hub.GoldLabel)?.GetComponent<TextMeshProUGUI>();
    }

    /// <summary>Refreshes all lists and detail display.</summary>
    private void RefreshAll()
    {
        RefreshPartyList();
        RefreshRosterList();
        RefreshDetail();
        RefreshGoldDisplay();
    }

    // ===================== Party List =====================

    /// <summary>Refreshes the active party member list.</summary>
    private void RefreshPartyList()
    {
        ClearContainer(partyContainer);
        if (partyContainer == null) return;

        var party = ProfileHelper.CurrentProfile?.CurrentSave?.Party?.Members;
        if (party == null) return;

        foreach (var member in party)
        {
            var cc = member.CharacterClass;
            if (cc == CharacterClass.None) continue;

            var derived = ExperienceHelper.DeriveFromTotalXP(Mathf.Max(0, member.TotalXP));
            int level = Mathf.Max(1, derived.level);

            var go = HubItemRowFactory.Create(partyContainer);

            // Hero portrait icon
            var heroData = ActorLibrary.Get(cc);
            if (heroData?.Portrait != null)
                HubItemRowFactory.SetIconSprite(go, heroData.Portrait);
            else
                HubItemRowFactory.SetIconColor(go, new Color(0.5f, 0.3f, 0.3f));

            HubItemRowFactory.SetLabel(go, $"{cc}  Lv.{level}");

            // Show XP progress + equipped weapon/armor summary
            int currentXP = derived.currentXP;
            int xpNeeded = ExperienceHelper.NextLevel(level);
            float xpPct = xpNeeded > 0 ? (float)currentXP / xpNeeded * 100f : 0f;

            var loadout = hub?.SharedLoadout?.Get(cc);
            var weapon = loadout?.GetEquipped(EquipmentSlot.Weapon);
            var armor = loadout?.GetEquipped(EquipmentSlot.Armor);
            var subParts = new List<string>();
            subParts.Add($"XP {xpPct:F0}%");
            if (weapon != null) subParts.Add(weapon.DisplayName);
            if (armor != null) subParts.Add(armor.DisplayName);
            if (weapon == null && armor == null) subParts.Add("No equipment");
            HubItemRowFactory.SetSubLabel(go, string.Join("  |  ", subParts));

            HubItemRowFactory.SetSelected(go, cc == selectedHero);

            var btn = go.GetComponent<Button>();
            if (btn != null)
            {
                var capturedCC = cc;
                btn.onClick.AddListener(() =>
                {
                    if (selectedHero == capturedCC)
                    {
                        // Second tap: remove from party
                        RemoveFromParty(capturedCC);
                    }
                    else
                    {
                        selectedHero = capturedCC;
                        RefreshAll();
                    }
                });
            }
        }

        // Add a "Remove from Party" row if a hero is selected and party > 1
        if (selectedHero != CharacterClass.None && party.Count > 1
            && party.Any(m => m.CharacterClass == selectedHero))
        {
            var removeGo = HubItemRowFactory.Create(partyContainer);
            HubItemRowFactory.SetLabel(removeGo, $"Remove {selectedHero} from Party");
            HubItemRowFactory.SetSubLabel(removeGo, "Return to roster");
            HubItemRowFactory.SetLabelColor(removeGo, new Color(1f, 0.5f, 0.5f));
            var removeBtn = removeGo.GetComponent<Button>();
            if (removeBtn != null)
            {
                var capturedRemove = selectedHero;
                removeBtn.onClick.AddListener(() => RemoveFromParty(capturedRemove));
            }
        }
    }

    // ===================== Roster List =====================

    /// <summary>Refreshes the available roster heroes not in the party.</summary>
    private void RefreshRosterList()
    {
        ClearContainer(rosterContainer);
        if (rosterContainer == null) return;

        var save = ProfileHelper.CurrentProfile?.CurrentSave;
        if (save?.Roster?.Members == null) return;

        var partySet = new HashSet<CharacterClass>(
            save.Party?.Members?.Select(m => m.CharacterClass) ?? Enumerable.Empty<CharacterClass>());

        foreach (var member in save.Roster.Members)
        {
            var cc = member.CharacterClass;
            if (cc == CharacterClass.None) continue;
            if (partySet.Contains(cc)) continue;

            var derived = ExperienceHelper.DeriveFromTotalXP(Mathf.Max(0, member.TotalXP));
            int level = Mathf.Max(1, derived.level);

            var go = HubItemRowFactory.Create(rosterContainer);

            // Hero portrait icon
            var rosterData = ActorLibrary.Get(cc);
            if (rosterData?.Portrait != null)
                HubItemRowFactory.SetIconSprite(go, rosterData.Portrait);
            else
                HubItemRowFactory.SetIconColor(go, new Color(0.4f, 0.4f, 0.5f));

            HubItemRowFactory.SetLabel(go, $"{rosterData?.CharacterName ?? cc.ToString()}  Lv.{level}");

            bool partyFull = (save.Party?.Members?.Count ?? 0) >= MaxPartySize;
            string desc = rosterData?.Description;
            if (partyFull)
                HubItemRowFactory.SetSubLabel(go, "<color=#CC6666>Party is full</color>");
            else if (!string.IsNullOrEmpty(desc) && desc.Length > 50)
                HubItemRowFactory.SetSubLabel(go, desc.Substring(0, 47) + "...");
            else
                HubItemRowFactory.SetSubLabel(go, !string.IsNullOrEmpty(desc) ? desc : "Tap to add to party");

            var btn = go.GetComponent<Button>();
            if (btn != null)
            {
                btn.interactable = !partyFull;
                btn.onClick.AddListener(() =>
                {
                    AddToParty(cc);
                    selectedHero = cc;
                });
            }
        }
    }

    // ===================== Party Management =====================

    /// <summary>Adds a hero from the roster to the active party.</summary>
    public void AddToParty(CharacterClass hero)
    {
        var save = ProfileHelper.CurrentProfile?.CurrentSave;
        if (save?.Party?.Members == null) return;

        // Check max party size
        if (save.Party.Members.Count >= MaxPartySize) return;

        // Already in party?
        if (save.Party.Members.Any(m => m.CharacterClass == hero)) return;

        // Find in roster
        var rosterEntry = save.Roster?.Members?.FirstOrDefault(m => m.CharacterClass == hero);
        if (rosterEntry == null) return;

        save.Party.Members.Add(new CharacterLevelPair(hero, rosterEntry.TotalXP));

        RefreshAll();
    }

    /// <summary>Removes a hero from the active party back to roster.</summary>
    public void RemoveFromParty(CharacterClass hero)
    {
        var save = ProfileHelper.CurrentProfile?.CurrentSave;
        if (save?.Party?.Members == null) return;

        // Don't allow removing last member
        if (save.Party.Members.Count <= 1) return;

        var idx = save.Party.Members.FindIndex(m => m.CharacterClass == hero);
        if (idx < 0) return;

        save.Party.Members.RemoveAt(idx);

        if (selectedHero == hero)
            selectedHero = save.Party.Members.Count > 0 ? save.Party.Members[0].CharacterClass : CharacterClass.None;

        RefreshAll();
    }

    // ===================== Detail Display =====================

    /// <summary>Refreshes the detail panel for the selected hero.</summary>
    private void RefreshDetail()
    {
        if (detailLabel == null) return;

        if (selectedHero == CharacterClass.None)
        {
            detailLabel.text = "Select a hero to view details";
            return;
        }

        var save = ProfileHelper.CurrentProfile?.CurrentSave;
        var member = save?.Party?.Members?.FirstOrDefault(m => m.CharacterClass == selectedHero);
        if (member == null)
        {
            member = save?.Roster?.Members?.FirstOrDefault(m => m.CharacterClass == selectedHero);
        }

        if (member == null)
        {
            detailLabel.text = $"{selectedHero} — No data";
            return;
        }

        var actorData = ActorLibrary.Get(selectedHero);
        var derived = ExperienceHelper.DeriveFromTotalXP(Mathf.Max(0, member.TotalXP));
        int level = Mathf.Max(1, derived.level);
        int currentXP = derived.currentXP;
        int xpNeeded = ExperienceHelper.NextLevel(level);

        // Use level-scaled stats instead of raw base stats
        var stats = actorData?.GetStats(level);

        var lines = new List<string>();
        lines.Add($"<b>{selectedHero}</b>  Level {level}");
        lines.Add($"XP: {currentXP} / {xpNeeded}  (Total: {member.TotalXP})");

        // XP bar text representation
        float xpPct = xpNeeded > 0 ? (float)currentXP / xpNeeded : 0f;
        int barFilled = Mathf.RoundToInt(xpPct * 20f);
        string bar = new string('█', barFilled) + new string('░', 20 - barFilled);
        lines.Add($"<color=#66AAFF>[{bar}] {xpPct * 100f:F0}%</color>");

        if (stats != null)
        {
            float hp = Formulas.Health(stats);
            lines.Add($"<color=#FF8888>HP {hp:F0}</color>");
            lines.Add($"STR {stats.Strength:F0}  VIT {stats.Vitality:F0}  AGI {stats.Agility:F0}  SPD {stats.Speed:F0}");
            lines.Add($"INT {stats.Intelligence:F0}  WIS {stats.Wisdom:F0}  LCK {stats.Luck:F0}  STA {stats.Stamina:F0}");
        }

        // Equipment bonuses
        var loadout = hub?.SharedLoadout?.Get(selectedHero);
        if (loadout != null)
        {
            var bonus = Formulas.ComputeEquipmentBonus(loadout);
            var bonusParts = new List<string>();
            if (bonus.Strength != 0) bonusParts.Add($"STR {bonus.Strength:+0;-0}");
            if (bonus.Vitality != 0) bonusParts.Add($"VIT {bonus.Vitality:+0;-0}");
            if (bonus.Agility != 0) bonusParts.Add($"AGI {bonus.Agility:+0;-0}");
            if (bonus.Intelligence != 0) bonusParts.Add($"INT {bonus.Intelligence:+0;-0}");
            if (bonus.Wisdom != 0) bonusParts.Add($"WIS {bonus.Wisdom:+0;-0}");
            if (bonus.Luck != 0) bonusParts.Add($"LCK {bonus.Luck:+0;-0}");
            if (bonusParts.Count > 0)
                lines.Add($"<color=#88CC88>Gear: {string.Join("  ", bonusParts)}</color>");

            // Equipment summary
            foreach (var slot in new[] { EquipmentSlot.Weapon, EquipmentSlot.Armor,
                                         EquipmentSlot.Relic1, EquipmentSlot.Relic2, EquipmentSlot.Relic3 })
            {
                var eq = loadout.GetEquipped(slot);
                var slotName = EquipmentSlotHelper.DisplayName(slot);
                if (eq != null)
                    lines.Add($"  {slotName}: <color=#{ColorUtility.ToHtmlStringRGB(HubItemRowFactory.RarityColor(eq.Rarity))}>{eq.DisplayName}</color>");
                else
                    lines.Add($"  {slotName}: <color=#666666>(empty)</color>");
            }
        }

        // Innate abilities
        if (actorData?.Abilities != null && actorData.Abilities.Count > 0)
        {
            lines.Add("");
            lines.Add("<color=#AACCFF>Abilities:</color>");
            foreach (var ab in actorData.Abilities)
            {
                string abilDesc = !string.IsNullOrEmpty(ab.Description) ? $" — {ab.Description}" : "";
                string category = ab.IsPassive ? " <color=#888888>(Passive)</color>" : "";
                lines.Add($"  • {ab.name}{category}{abilDesc}");
            }
        }

        // Trained skills
        var trainingData = save?.Training?.GetOrCreate(selectedHero);
        if (trainingData?.LearnedTrainingIds != null && trainingData.LearnedTrainingIds.Count > 0)
        {
            lines.Add("");
            lines.Add("<color=#88CC88>Trained Skills:</color>");
            foreach (var tid in trainingData.LearnedTrainingIds)
            {
                var td = TrainingLibrary.Get(tid);
                string tdDesc = td != null && !string.IsNullOrEmpty(td.Description) ? $" — {td.Description}" : "";
                lines.Add($"  • {td?.DisplayName ?? tid}{tdDesc}");
            }
        }

        // In-party action
        bool inParty = save.Party.Members.Any(m => m.CharacterClass == selectedHero);
        if (inParty && save.Party.Members.Count > 1)
            lines.Add("\n<i><color=#CC8888>Tap hero in party list to remove</color></i>");
        else if (!inParty)
            lines.Add("\n<i><color=#88CC88>Tap in roster to add to party</color></i>");

        detailLabel.text = string.Join("\n", lines);
    }

    // ===================== Helpers =====================

    /// <summary>Refreshes gold display.</summary>
    private void RefreshGoldDisplay()
    {
        if (goldLabel != null && hub?.SharedInventory != null)
            goldLabel.text = $"Gold: {hub.SharedInventory.Gold}";
    }

    /// <summary>Clears all children of a container.</summary>
    private void ClearContainer(RectTransform container)
    {
        if (container == null) return;
        for (int i = container.childCount - 1; i >= 0; i--)
            Destroy(container.GetChild(i).gameObject);
    }
}

}
