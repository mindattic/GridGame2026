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
/// EQUIPSECTIONCONTROLLER - Hub hero equipment management.
/// 
/// PURPOSE:
/// Allows the player to select a hero and equip/unequip items
/// from the shared inventory into the hero's equipment slots.
/// Shows stat previews so the player can compare gear.
/// 
/// LAYOUT:
/// ```
/// ┌──────────────────────────────────────────────┐
/// │  Gold: 1200                                  │
/// ├──────────┬───────────────────────────────────┤
/// │ Heroes   │  Equipment Slots                  │
/// │ ┌──────┐ │  ┌────────────────────────────┐  │
/// │ │Paladin│ │  │ Weapon: Iron Sword  [Unequip]│  │
/// │ │Archer │ │  │ Armor:  Chain Mail  [Unequip]│  │
/// │ │Cleric │ │  │ Helmet: (empty)     [Browse] │  │
/// │ └──────┘ │  │ Boots:  (empty)     [Browse] │  │
/// │          │  │ Ring:   (empty)     [Browse] │  │
/// │          │  │ Amulet: (empty)     [Browse] │  │
/// │          │  └────────────────────────────┘  │
/// │          │  Stats: STR 12 VIT 8 AGI 5 ...   │
/// │          ├───────────────────────────────────┤
/// │          │  Available Items for [Slot]       │
/// │          │  ┌─────────────────────────────┐ │
/// │          │  │ Steel Sword (+7 STR)  [Equip]│ │
/// │          │  │ War Hammer (+10 STR)  [Equip]│ │
/// │          │  └─────────────────────────────┘ │
/// └──────────┴───────────────────────────────────┘
/// ```
/// 
/// FLOW:
/// 1. Select hero from party list
/// 2. View currently equipped items per slot
/// 3. Click a slot to browse compatible inventory items
/// 4. Click an item to equip it (old item returns to inventory)
/// 5. Click [Unequip] to remove an equipped item
/// 
/// RELATED FILES:
/// - HubManager.cs: Owns SharedInventory and SharedLoadout
/// - HeroLoadout.cs: Per-hero equipment slots
/// - PartyLoadout.cs: All hero loadouts
/// - PlayerInventory.cs: Item ownership
/// - Formulas.cs: Equipment bonus calculations
/// </summary>
public class EquipSectionController : MonoBehaviour
{
    private HubManager hub;
    private CharacterClass selectedHero;
    private EquipmentSlot browsingSlot = EquipmentSlot.None;

    // Runtime UI containers
    private RectTransform heroListContainer;
    private RectTransform slotListContainer;
    private RectTransform itemPickerContainer;
    private TextMeshProUGUI goldLabel;
    private TextMeshProUGUI statsLabel;
    private TextMeshProUGUI detailLabel;

    private PlayerInventory Inventory => hub?.SharedInventory;
    private PartyLoadout Loadout => hub?.SharedLoadout;

    /// <summary>Initializes the controller.</summary>
    public void Initialize(HubManager hubManager)
    {
        hub = hubManager;
        ResolveUI();
        HubVendorFactory.Build(GetComponent<RectTransform>(), HubVendorFactory.EquipTheme);
    }

    /// <summary>Called when section becomes active.</summary>
    public void OnActivated()
    {
        browsingSlot = EquipmentSlot.None;
        RefreshHeroList();
        RefreshGoldDisplay();
    }

    /// <summary>Resolves UI references from scene hierarchy.</summary>
    private void ResolveUI()
    {
        var rt = GetComponent<RectTransform>();
        if (rt == null) return;

        // Left column — party members (inside HeroScrollView/Viewport)
        heroListContainer = rt.Find("HeroScrollView/Viewport/" + GameObjectHelper.Hub.HeroList)?.GetComponent<RectTransform>();
        // Right-top — 6 equipment slot rows (inside SlotScrollView/Viewport)
        slotListContainer = rt.Find("SlotScrollView/Viewport/" + GameObjectHelper.Hub.SlotList)?.GetComponent<RectTransform>();
        // Right-bottom — inventory items matching the browsed slot (inside ItemPickerScrollView/Viewport)
        itemPickerContainer = rt.Find("ItemPickerScrollView/Viewport/" + GameObjectHelper.Hub.ItemPicker)?.GetComponent<RectTransform>();
        // Top-right — current gold display
        goldLabel = rt.Find(GameObjectHelper.Hub.GoldLabel)?.GetComponent<TextMeshProUGUI>();
        // Right-middle — base stats + equipment bonuses + totals
        statsLabel = rt.Find(GameObjectHelper.Hub.StatsLabel)?.GetComponent<TextMeshProUGUI>();
        // Right header — "Equipment: {hero}"
        detailLabel = rt.Find(GameObjectHelper.Hub.DetailLabel)?.GetComponent<TextMeshProUGUI>();
    }

