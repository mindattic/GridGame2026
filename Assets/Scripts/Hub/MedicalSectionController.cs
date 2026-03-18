using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
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
/// MEDICALSECTIONCONTROLLER - Hub healing/resurrection section.
/// 
/// PURPOSE:
/// Provides healing and resurrection services for heroes
/// in the Hub medical facility. Shows each hero's HP bar,
/// provides heal and heal-all options for gold.
/// 
/// LAYOUT:
/// ```
/// ┌────────────────────────────────────────────┐
/// │  Medical Tent              Gold: 1200      │
/// ├────────────────────────────────────────────┤
/// │  [Icon] Paladin   HP 120/150   [Heal 15g] │
/// │  [Icon] Archer    HP  45/80    [Heal 18g] │
/// │  [Icon] Cleric    HP  80/80    (Full HP)  │
/// │                                            │
/// │  [Heal All — 33g]                          │
/// │  [Use Healing Potion]                      │
/// └────────────────────────────────────────────┘
/// ```
/// 
/// HEAL COST:
/// 1 gold per 5 HP missing (minimum 1g)
/// 
/// RELATED FILES:
/// - HubManager.cs: Hub scene controller
/// - ActorLibrary.cs: Hero stat lookup
/// - PlayerInventory.cs: Gold transactions
/// </summary>
public class MedicalSectionController : MonoBehaviour
{
    private HubManager hub;

    // HP tracking per hero (between battles)
    private Dictionary<CharacterClass, float> heroCurrentHP = new Dictionary<CharacterClass, float>();

    // Runtime UI references
    private RectTransform heroListContainer;
    private TextMeshProUGUI goldLabel;
    private TextMeshProUGUI detailLabel;

    private PlayerInventory Inventory => hub?.SharedInventory;

    /// <summary>Initializes the controller.</summary>
    public void Initialize(HubManager hubManager)
    {
        hub = hubManager;
        ResolveUI();
        LoadHeroHP();
        HubVendorFactory.Build(GetComponent<RectTransform>(), HubVendorFactory.MedicalTheme);
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

        // Center — scrollable list of hero HP rows (inside HeroScrollView/Viewport)
        heroListContainer = rt.Find("HeroScrollView/Viewport/" + GameObjectHelper.Hub.HeroList)?.GetComponent<RectTransform>();
        // Top-right — current gold display
        goldLabel = rt.Find(GameObjectHelper.Hub.GoldLabel)?.GetComponent<TextMeshProUGUI>();
        // Top-center — contextual heading
        detailLabel = rt.Find(GameObjectHelper.Hub.DetailLabel)?.GetComponent<TextMeshProUGUI>();
    }

    /// <summary>Loads hero HP from party data. Assumes full HP if not tracked.</summary>
    private void LoadHeroHP()
    {
        heroCurrentHP.Clear();
        var party = ProfileHelper.CurrentProfile?.CurrentSave?.Party?.Members;
        if (party == null) return;

        foreach (var member in party)
        {
            float maxHP = GetMaxHP(member.CharacterClass, member.TotalXP);
            // Start at full HP on entering hub (typical RPG pattern)
            heroCurrentHP[member.CharacterClass] = maxHP;
        }
    }

    /// <summary>Refreshes the hero list with HP bars and heal buttons.</summary>
    private void RefreshHeroList()
    {
        ClearContainer(heroListContainer);
        if (heroListContainer == null) return;

        var party = ProfileHelper.CurrentProfile?.CurrentSave?.Party?.Members;
        if (party == null) return;

        int totalHealCost = 0;
        bool anyDamaged = false;

        foreach (var member in party)
        {
            var cc = member.CharacterClass;
            float maxHP = GetMaxHP(cc, member.TotalXP);
            float curHP = heroCurrentHP.TryGetValue(cc, out var hp) ? hp : maxHP;
            curHP = Mathf.Clamp(curHP, 0f, maxHP);
            heroCurrentHP[cc] = curHP;

            bool isDamaged = curHP < maxHP;
            int healCost = isDamaged ? ComputeHealCost(maxHP - curHP) : 0;
            if (isDamaged) { totalHealCost += healCost; anyDamaged = true; }

            float hpPct = maxHP > 0 ? curHP / maxHP : 1f;

            var go = HubItemRowFactory.Create(heroListContainer);

            // Portrait
            var heroData = ActorLibrary.Get(cc);
            if (heroData?.Portrait != null)
                HubItemRowFactory.SetIconSprite(go, heroData.Portrait);
            else
                HubItemRowFactory.SetIconColor(go, new Color(0.3f, 0.5f, 0.3f));

            string hpColor = hpPct > 0.5f ? "#88FF88" : hpPct > 0.25f ? "#FFCC44" : "#FF6666";
            HubItemRowFactory.SetLabel(go, $"{cc}  <color={hpColor}>HP {curHP:F0}/{maxHP:F0}</color>");

            if (isDamaged)
            {
                bool canAfford = Inventory != null && Inventory.Gold >= healCost;
                HubItemRowFactory.SetSubLabel(go, canAfford ? $"Heal for {healCost}g" : $"Need {healCost}g (insufficient gold)");

                var btn = go.GetComponent<Button>();
                if (btn != null)
                {
                    btn.interactable = canAfford;
                    var capturedCC = cc;
                    var capturedCost = healCost;
                    btn.onClick.AddListener(() => { HealHero(capturedCC, capturedCost); RefreshHeroList(); RefreshGoldDisplay(); });
                }
            }
            else
            {
                HubItemRowFactory.SetSubLabel(go, "<color=#88CC88>Full health</color>");
                var btn = go.GetComponent<Button>();
                if (btn != null) btn.interactable = false;
            }
        }

        // Heal All row
        if (anyDamaged)
        {
            var healAllGo = HubItemRowFactory.Create(heroListContainer);
            bool canAffordAll = Inventory != null && Inventory.Gold >= totalHealCost;
            HubItemRowFactory.SetLabel(healAllGo, $"Heal All — {totalHealCost}g");
            HubItemRowFactory.SetSubLabel(healAllGo, canAffordAll ? "Restore all heroes to full HP" : "Insufficient gold");
            HubItemRowFactory.SetLabelColor(healAllGo, new Color(0.5f, 1f, 0.6f));
            var healAllBtn = healAllGo.GetComponent<Button>();
            if (healAllBtn != null)
            {
                healAllBtn.interactable = canAffordAll;
                var cost = totalHealCost;
                healAllBtn.onClick.AddListener(() => { HealAll(cost); RefreshHeroList(); RefreshGoldDisplay(); });
            }
        }

        // Use healing potion row
        if (Inventory != null)
        {
            int potionCount = Inventory.CountOf("healing_potion_basic");
            if (potionCount > 0 && anyDamaged)
            {
                var potionGo = HubItemRowFactory.Create(heroListContainer);
                var potionDef = ItemLibrary.Get("healing_potion_basic");
                HubItemRowFactory.SetIcon(potionGo, potionDef);
                HubItemRowFactory.SetLabel(potionGo, $"Use Healing Potion (x{potionCount})");
                HubItemRowFactory.SetSubLabel(potionGo, $"Heals {potionDef?.BaseHealing ?? 50} HP to most damaged hero");
                var potionBtn = potionGo.GetComponent<Button>();
                if (potionBtn != null) potionBtn.onClick.AddListener(() => { UseHealingPotion(); RefreshHeroList(); RefreshGoldDisplay(); });
            }
        }

        if (detailLabel != null)
            detailLabel.text = anyDamaged ? "Heroes need healing" : "All heroes healthy";
    }

