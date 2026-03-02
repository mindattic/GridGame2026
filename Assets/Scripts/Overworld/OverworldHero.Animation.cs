using UnityEngine;
using System;
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
    // Animator driving 8-way blend tree
    public Animator animator;

    // 8-way facing memory for idle pose. Defaults to down.
    private Vector2 lastLook = Vector2.down;

    // 4-way facing (legacy name for saves), while animator uses 8-way via lastLook
    private MoveDirection lastDirection = MoveDirection.Idle;

    // Direction stabilization for animator (prevents flicker between pure down and diagonals)
    private const float axisSnapEpsilon = 0.12f;   // if minor axis below this, snap to 0
    private const float axisDominance = 1.35f;     // major must be >= minor * dominance

    // Back-compat property remains accessible
    public string CurrentFacingName => lastDirection.ToString();

    // ---------------- Animation helpers (8-way blend tree) ----------------

    private void SetAnimation(Vector2 delta)
    {
        float speed = delta.magnitude;           // units moved this frame
        Vector2 dir = speed > 1e-6f ? delta.normalized : lastLook;
        dir = StabilizeDirectionForBlend(dir);

        lastLook = dir;                          // remember facing for idle
        lastDirection = DetermineDirection4Way(dir); // maintain legacy 4-way for save text

        ApplyAnimatorParameters(dir, speed);
    }

    private void SetAnimationFromInput(Vector2 dir, float speed)
    {
        if (dir.sqrMagnitude < 1e-6f)
        {
            SetIdle();
            return;
        }
        dir = dir.normalized;
        dir = StabilizeDirectionForBlend(dir);
        lastLook = dir;
        lastDirection = DetermineDirection4Way(dir);
        ApplyAnimatorParameters(dir, speed);
    }

    private void SetIdle()
    {
        lastDirection = MoveDirection.Idle;
        ApplyAnimatorParameters(lastLook, 0f);
    }

    private void ApplyAnimatorParameters(Vector2 dir, float speed)
    {
        animator.SetFloat("MoveX", dir.x);
        animator.SetFloat("MoveY", dir.y);
        // Convert per-frame distance into units/second so transitions with Speed>0.1f work reliably
        float speedPerSecond = speed;
        if (Application.isPlaying && Time.deltaTime > 0f)
            speedPerSecond = speed / Time.deltaTime;
        animator.SetFloat("Speed", speedPerSecond);
    }

    private Vector2 StabilizeDirectionForBlend(Vector2 dir)
    {
        if (dir.sqrMagnitude < 1e-6f) return dir;
        float ax = Mathf.Abs(dir.x);
        float ay = Mathf.Abs(dir.y);
        Vector2 d = dir;

        // Snap to pure vertical when vertical dominates and horizontal is tiny
        if (ay >= ax * axisDominance && ax < axisSnapEpsilon)
        {
            d.x = 0f;
            d = d.normalized;
        }
        // Snap to pure horizontal when horizontal dominates and vertical is tiny
        else if (ax >= ay * axisDominance && ay < axisSnapEpsilon)
        {
            d.y = 0f;
            d = d.normalized;
        }
        return d;
    }

    public void SetFacing(string facingName)
    {
        if (string.IsNullOrEmpty(facingName)) return;

        MoveDirection dir;
        if (!System.Enum.TryParse(facingName, true, out dir))
            dir = MoveDirection.Idle;

        lastDirection = dir;

        Vector2 look = lastLook;
        switch (dir)
        {
            case MoveDirection.Up: look = Vector2.up; break;
            case MoveDirection.Right: look = Vector2.right; break;
            case MoveDirection.Down: look = Vector2.down; break;
            case MoveDirection.Left: look = Vector2.left; break;
            case MoveDirection.Idle: default: break;
        }

        lastLook = look;
        ApplyAnimatorParameters(lastLook, 0f);
    }

    private static MoveDirection DetermineDirection4Way(Vector2 delta)
    {
        if (delta.sqrMagnitude < 1e-6f) return MoveDirection.Idle;

        if (Mathf.Abs(delta.x) >= Mathf.Abs(delta.y))
            return delta.x >= 0 ? MoveDirection.Right : MoveDirection.Left;
        else
            return delta.y >= 0 ? MoveDirection.Up : MoveDirection.Down;
    }
}

}
