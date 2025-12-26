using Assets.Helper;
using Game.Instances.Actor;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using g = Assets.Helpers.GameHelper;

public class ActorRenderers
{
    public ActorRenderers() { }

    public Color opaqueColor = ColorHelper.Solid.White;
    public Color qualityColor = ColorHelper.Solid.White;
    public float qualityAlphaMax = Opacity.Opaque;
    public Color glowColor = ColorHelper.Solid.White;
    public Color parallaxColor = ColorHelper.Solid.White;
    public float parallaxAlphaMax = Opacity.Percent50;
    public Color thumbnailColor = ColorHelper.Solid.White;
    public Color gradientColor = ColorHelper.Solid.White;
    public Color frameColor = ColorHelper.Solid.White;
    public Color healthBarColor = ColorHelper.Solid.White;
    public Color healthBarDrainColor = ColorHelper.HealthBar.Yellow;
    public Color actionBarColor = ColorHelper.ActionBar.Blue;
    public Color actionBarDrainColor = ColorHelper.HealthBar.Yellow;
    public Color turnDelayColor = ColorHelper.Solid.Red;

    public Color armorColor = ColorHelper.Solid.White;

    public Transform front;
    public Transform back;

    public SpriteRenderer opaque;
    public SpriteRenderer quality;
    public SpriteRenderer glow;
    public SpriteRenderer parallax;
    public SpriteRenderer thumbnail;
    public SpriteRenderer frame;
    public SpriteRenderer statusIcon;
    public SpriteRenderer gradient;
    public GameObject healthBar;
    public SpriteRenderer healthBarBack;
    public SpriteRenderer healthBarDrain;
    public SpriteRenderer healthBarFill;
    public TextMeshPro healthBarText;
    public GameObject actionBar;
    public SpriteRenderer actionBarBack;
    public SpriteRenderer actionBarDrain;
    public SpriteRenderer actionBarFill;
    public TextMeshPro actionBarText;
    public SpriteMask mask;
    public SpriteRenderer radialBack;
    public SpriteRenderer radial;
    public TextMeshPro radialText;
    public TextMeshPro turnDelayText;
    public TextMeshPro nameTagText;
    public SpriteRenderer armorNorth;
    public SpriteRenderer armorEast;
    public SpriteRenderer armorSouth;
    public SpriteRenderer armorWest;
    public SpriteRenderer activeIndicator;
    public SpriteRenderer focusIndicator;
    public SpriteRenderer targetIndicator;

    private ActorInstance instance;
    public void Initialize(ActorInstance parentInstance)
    {
        this.instance = parentInstance;

        front = instance.transform.GetChild(ActorLayer.Name.Front);

        opaque = front.GetChild(ActorLayer.Name.Opaque).GetComponent<SpriteRenderer>();
        quality = front.GetChild(ActorLayer.Name.Quality).GetComponent<SpriteRenderer>();
        glow = front.GetChild(ActorLayer.Name.Glow).GetComponent<SpriteRenderer>();
        parallax = front.GetChild(ActorLayer.Name.Parallax).GetComponent<SpriteRenderer>();
        thumbnail = front.GetChild(ActorLayer.Name.Thumbnail).GetComponent<SpriteRenderer>();
        gradient = front.GetChild(ActorLayer.Name.Gradient).GetComponent<SpriteRenderer>();
        frame = front.GetChild(ActorLayer.Name.Frame).GetComponent<SpriteRenderer>();
        statusIcon = front.GetChild(ActorLayer.Name.StatusIcon).GetComponent<SpriteRenderer>();
        healthBarBack = front.GetChild(ActorLayer.Name.HealthBar.Root).GetChild(ActorLayer.Name.HealthBar.Back).GetComponent<SpriteRenderer>();
        healthBarDrain = front.GetChild(ActorLayer.Name.HealthBar.Root).GetChild(ActorLayer.Name.HealthBar.Drain).GetComponent<SpriteRenderer>();
        healthBarFill = front.GetChild(ActorLayer.Name.HealthBar.Root).GetChild(ActorLayer.Name.HealthBar.Fill).GetComponent<SpriteRenderer>();
        healthBarText = front.GetChild(ActorLayer.Name.HealthBar.Root).GetChild(ActorLayer.Name.HealthBar.Text).GetComponent<TextMeshPro>();
        actionBarBack = front.GetChild(ActorLayer.Name.ActionBar.Root).GetChild(ActorLayer.Name.ActionBar.Back).GetComponent<SpriteRenderer>();
        actionBarDrain = front.GetChild(ActorLayer.Name.ActionBar.Root).GetChild(ActorLayer.Name.ActionBar.Drain).GetComponent<SpriteRenderer>();
        actionBarFill = front.GetChild(ActorLayer.Name.ActionBar.Root).GetChild(ActorLayer.Name.ActionBar.Fill).GetComponent<SpriteRenderer>();
        actionBarText = front.GetChild(ActorLayer.Name.ActionBar.Root).GetChild(ActorLayer.Name.ActionBar.Text).GetComponent<TextMeshPro>();
        mask = front.GetChild(ActorLayer.Name.Mask).GetComponent<SpriteMask>();
        radialBack = front.GetChild(ActorLayer.Name.RadialBack).GetComponent<SpriteRenderer>();
        radial = front.GetChild(ActorLayer.Name.RadialFill).GetComponent<SpriteRenderer>();
        radialText = front.GetChild(ActorLayer.Name.RadialText).GetComponent<TextMeshPro>();
        turnDelayText = front.GetChild(ActorLayer.Name.TurnDelayText).GetComponent<TextMeshPro>();
        nameTagText = front.GetChild(ActorLayer.Name.NameTagText).GetComponent<TextMeshPro>();
        armorNorth = front.GetChild(ActorLayer.Name.Armor.Root).GetChild(ActorLayer.Name.Armor.ArmorNorth).GetComponent<SpriteRenderer>();
        armorEast = front.GetChild(ActorLayer.Name.Armor.Root).GetChild(ActorLayer.Name.Armor.ArmorEast).GetComponent<SpriteRenderer>();
        armorSouth = front.GetChild(ActorLayer.Name.Armor.Root).GetChild(ActorLayer.Name.Armor.ArmorSouth).GetComponent<SpriteRenderer>();
        armorWest = front.GetChild(ActorLayer.Name.Armor.Root).GetChild(ActorLayer.Name.Armor.ArmorWest).GetComponent<SpriteRenderer>();
        activeIndicator = front.GetChild(ActorLayer.Name.ActiveIndicator).GetComponent<SpriteRenderer>();
        focusIndicator = front.GetChild(ActorLayer.Name.FocusIndicator).GetComponent<SpriteRenderer>();
        targetIndicator = front.GetChild(ActorLayer.Name.TargetIndicator).GetComponent<SpriteRenderer>();

        back = instance.transform.GetChild(ActorLayer.Name.Back);
    }

