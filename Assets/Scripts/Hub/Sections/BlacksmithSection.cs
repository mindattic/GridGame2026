using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using g = Scripts.Helpers.GameHelper;
using Scripts.Canvas;
using Scripts.Data.Actor;
using Scripts.Data.Items;
using Scripts.Data.Skills;
using Scripts.Effects;
using Scripts.Factories;
using Scripts.Helpers;
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

namespace Scripts.Hub.Sections
{
    /// <summary>
    /// BLACKSMITHSECTION - Time-gated weapon & armor upgrades.
    /// <para>PURPOSE: Two row kinds share a single list — eligible upgrades (owned base item +
    /// recipe registered) and pending jobs (source item already handed to the smith).
    /// Starting an upgrade consumes materials + gold + the base weapon up front; the weapon
    /// is "gone" until the timer completes and the player returns to collect the result.</para>
    /// <para>This design makes upgrades a commitment (you might not have that sword for the
    /// next fight) and sets up future MTX to shorten crafting timers.</para>
    /// <para>RELATED FILES: HubManager.cs, UpgradeLibrary.cs, UpgradeRecipe.cs, CraftJob.cs, CraftJobHelper.cs</para>
    /// </summary>
    public class BlacksmithSection : HubSection
    {
        private UpgradeRecipe selectedRecipe;
        private string selectedJobId;
        private Coroutine tickLoop;

        protected override void OnActivated()
        {
            UpgradeLibrary.Ensure();
            var confirm = FindButton("ConfirmButton");
            Wire(confirm, ConfirmPressed);

            if (tickLoop != null) StopCoroutine(tickLoop);
            tickLoop = StartCoroutine(TickLoop());
        }

        private void OnDisable()
        {
            if (tickLoop != null) { StopCoroutine(tickLoop); tickLoop = null; }
        }

        /// <summary>1-Hz repaint so progress bars and "00:43" countdowns stay live.</summary>
        private IEnumerator TickLoop()
        {
            var wait = new WaitForSeconds(1f);
            while (true)
            {
                yield return wait;
                if (!isActiveAndEnabled) yield break;
                Refresh();
            }
        }

        public override void Refresh()
        {
            var list = FindList("ItemList/Viewport/Content");
            if (list == null) return;
            ClearList(list);

            // Row set: pending jobs first (Ready → In Progress), then eligible upgrades.
            foreach (var job in CraftJobHelper.ForStation(CraftStation.Blacksmith).OrderByDescending(j => j.IsReady).ThenBy(j => j.Remaining))
                AddJobRow(list, job);

            foreach (var item in EligibleBaseItems())
            {
                var recipe = UpgradeLibrary.GetRecipe(item.Id);
                if (recipe == null) continue;
                AddUpgradeRow(list, item, recipe);
            }

            UpdateDetail();
            UpdateConfirmButton();
        }

        // -----------------------------------------------------------------
        // Row builders
        // -----------------------------------------------------------------

        private IEnumerable<ItemDefinition> EligibleBaseItems()
        {
            return Hub.Inventory.All()
                .Select(e => e.Definition)
                .Where(d => d != null && UpgradeLibrary.GetRecipe(d.Id) != null);
        }

        private void AddUpgradeRow(Transform list, ItemDefinition item, UpgradeRecipe recipe)
        {
            var row = HubItemRowFactory.Create(list);
            HubItemRowFactory.SetIcon(row, recipe.To);
            HubItemRowFactory.SetLabel(row, $"{item.DisplayName} → {recipe.To.DisplayName}");
            bool can = recipe.CanUpgrade(Hub.Inventory);
            string cost = HubTheme.FormatGold(recipe.GoldCost);
            float seconds = Scripts.Utilities.Formulas.CraftDurationSeconds(recipe.GoldCost, 1 + recipe.Materials.Sum(m => m.count));
            HubItemRowFactory.SetSubLabel(row, HubTheme.ColorByAffordable($"{cost} + {FormatMaterials(recipe)}  —  {FormatDuration(seconds)}", can));
            HubItemRowFactory.SetLabelColor(row, HubItemRowFactory.RarityColor(item.Rarity));

            var captured = recipe;
            row.GetComponent<Button>().onClick.AddListener(() => { selectedRecipe = captured; selectedJobId = null; Refresh(); });
            HubItemRowFactory.SetSelected(row, selectedRecipe != null && selectedRecipe.From.Id == item.Id);
        }

        private void AddJobRow(Transform list, CraftJob job)
        {
            var row = HubItemRowFactory.Create(list);
            var resultDef = ItemLibrary.Get(job.ResultItemId);
            if (resultDef != null)
            {
                HubItemRowFactory.SetIcon(row, resultDef);
                HubItemRowFactory.SetLabel(row, resultDef.DisplayName);
                HubItemRowFactory.SetLabelColor(row, HubItemRowFactory.RarityColor(resultDef.Rarity));
            }
            else
            {
                HubItemRowFactory.SetLabel(row, job.ResultItemId);
            }

            if (job.IsReady)
            {
                HubItemRowFactory.SetSubLabel(row, "<color=#55DD55><b>Ready — tap to collect</b></color>");
                HubItemRowFactory.SetProgress(row, 1f);
            }
            else
            {
                HubItemRowFactory.SetSubLabel(row,
                    $"<color=#FFB347>Forging…  {CraftJobHelper.FormatRemaining(job.Remaining)}</color>");
                HubItemRowFactory.SetProgress(row, job.Progress01);
            }

            var capturedId = job.JobId;
            row.GetComponent<Button>().onClick.AddListener(() => { selectedRecipe = null; selectedJobId = capturedId; Refresh(); });
            HubItemRowFactory.SetSelected(row, selectedJobId == job.JobId);
        }

