using UnityEngine;
using Scripts.Helpers;
using Scripts.Instances.Actor;
using Scripts.Libraries;
using Scripts.Models;

namespace Scripts.Data.Actor
{
    /// <summary>One armed trap's payload (US-139): flat damage plus an optional status.</summary>
    public class TrapDefinition
    {
        public string DisplayName;
        public float Damage;
        /// <summary>Buff id from <see cref="Scripts.Data.Buffs"/> applied on trigger (null = none).</summary>
        public string BuffId;
        /// <summary>The class that armed it (kill credit / feed lines).</summary>
        public CharacterClass Owner;
    }

    /// <summary>
    /// TRAPCATALOG - Which enemies lay traps, and what those traps do (US-139 / GG-A5).
    ///
    /// <para>PURPOSE: pure data + rules (no scene access) so EnemyTakeTurnSequence can branch and
    /// tests can assert. The first trap-layer archetype is the SCORPION (desert stages): a
    /// venom snare — modest damage plus Poisoned. Trap-laying rolls a chance per turn and only
    /// when no hero is adjacent (a cornered scorpion stings instead).</para>
    ///
    /// <para>RELATED FILES: TrapManager.cs, PlaceTrapSequence.cs, EnemyTakeTurnSequence.cs.</para>
    /// </summary>
    public static class TrapCatalog
    {
        /// <summary>Chance per eligible turn that a trap-layer spends its turn arming (RNG-rolled;
        /// deterministic under RNG.Seed in tests).</summary>
        public const float LayChancePerTurn = 0.45f;

        /// <summary>True when this enemy class lays traps.</summary>
        public static bool IsTrapLayer(ActorInstance enemy)
        {
            if (enemy == null || enemy.team != Team.Enemy) return false;
            switch (enemy.characterClass)
            {
                case CharacterClass.Scorpion:
                    return true;
                default:
                    return false;
            }
        }

        /// <summary>The trap this enemy arms (null for non-layers).</summary>
        public static TrapDefinition For(ActorInstance enemy)
        {
            if (!IsTrapLayer(enemy)) return null;
            return new TrapDefinition
            {
                DisplayName = "Venom Snare",
                Damage = 6f,
                BuffId = Buffs.Poisoned.Id,
                Owner = enemy.characterClass,
            };
        }
    }
}
