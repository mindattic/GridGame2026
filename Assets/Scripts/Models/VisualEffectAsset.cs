using System;
using UnityEngine;
using g = Scripts.Helpers.GameHelper;
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
using Scripts.Instances.SynergyLine;
using Scripts.Inventory;
using Scripts.Libraries;
using Scripts.Managers;
using Scripts.Models.Actor;
using Scripts.Overworld;
using Scripts.Sequences;
using Scripts.Serialization;
using Scripts.Utilities;

namespace Scripts.Models
{
/// <summary>
/// VISUALEFFECTASSET - VFX definition data.
/// 
/// PURPOSE:
/// Defines a visual effect including prefab reference,
/// positioning, timing, and behavior.
/// 
/// PROPERTIES:
/// - Name: Unique identifier
/// - Prefab: GameObject to instantiate
/// - RelativeOffset: Position offset from spawn point
/// - AngularRotation: Initial rotation
/// - RelativeScale: Scale multiplier
/// - Apex: Time when "impact" occurs (for damage sync)
/// - Duration: Total lifetime
/// - IsLooping: Whether effect loops until despawned
/// 
/// RELATED FILES:
/// - VisualEffectLibrary.cs: Asset registry
/// - VisualEffectManager.cs: Spawning
/// - VisualEffectInstance.cs: Runtime behavior
/// </summary>
[Serializable]
public class VisualEffectAsset
{
    public VisualEffectAsset() { }

    public VisualEffectAsset(VisualEffectAsset other)
    {
        Name = other.Name;
        Prefab = other.Prefab;
        RelativeOffset = other.RelativeOffset;
        AngularRotation = other.AngularRotation;
        RelativeScale = other.RelativeScale;
        Apex = other.Apex;
        Duration = other.Duration;
        IsLooping = other.IsLooping;
    }

    /// <summary>Unique identifier for this VFX.</summary>
    public string Name;

    /// <summary>Prefab to instantiate.</summary>
    public GameObject Prefab;

    /// <summary>Position offset from spawn point.</summary>
    public Vector3 RelativeOffset;

    /// <summary>Initial rotation.</summary>
    public Vector3 AngularRotation;

    /// <summary>Scale multiplier.</summary>
    public Vector3 RelativeScale;

    /// <summary>Whether effect loops until manually despawned.</summary>
    public bool IsLooping;

    /// <summary>Time when "impact" occurs for damage sync.</summary>
    public float Apex = 1f;

    /// <summary>Total lifetime in seconds.</summary>
    public float Duration;
}

}
