using Assets.Scripts.Libraries;
using Assets.Scripts.Models;
using Assets.Scripts.Sequences;
using System.Collections;
using UnityEngine;
using g = Assets.Helpers.GameHelper;

/// <summary>
/// PROJECTILEMANAGER - Spawns and controls projectiles.
/// 
/// PURPOSE:
/// Central manager for spawning projectiles with various
/// motion styles (straight, wiggle, lobbed, spiral).
/// 
/// SPAWN FLOW:
/// 1. Optional spawn VFX plays at start position
/// 2. Projectile created with trail VFX
/// 3. Projectile travels using MotionStyle
/// 4. Impact VFX plays on arrival
/// 5. Optional callback routine runs
/// 
/// CONVENIENCE METHODS:
/// - EnqueueHeal: Wiggle path healing projectile
/// - EnqueueFireball: Lobbed arc fire projectile
/// 
/// USAGE:
/// ```csharp
/// g.ProjectileManager.Spawn(settings);
/// yield return g.ProjectileManager.SpawnRoutine(settings);
/// ```
/// 
/// RELATED FILES:
/// - ProjectileSettings.cs: Configuration
/// - ProjectileInstance.cs: Runtime behavior
/// - FireProjectileSequence.cs: Sequence wrapper
/// </summary>
public class ProjectileManager : MonoBehaviour
{
    /// <summary>
    /// Queue a healing projectile sequence. Wiggle motion and heal VFX on impact.
    /// </summary>
    public void EnqueueHeal(Vector3 startPosition, ActorInstance target)
    {
        if (target == null) return;

        var heal = new ProjectileSettings
        {
            friendlyName = "Heal",
            startPosition = startPosition,
            target = target,

            // default visual keys if caller hasn't set them elsewhere
            spawnVfxKey = null,
            projectileVfxKey = "GreenSparkle",
            impactVfxKey = "BuffLife",
            routine = target.HealRoutine(10),

            motionStyle = MotionStyle.Wiggle,
            travelSeconds = 10.9f,
            wiggleAmplitudeTiles = 3.35f,
            wiggleHz = 3.5f,
            arriveRadiusTiles = 0.1f
        };

        g.SequenceManager.Add(new FireProjectileSequence(heal));
    }

    /// <summary>
    /// Queue a fireball projectile sequence. Lobbed arc and explosion VFX on impact.
    /// </summary>
    public void EnqueueFireball(Vector3 startPosition, ActorInstance target)
    {
        if (target == null) return;

        var fireball = new ProjectileSettings
        {
            friendlyName = "Fireball",
            startPosition = startPosition,
            target = target,

            spawnVfxKey = null,
            projectileVfxKey = "Fireball",
            impactVfxKey = "PuffyExplosion",
            routine = target.FireDamageRoutine(10),

            motionStyle = MotionStyle.LobbedArc,
            travelSeconds = 0.8f,
            lobbedHeightTiles = 0.9f,
            arriveRadiusTiles = 0.1f
        };

        g.SequenceManager.Add(new FireProjectileSequence(fireball));
    }


    /// <summary>
    /// Queue a healing projectile that homes in using a tightening spiral and plays heal VFX on impact.
    /// </summary>
    public void EnqueueHomingSpiral(Vector3 startPosition, ActorInstance target)
    {
        if (target == null) return;

        var heal = new ProjectileSettings
        {
            friendlyName = "Heal",
            startPosition = startPosition,
            target = target,

            // Visuals
            spawnVfxKey = null,
            projectileVfxKey = "GoldSparkle",
            impactVfxKey = "BuffLife",
            routine = target.HealRoutine(10),

            // Motion
            motionStyle = MotionStyle.HomingSpiral,
            travelSeconds = 1.0f,
            arriveRadiusTiles = 0.1f,

            // Spiral specific
            spiralTurns = 3,
            spiralStartRadiusTiles = 0.9f,

            faceDirection = false
        };

        g.SequenceManager.Add(new FireProjectileSequence(heal));
    }



    /// <summary>
    /// Backward compatible despawn by instance name. Delegates to TrailManager and VfxManager.
    /// </summary>
    public void Despawn(string instanceName)
    {
        if (string.IsNullOrEmpty(instanceName)) return;

        if (g.VisualEffectManager != null)
            g.VisualEffectManager.Despawn(instanceName);
    }

