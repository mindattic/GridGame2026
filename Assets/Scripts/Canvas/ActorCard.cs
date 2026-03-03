using Scripts.Helpers;
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
/// ACTORCARD - Hero detail card display.
/// 
/// PURPOSE:
/// Displays a detailed card for the selected hero showing
/// portrait, name, stats, and abilities.
/// 
/// VISUAL APPEARANCE:
/// ```
/// ┌────────────────────────┐
/// │  [Portrait]  Title     │
/// │              ────────  │
/// │              Details   │
/// │              HP: 100   │
/// │              ATK: 25   │
/// └────────────────────────┘
/// ```
/// 
/// FEATURES:
/// - Slide-in/out animations
/// - Portrait bounce effect
/// - Fade transitions for elements
/// - Dynamic stat display
/// 
/// ACCESS: g.Card
/// 
/// RELATED FILES:
/// - ActorInstance.cs: Selected actor data
/// - InputManager.cs: Selection events
/// </summary>
public class ActorCard : MonoBehaviour
{
    #region Cached Components

    private RectTransform card;
    private RectTransform backdrop;
    private RectTransform portrait;
    private RectTransform title;
    private RectTransform details;

    private CanvasGroup backdropCG;
    private CanvasGroup titleCG;
    private CanvasGroup detailsCG;
    private CanvasGroup portraitCG;

    #endregion

    #region Animation State

    private Vector3 offscreenPosition;
    private Vector3 destination;
    private AnimationCurve slideInCurve;
    private float slideDuration;

    private const float FixedPortraitSize = 1024f;
    private const float PortraitPosX = -128f;
    private const float PortraitPosY = -128f;

    #endregion

    #region Initialization

    /// <summary>Caches UI references, initializes animation curves, computes layout, and clears card.</summary>
    private void Awake()
    {
        card = GameObjectHelper.Game.Card.Rect;
        backdrop = GameObjectHelper.Game.Card.Backdrop;
        portrait = GameObjectHelper.Game.Card.Portrait;
        title = GameObjectHelper.Game.Card.Title;
        details = GameObjectHelper.Game.Card.Details;

        backdropCG = EnsureCanvasGroup(backdrop);
        titleCG = EnsureCanvasGroup(title);
        detailsCG = EnsureCanvasGroup(details);
        portraitCG = EnsureCanvasGroup(portrait);

        slideInCurve = AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);
        slideDuration = 0.25f; // was 0.5f, 2x faster

        RecomputeLayout();
        Clear();

