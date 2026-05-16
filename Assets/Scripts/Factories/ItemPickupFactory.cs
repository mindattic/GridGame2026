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
/// ITEMPICKUPFACTORY - Creates on-map crafting-material pickup GameObjects.
///
/// PURPOSE:
/// Builds a pickup visual (sprite + sorting + ItemPickupInstance) for the
/// loot-burst effect on enemy death. Mirrors CoinFactory, but the sprite is
/// tinted by rarity at Spawn-time and there is no ParticleSystem.
///
/// CREATED HIERARCHY:
/// ```
/// ItemPickup (root)
/// +-- SpriteRenderer (Coin sprite, color tinted by rarity)
/// +-- SortingGroup   (layer "Coin", order 999)
/// +-- ItemPickupInstance (bounce -> seek -> despawn)
/// ```
///
/// CALLED BY:
/// - ItemPickupManager.Spawn / SpawnBurst
///
/// RELATED FILES:
/// - ItemPickupInstance.cs: Animation behavior
/// - ItemPickupManager.cs: Spawns pickups on enemy death
/// - CoinFactory.cs: Sister factory (same visual building)
/// </summary>
public static class ItemPickupFactory
{
    /// <summary>Creates a new pickup GameObject with full configuration.</summary>
    public static GameObject Create(Transform parent = null)
    {
        var root = new GameObject("ItemPickup");
        root.layer = LayerMask.NameToLayer("DottedLine");
        root.tag = "Powerup";

        var t = root.transform;
        t.localPosition = Vector3.zero;
        t.localRotation = Quaternion.identity;
        t.localScale = Vector3.one;

        // SpriteRenderer — tint is set at Spawn time by ItemPickupInstance
        var sr = root.AddComponent<SpriteRenderer>();
        sr.sprite = SpriteLibrary.Sprites["Coin"];
        sr.color = Color.white;
        sr.shadowCastingMode = ShadowCastingMode.Off;
        sr.receiveShadows = false;
        sr.sortingLayerName = "Coin";
        sr.sortingOrder = 999;
        sr.drawMode = SpriteDrawMode.Simple;

        // Pickup instance with animation curves
        var pickup = root.AddComponent<ItemPickupInstance>();
        pickup.linearCurve = CreateLinearCurve();
        pickup.slopeCurve  = CreateSlopeCurve();
        pickup.sineCurve   = CreateSineCurve();

        // SortingGroup
        var sg = root.AddComponent<SortingGroup>();
        sg.sortingLayerName = "Coin";
        sg.sortingOrder = 0;

        if (parent != null) t.SetParent(parent, false);
        return root;
    }

    private static AnimationCurve CreateLinearCurve() =>
        new AnimationCurve(new Keyframe(0f, 0f, 1f, 1f), new Keyframe(1f, 1f, 1f, 1f));

    private static AnimationCurve CreateSlopeCurve() =>
        new AnimationCurve(new Keyframe(0f, 0f, 0f, 0f), new Keyframe(1f, 1f, 2f, 2f));

    private static AnimationCurve CreateSineCurve() =>
        new AnimationCurve(new Keyframe(0f, 0f, 0f, 0f), new Keyframe(1f, 1f, 0f, 0f));
}
}
