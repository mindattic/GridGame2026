using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Scripts.Canvas;
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

namespace Scripts.Factories
{
    /// <summary>
    /// HEROCARDFACTORY - Builds a rich hero row used by the Party screen.
    /// <para>VISUAL LAYOUT (single row, ~120 px tall):
    /// <code>
    /// ┌────────────────────────────────────────────────────────────┐
    /// │ ┌──────┐  Cleric  Lv 4                       ┌──────────┐ │
    /// │ │ POR- │  HP 76  AP 100                      │   Add    │ │
    /// │ │ TRAIT│  STR 31  VIT 12  AGI 12             │ to Party │ │
    /// │ │      │  INT 18  WIS 17  LCK 13             └──────────┘ │
    /// │ └──────┘                                                   │
    /// └────────────────────────────────────────────────────────────┘
    /// </code></para>
    /// <para>The whole row is clickable for selection (drives profile preview); the button on the
    /// right is a separate clickable region for the contextual party action.</para>
    /// <para>RELATED FILES: PartySection.cs, HubItemRowFactory.cs, HubTheme.cs</para>
    /// </summary>
    public static class HeroCardFactory
    {
        private const float RowHeight = 124f;
        private const float PortraitSize = 96f;
        private const float Padding = 12f;
        private const float ActionButtonWidth = 120f;
        private const float TextLeftOffset = PortraitSize + Padding * 2f;

        private static readonly Color RowBg            = new Color(0.20f, 0.24f, 0.34f, 1f);
        private static readonly Color RowSelectedBg    = new Color(0.36f, 0.50f, 0.78f, 1f);
        private static readonly Color RowInPartyAccent = new Color(0.30f, 0.65f, 0.40f, 1f); // green tint band

