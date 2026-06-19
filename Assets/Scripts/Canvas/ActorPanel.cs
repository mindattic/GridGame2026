using Scripts.Helpers;
using Scripts.Libraries;
using Scripts.Utilities;
using System;
using System.Collections;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using static Scripts.Helpers.GameObjectHelper;
using c = Scripts.Helpers.CanvasHelper;
using g = Scripts.Helpers.GameHelper;
using Scripts.Data.Actor;
using Scripts.Data.Items;
using Scripts.Data.Skills;
using Scripts.Effects;
using Scripts.Factories;
using Scripts.Hub;
using Scripts.Instances;
using Scripts.Instances.Actor;
using Scripts.Instances.Board;
using Scripts.Instances.SynergyLine;
using Scripts.Inventory;
using Scripts.Managers;
using Scripts.Models;
using Scripts.Models.Actor;
using Scripts.Overworld;
using Scripts.Sequences;
using Scripts.Serialization;

namespace Scripts.Canvas
{
    /// <summary>
    /// ACTORPANEL - The bottom-HUD character panel (HUD Row 15). Replaces the old single-view
    /// "ActorCard": a tabbed interface for the currently-selected actor.
    ///
    /// <para>VISUAL APPEARANCE (tabs across the top, full-width content below):</para>
    /// <code>
    /// ┌──────────────────────────────────────────┐
    /// │      [ Stats ][ Equipment ][ Lore ]    ◀ ▶│
    /// ├──────────────────────────────────────────┤
    /// │ [face] Kyle  Lv.5   HP 80/80               │
    /// │        STR.. VIT.. AGI.. SPD.. ... ATK DEF │
    /// └──────────────────────────────────────────┘
    /// </code>
    ///
    /// <para>TABS:
    /// <list type="number">
    ///   <item><b>Stats</b> — portrait, name, level, HP, the 8 core stats + derived ATK/DEF/SPD.</item>
    ///   <item><b>Equipment</b> — the hero's 5 equipment slots and what's slotted.</item>
    ///   <item><b>Lore</b> — the actor's lore/description text.</item>
    /// </list></para>
    ///
    /// <para>Built code-only: the GameBuilder scene object is just the root + this component;
    /// the tab bar and content panels are constructed at runtime in <see cref="BuildUi"/> (fonts
    /// via <see cref="FontLibrary"/>), so layout iteration needs no scene rebuild.</para>
    ///
    /// <para>ACCESS: g.ActorPanel</para>
    /// <para>RELATED FILES: ActorInstance.cs, SelectionManager.cs, AbilityButtonManager.cs</para>
    /// </summary>
    public class ActorPanel : MonoBehaviour
    {
        #region Layout constants
        private const float TabBarHeight = 44f;
        private const float Pad = 12f;
        private const float FrameInset = 8f;             // small padding inside the UiKit-style 2px border
        private const float PortraitSize = 440f;        // large; bleeds off the lower-right corner
        private const float StatsRightReserve = 450f;   // keep title/stats text out of the portrait's overlap margin
        private static readonly Color TabSelected     = HubTheme.NavActive;
        private static readonly Color TabUnselected   = HubTheme.NavIdle;
        private static readonly Color HeroBackdrop    = HubTheme.PanelBg;
        private static readonly Color EnemyBackdropBase = new Color(0.22f, 0.06f, 0.06f, 0.95f);
        private static readonly Color EnemyBackdropPeak = new Color(0.38f, 0.06f, 0.06f, 0.95f);
        private static readonly string[] TabNames = { "Stats", "Equipment", "Lore" };
        private static readonly EquipmentSlot[] EquipSlots =
            { EquipmentSlot.Weapon, EquipmentSlot.Armor, EquipmentSlot.Relic1, EquipmentSlot.Relic2, EquipmentSlot.Relic3 };
        #endregion

        #region Cached UI
        private Image backdropImage;
        private Image[] tabButtonBgs;
        private GameObject[] tabPanels;
        private Image portraitImg;
        private CanvasGroup portraitCG;
        private TextMeshProUGUI titleText;   // name + level
        private TextMeshProUGUI statsText;   // HP + stat block
        private TextMeshProUGUI equipmentText;
        private TextMeshProUGUI loreText;
        private int currentTab = 0;
        private Coroutine enemyFlickerRoutine;
        private bool built;
        #endregion

