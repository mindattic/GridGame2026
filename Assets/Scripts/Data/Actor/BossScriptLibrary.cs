using System.Collections.Generic;
using Scripts.Helpers;
using Scripts.Instances.Actor;
using Scripts.Models;
using Scripts.Sequences;

namespace Scripts.Data.Actor
{
    /// <summary>
    /// BOSSSCRIPTLIBRARY - Per-class boss phase scripts (US-083). Data-driven (Legion panel: the
    /// project's library idiom), keyed by <see cref="CharacterClass"/>; the engine
    /// (<see cref="Scripts.Services.BossPhaseRunner"/>) reads the current phase by HP and fires
    /// transitions. A class with no entry is not boss-scripted (normal enemy turn).
    ///
    /// <para>Phases are stored threshold-DESCENDING (opening phase 1.0 first). Add a boss by
    /// registering an ordered list in <see cref="Ensure"/>.</para>
    ///
    /// RELATED FILES: BossPhase.cs, BossPhaseRunner.cs, BossPhaseTransitionSequence.cs.
    /// </summary>
    public static class BossScriptLibrary
    {
        private static Dictionary<CharacterClass, List<BossPhase>> scripts;
        private static bool initialized;

        private static void Ensure()
        {
            if (initialized) return;
            initialized = true;
            scripts = new Dictionary<CharacterClass, List<BossPhase>>();

            // Cyclops (the 2×2 boss): two phases. Below half HP it ENRAGES — a one-time transition
            // that speeds it up (Quicken its own timeline icon, US-028) behind an "ENRAGED!" banner.
            // Demonstrates the framework end-to-end with a non-caster boss via the transition slot.
            scripts[CharacterClass.Cyclops00] = new List<BossPhase>
            {
                new BossPhase { Name = "Cyclops", HpThreshold = 1f },
                new BossPhase
                {
                    Name = "ENRAGED!",
                    HpThreshold = 0.5f,
                    Transition = boss => new BossPhaseTransitionSequence(boss, "ENRAGED!", hastenU: 0.35f)
                },
            };
        }

        /// <summary>The ordered phase list for <paramref name="cls"/>, or null if not boss-scripted.</summary>
        public static List<BossPhase> For(CharacterClass cls)
        {
            Ensure();
            return scripts.TryGetValue(cls, out var phases) ? phases : null;
        }

        /// <summary>True if this actor has an authored boss script.</summary>
        public static bool IsScripted(ActorInstance actor) => actor != null && For(actor.characterClass) != null;
    }
}
