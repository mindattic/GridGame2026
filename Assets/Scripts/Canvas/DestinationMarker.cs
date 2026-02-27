using UnityEngine;

namespace Assets.Scripts.Canvas
{
    /// <summary>
    /// Marks a destination point on the map.
    /// Used to visualize where the player is moving to.
    /// </summary>
    public class DestinationMarker : MonoBehaviour
    {
        [Tooltip("Distance at which the marker is considered 'arrived' and can be destroyed.")]
        public float arriveDistance = 0.1f;

        [Tooltip("If true, destroy this marker when countdown reaches zero.")]
        public bool destroyAtZero = true;

        private Transform target;
        private SpriteRenderer spriteRenderer;

        private void Awake()
        {
            spriteRenderer = GetComponent<SpriteRenderer>();
        }

        /// <summary>
        /// Sets the target transform to track arrival.
        /// </summary>
        public void SetTarget(Transform target)
        {
            this.target = target;
        }

        /// <summary>
        /// Sets the marker position.
        /// </summary>
        public void SetPosition(Vector3 position)
        {
            transform.position = position;
        }

        private void Update()
        {
            if (target == null) return;

            float distance = Vector3.Distance(transform.position, target.position);
            if (distance <= arriveDistance && destroyAtZero)
            {
                Destroy(gameObject);
            }
        }

        /// <summary>
        /// Destroys this marker.
        /// </summary>
        public void Remove()
        {
            Destroy(gameObject);
        }
    }
}