        #region Lifecycle
        private void Awake()
        {
            BuildUi();
            ShowTab(0);
            Clear();
            GameReady.Begin(this);
        }

        /// <summary>Constructs the tab bar + three content panels under this object's RectTransform.</summary>
        private void BuildUi()
        {
            if (built) return;
            built = true;

            var root = (RectTransform)transform;

            // Clear any legacy children (the old card's portrait/title/details/arrows) so only the
            // tabbed UI built below remains — defensive against an un-pruned GameBuilder root.
            for (int i = root.childCount - 1; i >= 0; i--) Destroy(root.GetChild(i).gameObject);

            // Backdrop — dark navy panel (HubTheme) + thin UiKit-style steel border. Also the enemy-flicker tint.
            backdropImage = MakeImage("Backdrop", root, HubTheme.PanelBg);
            Stretch((RectTransform)backdropImage.transform);
            backdropImage.raycastTarget = false;
            AddBorder((RectTransform)backdropImage.transform);

            // ── Tab bar (top strip): 3 tab buttons + the two hero-cycle arrows on the right. ──
            var tabBar = MakeRect("TabBar", root);
            tabBar.anchorMin = new Vector2(0f, 1f);
            tabBar.anchorMax = new Vector2(1f, 1f);
            tabBar.pivot = new Vector2(0.5f, 1f);
            tabBar.sizeDelta = new Vector2(-FrameInset * 2f, TabBarHeight);
            tabBar.anchoredPosition = new Vector2(0f, -FrameInset * 0.4f);

            tabButtonBgs = new Image[TabNames.Length];
            float btnW = 150f;
            for (int i = 0; i < TabNames.Length; i++)
            {
                int idx = i;
                var btnBg = MakeImage($"Tab{TabNames[i]}", tabBar, TabUnselected);
                var brt = (RectTransform)btnBg.transform;
                brt.anchorMin = new Vector2(0f, 0.5f);
                brt.anchorMax = new Vector2(0f, 0.5f);
                brt.pivot = new Vector2(0f, 0.5f);
                brt.sizeDelta = new Vector2(btnW, TabBarHeight - 8f);
                brt.anchoredPosition = new Vector2(i * (btnW + 6f), 0f);
                var btn = btnBg.gameObject.AddComponent<Button>();
                btn.targetGraphic = btnBg;
                btn.onClick.AddListener(() => { ShowTab(idx); g.AudioManager?.Play("Click"); });
                var lbl = MakeText(btnBg.transform, "Outfit", 26f, TextAlignmentOptions.Center);
                Stretch((RectTransform)lbl.transform);
                lbl.text = TabNames[i];
                tabButtonBgs[i] = btnBg;
            }

            // Hero-cycle arrows (right side of the tab bar). Use ASCII '<'/'>' — the Avenir TMP
            // font has no glyph for the ◀/▶ triangles, so those render as .notdef squares.
            MakeArrow(tabBar, "<", new Vector2(1f, 0.5f), -44f, () => OnPreviousHeroArrowClick());
            MakeArrow(tabBar, ">", new Vector2(1f, 0.5f), -6f,  () => OnNextHeroArrowClick());

            // ── Content area (below the tab bar) holds the three tab panels. ──
            var content = MakeRect("Content", root);
            content.anchorMin = new Vector2(0f, 0f);
            content.anchorMax = new Vector2(1f, 1f);
            content.pivot = new Vector2(0.5f, 0.5f);
            content.offsetMin = new Vector2(FrameInset, FrameInset);
            content.offsetMax = new Vector2(-FrameInset, -(TabBarHeight + FrameInset * 0.5f));

            tabPanels = new GameObject[TabNames.Length];

            // Tab 0 — Stats: portrait (left) + title (top) + stat block (body).
            var stats = MakeRect("StatsTab", content); Stretch(stats); tabPanels[0] = stats.gameObject;

            // Title + stats pushed to the LEFT; the right side is reserved (StatsRightReserve) so no
            // text prints into the large portrait's overlapping margin.
            titleText = MakeText(stats, "Attic", 30f, TextAlignmentOptions.TopLeft);
            var ttr = (RectTransform)titleText.transform;
            ttr.anchorMin = new Vector2(0f, 1f); ttr.anchorMax = new Vector2(1f, 1f); ttr.pivot = new Vector2(0f, 1f);
            ttr.offsetMin = new Vector2(0f, -34f);
            ttr.offsetMax = new Vector2(-StatsRightReserve, 0f);

            statsText = MakeText(stats, "Outfit", 22f, TextAlignmentOptions.TopLeft);
            var str = (RectTransform)statsText.transform;
            str.anchorMin = new Vector2(0f, 0f); str.anchorMax = new Vector2(1f, 1f); str.pivot = new Vector2(0f, 1f);
            str.offsetMin = new Vector2(0f, 0f);
            str.offsetMax = new Vector2(-StatsRightReserve, -38f);
            statsText.enableWordWrapping = true;

            // Large portrait anchored to the TOP-right: the top of the portrait aligns with the top
            // of the panel and the figure is big enough that its lower ~half bleeds off the bottom
            // edge (only the head/torso reads inside the panel). Built last in the Stats tab so it
            // draws over the (left-confined) text region.
            portraitImg = MakeImage("Portrait", stats, Color.white);
            var prt = (RectTransform)portraitImg.transform;
            prt.anchorMin = new Vector2(1f, 1f);
            prt.anchorMax = new Vector2(1f, 1f);
            prt.pivot = new Vector2(1f, 1f);
            prt.sizeDelta = new Vector2(PortraitSize, PortraitSize);
            prt.anchoredPosition = new Vector2(-6f, 6f);     // top edge ≈ panel top; bottom bleeds off
            portraitImg.preserveAspect = true;
            portraitImg.raycastTarget = false;
            portraitCG = portraitImg.gameObject.AddComponent<CanvasGroup>();

            // Tab 1 — Equipment.
            var equip = MakeRect("EquipmentTab", content); Stretch(equip); tabPanels[1] = equip.gameObject;
            equipmentText = MakeText(equip, "Outfit", 24f, TextAlignmentOptions.TopLeft);
            Stretch((RectTransform)equipmentText.transform);
            equipmentText.enableWordWrapping = true;

            // Tab 2 — Lore.
            var lore = MakeRect("LoreTab", content); Stretch(lore); tabPanels[2] = lore.gameObject;
            loreText = MakeText(lore, "Outfit", 22f, TextAlignmentOptions.TopLeft);
            Stretch((RectTransform)loreText.transform);
            loreText.enableWordWrapping = true;

            // The big portrait bleeds upward past the content area; keep the tab bar on top so its
            // buttons stay visible and clickable above the portrait.
            tabBar.SetAsLastSibling();
        }
        #endregion

