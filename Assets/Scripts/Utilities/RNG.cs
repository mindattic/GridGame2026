using Scripts.Helpers;
using Scripts.Helpers;
using Scripts.Libraries;
using Scripts.Instances.Actor;
using System;
using System.Collections.Generic;
using System.Linq;
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
using Scripts.Instances.Board;
using Scripts.Instances.SynergyLine;
using Scripts.Inventory;
using Scripts.Managers;
using Scripts.Models;
using Scripts.Models.Actor;
using Scripts.Overworld;
using Scripts.Sequences;
using Scripts.Serialization;

namespace Scripts.Utilities
{
/// <summary>
/// RNG - Centralized random number generation and selection utilities.
/// 
/// PURPOSE:
/// Provides unified random helpers for gameplay including random
/// actors, tiles, locations, directions, colors, and weighted choices.
/// 
/// RANDOM SELECTION:
/// - Pick&lt;T&gt;(items): Random element from collection
/// - Hero: Random playing hero actor
/// - Enemy: Random playing enemy actor
/// - Tile: Random tile on board
/// - UnoccupiedTile: Random empty tile
/// 
/// NUMERIC RANGES:
/// - Int(min, max): Random integer inclusive
/// - Float(min, max): Random float inclusive
/// - Percent: Random 0.0 to 1.0
/// - Bool: Random true/false
/// 
/// GAME-SPECIFIC:
/// - Location: Random board coordinate
/// - Direction: Random cardinal direction
/// - Stage(map): Random stage from map
/// - CharacterClass: Random hero class
/// 
/// WEIGHTED SELECTION:
/// - WeightedChoice: Select based on weight distribution
/// 
/// USAGE:
/// ```csharp
/// var enemy = RNG.Enemy;
/// var damage = RNG.Int(10, 20);
/// var crit = RNG.Percent &lt; 0.1f;
/// var dir = RNG.Direction;
/// ```
/// 
/// THREAD SAFETY:
/// Uses [ThreadStatic] for thread-local random instances.
/// </summary>
static class RNG
{
    [ThreadStatic] public static System.Random rng = new System.Random();

    #region Collection Selection

    /// <summary>
    /// Returns one uniformly random element from items.
    /// If items is null or empty, returns defaultValue.
    /// </summary>
    public static T Pick<T>(IReadOnlyList<T> items)
    {
        if (items == null || items.Count < 1) return default;
        return items[Int(0, items.Count - 1)];
    }

    #endregion

    #region Actor Selection

    /// <summary>Random hero that is currently playing.</summary>
    public static ActorInstance Hero => g.Actors.Heroes.Where(x => x.IsPlaying).Shuffle().First();

    /// <summary>Random enemy that is currently playing.</summary>
    public static ActorInstance Enemy => g.Actors.Enemies.Where(x => x.IsPlaying).Shuffle().First();

    #endregion

    #region Tile/Location Selection

    /// <summary>Random tile from all tiles.</summary>
    public static TileInstance Tile => g.Tiles.Shuffle().First();

    /// <summary>Random board location within inclusive 1..columnCount and 1..rowCount.</summary>
    public static Vector2Int Location => new Vector2Int(Int(1, g.Board.columnCount), Int(1, g.Board.rowCount));

        /// <summary>Random unoccupied tile if available, otherwise null.</summary>
        public static TileInstance UnoccupiedTile => g.Tiles.Where(x => !x.IsOccupied).Shuffle().FirstOrDefault();

        /// <summary>Random unoccupied location or Nowhere if none available.</summary>
        public static Vector2Int UnoccupiedLocation => UnoccupiedTile == null ? LocationHelper.Nowhere : UnoccupiedTile.location;

        /// <summary>
        /// Random unoccupied interior location that is not on the border; Nowhere if none found.
        /// </summary>
        public static Vector2Int UnoccupiedInteriorLocation
        {
            get
            {
                var tile = g.Tiles
                    .Where(t =>
                        !t.IsOccupied &&
                        t.location.x > 1 && t.location.x < g.Board.columnCount &&
                        t.location.y > 1 && t.location.y < g.Board.rowCount
                    )
                    .Shuffle()
                    .FirstOrDefault();

                return tile == null ? LocationHelper.Nowhere : tile.location;
            }
        }

        #endregion

        #region Numeric Ranges

        /// <summary>Random integer in inclusive range [min, max].</summary>
        public static int Int(int min, int max) => rng.Next(min, max + 1);

        /// <summary>Random float in range [min, max).</summary>
        public static float Float(float min = 0f, float max = 1f) => (float)rng.NextDouble() * (max - min) + min;

        /// <summary>Random percentage in [0, 1).</summary>
        public static float Percent => (float)rng.NextDouble();

        /// <summary>
        /// Random offset in [-amount, +amount] using two independent draws.
        /// </summary>
    public static float Range(float amount) => (-amount * Percent) + (amount * Percent);

    /// <summary>
    /// Returns a random float in [minInclusive, maxInclusive] using two independent draws.
    /// </summary>
    public static float Range(float minInclusive, float maxInclusive)
    {
        float lower = minInclusive * Percent;
        float upper = maxInclusive * Percent;
        return lower + upper;
    }

