using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Configuration for a single projectile launch. This is used end to end,
/// without converting to another request type.
/// </summary>
public class ProjectileSettings
{
    // Identification
    public string friendlyName;

    // Start and destination
    public Vector3 startPosition;
    public ActorInstance target;          // Preferred destination source
    public Transform followTarget;        // Fallback if target is null
    public Vector3 staticTargetPosition;  // Last fallback if both are null

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
