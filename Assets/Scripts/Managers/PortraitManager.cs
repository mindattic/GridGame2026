using Assets.Helper;
using Assets.Scripts.Libraries;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using g = Assets.Helpers.GameHelper;

/// <summary>
/// Unified manager for spawning and controlling portraits in UI (Image) and World (SpriteRenderer).
/// Uses PortraitInstance for behavior.
/// </summary>
public class PortraitManager : MonoBehaviour
{
    private GameObject portrait2DPrefab;
    private GameObject portrait3DPrefab;

    // Track all spawned portraits
    private readonly List<PortraitInstance> portraits = new List<PortraitInstance>();

    private void Awake()
    {
        // Prefabs must exist in PrefabLibrary
        portrait2DPrefab = PrefabLibrary.Prefabs["Portrait2DPrefab"]; // UI
        portrait3DPrefab = PrefabLibrary.Prefabs["Portrait3DPrefab"]; // World
    }

    // ========================= UI (Image) =========================

    public void SlideIn2D(ActorInstance actor, Direction direction, float? fixedX = null, float? fixedY = null)
    {
        StartCoroutine(SlideIn2DRoutine(actor, direction, fixedX, fixedY));
    }

    public IEnumerator SlideIn2DRoutine(ActorInstance actor, Direction direction, float? fixedX = null, float? fixedY = null)
    {
        var go = Instantiate(portrait2DPrefab, Vector3.zero, Quaternion.identity);
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

    // ======================= World (Sprite) =======================

    public void SlideIn3D(ActorInstance actor, Direction direction)
    {
        StartCoroutine(SlideIn3DRoutine(actor, direction));
    }

    public IEnumerator SlideIn3DRoutine(ActorInstance actor, Direction direction)
    {
        var go = Instantiate(portrait3DPrefab, Vector2.zero, Quaternion.identity);
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

    public void PopInOut(ActorInstance actor, float scale = 0.1666f)
    {
        StartCoroutine(PopInOutRoutine(actor, scale));
    }

    public IEnumerator PopInOutRoutine(ActorInstance actor, float scale = 0.1666f)
    {
        var go = Instantiate(portrait3DPrefab, Vector2.zero, Quaternion.identity);
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

    public IEnumerator PopInRoutine(ActorInstance actor, float scale = 0.1666f)
    {
        var existing = portraits.FirstOrDefault(x => x != null && x.actor == actor);
        if (existing != null)
        {
            Destroy(existing.gameObject);
            portraits.Remove(existing);
        }

        var go = Instantiate(portrait3DPrefab, Vector2.zero, Quaternion.identity);
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

    public IEnumerator PopOutRoutine(ActorInstance actor)
    {
        var instance = portraits.FirstOrDefault(x => x != null && x.actor == actor);
        if (instance != null)
        {
            yield return instance.PopOut();
        }
    }

    public void Dissolve(ActorInstance actor, IEnumerator routine = null)
    {
        var go = Instantiate(portrait3DPrefab, Vector2.zero, Quaternion.identity);
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

    public void Despawn(PortraitInstance portrait)
    {
        if (portrait != null && portraits.Contains(portrait))
        {
            portraits.Remove(portrait);
            Destroy(portrait.gameObject);
        }
    }
}
