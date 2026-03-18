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
/// BATTLEPREPSECTIONCONTROLLER - Pre-battle preparation screen.
/// 
/// PURPOSE:
/// Final review screen before entering combat. Shows full party
/// lineup with stats, equipment, and skill loadouts. Allows
/// selecting which consumable items to bring into battle and
/// which learned skills to equip on each hero.
/// 
/// LAYOUT:
/// ```
/// ┌────────────────────────────────────────────────────────┐
/// │  Battle Preparation                    Gold: 1200      │
/// ├────────────────────────────────────────────────────────┤
/// │  PARTY LINEUP                                          │
/// │  ┌───────────────────────────────────────────────────┐ │
/// │  │ [Icon] Paladin  Lv.5   HP 150                     │ │
/// │  │        STR 16  VIT 12  Weapon: Iron Sword         │ │
/// │  │        Skills: Heal, Shield Bash                  │ │
/// │  ├───────────────────────────────────────────────────┤ │
/// │  │ [Icon] Archer   Lv.3   HP  80                     │ │
/// │  │        STR  5  AGI 14  Weapon: Hunter's Bow       │ │
/// │  │        Skills: Power Shot                         │ │
/// │  └───────────────────────────────────────────────────┘ │
/// │                                                        │
/// │  BATTLE ITEMS (tap to toggle)                          │
/// │  [△] Healing Potion x5  [✓]                           │
/// │  [△] Mana Potion x3     [✓]                           │
/// │                                                        │
/// │  PARTY POWER                                           │
/// │  Total STR: 21  Total VIT: 20  Avg Level: 4.0         │
/// │                                                        │
/// │  [  ENTER BATTLE  ]                                    │
/// └────────────────────────────────────────────────────────┘
/// ```
/// 
/// RELATED FILES:
/// - HubManager.cs: GoToBattle transition
/// - HeroLoadout.cs: Equipped items and skills
/// - PlayerInventory.cs: Available consumables
/// - ActorLibrary.cs: Hero stats
/// </summary>
public class BattlePrepSectionController : MonoBehaviour
{
    private HubManager hub;

    // Runtime UI references
    private RectTransform partyListContainer;
    private RectTransform itemListContainer;
    private TextMeshProUGUI goldLabel;
    private TextMeshProUGUI detailLabel;
    private Button battleButton;

    // Items selected for battle
    private HashSet<string> selectedBattleItems = new HashSet<string>();

    private PlayerInventory Inventory => hub?.SharedInventory;
    private PartyLoadout Loadout => hub?.SharedLoadout;

    /// <summary>Initializes the controller.</summary>
    public void Initialize(HubManager hubManager)
    {
        hub = hubManager;
        ResolveUI();
        HubVendorFactory.Build(GetComponent<RectTransform>(), new HubVendorFactory.VendorTheme
        {
            VendorName = "Battle Prep",
            PortraitClass = CharacterClass.Captain,
            BackgroundTop = new Color(0.25f, 0.15f, 0.10f, 0.90f),
            BackgroundBottom = new Color(0.15f, 0.08f, 0.05f, 0.95f),
            NameplateColor = new Color(1f, 0.75f, 0.4f),
        });
    }

    /// <summary>Called when section becomes active.</summary>
    public void OnActivated()
    {
        // Auto-select all consumables for battle
        selectedBattleItems.Clear();
        if (Inventory != null)
        {
            foreach (var entry in Inventory.ByType(ItemType.Consumable))
                selectedBattleItems.Add(entry.Definition.Id);
        }

        RefreshPartyLineup();
        RefreshBattleItems();
        RefreshPartyPower();
        RefreshGoldDisplay();
    }

    /// <summary>Resolves UI references from scene hierarchy.</summary>
    private void ResolveUI()
    {
        var rt = GetComponent<RectTransform>();
        if (rt == null) return;

        // Top section — full hero cards (inside PartyScrollView/Viewport)
        partyListContainer = rt.Find("PartyScrollView/Viewport/" + GameObjectHelper.Hub.PartyList)?.GetComponent<RectTransform>();
        // Middle section — consumable items (inside ItemScrollView/Viewport)
        itemListContainer = rt.Find("ItemScrollView/Viewport/" + GameObjectHelper.Hub.ItemList)?.GetComponent<RectTransform>();
        // Top-right — current gold display
        goldLabel = rt.Find(GameObjectHelper.Hub.GoldLabel)?.GetComponent<TextMeshProUGUI>();
        // Bottom — aggregate party power summary (total STR/VIT/AGI, avg level)
        detailLabel = rt.Find(GameObjectHelper.Hub.DetailLabel)?.GetComponent<TextMeshProUGUI>();
        // Bottom-center — triggers HubManager.GoToBattle()
        battleButton = rt.Find(GameObjectHelper.Hub.BattlePrepBattleButton)?.GetComponent<Button>();

        if (battleButton != null) battleButton.onClick.AddListener(() => hub?.GoToBattle());
    }

