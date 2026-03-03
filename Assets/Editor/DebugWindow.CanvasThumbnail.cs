using Scripts.Helpers;
using Scripts.Helpers;
using Scripts.Libraries;
using Scripts.Models;
using UnityEditor;
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
using Scripts.Instances.Actor;
using Scripts.Instances.Board;
using Scripts.Instances.SynergyLine;
using Scripts.Inventory;
using Scripts.Managers;
using Scripts.Models.Actor;
using Scripts.Overworld;
using Scripts.Sequences;
using Scripts.Serialization;
using Scripts.Utilities;

public partial class DebugWindow
{
    // Canvas thumbnail editor state
    private string canvasX = "0";
    private string canvasY = "-150";
    private string canvasW = "96";
    private string canvasH = "96";
    private string canvasScaleX = "4";
    private string canvasScaleY = "4";
    private CharacterClass lastCanvasKey = CharacterClass.None;

    /// <summary>Render canvas thumbnail settings.</summary>
    private void RenderCanvasThumbnailSettings()
    {
        GUILayout.BeginHorizontal();
        GUILayout.Label("Canvas Thumbnail Options", EditorStyles.boldLabel, GUILayout.Width(Screen.width));
        GUILayout.EndHorizontal();

#if UNITY_EDITOR
        // Load values from the selected actor's ActorData when selection changes
        var selected = g.Actors.SelectedActor;
        CharacterClass key = selected != null ? selected.characterClass : CharacterClass.None;

        if (key != CharacterClass.None && key != lastCanvasKey)
        {
            var data = ActorLibrary.Get(key);
            var crop = (data != null && data.CanvasThumbnailSettings != null)
                ? data.CanvasThumbnailSettings
                : CanvasThumbnailSettings.Default;

            canvasX = crop.X.ToString("F2");
            canvasY = crop.Y.ToString("F2");
            canvasW = Mathf.Max(1, crop.Width).ToString();
            canvasH = Mathf.Max(1, crop.Height).ToString();
            canvasScaleX = crop.Scale.x.ToString("F2");
            canvasScaleY = crop.Scale.y.ToString("F2");

            lastCanvasKey = key;
        }

        float containerWidth = EditorGUIUtility.currentViewWidth * Increment.Percent33;

        GUILayout.BeginHorizontal();
        GUILayout.BeginVertical(GUILayout.Width(containerWidth));

        // Parse current text fields
        float.TryParse(canvasX, out float x);
        float.TryParse(canvasY, out float y);
        int.TryParse(canvasW, out int w);
        int.TryParse(canvasH, out int h);
        float.TryParse(canvasScaleX, out float sX);
        float.TryParse(canvasScaleY, out float sY);

        // Preserve old values to detect changes
        float oX = x, oY = y, oSX = sX, oSY = sY;
        int oW = w, oH = h;

        // Inputs
        x  = EditorGUILayout.FloatField("X", x);
        y  = EditorGUILayout.FloatField("Y", y);
        w  = Mathf.Max(1, EditorGUILayout.IntField("Width",  Mathf.Max(1, w)));
        h  = Mathf.Max(1, EditorGUILayout.IntField("Height", Mathf.Max(1, h)));
        sX = EditorGUILayout.FloatField("ScaleX", sX);
        sY = EditorGUILayout.FloatField("ScaleY", sY);

        // Apply preview when values change
        if (!Mathf.Approximately(x, oX) ||
            !Mathf.Approximately(y, oY) ||
            w != oW || h != oH ||
            !Mathf.Approximately(sX, oSX) ||
            !Mathf.Approximately(sY, oSY))
        {
            PreviewCanvasCrop(x, y, w, h, sX, sY);
        }

        // Save back to strings
        canvasX = x.ToString("F2");
        canvasY = y.ToString("F2");
        canvasW = w.ToString();
        canvasH = h.ToString();
        canvasScaleX = sX.ToString("F2");
        canvasScaleY = sY.ToString("F2");

        // Buttons row 1: Update (preview), Apply To Data (persist to ActorLibrary + rebuild)
        GUILayout.BeginHorizontal();
        if (GUILayout.Button("Update", GUILayout.Width(86)))
        {
            PreviewCanvasCrop(x, y, w, h, sX, sY);
        }

        if (GUILayout.Button("Apply To Data", GUILayout.Width(120)))
        {
            ApplyCanvasCropToData(x, y, w, h, sX, sY);
        }
        GUILayout.EndHorizontal();

        // Buttons row 2: Export C# snippet, Reset Default
        GUILayout.BeginHorizontal();
        if (GUILayout.Button("Export C#", GUILayout.Width(86)))
        {
            string text =
                $"CanvasThumbnailSettings = new CanvasThumbnailSettings({x}f, {y}f, {w}, {h}, new Vector2({sX}f, {sY}f));";
            EditorGUIUtility.systemCopyBuffer = text;
            if (key != CharacterClass.None)
                Debug.Log($"Copied `{key}` CanvasThumbnailSettings to clipboard.");
        }

        if (GUILayout.Button("Reset Default", GUILayout.Width(120)))
        {
            var def = CanvasThumbnailSettings.Default;
            canvasX = def.X.ToString("F2");
            canvasY = def.Y.ToString("F2");
            canvasW = Mathf.Max(1, def.Width).ToString();
            canvasH = Mathf.Max(1, def.Height).ToString();
            canvasScaleX = def.Scale.x.ToString("F2");
            canvasScaleY = def.Scale.y.ToString("F2");
            PreviewCanvasCrop(def.X, def.Y, def.Width, def.Height, def.Scale.x, def.Scale.y);
        }
        GUILayout.EndHorizontal();

        GUILayout.Space(10);
        GUILayout.EndVertical();
        GUILayout.EndHorizontal();
#endif
    }

    /// <summary>Preview canvas crop.</summary>
    private void PreviewCanvasCrop(float x, float y, int w, int h, float sx, float sy)
    {
        // timeline removed: no live preview
    }

    /// <summary>Applies the canvas crop to data.</summary>
    private void ApplyCanvasCropToData(float x, float y, int w, int h, float sx, float sy)
    {
        var actor = g.Actors.SelectedActor;
        if (actor == null) return;

        var data = ActorLibrary.Get(actor.characterClass);
        if (data == null) return;

        data.CanvasThumbnailSettings = new CanvasThumbnailSettings(x, y, w, h, new Vector2(sx, sy));
    }
}
