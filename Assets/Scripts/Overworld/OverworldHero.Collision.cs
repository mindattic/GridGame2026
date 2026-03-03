using UnityEngine;
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
using Scripts.Models;
using Scripts.Models.Actor;
using Scripts.Sequences;
using Scripts.Serialization;
using Scripts.Utilities;

namespace Scripts.Overworld
{
public partial class OverworldHero
{
    // Predict a final stop position using a simple cast-based approach; fixed step for determinism
    /// <summary>Predict stop.</summary>
    private Vector2 PredictStop(Vector2 start, Vector2 target)
    {
        Vector2 cur = start;
        const int maxIters = 128;
        float stepLen = Mathf.Max(1f, moveSpeed / 60f); // ~60Hz equivalent

        for (int i = 0; i < maxIters; i++)
        {
            Vector2 toTarget = target - cur;
            float dist = toTarget.magnitude;
            if (dist <= snapThreshold) return target;

            Vector2 dir = dist > 1e-6f ? (toTarget / dist) : Vector2.zero;
            float len = Mathf.Min(stepLen, dist);
            Vector2 next = cur;
            PlanCastMove(ref next, dir, len);
            if ((next - cur).sqrMagnitude <= 1e-6f)
                return cur; // blocked before target
            cur = next;
        }
        return cur;
    }

    /// <summary>Returns whether the is party member condition is met.</summary>
    private static bool IsPartyMember(Collider2D c)
    {
        if (c == null) return false;
        return c.GetComponentInParent<OverworldHero>() != null;
    }

    // Performs a cast and updates nextPos with the planned position (with slide)
    /// <summary>Plan cast move.</summary>
    private void PlanCastMove(ref Vector2 nextPos, Vector2 dir, float distance)
    {
        if (distance <= 1e-6f || dir.sqrMagnitude <= 1e-12f) return;
        dir = dir.normalized;

        // Ensure casts originate from current rb position
        Vector2 origin = rb.position;
        int hitCount = rb.Cast(dir, contactFilter, hitBuffer, distance + skin);
        if (hitCount == 0)
        {
            origin += dir * distance;
            nextPos = origin;
            return;
        }

        float closest = Mathf.Infinity;
        int closestIndex = -1;
        bool anyOverlap = false;
        Vector2 overlapNormalSum = Vector2.zero;
        for (int h = 0; h < hitCount; h++)
        {
            var hit = hitBuffer[h];
            // Ignore all party members (any OverworldHero)
            if (IsPartyMember(hit.collider))
                continue;

            float d = hit.distance;
            if (d >= 0f)
            {
                if (d < closest) { closest = d; closestIndex = h; }
            }
            else
            {
                anyOverlap = true;
                overlapNormalSum += hit.normal;
            }
        }

        if (closestIndex < 0)
        {
            // If there were no blocking hits after filtering party, move freely or depenetrate
            if (!anyOverlap)
            {
                origin += dir * distance;
                nextPos = origin;
                nextPos = ClampToMap(nextPos);
                return;
            }

            // Fully overlapping at origin: nudge outward to depenetrate
            Vector2 push;
            if (overlapNormalSum.sqrMagnitude > 1e-6f)
                push = overlapNormalSum.normalized * Mathf.Max(skin, 0.02f);
            else
                push = dir * Mathf.Max(skin, 0.02f); // no reliable normal; escape along intent
            origin += push;
            nextPos = origin;
            nextPos = ClampToMap(nextPos);
            return;
        }

        float allowed = Mathf.Max(0f, closest - skin);
        origin += dir * allowed;

        // Slide remaining along surface with a few iterations to avoid sticking
        float remain = Mathf.Max(0f, distance - allowed);
        if (remain <= 1e-6f) { nextPos = origin; nextPos = ClampToMap(nextPos); return; }

        Vector2 moveDir = dir;
        int iterations = Mathf.Max(1, maxSlideIterations);
        for (int i = 0; i < iterations && remain > 1e-6f; i++)
        {
            // Use the normal from the initial blocking hit for first slide
            Vector2 n = hitBuffer[closestIndex].normal;
            Vector2 remainingVec = moveDir * remain;
            Vector2 slide = remainingVec - Vector2.Dot(remainingVec, n) * n; // tangent component
            if (slide.sqrMagnitude <= 1e-8f) break;

            Vector2 sDir = slide.normalized;
            float sLen = slide.magnitude;

            int hitsSlide = rb.Cast(sDir, contactFilter, hitBuffer, sLen + skin);
            if (hitsSlide == 0)
            {
                origin += sDir * sLen;
                remain = 0f;
                break;
            }
            else
            {
                float closestSlide = Mathf.Infinity;
                int slideHitIndex = -1;
                for (int h = 0; h < hitsSlide; h++)
                {
                    var hit = hitBuffer[h];
                    // Ignore party members during slide too
                    if (IsPartyMember(hit.collider))
                        continue;

                    float d = hit.distance;
                    if (d >= 0f && d < closestSlide) { closestSlide = d; slideHitIndex = h; }
                }

                if (slideHitIndex < 0)
                {
                    origin += sDir * sLen;
                    remain = 0f;
                    break;
                }

                float allowSlide = Mathf.Max(0f, closestSlide - skin);
                if (allowSlide > 1e-6f)
                {
                    origin += sDir * allowSlide;
                    remain = Mathf.Max(0f, remain - allowSlide);
                    // update moveDir to keep sliding along last direction
                    moveDir = sDir;
                    closestIndex = slideHitIndex;
                }
                else
                {
                    // tiny progress or corner pinch; apply micro-nudge along tangent and stop
                    origin += sDir * Mathf.Max(0.001f, skin * 0.5f);
                    break;
                }
            }
        }

        nextPos = origin;

        // Clamp inside map bounds after planning
        nextPos = ClampToMap(nextPos);
    }

    // Chooses cast-and-slide; applies position and event
    /// <summary>Move with cast.</summary>
    private void MoveWithCast(Vector2 displacement)
    {
        if (displacement.sqrMagnitude <= 1e-10f) return;

        // If collision is disabled, move freely (still clamp to map)
        if (!enableCollision)
        {
            Vector2 freeNext = ClampToMap(GetPosition() + displacement);
            SetPosition(freeNext);
            OnHeroMoved?.Invoke(freeNext);
            return;
        }

        // Subdivide long moves to prevent tunneling through thin walls
        float remaining = displacement.magnitude;
        if (remaining <= 1e-6f) return;
        Vector2 dirNorm = displacement / remaining;

        Vector2 startPos = GetPosition();
        Vector2 curPos = startPos;

        int safetyIters = 0;
        const int maxIters = 16; // protect against infinite loops
        while (remaining > 1e-6f && safetyIters++ < maxIters)
        {
            float stepLen = Mathf.Min(remaining, Mathf.Max(0.05f, maxCastStepDistance));
            Vector2 nextPos = curPos;
            PlanCastMove(ref nextPos, dirNorm, stepLen);

            float moved = (nextPos - curPos).magnitude;
            if (moved <= 1e-6f)
            {
                // Blocked � stop here
                break;
            }

            // Advance to the step end so subsequent casts originate from the new position
            curPos = nextPos;
            SetPosition(curPos);

            // Reduce remaining by the distance actually moved to respect blocking/sliding
            remaining = Mathf.Max(0f, remaining - moved);
        }

        if ((curPos - startPos).sqrMagnitude > 1e-8f)
        {
            // Already set transform during stepping; just raise the event once
            OnHeroMoved?.Invoke(curPos);
        }
    }
}

}
