using TMPro;
using UnityEngine;
using UnityEngine.UI;
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
    /// HEROEXPERIENCEPANEFACTORY - Creates post-battle experience panes.
    /// 
    /// PURPOSE:
    /// Creates UI panes showing hero experience progress after battle.
    /// Each pane displays portrait, XP bar, and level up notifications.
    /// 
    /// VISUAL APPEARANCE:
    /// ```
    /// ┌─────────────────────────────────────────┐
    /// │ ┌────┐  Paladin           Level 5      │
    /// │ │    │  [████████████░░░░░░] +150 XP   │
    /// │ │ ⚔️ │  LEVEL UP!                      │
    /// │ └────┘                                  │
    /// └─────────────────────────────────────────┘
    /// ```
    /// 
    /// CREATED HIERARCHY:
    /// ```
    /// HeroExperiencePane (root)
    /// ├── Image (panel background)
    /// ├── HeroExperiencePane (behavior)
    /// ├── Portrait (hero image)
    /// ├── Name (hero name text)
    /// ├── Level (level text)
    /// ├── XPBar (Slider)
    /// │   ├── Background
    /// │   └── Fill Area → Fill
    /// ├── XPText (XP gained text)
    /// └── LevelUp (level up notification)
    /// ```
    /// 
    /// ANIMATION:
    /// XP bar animates from old to new value, showing progress.
    /// Level up text appears with animation when level threshold crossed.
    /// 
    /// CALLED BY:
    /// - PostBattleManager.BuildPanes()
    /// 
    /// RELATED FILES:
    /// - HeroExperiencePane.cs: Pane behavior/animation
    /// - PostBattleManager.cs: Post-battle screen
    /// - ExperienceTracker.cs: Tracks XP gains
    /// </summary>
    public static class HeroExperiencePaneFactory
    {
        /// <summary>Creates a new hero experience pane.</summary>
        public static GameObject Create(Transform parent = null)
        {
            // === ROOT: HeroExperiencePane ===
            var root = new GameObject("HeroExperiencePane");
            root.layer = 0;

            var rootRT = root.AddComponent<RectTransform>();
            rootRT.anchorMin = new Vector2(0f, 0.5f);
            rootRT.anchorMax = new Vector2(1f, 0.5f);
            rootRT.anchoredPosition = Vector2.zero;
            rootRT.sizeDelta = new Vector2(0f, 112f);
            rootRT.pivot = new Vector2(0.5f, 0.5f);

            root.AddComponent<CanvasRenderer>();

            var panelImage = root.AddComponent<Image>();
            panelImage.color = new Color(0.08f, 0.09f, 0.12f, 0.40f);
            panelImage.raycastTarget = true;
            panelImage.type = Image.Type.Sliced;

            // === CHILD: Portrait ===
            var portrait = new GameObject("Portrait");
            portrait.layer = 0;

            var portraitRT = portrait.AddComponent<RectTransform>();
            portraitRT.SetParent(rootRT, false);
            portraitRT.anchorMin = new Vector2(0f, 0.5f);
            portraitRT.anchorMax = new Vector2(0f, 0.5f);
            portraitRT.anchoredPosition = new Vector2(60f, 0f);
            portraitRT.sizeDelta = new Vector2(96f, 96f);
            portraitRT.pivot = new Vector2(0.5f, 0.5f);

            portrait.AddComponent<CanvasRenderer>();

            var portraitImage = portrait.AddComponent<Image>();
            portraitImage.color = Color.white;
            portraitImage.raycastTarget = true;
            portraitImage.preserveAspect = true;

            // === CHILD: Name ===
            var nameObj = new GameObject("Name");
            nameObj.layer = 0;

            var nameRT = nameObj.AddComponent<RectTransform>();
            nameRT.SetParent(rootRT, false);
            nameRT.anchorMin = new Vector2(0f, 0.5f);
            nameRT.anchorMax = new Vector2(0f, 0.5f);
            nameRT.anchoredPosition = new Vector2(120f, 32f);
            nameRT.sizeDelta = new Vector2(700f, 32f);
            nameRT.pivot = new Vector2(0f, 0.5f);

            nameObj.AddComponent<CanvasRenderer>();

            var nameTMP = nameObj.AddComponent<TextMeshProUGUI>();
            nameTMP.text = "Name";
            nameTMP.fontSize = 32;
            nameTMP.color = Color.white;
            nameTMP.alignment = TextAlignmentOptions.Left;
            nameTMP.raycastTarget = true;

            // === CHILD: Level ===
            var levelObj = new GameObject("Level");
            levelObj.layer = 0;

            var levelRT = levelObj.AddComponent<RectTransform>();
            levelRT.SetParent(rootRT, false);
            levelRT.anchorMin = new Vector2(1f, 0.5f);
            levelRT.anchorMax = new Vector2(1f, 0.5f);
            levelRT.anchoredPosition = new Vector2(-32f, 32f);
            levelRT.sizeDelta = new Vector2(100f, 32f);
            levelRT.pivot = new Vector2(1f, 0.5f);

            levelObj.AddComponent<CanvasRenderer>();

            var levelTMP = levelObj.AddComponent<TextMeshProUGUI>();
            levelTMP.text = "Lvl. 1";
            levelTMP.fontSize = 24;
            levelTMP.color = Color.white;
            levelTMP.alignment = TextAlignmentOptions.Right;
            levelTMP.raycastTarget = true;

            // === CHILD: XPBar ===
            var xpBar = new GameObject("XPBar");
            xpBar.layer = 0;

            var xpBarRT = xpBar.AddComponent<RectTransform>();
            xpBarRT.SetParent(rootRT, false);
            xpBarRT.anchorMin = new Vector2(0f, 0.5f);
            xpBarRT.anchorMax = new Vector2(1f, 0.5f);
            xpBarRT.anchoredPosition = new Vector2(44f, -32.4f);
            xpBarRT.sizeDelta = new Vector2(-152f, 32f);
            xpBarRT.pivot = new Vector2(0.5f, 0.5f);

            // === XPBar > Background ===
            var xpBg = new GameObject("Background");
            xpBg.layer = 0;

            var xpBgRT = xpBg.AddComponent<RectTransform>();
            xpBgRT.SetParent(xpBarRT, false);
            xpBgRT.anchorMin = Vector2.zero;
            xpBgRT.anchorMax = Vector2.one;
            xpBgRT.anchoredPosition = Vector2.zero;
            xpBgRT.sizeDelta = Vector2.zero;
            xpBgRT.pivot = new Vector2(0.5f, 0.5f);

            xpBg.AddComponent<CanvasRenderer>();

            var xpBgImage = xpBg.AddComponent<Image>();
            xpBgImage.color = new Color(0f, 0f, 0f, 0.6f);
            xpBgImage.raycastTarget = true;

            // === XPBar > Fill Area ===
            var fillArea = new GameObject("Fill Area");
            fillArea.layer = 0;

            var fillAreaRT = fillArea.AddComponent<RectTransform>();
            fillAreaRT.SetParent(xpBarRT, false);
            fillAreaRT.anchorMin = Vector2.zero;
            fillAreaRT.anchorMax = Vector2.one;
            fillAreaRT.anchoredPosition = Vector2.zero;
            fillAreaRT.sizeDelta = new Vector2(-8f, -8f);
            fillAreaRT.pivot = new Vector2(0.5f, 0.5f);

            // === Fill Area > Fill ===
            var fill = new GameObject("Fill");
            fill.layer = 0;

            var fillRT = fill.AddComponent<RectTransform>();
            fillRT.SetParent(fillAreaRT, false);
            fillRT.anchorMin = Vector2.zero;
            fillRT.anchorMax = Vector2.zero;
            fillRT.anchoredPosition = Vector2.zero;
            fillRT.sizeDelta = Vector2.zero;
            fillRT.pivot = new Vector2(0.5f, 0.5f);

            fill.AddComponent<CanvasRenderer>();

            var fillImage = fill.AddComponent<Image>();
            fillImage.color = new Color(1f, 0.8431373f, 0f, 1f); // Gold XP color
            fillImage.raycastTarget = true;

            // === Add Slider component ===
            var sliderComp = xpBar.AddComponent<Slider>();
            sliderComp.interactable = false; // Read-only XP bar
            sliderComp.targetGraphic = fillImage;
            sliderComp.fillRect = fillRT;
            sliderComp.handleRect = null;
            sliderComp.direction = Slider.Direction.LeftToRight;
            sliderComp.minValue = 0f;
            sliderComp.maxValue = 100f;
            sliderComp.wholeNumbers = true;
            sliderComp.value = 0f;

            // === CHILD: XPText ===
            var xpText = new GameObject("XPText");
            xpText.layer = 0;

            var xpTextRT = xpText.AddComponent<RectTransform>();
            xpTextRT.SetParent(rootRT, false);
            xpTextRT.anchorMin = new Vector2(0f, 0.5f);
            xpTextRT.anchorMax = new Vector2(1f, 0.5f);
            xpTextRT.anchoredPosition = new Vector2(44f, -32.4f);
            xpTextRT.sizeDelta = new Vector2(-152f, 32f);
            xpTextRT.pivot = new Vector2(0.5f, 0.5f);

            xpText.AddComponent<CanvasRenderer>();

            var xpTextTMP = xpText.AddComponent<TextMeshProUGUI>();
            xpTextTMP.text = "0 / 100";
            xpTextTMP.fontSize = 18;
            xpTextTMP.color = Color.white;
            xpTextTMP.alignment = TextAlignmentOptions.Center;
            xpTextTMP.raycastTarget = false;

            // === CHILD: LevelUp ===
            var levelUp = new GameObject("LevelUp");
            levelUp.layer = 0;
            levelUp.SetActive(false); // Hidden by default

            var levelUpRT = levelUp.AddComponent<RectTransform>();
            levelUpRT.SetParent(rootRT, false);
            levelUpRT.anchorMin = new Vector2(1f, 0.5f);
            levelUpRT.anchorMax = new Vector2(1f, 0.5f);
            levelUpRT.anchoredPosition = new Vector2(-32f, 0f);
            levelUpRT.sizeDelta = new Vector2(150f, 32f);
            levelUpRT.pivot = new Vector2(1f, 0.5f);

            levelUp.AddComponent<CanvasRenderer>();

            var levelUpTMP = levelUp.AddComponent<TextMeshProUGUI>();
            levelUpTMP.text = "LEVEL UP!";
            levelUpTMP.fontSize = 24;
            levelUpTMP.color = new Color(1f, 0.8431373f, 0f, 1f); // Gold
            levelUpTMP.alignment = TextAlignmentOptions.Right;
            levelUpTMP.fontStyle = FontStyles.Bold;
            levelUpTMP.raycastTarget = false;

            // === Add HeroExperiencePane component and wire fields ===
            var paneComp = root.AddComponent<HeroExperiencePane>();
            paneComp.Panel = panelImage;
            paneComp.Portrait = portraitImage;
            paneComp.NameLabel = nameTMP;
            paneComp.LevelLabel = levelTMP;
            paneComp.XPBar = sliderComp;
            paneComp.XPText = xpTextTMP;
            paneComp.LevelUpLabel = levelUpTMP;

            // Parent if specified
            if (parent != null)
            {
                rootRT.SetParent(parent, false);
            }

            return root;
        }
    }
}
