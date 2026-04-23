using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using g = Scripts.Helpers.GameHelper;
using scene = Scripts.Helpers.SceneHelper;
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
    /// PLACESSECTION - Biome selection hub replacing the Overworld entry.
    /// <para>PURPOSE: Shows four biome buttons (Field, Forest, Ruins, Cave) plus a Boss entry.
    /// Each biome maps to a themed enemy pool that drops biome-specific materials. Selecting
    /// a biome locks in the first stage tagged with that biome and fades to Game.</para>
    /// <para>DESIGN INTENT: Temporary stand-in for the full Overworld scene while we build out
    /// stocking-up flows (bring Holy Water to the Ruins, Flame Oil to the Cave, etc.).</para>
    /// <para>RELATED FILES: HubManager.cs, StageLibrary.cs (Biome enum), HubScaffold.cs</para>
    /// </summary>
    public class PlacesSection : HubSection
    {
        private Biome selected = Biome.None;

        protected override void OnActivated()
        {
            WireBiomeButton("FieldButton", Biome.Field);
            WireBiomeButton("ForestButton", Biome.Forest);
            WireBiomeButton("RuinsButton", Biome.Ruins);
            WireBiomeButton("CaveButton", Biome.Cave);
            WireBiomeButton("BossButton", Biome.Boss);

            Wire(FindButton("ConfirmButton"), ConfirmPressed);
        }

        public override void Refresh()
        {
            UpdateDetail();
            UpdateConfirmButton();
            UpdateBiomeHighlights();
        }

        private void WireBiomeButton(string buttonName, Biome biome)
        {
            var btn = FindButton(buttonName);
            if (btn == null) return;
            btn.onClick.RemoveAllListeners();
            btn.onClick.AddListener(() => { selected = biome; Refresh(); });
        }

        private void ConfirmPressed()
        {
            if (selected == Biome.None) return;
            var stage = StageLibrary.GetFirstByBiome(selected);
            if (stage == null)
            {
                HubToast.Show($"No stages available in {selected}.");
                return;
            }

            var save = ProfileHelper.CurrentProfile?.LatestSave;
            if (save != null) save.Stage.CurrentStage = stage.Name;

            Hub.PersistAndRefresh();
            scene.Fade.ToGame();
        }

        private void UpdateDetail()
        {
            var detail = FindLabel(GameObjectHelper.Hub.DetailLabel);
            if (detail == null) return;

            if (selected == Biome.None)
            {
                detail.text = "<b>Places</b>\nWhere will you hunt today?\n\nEach biome shelters different prey — and different materials. Stock consumables that match the threat before you go.";
                return;
            }

            detail.text = DescribeBiome(selected);
        }

        private string DescribeBiome(Biome biome)
        {
            switch (biome)
            {
                case Biome.Field:
                    return "<b>Field</b>\nOpen grassland. Slimes, scorpions, and scavenging frogs.\n\n<i>Drops: common materials — slime gel, chitin.</i>";
                case Biome.Forest:
                    return "<b>Forest</b>\nDense wood. Wolf packs, tree golems, werewolves at dusk.\n\n<i>Drops: pelts, bark, fang fragments.</i>\n<color=#FFB347>Bring Flame Oil — beasts burn well.</color>";
                case Biome.Ruins:
                    return "<b>Ruins</b>\nCrumbling stonework. Undead walk here, and ceramic knights stand watch.\n\n<i>Drops: bone, shards, forgotten coin.</i>\n<color=#FFB347>Bring Holy Water — the dead resent it.</color>";
                case Biome.Cave:
                    return "<b>Cave</b>\nLightless tunnels. Cyclops, trolls, yetis waiting in the cold.\n\n<i>Drops: ore, gem fragments, frost pelts.</i>";
                case Biome.Boss:
                    return "<b>Boss</b>\nA bespoke engagement against a named enemy.\n\n<color=#FF6666>Difficulty: extreme.</color> Expect to lose without themed consumables and upgraded gear.";
                default:
                    return "<b>Places</b>";
            }
        }

        private void UpdateConfirmButton()
        {
            var btn = FindButton("ConfirmButton");
            if (btn == null) return;
            var label = btn.GetComponentInChildren<TextMeshProUGUI>();

            if (selected == Biome.None)
            {
                btn.interactable = false;
                if (label != null) label.text = "Choose a biome";
                return;
            }

            var stage = StageLibrary.GetFirstByBiome(selected);
            btn.interactable = stage != null;
            if (label != null)
                label.text = stage != null ? $"Travel to {selected}" : $"No {selected} stages";
        }

        private void UpdateBiomeHighlights()
        {
            HighlightBiomeButton("FieldButton", Biome.Field);
            HighlightBiomeButton("ForestButton", Biome.Forest);
            HighlightBiomeButton("RuinsButton", Biome.Ruins);
            HighlightBiomeButton("CaveButton", Biome.Cave);
            HighlightBiomeButton("BossButton", Biome.Boss);
        }

        private void HighlightBiomeButton(string buttonName, Biome biome)
        {
            var btn = FindButton(buttonName);
            if (btn == null) return;
            var img = btn.GetComponent<Image>();
            if (img == null) return;
            img.color = selected == biome ? HubTheme.NavActive : HubTheme.NavIdle;
        }
    }
}