    // ===================== Hero Selection =====================

    /// <summary>Selects a hero and refreshes slot display.</summary>
    public void SelectHero(CharacterClass hero)
    {
        selectedHero = hero;
        browsingSlot = EquipmentSlot.None;
        RefreshSlotList();
        RefreshStatsDisplay();
        ClearContainer(itemPickerContainer);
    }

    /// <summary>Refreshes the hero selection list.</summary>
    private void RefreshHeroList()
    {
        ClearContainer(heroListContainer);
        if (heroListContainer == null) return;

        var party = ProfileHelper.CurrentProfile?.CurrentSave?.Party?.Members;
        if (party == null) return;

        foreach (var member in party)
        {
            var cc = member.CharacterClass;
            var go = HubItemRowFactory.Create(heroListContainer);
            var loadout = Loadout?.Get(cc);
            int equippedCount = loadout?.EquippedSlots?.Count ?? 0;

            // Hero portrait icon
            var heroData = ActorLibrary.Get(cc);
            if (heroData?.Portrait != null)
                HubItemRowFactory.SetIconSprite(go, heroData.Portrait);
            else
                HubItemRowFactory.SetIconColor(go, new Color(0.4f, 0.5f, 0.7f));

            HubItemRowFactory.SetLabel(go, cc.ToString());
            HubItemRowFactory.SetSubLabel(go, $"{equippedCount}/5 slots equipped");

            var btn = go.GetComponent<Button>();
            if (btn != null) btn.onClick.AddListener(() => SelectHero(cc));
        }

        // Auto-select first
        if (party.Count > 0) SelectHero(party[0].CharacterClass);
    }

    // ===================== Equipment Slots =====================

    private static readonly EquipmentSlot[] AllSlots = {
        EquipmentSlot.Weapon, EquipmentSlot.Armor,
        EquipmentSlot.Relic1, EquipmentSlot.Relic2, EquipmentSlot.Relic3
    };

    /// <summary>Refreshes the equipment slot list for the selected hero.</summary>
    private void RefreshSlotList()
    {
        ClearContainer(slotListContainer);
        if (slotListContainer == null || Loadout == null) return;

        var loadout = Loadout.Get(selectedHero);

        foreach (var slot in AllSlots)
        {
            var equipped = loadout.GetEquipped(slot);
            var go = HubItemRowFactory.Create(slotListContainer);
            var slotName = EquipmentSlotHelper.DisplayName(slot);

            if (equipped != null)
            {
                HubItemRowFactory.SetIcon(go, equipped);
                HubItemRowFactory.SetLabel(go, $"{slotName}: {equipped.DisplayName}");
                HubItemRowFactory.SetSubLabel(go, FormatItemStats(equipped));
                HubItemRowFactory.SetLabelColor(go, HubItemRowFactory.RarityColor(equipped.Rarity));

                var capturedSlot = slot;
                var btn = go.GetComponent<Button>();
                if (btn != null) btn.onClick.AddListener(() =>
                {
                    UnequipSlot(capturedSlot);
                });
            }
            else
            {
                HubItemRowFactory.SetLabel(go, $"{slotName}: (empty)");
                HubItemRowFactory.SetSubLabel(go, "Tap to browse");

                var capturedSlot = slot;
                var btn = go.GetComponent<Button>();
                if (btn != null) btn.onClick.AddListener(() => BrowseSlot(capturedSlot));
            }

            HubItemRowFactory.SetSelected(go, browsingSlot == slot);
        }

        if (detailLabel != null)
            detailLabel.text = $"Equipment: {selectedHero}";
    }

    // ===================== Item Picker =====================

    /// <summary>Opens the item picker for a specific slot.</summary>
    public void BrowseSlot(EquipmentSlot slot)
    {
        browsingSlot = slot;
        RefreshSlotList();
        RefreshItemPicker();
    }

