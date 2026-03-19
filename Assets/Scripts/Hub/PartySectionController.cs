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
/// One-stop character hub inspired by FF4/5/6 Pixel Remaster.
/// Manages roster, active party, and per-hero sub-tabs:
///   - Status: Stats, equipment summary, trained skills (read-only)
///   - Equip:  (placeholder, uses standalone Equip tab for now)
///   - Abilities: 5-slot ability bar assignment (assign/remove)
///
/// LAYOUT:
/// ```
/// ┌──────────┬──────────────────┬────────────────────────┐
/// │ ROSTER   │ ACTIVE PARTY     │ [Status][Equip][Abil]  │
/// │          │ ★ Paladin  Lv5   │                        │
/// │ • Monk   │ ★ Knight   Lv3   │ Right column content   │
/// │ • Cleric │                  │ changes by sub-tab     │
/// │          ├──────────────────│                        │
/// │          │ Detail / Header  │                        │
/// └──────────┴──────────────────┴────────────────────────┘
/// ```
///
/// RELATED FILES:
/// - HubManager.cs: Hub scene controller
/// - HeroLoadout.cs: Hero equipment/skills/ability bar
/// - AbilityLibrary.cs: Ability factories + GetBySkillId bridge
/// - ProfileHelper.cs: Save data
/// </summary>
public class PartySectionController : MonoBehaviour
{
    private HubManager hub;
    private CharacterClass selectedHero;

    private const int MaxPartySize = 4;

    // Sub-tab state
    private enum SubTab { Status, Equip, Abilities }
    private SubTab activeSubTab = SubTab.Status;

    // Runtime UI references
    private RectTransform rosterContainer;
    private RectTransform partyContainer;
    private RectTransform abilityBarContainer;
    private RectTransform abilityPickerContainer;
    private TextMeshProUGUI detailLabel;
    private TextMeshProUGUI goldLabel;

    // Sub-tab buttons
    private Button statusTabBtn;
    private Button equipTabBtn;
    private Button abilityTabBtn;

    // ScrollView parents for showing/hiding right-column areas
    private GameObject abilityBarScrollView;
    private GameObject abilityPickerScrollView;

    /// <summary>Initializes this controller.</summary>
    public void Initialize(HubManager hubManager)
    {
        hub = hubManager;
        ResolveUI();
        WireSubTabs();
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

        // Left column — scrollable roster
        rosterContainer = rt.Find("RosterScrollView/Viewport/" + GameObjectHelper.Hub.RosterList)?.GetComponent<RectTransform>();
        // Center — active party members
        partyContainer = rt.Find("PartyScrollView/Viewport/" + GameObjectHelper.Hub.PartyList)?.GetComponent<RectTransform>();
        // Right — ability bar slots
        abilityBarContainer = rt.Find("AbilityBarScrollView/Viewport/" + GameObjectHelper.Hub.AbilityBarList)?.GetComponent<RectTransform>();
        // Right — ability picker
        abilityPickerContainer = rt.Find("AbilityPickerScrollView/Viewport/" + GameObjectHelper.Hub.AbilityPicker)?.GetComponent<RectTransform>();
        // Header labels
        detailLabel = rt.Find(GameObjectHelper.Hub.DetailLabel)?.GetComponent<TextMeshProUGUI>();
        goldLabel = rt.Find(GameObjectHelper.Hub.GoldLabel)?.GetComponent<TextMeshProUGUI>();

        // Sub-tab buttons
        statusTabBtn = rt.Find(GameObjectHelper.Hub.PartyStatusTab)?.GetComponent<Button>();
        equipTabBtn = rt.Find(GameObjectHelper.Hub.PartyEquipTab)?.GetComponent<Button>();
        abilityTabBtn = rt.Find(GameObjectHelper.Hub.PartyAbilityTab)?.GetComponent<Button>();

        // ScrollView roots for visibility toggling
        abilityBarScrollView = rt.Find("AbilityBarScrollView")?.gameObject;
        abilityPickerScrollView = rt.Find("AbilityPickerScrollView")?.gameObject;
    }

    /// <summary>Wires sub-tab button click handlers.</summary>
    private void WireSubTabs()
    {
        if (statusTabBtn != null) statusTabBtn.onClick.AddListener(() => SetSubTab(SubTab.Status));
        if (equipTabBtn != null) equipTabBtn.onClick.AddListener(() => SetSubTab(SubTab.Equip));
        if (abilityTabBtn != null) abilityTabBtn.onClick.AddListener(() => SetSubTab(SubTab.Abilities));
    }

    /// <summary>Changes the active sub-tab and refreshes the right column.</summary>
    private void SetSubTab(SubTab tab)
    {
        activeSubTab = tab;
        RefreshRightColumn();
    }

