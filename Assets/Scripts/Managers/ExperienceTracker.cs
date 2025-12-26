using Assets.Helpers;
using System.Collections.Generic;
using UnityEngine;

namespace Assets.Scripts.Managers
{
    // Holds per-battle XP gains and participants, persisted across scene loads until consumed by PostBattleScreen.
    public static class ExperienceTracker
    {
        public class Entry
        {
            public string Character;
            public int XPGained;
        }

        private static readonly Dictionary<CharacterClass, int> characterXP = new Dictionary<CharacterClass, int>();
        private static readonly HashSet<CharacterClass> participants = new HashSet<CharacterClass>();

        public static string NextSceneAfterPostBattleScreen = Assets.Helpers.SceneHelper.Hub; // default to Hub

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

        public static void AddParticipant(CharacterClass character)
        {
            if (character == CharacterClass.None) return;
            participants.Add(character);
        }

        public static void AddXP(CharacterClass character, int amount)
        {
            if (character == CharacterClass.None || amount <= 0) return;
            if (characterXP.TryGetValue(character, out var cur))
                characterXP[character] = cur + amount;
            else
                characterXP[character] = amount;
        }

        public static int GetXPGained(CharacterClass characterClass)
        {
            if (characterClass == CharacterClass.None) return 0;
            return characterXP.TryGetValue(characterClass, out var v) ? v : 0;
        }

        public static IReadOnlyDictionary<CharacterClass, int> AllGains => characterXP;
        public static IReadOnlyCollection<CharacterClass> Participants => participants;

        public static void Clear()
        {
            characterXP.Clear();
            participants.Clear();
        }
    }
}
