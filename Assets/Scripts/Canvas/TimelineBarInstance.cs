using Scripts.Factories;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;
using g = Scripts.Helpers.GameHelper;
using c = Scripts.Helpers.CanvasHelper;
using Scripts.Data.Actor;
using Scripts.Data.Items;
using Scripts.Data.Skills;
using Scripts.Effects;
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
    /// TIMELINEBARINSTANCE - Visual turn order timeline UI component.
    /// 
    /// PURPOSE:
    /// Displays the turn order as a horizontal bar with actor tags that move
    /// from right to left. When a tag reaches the trigger point, that actor
    /// takes their turn.
    /// 
    /// VISUAL LAYOUT:
    /// ```
    /// [Trigger] ←←←← [Enemy Tags Moving Left] ←←←← [Spawn Point]
    ///    ↑                                              ↑
    ///  LeftX                                         RightX
    /// (tag reaches here = take turn)          (tags spawn here)
    /// ```
    /// 
    /// MOVEMENT SYSTEM:
    /// - Tags move at speed based on actor's Speed stat
    /// - Faster actors = tags move faster = act sooner
    /// - When tag reaches LeftX (trigger point), that actor's turn begins
    /// 
    /// KEY PROPERTIES:
    /// - activeTags: All TimelineTag instances currently on the bar
    /// - advancing: True while timeline is actively moving tags
    /// - crossingTimeSeconds: Base time for Speed 10 enemy to cross bar
    /// 
    /// PUSHBACK SYSTEM:
    /// When enemies are hit, their tags are pushed back on the timeline.
    /// - pushbackBase: Minimum pushback at far right
    /// - pushbackMax: Maximum pushback at trigger point
    /// 
    /// INTEGRATION:
    /// - TurnManager calls OnEnemyTurnFinished() after enemy acts
    /// - PincerAttackManager triggers pushback via ApplyPushback()
    /// - StageManager calls AddEnemy() when spawning enemies
    /// 
    /// RELATED FILES:
    /// - TimelineTag.cs: Individual actor tag on the timeline
    /// - TimelineTagFactory.cs: Creates TimelineTag instances
    /// - TurnManager.cs: Turn flow control
    /// - TimelineTriggerSequence.cs: Handles tag trigger events
    /// 
    /// ACCESS: g.TimelineBar
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class TimelineBarInstance : MonoBehaviour
    {
        #region Inspector Fields

        [Header("Parts")]
        [SerializeField] private RectTransform barRect;
        [SerializeField] private RectTransform tagsRoot;
        [SerializeField] private TimelineTag tagPrefab;
        [SerializeField] private RectTransform triggerPointRect;
        [SerializeField] private RectTransform spawnPointRect;

        [Header("Layout")]
        [Tooltip("Percent of canvas width used for the timeline length.")]
        [SerializeField] private float canvasPercent = 0.96f;

        [Header("Tuning")]
        [Tooltip("Base time in seconds for an enemy with Speed 10 to cross the full bar.")]
        [SerializeField] private float crossingTimeSeconds = 8f;
        [Tooltip("Maximum release delay in seconds for slowest enemies.")]
        [SerializeField] private float maxReleaseDelay = 4f;
        [Tooltip("Vertical spacing between duplicate tags.")]
        [SerializeField] private float tagRowHeight = 14f;
        [SerializeField] private bool debugLogs = false;

        [Header("Pushback on Attack")]
        [Tooltip("Minimum pushback when enemy is at the far right.")]
        [SerializeField] private float pushbackBase = 0.05f;
        [Tooltip("Maximum pushback when enemy is at the trigger point.")]
        [SerializeField] private float pushbackMax = 0.4f;
        [Tooltip("Base stun duration in seconds after pushback.")]
        [SerializeField] private float baseStunDuration = 1f;

        [Header("Queue Coordination")]
        [Tooltip("Minimum time gap between enemy releases.")]
        [SerializeField] private float minimumReleaseGap = 1.5f;

        #endregion

        #region Runtime State

        private readonly List<TimelineTag> activeTags = new List<TimelineTag>();
        private bool advancing;

        /// <summary>True while timeline is actively moving tags.</summary>
        public bool IsAdvancing => advancing;

        private float cachedLeftX;
        private float cachedRightX;
        private bool layoutReady;
        private float halfWidth;

        /// <summary>Left edge X position (trigger point).</summary>
        private float LeftX => -halfWidth;

        /// <summary>Right edge X position (spawn point).</summary>
        private float RightX => halfWidth;

        /// <summary>Total width of the timeline bar.</summary>
        private float Width => Mathf.Max(1f, RightX - LeftX);

        #endregion

        private void Awake()
        {
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

        private void Update()
        {
            // Periodically enforce queue spacing to prevent overlap
            if (advancing && activeTags.Count > 1)
            {
                EnforceQueueSpacing();
            }
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
            // Speed stat affects movement speed with gentler scaling for strategic play
            // Speed 10 = baseline (crosses in crossingTimeSeconds = 8s)
            // Speed 5  = 0.75x speed (crosses in ~10.7s) 
            // Speed 15 = 1.25x speed (crosses in ~6.4s)
            // Speed 20 = 1.5x speed (crosses in ~5.3s)
            // Formula: 0.5 + (speed / 20) gives range of 0.75x to 1.5x
            float crossing = Mathf.Max(0.1f, crossingTimeSeconds);
            float speedMultiplier = 0.5f + (speed / 20f);
            return Mathf.Max(0.01f, speedMultiplier / crossing);
        }

        private float GetQueueDelayFromSpeed(int speed)
        {
            // Queue delay based on speed: faster enemies wait less before starting approach
            // Speed 20 = ~1.5 second wait, Speed 5 = ~4 seconds wait
            // Gives player time to set up pincer attacks
            float delay = 4f - (speed / 20f) * 2.5f;
            return Mathf.Clamp(delay, 1.5f, 4f);
        }

        /// <summary>
        /// Calculate the earliest safe release time that won't cause overlap with other tags.
        /// Returns the queue delay needed to maintain minimum spacing.
        /// </summary>
        private float GetCoordinatedQueueDelay(float baseDelay)
        {
            if (activeTags.Count == 0) return baseDelay;
            
            // Collect all tags that are queued or approaching, sorted by when they'll reach the trigger
            var releaseInfo = new List<(float releaseTime, float arrivalTime)>();
            
            foreach (var t in activeTags)
            {
                if (t == null || t.Owner == null || !t.Owner.IsPlaying) continue;
                
                float queueTime = t.Mode == TimelineTagMode.Queued ? t.GetQueueTimer() : 0f;
                float moveTime = t.GetUPerSec() > 0f ? t.GetU() / t.GetUPerSec() : 0f;
                
                // Add stun time if applicable
                if (t.Mode == TimelineTagMode.Stunned)
                {
                    queueTime = t.GetSecondsRemaining() - moveTime;
                }
                
                float arrivalTime = queueTime + moveTime;
                releaseInfo.Add((queueTime, arrivalTime));
            }
            
            if (releaseInfo.Count == 0) return baseDelay;
            
            // New tag starts at u=1.0, so we need to check when it would arrive
            // and ensure it doesn't release within minimumReleaseGap of others
            float myMoveTime = crossingTimeSeconds; // Time to cross full bar
            float myReleaseTime = baseDelay;
            float myArrivalTime = myReleaseTime + myMoveTime;
            
            // Sort existing tags by arrival time
            releaseInfo.Sort((a, b) => a.arrivalTime.CompareTo(b.arrivalTime));
            
            // Check each existing tag and ensure our release doesn't conflict
            foreach (var (releaseTime, arrivalTime) in releaseInfo)
            {
                // If our arrival would be within gap of theirs, delay our release
                if (Mathf.Abs(myArrivalTime - arrivalTime) < minimumReleaseGap)
                {
                    // Push our arrival to be minimumReleaseGap after theirs
                    myArrivalTime = arrivalTime + minimumReleaseGap;
                    myReleaseTime = myArrivalTime - myMoveTime;
                }
            }
            
            return Mathf.Max(baseDelay, myReleaseTime);
        }

        /// <summary>
        /// Adjusts queue timers for all queued tags to prevent releases within minimumReleaseGap.
        /// Called periodically and after state changes.
        /// </summary>
        private void EnforceQueueSpacing()
        {
            // Get all queued tags with their projected arrival times
            var queuedTags = new List<(TimelineTag tag, float arrivalTime)>();
            var approachingTags = new List<(TimelineTag tag, float arrivalTime)>();
            
            foreach (var t in activeTags)
            {
                if (t == null || t.Owner == null || !t.Owner.IsPlaying) continue;
                
                if (t.Mode == TimelineTagMode.Queued)
                {
                    float moveTime = t.GetUPerSec() > 0f ? t.GetU() / t.GetUPerSec() : 0f;
                    float arrivalTime = t.GetQueueTimer() + moveTime;
                    queuedTags.Add((t, arrivalTime));
                }
                else if (t.Mode == TimelineTagMode.Approaching)
                {
                    float arrivalTime = t.GetSecondsRemaining();
                    approachingTags.Add((t, arrivalTime));
                }
            }
            
            if (queuedTags.Count == 0) return;
            
            // Sort queued tags by arrival time (earliest first)
            queuedTags.Sort((a, b) => a.arrivalTime.CompareTo(b.arrivalTime));
            
            // Get the earliest arrival time among approaching tags (these can't be adjusted)
            float earliestApproachingArrival = float.MaxValue;
            foreach (var (_, arrival) in approachingTags)
            {
                if (arrival < earliestApproachingArrival)
                    earliestApproachingArrival = arrival;
            }
            
            // Process queued tags to ensure spacing
            float lastArrivalTime = 0f;
            
            // If there are approaching tags, use the earliest as our baseline
            if (approachingTags.Count > 0)
            {
                approachingTags.Sort((a, b) => a.arrivalTime.CompareTo(b.arrivalTime));
                lastArrivalTime = approachingTags[approachingTags.Count - 1].arrivalTime;
            }
            
            foreach (var (tag, originalArrival) in queuedTags)
            {
                float requiredArrival = lastArrivalTime + minimumReleaseGap;
                
                if (originalArrival < requiredArrival)
                {
                    // Need to delay this tag
                    float moveTime = tag.GetUPerSec() > 0f ? tag.GetU() / tag.GetUPerSec() : 0f;
                    float newQueueTime = requiredArrival - moveTime;
                    
                    if (newQueueTime > tag.GetQueueTimer())
                    {
                        tag.SetQueueTimer(newQueueTime);
                        if (debugLogs) Debug.Log($"[TimelineBar] Adjusted {tag.Owner?.name} queue timer to {newQueueTime:F2}s to prevent overlap");
                    }
                    lastArrivalTime = requiredArrival;
                }
                else
                {
                    lastArrivalTime = originalArrival;
                }
            }
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
            isProcessingTrigger = false;
        }

        /// <summary>
        /// Clears all existing tags and rebuilds for a new wave.
        /// Call this when transitioning between waves to ensure clean slate.
        /// </summary>
        public void RebuildForNewWave()
        {
            Clear();
            EnsureTagsForAllEnemies(true);
            PauseAll();
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

            if (!layoutReady) StartCoroutine(EnsureLayoutThenReposition()); else { UpdateAllEndpoints(); Recalculate(); EnforceQueueSpacing(); }
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

        public void OnHeroStartMove() { Recalculate(); EnforceQueueSpacing(); ResumeAll(); }
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
                // Reset the tag to spawn position
                tag.ResetToSpawn();
                
                // Coordinate the queue timer to prevent overlap with other tags
                int speed = enemy.Stats?.Speed.ToInt() ?? 10;
                float baseDelay = GetQueueDelayFromSpeed(speed);
                float coordinatedDelay = GetCoordinatedQueueDelay(baseDelay);
                tag.SetQueueTimer(coordinatedDelay);
                
                if (debugLogs)
                    Debug.Log($"[TimelineBar] {enemy.name} requeued with coordinated delay {coordinatedDelay:F2}s");
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
            g.SequenceManager.Add(new Scripts.Sequences.TimelineTriggerSequence(triggeringEnemy));
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

            // Coordinate the release delay to prevent overlap with existing tags
            float coordinatedDelay = GetCoordinatedQueueDelay(releaseDelay);

            var parent = tagsRoot != null ? tagsRoot : barRect;
            // Use factory instead of Instantiate(tagPrefab)
            var tagGO = TimelineTagFactory.Create(parent);
            var tag = tagGO.GetComponent<TimelineTag>();
            tag.name = $"TimelineTag_{enemy.name}";
            int dup = activeTags.Count(a => a != null && a.Owner == enemy);
            var tr = tag.GetComponent<RectTransform>();
            // Tag rect: left-edge pivot, anchored at center for symmetric X
            tr.anchorMin = tr.anchorMax = new Vector2(0.5f, 0.5f);
            tr.pivot = new Vector2(0f, 0.5f);
            tr.anchoredPosition = new Vector2(Mathf.Lerp(LeftX, RightX, startU), -dup * tagRowHeight);
            float uSpeed = UnitsPerSecFromSpeed(enemy.Stats.Speed.ToInt());
            tag.InitializeNormalized(enemy, LeftX, RightX, startU, uSpeed, OnTagReachedLeft, coordinatedDelay);
            activeTags.Add(tag);

            if (debugLogs && coordinatedDelay != releaseDelay)
                Debug.Log($"[TimelineBar] Spawned {enemy.name} with coordinated delay {coordinatedDelay:F2}s (base was {releaseDelay:F2}s)");
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
