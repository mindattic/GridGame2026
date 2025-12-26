using Assets.Helper;
using Game.Behaviors;
using UnityEngine;
using g = Assets.Helpers.GameHelper;

// TargetIndicator is a MonoBehaviour responsible for displaying an indicator
// that highlights the currently targeted actor (if any) on the game board.
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
