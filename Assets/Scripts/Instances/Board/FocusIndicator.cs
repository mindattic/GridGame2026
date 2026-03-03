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
/// FOCUSINDICATOR - Visual indicator for focused/selected actor.
/// 
/// PURPOSE:
/// Highlights the currently selected actor with a visible marker,
/// helping players track which unit is being controlled.
/// 
/// VISUAL EFFECT:
/// ```
///    ┌───────┐
///    │ Actor │ ← Selected actor
///    └───────┘
///  ╔═══════════╗
///  ║  Focus    ║ ← Indicator ring/glow
///  ╚═══════════╝
/// ```
/// 
/// POSITIONING:
/// - Show(): Moves to selected actor's position
/// - Hide(): Moves off-screen (PositionHelper.Nowhere)
/// 
/// SCALING:
/// Scales to match tile size (g.TileScale * 1.1f) for proper fit.
/// 
/// RELATED FILES:
/// - SelectionManager.cs: Tracks selected actor
/// - TargetIndicator.cs: Similar indicator for targets
/// - InputManager.cs: Triggers selection changes
/// </summary>
public class FocusIndicator : MonoBehaviour
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

    /// <summary>Initializes component references and state.</summary>
    private void Awake()
    {
        spriteRenderer = GetComponent<SpriteRenderer>();
    }

    /// <summary>Initializes initialize.</summary>
    public void Initialize()
    {

        scale = g.TileScale * 1.1f;
    }

    // SelectProfile activates and positions the FocusIndicator based on whether a focused actor exists.
    /// <summary>Shows this component.</summary>
    public void Show()
    {
        position = g.Actors.HasSelectedActor ? g.Actors.SelectedActor.Position : PositionHelper.Nowhere;
    }

    // Hide deactivates the FocusIndicator and moves it off-screen.
    /// <summary>Hides this component.</summary>
    public void Hide()
    {
        position = PositionHelper.Nowhere;
    }
}

}