    // ===================== Party Lineup =====================

    /// <summary>Refreshes the party lineup with full hero cards.</summary>
    private void RefreshPartyLineup()
    {
        ClearContainer(partyListContainer);
        if (partyListContainer == null) return;

        var party = ProfileHelper.CurrentProfile?.CurrentSave?.Party?.Members;
        if (party == null || party.Count == 0)
        {
            var emptyGo = HubItemRowFactory.Create(partyListContainer);
            HubItemRowFactory.SetLabel(emptyGo, "No party members!");
            HubItemRowFactory.SetSubLabel(emptyGo, "Visit the Party section to add heroes");
            return;
        }

        // Header
        var headerGo = HubItemRowFactory.Create(partyListContainer);
        HubItemRowFactory.SetLabel(headerGo, $"Party Lineup ({party.Count}/4)");
        HubItemRowFactory.SetSubLabel(headerGo, "");
        HubItemRowFactory.SetLabelColor(headerGo, new Color(1f, 0.85f, 0.5f));
        var hdrBtn = headerGo.GetComponent<Button>();
        if (hdrBtn != null) hdrBtn.interactable = false;

        foreach (var member in party)
        {
            var cc = member.CharacterClass;
            if (cc == CharacterClass.None) continue;

            var actorData = ActorLibrary.Get(cc);
            var derived = ExperienceHelper.DeriveFromTotalXP(Mathf.Max(0, member.TotalXP));
            int level = Mathf.Max(1, derived.level);
            var stats = actorData?.GetStats(level);
            var loadout = Loadout?.Get(cc);
            var bonus = loadout != null ? Formulas.ComputeEquipmentBonus(loadout) : new EquipmentBonus();

            var go = HubItemRowFactory.Create(partyListContainer);

            // Portrait
            if (actorData?.Portrait != null)
                HubItemRowFactory.SetIconSprite(go, actorData.Portrait);
            else
                HubItemRowFactory.SetIconColor(go, new Color(0.5f, 0.35f, 0.25f));

            // Main label: name + level + HP
            float hp = stats != null ? Formulas.Health(stats) : 0f;
            HubItemRowFactory.SetLabel(go, $"{actorData?.CharacterName ?? cc.ToString()}  Lv.{level}  <color=#FF8888>HP {hp:F0}</color>");

            // Sub-label: key stats + weapon + skills
            var subParts = new List<string>();

            // Core stats (base + gear)
            if (stats != null)
            {
                float totalSTR = stats.Strength + bonus.Strength;
                float totalAGI = stats.Agility + bonus.Agility;
                float totalINT = stats.Intelligence + bonus.Intelligence;
                subParts.Add($"STR {totalSTR:F0}  AGI {totalAGI:F0}  INT {totalINT:F0}");
            }

            // Weapon
            var weapon = loadout?.GetEquipped(EquipmentSlot.Weapon);
            if (weapon != null)
                subParts.Add($"⚔ {weapon.DisplayName}");

            // Armor
            var armor = loadout?.GetEquipped(EquipmentSlot.Armor);
            if (armor != null)
                subParts.Add($"🛡 {armor.DisplayName}");

            // Skills
            var save = ProfileHelper.CurrentProfile?.CurrentSave;
            var trainingData = save?.Training?.GetOrCreate(cc);
            if (trainingData?.LearnedTrainingIds != null && trainingData.LearnedTrainingIds.Count > 0)
            {
                var skillNames = trainingData.LearnedTrainingIds
                    .Select(tid => TrainingLibrary.Get(tid)?.DisplayName ?? tid)
                    .Take(3);
                subParts.Add($"Skills: {string.Join(", ", skillNames)}");
            }

            // Innate abilities
            if (actorData?.Abilities != null && actorData.Abilities.Count > 0)
            {
                var abilityNames = actorData.Abilities
                    .Where(a => a.IsActive)
                    .Select(a => a.name)
                    .Take(3);
                if (abilityNames.Any())
                    subParts.Add($"Abilities: {string.Join(", ", abilityNames)}");
            }

            HubItemRowFactory.SetSubLabel(go, string.Join("  |  ", subParts));

            var btn = go.GetComponent<Button>();
            if (btn != null) btn.interactable = false;
        }
    }

    // ===================== Battle Items =====================

