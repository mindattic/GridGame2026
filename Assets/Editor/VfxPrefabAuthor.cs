#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;

/// <summary>
/// VFXPREFABAUTHOR - Editor-only tool that builds custom VFX <c>.prefab</c> files programmatically
/// and saves them to <c>Assets/VisualEffects/</c>. The "VFX exception" to the code-only rule:
/// particle systems are best authored as prefabs because their inspector has dozens of tightly
/// coupled modules — but the AUTHORING stays in code (this file), deterministic and regeneratable.
///
/// <para>Each <c>Author_Xxx</c> menu item produces one prefab. <c>Tools/VFX/Author ALL</c> rebuilds
/// every prefab in one go. After running, paste the suggested registration line(s) into
/// <c>Libraries/VisualEffectLibrary.cs</c> so the runtime can <c>LoadPrefab(...)</c> them.</para>
/// </summary>
public static class VfxPrefabAuthor
{
    private const string VfxFolder = "Assets/VisualEffects";

    [MenuItem("Tools/VFX/Author ALL Custom Prefabs")]
    public static void AuthorAll()
    {
        AuthorIcyWind();
        AuthorFlamingTwist();
        AuthorShockBolt();
        AuthorSleepDust();
        AuthorHealAura();
        AuthorPoisonCloud();
        AuthorAntidoteSparkle();
        AuthorScanRays();
        AuthorSlowShimmer();
        AuthorSilenceMute();
        Debug.Log("[VfxPrefabAuthor] All custom VFX prefabs authored. Don't forget to register them in VisualEffectLibrary.cs.");
    }

    [MenuItem("Tools/VFX/Author 'IcyWind'")]            public static void AuthorIcyWind()         => Author("IcyWind",         IcyWind);
    [MenuItem("Tools/VFX/Author 'FlamingTwist'")]       public static void AuthorFlamingTwist()    => Author("FlamingTwist",    FlamingTwist);
    [MenuItem("Tools/VFX/Author 'ShockBolt'")]          public static void AuthorShockBolt()       => Author("ShockBolt",       ShockBolt);
    [MenuItem("Tools/VFX/Author 'SleepDust'")]          public static void AuthorSleepDust()       => Author("SleepDust",       SleepDust);
    [MenuItem("Tools/VFX/Author 'HealAura'")]           public static void AuthorHealAura()        => Author("HealAura",        HealAura);
    [MenuItem("Tools/VFX/Author 'PoisonCloud'")]        public static void AuthorPoisonCloud()     => Author("PoisonCloud",     PoisonCloud);
    [MenuItem("Tools/VFX/Author 'AntidoteSparkle'")]    public static void AuthorAntidoteSparkle() => Author("AntidoteSparkle", AntidoteSparkle);
    [MenuItem("Tools/VFX/Author 'ScanRays'")]           public static void AuthorScanRays()        => Author("ScanRays",        ScanRays);
    [MenuItem("Tools/VFX/Author 'SlowShimmer'")]        public static void AuthorSlowShimmer()     => Author("SlowShimmer",     SlowShimmer);
    [MenuItem("Tools/VFX/Author 'SilenceMute'")]        public static void AuthorSilenceMute()     => Author("SilenceMute",     SilenceMute);

    // ── Per-spell configurations (compact, distinctive — tune to taste in the inspector) ──

    private static void IcyWind(ParticleSystem ps)
    {
        ConfigureMain(ps, dur: 1.0f, life: 0.9f, speed: 6f, sizeMin: 0.08f, sizeMax: 0.22f,
            colorA: new Color(0.85f, 0.95f, 1f), colorB: new Color(0.55f, 0.80f, 1f), max: 200);
        ConfigureBurst(ps, rate: 80f, burst: 40);
        ConfigureCone(ps, angle: 18f, radius: 0.05f, length: 1.0f);
        EnableSwirl(ps, swirl: 2f);
        FadeAlpha(ps);
    }

    private static void FlamingTwist(ParticleSystem ps)
    {
        ConfigureMain(ps, dur: 0.8f, life: 0.6f, speed: 5f, sizeMin: 0.12f, sizeMax: 0.3f,
            colorA: new Color(1f, 0.85f, 0.3f), colorB: new Color(1f, 0.35f, 0.1f), max: 150);
        ConfigureBurst(ps, rate: 120f, burst: 25);
        ConfigureCone(ps, angle: 25f, radius: 0.08f, length: 0.4f);
        EnableSwirl(ps, swirl: 6f); // strong corkscrew
        FadeAlpha(ps);
    }

    private static void ShockBolt(ParticleSystem ps)
    {
        ConfigureMain(ps, dur: 0.35f, life: 0.25f, speed: 10f, sizeMin: 0.05f, sizeMax: 0.18f,
            colorA: new Color(1f, 1f, 0.6f), colorB: new Color(0.6f, 0.85f, 1f), max: 80);
        ConfigureBurst(ps, rate: 200f, burst: 30);
        ConfigureCone(ps, angle: 4f, radius: 0.02f, length: 0.1f); // tight beam
        FadeAlpha(ps);
    }