    /// <summary>Refreshes the item picker showing inventory items for the browsed slot.</summary>
    private void RefreshItemPicker()
    {
        ClearContainer(itemPickerContainer);
        if (itemPickerContainer == null || Inventory == null) return;

        if (browsingSlot == EquipmentSlot.None) return;

        var loadout = Loadout?.Get(selectedHero);
        var currentlyEquipped = loadout?.GetEquipped(browsingSlot);

        // Collect IDs of items equipped in any relic slot (to avoid showing them in picker)
        var equippedRelicIds = new HashSet<string>();
        if (EquipmentSlotHelper.IsRelicSlot(browsingSlot) && loadout != null)
        {
            var relicSlots = new[] { EquipmentSlot.Relic1, EquipmentSlot.Relic2, EquipmentSlot.Relic3 };
            foreach (var rs in relicSlots)
            {
                var eq = loadout.GetEquipped(rs);
                if (eq != null) equippedRelicIds.Add(eq.Id);
            }
        }

        // Show items from inventory that match this slot
        foreach (var entry in Inventory.BySlot(browsingSlot))
        {
            // Skip the item that's already equipped in this slot
            if (currentlyEquipped != null && entry.Definition.Id == currentlyEquipped.Id) continue;
            // Skip relic items equipped in other relic slots
            if (equippedRelicIds.Contains(entry.Definition.Id)) continue;

            var item = entry.Definition;
            var go = HubItemRowFactory.Create(itemPickerContainer);

            HubItemRowFactory.SetIcon(go, item);
            HubItemRowFactory.SetLabel(go, $"{item.DisplayName} (x{entry.Count})");
            HubItemRowFactory.SetSubLabel(go, FormatStatComparison(item, currentlyEquipped));
            HubItemRowFactory.SetLabelColor(go, HubItemRowFactory.RarityColor(item.Rarity));

            var capturedItem = item;
            var btn = go.GetComponent<Button>();
            if (btn != null) btn.onClick.AddListener(() =>
            {
                EquipItem(capturedItem);
            });
        }

        // If currently equipped, show unequip option at the top
        if (currentlyEquipped != null)
        {
            var unequipGo = HubItemRowFactory.Create(itemPickerContainer);
            HubItemRowFactory.SetLabel(unequipGo, $"Unequip {currentlyEquipped.DisplayName}");
            HubItemRowFactory.SetSubLabel(unequipGo, "Return to inventory");
            // Move to first position
            unequipGo.transform.SetAsFirstSibling();

            var capturedSlot = browsingSlot;
            var btn = unequipGo.GetComponent<Button>();
            if (btn != null) btn.onClick.AddListener(() => UnequipSlot(capturedSlot));
        }
    }

    // ===================== Equip / Unequip =====================

    /// <summary>Equips an item from inventory into the selected hero's slot.</summary>
    public void EquipItem(ItemDefinition item)
    {
        if (item == null || Inventory == null || Loadout == null) return;
        if (item.Slot == EquipmentSlot.None) return;

        // Must own the item
        if (!Inventory.Contains(item.Id)) return;

        var loadout = Loadout.Get(selectedHero);

        // Remove from inventory
        Inventory.Remove(item.Id, 1);

        // Equip to the specific slot being browsed (important for relics)
        ItemDefinition previous;
        if (EquipmentSlotHelper.IsRelicSlot(browsingSlot) && EquipmentSlotHelper.IsRelicSlot(item.Slot))
            previous = loadout.EquipToSlot(item, browsingSlot);
        else
            previous = loadout.Equip(item);

        // Return old item to inventory
        if (previous != null)
            Inventory.Add(previous);

        RefreshSlotList();
        RefreshItemPicker();
        RefreshStatsDisplay();
        RefreshGoldDisplay();
    }

    /// <summary>Unequips the item in a slot and returns it to inventory.</summary>
    public void UnequipSlot(EquipmentSlot slot)
    {
        if (Loadout == null || Inventory == null) return;

        var loadout = Loadout.Get(selectedHero);
        var removed = loadout.Unequip(slot);

        if (removed != null)
            Inventory.Add(removed);

        browsingSlot = EquipmentSlot.None;
        RefreshSlotList();
        ClearContainer(itemPickerContainer);
        RefreshStatsDisplay();
    }

    // ===================== Stats Display =====================

