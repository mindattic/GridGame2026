using Scripts.Models;
using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using g = Scripts.Helpers.GameHelper;
using TMPro;
using Scripts.Libraries;
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
using Scripts.Managers;
using Scripts.Models.Actor;
using Scripts.Overworld;
using Scripts.Sequences;
using Scripts.Serialization;
using Scripts.Utilities;

namespace Scripts.Canvas
{
    /// <summary>
    /// TIMELINEICONMODE - State machine for timeline icon behavior.
    ///
    /// States:
    /// - Queued: At spawn point (left edge), waiting to be released (speed-based delay)
    /// - Approaching: Moving right toward trigger point ("loading")
    /// - PushedBack: Being pushed left toward spawn after taking damage (animated)
    /// - Stunned: Stopped after pushback, recovering (agility-based duration)
    /// </summary>
    public enum TimelineIconMode
    {
        /// <summary>Icon is at spawn point (left edge) waiting to be released.</summary>
        Queued,
        /// <summary>Icon is moving right toward the trigger point.</summary>
        Approaching,
        /// <summary>Icon is being pushed left (animated with deceleration).</summary>
        PushedBack,
        /// <summary>Icon has stopped after pushback, recovering.</summary>
        Stunned
    }

    /// <summary>
    /// TIMELINEICON - Individual actor icon on the timeline bar.
    /// 
    /// PURPOSE:
    /// Represents one actor on the timeline UI. Icons move from left to right
    /// ("loading") at a speed determined by the actor's Speed stat. When an icon
    /// reaches the trigger point (right edge), that actor's turn begins.
    ///
    /// MOVEMENT MODEL (Normalized Coordinates):
    /// - u = 0.0: Left edge (spawn point, fresh / not loaded)
    /// - u = 1.0: Right edge (trigger point, fully loaded — ready to fire)
    /// - uPerSec: Speed in u-units per second (based on actor Speed stat)
    ///
    /// STATE MACHINE (TimelineIconMode):
    /// 1. Queued → Waiting at spawn point (queueDelay countdown)
    /// 2. Approaching → Moving right at uPerSec speed
    /// 3. PushedBack → Animated pushback (toward spawn) after taking damage
    /// 4. Stunned → Recovery period after pushback (agility-based)
    ///
    /// PUSHBACK SYSTEM:
    /// When enemy is hit by pincer attack:
    /// - Icon pushed left based on position (closer to trigger = more pushback)
    /// - Pushback animates with deceleration (PushDeceleration)
    /// - After pushback, enters Stunned state
    /// - Stun duration based on actor's Agility stat
    ///
    /// KEY PROPERTIES:
    /// - Owner: ActorInstance this icon represents
    /// - Mode: Current TimelineIconMode state
    /// - u: Normalized position (0=spawn/left, 1=trigger/right)
    /// 
    /// VISUAL ELEMENTS:
    /// - Tag: Background image (team colored)
    /// - Icon: Actor portrait/icon
    /// - Label: Optional text (usually hidden)
    /// - CanvasGroup: For fade-out animations
    /// 
    /// RELATED FILES:
    /// - TimelineBarInstance.cs: Parent container managing all tags
    /// - TimelineIconFactory.cs: Creates tag GameObjects
    /// - TurnManager.cs: Receives trigger callbacks
    /// - TimelineTriggerSequence.cs: Handles turn trigger
    /// 
    /// CREATED BY: TimelineIconFactory.Create()
    /// </summary>
    [DisallowMultipleComponent]
    [RequireComponent(typeof(RectTransform))]
    public sealed class TimelineIcon : MonoBehaviour, IPointerClickHandler
    {
        #region Visual Elements

        // Resolved in Awake via transform.Find or GetComponentInChildren.
        // TimelineIconFactory creates Tag / Icon / Label children before attaching
        // the TimelineIcon component, so Awake's lookup always succeeds.
        private Image Tag;
        private Image Icon;
        private TextMeshProUGUI Label;
        private CanvasGroup CanvasGroup;