    private static void SleepDust(ParticleSystem ps)
    {
        ConfigureMain(ps, dur: 1.2f, life: 1.0f, speed: 1.2f, sizeMin: 0.05f, sizeMax: 0.15f,
            colorA: new Color(0.85f, 0.6f, 1f), colorB: new Color(0.6f, 0.4f, 0.9f), max: 120);
        ConfigureBurst(ps, rate: 40f, burst: 15);
        ConfigureCone(ps, angle: 35f, radius: 0.15f, length: 0.2f);
        EnableSwirl(ps, swirl: 1.5f);
        FadeAlpha(ps);
    }

    private static void HealAura(ParticleSystem ps)
    {
        ConfigureMain(ps, dur: 1.5f, life: 1.2f, speed: 0.8f, sizeMin: 0.06f, sizeMax: 0.18f,
            colorA: new Color(0.7f, 1f, 0.7f), colorB: new Color(0.95f, 1f, 0.85f), max: 100);
        ConfigureBurst(ps, rate: 50f, burst: 20);
        ConfigureShapeCircle(ps, radius: 0.4f);
        EnableUpwardDrift(ps, speed: 1.5f);
        FadeAlpha(ps);
    }

    private static void PoisonCloud(ParticleSystem ps)
    {
        ConfigureMain(ps, dur: 1.4f, life: 1.4f, speed: 0.6f, sizeMin: 0.2f, sizeMax: 0.45f,
            colorA: new Color(0.5f, 0.85f, 0.4f), colorB: new Color(0.35f, 0.5f, 0.2f), max: 90);
        ConfigureBurst(ps, rate: 25f, burst: 18);
        ConfigureShapeCircle(ps, radius: 0.35f);
        EnableUpwardDrift(ps, speed: 0.4f);
        FadeAlpha(ps);
    }

    private static void AntidoteSparkle(ParticleSystem ps)
    {
        ConfigureMain(ps, dur: 0.9f, life: 0.8f, speed: 2f, sizeMin: 0.04f, sizeMax: 0.12f,
            colorA: new Color(1f, 0.92f, 0.55f), colorB: new Color(1f, 1f, 0.85f), max: 80);
        ConfigureBurst(ps, rate: 60f, burst: 35);
        ConfigureShapeCircle(ps, radius: 0.25f);
        EnableUpwardDrift(ps, speed: 2.5f);
        FadeAlpha(ps);
    }

    private static void ScanRays(ParticleSystem ps)
    {
        ConfigureMain(ps, dur: 1.0f, life: 0.9f, speed: 3f, sizeMin: 0.05f, sizeMax: 0.10f,
            colorA: new Color(0.6f, 0.9f, 1f), colorB: new Color(0.85f, 0.95f, 1f), max: 80);
        ConfigureBurst(ps, rate: 50f, burst: 20);
        ConfigureCone(ps, angle: 12f, radius: 0.04f, length: 0.5f);
        FadeAlpha(ps);
    }

    private static void SlowShimmer(ParticleSystem ps)
    {
        ConfigureMain(ps, dur: 1.2f, life: 1.0f, speed: 0.5f, sizeMin: 0.08f, sizeMax: 0.18f,
            colorA: new Color(0.4f, 0.7f, 1f), colorB: new Color(0.6f, 0.85f, 1f), max: 80);
        ConfigureBurst(ps, rate: 30f, burst: 12);
        ConfigureShapeCircle(ps, radius: 0.35f);
        EnableSwirl(ps, swirl: -1.0f); // gentle counter-swirl
        FadeAlpha(ps);
    }

    private static void SilenceMute(ParticleSystem ps)
    {
        ConfigureMain(ps, dur: 0.7f, life: 0.6f, speed: 1.5f, sizeMin: 0.06f, sizeMax: 0.14f,
            colorA: new Color(0.95f, 0.7f, 1f), colorB: new Color(0.6f, 0.4f, 0.8f), max: 60);
        ConfigureBurst(ps, rate: 40f, burst: 20);
        ConfigureShapeCircle(ps, radius: 0.2f);
        FadeAlpha(ps);
    }

    // ── Shared building blocks ──

    private static void Author(string name, System.Action<ParticleSystem> configure)
    {
        var go = new GameObject(name);
        var ps = go.AddComponent<ParticleSystem>();
        ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
        configure(ps);
        ConfigureRenderer(ps);
        SavePrefab(go, name);
    }

    private static void ConfigureMain(ParticleSystem ps, float dur, float life, float speed,
        float sizeMin, float sizeMax, Color colorA, Color colorB, int max)
    {
        var main = ps.main;
        main.duration = dur;
        main.loop = false;
        main.startLifetime = life;
        main.startSpeed = speed;
        main.startSize = new ParticleSystem.MinMaxCurve(sizeMin, sizeMax);
        main.startColor = new ParticleSystem.MinMaxGradient(colorA, colorB);
        main.simulationSpace = ParticleSystemSimulationSpace.World;
        main.maxParticles = max;
        main.playOnAwake = true;
    }

