using Scripts.Helpers;
using Scripts.Factories;
using Scripts.Libraries;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using g = Scripts.Helpers.GameHelper;
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
using Scripts.Models;
using Scripts.Models.Actor;
using Scripts.Overworld;
using Scripts.Sequences;
using Scripts.Serialization;
using Scripts.Utilities;

namespace Scripts.Managers
{
/// <summary>
/// PORTRAITMANAGER - Spawns actor portraits for combat feedback.
/// 
/// PURPOSE:
/// Displays large actor portraits during combat sequences to show
/// who is attacking. Supports both 2D (UI Image) and 3D (SpriteRenderer)
/// portrait modes.
/// 
/// PORTRAIT TYPES:
/// - 2D Portraits: UI Images in Canvas (for UI overlays)
/// - 3D Portraits: World-space sprites (for in-game display)
/// 
/// VISUAL APPEARANCE:
/// ```
/// ┌─────────────────────────────────┐
/// │ [Hero A]          [Hero B]      │ ← Portraits slide in
/// │    ↘                 ↙          │
/// │       [Combat Area]             │
/// └─────────────────────────────────┘
/// ```
/// 
/// ANIMATION:
/// - SlideIn: Portraits slide from off-screen
/// - SlideOut: Portraits exit after combat
/// - SpawnPair: Two portraits for pincer attackers
/// 
/// USAGE:
/// ```csharp
/// yield return g.PortraitManager.SpawnPair3DRoutine(actorPair);
/// yield return g.PortraitManager.SlideIn2DRoutine(hero, Direction.Left);
/// ```
/// 
/// RELATED FILES:
/// - Portrait2DFactory.cs: Creates UI portraits
/// - Portrait3DFactory.cs: Creates world portraits
/// - PortraitInstance.cs: Portrait behavior component
/// - PincerAttackSequence.cs: Uses portraits during attacks
/// - ActorLibrary.cs: Provides portrait sprites
/// 
/// ACCESS: g.PortraitManager
/// </summary>
public class PortraitManager : MonoBehaviour
{
    /// <summary>All spawned portrait instances.</summary>
    private readonly List<PortraitInstance> portraits = new List<PortraitInstance>();

    #region 2D Portraits (UI Image)

    /// <summary>Slides in a 2D portrait from the specified direction.</summary>
    public void SlideIn2D(ActorInstance actor, Direction direction, float? fixedX = null, float? fixedY = null)
    {
        StartCoroutine(SlideIn2DRoutine(actor, direction, fixedX, fixedY));
    }

    /// <summary>Coroutine to slide in a 2D portrait.</summary>
    public IEnumerator SlideIn2DRoutine(ActorInstance actor, Direction direction, float? fixedX = null, float? fixedY = null)
    {
        var go = Portrait2DFactory.Create();
        go.transform.position = Vector3.zero;
        go.transform.rotation = Quaternion.identity;
        var instance = go.GetComponent<PortraitInstance>();
        instance.actor = actor;
        instance.direction = direction;
        instance.name = $"Portrait_{Guid.NewGuid():N}";
        instance.parent = g.PortraitsContainer;
        instance.sprite = ActorLibrary.Actors[actor.characterClass].Portrait;
        instance.scale = new Vector3(1f, 1f, 1f);
        if (instance.image != null) instance.image.color = new Color(1f, 1f, 1f, 1);

        // lanes
        instance.fixedX = fixedX;
        instance.fixedY = fixedY;

        portraits.Add(instance);
        yield return instance.SlideInRoutine();
    }

