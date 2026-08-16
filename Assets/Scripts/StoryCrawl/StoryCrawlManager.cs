using System.Collections;
using System.Text;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Scripts.Canvas;
using Scripts.Data;
using Scripts.Data.Actor;
using Scripts.Data.Items;
using Scripts.Data.Skills;
using Scripts.Effects;
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
using scene = Scripts.Helpers.SceneHelper;

namespace Scripts.StoryCrawl
{
    /// <summary>
    /// STORYCRAWLMANAGER - Runtime controller for the StoryCrawl scene (US-131 / GG-A5).
    /// <para>PURPOSE: Plays the theme's intro text as a Star-Wars-style upward crawl, then fades
    /// to Game. Always skippable (Skip button or tap anywhere). StageSelect routes here on first
    /// entry into a theme (per-save, GlobalSaveData.SeenStoryCrawls); every other launch goes
    /// straight to Game.</para>
    /// <para>HANDOFF: <see cref="PendingThemeId"/> is the carrier — StageSelectManager sets it
    /// before fading here; consumed on Awake so stale state never replays.</para>
    /// <para>RELATED FILES: StoryCrawlBuilder.cs, StoryCrawlData.cs, StageSelectManager.cs.</para>
    /// </summary>
    public class StoryCrawlManager : MonoBehaviour
    {
        public const string CrawlContentPath = "Canvas/CrawlViewport/CrawlText";
        public const string SkipButtonName = "Canvas/SkipButton";

        /// <summary>Set by StageSelect before fading here; consumed on Awake.</summary>
        public static string PendingThemeId;

        private const float ScrollSeconds = 26f;   // full bottom-to-top travel
        private const float StartDelaySeconds = 0.6f;

        private RectTransform crawlText;
        private bool finished;

        private void Awake()
        {
            string themeId = PendingThemeId;
            PendingThemeId = null; // consume — never latch stale state

            var canvas = GameObject.Find("Canvas");
            var textT = canvas != null ? canvas.transform.Find("CrawlViewport/CrawlText") : null;
            crawlText = textT != null ? textT.GetComponent<RectTransform>() : null;
            var tmp = crawlText != null ? crawlText.GetComponent<TextMeshProUGUI>() : null;

            var paragraphs = StoryCrawlData.Get(themeId);
            if (tmp != null)
            {
                if (paragraphs == null || paragraphs.Length == 0)
                {
                    // No text for this theme (or booted directly for dev) — show a beat, move on.
                    tmp.text = "…the descent continues.";
                }
                else
                {
                    var sb = new StringBuilder();
                    foreach (var p in paragraphs) sb.Append(p).Append("\n\n");
                    tmp.text = sb.ToString().TrimEnd();
                }
            }

            var skipT = canvas != null ? canvas.transform.Find("SkipButton") : null;
            var skip = skipT != null ? skipT.GetComponent<Button>() : null;
            if (skip != null)
            {
                skip.onClick.RemoveAllListeners();
                skip.onClick.AddListener(Finish);
            }
        }

        private void Start()
        {
            scene.FadeIn();
            StartCoroutine(CrawlRoutine());
        }

        private void Update()
        {
            // Tap/click anywhere (not just the button) also skips — it's a crawl, not a wall.
            if (!finished && Input.GetMouseButtonDown(0))
                Finish();
        }

        private IEnumerator CrawlRoutine()
        {
            if (crawlText == null) { Finish(); yield break; }

            // Travel from below the viewport to fully above it.
            LayoutRebuilder.ForceRebuildLayoutImmediate(crawlText);
            float textHeight = Mathf.Max(crawlText.rect.height, 400f);
            var viewport = crawlText.parent as RectTransform;
            float viewHeight = viewport != null ? viewport.rect.height : 1600f;

            float from = -viewHeight * 0.55f - textHeight * 0.5f;
            float to = viewHeight * 0.55f + textHeight * 0.5f;

            yield return new WaitForSecondsRealtime(StartDelaySeconds);

            float t = 0f;
            while (t < ScrollSeconds && !finished)
            {
                t += Time.unscaledDeltaTime;
                float y = Mathf.Lerp(from, to, t / ScrollSeconds);
                crawlText.anchoredPosition = new Vector2(0f, y);
                yield return null;
            }
            Finish();
        }

        private void Finish()
        {
            if (finished) return;
            finished = true;
            scene.Fade.ToGame();
        }
    }
}
