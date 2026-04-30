using System.Collections.Generic;
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
using Scripts.Models;
using Scripts.Models.Actor;
using Scripts.Overworld;
using Scripts.Sequences;
using Scripts.Serialization;
using Scripts.Utilities;

namespace Scripts.Managers
{
    /// <summary>
    /// BATTLEEVENTTRACKER - Lightweight cross-scene queue for combat events that need
    /// surfacing in the Hub or PostBattle screen.
    /// <para>PURPOSE: Some things that happen mid-battle ("the Iron Sword broke!") should not
    /// produce a flying combat-text overlay during the swing — they belong on the post-fight
    /// summary or the next Hub visit. This tracker holds a small list of human-readable strings
    /// drained by whoever shows them next.</para>
    /// <para>RELATED FILES: WeaponDurabilityHelper.cs (records breakage), PostBattleManager.cs
    /// + HubManager.cs (drain on entry).</para>
    /// </summary>
    public static class BattleEventTracker
    {
        private static readonly List<string> messages = new List<string>();

        /// <summary>Adds a message to the queue (de-duplicated).</summary>
        public static void Record(string message)
        {
            if (string.IsNullOrEmpty(message)) return;
            if (messages.Contains(message)) return;
            messages.Add(message);
        }

        /// <summary>True if any messages are pending.</summary>
        public static bool HasMessages => messages.Count > 0;

        /// <summary>Snapshot of pending messages.</summary>
        public static IReadOnlyList<string> Messages => messages;

        /// <summary>Drains and returns all pending messages.</summary>
        public static List<string> Drain()
        {
            var copy = new List<string>(messages);
            messages.Clear();
            return copy;
        }

        /// <summary>Wipes the queue without surfacing.</summary>
        public static void Clear() => messages.Clear();
    }
}
