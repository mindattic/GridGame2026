using UnityEngine;
using g = Scripts.Helpers.GameHelper;
using Scripts.Canvas;
using Scripts.Data.Actor;
using Scripts.Data.Items;
using Scripts.Data.Skills;
using Scripts.Effects;
using Scripts.Factories;
using Scripts.Helpers;
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
/// ITEMPICKUPINSTANCE - Collectible crafting-material pickup behavior.
///
/// PURPOSE:
/// Visual pickup spawned when an enemy dies and drops a crafting material.
/// Mirrors CoinInstance's bounce -> seek -> despawn lifecycle. The underlying
/// drop is already booked into LootTracker at the death site; this instance
/// is purely a celebratory readout so the player sees which rarity of loot
/// dropped (tint = WoW-style rarity color).
///
/// STATES:
/// 1. Bounce: Physics pop out of corpse with arced impulse, settles on tile.
/// 2. Seek:   Arcs toward the coin counter UI to be "collected".
/// 3. Despawn: Plays pickup SFX and self-destructs.
///
/// VISUAL TINT:
/// Driven by <see cref="ItemRarityColors"/>:
/// - Junk      gray      (#9d9d9d)
/// - Common    white     (#ffffff)
/// - Uncommon  green     (#1eff00)
/// - Rare      blue      (#0070dd)
/// - Epic      purple    (#a335ee)
/// - Legendary orange    (#ff8000)
///
/// RELATED FILES:
/// - ItemPickupFactory.cs: Creates pickup GameObjects
/// - ItemPickupManager.cs: Spawns pickups on enemy death
/// - CoinInstance.cs: Sister behavior (same animation pattern)
/// - ItemRarityColors.cs: Tint source
/// </summary>
public class ItemPickupInstance : MonoBehaviour
{
    #region Animation Curves

    public AnimationCurve linearCurve;
    public AnimationCurve slopeCurve;
    public AnimationCurve sineCurve;

    #endregion

    #region Components

    private SpriteRenderer spriteRenderer;

    #endregion

    #region Configuration

    private float scaleMultiplier = 0.05f;
    private float moveDuration = 0.6f;

    #endregion

    #region State

    private float timeElapsed = 0.0f;
    private Vector3 start;
    private Vector3 end;
    private CoinState state;
    private float t, x, y, z;

    #endregion

    #region Bounce Physics

    private Vector3 velocity;
    private float gravity = -20f;
    private int bouncesRemaining = 6;
    private float bounceDamp = 0.5f;
    private float minBounceVelocity = 1.5f;
    private float groundOffsetY = 0f;

    #endregion

    #region Data

    public ItemDefinition Definition { get; private set; }

    #endregion

    /// <summary>Initializes component references and state.</summary>
    private void Awake()
    {
        spriteRenderer = GetComponent<SpriteRenderer>();
        transform.localScale = g.TileScale * scaleMultiplier;
    }

    /// <summary>Spawns the pickup at <paramref name="position"/> for the given item.</summary>
    public void Spawn(Vector3 position, ItemDefinition def)
    {
        Definition = def;
        if (spriteRenderer != null && def != null)
            spriteRenderer.color = ItemRarityColors.Get(def.Rarity);

        start = position;
        end = g.CoinCounter != null ? g.CoinCounter.GetIconWorldPosition() : position;

        timeElapsed = 0f;
        moveDuration = 0.6f + RNG.Float(0, 0.2f);

        // Explosion impulse
        float angle = RNG.Float(0, 2 * Mathf.PI);
        float force = RNG.Float(0.5f, 1.5f);
        velocity = new Vector3(Mathf.Cos(angle), Mathf.Sin(angle), 0) * force;
        velocity.y = Mathf.Abs(velocity.y) + 4f; // always upward
        bouncesRemaining = RNG.Int(3, 6);

        // Add slight variation to bounce floor
        float maxOffset = g.TileSize * 0.6f;
        groundOffsetY = RNG.Float(-maxOffset, 0);

        transform.position = start;
        state = CoinState.Bounce;
    }

    /// <summary>Runs per-frame update logic.</summary>
    public void Update()
    {
        switch (state)
        {
            case CoinState.Bounce:  Bounce();  break;
            case CoinState.Seek:    Seek();    break;
            case CoinState.Despawn: Despawn(); break;
        }
        timeElapsed += Time.deltaTime;
    }

    /// <summary>Bounce physics (gravity + floor with dampened rebounds).</summary>
    private void Bounce()
    {
        velocity.y += gravity * Time.deltaTime;

        var pos = transform.position;
        pos += velocity * Time.deltaTime;

        if (pos.y <= start.y + groundOffsetY)
        {
            if (bouncesRemaining > 0 && Mathf.Abs(velocity.y) > minBounceVelocity)
            {
                pos.y = start.y + groundOffsetY;
                velocity.y *= -bounceDamp;
                bouncesRemaining--;
            }
            else
            {
                pos.y = start.y + groundOffsetY;
                velocity = Vector3.zero;
                timeElapsed = 0f;
                start = pos;
                end = g.CoinCounter != null ? g.CoinCounter.GetIconWorldPosition() : pos;
                state = CoinState.Seek;
                return;
            }
        }

        transform.position = pos;
    }

    /// <summary>Seek toward the collect endpoint using a sine-eased lerp.</summary>
    private void Seek()
    {
        t = Mathf.Clamp01(timeElapsed / moveDuration);
        x = Mathf.Lerp(start.x, end.x, sineCurve.Evaluate(t));
        y = Mathf.Lerp(start.y, end.y, sineCurve.Evaluate(t));
        z = transform.position.z;
        transform.position = new Vector3(x, y, z);
        if (timeElapsed >= moveDuration)
        {
            timeElapsed = 0;
            state = CoinState.Despawn;
        }
    }

    /// <summary>Fade out + SFX + self-destruct. Loot was already booked at death.</summary>
    private void Despawn()
    {
        if (spriteRenderer != null) spriteRenderer.enabled = false;
        g.AudioManager?.Play("Click");
        Destroy(gameObject);
    }
}
}
