using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Scripts.Helpers;
using System.Collections;
using Scripts.Libraries;
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
using Scripts.Utilities;

namespace Scripts.Canvas
{
/// <summary>
/// HEROEXPERIENCEPANE - Post-battle XP display for a hero.
/// 
/// PURPOSE:
/// Displays a single hero's experience gain after battle,
/// showing portrait, name, level, and animated XP bar fill.
/// 
/// VISUAL APPEARANCE:
/// ```
/// ┌──────────────────────────────┐
/// │ [Portrait]  Hero Name        │
/// │             Level 5          │
/// │             ████████░░ +150  │ ← XP bar animates
/// │             LEVEL UP!        │ ← Shows on level up
/// └──────────────────────────────┘
/// ```
/// 
/// ANIMATION:
/// - XP bar fills incrementally over time
/// - IsFillComplete tracks animation state
/// - LEVEL UP label appears when threshold crossed
/// 
/// LAYOUT:
/// - Auto-wires components via Reset()
/// - Uses constants for padding and sizing
/// - Panel color indicates highlight state
/// 
/// RELATED FILES:
/// - HeroExperiencePaneFactory.cs: Creates pane GameObjects
/// - PostBattleManager.cs: Manages all panes
/// - ExperienceTracker.cs: Provides XP data
/// </summary>
public class HeroExperiencePane : MonoBehaviour
{
    public Image Panel;
    public Image Portrait;
    public TextMeshProUGUI NameLabel;
    public TextMeshProUGUI LevelLabel;
    public Slider XPBar;
    public TextMeshProUGUI XPText;
    public TextMeshProUGUI LevelUpLabel;

    // Runtime
    public bool IsFillComplete;

    // Layout constants (tweak in prefab if desired)
    private const float PAD = 8f;
    private const float PORTRAIT_WIDTH = 64f; // assumed sprite scale roughly square
    private const float PORTRAIT_HEIGHT = 96f;
    private const float BAR_HEIGHT = 18f;

    // Fill pacing constants
    // Increased steps per level for more granular accumulation (was 50)
    private const int FILL_STEPS_PER_LEVEL = 255;          // target steps per level bar (more ticks)
    private const float FILL_STEP_DELAY = Interval.OneTick; // delay per step

    /// <summary>Auto-wires child component references by name for editor convenience.</summary>
    private void Reset()
    {
        // Auto-wire on add (editor convenience)
        if (!Panel) Panel = GetComponent<Image>();
        if (!Portrait) Portrait = transform.Find("Portrait")?.GetComponent<Image>();
        if (!NameLabel) NameLabel = transform.Find("Name")?.GetComponent<TextMeshProUGUI>();
        if (!LevelLabel) LevelLabel = transform.Find("Level")?.GetComponent<TextMeshProUGUI>();
        if (!XPBar) XPBar = transform.Find("XPBar")?.GetComponent<Slider>();
        if (!XPText) XPText = transform.Find("XPText")?.GetComponent<TextMeshProUGUI>();
        if (!LevelUpLabel) LevelUpLabel = transform.Find("LevelUp")?.GetComponent<TextMeshProUGUI>();

        // Ensure the XP bar is not interactable in editor when wiring
        MakeXPBarReadOnly();
    }

    /// <summary>Resolves component references and ensures the XP bar is non-interactive.</summary>
    private void Awake()
    {
        Reset();
        // Extra safety at runtime
        MakeXPBarReadOnly();
    }

