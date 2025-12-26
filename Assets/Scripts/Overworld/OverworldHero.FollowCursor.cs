using UnityEngine;

public partial class OverworldHero
{
    // ---------------- FollowCursor (DirectionalPress) ----------------
    private void TickFollowCursor()
    {
        // Ignore analog input, use directional override only
        Vector2 effectiveInput = (directionalActive && directionalOverride.sqrMagnitude > 1e-6f)
            ? Vector2.ClampMagnitude(directionalOverride, 1f) : Vector2.zero;

        if (effectiveInput.sqrMagnitude > 1e-6f)
        {
            if (requireVisibleToMove && !IsVisible()) { if (idleWhileOffscreen) SetIdle(); return; }

            Vector2 current = GetPosition();
            float inputMag = Mathf.Clamp01(effectiveInput.magnitude);
            Vector2 dir = inputMag > 1e-6f ? effectiveInput / inputMag : Vector2.zero;
            float baseStepLen = moveSpeed * Time.deltaTime * inputMag;
            float mult = GetSpeedMultiplier(current + dir * (baseStepLen * speedSampleAheadFactor));
            Vector2 step = dir * (baseStepLen * mult);

            // Move using collider cast-and-slide
            MoveWithCast(step);

            // Drive animator from desired input direction and speed
            SetAnimationFromInput(dir, step.magnitude);
        }
        else
        {
            SetIdle();
        }

        // Always update Y-sort so the hero's order follows their Y
        var sr = spriteRenderer != null ? spriteRenderer : GetComponent<SpriteRenderer>();
        if (sr != null)
            PartySortHelper.ApplyActorYSort(sr, PartySortHelper.GlobalScale);

        // While in directional mode we never follow click paths
        isMoving = false; _path = null;
    }

    private void TickFollowLeader()
    {
        if (leader == null)
        {
            SetIdle();
            return;
        }

        Vector2 current = GetPosition();
        Vector3 lp3 = leader.position;
        Vector2 leaderPos = new Vector2(lp3.x, lp3.y);
        Vector2 toLeader = leaderPos - current;
        float dist = toLeader.magnitude;

        // Teleport if extremely far to keep party coherent
        if (teleportIfBeyond > 0f && dist > teleportIfBeyond)
        {
            Vector2 snap = leaderPos;
            if (followDistance > 1e-4f && dist > 1e-6f)
                snap = leaderPos - toLeader / dist * followDistance;
            SetPosition(ClampToMap(snap));
            ApplyAnimatorParameters(lastLook, 0f);
            OnHeroMoved?.Invoke(GetPosition());
            return;
        }

        // If outside the comfort ring, move toward leader; else idle
        float outer = Mathf.Max(0f, followDistance + arriveBuffer);
        if (dist > outer)
        {
            Vector2 dir = toLeader / Mathf.Max(dist, 1e-6f);

            // Distance-based catchup (speeds up when far, clamped by catchupMultiplier)
            float catchup = 1f;
            if (followDistance > 1e-4f)
            {
                float t = Mathf.InverseLerp(followDistance, followDistance * 4f, dist);
                catchup = Mathf.Lerp(1f, Mathf.Max(1f, catchupMultiplier), t);
            }

            float stepLen = followSpeed * catchup * Time.deltaTime;
            Vector2 step = dir * stepLen;

            // Attempt not to overshoot near the target ring edge
            float overshoot = dist - outer;
            if (step.magnitude > overshoot)
                step = dir * overshoot;

            MoveWithCast(step);
            SetAnimationFromInput(dir, step.magnitude);
        }
        else
        {
            SetIdle();
        }
    }

    // Hold-to-move directional clicks (screen param kept for compatibility)
    public void BeginDirectionalFromScreen(Vector2 screenPos, RectTransform _)
    {
        var cam = worldCamera != null ? worldCamera : Camera.main;
        Vector3 wp = Mode7CameraController.ScreenToWorldOnZPlane(cam, screenPos, transform.position.z);
        SetDirectionalOverride(new Vector2(wp.x, wp.y));
    }

    public void UpdateDirectionalFromScreen(Vector2 screenPos, RectTransform _)
    {
        if (!directionalActive) return;
        var cam = worldCamera != null ? worldCamera : Camera.main;
        Vector3 wp = Mode7CameraController.ScreenToWorldOnZPlane(cam, screenPos, transform.position.z);
        SetDirectionalOverride(new Vector2(wp.x, wp.y));
    }

    public void FullStop()
    {
        directionalActive = false;
        directionalOverride = Vector2.zero;
        SetIdle();
    }

    private void SetDirectionalOverride(Vector2 world)
    {
        Vector2 delta = ClampToMap(world) - GetPosition();
        float dist = delta.magnitude;
        if (dist < 1e-6f)
        {
            directionalActive = false;
            return;
        }

        // Constant magnitude so FollowCursor speed is static (no distance ramp)
        float mag = Mathf.Clamp01(directionalClickMagnitude);
        Vector2 dir = (delta / dist) * mag;

        directionalOverride = dir;
        directionalActive = true;

        // Immediate visual feedback
        SetAnimation(dir);
        isMoving = false; // use analog-like path instead
    }
}
