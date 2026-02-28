using Assets.Helper;
using System.Collections;
using UnityEngine;
using g = Assets.Helpers.GameHelper;

/// <summary>
/// GHOSTINSTANCE - Actor ghost/afterimage effect.
/// 
/// PURPOSE:
/// Creates a semi-transparent "ghost" copy of an actor that
/// fades out, used for movement trails and visual feedback.
/// 
/// VISUAL EFFECT:
/// ```
/// Actor moves:
/// [Ghost1] → [Ghost2] → [Ghost3] → [Actor]
///  fading     fading     fading    solid
/// ```
/// 
/// USAGE:
/// Spawned by GhostManager during actor movement to create
/// afterimage/trail effects.
/// 
/// LIFECYCLE:
/// 1. Spawned at actor's position
/// 2. Copies actor's thumbnail sprite
/// 3. Starts with 25% opacity
/// 4. Fades out over time
/// 5. Self-destructs when fully faded
/// 
/// RELATED FILES:
/// - GhostManager.cs: Spawns ghost instances
/// - GhostFactory.cs: Creates ghost GameObjects
/// - GhostRenderers.cs: Holds sprite references
/// </summary>
public class GhostInstance : MonoBehaviour
{
    #region Properties

    public Transform parent
    {
        get => gameObject.transform.parent;
        set => gameObject.transform.SetParent(value, true);
    }

    public Vector3 Position
    {
        get => gameObject.transform.position;
        set => gameObject.transform.position = value;
    }

    public Sprite thumbnail
    {
        get => renderers.thumbnail.sprite;
        set => renderers.thumbnail.sprite = value;
    }

    public Sprite frame
    {
        get => renderers.frame.sprite;
        set => renderers.frame.sprite = value;
    }

    public int sortingOrder
    {
        set
        {
            renderers.thumbnail.sortingOrder = value;
            renderers.frame.sortingOrder = value + 1;
        }
    }

    #endregion

    #region Fields

    const int Thumbnail = 0;
    const int Frame = 1;
    public GhostRenderers renderers = new GhostRenderers();

    #endregion

    #region Spawn

    public void Spawn(ActorInstance actor)
    {
        this.renderers.frame.enabled = false;
        this.renderers.thumbnail.size = new Vector2(g.TileSize, g.TileSize);
        this.renderers.thumbnail.color = ColorHelper.RGBA(255, 255, 255, 64);
        this.Position = actor.Position;
        StartCoroutine(FadeOutRoutine());
    }

    #endregion

    #region Unity Lifecycle

    private void Awake()
    {
        renderers.thumbnail = gameObject.transform.GetChild(Thumbnail).GetComponent<SpriteRenderer>();
        renderers.frame = gameObject.transform.GetChild(Frame).GetComponent<SpriteRenderer>();
    }


    private IEnumerator FadeOutRoutine()
    {
        float alpha = renderers.thumbnail.color.a;
        Color color = renderers.thumbnail.color;

        while (alpha > 0)
        {
            alpha -= Increment.Percent5;
            alpha = Mathf.Max(alpha, 0f);
            color.a = alpha;
            renderers.thumbnail.color = color;
            renderers.frame.color = color;

            yield return Wait.For(Interval.FiveTicks);
        }

        Destroy(this.gameObject);
    }

    #endregion
}