    /// <summary>Refreshes the consumable item selection for battle.</summary>
    private void RefreshBattleItems()
    {
        ClearContainer(itemListContainer);
        if (itemListContainer == null || Inventory == null) return;

        var consumables = Inventory.ByType(ItemType.Consumable).ToList();
        if (consumables.Count == 0) return;

        // Header
        var headerGo = HubItemRowFactory.Create(itemListContainer);
        HubItemRowFactory.SetLabel(headerGo, "Battle Items");
        HubItemRowFactory.SetSubLabel(headerGo, "Tap to toggle items for battle");
        HubItemRowFactory.SetLabelColor(headerGo, new Color(0.7f, 0.8f, 1f));
        var hdrBtn = headerGo.GetComponent<Button>();
        if (hdrBtn != null) hdrBtn.interactable = false;

        foreach (var entry in consumables)
        {
            var item = entry.Definition;
            bool selected = selectedBattleItems.Contains(item.Id);

            var go = HubItemRowFactory.Create(itemListContainer);
            HubItemRowFactory.SetIcon(go, item);

            string checkMark = selected ? "<color=#88FF88>✓</color>" : "<color=#666666>✗</color>";
            HubItemRowFactory.SetLabel(go, $"{checkMark}  {item.DisplayName} x{entry.Count}");

            string desc = "";
            if (item.BaseHealing > 0) desc = $"Heals {item.BaseHealing} HP";
            else desc = item.Description ?? "";
            HubItemRowFactory.SetSubLabel(go, desc);

            HubItemRowFactory.SetSelected(go, selected);

            var capturedId = item.Id;
            var btn = go.GetComponent<Button>();
            if (btn != null) btn.onClick.AddListener(() =>
            {
                if (selectedBattleItems.Contains(capturedId))
                    selectedBattleItems.Remove(capturedId);
                else
                    selectedBattleItems.Add(capturedId);
                RefreshBattleItems();
            });
        }
    }

    // ===================== Party Power Summary =====================

    /// <summary>Refreshes the aggregate party power display.</summary>
    private void RefreshPartyPower()
    {
        if (detailLabel == null) return;

        var party = ProfileHelper.CurrentProfile?.CurrentSave?.Party?.Members;
        if (party == null || party.Count == 0)
        {
            detailLabel.text = "No party members";
            return;
        }

        float totalSTR = 0, totalVIT = 0, totalAGI = 0, totalINT = 0, totalWIS = 0;
        float totalHP = 0;
        int totalLevel = 0;

        foreach (var member in party)
        {
            var actorData = ActorLibrary.Get(member.CharacterClass);
            if (actorData == null) continue;

            var derived = ExperienceHelper.DeriveFromTotalXP(Mathf.Max(0, member.TotalXP));
            int level = Mathf.Max(1, derived.level);
            totalLevel += level;

            var stats = actorData.GetStats(level);
            var loadout = Loadout?.Get(member.CharacterClass);
            var bonus = loadout != null ? Formulas.ComputeEquipmentBonus(loadout) : new EquipmentBonus();

            totalSTR += stats.Strength + bonus.Strength;
            totalVIT += stats.Vitality + bonus.Vitality;
            totalAGI += stats.Agility + bonus.Agility;
            totalINT += stats.Intelligence + bonus.Intelligence;
            totalWIS += stats.Wisdom + bonus.Wisdom;
            totalHP += Formulas.Health(stats);
        }

        float avgLevel = (float)totalLevel / party.Count;

        var lines = new List<string>();
        lines.Add("<b>Party Power</b>");
        lines.Add($"Avg Level: {avgLevel:F1}  |  Total HP: <color=#FF8888>{totalHP:F0}</color>");
        lines.Add($"STR {totalSTR:F0}  VIT {totalVIT:F0}  AGI {totalAGI:F0}  INT {totalINT:F0}  WIS {totalWIS:F0}");
        lines.Add("");

        // Consumables going into battle
        int battleItemCount = selectedBattleItems.Count;
        lines.Add($"Battle Items: {battleItemCount} type(s) selected");

        // Equipment coverage
        int totalSlotsFilled = 0;
        int totalSlotsAvailable = party.Count * 6;
        foreach (var member in party)
        {
            var loadout = Loadout?.Get(member.CharacterClass);
            if (loadout?.EquippedSlots != null)
                totalSlotsFilled += loadout.EquippedSlots.Count;
        }
        float coveragePct = totalSlotsAvailable > 0 ? (float)totalSlotsFilled / totalSlotsAvailable * 100f : 0f;
        lines.Add($"Equipment: {totalSlotsFilled}/{totalSlotsAvailable} slots filled ({coveragePct:F0}%)");

        detailLabel.text = string.Join("\n", lines);
    }

    // ===================== Helpers =====================

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
