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

namespace Scripts.Vendor
{
    /// <summary>
    /// VENDORMANAGER - Runtime controller for the Vendor scene.
    /// <para>PURPOSE: Phone-portrait vendor UI with a Buy/Sell toggle. The 0-10vh strip is the
    /// header (hamburger + Merchant title), 10-20vh is the mode toggle, 20-90vh is a scrollable
    /// list of rows with [-] N [+] steppers, and 90-100vh is the cost/value label plus the
    /// commit button. Buy and Sell each have their own cart; switching tabs preserves the other
    /// tab's pending quantities until commit.</para>
    /// <para>BOOT BEHAVIOR: Designed to work as a standalone start scene during dev. If no
    /// profile exists on disk, creates a "Dev" profile with default starter inventory so the
    /// scene is immediately playable in isolation.</para>
    /// <para>RELATED FILES: VendorBuilder.cs (Editor builder), ItemLibrary.cs, ProfileHelper.cs</para>
    /// </summary>
    public class VendorManager : MonoBehaviour
    {
        // ----- Object names (match VendorBuilder) -----
        public const string TitleLabelName = "TitleLabel";
        public const string BuyTabButtonName = "BuyTabButton";
        public const string SellTabButtonName = "SellTabButton";
        public const string TotalLabelName = "TotalLabel";
        public const string ActionButtonName = "ActionButton";

        private const string ListContentPath = "List/Viewport/Content";

        // Items sell back to vendors at half their base cost (floored).
        private const float SellPriceRatio = 0.5f;

        private enum VendorMode { Buy, Sell }

        public PlayerInventory Inventory { get; private set; }

        private VendorMode mode = VendorMode.Buy;
        private readonly Dictionary<string, int> buyCart = new Dictionary<string, int>();
        private readonly Dictionary<string, int> sellCart = new Dictionary<string, int>();

        // Per-row stepper qty labels, keyed by item id. Cleared on each RebuildList.
        private readonly Dictionary<string, TextMeshProUGUI> rowQtyLabels = new Dictionary<string, TextMeshProUGUI>();

        private TextMeshProUGUI totalLabel;
        private RectTransform listContent;
        private Button buyTabButton;
        private Button sellTabButton;
        private Image buyTabImage;
        private Image sellTabImage;
        private Button actionButton;
        private TextMeshProUGUI actionLabel;

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
            if (canvas == null) { Debug.LogError("[VendorManager] Canvas not found."); return; }

            totalLabel = FindLabel(canvas.transform, "FooterBar/" + TotalLabelName);

            var contentT = canvas.transform.Find(ListContentPath);
            listContent = contentT != null ? contentT.GetComponent<RectTransform>() : null;
            if (listContent == null) Debug.LogError("[VendorManager] List Content not found at " + ListContentPath);

            var buyT = canvas.transform.Find("ModeBar/" + BuyTabButtonName);
            buyTabButton = buyT != null ? buyT.GetComponent<Button>() : null;
            buyTabImage = buyT != null ? buyT.GetComponent<Image>() : null;

            var sellT = canvas.transform.Find("ModeBar/" + SellTabButtonName);
            sellTabButton = sellT != null ? sellT.GetComponent<Button>() : null;
            sellTabImage = sellT != null ? sellT.GetComponent<Image>() : null;

            var actionT = canvas.transform.Find("FooterBar/" + ActionButtonName);
            actionButton = actionT != null ? actionT.GetComponent<Button>() : null;
            actionLabel = actionT != null ? actionT.GetComponentInChildren<TextMeshProUGUI>() : null;
        }

        private void WireButtons()
        {
            if (buyTabButton != null)
            {
                buyTabButton.onClick.RemoveAllListeners();
                buyTabButton.onClick.AddListener(() => SetMode(VendorMode.Buy));
            }
            if (sellTabButton != null)
            {
                sellTabButton.onClick.RemoveAllListeners();
                sellTabButton.onClick.AddListener(() => SetMode(VendorMode.Sell));
            }
            if (actionButton != null)
            {
                actionButton.onClick.RemoveAllListeners();
                actionButton.onClick.AddListener(CommitCart);
            }
        }

        // ---------- Catalogues ----------

        private IEnumerable<ItemDefinition> BuyCatalogue()
        {
            foreach (var mat in ItemLibrary.VendorMaterials())
                yield return mat;
            var potion = ItemLibrary.Get("healing_potion_basic");
            if (potion != null) yield return potion;
        }

        private IEnumerable<PlayerInventory.Entry> SellCatalogue()
        {
            return Inventory.All().Where(e => e.Definition != null && e.Definition.BaseCost > 0 && e.Count > 0);
        }

        private static int SellPriceFor(ItemDefinition item) => Mathf.Max(1, Mathf.FloorToInt(item.BaseCost * SellPriceRatio));

        // ---------- Mode toggle ----------

        private void SetMode(VendorMode next)
        {
            if (mode == next) return;
            mode = next;
            Refresh();
        }

