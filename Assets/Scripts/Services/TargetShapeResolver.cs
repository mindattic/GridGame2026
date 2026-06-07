using System.Collections.Generic;
using UnityEngine;
using Scripts.Instances.Actor;
using Scripts.Models;
using g = Scripts.Helpers.GameHelper;

namespace Scripts.Services
{
    /// <summary>
    /// TARGETSHAPERESOLVER - Pure functions: given an anchor tile + shape + radius + board size,
    /// returns the list of tiles the shape covers. Plus a companion <see cref="CollectActors"/>
    /// that walks those tiles and gathers the eligible actors per a <see cref="TargetFilter"/>.
    ///
    /// <para>Independent of Unity scene state for shape math — board size comes from a parameter
    /// so it's unit-testable. The actor collection step does query <c>g.Actors</c>.</para>
    /// </summary>
    public static class TargetShapeResolver
    {
        /// <summary>Resolve a shape into a list of tile coordinates clipped to the board.</summary>
        public static List<Vector2Int> Resolve(
            Vector2Int anchor, TargetShape shape, int radius, int boardWidth, int boardHeight)
        {
            var tiles = new List<Vector2Int>();
            switch (shape)
            {
                case TargetShape.Self:
                case TargetShape.SingleActor:
                case TargetShape.SingleTile:
                    AddIfInBounds(tiles, anchor, boardWidth, boardHeight);
                    break;

                case TargetShape.Square:
                    for (int dx = -radius; dx <= radius; dx++)
                        for (int dy = -radius; dy <= radius; dy++)
                            AddIfInBounds(tiles, new Vector2Int(anchor.x + dx, anchor.y + dy), boardWidth, boardHeight);
                    break;

                case TargetShape.Diamond:
                    for (int dx = -radius; dx <= radius; dx++)
                        for (int dy = -radius; dy <= radius; dy++)
                            if (Mathf.Abs(dx) + Mathf.Abs(dy) <= radius)
                                AddIfInBounds(tiles, new Vector2Int(anchor.x + dx, anchor.y + dy), boardWidth, boardHeight);
                    break;

                case TargetShape.Cross:
                    AddIfInBounds(tiles, anchor, boardWidth, boardHeight);
                    for (int d = 1; d <= radius; d++)
                    {
                        AddIfInBounds(tiles, new Vector2Int(anchor.x + d, anchor.y), boardWidth, boardHeight);
                        AddIfInBounds(tiles, new Vector2Int(anchor.x - d, anchor.y), boardWidth, boardHeight);
                        AddIfInBounds(tiles, new Vector2Int(anchor.x, anchor.y + d), boardWidth, boardHeight);
                        AddIfInBounds(tiles, new Vector2Int(anchor.x, anchor.y - d), boardWidth, boardHeight);
                    }
                    break;

                case TargetShape.Plus:
                    for (int x = 0; x < boardWidth; x++) AddIfInBounds(tiles, new Vector2Int(x, anchor.y), boardWidth, boardHeight);
                    for (int y = 0; y < boardHeight; y++) if (y != anchor.y) AddIfInBounds(tiles, new Vector2Int(anchor.x, y), boardWidth, boardHeight);
                    break;

                case TargetShape.Row:
                    for (int x = 0; x < boardWidth; x++) AddIfInBounds(tiles, new Vector2Int(x, anchor.y), boardWidth, boardHeight);
                    break;

                case TargetShape.Column:
                    for (int y = 0; y < boardHeight; y++) AddIfInBounds(tiles, new Vector2Int(anchor.x, y), boardWidth, boardHeight);
                    break;

                case TargetShape.AllEnemies:
                case TargetShape.AllAllies:
                    // No tile shape — actor collection produces the targets directly.
                    break;
            }
            return tiles;
        }

        private static void AddIfInBounds(List<Vector2Int> list, Vector2Int p, int w, int h)
        {
            if (p.x < 0 || p.x >= w || p.y < 0 || p.y >= h) return;
            list.Add(p);
        }

        /// <summary>Walk the resolved tiles and collect actors matching <paramref name="filter"/>
        /// (relative to <paramref name="caster"/>'s team). <see cref="TargetShape.AllEnemies"/> /
        /// <see cref="TargetShape.AllAllies"/> bypass tile-walking and pull from g.Actors directly.</summary>
        public static List<ActorInstance> CollectActors(
            List<Vector2Int> tiles, TargetShape shape, TargetFilter filter, ActorInstance caster)
        {
            var list = new List<ActorInstance>();

            if (shape == TargetShape.AllEnemies)
            {
                foreach (var a in g.Actors.Enemies) if (IsPlayable(a)) list.Add(a);
                return list;
            }
            if (shape == TargetShape.AllAllies)
            {
                foreach (var a in g.Actors.Heroes) if (IsPlayable(a)) list.Add(a);
                return list;
            }

            // Tile-walking path: for each tile, find any playable actor on it; filter by team.
            // A multi-tile enemy (2×2) covers several resolved tiles, so dedupe by actor identity —
            // an AOE overlapping any of its tiles hits it exactly ONCE.
            var seen = new HashSet<ActorInstance>();
            foreach (var tile in tiles)
            {
                var occupant = FindActorAt(tile);
                if (occupant == null)
                {
                    // Only Empty filter cares about un-occupied tiles; targets there are anchor-only
                    // (no actor to apply effects to). We do NOT add a null actor; callers can use
                    // the tile list directly for FX positioning.
                    continue;
                }
                if (!seen.Add(occupant)) continue; // already collected via another footprint tile
                if (Matches(occupant, filter, caster)) list.Add(occupant);
            }
            return list;
        }

        public static bool Matches(ActorInstance actor, TargetFilter filter, ActorInstance caster)
        {
            if (actor == null) return filter == TargetFilter.EmptyOnly;
            // Null-caster safety: team-relative filters need a reference team. If we have none,
            // assume Hero so Enemy/Ally filters still produce sensible results in debug flows.
            var casterTeam = caster != null ? caster.team : Scripts.Models.Team.Hero;
            switch (filter)
            {
                case TargetFilter.Any:        return true;
                case TargetFilter.EnemyOnly:  return actor.team != casterTeam;
                case TargetFilter.AllyOnly:   return actor.team == casterTeam;
                case TargetFilter.EmptyOnly:  return false;
            }
            return false;
        }

        /// <summary>The playing actor whose footprint covers <paramref name="tile"/> (footprint-aware
        /// via the occupancy chokepoint), or null. A 2×2 enemy is found from any of its tiles.</summary>
        public static ActorInstance FindActorAt(Vector2Int tile) => g.Actors.ActorAt(tile);

        private static bool IsPlayable(ActorInstance a) => a != null && a.IsPlaying;
    }
}
