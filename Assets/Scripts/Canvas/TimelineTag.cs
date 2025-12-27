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

        // Normalized motion state (resolution-independent)
        private float leftX; // bar-local x at u=0 (left edge target)
        private float spawnX; // bar-local x at u=1 (right edge spawn)
        private float u; // normalized position in [0..1],1 = far right,0 = left
        private float uPerSec; // normalized speed per second (u units per sec)

        private System.Action<TimelineTag> onReached;
        private bool isFading;
        private bool paused;
        private bool fired;

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
        public void InitializeNormalized(ActorInstance owner, float triggerX, float spawnX, float startU, float uPerSec, System.Action<TimelineTag> onReached)
        {
            Owner = owner;
            this.leftX = triggerX;
            this.spawnX = Mathf.Max(spawnX, triggerX +1f);
            this.u = Mathf.Clamp01(startU);
            this.uPerSec = Mathf.Max(0.0001f, uPerSec);
            this.onReached = onReached;
            if (CanvasGroup != null) CanvasGroup.alpha =1f;
            isFading = false;
            paused = true; // start paused; TimelineBar controls advance
            fired = false;

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
                        sprite = SpriteLibrary.GetIconForActorTags(data.Tags);
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
            float width = Mathf.Max(1f, spawnX - leftEdgeX);
            float startU = Mathf.InverseLerp(leftEdgeX, spawnX, startX);
            float uSpeed = Mathf.Abs(moveSpeedPxPerSec) / width;
            InitializeNormalized(owner, leftEdgeX, spawnX, startU, uSpeed, onReached);
        }

        public void UpdateEndpoints(float newLeftX, float newSpawnX)
        {
            // Preserve normalized u while endpoints shift
            leftX = newLeftX;
            spawnX = Mathf.Max(newSpawnX, newLeftX +1f);
            ApplyPosition();
            UpdateLabel();
        }

        public void Pause() => paused = true;
        public void Resume() => paused = false;
        public void SetAlpha(float a) { if (CanvasGroup != null) CanvasGroup.alpha = Mathf.Clamp01(a); }

        // Reset this tag's trigger state so it can fire on the next cycle
        public void ResetForNextCycle()
        {
            fired = false;
            UpdateLabel();
        }

        // Set anchored x from normalized u (left-edge pivot guarantees alignment)
        private void ApplyPosition()
        {
            if (Rect == null) return;
            float xLeft = Mathf.Lerp(leftX, spawnX, Mathf.Clamp01(u));
            // Prevent ever going past the trigger point; lock exactly at leftX when u<=0
            if (xLeft < leftX) xLeft = leftX;
            var p = Rect.anchoredPosition;
            Rect.anchoredPosition = new Vector2(xLeft, p.y);
        }

        public void SetX(float xLeft)
        {
            if (Rect != null)
            {
                // Clamp to never cross the trigger to the left
                float clamped = Mathf.Max(leftX, xLeft);
                var p = Rect.anchoredPosition;
                Rect.anchoredPosition = new Vector2(clamped, p.y);
                u = (spawnX - leftX) >0.0001f ? Mathf.InverseLerp(leftX, spawnX, clamped) : u;
            }
            else
            {
                var lp = transform.localPosition;
                float clamped = Mathf.Max(leftX, xLeft);
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
        public float GetSecondsRemaining() => uPerSec <=0f ?0f : Mathf.Max(0f, u / uPerSec);

        private void Update()
        {
            if (!isFading && !paused)
            {
                // Move toward left (u =0)
                u = Mathf.MoveTowards(u,0f, uPerSec * Time.deltaTime);
                ApplyPosition();
            }
            // Ensure we never drift past the trigger point due to float jitter
            if (Rect != null && Rect.anchoredPosition.x < leftX)
            {
                SetX(leftX);
            }

            // Update label after we potentially moved this frame
            UpdateLabel();

            // Left-edge strict check using anchoredPosition.x (left pivot)
            if (!fired && Rect != null && Rect.anchoredPosition.x <= leftX + ReachTolerance)
            {
                fired = true;
                onReached?.Invoke(this);
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
