// Handles spark particles that travel along a Synergy line path.
// The strand supplies path samplers so sparks line up exactly.

using Scripts.Libraries;
using System;
using System.Collections.Generic;
using UnityEngine;
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
using Scripts.Managers;
using Scripts.Models;
using Scripts.Models.Actor;
using Scripts.Overworld;
using Scripts.Sequences;
using Scripts.Serialization;
using Scripts.Utilities;

namespace Scripts.Instances.SynergyLine
{
public class SynergySpark
{
    // Spawn window on the path
    [SerializeField] private float minT = 0.01f;
    [SerializeField] private float maxT = 0.08f;

    // Motion
    [SerializeField] private float minBaseSpeed = 0.2f;
    [SerializeField] private float maxBaseSpeed = 0.6f;
    [SerializeField] private float revActiveSpeedMul = 1.2f;

    // Size and lifetime
    [SerializeField] private float minSize = 0.10f;
    [SerializeField] private float maxSize = 0.16f;
    [SerializeField] private float minLifetime = 0.40f;
    [SerializeField] private float maxLifetime = 2.0f;

    // Offset jitter along the local perpendicular
    [SerializeField] private float minOffsetJitter = -1f;
    [SerializeField] private float maxOffsetJitter = 1f;

    // Rate and speed randomization
    [SerializeField] private float spawnRateMin = 10f;
    [SerializeField] private float spawnRateMax = 16f;
    [SerializeField] private float speedMulMin = 0.85f;
    [SerializeField] private float speedMulMax = 1.35f;

    // Sprite library key
    [SerializeField] private string textureKey = "SynergySpark";

    // Particle system objects
    private ParticleSystem sparks;
    private ParticleSystemRenderer sparksRenderer;

    /// <summary>
    /// Spark instance as a reference type for in-place edits.
    /// </summary>
    public class SynergyLineSpark
    {
        public float t;
        public float speed;
        public float size;
        public float age;
        public float lifetime;
        public float offsetJitter;
    }

    private readonly List<SynergyLineSpark> active = new List<SynergyLineSpark>(64);
    private ParticleSystem.Particle[] particleBuffer = new ParticleSystem.Particle[64];
    private float spawnAccum;
    private float spawnRateR;
    private float speedMulR;

    /// <summary>
    /// Create child particle system under parent and set defaults.
    /// </summary>
    public void Init(Transform parent, string spriteKeyOverride = null)
    {
        var sparkGO = new GameObject("Sparks");
        sparkGO.transform.SetParent(parent, false);

        sparks = sparkGO.AddComponent<ParticleSystem>();
        sparksRenderer = sparkGO.GetComponent<ParticleSystemRenderer>();

        var shader = Shader.Find("Particles/Additive");
        if (shader == null) shader = Shader.Find("Universal Render Pipeline/Particles/Unlit");
        var mat = new Material(shader);

        string key = string.IsNullOrEmpty(spriteKeyOverride) ? textureKey : spriteKeyOverride;
        if (SpriteLibrary.Sprites != null && SpriteLibrary.Sprites.ContainsKey(key))
        {
            mat.mainTexture = SpriteLibrary.Sprites[key].texture;
        }

        sparksRenderer.material = mat;
        sparksRenderer.renderMode = ParticleSystemRenderMode.Billboard;

        var main = sparks.main;
        main.playOnAwake = true;
        main.loop = true;
        main.prewarm = true;
        main.simulationSpace = ParticleSystemSimulationSpace.World;
        main.maxParticles = 1024;
        main.startSpeed = 0f;
        main.startLifetime = 1f;
        main.startSize = 0.12f;

        var emission = sparks.emission;
        emission.enabled = false;

        var shape = sparks.shape;
        shape.enabled = false;

        var col = sparks.colorOverLifetime;
        col.enabled = true;
        var grad = new Gradient();
        grad.SetKeys(
            new GradientColorKey[]
            {
                new GradientColorKey(Color.white, 0f),
                new GradientColorKey(Color.white, 0.5f),
                new GradientColorKey(Color.white, 1f)
            },
            new GradientAlphaKey[]
            {
                new GradientAlphaKey(0.0f, 0f),
                new GradientAlphaKey(0.9f, 0.35f),
                new GradientAlphaKey(0.0f, 1f)
            }
        );
        col.color = new ParticleSystem.MinMaxGradient(grad);

        var sizeOL = sparks.sizeOverLifetime;
        sizeOL.enabled = true;
        sizeOL.size = new ParticleSystem.MinMaxCurve(
            1f,
            new AnimationCurve(
                new Keyframe(0.00f, 0.2f),
                new Keyframe(0.35f, 1.0f),
                new Keyframe(1.00f, 0.0f)
            )
        );

        spawnRateR = RNG.Float(spawnRateMin, spawnRateMax);
        speedMulR = RNG.Float(speedMulMin, speedMulMax);

        sparks.Play(true);
    }

