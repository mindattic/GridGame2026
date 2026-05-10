using System.Collections.Generic;
using Scripts.Helpers;

namespace Scripts.Libraries
{
    /// <summary>
    /// ENEMYBIOMEMAP - Centralized lookup of which biome each enemy belongs to.
    /// <para>PURPOSE: Tags every enemy CharacterClass with its primary campaign biome.
    /// Used by StageLibrary when authoring directed stage compositions and (in slice 10+)
    /// by enemy-tactics AI to bias spawning by biome. Centralized here instead of on
    /// individual ActorData entries so a slice 9 change touches one file, not 25+.</para>
    /// <para>Enemies that do not appear in the campaign return <see cref="Biome.None"/> —
    /// callers should treat that as "not-campaign-eligible" rather than as a bug.</para>
    /// <para>RELATED FILES: CampaignStages.cs, StageLibrary.cs, ActorLibrary.cs</para>
    /// </summary>
    public static class EnemyBiomeMap
    {
        private static readonly Dictionary<CharacterClass, Biome> map = new()
        {
            // GreenValley — easy starter critters (Slimes, Wolves, Bats, Frogs, light forest)
            { CharacterClass.Slime00,    Biome.GreenValley },
            { CharacterClass.Slime01,    Biome.GreenValley },
            { CharacterClass.Slime02,    Biome.GreenValley },
            { CharacterClass.Slime03,    Biome.GreenValley },
            { CharacterClass.Wolf00,     Biome.GreenValley },
            { CharacterClass.Wolf01,     Biome.GreenValley },
            { CharacterClass.Wolf02,     Biome.GreenValley },
            { CharacterClass.Wolf03,     Biome.GreenValley },
            { CharacterClass.Bat00,      Biome.GreenValley },
            { CharacterClass.Bat01,      Biome.GreenValley },

            // Desert — arid raiders + creatures
            { CharacterClass.Scorpion,   Biome.Desert },
            { CharacterClass.SandMaw,    Biome.Desert },
            { CharacterClass.Vulture,    Biome.Desert },
            { CharacterClass.Soldier00,  Biome.Desert },
            { CharacterClass.Soldier01,  Biome.Desert },
            { CharacterClass.Soldier02,  Biome.Desert },
            { CharacterClass.Soldier03,  Biome.Desert },
            { CharacterClass.Captain,    Biome.Desert },

            // Swamp — marsh fauna + hags
            { CharacterClass.Lurker00,         Biome.Swamp },
            { CharacterClass.Lurker01,         Biome.Swamp },
            { CharacterClass.Lurker02,         Biome.Swamp },
            { CharacterClass.MarshShambler00,  Biome.Swamp },
            { CharacterClass.MarshShambler01,  Biome.Swamp },
            { CharacterClass.MarshShambler03,  Biome.Swamp },
            { CharacterClass.SwampMistress00,  Biome.Swamp },
            { CharacterClass.Hag00,            Biome.Swamp },
            { CharacterClass.Hag01,            Biome.Swamp },
            { CharacterClass.Frog00,           Biome.Swamp },
            { CharacterClass.Frog01,           Biome.Swamp },
            { CharacterClass.Toad00,           Biome.Swamp },
            { CharacterClass.Naga00,           Biome.Swamp },

            // Cave — bruisers + cold-dwelling beasts
            { CharacterClass.Cyclops00,      Biome.Cave },
            { CharacterClass.Cyclops01,      Biome.Cave },
            { CharacterClass.Cyclops02,      Biome.Cave },
            { CharacterClass.MountainTroll,  Biome.Cave },
            { CharacterClass.Yeti,           Biome.Cave },
            { CharacterClass.IceMauler,      Biome.Cave },
            { CharacterClass.GoblinThug00,   Biome.Cave },
            { CharacterClass.Skelepede00,    Biome.Cave },
            { CharacterClass.Skelepede01,    Biome.Cave },

            // CityRuins — urban undead + boss-tier
            { CharacterClass.Undead00,   Biome.CityRuins },
            { CharacterClass.Undead01,   Biome.CityRuins },
            { CharacterClass.Undead02,   Biome.CityRuins },
            { CharacterClass.Undead04,   Biome.CityRuins },
            { CharacterClass.Ghost,      Biome.CityRuins },
            { CharacterClass.Phantom,    Biome.CityRuins },
            { CharacterClass.Reaper,     Biome.CityRuins },
            { CharacterClass.Vampire,    Biome.CityRuins },
        };

        /// <summary>Returns the campaign biome for the given enemy, or <see cref="Biome.None"/> if untagged.</summary>
        public static Biome BiomeOf(CharacterClass characterClass)
            => map.TryGetValue(characterClass, out var b) ? b : Biome.None;

        /// <summary>True when the given enemy is registered in the campaign biome map.</summary>
        public static bool IsCampaignEnemy(CharacterClass characterClass) => map.ContainsKey(characterClass);

        /// <summary>Returns every enemy registered in the given biome.</summary>
        public static List<CharacterClass> EnemiesIn(Biome biome)
        {
            var list = new List<CharacterClass>();
            foreach (var kv in map)
                if (kv.Value == biome) list.Add(kv.Key);
            return list;
        }
    }
}