        #endregion

        #region Runtime State

        [Header("Runtime")]
        /// <summary>The enemy actor this tag represents.</summary>
        public ActorInstance Owner;

        /// <summary>RectTransform for positioning.</summary>
        public RectTransform Rect { get; private set; }

        /// <summary>Current state machine mode.</summary>
        public TimelineIconMode Mode { get; private set; } = TimelineIconMode.Queued;

        // Normalized motion state (resolution-independent)
        private float leftX;      // bar-local x at u=0 (left edge, spawn)
        private float rightX;     // bar-local x at u=1 (right edge, trigger)
        private float u;          // normalized position [0..1], 0=spawn (left), 1=trigger (right)
        private float uPerSec;    // normalized speed per second (toward 1)
        private float queueDelay; // seconds to wait before moving
        private float queueTimer; // countdown for queue release

        // Pushback animation state
        private float pushTargetU;   // target position for pushback
        private float pushVelocity;  // current velocity during pushback
        private const float PushDeceleration = 2.5f;
        private const float PushMinVelocity = 0.01f;

        // Stun state
        private float stunDuration; // agility-based stun time
        private float stunTimer;    // countdown for recovery

        #endregion

        private System.Action<TimelineIcon> onReached;
        private bool isFading;
        private bool paused;
        private bool fired;

        // Label fade-in state (hidden in queue, fades in when approaching)
        private const float LabelFadeDuration = 0.4f;
        private Coroutine labelFadeCoroutine;
        private float labelAlpha;

        // Tolerance for deciding a tag reached the left edge (in local pixels)
        private const float ReachTolerance =0.25f;

        /// <summary>Initializes component references and state.</summary>
        private void Awake()
        {
            Rect = GetComponent<RectTransform>();
            if (CanvasGroup == null)
                CanvasGroup = GetComponent<CanvasGroup>();

            // Prefer exact-name children, then fall back to any in-tree
            if (Tag == null)
            {
                // Prefer a child named "Tag"; fall back to legacy name or first Image found
                var tagTransform = transform.Find("Tag") ?? transform.Find("Image");
                Tag = tagTransform != null ? tagTransform.GetComponent<Image>() : GetComponentInChildren<Image>(true);
                if (Tag == null)
                    Debug.LogWarning("TimelineIcon: Child Tag Image not found. Add a Tag child or assign `Tag`.", this);
            }
            if (Icon == null)
            {
                var iconTransform = transform.Find("Icon");
                Icon = iconTransform != null ? iconTransform.GetComponent<Image>() : null;
                if (Icon == null)
                {
                    // Try to find by name among all Images as a last resort (for nested or renamed children)
                    var images = GetComponentsInChildren<Image>(true);
                    foreach (var img in images)
                    {
                        if (img == null) continue;
                        if (img.name == "Icon") { Icon = img; break; }
                        var n = img.name.ToLowerInvariant();
                        if (n.Contains("icon") || n.Contains("_icon") || n.Contains("-icon")) { Icon = img; break; }
                    }
                }
                if (Icon == null)
                {
                    Debug.LogWarning("TimelineIcon: Child Icon Image not found. Add an Icon child or assign `Icon`.", this);
                }
                else
                {
                    Debug.Log($"TimelineIcon: Icon Image found on GameObject '{Icon.gameObject.name}' (path: {GetTransformPath(Icon.transform)})", this);
                }
            }
            if (Label == null)
            {
                var LabelTransform = transform.Find("Label");
                Label = LabelTransform != null ? LabelTransform.GetComponent<TextMeshProUGUI>() : GetComponentInChildren<TextMeshProUGUI>(true);
                if (Label == null)
                    Debug.LogWarning("TimelineIcon: Child Label (TextMeshProUGUI) not found. Add a Label child or assign `Label`.", this);
            }

            // Enable clicks on the tag so taps select the associated actor
            if (Tag != null) Tag.raycastTarget = true;
            if (Icon != null) Icon.raycastTarget = false;
            if (Label != null) Label.raycastTarget = false;

            // Right-edge pivot so anchoredPosition.x represents the icon's RIGHT
            // (leading) edge — i.e., the edge that hits the trigger first as the
            // icon moves left→right. Reach detection compares this directly to rightX.
            if (Rect != null)
            {
                Rect.anchorMin = new Vector2(0f,0.5f);
                Rect.anchorMax = new Vector2(0f,0.5f);
                Rect.pivot = new Vector2(1f,0.5f);
            }
            // Ignore layout so manual positioning is preserved
            var le = gameObject.GetComponent<LayoutElement>();
            if (le == null) le = gameObject.AddComponent<LayoutElement>();
            le.ignoreLayout = true;
        }

