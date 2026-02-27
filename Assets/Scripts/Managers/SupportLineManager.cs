using Assets.Scripts.Factories;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using g = Assets.Helpers.GameHelper;

/// <summary>
/// Manages lifecycle of SupportLineInstance objects keyed by (supporter, attacker).
/// Provides spawn, despawn, destroy, and bulk clear operations.
/// </summary>
public class SupportLineManager : MonoBehaviour
{
    // ------------------------------------------------------------
    // Fields
    // ------------------------------------------------------------

    public Dictionary<(ActorInstance, ActorInstance), SupportLineInstance> supportLines =
        new Dictionary<(ActorInstance, ActorInstance), SupportLineInstance>();

    // Line width used by SupportLineInstance (startWidth/endWidth = g.TileSize * 0.25f)
    private float LineWidth => g.TileSize * 0.25f;
    private float HalfLineWidth => LineWidth * 0.5f;

    // ------------------------------------------------------------
    // Public API
    // ------------------------------------------------------------

    /// <summary>
    /// Check if a support line already exists for the given pair.
    /// </summary>
    public bool Exists(ActorInstance supporter, ActorInstance attacker)
    {
        var key = GetKey(supporter, attacker);
        return supportLines.ContainsKey(key);
    }

    /// <summary>
    /// Create and register a new support line instance for the given pair.
    /// Returns null if one already exists.
    /// </summary>
    public SupportLineInstance Spawn(ActorInstance supporter, ActorInstance attacker)
    {
        var key = GetKey(supporter, attacker);

        if (Exists(supporter, attacker))
            return null;

        // Use factory instead of Instantiate(prefab)
        var go = SupportLineFactory.Create();
        go.transform.position = Vector2.zero;
        go.transform.rotation = Quaternion.identity;
        var instance = go.GetComponent<SupportLineInstance>();

        supportLines.Add(key, instance);
        instance.Spawn(supporter, attacker);

        return instance;
    }

    /// <summary>
    /// Request a graceful despawn for the given pair and remove from registry.
    /// </summary>
    public void Despawn(ActorInstance supporter, ActorInstance attacker)
    {
        var key = GetKey(supporter, attacker);

        if (supportLines.TryGetValue(key, out var instance))
        {
            if (instance != null)
                instance.Despawn();
            supportLines.Remove(key);
        }
    }

    /// <summary>
    /// Despawn all active support lines and clear the registry.
    /// </summary>
    public void Clear()
    {
        // Snapshot keys, then remove entries one by one to avoid modifying during enumeration
        var keys = supportLines.Keys.ToList();
        foreach (var key in keys)
        {
            if (supportLines.TryGetValue(key, out var instance) && instance != null)
            {
                // Destroy GameObjects directly to avoid callbacks that mutate the dictionary
                instance.Destroy();
            }
            // Remove the entry if it still exists
            supportLines.Remove(key);
        }
    }

    /// <summary>
    /// Immediately destroy the instance for the given pair and remove from registry.
    /// </summary>
    public void Destroy(ActorInstance supporter, ActorInstance attacker)
    {
        var key = GetKey(supporter, attacker);

        if (supportLines.TryGetValue(key, out var instance))
        {
            if (instance != null)
                instance.Destroy();
            supportLines.Remove(key);
        }
    }

    /// <summary>
    /// Despawn and remove all support lines that involve the given hero (as supporter or attacker).
    /// </summary>
    public void ClearFor(ActorInstance hero)
    {
        if (hero == null) return;
        var keys = supportLines.Keys.Where(k => k.Item1 == hero || k.Item2 == hero).ToList();
        foreach (var k in keys)
        {
            Despawn(k.Item1, k.Item2);
        }
    }

    /// <summary>
    /// Build a tuple key for the pair.
    /// </summary>
    public (ActorInstance, ActorInstance) GetKey(ActorInstance supporter, ActorInstance attacker)
    {
        return (supporter, attacker);
    }

    // ------------------------------------------------------------
    // Dynamic support line logic for moving hero
    // ------------------------------------------------------------

    /// <summary>
    /// Returns true if the moving hero is sufficiently inside its current tile along the axis
    /// perpendicular to the given direction. Uses half of the line width as the buffer from tile edge.
    /// Example: for horizontal (left/right) directions, checks Y distance to tile center.
    /// </summary>
    private bool IsInsideLaneBuffer(ActorInstance movingHero, Vector2Int direction)
    {
        if (movingHero == null || movingHero.currentTile == null) return false;
        var pos = movingHero.Position;
        var center = movingHero.currentTile.position;

        float tileHalf = g.TileSize * 0.5f;
        float maxOffsetFromCenter = Mathf.Max(0.0f, tileHalf - HalfLineWidth);

        if (direction == Vector2Int.left || direction == Vector2Int.right)
        {
            return Mathf.Abs(pos.y - center.y) <= maxOffsetFromCenter;
        }
        else if (direction == Vector2Int.up || direction == Vector2Int.down)
        {
            return Mathf.Abs(pos.x - center.x) <= maxOffsetFromCenter;
        }
        return false;
    }

    /// <summary>
    /// Buffered aligned heroes: returns nearest hero in each cardinal direction with no other actors between,
    /// only if the moving hero is sufficiently inside the lane band (buffer from tile edges) for that direction.
    /// Used for both spawning and retention so the line never overlaps visibly.
    /// </summary>
    private IEnumerable<ActorInstance> GetAlignedHeroesBuffered(ActorInstance movingHero)
    {
        var dirs = new Vector2Int[] { Vector2Int.up, Vector2Int.right, Vector2Int.down, Vector2Int.left };
        foreach (var d in dirs)
        {
            if (!IsInsideLaneBuffer(movingHero, d))
                continue;

            var loc = movingHero.location + d;
            while (g.TileMap.ContainsLocation(loc))
            {
                var occupant = g.Actors.All.FirstOrDefault(a => a != null && a.IsPlaying && a.location == loc);
                if (occupant != null)
                {
                    if (occupant.IsHero)
                        yield return occupant; // first occupant is a hero -> aligned with no actors between
                    break; // stop at first occupied tile
                }
                loc += d;
            }
        }
    }

    /// <summary>
    /// Spawn/maintain lines only for strictly horizontal/vertical alignment (no diagonals) with no actors between.
    /// Use buffered list for both spawn and despawn to ensure ends never overlap while crossing tile boundaries.
    /// </summary>
    public void UpdateForSelectedHeroLocation(ActorInstance movingHero)
    {
        if (movingHero == null || !movingHero.IsHero || !movingHero.IsPlaying)
            return;

        var alignedBuffered = GetAlignedHeroesBuffered(movingHero).ToList();

        // Update or spawn lines for aligned heroes
        foreach (var supporter in alignedBuffered)
        {
            var key = GetKey(supporter, movingHero);
            if (supportLines.TryGetValue(key, out var inst) && inst != null)
            {
                inst.UpdateEndpoints();
            }
            else
            {
                Spawn(supporter, movingHero);
            }
        }

        // Despawn lines not aligned anymore per buffered rule
        var existingKeys = supportLines.Keys.ToList();
        foreach (var key in existingKeys)
        {
            bool involvesHero = key.Item2 == movingHero; // only lines where movingHero is attacker
            if (!involvesHero) continue;

            var supporter = key.Item1;
            bool stillAligned = alignedBuffered.Contains(supporter);
            if (!stillAligned)
            {
                Despawn(key.Item1, key.Item2);
            }
        }
    }
}
