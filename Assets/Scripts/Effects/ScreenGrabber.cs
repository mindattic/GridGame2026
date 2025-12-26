using System;
using System.Collections;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Captures the current frame into a Texture2D. Call from a coroutine.
/// Optionally hides UI overlay during capture.
/// </summary>
public class ScreenGrabber : MonoBehaviour
{
    // Default to false so we capture exactly what the player currently sees.
    public bool hideFadeOverlayDuringCapture = false;

    /// <summary>
    /// Captures the current frame to a Texture2D and invokes the callback with it.
    /// Uses ScreenCapture.CaptureScreenshotAsTexture for robust, final-frame capture in URP/Built-in.
    /// Caller should Destroy() the texture when done.
    /// </summary>
    public IEnumerator CaptureToTexture(Action<Texture2D> onDone)
    {
        Image faded = null;
        bool prevEnabled = false;
        if (hideFadeOverlayDuringCapture)
        {
            var overlay = FindObjectOfType<FadeOverlayInstance>(true);
            if (overlay != null)
            {
                faded = overlay.GetComponent<Image>();
                if (faded != null)
                {
                    prevEnabled = faded.enabled;
                    faded.enabled = false;
                }
            }
        }

        // Wait for the frame to finish, then capture the final composited screen
        yield return new WaitForEndOfFrame();

        Texture2D tex = ScreenCapture.CaptureScreenshotAsTexture();
        if (tex != null)
        {
            tex.wrapMode = TextureWrapMode.Clamp;
            tex.filterMode = FilterMode.Bilinear;
        }

        if (faded != null) faded.enabled = prevEnabled;

        onDone?.Invoke(tex);
    }
}
