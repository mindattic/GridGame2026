using UnityEngine;
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
/// BACKGROUNDINSTANCE - Animated background sprite.
/// 
/// PURPOSE:
/// Manages a background sprite that scales to fill the screen
/// and gently sways using sine wave movement.
/// 
/// VISUAL EFFECT:
/// ```
/// Background fills screen and slowly drifts:
///    ↔ horizontal sway
///    ↕ vertical sway
/// ```
/// 
/// INITIALIZATION:
/// - Randomizes sprite on start
/// - Calculates scale to fill screen with padding
/// - Sets up sine wave movement parameters
/// 
/// MOVEMENT:
/// Uses dual sine waves (X and Y) with different speeds
/// for organic, non-repetitive drift effect.
/// 
/// CONFIGURATION:
/// - amplitude: Range of movement (based on padding)
/// - speed: Oscillation frequency
/// 
/// RELATED FILES:
/// - SpriteLibrary.cs: Background sprites
/// - GameManager.cs: Scene setup
/// </summary>
public class BackgroundInstance : MonoBehaviour
{
    // Fields
    private Vector3 initialPosition;       // Starting position of the background
    private SpriteRenderer spriteRenderer; // Cached SpriteRenderer reference
    private Vector2 padding;               // Padding applied to screen fit
    private Vector3 scale;                 // Calculated scale to fit screen
    private Vector2 amplitude;             // Amplitude of sine wave movement
    private Vector2 speed;                  // Speed of sine wave movement
    private float time;                     // Time accumulator for movement

    private void Awake()
    {
        // Cache the SpriteRenderer component
        spriteRenderer = GetComponent<SpriteRenderer>();
    }

    private void Start()
    {
        // Store the initial position
        initialPosition = transform.position;

        // Randomize background sprite
        Randomize();

        // Get screen dimensions in world units
        float screenHeight = Camera.main.orthographicSize * 2f;
        float screenWidth = screenHeight * Camera.main.aspect;

        // Get the sprite's size in world units
        Bounds spriteBounds = spriteRenderer.sprite.bounds;
        Vector2 spriteSize = spriteBounds.size;

        // Calculate padding and scale to fit the screen
        padding = new Vector2(screenWidth * 0.01f, screenHeight * 0.01f);
        scale = new Vector3(
            screenWidth / spriteSize.x + padding.x,
            screenHeight / spriteSize.y + padding.y,
            1
        );
        transform.localScale = scale;

        // Set movement amplitude and speed
        amplitude = new Vector2(padding.x, padding.y);
        speed = new Vector2(0.2f, 0.2f);
    }

    private void FixedUpdate()
    {
        // Update time
        time += Time.deltaTime;

        // Calculate sine wave offset
        float x = initialPosition.x + Mathf.Sin(time * speed.x) * amplitude.x;
        float y = initialPosition.y + Mathf.Sin(time * speed.y) * amplitude.y;

        // Apply movement
        transform.position = new Vector3(x, y, initialPosition.z);
    }

    public void Randomize()
    {
        // Assign a random background sprite
        spriteRenderer.sprite = RNG.Background();
    }
}

}
