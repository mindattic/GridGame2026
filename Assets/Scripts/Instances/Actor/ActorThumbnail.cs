using Scripts.Libraries;
using Scripts.Models;
using UnityEngine;
using g = Scripts.Helpers.GameHelper;
using s = Scripts.Helpers.SettingsHelper;
using Scripts.Canvas;
using Scripts.Data.Actor;
using Scripts.Data.Items;
using Scripts.Data.Skills;
using Scripts.Effects;
using Scripts.Factories;
using Scripts.Helpers;
using Scripts.Hub;
using Scripts.Instances;
using Scripts.Instances.Board;
using Scripts.Instances.SynergyLine;
using Scripts.Inventory;
using Scripts.Managers;
using Scripts.Models.Actor;
using Scripts.Overworld;
using Scripts.Sequences;
using Scripts.Serialization;
using Scripts.Utilities;

namespace Scripts.Instances.Actor
{
/// <summary>
/// ACTORTHUMBNAIL - Animated portrait display for actors.
/// 
/// PURPOSE:
/// Controls the main sprite display for an actor with subtle
/// pan and wobble animation for visual interest.
/// 
/// ANIMATION EFFECTS:
/// - Pan: Slow drift across sprite based on Perlin noise
/// - Wobble: Small oscillating movement for life-like feel
/// - Pause: Periodic pauses in animation cycle
/// 
/// SHADER INTEGRATION:
/// Uses _MainTexOffset shader property to pan the texture
/// UV coordinates without moving the sprite.
/// 
/// CONFIGURATION:
/// - range: Maximum UV pan range
/// - panFocus: Pan animation speed
/// - wobbleAmplitudeFactorX/Y: Wobble intensity
/// - pauseDuration: How long to pause
/// - pauseRampDuration: Ease in/out of pauses
/// 
/// THUMBNAIL SETTINGS:
/// Uses ThumbnailSettings from ActorData to control
/// initial offset and scale for portrait cropping.
/// 
/// RELATED FILES:
/// - ActorRenderers.cs: Owns thumbnail reference
/// - ThumbnailSettings.cs: Cropping configuration
/// - ActorData.cs: Contains ThumbnailSettings
/// </summary>
[RequireComponent(typeof(SpriteRenderer))]
public class ActorThumbnail : MonoBehaviour
{
    private static readonly int MainTexId = Shader.PropertyToID("_MainTex");
    private static readonly int MainTexOffsetId = Shader.PropertyToID("_MainTexOffset");
    private const float Epsilon = 0.0001f;

    private ActorInstance instance;
    public ThumbnailSettings settings;
    private SpriteRenderer spriteRenderer;

    public float rangeMultiplier;
    public Vector2 range;
    public float panFocus;
    public float wobbleAmplitudeFactorX;
    public float wobbleAmplitudeFactorY;
    public float nextPauseInterval;
    public float pauseDuration;
    public float pauseRampDuration;

    private float effectiveNoiseTime;
    private float cycleTime;
    private float cyclePeriod;
    private Vector2 noiseSeed;

    // Properties
    public Texture2D texture => spriteRenderer != null && spriteRenderer.sprite != null ? spriteRenderer.sprite.texture : null;
    public Sprite sprite => spriteRenderer != null ? spriteRenderer.sprite : null;

    public void Awake()
    {
        spriteRenderer = GetComponent<SpriteRenderer>();

        noiseSeed = new Vector2(RNG.Float(0f, 100f), RNG.Float(0f, 100f));

        panFocus = 0.25f;
        effectiveNoiseTime = 0f;
        cycleTime = 0f;

        ResetCycle();

        // Defaults
        range = new Vector2(0.1f, 0.1f);
        wobbleAmplitudeFactorX = 0.2f;
        wobbleAmplitudeFactorY = 0.2f;

        // Avoid NRE if sprite not assigned in editor yet; compute once sprite is set.
        rangeMultiplier = 0f;
        if (spriteRenderer.sprite != null)
        {
            // If a sprite exists on the renderer at Awake, ensure shader has the texture.
            spriteRenderer.material.SetTexture(MainTexId, spriteRenderer.sprite.texture);
            RecalculateRangeMultiplier();
        }

        // Ensure settings exist to avoid null reads in Update before Initialize/Set is called.
        if (settings == null)
        {
            settings = new ThumbnailSettings(Vector2.zero, Vector2.one);
            ApplySettingsToTransform();
        }
    }

    public void Set(Vector3 position, Vector3 scale)
    {
        settings = new ThumbnailSettings(position, scale);
        ApplySettingsToTransform();
    }

