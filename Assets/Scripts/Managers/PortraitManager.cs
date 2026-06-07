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
/// <para>PURPOSE: Displays large actor portraits during combat sequences to show who's
/// attacking. Routes between two rendering modes:
/// <list type="bullet">
/// <item>2D portraits (Portrait2DInstance) — ScreenSpaceOverlay Canvas Images; draw on top
/// of the HUD. Used for pincer slide-ins and anywhere the portrait must visually dominate.</item>
/// <item>3D portraits (Portrait3DInstance) — world-space SpriteRenderers; draw beneath
/// overlay UI. Used for in-world dissolve / pop-in effects anchored to an actor sprite.</item>
/// </list>
/// </para>
/// <para>USAGE:
/// <code>
/// yield return g.PortraitManager.SpawnPair2DRoutine(actorPair);   // on-top pincer reveal
/// yield return g.PortraitManager.PopInRoutine(actor);             // in-world pop above actor
/// </code>
/// </para>
/// <para>RELATED FILES: Portrait2DFactory.cs, Portrait3DFactory.cs, Portrait2DInstance.cs,
/// Portrait3DInstance.cs, PincerAttackSequence.cs, ActorLibrary.cs</para>
/// <para>ACCESS: g.PortraitManager</para>
/// </summary>
public class PortraitManager : MonoBehaviour
{
    private readonly List<Portrait2DInstance> portraits2D = new List<Portrait2DInstance>();
    private readonly List<Portrait3DInstance> portraits3D = new List<Portrait3DInstance>();

    #region 2D Portraits (canvas — on top of HUD)

    /// <summary>Slides in a canvas-space portrait from the specified direction.</summary>
    public void SlideIn2D(ActorInstance actor, Direction direction, float? fixedX = null, float? fixedY = null)
    {
        StartCoroutine(SlideIn2DRoutine(actor, direction, fixedX, fixedY));
    }

    /// <summary>Coroutine to slide in a canvas-space portrait.</summary>
    public IEnumerator SlideIn2DRoutine(ActorInstance actor, Direction direction, float? fixedX = null, float? fixedY = null)
    {
        var go = Portrait2DFactory.Create();
        go.transform.position = Vector3.zero;
        go.transform.rotation = Quaternion.identity;
        var instance = go.GetComponent<Portrait2DInstance>();
        instance.actor = actor;
        instance.direction = direction;
        instance.name = $"Portrait2D_{Guid.NewGuid():N}";
        instance.parent = g.PortraitsContainer;
        instance.sprite = ActorLibrary.Actors[actor.characterClass].Portrait;
        instance.scale = new Vector3(1f, 1f, 1f);
        if (instance.image != null) instance.image.color = new Color(1f, 1f, 1f, 1f);

        // Lane locking keeps paired slide-ins on consistent axes.
        instance.fixedX = fixedX;
        instance.fixedY = fixedY;

        portraits2D.Add(instance);
        yield return instance.SlideInRoutine();
    }

    /// <summary>Spawns a pair of canvas-space portraits for pincer attackers.</summary>
    public IEnumerator SpawnPair2DRoutine(ActorPair actorPair)
    {
        yield return Wait.For(Intermission.Before.Player.Attack);
        g.AudioManager.Play("Click");

        var (d1, d2) = GetDirections(actorPair);

        if (actorPair.axis == Axis.Vertical)
        {
            // Flank the attacking COLUMN: lanes sit left & right of the column's screen X so the
            // portraits read as "these two are attacking this column" without covering it. Edge
            // columns (no room on one side) shift both lanes into the open space.
            var (xA, xB) = ComputeVerticalLanes(actorPair.actor1);

            yield return CoroutineHelper.WaitForAll(this,
                SlideIn2DRoutine(actorPair.actor1, d1, fixedX: xA, fixedY: null),
                SlideIn2DRoutine(actorPair.actor2, d2, fixedX: xB, fixedY: null)
            );
        }
        else
        {
            // Flank the attacking ROW: lanes sit above & below the row's screen Y. Edge rows
            // shift both lanes into the open space.
            var (yA, yB) = ComputeHorizontalLanes(actorPair.actor1);

            yield return CoroutineHelper.WaitForAll(this,
                SlideIn2DRoutine(actorPair.actor1, d1, fixedX: null, fixedY: yA),
                SlideIn2DRoutine(actorPair.actor2, d2, fixedX: null, fixedY: yB)
            );
        }

        yield return Wait.For(Intermission.Before.Portrait.SlideIn);
    }

