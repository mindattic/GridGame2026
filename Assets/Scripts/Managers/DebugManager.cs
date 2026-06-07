using Scripts.Helpers;
using Scripts.Libraries;
using Scripts.Managers;
using Scripts.Models;
using Scripts.Sequences;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;
using g = Scripts.Helpers.GameHelper;
using scene = Scripts.Helpers.SceneHelper;
using Scripts.Canvas;
using Scripts.Data.Actor;
using Scripts.Data.Items;
using Scripts.Data.Skills;
using Scripts.Effects;
using Scripts.Factories;
using Scripts.Hub;
using Scripts.Instances;
using Scripts.Instances.Actor;
using Scripts.Instances.Board;
using Scripts.Instances.SynergyLine;
using Scripts.Inventory;
using Scripts.Models.Actor;
using Scripts.Overworld;
using Scripts.Serialization;
using Scripts.Utilities;

namespace Scripts.Managers
{
    public class DebugManager : MonoBehaviour
    {
        //DEBUG: No gaurentee these values exist, define and use inside tests...
        ActorInstance hero1 => g.Actors.Heroes.Skip(0).Take(1).First();
        ActorInstance hero2 => g.Actors.Heroes.Skip(1).Take(1).First();
        ActorInstance hero3 => g.Actors.Heroes.Skip(2).Take(1).First();
        ActorInstance hero4 => g.Actors.Heroes.Skip(3).Take(1).First();

        //Fields — resolved at runtime if wired by DebugManager.Initialize / builder.
        private TMP_Dropdown Dropdown;
        public bool showActorNameTag = false;
        public bool showActorFrame = false;
        public bool showTutorials = false;
        public bool isHeroInvincible = false;
        public bool isEnemyInvincible = false;
        public bool isTimerInfinite = false;
        public bool isEnemyStunned = false;

        #region Demos (driven by DebugWindow buttons)

        // Shared demo mana line (12-orb cap) — lets the harvest→line→cast loop be exercised from
        // the editor before it's wired into the live battle resource system.
        private readonly ManaBank demoManaBank = new ManaBank();

        // Spawned-at-runtime HUD pieces (Row 14 orb line + shield button). Held so the Hide demo
        // buttons can destroy them.
        private ManaOrbLine spawnedOrbLine;
        private ShieldButton spawnedShield;

        /// <summary>Demo: spawn the Row-14 mana orb line under the main Canvas, bound to the demo bank.</summary>
        public void Demo_ShowOrbLine()
        {
            var canvas = GameObject.Find("Canvas");
            if (canvas == null) { Debug.LogWarning("[Demo] No Canvas found — enter a scene with one (e.g., Game)."); return; }
            if (spawnedOrbLine != null) { Debug.Log("[Demo] Orb line already spawned."); return; }
            spawnedOrbLine = ManaOrbLineFactory.Create(canvas.transform, demoManaBank);
            Debug.Log("[Demo] Mana orb line spawned (Row 14). Harvest to fill it.");
        }

        /// <summary>Demo: destroy the spawned orb line.</summary>
        public void Demo_HideOrbLine()
        {
            if (spawnedOrbLine == null) return;
            Destroy(spawnedOrbLine.gameObject);
            spawnedOrbLine = null;
            Debug.Log("[Demo] Mana orb line removed.");
        }

        /// <summary>Demo: spawn the Shield button (bottom-right of timeline). Click logs a stub action.</summary>
        public void Demo_ShowShield()
        {
            var canvas = GameObject.Find("Canvas");
            if (canvas == null) { Debug.LogWarning("[Demo] No Canvas found."); return; }
            if (spawnedShield != null) { Debug.Log("[Demo] Shield already spawned."); return; }
            spawnedShield = ShieldButtonFactory.Create(canvas.transform);
            spawnedShield.OnPressed += () => Debug.Log("[Demo] Shield pressed (behavior TBD).");
            Debug.Log("[Demo] Shield button spawned (bottom-right of timeline).");
        }

        /// <summary>Demo: destroy the spawned shield button.</summary>
        public void Demo_HideShield()
        {
            if (spawnedShield == null) return;
            Destroy(spawnedShield.gameObject);
            spawnedShield = null;
            Debug.Log("[Demo] Shield button removed.");
        }

        /// <summary>Demo: show the world-space ActionTitle banner so you can see it render + place.</summary>
        public void Demo_ShowActionTitle()
        {
            if (g.ActionTitle == null) { Debug.LogWarning("[Demo] ActionTitle is null."); return; }
            g.ActionTitle.Show("DEMO: Casting Fireball");
            Debug.Log("[Demo] ActionTitle shown (world-space top band).");
        }

        /// <summary>Demo: pop the world-space cast-confirm modal.</summary>
        public void Demo_ShowCastConfirm()
        {
            var m = AbilityCastConfirm.instance;
            if (m == null) { Debug.LogWarning("[Demo] AbilityCastConfirm.instance is null."); return; }
            m.SetTitle("Cast Meteor Slam?");
            m.SetDescription("Fire + Physical + Physical — a devastating combo strike.");
            m.Toggle(true);
        }

        /// <summary>Demo: hide the cast-confirm modal.</summary>
        public void Demo_HideCastConfirm() => AbilityCastConfirm.instance?.FadeOut();

        /// <summary>Demo: harvest one Blue orb (as if one hero contributed via a pincer), then log the line.</summary>
        public void Demo_HarvestBlue()
        {
            int added = demoManaBank.Add(ManaType.Blue);
            if (added == 0) Debug.LogWarning("[Demo] Mana line is full (12). Cast something first.");
            Demo_LogManaBank();
        }

        /// <summary>Demo: harvest one Blue orb per hero in the party (the heroes-as-source rule). Falls back to 3 outside play mode.</summary>
        public void Demo_HarvestParty()
        {
            int heroes = 0;
            try { heroes = System.Linq.Enumerable.Count(g.Actors.Heroes); } catch { /* not in battle */ }
            if (heroes <= 0) heroes = 3;
            int added = demoManaBank.Add(ManaType.Blue, heroes);
            Debug.Log($"[Demo] Harvested {added} Blue orb(s) from {heroes} hero(es)" + (added < heroes ? " (line hit cap)." : "."));
            Demo_LogManaBank();
        }

        /// <summary>Demo: log the current mana line as an ordered row of orbs.</summary>
        public void Demo_LogManaBank()
        {
            var sb = new System.Text.StringBuilder($"[Demo] Mana line {demoManaBank.Total}/{demoManaBank.Capacity}: ");
            if (demoManaBank.Total == 0) sb.Append("(empty)");
            else foreach (var orb in demoManaBank.Orbs) sb.Append($"[{orb}]");
            Debug.Log(sb.ToString());
        }

        /// <summary>Demo: use one of the 6 abilities. Spells pay mana; items consume a charge.</summary>
        public void Demo_UseAbility(ManaAbility ability)
        {
            if (ability == null) { Debug.LogWarning("[Demo] Slot is empty (reserved)."); return; }

            if (ability.Kind == AbilityKind.Item)
            {
                if (ability.TryConsumeCharge())
                    Debug.Log($"[Demo] Item '{ability.Name}' used ({ability.Charges}/{ability.MaxStackSize} left).");
                else
                    Debug.LogWarning($"[Demo] Item '{ability.Name}' is empty (0/{ability.MaxStackSize}). Buy at vendor / craft at alchemist.");
                return;
            }

            // Spell — pays mana orbs.
            if (demoManaBank.Spend(ability.Cost))
                Debug.Log($"[Demo] Spell '{ability.Name}' cast — spent {ability.Cost.Describe()}.");
            else
                Debug.LogWarning($"[Demo] Can't afford Spell '{ability.Name}' ({ability.Cost.Describe()}). Harvest more orbs.");
            Demo_LogManaBank();
        }

        public void Demo_Cast_Heal()     => Demo_UseAbility(Scripts.Data.ManaAbilities.Heal);
        public void Demo_Cast_Fireball() => Demo_UseAbility(Scripts.Data.ManaAbilities.Fireball);
        public void Demo_Cast_Frost()    => Demo_UseAbility(Scripts.Data.ManaAbilities.Frost);
        public void Demo_Cast_Bolt()     => Demo_UseAbility(Scripts.Data.ManaAbilities.Bolt);
        public void Demo_Cast_Potion()   => Demo_UseAbility(Scripts.Data.ManaAbilities.Potion);

