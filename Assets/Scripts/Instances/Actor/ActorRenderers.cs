using Scripts.Helpers;
using Scripts.Instances.Actor;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using g = Scripts.Helpers.GameHelper;
using Scripts.Canvas;
using Scripts.Data.Actor;
using Scripts.Data.Items;
using Scripts.Data.Skills;
using Scripts.Effects;
using Scripts.Factories;
using Scripts.Hub;
using Scripts.Instances;
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

namespace Scripts.Instances.Actor
{
/// <summary>
/// ACTORRENDERERS - Visual component references for an actor.
/// 
/// PURPOSE:
/// Holds references to all SpriteRenderers, TextMeshPro components,
/// and other visual elements that make up an actor's appearance.
/// 
/// VISUAL HIERARCHY:
/// ```
/// Actor
/// ├── Front (sorting group)
/// │   ├── Backdrop
/// │   ├── Thumbnail (main sprite)
/// │   ├── Frame
/// │   ├── Mask
/// │   ├── NameTagText
/// │   ├── HealthText (top-right HP readout)
/// │   └── ActiveIndicator / FocusIndicator / TargetIndicator
/// │
/// └── Back
/// ```
///
/// RELATED FILES:
/// - ActorInstance.cs: Owns the Render component
/// - ActorFactory.cs: Creates visual hierarchy
/// - ActorAnimation.cs: Animates these renderers
/// </summary>
public class ActorRenderers
{
    public ActorRenderers() { }

    #region Color Settings

    public Color backdropColor = ColorHelper.Solid.White;
    public Color frameColor = ColorHelper.Solid.White;
    public float frameAlphaMax = Opacity.Opaque;
    public Color thumbnailColor = ColorHelper.Solid.White;

    #endregion

    #region Transform References

    public Transform front;
    public Transform back;

    #endregion

    #region Sprite Renderers

    public SpriteRenderer backdrop;
    public SpriteRenderer frame;
    public SpriteRenderer thumbnail;
    public SpriteRenderer gradient;

    #endregion

    #region Other Components

    public SpriteMask mask;
    public TextMeshPro nameTagText;
    public TextMeshPro healthText;

    #endregion
    public SpriteRenderer activeIndicator;
    public SpriteRenderer focusIndicator;
    public SpriteRenderer targetIndicator;

    private ActorInstance instance;
    /// <summary>Initializes initialize.</summary>
    public void Initialize(ActorInstance parentInstance)
    {
        this.instance = parentInstance;

        front = instance.transform.GetChild(ActorLayer.Name.Front);

        backdrop = front.GetChild(ActorLayer.Name.Backdrop).GetComponent<SpriteRenderer>();
        frame = front.GetChild(ActorLayer.Name.Frame).GetComponent<SpriteRenderer>();
        thumbnail = front.GetChild(ActorLayer.Name.Thumbnail).GetComponent<SpriteRenderer>();
        mask = front.GetChild(ActorLayer.Name.Mask).GetComponent<SpriteMask>();
        gradient = front.GetChild(ActorLayer.Name.Gradient).GetComponent<SpriteRenderer>();
        nameTagText = front.GetChild(ActorLayer.Name.NameTagText).GetComponent<TextMeshPro>();
        healthText = front.GetChild(ActorLayer.Name.HealthText).GetComponent<TextMeshPro>();
        activeIndicator = front.GetChild(ActorLayer.Name.ActiveIndicator).GetComponent<SpriteRenderer>();
        focusIndicator = front.GetChild(ActorLayer.Name.FocusIndicator).GetComponent<SpriteRenderer>();
        targetIndicator = front.GetChild(ActorLayer.Name.TargetIndicator).GetComponent<SpriteRenderer>();

        back = instance.transform.GetChild(ActorLayer.Name.Back);
    }

    /// <summary>Sets the alpha.</summary>
    public void SetAlpha(float alpha)
    {
        SetBackdropAlpha(alpha);
        SetFrameAlpha(alpha);
        SetThumbnailAlpha(alpha);
        SetNameTagTextAlpha(alpha);
        SetHealthTextAlpha(alpha);
    }

    /// <summary>Sets the backdrop color.</summary>
    public void SetBackdropColor(Color color)
    {
        backdropColor = new Color(color.r, color.g, color.b, color.a);
        if (backdrop != null) backdrop.color = backdropColor;
    }

    /// <summary>Sets the backdrop alpha.</summary>
    public void SetBackdropAlpha(float alpha)
    {
        backdropColor.a = Mathf.Clamp(alpha, 0, 1);
        if (backdrop != null) backdrop.color = backdropColor;
    }