        #region Tab switching
        /// <summary>Activates one tab's content panel and highlights its button.</summary>
        public void ShowTab(int index)
        {
            if (tabPanels == null) return;
            currentTab = Mathf.Clamp(index, 0, tabPanels.Length - 1);
            for (int i = 0; i < tabPanels.Length; i++)
            {
                if (tabPanels[i] != null) tabPanels[i].SetActive(i == currentTab);
                if (tabButtonBgs != null && tabButtonBgs[i] != null)
                    tabButtonBgs[i].color = (i == currentTab) ? TabSelected : TabUnselected;
            }
        }
        #endregion

        #region Population
        /// <summary>Fills all three tabs for the currently-selected actor; keeps the active tab.</summary>
        public void Assign()
        {
            if (!built || !g.Actors.HasSelectedActor) return;

            var actor = g.Actors.SelectedActor;
            var cls = actor.characterClass;
            var data = ActorLibrary.Get(cls);

            if (portraitImg != null && data != null) portraitImg.sprite = data.Portrait;
            // Hide the Image entirely when it has no sprite — otherwise the default white quad
            // renders as a blank white square in the panel.
            SetAlpha(portraitCG, portraitImg != null && portraitImg.sprite != null ? 1f : 0f);

            var s = actor.Stats;
            int lvl = s.Level;
            int hp = Mathf.RoundToInt(s.HP), maxHp = Mathf.RoundToInt(s.MaxHP);
            int atk = Mathf.FloorToInt(Formulas.Offense(s, 0f));
            int def = Mathf.FloorToInt(Formulas.Defense(s, 0f));

            if (titleText != null) titleText.text = $"{cls}    <size=70%>Lv.{lvl}</size>";

            if (statsText != null)
            {
                var sb = new System.Text.StringBuilder();
                sb.AppendLine($"<color=#FF8888>HP</color> {hp}/{maxHp}    <color=#88CCFF>ATK</color> {atk}   <color=#88CC88>DEF</color> {def}");
                sb.AppendLine();
                sb.AppendLine($"STR {Mathf.RoundToInt(s.Strength),-3}  VIT {Mathf.RoundToInt(s.Vitality),-3}  AGI {Mathf.RoundToInt(s.Agility),-3}  SPD {Mathf.RoundToInt(s.Speed),-3}");
                sb.Append($"STA {Mathf.RoundToInt(s.Stamina),-3}  INT {Mathf.RoundToInt(s.Intelligence),-3}  WIS {Mathf.RoundToInt(s.Wisdom),-3}  LCK {Mathf.RoundToInt(s.Luck),-3}");
                statsText.text = sb.ToString();
            }

            if (equipmentText != null) equipmentText.text = BuildEquipmentText(cls);
            if (loreText != null)
            {
                string lore = data != null ? (!string.IsNullOrEmpty(data.Lore) ? data.Lore : data.Description) : null;
                loreText.text = string.IsNullOrEmpty(lore) ? "<i><color=#888888>No lore recorded.</color></i>" : lore;
            }

            backdropImage?.gameObject.SetActive(true);
            ApplyBackdropFor(actor.IsEnemy);
        }