        /// <summary>Demo: simulate buying a potion at the vendor — refill one charge.</summary>
        public void Demo_RefillPotion()
        {
            var p = Scripts.Data.ManaAbilities.Potion;
            p.Refill(1);
            Debug.Log($"[Demo] Bought 1 Potion — now {p.Charges}/{p.MaxStackSize}.");
        }

        private static readonly ManaAbility[] DemoSkills =
            { Scripts.Data.ManaAbilities.Steal, Scripts.Data.ManaAbilities.Mug, Scripts.Data.ManaAbilities.Teleport };

        /// <summary>Demo: lock the selected hero's Skill abilities so the bar shows the fade + countdown.</summary>
        public void Demo_LockSkillCooldowns()
        {
            var hero = Scripts.Helpers.GameHelper.Actors.SelectedActor;
            if (hero == null) { Debug.LogWarning("[Demo] Select a hero first — its skills will be put on cooldown."); return; }
            foreach (var s in DemoSkills)
            {
                Scripts.Managers.SkillCooldownManager.Begin(hero, s);
                Debug.Log($"[Demo] {hero.name}: {s.Name} locked for {Scripts.Managers.SkillCooldownManager.GetRemaining(hero, s)} turn(s).");
            }
        }

        /// <summary>Demo: tick skill cooldowns by one turn-cycle (mimics a hero-window start) and log.</summary>
        public void Demo_TickSkillCooldowns()
        {
            Scripts.Managers.SkillCooldownManager.TickAll();
            var hero = Scripts.Helpers.GameHelper.Actors.SelectedActor;
            if (hero == null) { Debug.Log("[Demo] Ticked skill cooldowns (no hero selected to report)."); return; }
            foreach (var s in DemoSkills)
                Debug.Log($"[Demo] {hero.name}: {s.Name} cooldown = {Scripts.Managers.SkillCooldownManager.GetRemaining(hero, s)}.");
        }

        /// <summary>Demo (US-053): wound every living hero to ~50% HP — set up a carry-over test
        /// (win this battle, then the next one should spawn them still wounded).</summary>
        public void Demo_WoundParty()
        {
            int n = 0;
            foreach (var a in g.Actors.All)
            {
                if (a == null || !a.IsHero || !a.IsPlaying || a.Stats == null) continue;
                a.Stats.HP = Mathf.Max(1f, a.Stats.MaxHP * 0.5f);
                n++;
                Debug.Log($"[Demo] {a.name} wounded to {a.Stats.HP:0}/{a.Stats.MaxHP:0}.");
            }
            if (n == 0) Debug.LogWarning("[Demo] No living heroes to wound (start a battle first).");
        }

        /// <summary>Demo (US-053, §29.3 #12 model A): full-heal the party — preview of the gold-cost
        /// Alchemist heal service. Restores every living hero to MaxHP.</summary>
        public void Demo_HealParty()
        {
            int n = 0;
            foreach (var a in g.Actors.All)
            {
                if (a == null || !a.IsHero || !a.IsPlaying || a.Stats == null) continue;
                a.Stats.HP = a.Stats.MaxHP;
                n++;
            }
            Debug.Log(n > 0 ? $"[Demo] Healed {n} hero(es) to full." : "[Demo] No living heroes to heal.");
        }

        /// <summary>Demo (US-054): log Bestiary progress — seen vs defeated enemy classes
        /// (written on enemy spawn + death).</summary>
        public void Demo_LogBestiary()
        {
            var bestiary = ProfileHelper.CurrentProfile?.CurrentSave?.Bestiary;
            if (bestiary?.Entries == null || bestiary.Entries.Count == 0)
            {
                Debug.Log("[Demo] Bestiary empty — spawn/defeat some enemies first.");
                return;
            }
            int seen = 0, defeated = 0;
            foreach (var e in bestiary.Entries)
            {
                if (e == null) continue;
                if (e.Seen) seen++;
                if (e.Defeated) defeated++;
                Debug.Log($"[Demo] {e.CharacterClass}: seen={e.Seen} defeated={e.Defeated} x{e.TimesDefeated}");
            }
            Debug.Log($"[Demo] Bestiary: {seen} seen, {defeated} defeated ({bestiary.Entries.Count} classes recorded).");
        }

        /// <summary>Demo (US-024 stagger model): report the selected hero's cast-stagger resistance —
        /// WIS-driven resist chance + cast-time added per landed hit. A cast cancels once the total
        /// added delay exceeds its cast time.</summary>
        public void Demo_RollCastInterrupt()
        {
            var hero = Scripts.Helpers.GameHelper.Actors.SelectedActor;
            if (hero == null) { Debug.LogWarning("[Demo] Select a hero first."); return; }
            float wis = hero.Stats?.Wisdom ?? 0f;
            float lck = hero.Stats?.Luck ?? 0f;
            float clutch = Mathf.Clamp(lck * Scripts.Services.CastInterruptResolver.ClutchChancePerLuck,
                0f, Scripts.Services.CastInterruptResolver.ClutchMaxChance);
            float resist = Mathf.Clamp(wis * Scripts.Services.CastInterruptResolver.WisdomResistPerPoint,
                0f, Scripts.Services.CastInterruptResolver.MaxResistChance);
            float delayVsStr10 = Scripts.Services.CastInterruptResolver.BaseInterruptDelay
                * (1f + 10f / Scripts.Services.CastInterruptResolver.StrengthScale)
                / (1f + wis / Scripts.Services.CastInterruptResolver.WisdomDelayScale);
            Debug.Log($"[Demo] Cast interrupt for {hero.name} (LCK {lck:0}, WIS {wis:0}): {clutch:P0} Clutch (LCK miracle save) → else {resist:P0} WIS shrug → else ~{delayVsStr10:0.00}s cast-time added per hit (vs STR 10). Cast cancels once total added ≥ its cast time.");
        }

        /// <summary>Demo (US-025): fire the Clutch! miracle save. If the selected hero has a spell
        /// in flight, snaps it to the trigger and resolves it on the spot with the flash/SFX/text;
        /// otherwise just plays the juice so the effect is visible. Cast a spell first to see the snap.</summary>
        public void Demo_Clutch()
        {
            var hero = Scripts.Helpers.GameHelper.Actors.SelectedActor;
            var icon = g.TimelineBar?.GetSpellIconFor(hero);
            if (icon != null) icon.Pause();
            else Debug.Log("[Demo] No in-flight cast for the selected hero — playing the Clutch juice only (cast a spell, then re-run to see the snap-to-resolve).");
            g.SequenceManager.Add(new Scripts.Sequences.ClutchSequence(icon, hero));
            g.SequenceManager.Execute();
        }

        /// <summary>Demo (US-026): force a caster enemy to telegraph a charge spell at the nearest hero.
        /// Uses the selected enemy if one is selected, else the first Magic-tagged enemy on the board.
        /// Watch the colored cast-icon load on the timeline, then resolve into a magic hit at u=1.</summary>
        public void Demo_EnemyCharge()
        {
            var enemy = g.Actors.SelectedActor;
            if (enemy == null || !enemy.IsEnemy)
                enemy = g.Actors.Enemies?.FirstOrDefault(e => e != null && e.IsPlaying && EnemyChargeCatalog.IsCaster(e))
                     ?? g.Actors.Enemies?.FirstOrDefault(e => e != null && e.IsPlaying);
            if (enemy == null) { Debug.LogWarning("[Demo] No enemy on the board — start a battle first."); return; }

            // Real casters (IceMauler etc.) get their affinity spell; for any other enemy, force a
            // demo Fireball so the charge mechanic is visible on every stage.
            var ability = EnemyChargeCatalog.For(enemy) ?? new Scripts.Instances.Ability
            {
                name = "Fireball",
                type = AbilityType.TargetOpponent,
                Effect = AbilityEffect.Fireball,
                CastTimeSeconds = EnemyChargeCatalog.DefaultChargeCastSeconds,
                TargetingMode = AbilityTargetingMode.AnyActor
            };

            var target = g.Actors.Heroes?
                .Where(h => h != null && h.IsPlaying)
                .OrderBy(h => Mathf.Abs(enemy.location.x - h.location.x) + Mathf.Abs(enemy.location.y - h.location.y))
                .FirstOrDefault();
            if (target == null) { Debug.LogWarning("[Demo] No living hero to target."); return; }

            Debug.Log($"[Demo] {enemy.characterClass} charging {ability.name} ({ability.CastTimeSeconds:0.#}s base) at {target.characterClass}. Color (US-027) = {EnemyChargeCatalog.ColorFor(enemy)}.");
            g.SequenceManager.Add(new Scripts.Sequences.EnemyChargeSequence(enemy, target, ability));
            g.SequenceManager.Execute();
        }

