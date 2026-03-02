using System.Collections;
using UnityEngine;
using TMPro;
using Scripts.Helpers;
using UnityEngine.UI;
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
/// VICTORYANNOUNCEMENT - "Victory!" banner display.
/// 
/// PURPOSE:
/// Shows a centered victory banner that slides in from off-screen
/// when all enemies are defeated.
/// 
/// ANIMATION:
/// - Starts off-screen (above viewport)
/// - Slides down to center
/// - Holds position
/// 
/// USAGE:
/// ```csharp
/// g.VictoryAnnouncement.Show();
/// ```
/// 
/// RELATED FILES:
/// - BattleWonSequence.cs: Triggers display
/// - DefeatAnnouncement.cs: Defeat equivalent
/// - WaveAnnouncement.cs: Wave start display
/// </summary>
public class VictoryAnnouncement : MonoBehaviour
{
    // Units per second for sliding movement.
    public float slideSpeed = 1200f;

    // Optional hold (not strictly required to slide back out in current flow).
    public float holdDuration = 0f;

    // Track the currently running animation to prevent overlapping.
    private Coroutine animationRoutine;

    // Ensure this plays at most once per session.
    private bool hasPlayed = false;

    // Cached references from GameObjectHelper
    private GameObject root;
    private Image image;
    private TextMeshProUGUI back;
    private TextMeshProUGUI front;

    private RectTransform rect;
    private Vector2 offscreenStart;   // computed in Start
    private Vector2 centerPos = Vector2.zero;

    private void Awake()
    {
        // Resolve labels using GameObjectHelper strongly-typed paths.
        root = GameObjectHelper.Game.VictoryAnnouncement.Root;
        image = GameObjectHelper.Game.VictoryAnnouncement.Image;
        back = GameObjectHelper.Game.VictoryAnnouncement.Back;
        front = GameObjectHelper.Game.VictoryAnnouncement.Front;
        rect = root != null ? root.GetComponent<RectTransform>() : GetComponent<RectTransform>();
    }

    private void Start()
    {
        // Compute an off-screen start position (above the viewport) based on canvas height.
        var canvas = GetComponentInParent<UnityEngine.Canvas>();
        var canvasRect = canvas != null ? canvas.GetComponent<RectTransform>() : null;
        float height = canvasRect != null ? canvasRect.rect.height : Screen.height;
        float y = (height * 0.5f) + (rect != null ? rect.rect.height : 0f) + 50f; // a bit past the top
        offscreenStart = new Vector2(0f, y);

        if (rect != null)
            rect.anchoredPosition = offscreenStart;

        // Keep object active; hide by alpha initially
        SetLabelAlpha(0f);
    }

    /// <summary>
    /// Shows the Victory banner (slides in from off-screen to center). Only plays once.
    /// </summary>
    public void Show()
    {
        if (hasPlayed) return;
        hasPlayed = true;

        g.AudioManager.Play("Victory");
        SetText("Victory!");
        SetLabelAlpha(1f);
        RestartAnimation();
    }

    private void RestartAnimation()
    {
        // Do not restart if already animating
        if (animationRoutine != null)
            return;

        animationRoutine = StartCoroutine(AnimateSlideInRoutine());
    }

    private IEnumerator AnimateSlideInRoutine()
    {
        // Ensure we start off-screen at the top edge
        if (rect != null)
            rect.anchoredPosition = offscreenStart;

        // Slide into view towards the center
        while (rect != null && (rect.anchoredPosition - centerPos).sqrMagnitude > 0.25f)
        {
            rect.anchoredPosition = Vector2.MoveTowards(rect.anchoredPosition, centerPos, slideSpeed * Time.deltaTime);
            yield return Wait.None();
        }

        if (rect != null)
            rect.anchoredPosition = centerPos;

        // Optional hold (no auto slide out currently)
        if (holdDuration > 0f)
            yield return new WaitForSeconds(holdDuration);

        animationRoutine = null;
    }

    // ------------------------------------------------------------------------
    // Helpers
    // ------------------------------------------------------------------------

    private void SetText(string value)
    {
        if (back != null) back.text = value;
        if (front != null) front.text = value;
    }

    private void SetLabelAlpha(float a)
    {
        if (image != null)
        {
            var c = image.color;
            c.a = a;
            image.color = c;
        }

        if (back != null)
        {
            var c = back.color;
            c.a = a;
            back.color = c;
        }

        if (front != null)
        {
            var c = front.color;
            c.a = a;
            front.color = c;
        }
    }
}

}
