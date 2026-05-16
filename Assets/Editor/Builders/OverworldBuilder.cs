using UnityEngine;
using UnityEngine.UI;
using UnityEditor;
using TMPro;
using Scripts.Hub;

/// <summary>
/// OVERWORLDSCAFFOLD - Placeholder builder for the parked Overworld scene.
/// <para>STATUS: Disabled in slice 9. The original Mode7-camera explorer scene was
/// retired in favor of <see cref="StageSelectBuilder"/> as the campaign gateway.
/// `OverworldManager.cs` and the rest of `Assets/Scripts/Overworld/` stay in tree
/// (parked) so they can be revived without churn; this builder just emits a
/// "Disabled" placeholder + a button back to StageSelect so the scene file remains
/// valid + buildable.</para>
/// <para>To revive Overworld: replace this file with the previous auto-generated
/// builder (see git history before slice 9).</para>
/// </summary>
public static class OverworldBuilder
{
    private const string SceneName = "Overworld";

    public static void Build()
    {
        if (!SceneBuilderHelper.OpenScene(SceneName)) return;
        int created = 0, found = 0;

        SceneBuilderHelper.EnsureCamera("Main Camera", ref created, ref found);
        SceneBuilderHelper.EnsureEventSystem(ref created, ref found);

        var canvas = SceneBuilderHelper.EnsureCanvas("Canvas", ref created, ref found);
        if (canvas == null) { SceneBuilderHelper.LogResults(SceneName, created, found); return; }

        var canvasBg = canvas.GetComponent<Image>();
        if (canvasBg != null) canvasBg.color = HubTheme.PanelBg;

        BuildPlaceholderLabel(canvas);
        BuildBackButton(canvas);

        SceneBuilderHelper.EnsureFadeOverlay(canvas, ref created, ref found);
        SceneBuilderHelper.LogResults(SceneName, created, found);
    }

    private static void BuildPlaceholderLabel(RectTransform canvas)
    {
        var go = new GameObject("PlaceholderLabel");
        go.layer = LayerMask.NameToLayer("UI");
        var rt = go.AddComponent<RectTransform>();
        rt.SetParent(canvas, false);
        rt.anchorMin = new Vector2(0.1f, 0.4f);
        rt.anchorMax = new Vector2(0.9f, 0.7f);
        rt.offsetMin = Vector2.zero; rt.offsetMax = Vector2.zero;
        go.AddComponent<CanvasRenderer>();
        var tmp = go.AddComponent<TextMeshProUGUI>();
        tmp.font = SceneBuilderHelper.LoadFont(SceneBuilderHelper.FontPaths.Attic);
        tmp.text = "<b>Overworld</b>\n<size=70%>Parked. Use <color=#ffcc44>Campaign</color> for stage selection.</size>";
        tmp.fontSize = 56;
        tmp.color = HubTheme.TextLight;
        tmp.alignment = TextAlignmentOptions.Center;
        tmp.enableWordWrapping = true;
        tmp.richText = true;
        UnityEditor.Undo.RegisterCreatedObjectUndo(go, "Create PlaceholderLabel");
    }

    private static void BuildBackButton(RectTransform canvas)
    {
        var go = new GameObject("ToCampaignButton");
        go.layer = LayerMask.NameToLayer("UI");
        var rt = go.AddComponent<RectTransform>();
        rt.SetParent(canvas, false);
        rt.anchorMin = new Vector2(0.5f, 0.18f);
        rt.anchorMax = new Vector2(0.5f, 0.18f);
        rt.pivot = new Vector2(0.5f, 0.5f);
        rt.sizeDelta = new Vector2(360f, 80f);
        rt.anchoredPosition = Vector2.zero;
        go.AddComponent<CanvasRenderer>();
        var img = go.AddComponent<Image>();
        img.color = HubTheme.Accent;
        var btn = go.AddComponent<Button>();
        btn.targetGraphic = img;

        var labelGO = new GameObject("Label");
        labelGO.layer = LayerMask.NameToLayer("UI");
        var labelRT = labelGO.AddComponent<RectTransform>();
        labelRT.SetParent(rt, false);
        labelRT.anchorMin = Vector2.zero; labelRT.anchorMax = Vector2.one;
        labelRT.offsetMin = labelRT.offsetMax = Vector2.zero;
        labelGO.AddComponent<CanvasRenderer>();
        var tmp = labelGO.AddComponent<TextMeshProUGUI>();
        tmp.font = SceneBuilderHelper.LoadFont(SceneBuilderHelper.FontPaths.Attic);
        tmp.text = "Go to Campaign";
        tmp.fontSize = 32;
        tmp.color = Color.black;
        tmp.alignment = TextAlignmentOptions.Center;
        tmp.enableWordWrapping = false;

        // OverworldPlaceholderInstance wires this button to scene.Fade.ToStageSelect at runtime.
        UnityEditor.Undo.RegisterCreatedObjectUndo(go, "Create ToCampaignButton");

        // Attach a tiny runtime component to wire the click. The component lives in
        // Scripts.Overworld to keep the parked Overworld scripts as the home.
        SceneBuilderHelper.EnsureScript<Scripts.Overworld.OverworldPlaceholderInstance>(canvas.gameObject);
    }
}