    /// <summary>Spawns a pair of 2D portraits for pincer attackers.</summary>
    public IEnumerator SpawnPair2DRoutine(ActorPair actorPair)
    {
        yield return Wait.For(Intermission.Before.Player.Attack);
        g.AudioManager.Play("Click");

        var (d1, d2) = GetDirections(actorPair);

        if (actorPair.axis == Axis.Vertical)
        {
            var (leftX, rightX) = ComputeVerticalLaneXs();
            bool a1Left = actorPair.actor1.location.x <= actorPair.actor2.location.x;
            float a1X = a1Left ? leftX : rightX;
            float a2X = a1Left ? rightX : leftX;

            yield return CoroutineHelper.WaitForAll(this,
                SlideIn2DRoutine(actorPair.actor1, d1, fixedX: a1X, fixedY: null),
                SlideIn2DRoutine(actorPair.actor2, d2, fixedX: a2X, fixedY: null)
            );
        }
        else
        {
            var (bottomY, topY) = ComputeHorizontalLaneYs();
            bool a1Bottom = actorPair.actor1.location.y <= actorPair.actor2.location.y;
            float a1Y = a1Bottom ? bottomY : topY;
            float a2Y = a1Bottom ? topY : bottomY;

            yield return CoroutineHelper.WaitForAll(this,
                SlideIn2DRoutine(actorPair.actor1, d1, fixedX: null, fixedY: a1Y),
                SlideIn2DRoutine(actorPair.actor2, d2, fixedX: null, fixedY: a2Y)
            );
        }

        yield return Wait.For(Intermission.Before.Portrait.SlideIn);
    }

    #endregion

    #region 3D Portraits (World Sprite)

    /// <summary>Slides in a 3D world-space portrait.</summary>
    public void SlideIn3D(ActorInstance actor, Direction direction)
    {
        StartCoroutine(SlideIn3DRoutine(actor, direction));
    }

    /// <summary>Coroutine to slide in a 3D portrait.</summary>
    public IEnumerator SlideIn3DRoutine(ActorInstance actor, Direction direction)
    {
        var go = Portrait3DFactory.Create();
        go.transform.position = Vector2.zero;
        go.transform.rotation = Quaternion.identity;
        var instance = go.GetComponent<PortraitInstance>();
        instance.actor = actor;
        instance.direction = direction;
        instance.name = $"Portrait_{Guid.NewGuid():N}";
        instance.parent = g.Board.transform;
        instance.sprite = ActorLibrary.Actors[actor.characterClass].Portrait;
        instance.transform.localScale = new Vector3(0.5f, 0.5f, 1);
        if (instance.spriteRenderer != null)
            instance.spriteRenderer.color = new Color(1, 1, 1, Opacity.Percent90);
        instance.startTime = Time.time;

        portraits.Add(instance);
        yield return instance.SlideIn();
    }

    /// <summary>Pop in out.</summary>
    public void PopInOut(ActorInstance actor, float scale = 0.1666f)
    {
        StartCoroutine(PopInOutRoutine(actor, scale));
    }

    /// <summary>Coroutine that executes the pop in out sequence.</summary>
    public IEnumerator PopInOutRoutine(ActorInstance actor, float scale = 0.1666f)
    {
        // Use factory instead of Instantiate(prefab)
        var go = Portrait3DFactory.Create();
        go.transform.position = Vector2.zero;
        go.transform.rotation = Quaternion.identity;
        var instance = go.GetComponent<PortraitInstance>();
        instance.actor = actor;
        instance.name = $"Portrait_{Guid.NewGuid():N}";
        instance.parent = g.Board.transform;
        g.SortingManager.OnPortraitPopIn(instance);
        instance.sprite = ActorLibrary.Actors[actor.characterClass].Portrait;
        instance.transform.localScale = new Vector3(scale, scale, 1);
        if (instance.spriteRenderer != null)
            instance.spriteRenderer.color = new Color(1, 1, 1, Opacity.Transparent);
        instance.startTime = Time.time;

        portraits.Add(instance);
        yield return instance.PopInOut();
    }