    #endregion

    #region 3D Portraits (world space — beneath overlay UI)

    /// <summary>Slides in a world-space portrait.</summary>
    public void SlideIn3D(ActorInstance actor, Direction direction)
    {
        StartCoroutine(SlideIn3DRoutine(actor, direction));
    }

    /// <summary>Coroutine to slide in a world-space portrait.</summary>
    public IEnumerator SlideIn3DRoutine(ActorInstance actor, Direction direction)
    {
        var go = Portrait3DFactory.Create();
        go.transform.position = Vector2.zero;
        go.transform.rotation = Quaternion.identity;
        var instance = go.GetComponent<Portrait3DInstance>();
        instance.actor = actor;
        instance.direction = direction;
        instance.name = $"Portrait3D_{Guid.NewGuid():N}";
        instance.parent = g.Board.transform;
        instance.sprite = ActorLibrary.Actors[actor.characterClass].Portrait;
        instance.transform.localScale = new Vector3(1f, 1f, 1f);
        if (instance.spriteRenderer != null)
            instance.spriteRenderer.color = new Color(1f, 1f, 1f, 1f);
        instance.startTime = Time.time;

        portraits3D.Add(instance);
        yield return instance.SlideIn();
    }

    /// <summary>Pop-in followed by hold followed by pop-out — world-space only.</summary>
    public void PopInOut(ActorInstance actor, float scale = 0.1666f)
    {
        StartCoroutine(PopInOutRoutine(actor, scale));
    }

    /// <summary>Coroutine for the pop-in + hold + pop-out flourish.</summary>
    public IEnumerator PopInOutRoutine(ActorInstance actor, float scale = 0.1666f)
    {
        var go = Portrait3DFactory.Create();
        go.transform.position = Vector2.zero;
        go.transform.rotation = Quaternion.identity;
        var instance = go.GetComponent<Portrait3DInstance>();
        instance.actor = actor;
        instance.name = $"Portrait3D_{Guid.NewGuid():N}";
        instance.parent = g.Board.transform;
        g.SortingManager.OnPortraitPopIn(instance);
        instance.sprite = ActorLibrary.Actors[actor.characterClass].Portrait;
        instance.transform.localScale = new Vector3(scale, scale, 1);
        if (instance.spriteRenderer != null)
            instance.spriteRenderer.color = new Color(1, 1, 1, Opacity.Transparent);
        instance.startTime = Time.time;

        portraits3D.Add(instance);
        yield return instance.PopInOut();
    }

    /// <summary>Coroutine for the pop-in-only flourish (PopOut is expected to finish the pair).</summary>
    public IEnumerator PopInRoutine(ActorInstance actor, float scale = 0.1666f)
    {
        var existing = portraits3D.FirstOrDefault(x => x != null && x.actor == actor);
        if (existing != null)
        {
            Destroy(existing.gameObject);
            portraits3D.Remove(existing);
        }

        var go = Portrait3DFactory.Create();
        go.transform.position = Vector2.zero;
        go.transform.rotation = Quaternion.identity;
        var instance = go.GetComponent<Portrait3DInstance>();
        instance.name = $"Portrait3D_{Guid.NewGuid():N}";
        instance.parent = g.Board.transform;
        g.SortingManager.OnPortraitPopIn(instance);
        instance.sprite = ActorLibrary.Actors[actor.characterClass].Portrait;
        instance.transform.localScale = new Vector3(scale, scale, 1);
        if (instance.spriteRenderer != null)
            instance.spriteRenderer.color = new Color(1, 1, 1, Opacity.Transparent);
        instance.actor = actor;
        instance.startTime = Time.time;

        portraits3D.Add(instance);
        yield return instance.PopIn();
    }

    /// <summary>Coroutine for the matching pop-out on the existing portrait for this actor.</summary>
    public IEnumerator PopOutRoutine(ActorInstance actor)
    {
        var instance = portraits3D.FirstOrDefault(x => x != null && x.actor == actor);
        if (instance != null)
        {
            yield return instance.PopOut();
        }
    }