        /// <summary>Wire.</summary>
        public void Wire(Image tagImage, CanvasGroup group)
        {
            if (tagImage != null) Tag = tagImage;
            if (group != null) CanvasGroup = group;
            if (Label == null)
            {
                var labelTransform = transform.Find("Label");
                Label = labelTransform != null ? labelTransform.GetComponent<TextMeshProUGUI>() : GetComponentInChildren<TextMeshProUGUI>(true);
            }
            if (Tag == null)
            {
                var tagTransform = transform.Find("Tag");
                Tag = tagTransform != null ? tagTransform.GetComponent<Image>() : GetComponentInChildren<Image>(true);
            }
            if (Icon == null)
            {
                var iconTransform = transform.Find("Icon");
                Icon = iconTransform != null ? iconTransform.GetComponent<Image>() : null;
            }
            if (Tag != null) Tag.raycastTarget = true;
            if (Icon != null) Icon.raycastTarget = false;
            if (Label != null) Label.raycastTarget = false;
        }

        // Handle pointer clicks on the tag to select the owner actor and show their card
        /// <summary>Handles pointer click on this UI element.</summary>
        public void OnPointerClick(PointerEventData eventData)
        {
            if (Owner == null) 
                return;
            g.SelectionManager.Select(Owner);
        }

        // Initialize using normalized coordinates and speed
        /// <summary>Initializes initialize normalized.</summary>
        public void InitializeNormalized(ActorInstance owner, float leftX, float rightX, float startU, float uPerSec, System.Action<TimelineIcon> onReached, float queueDelay = 0f)
        {
            Owner = owner;
            this.leftX = leftX;
            this.rightX = Mathf.Max(rightX, leftX + 1f);
            this.u = Mathf.Clamp01(startU);
            this.uPerSec = Mathf.Max(0.0001f, uPerSec);
            this.onReached = onReached;
            this.queueDelay = Mathf.Max(0f, queueDelay);
            this.queueTimer = this.queueDelay;
            
            // Start in Queued mode if there's a delay, otherwise go straight to Approaching
            Mode = queueDelay > 0f ? TimelineIconMode.Queued : TimelineIconMode.Approaching;
            
            // Label is hidden in queue, visible when approaching
            labelAlpha = Mode == TimelineIconMode.Queued ? 0f : 1f;
            ApplyLabelAlpha();
            
            if (CanvasGroup != null) CanvasGroup.alpha =1f;
            isFading = false;
            paused = true; // start paused; TimelineBar controls advance
            fired = false;
            
            // Reset pushback/stun state
            pushVelocity = 0f;
            pushTargetU = u;
            stunTimer = 0f;
            stunDuration = 0f;

            // Assign the owner's portrait sprite to the Icon image if available
            if (Icon != null && Owner != null)
            {
                var data = ActorLibrary.Get(Owner.characterClass);
                Sprite sprite = null;
                if (data != null)
                {
                    // First try to get an icon that matches the actor's tags
                    try
                    {
                        sprite = SpriteLibrary.GetActorTagIcon(data.Tags);
                    }
                    catch { sprite = null; }

                    // Fallback to portrait if no tag icon found
                    if (sprite == null) sprite = data.Portrait;
                }

                // Final fallback: transparent 32x32 from sprite library
                if (sprite == null)
                {
                    var fallback = SpriteLibrary.Sprites != null && SpriteLibrary.Sprites.TryGetValue("Transparent32x32", out var s) ? s : null;
                    sprite = fallback;
                }

                Icon.sprite = sprite;
                Icon.enabled = sprite != null;
                Icon.preserveAspect = true;
                Debug.Log($"TimelineIcon: Assigned icon '{(sprite != null ? sprite.name : "<null>")}' for actor '{Owner?.characterClass}' tags='{(data != null ? data.Tags.ToString() : "<no data>")}' on GameObject '{Icon.gameObject.name}' (path: {GetTransformPath(Icon.transform)})", this);
            }

            ApplyPosition();
            UpdateLabel();
        }

