using UnityEditor;
using UnityEngine;
using g = Scripts.Helpers.GameHelper;
using Scripts.Canvas;
using Scripts.Data.Actor;
using Scripts.Data.Items;
using Scripts.Data.Skills;
using Scripts.Effects;
using Scripts.Factories;
using Scripts.Helpers;
using Scripts.Hub;
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

public partial class DebugWindow
{
    /// <summary>
    /// Live demos for in-progress work — lets you exercise things from the editor instead of
    /// hunting for the right gameplay trigger. World-space UI render checks + the glyph economy.
    /// Add a button here that calls a DebugManager.Demo_* method.
    /// </summary>
    private void RenderDemos()
    {
        GUILayout.Space(8);
        GUILayout.Label("— Demos —", EditorStyles.boldLabel);

        // World-space UI render checks.
        RenderButtonRow(
            ("Show ActionTitle", () => g.DebugManager.Demo_ShowActionTitle()),
            ("Show CastConfirm", () => g.DebugManager.Demo_ShowCastConfirm()),
            ("Hide CastConfirm", () => g.DebugManager.Demo_HideCastConfirm()),
            ("Log Mana Line", () => g.DebugManager.Demo_LogManaBank())
        );

        // In-game HUD — spawn / hide the (debug) demo orb line and shield. NOTE: the LIVE orb line
        // and shield button auto-spawn in ManaPoolManager.Start; these Demo_Show buttons spawn an
        // EXTRA debug copy bound to the demo bank.
        RenderButtonRow(
            ("Demo Orb Line", () => g.DebugManager.Demo_ShowOrbLine()),
            ("Hide Demo Orb Line", () => g.DebugManager.Demo_HideOrbLine()),
            ("Toggle Any-Color", () => g.DebugManager.Demo_ToggleAnyColor()),
            ("Random Hero Abilities", () => g.DebugManager.Demo_RandomHeroAbilities())
        );

        // Give Mana — drop a bouncing orb of each color into the LIVE bank for spell testing.
        RenderButtonRow(
            ("Give (W)", () => g.DebugManager.Demo_GiveMana_White()),
            ("Give (U)", () => g.DebugManager.Demo_GiveMana_Blue()),
            ("Give (B)", () => g.DebugManager.Demo_GiveMana_Black()),
            ("Give (R)", () => g.DebugManager.Demo_GiveMana_Red())
        );

        // Buffs — US-016: apply a turn-unit debuff then advance enemy turns to watch it tick + expire.
        // US-013: Blinded halves the bearer's attack accuracy.
        RenderButtonRow(
            ("Slowed → Enemy", () => g.DebugManager.Demo_ApplySlowedToEnemy()),
            ("Blinded → Enemy", () => g.DebugManager.Demo_ApplyBlindedToEnemy()),
            ("Silenced → Hero", () => g.DebugManager.Demo_ApplySilencedToHero()),
            ("Trigger Enemy Attack", () => g.DebugManager.TriggerEnemyAttack())
        );

        // Mana economy — harvest orbs (V1: all Blue, sourced from the heroes you bring along)
        // and simulate vendor restock for the item slot.
        RenderButtonRow(
            ("+ Blue (1 hero)", () => g.DebugManager.Demo_HarvestBlue()),
            ("Harvest Party", () => g.DebugManager.Demo_HarvestParty()),
            ("Buy Potion (+1)", () => g.DebugManager.Demo_RefillPotion()),
            ("Clear Mana", () => g.DebugManager.Demo_ClearMana())
        );

        // Skill cooldowns — lock the selected hero's skills, then tick (or play through a turn) to
        // watch the bar slots fade + count down and reactivate at 0.
        RenderButtonRow(
            ("Lock Skill CDs", () => g.DebugManager.Demo_LockSkillCooldowns()),
            ("Tick CD (1 turn)", () => g.DebugManager.Demo_TickSkillCooldowns())
        );

        // HP carry-over (US-053) — wound the party, win, and the next battle spawns them still hurt;
        // Heal Party previews the gold-cost Alchemist full-heal (§29.3 #12, model A).
        RenderButtonRow(
            ("Wound Party 50%", () => g.DebugManager.Demo_WoundParty()),
            ("Heal Party Full", () => g.DebugManager.Demo_HealParty())
        );

        // Mana economy — the 6 ability slots, all on one row, sized to read like icons.
        // The shared RenderButtonRow hardcodes 25% width (only fits 4), so size per-slot here.
        // Labels regenerate from the actual ManaAbility data so cost icons match the recipe.
        var abilities = Scripts.Data.ManaAbilities.Slots;
        var castActions = new System.Action[]
        {
            () => g.DebugManager.Demo_Cast_Heal(),
            () => g.DebugManager.Demo_Cast_Fireball(),
            () => g.DebugManager.Demo_Cast_Frost(),
            () => g.DebugManager.Demo_Cast_Bolt(),
            () => g.DebugManager.Demo_Cast_Potion(),
            null, // slot 6: reserved
        };
        GUILayout.BeginHorizontal();
        float slotW = Mathf.Max(56f, (Screen.width - 24f) / 6f);
        var w = GUILayout.Width(slotW);
        var h = GUILayout.Height(36f);
        for (int i = 0; i < 6; i++)
        {
            var a = abilities[i];
            if (a == null)
            {
                GUI.enabled = false;
                GUILayout.Button("—\nReserved", w, h);
                GUI.enabled = true;
                continue;
            }
            string label = $"{a.Name}\n{Scripts.Data.ManaAbilities.CostIcons(a)}";
            if (GUILayout.Button(label, w, h)) castActions[i]?.Invoke();
        }
        GUILayout.EndHorizontal();
    }
}
