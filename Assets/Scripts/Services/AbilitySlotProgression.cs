using UnityEngine;
using Scripts.Helpers;

namespace Scripts.Services
{
    /// <summary>
    /// ABILITYSLOTPROGRESSION - Pure unlock rules for the ability bar's 5 slots (US-143 / GG-A6).
    ///
    /// <para>PURPOSE: The bar starts with 2 usable slots; campaign progress unlocks the rest —
    /// one per gate stage — up to the hard max of 5 (one clear button each, per the owner's
    /// combat-UX spec). Gates key off <c>StageSaveData.HighestClearedStageIndex</c>, the same
    /// marker StageSelect unlock gating uses, so the two progressions always agree.</para>
    ///
    /// <para>GATES: fresh save (-1) = 2 slots; clearing stage index 0 → 3; index 2 (first theme
    /// done) → 4; index 5 (second theme done) → 5. Designer-tunable via
    /// <see cref="UnlockThresholds"/>.</para>
    ///
    /// <para>RELATED FILES: AbilityBar.cs (combat gate + render), AbilitiesManager.cs (loadout
    /// scene gate + render), CampaignStages.cs (the index this keys off).</para>
    /// </summary>
    public static class AbilitySlotProgression
    {
        public const int StartingSlots = 2;
        public const int MaxSlots = 5;

        /// <summary>HighestClearedStageIndex values that each unlock one more slot, in order.</summary>
        public static readonly int[] UnlockThresholds = { 0, 2, 5 };

        /// <summary>Usable slots for a given campaign progression marker.</summary>
        public static int UnlockedSlots(int highestClearedStageIndex)
        {
            int slots = StartingSlots;
            foreach (var threshold in UnlockThresholds)
                if (highestClearedStageIndex >= threshold) slots++;
            return Mathf.Min(MaxSlots, slots);
        }

        /// <summary>Usable slots for the live save (fresh/no save = the starting count).</summary>
        public static int UnlockedSlotsForCurrentSave()
            => UnlockedSlots(ProfileHelper.CurrentProfile?.CurrentSave?.Stage?.HighestClearedStageIndex ?? -1);

        /// <summary>The stage-clear count still needed to open <paramref name="slotIndex"/>
        /// (0-based), for "Locked — clear stage N" style labels. -1 when already unlocked or
        /// beyond MaxSlots.</summary>
        public static int GateForSlot(int slotIndex)
        {
            if (slotIndex < StartingSlots || slotIndex >= MaxSlots) return -1;
            return UnlockThresholds[slotIndex - StartingSlots];
        }
    }
}