    public void Initialize(ActorInstance parentInstance)
    {
        instance = parentInstance;

        var actorData = ActorLibrary.Get(instance.characterClass);

        spriteRenderer.sprite = actorData.Portrait;
        if (spriteRenderer.sprite != null)
        {
            spriteRenderer.material.SetTexture(MainTexId, spriteRenderer.sprite.texture);
        }

        settings = new ThumbnailSettings(actorData.ThumbnailSettings);
        ApplySettingsToTransform();

        RecalculateRangeMultiplier();

        // Override defaults for initialized thumbnails
        range = new Vector2(0.1f, 0.1f);
        wobbleAmplitudeFactorX = 0.25f;
        wobbleAmplitudeFactorY = 0.25f;
    }

    private void ApplySettingsToTransform()
    {
        // Clamp localPosition so portrait cannot be pushed beyond the mask edges based on scale.
        var off = settings.Offset;
        float maxOX = Mathf.Max(0f, (settings.Scale.x - 1f) * 0.5f);
        float maxOY = Mathf.Max(0f, (settings.Scale.y - 1f) * 0.5f);
        off.x = Mathf.Clamp(off.x, -maxOX, maxOX);
        off.y = Mathf.Clamp(off.y, -maxOY, maxOY);

        transform.localPosition = off;
        transform.localScale = settings.Scale;
    }

    private void Update()
    {
        if (spriteRenderer == null || settings == null)
            return;

        // maxOffset simplifies to inverse of scale; range cancels out.
        float invScaleX = 1f / Mathf.Max(settings.Scale.x, Epsilon);
        float invScaleY = 1f / Mathf.Max(settings.Scale.y, Epsilon);
        Vector2 maxOffset = new Vector2(invScaleX, invScaleY);

        // Advance cycle timer
        cycleTime += Time.deltaTime;
        if (cycleTime >= cyclePeriod)
        {
            cycleTime -= cyclePeriod;
            ResetCycle();
        }

        // Determine speed multiplier based on pause cycle
        float multiplier = EvaluatePauseMultiplier(cycleTime);

        // Advance effective noise time
        effectiveNoiseTime += Time.deltaTime * multiplier * panFocus;

        // Perlin noise sampling
        float noiseX = Mathf.PerlinNoise(effectiveNoiseTime, noiseSeed.x);
        float noiseY = Mathf.PerlinNoise(effectiveNoiseTime, noiseSeed.y);

        // Center noise and compute UV offset
        float centeredNoiseX = noiseX - 0.5f;
        float centeredNoiseY = noiseY - 0.5f;

        float wobbleX = centeredNoiseX * maxOffset.x * wobbleAmplitudeFactorX * 0.5f;
        float wobbleY = centeredNoiseY * maxOffset.y * wobbleAmplitudeFactorY * 0.5f;

        float baseOffsetX = maxOffset.x * 0.5f;
        float baseOffsetY = maxOffset.y * 0.5f;

        float offsetX = baseOffsetX + wobbleX;
        float offsetY = baseOffsetY + wobbleY;

        spriteRenderer.material.SetVector(MainTexOffsetId, new Vector4(offsetX, offsetY, 0f, 0f));
    }

    private void RecalculateRangeMultiplier()
    {
        var tex = texture;
        if (tex == null)
        {
            rangeMultiplier = 0f;
            return;
        }

        float textureSize = Mathf.Max(tex.width, tex.height);
        rangeMultiplier = 0.05f * (textureSize / 1024f);
    }

    private void ResetCycle()
    {
        nextPauseInterval = RNG.Float(3f, 7f);
        pauseDuration = RNG.Float(2f, 5f);
        pauseRampDuration = RNG.Float(0.25f, 0.75f);
        cyclePeriod = nextPauseInterval + pauseDuration + 2f * pauseRampDuration;
    }

    private float EvaluatePauseMultiplier(float t)
    {
        float rampDownStart = nextPauseInterval - pauseRampDuration;
        float pauseStart = nextPauseInterval;
        float pauseEnd = nextPauseInterval + pauseDuration;
        float rampUpEnd = pauseEnd + pauseRampDuration;

        if (t < rampDownStart)
            return 1f;

        if (t < pauseStart)
            return Mathf.Lerp(1f, 0f, (t - rampDownStart) / pauseRampDuration);

        if (t < pauseEnd)
            return 0f;

        if (t < rampUpEnd)
            return Mathf.Lerp(0f, 1f, (t - pauseEnd) / pauseRampDuration);

        return 1f;
    }
}
}