        /// <summary>Lists the actor's five equipment slots and what's slotted (from the save).</summary>
        private static string BuildEquipmentText(CharacterClass cls)
        {
            var save = ProfileHelper.CurrentProfile?.CurrentSave;
            var eq = save?.Equipment?.GetOrCreate(cls);
            var sb = new System.Text.StringBuilder();
            foreach (var slot in EquipSlots)
            {
                string itemId = eq?.GetSlot(slot);
                string name = "<color=#666666>— empty —</color>";
                if (!string.IsNullOrEmpty(itemId))
                {
                    var def = ItemLibrary.Get(itemId);
                    name = def != null ? def.DisplayName : itemId;
                }
                sb.AppendLine($"<color=#AAAAAA>{slot,-7}</color>  {name}");
            }
            return sb.ToString();
        }

        /// <summary>Preview an ability in the Stats tab (portrait → icon, title → name, body → cost/desc).</summary>
        public void AssignAbility(Ability ability)
        {
            if (!built || ability == null) return;
            ShowTab(0);
            if (portraitImg != null && ability.button != null) portraitImg.sprite = ability.button;
            SetAlpha(portraitCG, 1f);
            if (titleText != null) titleText.text = ability.name;

            if (statsText != null)
            {
                var sb = new System.Text.StringBuilder();
                sb.Append("<color=#88CCFF>MP Cost:</color> ").AppendLine(ability.ManaCost.ToString());
                var formula = Formulas.DescribeAbility(ability);
                if (!string.IsNullOrEmpty(formula)) sb.AppendLine(formula);
                if (!string.IsNullOrEmpty(ability.Description))
                {
                    sb.AppendLine();
                    sb.Append("<color=#AAAAAA><i>").Append(ability.Description).Append("</i></color>");
                }
                statsText.text = sb.ToString();
            }
            backdropImage?.gameObject.SetActive(true);
            ApplyBackdropFor(isEnemy: false);
        }

        /// <summary>Blanks the panel's text/portrait.</summary>
        public void Clear()
        {
            if (!built) return;
            StopAllCoroutines();
            enemyFlickerRoutine = null;
            if (backdropImage != null) { backdropImage.color = HeroBackdrop; backdropImage.gameObject.SetActive(true); }
            if (titleText != null) titleText.text = "";
            if (statsText != null) statsText.text = "";
            if (equipmentText != null) equipmentText.text = "";
            if (loreText != null) loreText.text = "";
            // Nothing selected → no portrait sprite → hide it so no white square lingers.
            SetAlpha(portraitCG, portraitImg != null && portraitImg.sprite != null ? 1f : 0f);
        }

