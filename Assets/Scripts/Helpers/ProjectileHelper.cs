using System.Collections;
using UnityEngine;
using Scripts.Models;
using g = Scripts.Helpers.GameHelper;
using Scripts.Canvas;
using Scripts.Data.Actor;
using Scripts.Data.Items;
using Scripts.Data.Skills;
using Scripts.Effects;
using Scripts.Factories;
using Scripts.Hub;
using Scripts.Instances;
using Scripts.Instances.Actor;
using Scripts.Instances.Board;
using Scripts.Instances.SynergyLine;
using Scripts.Inventory;
using Scripts.Libraries;
using Scripts.Managers;
using Scripts.Models.Actor;
using Scripts.Overworld;
using Scripts.Sequences;
using Scripts.Serialization;
using Scripts.Utilities;

namespace Scripts.Helpers
{
/// <summary>
/// PROJECTILEHELPER - Convenience methods for projectile spawning.
/// 
/// PURPOSE:
/// Provides simple fire-and-forget methods for common projectile
/// patterns without manually constructing ProjectileSettings.
/// 
/// METHODS:
/// - FireStraight: Direct line to target
/// - FireWiggle: Oscillating path
/// - FireLobbed: Arc trajectory
/// - FireSpiral: Circular motion
/// 
/// USAGE:
/// ```csharp
/// ProjectileHelper.FireStraight(startPos, target.transform, "Fireball", "Explosion");
/// ```
/// 
/// RELATED FILES:
/// - ProjectileManager.cs: Actual spawning
/// - ProjectileSettings.cs: Full configuration
/// - ProjectileInstance.cs: Projectile behavior
/// </summary>
public static class ProjectileHelper
{
    /// <summary>Fire straight.</summary>
    public static void FireStraight(Vector3 start, Transform target, string trailEffectKey, string impactVfxKey, float travelSeconds = 0.7f, IEnumerator onImpact = null)
    {
        g.ProjectileManager.Spawn(new ProjectileSettings
        {
            startPosition = start,
            followTarget = target,
            projectileVfxKey = trailEffectKey,
            impactVfxKey = impactVfxKey,
            motionStyle = MotionStyle.Straight,
            travelSeconds = travelSeconds,
            routine = onImpact
        });
    }

    /// <summary>Fire wiggle.</summary>
    public static void FireWiggle(Vector3 start, Transform target, string trailEffectKey, string impactVfxKey, float wiggleAmplitudeTiles = 0.35f, float wiggleHz = 3.5f, float travelSeconds = 0.8f, IEnumerator onImpact = null)
    {
        g.ProjectileManager.Spawn(new ProjectileSettings
        {
            startPosition = start,
            followTarget = target,
            projectileVfxKey = trailEffectKey,
            impactVfxKey = impactVfxKey,
            motionStyle = MotionStyle.Wiggle,
            wiggleAmplitudeTiles = wiggleAmplitudeTiles,
            wiggleHz = wiggleHz,
            travelSeconds = travelSeconds,
            routine = onImpact
        });
    }

    /// <summary>Fire lobbed.</summary>
    public static void FireLobbed(Vector3 start, Transform target, string trailEffectKey, string impactVfxKey, float heightTiles = 1.0f, float travelSeconds = 0.9f, IEnumerator onImpact = null)
    {
        g.ProjectileManager.Spawn(new ProjectileSettings
        {
            startPosition = start,
            followTarget = target,
            projectileVfxKey = trailEffectKey,
            impactVfxKey = impactVfxKey,
            motionStyle = MotionStyle.LobbedArc,
            lobbedHeightTiles = Mathf.Max(0f, heightTiles),
            travelSeconds = travelSeconds,
            routine = onImpact
        });
    }

    /// <summary>Fire homing spiral.</summary>
    public static void FireHomingSpiral(Vector3 start, Transform target, string trailEffectKey, string impactVfxKey, int turns = 2, float startRadiusTiles = 0.6f, float travelSeconds = 1.0f, IEnumerator onImpact = null)
    {
        g.ProjectileManager.Spawn(new ProjectileSettings
        {
            startPosition = start,
            followTarget = target,
            projectileVfxKey = trailEffectKey,
            impactVfxKey = impactVfxKey,
            motionStyle = MotionStyle.HomingSpiral,
            spiralTurns = Mathf.Max(1, turns),
            spiralStartRadiusTiles = Mathf.Max(0.05f, startRadiusTiles),
            travelSeconds = travelSeconds,
            routine = onImpact
        });
    }


}

}
