using Assets.Scripts.Libraries;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;
using g = Assets.Helpers.GameHelper;
using c = Assets.Helpers.CanvasHelper;

namespace Assets.Scripts.Canvas
{
    [DisallowMultipleComponent]
    public sealed class TimelineBarInstance : MonoBehaviour
    {
        [Header("Parts")]
        [SerializeField] private RectTransform barRect; // visual line (center pivot)
        [SerializeField] private RectTransform tagsRoot; // parent for tags
        [SerializeField] private TimelineTag tagPrefab;
        [SerializeField] private RectTransform triggerPointRect; // left-most trigger
        [SerializeField] private RectTransform spawnPointRect; // right-most spawn

        [Header("Layout")]
        [Tooltip("Percent of canvas width used for the timeline length (match TimerBar).")]
        [SerializeField] private float canvasPercent = 0.96f;

        [Header("Tuning")]
        [Tooltip("Time in seconds for a tag to cross the full bar (constant for all enemies).")]
        [SerializeField] private float crossingTimeSeconds = 5f;
        [Tooltip("Maximum release delay in seconds for slowest enemies (fastest enemies release immediately).")]
        [SerializeField] private float maxReleaseDelay = 3f;
        [Tooltip("Vertical spacing between duplicate tags (same enemy) in local pixels.")]
        [SerializeField] private float tagRowHeight = 14f;
        [SerializeField] private bool debugLogs = false;

        [Header("Pushback on Attack")]
        [Tooltip("Minimum pushback (normalized 0-1) when enemy is at the far right.")]
        [SerializeField] private float pushbackBase = 0.05f;
        [Tooltip("Maximum pushback (normalized 0-1) when enemy is at the trigger point (far left).")]
        [SerializeField] private float pushbackMax = 0.4f;
        [Tooltip("Base stun duration in seconds after pushback (at Agility 10).")]
        [SerializeField] private float baseStunDuration = 1f;

        private readonly List<TimelineTag> activeTags = new List<TimelineTag>();
        private bool advancing;
        public bool IsAdvancing => advancing;

        private float cachedLeftX;
        private float cachedRightX;
        private bool layoutReady;
        private float halfWidth; // runtime half-length of bar

        // Exposed endpoints (center is 0). Tags move from RightX toward LeftX.
        private float LeftX => -halfWidth;
        private float RightX => halfWidth;
        private float Width => Mathf.Max(1f, RightX - LeftX);

        private void Awake()
        {
            var tagPrefabGO = PrefabLibrary.Get("TimelineTagPrefab");
            if (tagPrefabGO != null)
                tagPrefab = tagPrefabGO.GetComponent<TimelineTag>();

            if (barRect == null) barRect = GetComponent<RectTransform>();
            if (barRect != null)
            {
                barRect.pivot = new Vector2(0.5f, 0.5f); // center pivot for symmetric coordinates
                barRect.anchorMin = barRect.anchorMax = new Vector2(0.5f, 0.5f);
            }

            // Ensure trigger & spawn point objects exist for visual debugging / design hooks
            if (triggerPointRect == null && barRect != null)
            {
                triggerPointRect = new GameObject("TriggerPoint", typeof(RectTransform)).GetComponent<RectTransform>();
                triggerPointRect.SetParent(barRect, false);
            }
            if (spawnPointRect == null && barRect != null)
            {
                spawnPointRect = new GameObject("SpawnPoint", typeof(RectTransform)).GetComponent<RectTransform>();
                spawnPointRect.SetParent(barRect, false);
            }
            if (tagsRoot == null && barRect != null)
            {
                var go = new GameObject("Tags", typeof(RectTransform));
                tagsRoot = go.GetComponent<RectTransform>();
                tagsRoot.SetParent(barRect, false);
                tagsRoot.anchorMin = tagsRoot.anchorMax = new Vector2(0.5f, 0.5f); // center reference frame
                tagsRoot.pivot = new Vector2(0.5f, 0.5f);
            }
            cachedLeftX = float.NaN; cachedRightX = float.NaN;
        }

        private void Start()
        {
            RebuildLayout();
            StartCoroutine(EnsureLayoutThenReposition());
            PauseAll();
        }

        private System.Collections.IEnumerator EnsureLayoutThenReposition()
        {
            for (int i = 0; i < 2; i++) yield return null;
            if (barRect != null) LayoutRebuilder.ForceRebuildLayoutImmediate(barRect);
            layoutReady = true;
            UpdateAllEndpoints();
            Recalculate();
            if (debugLogs) Debug.Log($"[TimelineBar] LayoutReady left={LeftX:F1} right={RightX:F1} width={Width:F1}");
        }

        private void OnRectTransformDimensionsChange()
        {
            RebuildLayout();
            UpdateAllEndpoints();
            Recalculate();
        }

