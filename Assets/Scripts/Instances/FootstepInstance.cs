using Scripts.Helpers;
using System.Collections;
using UnityEngine;
using g = Scripts.Helpers.GameHelper;
using Scripts.Canvas;
using Scripts.Data.Actor;
using Scripts.Data.Items;
using Scripts.Data.Skills;
using Scripts.Effects;
using Scripts.Factories;
using Scripts.Hub;
using Scripts.Instances.Actor;
using Scripts.Instances.Board;
using Scripts.Instances.SynergyLine;
using Scripts.Inventory;
using Scripts.Libraries;
using Scripts.Managers;
using Scripts.Models;
using Scripts.Models.Actor;
using Scripts.Overworld;
using Scripts.Sequences;
using Scripts.Serialization;
using Scripts.Utilities;

namespace Scripts.Instances
{
/// <summary>
/// FOOTSTEPINSTANCE - Individual footstep trail effect.
/// 
/// PURPOSE:
/// Represents a single footstep sprite that appears behind moving
/// actors and fades out over time.
/// 
/// VISUAL EFFECT:
/// ```
/// [Actor] →→→ moving
///   👣 👣 👣
/// older ← newer (fading trail)
/// ```
/// 
/// LIFECYCLE:
/// 1. Spawned by FootstepManager during actor movement
/// 2. Positioned at actor's previous location
/// 3. Rotated based on movement direction
/// 4. Fades out over Duration (10 seconds)
/// 5. Self-destructs when fully faded
/// 
/// ALTERNATING FEET:
/// Uses isRightFoot parameter to alternate between
/// left and right foot sprites.
/// 
/// RELATED FILES:
/// - FootstepManager.cs: Spawns footsteps during movement
/// - FootstepFactory.cs: Creates footstep GameObjects
/// - SpriteLibrary.cs: Footstep sprites
/// </summary>
public class FootstepInstance : MonoBehaviour
{
    #region Properties

    /// <summary>GameObject name.</summary>
    public string Name
    {
        get => name;
        set => name = value;  // Fixed: was causing infinite recursion
    }

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

    public Quaternion Rotation
    {
        get => gameObject.transform.rotation;
        set => gameObject.transform.rotation = value;
    }

    public Sprite sprite
    {
        get => spriteRenderer.sprite;
        set => spriteRenderer.sprite = value;
    }

    #endregion

    #region Fields

    float Duration;
    SpriteRenderer spriteRenderer;

    #endregion

    #region Unity Lifecycle

    /// <summary>Initializes component references and state.</summary>
    private void Awake()
    {
        transform.localScale = g.TileScale / 2;
        spriteRenderer = GetComponent<SpriteRenderer>();
        Duration = Interval.OneSecond * 10;
    }

    #endregion

    #region Spawn

    /// <summary>Creates the instance.</summary>
    public void Spawn(Vector3 position, Quaternion rotation, bool isRightFoot)
    {
        this.Position = position;
        this.Rotation = rotation;
        spriteRenderer.flipX = !isRightFoot;

        StartCoroutine(FadeOutRoutine());
    }

        /// <summary>Coroutine that executes the fade out sequence.</summary>
        private IEnumerator FadeOutRoutine()
        {
            yield return Wait.For(Duration);

            float alpha = spriteRenderer.color.a;
            spriteRenderer.color = new Color(1, 1, 1, alpha);

            while (alpha > 0)
            {
                alpha -= Increment.Percent1;
                alpha = Mathf.Max(alpha, 0f);
                spriteRenderer.color = new Color(1, 1, 1, alpha);

                yield return Wait.For(Interval.TenTicks);
            }

            Destroy(this.gameObject);
        }

        #endregion
    }

}
