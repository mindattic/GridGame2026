namespace Scripts.Models
{
    /// <summary>
    /// TARGETSHAPE - The geometric footprint a spell's effect covers, relative to its anchor tile.
    /// Combined with <see cref="TargetMode"/> (how the anchor is picked) and
    /// <see cref="TargetFilter"/> (which actors on the covered tiles are eligible).
    ///
    /// <para>Holistic on purpose: a designer pairs any Shape with any Mode/Filter to express
    /// most RPG targeting idioms — pick-an-actor-and-hit-their-row, pick-a-tile-3×3-of-enemies,
    /// hit-everyone, pick-a-tile-cross-AOE-but-only-allies-to-heal, etc.</para>
    /// </summary>
    public enum TargetShape
    {
        Self,           // anchor = caster's tile; affects only the caster
        SingleActor,    // anchor = picked actor's tile; affects only that tile
        SingleTile,     // anchor = picked tile; affects only that tile
        Square,         // Chebyshev distance ≤ Radius (square AOE around anchor)
        Diamond,        // Manhattan distance ≤ Radius (rhombus / "diamond" AOE)
        Cross,          // anchor + Radius tiles in each cardinal direction (+-arm)
        Plus,           // entire row UNION entire column of anchor (board-wide + spanning the board)
        Row,            // entire row containing anchor
        Column,         // entire column containing anchor
        AllEnemies,     // all enemy actors (no anchor needed)
        AllAllies,      // all hero actors (no anchor needed)
    }

    /// <summary>How the anchor for the shape is determined.</summary>
    public enum TargetMode
    {
        Auto,        // no pick (Self / AllEnemies / AllAllies)
        PickActor,   // click an eligible actor; anchor = their tile
        PickTile,    // click any tile on the board; anchor = that tile
    }

    /// <summary>Which actors on the shape's tiles count as targets.</summary>
    public enum TargetFilter
    {
        Any,
        EnemyOnly,
        AllyOnly,
        EmptyOnly,   // anchors that have no actor (for movement-placement spells; rare)
    }
}