    public void SetAlpha(float alpha)
    {
        SetOpaqueAlpha(alpha);
        SetQualityAlpha(alpha);
        SetGlowAlpha(alpha);
        SetParallaxAlpha(alpha);
        SetThumbnailAlpha(alpha);
        SetGradientAlpha(alpha);
        SetFrameAlpha(alpha);
        SetHealthBarAlpha(alpha);
        SetActionBarAlpha(alpha);
        SetRadialAlpha(alpha);
        SetTurnDelayTextAlpha(alpha);
        SetNameTagTextAlpha(alpha);
        SetArmorAlpha(alpha);
    }

    public void SetOpaqueColor(Color color)
    {
        opaqueColor = new Color(color.r, color.g, color.b, color.a);
        if (opaque != null) opaque.color = opaqueColor;
    }

    public void SetOpaqueAlpha(float alpha)
    {
        opaqueColor.a = Mathf.Clamp(alpha, 0, 1);
        if (opaque != null) opaque.color = opaqueColor;
    }

    public void SetQualityColor(Color color)
    {
        qualityColor = new Color(color.r, color.g, color.b, Mathf.Clamp(color.a, Opacity.Transparent, qualityAlphaMax));
        if (quality != null) quality.color = qualityColor;
    }


    public void SetQualityAlpha(float alpha)
    {
        qualityColor.a = Mathf.Clamp(alpha, Opacity.Transparent, qualityAlphaMax);
        if (quality != null) this.quality.color = qualityColor;
    }

    public void SetGlowColor(Color color)
    {
        glowColor = new Color(color.r, color.g, color.b, color.a);
        if (glow != null) this.glow.color = glowColor;
    }

    public void SetGlowAlpha(float alpha)
    {
        glowColor.a = Mathf.Clamp(alpha, Opacity.Transparent, Opacity.Percent50);
        if (glow != null) this.glow.color = glowColor;
    }

    public void SetGlowScale(Vector3 scale)
    {
        if (glow != null) this.glow.transform.localScale = scale;
    }

    public void SetParallaxSprite(Sprite sprite)
    {
        if (parallax != null) parallax.sprite = sprite;
    }

    public void SetParallaxMaterial(Material material)
    {
        if (parallax != null) parallax.material = material;
    }

    public void SetParallaxMaterial(Material material, Texture texture = null)
    {
        if (parallax == null) return;
        parallax.material = material;
        if (texture != null)
            parallax.material.mainTexture = texture;
    }

    public void SetParallaxAlpha(float alpha)
    {
        parallaxColor.a = Mathf.Clamp(alpha, Opacity.Transparent, parallaxAlphaMax);
        if (parallax != null) this.parallax.color = parallaxColor;
    }

