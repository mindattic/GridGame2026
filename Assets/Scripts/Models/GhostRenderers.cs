using UnityEngine;

/// <summary>
/// GHOSTRENDERERS - Sprite references for ghost effect.
/// 
/// PURPOSE:
/// Holds references to the SpriteRenderers used by
/// GhostInstance for the ghost/afterimage effect.
/// 
/// COMPONENTS:
/// - thumbnail: Main sprite (actor appearance)
/// - frame: Optional border sprite
/// 
/// RELATED FILES:
/// - GhostInstance.cs: Uses these renderers
/// - GhostFactory.cs: Creates ghost with renderers
/// - GhostManager.cs: Manages ghost spawning
/// </summary>
public class GhostRenderers
{
    public GhostRenderers() { }

    public SpriteRenderer thumbnail;
    public SpriteRenderer frame;
}
