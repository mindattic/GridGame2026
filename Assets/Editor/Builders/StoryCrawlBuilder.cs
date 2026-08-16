using UnityEngine;
using UnityEngine.UI;
using UnityEditor;
using TMPro;
using Scripts.Hub;
using Scripts.StoryCrawl;

/// <summary>
/// STORYCRAWLSCAFFOLD - Editor tool that builds the StoryCrawl scene from code (US-131 / GG-A5).
///
/// SCENE HIERARCHY:
/// ```
/// Main Camera / EventSystem / StoryCrawlManagerGO / Canvas (near-black)
///   ├── CrawlViewport {stretch, RectMask2D}
///   │   └── CrawlText   Large centered TMP block; the manager scrolls it bottom→top
///   ├── SkipButton      Bottom-right — "Skip ▸"
///   └── FadeOverlay
/// ```
///
/// SCENE FLOW: StageSelect (first entry into a theme) → StoryCrawl → Game
/// RELATED FILES: StoryCrawlManager.cs, StoryCrawlData.cs, StageSelectManager.cs
/// </summary>
public static class StoryCrawlBuilder
{
    private const string SceneName = "StoryCrawl";

    public static void Build()
    {
        if (!SceneBuilderHelper.OpenScene(SceneName)) return;
        int created = 0, found = 0;

        SceneBuilderHelper.EnsureCamera("Main Camera", ref created, ref found);
        SceneBuilderHelper.EnsureEventSystem(ref created, ref found);

        var mgrGO = SceneBuilderHelper.EnsureEmptyGameObject("StoryCrawlManagerGO", ref created, ref found);
        SceneBuilderHelper.EnsureScript<StoryCrawlManager>(mgrGO);

        var canvas = SceneBuilderHelper.EnsureCanvas("Canvas", ref created, ref found);
        if (canvas == null) { SceneBuilderHelper.LogResults(SceneName, created, found); return; }

        // Near-black space; the crawl is the only light.
        var canvasBg = canvas.GetComponent<Image>();
        if (canvasBg != null) canvasBg.color = new Color(0.02f, 0.02f, 0.05f, 1f);

        BuildCrawl(canvas, ref created, ref found);
        BuildSkip(canvas, ref created, ref found);

        SceneBuilderHelper.EnsureFadeOverlay(canvas, ref created, ref found);
        SceneBuilderHelper.LogResults(SceneName, created, found);
    }

    private static void BuildCrawl(RectTransform canvas, ref int created, ref int found)
    {
        RectTransform viewport;
        var existing = canvas.Find("CrawlViewport");
        if (existing != null) { viewport = (RectTransform)existing; found++; }
        else
        {
            var go = new GameObject("CrawlViewport");
            go.layer = LayerMask.NameToLayer("UI");
            viewport = go.AddComponent<RectTransform>();
            viewport.SetParent(canvas, false);
            Undo.RegisterCreatedObjectUndo(go, "Create CrawlViewport");
            created++;
        }
        viewport.anchorMin = new Vector2(0f, 0f);
        viewport.anchorMax = new Vector2(1f, 1f);
        viewport.offsetMin = new Vector2(60f, UiKit.SafeAreaBottom + 40f);
        viewport.offsetMax = new Vector2(-60f, -(UiKit.SafeAreaTop + 40f));
        if (viewport.GetComponent<RectMask2D>() == null)
            viewport.gameObject.AddComponent<RectMask2D>();

        var text = UiKit.Label(viewport, "CrawlText", "", 34f);
        text.anchorMin = new Vector2(0f, 0.5f);
        text.anchorMax = new Vector2(1f, 0.5f);
        text.pivot = new Vector2(0.5f, 0.5f);
        text.sizeDelta = new Vector2(0f, 1800f);
        text.anchoredPosition = new Vector2(0f, -2200f); // starts below the viewport
        var tmp = text.GetComponent<TextMeshProUGUI>();
        if (tmp != null)
        {
            tmp.alignment = TextAlignmentOptions.Top;
            tmp.enableWordWrapping = true;
            tmp.lineSpacing = 12f;
            tmp.color = new Color(1f, 0.92f, 0.62f); // warm crawl gold
        }
    }

    private static void BuildSkip(RectTransform canvas, ref int created, ref int found)
    {
        var skip = UiKit.Button(canvas, "SkipButton", "Skip", UiKit.UiButtonStyle.Secondary, 24f);
        skip.anchorMin = new Vector2(1f, 0f);
        skip.anchorMax = new Vector2(1f, 0f);
        skip.pivot = new Vector2(1f, 0f);
        skip.sizeDelta = new Vector2(180f, 60f);
        skip.anchoredPosition = new Vector2(-24f, UiKit.SafeAreaBottom + 24f);
        created++;
    }
}
