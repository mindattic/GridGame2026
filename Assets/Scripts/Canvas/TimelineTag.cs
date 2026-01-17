using Assets.Scripts.Models;
using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using g = Assets.Helpers.GameHelper;
using TMPro;
using Assets.Scripts.Libraries; // for ActorLibrary

namespace Assets.Scripts.Canvas
{
    /// <summary>
    /// Represents the current state of a timeline tag
    /// </summary>
    public enum TimelineTagMode
    {
        /// <summary>Tag is at spawn point waiting to be released (based on enemy speed)</summary>
        Queued,
        /// <summary>Tag is moving left toward the trigger point</summary>
        Approaching,
        /// <summary>Tag is being pushed back to the right (animated with deceleration)</summary>
        PushedBack,
        /// <summary>Tag has stopped after pushback, recovering based on Agility</summary>
        Stunned
    }

    [DisallowMultipleComponent]
    [RequireComponent(typeof(RectTransform))]
    public sealed class TimelineTag : MonoBehaviour, IPointerClickHandler
    {
        [Header("Parts")]
        [SerializeField] private Image Tag;
        [SerializeField] private Image Icon; 
        [SerializeField] private TextMeshProUGUI Label;
        [SerializeField] private CanvasGroup CanvasGroup; // for fade-out

        [Header("Runtime")]
        public ActorInstance Owner; // enemy owning this tag
        public RectTransform Rect { get; private set; }
        public TimelineTagMode Mode { get; private set; } = TimelineTagMode.Queued;

        // Normalized motion state (resolution-independent)
        private float leftX; // bar-local x at u=0 (left edge)
        private float rightX; // bar-local x at u=1 (right edge)
        private float u; // normalized position in [0..1], 1 = right, 0 = left
        private float uPerSec; // normalized speed per second (u units per sec)
        private float queueDelay; // time in seconds to wait at right before moving (based on Speed)
        private float queueTimer; // countdown timer for queue release

        // Pushback animation state
        private float pushTargetU; // target position for pushback animation
        private float pushVelocity; // current velocity during pushback (decelerates)
        private const float PushDeceleration = 2.5f; // how quickly pushback slows down
        private const float PushMinVelocity = 0.01f; // velocity threshold to stop pushback

        // Stun state
        private float stunDuration; // how long to stay stunned (based on Agility)
        private float stunTimer; // countdown for stun recovery

        private System.Action<TimelineTag> onReached;
        private bool isFading;
        private bool paused;
        private bool fired;

        // Label fade-in state (hidden in queue, fades in when approaching)
        private const float LabelFadeDuration = 0.4f;
        private Coroutine labelFadeCoroutine;
        private float labelAlpha;

