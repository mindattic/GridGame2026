using UnityEngine;

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