        /// <summary>Creates a new hero card row attached to <paramref name="parent"/>.
        /// The returned root carries a <see cref="Button"/> for the body click; the action button
        /// (Add/Remove) is at child path "ActionButton".</summary>
        public static GameObject Create(Transform parent)
        {
            var root = new GameObject("HeroCard");
            root.layer = LayerMask.NameToLayer("UI");
            var rootRT = root.AddComponent<RectTransform>();
            // Canonical top-anchored stretch used by VerticalLayoutGroup children. With pivot
            // (0.5, 1) the layout group writes anchoredPosition.y for each row top-down without
            // any pivot/offset arithmetic that bites with center pivots.
            rootRT.anchorMin = new Vector2(0f, 1f);
            rootRT.anchorMax = new Vector2(1f, 1f);
            rootRT.pivot = new Vector2(0.5f, 1f);
            rootRT.sizeDelta = new Vector2(0f, RowHeight);
            rootRT.SetParent(parent, false);

            root.AddComponent<CanvasRenderer>();
            var bg = root.AddComponent<Image>();
            bg.color = RowBg;
            bg.raycastTarget = true;

            var btn = root.AddComponent<Button>();
            btn.targetGraphic = bg;
            btn.transition = Selectable.Transition.ColorTint;
            btn.colors = new ColorBlock
            {
                normalColor = Color.white,
                highlightedColor = new Color(1.10f, 1.10f, 1.18f, 1f),
                pressedColor = new Color(0.70f, 0.70f, 0.85f, 1f),
                selectedColor = new Color(1.00f, 1.00f, 1.10f, 1f),
                disabledColor = new Color(0.55f, 0.55f, 0.55f, 0.60f),
                colorMultiplier = 1f,
                fadeDuration = 0.08f,
            };
            btn.navigation = new Navigation { mode = Navigation.Mode.None };

            var le = root.AddComponent<LayoutElement>();
            le.preferredHeight = RowHeight;
            le.flexibleWidth = 1f;

            // Left accent strip — green when this hero is in the active party.
            var accent = MakeChild(rootRT, "PartyAccent");
            accent.anchorMin = new Vector2(0f, 0f);
            accent.anchorMax = new Vector2(0f, 1f);
            accent.pivot = new Vector2(0f, 0.5f);
            accent.sizeDelta = new Vector2(6f, 0f);
            accent.anchoredPosition = Vector2.zero;
            var accentImg = accent.gameObject.AddComponent<Image>();
            accentImg.color = RowInPartyAccent;
            accentImg.raycastTarget = false;
            accent.gameObject.SetActive(false);

            // Portrait
            var portrait = MakeChild(rootRT, "Portrait");
            portrait.anchorMin = new Vector2(0f, 0.5f);
            portrait.anchorMax = new Vector2(0f, 0.5f);
            portrait.pivot = new Vector2(0f, 0.5f);
            portrait.sizeDelta = new Vector2(PortraitSize, PortraitSize);
            portrait.anchoredPosition = new Vector2(Padding, 0f);
            var portraitImg = portrait.gameObject.AddComponent<Image>();
            portraitImg.preserveAspect = true;
            portraitImg.raycastTarget = false;
            portraitImg.color = Color.white;

            // Name + Lv (top text line)
            var name = MakeChild(rootRT, "NameLabel");
            name.anchorMin = new Vector2(0f, 1f);
            name.anchorMax = new Vector2(1f, 1f);
            name.pivot = new Vector2(0f, 1f);
            name.offsetMin = new Vector2(TextLeftOffset, -38f);
            name.offsetMax = new Vector2(-(ActionButtonWidth + Padding * 2f), -6f);
            var nameTmp = name.gameObject.AddComponent<TextMeshProUGUI>();
            nameTmp.fontSize = 28;
            nameTmp.color = Color.white;
            nameTmp.alignment = TextAlignmentOptions.MidlineLeft;
            nameTmp.enableWordWrapping = false;
            nameTmp.overflowMode = TextOverflowModes.Ellipsis;
            nameTmp.richText = true;
            nameTmp.raycastTarget = false;
            nameTmp.fontStyle = FontStyles.Bold;

            // Stats lines (two short rows fitting the body height)
            var statsTop = MakeChild(rootRT, "StatsTop");
            statsTop.anchorMin = new Vector2(0f, 0.5f);
            statsTop.anchorMax = new Vector2(1f, 0.5f);
            statsTop.pivot = new Vector2(0f, 0.5f);
            statsTop.offsetMin = new Vector2(TextLeftOffset, -8f);
            statsTop.offsetMax = new Vector2(-(ActionButtonWidth + Padding * 2f), 18f);
            var statsTopTmp = statsTop.gameObject.AddComponent<TextMeshProUGUI>();
            statsTopTmp.fontSize = 18;
            statsTopTmp.color = new Color(0.85f, 0.85f, 0.90f, 1f);
            statsTopTmp.alignment = TextAlignmentOptions.MidlineLeft;
            statsTopTmp.enableWordWrapping = false;
            statsTopTmp.overflowMode = TextOverflowModes.Ellipsis;
            statsTopTmp.richText = true;
            statsTopTmp.raycastTarget = false;

            var statsBottom = MakeChild(rootRT, "StatsBottom");
            statsBottom.anchorMin = new Vector2(0f, 0f);
            statsBottom.anchorMax = new Vector2(1f, 0f);
            statsBottom.pivot = new Vector2(0f, 0f);
            statsBottom.offsetMin = new Vector2(TextLeftOffset, 6f);
            statsBottom.offsetMax = new Vector2(-(ActionButtonWidth + Padding * 2f), 28f);
            var statsBottomTmp = statsBottom.gameObject.AddComponent<TextMeshProUGUI>();
            statsBottomTmp.fontSize = 18;
            statsBottomTmp.color = new Color(0.75f, 0.75f, 0.82f, 1f);
            statsBottomTmp.alignment = TextAlignmentOptions.MidlineLeft;
            statsBottomTmp.enableWordWrapping = false;
            statsBottomTmp.overflowMode = TextOverflowModes.Ellipsis;
            statsBottomTmp.richText = true;
            statsBottomTmp.raycastTarget = false;

            // Action button — anchored to the right edge.
            var action = MakeChild(rootRT, "ActionButton");
            action.anchorMin = new Vector2(1f, 0.5f);
            action.anchorMax = new Vector2(1f, 0.5f);
            action.pivot = new Vector2(1f, 0.5f);
            action.sizeDelta = new Vector2(ActionButtonWidth, RowHeight - Padding * 2f);
            action.anchoredPosition = new Vector2(-Padding, 0f);
            var actionImg = action.gameObject.AddComponent<Image>();
            actionImg.color = HubTheme.Accent;
            actionImg.raycastTarget = true;
            var actionBtn = action.gameObject.AddComponent<Button>();
            actionBtn.targetGraphic = actionImg;
            actionBtn.transition = Selectable.Transition.ColorTint;
            actionBtn.colors = new ColorBlock
            {
                normalColor = Color.white,
                highlightedColor = new Color(1.15f, 1.15f, 1.20f, 1f),
                pressedColor = new Color(0.65f, 0.65f, 0.80f, 1f),
                selectedColor = new Color(1.00f, 1.00f, 1.10f, 1f),
                disabledColor = new Color(0.50f, 0.50f, 0.50f, 0.60f),
                colorMultiplier = 1f,
                fadeDuration = 0.08f,
            };
            actionBtn.navigation = new Navigation { mode = Navigation.Mode.None };

            var actionLabel = MakeChild(action, "Label");
            actionLabel.anchorMin = Vector2.zero; actionLabel.anchorMax = Vector2.one;
            actionLabel.offsetMin = Vector2.zero; actionLabel.offsetMax = Vector2.zero;
            var actionTmp = actionLabel.gameObject.AddComponent<TextMeshProUGUI>();
            actionTmp.text = "Add";
            actionTmp.fontSize = 24;
            actionTmp.color = Color.black;
            actionTmp.alignment = TextAlignmentOptions.Center;
            actionTmp.enableWordWrapping = false;
            actionTmp.raycastTarget = false;
            actionTmp.fontStyle = FontStyles.Bold;

            return root;
        }

