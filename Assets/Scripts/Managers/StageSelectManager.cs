using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Scripts.Canvas;
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
using Scripts.Models;
using Scripts.Models.Actor;
using Scripts.Overworld;
using Scripts.Sequences;
using Scripts.Serialization;
using Scripts.Utilities;
using scene = Scripts.Helpers.SceneHelper;

namespace Scripts.Managers
{
    /// <summary>
    /// STAGESELECTMANAGER - Runtime controller for the StageSelect scene.
    /// <para>PURPOSE: Top-level gateway for the campaign. Lists every stage in
    /// <see cref="CampaignStages.Order"/> down the left side; clicking one populates a
    /// right-side detail panel (biome, wave count, enemy roster) with Confirm / Cancel
    /// buttons. Confirm writes the stage name to <c>StageSaveData.CurrentStage</c> and
    /// fades to Game. Cancel clears the selection and resets the panel.</para>
    /// <para>UNLOCK GATING: Stage 0 is always selectable. Stage N requires
    /// <c>HighestClearedStageIndex &gt;= N - 1</c> in the save. Locked rows are dimmed
    /// and not clickable. Cleared rows show a star prefix.</para>
    /// <para>NO BACK BUTTON — StageSelect is the campaign home. Players reach vendors
    /// via the persistent VendorNavBar; combat launches by Confirming a stage.</para>
    /// <para>RELATED FILES: StageSelectBuilder.cs, CampaignStages.cs, StageLibrary.cs</para>
    /// </summary>
    public class StageSelectManager : MonoBehaviour
    {
        public const string ListContentPath = "Body/StageList/Viewport/Content";
        public const string DetailLabelName = "Body/DetailPanel/DetailLabel";
        public const string ConfirmButtonName = "Body/DetailPanel/ConfirmButton";
        public const string CancelButtonName = "Body/DetailPanel/CancelButton";
        public const string HubButtonName = "Header/HubButton";

        private RectTransform listContent;
        private TextMeshProUGUI detailLabel;
        private Button confirmButton;
        private Button cancelButton;
        private Button hubButton;

        private int selectedIndex = -1;

        private void Awake()
        {
            BootstrapProfile();
            CacheUiReferences();
            WireButtons();
        }

        private void Start()
        {
            scene.FadeIn();
            Refresh();
        }

        private static void BootstrapProfile()
        {
            if (ProfileHelper.CurrentProfile == null) ProfileHelper.Load();
            if (!ProfileHelper.HasCurrentSave) ProfileHelper.CreateProfile("Dev");
        }

        private void CacheUiReferences()
        {
            var canvas = GameObject.Find("Canvas");
            if (canvas == null) { Debug.LogError("[StageSelectManager] Canvas not found."); return; }

            var contentT = canvas.transform.Find(ListContentPath);
            listContent = contentT != null ? contentT.GetComponent<RectTransform>() : null;

            detailLabel = FindLabel(canvas.transform, DetailLabelName);

            var confirmT = canvas.transform.Find(ConfirmButtonName);
            confirmButton = confirmT != null ? confirmT.GetComponent<Button>() : null;

            var cancelT = canvas.transform.Find(CancelButtonName);
            cancelButton = cancelT != null ? cancelT.GetComponent<Button>() : null;

            var hubT = canvas.transform.Find(HubButtonName);
            hubButton = hubT != null ? hubT.GetComponent<Button>() : null;
        }

        private void WireButtons()
        {
            if (confirmButton != null)
            {
                confirmButton.onClick.RemoveAllListeners();
                confirmButton.onClick.AddListener(ConfirmLaunch);
            }
            if (cancelButton != null)
            {
                cancelButton.onClick.RemoveAllListeners();
                cancelButton.onClick.AddListener(ClearSelection);
            }
            if (hubButton != null)
            {
                hubButton.onClick.RemoveAllListeners();
                hubButton.onClick.AddListener(() => scene.Fade.ToHub());
            }
        }

        private void Refresh()
        {
            RebuildList();
            UpdateDetail();
            UpdateButtons();
        }

        private void RebuildList()
        {
            if (listContent == null) return;
            for (int i = listContent.childCount - 1; i >= 0; i--)
                Object.Destroy(listContent.GetChild(i).gameObject);

            var save = ProfileHelper.CurrentProfile?.CurrentSave;
            int highestCleared = save?.Stage?.HighestClearedStageIndex ?? -1;

            // US-110: newest-on-top — themes in reverse order, stages within each theme in reverse.
            // Precompute each theme's starting global index.
            int[] themeStart = new int[CampaignStages.Themes.Count];
            int offset = 0;
            for (int t = 0; t < CampaignStages.Themes.Count; t++)
            {
                themeStart[t] = offset;
                offset += CampaignStages.Themes[t].StageNames.Count;
            }

            for (int t = CampaignStages.Themes.Count - 1; t >= 0; t--)
            {
                var theme = CampaignStages.Themes[t];
                CreateThemeHeader(theme);
                for (int s = theme.StageNames.Count - 1; s >= 0; s--)
                    CreateStageRow(themeStart[t] + s, highestCleared);
            }
        }

