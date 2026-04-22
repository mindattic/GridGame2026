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
using Scripts.Models.Actor;
using Scripts.Overworld;
using Scripts.Sequences;
using Scripts.Serialization;
using Scripts.Utilities;

namespace Scripts.Models
{
    /// <summary>
    /// TARGETPOINT - Coordinate-space agnostic endpoint for TargetLineInstance arcs.
    /// <para>PURPOSE: Lets a targeting arc connect any combination of world and canvas positions
    /// (world↔world, world↔canvas, canvas↔canvas). Canvas overlay endpoints are unprojected
    /// into world space at arc-render time via the main camera.</para>
    /// <para>USAGE:
    /// <code>
    /// TargetLineManager.Show("enemy-select",
    ///     TargetPoint.Canvas(icon.Rect),
    ///     TargetPoint.Actor(enemy),
    ///     Color.red);
    /// </code>
    /// </para>
    /// <para>RELATED FILES: TargetLineInstance.cs, TargetLineManager.cs</para>
    /// </summary>
    public readonly struct TargetPoint
    {
        private enum Kind { World, CanvasOverlay, Actor }

        private readonly Vector3 worldPos;
        private readonly RectTransform canvasRect;
        private readonly ActorInstance actor;
        private readonly Kind kind;

        private TargetPoint(Vector3 w, RectTransform r, ActorInstance a, Kind k)
        {
            worldPos = w; canvasRect = r; actor = a; kind = k;
        }

        /// <summary>Static world-space point (does not follow a moving object).</summary>
        public static TargetPoint World(Vector3 worldPos)
            => new TargetPoint(worldPos, null, null, Kind.World);

        /// <summary>A canvas UI element (ScreenSpaceOverlay) unprojected at render time.</summary>
        public static TargetPoint Canvas(RectTransform rt)
            => new TargetPoint(Vector3.zero, rt, null, Kind.CanvasOverlay);

        /// <summary>An actor on the board — follows the actor's world position each frame.</summary>
        public static TargetPoint Actor(ActorInstance a)
            => new TargetPoint(Vector3.zero, null, a, Kind.Actor);

        /// <summary>Depth (camera-Z distance) at which canvas endpoints are projected into world space.
        /// Chosen to land on the board plane for a natural connect-to-sprite look.</summary>
        public const float CanvasProjectionDepth = 10f;

        /// <summary>Resolves this point into a world-space Vector3 for the current frame.</summary>
        public Vector3 GetWorldPosition(Camera cam)
        {
            switch (kind)
            {
                case Kind.World:
                    return worldPos;
                case Kind.Actor:
                    return actor != null ? actor.Position : Vector3.zero;
                case Kind.CanvasOverlay:
                {
                    if (canvasRect == null) return Vector3.zero;
                    if (cam == null) cam = Camera.main;
                    if (cam == null) return Vector3.zero;
                    // Overlay canvas: RectTransform.position is in screen pixels.
                    var screen = canvasRect.position;
                    return cam.ScreenToWorldPoint(new Vector3(screen.x, screen.y, CanvasProjectionDepth));
                }
                default:
                    return worldPos;
            }
        }

        /// <summary>True when this endpoint still has a live underlying reference.</summary>
        public bool IsValid
        {
            get
            {
                switch (kind)
                {
                    case Kind.World: return true;
                    case Kind.CanvasOverlay: return canvasRect != null;
                    case Kind.Actor: return actor != null;
                    default: return false;
                }
            }
        }
    }
}
