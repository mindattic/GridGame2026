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

        // Bestiary (US-054) — log seen/defeated progress, written on enemy spawn + death.
        RenderButtonRow(
            ("Log Bestiary", () => g.DebugManager.Demo_LogBestiary())
        );

        // Cast interrupt (US-024 stagger model) — report the selected hero's WIS-driven cast-stagger
        // resistance + cast-time added per hit (cancel when total added exceeds the cast time).
        RenderButtonRow(
            ("Cast-Stagger Info", () => g.DebugManager.Demo_RollCastInterrupt()),
            ("Clutch! (Force)", () => g.DebugManager.Demo_Clutch())
        );

        // Enemy charge/telegraph (US-026) — make a caster enemy load a spell on the timeline that
        // resolves into a magic hit at u=1 (enemies were melee-only before this).
        RenderButtonRow(
            ("Enemy Charge", () => g.DebugManager.Demo_EnemyCharge())
        );

        // ItemDefinition fields (US-040) — report how many items declare each new field (unblocks EPIC E).
        RenderButtonRow(
            ("Log ItemDef Fields", () => g.DebugManager.Demo_LogItemDefFields())
        );

        // Battle-start orbs (US-041) — re-run the equipped-robe grant (Mage Robe=2, Wizard Robe=3).
        RenderButtonRow(
            ("Battle-Start Orbs", () => g.DebugManager.Demo_ApplyBattleStartOrbs())
        );

        // Sleep Dart (US-042) — verify the item→spell wiring (OnUseSpellName='Sleep' resolves).
        RenderButtonRow(
            ("Verify Sleep Dart Route", () => g.DebugManager.Demo_VerifyItemSpellRoute())
        );

        // Resistances (US-043) — log the selected hero's effective per-type resistance (class × gear).
        RenderButtonRow(
            ("Log Resistances", () => g.DebugManager.Demo_LogResistance())
        );

        // Threat (US-080) — log per-hero damage-dealt; smart (high-INT) enemies hunt the highest.
        RenderButtonRow(
            ("Log Threat", () => g.DebugManager.Demo_LogThreat())
        );

        // Wild orb (US-031) — mint a Colorless "wild" orb (the crit reward); flashes every color.
        RenderButtonRow(
            ("Mint Wild Orb", () => g.DebugManager.Demo_MintWildOrb())
        );

        // Color affinity (US-030) — log what color each hero class mints on a pincer.
        RenderButtonRow(
            ("Log Color Affinities", () => g.DebugManager.Demo_LogColorAffinities())
        );

        // Colorless wildcard (US-033) — prove a Colorless "wild" orb pays any color on spend.
        RenderButtonRow(
            ("Test Wildcard Spend", () => g.DebugManager.Demo_TestColorlessWildcard())
        );

        // Coordinated retreat (US-081) — wound an enemy and log that it plans to flee the heroes.
        RenderButtonRow(
            ("Test Enemy Retreat", () => g.DebugManager.Demo_TestEnemyRetreat())
        );

        // Enemy planning (US-082) — log every living enemy's planned step (incl. ally-supporter positioning).
        RenderButtonRow(
            ("Log Enemy Plans", () => g.DebugManager.Demo_LogEnemyPlans())
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
