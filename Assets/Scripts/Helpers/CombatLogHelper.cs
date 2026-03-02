using System;
using System.Collections.Generic;
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
    /// Runtime combat log.
    /// Provides Write and Clear, and raises an event so Editor tooling can mirror entries.
    /// Safe to call from anywhere in play mode.
    /// </summary>
    public static class CombatLogHelper
    {
        public static event Action<string> OnWrite;

        private static readonly List<string> _messages = new List<string>(256);

        /// <summary>
        /// All messages currently stored in the runtime log.
        /// Intended for read-only display by tools.
        /// </summary>
        public static IReadOnlyList<string> Messages => _messages;

        /// <summary>
        /// Append a new line to the combat log.
        /// </summary>
        public static void Write(string message)
        {
            if (string.IsNullOrEmpty(message)) return;

            _messages.Add(message);
            OnWrite?.Invoke(message);
        }

        /// <summary>
        /// Clear all messages from the combat log.
        /// </summary>
        public static void Clear()
        {
            _messages.Clear();
            OnWrite?.Invoke(null);
        }
    }
}
