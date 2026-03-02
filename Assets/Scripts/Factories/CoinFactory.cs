using Scripts.Helpers;
using Scripts.Libraries;
using UnityEngine;
using UnityEngine.Rendering;
using Scripts.Canvas;
using Scripts.Data.Actor;
using Scripts.Data.Items;
using Scripts.Data.Skills;
using Scripts.Effects;
using Scripts.Hub;
using Scripts.Instances;
using Scripts.Instances.Actor;
using Scripts.Instances.Board;
using Scripts.Instances.SynergyLine;
using Scripts.Inventory;
using Scripts.Managers;
using Scripts.Models;
using Scripts.Models.Actor;
using Scripts.Overworld;
using Scripts.Sequences;
using Scripts.Serialization;
using Scripts.Utilities;

namespace Scripts.Factories
{
    /// <summary>
    /// COINFACTORY - Creates coin pickup GameObjects.
    /// 
    /// PURPOSE:
    /// Creates animated coin objects that spawn when enemies die.
    /// Coins fly toward the coin counter UI and add to player currency.
    /// 
    /// VISUAL EFFECT:
    /// ```
    /// [Enemy dies] → 🪙 (coin spawns)
    ///                  ↘
    ///                   [Coin Counter] (coin flies to UI)
    /// ```
    /// 
    /// CREATED HIERARCHY:
    /// ```
    /// Coin (root)
    /// ├── SpriteRenderer (coin sprite)
    /// ├── ParticleSystem (sparkle effects)
    /// ├── SortingGroup (render order)
    /// └── CoinInstance (behavior/animation)
    /// ```
    /// 
    /// CONFIGURATION:
    /// - Tag: "Powerup"
    /// - Color: Gold (1, 0.87, 0, 1)
    /// - SortingLayer: Props
    /// 
    /// ANIMATION CURVES:
    /// - linearCurve: Constant speed movement
    /// - slopeCurve: Ease-out movement
    /// - sineCurve: Bounce/wave motion
    /// 
    /// NOTE: Animator removed - CoinInstance handles motion via curves.
    /// 
    /// CALLED BY:
    /// - CoinManager.Spawn()
    /// 
    /// RELATED FILES:
    /// - CoinInstance.cs: Animation behavior
    /// - CoinManager.cs: Spawns coins on enemy death
    /// - CoinCounter.cs: UI displaying total
    /// - DeathHelper.cs: Triggers coin spawn
    /// </summary>
    public static class CoinFactory
    {
        /// <summary>Creates a new coin GameObject with full configuration.</summary>
        public static GameObject Create(Transform parent = null)
        {
            var root = new GameObject("Coin");
            root.layer = 0;
            root.tag = "Powerup";

            var transform = root.transform;
            transform.localPosition = Vector3.zero;
            transform.localRotation = Quaternion.identity;
            transform.localScale = Vector3.one;

            // SpriteRenderer
            var spriteRenderer = root.AddComponent<SpriteRenderer>();
            spriteRenderer.sprite = SpriteLibrary.Sprites["Coin"];
            spriteRenderer.color = new Color(1f, 0.8745098f, 0.003921569f, 1f);
            spriteRenderer.shadowCastingMode = ShadowCastingMode.Off;
            spriteRenderer.receiveShadows = false;
            spriteRenderer.sortingLayerName = "Props";
            spriteRenderer.sortingOrder = 800;
            spriteRenderer.drawMode = SpriteDrawMode.Simple;

            // NOTE: Animator removed - CoinInstance handles animation via curves
            // If you need a spinning coin animation, add an AnimatorController to
            // Resources/Animations/Coin.controller and uncomment below:
            // var animator = root.AddComponent<Animator>();
            // animator.runtimeAnimatorController = AssetHelper.LoadAsset<RuntimeAnimatorController>("Animations/Coin");

            // CoinInstance with animation curves
            var coinInstance = root.AddComponent<CoinInstance>();
            coinInstance.linearCurve = CreateLinearCurve();
            coinInstance.slopeCurve = CreateSlopeCurve();
            coinInstance.sineCurve = CreateSineCurve();

            // ParticleSystem - configure BEFORE it starts playing
            var particleSystem = root.AddComponent<ParticleSystem>();
            particleSystem.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            ConfigureParticleSystem(particleSystem);

            // ParticleSystemRenderer
            var particleRenderer = root.GetComponent<ParticleSystemRenderer>();
            ConfigureParticleRenderer(particleRenderer);

            // SortingGroup
            var sortingGroup = root.AddComponent<SortingGroup>();
            sortingGroup.sortingLayerName = "Props";
            sortingGroup.sortingOrder = 0;

            // Parent if specified
            if (parent != null)
            {
                transform.SetParent(parent, false);
            }

            return root;
        }

