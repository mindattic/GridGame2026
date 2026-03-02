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
/// DEFEATANNOUNCEMENT - "Defeat" banner display.
/// 
/// PURPOSE:
/// Shows a centered defeat banner that floats down and fades in
/// when all heroes are defeated.
/// 
/// ANIMATION:
/// - Starts above center, transparent
/// - Floats downward while fading in
/// - Settles at center position
/// 
/// USAGE:
/// ```csharp
/// g.DefeatAnnouncement.Show();
/// ```
/// 
/// RELATED FILES:
/// - BattleLostSequence.cs: Triggers display
/// - VictoryAnnouncement.cs: Victory equivalent
/// </summary>
public class DefeatAnnouncement : MonoBehaviour
{
    // Units per second for downward floating movement.
    public float floatSpeed = 900f;

    // Vertical offset to start above the center.
    public float startOffsetY = 250f;

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
    private Vector2 startPos;
    private Vector2 centerPos = Vector2.zero;

    private void Awake()
    {
        // Resolve UI references via GameObjectHelper.
        root = GameObjectHelper.Game.DefeatAnnouncement.Root;
        image = GameObjectHelper.Game.DefeatAnnouncement.Image;
        back = GameObjectHelper.Game.DefeatAnnouncement.Back;
        front = GameObjectHelper.Game.DefeatAnnouncement.Front;
        rect = root != null ? root.GetComponent<RectTransform>() : GetComponent<RectTransform>();
    }

    private void Start()
    {
        // Set initial position above center and make fully transparent.
        if (rect != null)
        {
            startPos = centerPos + new Vector2(0f, Mathf.Abs(startOffsetY));
            rect.anchoredPosition = startPos;
        }
        SetLabelAlpha(0f);
    }

    /// <summary>
    /// Shows the Defeat banner (floats down from above while fading in). Only plays once.
    /// </summary>
    public void Show()
    {
        if (hasPlayed) return;
        hasPlayed = true;

        g.AudioManager.Play("Defeat");
        SetText("Defeat");
        RestartAnimation();
    }

    private void RestartAnimation()
    {
        // Do not restart if already animating
        if (animationRoutine != null)
            return;

        animationRoutine = StartCoroutine(AnimateFloatInRoutine());
    }

    private IEnumerator AnimateFloatInRoutine()
    {
        if (rect != null)
            rect.anchoredPosition = startPos;

        // Prepare fade in
        SetLabelAlpha(0f);

        float totalDist = rect != null ? Vector2.Distance(startPos, centerPos) : 1f;
        if (totalDist <= 0.01f) totalDist = 1f;

        while (rect != null && (rect.anchoredPosition - centerPos).sqrMagnitude > 0.25f)
        {
            // Move down towards center
            rect.anchoredPosition = Vector2.MoveTowards(rect.anchoredPosition, centerPos, floatSpeed * Time.deltaTime);

            // Fade in proportionally to progress
            float remaining = Vector2.Distance(rect.anchoredPosition, centerPos);
            float progress = Mathf.Clamp01(1f - (remaining / totalDist));
            float a = progress;
            SetLabelAlpha(a);

            yield return Wait.None();
        }

        if (rect != null)
            rect.anchoredPosition = centerPos;
        SetLabelAlpha(1f);

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