        private void RebuildLayout()
        {
            if (c.CanvasRect == null || barRect == null) return;
            float targetWidth = Mathf.Max(1f, c.CanvasRect.rect.width * canvasPercent);
            // Preserve existing height
            Vector2 size = barRect.sizeDelta;
            size.x = targetWidth;
            barRect.sizeDelta = size;
            halfWidth = targetWidth * 0.5f;

            // Position trigger/spawn points
            if (triggerPointRect != null)
            {
                triggerPointRect.anchorMin = triggerPointRect.anchorMax = new Vector2(0.5f, 0.5f);
                triggerPointRect.pivot = new Vector2(0.5f, 0.5f);
                triggerPointRect.anchoredPosition = new Vector2(LeftX, 0f);
            }
            if (spawnPointRect != null)
            {
                spawnPointRect.anchorMin = spawnPointRect.anchorMax = new Vector2(0.5f, 0.5f);
                spawnPointRect.pivot = new Vector2(0.5f, 0.5f);
                spawnPointRect.anchoredPosition = new Vector2(RightX, 0f);
            }
        }

        private float UnitsPerSecFromSpeed(int speed)
        {
            // Speed stat affects movement speed: higher speed = faster movement
            // Speed 10 = crosses bar in crossingTimeSeconds (baseline)
            // Speed 20 = crosses bar in half the time, Speed 5 = takes twice as long
            float crossing = Mathf.Max(0.1f, crossingTimeSeconds);
            float speedMultiplier = speed / 10f;
            return Mathf.Max(0.01f, speedMultiplier / crossing);
        }

        private float GetQueueDelayFromSpeed(int speed)
        {
            // Queue delay based on speed: faster enemies wait less
            // Speed 20 = ~1 second wait, Speed 5 = ~6 seconds wait
            // Formula: 6 - (speed / 20) * 5, clamped to [1, 6]
            float delay = 6f - (speed / 20f) * 5f;
            return Mathf.Clamp(delay, 1f, 6f);
        }

        private float GetInitialPositionFromSpeed(int speed, int maxSpeed, int minSpeed)
        {
            // Scatter enemies across timeline based on speed
            // Fastest enemies start closer to left (lower u), slowest start at right (higher u)
            if (maxSpeed <= minSpeed) return 0.5f;
            float t = (float)(maxSpeed - speed) / (maxSpeed - minSpeed);
            // t=0 for fastest (start at u=0.2), t=1 for slowest (start at u=0.9)
            return Mathf.Lerp(0.2f, 0.9f, t);
        }

        private System.Collections.Generic.IEnumerable<ActorInstance> SortedEnemiesBySpeedDesc()
        {
            return g.Actors.Enemies.Where(e => e != null && e.IsPlaying).OrderByDescending(e => e.Stats.Speed.ToInt());
        }

        public void Clear()
        {
            for (int i = activeTags.Count - 1; i >= 0; i--) if (activeTags[i] != null) Destroy(activeTags[i].gameObject);
            activeTags.Clear();
        }

        /// <summary>
        /// Ensure all currently playing enemies have a tag.
        /// If there are no tags yet, scatter across timeline by speed (fastest near left).
        /// Otherwise, only add missing ones at the far right.
        /// Also prunes tags whose owners are gone/inactive.
        /// </summary>
        public void EnsureTagsForAllEnemies(bool redistributeIfNone = true)
        {
            // Remove stale tags (dead or despawned)
            for (int i = activeTags.Count - 1; i >= 0; i--)
            {
                var t = activeTags[i];
                if (t == null || t.Owner == null || !t.Owner.IsPlaying)
                {
                    if (t != null) t.FadeAndDestroy(0.15f);
                    activeTags.RemoveAt(i);
                }
            }

            var playing = g.Actors.Enemies.Where(e => e != null && e.IsPlaying).ToList();
            if (playing.Count == 0)
            {
                return;
            }

            // Add missing tags
            var missing = playing.Where(e => !activeTags.Any(t => t != null && t.Owner == e)).ToList();

            if (activeTags.Count == 0 && redistributeIfNone)
            {
                // Scatter enemies across timeline based on speed (fastest near left)
                int maxSpd = playing.Max(e => e.Stats.Speed.ToInt());
                int minSpd = playing.Min(e => e.Stats.Speed.ToInt());
                foreach (var enemy in playing)
                {
                    int spd = enemy.Stats.Speed.ToInt();
                    float startU = GetInitialPositionFromSpeed(spd, maxSpd, minSpd);
                    // No queue delay on initial spawn - they start already on the timeline
                    SpawnTag(enemy, startU, 0f);
                }
            }
            else
            {
                // New enemies spawn at far right with speed-based queue delay
                foreach (var enemy in missing)
                {
                    int spd = enemy.Stats.Speed.ToInt();
                    float delay = GetQueueDelayFromSpeed(spd);
                    SpawnTag(enemy, 1f, delay);
                }
            }

            if (!layoutReady) StartCoroutine(EnsureLayoutThenReposition()); else { UpdateAllEndpoints(); Recalculate(); }
            PauseAll(); // Start paused until hero moves
        }

