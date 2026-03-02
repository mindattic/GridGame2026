// --- File: Assets/Scripts/Instances/SynergyLine/SynergyStrand.cs ---

using UnityEngine;
using UnityEditor;
using Scripts.Canvas;
using Scripts.Data.Actor;
using Scripts.Data.Items;
using Scripts.Data.Skills;
using Scripts.Effects;
using Scripts.Factories;
using Scripts.Helpers;
using Scripts.Hub;
using Scripts.Instances;
using Scripts.Instances.Actor;
using Scripts.Instances.Board;
using Scripts.Inventory;
using Scripts.Libraries;
using Scripts.Managers;
using Scripts.Models;
using Scripts.Models.Actor;
using Scripts.Overworld;
using Scripts.Sequences;
using Scripts.Serialization;
using Scripts.Utilities;

namespace Scripts.Instances.SynergyLine
{
/// <summary>
/// SYNERGYSTRAND - Single animated strand of a synergy line (alternate version).
/// 
/// PURPOSE:
/// Renders one waveform strand with sine wave motion, Perlin noise
/// jitter, halo glow, and rev burst effects.
/// 
/// VISUAL EFFECT:
/// ```
/// [Anchor A] ≈≈≈≈≈≈≈≈≈≈≈≈≈ [Anchor B]
///            ↑ animated wave
/// ```
/// 
/// FEATURES:
/// - Sine wave motion with configurable frequency
/// - Perlin noise jitter for organic feel
/// - Halo glow with pulse animation
/// - Rev bursts (random amplitude spikes)
/// - Prewarm for instant visual feedback
/// 
/// NOTE:
/// Similar to SynergyLineStrand but may have different parameters
/// or be used in different contexts.
/// 
/// RELATED FILES:
/// - SynergyLineStrand.cs: Primary strand implementation
/// - SynergyLineInstance.cs: Parent multi-strand line
/// - SynergyLineManager.cs: Manages all synergy lines
/// </summary>
[RequireComponent(typeof(LineRenderer))]
public class SynergyStrand : MonoBehaviour
{
    #region Core Settings

    [SerializeField] private float coreAlpha = 0.55f;

    #endregion

    #region Halo Settings

    [SerializeField] private bool haloRandomize = true;
    [SerializeField] private Vector2 haloWidthScaleRange = new Vector2(2.2f, 3.1f);
    [SerializeField] private Vector2 haloAlphaRange = new Vector2(0.14f, 0.26f);
    [SerializeField] private Vector2 haloPulseAmpRange = new Vector2(0.22f, 0.36f);
    [SerializeField] private Vector2 haloPulseSpeedMultRange = new Vector2(0.75f, 1.25f);
    [SerializeField] private Vector2 haloHDRBoostRange = new Vector2(1.10f, 1.60f);
    [SerializeField] private Vector2 haloPhaseOffsetRange = new Vector2(0.0f, 6.283185f);

    #endregion

    #region Rev Burst Settings

    [SerializeField] private float revChancePerSecond = 0.12f;
    [SerializeField] private float revPeakMultiplier = 2.2f;
    [SerializeField] private float revAccelTime = 0.20f;
    [SerializeField] private float revDecelTime = 0.60f;
    [SerializeField] private float revCooldownMin = 0.60f;
    [SerializeField] private float revCooldownMax = 1.60f;

    #endregion

    #region Prewarm Settings

    [SerializeField] private float prewarmSeconds = 0.25f;
    [SerializeField] private int prewarmSteps = 16;

    #endregion

    #region Components

    private LineRenderer line;
    private LineRenderer glow;

    #endregion

    #region Endpoints

    private Transform a;
    private Transform b;

    #endregion

    #region Strand Parameters

    private float phaseOffset;
    private float widthAbs;
    private float radiusAbs;

    #endregion

    #region Geometry Settings

    [SerializeField] private float frequency = 2.2f;
    [SerializeField] private float noiseAmplitude = 0.015f;
    [SerializeField] private float noiseScale = 2.5f;
    [SerializeField] private float noiseSpeed = 0.18f;
    [SerializeField] private AnimationCurve radiusOverT = AnimationCurve.Linear(0f, 1f, 1f, 1f);

    private float fade;
    private float noiseSeed;
    private bool configured;

    // Tropical tint state
    private int strandIndex;
    private float weightNorm;
    [SerializeField] private float hueSpeed = 0.06f;
    [SerializeField] private float huePhase = 0.12f;
    [SerializeField] private float hueRange = 0.06f;
    [SerializeField] private float satBase = 0.90f;
    [SerializeField] private float satRange = 0.08f;
    [SerializeField] private float valBase = 1.00f;
    [SerializeField] private float valPulseAmp = 0.08f;
    [SerializeField] private float valPulseSpeed = 0.70f;

