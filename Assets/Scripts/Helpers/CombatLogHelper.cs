using System;
using System.Collections.Generic;

namespace Assets.Helpers
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
