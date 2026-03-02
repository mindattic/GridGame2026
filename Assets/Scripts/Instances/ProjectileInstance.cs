using Scripts.Helpers;
using Scripts.Models;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using g = Scripts.Helpers.GameHelper;
using Scripts.Canvas;
using Scripts.Data.Actor;
using Scripts.Data.Items;
using Scripts.Data.Skills;
using Scripts.Effects;
using Scripts.Factories;
using Scripts.Hub;
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

namespace Scripts.Instances
{
/// <summary>
/// PROJECTILEINSTANCE - Runtime projectile behavior.
/// 
/// PURPOSE:
/// Controls a projectile's movement, rotation, and lifecycle
/// as it travels from start to target.
/// 
/// LIFECYCLE:
/// 1. Spawned by ProjectileManager
/// 2. Trail VFX attached
/// 3. Moves toward target using MotionStyle
/// 4. Impact VFX spawned on arrival
/// 5. Self-destructs
/// 
/// MOTION STYLES:
/// Motion controlled by ProjectileSettings.motionStyle.
/// 
/// RELATED FILES:
/// - ProjectileManager.cs: Spawns projectiles
/// - ProjectileSettings.cs: Configuration
/// </summary>
public class ProjectileInstance : MonoBehaviour
{
    #region Properties

    public Transform parent
    {
        get => gameObject.transform.parent;
        set => gameObject.transform.SetParent(value, true);
    }

    public Vector3 position
    {
        get => gameObject.transform.position;
        set => gameObject.transform.position = value;
    }

    public Quaternion rotation
    {
        get => gameObject.transform.rotation;
        set => gameObject.transform.rotation = value;
    }

    public Vector3 scale
    {
        get => gameObject.transform.localScale;
        set => gameObject.transform.localScale = value;
    }

    #endregion

    #region Fields

    private ProjectileSettings projectile = new ProjectileSettings();
    private Vector3 startPosition;
    private Vector3 endPosition;
    private GameObject trailInstance;

    #endregion
}

}
