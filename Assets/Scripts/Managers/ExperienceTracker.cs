using Scripts.Helpers;
using System.Collections.Generic;
using UnityEngine;
using Scripts.Canvas;
using Scripts.Data.Actor;
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
using Scripts.Models;
using Scripts.Models.Actor;
using Scripts.Overworld;
using Scripts.Sequences;
using Scripts.Serialization;
using Scripts.Utilities;

namespace Scripts.Managers
{
    /// <summary>
    /// EXPERIENCETRACKER - Tracks XP gains during battle.
    /// 
    /// PURPOSE:
    /// Static class that accumulates XP earned during a battle
    /// and persists it across scene loads until consumed by
    /// PostBattleScreen.
    /// 
    /// LIFECYCLE:
    /// 1. StartSession() clears and initializes participants
    /// 2. AddXP() accumulates XP for each character
    /// 3. PostBattleScreen reads and distributes XP
    /// 4. Clear() resets for next battle
    /// 
    /// PARTICIPANTS:
    /// Only party members who participated in the battle
    /// receive XP. Tracked via AddParticipant().
    /// 
    /// SCENE ROUTING:
    /// NextSceneAfterPostBattleScreen determines where to go
    /// after XP is awarded (default: Hub).
    /// 
    /// RELATED FILES:
    /// - PostBattleManager.cs: Distributes XP
    /// - ExperienceHelper.cs: XP calculations
    /// - BattleWonSequence.cs: Sets next scene
    /// </summary>
    public static class ExperienceTracker
    {
        public class Entry
        {
            public string Character;
            public int XPGained;
        }

        private static readonly Dictionary<CharacterClass, int> characterXP = new Dictionary<CharacterClass, int>();
        private static readonly HashSet<CharacterClass> participants = new HashSet<CharacterClass>();

        public static string NextSceneAfterPostBattleScreen = Scripts.Helpers.SceneHelper.Hub;

        /// <summary>Start session.</summary>
        public static void StartSession(IEnumerable<CharacterClass> participantCharacters)
        {
            characterXP.Clear();
            participants.Clear();
            if (participantCharacters != null)
            {
                foreach (var c in participantCharacters)
                {
                    if (c != CharacterClass.None) participants.Add(c);
                }
            }
        }

        /// <summary>Add participant.</summary>
        public static void AddParticipant(CharacterClass character)
        {
            if (character == CharacterClass.None) return;
            participants.Add(character);
        }

        /// <summary>Add xp.</summary>
        public static void AddXP(CharacterClass character, int amount)
        {
            if (character == CharacterClass.None || amount <= 0) return;
            if (characterXP.TryGetValue(character, out var cur))
                characterXP[character] = cur + amount;
            else
                characterXP[character] = amount;
        }

        /// <summary>Gets the xp gained.</summary>
        public static int GetXPGained(CharacterClass characterClass)
        {
            if (characterClass == CharacterClass.None) return 0;
            return characterXP.TryGetValue(characterClass, out var v) ? v : 0;
        }

        public static IReadOnlyDictionary<CharacterClass, int> AllGains => characterXP;
        public static IReadOnlyCollection<CharacterClass> Participants => participants;

        /// <summary> clear..Groups[0].Value.ToUpper() lear.</summary>
        public static void Clear()
        {
            characterXP.Clear();
            participants.Clear();
        }
    }
}
