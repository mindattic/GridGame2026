using UnityEngine;
using g = Scripts.Helpers.GameHelper;
using Scripts.Helpers;
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
using Scripts.Libraries;
using Scripts.Models;
using Scripts.Models.Actor;
using Scripts.Overworld;
using Scripts.Sequences;
using Scripts.Serialization;
using Scripts.Utilities;

namespace Scripts.Managers
{
    /// <summary>
    /// MANAPOOLMANAGER - Hero mana resource system (PHASE B: shim over the new orb economy).
    ///
    /// <para>PURPOSE: owns the team's <see cref="ManaBank"/> (a capped line of colored orbs) and
    /// translates between the legacy float-mana API and orb spends/grants so existing callers
    /// (AbilityManager, AbilityButtonManager, FX/pickups) keep working unchanged.</para>
    ///
    /// <para>PHASE B CHANGES:</para>
    /// <list type="bullet">
    ///   <item>The 5/sec time-accrual is REMOVED. Mana is harvested by completing pincers — see
    ///   <see cref="PincerAttackManager"/> (each hero attacker + supporter drops a Blue orb via
    ///   <see cref="ManaOrbFactory"/>).</item>
    ///   <item>The old <c>Canvas/ManaPool</c> subtree (fill bar + Bank button + glow) is GONE —
    ///   deleted from <c>GameBuilder.cs</c>. <see cref="OnBankButtonClicked"/> survives because
    ///   <see cref="TurnManager"/> calls it as the auto-skip-to-next-enemy fallback — but it no
    ///   longer grants mana.</item>
    ///   <item>The new HUD is spawned in <see cref="Start"/>: <see cref="ManaOrbLineFactory"/> +
    ///   <see cref="ShieldButtonFactory"/>, both parented to the Canvas.</item>
    ///   <item><see cref="heroMana"/> is a DERIVED view: <c>Bank.Count(Blue) * ManaPerOrb</c>.
    ///   <see cref="Spend"/> rounds float costs to orbs via <c>ceil(cost / ManaPerOrb)</c>.</item>
    /// </list>
    ///
    /// <para>ACCESS: <c>g.ManaPoolManager</c> (legacy) or <c>g.ManaBank</c> (new, preferred).</para>
    /// </summary>
    public class ManaPoolManager : MonoBehaviour
    {
        /// <summary>Float-mana-per-orb conversion factor — bridges legacy float costs to orb counts.</summary>
        public const float ManaPerOrb = 10f;

        public float maxMana = 100f;
        public float enemyMana = 0f;

        /// <summary>The live mana-orb line; ability spends/grants run through this.</summary>
        public ManaBank Bank { get; private set; } = new ManaBank();

        /// <summary>The live orb-line HUD spawned at Start — held so dropping orbs can find their target slot.</summary>
        public ManaOrbLine OrbLine { get; private set; }

        /// <summary>Derived: float-mana view onto the orb line. Setter is a no-op (kept for back-compat).</summary>
        public float heroMana
        {
            get => Bank.Count(ManaType.Blue) * ManaPerOrb;
            set { /* PHASE B: no-op — orbs are added/spent through Bank now. */ }
        }

        /// <summary>PHASE B: spawn the new HUD pieces under the main Canvas — orb line (Row 14),
        /// shield button (Row 2 right), and the 6-slot mana ability bar (Row 13, inside the
        /// existing AbilityButtonContainer that GameBuilder placed). After actors are ready,
        /// attach a debuff icon strip above each. <b>Game-scene-only</b> — silently no-ops in
        /// Bestiary / Overworld / vendor scenes that also happen to host this component.</summary>
        private void Start()
        {
            // Fix #1: gate by scene name so other scenes with a Canvas (e.g. Bestiary) don't get
            // an orb line stranded on top of unrelated UI.
            var activeScene = UnityEngine.SceneManagement.SceneManager.GetActiveScene();
            if (activeScene.name != "Game") return;

            var canvas = GameObject.Find("Canvas");
            if (canvas == null) return;
            OrbLine = ManaOrbLineFactory.Create(canvas.transform, Bank);
            ShieldButtonFactory.Create(canvas.transform);
            Scripts.Factories.AnnouncementWindowFactory.Create(canvas.transform); // dedicated event-callout banner

            var abilityContainer = GameObject.Find("Canvas/AbilityButtonContainer");
            if (abilityContainer != null)
                AbilityBarFactory.Create(abilityContainer.transform, Bank);

            // Once heroes + enemies exist on the board, drop a debuff icon strip above each.
            Scripts.Utilities.GameReady.WhenReady(this, () => AttachDebuffBarsToAll(canvas.transform));

            // US-041: once the party is on the board, equipped robes (Mage/Wizard) seed the bank
            // with their BattleStartManaOrbs as random-color orbs (respecting the 12-orb cap).
            Scripts.Utilities.GameReady.WhenReady(this, ApplyBattleStartManaOrbs);

            // Per-tick buff effects (Burning damage, Poisoned damage, Wet/Warm countdown).
            if (GetComponent<BuffTickManager>() == null) gameObject.AddComponent<BuffTickManager>();
        }

        /// <summary>Colors a battle-start orb can roll (the five castable colors; not Colorless).</summary>
        private static readonly ManaType[] BattleStartColors =
            { ManaType.White, ManaType.Blue, ManaType.Black, ManaType.Red, ManaType.Green };

