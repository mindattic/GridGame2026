using Assets.Helper;
using Assets.Helpers;
using Assets.Scripts.Factories;
using Assets.Scripts.Libraries;
using Assets.Scripts.Models;
using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using scene = Assets.Helpers.SceneHelper;

public class SettingsManager : MonoBehaviour
{
    // Prefab references (none - using factories)

    // UI roots
    private RectTransform contentRoot;

    // Slider settings
    private static readonly List<SliderSetting> Sliders = new List<SliderSetting>
    {
        new SliderSetting(
            "Actor Pan Multiplier",
            "Determines the activity of the actor panning effect",
            0f, 1f, 0.01f,
            s => s.ActorPanMultiplier,
            (s, v) => s.ActorPanMultiplier = v),

        new SliderSetting(
            "Game Speed",
            "Determines the speed of the game.",
            0.25f, 3f, 0.05f,
            s => s.GameSpeed,
            (s, v) => s.GameSpeed = v),

        new SliderSetting(
            "Drag Sensitivity",
            "Controls the sensitivity of drag actions.",
            0.01f, 0.10f, 0.01f,
            s => s.DragSensitivity,
            (s, v) => s.DragSensitivity = v),

        new SliderSetting(
            "Coin Count Multiplier",
            "Coin spawn multiplier",
            0f, 5f, 0.05f,
            s => s.CoinCountMultiplier,
            (s, v) => s.CoinCountMultiplier = v),
    };

    // Toggle settings
    private static readonly List<ToggleSetting> Toggles = new List<ToggleSetting>
    {
        new ToggleSetting(
            "Apply Movement Tilt",
            "Determines whether movement tilt effects are applied.",
            s => s.ApplyMovementTilt,
            (s, v) => s.ApplyMovementTilt = v),

        new ToggleSetting(
            "Reload Thumbnail Settings",
            "If enabled, thumbnails will be reloaded based on current settings.",
            s => s.ReloadThumbnailSettings,
            (s, v) => s.ReloadThumbnailSettings = v),
    };

    // Dropdown settings
    //private static readonly List<DropdownSetting> Dropdowns = new List<DropdownSetting>
    //{
    //    new DropdownSetting(
    //        "Texture Resolution",
    //        "Sets the texture resolution quality.",
    //        typeof(TextureResolution),
    //        s => (object)s.TextureResolution,
    //        (s, o) => s.TextureResolution = (TextureResolution)o),
    //};

    /// <summary>
    /// Initializes prefab and content roots, then builds the UI from the active profile.
    /// </summary>
    private void Awake()
    {
        if (!ProfileHelper.HasProfiles())
            return;

        contentRoot = GameObjectHelper.Settings.ContentRect;

                ReloadUI();
    }

    /// <summary>
    /// Fades the scene in when the settings screen starts.
    /// </summary>
    private void Start()
    {
        scene.FadeIn();
    }

    /// <summary>
    /// Clears content area and rebuilds all setting widgets from the current profile.
    /// </summary>
    private void ReloadUI()
    {
        // Clear existing controls
        foreach (Transform child in contentRoot)
            Destroy(child.gameObject);

        // Retrieve latest settings
        var settings = ProfileHelper.CurrentProfile.Settings;

        // Sliders
        foreach (var x in Sliders)
        {
            CreateSlider(
                x.FriendlyName,
                x.TooltipText,
                x.Getter(settings),
                x.Min,
                x.Max,
                x.Increment,
                v => { x.Setter(settings, v); },
                x.AsInt);
        }

        // Toggles
        foreach (var x in Toggles)
        {
            CreateToggle(
                x.FriendlyName,
                x.TooltipText,
                x.Getter(settings),
                v => { x.Setter(settings, v); });
        }

        // Dropdowns
        //foreach (var x in Dropdowns)
        //{
        //    CreateDropdown(
        //        x.FriendlyName,
        //        x.TooltipText,
        //        x.EnumType,
        //        x.Getter(settings),
        //        val => { x.Setter(settings, val); });
        //}
    }

