using System.Collections.Generic;
using System.Linq;
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
using Scripts.Managers;
using Scripts.Models;
using Scripts.Models.Actor;
using Scripts.Overworld;
using Scripts.Sequences;
using Scripts.Serialization;
using Scripts.Utilities;
using scene = Scripts.Helpers.SceneHelper;

namespace Scripts.Vendor.Store
{
    /// <summary>
    /// STOREMANAGER - Runtime controller for the Store scene.
    /// <para>PURPOSE: Self-contained vendor scene. Owns its own PlayerInventory hydrated from
    /// ProfileHelper.CurrentProfile.CurrentSave on Awake. Lists buyable materials + a basic
    /// healing potion. Click a row to select; Buy deducts gold, adds the item, and persists
    /// to disk. Back fades to Overworld.</para>
    /// <para>BOOT BEHAVIOR: Designed to work as a standalone start scene during dev. If no
    /// profile exists on disk, creates a "Dev" profile with default starter inventory so the
    /// scene is immediately playable in isolation.</para>
    /// <para>RELATED FILES: StoreScaffold.cs (Editor builder), ItemLibrary.cs, ProfileHelper.cs</para>
    /// </summary>
    public class StoreManager : MonoBehaviour
    {
        // ----- Object names (match StoreScaffold) -----
        public const string GoldLabelName = "GoldLabel";
        public const string ItemListContentPath = "Body/ItemList/Viewport/Content";
        public const string DetailLabelName = "Body/DetailLabel";
        public const string BuyButtonName = "Body/BuyButton";
        public const string BackButtonName = "BackButton";

        public PlayerInventory Inventory { get; private set; }

        private ItemDefinition selected;
        private TextMeshProUGUI goldLabel;
        private TextMeshProUGUI detailLabel;
        private RectTransform listContent;
        private Button buyButton;

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

        // ---------- Boot / persistence ----------