    // Base green in HSV
    private static readonly Color BaseGreenRGB = new Color(0.25f, 1.00f, 0.25f);
    private static float BaseHue = 0.42f;
    private static float BaseSat = 0.9f;
    private static float BaseVal = 1.0f;
    private static bool BaseHSVInit = false;

    // Halo controls (inputs)
    [SerializeField] private bool useHalo = true;
    [SerializeField] private float glowWidthScale = 2.6f;
    [SerializeField] private float glowAlpha = 0.20f;
    [SerializeField] private float glowPulseAmp = 0.28f;
    [SerializeField] private float glowPulseSpeed = 0.90f;
    [SerializeField] private float glowHDRBoost = 1.35f;

    // Halo randomized instance values
    private float glowWidthScaleR;
    private float glowAlphaR;
    private float glowPulseAmpR;
    private float glowPulseSpeedR;
    private float glowHDRBoostR;
    private float glowPhaseOffsetR;

    // Derived halo rate
    private float glowAlphaPulseSpeed;

    // Geometry
    private int segmentCount = 32;

    // Shader property ids
    private static int idBaseColor = -1;
    private static int idColor = -1;

    // Rev state
    private float pathTime;
    private bool revActive;
    private float revElapsed;
    private float revCooldown;

    // External spark system
    private SynergySpark sparkSystem = new SynergySpark();

    /// <summary>
    /// Sets up renderers, shader ids, HSV base, rev cooldown, and spark system.
    /// Initializes random seeds.
    /// </summary>
    private void Awake()
    {
        line = GetComponent<LineRenderer>();
        if (line == null) line = gameObject.AddComponent<LineRenderer>();
        if (line.material != null) line.material = new Material(line.material);
        SetupLineRenderer(line);

        var glowGO = new GameObject("Glow");
        glowGO.transform.SetParent(transform, false);
        glow = glowGO.AddComponent<LineRenderer>();
        glow.material = line.material != null ? new Material(line.material) : null;
        SetupLineRenderer(glow);

        sparkSystem.Init(transform);

        noiseSeed = RNG.Float(0f, 1000f);
        pathTime = RNG.Float(0f, 1000f);

        if (idBaseColor == -1) idBaseColor = Shader.PropertyToID("_BaseColor");
        if (idColor == -1) idColor = Shader.PropertyToID("_Color");

        if (!BaseHSVInit)
        {
            Color.RGBToHSV(BaseGreenRGB, out BaseHue, out BaseSat, out BaseVal);
            BaseHSVInit = true;
        }

        revCooldown = RNG.Float(revCooldownMin, revCooldownMax);
    }

    /// <summary>
    /// Initialize a LineRenderer with consistent defaults.
    /// </summary>
    private void SetupLineRenderer(LineRenderer lr)
    {
        lr.useWorldSpace = true;
        lr.textureMode = LineTextureMode.Stretch;
        lr.alignment = LineAlignment.View;
        lr.numCornerVertices = 3;
        lr.numCapVertices = 3;
        lr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        lr.receiveShadows = false;
    }

    /// <summary>
    /// Configure per-strand values that actually vary between instances.
    /// Leaves color, noise, halo, and waveform resolution to this class defaults.
    /// </summary>
    public void Configure(
        Transform start,
        Transform end,
        float widthAbsolute,
        float radiusAbsolute,
        float phase,
        string sortingLayer,
        int sortingOrder,
        int inStrandIndex,
        float inWeightNorm,
        int inSegmentCount
    )
    {
        a = start;
        b = end;

        widthAbs = Mathf.Max(0.0025f, widthAbsolute);
        radiusAbs = Mathf.Max(0.01f, radiusAbsolute);
        phaseOffset = phase;

        strandIndex = inStrandIndex;
        weightNorm = Mathf.Clamp01(inWeightNorm);

        segmentCount = Mathf.Max(2, inSegmentCount);
        line.positionCount = segmentCount;
        glow.positionCount = segmentCount;

        if (haloRandomize)
        {
            glowWidthScaleR = RNG.Float(haloWidthScaleRange.x, haloWidthScaleRange.y);
            glowAlphaR = RNG.Float(haloAlphaRange.x, haloAlphaRange.y);
            glowPulseAmpR = RNG.Float(haloPulseAmpRange.x, haloPulseAmpRange.y);
            float speedMult = RNG.Float(haloPulseSpeedMultRange.x, haloPulseSpeedMultRange.y);
            glowPulseSpeedR = glowPulseSpeed * speedMult;
            glowHDRBoostR = RNG.Float(haloHDRBoostRange.x, haloHDRBoostRange.y);
            glowPhaseOffsetR = RNG.Float(haloPhaseOffsetRange.x, haloPhaseOffsetRange.y);
        }
        else
        {
            glowWidthScaleR = glowWidthScale;
            glowAlphaR = glowAlpha;
            glowPulseAmpR = glowPulseAmp;
            glowPulseSpeedR = glowPulseSpeed;
            glowHDRBoostR = glowHDRBoost;
            glowPhaseOffsetR = 0f;
        }

        glowAlphaPulseSpeed = glowPulseSpeedR * 1.3f;

        line.sortingLayerName = sortingLayer;
        line.sortingOrder = sortingOrder;
        glow.sortingLayerName = sortingLayer;
        glow.sortingOrder = sortingOrder - 1;

        sparkSystem.SetSorting(sortingLayer, sortingOrder + 2);

        configured = true;

        Prewarm(prewarmSeconds, prewarmSteps);
    }