        // Backward-compatible initializer
        /// <summary>Initializes initialize.</summary>
        public void Initialize(ActorInstance owner, float leftEdgeX, float startX, float moveSpeedPxPerSec, System.Action<TimelineIcon> onReached)
        {
            float width = Mathf.Max(1f, rightX - leftEdgeX);
            float startU = Mathf.InverseLerp(leftEdgeX, rightX, startX);
            float uSpeed = Mathf.Abs(moveSpeedPxPerSec) / width;
            InitializeNormalized(owner, leftEdgeX, rightX, startU, uSpeed, onReached);
        }

        /// <summary>Updates the endpoints.</summary>
        public void UpdateEndpoints(float newLeftX, float newRightX)
        {
            // Preserve normalized u while endpoints shift
            leftX = newLeftX;
            rightX = Mathf.Max(newRightX, newLeftX + 1f);
            ApplyPosition();
            UpdateLabel();
        }

        /// <summary>Pause.</summary>
        public void Pause() => paused = true;
        /// <summary>Resume.</summary>
        public void Resume() => paused = false;
        /// <summary>Sets the alpha.</summary>
        public void SetAlpha(float a) { if (CanvasGroup != null) CanvasGroup.alpha = Mathf.Clamp01(a); }

        /// <summary>
        /// Resets the tag to the spawn point (far left) and enters Queued mode.
        /// Called when an enemy's turn finishes. Assigns queue delay based on speed.
        /// </summary>
        public void ResetToSpawn()
        {
            fired = false;

            // IMPORTANT: Immediately snap u to 0.0 to prevent re-triggering while at right edge
            // This fixes the double-trigger bug where the tag could fire again before animation completes
            u = 0f;
            ApplyPosition();
            
            // Assign queue delay based on speed: faster enemies wait less (1.5-4 seconds)
            int speed = Owner != null ? Owner.Stats.Speed.ToInt() : 10;
            queueDelay = Mathf.Clamp(4f - (speed / 20f) * 2.5f, 1.5f, 4f);
            queueTimer = queueDelay;
            
            // Enter Queued mode (or Approaching if no delay)
            Mode = queueDelay > 0f ? TimelineIconMode.Queued : TimelineIconMode.Approaching;
            
            // Hide label in queue, show if immediately approaching
            labelAlpha = Mode == TimelineIconMode.Queued ? 0f : 1f;
            ApplyLabelAlpha();
            
            // Reset pushback/stun state
            pushVelocity = 0f;
            stunTimer = 0f;
            stunDuration = 0f;
            
            UpdateLabel();
        }

        /// <summary>
        /// Legacy reset - immediately snaps to spawn (left) and waits in queue.
        /// </summary>
        public void ResetForNextCycle()
        {
            fired = false;
            u = 0f;
            ApplyPosition();
            // Assign queue delay based on speed (1.5-4 seconds)
            int speed = Owner != null ? Owner.Stats.Speed.ToInt() : 10;
            queueDelay = Mathf.Clamp(4f - (speed / 20f) * 2.5f, 1.5f, 4f);
            queueTimer = queueDelay;
            Mode = queueDelay > 0f ? TimelineIconMode.Queued : TimelineIconMode.Approaching;
            
            // Hide label in queue, show if immediately approaching
            labelAlpha = Mode == TimelineIconMode.Queued ? 0f : 1f;
            ApplyLabelAlpha();
            
            pushVelocity = 0f;
            stunTimer = 0f;
            UpdateLabel();
        }

