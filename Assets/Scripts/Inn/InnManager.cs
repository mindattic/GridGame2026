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

namespace Scripts.Vendor.Inn
{
    /// <summary>
    /// INNMANAGER - Runtime controller for the Inn scene.
    /// <para>PURPOSE: One-click gold sink that "rests" the party. Cost is <c>CostPerHero</c>
    /// (25g) × party size, minimum 1 hero's worth even if the roster is empty. Combat HP/MP
    /// is not persisted across scenes today, so the rest is narrative — gold is consumed,
    /// the player gets a flavor confirmation, and the save is stamped. The button stays
    /// disabled when gold is insufficient.</para>
    /// <para>RELATED FILES: InnScaffold.cs, ProfileHelper.cs, InnSection.cs (legacy Hub-era source)</para>
    /// </summary>
    public class InnManager : MonoBehaviour
    {
        public const string GoldLabelName = "GoldLabel";
        public const string DetailLabelName = "Body/DetailLabel";
        public const string FlashLabelName = "Body/FlashLabel";
        public const string RestButtonName = "Body/RestButton";
        public const string BackButtonName = "BackButton";

        private const int CostPerHero = 25;

        public PlayerInventory Inventory { get; private set; }

        private TextMeshProUGUI goldLabel;
        private TextMeshProUGUI detailLabel;
        private TextMeshProUGUI flashLabel;
        private Button restButton;

        private void Awake()
        {
            BootstrapProfile();
            HydrateInventoryFromSave();
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

        private void HydrateInventoryFromSave()
        {
            Inventory = new PlayerInventory();
            var save = ProfileHelper.CurrentProfile?.CurrentSave;
            if (save?.Inventory != null) Inventory.LoadFromSaveData(save.Inventory);
        }

        private void PersistInventory()
        {
            var save = ProfileHelper.CurrentProfile?.CurrentSave;
            if (save == null) return;
            save.Inventory = Inventory.ToSaveData();
            ProfileHelper.Save(overwrite: true);
        }

        private void CacheUiReferences()
        {
            var canvas = GameObject.Find("Canvas");
            if (canvas == null) { Debug.LogError("[InnManager] Canvas not found."); return; }

            goldLabel = FindLabel(canvas.transform, "Header/" + GoldLabelName);
            detailLabel = FindLabel(canvas.transform, DetailLabelName);
            flashLabel = FindLabel(canvas.transform, FlashLabelName);
            if (flashLabel != null) flashLabel.text = "";

            var restT = canvas.transform.Find(RestButtonName);
            restButton = restT != null ? restT.GetComponent<Button>() : null;
        }

        private void WireButtons()
        {
            if (restButton != null)
            {
                restButton.onClick.RemoveAllListeners();
                restButton.onClick.AddListener(ConfirmRest);
            }

            var canvas = GameObject.Find("Canvas");
            var backT = canvas != null ? canvas.transform.Find(BackButtonName) : null;
            var backBtn = backT != null ? backT.GetComponent<Button>() : null;
            if (backBtn != null)
            {
                backBtn.onClick.RemoveAllListeners();
                backBtn.onClick.AddListener(() => { PersistInventory(); scene.Fade.ToStageSelect(); });
            }
        }

        private static int PartyCost()
        {
            var party = ProfileHelper.CurrentProfile?.CurrentSave?.Party?.Members;
            int n = party?.Count ?? 0;
            return Mathf.Max(1, n) * CostPerHero;
        }

        public void Refresh()
        {
            int cost = PartyCost();
            bool canAfford = Inventory.Gold >= cost;

            if (goldLabel != null) goldLabel.text = "Gold: " + HubTheme.FormatGold(Inventory.Gold);

            if (detailLabel != null)
            {
                var sb = new System.Text.StringBuilder();
                sb.Append("<b>The Wayfarer's Rest</b>\n");
                sb.Append("<i>\"A warm meal, a soft bed. ")
                  .Append(HubTheme.FormatGold(cost))
                  .Append(" for the lot of you.\"</i>\n\n");
                sb.Append("Rest cost: ").Append(HubTheme.ColorByAffordable(HubTheme.FormatGold(cost), canAfford)).Append('\n');
                sb.Append("Your gold: ").Append(HubTheme.FormatGold(Inventory.Gold));
                detailLabel.text = sb.ToString();
            }

            if (restButton != null) restButton.interactable = canAfford;
        }

        private void ConfirmRest()
        {
            int cost = PartyCost();
            if (Inventory.Gold < cost) return;
            Inventory.Gold -= cost;
            if (flashLabel != null)
                flashLabel.text = "<color=#66cc88>Rested. The party feels renewed.</color>";
            PersistInventory();
            Refresh();
        }

        private static TextMeshProUGUI FindLabel(Transform root, string path)
        {
            var t = root.Find(path);
            return t != null ? t.GetComponent<TextMeshProUGUI>() : null;
        }
    }
}
