using System.Collections;
using UnityEngine;
using Assets.Scripts.Models;
using g = Assets.Helpers.GameHelper;

/// <summary>
/// Convenience helpers that construct ProjectileSettings and start the coroutine.
/// Uses the unified ProjectileSettings fields.
/// </summary>
public static class ProjectileHelper
{
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
