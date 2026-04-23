using UnityEngine;
using Scripts.Canvas;
using Scripts.Data.Actor;
using Scripts.Data.Bounties;
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
using Scripts.Sequences;
using Scripts.Serialization;
using Scripts.Utilities;

namespace Scripts.Helpers
{
    /// <summary>
    /// BOUNTYHELPER - Static helpers for the single-slot bounty contract system.
    /// <para>PURPOSE: Accept / abandon / complete / record-kill operations that read and
    /// write <see cref="BountySaveData"/> on the current save.</para>
    /// <para>RELATED FILES: BountyLibrary.cs, BountySaveData (Profile.cs), BountySection.cs</para>
    /// </summary>
    public static class BountyHelper
    {
        public static BountyDefinition ActiveBounty()
        {
            var save = ProfileHelper.CurrentProfile?.CurrentSave;
            if (save?.Bounty == null || string.IsNullOrEmpty(save.Bounty.ActiveBountyId)) return null;
            return BountyLibrary.Get(save.Bounty.ActiveBountyId);
        }

        public static int ActiveProgress()
        {
            var save = ProfileHelper.CurrentProfile?.CurrentSave;
            return save?.Bounty != null ? save.Bounty.Progress : 0;
        }

        public static bool HasActive()
        {
            return ActiveBounty() != null;
        }

        public static bool IsComplete()
        {
            var b = ActiveBounty();
            return b != null && ActiveProgress() >= b.RequiredCount;
        }

        public static bool Accept(string bountyId)
        {
            var def = BountyLibrary.Get(bountyId);
            if (def == null) return false;
            var save = ProfileHelper.CurrentProfile?.CurrentSave;
            if (save == null) return false;
            if (save.Bounty == null) save.Bounty = new BountySaveData();
            save.Bounty.ActiveBountyId = def.Id;
            save.Bounty.Progress = 0;
            return true;
        }

        public static void Abandon()
        {
            var save = ProfileHelper.CurrentProfile?.CurrentSave;
            if (save?.Bounty == null) return;
            save.Bounty.ActiveBountyId = "";
            save.Bounty.Progress = 0;
        }

        /// <summary>Increments progress on the active bounty if this killed actor matches the contract.</summary>
        public static void RecordKill(CharacterClass killedClass)
        {
            var def = ActiveBounty();
            if (def == null) return;
            if (def.TargetClass != killedClass) return;
            var save = ProfileHelper.CurrentProfile.CurrentSave;
            if (save.Bounty.Progress >= def.RequiredCount) return;
            save.Bounty.Progress += 1;
        }

        /// <summary>
        /// Claims the reward for the active bounty if it's complete. Credits gold to inventory
        /// and grants the reward item. Returns true if a reward was delivered.
        /// </summary>
        public static bool ClaimReward(PlayerInventory inventory)
        {
            var def = ActiveBounty();
            if (def == null || !IsComplete() || inventory == null) return false;

            inventory.Gold += def.RewardGold;
            if (!string.IsNullOrEmpty(def.RewardItemId))
            {
                var itemDef = ItemLibrary.Get(def.RewardItemId);
                if (itemDef != null) inventory.Add(itemDef, Mathf.Max(1, def.RewardItemCount));
            }

            Abandon();
            return true;
        }
    }
}