        private void CreateThemeHeader(CampaignTheme theme)
        {
            var go = new GameObject($"ThemeHeader_{theme.Id}");
            go.layer = LayerMask.NameToLayer("UI");
            var rt = go.AddComponent<RectTransform>();
            rt.SetParent(listContent, false);
            rt.sizeDelta = new Vector2(0f, 44f);

            go.AddComponent<CanvasRenderer>();
            var bg = go.AddComponent<Image>();
            bg.color = HubTheme.HeaderBg;
            bg.raycastTarget = false;

            var le = go.AddComponent<LayoutElement>();
            le.minHeight = 44f; le.preferredHeight = 44f; le.flexibleWidth = 1f;

            var labelGO = new GameObject("Label");
            labelGO.layer = LayerMask.NameToLayer("UI");
            var labelRT = labelGO.AddComponent<RectTransform>();
            labelRT.SetParent(rt, false);
            labelRT.anchorMin = Vector2.zero; labelRT.anchorMax = Vector2.one;
            labelRT.offsetMin = new Vector2(16f, 0f); labelRT.offsetMax = new Vector2(-16f, 0f);
            labelGO.AddComponent<CanvasRenderer>();
            var tmp = labelGO.AddComponent<TextMeshProUGUI>();
            tmp.font = UiFonts.Display;
            tmp.text = $"<color=#ffcc44>{theme.DisplayName}</color>";
            tmp.fontSize = 22;
            tmp.fontStyle = FontStyles.Bold;
            tmp.color = HubTheme.Accent;
            tmp.alignment = TextAlignmentOptions.MidlineLeft;
            tmp.enableWordWrapping = false;
            tmp.richText = true;
            tmp.raycastTarget = false;
        }

        private void CreateStageRow(int globalIndex, int highestCleared)
        {
            string stageName = CampaignStages.Order[globalIndex];
            var stage = StageLibrary.Get(stageName);
            bool unlocked = CampaignStages.IsUnlocked(globalIndex, highestCleared);
            bool cleared = highestCleared >= globalIndex;
            bool selected = selectedIndex == globalIndex;

            var go = new GameObject($"Row_{globalIndex:D2}_{stageName}");
            go.layer = LayerMask.NameToLayer("UI");
            var rt = go.AddComponent<RectTransform>();
            rt.SetParent(listContent, false);
            rt.sizeDelta = new Vector2(0f, 72f);

            go.AddComponent<CanvasRenderer>();
            var bg = go.AddComponent<Image>();
            if (selected)        bg.color = HubTheme.RowSelected;
            else if (!unlocked)  bg.color = HubTheme.RowLocked;
            else                 bg.color = HubTheme.RowBg;
            bg.raycastTarget = true;

            var btn = go.AddComponent<Button>();
            btn.targetGraphic = bg;
            btn.interactable = unlocked;
            int captured = globalIndex;
            btn.onClick.AddListener(() => OnRowClicked(captured));

            var le = go.AddComponent<LayoutElement>();
            le.minHeight = 72f; le.preferredHeight = 72f; le.flexibleWidth = 1f;

            var labelGO = new GameObject("Label");
            labelGO.layer = LayerMask.NameToLayer("UI");
            var labelRT = labelGO.AddComponent<RectTransform>();
            labelRT.SetParent(rt, false);
            labelRT.anchorMin = Vector2.zero; labelRT.anchorMax = Vector2.one;
            labelRT.offsetMin = new Vector2(28f, 4f); labelRT.offsetMax = new Vector2(-20f, -4f);
            labelGO.AddComponent<CanvasRenderer>();
            var tmp = labelGO.AddComponent<TextMeshProUGUI>();
            tmp.font = UiFonts.Body;

            string starPrefix = cleared ? "<color=#ffcc44>★</color> " : "  ";
            string lockSuffix = unlocked ? "" : "  <color=#888888>(locked)</color>";
            int waveCount = stage?.Waves?.Count ?? 0;
            int enemyCount = 0;
            if (stage?.Waves != null)
                foreach (var w in stage.Waves) enemyCount += w.Actors?.Count ?? 0;

            // US-110: notable drops hint on a second line
            string dropsHint = BuildDropsHint(stage);
            string dropsLine = !string.IsNullOrEmpty(dropsHint)
                ? $"\n  <color=#7799aa><size=18>drops: {dropsHint}</size></color>"
                : "";

            string label = $"{starPrefix}<b>{CampaignStages.LabelFor(globalIndex)}</b>   <color=#aaaaaa>{waveCount}w / {enemyCount}e</color>{lockSuffix}{dropsLine}";

            tmp.text = label;
            tmp.fontSize = 24;
            tmp.color = unlocked ? Color.white : HubTheme.TextDim;
            tmp.alignment = TextAlignmentOptions.MidlineLeft;
            tmp.enableWordWrapping = false;
            tmp.richText = true;
            tmp.raycastTarget = false;
        }

