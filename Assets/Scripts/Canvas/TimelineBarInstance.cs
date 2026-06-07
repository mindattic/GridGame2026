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
        private RectTransform zoneEdgeRect;
        private Image zoneEdgeImage;
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

            // Solid bright edge line at the Zone's left boundary — visually announces
            // the "if you can land a pincer that lands an enemy past this line, they
            // get pushed back" threshold.
            if (zoneEdgeRect == null && barRect != null)
            {
                var ego = new GameObject("ZoneEdge", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
                zoneEdgeRect = ego.GetComponent<RectTransform>();
                zoneEdgeRect.SetParent(barRect, false);
                zoneEdgeRect.SetAsFirstSibling();
                zoneEdgeImage = ego.GetComponent<Image>();
                zoneEdgeImage.color = TimelineBarConfig.ZoneEdgeColor;
                zoneEdgeImage.raycastTarget = false;
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
            StartCoroutine(EnsureLayoutThenReposition());
            PauseAll();
        }

        /// <summary>Ensure layout then reposition.</summary>
        private System.Collections.IEnumerator EnsureLayoutThenReposition()
        {
            // Wait for the board to publish its bounds (BoardInstance.AssignBounds runs
            // in its Start). Cap at ~2s so we never hang the scene if Board is missing.
            float waited = 0f;
            while ((g.Board == null || g.Board.bounds == null) && waited < 2f)
            {
                waited += Time.unscaledDeltaTime;
                yield return null;
            }

            // Standard 2-frame settle for canvas layout.
            for (int i = 0; i < 2; i++) yield return null;

            if (barRect != null) LayoutRebuilder.ForceRebuildLayoutImmediate(barRect);
            RebuildLayout();
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

            // Fixed Y from GameBuilder.cs — keep the bar at the builder-authored position.

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
                zoneRect.sizeDelta = new Vector2(zoneWidth, TimelineBarConfig.ZoneHeight);
                zoneRect.anchoredPosition = new Vector2(RightX, 0f);
            }

            // Bright thin line at the Zone's left boundary (u = 1 - ZoneU).
            if (zoneEdgeRect != null)
            {
                zoneEdgeRect.anchorMin = zoneEdgeRect.anchorMax = new Vector2(0.5f, 0.5f);
                zoneEdgeRect.pivot = new Vector2(0.5f, 0.5f);
                zoneEdgeRect.sizeDelta = new Vector2(TimelineBarConfig.ZoneEdgeWidth, TimelineBarConfig.ZoneEdgeHeight);
                float edgeX = Mathf.Lerp(LeftX, RightX, 1f - TimelineBarConfig.ZoneU);
                zoneEdgeRect.anchoredPosition = new Vector2(edgeX, 0f);
            }
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
        /// Speed-derived wait scaled by a per-spawn random multiplier so enemies of the
        /// same Speed don't release in lockstep. Multiplier range [0.7, 1.4] keeps the
        /// stagger feeling natural without ever zeroing out the wait.
        /// </summary>
        private float GetRandomizedQueueDelay(int speed)
        {
            float baseDelay = GetQueueDelayFromSpeed(speed);
            float multiplier = UnityEngine.Random.Range(0.7f, 1.4f);
            return baseDelay * multiplier;
        }

        /// <summary>
        /// Initial battle-start position: u = (normalizedSpeed × 0.6) × random(0.5, 1.0),
        /// clamped to never spawn inside the Zone. Faster enemies spawn closer to the
        /// trigger on average, but the random multiplier prevents same-speed lockstep
        /// and lets a slower enemy sometimes get a head start. Once moving, the natural
        /// speed gap takes over and faster enemies overtake again after a respawn.
        /// </summary>
        private float GetInitialPositionFromSpeed(int speed, int maxSpeed, int minSpeed)
        {
            float speedT = (maxSpeed > minSpeed)
                ? (float)(speed - minSpeed) / (maxSpeed - minSpeed)
                : 1f;
            float multiplier = UnityEngine.Random.Range(0.5f, 1.0f);
            float startU = speedT * 0.6f * multiplier;
            // Cap below the Zone start so no one begins in the danger strip.
            float maxStartU = (1f - TimelineBarConfig.ZoneU) - 0.05f;
            return Mathf.Clamp(startU, 0f, maxStartU);
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
                    float delay = GetRandomizedQueueDelay(spd);
                    SpawnTag(enemy, 0f, delay);
                }
            }

            if (!layoutReady) StartCoroutine(EnsureLayoutThenReposition()); else { UpdateAllEndpoints(); Recalculate(); }
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
        public void OnHeroStartMove() { Recalculate(); ResumeAll(); }
        /// <summary>Handles the hero stop move event.</summary>
        public void OnHeroStopMove() { PauseAll(); }

        /// <summary>
        /// Freezes the timeline while a cast is resolving — called by TurnManager.BeginCastResolution.
        /// Halts all icon advancement (other enemies stop marching), which in turn stops
        /// ManaPoolManager accrual (gated on IsAdvancing) and prevents any new enemy triggers
        /// from queuing mid-heal. Unfreeze is implicit: EndCastResolution flows into
        /// EndTurnSequence → NextTurn → BeginHeroWindow/BeginEnemyTurn, each of which sets
        /// the bar's pause state to match the new window.
        /// </summary>
        public void PauseForCastResolution() { PauseAll(); }
        /// <summary>Handles the enemy turn started event.</summary>
        public void OnEnemyTurnStarted(ActorInstance enemy) {
            PauseAll();
            // Lock any tags that are already at/past the trigger (right) to the exact right position
            UpdateAllEndpoints();
            float right = RightX;
            foreach (var t in activeIcons)
            {
                if (t == null || t.Rect == null) continue;
                // Skip in-flight spell icons — snapping a hero's cast icon to u=1 would make it
                // resolve early when the bar resumes. Only enemy march icons get locked here.
                if (t.IsSpellIcon) continue;
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
            if (tag == null) return;

            UpdateAllEndpoints();
            tag.ResetToSpawn();

            int speed = enemy.Stats?.Speed.ToInt() ?? 10;
            tag.SetQueueTimer(GetRandomizedQueueDelay(speed));
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
        }

        /// <summary>US-028: slide an actor's timeline icon FORWARD (toward the trigger) by
        /// <paramref name="amountU"/> u — the inverse of <see cref="PushbackOnAttack"/>. Higher u =
        /// sooner turn; since turn order is arrival-at-trigger order, the hastened icon may overtake
        /// icons that were ahead of it. No-op if the actor has no icon. Driven by Quicken (US-028).</summary>
        public void HastenIcon(ActorInstance actor, float amountU)
        {
            if (actor == null || amountU <= 0f) return;
            var icon = GetIconFor(actor);
            if (icon == null) return;
            icon.Hasten(amountU);
            if (TimelineBarConfig.DebugLogs)
                Debug.Log($"[TimelineBar] Hastened {actor.name} by {amountU:F2}u → u={icon.GetU():F2}");
        }

        /// <summary>Returns the tag whose Owner is the given actor, or null if absent.</summary>
        public TimelineIcon GetIconFor(ActorInstance actor)
        {
            if (actor == null) return null;
            return activeIcons.FirstOrDefault(t => t != null && t.Owner == actor);
        }

        /// <summary>Returns the in-flight spell-cast icon owned by <paramref name="actor"/>
        /// (a caster has both a turn icon and, while casting, a spell icon — this picks the
        /// spell one), or null if the actor isn't currently casting. Used by US-025's Clutch demo.</summary>
        public TimelineIcon GetSpellIconFor(ActorInstance actor)
        {
            if (actor == null) return null;
            return activeIcons.FirstOrDefault(t => t != null && t.IsSpellIcon && t.Owner == actor
                && t.ActiveCast != null && !t.ActiveCast.IsInterrupted && !t.ActiveCast.IsComplete);
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
            // Spell icons own their own onReached closure (set in SpawnSpellIcon) —
            // never queue an enemy turn from one.
            if (tag.IsSpellIcon) return;

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
            float zoneStartU = 1f - TimelineBarConfig.ZoneU;
            float zonePace = Mathf.Max(0.0001f, TimelineBarConfig.ZonePaceUPerSec);
            bool anyReached = false;
            foreach (var t in activeIcons)
            {
                if (t == null) continue;
                float u = t.GetU();
                // US-011: GetEffectiveUPerSec folds in the Slowed debuff (×0.5), so a Slowed
                // enemy's icon crawls the racing leg and its turn is visibly delayed.
                float uPerSec = Mathf.Max(0.0001f, t.GetEffectiveUPerSec());
                float remaining = seconds;
                float newU = u;
                if (newU < zoneStartU)
                {
                    float distToZone = zoneStartU - newU;
                    float timeToZone = distToZone / uPerSec;
                    if (remaining <= timeToZone)
                    {
                        newU += uPerSec * remaining;
                        remaining = 0f;
                    }
                    else
                    {
                        newU = zoneStartU;
                        remaining -= timeToZone;
                    }
                }
                if (remaining > 0f)
                {
                    newU = Mathf.Min(1f, newU + zonePace * remaining);
                }
                t.SetU(newU);
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

            var parent = iconsRoot != null ? iconsRoot : barRect;
            var tagGO = TimelineIconFactory.Create(parent);
            var tag = tagGO.GetComponent<TimelineIcon>();
            tag.name = $"TimelineIcon_{enemy.name}";
            int dup = activeIcons.Count(a => a != null && a.Owner == enemy);
            var tr = tag.GetComponent<RectTransform>();
            // Tag rect: centered pivot so the box straddles the bar line, anchored at center for symmetric X
            tr.anchorMin = tr.anchorMax = new Vector2(0.5f, 0.5f);
            tr.pivot = new Vector2(0.5f, 0.5f);
            tr.anchoredPosition = new Vector2(Mathf.Lerp(LeftX, RightX, startU), -dup * TimelineBarConfig.TagRowHeight);
            float uSpeed = UnitsPerSecFromSpeed(enemy.Stats.Speed.ToInt());
            tag.InitializeNormalized(enemy, LeftX, RightX, startU, uSpeed, OnIconReachedTrigger, releaseDelay);
            activeIcons.Add(tag);
        }

        /// <summary>
        /// Spawns a spell-cast icon on the timeline. The icon spawns "N seconds out from
        /// the right" (where N = state.TotalCastTime) using the bar's canonical pace
        /// (UnitsPerSecFromSpeed(10) = 1/CrossingTime). onComplete fires when the icon
        /// reaches u=1; the icon parks in Resolving mode and is destroyed by the caller's
        /// cleanup. onInterrupted fires if state.IsInterrupted flips before completion.
        /// Returns the spawned TimelineIcon so the caller can fade/destroy it after
        /// the resolution sequences finish.
        /// </summary>
        public TimelineIcon SpawnSpellIcon(CastingState state, System.Action<TimelineIcon> onComplete, System.Action onInterrupted = null)
        {
            if (state == null || state.Caster == null || state.TotalCastTime <= 0f) return null;

            // Canonical bar pace: an enemy of Speed 10 traverses the whole bar in
            // CrossingTimeSeconds. Spell icons use this same pace so "3s remaining"
            // visually matches "3s remaining" on any other actor's icon.
            float uSpeed = UnitsPerSecFromSpeed(10);
            float startU = Mathf.Clamp01(1f - state.TotalCastTime * uSpeed);

            var parent = iconsRoot != null ? iconsRoot : barRect;
            var iconGO = TimelineIconFactory.CreateForCast(parent, state, LeftX, RightX, startU, activeIcons.Count);
            var icon = iconGO.GetComponent<TimelineIcon>();

            System.Action<TimelineIcon> onReached = (TimelineIcon self) =>
            {
                // Park icon at u=1 in Resolving mode and suspend input — the cast is
                // about to play out as a third turn state (neither hero nor enemy).
                // The icon stays visible during resolution; the caller's cleanup
                // callback fades it once the resolution sequences finish.
                if (self.ActiveCast != null) self.ActiveCast.ElapsedTime = self.ActiveCast.TotalCastTime;
                self.EnterResolvingMode();
                self.Pause();
                g.TurnManager?.BeginCastResolution();
                activeIcons.Remove(self);
                onComplete?.Invoke(self);
            };

            icon.InitializeForSpell(state, LeftX, RightX, uSpeed, onReached, onInterrupted: () =>
            {
                activeIcons.Remove(icon);
                // Defensive: if interrupt fires after the icon already entered Resolving
                // (race), clear the flag so input isn't left suspended.
                g.TurnManager?.EndCastResolution();
                onInterrupted?.Invoke();
            });

            // Spawn at startU rather than 0 so the visual position reads as
            // "TotalCastTime seconds remaining" at the bar's canonical pace.
            icon.SetU(startU);

            activeIcons.Add(icon);

            // Match the bar's current pause state — casts only advance when the timeline does.
            if (advancing) icon.Resume(); else icon.Pause();

            if (TimelineBarConfig.DebugLogs)
                Debug.Log($"[TimelineBar] Spawned spell icon for {state.Caster.name} casting {state.Ability?.name} ({state.TotalCastTime:F1}s, startU={startU:F2}, uPerSec={uSpeed:F3})");

            return icon;
        }

        // ── Parallel cast lane (US-#3) ───────────────────────────────────────
        // A spell cast renders as an ICON that loads left→right on a lane just BELOW the enemy-icon
        // rows, in PARALLEL with them — distinct from SpawnSpellIcon (which rides the main rows and
        // suspends input on resolve). Geometry only: SpellCastBar owns timing and travels the icon
        // via anchoredPosition.x = Lerp(leftX, rightX, t); the lane Y keeps it clear of the rows.
        private const float CastLaneIconSize = 28f;
        private const float CastLaneTopY = -44f;   // first lane, below the enemy rows
        private const float CastLaneStrideY = 32f; // each concurrent cast stacks one lane lower

        /// <summary>Create a cast-lane icon below the enemy rows and return its RectTransform plus the
        /// bar's u=0 (left) / u=1 (right/trigger) X bounds. <paramref name="laneIndex"/> stacks
        /// concurrent casts downward. Caller owns lifetime + travel; no turn-state side effects.</summary>
        public RectTransform CreateCastLaneIcon(Sprite sprite, Color tint, int laneIndex, out float leftX, out float rightX)
        {
            leftX = LeftX;
            rightX = RightX;
            var parent = iconsRoot != null ? iconsRoot : barRect;
            var go = new GameObject("CastLaneIcon", typeof(RectTransform), typeof(UnityEngine.UI.Image));
            go.layer = LayerMask.NameToLayer("UI");
            var rt = (RectTransform)go.transform;
            rt.SetParent(parent, false);
            rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0.5f);
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.sizeDelta = new Vector2(CastLaneIconSize, CastLaneIconSize);
            rt.anchoredPosition = new Vector2(LeftX, CastLaneTopY - Mathf.Max(0, laneIndex) * CastLaneStrideY);
            var img = go.GetComponent<UnityEngine.UI.Image>();
            img.sprite = sprite;
            img.color = tint;
            img.preserveAspect = true;
            img.raycastTarget = false;
            return rt;
        }

        /// <summary>
        /// Interrupts any in-flight spell casts owned by <paramref name="caster"/> (hero OR enemy)
        /// when it takes a landing hit, via the US-024 cast-stagger model. Each hit adds WIS/STR-scaled
        /// cast-time delay; accumulated delay past the original cast time cancels the cast. WIS poise
        /// may shrug a hit. <b>Clutch</b> (the rare LCK miracle save, US-025) is HERO-ONLY — it is the
        /// player's dramatic save and would be perverse on an enemy (instant-resolve its charge), so
        /// enemy casters never roll it. <b>US-027:</b> a cancelled ENEMY charge mints one orb of the
        /// charge's color to the team bank (how off-palette colors flow in). Returns the number of
        /// casts affected (0 if the actor wasn't casting).
        /// </summary>
        public int InterruptCastsByOwner(ActorInstance caster, ActorInstance attacker = null)
        {
            if (caster == null) return 0;
            bool isHero = caster.IsHero;
            int count = 0;
            // Snapshot the list — Interrupt may indirectly mutate activeIcons.
            var snapshot = activeIcons.Where(i => i != null && i.IsSpellIcon && i.Owner == caster && i.ActiveCast != null && !i.ActiveCast.IsInterrupted && !i.ActiveCast.IsComplete).ToList();
            foreach (var icon in snapshot)
            {
                // US-024 (stagger model): each landed hit pushes the cast back (adds cast-time);
                // accumulated delay past the original cast time cancels it. WIS resists both.
                var cast = icon.ActiveCast;
                var result = Scripts.Services.CastInterruptResolver.Resolve(caster, attacker, cast, allowClutch: isHero);
                var combatText = Scripts.Helpers.GameHelper.CombatTextManager;
                switch (result.Outcome)
                {
                    case Scripts.Services.CastInterruptOutcome.Clutch:
                        // US-025 — rare LCK miracle save (hero-only): the cast shrugs the hit AND snaps
                        // to the trigger to resolve on the spot. Pause the icon now so it can't reach u=1
                        // on its own (resolving without the juice) before the queued ClutchSequence runs;
                        // AddFirst makes the save fire right after the triggering attack. The sequence
                        // plays the flash/SFX/"Clutch!" text, then ForceResolve() drives resolution.
                        icon.Pause();
                        Scripts.Helpers.GameHelper.SequenceManager?.AddFirst(
                            new Scripts.Sequences.ClutchSequence(icon, caster));
                        break;

                    case Scripts.Services.CastInterruptOutcome.Resisted:
                        combatText?.Spawn("Resisted!", caster.Position, isHero ? "Heal" : "Miss");
                        break;

                    case Scripts.Services.CastInterruptOutcome.Cancelled:
                        cast.AccumulatedInterruptDelay += result.DelayAdded;
                        cast.Interrupt(); // total stagger exceeded the original cast time
                        // US-027: cancelling an enemy charge mints one charge-color orb to the bank.
                        if (!isHero) MintInterruptOrb(caster);
                        break;

                    default: // Delayed — cast survives, pushed back on the timeline.
                        cast.AccumulatedInterruptDelay += result.DelayAdded;
                        icon.DelayCast(result.DelayAdded);
                        combatText?.Spawn("Stagger!", caster.Position, "Miss");
                        break;
                }
                count++;
            }
            return count;
        }

        /// <summary>US-027: drop a bouncing orb of the enemy's charge color (it lands in the team
        /// bank). Reuses the pincer mint path (<see cref="Scripts.Factories.ManaOrbFactory.Drop"/>);
        /// the color comes from <see cref="Scripts.Data.Actor.EnemyChargeCatalog.ColorFor"/>.</summary>
        private void MintInterruptOrb(ActorInstance enemy)
        {
            if (enemy == null || enemy.transform == null) return;
            var color = Scripts.Data.Actor.EnemyChargeCatalog.ColorFor(enemy);
            Scripts.Factories.ManaOrbFactory.Drop(enemy.transform.position, color);
            Scripts.Helpers.GameHelper.CombatTextManager?.Spawn("Interrupted!", enemy.Position, "Heal");
            Scripts.Helpers.GameHelper.AudioManager?.Play("Orb");
            Scripts.Canvas.AnnouncementWindow.Announce($"{enemy.characterClass}'s cast interrupted!");
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
