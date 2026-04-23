using System;
using System.Collections.Generic;
using UnityEngine;

namespace Scripts.Models
{
    /// <summary>Where a <see cref="CraftJob"/> is being processed.</summary>
    public enum CraftStation
    {
        Blacksmith,
        Alchemist,
        Enchanter,
    }

    /// <summary>
    /// CRAFTJOB - A pending, time-gated crafting/upgrade order the player has placed
    /// with the Blacksmith or Alchemist.
    /// <para>PURPOSE: Materials, gold and (for Blacksmith upgrades) the source weapon are
    /// removed from the player's inventory the moment the order is placed — the vendor is
    /// "holding onto it". The job finishes at an absolute wall-clock timestamp
    /// (<see cref="FinishTicksUtc"/>) so it ticks even while the game is closed. Players return
    /// to the vendor to collect the result when <see cref="IsReady"/> is true. The design
    /// supports future MTX that shortens <see cref="FinishTicksUtc"/>.</para>
    /// <para>SERIALIZATION: uses `long` ticks (DateTime.UtcNow.Ticks) so Unity JsonUtility can
    /// round-trip the timestamps cleanly — DateTime struct serialization is unreliable.</para>
    /// <para>RELATED FILES: CraftJobHelper.cs, BlacksmithSection.cs, AlchemistSection.cs, Profile.cs</para>
    /// </summary>
    [Serializable]
    public class CraftJob
    {
        public CraftStation Station;

        /// <summary>Unique job id; used to distinguish rows and handle concurrent jobs on the same item.</summary>
        public string JobId;

        /// <summary>Item id produced when this job completes (looked up in ItemLibrary at collect time).</summary>
        public string ResultItemId;

        /// <summary>Number of result items produced.</summary>
        public int ResultCount = 1;

        /// <summary>Blacksmith only — the item id that was handed over and will be consumed.
        /// Alchemist jobs leave this empty (nothing was "traded in").</summary>
        public string ConsumedItemId;

        /// <summary>Gold paid up-front at job start. Persisted so refund logic could restore it if ever needed.</summary>
        public int GoldPaid;

        /// <summary>Materials consumed up-front; persisted for the same reason.</summary>
        public List<MaterialEntry> MaterialsPaid = new List<MaterialEntry>();

        public long StartTicksUtc;
        public long FinishTicksUtc;

        public CraftJob() { }

        public CraftJob(CraftJob other)
        {
            Station = other.Station;
            JobId = other.JobId;
            ResultItemId = other.ResultItemId;
            ResultCount = other.ResultCount;
            ConsumedItemId = other.ConsumedItemId;
            GoldPaid = other.GoldPaid;
            StartTicksUtc = other.StartTicksUtc;
            FinishTicksUtc = other.FinishTicksUtc;
            MaterialsPaid = new List<MaterialEntry>();
            if (other.MaterialsPaid != null)
                foreach (var m in other.MaterialsPaid)
                    MaterialsPaid.Add(new MaterialEntry(m));
        }

        public bool IsReady => DateTime.UtcNow.Ticks >= FinishTicksUtc;

        public TimeSpan Remaining
        {
            get
            {
                long delta = FinishTicksUtc - DateTime.UtcNow.Ticks;
                return delta <= 0L ? TimeSpan.Zero : new TimeSpan(delta);
            }
        }

        /// <summary>0 at start, 1 at (or past) completion.</summary>
        public float Progress01
        {
            get
            {
                long now = DateTime.UtcNow.Ticks;
                if (now >= FinishTicksUtc) return 1f;
                long total = FinishTicksUtc - StartTicksUtc;
                if (total <= 0L) return 1f;
                long done = now - StartTicksUtc;
                return Mathf.Clamp01((float)done / total);
            }
        }

        [Serializable]
        public class MaterialEntry
        {
            public string Id;
            public int Count;

            public MaterialEntry() { }
            public MaterialEntry(string id, int count) { Id = id; Count = count; }
            public MaterialEntry(MaterialEntry other) { Id = other.Id; Count = other.Count; }
        }
    }
}