    public void SetParallaxFocus(float xScroll, float yScroll)
    {
        if (instance == null || parallax == null) return;
        instance.StartCoroutine(UpdateParallaxFocusRoutine("_XScroll", xScroll));
        instance.StartCoroutine(UpdateParallaxFocusRoutine("_YScroll", yScroll));
    }

    private IEnumerator UpdateParallaxFocusRoutine(string scrollProperty, float targetValue)
    {
        //Fetch the CurrentProfile value once at the start
        float currentValue = parallax.material.GetFloat(scrollProperty);
        float duration = 1f; //Adjust the duration for the transition
        float elapsed = 0f;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / duration); //Ensure t stays between 0 and 1
            float newValue = Mathf.Lerp(currentValue, targetValue, t);
            parallax.material.SetFloat(scrollProperty, newValue);
            yield return Wait.OneTick(); //Custom Wait method
        }

        //SelectProfile the final value to ensure precision
        parallax.material.SetFloat(scrollProperty, targetValue);
    }

    public void SetThumbnailAlpha(float alpha)
    {
        thumbnailColor.a = Mathf.Clamp(alpha, Opacity.Transparent, Opacity.Opaque);
        if (thumbnail != null) thumbnail.color = thumbnailColor;
    }
    public void SetGradientAlpha(float alpha)
    {
        gradientColor.a = Mathf.Clamp(alpha, Opacity.Transparent, Opacity.Opaque);
        if (gradient != null) gradient.color = gradientColor;
    }

    public void SetThumbnailMaterial(Material material)
    {
        if (thumbnail != null) thumbnail.material = material;
    }

    public void SetThumbnailSprite(Sprite sprite)
    {
        if (thumbnail != null) thumbnail.sprite = sprite;
    }

    public void SetFrameAlpha(float alpha)
    {
        frameColor.a = Mathf.Clamp(alpha, Opacity.Transparent, Opacity.Opaque);
        if (frame != null) frame.color = frameColor;
    }

    public void SetFrameEnabled(bool isEnabled)
    {
        if (frame != null) frame.enabled = isEnabled;
    }

    public void SetHealthBarAlpha(float alpha)
    {
        if (healthBarBack != null)
            healthBarBack.color = new Color(0, 0, 0, Mathf.Clamp(alpha, Opacity.Transparent, Opacity.Translucent.Alpha196));
        var drain = healthBarDrainColor;
        if (healthBarDrain != null)
            healthBarDrain.color = new Color(drain.r, drain.g, drain.b, alpha);
        var fill = healthBarColor;
        if (healthBarFill != null)
            healthBarFill.color = new Color(fill.r, fill.g, fill.b, alpha);
        if (healthBarText != null)
            healthBarText.color = new Color(1, 1, 1, alpha);
    }

    public void SetActionBarAlpha(float alpha)
    {
        if (actionBarBack != null)
            actionBarBack.color = new Color(1, 1, 1, Mathf.Clamp(alpha, Opacity.Transparent, Opacity.Translucent.Alpha196));
        if (actionBarDrain != null)
            actionBarDrain.color = new Color(1, 0, 0, alpha);
        var drain = actionBarDrainColor;
        if (actionBarDrain != null)
            actionBarDrain.color = new Color(drain.r, drain.g, drain.b, alpha);
        var fill = actionBarColor;
        if (actionBarFill != null)
            actionBarFill.color = new Color(fill.r, fill.g, fill.b, alpha);
        if (actionBarText != null)
            actionBarText.color = new Color(1, 1, 1, alpha);
    }

    public void SetRadialEnabled(bool isEnabled)
    {
        if (radialBack != null) radialBack.enabled = isEnabled;
        if (radial != null) radial.enabled = isEnabled;
        if (radialText != null) radialText.enabled = isEnabled;
    }

    public void SetRadialAlpha(float alpha)
    {
        if (radialBack != null)
            radialBack.color = new Color(1, 1, 1, Mathf.Clamp(alpha, Opacity.Transparent, Opacity.Translucent.Alpha196));
        if (radial != null)
            radial.color = new Color(1, 1, 1, alpha);
        if (radialText != null)
            radialText.color = new Color(1, 1, 1, alpha);
    }

    public void SetFrameColor(Color color)
    {
        frameColor = color;
        if (frame != null) this.frame.color = frameColor;
    }


    public void SetTurnDelayFontSize(int key)
    {
        var fontSizeKeyValueMap = new Dictionary<int, float>() {
            { 9, 1.0000f },
            { 8, 1.3750f },
            { 7, 1.7500f },
            { 6, 2.1250f },
            { 5, 2.5000f },
            { 4, 2.8750f },
            { 3, 3.2500f },
            { 2, 3.6250f },
            { 1, 4.0000f },
        };

        if (turnDelayText != null)
            turnDelayText.fontSize = key > 9 ? 1f : fontSizeKeyValueMap[key];
    }

    /// <summary>
    /// UI helper to show a numeric countdown beside this enemy.
    /// Timeline owns the number. This method only paints the label.
    /// Pass a negative value to clear the label.
    /// Displays turns remaining as 1-based (next = 1).
    /// </summary>
    public void SetTurnDelayText(int value)
    {
        if (turnDelayText != null)
            turnDelayText.text = string.Empty;
    }

    public void SetTurnDelayTextEnabled(bool isEnabled)
    {
        if (turnDelayText != null)
            turnDelayText.enabled = isEnabled;
    }

    public void SetTurnDelayTextAlpha(float alpha)
    {
        turnDelayColor.a = Mathf.Clamp(alpha, Opacity.Transparent, Opacity.Opaque);
        if (turnDelayText != null)
            turnDelayText.color = turnDelayColor;
    }

    public void SetTurnDelayTextColor(Color color)
    {
        turnDelayColor = color;
        if (turnDelayText != null)
            turnDelayText.color = turnDelayColor;
    }

    public void SetNameTagText(string text)
    {
        if (nameTagText != null)
            nameTagText.text = text;
    }

    public void SetNameTagTextAlpha(float alpha)
    {
        if (nameTagText != null)
            nameTagText.color = new Color(1, 1, 1, alpha);
    }

    public void SetNameTagEnabled(bool isEnabled)
    {
        if (nameTagText != null)
            nameTagText.enabled = isEnabled;
    }

    public void SetActionBarEnabled(bool isEnabled)
    {
        if (actionBarBack != null) actionBarBack.enabled = isEnabled; if (actionBarFill != null) actionBarFill.enabled = isEnabled;
    }

    public void SetArmorAlpha(float alpha)
    {
        armorColor = new Color(1, 1, 1, alpha);
        if (armorNorth != null) armorNorth.color = armorColor;
        if (armorEast != null) armorEast.color = armorColor;
        if (armorSouth != null) armorSouth.color = armorColor;
        if (armorWest != null) armorWest.color = armorColor;
    }

    public void SetActiveIndicatorEnabled(bool isEnabled)
    { if (activeIndicator != null) activeIndicator.enabled = isEnabled; }
    public void SetFocusIndicatorEnabled(bool isEnabled)
    { if (focusIndicator != null) focusIndicator.enabled = isEnabled; }
    public void SetTargetIndicatorEnabled(bool isEnabled)
    { if (targetIndicator != null) targetIndicator.enabled = isEnabled; }

    // ---------------- Saturation helpers ----------------

    private bool saturationCached;
    private Color oOpaque, oQuality, oParallax, oThumbnail, oFrame, oArmor;

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

        // Restore
        if (k >= 0.999f)
        {
            if (saturationCached)
            {
                SetOpaqueColor(oOpaque);
                SetQualityColor(oQuality);
                if (parallax != null) { parallaxColor = oParallax; parallax.color = parallaxColor; }
                SetThumbnailColor(oThumbnail);
                SetFrameColor(oFrame);
                if (armorNorth != null) armorNorth.color = oArmor;
                if (armorEast != null) armorEast.color = oArmor;
                if (armorSouth != null) armorSouth.color = oArmor;
                if (armorWest != null) armorWest.color = oArmor;
            }
            saturationCached = false;
            return;
        }

        // Cache originals once
        if (!saturationCached)
        {
            oOpaque = opaqueColor;
            oQuality = qualityColor;
            oParallax = parallaxColor;
            oThumbnail = thumbnailColor;
            oFrame = frameColor;
            oArmor = armorColor;
            saturationCached = true;
        }

        // Apply desaturation from cached originals
        SetOpaqueColor(Desaturate(oOpaque, k));
        SetQualityColor(Desaturate(oQuality, k));
        if (parallax != null) { parallaxColor = Desaturate(oParallax, k); parallax.color = parallaxColor; }
        SetThumbnailColor(Desaturate(oThumbnail, k));
        SetFrameColor(Desaturate(oFrame, k));
        var armorDesat = Desaturate(oArmor, k);
        if (armorNorth != null) armorNorth.color = armorDesat;
        if (armorEast != null) armorEast.color = armorDesat;
        if (armorSouth != null) armorSouth.color = armorDesat;
        if (armorWest != null) armorWest.color = armorDesat;
    }

    // helpers used above
    public void SetThumbnailColor(Color color)
    {
        thumbnailColor = new Color(color.r, color.g, color.b, color.a);
        if (thumbnail != null) thumbnail.color = thumbnailColor;
    }
}