        // ---------- Refresh ----------

        public void Refresh()
        {
            UpdateTabTints();
            RebuildList();
            UpdateFooter();
        }

        private void UpdateTabTints()
        {
            if (buyTabImage != null)
                buyTabImage.color = (mode == VendorMode.Buy) ? HubTheme.Accent : HubTheme.NavIdle;
            if (sellTabImage != null)
                sellTabImage.color = (mode == VendorMode.Sell) ? HubTheme.Accent : HubTheme.NavIdle;
        }

        private void RebuildList()
        {
            if (listContent == null) return;
            for (int i = listContent.childCount - 1; i >= 0; i--)
                Object.Destroy(listContent.GetChild(i).gameObject);
            rowQtyLabels.Clear();

            if (mode == VendorMode.Buy)
            {
                foreach (var item in BuyCatalogue())
                    CreateBuyRow(item);
            }
            else
            {
                foreach (var entry in SellCatalogue())
                    CreateSellRow(entry);
            }
        }

        private void UpdateFooter()
        {
            int total = ComputeCartTotal();
            if (totalLabel != null)
            {
                string verb = (mode == VendorMode.Buy) ? "Pay" : "Receive";
                string totalStr = HubTheme.FormatGold(total);
                string goldStr = HubTheme.FormatGold(Inventory.Gold);
                bool affordable = mode != VendorMode.Buy || Inventory.Gold >= total;
                string totalColored = HubTheme.ColorByAffordable(totalStr, affordable);
                totalLabel.text = $"{verb}: {totalColored}  |  Gold: {goldStr}";
            }

            if (actionLabel != null)
                actionLabel.text = (mode == VendorMode.Buy) ? "Buy" : "Sell";

            if (actionButton != null)
            {
                bool hasItems = total > 0;
                bool affordable = mode != VendorMode.Buy || Inventory.Gold >= total;
                actionButton.interactable = hasItems && affordable;
            }
        }

        private int ComputeCartTotal()
        {
            int total = 0;
            if (mode == VendorMode.Buy)
            {
                foreach (var kvp in buyCart)
                {
                    var def = ItemLibrary.Get(kvp.Key);
                    if (def != null) total += def.BaseCost * kvp.Value;
                }
            }
            else
            {
                foreach (var kvp in sellCart)
                {
                    var def = ItemLibrary.Get(kvp.Key);
                    if (def != null) total += SellPriceFor(def) * kvp.Value;
                }
            }
            return total;
        }

        // ---------- Row factories ----------

        private void CreateBuyRow(ItemDefinition item)
        {
            int owned = Inventory.CountOf(item.Id);
            int maxAdd = Mathf.Max(0, item.MaxStack - owned);
            int unitPrice = item.BaseCost;
            CreateStepperRow(
                itemId: item.Id,
                displayName: item.DisplayName,
                rarity: item.Rarity,
                unitPrice: unitPrice,
                maxQty: maxAdd,
                cart: buyCart);
        }

        private void CreateSellRow(PlayerInventory.Entry entry)
        {
            int unitPrice = SellPriceFor(entry.Definition);
            CreateStepperRow(
                itemId: entry.Definition.Id,
                displayName: entry.Definition.DisplayName,
                rarity: entry.Definition.Rarity,
                unitPrice: unitPrice,
                maxQty: entry.Count,
                cart: sellCart);
        }

        private void CreateStepperRow(string itemId, string displayName, ItemRarity rarity,
            int unitPrice, int maxQty, Dictionary<string, int> cart)
        {
            var go = new GameObject("Row_" + itemId);
            go.layer = LayerMask.NameToLayer("UI");
            var rt = go.AddComponent<RectTransform>();
            rt.SetParent(listContent, false);
            rt.sizeDelta = new Vector2(0f, 64f);
            go.AddComponent<CanvasRenderer>();
            var bg = go.AddComponent<Image>();
            bg.color = HubTheme.RowBg;
            bg.raycastTarget = true;

            var le = go.AddComponent<LayoutElement>();
            le.minHeight = 64f;
            le.preferredHeight = 64f;
            le.flexibleWidth = 1f;

            var hlg = go.AddComponent<HorizontalLayoutGroup>();
            hlg.padding = new RectOffset(16, 16, 8, 8);
            hlg.spacing = 12f;
            hlg.childAlignment = TextAnchor.MiddleLeft;
            hlg.childControlWidth = true;
            hlg.childControlHeight = true;
            hlg.childForceExpandWidth = false;
            hlg.childForceExpandHeight = false;

            // Name + price (flex)
            var labelTmp = MakeRowLabel(rt, "Label",
                $"{displayName}    <color=#FFE082>{HubTheme.FormatGold(unitPrice)}</color>",
                HubItemRowFactory.RarityColor(rarity),
                TextAlignmentOptions.MidlineLeft);
            var labelLE = labelTmp.gameObject.AddComponent<LayoutElement>();
            labelLE.flexibleWidth = 1f;
            labelLE.minWidth = 0f;

            // Stepper: [-] N [+]
            var minusBtn = MakeStepperButton(rt, "Minus", "−");
            var qtyLabel = MakeRowLabel(rt, "Qty", "0", HubTheme.TextLight, TextAlignmentOptions.Center);
            qtyLabel.fontSize = 28;
            var qtyLE = qtyLabel.gameObject.AddComponent<LayoutElement>();
            qtyLE.minWidth = 56f;
            qtyLE.preferredWidth = 56f;
            qtyLE.flexibleWidth = 0f;
            var plusBtn = MakeStepperButton(rt, "Plus", "+");

            rowQtyLabels[itemId] = qtyLabel;

            // Seed N from existing cart state (so toggle round-trip preserves quantity).
            cart.TryGetValue(itemId, out int currentQty);
            if (currentQty > maxQty) currentQty = maxQty;
            if (currentQty < 0) currentQty = 0;
            cart[itemId] = currentQty;
            qtyLabel.text = currentQty.ToString();

            string capturedId = itemId;
            int capturedMax = maxQty;
            Dictionary<string, int> capturedCart = cart;

            minusBtn.onClick.AddListener(() =>
            {
                capturedCart.TryGetValue(capturedId, out int q);
                if (q > 0) q--;
                capturedCart[capturedId] = q;
                qtyLabel.text = q.ToString();
                UpdateFooter();
            });

            plusBtn.onClick.AddListener(() =>
            {
                capturedCart.TryGetValue(capturedId, out int q);
                if (q < capturedMax) q++;
                capturedCart[capturedId] = q;
                qtyLabel.text = q.ToString();
                UpdateFooter();
            });
        }