        // Tolerance for deciding a tag reached the left edge (in local pixels)
        private const float ReachTolerance =0.25f;

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
                    Debug.LogWarning("TimelineTag: Child Tag Image not found. Add a Tag child or assign `Tag`.", this);
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
                    Debug.LogWarning("TimelineTag: Child Icon Image not found. Add an Icon child or assign `Icon`.", this);
                }
                else
                {
                    Debug.Log($"TimelineTag: Icon Image found on GameObject '{Icon.gameObject.name}' (path: {GetTransformPath(Icon.transform)})", this);
                }
            }
            if (Label == null)
            {
                var LabelTransform = transform.Find("Label");
                Label = LabelTransform != null ? LabelTransform.GetComponent<TextMeshProUGUI>() : GetComponentInChildren<TextMeshProUGUI>(true);
                if (Label == null)
                    Debug.LogWarning("TimelineTag: Child Label (TextMeshProUGUI) not found. Add a Label child or assign `Label`.", this);
            }

            // Enable clicks on the tag so taps select the associated actor
            if (Tag != null) Tag.raycastTarget = true;
            if (Icon != null) Icon.raycastTarget = false;
            if (Label != null) Label.raycastTarget = false;

            // Left-edge pivot so anchoredPosition.x represents the tag's LEFT edge exactly
            if (Rect != null)
            {
                Rect.anchorMin = new Vector2(0f,0.5f);
                Rect.anchorMax = new Vector2(0f,0.5f);
                Rect.pivot = new Vector2(0f,0.5f); // changed from0.5f to0f for precise alignment
            }
            // Ignore layout so manual positioning is preserved
            var le = gameObject.GetComponent<LayoutElement>();
            if (le == null) le = gameObject.AddComponent<LayoutElement>();
            le.ignoreLayout = true;
        }

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
        public void OnPointerClick(PointerEventData eventData)
        {
            if (Owner == null) 
                return;
            g.SelectionManager.Select(Owner);
        }

        // Initialize using normalized coordinates and speed
        public void InitializeNormalized(ActorInstance owner, float leftX, float rightX, float startU, float uPerSec, System.Action<TimelineTag> onReached, float queueDelay = 0f)
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
            Mode = queueDelay > 0f ? TimelineTagMode.Queued : TimelineTagMode.Approaching;
            
            // Label is hidden in queue, visible when approaching
            labelAlpha = Mode == TimelineTagMode.Queued ? 0f : 1f;
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
                Debug.Log($"TimelineTag: Assigned icon '{(sprite != null ? sprite.name : "<null>")}' for actor '{Owner?.characterClass}' tags='{(data != null ? data.Tags.ToString() : "<no data>")}' on GameObject '{Icon.gameObject.name}' (path: {GetTransformPath(Icon.transform)})", this);
            }

            ApplyPosition();
            UpdateLabel();
        }

        // Backward-compatible initializer
        public void Initialize(ActorInstance owner, float leftEdgeX, float startX, float moveSpeedPxPerSec, System.Action<TimelineTag> onReached)
        {
            float width = Mathf.Max(1f, rightX - leftEdgeX);
            float startU = Mathf.InverseLerp(leftEdgeX, rightX, startX);
            float uSpeed = Mathf.Abs(moveSpeedPxPerSec) / width;
            InitializeNormalized(owner, leftEdgeX, rightX, startU, uSpeed, onReached);
        }

        public void UpdateEndpoints(float newLeftX, float newRightX)
        {
            // Preserve normalized u while endpoints shift
            leftX = newLeftX;
            rightX = Mathf.Max(newRightX, newLeftX + 1f);
            ApplyPosition();
            UpdateLabel();
        }

        public void Pause() => paused = true;
        public void Resume() => paused = false;
        public void SetAlpha(float a) { if (CanvasGroup != null) CanvasGroup.alpha = Mathf.Clamp01(a); }

        /// <summary>
        /// Resets the tag to the spawn point (far right) and enters Queued mode.
        /// Called when an enemy's turn finishes. Assigns queue delay based on speed.
        /// </summary>
        public void ResetToSpawn()
        {
            fired = false;
            
            // IMPORTANT: Immediately snap u to 1.0 to prevent re-triggering while at left edge
            // This fixes the double-trigger bug where the tag could fire again before animation completes
            u = 1f;
            ApplyPosition();
            
            // Assign queue delay based on speed: faster enemies wait less (1-6 seconds)
            int speed = Owner != null ? Owner.Stats.Speed.ToInt() : 10;
            queueDelay = Mathf.Clamp(6f - (speed / 20f) * 5f, 1f, 6f);
            queueTimer = queueDelay;
            
            // Enter Queued mode (or Approaching if no delay)
            Mode = queueDelay > 0f ? TimelineTagMode.Queued : TimelineTagMode.Approaching;
            
            // Hide label in queue, show if immediately approaching
            labelAlpha = Mode == TimelineTagMode.Queued ? 0f : 1f;
            ApplyLabelAlpha();
            
            // Reset pushback/stun state
            pushVelocity = 0f;
            stunTimer = 0f;
            stunDuration = 0f;
            
            UpdateLabel();
        }

        /// <summary>
        /// Legacy reset - immediately snaps to spawn and waits in queue.
        /// </summary>
        public void ResetForNextCycle()
        {
            fired = false;
            u = 1f;
            ApplyPosition();
            // Assign queue delay based on speed (1-6 seconds)
            int speed = Owner != null ? Owner.Stats.Speed.ToInt() : 10;
            queueDelay = Mathf.Clamp(6f - (speed / 20f) * 5f, 1f, 6f);
            queueTimer = queueDelay;
            Mode = queueDelay > 0f ? TimelineTagMode.Queued : TimelineTagMode.Approaching;
            
            // Hide label in queue, show if immediately approaching
            labelAlpha = Mode == TimelineTagMode.Queued ? 0f : 1f;
            ApplyLabelAlpha();
            
            pushVelocity = 0f;
            stunTimer = 0f;
            UpdateLabel();
        }

        // Set anchored x from normalized u (left-edge pivot guarantees alignment)
        private void ApplyPosition()
        {
            if (Rect == null) return;
            float xPos = Mathf.Lerp(leftX, rightX, Mathf.Clamp01(u));
            // Prevent ever going past the left; lock exactly at leftX when u<=0
            if (xPos < leftX) xPos = leftX;
            var p = Rect.anchoredPosition;
            Rect.anchoredPosition = new Vector2(xPos, p.y);
        }

        public void SetX(float xPos)
        {
            if (Rect != null)
            {
                // Clamp to never cross past the left
                float clamped = Mathf.Max(leftX, xPos);
                var p = Rect.anchoredPosition;
                Rect.anchoredPosition = new Vector2(clamped, p.y);
                u = (rightX - leftX) > 0.0001f ? Mathf.InverseLerp(leftX, rightX, clamped) : u;
            }
            else
            {
                var lp = transform.localPosition;
                float clamped = Mathf.Max(leftX, xPos);
                transform.localPosition = new Vector3(clamped, lp.y, lp.z);
            }
            UpdateLabel();
        }

        public void SetU(float value)
        {
            u = Mathf.Clamp01(value);
            ApplyPosition();
            UpdateLabel();
        }

        public float GetU() => u;
        public float GetUPerSec() => uPerSec;
        public float GetQueueTimer() => queueTimer;
        
        /// <summary>
        /// Sets the queue timer to a new value. Used by TimelineBar to coordinate release spacing.
        /// </summary>
        public void SetQueueTimer(float time)
        {
            queueTimer = Mathf.Max(0f, time);
            if (Mode == TimelineTagMode.Queued && queueTimer <= 0f)
            {
                Mode = TimelineTagMode.Approaching;
                StartLabelFadeIn();
            }
            UpdateLabel();
        }
        
        public float GetSecondsRemaining()
        {
            float moveTime = uPerSec <= 0f ? 0f : Mathf.Max(0f, u / uPerSec);
            
            // Add wait time based on current mode
            float waitTime = 0f;
            switch (Mode)
            {
                case TimelineTagMode.Queued:
                    waitTime = Mathf.Max(0f, queueTimer);
                    break;
                case TimelineTagMode.Stunned:
                    waitTime = Mathf.Max(0f, stunTimer);
                    break;
                case TimelineTagMode.PushedBack:
                    // Estimate time to finish pushback + any stun that will follow
                    waitTime = stunDuration;
                    break;
            }
            
            return waitTime + moveTime;
        }

        /// <summary>
        /// Pushes the tag to the right with animated deceleration. 
        /// The effect is stronger when closer to the left (trigger point) and scales with attacker's strength.
        /// After pushback completes, enters Stunned mode where recovery is based on enemy's Agility.
        /// </summary>
        /// <param name="basePush">Minimum push amount at u=1.0 (far right)</param>
        /// <param name="maxPush">Maximum push amount at u=0.0 (at trigger)</param>
        /// <param name="strengthMultiplier">Multiplier based on attacker's strength (1.0 = baseline)</param>
        /// <param name="enemyAgility">Enemy's agility stat - higher = faster stun recovery</param>
        /// <param name="baseStunDuration">Base stun duration in seconds at agility 10</param>
        public void Pushback(float basePush, float maxPush, float strengthMultiplier = 1f, int enemyAgility = 10, float baseStunDuration = 1f)
        {
            // Calculate pushback: stronger when u is lower (closer to left/trigger)
            // At u=0 (left), push = maxPush
            // At u=1 (right), push = basePush
            float proximity = 1f - u; // 0 at right, 1 at left
            float pushAmount = Mathf.Lerp(basePush, maxPush, proximity);
            
            // Scale by attacker's strength
            pushAmount *= Mathf.Max(0.1f, strengthMultiplier);
            
            // Set target position and initial velocity for animated pushback
            pushTargetU = Mathf.Clamp01(u + pushAmount);
            pushVelocity = pushAmount * 3f; // Initial velocity proportional to push distance
            
            // Calculate stun duration based on enemy's agility
            // Agility 10 = baseStunDuration, Agility 20 = half duration, Agility 5 = double duration
            float agilityMultiplier = 10f / Mathf.Max(1f, enemyAgility);
            stunDuration = baseStunDuration * agilityMultiplier;
            
            // Enter pushback mode
            Mode = TimelineTagMode.PushedBack;
        }

        private void Update()
        {
            if (isFading) return;
            
            // Process based on current mode
            switch (Mode)
            {
                case TimelineTagMode.Queued:
                    UpdateQueued();
                    break;
                case TimelineTagMode.Approaching:
                    UpdateApproaching();
                    break;
                case TimelineTagMode.PushedBack:
                    UpdatePushedBack();
                    break;
                case TimelineTagMode.Stunned:
                    UpdateStunned();
                    break;
            }
            
            // Ensure we never drift past the trigger point due to float jitter
            if (Rect != null && Rect.anchoredPosition.x < leftX)
            {
                SetX(leftX);
            }

            // Update label after we potentially moved this frame
            UpdateLabel();

            // Left-edge strict check using anchoredPosition.x (left pivot)
            // Only trigger if in Approaching mode (not during pushback/stun)
            if (!fired && Mode == TimelineTagMode.Approaching && Rect != null && Rect.anchoredPosition.x <= leftX + ReachTolerance)
            {
                fired = true;
                onReached?.Invoke(this);
            }
        }

        private void UpdateQueued()
        {
            if (paused) return;
            
            // Countdown queue timer
            queueTimer -= Time.deltaTime;
            if (queueTimer <= 0f)
            {
                queueTimer = 0f;
                Mode = TimelineTagMode.Approaching;
                StartLabelFadeIn();
            }
        }

        private void UpdateApproaching()
        {
            if (paused) return;
            
            // Move toward left (u = 0)
            u = Mathf.MoveTowards(u, 0f, uPerSec * Time.deltaTime);
            ApplyPosition();
        }

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
                
                // If we hit the far right during reset animation, go to Queued
                if (u >= 0.99f)
                {
                    u = 1f;
                    ApplyPosition();
                    queueTimer = queueDelay;
                    Mode = queueDelay > 0f ? TimelineTagMode.Queued : TimelineTagMode.Approaching;
                    
                    // Hide label when entering queue
                    if (Mode == TimelineTagMode.Queued)
                    {
                        labelAlpha = 0f;
                        ApplyLabelAlpha();
                    }
                }
                // Otherwise go to Stunned (from attack pushback)
                else if (stunDuration > 0f)
                {
                    stunTimer = stunDuration;
                    Mode = TimelineTagMode.Stunned;
                }
                else
                {
                    // No stun, resume approaching
                    Mode = TimelineTagMode.Approaching;
                }
            }
        }

        private void UpdateStunned()
        {
            // Stun recovery runs even when paused (it's a status effect)
            stunTimer -= Time.deltaTime;
            if (stunTimer <= 0f)
            {
                stunTimer = 0f;
                Mode = TimelineTagMode.Approaching;
            }
        }

        // Helper for debugging: return a path for a transform (useful in logs)
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

        private void UpdateLabel()
        {
            if (Label == null) return;
            float sec = GetSecondsRemaining();
            Label.text = sec.ToString("0.0");
        }

        private void ApplyLabelAlpha()
        {
            if (Label == null) return;
            var c = Label.color;
            Label.color = new Color(c.r, c.g, c.b, labelAlpha);
        }

        private void StartLabelFadeIn()
        {
            if (labelFadeCoroutine != null)
                StopCoroutine(labelFadeCoroutine);
            labelFadeCoroutine = StartCoroutine(LabelFadeInRoutine());
        }

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

        public void FadeAndDestroy(float duration =0.25f)
        {
            if (isFading) return;
            isFading = true;
            StartCoroutine(FadeOutAndDestroy(duration));
        }

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