        /// <summary>Demo (US-027): interrupt a charging enemy. Finds an enemy with an in-flight charge
        /// (run "Enemy Charge" first) and hammers it with simulated hero hits until the cast-stagger
        /// cancels it — which mints one charge-color orb to the team bank.</summary>
        public void Demo_InterruptEnemyCharge()
        {
            var enemy = g.Actors.Enemies?.FirstOrDefault(e => e != null && e.IsPlaying && g.TimelineBar?.GetSpellIconFor(e) != null);
            if (enemy == null) { Debug.LogWarning("[Demo] No charging enemy on the board — run 'Enemy Charge' first."); return; }
            var hero = g.Actors.Heroes?.FirstOrDefault(h => h != null && h.IsPlaying);

            var color = EnemyChargeCatalog.ColorFor(enemy);
            int before = g.ManaPoolManager?.Bank?.Count(color) ?? 0;
            int hits = 0;
            while (g.TimelineBar?.GetSpellIconFor(enemy) != null && hits < 25)
            {
                g.TimelineBar.InterruptCastsByOwner(enemy, hero);
                hits++;
            }
            Debug.Log($"[Demo] Staggered {enemy.characterClass}'s charge across {hits} hit(s) → cancelled; minting a {color} orb (it bounces to the bank). Before={before} {color}.");
        }

        /// <summary>Demo (US-028): Quicken — slide an actor's timeline icon forward toward the trigger
        /// (inverse of pushback). Uses the selected actor, else the first enemy. Watch its icon jump
        /// ahead on the timeline; if it now reaches the trigger first it overtakes others' turns.</summary>
        public void Demo_Quicken()
        {
            var actor = g.Actors.SelectedActor;
            if (actor == null || !actor.IsPlaying)
                actor = g.Actors.Enemies?.FirstOrDefault(e => e != null && e.IsPlaying);
            if (actor == null) { Debug.LogWarning("[Demo] No actor to Quicken — start a battle first."); return; }
            var icon = g.TimelineBar?.GetIconFor(actor);
            if (icon == null) { Debug.LogWarning($"[Demo] {actor.characterClass} has no timeline icon to hasten."); return; }
            float before = icon.GetU();
            float amount = Scripts.Data.SpellLibrary.Quicken.HastenU;
            g.TimelineBar.HastenIcon(actor, amount);
            Debug.Log($"[Demo] Quickened {actor.characterClass} by {amount:0.##}u: u {before:0.00} → {icon.GetU():0.00} ({icon.GetSecondsRemaining():0.0}s to its turn now).");
        }

        /// <summary>Demo (Multi-tile actors): spawn the 2×2 Cyclops boss into the current battle. It
        /// occupies a free 2×2 rectangle, is an immovable wall to hero slides, is pincered by flanking
        /// its width, and shoves heroes when it moves on its turn.</summary>
        public void Demo_SpawnBoss()
        {
            var boss = g.StageManager.AddEnemy(CharacterClass.Cyclops00);
            if (boss == null) { Debug.LogWarning("[Demo] Couldn't spawn the Cyclops (no free 2×2 space on the board?)."); return; }
            Debug.Log($"[Demo] Spawned 2×2 {boss.characterClass} at anchor {boss.location} (footprint {boss.Footprint.x}×{boss.Footprint.y}). Flank its 2-tile width with two heroes to pincer it; drag into it and the slide stops at its edge; on its turn it shoves heroes aside.");
        }

        /// <summary>Demo (US-083): wound the boss-scripted enemy past its phase-2 HP threshold and fire
        /// the phase transition (the Cyclops ENRAGE: banner + Quicken). Run "Spawn 2×2 Boss" first.</summary>
        public void Demo_TriggerBossEnrage()
        {
            var boss = g.Actors.Enemies?.FirstOrDefault(e => e != null && e.IsPlaying && Scripts.Data.Actor.BossScriptLibrary.IsScripted(e));
            if (boss == null) { Debug.LogWarning("[Demo] No boss-scripted enemy on the board — run 'Spawn 2×2 Boss' first."); return; }
            if (boss.Stats != null) { boss.Stats.HP = boss.Stats.MaxHP * 0.45f; boss.HealthText.Refresh(); }
            var transitions = Scripts.Services.BossPhaseRunner.AdvancePhasesAndCollectTransitions(boss);
            if (transitions.Count == 0) { Debug.Log($"[Demo] {boss.characterClass} entered no new phase (already at index {boss.Flags.BossPhaseIndex})."); return; }
            foreach (var t in transitions) g.SequenceManager.Add(t);
            g.SequenceManager.Execute();
            Debug.Log($"[Demo] {boss.characterClass} wounded to 45% HP → fired {transitions.Count} phase transition(s); now phase index {boss.Flags.BossPhaseIndex}.");
        }

        /// <summary>Demo (AnnouncementWindow): queue a few event callouts to show the cadence —
        /// each flashes a few times, holds, fades, then the next plays (with a chiptune sting).</summary>
        public void Demo_Announce()
        {
            Scripts.Canvas.AnnouncementWindow.Announce("Rogue casts Teleport");
            Scripts.Canvas.AnnouncementWindow.Announce("Slime A is poisoned");
            Scripts.Canvas.AnnouncementWindow.Announce("Cyclops is ENRAGED!");
            Debug.Log("[Demo] Queued 3 announcements — they play one at a time with flash/hold/fade cadence.");
        }

        /// <summary>Demo (US-077): cast Scan on the first enemy — reveals its stats in the
        /// AnnouncementWindow and flags it Seen in the Bestiary.</summary>
        public void Demo_ScanEnemy()
        {
            var caster = g.Actors.Heroes?.FirstOrDefault(h => h != null && h.IsPlaying);
            var target = g.Actors.Enemies?.FirstOrDefault(e => e != null && e.IsPlaying);
            if (target == null) { Debug.LogWarning("[Demo] No enemy to scan — start a battle first."); return; }
            Scripts.Managers.SpellEffectDispatcher.Cast(Scripts.Data.SpellLibrary.Scan, caster, target);
            Debug.Log($"[Demo] Scanning {target.characterClass} — watch the AnnouncementWindow for its stats.");
        }

        /// <summary>Demo (US-040): scan all ItemDefinitions and report how many declare each new
        /// field — proves the data plumbing exists. Counts are 0 until EPIC E populates them.</summary>
        public void Demo_LogItemDefFields()
        {
            int orbs = 0, spells = 0, resists = 0, total = 0;
            foreach (var item in Scripts.Data.Items.ItemLibrary.All())
            {
                if (item == null) continue;
                total++;
                if (item.BattleStartManaOrbs > 0) orbs++;
                if (!string.IsNullOrEmpty(item.OnUseSpellName)) spells++;
                if (item.ResistanceModifiers != null && item.ResistanceModifiers.Count > 0) resists++;
            }
            Debug.Log($"[Demo] US-040 fields across {total} items → BattleStartManaOrbs:{orbs}, OnUseSpellName:{spells}, ResistanceModifiers:{resists}.");
        }

        /// <summary>Demo (US-041): re-run the equipped-robe battle-start orb grant (Mage Robe = 2,
        /// Wizard Robe = 3 random orbs, capped at 12). Equip a robe in the Equip vendor first.</summary>
        public void Demo_ApplyBattleStartOrbs()
        {
            var mp = g.ManaPoolManager;
            if (mp == null) { Debug.LogWarning("[Demo] No ManaPoolManager (start a battle first)."); return; }
            mp.ApplyBattleStartManaOrbs();
            Demo_LogManaBank();
        }

