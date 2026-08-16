using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Scripts.Instances.Board;

namespace Scripts.Managers
{
    /// <summary>
    /// TRAPMANAGER - Per-battle tile-trap state (US-139 / GG-A5).
    ///
    /// <para>PURPOSE: Trap-layer enemies spend a turn arming a tile; any HERO who enters that
    /// tile — by drag-slide OR by being displaced there — springs it (damage + a status).
    /// State is a static per-battle dictionary (same shape as BuffSystem/SkillCooldownManager);
    /// consumption is caller-driven: movement code calls <see cref="TryConsume"/> and applies
    /// the payload, keeping this class scene-free and unit-testable.</para>
    ///
    /// <para>VISUAL: armed traps tint their tile purple via one shared LineTelegraph marker
    /// (visible by design for the PoC — hidden traps are a tuning option later).</para>
    ///
    /// <para>RELATED FILES: TrapCatalog.cs (who lays what), PlaceTrapSequence.cs,
    /// ActorMovement.cs (trigger hooks), TurnManager.cs / StageManager.cs (Clear).</para>
    /// </summary>
    public static class TrapManager
    {
        private static readonly Dictionary<Vector2Int, Scripts.Data.Actor.TrapDefinition> traps
            = new Dictionary<Vector2Int, Scripts.Data.Actor.TrapDefinition>();

        private static LineTelegraph marker;

        /// <summary>Arms <paramref name="trap"/> on <paramref name="location"/> (one per tile —
        /// re-arming replaces). Returns false for a null trap.</summary>
        public static bool Place(Vector2Int location, Scripts.Data.Actor.TrapDefinition trap)
        {
            if (trap == null) return false;
            traps[location] = trap;
            RefreshMarker();
            return true;
        }

        /// <summary>True when a trap is armed at <paramref name="location"/>.</summary>
        public static bool HasTrapAt(Vector2Int location) => traps.ContainsKey(location);

        /// <summary>Count of armed traps (marker/test convenience).</summary>
        public static int Count => traps.Count;

        /// <summary>Springs and removes the trap at <paramref name="location"/>, if any.
        /// The CALLER applies the payload (damage/status/feed) — this only owns the state.</summary>
        public static bool TryConsume(Vector2Int location, out Scripts.Data.Actor.TrapDefinition trap)
        {
            if (traps.TryGetValue(location, out trap))
            {
                traps.Remove(location);
                RefreshMarker();
                return true;
            }
            trap = null;
            return false;
        }

        /// <summary>Wipe all traps (new battle / restart).</summary>
        public static void Clear()
        {
            traps.Clear();
            if (marker != null) { Object.Destroy(marker.gameObject); marker = null; }
        }

        private static void RefreshMarker()
        {
            if (!Application.isPlaying) return; // EditMode tests exercise state only
            if (traps.Count == 0)
            {
                if (marker != null) { Object.Destroy(marker.gameObject); marker = null; }
                return;
            }
            var tiles = traps.Keys.ToList();
            if (marker == null) marker = LineTelegraph.Show(tiles, LineTelegraph.TrapTint);
            else marker.SetTiles(tiles);
        }
    }
}