    // ===================== Actions =====================

    /// <summary>Heals a single hero to full HP for gold.</summary>
    public void HealHero(CharacterClass hero, int cost)
    {
        if (Inventory == null || Inventory.Gold < cost) return;
        Inventory.Gold -= cost;
        var party = ProfileHelper.CurrentProfile?.CurrentSave?.Party?.Members;
        var member = party?.FirstOrDefault(m => m.CharacterClass == hero);
        if (member == null) return;
        heroCurrentHP[hero] = GetMaxHP(hero, member.TotalXP);
    }

    /// <summary>Heals all heroes to full HP for gold.</summary>
    public void HealAll(int totalCost)
    {
        if (Inventory == null || Inventory.Gold < totalCost) return;
        Inventory.Gold -= totalCost;
        var party = ProfileHelper.CurrentProfile?.CurrentSave?.Party?.Members;
        if (party == null) return;
        foreach (var member in party)
        {
            heroCurrentHP[member.CharacterClass] = GetMaxHP(member.CharacterClass, member.TotalXP);
        }
    }

    /// <summary>Uses a healing potion on the most damaged hero.</summary>
    public void UseHealingPotion()
    {
        if (Inventory == null) return;
        if (!Inventory.Contains("healing_potion_basic")) return;

        var potionDef = ItemLibrary.Get("healing_potion_basic");
        int healing = potionDef?.BaseHealing ?? 50;

        // Find most damaged hero
        CharacterClass target = CharacterClass.None;
        float mostMissing = 0f;
        var party = ProfileHelper.CurrentProfile?.CurrentSave?.Party?.Members;
        if (party == null) return;

        foreach (var member in party)
        {
            float maxHP = GetMaxHP(member.CharacterClass, member.TotalXP);
            float curHP = heroCurrentHP.TryGetValue(member.CharacterClass, out var hp) ? hp : maxHP;
            float missing = maxHP - curHP;
            if (missing > mostMissing)
            {
                mostMissing = missing;
                target = member.CharacterClass;
            }
        }

        if (target == CharacterClass.None || mostMissing <= 0f) return;

        Inventory.Remove("healing_potion_basic", 1);
        var targetMember = party.FirstOrDefault(m => m.CharacterClass == target);
        float targetMax = GetMaxHP(target, targetMember?.TotalXP ?? 0);
        float current = heroCurrentHP.TryGetValue(target, out var cur) ? cur : targetMax;
        heroCurrentHP[target] = Mathf.Min(targetMax, current + healing);
    }

    // ===================== Helpers =====================

    /// <summary>Gets the max HP for a hero at their current level.</summary>
    private float GetMaxHP(CharacterClass hero, int totalXP = 0)
    {
        var data = ActorLibrary.Get(hero);
        if (data == null) return 100f;
        var derived = ExperienceHelper.DeriveFromTotalXP(Mathf.Max(0, totalXP));
        int level = Mathf.Max(1, derived.level);
        var stats = data.GetStats(level);
        return Mathf.Max(1f, Formulas.Health(stats));
    }

    /// <summary>Computes heal cost: 1g per 5 HP missing, minimum 1g.</summary>
    private int ComputeHealCost(float missing)
    {
        return Mathf.Max(1, Mathf.CeilToInt(missing / 5f));
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
