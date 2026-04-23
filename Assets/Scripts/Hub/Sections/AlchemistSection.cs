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
    /// ALCHEMISTSECTION - Time-gated potion brewing.
    /// <para>PURPOSE: Same async vendor pattern as the Blacksmith but without a source item —
    /// ingredients + gold go in, a potion comes out after the timer elapses. Pending jobs show
    /// at the top of the list alongside available recipes.</para>
    /// <para>RELATED FILES: HubManager.cs, RecipeLibrary.cs, CraftingRecipe.cs, CraftJob.cs, CraftJobHelper.cs</para>
    /// </summary>
    public class AlchemistSection : HubSection
    {
        private CraftingRecipe selectedRecipe;
        private string selectedJobId;
        private Coroutine tickLoop;

        protected override void OnActivated()
        {
            var confirm = FindButton("ConfirmButton");
            Wire(confirm, ConfirmPressed);

            if (tickLoop != null) StopCoroutine(tickLoop);
            tickLoop = StartCoroutine(TickLoop());
        }

        private void OnDisable()
        {
            if (tickLoop != null) { StopCoroutine(tickLoop); tickLoop = null; }
        }

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

            foreach (var job in CraftJobHelper.ForStation(CraftStation.Alchemist).OrderByDescending(j => j.IsReady).ThenBy(j => j.Remaining))
                AddJobRow(list, job);

            foreach (var recipe in PotionRecipes())
                AddRecipeRow(list, recipe);

            UpdateDetail();
            UpdateConfirmButton();
        }

        private IEnumerable<CraftingRecipe> PotionRecipes()
        {
            return RecipeLibrary.All().Where(r =>
            {
                var result = ItemLibrary.Get(r.ResultItemId);
                return result != null && result.Type == ItemType.Consumable;
            });
        }

        private void AddRecipeRow(Transform list, CraftingRecipe recipe)
        {
            var row = HubItemRowFactory.Create(list);
            var result = ItemLibrary.Get(recipe.ResultItemId);
            if (result != null)
            {
                HubItemRowFactory.SetIcon(row, result);
                HubItemRowFactory.SetLabelColor(row, HubItemRowFactory.RarityColor(result.Rarity));
            }
            HubItemRowFactory.SetLabel(row, recipe.DisplayName);

            bool can = recipe.CanCraft(Hub.Inventory);
            string cost = HubTheme.FormatGold(recipe.GoldCost);
            float seconds = Scripts.Utilities.Formulas.CraftDurationSeconds(recipe.GoldCost, recipe.Ingredients.Sum(i => i.Count));
            HubItemRowFactory.SetSubLabel(row, HubTheme.ColorByAffordable($"{cost} + {FormatIngredients(recipe)}  —  {FormatDuration(seconds)}", can));

            var captured = recipe;
            row.GetComponent<Button>().onClick.AddListener(() => { selectedRecipe = captured; selectedJobId = null; Refresh(); });
            HubItemRowFactory.SetSelected(row, selectedRecipe != null && selectedRecipe.Id == recipe.Id);
        }

        private void AddJobRow(Transform list, CraftJob job)
        {
            var row = HubItemRowFactory.Create(list);
            var result = ItemLibrary.Get(job.ResultItemId);
            if (result != null)
            {
                HubItemRowFactory.SetIcon(row, result);
                HubItemRowFactory.SetLabel(row, $"{result.DisplayName} ×{job.ResultCount}");
                HubItemRowFactory.SetLabelColor(row, HubItemRowFactory.RarityColor(result.Rarity));
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
                HubItemRowFactory.SetSubLabel(row, $"<color=#FFB347>Brewing…  {CraftJobHelper.FormatRemaining(job.Remaining)}</color>");
                HubItemRowFactory.SetProgress(row, job.Progress01);
            }

            var capturedId = job.JobId;
            row.GetComponent<Button>().onClick.AddListener(() => { selectedRecipe = null; selectedJobId = capturedId; Refresh(); });
            HubItemRowFactory.SetSelected(row, selectedJobId == job.JobId);
        }

        private void ConfirmPressed()
        {
            if (!string.IsNullOrEmpty(selectedJobId))
            {
                var job = CraftJobHelper.ForStation(CraftStation.Alchemist).FirstOrDefault(j => j.JobId == selectedJobId);
                if (job == null) { selectedJobId = null; Refresh(); return; }
                if (!job.IsReady) return;
                if (!CraftJobHelper.Collect(job, Hub.Inventory)) return;
                HubToast.Show($"Collected {ItemLibrary.Get(job.ResultItemId)?.DisplayName ?? job.ResultItemId}");
                selectedJobId = null;
                Hub.PersistAndRefresh();
                return;
            }

            if (selectedRecipe == null) return;
            var started = CraftJobHelper.StartAlchemist(selectedRecipe, Hub.Inventory);
            if (started == null) return;
            HubToast.Show($"Brewing: {selectedRecipe.DisplayName}  ({CraftJobHelper.FormatRemaining(started.Remaining)})");
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
                var job = CraftJobHelper.ForStation(CraftStation.Alchemist).FirstOrDefault(j => j.JobId == selectedJobId);
                if (job == null) { btn.interactable = false; if (label != null) label.text = "—"; return; }
                if (job.IsReady) { btn.interactable = true; if (label != null) label.text = "Collect"; }
                else { btn.interactable = false; if (label != null) label.text = $"In Progress  {CraftJobHelper.FormatRemaining(job.Remaining)}"; }
                return;
            }

            if (selectedRecipe == null)
            {
                btn.interactable = false;
                if (label != null) label.text = "Select a recipe";
                return;
            }

            bool can = selectedRecipe.CanCraft(Hub.Inventory);
            btn.interactable = can;
            float seconds = Scripts.Utilities.Formulas.CraftDurationSeconds(selectedRecipe.GoldCost, selectedRecipe.Ingredients.Sum(i => i.Count));
            if (label != null) label.text = can ? $"Start Brewing  ({FormatDuration(seconds)})" : "Missing ingredients";
        }

        private void UpdateDetail()
        {
            var detail = FindLabel(GameObjectHelper.Hub.DetailLabel);
            if (detail == null) return;

            if (!string.IsNullOrEmpty(selectedJobId))
            {
                var job = CraftJobHelper.ForStation(CraftStation.Alchemist).FirstOrDefault(j => j.JobId == selectedJobId);
                if (job == null) { detail.text = "<b>Alchemist</b>\nJob complete."; return; }
                var result = ItemLibrary.Get(job.ResultItemId);
                string resultName = result?.DisplayName ?? job.ResultItemId;
                if (job.IsReady)
                    detail.text = $"<b>{resultName}</b> ×{job.ResultCount}\n<color=#55DD55>Your order is ready to collect.</color>";
                else
                    detail.text = $"<b>{resultName}</b> ×{job.ResultCount}\nBrewing in progress…\nTime remaining: <b>{CraftJobHelper.FormatRemaining(job.Remaining)}</b>";
                return;
            }

            if (selectedRecipe == null)
            {
                detail.text = "<b>Alchemist</b>\nSelect a recipe to brew potions.\nBrewing takes real time — come back when it's ready.";
                return;
            }
            var resultDef = ItemLibrary.Get(selectedRecipe.ResultItemId);
            string rname = resultDef?.DisplayName ?? selectedRecipe.ResultItemId;
            float seconds = Scripts.Utilities.Formulas.CraftDurationSeconds(selectedRecipe.GoldCost, selectedRecipe.Ingredients.Sum(i => i.Count));
            detail.text = $"<b>{selectedRecipe.DisplayName}</b>\nProduces: {rname} ×{selectedRecipe.ResultCount}\nCost: {HubTheme.FormatGold(selectedRecipe.GoldCost)}\n{FormatIngredients(selectedRecipe)}\n<color=#FFB347>Brew time: {FormatDuration(seconds)}</color>";
        }

        private string FormatIngredients(CraftingRecipe recipe)
        {
            var parts = new List<string>();
            foreach (var ing in recipe.Ingredients)
            {
                var def = ItemLibrary.Get(ing.ItemId);
                string name = def != null ? def.DisplayName : ing.ItemId;
                int owned = Hub.Inventory.CountOf(ing.ItemId);
                parts.Add($"{ing.Count}× {name} ({owned})");
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