    /// <summary>
    /// External fade and pins the core width.
    /// </summary>
    public void SetFade(float k)
    {
        fade = Mathf.Clamp01(k);
        line.widthMultiplier = widthAbs;
    }

    /// <summary>
    /// Per frame update of color, halo, geometry, rev motion, and sparks.
    /// </summary>
    public void Tick()
    {
        if (!configured || a == null || b == null) return;

        Color greenTint = ComputeTintedGreen();

        Color core = greenTint;
        core.a *= fade * coreAlpha;

        Color haloC = greenTint;
        float alphaPulse = 0.65f + 0.35f * Mathf.Sin(Time.time * glowAlphaPulseSpeed + strandIndex + glowPhaseOffsetR);
        haloC.a = Mathf.Clamp01(glowAlphaR * alphaPulse * Mathf.Pow(fade, 0.9f));
        Color haloHDR = haloC * glowHDRBoostR;

        ApplyColor(line, core);
        if (useHalo) ApplyColor(glow, haloHDR);

        var sparkTint = greenTint * 1.35f;
        sparkTint.a = 0.9f;
        sparkSystem.SetTint(sparkTint);

        Vector3 start = a.position; start.z = 0f;
        Vector3 end = b.position; end.z = 0f;

        Vector3 dir = end - start;
        float len = dir.magnitude;
        if (len < 0.0001f)
        {
            if (line.positionCount != 2) line.positionCount = 2;
            if (glow.positionCount != 2) glow.positionCount = 2;
            line.SetPosition(0, start); line.SetPosition(1, start);
            glow.SetPosition(0, start); glow.SetPosition(1, start);

            sparkSystem.Clear();
            return;
        }

        Vector3 forward = dir / len;
        Vector3 perp = new Vector3(-forward.y, forward.x, 0f);

        if (line.positionCount != segmentCount) line.positionCount = segmentCount;
        if (glow.positionCount != segmentCount) glow.positionCount = segmentCount;

        float twoPi = Mathf.PI * 2f;

        float envelope = 1f + Mathf.Sin(Time.time * 0.35f + phaseOffset * 0.7f) * 0.35f;

        if (useHalo)
        {
            float haloWidthPulse = 1f + Mathf.Sin(Time.time * glowPulseSpeedR + strandIndex + glowPhaseOffsetR) * glowPulseAmpR;
            glow.widthMultiplier = widthAbs * glowWidthScaleR * haloWidthPulse;
        }

        float timeWarp = UpdateRevAndGetTimeWarp(Time.deltaTime);
        pathTime += Time.deltaTime * timeWarp;

        for (int i = 0; i < segmentCount; i++)
        {
            float t = i / (float)(segmentCount - 1);
            Vector3 p = EvaluatePathPoint(start, end, perp, envelope, twoPi, t);
            line.SetPosition(i, p);
            glow.SetPosition(i, p);
        }

        System.Func<float, Vector3> sampler = (t) => EvaluatePathPoint(start, end, perp, envelope, twoPi, t);
        System.Func<float, float> radiusSampler = (t) => radiusAbs * radiusOverT.Evaluate(t) * envelope;

        sparkSystem.Tick(
            fade,
            revActive,
            sampler,
            radiusSampler,
            Time.deltaTime
        );
    }