        private static void BootstrapProfile()
        {
            // If launched cold (Store as start scene), there's no profile in memory yet.
            // ProfileHelper.Load auto-discovers folders on disk; if there are none, fall back
            // to creating a Dev profile so the scene is immediately functional.
            if (ProfileHelper.CurrentProfile == null)
                ProfileHelper.Load();
            if (!ProfileHelper.HasCurrentSave)
                ProfileHelper.CreateProfile("Dev");
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

        // ---------- UI lookups & wiring ----------

        private void CacheUiReferences()
        {
            var canvas = GameObject.Find("Canvas");
            if (canvas == null) { Debug.LogError("[StoreManager] Canvas not found."); return; }

            goldLabel = FindLabel(canvas.transform, "Header/" + GoldLabelName);
            detailLabel = FindLabel(canvas.transform, DetailLabelName);

            var contentT = canvas.transform.Find(ItemListContentPath);
            listContent = contentT != null ? contentT.GetComponent<RectTransform>() : null;
            if (listContent == null) Debug.LogError("[StoreManager] ItemList Content not found at " + ItemListContentPath);

            var buyT = canvas.transform.Find(BuyButtonName);
            buyButton = buyT != null ? buyT.GetComponent<Button>() : null;
        }

        private void WireButtons()
        {
            if (buyButton != null)
            {
                buyButton.onClick.RemoveAllListeners();
                buyButton.onClick.AddListener(ConfirmBuy);
            }

            var canvas = GameObject.Find("Canvas");
            var backT = canvas != null ? canvas.transform.Find(BackButtonName) : null;
            var backBtn = backT != null ? backT.GetComponent<Button>() : null;
            if (backBtn != null)
            {
                backBtn.onClick.RemoveAllListeners();
                backBtn.onClick.AddListener(() => { PersistInventory(); scene.Fade.ToOverworld(); });
            }
        }

        // ---------- Catalogue ----------

        private IEnumerable<ItemDefinition> BuyCatalogue()
        {
            // Materials at entry prices first, then the basic healing potion at the bottom of the list.
            foreach (var mat in ItemLibrary.VendorMaterials())
                yield return mat;
            var potion = ItemLibrary.Get("healing_potion_basic");
            if (potion != null) yield return potion;
        }

        // ---------- Refresh ----------

        public void Refresh()
        {
            UpdateGoldLabel();
            RebuildList();
            UpdateDetail();
            UpdateBuyButtonInteractable();
        }

        private void UpdateGoldLabel()
        {
            if (goldLabel != null)
                goldLabel.text = "Gold: " + HubTheme.FormatGold(Inventory.Gold);
        }

        private void RebuildList()
        {
            if (listContent == null) return;
            for (int i = listContent.childCount - 1; i >= 0; i--)
                Object.Destroy(listContent.GetChild(i).gameObject);

            foreach (var item in BuyCatalogue())
                CreateRow(item);
        }

        private void UpdateDetail()
        {
            if (detailLabel == null) return;
            if (selected == null)
            {
                detailLabel.text = "<b>Store</b>\nBrowse to buy materials and basic supplies.\n\nClick a row to select an item.";
                return;
            }
            int owned = Inventory.CountOf(selected.Id);
            string ownedLine = owned > 0 ? $"\n\nOwned: {owned}" : "";
            detailLabel.text = $"<b>{selected.DisplayName}</b>\n{selected.Description}\n\nBuy: {HubTheme.FormatGold(selected.BaseCost)}{ownedLine}";
        }

        private void UpdateBuyButtonInteractable()
        {
            if (buyButton == null) return;
            buyButton.interactable = selected != null && Inventory.Gold >= selected.BaseCost;
        }

        // ---------- Row factory (inline — kept self-contained until 2nd vendor scene exists) ----------

        private void CreateRow(ItemDefinition item)
        {
            var go = new GameObject("Row_" + item.Id);
            go.layer = LayerMask.NameToLayer("UI");
            var rt = go.AddComponent<RectTransform>();
            rt.SetParent(listContent, false);
            rt.sizeDelta = new Vector2(0f, 56f);

            go.AddComponent<CanvasRenderer>();
            var bg = go.AddComponent<Image>();
            bg.color = (selected != null && selected.Id == item.Id)
                ? new Color(0.36f, 0.50f, 0.78f, 1f)
                : new Color(0.20f, 0.24f, 0.34f, 1f);
            bg.raycastTarget = true;

            var btn = go.AddComponent<Button>();
            btn.targetGraphic = bg;
            var captured = item;
            btn.onClick.AddListener(() => { selected = captured; Refresh(); });

            // Layout sizing — VerticalLayoutGroup parent honors LayoutElement.minHeight / preferredHeight.
            var le = go.AddComponent<LayoutElement>();
            le.minHeight = 56f;
            le.preferredHeight = 56f;
            le.flexibleWidth = 1f;

            // Label
            var labelGO = new GameObject("Label");
            labelGO.layer = LayerMask.NameToLayer("UI");
            var labelRT = labelGO.AddComponent<RectTransform>();
            labelRT.SetParent(rt, false);
            labelRT.anchorMin = Vector2.zero; labelRT.anchorMax = Vector2.one;
            labelRT.offsetMin = new Vector2(16f, 4f); labelRT.offsetMax = new Vector2(-16f, -4f);
            labelGO.AddComponent<CanvasRenderer>();
            var tmp = labelGO.AddComponent<TextMeshProUGUI>();
            tmp.text = $"{item.DisplayName}    {HubTheme.ColorByAffordable(HubTheme.FormatGold(item.BaseCost), Inventory.Gold >= item.BaseCost)}";
            tmp.fontSize = 24;
            tmp.color = HubItemRowFactory.RarityColor(item.Rarity);
            tmp.alignment = TextAlignmentOptions.MidlineLeft;
            tmp.enableWordWrapping = false;
            tmp.richText = true;
            tmp.raycastTarget = false;
        }

        // ---------- Buy ----------

        private void ConfirmBuy()
        {
            if (selected == null) return;
            if (Inventory.Gold < selected.BaseCost) return;
            if (!Inventory.Add(selected, 1)) return;
            Inventory.Gold -= selected.BaseCost;
            PersistInventory();
            Refresh();
        }

        // ---------- Tiny helpers ----------

        private static TextMeshProUGUI FindLabel(Transform root, string path)
        {
            var t = root.Find(path);
            return t != null ? t.GetComponent<TextMeshProUGUI>() : null;
        }
    }
}