    /// <summary>Populates the pane with hero data and starts the animated XP fill if XP was gained.</summary>
    public void Build(CharacterClass characterClass, int xpGained, bool highlight)
    {
        IsFillComplete = false;
        if (Panel)
            Panel.color = highlight ? new Color(0.12f, 0.25f, 0.45f, 0.55f) : new Color(0.08f, 0.09f, 0.12f, 0.40f);

        // Portrait
        if (Portrait)
        {
            var actor = ActorLibrary.Get(characterClass);
            if (actor != null && actor.Portrait != null)
            {
                Portrait.sprite = actor.Portrait;
                Portrait.preserveAspect = true;
            }
        }

        // Save lookup (derive level/current from TotalXP)
        int level = 1, currentXP = 0;
        var save = ProfileHelper.CurrentProfile?.CurrentSave;
        var entry = save.Party.Members.Find(m => m.CharacterClass == characterClass) 
            ?? save?.Roster?.Members?.Find(m => m.CharacterClass == characterClass);
        if (entry != null)
        {
            var derived = ExperienceHelper.DeriveFromTotalXP(Mathf.Max(0, entry.TotalXP));
            level = Mathf.Max(1, derived.level);
            currentXP = Mathf.Max(0, derived.currentXP);
        }
        int needed = ExperienceHelper.NextLevel(level);

        if (NameLabel) NameLabel.text = characterClass.ToString();
        if (LevelLabel) LevelLabel.text = $"Lvl. {level}";
        if (XPBar)
        {
            XPBar.minValue = 0; XPBar.maxValue = needed; XPBar.wholeNumbers = true; XPBar.value = Mathf.Clamp(currentXP, 0, needed);
            // Ensure the bar is not user-interactive even if prefab defaults differ
            MakeXPBarReadOnly();

            // Force fill to render on top and fully opaque to avoid looking dark
            var fillArea = XPBar.transform.Find("Fill Area");
            var background = XPBar.transform.Find("Background");
            if (fillArea != null)
            {
                // Ensure Fill Area renders after Background
                if (background != null && fillArea.GetSiblingIndex() < background.GetSiblingIndex())
                    fillArea.SetSiblingIndex(background.GetSiblingIndex() + 1);

                var fillImg = fillArea.Find("Fill")?.GetComponent<Image>();
                if (fillImg != null)
                {
                    var c = fillImg.color; c.a = 1f; fillImg.color = c; // full opacity
                    fillImg.maskable = false; // avoid unintended mask tinting
                    // Ensure default UI material
                    if (fillImg.material != null) fillImg.material = null;
                }
            }
        }
        if (XPText) XPText.text = $"EXP: {currentXP} / {needed} (+{xpGained})";
        if (LevelUpLabel) LevelUpLabel.color = new Color(1f, 0.95f, 0.3f, 0f);

        // Start animation if needed
        if (xpGained > 0) StartCoroutine(FillRoutine(level, currentXP, xpGained)); else IsFillComplete = true;

        // Defer layout one frame so parent layout (width) established
       // StartCoroutine(DeferredLayout());
    }

    /// <summary>Disables interaction, navigation, and transition on the XP bar slider.</summary>
    private void MakeXPBarReadOnly()
    {
        if (!XPBar) return;
        XPBar.interactable = false; // blocks pointer/controller interaction
        // Disable navigation so it cannot be focused via keyboard/gamepad
        var nav = new Navigation { mode = Navigation.Mode.None };
        XPBar.navigation = nav;
        // Remove visual transition highlights
        XPBar.transition = Selectable.Transition.None;
    }

    /// <summary>Waits one frame for parent layout sizing before applying layout.</summary>
    private IEnumerator DeferredLayout()
    {
        yield return null; // wait 1 frame for layout sizing
        //ApplyLayout();
    }

