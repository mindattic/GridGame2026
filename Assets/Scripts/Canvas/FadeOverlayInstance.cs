using Assets.Helper;
using Assets.Scripts.Libraries;
using System.Collections;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Controls a full-screen black Image used as a fade overlay.
/// Provides fade in, fade out, show, and hide operations, each optionally followed by a caller-supplied routine.
/// </summary>
public class FadeOverlayInstance : MonoBehaviour
{
    private Image image;
    private float fadeDuration = 0.25f;

    private void Awake()
    {
        // Cache the Image and initialize as an opaque black overlay
        image = GetComponent<Image>();
        image.sprite = SpriteLibrary.Sprites["Black32x32"];
        SetAlpha(1f);
    }

    /// <summary>
    /// Fades from black to transparent over fadeDuration, then runs the optional routine.
    /// </summary>
    public void FadeIn(IEnumerator routine = null) => StartCoroutine(FadeInRoutine(routine));

    private IEnumerator FadeInRoutine(IEnumerator routine = null)
    {
        // Start fully opaque
        SetAlpha(1f);
        float elapsedTime = 0f;

        // Reduce alpha to 0 over time
        while (elapsedTime < fadeDuration)
        {
            elapsedTime += Time.deltaTime;
            float alpha = 1f - Mathf.Clamp01(elapsedTime / fadeDuration);
            SetAlpha(alpha);
            // IMPORTANT: yield a real frame so Time.deltaTime advances.
            yield return Wait.OneTick();
        }

        // Ensure fully transparent
        SetAlpha(0f);

        // Run additional routine (if provided)
        if (routine != null)
            yield return routine;
    }

    /// <summary>
    /// Fades from transparent to black over fadeDuration, then runs the optional routine.
    /// </summary>
    public void FadeOut(IEnumerator routine = null) => StartCoroutine(FadeOutRoutine(routine));

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

    private IEnumerator HideRoutine(IEnumerator routine = null)
    {
        SetAlpha(1f);

        // Run additional routine (if provided)
        if (routine != null)
            yield return routine;
    }

    // Sets the overlay color to black with the specified alpha
    private void SetAlpha(float alpha)
    {
        image.color = new Color(1f, 1f, 1f, Mathf.Clamp01(alpha));
    }
}