        /// <summary>Demo (US-042): verify the Sleep Dart → Sleep wiring — its OnUseSpellName resolves
        /// to a real SpellDefinition. (Full cast is via the Alchemist's bar slot in a battle.)</summary>
        public void Demo_VerifyItemSpellRoute()
        {
            var def = Scripts.Data.Items.ItemLibrary.Get(Scripts.Data.Items.ItemData_Consumables.SleepDart.Id);
            if (def == null) { Debug.LogWarning("[Demo] Sleep Dart not registered in ItemLibrary."); return; }
            SpellDefinition spell = null;
            foreach (var s in Scripts.Data.SpellLibrary.All)
                if (s?.Ability != null && s.Ability.Name == def.OnUseSpellName) { spell = s; break; }
            Debug.Log($"[Demo] {def.DisplayName}: OnUseSpellName='{def.OnUseSpellName}', stack={def.MaxStack} → resolves to spell: {(spell != null ? "YES (" + spell.Ability.Name + ")" : "NO")}. Equip on the Alchemist's bar to cast it in battle.");
        }

        /// <summary>Demo (US-043): log the selected hero's effective resistance per DamageType —
        /// per-class × equipped ResistanceModifiers (equip e.g. the Sunfire Amulet to see Fire ×0.7).</summary>
        public void Demo_LogResistance()
        {
            var hero = Scripts.Helpers.GameHelper.Actors.SelectedActor;
            if (hero == null) { Debug.LogWarning("[Demo] Select a hero first."); return; }
            var data = Scripts.Libraries.ActorLibrary.Get(hero.characterClass);
            int shown = 0;
            foreach (Scripts.Models.DamageType t in System.Enum.GetValues(typeof(Scripts.Models.DamageType)))
            {
                float cls = data != null ? data.ResistanceMultiplier(t) : 1f;
                float gear = Scripts.Managers.SpellEffectDispatcher.EquipmentResistanceMultiplier(hero, t);
                if (cls != 1f || gear != 1f)
                {
                    Debug.Log($"[Demo] {hero.name} {t}: class ×{cls:0.##} × gear ×{gear:0.##} = ×{cls * gear:0.##}");
                    shown++;
                }
            }
            Debug.Log($"[Demo] {hero.name} resistance scan: {shown} non-default type(s) (others = ×1.0).");
        }

        /// <summary>Demo (US-080): log each hero's accrued threat (damage dealt this battle). Smart
        /// enemies prefer the highest — fight a bit, then check who the enemies will hunt.</summary>
        public void Demo_LogThreat()
        {
            var all = g.Actors.All;
            if (all == null) { Debug.LogWarning("[Demo] No actors (start a battle)."); return; }
            int n = 0;
            foreach (var a in all)
            {
                if (a == null || !a.IsHero) continue;
                Debug.Log($"[Demo] Threat — {a.name}: {Scripts.Managers.ThreatTracker.GetThreat(a):0} damage dealt.");
                n++;
            }
            if (n == 0) Debug.Log("[Demo] No heroes to report threat for.");
        }

        /// <summary>Demo (US-031): mint one wild (Colorless) orb — the crit reward. Watch it flash
        /// through the spectrum in the orb line (capped at 12).</summary>
        public void Demo_MintWildOrb()
        {
            var mp = g.ManaPoolManager;
            if (mp?.Bank == null) { Debug.LogWarning("[Demo] No ManaBank (start a battle first)."); return; }
            int added = mp.Bank.Add(ManaType.Colorless, 1);
            Debug.Log(added > 0 ? "[Demo] Minted 1 wild (Colorless) orb — watch it flash in the line." : "[Demo] Bank full — no wild orb minted.");
            Demo_LogManaBank();
        }

        /// <summary>Demo (US-030): log each hero class's mana color affinity (what it mints on a pincer).</summary>
        public void Demo_LogColorAffinities()
        {
            var classes = new[]
            {
                CharacterClass.Cleric, CharacterClass.Paladin, CharacterClass.Barbarian, CharacterClass.Alchemist,
                CharacterClass.Assassain, CharacterClass.GreenNinja, CharacterClass.RedNinja
            };
            foreach (var cls in classes)
                Debug.Log($"[Demo] {cls} mints {Scripts.Data.Actor.ManaColorAffinity.For(cls)} on pincer.");
        }

        /// <summary>Demo (US-033, rule B): prove Colorless "wild" orbs pay any color on spend.
        /// White satisfies Heal(W); a lone Colorless ALSO satisfies it (wildcard); a lone Red does not.</summary>
        public void Demo_TestColorlessWildcard()
        {
            var cost = Scripts.Data.ManaAbilities.Heal.Cost; // (W) ×1
            var withWhite = new ManaBank(); withWhite.Add(ManaType.White, 1);
            var withWild = new ManaBank(); withWild.Add(ManaType.Colorless, 1);
            var withRed = new ManaBank(); withRed.Add(ManaType.Red, 1);
            Debug.Log($"[Demo] Heal(W) — White:{withWhite.CanAfford(cost)} (exp true), Colorless-wild:{withWild.CanAfford(cost)} (exp true), Red:{withRed.CanAfford(cost)} (exp false).");
        }

        /// <summary>Demo (US-081): wound a living enemy below the retreat threshold and log its planned
        /// step — it should move AWAY from the heroes instead of advancing.</summary>
        public void Demo_TestEnemyRetreat()
        {
            var enemy = g.Actors.All?.FirstOrDefault(a => a != null && a.IsEnemy && a.IsPlaying);
            if (enemy == null) { Debug.LogWarning("[Demo] No living enemy (start a battle first)."); return; }
            enemy.Stats.HP = Mathf.Max(1f, enemy.Stats.MaxHP * 0.2f); // below RetreatHpThreshold (0.30)
            var step = Scripts.Services.EnemyPlanner.PlanStep(enemy, g.Actors.All, g.TileMap);
            Debug.Log($"[Demo] Wounded {enemy.name} ({enemy.Stats.HP:0}/{enemy.Stats.MaxHP:0}) plans {enemy.location} → {step} (should retreat from heroes).");
        }

        /// <summary>Demo (US-082): log every living enemy's planned step — exercises the full planner
        /// (advance / retreat / own-pincer-seek / ally-supporter positioning).</summary>
        public void Demo_LogEnemyPlans()
        {
            var all = g.Actors.All;
            if (all == null) { Debug.LogWarning("[Demo] No actors (start a battle first)."); return; }
            int n = 0;
            foreach (var e in all)
            {
                if (e == null || !e.IsEnemy || !e.IsPlaying) continue;
                var step = Scripts.Services.EnemyPlanner.PlanStep(e, all, g.TileMap);
                Debug.Log($"[Demo] {e.name} plans {e.location} → {step}.");
                n++;
            }
            if (n == 0) Debug.Log("[Demo] No living enemies to plan.");
        }

        public void Demo_ClearMana() { demoManaBank.Clear(); Demo_LogManaBank(); }

        // ── Phase-B live HUD demos (target the LIVE ManaBank, not the demo bank) ──

        /// <summary>Demo: drop a bouncing mana orb of the given color from a random enemy's position toward the live line.</summary>
        public void Demo_DropOrb(ManaType color)
        {
            ActorInstance source = null;
            try { source = System.Linq.Enumerable.FirstOrDefault(g.Actors.Enemies, e => e.IsPlaying); } catch { }
            if (source == null) try { source = System.Linq.Enumerable.FirstOrDefault(g.Actors.Heroes, h => h.IsPlaying); } catch { }
            if (source == null) { Debug.LogWarning("[Demo] No actor on board to drop from."); return; }
            Scripts.Factories.ManaOrbFactory.Drop(source.transform.position, color);
        }

        public void Demo_GiveMana_White() => Demo_DropOrb(ManaType.White);
        public void Demo_GiveMana_Blue()  => Demo_DropOrb(ManaType.Blue);
        public void Demo_GiveMana_Black() => Demo_DropOrb(ManaType.Black);
        public void Demo_GiveMana_Red()   => Demo_DropOrb(ManaType.Red);
        public void Demo_GiveMana_Green() => Demo_DropOrb(ManaType.Green);

