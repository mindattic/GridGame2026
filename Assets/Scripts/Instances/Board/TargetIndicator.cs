using Scripts.Helpers;
using Scripts.Managers;
using UnityEngine;
using g = Scripts.Helpers.GameHelper;
using Scripts.Canvas;
using Scripts.Data.Actor;
using Scripts.Data.Items;
using Scripts.Data.Skills;
using Scripts.Effects;
using Scripts.Factories;
using Scripts.Hub;
using Scripts.Instances;
using Scripts.Instances.Actor;
using Scripts.Instances.SynergyLine;
using Scripts.Inventory;
using Scripts.Libraries;
using Scripts.Models;
using Scripts.Models.Actor;
using Scripts.Overworld;
using Scripts.Sequences;
using Scripts.Serialization;
using Scripts.Utilities;

namespace Scripts.Instances.Board
{
/// <summary>
/// TARGETINDICATOR - Visual indicator for targeted actor.
/// 
/// PURPOSE:
/// Highlights the currently targeted actor (enemy being aimed at)
/// with a visible marker during ability targeting.
/// 
/// VISUAL EFFECT:
/// ```
///    ┌───────┐
///    │ Enemy │ ← Targeted actor
///    └───────┘
///  ╔═══════════╗
///  ║  Target   ║ ← Indicator ring (typically red/hostile)
///  ╚═══════════╝
/// ```
/// 
/// POSITIONING:
/// - Show(): Moves to target actor's position
/// - Hide(): Moves off-screen (PositionHelper.Nowhere)
/// 
/// SCALING:
/// Scales to match tile size (g.TileScale * 1.1f) for proper fit.
/// 
/// RELATED FILES:
/// - AbilityManager.cs: Sets target during ability targeting
/// - FocusIndicator.cs: Similar indicator for selection
/// - TargetModeOverlay.cs: Board dimming during targeting
/// </summary>
public class TargetIndicator : MonoBehaviour
{

    private SpriteRenderer spriteRenderer;

    #region Instance Properties
    public Transform parent
    {
        get => gameObject.transform.parent;
        set => gameObject.transform.SetParent(value, true);
    }
    public Vector3 position
    {
        get => gameObject.transform.position;
        set => gameObject.transform.position = value;
    }
    public Quaternion rotation
    {
        get => gameObject.transform.rotation;
        set => gameObject.transform.rotation = value;
    }
    public Vector3 scale
    {
        get => gameObject.transform.localScale;
        set => gameObject.transform.localScale = value;
    }
    #endregion

    private void Awake()
    {
        spriteRenderer = GetComponent<SpriteRenderer>();
    }

    public void Initialize()
    {
        scale = g.TileScale * 1.1f;
    }

    // Activates and positions the TargetIndicator based on whether a focused actor exists.
    public void Show()
    {
        position = g.Actors.HasTargetActor ? g.Actors.TargetActor.Position : PositionHelper.Nowhere;
    }

    // Hide deactivates the TargetIndicator and moves it off-screen.
    public void Hide()
    {
        position = PositionHelper.Nowhere;
    }
}

}