        private static TextMeshProUGUI MakeRowLabel(RectTransform parent, string name, string text,
            Color color, TextAlignmentOptions align)
        {
            var go = new GameObject(name);
            go.layer = LayerMask.NameToLayer("UI");
            var rt = go.AddComponent<RectTransform>();
            rt.SetParent(parent, false);
            go.AddComponent<CanvasRenderer>();
            var tmp = go.AddComponent<TextMeshProUGUI>();
            tmp.font = UiFonts.Body;
            tmp.text = text;
            tmp.fontSize = 24;
            tmp.color = color;
            tmp.alignment = align;
            tmp.enableWordWrapping = false;
            tmp.richText = true;
            tmp.raycastTarget = false;
            return tmp;
        }

        private static Button MakeStepperButton(RectTransform parent, string name, string label)
        {
            var go = new GameObject(name);
            go.layer = LayerMask.NameToLayer("UI");
            var rt = go.AddComponent<RectTransform>();
            rt.SetParent(parent, false);
            go.AddComponent<CanvasRenderer>();
            var img = go.AddComponent<Image>();
            img.color = HubTheme.NavIdle;
            img.raycastTarget = true;
            var btn = go.AddComponent<Button>();
            btn.targetGraphic = img;

            var le = go.AddComponent<LayoutElement>();
            le.minWidth = 48f;
            le.preferredWidth = 48f;
            le.minHeight = 48f;
            le.preferredHeight = 48f;
            le.flexibleWidth = 0f;
            le.flexibleHeight = 0f;

            var labelGO = new GameObject("Label");
            labelGO.layer = LayerMask.NameToLayer("UI");
            var labelRT = labelGO.AddComponent<RectTransform>();
            labelRT.SetParent(rt, false);
            labelRT.anchorMin = Vector2.zero; labelRT.anchorMax = Vector2.one;
            labelRT.offsetMin = labelRT.offsetMax = Vector2.zero;
            labelGO.AddComponent<CanvasRenderer>();
            var tmp = labelGO.AddComponent<TextMeshProUGUI>();
            tmp.font = UiFonts.Body;
            tmp.text = label;
            tmp.fontSize = 32;
            tmp.fontStyle = FontStyles.Bold;
            tmp.color = HubTheme.TextLight;
            tmp.alignment = TextAlignmentOptions.Center;
            tmp.raycastTarget = false;
            return btn;
        }

        // ---------- Commit ----------

        private void CommitCart()
        {
            if (mode == VendorMode.Buy) CommitBuy();
            else                        CommitSell();
        }

        private void CommitBuy()
        {
            int total = ComputeCartTotal();
            if (total <= 0 || Inventory.Gold < total) return;

            foreach (var kvp in buyCart)
            {
                if (kvp.Value <= 0) continue;
                var def = ItemLibrary.Get(kvp.Key);
                if (def == null) continue;
                Inventory.Add(def, kvp.Value);
            }
            Inventory.Gold -= total;
            buyCart.Clear();
            PersistInventory();
            Refresh();
        }

        private void CommitSell()
        {
            int total = ComputeCartTotal();
            if (total <= 0) return;

            foreach (var kvp in sellCart)
            {
                if (kvp.Value <= 0) continue;
                Inventory.Remove(kvp.Key, kvp.Value);
            }
            Inventory.Gold += total;
            sellCart.Clear();
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
