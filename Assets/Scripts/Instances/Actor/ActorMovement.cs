// --- File: Assets/Scripts/Instances/Actor/ActorMovement.cs ---
using Assets.Helper;
using Assets.Scripts.Behaviors.Actor;
using Assets.Scripts.Models;
using System.Collections;
using System.Linq;
using UnityEngine;
using g = Assets.Helpers.GameHelper;
using s = Assets.Helpers.SettingsHelper;

namespace Assets.Scripts.Instances.Actor
{
    /// <summary>
    /// ACTORMOVEMENT - Movement and drag handling for actors.
    /// 
    /// PURPOSE:
    /// Handles all movement-related operations for ActorInstance including
    /// cursor following, tile-to-tile movement, and movement tilt effects.
    /// 
    /// MOVEMENT TYPES:
    /// ```
    /// Cursor Follow - Actor follows touch/mouse while dragging
    /// Tile Movement - Animated move from tile A to tile B
    /// Swap Movement - Exchange positions with another actor
    /// ```
    /// 
    /// TOUCH OFFSET:
    /// Maintains the grab point offset so actors don't snap to
    /// cursor center when picked up.
    /// 
    /// TILT EFFECT:
    /// Actors tilt in the direction of movement for visual feedback.
    /// 
    /// WATCHDOG:
    /// Includes safety watchdog to prevent infinite movement loops
    /// that could stall the sequence queue.
    /// 
    /// RELATED FILES:
    /// - ActorInstance.cs: Owns the Movement component
    /// - InputManager.cs: Triggers cursor-follow mode
    /// - TileInstance.cs: Movement destinations
    /// - ActorAnimation.cs: Visual animation effects
    /// </summary>
    public class ActorMovement
    {
        #region References

        protected ActorFlags flags => instance.Flags;
        protected ActorRenderers render => instance.Render;
        protected ActorStats stats => instance.Stats;
        private bool isActive => instance.IsActive;
        private bool isAlive => instance.IsAlive;
        private Quaternion rotation { get => instance.Rotation; set => instance.Rotation = value; }
        protected Vector2Int previousLocation { get => instance.previousLocation; set => instance.previousLocation = value; }
        private Vector2Int location { get => instance.location; set => instance.location = value; }
        protected Vector3 previousPosition { get => instance.previousPosition; set => instance.previousPosition = value; }
        private Vector3 position { get => instance.Position; set => instance.Position = value; }
        private Vector3 scale { get => instance.Scale; set => instance.Scale = value; }

        protected bool isSelectedHero => g.Actors.HasMovingHero && g.Actors.MovingHero == instance;

        private ActorInstance instance;

        #endregion

        #region Lifecycle

        public void Start()
        {
            // Movement is driven by explicit calls
        }

        public void Initialize(ActorInstance parentInstance)
        {
            this.instance = parentInstance;
        }

        #endregion

        #region Cursor-Follow Movement

        /// <summary>
        /// Moves the actor toward the cursor while selected.
        /// Uses g.TouchOffset to maintain grab point without snapping.
        /// </summary>
        public IEnumerator MoveTowardCursorRoutine()
        {
            flags.IsMoving = true;

            while (flags.IsMoving)
            {
                previousPosition = instance.Position;

                // Apply TouchOffset. If not set, it should be Vector3.zero.
                Vector3 target = (g.TouchPosition3D + g.TouchOffset).ClampToBoard();
                instance.Position = target;

                ApplyTilt(instance.Position - previousPosition);
                CheckLocationChanged();

                // New: Update dynamic support lines for the currently moving hero
                if (isSelectedHero)
                {
                    g.SupportLineManager.UpdateForSelectedHeroLocation(instance);
                }

                yield return Wait.None();
            }

            // Leaving movement: clear any dynamic lines related to this hero
            if (isSelectedHero)
            {
                g.SupportLineManager.ClearFor(instance);
            }

            flags.IsMoving = false;
            instance.transform.localRotation = Quaternion.Euler(Vector3.zero);
        }

        // --------------------------------------------------------------------
        // Grid destination movement with watchdog
        // --------------------------------------------------------------------