        public void SpawnInitialForAllEnemies()
        {
            EnsureTagsForAllEnemies(true);
        }

        private void PauseAll()
        { foreach (var t in activeTags) t?.Pause(); advancing = false; }
        private void ResumeAll()
        { foreach (var t in activeTags) t?.Resume(); advancing = true; }

        public void OnHeroStartMove() { Recalculate(); ResumeAll(); }
        public void OnHeroStopMove() { PauseAll(); }
        public void OnEnemyTurnStarted(ActorInstance enemy) { 
            PauseAll(); 
            // Lock any tags that are already at/past the left to the exact left position
            UpdateAllEndpoints();
            float left = LeftX;
            foreach (var t in activeTags)
            {
                if (t == null || t.Rect == null) continue;
                if (t.Rect.anchoredPosition.x <= left + 0.25f)
                {
                    t.SetU(0f); // snaps exactly to LeftX via ApplyPosition clamp
                    t.Pause();
                }
            }
        }
        public void OnEnemyTurnFinished(ActorInstance enemy)
        {
            var tag = activeTags.FirstOrDefault(t => t != null && t.Owner == enemy);
            if (tag != null)
            {
                UpdateAllEndpoints();
                // Animate the tag back to spawn point, then it enters Queued mode
                tag.ResetToSpawn();
            }
        }


        /// <summary>
        /// Pushes the enemy's timeline tag to the right when attacked.
        /// The closer the enemy is to the left (trigger point), the stronger the pushback.
        /// Pushback scales with attacker's Strength, and stun recovery depends on enemy's Agility.
        /// </summary>
        public void PushbackOnAttack(ActorInstance enemy, int attackerStrength = 10)
        {
            if (enemy == null) return;
            var tag = activeTags.FirstOrDefault(t => t != null && t.Owner == enemy);
            if (tag != null)
            {
                // Strength of 10 = 1.0x multiplier (baseline)
                float strengthMultiplier = attackerStrength / 10f;
                
                // Get enemy's agility for stun recovery calculation
                int enemyAgility = enemy.Stats != null 
                    ? enemy.Stats.Agility.ToInt() 
                    : 10;
                
                tag.Pushback(pushbackBase, pushbackMax, strengthMultiplier, enemyAgility, baseStunDuration);
                if (debugLogs) Debug.Log($"[TimelineBar] Pushed {enemy.name} tag (str={attackerStrength}, agi={enemyAgility}, mode={tag.Mode})");
            }
        }

        // Track if we're currently processing a trigger to prevent double-triggers
        private bool isProcessingTrigger = false;

        /// <summary>
        /// Called when hero turn starts - reset the trigger flag to allow future enemy triggers
        /// </summary>
        public void ResetTriggerFlag()
        {
            isProcessingTrigger = false;
        }

        private void OnTagReachedLeft(TimelineTag tag)
        {
            if (tag == null) return;
            
            // Prevent processing if already processing a trigger OR if enemy turn in progress
            if (isProcessingTrigger) return;
            if (g.TurnManager != null && g.TurnManager.IsEnemyTurn) return;
            
            var triggeringEnemy = tag.Owner;
            if (triggeringEnemy == null || !triggeringEnemy.IsPlaying) return;
            
            // SET FLAG IMMEDIATELY - don't reset until hero turn starts
            isProcessingTrigger = true;
            
            // Lock the arriving tag exactly at the trigger and pause all
            tag.SetU(0f);
            PauseAll();
            
            // Disable input during transition
            g.InputManager.InputMode = InputMode.None;
            
            // Queue the timeline trigger sequence
            g.SequenceManager.Add(new Assets.Scripts.Sequences.TimelineTriggerSequence(triggeringEnemy));
            g.SequenceManager.Execute();
        }

        public float GetSecondsUntilNextEnemyReachesLeft()
        {
            if (activeTags.Count == 0) 
                return 0f;

            float min = float.PositiveInfinity;
            foreach (var t in activeTags)
            {
                if (t == null || t.Owner == null || !t.Owner.IsPlaying) continue;
                float sec = t.GetSecondsRemaining();
                if (sec < min) min = sec;
            }
            return float.IsInfinity(min) ? 0f : Mathf.Max(0f, min);
        }

