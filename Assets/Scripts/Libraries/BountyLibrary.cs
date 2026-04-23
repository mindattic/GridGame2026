using System.Collections.Generic;
using UnityEngine;
using Scripts.Canvas;
using Scripts.Data.Actor;
using Scripts.Data.Bounties;
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
using Scripts.Managers;
using Scripts.Models;
using Scripts.Models.Actor;
using Scripts.Overworld;
using Scripts.Sequences;
using Scripts.Serialization;
using Scripts.Utilities;

namespace Scripts.Libraries
{
    /// <summary>
    /// BOUNTYLIBRARY - Central registry for all <see cref="BountyDefinition"/> data.
    /// <para>USAGE: <c>BountyLibrary.Get("bounty_wolf_pack")</c> or <c>BountyLibrary.All()</c>.</para>
    /// <para>RELATED FILES: BountyDefinition.cs, BountyData_Hunts.cs, BountySection.cs</para>
    /// </summary>
    public static class BountyLibrary
    {
        private static Dictionary<string, BountyDefinition> bounties = new Dictionary<string, BountyDefinition>();
        private static bool initialized;

        private static void Ensure()
        {
            if (initialized) return;
            initialized = true;

            Register(BountyData_Hunts.SlimeCulling);
            Register(BountyData_Hunts.WolfPack);
            Register(BountyData_Hunts.RestlessDead);
            Register(BountyData_Hunts.CaveDweller);
            Register(BountyData_Hunts.VampireLord);
        }

        private static void Register(BountyDefinition def)
        {
            if (def == null || string.IsNullOrEmpty(def.Id)) return;
            bounties[def.Id] = def;
        }

        public static BountyDefinition Get(string id)
        {
            Ensure();
            if (string.IsNullOrEmpty(id)) return null;
            bounties.TryGetValue(id, out var def);
            return def;
        }

        public static IEnumerable<BountyDefinition> All()
        {
            Ensure();
            return bounties.Values;
        }

        public static IEnumerable<BountyDefinition> ByBiome(Biome biome)
        {
            Ensure();
            var list = new List<BountyDefinition>();
            foreach (var b in bounties.Values)
                if (b.Biome == biome) list.Add(b);
            return list;
        }
    }
}