    /// <summary>Random boolean.</summary>
    public static bool Boolean => Int(1, 2) == 1;

    #endregion

    #region Direction Selection

    /// <summary>Random cardinal direction.</summary>
    public static Direction AdjacentDirection
    {
        get
        {
            var result = Int(1, 4);
            return result switch
            {
                1 => Direction.North,
                2 => Direction.East,
                3 => Direction.South,
                _ => Direction.West,
            };
        }
    }

    /// <summary>Random 8-way direction.</summary>
    public static Direction Direction
    {
        get
        {
            var result = Int(1, 8);
            return result switch
            {
                1 => Direction.North,
                2 => Direction.NorthEast,
                3 => Direction.East,
                4 => Direction.SouthEast,
                5 => Direction.South,
                6 => Direction.SouthWest,
                7 => Direction.West,
                _ => Direction.NorthWest
            };
        }
    }

    #endregion

    #region Color and Misc

    /// <summary>Random opaque color.</summary>
    public static Color Color => new Color(Float(), Float(), Float(), 1f);

    /// <summary>
    /// Random attack strategy. Preserves commented reference code for future weighted logic.
    /// </summary>
    public static AttackStrategy Strategy(params int[] ratios)
    {
        //int sum = Int(0, ratios.Sum());

        //int ratio0 = ratios[0];
        //int ratio1 = ratio0 + ratios[1];
        //int ratio2 = ratio1 + ratios[2];
        //int ratio3 = ratio2 + ratios[3];
        //int ratio4 = ratio3 + ratios[4];
        //int ratio5 = ratio4 + ratios[5];

        //int attackResult = Int(0, sum);

        //if ((attackResult -= ratio0) < 0) return Strategy.AttackClosest;

        //{
        //   do_something1();
        //}
        //else if ((s -= RATIO_CHANCE_B) < 0) //Test for B
        //{
        //   do_something2();
        //}
        ////... etc
        //else //No need for final if statement
        //{
        //   do_somethingN();
        //}

        //TODO: SpawnActor in weighted value so some attackResults are more common that others...

        //int attackResult = Int(0, ratios.Sum());

        /*
        int RATIO_CHANCE_A = 10;
        int RATIO_CHANCE_B = 30;
        int RATIO_CHANCE_C = 60;    
        int RATIO_TOTAL = RATIO_CHANCE_A + RATIO_CHANCE_B + RATIO_CHANCE_C;

        RNG random = new RNG();
        int s = random.None(0, RATIO_TOTAL);

        if ((s -= RATIO_CHANCE_A) < 0) //Test for A
        { 
             do_something1();
        } 
        else if ((s -= RATIO_CHANCE_B) < 0) //Test for B
        { 
             do_something2();
        }
        //... etc
        else //No need for final if statement
        { 
             do_somethingN();
        }
        */

        //var attackResult = Int(1, 5);
        //return attackResult switch
        //{
        //   1 => Strategy.MoveAnywhere,
        //   2 => Strategy.AttackClosest,
        //   3 => Strategy.AttackWeakest,
        //   4 => Strategy.AttackStrongest,
        //   5 => Strategy.AttackRandom,
        //   Attack => Strategy.MoveAnywhere,
        //};

        var result = Int(1, 2);
        return result switch
        {
            1 => AttackStrategy.AttackClosest,
            2 => AttackStrategy.AttackRandom,
            _ => AttackStrategy.AttackClosest,
        };
    }

    /// <summary>
    /// Random enum value of type T.
    /// </summary>
    public static T EnumValue<T>() where T : Enum
    {
        Array values = Enum.GetValues(typeof(T));
        return (T)values.GetValue(Int(0, values.Length - 1));
    }

    /// <summary>
    /// Random Weapon type.
    /// </summary>
    public static WeaponType WeaponType()
    {
        return EnumValue<WeaponType>();
    }

    /// <summary>
    /// Random shake intensity level: High, Medium, or Low.
    /// </summary>
    public static float ShakeIntensityLevel()
    {
        int choice = Int(1, 3);
        return choice switch
        {
            1 => ShakeIntensity.High,
            2 => ShakeIntensity.Medium,
            3 => ShakeIntensity.Low,
            _ => ShakeIntensity.Low
        };
    }

    /// <summary>
    /// Random background sprite from the SpriteRepo.
    /// </summary>
    public static Sprite Background()
    {
        var keys = SpriteLibrary.Backgrounds.Keys.ToList();
        string key = keys[Int(0, keys.Count - 1)];
        return SpriteLibrary.Backgrounds[key];
    }

        /// <summary>Random hit type.</summary>
        public static HitOutcome HitType()
        {
            return EnumValue<HitOutcome>();
        }

        #endregion

        #region Stage Selection

        public static string Stage(Map map)
        {
            return StageLibrary.Stages.Keys.Where(x => x.StartsWith(map.ToString())).Shuffle().First();
        }

        public static string Stage(string mapName)
        {
            Map map = (Map)Enum.Parse(typeof(Map), mapName);
            return StageLibrary.Stages.Keys.Where(x => x.StartsWith(map.ToString())).Shuffle().First();
        }

        #endregion
    }

}