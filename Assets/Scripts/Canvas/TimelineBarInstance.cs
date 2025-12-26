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
        [SerializeField] private float canvasPercent = 0.97f;

        [Header("Tuning")]
        [Tooltip("Baseline normalized units per second for a tag with Speed=1 (1.0 crosses full bar in1s).")]
        [SerializeField] private float baseUnitsPerSec = 1f;
        [Tooltip("Global multiplier applied to timeline movement speed. Lower = slower countdown/approach to trigger.")]
        [SerializeField] private float timelineSpeedMultiplier = 0.66f;
        [Tooltip("Vertical spacing between duplicate tags (same enemy) in local pixels.")]
        [SerializeField] private float tagRowHeight = 14f;
        [SerializeField] private bool debugLogs = false;

        private readonly List<TimelineTag> activeTags = new List<TimelineTag>();
        private bool advancing;
        public bool IsAdvancing => advancing;

        private float cachedTriggerX;
        private float cachedSpawnX;
        private bool layoutReady;
        private float halfWidth; // runtime half-length of bar

        // Exposed endpoints (center is0). Tags move from SpawnX (right) toward TriggerX (left).
        private float TriggerX => -halfWidth;
        private float SpawnX => halfWidth;
        private float Width => Mathf.Max(1f, SpawnX - TriggerX);

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
            cachedTriggerX = float.NaN; cachedSpawnX = float.NaN;
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
            if (debugLogs) Debug.Log($"[TimelineBar] LayoutReady trigger={TriggerX:F1} spawn={SpawnX:F1} width={Width:F1}");
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
                triggerPointRect.anchoredPosition = new Vector2(TriggerX, 0f);
            }
            if (spawnPointRect != null)
            {
                spawnPointRect.anchorMin = spawnPointRect.anchorMax = new Vector2(0.5f, 0.5f);
                spawnPointRect.pivot = new Vector2(0.5f, 0.5f);
                spawnPointRect.anchoredPosition = new Vector2(SpawnX, 0f);
            }
        }

        private float UnitsPerSecFromSpeed(int speed)
        {
            // Consolidate base speed and per-speed into a base value then apply a global multiplier
            float baseSpeed = baseUnitsPerSec * Mathf.Max(0, speed);
            float mult = Mathf.Max(0.0001f, timelineSpeedMultiplier);
            return Mathf.Max(0.001f, baseSpeed * mult);
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
        /// If there are no tags yet, distribute initial positions by speed (right=fastest).
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
                // Distribute along [0.1..1.0] by speed ordering (right=fastest)
                var ordered = playing.OrderByDescending(e => e.Stats.Speed.ToInt()).ToList();
                int n = ordered.Count;
                for (int i = 0; i < n; i++)
                {
                    var enemy = ordered[i];
                    float startU = n > 1 ? Mathf.Lerp(1f, 0.1f, i / Mathf.Max(1f, n - 1f)) : 1f;
                    SpawnTag(enemy, startU);
                }
            }
            else
            {
                // Only add new ones at the far right
                foreach (var enemy in missing)
                {
                    SpawnTag(enemy, 1f);
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
            // Lock any tags that are already at/past the trigger to the exact trigger position
            UpdateAllEndpoints();
            float left = TriggerX;
            foreach (var t in activeTags)
            {
                if (t == null || t.Rect == null) continue;
                if (t.Rect.anchoredPosition.x <= left + 0.25f)
                {
                    t.SetU(0f); // snaps exactly to leftX via ApplyPosition clamp
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
                tag.SetU(1f); // Snap back to right
                tag.ResetForNextCycle(); // allow it to trigger again next pass
                tag.Pause();
            }
        }

        private void OnTagReachedLeft(TimelineTag tag)
        {
            if (tag == null) return;
            // Tag arrival at TriggerPoint drives turn queue & selection drop
            g.InputManager.InputMode = InputMode.None;
            g.TurnManager.QueueEnemyAfterHero(tag.Owner);
            g.SelectionManager.Drop();
            // Lock the arriving tag exactly at the trigger
            tag.SetU(0f);
            PauseAll();
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

        // NEW: Bank directly to next arriving tag, queue its owner immediately and return it.
        public ActorInstance BankToNextTrigger(out float secondsUsed)
        {
            secondsUsed = 0f;
            if (activeTags.Count == 0) return null;

            // Find earliest tag by seconds remaining
            TimelineTag earliest = null;
            float minSec = float.PositiveInfinity;
            foreach (var t in activeTags)
            {
                if (t == null || t.Owner == null || !t.Owner.IsPlaying) continue;
                float sec = t.GetSecondsRemaining();
                if (sec < minSec)
                {
                    minSec = sec; earliest = t;
                }
            }
            if (earliest == null || float.IsInfinity(minSec) || minSec <= 0f) return null;

            secondsUsed = Mathf.Max(0f, minSec);
            // Advance everyone by minSec
            AdvanceBySeconds(secondsUsed);
            // Explicitly queue arrival for the earliest tag now
            OnTagReachedLeft(earliest);
            return earliest.Owner;
        }

        private void SpawnTag(ActorInstance enemy, float startU)
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
            tr.anchoredPosition = new Vector2(Mathf.Lerp(TriggerX, SpawnX, startU), -dup * tagRowHeight);
            float uSpeed = UnitsPerSecFromSpeed(enemy.Stats.Speed.ToInt());
            tag.InitializeNormalized(enemy, TriggerX, SpawnX, startU, uSpeed, OnTagReachedLeft);
            activeTags.Add(tag);
        }

        private void UpdateAllEndpoints()
        {
            float left = TriggerX; float spawn = SpawnX;
            foreach (var t in activeTags) t?.UpdateEndpoints(left, spawn);
        }

        private void Recalculate()
        {
            float left = TriggerX; float spawn = SpawnX;
            if (float.IsNaN(cachedTriggerX) || float.IsNaN(cachedSpawnX) || !Mathf.Approximately(left, cachedTriggerX) || !Mathf.Approximately(spawn, cachedSpawnX))
            {
                cachedTriggerX = left; cachedSpawnX = spawn;
                foreach (var t in activeTags)
                {
                    if (t == null || t.Rect == null) continue;
                    t.UpdateEndpoints(left, spawn);
                    var p = t.Rect.anchoredPosition;
                    // Only auto-loop tags that slipped past the left edge during HERO turns.
                    // During enemy turns, keep the tag at TriggerX until OnEnemyTurnFinished resets it.
                    bool isHeroTurn = g.TurnManager == null || g.TurnManager.IsHeroTurn;
                    if (isHeroTurn && p.x <= left) t.SetU(1f);
                }
            }
        }
    }
}