        /// <summary>Demo (US-016): apply Slowed (2 Turns) to the first playing enemy so you can
        /// advance its turns (e.g. Trigger Enemy Attack) and watch the turn-unit buff tick down
        /// and expire — verifies BuffSystem.TickTurn is wired into the turn boundary.</summary>
        public void Demo_ApplySlowedToEnemy()
        {
            var enemy = g.Actors.Enemies?.FirstOrDefault(e => e != null && e.IsPlaying);
            if (enemy == null) { Debug.LogWarning("[Demo] No playing enemy to debuff — spawn one first."); return; }
            BuffSystem.Apply(enemy, Scripts.Data.Buffs.Slowed);
            Debug.Log($"[Demo] Applied Slowed (2 Turns) to {enemy.name}. Advance its turns (Trigger Enemy Attack) to watch TickTurn decrement + expire (US-016).");
        }

        /// <summary>Demo (US-013): apply Blinded (2 Turns) to the first playing enemy so its attacks
        /// roll at halved accuracy — Trigger Enemy Attack repeatedly to see extra misses.</summary>
        public void Demo_ApplyBlindedToEnemy()
        {
            var enemy = g.Actors.Enemies?.FirstOrDefault(e => e != null && e.IsPlaying);
            if (enemy == null) { Debug.LogWarning("[Demo] No playing enemy to debuff — spawn one first."); return; }
            BuffSystem.Apply(enemy, Scripts.Data.Buffs.Blinded);
            Debug.Log($"[Demo] Applied Blinded (2 Turns) to {enemy.name}. Its attacks now hit at ×{Scripts.Data.Buffs.BlindedAccuracyMultiplier} accuracy (US-013).");
        }

        /// <summary>Demo (US-012): Silence the selected hero so their Spell slots block (red) and
        /// spell clicks are refused — Skills/Items still work.</summary>
        public void Demo_ApplySilencedToHero()
        {
            var hero = g.Actors.SelectedActor;
            if (hero == null || !hero.IsHero) { Debug.LogWarning("[Demo] Select a hero first."); return; }
            BuffSystem.Apply(hero, Scripts.Data.Buffs.Silenced);
            Debug.Log($"[Demo] Silenced {hero.name} (2 Turns). Spell slots now blocked (US-012); Skills/Items unaffected.");
        }

        /// <summary>Toggle the cheat that lets 1–N orbs of ANY color pay for any 1–N-cost spell.</summary>
        public void Demo_ToggleAnyColor()
        {
            var bank = g.ManaBank;
            if (bank == null) { Debug.LogWarning("[Demo] No live ManaBank yet (enter Game scene)."); return; }
            bank.AllowAnyColor = !bank.AllowAnyColor;
            Debug.Log($"[Demo] ManaBank.AllowAnyColor = {bank.AllowAnyColor}");
        }

        /// <summary>Randomize the selected hero's ability bar by writing a runtime override into
        /// <see cref="Scripts.Data.HeroLoadouts.perClass"/>. The bar re-binds on the next selection
        /// change (or already shows the new set if this hero is currently selected and the bar
        /// polls via Update). Fix #9: was log-only; now actually swaps.</summary>
        public void Demo_RandomHeroAbilities()
        {
            ActorInstance hero = null;
            try { hero = System.Linq.Enumerable.FirstOrDefault(g.Actors.Heroes, h => h.IsPlaying); } catch { }
            if (hero == null) { Debug.LogWarning("[Demo] No hero on board."); return; }

            // Build a random 6-entry list from castable spells + one Potion slot.
            var spellPool = new System.Collections.Generic.List<ManaAbility>();
            foreach (var a in Scripts.Data.ManaAbilities.Slots)
                if (a != null && a.Kind == AbilityKind.Spell) spellPool.Add(a);

            var rng = new System.Random();
            for (int i = spellPool.Count - 1; i > 0; i--)
            {
                int j = rng.Next(i + 1);
                (spellPool[i], spellPool[j]) = (spellPool[j], spellPool[i]);
            }

            var loadout = new ManaAbility[6];
            for (int s = 0; s < 6; s++) loadout[s] = (s < spellPool.Count) ? spellPool[s] : null;
            // Override slot 5 with the Potion item so the random loadout still has a consumable.
            loadout[5] = Scripts.Data.ManaAbilities.Potion;

            Scripts.Data.HeroLoadouts.Set(hero.characterClass, loadout);

            // Force a re-bind by clearing then restoring the selected actor (cheap kick).
            var prev = g.Actors.SelectedActor;
            if (prev != null)
            {
                g.Actors.SelectedActor = null;
                g.Actors.SelectedActor = prev;
            }

            var sb = new System.Text.StringBuilder($"[Demo] Set random loadout for {hero.characterClass}: ");
            foreach (var a in loadout) sb.Append(a == null ? "[—] " : $"{a.Name}{Scripts.Data.ManaAbilities.CostIcons(a)} ");
            Debug.Log(sb.ToString());
        }

        #endregion

        // Small helper to spawn a VFX for available heroes (guards null heroes)
        /// <summary>Creates the visual effect.</summary>
        private void SpawnVisualEffect(VisualEffectAsset vfx)
        {
            g.VisualEffectManager.Spawn(vfx, hero1.Position);
            g.VisualEffectManager.Spawn(vfx, hero2.Position);
        }

        // Gain a small, random chunk of XP for a random hero (wired in DebugWindow DebugOptions -> AddExperience)
        /// <summary>Add experience.</summary>
        public void AddExperience()
        {
            var hero = RNG.Hero;
            if (hero == null) return;

            var nextLevel = ExperienceHelper.NextLevel(hero.Stats.Level);
            var xp = Mathf.Max(1, (nextLevel * RNG.Float(0.25f, 0.33f)).ToInt());
            ExperienceHelper.Gain(hero, xp);

            g.CombatTextManager.Spawn($"+{xp} XP", hero.Position, "Heal");
            g.AudioManager.Play("Click");
        }

        // New: Gain exact XP for the selected/random hero (utility)
        /// <summary>Add experience.</summary>
        public void AddExperience(int amount)
        {
            var hero = RNG.Hero;
            if (hero == null || amount <= 0) return;

            ExperienceHelper.Gain(hero, amount);
            g.CombatTextManager.Spawn($"+{amount} XP", hero.Position, "Heal");
            g.AudioManager.Play("Click");
        }

        // TODO: Should be controlled by CoinManager
        // Wired in DebugWindow DebugOptions -> SpawnCoins
        //public void SpawnCoins()
        //{
        //    var target = hero1 ?? RNG.Hero;
        //    if (target == null) return;

        //    // Default burst of 10
        //    SpawnCoins(10);
        //}

        // New: spawn an exact number of coins at hero1 (or a random hero) using CoinManager
        /// <summary>Creates the coins.</summary>
        public void SpawnCoins()
        {
            var target = hero1 ?? RNG.Hero;
            var amount = RNG.Int(10, 20);
            if (target == null || amount <= 0) return;

            // Optional VFX gate, then spawn coins
            //if (VfxLibrary.VisualEffects.TryGetValue("YellowHit", out var vfx))
            //    g.VfxManager.Spawn(vfx, target.Position);

            g.CoinManager.SpawnBurst(target.Position, amount);
        }

        /// <summary>
        /// Lays out a single horizontal pincer lane for quick debugging.
        /// Spawns six slimes, destroys all other enemies, teleports up to two heroes and the six slimes
        /// to fixed positions, and moves all other playing actors to random unoccupied tiles.
        /// By keeping the newly spawned slimes alive while removing other enemies,
        /// the wave does not advance and the stage does not restart.
        /// </summary>
        public void ArrangeSingleCombo()
        {
            // Spawn six slimes for this debug layout and keep references
            var keptSlimes = new List<ActorInstance>(6);
            for (int i = 0; i < 6; i++)
                keptSlimes.Add(SpawnSlime());

            // Destroy all existing enemies except the six slimes we just spawned
            foreach (var enemy in g.Actors.Enemies.ToArray())
            {
                if (enemy == null) continue;
                if (keptSlimes.Contains(enemy)) continue;

                UnityEngine.Object.Destroy(enemy.gameObject);
            }

            // Horizontal lane positions
            hero1?.Teleport(new Vector2Int(3, 1));
            keptSlimes[0]?.Teleport(new Vector2Int(3, 2));
            keptSlimes[1]?.Teleport(new Vector2Int(3, 3));
            keptSlimes[2]?.Teleport(new Vector2Int(3, 4));
            keptSlimes[3]?.Teleport(new Vector2Int(3, 5));
            keptSlimes[4]?.Teleport(new Vector2Int(3, 6));
            keptSlimes[5]?.Teleport(new Vector2Int(3, 7));
            hero2?.Teleport(new Vector2Int(3, 8));

            // Build alignment group
            var group = new List<ActorInstance> { hero1, hero2 };
            group.AddRange(keptSlimes.Where(s => s != null));

            // Move every other playing actor to an unoccupied location
            foreach (var actor in g.Actors.All)
            {
                if (actor == null) continue;
                if (!actor.IsPlaying) continue;
                if (group.Contains(actor)) continue;

                actor.Teleport(RNG.UnoccupiedLocation);
            }
        }