        // Set anchored x from normalized u (right-edge pivot: anchoredPosition.x = leading edge)
        /// <summary>Applies the position.</summary>
        private void ApplyPosition()
        {
            if (Rect == null) return;
            float xPos = Mathf.Lerp(leftX, rightX, Mathf.Clamp01(u));
            // Prevent ever going past the right (trigger); lock exactly at rightX when u>=1
            if (xPos > rightX) xPos = rightX;
            var p = Rect.anchoredPosition;
            Rect.anchoredPosition = new Vector2(xPos, p.y);
        }

        /// <summary>Sets the x.</summary>
        public void SetX(float xPos)
        {
            if (Rect != null)
            {
                // Clamp to never cross past the right (trigger)
                float clamped = Mathf.Min(rightX, xPos);
                var p = Rect.anchoredPosition;
                Rect.anchoredPosition = new Vector2(clamped, p.y);
                u = (rightX - leftX) > 0.0001f ? Mathf.InverseLerp(leftX, rightX, clamped) : u;
            }
            else
            {
                var lp = transform.localPosition;
                float clamped = Mathf.Min(rightX, xPos);
                transform.localPosition = new Vector3(clamped, lp.y, lp.z);
            }
            UpdateLabel();
        }

        /// <summary>Sets the u.</summary>
        public void SetU(float value)
        {
            u = Mathf.Clamp01(value);
            ApplyPosition();
            UpdateLabel();
        }

        /// <summary>Gets the u.</summary>
        public float GetU() => u;
        /// <summary>Gets the u per sec.</summary>
        public float GetUPerSec() => uPerSec;

        /// <summary>
        /// Returns the u this tag is heading toward — pushTargetU when in PushedBack mode,
        /// the current u otherwise. Used by the bar's spatial overlap resolver.
        /// </summary>
        public float GetEffectiveTargetU()
        {
            return Mode == TimelineIconMode.PushedBack ? pushTargetU : u;
        }

        /// <summary>
        /// Snaps the tag to a new u, or — if in PushedBack mode — retargets the pushback so the
        /// tag finishes at <paramref name="targetU"/>. Used by the bar's overlap resolver to keep
        /// pushed-back / spawned tags from stacking on the same slot.
        /// </summary>
        public void SetTargetU(float targetU)
        {
            targetU = Mathf.Clamp01(targetU);
            if (Mode == TimelineIconMode.PushedBack)
            {
                pushTargetU = targetU;
                pushVelocity = Mathf.Max(pushVelocity, Mathf.Abs(targetU - u) * 3f);
            }
            else
            {
                SetU(targetU);
            }
        }
        /// <summary>Gets the queue timer.</summary>
        public float GetQueueTimer() => queueTimer;
        
        /// <summary>
        /// Sets the queue timer to a new value. Used by TimelineBar to coordinate release spacing.
        /// </summary>
        public void SetQueueTimer(float time)
        {
            queueTimer = Mathf.Max(0f, time);
            if (Mode == TimelineIconMode.Queued && queueTimer <= 0f)
            {
                Mode = TimelineIconMode.Approaching;
                StartLabelFadeIn();
            }
            UpdateLabel();
        }
        
