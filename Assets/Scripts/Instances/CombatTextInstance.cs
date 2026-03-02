using Scripts.Helpers;
using System.Collections;
using TMPro;
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
/// COMBATTEXTINSTANCE - Floating damage/status text.
/// 
/// PURPOSE:
/// Handles the behavior and animation of floating combat text
/// (damage numbers, status effects, healing, etc).
/// 
/// VISUAL EFFECT:
/// ```
/// Actor takes damage:
///     -42
///      ↑↗ (rises and fades)
///   [Actor]
/// ```
/// 
/// TEXT STYLES:
/// Configured via TextStyle profile which specifies:
/// - Font, Color, Size
/// - Motion type (Rise, Oscillate, etc)
/// 
/// MOTION TYPES:
/// - Rise: Float straight up
/// - Oscillate: Wiggle side-to-side while rising
/// - Bounce: Arc motion
/// 
/// LIFECYCLE:
/// 1. Spawned by CombatTextManager
/// 2. Positioned above actor with random offset
/// 3. Animates based on motion style
/// 4. Fades out
/// 5. Self-destructs
/// 
/// RELATED FILES:
/// - CombatTextManager.cs: Spawns text instances
/// - CombatTextFactory.cs: Creates text GameObjects
/// - TextStyle.cs: Style configuration
/// </summary>
public class CombatTextInstance : MonoBehaviour
{
    #region Fields

    [SerializeField] AnimationCurve riseCurve;
    public TextMeshPro textMesh;
    public Vector3 speed;
    public TextMotion style = TextMotion.Oscillate;

    #endregion

    #region Properties

    /// <summary>Parent transform for positioning.</summary>
    public Transform parent
    {
        get => transform.parent;
        set => transform.SetParent(value, true);
    }

    /// <summary>World position.</summary>
    public Vector3 position
    {
        get => transform.position;
        set => transform.position = value;
    }

    #endregion

    #region Initialization

    void Awake()
    {
        textMesh = GetComponent<TextMeshPro>();
        speed = new Vector3(g.TileSize, g.TileSize / 32, 0);
    }

    #endregion

    #region Spawn

    /// <summary>Configures and spawns the floating text using a TextStyle profile.</summary>
    public void Spawn(string text, Vector3 pos, TextStyle profile)
    {
        style = profile.Motion;
        textMesh.text = text;
        textMesh.font = profile.Font;
        textMesh.fontSize = profile.Size;
        textMesh.color = profile.Color;

        transform.position = new Vector3(
            pos.x + RNG.Range(g.TileSize / 4),
            pos.y + g.TileSize / 4,
            0);

        // BounceRoutine the selected motion coroutine
        StartCoroutine(style switch
        {
            TextMotion.Float => FloatRoutine(),
            TextMotion.Oscillate => OscillateRoutine(),
            TextMotion.Bounce => BounceRoutine(),
            _ => FloatRoutine(),
        });
    }

    /// <summary>
    /// Floats the text upward while fading out.
    /// </summary>
    private IEnumerator FloatRoutine()
    {
        float alpha = 1;
        Color color = textMesh.color;
        Vector3 startPos = transform.position;

        while (textMesh.color.a > 0)
        {
            alpha = Mathf.Max(alpha - Increment.Percent3, 0);
            if (alpha < 0.5f)
            {
                color.a = alpha;
                textMesh.color = color;
            }
            // Seek upward
            transform.position = new Vector3(startPos.x, position.y + speed.y, 0);
            yield return Wait.For(Interval.OneTick);
        }
        Destroy(gameObject);
    }

    /// <summary>
    /// Oscillates the text horizontally while rising and fading out.
    /// </summary>
    private IEnumerator OscillateRoutine()
    {
        float alpha = 1;
        Color color = textMesh.color;
        Vector3 startPos = transform.position;
        float timer = 0f, duration = 0.25f;

        while (textMesh.color.a > 0)
        {
            alpha = Mathf.Max(alpha - Increment.Percent3, 0);
            if (alpha < 0.5f)
            {
                color.a = alpha;
                textMesh.color = color;
            }

            timer += Time.deltaTime;
            float normalized = (timer % duration) / duration;
            float curve = riseCurve.Evaluate(normalized) * g.TileSize / 8;

            transform.position = new Vector3(startPos.x + curve, position.y + speed.y, 0);
            yield return Wait.For(Interval.OneTick);
        }
        Destroy(gameObject);
    }

    /// <summary>
    /// Bounces the text and fades it out after the first bounce.
    /// </summary>
    private IEnumerator BounceRoutine()
    {
        float alpha = 1f;
        Color color = textMesh.color;
        Vector3 startPos = transform.position;

        float vY = g.TileSize * 6, gravity = -g.TileSize * 18f;
        float bounceDamping = 0.5f, groundY = startPos.y, bounceEnd = g.TileSize * 0.1f;
        float hFocus = g.TileSize * Increment.Percent33 * RNG.Float(-1f, 1f);
        int bounceCount = 0;
        bool fadeStarted = false;

        while (alpha > 0)
        {
            vY += gravity * Time.deltaTime;
            Vector3 pos = transform.position;
            pos.y += vY * Time.deltaTime;
            if (bounceCount <= 3) pos.x += hFocus * Time.deltaTime;

            if (pos.y <= groundY)
            {
                pos.y = groundY;
                if (!fadeStarted) fadeStarted = true;
                if (Mathf.Abs(vY) < bounceEnd) vY = 0;
                else { vY = -vY * bounceDamping; bounceCount++; }
            }

            transform.position = pos;

            if (fadeStarted)
            {
                alpha = Mathf.Max(alpha - Increment.Percent3, 0);
                color.a = alpha;
                textMesh.color = color;
            }

            yield return Wait.For(Interval.OneTick);
        }
        Destroy(gameObject);
    }

    #endregion
}

}