        /// <summary>Arrange double combo.</summary>
        public void ArrangeDoubleCombo()
        {
            // Spawn either slimes used by this debug layout
            for (int i = 0; i < 8; i++)
                SpawnSlime();

            // Collect up to 9 enemies, some may be missing
            var enemies = g.Actors.Enemies.Take(8).ToArray();

            // Utility to teleport only when the actor exists
            void SafeTeleport(ActorInstance a, Vector2Int pos)
            {
                if (a != null) a.Teleport(pos);
            }

            // Heroes may be assigned in SpawnSlime; guard in case any are missing
            SafeTeleport(hero1, new Vector2Int(1, 1));
            SafeTeleport(enemies[0], new Vector2Int(1, 2));
            SafeTeleport(enemies[1], new Vector2Int(1, 3));
            SafeTeleport(enemies[2], new Vector2Int(1, 4));
            SafeTeleport(enemies[3], new Vector2Int(1, 5));
            SafeTeleport(hero2, new Vector2Int(1, 6));
            SafeTeleport(enemies[4], new Vector2Int(2, 6));
            SafeTeleport(enemies[5], new Vector2Int(3, 6));
            SafeTeleport(enemies[6], new Vector2Int(4, 6));
            SafeTeleport(enemies[7], new Vector2Int(5, 6));

            // Build the alignment group without nulls
            var group = new List<ActorInstance> { hero1, hero2, hero3, hero4 };
            group.AddRange(enemies.Where(e => e != null));
            group = group.Where(x => x != null).ToList();

            // Move every other playing actor to an unoccupied location
            foreach (var actor in g.Actors.All)
            {
                if (actor == null) continue;
                if (!actor.IsPlaying) continue;
                if (group.Contains(actor)) continue;

                actor.Teleport(RNG.UnoccupiedLocation);
            }
        }

        /// <summary>Arrange triple combo.</summary>
        public void ArrangeTripleCombo()
        {
            // Spawn nine slimes used by this debug layout
            for (int i = 0; i < 9; i++)
                SpawnSlime();

            // Collect up to 9 enemies, some may be missing
            var enemies = g.Actors.Enemies.Take(9).ToArray();

            // Utility to teleport only when the actor exists
            void SafeTeleport(ActorInstance a, Vector2Int pos)
            {
                if (a != null) a.Teleport(pos);
            }

            // Heroes may be assigned in SpawnSlime; guard in case any are missing
            SafeTeleport(hero1, new Vector2Int(1, 1));
            SafeTeleport(enemies[0], new Vector2Int(1, 2));
            SafeTeleport(enemies[1], new Vector2Int(1, 3));
            SafeTeleport(hero2, new Vector2Int(1, 4));
            SafeTeleport(enemies[2], new Vector2Int(2, 4));
            SafeTeleport(enemies[3], new Vector2Int(3, 4));
            SafeTeleport(enemies[4], new Vector2Int(4, 4));
            SafeTeleport(enemies[5], new Vector2Int(5, 4));
            SafeTeleport(hero3, new Vector2Int(6, 4));
            SafeTeleport(enemies[6], new Vector2Int(6, 5));
            SafeTeleport(enemies[7], new Vector2Int(6, 6));
            SafeTeleport(enemies[8], new Vector2Int(6, 7));
            SafeTeleport(hero4, new Vector2Int(6, 8));

            // Build the alignment group without nulls
            var group = new List<ActorInstance> { hero1, hero2, hero3, hero4 };
            group.AddRange(enemies.Where(e => e != null));
            group = group.Where(x => x != null).ToList();

            // Move every other playing actor to an unoccupied location
            foreach (var actor in g.Actors.All)
            {
                if (actor == null) continue;
                if (!actor.IsPlaying) continue;
                if (group.Contains(actor)) continue;

                actor.Teleport(RNG.UnoccupiedLocation);
            }
        }


        /// <summary>
        /// Arranges a surround combo for debug testing.
        /// Spawns a slime in the center and positions up to four heroes
        /// around it (above, right, below, left).
        /// </summary>
        public void ArrangeSurroundCombo()
        {
            var center = new Vector2Int(3, 3);
            var above = new Vector2Int(3, 2);
            var right = new Vector2Int(4, 3);
            var below = new Vector2Int(3, 4);
            var left = new Vector2Int(2, 3);

            // Ensure at least one slime exists
            SpawnSlime();

            var slime = g.Actors.Enemies.FirstOrDefault(x => x != null && x.characterClass == CharacterClass.Slime00);
            if (slime == null)
            {
                Debug.LogError("ArrangeSurroundCombo: No slime found to place in center.");
                return;
            }

            // Safe teleport helper
            void SafeTeleport(ActorInstance actor, Vector2Int pos)
            {
                if (actor != null) actor.Teleport(pos);
            }

            // Place slime and heroes
            SafeTeleport(slime, center);
            SafeTeleport(hero1, above);
            SafeTeleport(hero2, right);
            SafeTeleport(hero3, below);
            SafeTeleport(hero4, left);
        }



        /// <summary>Bump.</summary>
        public void Bump()
        {
            var hero = RNG.Hero;
            hero.Teleport(RNG.UnoccupiedLocation);

            // 3) try to find an attacker already adjacent
            var enemy = Geometry.GetAdjacentOpponent(hero);
            if (!enemy.Exists())
                enemy = RNG.Enemy;

            var location = Geometry.GetClosestUnoccupiedAdjacentTileByLocation(hero.location).location;
            if (!location.Exists())
                location = Geometry.GetAdjacentLocationInDirection(hero.location, RNG.AdjacentDirection);

            enemy.Teleport(location);
            hero.Animation.Bump(enemy);
        }

        /// <summary>Dodge.</summary>
        public void Dodge()
        {
            hero1.Animation.Dodge();
        }

        /// <summary>Kill enemies.</summary>
        public void KillEnemies()
        {
            var actors = g.Actors.Enemies.Where(x => x != null && x.IsPlaying).ToList();
            StartCoroutine(KillRoutine(actors));
        }

        /// <summary>Kill heroes.</summary>
        public void KillHeroes()
        {
            var actors = g.Actors.Heroes.Where(x => x != null && x.IsPlaying).ToList();
            StartCoroutine(KillRoutine(actors));
        }

        /// <summary>Coroutine that executes the kill sequence.</summary>
        private IEnumerator KillRoutine(List<ActorInstance> playingActors)
        {
            // Capture the currently playing actors so we only wait on these
            if (playingActors.Count < 1)
                yield break;

            // Apply lethal damage
            foreach (var actor in playingActors)
            {
                var attacker = actor.IsHero ? RNG.Enemy : RNG.Hero;
                var attackResult = new AttackResult(attacker, actor, 9999, HitOutcome.Critical);
                actor.Damage(attackResult);
            }

            // Let the centralized death processor finish deaths (spawns coins, notifies stage, etc.)
            yield return DeathHelper.ProcessRoutine();

            // Ensure they have transitioned to dead/inactive
            yield return new WaitUntil(() => playingActors.All(e => e == null || e.IsDead));

            //DEBUG IS this the best way to trigger the steps leading to death?
            // Nudge stage flow in case some deaths happened outside normal flow
            g.StageManager.OnActorDeath();
        }