    /// <summary>Spawns a world-space dissolve portrait at the actor's position (used on death).</summary>
    public void Dissolve(ActorInstance actor, IEnumerator routine = null)
    {
        var go = Portrait3DFactory.Create();
        go.transform.position = Vector2.zero;
        go.transform.rotation = Quaternion.identity;
        var instance = go.GetComponent<Portrait3DInstance>();
        instance.actor = actor;
        instance.name = $"Portrait3D_{Guid.NewGuid():N}";
        instance.parent = g.Board.transform;
        instance.sprite = ActorLibrary.Actors[actor.characterClass].Portrait;
        instance.transform.localScale = new Vector3(0.25f, 0.25f, 1);
        if (instance.spriteRenderer != null)
            instance.spriteRenderer.color = new Color(1, 1, 1, Opacity.Translucent.Alpha196);
        instance.position = actor.Position;
        instance.startPosition = actor.Position;

        portraits3D.Add(instance);
        StartCoroutine(instance.DissolveRoutine(routine));
    }

    /// <summary>Spawns a pair of world-space portraits for pincer attackers.</summary>
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

    #endregion

    #region Utils

    private (Direction, Direction) GetDirections(ActorPair pair)
    {
        var first = pair.axis == Axis.Vertical ? Direction.North : Direction.West;
        var second = pair.axis == Axis.Vertical ? Direction.South : Direction.East;
        return (pair.actor1 == pair.startActor ? first : second,
                pair.actor2 == pair.startActor ? first : second);
    }

    /// <summary>
    /// Two X-lanes flanking the attacking column (vertical pincer). Lane X is derived from the
    /// column's actual screen position so the portraits sit beside that column, not at fixed
    /// screen-center lanes. If a lane would run off the edge (leftmost/rightmost column), both
    /// lanes shift into the open space on the other side.
    /// </summary>
    private (float a, float b) ComputeVerticalLanes(ActorInstance columnActor)
    {
        var container = g.PortraitsContainer as RectTransform;
        // Guard a degenerate rect: if the container hasn't been laid out yet its rect.width is 0,
        // which collapses `offset` to 0 and stacks both pincer portraits on the exact same X
        // (100% overlap). Fall back to a sane width whenever the measured rect is ~0, not only
        // when the container is null.
        float width = container != null ? container.rect.width : 0f;
        if (width < 1f) width = 1920f;
        float halfW = width * 0.5f;
        float colX = container != null
            ? UnitConversionHelper.World.ToCanvas(container, columnActor.Position).x
            : 0f;

        float offset = halfW * 0.34f;   // ~portrait half-width + gap
        float margin = halfW * 0.10f;

        float left = colX - offset;
        float right = colX + offset;

        if (left < -halfW + margin)        // leftmost column — no room on the left
            return (colX + offset, colX + offset * 2f);
        if (right > halfW - margin)        // rightmost column — no room on the right
            return (colX - offset, colX - offset * 2f);
        return (left, right);
    }

    /// <summary>
    /// Two Y-lanes flanking the attacking row (horizontal pincer). Mirrors ComputeVerticalLanes:
    /// above/below the row's screen Y, shifting into open space for top/bottom rows.
    /// </summary>
    private (float a, float b) ComputeHorizontalLanes(ActorInstance rowActor)
    {
        var container = g.PortraitsContainer as RectTransform;
        // Same degenerate-rect guard as ComputeVerticalLanes: a 0-height container would stack
        // both pincer portraits on the same Y. Fall back when the measured rect is ~0.
        float height = container != null ? container.rect.height : 0f;
        if (height < 1f) height = 1080f;
        float halfH = height * 0.5f;
        float rowY = container != null
            ? UnitConversionHelper.World.ToCanvas(container, rowActor.Position).y
            : 0f;

        float offset = halfH * 0.22f;
        float margin = halfH * 0.10f;

        float below = rowY - offset;
        float above = rowY + offset;

        if (below < -halfH + margin)       // bottom row — no room below
            return (rowY + offset, rowY + offset * 2f);
        if (above > halfH - margin)        // top row — no room above
            return (rowY - offset, rowY - offset * 2f);
        return (below, above);
    }

    /// <summary>Removes and destroys a canvas-space portrait.</summary>
    public void Despawn(Portrait2DInstance portrait)
    {
        if (portrait != null && portraits2D.Contains(portrait))
        {
            portraits2D.Remove(portrait);
            Destroy(portrait.gameObject);
        }
    }

    /// <summary>Removes and destroys a world-space portrait.</summary>
    public void Despawn(Portrait3DInstance portrait)
    {
        if (portrait != null && portraits3D.Contains(portrait))
        {
            portraits3D.Remove(portrait);
            Destroy(portrait.gameObject);
        }
    }

    #endregion
}

}
