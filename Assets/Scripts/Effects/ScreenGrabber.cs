using System;
using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using Scripts.Canvas;
using Scripts.Data.Actor;
using Scripts.Data.Items;
using Scripts.Data.Skills;
using Scripts.Factories;
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

namespace Scripts.Effects
{
/// <summary>
/// SCREENGRABBER - Captures screen to texture.
/// 
/// PURPOSE:
/// Captures the current rendered frame into a Texture2D.
/// Used for screenshot features, transitions, etc.
/// 
/// USAGE:
/// ```csharp
/// yield return grabber.CaptureToTexture(tex => {
///     // Use tex...
///     Destroy(tex); // Cleanup when done
/// });
/// ```
/// 
/// OPTIONS:
/// - hideFadeOverlayDuringCapture: Temporarily hide fade overlay
/// 
/// NOTE:
/// Caller should Destroy() the texture when done.
/// 
/// RELATED FILES:
/// - ScreenShatter.cs: Uses captured texture for shatter effect
/// </summary>
public class ScreenGrabber : MonoBehaviour
{
    public bool hideFadeOverlayDuringCapture = false;

    /// <summary>Captures current frame and invokes callback with texture.</summary>
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

}