        /// <summary>Gets the seconds remaining.</summary>
        public float GetSecondsRemaining()
        {
            // Time to travel from current u up to u=1 (trigger).
            float moveTime = uPerSec <= 0f ? 0f : Mathf.Max(0f, (1f - u) / uPerSec);
            
            // Add wait time based on current mode
            float waitTime = 0f;
            switch (Mode)
            {
                case TimelineIconMode.Queued:
                    waitTime = Mathf.Max(0f, queueTimer);
                    break;
                case TimelineIconMode.Stunned:
                    waitTime = Mathf.Max(0f, stunTimer);
                    break;
                case TimelineIconMode.PushedBack:
                    // Estimate time to finish pushback + any stun that will follow
                    waitTime = stunDuration;
                    break;
            }
            
            return waitTime + moveTime;
        }

        /// <summary>
        /// Pushes the tag to the left (away from the trigger) with animated deceleration.
        /// The effect is stronger when closer to the right (trigger point) and scales with attacker's strength.
        /// After pushback completes, enters Stunned mode where recovery is based on enemy's Agility.
        /// </summary>
        /// <param name="basePush">Minimum push amount at u=0.0 (far left, just spawned)</param>
        /// <param name="maxPush">Maximum push amount at u=1.0 (at trigger)</param>
        /// <param name="strengthMultiplier">Multiplier based on attacker's strength (1.0 = baseline)</param>
        /// <param name="enemyAgility">Enemy's agility stat - higher = faster stun recovery</param>
        /// <param name="baseStunDuration">Base stun duration in seconds at agility 10</param>
        public void Pushback(float basePush, float maxPush, float strengthMultiplier = 1f, int enemyAgility = 10, float baseStunDuration = 1f)
        {
            // Calculate pushback: stronger when u is higher (closer to right/trigger)
            // At u=1 (right/trigger), push = maxPush
            // At u=0 (left/spawn), push = basePush
            float proximity = u; // 0 at left, 1 at right (at trigger)
            float pushAmount = Mathf.Lerp(basePush, maxPush, proximity);

            // Scale by attacker's strength
            pushAmount *= Mathf.Max(0.1f, strengthMultiplier);

            // Set target position and initial velocity for animated pushback (toward left / lower u)
            pushTargetU = Mathf.Clamp01(u - pushAmount);
            pushVelocity = pushAmount * 3f; // Initial velocity proportional to push distance
            
            // Calculate stun duration based on enemy's agility
            // Agility 10 = baseStunDuration, Agility 20 = half duration, Agility 5 = double duration
            float agilityMultiplier = 10f / Mathf.Max(1f, enemyAgility);
            stunDuration = baseStunDuration * agilityMultiplier;
            
            // Enter pushback mode
            Mode = TimelineIconMode.PushedBack;
        }

        /// <summary>Runs per-frame update logic.</summary>
        private void Update()
        {
            if (isFading) return;
            
            // Process based on current mode
            switch (Mode)
            {
                case TimelineIconMode.Queued:
                    UpdateQueued();
                    break;
                case TimelineIconMode.Approaching:
                    UpdateApproaching();
                    break;
                case TimelineIconMode.PushedBack:
                    UpdatePushedBack();
                    break;
                case TimelineIconMode.Stunned:
                    UpdateStunned();
                    break;
            }
            
            // Ensure we never drift past the trigger point due to float jitter
            if (Rect != null && Rect.anchoredPosition.x > rightX)
            {
                SetX(rightX);
            }

            // Update label after we potentially moved this frame
            UpdateLabel();

            // Update cast bar visualization
            UpdateCastBar();

            // Right-edge strict check using anchoredPosition.x (right-edge pivot = leading edge)
            // Only trigger if in Approaching mode (not during pushback/stun)
            if (!fired && Mode == TimelineIconMode.Approaching && Rect != null && Rect.anchoredPosition.x >= rightX - ReachTolerance)
            {
                fired = true;
                onReached?.Invoke(this);
            }
        }

        /// <summary>Updates the queued.</summary>
        private void UpdateQueued()
        {
            if (paused) return;
            
            // Countdown queue timer
            queueTimer -= Time.deltaTime;
            if (queueTimer <= 0f)
            {
                queueTimer = 0f;
                Mode = TimelineIconMode.Approaching;
                StartLabelFadeIn();
            }
        }