        private static AnimationCurve CreateLinearCurve()
        {
            return new AnimationCurve(
                new Keyframe(0f, 0f, 1f, 1f),
                new Keyframe(1f, 1f, 1f, 1f)
            );
        }

        private static AnimationCurve CreateSlopeCurve()
        {
            return new AnimationCurve(
                new Keyframe(0f, 0f, 0f, 0f),
                new Keyframe(1f, 1f, 2f, 2f)
            );
        }

        private static AnimationCurve CreateSineCurve()
        {
            return new AnimationCurve(
                new Keyframe(0f, 0f, 0f, 0f),
                new Keyframe(1f, 1f, 0f, 0f)
            );
        }

        private static void ConfigureParticleSystem(ParticleSystem ps)
        {
            // Main module
            var main = ps.main;
            main.duration = 1f;
            main.loop = true;
            main.prewarm = true;
            main.startDelay = 0f;
            main.startLifetime = 1f;
            main.startSpeed = 5f;
            main.startSize = 1f;
            main.startColor = new Color(1f, 0.8745098f, 0.003921569f, 1f); // Gold
            main.gravityModifier = 0.1f;
            main.simulationSpace = ParticleSystemSimulationSpace.Local;
            main.scalingMode = ParticleSystemScalingMode.Hierarchy;
            main.playOnAwake = true;
            main.maxParticles = 1000;

            // Emission module
            var emission = ps.emission;
            emission.enabled = true;
            emission.rateOverTime = 2f;

            // Shape module (Circle)
            var shape = ps.shape;
            shape.enabled = true;
            shape.shapeType = ParticleSystemShapeType.Circle;
            shape.radius = 1f;
            shape.radiusThickness = 1f;
            shape.arc = 360f;
            shape.randomDirectionAmount = 1f;
            shape.randomPositionAmount = 2f;

            // Color over lifetime module
            var colorOverLifetime = ps.colorOverLifetime;
            colorOverLifetime.enabled = true;
            var gradient = new Gradient();
            gradient.SetKeys(
                new GradientColorKey[]
                {
                    new GradientColorKey(Color.cyan, 0f),
                    new GradientColorKey(Color.cyan, 1f)
                },
                new GradientAlphaKey[]
                {
                    new GradientAlphaKey(1f, 0f),
                    new GradientAlphaKey(0f, 1f)
                }
            );
            colorOverLifetime.color = gradient;

            // Disable unused modules
            var velocityOverLifetime = ps.velocityOverLifetime;
            velocityOverLifetime.enabled = false;

            var sizeOverLifetime = ps.sizeOverLifetime;
            sizeOverLifetime.enabled = false;

            var rotationOverLifetime = ps.rotationOverLifetime;
            rotationOverLifetime.enabled = false;

            var noise = ps.noise;
            noise.enabled = false;

            var collision = ps.collision;
            collision.enabled = false;

            var trails = ps.trails;
            trails.enabled = false;

            var lights = ps.lights;
            lights.enabled = false;
        }

        private static void ConfigureParticleRenderer(ParticleSystemRenderer renderer)
        {
            renderer.enabled = true;
            renderer.renderMode = ParticleSystemRenderMode.Billboard;
            renderer.sortMode = ParticleSystemSortMode.None;
            renderer.minParticleSize = 0.01f;
            renderer.maxParticleSize = 0.03f;
            renderer.shadowCastingMode = ShadowCastingMode.Off;
            renderer.receiveShadows = false;
            renderer.sortingLayerName = "Default";
            renderer.sortingOrder = 0;

            // Material - use default particle material or load custom
            // The prefab uses a custom material at guid: 2990b7fd8aa06b242b9f37f42e97bb7d
            // You may need to load this from Resources or use a default
            renderer.material = new Material(Shader.Find("Particles/Standard Unlit"));
        }
    }
}