        /// <summary>Goto post battle screen.</summary>
        public void GotoPostBattleScreen()
        {
            // Seed XP for participating heroes and route to PostBattleScreen for accumulation
            var save = ProfileHelper.CurrentProfile?.CurrentSave;

            // Prefer the party list from the save; fall back to active heroes in scene
            var participants = (save?.Party?.Members?.Select(m => m.CharacterClass)
                                    .Where(ch => ch != CharacterClass.None)
                                    .ToList())
                               ?? new List<CharacterClass>();

            if (participants.Count == 0)
            {
                participants = g.Actors.Heroes
                    .Where(h => h != null && h.characterClass != CharacterClass.None)
                    .Select(h => h.characterClass)
                    .Distinct()
                    .ToList();
            }

            // Start a new XP session (optional if already started by StageManager)
            ExperienceTracker.StartSession(participants);

            // Grant a small amount of XP to each participant
            foreach (var ch in participants)
            {
                // Example debug amount: 100 +/- 25
                int amount = RNG.Int(75, 125);
                ExperienceTracker.AddParticipant(ch);
                ExperienceTracker.AddXP(ch, amount);
            }

            // Jump to PostBattle screen so the UI can display and then apply gains
            scene.Fade.ToPostBattleScreen();
        }

        /// <summary>Portrait2 d slide in.</summary>
        public void Portrait2DSlideIn()
        {
            var hero = RNG.Hero;
            var direction = RNG.AdjacentDirection;
            g.PortraitManager.SlideIn2D(hero, direction);
        }

        /// <summary>Portrait3 d slide in.</summary>
        public void Portrait3DSlideIn()
        {
            var hero = RNG.Hero;
            var direction = RNG.AdjacentDirection;
            g.PortraitManager.SlideIn3D(hero, direction);
        }

        /// <summary>Portrait pop in.</summary>
        public void PortraitPopIn()
        {
            var hero = RNG.Hero;
            g.SequenceManager.Add(new PortraitPopInSequence(hero));
            g.SequenceManager.Add(new PortraitPopOutSequence(hero));
            StartCoroutine(g.SequenceManager.ExecuteRoutine());
        }

        /// <summary>Creates the damage text.</summary>
        public void SpawnDamageText()
        {
            var hero = RNG.Hero;
            var text = $"{RNG.Int(1, 100)}";
            g.CombatTextManager.Spawn(text, hero.Position, "Damage");
        }

        /// <summary>Creates the heal text.</summary>
        public void SpawnHealText()
        {
            var hero = RNG.Hero;
            var text = $"{RNG.Int(1, 100)}";
            g.CombatTextManager.Spawn(text, hero.Position, "Heal");
        }


        /// <summary>Shake.</summary>
        public void Shake()
        {
            var intensity = RNG.ShakeIntensityLevel();
            var duration = RNG.Float(Interval.HalfSecond, Interval.TwoSeconds);
            hero1.Animation.Shake(intensity, duration);
        }

        /// <summary>Spin.</summary>
        public void Spin()
        {
            hero1.Animation.Spin360();
        }

        /// <summary>Creates the support lines.</summary>
        public void SpawnSupportLines()
        {
            foreach (var attacker in g.Actors.Heroes)
            {
                var supporters = g.PincerAttackManager.FindSupporters(attacker);
                foreach (var supporter in supporters)
                {
                    var newest = g.SupportLineManager.Spawn(supporter, attacker);
                    newest.isStatic = true;
                }
            }
        }

        /// <summary>Creates the synergy lines.</summary>
        public void SpawnSynergyLines()
        {
            foreach (var attacker in g.Actors.Heroes)
            {
                var supporters = g.PincerAttackManager.FindSupporters(attacker);
                foreach (var supporter in supporters)
                {
                    g.SynergyLineManager.Spawn(supporter, attacker);
                }
            }
        }

        /// <summary>Creates the tooltip1.</summary>
        public void SpawnTooltip1()
        {
            var tt = new TooltipSettings()
            {
                message = "Tap here to confirm",
                target = hero1.transform,
                placement = TooltipPlacement.Top,
                useFade = true,
                useTypewriter = true,
                autoDestroy = true,
                followPointer = false,
                autoDestroyDelay = 2.5f,
            };

            Tooltip.Show(tt);
        }

        /// <summary>Creates the tooltip2.</summary>
        public void SpawnTooltip2()
        {
            var tt = new TooltipSettings()
            {
                message = "Tap here to confirm",
                target = hero1.transform,
                placement = TooltipPlacement.Top,
                useFade = false,
                useTypewriter = false,
                autoDestroy = true,
                followPointer = false,
                autoDestroyDelay = 2.5f,
            };

            Tooltip.Show(tt);
        }

        /// <summary>Trigger enemy move attack.</summary>
        public void TriggerEnemyMoveAttack()
        {
            var attackingEnemies = g.Actors.Enemies.Where(x => x.IsPlaying).ToList();
            attackingEnemies.ForEach(x => x.SetReady());

            if (g.TurnManager.IsHeroTurn)
                g.TurnManager.NextTurn();           // switch to attacker turn

        }

        /// <summary>Trigger enemy attack.</summary>
        public void TriggerEnemyAttack()
        {
            if (g.TurnManager.IsHeroTurn)
                g.TurnManager.NextTurn();           // switch to attacker turn
        }


        /// <summary>Title test.</summary>
        public void TitleTest()
        {
            var text = DateTime.UtcNow.Ticks.ToString();

        }

        /// <summary>Tutorial test.</summary>
        public void TutorialTest()
        {
            var tutorial = TutorialLibrary.Tutorials["Tutorial1"];
            g.TutorialPopup.Load(tutorial);
        }

        /// <summary>Creates the slime.</summary>
        public ActorInstance SpawnSlime()
        {
            return g.StageManager.AddEnemy(CharacterClass.Slime00);
        }

        /// <summary>Creates the bat.</summary>
        public ActorInstance SpawnBat()
        {
            return g.StageManager.AddEnemy(CharacterClass.Bat00);
        }

        /// <summary>Creates the scorpion.</summary>
        public ActorInstance SpawnScorpion()
        {
            return g.StageManager.AddEnemy(CharacterClass.Scorpion);
        }

        /// <summary>Creates the yeti.</summary>
        public ActorInstance SpawnYeti()
        {
            return g.StageManager.AddEnemy(CharacterClass.Yeti);
        }

        /// <summary>Creates the soldier.</summary>
        public ActorInstance SpawnSoldier()
        {
            return SpawnRandomByGroup(ActorTag.Soldier | ActorTag.Soldier);
        }


        /// <summary>Creates the random enemy.</summary>
        public void SpawnRandomEnemy()
        {
            var r = RNG.Int(1, 10);
            if (r <= 7) SpawnSlime();
            else if (r == 8) SpawnBat();
            else if (r == 9) SpawnScorpion();
            else if (r == 10) SpawnYeti();
        }

        /// <summary>
        /// Spawns a random enemy whose ActorData matches all requested groups.
        /// Example: SpawnRandomByGroup(ActorGroup.Soldier | ActorGroup.Elite)
        /// </summary>
        public ActorInstance SpawnRandomByGroup(ActorTag requiredGroups)
        {
            var actorData = ActorLibrary.Actors
                .Where(x => x.Value.InGroups(requiredGroups)).ToList()
                .Shuffle().FirstOrDefault().Value;

            if (actorData == null) return null;

            return g.StageManager.AddEnemy(actorData.CharacterClass);
        }


        /// <summary>Fireball.</summary>
        public void Fireball()
        {
            var startPosition = hero1.Position;
            var target = hero2;

            // Use ProjectileManager helper which sets MotionStyle and pacing
            g.ProjectileManager.EnqueueFireball(startPosition, target);
            g.SequenceManager.Execute();
        }

        /// <summary>Heal.</summary>
        public void Heal()
        {
            var source = hero1.Position;
            var target = hero2;

            // Use ProjectileManager helper which sets MotionStyle and pacing
            g.ProjectileManager.EnqueueHeal(source, target);
            g.SequenceManager.Execute();
        }

        /// <summary>Homing spiral.</summary>
        public void HomingSpiral()
        {
            var source = hero1.Position;
            var target = hero2;

            // Use ProjectileManager helper which sets MotionStyle and pacing
            g.ProjectileManager.EnqueueHomingSpiral(source, target);
            g.SequenceManager.Execute();
        }


        /// <summary>Randomize background.</summary>
        public void RandomizeBackground()
        {
            g.Background.Randomize();
        }


        /// <summary>Trigger next turn.</summary>
        public void TriggerNextTurn()
        {
            g.TurnManager.NextTurn();
        }