        /// <summary>Updates the approaching.</summary>
        private void UpdateApproaching()
        {
            if (paused) return;

            // Move toward right / trigger (u = 1 — "loading complete")
            u = Mathf.MoveTowards(u, 1f, uPerSec * Time.deltaTime);
            ApplyPosition();
        }

        /// <summary>Updates the pushed back.</summary>
        private void UpdatePushedBack()
        {
            // Pushback animation runs even when paused (it's a reaction to being hit)
            
            // Decelerate velocity
            pushVelocity = Mathf.MoveTowards(pushVelocity, 0f, PushDeceleration * Time.deltaTime);
            
            // Move toward target with current velocity
            float step = pushVelocity * Time.deltaTime;
            u = Mathf.MoveTowards(u, pushTargetU, step);
            ApplyPosition();
            
            // Check if pushback is complete (reached target or velocity depleted)
            if (pushVelocity <= PushMinVelocity || Mathf.Approximately(u, pushTargetU))
            {
                pushVelocity = 0f;

                // If we hit the far left during reset animation, go to Queued
                if (u <= 0.01f)
                {
                    u = 0f;
                    ApplyPosition();
                    queueTimer = queueDelay;
                    Mode = queueDelay > 0f ? TimelineIconMode.Queued : TimelineIconMode.Approaching;

                    // Hide label when entering queue
                    if (Mode == TimelineIconMode.Queued)
                    {
                        labelAlpha = 0f;
                        ApplyLabelAlpha();
                    }
                }
                // Otherwise go to Stunned (from attack pushback)
                else if (stunDuration > 0f)
                {
                    stunTimer = stunDuration;
                    Mode = TimelineIconMode.Stunned;
                }
                else
                {
                    // No stun, resume approaching
                    Mode = TimelineIconMode.Approaching;
                }
            }
        }

        /// <summary>Updates the stunned.</summary>
        private void UpdateStunned()
        {
            // Stun recovery runs even when paused (it's a status effect)
            stunTimer -= Time.deltaTime;
            if (stunTimer <= 0f)
            {
                stunTimer = 0f;
                Mode = TimelineIconMode.Approaching;
            }
        }

        // Helper for debugging: return a path for a transform (useful in logs)
        /// <summary>Gets the transform path.</summary>
        private string GetTransformPath(Transform t)
        {
            if (t == null) return "<null>";
            var parts = new System.Collections.Generic.List<string>();
            var cur = t;
            while (cur != null)
            {
                parts.Add(cur.name);
                cur = cur.parent;
            }
            parts.Reverse();
            return string.Join("/", parts);
        }

        /// <summary>Updates the label.</summary>
        private void UpdateLabel()
        {
            if (Label == null) return;
            float sec = GetSecondsRemaining();
            Label.text = sec.ToString("0.0");
        }

        /// <summary>Applies the label alpha.</summary>
        private void ApplyLabelAlpha()
        {
            if (Label == null) return;
            var c = Label.color;
            Label.color = new Color(c.r, c.g, c.b, labelAlpha);
        }

        /// <summary>Start label fade in.</summary>
        private void StartLabelFadeIn()
        {
            if (labelFadeCoroutine != null)
                StopCoroutine(labelFadeCoroutine);
            labelFadeCoroutine = StartCoroutine(LabelFadeInRoutine());
        }

        /// <summary>Coroutine that executes the label fade in sequence.</summary>
        private IEnumerator LabelFadeInRoutine()
        {
            float startAlpha = labelAlpha;
            float elapsed = 0f;

            while (elapsed < LabelFadeDuration)
            {
                elapsed += Time.deltaTime;
                labelAlpha = Mathf.Lerp(startAlpha, 1f, elapsed / LabelFadeDuration);
                ApplyLabelAlpha();
                yield return null;
            }

            labelAlpha = 1f;
            ApplyLabelAlpha();
            labelFadeCoroutine = null;
        }

