using Scripts.Helpers;
using Scripts.Libraries;
using System.Collections;
using UnityEngine;
using UnityEngine.UI;
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
/// FADEOVERLAYINSTANCE - Full-screen fade transition overlay.
/// 
/// PURPOSE:
/// Controls a full-screen black Image used for scene transition
/// fade effects (fade to black, fade from black).
/// 
/// VISUAL EFFECT:
/// ```
/// Fade Out (to black):
/// [Scene Visible] → [Darkening...] → [Solid Black]
/// 
/// Fade In (from black):
/// [Solid Black] → [Lightening...] → [Scene Visible]
/// ```
/// 
/// OPERATIONS:
/// - FadeIn(): Black → Transparent (reveal scene)
/// - FadeOut(): Transparent → Black (hide scene)
/// - Show(): Instant black
/// - Hide(): Instant transparent
/// 
/// CHAINING:
/// Each operation accepts an optional IEnumerator that runs
/// after the fade completes, enabling sequenced operations.
/// 
/// USAGE:
/// ```csharp
/// // Fade out, load scene, fade in
/// overlay.FadeOut(LoadSceneAndFadeIn());
/// 
/// // Simple fade in on scene start
/// overlay.FadeIn();
/// ```
/// 
/// RELATED FILES:
/// - FadeOverlayHelper.cs: Cached access to overlay
/// - SceneHelper.cs: Uses for scene transitions
/// - FadeOverlayFactory.cs: Creates overlay prefab
/// </summary>
public class FadeOverlayInstance : MonoBehaviour
{
    #region Configuration

    private Image image;
    private float fadeDuration = 0.25f;

    #endregion

    #region Initialization

    /// <summary>Caches the Image component, assigns the black sprite, and sets initial full-opaque state.</summary>
    private void Awake()
    {
        image = GetComponent<Image>();
        image.sprite = SpriteLibrary.Sprites["Black32x32"];
        SetAlpha(1f);
    }

    #endregion

    #region Fade Operations

    /// <summary>
    /// Fades from black to transparent over fadeDuration, then runs the optional routine.
    /// </summary>
    public void FadeIn(IEnumerator routine = null) => StartCoroutine(FadeInRoutine(routine));

    /// <summary>Gradually reduces alpha from 1 to 0, then runs the optional chained routine.</summary>
    private IEnumerator FadeInRoutine(IEnumerator routine = null)
    {
        SetAlpha(1f);
        float elapsedTime = 0f;

        while (elapsedTime < fadeDuration)
        {
            elapsedTime += Time.deltaTime;
            float alpha = 1f - Mathf.Clamp01(elapsedTime / fadeDuration);
            SetAlpha(alpha);
            yield return Wait.OneTick();
        }

        SetAlpha(0f);

        if (routine != null)
            yield return routine;
    }

    /// <summary>
    /// Fades from transparent to black over fadeDuration, then runs the optional routine.
    /// </summary>
    public void FadeOut(IEnumerator routine = null) => StartCoroutine(FadeOutRoutine(routine));

    /// <summary>Gradually increases alpha from 0 to 1, then runs the optional chained routine.</summary>
    private IEnumerator FadeOutRoutine(IEnumerator routine = null)
    {
        // Start fully transparent
        SetAlpha(0f);
        float elapsedTime = 0f;

        // Increase alpha to 1 over time
        while (elapsedTime < fadeDuration)
        {
            elapsedTime += Time.deltaTime;
            float alpha = Mathf.Clamp01(elapsedTime / fadeDuration);
            SetAlpha(alpha);
            // IMPORTANT: yield a real frame so Time.deltaTime advances.
            yield return Wait.OneTick();
        }

        // Ensure fully opaque
        SetAlpha(1f);

        // Run additional routine (if provided)
        if (routine != null)
            yield return routine;
    }

    /// <summary>
    /// Immediately makes the overlay transparent, then runs the optional routine.
    /// </summary>
    public void Show(IEnumerator routine = null) => StartCoroutine(ShowRoutine(routine));

    /// <summary>Sets overlay to transparent immediately, then runs the optional chained routine.</summary>
    private IEnumerator ShowRoutine(IEnumerator routine = null)
    {
        SetAlpha(0f);

        if (routine != null)
            yield return routine;
    }

    /// <summary>
    /// Immediately makes the overlay opaque, then runs the optional routine.
    /// </summary>
    public void Hide(IEnumerator routine = null) => StartCoroutine(HideRoutine(routine));

    /// <summary>Sets overlay to opaque immediately, then runs the optional chained routine.</summary>
    private IEnumerator HideRoutine(IEnumerator routine = null)
    {
        SetAlpha(1f);

        // Run additional routine (if provided)
        if (routine != null)
            yield return routine;
    }

    #endregion

    #region Helper Methods

    /// <summary>Sets the overlay color with specified alpha.</summary>
    private void SetAlpha(float alpha)
    {
        image.color = new Color(1f, 1f, 1f, Mathf.Clamp01(alpha));
    }

    #endregion
}

}
