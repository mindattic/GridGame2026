using Scripts.Factories;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;
using g = Scripts.Helpers.GameHelper;
using c = Scripts.Helpers.CanvasHelper;
using Scripts.Data.Actor;
using Scripts.Data.Config;
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
    /// Displays the turn order as a horizontal bar with actor tags that "load"
    /// from left to right. When a tag reaches the trigger point on the right,
    /// that actor takes their turn.
    ///
    /// VISUAL LAYOUT:
    /// ```
    /// [Spawn Point] →→→→ [Enemy Tags Loading Right] →→→→ [Trigger]
    ///      ↑                                                 ↑
    ///    LeftX                                             RightX
    /// (tags spawn here)                          (tag reaches here = take turn)
    /// ```
    ///
    /// MOVEMENT SYSTEM:
    /// - Tags move at speed based on actor's Speed stat
    /// - Faster actors = tags move faster = act sooner
    /// - When tag reaches RightX (trigger point), that actor's turn begins
    ///
    /// KEY PROPERTIES:
    /// - activeIcons: All TimelineIcon instances currently on the bar
    /// - advancing: True while timeline is actively moving tags
    /// - TimelineBarConfig.CrossingTimeSeconds: Base time for Speed 10 enemy to cross bar
    ///
    /// PUSHBACK SYSTEM:
    /// When enemies are hit inside the rightmost Zone strip, their tags are pushed left.
    /// - TimelineBarConfig.PushbackBase: Minimum pushback at far left (just spawned)
    /// - TimelineBarConfig.PushbackMax: Maximum pushback at trigger point (right edge)
    /// 
    /// INTEGRATION:
    /// - TurnManager calls OnEnemyTurnFinished() after enemy acts
    /// - PincerAttackManager triggers pushback via ApplyPushback()
    /// - StageManager calls AddEnemy() when spawning enemies
    /// 
    /// RELATED FILES:
    /// - TimelineIcon.cs: Individual actor tag on the timeline
    /// - TimelineIconFactory.cs: Creates TimelineIcon instances
    /// - TurnManager.cs: Turn flow control
    /// - TimelineTriggerSequence.cs: Handles tag trigger events
    /// 
    /// ACCESS: g.TimelineBar
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class TimelineBarInstance : MonoBehaviour
    {
        #region Runtime Scene References

        // All tuning values live in Scripts.Data.Config.TimelineBarConfig.
        // These transforms are acquired/created in Awake, not Inspector-assigned.
        private RectTransform barRect;
        private RectTransform iconsRoot;
        private RectTransform triggerPointRect;
        private RectTransform spawnPointRect;
        private RectTransform zoneRect;
        private Image zoneImage;

        #endregion

        #region Runtime State

        private readonly List<TimelineIcon> activeIcons = new List<TimelineIcon>();
        private bool advancing;

        /// <summary>True while timeline is actively moving tags.</summary>
        public bool IsAdvancing => advancing;

        private float cachedLeftX;
        private float cachedRightX;
        private bool layoutReady;
        private float halfWidth;

        /// <summary>Left edge X position (spawn point).</summary>
        private float LeftX => -halfWidth;

        /// <summary>Right edge X position (trigger point — loading complete).</summary>
        private float RightX => halfWidth;

        /// <summary>Total width of the timeline bar.</summary>
        private float Width => Mathf.Max(1f, RightX - LeftX);

        #endregion

        /// <summary>Initializes component references and state.</summary>
        private void Awake()
        {
            if (barRect == null) barRect = GetComponent<RectTransform>();
            if (barRect != null)
            {
                barRect.pivot = new Vector2(0.5f, 0.5f); // center pivot for symmetric coordinates
                barRect.anchorMin = barRect.anchorMax = new Vector2(0.5f, 0.5f);
            }

            // Strategic pushback Zone strip — drawn first so tags render on top of it.
            if (zoneRect == null && barRect != null)
            {
                var zgo = new GameObject("Zone", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
                zoneRect = zgo.GetComponent<RectTransform>();
                zoneRect.SetParent(barRect, false);
                zoneRect.SetAsFirstSibling();
                zoneImage = zgo.GetComponent<Image>();
                zoneImage.color = TimelineBarConfig.ZoneFillColor;
                zoneImage.raycastTarget = false;
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
            if (iconsRoot == null && barRect != null)
            {
                var go = new GameObject("Icons", typeof(RectTransform));
                iconsRoot = go.GetComponent<RectTransform>();
                iconsRoot.SetParent(barRect, false);
                iconsRoot.anchorMin = iconsRoot.anchorMax = new Vector2(0.5f, 0.5f); // center reference frame
                iconsRoot.pivot = new Vector2(0.5f, 0.5f);
            }
            cachedLeftX = float.NaN; cachedRightX = float.NaN;
        }

        /// <summary>Performs initial setup after all Awake calls complete.</summary>
        private void Start()
        {
            RebuildLayout();
            StartCoroutine(EnsureLayoutThenReposition());
            PauseAll();
        }

        /// <summary>Ensure layout then reposition.</summary>
        private System.Collections.IEnumerator EnsureLayoutThenReposition()
        {
            for (int i = 0; i < 2; i++) yield return null;
            if (barRect != null) LayoutRebuilder.ForceRebuildLayoutImmediate(barRect);
            layoutReady = true;
            UpdateAllEndpoints();
            Recalculate();
            if (TimelineBarConfig.DebugLogs) Debug.Log($"[TimelineBar] LayoutReady left={LeftX:F1} right={RightX:F1} width={Width:F1}");
        }

        /// <summary>Called when the RectTransform dimensions change.</summary>
        private void OnRectTransformDimensionsChange()
        {
            RebuildLayout();
            UpdateAllEndpoints();
            Recalculate();
        }

        /// <summary>Runs per-frame update logic.</summary>
        private void Update()
        {
            // Periodically enforce queue spacing to prevent overlap
            if (advancing && activeIcons.Count > 1)
            {
                EnforceQueueSpacing();
            }
        }

        /// <summary>Rebuild layout.</summary>
        private void RebuildLayout()
        {
            if (c.CanvasRect == null || barRect == null) return;
            float targetWidth = Mathf.Max(1f, c.CanvasRect.rect.width * TimelineBarConfig.CanvasPercent);
            // Preserve existing height
            Vector2 size = barRect.sizeDelta;
            size.x = targetWidth;
            barRect.sizeDelta = size;
            halfWidth = targetWidth * 0.5f;

            AnchorAboveBoard();

            // Position trigger/spawn points (trigger is on the RIGHT, spawn is on the LEFT)
            if (triggerPointRect != null)
            {
                triggerPointRect.anchorMin = triggerPointRect.anchorMax = new Vector2(0.5f, 0.5f);
                triggerPointRect.pivot = new Vector2(0.5f, 0.5f);
                triggerPointRect.anchoredPosition = new Vector2(RightX, 0f);
            }
            if (spawnPointRect != null)
            {
                spawnPointRect.anchorMin = spawnPointRect.anchorMax = new Vector2(0.5f, 0.5f);
                spawnPointRect.pivot = new Vector2(0.5f, 0.5f);
                spawnPointRect.anchoredPosition = new Vector2(LeftX, 0f);
            }

            // Pushback Zone strip: rightmost ZoneU * Width (right-anchored, right-edge pivot).
            if (zoneRect != null)
            {
                zoneRect.anchorMin = zoneRect.anchorMax = new Vector2(0.5f, 0.5f);
                zoneRect.pivot = new Vector2(1f, 0.5f);
                float zoneWidth = TimelineBarConfig.ZoneU * Width;
                zoneRect.sizeDelta = new Vector2(zoneWidth, barRect.sizeDelta.y);
                zoneRect.anchoredPosition = new Vector2(RightX, 0f);
            }
        }

        /// <summary>Positions the bar just above the board's top edge, with the mana pool stacked between.</summary>
        private void AnchorAboveBoard()
        {
            if (barRect == null || g.Board == null || c.CanvasRect == null) return;

            var boardTopWorld = new Vector3(0f, g.Board.bounds.Top, 0f);
            var boardTopCanvas = UnitConversionHelper.World.ToCanvas(c.CanvasRect, boardTopWorld);

            const float padBoardToMana = 8f;
            const float padManaToTimeline = 8f;
            float manaHeight = ManaPoolManager.UiHeight;
            float timelineHeight = barRect.sizeDelta.y;

            float manaTop = boardTopCanvas.y + padBoardToMana + manaHeight;
            float timelineY = manaTop + padManaToTimeline + timelineHeight * 0.5f;

            barRect.anchoredPosition = new Vector2(0f, timelineY);
        }

        /// <summary>Units per sec from speed.</summary>
        private float UnitsPerSecFromSpeed(int speed)
        {
            // Speed stat affects movement speed with gentler scaling for strategic play
            // Speed 10 = baseline (crosses in TimelineBarConfig.CrossingTimeSeconds = 8s)
            // Speed 5  = 0.75x speed (crosses in ~10.7s) 
            // Speed 15 = 1.25x speed (crosses in ~6.4s)
            // Speed 20 = 1.5x speed (crosses in ~5.3s)
            // Formula: 0.5 + (speed / 20) gives range of 0.75x to 1.5x
            float crossing = Mathf.Max(0.1f, TimelineBarConfig.CrossingTimeSeconds);
            float speedMultiplier = 0.5f + (speed / 20f);
            return Mathf.Max(0.01f, speedMultiplier / crossing);
        }

        /// <summary>Gets the queue delay from speed.</summary>
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
            if (activeIcons.Count == 0) return baseDelay;
            
            // Collect all tags that are queued or approaching, sorted by when they'll reach the trigger
            var releaseInfo = new List<(float releaseTime, float arrivalTime)>();
            
            foreach (var t in activeIcons)
            {
                if (t == null || t.Owner == null || !t.Owner.IsPlaying) continue;

                float queueTime = t.Mode == TimelineIconMode.Queued ? t.GetQueueTimer() : 0f;
                float moveTime = t.GetUPerSec() > 0f ? (1f - t.GetU()) / t.GetUPerSec() : 0f;

                // Add stun time if applicable
                if (t.Mode == TimelineIconMode.Stunned)
                {
                    queueTime = t.GetSecondsRemaining() - moveTime;
                }

                float arrivalTime = queueTime + moveTime;
                releaseInfo.Add((queueTime, arrivalTime));
            }

            if (releaseInfo.Count == 0) return baseDelay;

            // New tag starts at u=0.0, so we need to check when it would arrive
            // and ensure it doesn't release within TimelineBarConfig.MinimumReleaseGap of others
            float myMoveTime = TimelineBarConfig.CrossingTimeSeconds; // Time to cross full bar
            float myReleaseTime = baseDelay;
            float myArrivalTime = myReleaseTime + myMoveTime;
            
            // Sort existing tags by arrival time
            releaseInfo.Sort((a, b) => a.arrivalTime.CompareTo(b.arrivalTime));
            
            // Check each existing tag and ensure our release doesn't conflict
            foreach (var (releaseTime, arrivalTime) in releaseInfo)
            {
                // If our arrival would be within gap of theirs, delay our release
                if (Mathf.Abs(myArrivalTime - arrivalTime) < TimelineBarConfig.MinimumReleaseGap)
                {
                    // Push our arrival to be TimelineBarConfig.MinimumReleaseGap after theirs
                    myArrivalTime = arrivalTime + TimelineBarConfig.MinimumReleaseGap;
                    myReleaseTime = myArrivalTime - myMoveTime;
                }
            }
            
            return Mathf.Max(baseDelay, myReleaseTime);
        }

        /// <summary>
        /// Adjusts queue timers for all queued tags to prevent releases within TimelineBarConfig.MinimumReleaseGap.
        /// Called periodically and after state changes.
        /// </summary>
        private void EnforceQueueSpacing()
        {
            // Get all queued tags with their projected arrival times
            var queuedTags = new List<(TimelineIcon tag, float arrivalTime)>();
            var approachingTags = new List<(TimelineIcon tag, float arrivalTime)>();
            
            foreach (var t in activeIcons)
            {
                if (t == null || t.Owner == null || !t.Owner.IsPlaying) continue;
                
                if (t.Mode == TimelineIconMode.Queued)
                {
                    float moveTime = t.GetUPerSec() > 0f ? (1f - t.GetU()) / t.GetUPerSec() : 0f;
                    float arrivalTime = t.GetQueueTimer() + moveTime;
                    queuedTags.Add((t, arrivalTime));
                }
                else if (t.Mode == TimelineIconMode.Approaching)
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
                float requiredArrival = lastArrivalTime + TimelineBarConfig.MinimumReleaseGap;
                
                if (originalArrival < requiredArrival)
                {
                    // Need to delay this tag
                    float moveTime = tag.GetUPerSec() > 0f ? (1f - tag.GetU()) / tag.GetUPerSec() : 0f;
                    float newQueueTime = requiredArrival - moveTime;
                    
                    if (newQueueTime > tag.GetQueueTimer())
                    {
                        tag.SetQueueTimer(newQueueTime);
                        if (TimelineBarConfig.DebugLogs) Debug.Log($"[TimelineBar] Adjusted {tag.Owner?.name} queue timer to {newQueueTime:F2}s to prevent overlap");
                    }
                    lastArrivalTime = requiredArrival;
                }
                else
                {
                    lastArrivalTime = originalArrival;
                }
            }
        }

        /// <summary>Gets the initial position from speed.</summary>
        private float GetInitialPositionFromSpeed(int speed, int maxSpeed, int minSpeed)
        {
            // Scatter enemies across timeline based on speed
            // Fastest enemies start closer to the trigger (higher u), slowest start at spawn (lower u)
            if (maxSpeed <= minSpeed) return 0.5f;
            float t = (float)(maxSpeed - speed) / (maxSpeed - minSpeed);
            // t=0 for fastest (start at u=0.8, near trigger), t=1 for slowest (start at u=0.1, near spawn)
            return Mathf.Lerp(0.8f, 0.1f, t);
        }

        /// <summary>Sorted enemies by speed desc.</summary>
        private System.Collections.Generic.IEnumerable<ActorInstance> SortedEnemiesBySpeedDesc()
        {
            return g.Actors.Enemies.Where(e => e != null && e.IsPlaying).OrderByDescending(e => e.Stats.Speed.ToInt());
        }

        /// <summary> clear..Groups[0].Value.ToUpper() lear.</summary>
        public void Clear()
        {
            for (int i = activeIcons.Count - 1; i >= 0; i--) if (activeIcons[i] != null) Destroy(activeIcons[i].gameObject);
            activeIcons.Clear();
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
        /// If there are no tags yet, scatter across timeline by speed (fastest near trigger on the right).
        /// Otherwise, only add missing ones at the far left (fresh spawn).
        /// Also prunes tags whose owners are gone/inactive.
        /// </summary>
        public void EnsureTagsForAllEnemies(bool redistributeIfNone = true)
        {
            // Remove stale tags (dead or despawned)
            for (int i = activeIcons.Count - 1; i >= 0; i--)
            {
                var t = activeIcons[i];
                if (t == null || t.Owner == null || !t.Owner.IsPlaying)
                {
                    if (t != null) t.FadeAndDestroy(0.15f);
                    activeIcons.RemoveAt(i);
                }
            }

            var playing = g.Actors.Enemies.Where(e => e != null && e.IsPlaying).ToList();
            if (playing.Count == 0)
            {
                return;
            }

            // Add missing tags
            var missing = playing.Where(e => !activeIcons.Any(t => t != null && t.Owner == e)).ToList();

            if (activeIcons.Count == 0 && redistributeIfNone)
            {
                // Scatter enemies across timeline based on speed (fastest near trigger on the right)
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
                // New enemies spawn at far left (u=0, fresh / not loaded) with speed-based queue delay
                foreach (var enemy in missing)
                {
                    int spd = enemy.Stats.Speed.ToInt();
                    float delay = GetQueueDelayFromSpeed(spd);
                    SpawnTag(enemy, 0f, delay);
                }
            }

            if (!layoutReady) StartCoroutine(EnsureLayoutThenReposition()); else { UpdateAllEndpoints(); Recalculate(); EnforceQueueSpacing(); }
            PauseAll(); // Start paused until hero moves
        }

        /// <summary>Creates the initial for all enemies.</summary>
        public void SpawnInitialForAllEnemies()
        {
            EnsureTagsForAllEnemies(true);
        }

        /// <summary>Pause all.</summary>
        private void PauseAll()
        { foreach (var t in activeIcons) t?.Pause(); advancing = false; }
        /// <summary>Resume all.</summary>
        private void ResumeAll()
        { foreach (var t in activeIcons) t?.Resume(); advancing = true; }

        /// <summary>Handles the hero start move event.</summary>
        public void OnHeroStartMove() { Recalculate(); EnforceQueueSpacing(); ResumeAll(); }
        /// <summary>Handles the hero stop move event.</summary>
        public void OnHeroStopMove() { PauseAll(); }
        /// <summary>Handles the enemy turn started event.</summary>
        public void OnEnemyTurnStarted(ActorInstance enemy) {
            PauseAll();
            // Lock any tags that are already at/past the trigger (right) to the exact right position
            UpdateAllEndpoints();
            float right = RightX;
            foreach (var t in activeIcons)
            {
                if (t == null || t.Rect == null) continue;
                if (t.Rect.anchoredPosition.x >= right - 0.25f)
                {
                    t.SetU(1f); // snaps exactly to RightX via ApplyPosition clamp
                    t.Pause();
                }
            }
        }
        /// <summary>Handles the enemy turn finished event.</summary>
        public void OnEnemyTurnFinished(ActorInstance enemy)
        {
            var tag = activeIcons.FirstOrDefault(t => t != null && t.Owner == enemy);
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
                
                if (TimelineBarConfig.DebugLogs)
                    Debug.Log($"[TimelineBar] {enemy.name} requeued with coordinated delay {coordinatedDelay:F2}s");
            }
        }


        /// <summary>
        /// Pushes the enemy's timeline tag back (toward spawn / left) when attacked.
        /// The closer the enemy is to the right (trigger point), the stronger the pushback.
        /// Pushback scales with attacker's Strength, and stun recovery depends on enemy's Agility.
        /// </summary>
        public void PushbackOnAttack(ActorInstance enemy, int attackerStrength = 10)
        {
            if (enemy == null) return;
            var tag = activeIcons.FirstOrDefault(t => t != null && t.Owner == enemy);
            if (tag == null) return;

            // Zone gate: only enemies whose tag is inside the rightmost Zone strip are
            // vulnerable to pushback. Damage is applied elsewhere; this method only
            // controls the timeline-delay reaction.
            float zoneStartU = 1f - TimelineBarConfig.ZoneU;
            if (tag.GetEffectiveTargetU() < zoneStartU)
            {
                if (TimelineBarConfig.DebugLogs)
                    Debug.Log($"[TimelineBar] {enemy.name} outside Zone (u={tag.GetU():F2} < {zoneStartU:F2}) — no pushback");
                return;
            }

            // Strength of 10 = 1.0x multiplier (baseline)
            float strengthMultiplier = attackerStrength / 10f;

            // Get enemy's agility for stun recovery calculation
            int enemyAgility = enemy.Stats != null
                ? enemy.Stats.Agility.ToInt()
                : 10;

            tag.Pushback(TimelineBarConfig.PushbackBase, TimelineBarConfig.PushbackMax, strengthMultiplier, enemyAgility, TimelineBarConfig.BaseStunDuration);
            if (TimelineBarConfig.DebugLogs) Debug.Log($"[TimelineBar] Pushed {enemy.name} tag (str={attackerStrength}, agi={enemyAgility}, mode={tag.Mode})");

            ResolveSpatialOverlap();
        }

        /// <summary>
        /// Re-spaces visible tags so none overlap on the bar. When a new icon spawns near an
        /// existing icon's slot, the nearest overlapping icon to its left is pushed further left
        /// (time added); if that push causes another overlap further left, it cascades like a train
        /// until no more overlaps remain. Tags within MinSpatialGap of each other form a "cluster";
        /// the cluster's rightmost tag keeps its position and each neighbor to the left is spaced
        /// MinSpatialGap intervals further left — **order-preserving**, no speed-based reshuffle.
        /// Tags that don't collide are left untouched.
        /// </summary>
        private void ResolveSpatialOverlap()
        {
            // Visible = anything currently rendered on the bar (queued tags are invisible until release)
            var visible = new List<TimelineIcon>();
            foreach (var t in activeIcons)
            {
                if (t == null || t.Owner == null || !t.Owner.IsPlaying) continue;
                if (t.Mode == TimelineIconMode.Queued) continue;
                visible.Add(t);
            }
            if (visible.Count <= 1) return;

            // Sort by current effective u ascending so we can scan left-to-right for clusters.
            visible.Sort((a, b) => a.GetEffectiveTargetU().CompareTo(b.GetEffectiveTargetU()));

            float gap = TimelineBarConfig.MinSpatialGap;

            // Single right-to-left pass: starting from the rightmost tag, ensure each tag to
            // its left sits at least `gap` below it; if not, push it left by the shortfall,
            // which may then shove the next one further left (the "train" cascade).
            for (int k = visible.Count - 2; k >= 0; k--)
            {
                float rightU = visible[k + 1].GetEffectiveTargetU();
                float leftU = visible[k].GetEffectiveTargetU();
                float minAllowed = rightU - gap;
                if (leftU > minAllowed)
                {
                    visible[k].SetTargetU(Mathf.Max(0f, minAllowed));
                }
            }
        }

        /// <summary>Returns the tag whose Owner is the given actor, or null if absent.</summary>
        public TimelineIcon GetIconFor(ActorInstance actor)
        {
            if (actor == null) return null;
            return activeIcons.FirstOrDefault(t => t != null && t.Owner == actor);
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

        /// <summary>Handles the tag reaching the trigger (right edge) event.</summary>
        private void OnIconReachedTrigger(TimelineIcon tag)
        {
            if (tag == null) return;

            // Prevent processing if already processing a trigger OR if enemy turn in progress
            if (isProcessingTrigger) return;
            if (g.TurnManager != null && g.TurnManager.IsEnemyTurn) return;

            var triggeringEnemy = tag.Owner;
            if (triggeringEnemy == null || !triggeringEnemy.IsPlaying) return;

            // SET FLAG IMMEDIATELY - don't reset until hero turn starts
            isProcessingTrigger = true;

            // Lock the arriving tag exactly at the trigger (right) and pause all
            tag.SetU(1f);
            PauseAll();

            // Disable input during transition
            g.InputManager.InputMode = InputMode.None;

            // Queue the timeline trigger sequence
            g.SequenceManager.Add(new Scripts.Sequences.TimelineTriggerSequence(triggeringEnemy));
            g.SequenceManager.Execute();
        }

        /// <summary>Gets the seconds until the next enemy reaches the trigger (right edge).</summary>
        public float GetSecondsUntilNextEnemyReachesTrigger()
        {
            if (activeIcons.Count == 0) 
                return 0f;

            float min = float.PositiveInfinity;
            foreach (var t in activeIcons)
            {
                if (t == null || t.Owner == null || !t.Owner.IsPlaying) continue;
                float sec = t.GetSecondsRemaining();
                if (sec < min) min = sec;
            }
            return float.IsInfinity(min) ? 0f : Mathf.Max(0f, min);
        }

        // NEW: Advance all tags by a number of seconds instantly (banking mechanic)
        // Returns true if at least one tag reached the trigger point.
        /// <summary>Advance by seconds.</summary>
        public bool AdvanceBySeconds(float seconds)
        {
            seconds = Mathf.Max(0f, seconds);
            if (seconds <= 0f || activeIcons.Count == 0) return false;
            bool anyReached = false;
            foreach (var t in activeIcons)
            {
                if (t == null) continue;
                float u = t.GetU();
                float uPerSec = Mathf.Max(0.0001f, t.GetUPerSec());
                float newU = Mathf.Min(1f, u + uPerSec * seconds);
                t.SetU(newU);
                // If moved to (or past) right edge, TimelineIcon will invoke its callback next Update frame.
                if (Mathf.Approximately(newU, 1f)) anyReached = true;
            }
            return anyReached;
        }

        /// <summary>
        /// Bank directly to next arriving tag. Returns the seconds that would be skipped
        /// and the enemy that would trigger. Does NOT start the sequence - caller does that.
        /// </summary>
        public (ActorInstance enemy, float secondsUsed) GetNextBankTarget()
        {
            if (activeIcons.Count == 0)
                return (null, 0f);

            // Find earliest tag by seconds remaining (next enemy to arrive)
            TimelineIcon earliest = null;
            float minSec = float.PositiveInfinity;
            foreach (var t in activeIcons)
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
            
            // Lock the arriving tag at the trigger point (right edge, u=1)
            var tag = activeIcons.FirstOrDefault(t => t != null && t.Owner == enemy);
            if (tag != null)
            {
                tag.SetU(1f);
                tag.Pause();
            }
            
            PauseAll();
        }

        /// <summary>Creates the tag.</summary>
        private void SpawnTag(ActorInstance enemy, float startU, float releaseDelay = 0f)
        {
            if (enemy == null || !enemy.IsEnemy) return;

            // Coordinate the release delay to prevent overlap with existing tags
            float coordinatedDelay = GetCoordinatedQueueDelay(releaseDelay);

            var parent = iconsRoot != null ? iconsRoot : barRect;
            var tagGO = TimelineIconFactory.Create(parent);
            var tag = tagGO.GetComponent<TimelineIcon>();
            tag.name = $"TimelineIcon_{enemy.name}";
            int dup = activeIcons.Count(a => a != null && a.Owner == enemy);
            var tr = tag.GetComponent<RectTransform>();
            // Tag rect: right-edge pivot (leading edge moving toward trigger), anchored at center for symmetric X
            tr.anchorMin = tr.anchorMax = new Vector2(0.5f, 0.5f);
            tr.pivot = new Vector2(1f, 0.5f);
            tr.anchoredPosition = new Vector2(Mathf.Lerp(LeftX, RightX, startU), -dup * TimelineBarConfig.TagRowHeight);
            float uSpeed = UnitsPerSecFromSpeed(enemy.Stats.Speed.ToInt());
            tag.InitializeNormalized(enemy, LeftX, RightX, startU, uSpeed, OnIconReachedTrigger, coordinatedDelay);
            activeIcons.Add(tag);

            if (TimelineBarConfig.DebugLogs && coordinatedDelay != releaseDelay)
                Debug.Log($"[TimelineBar] Spawned {enemy.name} with coordinated delay {coordinatedDelay:F2}s (base was {releaseDelay:F2}s)");

            // Re-space against existing tags so the new spawn doesn't land on top of one.
            ResolveSpatialOverlap();
        }

        /// <summary>Updates the all endpoints.</summary>
        private void UpdateAllEndpoints()
        {
            float left = LeftX; float right = RightX;
            foreach (var t in activeIcons) t?.UpdateEndpoints(left, right);
        }

        /// <summary>Recalculate.</summary>
        private void Recalculate()
        {
            float left = LeftX; float right = RightX;
            if (float.IsNaN(cachedLeftX) || float.IsNaN(cachedRightX) || !Mathf.Approximately(left, cachedLeftX) || !Mathf.Approximately(right, cachedRightX))
            {
                cachedLeftX = left; cachedRightX = right;
                foreach (var t in activeIcons)
                {
                    if (t == null || t.Rect == null) continue;
                    t.UpdateEndpoints(left, right);
                    var p = t.Rect.anchoredPosition;
                    // Only auto-loop tags that slipped past the trigger (right edge) during HERO turns.
                    // During enemy turns, keep the tag at RightX until OnEnemyTurnFinished resets it.
                    bool isHeroTurn = g.TurnManager == null || g.TurnManager.IsHeroTurn;
                    if (isHeroTurn && p.x >= right) t.SetU(0f);
                }
            }
        }
    }
}
