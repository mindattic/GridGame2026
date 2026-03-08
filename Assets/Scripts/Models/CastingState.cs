using UnityEngine;
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

namespace Scripts.Models
{
    /// <summary>
    /// CASTINGSTATE - Tracks an actor's current casting progress.
    ///
    /// PURPOSE:
    /// When a hero begins casting a spell or using an item that has a
    /// CastTimeSeconds > 0, this state tracks the elapsed time and
    /// whether the cast was interrupted by enemy damage.
    ///
    /// INTERRUPTION:
    /// If the casting hero is attacked before the cast completes,
    /// the cast is interrupted:
    /// - MP is still consumed (already paid when cast began)
    /// - The spell/item effect does NOT apply
    /// - The item is NOT consumed (for item-backed abilities)
    /// - Combat text shows "Interrupted!"
    ///
    /// TIMELINE VISUALIZATION:
    /// The cast is represented as a colored bar on the timeline that
    /// moves from right to left. When it reaches the trigger point,
    /// the cast completes and the ability fires.
    ///
    /// RELATED FILES:
    /// - Ability.cs: CastTimeSeconds property
    /// - TimelineTag.cs: Cast bar visualization
    /// - EnemyAttackSequence.cs: Triggers interruption
    /// - AbilityManager.cs: Initiates casting
    /// </summary>
    [System.Serializable]
    public class CastingState
    {
        /// <summary>The actor currently casting.</summary>
        public ActorInstance Caster;

        /// <summary>The ability being cast.</summary>
        public Ability Ability;

        /// <summary>Target of the ability (may be null for self-targeting).</summary>
        public ActorInstance Target;

        /// <summary>Total cast time required in seconds.</summary>
        public float TotalCastTime;

        /// <summary>Time elapsed since cast began.</summary>
        public float ElapsedTime;

        /// <summary>True if the cast was interrupted by damage.</summary>
        public bool IsInterrupted;

        /// <summary>True if casting is still in progress.</summary>
        public bool IsCasting => !IsInterrupted && ElapsedTime < TotalCastTime;

        /// <summary>True if casting has completed successfully.</summary>
        public bool IsComplete => !IsInterrupted && ElapsedTime >= TotalCastTime;

        /// <summary>Normalized progress (0..1).</summary>
        public float Progress => TotalCastTime > 0f ? Mathf.Clamp01(ElapsedTime / TotalCastTime) : 1f;

        public CastingState(ActorInstance caster, Ability ability, ActorInstance target)
        {
            Caster = caster;
            Ability = ability;
            Target = target;
            TotalCastTime = ability?.CastTimeSeconds ?? 0f;
            ElapsedTime = 0f;
            IsInterrupted = false;
        }

        /// <summary>Advances the cast timer. Returns true when cast completes.</summary>
        public bool Tick(float deltaTime)
        {
            if (IsInterrupted) return false;
            ElapsedTime += deltaTime;
            return IsComplete;
        }

        /// <summary>Interrupts the cast. Shows combat text at caster position.</summary>
        public void Interrupt()
        {
            if (IsInterrupted || IsComplete) return;
            IsInterrupted = true;

            // Show "Interrupted!" combat text
            var g = Scripts.Helpers.GameHelper.CombatTextManager;
            if (g != null && Caster != null)
            {
                g.Spawn("Interrupted!", Caster.Position, "Miss");
            }

            // Show on ability bar
            var bar = Scripts.Helpers.GameHelper.AbilityBar;
            bar?.Show($"{Caster?.characterClass}'s {Ability?.name} was interrupted!");
        }
    }
}
