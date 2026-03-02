using UnityEngine;
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
using Scripts.Managers;
using Scripts.Models.Actor;
using Scripts.Overworld;
using Scripts.Sequences;
using Scripts.Serialization;
using Scripts.Utilities;

namespace Scripts.Models
{
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

}
