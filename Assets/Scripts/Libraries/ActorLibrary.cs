using Scripts.Data.Actor;
using Scripts.Helpers;
using Sentry.Protocol;
using System;
using System.Collections.Generic;
using Unity.Mathematics;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.Analytics;
using UnityEngine.Rendering.VirtualTexturing;
using Scripts.Canvas;
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
    /// ACTORLIBRARY - Central registry of all character data.
    /// 
    /// PURPOSE:
    /// Lazy-loads and caches ActorData for all CharacterClasses.
    /// Provides lookup methods for retrieving character stats,
    /// abilities, and visual data.
    /// 
    /// USAGE:
    /// ```csharp
    /// var paladin = ActorLibrary.Get(CharacterClass.Paladin);
    /// var stats = paladin.BaseStats;
    /// ```
    /// 
    /// DATA SOURCES:
    /// Individual actor data files in Assets/Scripts/Data/Actor/
    /// (e.g., Paladin.cs, Barbarian.cs, etc.)
    /// 
    /// RELATED FILES:
    /// - ActorData.cs: Data structure
    /// - CharacterClass.cs: Character enum
    /// - Data/Actor/*.cs: Individual character definitions
    /// </summary>
    public static class ActorLibrary
    {
        private static Dictionary<CharacterClass, ActorData> actors;
        private static bool isLoaded = false;

        public static Dictionary<CharacterClass, ActorData> Actors
        {
            get
            {
                if (!isLoaded)
                    Load();
                return actors;
            }
        }

        public static ActorData Get(CharacterClass key)
        {
            if (!isLoaded)
                Load();

            return actors != null && actors.TryGetValue(key, out ActorData data) ? data : null;
        }

        public static ActorData Get(string key)
        {
            if (!isLoaded)
                Load();

            if (string.IsNullOrEmpty(key)) return null;
            if (Enum.TryParse<CharacterClass>(key, true, out var cc))
                return Get(cc);
            return null;
        }

        private static void Load()
        {
            if (isLoaded) return;
            actors = new Dictionary<CharacterClass, ActorData>()
            {
                { CharacterClass.Alchemist, Alchemist.Data() },
                { CharacterClass.Barbarian, Barbarian.Data() },
                { CharacterClass.Basher, Basher.Data() },
                { CharacterClass.Bat00, Bat00.Data() },
                { CharacterClass.Bat01, Bat01.Data() },
                { CharacterClass.Bat02, Bat02.Data() },
                { CharacterClass.BlackNinja, BlackNinja.Data() },
                { CharacterClass.BlackWitch, BlackWitch.Data() },
                { CharacterClass.BlueLion, BlueLion.Data() },
                { CharacterClass.BlueNinja, BlueNinja.Data() },
                { CharacterClass.Bruiser, Bruiser.Data() },
                { CharacterClass.Captain, Captain.Data() },
                { CharacterClass.CeramicKnight00, CeramicKnight00.Data() },
                { CharacterClass.CeramicKnight01, CeramicKnight01.Data() },
                { CharacterClass.CeramicKnight02, CeramicKnight02.Data() },
                { CharacterClass.CeramicKnight03, CeramicKnight03.Data() },
                { CharacterClass.CeramicKnight04, CeramicKnight04.Data() },
                { CharacterClass.CeramicKnight05, CeramicKnight05.Data() },
                { CharacterClass.CeramicKnight06, CeramicKnight06.Data() },
                { CharacterClass.ChromaNinja, ChromaNinja.Data() },
                { CharacterClass.Cleric, Cleric.Data() },
                { CharacterClass.Courier, Courier.Data() },
                { CharacterClass.CyberZombie00, CyberZombie00.Data() },
                { CharacterClass.CyberZombie01, CyberZombie01.Data() },
                { CharacterClass.CyberZombie02, CyberZombie02.Data() },
                { CharacterClass.CyberZombie03, CyberZombie03.Data() },
                { CharacterClass.CyberZombie04, CyberZombie04.Data() },
                { CharacterClass.Cyclops00, Cyclops00.Data() },
                { CharacterClass.Cyclops01, Cyclops01.Data() },
                { CharacterClass.Cyclops02, Cyclops02.Data() },
                { CharacterClass.Cyclops03, Cyclops03.Data() },
                { CharacterClass.Cyclops04, Cyclops04.Data() },
                { CharacterClass.Cyclops06, Cyclops06.Data() },
                { CharacterClass.DarkTemplar, DarkTemplar.Data() },
                { CharacterClass.Defender, Defender.Data() },
                { CharacterClass.DemonLord, DemonLord.Data() },
                { CharacterClass.Dervish, Dervish.Data() },
                { CharacterClass.Doctor, Doctor.Data() },
                { CharacterClass.Drifter, Drifter.Data() },
                { CharacterClass.Duelist, Duelist.Data() },
                { CharacterClass.Engineer, Engineer.Data() },
                { CharacterClass.Fencer, Fencer.Data() },
                { CharacterClass.Fighter, Fighter.Data() },
                { CharacterClass.FlyingMonkey, FlyingMonkey.Data() },
                { CharacterClass.Frog00, Frog00.Data() },
                { CharacterClass.Frog01, Frog01.Data() },
                { CharacterClass.Frog02, Frog02.Data() },
                { CharacterClass.Frog03, Frog03.Data() },
                { CharacterClass.Ganger00, Ganger00.Data() },
                { CharacterClass.Ganger01, Ganger01.Data() },
                { CharacterClass.Ganger02, Ganger02.Data() },
                { CharacterClass.Ganger03, Ganger03.Data() },
                { CharacterClass.Ganger04, Ganger04.Data() },
                { CharacterClass.Ganger05, Ganger05.Data() },
                { CharacterClass.Ganger06, Ganger06.Data() },
                { CharacterClass.Ghost, Ghost.Data() },
                { CharacterClass.GoblinThug00, GoblinThug00.Data() },
                { CharacterClass.GreenNinja, GreenNinja.Data() },
                { CharacterClass.Hag00, Hag00.Data() },
                { CharacterClass.Hag01, Hag01.Data() },
                { CharacterClass.Hag02, Hag02.Data() },
                { CharacterClass.Hag03, Hag03.Data() },
                { CharacterClass.Harbinger, Harbinger.Data() },
                { CharacterClass.IceMauler, IceMauler.Data() },
                { CharacterClass.JadeKnight, JadeKnight.Data() },
                { CharacterClass.Knight, Knight.Data() },
                { CharacterClass.Lancer, Lancer.Data() },
                { CharacterClass.Lurker00, Lurker00.Data() },
                { CharacterClass.Lurker01, Lurker01.Data() },
                { CharacterClass.Lurker02, Lurker02.Data() },
                { CharacterClass.Machinist, Machinist.Data() },
                { CharacterClass.Mannequin, Mannequin.Data() },
                { CharacterClass.MarshShambler00, MarshShambler00.Data() },
                { CharacterClass.MarshShambler01, MarshShambler01.Data() },
                { CharacterClass.MarshShambler03, MarshShambler03.Data() },
                { CharacterClass.MartialArtist, MartialArtist.Data() },
                { CharacterClass.MechaArmor00, MechaArmor00.Data() },
                { CharacterClass.MechaArmor01, MechaArmor01.Data() },
                { CharacterClass.MechaArmor02, MechaArmor02.Data() },
                { CharacterClass.Monk, Monk.Data() },
                { CharacterClass.MountainTroll, MountainTroll.Data() },
                { CharacterClass.Myrmidon, Myrmidon.Data() },
                { CharacterClass.Naga00, Naga00.Data() },
                { CharacterClass.NightHunter, NightHunter.Data() },
                { CharacterClass.Odachi, Odachi.Data() },
                { CharacterClass.Oni00, Oni00.Data() },
                { CharacterClass.Oni01, Oni01.Data() },
                { CharacterClass.Oni02, Oni02.Data() },
                { CharacterClass.Operative, Operative.Data() },
                { CharacterClass.Paladin, Paladin.Data() },
                { CharacterClass.PandaGirl, PandaGirl.Data() },
                { CharacterClass.Phantom, Phantom.Data() },
                { CharacterClass.PrizeFighter, PrizeFighter.Data() },
                { CharacterClass.Pugilist, Pugilist.Data() },
                { CharacterClass.PurplePrototype00, PurplePrototype00.Data() },
                { CharacterClass.PurplePrototype01, PurplePrototype01.Data() },
                { CharacterClass.PurplePrototype02, PurplePrototype02.Data() },
                { CharacterClass.PurplePrototype03, PurplePrototype03.Data() },
                { CharacterClass.PurplePrototype04, PurplePrototype04.Data() },
                { CharacterClass.Raider, Raider.Data() },
                { CharacterClass.Reaper, Reaper.Data() },
                { CharacterClass.RedMage, RedMage.Data() },
                { CharacterClass.RedNinja, RedNinja.Data() },
                { CharacterClass.Ripper, Ripper.Data() },
                { CharacterClass.Ritualist, Ritualist.Data() },
                { CharacterClass.Ronin, Ronin.Data() },
                { CharacterClass.Sage, Sage.Data() },
                { CharacterClass.SandMaw, SandMaw.Data() },
                { CharacterClass.Scorpion, Scorpion.Data() },
                { CharacterClass.Sellsword, Sellsword.Data() },
                { CharacterClass.ShieldMaiden, ShieldMaiden.Data() },
                { CharacterClass.Sister, Sister.Data() },
                { CharacterClass.Skelepede00, Skelepede00.Data() },
                { CharacterClass.Skelepede01, Skelepede01.Data() },
                { CharacterClass.Skelepede02, Skelepede02.Data() },
                { CharacterClass.Slasher, Slasher.Data() },
                { CharacterClass.Slime00, Slime00.Data() },
                { CharacterClass.Slime01, Slime01.Data() },
                { CharacterClass.Slime02, Slime02.Data() },
                { CharacterClass.Slime03, Slime03.Data() },
                { CharacterClass.Soldier00, Soldier00.Data() },
                { CharacterClass.Soldier01, Soldier01.Data() },
                { CharacterClass.Soldier02, Soldier02.Data() },
                { CharacterClass.Soldier03, Soldier03.Data() },
                { CharacterClass.Speedster, Speedster.Data() },
                { CharacterClass.SteppinRazor00, SteppinRazor00.Data() },
                { CharacterClass.SteppinRazor01, SteppinRazor01.Data() },
                { CharacterClass.SteppinRazor02, SteppinRazor02.Data() },
                { CharacterClass.SteppinRazor04, SteppinRazor04.Data() },
                { CharacterClass.SteppinRazor05, SteppinRazor05.Data() },
                { CharacterClass.StreetFighter, StreetFighter.Data() },
                { CharacterClass.Striker, Striker.Data() },
                { CharacterClass.SwampMistress00, SwampMistress00.Data() },
                { CharacterClass.SwordMaster, SwordMaster.Data() },
                { CharacterClass.Tank, Tank.Data() },
                { CharacterClass.TechGremlin00, TechGremlin00.Data() },
                { CharacterClass.TechGremlin01, TechGremlin01.Data() },
                { CharacterClass.TechGremlin02, TechGremlin02.Data() },
                { CharacterClass.Technician, Technician.Data() },
                { CharacterClass.Templar00, Templar00.Data() },
                { CharacterClass.Templar01, Templar01.Data() },
                { CharacterClass.Templar02, Templar02.Data() },
                { CharacterClass.Templar03, Templar03.Data() },
                { CharacterClass.Templar04, Templar04.Data() },
                { CharacterClass.Templar05, Templar05.Data() },
                { CharacterClass.Thief, Thief.Data() },
                { CharacterClass.Tinkerer, Tinkerer.Data() },
                { CharacterClass.Toad00, Toad00.Data() },
                { CharacterClass.TreeGolem00, TreeGolem00.Data() },
                { CharacterClass.TreeGolem01, TreeGolem01.Data() },
                { CharacterClass.TreeGolem02, TreeGolem02.Data() },
                { CharacterClass.TreeGolem03, TreeGolem03.Data() },
                { CharacterClass.TreeGolem04, TreeGolem04.Data() },
                { CharacterClass.TreeGolem06, TreeGolem06.Data() },
                { CharacterClass.Undead00, Undead00.Data() },
                { CharacterClass.Undead01, Undead01.Data() },
                { CharacterClass.Undead02, Undead02.Data() },
                { CharacterClass.Undead04, Undead04.Data() },
                { CharacterClass.Vampire, Vampire.Data() },
                { CharacterClass.Vulture, Vulture.Data() },
                { CharacterClass.WarChief, WarChief.Data() },
                { CharacterClass.Werewolf00, Werewolf00.Data() },
                { CharacterClass.WhiteNinja, WhiteNinja.Data() },
                { CharacterClass.WhiteWitch, WhiteWitch.Data() },
                { CharacterClass.WildChild, WildChild.Data() },
                { CharacterClass.Wolf00, Wolf00.Data() },
                { CharacterClass.Wolf01, Wolf01.Data() },
                { CharacterClass.Wolf02, Wolf02.Data() },
                { CharacterClass.Wolf03, Wolf03.Data() },
                { CharacterClass.YellowNinja, YellowNinja.Data() },
                { CharacterClass.Yeti, Yeti.Data() },
            };

            isLoaded = true;
        }
    }
}
