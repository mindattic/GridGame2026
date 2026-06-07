using Scripts.Helpers;
using System.Collections;
using UnityEngine;
using g = Scripts.Helpers.GameHelper;
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
using Scripts.Managers;
using Scripts.Models;
using Scripts.Models.Actor;
using Scripts.Overworld;
using Scripts.Serialization;
using Scripts.Utilities;

namespace Scripts.Sequences
{
    /// <summary>
    /// ENEMYCHARGESEQUENCE - A caster enemy telegraphs a spell on the timeline (US-026).
    ///
    /// <para>PURPOSE: Enemies were melee-only. A <b>Caster</b> (tagged <see cref="ActorTag.Magic"/>)
    /// that <see cref="Scripts.Services.EnemyPlanner.PlanCast"/> elects to charge spawns a colored
    /// cast-icon on the SHARED timeline (reusing the team-agnostic <see cref="TimelineBarInstance.SpawnSpellIcon"/>).
    /// The icon loads left→right over its cast time and, on reaching the trigger (u=1), resolves into a
    /// <see cref="MagicAttackSequence"/> against the target — the SAME third-state cast resolution
    /// (<c>BeginCastResolution</c> → effect → <c>EndCastResolution</c>) hero casts use, but WITHOUT an
    /// <c>EndTurnSequence</c> (the enemy's turn already ended; the cast resolves later on the shared
    /// clock, typically during the next hero window).</para>
    ///
    /// <para>Queued by <see cref="EnemyTakeTurnSequence"/> in place of the move/attack chain. Interrupting
    /// the charge uses the same cast-stagger model as hero casts (US-024); US-027 mints a charge-color
    /// orb at the cancel.</para>
    ///
    /// RELATED FILES: EnemyTakeTurnSequence.cs, EnemyPlanner.cs (PlanCast), EnemyChargeCatalog.cs,
    /// TimelineBarInstance.cs (SpawnSpellIcon), MagicAttackSequence.cs, AbilityManager.cs (TryGetMagicEffect).
    /// </summary>
    public sealed class EnemyChargeSequence : SequenceEvent
    {
        private readonly ActorInstance enemy;
        private readonly ActorInstance target;
        private readonly Ability ability;

        public EnemyChargeSequence(ActorInstance enemy, ActorInstance target, Ability ability)
        {
            this.enemy = enemy;
            this.target = target;
            this.ability = ability;
        }

        /// <summary>Coroutine that spawns the charge cast-icon; resolution is deferred to its u=1 closure.</summary>
        public override IEnumerator ProcessRoutine()
        {
            if (enemy == null || !enemy.IsPlaying || target == null || ability == null)
                yield break;

            // Never stack a second charge while one is already in flight for this enemy.
            if (g.TimelineBar == null || g.TimelineBar.GetSpellIconFor(enemy) != null)
                yield break;

            g.ActionTitle?.Show($"{enemy.characterClass} is charging {ability.name}!");
            g.AudioManager?.Play("Charge"); // rising chiptune telegraph

            var state = new CastingState(enemy, ability, target);

            // Red telegraph arc: caster → target, so the player can see who's about to get hit.
            var arcKey = "enemycast:" + enemy.name;
            g.TargetLineManager?.Show2D(arcKey,
                TargetPoint.Actor(enemy),
                TargetPoint.Actor(target),
                Color.red);

            g.TimelineBar.SpawnSpellIcon(state,
                onComplete: spellIcon =>
                {
                    // Cast reached the trigger — input is suspended (BeginCastResolution, set by the
                    // icon's onReached). Resolve into a magic attack, clean up, then return control.
                    // No EndTurnSequence here: this resolves on the shared clock, not as a turn.
                    if (Scripts.Managers.AbilityManager.TryGetMagicEffect(ability.Effect, out var element, out var vfxKey))
                        g.SequenceManager.Add(new MagicAttackSequence(enemy, target, element, vfxKey));
                    g.SequenceManager.Add(new DeathSequence());
                    g.SequenceManager.Add(new SequenceCallback(() =>
                    {
                        spellIcon?.FadeAndDestroy(0.25f);
                        g.TargetLineManager?.Hide(arcKey);
                        g.TurnManager?.EndCastResolution();
                    }));
                    g.SequenceManager.Execute();
                },
                onInterrupted: () =>
                {
                    // US-027 will mint the charge-color orb here; for now just drop the arc with the icon.
                    g.TargetLineManager?.Hide(arcKey);
                });

            yield return Wait.None();
        }
    }
}
