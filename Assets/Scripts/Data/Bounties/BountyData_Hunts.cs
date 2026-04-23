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
    /// BOUNTYDATA_HUNTS - Starter bounty catalog, one per biome plus a boss hunt.
    /// <para>PURPOSE: Themed kill quests that steer the player toward specific biomes
    /// and the consumables they should stock beforehand.</para>
    /// </summary>
    public static class BountyData_Hunts
    {
        public static readonly BountyDefinition SlimeCulling = new BountyDefinition
        {
            Id = "bounty_slime_culling",
            DisplayName = "Slime Culling",
            Description = "The villagers are fed up with slimes in the grain stores. Cull five.",
            Biome = Biome.Field,
            TargetClass = CharacterClass.Slime00,
            RequiredCount = 5,
            RewardGold = 200,
            RewardItemId = "healing_potion_basic",
            RewardItemCount = 3,
        };

        public static readonly BountyDefinition WolfPack = new BountyDefinition
        {
            Id = "bounty_wolf_pack",
            DisplayName = "Wolf Pack",
            Description = "A pack has turned bold. Thin the ranks — three wolves should do it.",
            Biome = Biome.Forest,
            TargetClass = CharacterClass.Wolf00,
            RequiredCount = 3,
            RewardGold = 300,
            RewardItemId = "hi_potion",
            RewardItemCount = 2,
        };

        public static readonly BountyDefinition RestlessDead = new BountyDefinition
        {
            Id = "bounty_restless_dead",
            DisplayName = "Restless Dead",
            Description = "Something has stirred the old ruins awake. Put four revenants back to rest.",
            Biome = Biome.Ruins,
            TargetClass = CharacterClass.Undead00,
            RequiredCount = 4,
            RewardGold = 400,
            RewardItemId = "holy_water",
            RewardItemCount = 3,
        };

        public static readonly BountyDefinition CaveDweller = new BountyDefinition
        {
            Id = "bounty_cave_dweller",
            DisplayName = "Cave Dweller",
            Description = "Miners vanished near the cave mouth. Deal with the cyclops holed up inside.",
            Biome = Biome.Cave,
            TargetClass = CharacterClass.Cyclops00,
            RequiredCount = 2,
            RewardGold = 500,
            RewardItemId = "flame_oil",
            RewardItemCount = 3,
        };

        public static readonly BountyDefinition VampireLord = new BountyDefinition
        {
            Id = "bounty_vampire_lord",
            DisplayName = "The Vampire Lord",
            Description = "A named horror sleeps in the deepest crypt. End him before nightfall.",
            Biome = Biome.Boss,
            TargetClass = CharacterClass.Vampire,
            RequiredCount = 1,
            RewardGold = 2000,
            RewardItemId = "phoenix_down",
            RewardItemCount = 1,
        };
    }
}
