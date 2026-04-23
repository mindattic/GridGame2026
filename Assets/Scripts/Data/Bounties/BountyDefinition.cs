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

namespace Scripts.Data.Bounties
{
    /// <summary>
    /// BOUNTYDEFINITION - Static data for a hunter's bounty.
    /// <para>PURPOSE: A bounty is a "kill N of CharacterClass X in biome Y" contract. The player
    /// can accept one bounty at a time; progress is tracked across battles and the reward is
    /// claimed at the Bounty board once the required count is met.</para>
    /// <para>RELATED FILES: BountyLibrary.cs, BountySection.cs, BountySaveData (Profile.cs)</para>
    /// </summary>
    public class BountyDefinition
    {
        public string Id;
        public string DisplayName;
        public string Description;
        public Biome Biome;
        public CharacterClass TargetClass;
        public int RequiredCount;
        public int RewardGold;

        /// <summary>Optional reward item; empty means gold only.</summary>
        public string RewardItemId;
        public int RewardItemCount = 1;
    }
}
