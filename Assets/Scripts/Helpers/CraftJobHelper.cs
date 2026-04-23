using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Scripts.Data.Items;
using Scripts.Inventory;
using Scripts.Libraries;
using Scripts.Models;
using Scripts.Utilities;

namespace Scripts.Helpers
{
    /// <summary>
    /// CRAFTJOBHELPER - Start / query / collect time-gated vendor orders.
    /// <para>PURPOSE: Shared entry points for <see cref="Scripts.Hub.Sections.BlacksmithSection"/>
    /// and <see cref="Scripts.Hub.Sections.AlchemistSection"/> so they don't duplicate the
    /// "consume-at-start / produce-at-collect" split that async crafting needs.</para>
    /// <para>OWNERSHIP: jobs live in the active <see cref="SaveState.CraftJobs"/> list. Callers
    /// are responsible for triggering <see cref="ProfileHelper.Save"/> (via
    /// <c>HubManager.PersistAndRefresh</c>) after any mutation so the list hits disk.</para>
    /// <para>RELATED FILES: CraftJob.cs, BlacksmithSection.cs, AlchemistSection.cs, ProfileHelper.cs</para>
    /// </summary>
    public static class CraftJobHelper
    {
        private static List<CraftJob> Jobs
        {
            get
            {
                var save = ProfileHelper.CurrentProfile?.CurrentSave;
                if (save == null) return null;
                if (save.CraftJobs == null) save.CraftJobs = new List<CraftJob>();
                return save.CraftJobs;
            }
        }

        public static IEnumerable<CraftJob> All => Jobs ?? Enumerable.Empty<CraftJob>();

        public static IEnumerable<CraftJob> ForStation(CraftStation station)
            => All.Where(j => j != null && j.Station == station);

        /// <summary>Any pending job — in-progress OR ready-to-collect.</summary>
        public static bool AnyFor(CraftStation station)
            => ForStation(station).Any();

        /// <summary>Is this base-item id currently handed over to the Blacksmith?</summary>
        public static bool IsHeldByBlacksmith(string itemId)
            => !string.IsNullOrEmpty(itemId) && ForStation(CraftStation.Blacksmith)
                .Any(j => j.ConsumedItemId == itemId);

        /// <summary>Is this base-item id currently handed over to the Enchanter?</summary>
        public static bool IsHeldByEnchanter(string itemId)
            => !string.IsNullOrEmpty(itemId) && ForStation(CraftStation.Enchanter)
                .Any(j => j.ConsumedItemId == itemId);

        /// <summary>True if any vendor is currently holding this item (Blacksmith or Enchanter).
        /// Salvage / equip / sell flows must exclude items that are in-flight.</summary>
        public static bool IsHeldByAnyVendor(string itemId)
            => IsHeldByBlacksmith(itemId) || IsHeldByEnchanter(itemId);

        /// <summary>Active job for the Blacksmith upgrade whose source item is <paramref name="itemId"/>.
        /// Null if none.</summary>
        public static CraftJob FindBlacksmithJobForBase(string itemId)
            => ForStation(CraftStation.Blacksmith).FirstOrDefault(j => j.ConsumedItemId == itemId);

        /// <summary>Active job for the Enchanter enchantment whose source item is <paramref name="itemId"/>.
        /// Null if none.</summary>
        public static CraftJob FindEnchanterJobForBase(string itemId)
            => ForStation(CraftStation.Enchanter).FirstOrDefault(j => j.ConsumedItemId == itemId);

        /// <summary>Active job for the Alchemist recipe whose result is <paramref name="resultItemId"/>.
        /// Null if none.</summary>
        public static CraftJob FindAlchemistJobForResult(string resultItemId)
            => ForStation(CraftStation.Alchemist).FirstOrDefault(j => j.ResultItemId == resultItemId);

        // -----------------------------------------------------------------------
        // Start
        // -----------------------------------------------------------------------

        public static CraftJob StartBlacksmith(UpgradeRecipe recipe, PlayerInventory inv)
        {
            if (recipe == null || inv == null || recipe.From == null || recipe.To == null) return null;
            if (!recipe.CanUpgrade(inv)) return null;
            var jobs = Jobs;
            if (jobs == null) return null;

            // Consume inputs atomically — same bookkeeping UpgradeRecipe.Execute does, minus the Add.
            inv.Gold -= recipe.GoldCost;
            inv.Remove(recipe.From.Id, 1);
            foreach (var m in recipe.Materials) inv.Remove(m.itemId, m.count);

            int totalMats = 1; // base item counts as 1 "unit" of effort
            foreach (var m in recipe.Materials) totalMats += m.count;

            long nowTicks = DateTime.UtcNow.Ticks;
            long durationTicks = (long)(Formulas.CraftDurationSeconds(recipe.GoldCost, totalMats) * TimeSpan.TicksPerSecond);

            var job = new CraftJob
            {
                Station = CraftStation.Blacksmith,
                JobId = Guid.NewGuid().ToString("N"),
                ResultItemId = recipe.To.Id,
                ResultCount = 1,
                ConsumedItemId = recipe.From.Id,
                GoldPaid = recipe.GoldCost,
                StartTicksUtc = nowTicks,
                FinishTicksUtc = nowTicks + durationTicks,
            };
            foreach (var m in recipe.Materials)
                job.MaterialsPaid.Add(new CraftJob.MaterialEntry(m.itemId, m.count));

            jobs.Add(job);
            return job;
        }