        // NEW: Advance all tags by a number of seconds instantly (banking mechanic)
        // Returns true if at least one tag reached the trigger point.
        public bool AdvanceBySeconds(float seconds)
        {
            seconds = Mathf.Max(0f, seconds);
            if (seconds <= 0f || activeTags.Count == 0) return false;
            bool anyReached = false;
            foreach (var t in activeTags)
            {
                if (t == null) continue;
                float u = t.GetU();
                float uPerSec = Mathf.Max(0.0001f, t.GetUPerSec());
                float newU = Mathf.Max(0f, u - uPerSec * seconds);
                t.SetU(newU);
                // If moved to (or past) left edge, TimelineTag will invoke its callback next Update frame.
                if (Mathf.Approximately(newU, 0f)) anyReached = true;
            }
            return anyReached;
        }

        /// <summary>
        /// Bank directly to next arriving tag. Returns the seconds that would be skipped
        /// and the enemy that would trigger. Does NOT start the sequence - caller does that.
        /// </summary>
        public (ActorInstance enemy, float secondsUsed) GetNextBankTarget()
        {
            if (activeTags.Count == 0)
                return (null, 0f);

            // Find earliest tag by seconds remaining (next enemy to arrive)
            TimelineTag earliest = null;
            float minSec = float.PositiveInfinity;
            foreach (var t in activeTags)
            {
                if (t == null || t.Owner == null || !t.Owner.IsPlaying) continue;
                if (!t.Owner.IsEnemy) continue;
                float sec = t.GetSecondsRemaining();
                if (sec < minSec)
                {
                    minSec = sec; earliest = t;
                }
            }
            
            if (earliest == null || float.IsInfinity(minSec))
                return (null, 0f);
            
            // Allow banking even if minSec is very small (but not zero)
            float secondsUsed = Mathf.Max(0.001f, minSec);
            return (earliest.Owner, secondsUsed);
        }

        /// <summary>
        /// Advance timeline to next trigger point visually. Called before TimelineTriggerSequence.
        /// </summary>
        public void AdvanceToNextTrigger(ActorInstance enemy, float seconds)
        {
            // Advance all tags by the time skipped (visual movement)
            AdvanceBySeconds(seconds);
            
            // Lock the arriving tag at the trigger point
            var tag = activeTags.FirstOrDefault(t => t != null && t.Owner == enemy);
            if (tag != null)
            {
                tag.SetU(0f);
                tag.Pause();
            }
            
            PauseAll();
        }

        private void SpawnTag(ActorInstance enemy, float startU, float releaseDelay = 0f)
        {
            if (enemy == null || !enemy.IsEnemy) return;
            if (tagPrefab == null) { Debug.LogError("TimelineBarInstance: tagPrefab not set."); return; }
            var parent = tagsRoot != null ? tagsRoot : barRect;
            var tag = Instantiate(tagPrefab, parent, false);
            tag.name = $"TimelineTag_{enemy.name}";
            int dup = activeTags.Count(a => a != null && a.Owner == enemy);
            var tr = tag.GetComponent<RectTransform>();
            // Tag rect: left-edge pivot, anchored at center for symmetric X
            tr.anchorMin = tr.anchorMax = new Vector2(0.5f, 0.5f);
            tr.pivot = new Vector2(0f, 0.5f);
            tr.anchoredPosition = new Vector2(Mathf.Lerp(LeftX, RightX, startU), -dup * tagRowHeight);
            float uSpeed = UnitsPerSecFromSpeed(enemy.Stats.Speed.ToInt());
            tag.InitializeNormalized(enemy, LeftX, RightX, startU, uSpeed, OnTagReachedLeft, releaseDelay);
            activeTags.Add(tag);
        }

        private void UpdateAllEndpoints()
        {
            float left = LeftX; float right = RightX;
            foreach (var t in activeTags) t?.UpdateEndpoints(left, right);
        }

        private void Recalculate()
        {
            float left = LeftX; float right = RightX;
            if (float.IsNaN(cachedLeftX) || float.IsNaN(cachedRightX) || !Mathf.Approximately(left, cachedLeftX) || !Mathf.Approximately(right, cachedRightX))
            {
                cachedLeftX = left; cachedRightX = right;
                foreach (var t in activeTags)
                {
                    if (t == null || t.Rect == null) continue;
                    t.UpdateEndpoints(left, right);
                    var p = t.Rect.anchoredPosition;
                    // Only auto-loop tags that slipped past the left edge during HERO turns.
                    // During enemy turns, keep the tag at LeftX until OnEnemyTurnFinished resets it.
                    bool isHeroTurn = g.TurnManager == null || g.TurnManager.IsHeroTurn;
                    if (isHeroTurn && p.x <= left) t.SetU(1f);
                }
            }
        }
    }
}