    /// <summary>Positions portrait, name, level, XP bar, and level-up labels within the pane.</summary>
    private void ApplyLayout()
    {
        var rt = (RectTransform)transform;
        float width = rt.rect.width;
        // Portrait anchored FAR RIGHT
        if (Portrait)
        {
            var pr = (RectTransform)Portrait.transform;
            pr.anchorMin = pr.anchorMax = new Vector2(1f, 1f);
            pr.pivot = new Vector2(1f, 1f);
            pr.sizeDelta = new Vector2(PORTRAIT_WIDTH, PORTRAIT_HEIGHT);
            pr.anchoredPosition = new Vector2(-PAD, -PAD);
        }

        // Name top-right, immediately LEFT of portrait
        float textRightInset = PORTRAIT_WIDTH + PAD * 2f;
        if (NameLabel)
        {
            var nr = (RectTransform)NameLabel.transform;
            nr.anchorMin = nr.anchorMax = new Vector2(1f, 1f);
            nr.pivot = new Vector2(1f, 1f);
            nr.anchoredPosition = new Vector2(-textRightInset, -PAD);
            NameLabel.alignment = TextAlignmentOptions.Right;
        }
        if (LevelLabel)
        {
            var lr = (RectTransform)LevelLabel.transform;
            lr.anchorMin = lr.anchorMax = new Vector2(1f, 1f);
            lr.pivot = new Vector2(1f, 1f);
            lr.anchoredPosition = new Vector2(-textRightInset, -(PAD + 20f));
            LevelLabel.alignment = TextAlignmentOptions.Right;
        }

        // XP Text far left (still right-aligned) or far right? Requirement: far right right-aligned.
        if (XPText)
        {
            var xr = (RectTransform)XPText.transform;
            xr.anchorMin = xr.anchorMax = new Vector2(0f, 1f);
            xr.pivot = new Vector2(0f, 1f);
            xr.anchoredPosition = new Vector2(PAD, -PAD);
            XPText.alignment = TextAlignmentOptions.Left;
        }

        // XP Bar along bottom full width minus padding
        if (XPBar)
        {
            var br = (RectTransform)XPBar.transform;
            br.anchorMin = new Vector2(0f, 0f);
            br.anchorMax = new Vector2(1f, 0f);
            br.pivot = new Vector2(0.5f, 0f);
            br.sizeDelta = new Vector2(-PAD * 2f, BAR_HEIGHT);
            br.anchoredPosition = new Vector2(0f, PAD);
        }

        if (LevelUpLabel)
        {
            var ur = (RectTransform)LevelUpLabel.transform;
            ur.anchorMin = ur.anchorMax = new Vector2(0.5f, 0f);
            ur.pivot = new Vector2(0.5f, 0f);
            ur.anchoredPosition = new Vector2(0f, BAR_HEIGHT + PAD * 1.5f);
            LevelUpLabel.alignment = TextAlignmentOptions.Center;
        }
    }

    /// <summary>Incrementally fills the XP bar, triggering level-up flashes when thresholds are crossed.</summary>
    private IEnumerator FillRoutine(int level, int currentXP, int gained)
    {
        if (!XPBar)
        {
            IsFillComplete = true; yield break;
        }
        int needed = ExperienceHelper.NextLevel(level);
        int cur = currentXP;
        int remaining = gained;
        while (remaining > 0)
        {
            // Compute a step based on desired steps-per-level pacing
            int stepPerLevel = Mathf.Max(1, Mathf.CeilToInt((float)needed / FILL_STEPS_PER_LEVEL));
            int step = Mathf.Min(remaining, stepPerLevel);

            cur += step; remaining -= step;
            if (cur >= needed)
            {
                cur -= needed; level++; needed = ExperienceHelper.NextLevel(level); XPBar.maxValue = needed;
                if (LevelLabel) LevelLabel.text = $"Lvl. {level}";
                if (LevelUpLabel) StartCoroutine(FlashLevelUp());
            }
            XPBar.value = cur;
            if (XPText) XPText.text = $"EXP: {cur} / {needed} (+{gained})";
            yield return Wait.For(FILL_STEP_DELAY);
        }
        IsFillComplete = true;
    }

    /// <summary>Briefly flashes the "LEVEL UP" label with a pulsing yellow glow.</summary>
    private IEnumerator FlashLevelUp()
    {
        if (!LevelUpLabel) yield break;
        float t = 0f;
        while (t < 1f)
        {
            t += Time.deltaTime * 3f;
            LevelUpLabel.color = new Color(1f, 0.95f, 0.3f, Mathf.PingPong(t, 0.7f));
            yield return null;
        }
        LevelUpLabel.color = new Color(1f, 0.95f, 0.3f, 0f);
    }
}

}