    /// <summary>Sets the frame color.</summary>
    public void SetFrameColor(Color color)
    {
        frameColor = new Color(color.r, color.g, color.b, Mathf.Clamp(color.a, Opacity.Transparent, frameAlphaMax));
        if (frame != null) frame.color = frameColor;
    }

    /// <summary>Sets the frame alpha.</summary>
    public void SetFrameAlpha(float alpha)
    {
        frameColor.a = Mathf.Clamp(alpha, Opacity.Transparent, frameAlphaMax);
        if (frame != null) this.frame.color = frameColor;
    }

    /// <summary>Sets the frame enabled.</summary>
    public void SetFrameEnabled(bool isEnabled)
    {
        if (frame != null) frame.enabled = isEnabled;
    }

    /// <summary>Sets the thumbnail alpha.</summary>
    public void SetThumbnailAlpha(float alpha)
    {
        thumbnailColor.a = Mathf.Clamp(alpha, Opacity.Transparent, Opacity.Opaque);
        if (thumbnail != null) thumbnail.color = thumbnailColor;
    }

    /// <summary>Sets the thumbnail material.</summary>
    public void SetThumbnailMaterial(Material material)
    {
        if (thumbnail != null) thumbnail.material = material;
    }

    /// <summary>Sets the thumbnail sprite.</summary>
    public void SetThumbnailSprite(Sprite sprite)
    {
        if (thumbnail != null) thumbnail.sprite = sprite;
    }

    /// <summary>Sets the health text alpha (preserves current RGB so color thresholds stay live).</summary>
    public void SetHealthTextAlpha(float alpha)
    {
        if (healthText == null) return;
        var c = healthText.color;
        c.a = Mathf.Clamp01(alpha);
        healthText.color = c;
    }

    /// <summary>Sets the name tag text.</summary>
    public void SetNameTagText(string text)
    {
        if (nameTagText != null)
            nameTagText.text = text;
    }

    /// <summary>Sets the name tag text alpha.</summary>
    public void SetNameTagTextAlpha(float alpha)
    {
        if (nameTagText != null)
            nameTagText.color = new Color(1, 1, 1, alpha);
    }

    /// <summary>Sets the name tag enabled.</summary>
    public void SetNameTagEnabled(bool isEnabled)
    {
        if (nameTagText != null)
            nameTagText.enabled = isEnabled;
    }

    /// <summary>Sets the active indicator enabled.</summary>
    public void SetActiveIndicatorEnabled(bool isEnabled)
    { if (activeIndicator != null) activeIndicator.enabled = isEnabled; }
    /// <summary>Sets the focus indicator enabled.</summary>
    public void SetFocusIndicatorEnabled(bool isEnabled)
    { if (focusIndicator != null) focusIndicator.enabled = isEnabled; }
    /// <summary>Sets the target indicator enabled.</summary>
    public void SetTargetIndicatorEnabled(bool isEnabled)
    { if (targetIndicator != null) targetIndicator.enabled = isEnabled; }

    // ---------------- Saturation helpers ----------------

    private bool saturationCached;
    private Color oBackdrop, oFrame, oThumbnail;

    /// <summary>Desaturate.</summary>
    private static Color Desaturate(Color c, float k)
    {
        Color.RGBToHSV(c, out float h, out float s, out float v);
        s = Mathf.Clamp01(s * Mathf.Clamp01(k));
        var outC = Color.HSVToRGB(h, s, v);
        outC.a = c.a;
        return outC;
    }

    /// <summary>
    /// Sets saturation for key sprite layers. k=1 keeps original tint; k=0 makes grayscale.
    /// Safe to call repeatedly. Restores original colors when k>=1.
    /// </summary>
    public void SetSaturation(float k)
    {
        k = Mathf.Clamp01(k);

        if (k >= 0.999f)
        {
            if (saturationCached)
            {
                SetBackdropColor(oBackdrop);
                SetFrameColor(oFrame);
                SetThumbnailColor(oThumbnail);
            }
            saturationCached = false;
            return;
        }

        if (!saturationCached)
        {
            oBackdrop = backdropColor;
            oFrame = frameColor;
            oThumbnail = thumbnailColor;
            saturationCached = true;
        }

        SetBackdropColor(Desaturate(oBackdrop, k));
        SetFrameColor(Desaturate(oFrame, k));
        SetThumbnailColor(Desaturate(oThumbnail, k));
    }

    // helpers used above
    /// <summary>Sets the thumbnail color.</summary>
    public void SetThumbnailColor(Color color)
    {
        thumbnailColor = new Color(color.r, color.g, color.b, color.a);
        if (thumbnail != null) thumbnail.color = thumbnailColor;
    }
}

}