    /// <summary>Coroutine that executes the pop in sequence.</summary>
    public IEnumerator PopInRoutine(ActorInstance actor, float scale = 0.1666f)
    {
        var existing = portraits.FirstOrDefault(x => x != null && x.actor == actor);
        if (existing != null)
        {
            Destroy(existing.gameObject);
            portraits.Remove(existing);
        }

        // Use factory instead of Instantiate(prefab)
        var go = Portrait3DFactory.Create();
        go.transform.position = Vector2.zero;
        go.transform.rotation = Quaternion.identity;
        var instance = go.GetComponent<PortraitInstance>();
        instance.name = $"Portrait3D_{Guid.NewGuid():N}";
        instance.parent = g.Board.transform;
        g.SortingManager.OnPortraitPopIn(instance);
        instance.sprite = ActorLibrary.Actors[actor.characterClass].Portrait;
        instance.transform.localScale = new Vector3(scale, scale, 1);
        if (instance.spriteRenderer != null)
            instance.spriteRenderer.color = new Color(1, 1, 1, Opacity.Transparent);
        instance.actor = actor;
        instance.startTime = Time.time;

        portraits.Add(instance);
        yield return instance.PopIn();
    }

    /// <summary>Coroutine that executes the pop out sequence.</summary>
    public IEnumerator PopOutRoutine(ActorInstance actor)
    {
        var instance = portraits.FirstOrDefault(x => x != null && x.actor == actor);
        if (instance != null)
        {
            yield return instance.PopOut();
        }
    }

    /// <summary>Dissolve.</summary>
    public void Dissolve(ActorInstance actor, IEnumerator routine = null)
    {
        // Use factory instead of Instantiate(prefab)
        var go = Portrait3DFactory.Create();
        go.transform.position = Vector2.zero;
        go.transform.rotation = Quaternion.identity;
        var instance = go.GetComponent<PortraitInstance>();
        instance.actor = actor;
        instance.name = $"Portrait_{Guid.NewGuid():N}";
        instance.parent = g.Board.transform;
        instance.sprite = ActorLibrary.Actors[actor.characterClass].Portrait;
        instance.transform.localScale = new Vector3(0.25f, 0.25f, 1);
        if (instance.spriteRenderer != null)
            instance.spriteRenderer.color = new Color(1, 1, 1, Opacity.Percent90);
        instance.position = actor.Position;
        instance.startPosition = actor.Position;

        portraits.Add(instance);
        StartCoroutine(instance.DissolveRoutine(routine));
    }

    /// <summary>Coroutine that executes the spawn pair3 d sequence.</summary>
    public IEnumerator SpawnPair3DRoutine(ActorPair actorPair)
    {
        yield return Wait.For(Intermission.Before.Player.Attack);
        g.AudioManager.Play("Click");

        var (direction1, direction2) = GetDirections(actorPair);

        yield return CoroutineHelper.WaitForAll(this,
            SlideIn3DRoutine(actorPair.actor1, direction1),
            SlideIn3DRoutine(actorPair.actor2, direction2)
        );

        yield return Wait.For(Intermission.Before.Portrait.SlideIn);
    }

    // ============================ Utils ============================

    private (Direction, Direction) GetDirections(ActorPair pair)
    {
        var first = pair.axis == Axis.Vertical ? Direction.North : Direction.West;
        var second = pair.axis == Axis.Vertical ? Direction.South : Direction.East;
        return (pair.actor1 == pair.startActor ? first : second,
                pair.actor2 == pair.startActor ? first : second);
    }

    private (float leftX, float rightX) ComputeVerticalLaneXs()
    {
        var parentRect = g.PortraitsContainer as RectTransform;
        float width = parentRect != null ? parentRect.rect.width : 1920f;
        float lane = width * 0.25f;
        return (-lane, lane);
    }

    private (float bottomY, float topY) ComputeHorizontalLaneYs()
    {
        var parentRect = g.PortraitsContainer as RectTransform;
        float height = parentRect != null ? parentRect.rect.height : 1080f;
        float lane = height * 0.25f;
        return (-lane, lane);
    }

    /// <summary>Removes and destroys a portrait instance.</summary>
    public void Despawn(PortraitInstance portrait)
    {
        if (portrait != null && portraits.Contains(portrait))
        {
            portraits.Remove(portrait);
            Destroy(portrait.gameObject);
        }
    }

    #endregion
}

}