    /// <summary>Refreshes all lists and detail display.</summary>
    private void RefreshAll()
    {
        RefreshPartyList();
        RefreshRosterList();
        RefreshRightColumn();
        RefreshGoldDisplay();
    }

    /// <summary>Refreshes the right column content based on active sub-tab.</summary>
    private void RefreshRightColumn()
    {
        // Show/hide right-column scroll views based on sub-tab
        bool showAbilityViews = (activeSubTab == SubTab.Abilities);
        if (abilityBarScrollView != null) abilityBarScrollView.SetActive(showAbilityViews);
        if (abilityPickerScrollView != null) abilityPickerScrollView.SetActive(showAbilityViews);

        switch (activeSubTab)
        {
            case SubTab.Status:
                RefreshStatusDetail();
                break;
            case SubTab.Equip:
                RefreshEquipDetail();
                break;
            case SubTab.Abilities:
                RefreshAbilityBar();
                RefreshAbilityPicker();
                RefreshAbilityDetail();
                break;
        }
    }

    // ===================== Party List =====================

    /// <summary>Refreshes the active party member list.</summary>
    private void RefreshPartyList()
    {
        ClearContainer(partyContainer);
        if (partyContainer == null) return;

        var party = ProfileHelper.CurrentProfile?.CurrentSave?.Party?.Members;
        if (party == null || party.Count == 0)
        {
            var emptyGo = HubItemRowFactory.Create(partyContainer);
            HubItemRowFactory.SetLabel(emptyGo, "No active party");
            HubItemRowFactory.SetSubLabel(emptyGo, "Add heroes from the roster on the left");
            var emptyBtn = emptyGo.GetComponent<Button>();
            if (emptyBtn != null) emptyBtn.interactable = false;
            return;
        }

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
        if (save?.Roster?.Members == null || save.Roster.Members.Count == 0)
        {
            var emptyGo = HubItemRowFactory.Create(rosterContainer);
            HubItemRowFactory.SetLabel(emptyGo, "Roster is empty");
            HubItemRowFactory.SetSubLabel(emptyGo, "Heroes join your roster through play");
            var emptyBtn = emptyGo.GetComponent<Button>();
            if (emptyBtn != null) emptyBtn.interactable = false;
            return;
        }

        var partySet = new HashSet<CharacterClass>(
            save.Party?.Members?.Select(m => m.CharacterClass) ?? Enumerable.Empty<CharacterClass>());

        int rowsAdded = 0;

        foreach (var member in save.Roster.Members)
        {
            var cc = member.CharacterClass;
            if (cc == CharacterClass.None) continue;
            if (partySet.Contains(cc)) continue;

            var derived = ExperienceHelper.DeriveFromTotalXP(Mathf.Max(0, member.TotalXP));
            int level = Mathf.Max(1, derived.level);
            rowsAdded++;

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

        if (rowsAdded == 0)
        {
            var emptyGo = HubItemRowFactory.Create(rosterContainer);
            HubItemRowFactory.SetLabel(emptyGo, "All heroes in party");
            HubItemRowFactory.SetSubLabel(emptyGo, "Remove a hero from the party to free a roster slot");
            var emptyBtn = emptyGo.GetComponent<Button>();
            if (emptyBtn != null) emptyBtn.interactable = false;
        }
    }

    /// <summary>Adds a hero from the roster to the active party.</summary>
    public void AddToParty(CharacterClass hero)
    {
        var save = ProfileHelper.CurrentProfile?.CurrentSave;
        if (save?.Party?.Members == null) return;
        if (save.Party.Members.Count >= MaxPartySize) return;
        if (save.Party.Members.Any(m => m.CharacterClass == hero)) return;

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
        if (save.Party.Members.Count <= 1) return;

        var idx = save.Party.Members.FindIndex(m => m.CharacterClass == hero);
        if (idx < 0) return;

        save.Party.Members.RemoveAt(idx);

        if (selectedHero == hero)
            selectedHero = save.Party.Members.Count > 0 ? save.Party.Members[0].CharacterClass : CharacterClass.None;

        RefreshAll();
    }

    // ===================== Sub-Tab: Status =====================

    /// <summary>Refreshes the Status sub-tab detail text.</summary>
    private void RefreshStatusDetail()
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
            member = save?.Roster?.Members?.FirstOrDefault(m => m.CharacterClass == selectedHero);

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
        var stats = actorData?.GetStats(level);

        var lines = new List<string>();
        lines.Add($"<b>{selectedHero}</b>  Level {level}");
        lines.Add($"XP: {currentXP} / {xpNeeded}  (Total: {member.TotalXP})");

        float xpPct = xpNeeded > 0 ? (float)currentXP / xpNeeded : 0f;
        int barFilled = Mathf.RoundToInt(xpPct * 20f);
        string bar = new string('\u2588', barFilled) + new string('\u2591', 20 - barFilled);
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
                lines.Add($"  \u2022 {ab.name}{category}{abilDesc}");
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
                lines.Add($"  \u2022 {td?.DisplayName ?? tid}{tdDesc}");
            }
        }

        // In-party action hint
        bool inParty = save.Party.Members.Any(m => m.CharacterClass == selectedHero);
        if (inParty && save.Party.Members.Count > 1)
            lines.Add("\n<i><color=#CC8888>Tap hero in party list to remove</color></i>");
        else if (!inParty)
            lines.Add("\n<i><color=#88CC88>Tap in roster to add to party</color></i>");

        detailLabel.text = string.Join("\n", lines);
    }

    // ===================== Sub-Tab: Equip =====================

    /// <summary>Refreshes the Equip sub-tab (placeholder — points to Equip tab).</summary>
    private void RefreshEquipDetail()
    {
        if (detailLabel == null) return;

        if (selectedHero == CharacterClass.None)
        {
            detailLabel.text = "Select a hero to manage equipment";
            return;
        }

        var loadout = hub?.SharedLoadout?.Get(selectedHero);
        var lines = new List<string>();
        lines.Add($"<b>{selectedHero}</b> — Equipment");
        lines.Add("");

        if (loadout != null)
        {
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

        lines.Add("");
        lines.Add("<i><color=#AACCFF>Use the Equip tab for full equipment management.</color></i>");

        detailLabel.text = string.Join("\n", lines);
    }

    // ===================== Sub-Tab: Abilities =====================

    /// <summary>Refreshes the detail label for Abilities sub-tab.</summary>
    private void RefreshAbilityDetail()
    {
        if (detailLabel == null) return;

        if (selectedHero == CharacterClass.None)
        {
            detailLabel.text = "Select a hero to manage abilities";
            return;
        }

        var loadout = hub?.SharedLoadout?.Get(selectedHero);
        int equipped = loadout?.EquippedAbilities?.Count ?? 0;
        detailLabel.text = $"<b>{selectedHero}</b> — Ability Bar ({equipped}/{HeroLoadout.MaxAbilitySlots})";
    }

    /// <summary>Refreshes the ability bar (5 equipped slots) for selected hero.</summary>
    private void RefreshAbilityBar()
    {
        ClearContainer(abilityBarContainer);
        if (abilityBarContainer == null || selectedHero == CharacterClass.None) return;

        var loadout = hub?.SharedLoadout?.Get(selectedHero);
        if (loadout == null) return;

        // Show up to MaxAbilitySlots rows
        for (int i = 0; i < HeroLoadout.MaxAbilitySlots; i++)
        {
            bool hasAbility = i < loadout.EquippedAbilities.Count && loadout.EquippedAbilities[i] != null;

            var go = HubItemRowFactory.Create(abilityBarContainer);

            if (hasAbility)
            {
                var ability = loadout.EquippedAbilities[i];
                string categoryTag = ability.IsPassive ? " <color=#888888>(P)</color>"
                    : ability.IsReactive ? " <color=#CC8888>(R)</color>"
                    : "";
                string costTag = ability.ManaCost > 0 ? $"  <color=#66AAFF>{ability.ManaCost} MP</color>" : "";

                HubItemRowFactory.SetLabel(go, $"{i + 1}. {ability.name}{categoryTag}");
                HubItemRowFactory.SetSubLabel(go, $"{ability.Description ?? ""}{costTag}");

                // Item-backed abilities show in a distinct color
                if (ability.IsItemAbility)
                    HubItemRowFactory.SetLabelColor(go, new Color(0.6f, 0.9f, 0.6f));

                // Click to remove
                var btn = go.GetComponent<Button>();
                if (btn != null)
                {
                    int capturedIndex = i;
                    btn.onClick.AddListener(() =>
                    {
                        loadout.RemoveAbility(capturedIndex);
                        RefreshAbilityBar();
                        RefreshAbilityPicker();
                        RefreshAbilityDetail();
                    });
                }
            }
            else
            {
                HubItemRowFactory.SetLabel(go, $"{i + 1}. <color=#555555>(Empty Slot)</color>");
                HubItemRowFactory.SetSubLabel(go, "<color=#444444>Select from available abilities below</color>");
                var btn = go.GetComponent<Button>();
                if (btn != null) btn.interactable = false;
            }
        }
    }

    /// <summary>Refreshes the ability picker showing all available abilities for assignment.</summary>
    private void RefreshAbilityPicker()
    {
        ClearContainer(abilityPickerContainer);
        if (abilityPickerContainer == null || selectedHero == CharacterClass.None) return;

        var loadout = hub?.SharedLoadout?.Get(selectedHero);
        if (loadout == null) return;

        bool barFull = loadout.EquippedAbilities.Count >= HeroLoadout.MaxAbilitySlots;

        // Collect names already equipped (for de-duplication)
        var equippedNames = new HashSet<string>(
            loadout.EquippedAbilities.Where(a => a != null).Select(a => a.name),
            System.StringComparer.OrdinalIgnoreCase);

        // Header
        var headerGo = HubItemRowFactory.Create(abilityPickerContainer);
        HubItemRowFactory.SetLabel(headerGo, "<color=#AACCFF>Available Abilities</color>");
        HubItemRowFactory.SetSubLabel(headerGo, "Tap to assign to the ability bar");
        var headerBtn = headerGo.GetComponent<Button>();
        if (headerBtn != null) headerBtn.interactable = false;

        int rowsAdded = 0;

        // Source 1: Innate abilities from ActorLibrary
        var actorData = ActorLibrary.Get(selectedHero);
        if (actorData?.Abilities != null)
        {
            foreach (var ab in actorData.Abilities)
            {
                if (equippedNames.Contains(ab.name)) continue;
                AddPickerRow(ab.name, ab.Description, ab.ManaCost, ab.IsPassive, ab.IsReactive,
                    false, barFull, loadout);
                rowsAdded++;
            }
        }

        // Source 2: Trained skills via AbilityLibrary.GetBySkillId bridge
        var save = ProfileHelper.CurrentProfile?.CurrentSave;
        var trainingData = save?.Training?.GetOrCreate(selectedHero);
        if (trainingData?.LearnedTrainingIds != null)
        {
            foreach (var tid in trainingData.LearnedTrainingIds)
            {
                var td = TrainingLibrary.Get(tid);
                if (td == null || string.IsNullOrEmpty(td.SkillId)) continue;

                var ability = AbilityLibrary.GetBySkillId(td.SkillId);
                if (ability == null) continue;
                if (equippedNames.Contains(ability.name)) continue;

                AddPickerRow(ability.name, ability.Description, ability.ManaCost,
                    ability.IsPassive, ability.IsReactive, false, barFull, loadout);
                rowsAdded++;
            }
        }

        // Source 3: Consumable items from inventory
        var inventory = hub?.SharedInventory;
        if (inventory != null)
        {
            foreach (var entry in inventory.All())
            {
                if (entry.Definition == null || !entry.Definition.IsConsumable) continue;
                if (entry.Count <= 0) continue;

                var itemAbility = AbilityLibrary.FromConsumable(entry.Definition);
                if (itemAbility == null) continue;
                if (equippedNames.Contains(itemAbility.name)) continue;

                AddPickerRow(itemAbility.name,
                    $"{itemAbility.Description}  (x{entry.Count})",
                    0, false, false, true, barFull, loadout,
                    entry.Definition);
                rowsAdded++;
            }
        }

        if (rowsAdded == 0)
        {
            var emptyGo = HubItemRowFactory.Create(abilityPickerContainer);
            HubItemRowFactory.SetLabel(emptyGo, "<color=#666666>No abilities available</color>");
            HubItemRowFactory.SetSubLabel(emptyGo, "Train skills or acquire items");
            var emptyBtn = emptyGo.GetComponent<Button>();
            if (emptyBtn != null) emptyBtn.interactable = false;
        }
    }

    /// <summary>Adds a row to the ability picker.</summary>
    private void AddPickerRow(
        string abilityName, string description, int manaCost,
        bool isPassive, bool isReactive, bool isItem,
        bool barFull, HeroLoadout loadout,
        ItemDefinition sourceItem = null)
    {
        var go = HubItemRowFactory.Create(abilityPickerContainer);

        string categoryTag = isPassive ? " <color=#888888>(Passive)</color>"
            : isReactive ? " <color=#CC8888>(Reactive)</color>"
            : "";
        string costTag = manaCost > 0 ? $"  <color=#66AAFF>{manaCost} MP</color>" : "";
        string itemTag = isItem ? " <color=#66CC66>[Item]</color>" : "";

        HubItemRowFactory.SetLabel(go, $"{abilityName}{categoryTag}{itemTag}");
        HubItemRowFactory.SetSubLabel(go, $"{description ?? ""}{costTag}");

        if (isItem)
            HubItemRowFactory.SetLabelColor(go, new Color(0.6f, 0.9f, 0.6f));

        var btn = go.GetComponent<Button>();
        if (btn != null)
        {
            btn.interactable = !barFull;
            btn.onClick.AddListener(() =>
            {
                Ability ability;
                if (isItem && sourceItem != null)
                    ability = AbilityLibrary.FromConsumable(sourceItem);
                else
                    ability = AbilityLibrary.Get(abilityName);

                if (ability != null && loadout.AddAbility(ability))
                {
                    RefreshAbilityBar();
                    RefreshAbilityPicker();
                    RefreshAbilityDetail();
                }
            });
        }
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