    /// <summary>
    /// Instantiates and wires a slider with snapping, value formatting, and drag-to-set behavior.
    /// </summary>
    private void CreateSlider(
        string label,
        string tooltipText,
        float current,
        float min,
        float max,
        float increment,
        Action<float> onChanged,
        bool asInt)
    {
        // Use factory instead of Instantiate(prefab)
        var go = SettingSliderFactory.Create(contentRoot);
        go.name = $"SettingSlider_{label.ToPascalCase()}";

        // Label
        var labelText = go.GetComponentInChildrenByName<TextMeshProUGUI>("Label");
        if (labelText != null) labelText.text = label;

        // Value text
        var value = go.GetComponentInChildrenByName<TextMeshProUGUI>("Value");

        // Slider
        var slider = go.GetComponentInChildrenByName<Slider>("Slider");
        if (slider == null)
        {
            slider = go.GetComponentInChildren<Slider>();
        }
        slider.SetDirection(Slider.Direction.LeftToRight, true);
        slider.navigation = new Navigation { mode = Navigation.Mode.None };
        slider.transition = Selectable.Transition.None;
        slider.interactable = true;
        slider.wholeNumbers = asInt;
        slider.minValue = min;
        slider.maxValue = max;

        // Local helpers
        float Snap(float v)
        {
            var clamped = Mathf.Clamp(v, min, max);
            if (asInt) return Mathf.Clamp(Mathf.Round(clamped), min, max);
            if (increment > 0f)
            {
                var steps = Mathf.Round((clamped - min) / increment);
                var snapped = min + steps * increment;
                return Mathf.Clamp(snapped, min, max);
            }
            return clamped;
        }

        string Format(float v)
        {
            return asInt
                ? Mathf.RoundToInt(v).ToString()
                : v.ToString(increment >= 0.1f ? "0.0" : "0.00");
        }

        // Initialize without firing onChanged
        var initial = Snap(current);
        slider.SetValueWithoutNotify(initial);
        if (value != null) value.text = Format(initial);

        // Pointer-driven positioning along the slider track
        void SetFromPointer(PointerEventData e)
        {
            if (e == null) return;

            var rt = slider.fillRect != null ? slider.fillRect : slider.GetComponent<RectTransform>();
            if (rt == null) return;

            Vector2 local;
            if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(rt, e.position, e.pressEventCamera, out local))
                return;

            var rect = rt.rect;
            float t = Mathf.InverseLerp(rect.xMin, rect.xMax, local.x);
            t = Mathf.Clamp01(t);
            if (slider.direction == Slider.Direction.RightToLeft)
                t = 1f - t;

            var target = min + t * (max - min);
            var snapped = Snap(target);

            if (Mathf.Abs(slider.value - snapped) > 0.0001f)
                slider.value = snapped;

            if (value != null) value.text = Format(snapped);
            e.Use();
        }

        // EventTrigger hookup
        var trigger = slider.gameObject.GetComponent<EventTrigger>();
        if (trigger == null) trigger = slider.gameObject.AddComponent<EventTrigger>();
        trigger.triggers = trigger.triggers ?? new List<EventTrigger.Entry>();

        void AddTrigger(EventTriggerType type, Action<BaseEventData> act)
        {
            var entry = new EventTrigger.Entry { eventID = type };
            entry.callback.AddListener(data => act(data));
            trigger.triggers.Add(entry);
        }

        AddTrigger(EventTriggerType.PointerDown, d => SetFromPointer(d as PointerEventData));
        AddTrigger(EventTriggerType.Drag, d => SetFromPointer(d as PointerEventData));

        // Value change
        slider.onValueChanged.RemoveAllListeners();
        slider.onValueChanged.AddListener(v =>
        {
            var snapped = Snap(v);
            if (Mathf.Abs(snapped - v) > 0.0001f)
                slider.SetValueWithoutNotify(snapped);

            if (value != null) value.text = Format(snapped);
            onChanged(snapped);
        });
    }

    /// <summary>
    /// Instantiates and wires a toggle option.
    /// </summary>
    private void CreateToggle(
        string label,
        string tooltipText,
        bool current,
        Action<bool> onChanged)
    {
        // Use factory instead of Instantiate(prefab)
        var go = SettingToggleFactory.Create(contentRoot);
        go.name = $"SettingToggle_{label.ToPascalCase()}";

        var labelText = go.GetComponentInChildrenByName<TextMeshProUGUI>("Label");
        if (labelText != null) labelText.text = label;

        var toggle = go.GetComponentInChildren<Toggle>();
        toggle.isOn = current;
        toggle.onValueChanged.AddListener(v => onChanged(v));
    }

    /// <summary>
    /// Instantiates and wires a dropdown for enum choices.
    /// </summary>
    private void CreateDropdown(
        string label,
        string tooltipText,
        Type enumType,
        object current,
        Action<object> onChanged)
    {
        // Use factory instead of Instantiate(prefab)
        var go = SettingDropdownFactory.Create(contentRoot);
        go.name = $"SettingDropdown_{label.ToPascalCase()}";

        var labelText = go.GetComponentInChildrenByName<TextMeshProUGUI>("Label");
        if (labelText != null) labelText.text = label;

        var dropdown = go.GetComponentInChildren<TMP_Dropdown>();
        dropdown.ClearOptions();

        var names = Enum.GetNames(enumType);
        dropdown.AddOptions(new List<string>(names));

        int idx = Array.IndexOf(names, current.ToString());
        dropdown.value = idx >= 0 ? idx : 0;

        dropdown.onValueChanged.AddListener(i =>
        {
            var val = Enum.Parse(enumType, names[i]);
            onChanged(val);
        });
    }

    /// <summary>
    /// Returns to the previous scene.
    /// </summary>
    public void OnBackButtonClicked()
    {
        scene.Fade.ToPreviousScene();
    }

    /// <summary>
    /// Persists current in-memory settings to the active profile.
    /// </summary>
    public void OnSaveButtonClicked()
    {
        ProfileHelper.SaveSettings();
        Debug.Log("Settings saved.");
    }

    /// <summary>
    /// Restores defaults to the active profile and rebuilds the UI.
    /// </summary>
    public void OnDefaultsButtonClick()
    {
        var profile = ProfileHelper.CurrentProfile;
        if (profile == null) return;

        profile.Settings = new ProfileSettings(ProfileHelper.DefaultSettings);
        ProfileHelper.SaveSettings(profile);
        ReloadUI();

        Debug.Log("Settings reset to defaults.");
    }
}