        /// <summary>No-op kept for call-site compatibility (panel stays visible).</summary>
        public void SlideOut()
        {
            backdropImage?.gameObject.SetActive(true);
        }
        #endregion

        #region Backdrop flicker (enemy)
        private void ApplyBackdropFor(bool isEnemy)
        {
            if (enemyFlickerRoutine != null) { StopCoroutine(enemyFlickerRoutine); enemyFlickerRoutine = null; }
            if (backdropImage == null) return;
            if (isEnemy)
            {
                backdropImage.color = EnemyBackdropBase;
                enemyFlickerRoutine = StartCoroutine(EnemyFlickerRoutine());
            }
            else backdropImage.color = HeroBackdrop;
        }

        private IEnumerator EnemyFlickerRoutine()
        {
            float t0 = Time.time;
            while (true)
            {
                float b = Scripts.Effects.QuakeLightFlicker.SampleSmooth(
                    Scripts.Effects.QuakeLightFlicker.FluorescentFlicker, Time.time - t0);
                float k = Mathf.Clamp01(b);
                backdropImage.color = Color.Lerp(EnemyBackdropBase, EnemyBackdropPeak, k);
                yield return Wait.None();
            }
        }
        #endregion

        #region Portrait helpers
        /// <summary>World position of the portrait — used as the origin for ability projectiles.</summary>
        public Vector3 PortraitWorldPosition()
        {
            if (portraitImg == null) return transform.position;
            return UnitConversionHelper.Canvas.ToWorld(portraitImg.transform);
        }

        /// <summary>Plays a vertical bounce on the portrait.</summary>
        public void BouncePortrait(float percentOfScreenHeight = 0.03f, float bounceDuration = 0.3333f)
        {
            if (portraitImg == null) return;
            StartCoroutine(BouncePortraitRoutine(Screen.height * percentOfScreenHeight, bounceDuration));
        }

        private IEnumerator BouncePortraitRoutine(float distance, float duration)
        {
            var rt = (RectTransform)portraitImg.transform;
            Vector2 origin = rt.anchoredPosition;
            Vector2 up = origin + Vector2.up * distance;
            float half = duration * 0.5f, elapsed = 0f;
            while (elapsed < half) { rt.anchoredPosition = Vector2.Lerp(origin, up, Mathf.SmoothStep(0f, 1f, elapsed / half)); elapsed += Time.deltaTime; yield return Wait.None(); }
            elapsed = 0f;
            while (elapsed < half) { rt.anchoredPosition = Vector2.Lerp(up, origin, Mathf.SmoothStep(0f, 1f, elapsed / half)); elapsed += Time.deltaTime; yield return Wait.None(); }
            rt.anchoredPosition = origin;
        }
        #endregion

        #region Hero cycling
        public void OnPreviousHeroArrowClick() => CycleHero(-1);
        public void OnNextHeroArrowClick() => CycleHero(1);

        private void CycleHero(int direction)
        {
            if (g.InputManager != null)
            {
                var mode = g.InputManager.InputMode;
                if (mode == InputMode.AnyTarget || mode == InputMode.LinearTarget) return;
                if (g.InputManager.isDragging) return;
            }

            var heroes = g.Actors.Heroes.Where(h => h != null && h.IsPlaying).ToList();
            if (heroes.Count == 0) return;

            var current = g.Actors.SelectedActor != null && g.Actors.SelectedActor.IsHero
                ? g.Actors.SelectedActor
                : (g.TurnManager != null && g.TurnManager.ActiveActor != null && g.TurnManager.ActiveActor.IsHero
                    ? g.TurnManager.ActiveActor : heroes.First());

            int idx = heroes.IndexOf(current);
            if (idx < 0) idx = 0;
            int next = (idx + direction) % heroes.Count;
            if (next < 0) next += heroes.Count;

            var target = heroes[next];
            if (target == null) return;
            g.SelectionManager?.Select(target);

            // Select() clears every actor's focus indicator by design; re-show it on just the
            // cycled-to hero so the cycle buttons visibly mark who's now selected on the board.
            g.Actors.All?.ForEach(x => x?.Render?.SetFocusIndicatorEnabled(x == target));

            g.AudioManager?.Play("Click");
        }
        #endregion