        /// <summary>US-041: scans the active party's equipped gear and adds each item's
        /// <see cref="ItemDefinition.BattleStartManaOrbs"/> as random-color orbs to the team bank,
        /// stacking across wearers and clamped to the 12-orb capacity (§3.1.4). Public so the Debug
        /// Window can re-trigger it for testing.</summary>
        public void ApplyBattleStartManaOrbs()
        {
            var save = ProfileHelper.CurrentProfile?.CurrentSave;
            if (save?.Equipment?.Heroes == null || save.Party?.Members == null) return;

            int requested = 0;
            foreach (var member in save.Party.Members)
            {
                if (member == null) continue;
                HeroEquipmentSave heroSave = null;
                foreach (var h in save.Equipment.Heroes)
                    if (h != null && h.CharacterClass == member.CharacterClass) { heroSave = h; break; }
                if (heroSave == null) continue;

                foreach (var id in new[] { heroSave.WeaponId, heroSave.ArmorId, heroSave.Relic1Id, heroSave.Relic2Id, heroSave.Relic3Id })
                {
                    if (string.IsNullOrEmpty(id)) continue;
                    var item = ItemLibrary.Get(id);
                    if (item != null) requested += item.BattleStartManaOrbs;
                }
            }

            if (requested <= 0) return;

            int added = 0;
            for (int i = 0; i < requested && !Bank.IsFull; i++)
                added += Bank.Add(BattleStartColors[RNG.Int(0, BattleStartColors.Length - 1)], 1);

            if (added > 0)
                Debug.Log($"[ManaPool] Battle-start robes granted +{added} orb(s) (requested {requested}, cap {Bank.Capacity}).");
        }

        private static void AttachDebuffBarsToAll(Transform canvas)
        {
            // `g.Actors` is a nested static class — null-check the All list instead.
            var all = g.Actors.All;
            if (all == null) return;
            foreach (var a in all)
                if (a != null) Scripts.Factories.DebuffIconBarFactory.EnsureAttached(a);
        }

        /// <summary>
        /// PHASE B: the Bank BUTTON is gone. This method still drives <b>auto-skip to next enemy</b>
        /// when <see cref="TurnManager"/> finds remaining time too short for the player to act, so
        /// we keep the timeline-advance + enemy-turn-queue flow. The mana grant is removed.
        /// </summary>
        public void OnBankButtonClicked()
        {
            if (!g.TurnManager.IsHeroTurn) return;
            if (g.TimelineBar == null) return; // teardown guard (scene unload / restart mid-frame)

            var (arrivingEnemy, secondsSkipped) = g.TimelineBar.GetNextBankTarget();
            if (arrivingEnemy == null) return;

            g.TimelineBar.AdvanceToNextTrigger(arrivingEnemy, secondsSkipped);
            g.InputManager.InputMode = InputMode.None;
            g.SequenceManager.Add(new Scripts.Sequences.TimelineTriggerSequence(arrivingEnemy));
            g.SequenceManager.Execute();
        }

        /// <summary>
        /// PHASE B: Spend mana for an ability. Hero spends route through the orb line —
        /// <c>orbs = ceil(cost / ManaPerOrb)</c>, spent as Blue (V1: all magic costs Blue). Returns
        /// true if successful, false if insufficient orbs. Enemy mana keeps the legacy float path.
        /// </summary>
        public bool Spend(Team team, float cost)
        {
            cost = Mathf.Max(0f, cost);

            if (team == Team.Hero)
            {
                int orbsNeeded = cost <= 0f ? 0 : Mathf.Max(1, Mathf.CeilToInt(cost / ManaPerOrb));
                if (Bank.Count(ManaType.Blue) < orbsNeeded) return false;
                var recipe = new ManaRecipe("Cast", new ManaCost(ManaType.Blue, orbsNeeded));
                if (!Bank.Spend(recipe)) return false;
            }
            else
            {
                if (enemyMana < cost) return false;
                enemyMana -= cost;
            }

            g.AbilityButtonManager?.UpdateAllInteractables(heroMana);
            return true;
        }

        /// <summary>
        /// PHASE B: Add mana directly (special effects, pickups, etc.). Hero adds become Blue orbs;
        /// enemy adds remain on the legacy float.
        /// </summary>
        public void AddMana(Team team, float amount)
        {
            amount = Mathf.Max(0f, amount);

            if (team == Team.Hero)
            {
                int orbs = amount <= 0f ? 0 : Mathf.Max(1, Mathf.CeilToInt(amount / ManaPerOrb));
                Bank.Add(ManaType.Blue, orbs);
            }
            else
            {
                enemyMana = Mathf.Clamp(enemyMana + amount, 0f, maxMana);
            }

            g.AbilityButtonManager?.UpdateAllInteractables(heroMana);
        }

        // ── PHASE B no-op shims (kept because live callers still reference them) ──

        /// <summary>PHASE B: legacy fill-bar UI is gone; no work required. Kept for back-compat (SelectionManager, ForceHeroDropSequence call this).</summary>
        public void RefreshUI() { }

        /// <summary>PHASE B: legacy turn-start hook; previously refreshed the fill bar. No-op now (TurnManager still calls this).</summary>
        public void OnTurnStarted(Team team) { }
    }
}
