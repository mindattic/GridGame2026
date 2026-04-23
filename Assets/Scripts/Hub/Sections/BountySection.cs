using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using g = Scripts.Helpers.GameHelper;
using Scripts.Canvas;
using Scripts.Data.Actor;
using Scripts.Data.Bounties;
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
    /// BOUNTYSECTION - One-slot bounty board for the hub.
    /// <para>PURPOSE: If no bounty is active, shows the full catalog — one tap accepts.
    /// If a bounty is active, shows only that bounty with progress; Confirm either
    /// <b>Claim Reward</b> (when complete) or <b>Abandon</b> (when not).</para>
    /// <para>RELATED FILES: BountyLibrary.cs, BountyHelper.cs, BountySaveData (Profile.cs)</para>
    /// </summary>
    public class BountySection : HubSection
    {
        private string selectedBountyId;

        protected override void OnActivated()
        {
            Wire(FindButton("ConfirmButton"), ConfirmPressed);
            Wire(FindButton("AbandonButton"), AbandonPressed);
        }

        public override void Refresh()
        {
            var list = FindList("ItemList/Viewport/Content");
            if (list == null) return;
            ClearList(list);

            var active = BountyHelper.ActiveBounty();
            if (active != null)
            {
                AddActiveRow(list, active);
            }
            else
            {
                selectedBountyId = null;
                foreach (var def in BountyLibrary.All())
                    AddOfferRow(list, def);
            }

            UpdateDetail();
            UpdateConfirmButton();
            UpdateAbandonButton();
        }

        private void AddOfferRow(Transform list, BountyDefinition def)
        {
            var row = HubItemRowFactory.Create(list);
            HubItemRowFactory.SetLabel(row, def.DisplayName);
            HubItemRowFactory.SetSubLabel(row,
                $"<color=#AAAAAA>{def.Biome} · kill {def.RequiredCount}× {def.TargetClass}</color>  " +
                $"<color=#FFD77A>reward: {HubTheme.FormatGold(def.RewardGold)}</color>");

            var capturedId = def.Id;
            row.GetComponent<Button>().onClick.AddListener(() => { selectedBountyId = capturedId; Refresh(); });
            HubItemRowFactory.SetSelected(row, selectedBountyId == def.Id);
        }

        private void AddActiveRow(Transform list, BountyDefinition def)
        {
            var row = HubItemRowFactory.Create(list);
            int progress = BountyHelper.ActiveProgress();
            bool complete = progress >= def.RequiredCount;

            HubItemRowFactory.SetLabel(row, def.DisplayName);
            string status = complete
                ? "<color=#55DD55><b>Complete — tap Confirm to claim.</b></color>"
                : $"<color=#FFB347>In progress: {progress}/{def.RequiredCount} {def.TargetClass}</color>";
            HubItemRowFactory.SetSubLabel(row, $"{def.Biome} · {status}");
            HubItemRowFactory.SetProgress(row, def.RequiredCount > 0
                ? Mathf.Clamp01((float)progress / def.RequiredCount)
                : 0f);
            HubItemRowFactory.SetSelected(row, true);
        }

        private void UpdateDetail()
        {
            var detail = FindLabel(GameObjectHelper.Hub.DetailLabel);
            if (detail == null) return;

            var active = BountyHelper.ActiveBounty();
            if (active != null)
            {
                int p = BountyHelper.ActiveProgress();
                bool complete = p >= active.RequiredCount;
                string rewardLine = $"<color=#FFD77A>Reward: {HubTheme.FormatGold(active.RewardGold)}";
                if (!string.IsNullOrEmpty(active.RewardItemId))
                {
                    var rewardDef = ItemLibrary.Get(active.RewardItemId);
                    string rewardName = rewardDef?.DisplayName ?? active.RewardItemId;
                    rewardLine += $" + {active.RewardItemCount}× {rewardName}";
                }
                rewardLine += "</color>";
                string prog = complete
                    ? "<color=#55DD55><b>Contract fulfilled.</b></color>"
                    : $"Progress: {p}/{active.RequiredCount}";
                detail.text = $"<b>{active.DisplayName}</b>\n<i>{active.Biome}</i>\n\n{active.Description}\n\n{prog}\n{rewardLine}";
                return;
            }

            if (string.IsNullOrEmpty(selectedBountyId))
            {
                detail.text = "<b>Bounty Board</b>\nOne contract at a time. Pick carefully — you'll have to abandon to switch.\n\nBounties reward gold and themed consumables, not XP.";
                return;
            }

            var sel = BountyLibrary.Get(selectedBountyId);
            if (sel == null) { detail.text = "<b>Bounty Board</b>"; return; }

            string selReward = $"<color=#FFD77A>Reward: {HubTheme.FormatGold(sel.RewardGold)}";
            if (!string.IsNullOrEmpty(sel.RewardItemId))
            {
                var rewardDef = ItemLibrary.Get(sel.RewardItemId);
                string rewardName = rewardDef?.DisplayName ?? sel.RewardItemId;
                selReward += $" + {sel.RewardItemCount}× {rewardName}";
            }
            selReward += "</color>";
            detail.text = $"<b>{sel.DisplayName}</b>\n<i>{sel.Biome}</i>\n\n{sel.Description}\n\nTarget: {sel.RequiredCount}× {sel.TargetClass}\n{selReward}";
        }

        private void UpdateConfirmButton()
        {
            var btn = FindButton("ConfirmButton");
            if (btn == null) return;
            var label = btn.GetComponentInChildren<TextMeshProUGUI>();

            if (BountyHelper.HasActive())
            {
                bool complete = BountyHelper.IsComplete();
                btn.interactable = complete;
                if (label != null) label.text = complete ? "Claim Reward" : "In Progress";
                return;
            }

            bool canAccept = !string.IsNullOrEmpty(selectedBountyId);
            btn.interactable = canAccept;
            if (label != null) label.text = canAccept ? "Accept Bounty" : "Select a bounty";
        }

        private void UpdateAbandonButton()
        {
            var btn = FindButton("AbandonButton");
            if (btn == null) return;
            btn.gameObject.SetActive(BountyHelper.HasActive() && !BountyHelper.IsComplete());
        }

        private void ConfirmPressed()
        {
            if (BountyHelper.HasActive())
            {
                if (!BountyHelper.IsComplete()) return;
                var def = BountyHelper.ActiveBounty();
                if (BountyHelper.ClaimReward(Hub.Inventory))
                    HubToast.Show($"Bounty complete — {def.DisplayName}");
                Hub.PersistAndRefresh();
                return;
            }

            if (string.IsNullOrEmpty(selectedBountyId)) return;
            var picked = BountyLibrary.Get(selectedBountyId);
            if (picked == null) return;
            if (!BountyHelper.Accept(picked.Id)) return;
            HubToast.Show($"Accepted: {picked.DisplayName}");
            selectedBountyId = null;
            Hub.PersistAndRefresh();
        }

        private void AbandonPressed()
        {
            if (!BountyHelper.HasActive()) return;
            var def = BountyHelper.ActiveBounty();
            BountyHelper.Abandon();
            HubToast.Show($"Abandoned: {def?.DisplayName}");
            Hub.PersistAndRefresh();
        }
    }
}
