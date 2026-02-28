using Assets.Helper;
using System.Collections;
using UnityEngine;

/// <summary>
/// CANVASPARTICLEINSTANCE - Individual UI particle behavior.
/// 
/// PURPOSE:
/// Controls a single particle in the UI canvas particle system.
/// Handles movement, rotation, and self-destruction when off-screen.
/// 
/// VISUAL EFFECT:
/// ```
/// Particle drifts across screen:
/// 
///  🍂 → → → (horizontal drift)
///    ↘       (falling)
///      🍂 
///        ↘
/// ```
/// 
/// BEHAVIOR:
/// 1. Spawned by CanvasParticleEmitter
/// 2. Drifts horizontally with horizontalFocus speed
/// 3. Falls vertically with fallFocus speed
/// 4. Rotates on X/Y/Z axes with random variation
/// 5. Self-destructs when past screen edge
/// 
/// CONFIGURATION:
/// - rotationFocus: Max rotation speed per axis
/// - horizontalFocus: Horizontal drift speed
/// - fallFocus: Vertical fall speed
/// 
/// RELATED FILES:
/// - CanvasParticleEmitter.cs: Spawns particles
/// - CanvasParticleFactory.cs: Creates particle GameObjects
/// - SpriteLibrary.cs: Particle sprites
/// </summary>
public class CanvasParticleInstance : MonoBehaviour
{
    #region Fields

    private float xRotationFocus;
    private float yRotationFocus;
    private float zRotationFocus;
    private float horizontalFocus;
    private float fallFocus;
    private RectTransform rectTransform;

    #endregion

    #region Properties

    public Transform parent
    {
        get => gameObject.transform.parent;
        set => gameObject.transform.SetParent(value, true);
    }

    #endregion

    #region Initialization

    /// <summary>Initializes particle with movement parameters and starts animation.</summary>
    public void Initialize(float rotationFocus, float horizontalFocus, float fallFocus)
    {
        this.xRotationFocus = RNG.Boolean ? RNG.Float(0, rotationFocus) : 0;
        this.yRotationFocus = RNG.Boolean ? RNG.Float(0, rotationFocus) : 0;
        this.zRotationFocus = rotationFocus;
        this.horizontalFocus = horizontalFocus;
        this.fallFocus = fallFocus;
        rectTransform = GetComponent<RectTransform>();
        StartCoroutine(MoveAndDestroyRoutine());
    }

    #endregion

    #region Movement

    private IEnumerator MoveAndDestroyRoutine()
    {
        while (rectTransform.anchoredPosition.x < Screen.width)
        {
            rectTransform.anchoredPosition += new Vector2(
                horizontalFocus * Time.deltaTime,
                -fallFocus * Time.deltaTime);

            rectTransform.Rotate(
                xRotationFocus * Time.deltaTime,
                yRotationFocus * Time.deltaTime,
                zRotationFocus * Time.deltaTime);

            yield return Wait.None();
        }
        Destroy(gameObject);
    }

    #endregion
}