        GameReady.Begin(this);
    }

    /// <summary>Recomputes layout after the first frame to account for canvas sizing.</summary>
    private void Start()
    {
        RecomputeLayout();
    }

    /// <summary>Recalculates layout when the card's RectTransform dimensions change.</summary>
    private void OnRectTransformDimensionsChange()
    {
        RecomputeLayout();
    }

    /// <summary>Populates the card with the currently selected actor's portrait, name, and stats.</summary>
    public void Assign()
    {
        if (!g.Actors.HasSelectedActor) return;

        var characterClass = g.Actors.SelectedActor.characterClass;
        var actorData = ActorLibrary.Get(characterClass);

        // Always visible
        backdrop.gameObject.SetActive(true);
        portrait.gameObject.SetActive(true);

        StopAllCoroutines();

        // Set content immediately
        portrait.GetComponent<Image>().sprite = actorData.Portrait;
        title.GetComponent<TextMeshProUGUI>().text = characterClass.ToString();



        // Gather stats for display
        var s = g.Actors.SelectedActor.Stats;
        int lvl = Mathf.RoundToInt(s.Level);
        int hp = Mathf.RoundToInt(s.HP);
        int maxHp = Mathf.RoundToInt(s.MaxHP);
        int atk = Mathf.FloorToInt(Formulas.Offense(s, 0f));
        int def = Mathf.FloorToInt(Formulas.Defense(s, 0f));
        int spd = Mathf.FloorToInt(s.Speed);
        //int crit = Mathf.RoundToInt(Formulas.CriticalHitPercent(actor, actor)); // opponent ignored in simplified model

        // Build a compact card line. Uses fixed-width-ish spacing; tweak as needed for your TMP font.
        var sb = new System.Text.StringBuilder();
        sb.AppendLine("LVL   HP         ATK  DEF  SPD");
        sb.AppendLine($"{lvl,-5} {hp}/{maxHp,-8} {atk,3}  {def,3}  {spd,3}");

        details.GetComponent<TextMeshProUGUI>().text = sb.ToString();





        // Ensure full visibility and placement
        portrait.anchoredPosition = new Vector2(PortraitPosX, PortraitPosY);
        portrait.sizeDelta = new Vector2(FixedPortraitSize, FixedPortraitSize);
        SetAlpha(backdropCG, 1f);
        SetAlpha(portraitCG, 1f);
        SetAlpha(titleCG, 1f);
        SetAlpha(detailsCG, 1f);
    }

    /// <summary>Immediately sets all card elements to full visibility without animation.</summary>
    private void SlideIn()
    {
        // Immediate ensure visible; no fade/slide
        StopAllCoroutines();
        SetAlpha(backdropCG, 1f);
        SetAlpha(portraitCG, 1f);
        SetAlpha(titleCG, 1f);
        SetAlpha(detailsCG, 1f);
        portrait.anchoredPosition = new Vector2(PortraitPosX, PortraitPosY);
        portrait.sizeDelta = new Vector2(FixedPortraitSize, FixedPortraitSize);
    }

    /// <summary>Coroutine that ensures full visibility immediately (animation removed).</summary>
    private IEnumerator SlideInRoutine(bool fadeText)
    {
        // No animation anymore; ensure fully visible immediately
        portrait.anchoredPosition = new Vector2(PortraitPosX, PortraitPosY);
        portrait.sizeDelta = new Vector2(FixedPortraitSize, FixedPortraitSize);
        SetAlpha(backdropCG, 1f);
        SetAlpha(portraitCG, 1f);
        SetAlpha(titleCG, 1f);
        SetAlpha(detailsCG, 1f);
        yield break;
    }

    /// <summary>Keeps all card elements visible (slide-out animation removed).</summary>
    public void SlideOut()
    {
        // No-op: keep visible
        StopAllCoroutines();
        SetAlpha(backdropCG, 1f);
        SetAlpha(portraitCG, 1f);
        SetAlpha(titleCG, 1f);
        SetAlpha(detailsCG, 1f);
        portrait.anchoredPosition = new Vector2(PortraitPosX, PortraitPosY);
        portrait.sizeDelta = new Vector2(FixedPortraitSize, FixedPortraitSize);
        backdrop.gameObject.SetActive(true);
        portrait.gameObject.SetActive(true);
    }

    /// <summary>Coroutine that maintains visibility (slide-out animation removed).</summary>
    private IEnumerator SlideOutRoutine()
    {
        // No-op animation; maintain visibility
        portrait.anchoredPosition = new Vector2(PortraitPosX, PortraitPosY);
        portrait.sizeDelta = new Vector2(FixedPortraitSize, FixedPortraitSize);
        SetAlpha(backdropCG, 1f);
        SetAlpha(titleCG, 1f);
        SetAlpha(detailsCG, 1f);
        SetAlpha(portraitCG, 1f);
        yield break;
    }

    /// <summary>Resets the card to a blank state, clearing title and detail text.</summary>
    public void Clear()
    {
        StopAllCoroutines();

        // Keep everything active and visible
        backdrop.gameObject.SetActive(true);
        portrait.gameObject.SetActive(true);

        title.GetComponent<TextMeshProUGUI>().text = "";
        details.GetComponent<TextMeshProUGUI>().text = "";

        // Enforce portrait position and size
        portrait.anchoredPosition = new Vector2(PortraitPosX, PortraitPosY);
        portrait.sizeDelta = new Vector2(FixedPortraitSize, FixedPortraitSize);

        SetAlpha(backdropCG, 1f);
        SetAlpha(titleCG, 1f);
        SetAlpha(detailsCG, 1f);
        SetAlpha(portraitCG, 1f);
    }

    /// <summary>Returns the portrait's world position converted from canvas coordinates.</summary>
    public Vector3 PortraitWorldPosition()
    {
        return UnitConversionHelper.Canvas.ToWorld(portrait.transform);
    }

    /// <summary>Plays a vertical bounce animation on the portrait image.</summary>
    public void BouncePortrait(float percentOfScreenHeight = 0.03f, float bounceDuration = 0.3333f)
    {
        float bounceDistance = Screen.height * percentOfScreenHeight;
        StartCoroutine(BouncePortraitRoutine(bounceDistance, bounceDuration));
    }

    /// <summary>Coroutine that moves the portrait up then back down with smooth easing.</summary>
    private IEnumerator BouncePortraitRoutine(float bounceDistance, float bounceDuration)
    {
        Vector2 originalPos = portrait.anchoredPosition;
        Vector2 upPos = originalPos + Vector2.up * bounceDistance;
        float half = bounceDuration * 0.5f;
        float elapsed = 0f;

        while (elapsed < half)
        {
            float t = elapsed / half;
            portrait.anchoredPosition = Vector2.Lerp(originalPos, upPos, Mathf.SmoothStep(0f, 1f, t));
            elapsed += Time.deltaTime;
            yield return Wait.None();
        }
        portrait.anchoredPosition = upPos;

        elapsed = 0f;
        while (elapsed < half)
        {
            float t = elapsed / half;
            portrait.anchoredPosition = Vector2.Lerp(upPos, originalPos, Mathf.SmoothStep(0f, 1f, t));
            elapsed += Time.deltaTime;
            yield return Wait.None();
        }
        portrait.anchoredPosition = originalPos;
    }

    /// <summary>Immediately swaps the card's portrait, title, and details without animation.</summary>
    private IEnumerator QuickSwapRoutine(Sprite newSprite, string newTitle, string newDetails, bool fadeText)
    {
        // No fade or slide: swap immediately and stay visible
        portrait.anchoredPosition = new Vector2(PortraitPosX, PortraitPosY);
        portrait.sizeDelta = new Vector2(FixedPortraitSize, FixedPortraitSize);

        portrait.GetComponent<Image>().sprite = newSprite;
        title.GetComponent<TextMeshProUGUI>().text = newTitle;
        details.GetComponent<TextMeshProUGUI>().text = newDetails;

        SetAlpha(titleCG, 1f);
        SetAlpha(detailsCG, 1f);
        SetAlpha(backdropCG, 1f);
        SetAlpha(portraitCG, 1f);
        yield break;
    }

    /// <summary>Recalculates portrait size and offscreen/destination positions based on current screen size.</summary>
    private void RecomputeLayout()
    {
        if (card == null || portrait == null) return;

        // Set fixed portrait size and position
        portrait.sizeDelta = new Vector2(FixedPortraitSize, FixedPortraitSize);

        // Maintain fixed destination
        destination = new Vector3(PortraitPosX, PortraitPosY, 0f);

        // Keep an offscreen position defined (unused for now but harmless)
        float width = FixedPortraitSize;
        offscreenPosition = new Vector3(width, 0f, 0f);
    }

    /// <summary>Ensure canvas group.</summary>
    private static CanvasGroup EnsureCanvasGroup(RectTransform target)
    {
        var cg = target.GetComponent<CanvasGroup>();
        if (cg == null) cg = target.gameObject.AddComponent<CanvasGroup>();
        return cg;
    }

    /// <summary>Sets the alpha.</summary>
    private static void SetAlpha(CanvasGroup cg, float a)
    {
        if (cg != null) cg.alpha = a;
    }

    /// <summary>Resets component to default values (editor only).</summary>
    private static void Reset(CanvasGroup cg)
    {
        if (cg == null) return;
        cg.alpha = 0f;
        cg.interactable = false;
        cg.blocksRaycasts = false;
    }

    /// <summary>Approximately vector2.</summary>
    private static bool ApproximatelyVector2(Vector2 a, Vector2 b, float tol = 0.5f)
    {
        return Mathf.Abs(a.x - b.x) <= tol && Mathf.Abs(a.y - b.y) <= tol;
    }

    // --------------------------------------------------------------------------------------------
    // UI: Arrow buttons to cycle focused hero
    // --------------------------------------------------------------------------------------------

    /// <summary>Cycle hero.</summary>
    private void CycleHero(int direction)
    {
        // Block during ability targeting flows to avoid conflicting UI states
        if (g.InputManager != null)
        {
            var mode = g.InputManager.InputMode;
            if (mode == InputMode.AnyTarget || mode == InputMode.LinearTarget)
                return;
            // Ignore while dragging a selected hero
            if (g.InputManager.isDragging)
                return;
        }

        var heroes = g.Actors.Heroes.Where(h => h != null && h.IsPlaying).ToList();
        if (heroes.Count == 0) return;

        // Choose a baseline current hero
        var current = g.Actors.SelectedActor != null && g.Actors.SelectedActor.IsHero
            ? g.Actors.SelectedActor
            : (g.TurnManager != null && g.TurnManager.ActiveActor != null && g.TurnManager.ActiveActor.IsHero
                ? g.TurnManager.ActiveActor
                : heroes.First());

        int idx = heroes.IndexOf(current);
        if (idx < 0) idx = 0;

        int next = (idx + direction) % heroes.Count;
        if (next < 0) next += heroes.Count;

        var target = heroes[next];
        if (target == null) return;

        g.SelectionManager?.Select(target);
        g.AudioManager?.Play("Click");
    }

    // Bind these in the Inspector to Card/ArrowLeft and Card/ArrowRight buttons
    /// <summary>Handles the previous hero arrow click event.</summary>
    public void OnPreviousHeroArrowClick()
    {
        CycleHero(-1);
    }

    /// <summary>Handles the next hero arrow click event.</summary>
    public void OnNextHeroArrowClick()
    {
        CycleHero(1);
    }

    #endregion
}

}
