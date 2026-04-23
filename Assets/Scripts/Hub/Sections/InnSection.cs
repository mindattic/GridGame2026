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
    /// INNSECTION - Rest (combined former Medical + Residence).
    /// <para>PURPOSE: One-click gold sink that restores the party's HP/MP to full. Runtime combat
    /// state is not persisted across Hub visits today, so the "rest" is narrative — the gold is
    /// spent, the flavor text reassures the player, and the save is stamped. Cost scales with party
    /// size (25g per member).</para>
    /// <para>RELATED FILES: HubManager.cs, ProfileHelper.cs</para>
    /// </summary>
    public class InnSection : HubSection
    {
        private const int CostPerHero = 25;

        protected override void OnActivated()
        {
            Wire(FindButton("RestButton"), Rest);
        }

        public override void Refresh()
        {
            var detail = FindLabel(GameObjectHelper.Hub.DetailLabel);
            if (detail == null) return;
            int cost = PartyCost();
            bool canAfford = Hub.Inventory.Gold >= cost;
            detail.text = $"<b>The Wayfarer's Rest</b>\n"
                        + $"\"A warm meal, a soft bed. {HubTheme.FormatGold(cost)} for the lot of you.\"\n\n"
                        + $"Rest cost: {HubTheme.ColorByAffordable(HubTheme.FormatGold(cost), canAfford)}\n"
                        + $"Your gold: {HubTheme.FormatGold(Hub.Inventory.Gold)}";
        }

        private int PartyCost()
        {
            var party = ProfileHelper.CurrentProfile?.CurrentSave?.Party?.Members;
            int n = party?.Count ?? 0;
            return Mathf.Max(1, n) * CostPerHero;
        }

        private void Rest()
        {
            int cost = PartyCost();
            if (Hub.Inventory.Gold < cost) return;
            Hub.Inventory.Gold -= cost;
            Hub.PersistAndRefresh();
        }
    }
}