    /// <summary>
    /// Simulate strand and sparks forward so first visible frame is already settled.
    /// </summary>
    public void Prewarm(float seconds, int steps)
    {
        if (!configured || a == null || b == null) return;
        if (seconds <= 0f || steps <= 0) return;

        Vector3 start = a.position; start.z = 0f;
        Vector3 end = b.position; end.z = 0f;

        Vector3 dir = end - start;
        float len = dir.magnitude;
        if (len < 0.0001f) return;

        Vector3 forward = dir / len;
        Vector3 perp = new Vector3(-forward.y, forward.x, 0f);

        float twoPi = Mathf.PI * 2f;
        float dt = seconds / steps;

        for (int i = 0; i < steps; i++)
        {
            float env = 1f;

            float timeWarp = UpdateRevAndGetTimeWarp(dt);
            pathTime += dt * timeWarp;

            for (int j = 0; j < segmentCount; j++)
            {
                float t = j / (float)(segmentCount - 1);
                Vector3 p = EvaluatePathPoint(start, end, perp, env, twoPi, t);
                line.SetPosition(j, p);
                glow.SetPosition(j, p);
            }

            System.Func<float, Vector3> sampler = (t) => EvaluatePathPoint(start, end, perp, env, twoPi, t);
            System.Func<float, float> radiusSampler = (t) => radiusAbs * radiusOverT.Evaluate(t) * env;

            sparkSystem.Prewarm(dt, 1, revActive, sampler, radiusSampler);
        }
    }

    /// <summary>
    /// Clear renderers when despawning.
    /// </summary>
    public void Clear()
    {
        if (line != null) line.positionCount = 0;
        if (glow != null) glow.positionCount = 0;
        sparkSystem.Clear();
        configured = false;
    }

    private void ApplyColor(LineRenderer lr, Color c)
    {
        lr.startColor = c;
        lr.endColor = c;

        var mat = lr.material;
        if (mat != null)
        {
            if (mat.HasProperty(idBaseColor)) mat.SetColor(idBaseColor, c);
            else if (mat.HasProperty(idColor)) mat.SetColor(idColor, c);
        }
    }

    private Color ComputeTintedGreen()
    {
        float t = Time.time;

        float hueOffset = Mathf.Sin(t * hueSpeed + strandIndex * huePhase) * hueRange;
        float h = Mathf.Repeat(BaseHue + hueOffset, 1f);

        float s = Mathf.Clamp01(satBase + satRange * weightNorm);

        float vPulse = Mathf.Sin(t * valPulseSpeed + strandIndex * 0.4f);
        float v = Mathf.Clamp01(valBase + valPulseAmp * vPulse);

        return Color.HSVToRGB(h, s, v);
    }

    private float UpdateRevAndGetTimeWarp(float dt)
    {
        if (!revActive)
        {
            revCooldown -= dt;
            if (revCooldown <= 0f)
            {
                if (RNG.Percent < revChancePerSecond * dt)
                {
                    revActive = true;
                    revElapsed = 0f;
                }
            }
        }

        if (!revActive) return 1f;

        revElapsed += dt;

        float acc = Mathf.Max(0.0001f, revAccelTime);
        float dec = Mathf.Max(0.0001f, revDecelTime);
        float total = acc + dec;

        float k;
        if (revElapsed <= acc)
        {
            float u = Mathf.Clamp01(revElapsed / acc);
            k = Mathf.SmoothStep(0f, 1f, u);
        }
        else if (revElapsed <= total)
        {
            float u = Mathf.Clamp01((revElapsed - acc) / dec);
            k = 1f - Mathf.SmoothStep(0f, 1f, u);
        }
        else
        {
            revActive = false;
            revCooldown = RNG.Float(revCooldownMin, revCooldownMax);
            return 1f;
        }

        float peak = Mathf.Max(1f, revPeakMultiplier);
        return 1f + (peak - 1f) * k;
    }

    private Vector3 EvaluatePathPoint(Vector3 start, Vector3 end, Vector3 perp, float envelope, float twoPi, float t)
    {
        float radius = radiusAbs * radiusOverT.Evaluate(t) * envelope;

        float sin = Mathf.Sin(twoPi * frequency * t + phaseOffset + pathTime * 0.18f);
        float n = (Mathf.PerlinNoise(noiseSeed + t * noiseScale, pathTime * noiseSpeed) - 0.5f) * 2f;

        Vector3 basePos = Vector3.Lerp(start, end, t);
        Vector3 offset = perp * ((sin + n) * radius);
        return basePos + offset;
    }

    /// <summary>
    /// Change only the sorting layer, preserving per-strand relative order.
    /// </summary>
    public void SetSortingLayer(string sortingLayer)
    {
        if (line != null) line.sortingLayerName = sortingLayer;
        if (glow != null) glow.sortingLayerName = sortingLayer;
        sparkSystem.SetSorting(sortingLayer, (line != null ? line.sortingOrder : 0) + 2);
    }

    #endregion
}

}