    private static void ConfigureBurst(ParticleSystem ps, float rate, int burst)
    {
        var em = ps.emission;
        em.enabled = true;
        em.rateOverTime = rate;
        em.SetBurst(0, new ParticleSystem.Burst(0f, burst));
    }

    private static void ConfigureCone(ParticleSystem ps, float angle, float radius, float length)
    {
        var sh = ps.shape;
        sh.enabled = true;
        sh.shapeType = ParticleSystemShapeType.Cone;
        sh.angle = angle;
        sh.radius = radius;
        sh.length = length;
    }

    private static void ConfigureShapeCircle(ParticleSystem ps, float radius)
    {
        var sh = ps.shape;
        sh.enabled = true;
        sh.shapeType = ParticleSystemShapeType.Circle;
        sh.radius = radius;
        sh.randomDirectionAmount = 1f;
    }

    private static void EnableSwirl(ParticleSystem ps, float swirl)
    {
        var vel = ps.velocityOverLifetime;
        vel.enabled = true;
        vel.orbitalY = new ParticleSystem.MinMaxCurve(-swirl, swirl);
    }

    private static void EnableUpwardDrift(ParticleSystem ps, float speed)
    {
        var vel = ps.velocityOverLifetime;
        vel.enabled = true;
        vel.y = new ParticleSystem.MinMaxCurve(speed * 0.6f, speed);
    }

    private static void FadeAlpha(ParticleSystem ps)
    {
        var col = ps.colorOverLifetime;
        col.enabled = true;
        var grad = new Gradient();
        grad.SetKeys(
            new[] { new GradientColorKey(Color.white, 0f), new GradientColorKey(Color.white, 1f) },
            new[] { new GradientAlphaKey(0f, 0f), new GradientAlphaKey(1f, 0.15f), new GradientAlphaKey(0f, 1f) });
        col.color = grad;
    }

    private static void ConfigureRenderer(ParticleSystem ps)
    {
        var r = ps.GetComponent<ParticleSystemRenderer>();
        r.renderMode = ParticleSystemRenderMode.Billboard;
        r.material = new Material(FindParticleShader());
        r.sortingLayerName = "VFX";
        r.shadowCastingMode = ShadowCastingMode.Off;
        r.receiveShadows = false;
    }

    /// <summary>Try URP particle shader first, fall back to built-in. Prevents pink-material
    /// breakage when the project switches render pipelines.</summary>
    private static Shader FindParticleShader()
    {
        // Try URP → BiRP → safe fallback ("Sprites/Default" exists in every Unity install).
        var candidates = new[]
        {
            "Universal Render Pipeline/Particles/Unlit",
            "Particles/Standard Unlit",
            "Particles/Additive",
            "Sprites/Default",
        };
        foreach (var name in candidates)
        {
            var s = Shader.Find(name);
            if (s != null) return s;
        }
        return Shader.Find("Hidden/InternalErrorShader"); // last-resort visible-magenta
    }

    private static void SavePrefab(GameObject go, string name)
    {
        if (!AssetDatabase.IsValidFolder(VfxFolder))
            AssetDatabase.CreateFolder("Assets", "VisualEffects");
        string path = $"{VfxFolder}/{name}.prefab";
        PrefabUtility.SaveAsPrefabAsset(go, path);
        Object.DestroyImmediate(go);
        AssetDatabase.Refresh();
        RegisterAddressable(path, $"VisualEffects/{name}");
        Debug.Log($"[VfxPrefabAuthor] {path} saved + registered as Addressable 'VisualEffects/{name}'.\n" +
                  $"If new, also add to Libraries/VisualEffectLibrary.cs:\n" +
                  $"  {{ \"{name}\", new VisualEffectAsset {{ Name = \"{name}\", Prefab = LoadPrefab(\"VisualEffects/{name}\"), Duration = 1.2f, IsLooping = false }} }}");
    }

    /// <summary>Adds the prefab to the default Addressables group so the runtime LoadPrefab
    /// address resolves — previously a silent manual step that was easy to forget (the Shuriken
    /// library-entry gap shipped that way).</summary>
    private static void RegisterAddressable(string assetPath, string address)
    {
        var settings = UnityEditor.AddressableAssets.AddressableAssetSettingsDefaultObject.Settings;
        if (settings == null)
        {
            Debug.LogWarning("[VfxPrefabAuthor] AddressableAssetSettings missing — prefab saved but not addressable.");
            return;
        }
        string guid = AssetDatabase.AssetPathToGUID(assetPath);
        if (string.IsNullOrEmpty(guid)) return;
        var entry = settings.CreateOrMoveEntry(guid, settings.DefaultGroup);
        entry.address = address;
        settings.SetDirty(UnityEditor.AddressableAssets.Settings.AddressableAssetSettings.ModificationEvent.EntryModified, entry, true);
    }
}
#endif