    /// <summary>Shows base stats, equipment bonuses, and totals for the selected hero.</summary>
    private void RefreshStatsDisplay()
    {
        if (statsLabel == null || Loadout == null) return;

        // Get hero level and base stats
        var save = ProfileHelper.CurrentProfile?.CurrentSave;
        var member = save?.Party?.Members?.FirstOrDefault(m => m.CharacterClass == selectedHero);
        var actorData = ActorLibrary.Get(selectedHero);

        if (actorData == null || member == null)
        {
            statsLabel.text = "Select a hero";
            return;
        }

        var derived = ExperienceHelper.DeriveFromTotalXP(Mathf.Max(0, member.TotalXP));
        int level = Mathf.Max(1, derived.level);
        var baseStats = actorData.GetStats(level);
        var loadout = Loadout.Get(selectedHero);
        var bonus = Formulas.ComputeEquipmentBonus(loadout);

        var lines = new List<string>();
        lines.Add($"<b>{selectedHero}</b>  Lv.{level}");

        float baseHP = Formulas.Health(baseStats);
        lines.Add($"<color=#FF8888>HP {baseHP:F0}</color>");
        lines.Add("");

        // Stat table: Base + Gear = Total
        lines.Add("       <color=#888888>Base  Gear  Total</color>");
        lines.Add(FormatStatLine("STR", baseStats.Strength, bonus.Strength));
        lines.Add(FormatStatLine("VIT", baseStats.Vitality, bonus.Vitality));
        lines.Add(FormatStatLine("AGI", baseStats.Agility, bonus.Agility));
        lines.Add(FormatStatLine("SPD", baseStats.Speed, bonus.Stamina));
        lines.Add(FormatStatLine("INT", baseStats.Intelligence, bonus.Intelligence));
        lines.Add(FormatStatLine("WIS", baseStats.Wisdom, bonus.Wisdom));
        lines.Add(FormatStatLine("LCK", baseStats.Luck, bonus.Luck));

        statsLabel.text = string.Join("\n", lines);
    }

    /// <summary>Formats a single stat line: "STR  12  +4  16".</summary>
    private string FormatStatLine(string label, float baseVal, float gearVal)
    {
        float total = baseVal + gearVal;
        string gearStr = gearVal != 0
            ? $"<color=#88CC88>{gearVal:+0;-0}</color>"
            : "<color=#666666> 0</color>";
        return $"{label}    {baseVal,4:F0}  {gearStr}  {total,4:F0}";
    }

    // ===================== Display Helpers =====================

    /// <summary>Formats equipment stat modifiers as a compact string.</summary>
    private string FormatItemStats(ItemDefinition item)
    {
        if (item == null) return "";
        var parts = new List<string>();
        if (item.Strength != 0) parts.Add($"STR {item.Strength:+0;-0}");
        if (item.Vitality != 0) parts.Add($"VIT {item.Vitality:+0;-0}");
        if (item.Agility != 0) parts.Add($"AGI {item.Agility:+0;-0}");
        if (item.Stamina != 0) parts.Add($"STA {item.Stamina:+0;-0}");
        if (item.Intelligence != 0) parts.Add($"INT {item.Intelligence:+0;-0}");
        if (item.Wisdom != 0) parts.Add($"WIS {item.Wisdom:+0;-0}");
        if (item.Luck != 0) parts.Add($"LCK {item.Luck:+0;-0}");
        return string.Join("  ", parts);
    }

    /// <summary>Formats stat comparison between a candidate item and currently equipped item.</summary>
    private string FormatStatComparison(ItemDefinition candidate, ItemDefinition current)
    {
        if (candidate == null) return "";
        if (current == null) return FormatItemStats(candidate);

        var parts = new List<string>();
        FormatDiff(parts, "STR", candidate.Strength, current.Strength);
        FormatDiff(parts, "VIT", candidate.Vitality, current.Vitality);
        FormatDiff(parts, "AGI", candidate.Agility, current.Agility);
        FormatDiff(parts, "STA", candidate.Stamina, current.Stamina);
        FormatDiff(parts, "INT", candidate.Intelligence, current.Intelligence);
        FormatDiff(parts, "WIS", candidate.Wisdom, current.Wisdom);
        FormatDiff(parts, "LCK", candidate.Luck, current.Luck);
        return parts.Count > 0 ? string.Join("  ", parts) : "No stat change";
    }

    /// <summary>Adds a colored diff entry if the stat differs.</summary>
    private void FormatDiff(List<string> parts, string label, float candidateVal, float currentVal)
    {
        float diff = candidateVal - currentVal;
        if (candidateVal == 0 && currentVal == 0) return;
        if (diff > 0)
            parts.Add($"<color=#88FF88>{label} {diff:+0}</color>");
        else if (diff < 0)
            parts.Add($"<color=#FF6666>{label} {diff:+0;-0}</color>");
        else if (candidateVal != 0)
            parts.Add($"{label} {candidateVal:+0;-0}");
    }

    /// <summary>Refreshes gold display.</summary>
    private void RefreshGoldDisplay()
    {
        if (goldLabel != null && Inventory != null)
            goldLabel.text = $"Gold: {Inventory.Gold}";
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