    /// <summary>
    /// Set renderer sorting layer and order.
    /// </summary>
    public void SetSorting(string sortingLayer, int order)
    {
        if (sparksRenderer == null) return;
        sparksRenderer.sortingLayerName = sortingLayer;
        sparksRenderer.sortingOrder = order;
    }

    /// <summary>
    /// Set the base tint used for sparks.
    /// </summary>
    public void SetTint(Color tint)
    {
        if (sparks == null) return;
        var main = sparks.main;
        main.startColor = new ParticleSystem.MinMaxGradient(tint);
    }

    /// <summary>
    /// Advance simulation, spawn new sparks, and write particle buffer.
    /// The strand provides the exact path and radius samplers so sparks align perfectly.
    /// </summary>
    public void Tick(
        float fade,
        bool revActive,
        Func<float, Vector3> samplePos,
        Func<float, float> radiusAtT,
        float dt)
    {
        if (sparks == null) return;

        float spawnRate = spawnRateR * Mathf.Clamp01(fade);
        spawnAccum += spawnRate * dt;
        while (spawnAccum >= 1f)
        {
            spawnAccum -= 1f;
            Spawn(revActive);
        }

        UpdateActive(samplePos, radiusAtT, dt);

        if (!sparks.isPlaying) sparks.Play(true);
    }

    /// <summary>
    /// Immediately simulate N steps so sparks appear warmed up.
    /// </summary>
    public void Prewarm(
        float seconds,
        int steps,
        bool revActive,
        Func<float, Vector3> samplePos,
        Func<float, float> radiusAtT)
    {
        if (steps <= 0 || seconds <= 0f) return;
        float dt = seconds / steps;
        for (int i = 0; i < steps; i++)
        {
            Tick(1f, revActive, samplePos, radiusAtT, dt);
        }
    }

    /// <summary>
    /// Clear particles and active list.
    /// </summary>
    public void Clear()
    {
        active.Clear();
        if (sparks != null) sparks.Clear();
    }

    private void Spawn(bool revActiveFlag)
    {
        var s = new SynergyLineSpark();
        s.t = RNG.Float(minT, maxT);

        float baseSpeed = RNG.Float(minBaseSpeed, maxBaseSpeed) * speedMulR;
        s.speed = baseSpeed * (revActiveFlag ? revActiveSpeedMul : 1.0f);

        s.size = RNG.Float(minSize, maxSize);

        float travelT = 1f - s.t;
        float timeNeeded = travelT / Mathf.Max(0.001f, s.speed);
        float padding = timeNeeded * RNG.Float(0.10f, 0.35f);
        s.lifetime = Mathf.Clamp(timeNeeded + padding, minLifetime, maxLifetime);

        s.age = 0f;
        s.offsetJitter = RNG.Float(minOffsetJitter, maxOffsetJitter);

        active.Add(s);
    }

    private void UpdateActive(
        Func<float, Vector3> samplePos,
        Func<float, float> radiusAtT,
        float dt)
    {
        if (active.Count == 0)
        {
            if (sparks != null) sparks.Clear();
            return;
        }

        if (particleBuffer.Length < active.Count)
            particleBuffer = new ParticleSystem.Particle[Mathf.NextPowerOfTwo(active.Count)];

        int alive = 0;
        var tint = sparks.main.startColor.color;

        for (int i = 0; i < active.Count; i++)
        {
            SynergyLineSpark s = active[i];
            s.age += dt;
            s.t += s.speed * dt;

            if (s.t >= 1f || s.age >= s.lifetime)
                continue;

            Vector3 p = samplePos(s.t);

            float tPrev = Mathf.Max(0f, s.t - 0.01f);
            float tNext = Mathf.Min(1f, s.t + 0.01f);
            Vector3 tangent = samplePos(tNext) - samplePos(tPrev);
            if (tangent.sqrMagnitude < 1e-6f) tangent = Vector3.right;

            Vector3 perp = new Vector3(-tangent.y, tangent.x, 0f).normalized;
            float rAtT = Mathf.Max(0f, radiusAtT(s.t));
            p += perp * (s.offsetJitter * 0.12f * rAtT);

            ParticleSystem.Particle pp = new ParticleSystem.Particle();
            pp.position = p;
            pp.remainingLifetime = Mathf.Max(0.01f, s.lifetime - s.age);
            pp.startLifetime = s.lifetime;
            pp.startSize = s.size;
            pp.startColor = tint;
            pp.velocity = Vector3.zero;
            pp.rotation3D = Vector3.zero;

            particleBuffer[alive] = pp;
            alive++;
        }

        if (alive < active.Count)
        {
            int write = 0;
            for (int read = 0; read < active.Count; read++)
            {
                var s = active[read];
                if (s.t < 1f && s.age < s.lifetime)
                    active[write++] = s;
            }
            if (write < active.Count)
                active.RemoveRange(write, active.Count - write);
        }

        sparks.SetParticles(particleBuffer, alive);
    }
}

}
