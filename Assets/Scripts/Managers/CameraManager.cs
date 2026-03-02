using Scripts.Models;
using System.Collections;
using System.Collections.Generic;
using Unity.Mathematics;
using Unity.VisualScripting;
using UnityEngine;
using static UnityEngine.RuleTile.TilingRuleOutput;
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
using Scripts.Models.Actor;
using Scripts.Overworld;
using Scripts.Sequences;
using Scripts.Serialization;
using Scripts.Utilities;

namespace Scripts.Managers
{
    /// <summary>Camera world-space corner coordinates.</summary>
    public class CameraWorldSpace
    {
        public Vector3 TopLeft;
        public Vector3 TopRight;
        public Vector3 BottomRight;
        public Vector3 BottomLeft;

        public CameraWorldSpace()
        {
            TopRight = Camera.main.ScreenToWorldPoint(new Vector3(Screen.width, Screen.height, 0));
        }
    }

    /// <summary>Camera screen-space corner coordinates.</summary>
    public class CameraLocalSpace
    {
        public Vector3 TopLeft;
        public Vector3 TopRight;
        public Vector3 BottomRight;
        public Vector3 BottomLeft;

        public CameraLocalSpace()
        {
            TopRight = new Vector3(Screen.width, Screen.height, Camera.main.nearClipPlane);
        }
    }

    /// <summary>
    /// CAMERAMANAGER - Camera state and effects.
    /// 
    /// PURPOSE:
    /// Manages camera positioning, bounds, and effects like
    /// screen shake and smooth following.
    /// 
    /// PROPERTIES:
    /// - viewBounds: Visible area in world space
    /// - screenBounds: Screen pixel bounds
    /// - world: World-space corner coordinates
    /// - local: Screen-space corner coordinates
    /// 
    /// EFFECTS:
    /// - Shake(intensity): Camera shake effect
    /// - Follow(target): Smooth camera follow
    /// 
    /// RELATED FILES:
    /// - BoardInstance.cs: Positions board in view
    /// - InputManager.cs: Pan controls
    /// </summary>
    public class CameraManager : MonoBehaviour
    {
        public RectFloat viewBounds;
        public Scripts.Models.RectInt screenBounds;
        public CameraWorldSpace world;
        public CameraLocalSpace local;

        private void Awake()
        {
            world = new CameraWorldSpace();
            local = new CameraLocalSpace();
        }

        public Vector2 ScreenToViewport(Vector2 point, int pixelWidth, int pixelHeight)
        {
            float x = point.x / Camera.main.pixelWidth;
            float y = point.y / Camera.main.pixelHeight;
            return new float2(x, y);
        }


        private void OnDrawGizmos()
        {
            var p = Camera.main.ViewportToWorldPoint(new Vector3(1, 1, Camera.main.nearClipPlane));
            Gizmos.color = Color.yellow;
            Gizmos.DrawSphere(p, 0.1F);
        }
    }
}