        public static CraftJob StartEnchant(EnchantRecipe recipe, PlayerInventory inv)
        {
            if (recipe == null || inv == null || recipe.From == null || recipe.To == null) return null;
            if (!recipe.CanEnchant(inv)) return null;
            var jobs = Jobs;
            if (jobs == null) return null;

            inv.Gold -= recipe.GoldCost;
            inv.Remove(recipe.From.Id, 1);
            foreach (var m in recipe.Materials) inv.Remove(m.itemId, m.count);

            int totalMats = 1;
            foreach (var m in recipe.Materials) totalMats += m.count;

            long nowTicks = DateTime.UtcNow.Ticks;
            long durationTicks = (long)(Formulas.CraftDurationSeconds(recipe.GoldCost, totalMats) * TimeSpan.TicksPerSecond);

            var job = new CraftJob
            {
                Station = CraftStation.Enchanter,
                JobId = Guid.NewGuid().ToString("N"),
                ResultItemId = recipe.To.Id,
                ResultCount = 1,
                ConsumedItemId = recipe.From.Id,
                GoldPaid = recipe.GoldCost,
                StartTicksUtc = nowTicks,
                FinishTicksUtc = nowTicks + durationTicks,
            };
            foreach (var m in recipe.Materials)
                job.MaterialsPaid.Add(new CraftJob.MaterialEntry(m.itemId, m.count));

            jobs.Add(job);
            return job;
        }

        public static CraftJob StartAlchemist(CraftingRecipe recipe, PlayerInventory inv)
        {
            if (recipe == null || inv == null || string.IsNullOrEmpty(recipe.ResultItemId)) return null;
            if (!recipe.CanCraft(inv)) return null;
            var jobs = Jobs;
            if (jobs == null) return null;

            foreach (var ing in recipe.Ingredients) inv.Remove(ing.ItemId, ing.Count);
            inv.Gold -= recipe.GoldCost;

            int totalMats = 0;
            foreach (var ing in recipe.Ingredients) totalMats += ing.Count;

            long nowTicks = DateTime.UtcNow.Ticks;
            long durationTicks = (long)(Formulas.CraftDurationSeconds(recipe.GoldCost, totalMats) * TimeSpan.TicksPerSecond);

            var job = new CraftJob
            {
                Station = CraftStation.Alchemist,
                JobId = Guid.NewGuid().ToString("N"),
                ResultItemId = recipe.ResultItemId,
                ResultCount = Mathf.Max(1, recipe.ResultCount),
                ConsumedItemId = null,
                GoldPaid = recipe.GoldCost,
                StartTicksUtc = nowTicks,
                FinishTicksUtc = nowTicks + durationTicks,
            };
            foreach (var ing in recipe.Ingredients)
                job.MaterialsPaid.Add(new CraftJob.MaterialEntry(ing.ItemId, ing.Count));

            jobs.Add(job);
            return job;
        }

        // -----------------------------------------------------------------------
        // Collect
        // -----------------------------------------------------------------------

        /// <summary>Adds the job's output to the inventory and removes the job from the save.
        /// Returns true on success; false if the job isn't ready or the result item id is invalid.</summary>
        public static bool Collect(CraftJob job, PlayerInventory inv)
        {
            if (job == null || inv == null || !job.IsReady) return false;
            var jobs = Jobs;
            if (jobs == null) return false;

            var def = ItemLibrary.Get(job.ResultItemId);
            if (def == null)
            {
                Debug.LogWarning($"[CraftJobHelper] Collect: could not resolve result item '{job.ResultItemId}'. Job removed without payout.");
                jobs.RemoveAll(j => j != null && j.JobId == job.JobId);
                return false;
            }

            if (!inv.Add(def, Mathf.Max(1, job.ResultCount)))
            {
                Debug.LogWarning($"[CraftJobHelper] Inventory refused '{def.DisplayName}' — job stays pending for retry.");
                return false;
            }

            jobs.RemoveAll(j => j != null && j.JobId == job.JobId);
            return true;
        }

        /// <summary>Formats a countdown as mm:ss or hh:mm:ss. Short and readable in UI rows.</summary>
        public static string FormatRemaining(TimeSpan t)
        {
            if (t.TotalSeconds <= 0) return "0:00";
            if (t.TotalHours >= 1) return $"{(int)t.TotalHours}:{t.Minutes:00}:{t.Seconds:00}";
            return $"{t.Minutes}:{t.Seconds:00}";
        }
    }
}