        // -----------------------------------------------------------------
        // Confirm button state machine
        // -----------------------------------------------------------------

        private void ConfirmPressed()
        {
            // Selected a pending job → collect (if ready).
            if (!string.IsNullOrEmpty(selectedJobId))
            {
                var job = CraftJobHelper.ForStation(CraftStation.Blacksmith).FirstOrDefault(j => j.JobId == selectedJobId);
                if (job == null) { selectedJobId = null; Refresh(); return; }
                if (!job.IsReady) return;
                if (!CraftJobHelper.Collect(job, Hub.Inventory)) return;
                HubToast.Show($"Collected {ItemLibrary.Get(job.ResultItemId)?.DisplayName ?? job.ResultItemId}");
                selectedJobId = null;
                Hub.PersistAndRefresh();
                return;
            }

            // Selected an upgrade recipe → start the job.
            if (selectedRecipe == null) return;
            var started = CraftJobHelper.StartBlacksmith(selectedRecipe, Hub.Inventory);
            if (started == null) return;
            HubToast.Show($"Started: {selectedRecipe.To.DisplayName}  ({CraftJobHelper.FormatRemaining(started.Remaining)})");
            selectedRecipe = null;
            selectedJobId = started.JobId;
            Hub.PersistAndRefresh();
        }

        private void UpdateConfirmButton()
        {
            var btn = FindButton("ConfirmButton");
            if (btn == null) return;
            var label = btn.GetComponentInChildren<TextMeshProUGUI>();

            if (!string.IsNullOrEmpty(selectedJobId))
            {
                var job = CraftJobHelper.ForStation(CraftStation.Blacksmith).FirstOrDefault(j => j.JobId == selectedJobId);
                if (job == null) { btn.interactable = false; if (label != null) label.text = "—"; return; }
                if (job.IsReady) { btn.interactable = true; if (label != null) label.text = "Collect"; }
                else { btn.interactable = false; if (label != null) label.text = $"In Progress  {CraftJobHelper.FormatRemaining(job.Remaining)}"; }
                return;
            }

            if (selectedRecipe == null)
            {
                btn.interactable = false;
                if (label != null) label.text = "Select an item";
                return;
            }

            bool can = selectedRecipe.CanUpgrade(Hub.Inventory);
            btn.interactable = can;
            float seconds = Scripts.Utilities.Formulas.CraftDurationSeconds(selectedRecipe.GoldCost, 1 + selectedRecipe.Materials.Sum(m => m.count));
            if (label != null) label.text = can ? $"Start Upgrade  ({FormatDuration(seconds)})" : "Not enough materials";
        }

        // -----------------------------------------------------------------
        // Detail panel
        // -----------------------------------------------------------------

        private void UpdateDetail()
        {
            var detail = FindLabel(GameObjectHelper.Hub.DetailLabel);
            if (detail == null) return;

            if (!string.IsNullOrEmpty(selectedJobId))
            {
                var job = CraftJobHelper.ForStation(CraftStation.Blacksmith).FirstOrDefault(j => j.JobId == selectedJobId);
                if (job == null) { detail.text = "<b>Blacksmith</b>\nJob complete."; return; }
                var result = ItemLibrary.Get(job.ResultItemId);
                string resultName = result?.DisplayName ?? job.ResultItemId;
                if (job.IsReady)
                {
                    detail.text = $"<b>{resultName}</b>\n<color=#55DD55>Your order is ready to collect.</color>";
                }
                else
                {
                    detail.text = $"<b>{resultName}</b>\nForging in progress…\nTime remaining: <b>{CraftJobHelper.FormatRemaining(job.Remaining)}</b>\n\n"
                                + "The smith is holding your weapon. Come back when it's ready.";
                }
                return;
            }

            if (selectedRecipe == null)
            {
                detail.text = "<b>Blacksmith</b>\nBring me a weapon and I'll make it sharper.\nUpgrades take real time — you give me the weapon, I return a better one later.";
                return;
            }
            float seconds = Scripts.Utilities.Formulas.CraftDurationSeconds(selectedRecipe.GoldCost, 1 + selectedRecipe.Materials.Sum(m => m.count));
            detail.text = $"<b>{selectedRecipe.From.DisplayName} → {selectedRecipe.To.DisplayName}</b>\n"
                        + $"Stat gain: +{(selectedRecipe.To.Strength - selectedRecipe.From.Strength):0} STR, +{(selectedRecipe.To.Intelligence - selectedRecipe.From.Intelligence):0} INT\n"
                        + $"Cost: {HubTheme.FormatGold(selectedRecipe.GoldCost)}\n"
                        + $"{FormatMaterials(selectedRecipe)}\n"
                        + $"<color=#FFB347>Forge time: {FormatDuration(seconds)}</color>";
        }

        private string FormatMaterials(UpgradeRecipe recipe)
        {
            var parts = new List<string>();
            foreach (var (id, count) in recipe.Materials)
            {
                var def = ItemLibrary.Get(id);
                string name = def != null ? def.DisplayName : id;
                int owned = Hub.Inventory.CountOf(id);
                parts.Add($"{count}× {name} ({owned})");
            }
            return string.Join(", ", parts);
        }

        private static string FormatDuration(float seconds)
        {
            if (seconds < 60f) return $"{Mathf.CeilToInt(seconds)}s";
            int mins = Mathf.FloorToInt(seconds / 60f);
            int secs = Mathf.FloorToInt(seconds - mins * 60f);
            return secs == 0 ? $"{mins}m" : $"{mins}m{secs:00}s";
        }
    }
}
