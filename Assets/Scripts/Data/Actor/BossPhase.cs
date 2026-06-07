using System;
using Scripts.Instances.Actor;
using Scripts.Sequences;

namespace Scripts.Data.Actor
{
    /// <summary>
    /// BOSSPHASE - One authored phase of a boss fight (US-083).
    ///
    /// <para>A boss's script is an ordered list of phases (thresholds DESCENDING; the opening phase
    /// has <see cref="HpThreshold"/> = 1). The boss is "in" the deepest phase whose threshold its
    /// current HP-fraction has dropped to or below; each newly-entered phase fires its one-time
    /// <see cref="Transition"/> once.</para>
    ///
    /// <para>The phase carries declarative behavior knobs (<see cref="PrefersCharge"/>) that the
    /// enemy-turn flow reads, AND a <see cref="Transition"/> — an arbitrary <see cref="SequenceEvent"/>
    /// factory. The transition slot IS the per-boss bespoke-code seam (Legion panel synthesis): routine
    /// phases stay declarative; anything special a boss must do on entering a phase plugs in here as a
    /// real sequence, without per-boss subclasses.</para>
    ///
    /// RELATED FILES: BossScriptLibrary.cs, Services/BossPhaseRunner.cs,
    /// Sequences/BossPhaseTransitionSequence.cs, EnemyTakeTurnSequence.cs.
    /// </summary>
    public sealed class BossPhase
    {
        /// <summary>Display name surfaced by the transition banner (e.g. "ENRAGED!").</summary>
        public string Name;

        /// <summary>Entered when the boss's HP fraction (HP/MaxHP) is ≤ this. Opening phase = 1f.</summary>
        public float HpThreshold = 1f;

        /// <summary>Behavior knob: in this phase a caster boss telegraphs a charge even at melee range
        /// (read by EnemyTakeTurnSequence). No effect on a non-caster boss.</summary>
        public bool PrefersCharge;

        /// <summary>One-time effect queued when this phase is ENTERED (null = none). Built per-boss as
        /// any SequenceEvent — the bespoke-behavior seam.</summary>
        public Func<ActorInstance, SequenceEvent> Transition;
    }
}
