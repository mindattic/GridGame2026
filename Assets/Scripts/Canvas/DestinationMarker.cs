using UnityEngine;
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
using Scripts.Overworld;
using Scripts.Sequences;
using Scripts.Serialization;
using Scripts.Utilities;

namespace Scripts.Canvas
{
    /// <summary>
    /// DESTINATIONMARKER - Visual indicator showing movement destination.
    /// 
    /// PURPOSE:
    /// Displays a marker sprite at the location where a hero will move to.
    /// Auto-destroys when the target arrives at the destination.
    /// 
    /// VISUAL APPEARANCE:
    /// ```
    /// [Hero] · · · · [X]
    ///                 ↑
    ///         destination marker
    /// ```
    /// 
    /// ARRIVAL DETECTION:
    /// - Tracks distance between marker and target transform
    /// - When distance <= arriveDistance, destroys self
    /// - Can be disabled via destroyAtZero = false
    /// 
    /// USAGE:
    /// ```csharp
    /// var marker = DestinationMarkerFactory.Create();
    /// marker.SetPosition(targetPosition);
    /// marker.SetTarget(heroTransform);
    /// // Marker auto-destroys when hero arrives
    /// ```
    /// 
    /// RELATED FILES:
    /// - DestinationMarkerFactory.cs: Creates marker GameObjects
    /// - InputManager.cs: May create markers during drag
    /// - SpriteLibrary.cs: Provides marker sprite
    /// </summary>
    public class DestinationMarker : MonoBehaviour
    {
        #region Settings

        [Tooltip("Distance at which the marker is considered 'arrived'.")]
        public float arriveDistance = 0.1f;

        [Tooltip("If true, destroy this marker when target arrives.")]
        public bool destroyAtZero = true;

        #endregion

        #region State

        private Transform target;
        private SpriteRenderer spriteRenderer;

        #endregion

        #region Initialization

        /// <summary>Caches the SpriteRenderer component.</summary>
        private void Awake()
        {
            spriteRenderer = GetComponent<SpriteRenderer>();
        }

        #endregion

        #region Public Methods

        /// <summary>Sets the target transform to track for arrival.</summary>
        public void SetTarget(Transform target)
        {
            this.target = target;
        }

        /// <summary>Sets the marker world position.</summary>
        public void SetPosition(Vector3 position)
        {
            transform.position = position;
        }

        /// <summary>Immediately destroys this marker.</summary>
        public void Remove()
        {
            Destroy(gameObject);
        }

        #endregion

        #region Update Loop

        /// <summary>Checks distance to target each frame and self-destructs on arrival.</summary>
        private void Update()
        {
            if (target == null) return;

            float distance = Vector3.Distance(transform.position, target.position);
            if (distance <= arriveDistance && destroyAtZero)
            {
                Destroy(gameObject);
            }
        }

        #endregion
    }
}