        /// <summary>Fade and destroy.</summary>
        public void FadeAndDestroy(float duration =0.25f)
        {
            if (isFading) return;
            isFading = true;
            StartCoroutine(FadeOutAndDestroy(duration));
        }

        // ===================== Cast Bar =====================

        /// <summary>Current casting state (null when not casting).</summary>
        public CastingState ActiveCast { get; private set; }

        /// <summary>Cast bar image child (created on demand).</summary>
        private Image castBarImage;

        /// <summary>
        /// Begins a cast visualization on this tag. Creates a colored bar that
        /// fills over the cast duration.
        /// </summary>
        public void BeginCast(CastingState state)
        {
            ActiveCast = state;
            EnsureCastBar();
            if (castBarImage != null)
            {
                castBarImage.gameObject.SetActive(true);
                castBarImage.fillAmount = 0f;
                castBarImage.color = new Color(0.3f, 0.6f, 1f, 0.8f); // blue cast bar
            }
        }

        /// <summary>Clears the casting state and hides the bar.</summary>
        public void ClearCast()
        {
            ActiveCast = null;
            if (castBarImage != null)
                castBarImage.gameObject.SetActive(false);
        }

        /// <summary>Updates the cast bar fill each frame.</summary>
        private void UpdateCastBar()
        {
            if (ActiveCast == null || castBarImage == null) return;

            if (ActiveCast.IsInterrupted)
            {
                castBarImage.color = new Color(1f, 0.3f, 0.3f, 0.8f); // red on interrupt
                StartCoroutine(FadeCastBar());
                ActiveCast = null;
                return;
            }

            castBarImage.fillAmount = ActiveCast.Progress;

            if (ActiveCast.IsComplete)
            {
                castBarImage.gameObject.SetActive(false);
                ActiveCast = null;
            }
        }

        /// <summary>Ensures the cast bar child image exists.</summary>
        private void EnsureCastBar()
        {
            if (castBarImage != null) return;
            var barGO = new GameObject("CastBar");
            barGO.layer = LayerMask.NameToLayer("Default");
            var rt = barGO.AddComponent<RectTransform>();
            rt.SetParent(Rect, false);
            rt.anchorMin = new Vector2(0f, 0f);
            rt.anchorMax = new Vector2(1f, 0.15f);
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;
            barGO.AddComponent<CanvasRenderer>();
            castBarImage = barGO.AddComponent<Image>();
            castBarImage.type = Image.Type.Filled;
            castBarImage.fillMethod = Image.FillMethod.Horizontal;
            castBarImage.fillOrigin = 0;
            castBarImage.fillAmount = 0f;
            castBarImage.color = new Color(0.3f, 0.6f, 1f, 0.8f);
            barGO.SetActive(false);
        }

        /// <summary>Fades the cast bar out after interruption.</summary>
        private IEnumerator FadeCastBar()
        {
            if (castBarImage == null) yield break;
            float t = 0f;
            while (t < 0.5f)
            {
                t += Time.deltaTime;
                var c = castBarImage.color;
                castBarImage.color = new Color(c.r, c.g, c.b, Mathf.Lerp(0.8f, 0f, t / 0.5f));
                yield return null;
            }
            castBarImage.gameObject.SetActive(false);
        }

        /// <summary>Fade out and destroy.</summary>
        private IEnumerator FadeOutAndDestroy(float duration)
        {
            float t =0f;
            float start = CanvasGroup != null ? CanvasGroup.alpha :1f;
            while (t < duration)
            {
                t += Time.deltaTime;
                float a = Mathf.Lerp(start,0f, Mathf.Clamp01(t / duration));
                if (CanvasGroup != null) CanvasGroup.alpha = a;
                else if (Tag != null) Tag.color = new Color(Tag.color.r, Tag.color.g, Tag.color.b, a);
                yield return null;
            }
            Destroy(gameObject);
        }
    }
}
