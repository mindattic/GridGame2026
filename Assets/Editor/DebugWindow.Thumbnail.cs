using Scripts.Helpers;
using Scripts.Helpers;
using System;
using UnityEditor;
using UnityEngine;
using g = Scripts.Helpers.GameHelper;
using s = Scripts.Helpers.SettingsHelper;
using Scripts.Canvas;
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

public partial class DebugWindow
{
    // Class-level fields (now using pixel focus instead of offset)
    private string thumbnailPixelX = "512";
    private string thumbnailPixelY = "512";
    private string thumbnailScaleX = "5";
    private string thumbnailScaleY = "5";
    private string thumbnailTextureSize = "1024";

    // Track last selected actor to auto-load values into the UI when selection changes
    private CharacterClass lastThumbKey = CharacterClass.None;

    /// <summary>Render thumbnail settings.</summary>
    private void RenderThumbnailSettings()
    {

        GUILayout.BeginHorizontal();
        GUILayout.Label("Thumbnail Options", EditorStyles.boldLabel, GUILayout.Width(Screen.width));
        GUILayout.EndHorizontal();

#if UNITY_EDITOR
        // Auto-populate fields when selection changes or when a reload flag is set
        var selected = g.Actors.SelectedActor;
        CharacterClass key = selected != null ? selected.characterClass : CharacterClass.None;

        bool selectionChanged = key != CharacterClass.None && key != lastThumbKey;
        bool reloadRequested = s.ReloadThumbnailSettings && key != CharacterClass.None;
        if ((selectionChanged || reloadRequested) && selected != null)
        {
            var t = selected.Thumbnail;
            int texSize = 1024;
            if (t != null && t.sprite != null && t.sprite.texture != null)
            {
                // Prefer larger dimension as canonical size
                var tex = t.sprite.texture;
                texSize = Mathf.Max(tex.width, tex.height);
            }

            // Load from current settings
            thumbnailPixelX = t != null && t.settings != null ? t.settings.PixelPosition.x.ToString() : "512";
            thumbnailPixelY = t != null && t.settings != null ? t.settings.PixelPosition.y.ToString() : "512";
            thumbnailScaleX = t != null && t.settings != null ? t.settings.Scale.x.ToString("F2") : "5.00";
            thumbnailScaleY = t != null && t.settings != null ? t.settings.Scale.y.ToString("F2") : "5.00";
            thumbnailTextureSize = texSize.ToString();

            lastThumbKey = key;
            s.ReloadThumbnailSettings = false;
        }

        float containerWidth = EditorGUIUtility.currentViewWidth * Increment.Percent33;

        GUILayout.BeginHorizontal();
        GUILayout.BeginVertical(GUILayout.Width(containerWidth));

        // Parse values
        int.TryParse(thumbnailPixelX, out int pX);
        int.TryParse(thumbnailPixelY, out int pY);
        float.TryParse(thumbnailScaleX, out float sX);
        float.TryParse(thumbnailScaleY, out float sY);
        int.TryParse(thumbnailTextureSize, out int tSize);

        int oldPX = pX, oldPY = pY, oldT = tSize;
        float oldSX = sX, oldSY = sY;

        // Inputs
        pX = EditorGUILayout.IntField("pixelX", pX);
        pY = EditorGUILayout.IntField("pixelY", pY);
        sX = EditorGUILayout.FloatField("scaleX", sX);
        sY = EditorGUILayout.FloatField("scaleY", sY);
        tSize = Mathf.Max(1, EditorGUILayout.IntField("textureSize", Mathf.Max(1, tSize)));

        void apply()
        {
            var sel = g.Actors.SelectedActor;
            if (sel != null && sel.Thumbnail != null)
            {
                var ts = new Scripts.Models.ThumbnailSettings(new Vector2Int(pX, pY), new Vector2(sX, sY), tSize);
                sel.Thumbnail.settings = ts;

                // Apply to transform for immediate preview in world
                sel.Thumbnail.transform.localPosition = ts.Offset;
                sel.Thumbnail.transform.localScale = ts.Scale;
            }
        }

        if (pX != oldPX || pY != oldPY ||
            !Mathf.Approximately(sX, oldSX) ||
            !Mathf.Approximately(sY, oldSY) ||
            tSize != oldT)
        {
            apply();
        }

        // Save back to strings
        thumbnailPixelX = pX.ToString();
        thumbnailPixelY = pY.ToString();
        thumbnailScaleX = sX.ToString("F2");
        thumbnailScaleY = sY.ToString("F2");
        thumbnailTextureSize = tSize.ToString();

        // Buttons
        GUILayout.BeginHorizontal();
        if (GUILayout.Button("Update", GUILayout.Width(64))) apply();

        if (GUILayout.Button("Export", GUILayout.Width(64)))
        {
            // Export snippet using pixel-based constructor
            string exportText =
                $"    ThumbnailSettings = new ThumbnailSettings(new Vector2Int({pX}, {pY}), new Vector2({sX}f, {sY}f), {tSize}),";

            EditorGUIUtility.systemCopyBuffer = exportText;
            if (key != CharacterClass.None)
                Debug.Log($"Copied `{key}` ThumbnailSettings (pixel-based) to clipboard.");
        }
        GUILayout.EndHorizontal();

        // Info: show derived offset (read-only) for reference
        var selRef = g.Actors.SelectedActor;
        if (selRef != null && selRef.Thumbnail != null && selRef.Thumbnail.settings != null)
        {
            var off = selRef.Thumbnail.settings.Offset;
            EditorGUILayout.LabelField("derivedOffset", $"({off.x:F2}, {off.y:F2})");
        }

        GUILayout.Space(10);
        GUILayout.EndVertical();
        GUILayout.EndHorizontal();
#endif
    }
}
