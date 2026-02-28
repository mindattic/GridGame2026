using Assets.Helper;
using Game.Behaviors;
using UnityEngine;
using g = Assets.Helpers.GameHelper;

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

    private void Awake()
    {
        spriteRenderer = GetComponent<SpriteRenderer>();
    }

    public void Initialize()
    {

        scale = g.TileScale * 1.1f;
    }

    // SelectProfile activates and positions the FocusIndicator based on whether a focused actor exists.
    public void Show()
    {
        position = g.Actors.HasSelectedActor ? g.Actors.SelectedActor.Position : PositionHelper.Nowhere;
    }

    // Hide deactivates the FocusIndicator and moves it off-screen.
    public void Hide()
    {
        position = PositionHelper.Nowhere;
    }
}
