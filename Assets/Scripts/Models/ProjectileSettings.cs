using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// PROJECTILESETTINGS - Configuration for projectile spawning.
/// 
/// PURPOSE:
/// Defines all parameters needed to spawn and animate a projectile,
/// from start position to target with visual effects.
/// 
/// CATEGORIES:
/// - Identification: friendlyName
/// - Positions: startPosition, target, followTarget
/// - Visuals: spawnVfxKey, projectileVfxKey, impactVfxKey
/// - Motion: motionStyle, travelSeconds, speed limits
/// - Style-specific: wiggle, lob height, spiral params
/// 
/// MOTION STYLES:
/// - Straight: Direct line to target
/// - Wiggle: Oscillating path
/// - Lobbed: Arc trajectory
/// - Spiral: Circular motion toward target
/// 
/// RELATED FILES:
/// - ProjectileManager.cs: Spawns projectiles
/// - ProjectileInstance.cs: Projectile behavior
/// - FireProjectileSequence.cs: Sequence wrapper
/// </summary>
public class ProjectileSettings
{
    // Identification
    public string friendlyName;

    // Start and destination
    public Vector3 startPosition;
    public ActorInstance target;
    public Transform followTarget;
    public Vector3 staticTargetPosition;

    // Visuals
    public string spawnVfxKey;               
    public string projectileVfxKey;              
    public string impactVfxKey;               

    // Travel behavior
    public MotionStyle motionStyle = MotionStyle.Straight;

    // Travel pacing and arrival
    public float travelSeconds = 0.8f;
    public float minTilesPerSec = 1.5f;
    public float maxTilesPerSec = 4.0f;
    public float arriveRadiusTiles = 0.1f;

    // Facing
    public bool faceDirection = true;

    // Style specific
    public float wiggleAmplitudeTiles = 0.35f;
    public float wiggleHz = 3.5f;

    public float lobbedHeightTiles = 1.0f;

    public int spiralTurns = 2;
    public float spiralStartRadiusTiles = 0.6f;

    // Optional ricochet settings
    public Bounds? worldBoundsForRicochet = null;
    public float ricochetBiasTowardTarget = 4.0f;

    // Callback after impact VFX
    public IEnumerator routine;
}