    /// <summary>
    /// Fire and forget spawn.
    /// </summary>
    public void Spawn(ProjectileSettings s)
    {
        StartCoroutine(SpawnRoutine(s));
    }

    /// <summary>
    /// Creates the node, optionally plays a spawn VFX and waits for its TriggerAt apex,
    /// attaches one projectile trail, travels, plays impact, yields routine, cleans up.
    /// </summary>
    public IEnumerator SpawnRoutine(ProjectileSettings s)
    {
        if (s == null) yield break;

        // Resolve destination
        Transform targetTf = s.target != null ? s.target.transform : s.followTarget;
        Vector3 end = targetTf != null ? targetTf.position : s.staticTargetPosition;

        Vector3 start = s.startPosition;

        // Optional spawn VFX gate at start
        if (!string.IsNullOrEmpty(s.spawnVfxKey) && g.VisualEffectManager != null)
        {
            var spawnAsset = VisualEffectLibrary.Get(s.spawnVfxKey);
            if (spawnAsset != null)
            {
                var (spawnInst, spawnRoutine) = g.VisualEffectManager.SpawnAndWait(spawnAsset, start);
                // Wait until the spawn reaches its apex
                if (spawnInst != null)
                    yield return spawnInst.WaitUntilTrigger(spawnAsset);
                // Do not block full lifecycle here; let it continue while projectile travels
                if (spawnRoutine != null) StartCoroutine(spawnRoutine);
            }
        }

        // Short circuit if already there (still honor impact)
        if ((end - start).sqrMagnitude < 1e-8f)
        {
            yield return SpawnImpactRoutine(s.impactVfxKey, start, s);
            yield break;
        }

        // Node
        var root = GameObject.Find("Effects");
        var nodeGo = new GameObject("ProjectileNode");
        nodeGo.transform.position = start;
        nodeGo.transform.SetParent(root.transform, true); // no parent, no inherited scale


        var node = new ProjectileNode(nodeGo.transform, s);

        // One trail (projectile visual)
        AttachTrail(node, string.IsNullOrEmpty(s.projectileVfxKey) ? null : s.projectileVfxKey);

        // Travel
        yield return StartCoroutine(node.TravelRoutine());

        // On arrival: shrink projectile trail out of existence within ~100 ms
        yield return node.ShrinkAndDisposeRoutine(0.8f, 0.01f);

        // Impact
        Vector3 finalPos = node.position;
        yield return SpawnImpactRoutine(string.IsNullOrEmpty(s.impactVfxKey) ? null : s.impactVfxKey, finalPos, s);

        // Cleanup (in case shrink path already disposed node safely)
        node.Cleanup();
    }

    /// <summary>
    /// Spawns a single trail and parents it to the node so it follows.
    /// </summary>
    private void AttachTrail(ProjectileNode node, string trailKey)
    {
        if (string.IsNullOrEmpty(trailKey)) return;

        var trailAsset = VisualEffectLibrary.Get(trailKey);
        node.AttachTrail(trailAsset);
    }

    /// <summary>
    /// Spawns an impact VFX via VfxManager and VfxLibrary at a world position and waits until it completes.
    /// Also waits for the impact's Apex to gate gameplay side-effects like heal.
    /// </summary>
    private IEnumerator SpawnImpactRoutine(string vfxKey, Vector3 position, ProjectileSettings s)
    {
        if (string.IsNullOrEmpty(vfxKey) || g.VisualEffectManager == null) yield break;

        var vfx = VisualEffectLibrary.Get(vfxKey);
        if (vfx == null || vfx.Prefab == null)
        {
            Debug.LogError($"ProjectileManager: VFX `{vfxKey}` not found or prefab is null.");
            yield break;
        }

        // Play and wait full lifecycle
        var (inst, routine) = g.VisualEffectManager.SpawnAndWait(vfx, position);
        if (inst != null)
        {
            // Wait apex to trigger gameplay side effects (e.g. heal)
            yield return inst.WaitUntilTrigger(vfx);
        }

        // Trigger any queued routine at apex
        if (s != null && s.routine != null)
            yield return s.routine;

        // Then wait for remainder of the effect to finish before exiting
        if (routine != null)
            yield return routine;
    }
}