        private static string BuildDropsHint(Stage stage)
        {
            if (stage?.Waves == null) return "";
            var seenIds = new System.Collections.Generic.HashSet<string>();
            var names = new System.Collections.Generic.List<string>();
            foreach (var wave in stage.Waves)
            {
                if (wave.Actors == null) continue;
                foreach (var actor in wave.Actors)
                {
                    var table = DropTableLibrary.Get(actor.CharacterClass);
                    if (table?.Entries == null) continue;
                    foreach (var entry in table.Entries)
                    {
                        if (!seenIds.Add(entry.ItemId)) continue;
                        var item = ItemLibrary.Get(entry.ItemId);
                        if (item != null && !string.IsNullOrEmpty(item.DisplayName))
                            names.Add(item.DisplayName);
                        if (names.Count >= 2) return string.Join(", ", names);
                    }
                }
            }
            return string.Join(", ", names);
        }

        private void OnRowClicked(int index)
        {
            selectedIndex = index;
            Refresh();
        }

        private void ClearSelection()
        {
            selectedIndex = -1;
            Refresh();
        }

        private void UpdateDetail()
        {
            if (detailLabel == null) return;

            if (selectedIndex < 0)
            {
                detailLabel.text = "<b>Campaign</b>\n\nSelect a stage on the left to preview wave count and enemy composition. Press Confirm to deploy.";
                return;
            }

            string stageName = CampaignStages.Order[selectedIndex];
            var stage = StageLibrary.Get(stageName);
            if (stage == null)
            {
                detailLabel.text = $"<color=#cc3333>Stage `{stageName}` missing from StageLibrary.</color>";
                return;
            }

            int totalEnemies = stage.Waves != null
                ? stage.Waves.Sum(w => w.Actors?.Count ?? 0)
                : 0;

            var sb = new StringBuilder();
            sb.Append("<b>").Append(CampaignStages.LabelFor(selectedIndex)).Append(": ").Append(stage.Biome).Append("</b>\n");
            if (!string.IsNullOrEmpty(stage.Description))
                sb.Append("<i>").Append(stage.Description).Append("</i>\n\n");

            sb.Append("Waves: ").Append(stage.Waves?.Count ?? 0).Append('\n');
            sb.Append("Enemies: ").Append(totalEnemies).Append("\n\n");

            if (stage.Waves != null)
            {
                for (int w = 0; w < stage.Waves.Count; w++)
                {
                    var wave = stage.Waves[w];
                    sb.Append("<color=#ffcc44>Wave ").Append(w + 1).Append(":</color>\n");
                    if (wave.Actors == null || wave.Actors.Count == 0)
                    {
                        sb.Append("  (none)\n");
                        continue;
                    }
                    foreach (var group in wave.Actors.GroupBy(a => a.CharacterClass))
                    {
                        int count = group.Count();
                        string suffix = count > 1 ? $" ×{count}" : "";
                        sb.Append("  • ").Append(group.Key).Append(suffix).Append('\n');
                    }
                }
            }

            detailLabel.text = sb.ToString();
        }

        private void UpdateButtons()
        {
            bool hasSelection = selectedIndex >= 0;
            if (confirmButton != null) confirmButton.interactable = hasSelection;
            if (cancelButton != null) cancelButton.interactable = hasSelection;
        }

        private void ConfirmLaunch()
        {
            if (selectedIndex < 0) return;

            var save = ProfileHelper.CurrentProfile?.CurrentSave;
            if (save == null) return;

            // Empty-party guard — combat would lose instantly.
            var party = save.Party?.Members;
            if (party == null || party.Count == 0)
            {
                Debug.LogWarning("[StageSelectManager] Launch aborted — empty party. Visit the Party vendor to add heroes.");
                if (detailLabel != null)
                    detailLabel.text = "<color=#cc6666>Your party is empty. Visit the Party vendor (top bar) to recruit heroes before deploying.</color>";
                return;
            }

            string stageName = CampaignStages.Order[selectedIndex];
            save.Stage.CurrentStage = stageName;
            save.Stage.CurrentWave = 0;
            ProfileHelper.Save(overwrite: true);

            // Post-battle returns the player to StageSelect (the new gateway).
            ExperienceTracker.NextSceneAfterPostBattleScreen = scene.StageSelect;

            scene.Fade.ToGame();
        }

        private static TextMeshProUGUI FindLabel(Transform root, string path)
        {
            var t = root.Find(path);
            return t != null ? t.GetComponent<TextMeshProUGUI>() : null;
        }
    }
}