        /// <summary>Vfx test_blue slash1.</summary>
        public void VFXTest_BlueSlash1()
        {
            var targetEnemy = g.Actors.Enemies.FirstOrDefault();
            if (targetEnemy == null)
                return;

            var attackResult = new AttackResult(hero1, targetEnemy, 3, HitOutcome.Normal);
            if (attackResult.HitType == HitOutcome.Critical)
            {
                var crit = VisualEffectLibrary.VisualEffects["YellowHit"];
                g.VisualEffectManager.Spawn(crit, hero1.Position);
                attackResult.Damage = (int)Math.Round(attackResult.Damage * 1.5f);
            }

            var vfx = VisualEffectLibrary.VisualEffects["BlueSlash1"];
            g.VisualEffectManager.Spawn(vfx, hero1.Position, hero1.DamageRoutine(attackResult));
        }

        /// <summary>Vfx test_blue slash2.</summary>
        public void VFXTest_BlueSlash2()
        {
            var vfx = VisualEffectLibrary.VisualEffects["BlueSlash2"];
            SpawnVisualEffect(vfx);
        }

        /// <summary>Vfx test_blue slash3.</summary>
        public void VFXTest_BlueSlash3()
        {
            var vfx = VisualEffectLibrary.VisualEffects["BlueSlash3"];
            SpawnVisualEffect(vfx);
        }

        /// <summary>Vfx test_blue slash4.</summary>
        public void VFXTest_BlueSlash4()
        {
            var vfx = VisualEffectLibrary.VisualEffects["BlueSlash4"];
            SpawnVisualEffect(vfx);
        }

        /// <summary>Vfx test_blue sword.</summary>
        public void VFXTest_BlueSword()
        {
            var vfx = VisualEffectLibrary.VisualEffects["BlueSword"];
            SpawnVisualEffect(vfx);
        }

        /// <summary>Vfx test_blue sword4 x.</summary>
        public void VFXTest_BlueSword4X()
        {
            var vfx = VisualEffectLibrary.VisualEffects["BlueSword4X"];
            SpawnVisualEffect(vfx);
        }

        /// <summary>Vfx test_blood claw.</summary>
        public void VFXTest_BloodClaw()
        {
            var vfx = VisualEffectLibrary.VisualEffects["BloodClaw"];
            SpawnVisualEffect(vfx);
        }

        /// <summary>Vfx test_level up.</summary>
        public void VFXTest_LevelUp()
        {
            var vfx = VisualEffectLibrary.VisualEffects["LevelUp"];
            SpawnVisualEffect(vfx);
        }

        /// <summary>Vfx test_yellow hit.</summary>
        public void VFXTest_YellowHit()
        {
            var vfx = VisualEffectLibrary.VisualEffects["YellowHit"];
            SpawnVisualEffect(vfx);
        }

        /// <summary>Vfx test_double claw.</summary>
        public void VFXTest_DoubleClaw()
        {
            var vfx = VisualEffectLibrary.VisualEffects["DoubleClaw"];
            SpawnVisualEffect(vfx);
        }

        /// <summary>Vfx test_lightning explosion.</summary>
        public void VFXTest_LightningExplosion()
        {
            var vfx = VisualEffectLibrary.VisualEffects["LightningExplosion"];
            SpawnVisualEffect(vfx);
        }

        /// <summary>Vfx test_buff life.</summary>
        public void VFXTest_BuffLife()
        {
            var vfx = VisualEffectLibrary.VisualEffects["BuffLife"];
            SpawnVisualEffect(vfx);
        }

        /// <summary>Vfx test_rotary knife.</summary>
        public void VFXTest_RotaryKnife()
        {
            var vfx = VisualEffectLibrary.VisualEffects["RotaryKnife"];
            SpawnVisualEffect(vfx);
        }

        /// <summary>Vfx test_air slash.</summary>
        public void VFXTest_AirSlash()
        {
            var vfx = VisualEffectLibrary.VisualEffects["AirSlash"];
            SpawnVisualEffect(vfx);
        }

        /// <summary>Vfx test_fire rain.</summary>
        public void VFXTest_FireRain()
        {
            var vfx = VisualEffectLibrary.VisualEffects["FireRain"];
            SpawnVisualEffect(vfx);
        }

        /// <summary>Vfx test_ray blast.</summary>
        public void VFXTest_RayBlast()
        {
            var vfx = VisualEffectLibrary.VisualEffects["RayBlast"];
            SpawnVisualEffect(vfx);
        }

        /// <summary>Vfx test_lightning strike.</summary>
        public void VFXTest_LightningStrike()
        {
            var vfx = VisualEffectLibrary.VisualEffects["LightningStrike"];
            SpawnVisualEffect(vfx);
        }

        /// <summary>Vfx test_puffy explosion.</summary>
        public void VFXTest_PuffyExplosion()
        {
            var vfx = VisualEffectLibrary.VisualEffects["PuffyExplosion"];
            SpawnVisualEffect(vfx);
        }

        /// <summary>Vfx test_red slash2 x.</summary>
        public void VFXTest_RedSlash2X()
        {
            var vfx = VisualEffectLibrary.VisualEffects["RedSlash2X"];
            SpawnVisualEffect(vfx);
        }

        /// <summary>Vfx test_god rays.</summary>
        public void VFXTest_GodRays()
        {
            var vfx = VisualEffectLibrary.VisualEffects["GodRays"];
            SpawnVisualEffect(vfx);
        }

        /// <summary>Vfx test_acid splash.</summary>
        public void VFXTest_AcidSplash()
        {
            var vfx = VisualEffectLibrary.VisualEffects["AcidSplash"];
            SpawnVisualEffect(vfx);
        }
        /// <summary>Vfx test_green buff.</summary>
        public void VFXTest_GreenBuff()
        {
            var vfx = VisualEffectLibrary.VisualEffects["GreenBuff"];
            SpawnVisualEffect(vfx);
        }

        /// <summary>Vfx test_gold buff.</summary>
        public void VFXTest_GoldBuff()
        {
            var vfx = VisualEffectLibrary.VisualEffects["GoldBuff"];
            SpawnVisualEffect(vfx);
        }

        /// <summary>Vfx test_hex shield.</summary>
        public void VFXTest_HexShield()
        {
            var vfx = VisualEffectLibrary.VisualEffects["HexShield"];
            SpawnVisualEffect(vfx);
        }

        /// <summary>Vfx test_toxic cloud.</summary>
        public void VFXTest_ToxicCloud()
        {
            var vfx = VisualEffectLibrary.VisualEffects["ToxicCloud"];
            SpawnVisualEffect(vfx);
        }

        /// <summary>Vfx test_orange slash.</summary>
        public void VFXTest_OrangeSlash()
        {
            var vfx = VisualEffectLibrary.VisualEffects["OrangeSlash"];
            SpawnVisualEffect(vfx);
        }

        /// <summary>Vfx test_moon feather.</summary>
        public void VFXTest_MoonFeather()
        {
            var vfx = VisualEffectLibrary.VisualEffects["MoonFeather"];
            SpawnVisualEffect(vfx);
        }

        /// <summary>Vfx test_pink spark.</summary>
        public void VFXTest_PinkSpark()
        {
            var vfx = VisualEffectLibrary.VisualEffects["PinkSpark"];
            SpawnVisualEffect(vfx);
        }

        /// <summary>Vfx test_blue yellow sword.</summary>
        public void VFXTest_BlueYellowSword()
        {
            var vfx = VisualEffectLibrary.VisualEffects["BlueYellowSword"];
            SpawnVisualEffect(vfx);
        }

        /// <summary>Vfx test_blue yellow sword3 x.</summary>
        public void VFXTest_BlueYellowSword3X()
        {
            var vfx = VisualEffectLibrary.VisualEffects["BlueYellowSword3X"];
            SpawnVisualEffect(vfx);
        }

        /// <summary>Vfx test_red sword.</summary>
        public void VFXTest_RedSword()
        {
            var vfx = VisualEffectLibrary.VisualEffects["RedSword"];
            SpawnVisualEffect(vfx);
        }



        /// <summary>Vfx test_tech sword.</summary>
        public void VFXTest_TechSword()
        {
            var vfx = VisualEffectLibrary.VisualEffects["TechSword"];
            SpawnVisualEffect(vfx);
        }

    }

}
