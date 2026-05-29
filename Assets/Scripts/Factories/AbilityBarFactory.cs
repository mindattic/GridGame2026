using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Scripts.Canvas;
using Scripts.Data;
using Scripts.Models;

namespace Scripts.Factories
{
    /// <summary>
    /// ABILITYBARFACTORY - Builds the Row-13 6-slot <see cref="AbilityBar"/> inside the existing
    /// AbilityButtonContainer (canvas child placed by GameBuilder at Row 13).
    ///
    /// <para>Each slot has a frame Image + Name (top) + Cost icons (bottom). The TMP font is
    /// "stolen" from any existing TextMeshProUGUI in the scene (no Resources.Load needed and no
    /// Addressables wiring required).</para>
    ///
    /// <para>The bar holds three kinds of <see cref="ManaAbility"/> entries — Skill / Spell /
    /// Item (see <see cref="AbilityKind"/>). The bar instance dispatches clicks per kind.</para>
    /// </summary>
    public static class AbilityBarFactory
    {
        public const int Slots = 6;
        public const float SlotWidth = 168f;
        public const float SlotHeight = 130f;
        public const float Spacing = 8f;

        public static AbilityBar Create(Transform container, ManaBank bank)
        {
            if (container == null) return null;

            // Ensure the container's HorizontalLayoutGroup is in our desired config (replaces
            // the per-hero MiddleLeft layout the legacy AbilityButtonManager set up).
            var hlg = container.GetComponent<HorizontalLayoutGroup>();
            if (hlg == null) hlg = container.gameObject.AddComponent<HorizontalLayoutGroup>();
            hlg.childAlignment = TextAnchor.MiddleCenter;
            hlg.spacing = Spacing;
            hlg.childControlWidth = false;
            hlg.childControlHeight = false;
            hlg.childForceExpandWidth = false;
            hlg.childForceExpandHeight = false;
            hlg.padding = new RectOffset(8, 8, 8, 8);

            // Fix #10: GameBuilder leaves the legacy `action-bar-1` Image on this container — it
            // shows up faintly behind the new 6-slot bar. Make it transparent so the new bar reads
            // cleanly (we can't easily delete the component from a builder-spawned scene).
            var legacyImg = container.GetComponent<Image>();
            if (legacyImg != null) legacyImg.color = new Color(0f, 0f, 0f, 0f);

            // Reuse a scene-side font so labels render. Null is tolerable — the buttons still work.
            var font = StealSceneFont();

            var buttons = new Button[Slots];
            var nameLabels = new TMP_Text[Slots];
            var costLabels = new TMP_Text[Slots];
            var frames = new Image[Slots];

            // The bar is added at the end; capture via closure variable so click handlers can route
            // through it (it knows the currently-selected-hero's loadout).
            AbilityBar barRef = null;

            for (int i = 0; i < Slots; i++)
            {
                int idx = i; // capture for closure

                var slotGO = new GameObject($"AbilitySlot_{i:D2}",
                    typeof(RectTransform), typeof(Image), typeof(Button), typeof(LayoutElement));
                slotGO.layer = LayerMask.NameToLayer("UI");
                var rt = (RectTransform)slotGO.transform;
                rt.SetParent(container, false);
                rt.sizeDelta = new Vector2(SlotWidth, SlotHeight);

                var le = slotGO.GetComponent<LayoutElement>();
                le.preferredWidth = SlotWidth;
                le.preferredHeight = SlotHeight;
                le.flexibleWidth = 0f;
                le.flexibleHeight = 0f;

                var img = slotGO.GetComponent<Image>();
                img.color = new Color(0.20f, 0.25f, 0.40f, 0.92f);
                frames[i] = img;

                var btn = slotGO.GetComponent<Button>();
                btn.targetGraphic = img;
                btn.onClick.AddListener(() => { if (barRef != null) barRef.OnSlotClicked(idx); });
                buttons[i] = btn;

                nameLabels[i] = CreateLabel(slotGO.transform, "Name", font,
                    fontSize: 28,
                    anchorMin: new Vector2(0f, 0.45f), anchorMax: new Vector2(1f, 1f),
                    color: Color.white);

                costLabels[i] = CreateLabel(slotGO.transform, "Cost", font,
                    fontSize: 22,
                    anchorMin: new Vector2(0f, 0f), anchorMax: new Vector2(1f, 0.45f),
                    color: new Color(1f, 0.92f, 0.55f));
            }

            barRef = container.gameObject.GetComponent<AbilityBar>();
            if (barRef == null) barRef = container.gameObject.AddComponent<AbilityBar>();
            barRef.Bind(bank, buttons, nameLabels, costLabels, frames);
            return barRef;
        }

        private static TMP_Text CreateLabel(Transform parent, string name, TMP_FontAsset font,
            int fontSize, Vector2 anchorMin, Vector2 anchorMax, Color color)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(TextMeshProUGUI));
            go.layer = parent.gameObject.layer;
            var rt = (RectTransform)go.transform;
            rt.SetParent(parent, false);
            rt.anchorMin = anchorMin;
            rt.anchorMax = anchorMax;
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;

            var tmp = go.GetComponent<TextMeshProUGUI>();
            if (font != null) tmp.font = font;
            tmp.fontSize = fontSize;
            tmp.color = color;
            tmp.alignment = TextAlignmentOptions.Center;
            tmp.text = string.Empty;
            tmp.raycastTarget = false;
            return tmp;
        }

        private static TMP_FontAsset StealSceneFont()
        {
            // Any TMP label already in the scene (ActionTitle, CoinCounter, etc.) has a usable font
            // asset wired up by GameBuilder via SceneBuilderHelper.LoadFont. Borrow it. Unity 6's
            // FindFirstObjectByType replaces the deprecated FindObjectOfType.
            var any = Object.FindFirstObjectByType<TextMeshProUGUI>();
            return any != null ? any.font : null;
        }

        // Click resolution lives on the AbilityBar component itself so it can read the
        // currently-selected hero's loadout (not a global slot list).
    }
}
