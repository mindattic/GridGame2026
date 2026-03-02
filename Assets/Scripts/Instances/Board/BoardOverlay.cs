using Scripts.Helpers;
using System.Collections;
using UnityEngine;
using Scripts.Canvas;
using Scripts.Data.Actor;
using Scripts.Data.Items;
using Scripts.Data.Skills;
using Scripts.Effects;
using Scripts.Factories;
using Scripts.Hub;
using Scripts.Instances;
using Scripts.Instances.Actor;
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

namespace Scripts.Instances.Board
{
/// <summary>
/// BOARDOVERLAY - Semi-transparent overlay for board dimming.
/// 
/// PURPOSE:
/// Provides a fade-able overlay sprite that dims the board during
/// special sequences like ability targeting or cutscenes.
/// 
/// VISUAL EFFECT:
/// ```
/// Normal:            With Overlay:
/// [Board visible]    [Board dimmed]
///                    ████████████
/// ```
/// 
/// OPERATIONS:
/// - Show(): Instantly display overlay at max alpha
/// - Hide(): Instantly hide overlay (alpha = 0)
/// - FadeInRoutine(): Smooth fade to visible
/// - FadeOutRoutine(): Smooth fade to transparent
/// 
/// CONFIGURATION:
/// - fadeDuration: Time for fade transitions
/// - minAlpha: Fully transparent value
/// - maxAlpha: Maximum opacity value
/// - overlayColor: Tint color for overlay
/// 
/// RELATED FILES:
/// - BoardManager.cs: Uses for board dimming
/// - TargetModeOverlay.cs: Similar targeting overlay
/// </summary>
[RequireComponent(typeof(SpriteRenderer))]
public class BoardOverlay : MonoBehaviour
{
    private SpriteRenderer spriteRenderer;
    private Coroutine runningCoroutine;

    private float fadeDuration = 0.25f; // Duration of overlay effect
    private float minAlpha = Opacity.Transparent; // Fully transparent
    private float maxAlpha = Opacity.Translucent.Alpha196; // Maximum opacity
    private Color overlayColor = ColorHelper.Translucent.DarkBlack; // Default color

    private void Awake()
    {
        spriteRenderer = GetComponent<SpriteRenderer>();
        //spriteRenderer.sortingOrder = SortingOrder.BoardOverlay;
        spriteRenderer.enabled = false;
        SetAlpha(minAlpha);
    }

    /// <summary>
    /// Instantly hides the overlay (fully transparent).
    /// </summary>
    public void Hide()
    {
        SetAlpha(minAlpha);
        spriteRenderer.enabled = false;
    }

    /// <summary>
    /// Instantly shows the overlay (fully visible).
    /// </summary>
    public void Show()
    {
        SetAlpha(maxAlpha);
        spriteRenderer.enabled = true;
    }

    public IEnumerator FadeInRoutine()
    {
        if (runningCoroutine != null)
            StopCoroutine(runningCoroutine);
        yield return runningCoroutine = StartCoroutine(FadeRoutine(maxAlpha, true));
    }

    public IEnumerator FadeOutRoutine()
    {
        if (runningCoroutine != null)
            StopCoroutine(runningCoroutine);
        yield return runningCoroutine = StartCoroutine(FadeRoutine(minAlpha, false));
    }


    /// <summary>
    /// Handles the overlay transition over time.
    /// </summary>
    private IEnumerator FadeRoutine(float targetAlpha, bool enableOnStart)
    {
        if (enableOnStart)
            spriteRenderer.enabled = true;

        float startAlpha = spriteRenderer.color.a;
        float elapsedTime = 0f;

        while (elapsedTime < fadeDuration)
        {
            elapsedTime += Time.deltaTime;
            float newAlpha = Mathf.Lerp(startAlpha, targetAlpha, elapsedTime / fadeDuration);
            SetAlpha(newAlpha);
            yield return Wait.None();
        }

        SetAlpha(targetAlpha);

        if (!enableOnStart)
            spriteRenderer.enabled = false;
    }

    /// <summary>
    /// Sets the alpha of the overlay instantly.
    /// </summary>
    private void SetAlpha(float alpha)
    {
        overlayColor.a = alpha;
        spriteRenderer.color = overlayColor;
    }
}

}