        #region UI build helpers
        private static RectTransform MakeRect(string name, Transform parent)
        {
            var go = new GameObject(name, typeof(RectTransform));
            go.layer = LayerMask.NameToLayer("UI");
            var rt = (RectTransform)go.transform;
            rt.SetParent(parent, false);
            return rt;
        }

        private static Image MakeImage(string name, Transform parent, Color color)
        {
            var rt = MakeRect(name, parent);
            var img = rt.gameObject.AddComponent<Image>();
            img.color = color;
            return img;
        }

        private static TextMeshProUGUI MakeText(Transform parent, string fontKey, float size, TextAlignmentOptions align)
        {
            var rt = MakeRect("Text", parent);
            var tmp = rt.gameObject.AddComponent<TextMeshProUGUI>();
            var font = FontLibrary.Get(fontKey);
            if (font != null) tmp.font = font;
            tmp.fontSize = size;
            tmp.alignment = align;
            tmp.color = Color.white;
            tmp.raycastTarget = false;
            tmp.richText = true;
            return tmp;
        }

        private void MakeArrow(Transform parent, string glyph, Vector2 anchor, float x, Action onClick)
        {
            var bg = MakeImage($"Arrow{glyph}", parent, TabUnselected);
            var rt = (RectTransform)bg.transform;
            rt.anchorMin = anchor; rt.anchorMax = anchor; rt.pivot = new Vector2(1f, 0.5f);
            rt.sizeDelta = new Vector2(36f, TabBarHeight - 8f);
            rt.anchoredPosition = new Vector2(x, 0f);
            var btn = bg.gameObject.AddComponent<Button>();
            btn.targetGraphic = bg;
            btn.onClick.AddListener(() => onClick());
            var lbl = MakeText(bg.transform, "Outfit", 24f, TextAlignmentOptions.Center);
            Stretch((RectTransform)lbl.transform);
            lbl.text = glyph;
        }

        private static void Stretch(RectTransform rt)
        {
            rt.anchorMin = Vector2.zero; rt.anchorMax = Vector2.one; rt.pivot = new Vector2(0.5f, 0.5f);
            rt.offsetMin = Vector2.zero; rt.offsetMax = Vector2.zero;
        }

        /// <summary>Adds a 2px UiKit-style steel border (4 thin Images) to any rect.</summary>
        private static void AddBorder(RectTransform rt)
        {
            MakeEdgeImage(rt, "BorderTop",    new Vector2(0f,1f), new Vector2(1f,1f), new Vector2(0.5f,1f), new Vector2(0f,2f));
            MakeEdgeImage(rt, "BorderBottom", new Vector2(0f,0f), new Vector2(1f,0f), new Vector2(0.5f,0f), new Vector2(0f,2f));
            MakeEdgeImage(rt, "BorderLeft",   new Vector2(0f,0f), new Vector2(0f,1f), new Vector2(0f,0.5f), new Vector2(2f,0f));
            MakeEdgeImage(rt, "BorderRight",  new Vector2(1f,0f), new Vector2(1f,1f), new Vector2(1f,0.5f), new Vector2(2f,0f));
        }

        private static void MakeEdgeImage(RectTransform parent, string name,
            Vector2 aMin, Vector2 aMax, Vector2 pivot, Vector2 size)
        {
            var go = new GameObject(name, typeof(RectTransform));
            go.layer = LayerMask.NameToLayer("UI");
            var rt = (RectTransform)go.transform;
            rt.SetParent(parent, false);
            rt.anchorMin = aMin; rt.anchorMax = aMax; rt.pivot = pivot;
            rt.sizeDelta = size; rt.anchoredPosition = Vector2.zero;
            var img = go.AddComponent<Image>();
            img.color = HubTheme.PanelBorder;
            img.raycastTarget = false;
        }

        private static void SetAlpha(CanvasGroup cg, float a) { if (cg != null) cg.alpha = a; }
        #endregion
    }
}