        // ---- Setters ----

        public static void SetName(GameObject row, string name, int level)
        {
            var t = row?.transform.Find("NameLabel")?.GetComponent<TextMeshProUGUI>();
            if (t != null) t.text = $"{name}  <color=#888>Lv {level}</color>";
        }

        public static void SetStats(GameObject row, ActorStats stats)
        {
            var top = row?.transform.Find("StatsTop")?.GetComponent<TextMeshProUGUI>();
            var bot = row?.transform.Find("StatsBottom")?.GetComponent<TextMeshProUGUI>();
            if (top != null) top.text = $"HP {stats.MaxHP:0}  STR {stats.Strength:0}  VIT {stats.Vitality:0}  AGI {stats.Agility:0}";
            if (bot != null) bot.text = $"AP {stats.MaxAP:0}  INT {stats.Intelligence:0}  WIS {stats.Wisdom:0}  LCK {stats.Luck:0}";
        }

        public static void SetPortrait(GameObject row, Sprite portrait, Color fallbackTint)
        {
            var img = row?.transform.Find("Portrait")?.GetComponent<Image>();
            if (img == null) return;
            if (portrait != null)
            {
                img.sprite = portrait;
                img.color = Color.white;
            }
            else
            {
                // Use the placeholder factory for a clean filled square in the class colour.
                img.sprite = PlaceholderIconFactory.GetFallback();
                img.color = fallbackTint;
            }
        }

        public static void SetSelected(GameObject row, bool selected)
        {
            var img = row?.GetComponent<Image>();
            if (img != null) img.color = selected ? RowSelectedBg : RowBg;
        }

        public static void SetInParty(GameObject row, bool inParty)
        {
            var accent = row?.transform.Find("PartyAccent");
            if (accent != null) accent.gameObject.SetActive(inParty);
        }

        public static void SetAction(GameObject row, string label, bool interactable, System.Action onClick)
        {
            var actionTr = row?.transform.Find("ActionButton");
            if (actionTr == null) return;
            var btn = actionTr.GetComponent<Button>();
            var tmp = actionTr.Find("Label")?.GetComponent<TextMeshProUGUI>();
            if (tmp != null) tmp.text = label;
            if (btn != null)
            {
                btn.onClick.RemoveAllListeners();
                btn.interactable = interactable;
                if (onClick != null) btn.onClick.AddListener(() => onClick());
            }
        }

        public static void OnRowClick(GameObject row, System.Action onClick)
        {
            var btn = row?.GetComponent<Button>();
            if (btn == null || onClick == null) return;
            btn.onClick.RemoveAllListeners();
            btn.onClick.AddListener(() => onClick());
        }

        // ---- internals ----

        private static RectTransform MakeChild(Transform parent, string name)
        {
            var go = new GameObject(name);
            go.layer = LayerMask.NameToLayer("UI");
            var rt = go.AddComponent<RectTransform>();
            rt.SetParent(parent, false);
            go.AddComponent<CanvasRenderer>();
            return rt;
        }
    }
}