        /// <summary>
        /// Moves the actor toward its grid destination using right-angle Move.
        /// Includes a watchdog to prevent infinite loops if misconfigured.
        /// </summary>
        public IEnumerator TowardDestinationRoutine()
        {
            flags.IsMoving = true;
            g.AudioManager.Play("Slide");

            Vector3 destination = Geometry.GetPositionByLocation(location);

            const float MaxSeconds = 5.0f;
            const int MaxIterations = 2000;
            float elapsed = 0f;
            int iterations = 0;

            // Horizontal leg
            if (Mathf.Abs(this.position.x - destination.x) > g.SnapThreshold)
            {
                Vector3 horizontalTarget = new Vector3(destination.x, this.position.y, this.position.z);

                while (Mathf.Abs(this.position.x - destination.x) > g.SnapThreshold)
                {
                    ApplyTilt(instance.Position - previousPosition);

                    previousPosition = instance.Position;
                    this.position = Vector3.MoveTowards(this.position, horizontalTarget, g.MoveFocus).ClampToBoard();

                    CheckLocationChanged();

                    // New: Update support lines while sliding to destination as well
                    if (isSelectedHero)
                    {
                        g.SupportLineManager.UpdateForSelectedHeroLocation(instance);
                    }

                    elapsed += Time.deltaTime;
                    iterations++;

                    if (elapsed > MaxSeconds || iterations > MaxIterations)
                    {
                        Debug.LogWarning($"[ActorMovement] MoveTowardDestinationRoutine X watchdog fired. Forcing snap. Actor={instance?.name}");
                        break;
                    }

                    yield return Wait.None();
                }

                previousPosition = instance.Position;
                position = new Vector3(destination.x, position.y, position.z).ClampToBoard();
            }

            // Reset watchdog for vertical
            elapsed = 0f;
            iterations = 0;

            // Vertical leg
            if (Mathf.Abs(this.position.y - destination.y) > g.SnapThreshold)
            {
                Vector3 verticalTarget = new Vector3(position.x, destination.y, position.z);

                while (Mathf.Abs(position.y - destination.y) > g.SnapThreshold)
                {
                    ApplyTilt(instance.Position - previousPosition);

                    previousPosition = instance.Position;
                    position = Vector3.MoveTowards(position, verticalTarget, g.MoveFocus).ClampToBoard();

                    CheckLocationChanged();

                    // New: Update support lines while sliding to destination as well
                    if (isSelectedHero)
                    {
                        g.SupportLineManager.UpdateForSelectedHeroLocation(instance);
                    }

                    elapsed += Time.deltaTime;
                    iterations++;

                    if (elapsed > MaxSeconds || iterations > MaxIterations)
                    {
                        Debug.LogWarning($"[ActorMovement] MoveTowardDestinationRoutine Y watchdog fired. Forcing snap. Actor={instance?.name}");
                        break;
                    }

                    yield return Wait.None();
                }

                previousPosition = instance.Position;
                position = new Vector3(position.x, destination.y, position.z).ClampToBoard();
            }

            // Done moving: clear any dynamic support lines
            if (isSelectedHero)
            {
                g.SupportLineManager.ClearFor(instance);
            }

            flags.IsMoving = false;
            flags.IsSwapping = false;
            scale = g.TileScale;
            rotation = Geometry.Rotation(0, 0, 0);
        }

        /// <summary>
        /// Force the selected hero to the nearest tile. Utility for UI.
        /// </summary>
        public void ToLocation()
        {
            flags.IsMoving = false;

            var closestTile = g.TileMap.GetTile(instance.location);
            instance.location = closestTile.location;
            instance.Position = closestTile.position;
        }

        // --------------------------------------------------------------------
        // Grid change and overlap handling
        // --------------------------------------------------------------------

        /// <summary>
        /// Checks if the actor crossed into a new tile.
        /// Updates logical location and handles overlap rules.
        /// </summary>
        private void CheckLocationChanged()
        {
            if (!flags.IsMoving)
                return;

            if (flags.IsSwapping)
                return;

            var closestTile = Geometry.GetClosestTile(this.position);

            if (location == closestTile.location)
                return;

            previousLocation = location;
            location = closestTile.location;

            if (isSelectedHero)
                g.TileManager.Hightlight(previousLocation, location);

            ActorInstance overlappingActor = g.Actors.All.FirstOrDefault(x =>
                x != instance &&
                x.IsPlaying &&
                x.location == location);

            if (overlappingActor == null)
            {
                g.SortingManager.OnActorMoving(this.instance);
            }
            else
            {
                g.SortingManager.OnActorOverlap(this.instance, overlappingActor);
                overlappingActor.Move.HandleOverlap(previousLocation);
            }
        }

        /// <summary>
        /// Public entry to start MoveTowardCursorRoutine.
        /// </summary>
        public void MoveTowardCursor()
        {
            instance.StartCoroutine(MoveTowardCursorRoutine());
        }

        /// <summary>
        /// Handles swap movement after detecting an overlap.
        /// </summary>
        public void HandleOverlap(Vector2Int targetLocation)
        {
            if (flags.IsSwapping)
                return;

            var currentTile = g.TileMap.GetTile(targetLocation);

            if (currentTile.IsOccupied)
            {
                Debug.Log($"Tile {currentTile.location.x},{currentTile.location.y} is occupied.");
            }
            else
            {
                flags.IsSwapping = true;
                location = currentTile.location;
                instance.StartCoroutine(TowardDestinationRoutine());
            }
        }

        /// <summary>
        /// Applies a tilt effect based on movement velocity.
        /// </summary>
        public void ApplyTilt(Vector3 velocity)
        {
            if (!s.ApplyMovementTilt)
                return;

            Vector3 tiltFactor = new Vector3(5f, 0f, 5f);
            Vector3 maxTilt = new Vector3(20f, 0, 20f);

            float rotateSpeed = 5f;
            float resetSpeed = 5f;

            if (velocity.sqrMagnitude > 0.0001f)
            {
                Vector3 v = velocity.normalized;
                float tiltZ = Mathf.Clamp(v.x * tiltFactor.z, -maxTilt.z, maxTilt.z);
                float tiltX = Mathf.Clamp(-v.y * tiltFactor.x, -maxTilt.x, maxTilt.x);

                Vector3 targetEuler = new Vector3(tiltX, 0, tiltZ);

                instance.transform.localRotation = Quaternion.Slerp(
                    instance.transform.localRotation,
                    Quaternion.Euler(targetEuler),
                    Time.deltaTime * rotateSpeed
                );
            }
            else
            {
                instance.transform.localRotation = Quaternion.Slerp(
                    instance.transform.localRotation,
                    Quaternion.Euler(Vector3.zero),
                    Time.deltaTime * resetSpeed
                );
            }
        }

        #endregion
    }
}
