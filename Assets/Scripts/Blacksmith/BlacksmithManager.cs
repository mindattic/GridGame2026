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
using Scripts.Models;
using Scripts.Models.Actor;
using Scripts.Overworld;
using Scripts.Sequences;
using Scripts.Serialization;
using Scripts.Utilities;
using scene = Scripts.Helpers.SceneHelper;

namespace Scripts.Vendor.Blacksmith
{
    /// <summary>
    /// BLACKSMITHMANAGER - Runtime controller for the Blacksmith scene.
    /// <para>PURPOSE: Two modes selected via tab buttons.</para>
    /// <para>FORGE: Lists every Equipment recipe (RecipeLibrary entries whose ResultItemId
    /// resolves to <see cref="ItemType.Equipment"/>). Click "Forge" to consume ingredients
    /// + gold and add the result to inventory. Forging is deterministic — never fails.</para>
    /// <para>SALVAGE (slice 9): Lists every Equipment in the player's inventory. Click a
    /// row to break that piece down into <see cref="SalvageRefundFraction"/> of the original
    /// recipe's ingredients (floor, min 1 of each). Item slots that have no recipe in the
    /// library cannot be salvaged.</para>
    /// <para>RELATED FILES: BlacksmithScaffold.cs, RecipeLibrary.cs, AlchemistManager.cs (parallel)</para>
    /// </summary>
    public class BlacksmithManager : MonoBehaviour
    {
        public enum Mode { Forge, Salvage }

        public const string GoldLabelName = "GoldLabel";
        public const string ItemListContentPath = "Body/ItemList/Viewport/Content";
        public const string DetailLabelName = "Body/DetailLabel";
        public const string ActionButtonName = "Body/ForgeButton";
        public const string ActionButtonLabelPath = "Body/ForgeButton/Label";
        public const string FlashLabelName = "Body/FlashLabel";
        public const string ForgeTabName = "Body/ForgeTab";
        public const string SalvageTabName = "Body/SalvageTab";
        public const string BackButtonName = "BackButton";

        // Salvage refunds floor(ing.Count * 0.5), min 1 per ingredient.
        private const float SalvageRefundFraction = 0.5f;

        public PlayerInventory Inventory { get; private set; }

        private Mode mode = Mode.Forge;
        private CraftingRecipe selectedRecipe;
        private PlayerInventory.Entry selectedSalvage;

        private TextMeshProUGUI goldLabel;
        private TextMeshProUGUI detailLabel;
        private TextMeshProUGUI flashLabel;
        private RectTransform listContent;
        private Button actionButton;
        private TextMeshProUGUI actionButtonLabel;
        private Button forgeTab;
        private Button salvageTab;

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
            if (canvas == null) { Debug.LogError("[BlacksmithManager] Canvas not found."); return; }

            goldLabel = FindLabel(canvas.transform, "Header/" + GoldLabelName);
            detailLabel = FindLabel(canvas.transform, DetailLabelName);
            flashLabel = FindLabel(canvas.transform, FlashLabelName);
            if (flashLabel != null) flashLabel.text = "";

            var contentT = canvas.transform.Find(ItemListContentPath);
            listContent = contentT != null ? contentT.GetComponent<RectTransform>() : null;

            var actT = canvas.transform.Find(ActionButtonName);
            actionButton = actT != null ? actT.GetComponent<Button>() : null;
            actionButtonLabel = FindLabel(canvas.transform, ActionButtonLabelPath);

            var forgeT = canvas.transform.Find(ForgeTabName);
            forgeTab = forgeT != null ? forgeT.GetComponent<Button>() : null;
            var salvageT = canvas.transform.Find(SalvageTabName);
            salvageTab = salvageT != null ? salvageT.GetComponent<Button>() : null;
        }

        private void WireButtons()
        {
            if (actionButton != null)
            {
                actionButton.onClick.RemoveAllListeners();
                actionButton.onClick.AddListener(ConfirmAction);
            }
            if (forgeTab != null)
            {
                forgeTab.onClick.RemoveAllListeners();
                forgeTab.onClick.AddListener(() => SetMode(Mode.Forge));
            }
            if (salvageTab != null)
            {
                salvageTab.onClick.RemoveAllListeners();
                salvageTab.onClick.AddListener(() => SetMode(Mode.Salvage));
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

        private void SetMode(Mode newMode)
        {
            if (mode == newMode) return;
            mode = newMode;
            selectedRecipe = null;
            selectedSalvage = null;
            if (flashLabel != null) flashLabel.text = "";
            Refresh();
        }

        public void Refresh()
        {
            if (goldLabel != null) goldLabel.text = "Gold: " + HubTheme.FormatGold(Inventory.Gold);
            UpdateTabTints();
            RebuildList();
            UpdateDetail();
            UpdateActionButton();
        }

        private void UpdateTabTints()
        {
            var active = HubTheme.NavActive;
            var idle = HubTheme.NavIdle;
            if (forgeTab != null)
            {
                var img = forgeTab.GetComponent<Image>();
                if (img != null) img.color = mode == Mode.Forge ? active : idle;
            }
            if (salvageTab != null)
            {
                var img = salvageTab.GetComponent<Image>();
                if (img != null) img.color = mode == Mode.Salvage ? active : idle;
            }
        }

        private static IEnumerable<CraftingRecipe> EquipmentRecipes()
        {
            return RecipeLibrary.All().Where(r =>
            {
                var result = ItemLibrary.Get(r.ResultItemId);
                return result != null && result.Type == ItemType.Equipment;
            });
        }

        private void RebuildList()
        {
            if (listContent == null) return;
            for (int i = listContent.childCount - 1; i >= 0; i--)
                Object.Destroy(listContent.GetChild(i).gameObject);

            if (mode == Mode.Forge)
            {
                foreach (var recipe in EquipmentRecipes())
                    CreateForgeRow(recipe);
            }
            else
            {
                foreach (var entry in Inventory.ByType(ItemType.Equipment))
                    CreateSalvageRow(entry);
            }
        }

        private void UpdateDetail()
        {
            if (detailLabel == null) return;

            if (mode == Mode.Forge)
            {
                if (selectedRecipe == null)
                {
                    detailLabel.text = "<b>Forge</b>\nForge weapons + armor from raw materials.\nClick a recipe to see the requirements.";
                    return;
                }

                var result = ItemLibrary.Get(selectedRecipe.ResultItemId);
                string resultLine = result != null ? $"{result.DisplayName} ×{selectedRecipe.ResultCount}" : selectedRecipe.ResultItemId;

                var sb = new System.Text.StringBuilder();
                sb.Append("<b>").Append(selectedRecipe.DisplayName).Append("</b>\n");
                sb.Append("Result: ").Append(resultLine).Append('\n');
                if (result != null && !string.IsNullOrEmpty(result.Description))
                    sb.Append("<i>").Append(result.Description).Append("</i>\n");
                sb.Append('\n');
                sb.Append("Cost: ").Append(HubTheme.FormatGold(selectedRecipe.GoldCost)).Append('\n');
                sb.Append("Ingredients:\n");
                foreach (var ing in selectedRecipe.Ingredients)
                {
                    var ingDef = ItemLibrary.Get(ing.ItemId);
                    int owned = Inventory.CountOf(ing.ItemId);
                    bool enough = owned >= ing.Count;
                    string name = ingDef != null ? ingDef.DisplayName : ing.ItemId;
                    sb.Append("  • ").Append(name).Append("  ")
                      .Append(HubTheme.ColorByAffordable($"{owned}/{ing.Count}", enough))
                      .Append('\n');
                }
                detailLabel.text = sb.ToString();
                return;
            }

            // Salvage mode
            if (selectedSalvage == null)
            {
                detailLabel.text = "<b>Salvage</b>\nBreak equipment down into raw materials. You recover " +
                                   $"{Mathf.RoundToInt(SalvageRefundFraction * 100)}% of the recipe's ingredients (floor, min 1).\n\nClick a piece in your inventory to preview.";
                return;
            }

            var item = selectedSalvage.Definition;
            var recipe = FindRecipeFor(item.Id);
            var sb2 = new System.Text.StringBuilder();
            sb2.Append("<b>").Append(item.DisplayName).Append("</b>");
            if (selectedSalvage.Count > 1) sb2.Append("  ×").Append(selectedSalvage.Count);
            sb2.Append('\n');
            if (!string.IsNullOrEmpty(item.Description))
                sb2.Append("<i>").Append(item.Description).Append("</i>\n");
            sb2.Append('\n');
            if (recipe == null)
            {
                sb2.Append("<color=#cc6666>Unsalvageable.</color>\nNo crafting recipe registered for this item, so the smith can't break it down cleanly.");
            }
            else
            {
                sb2.Append("Salvage yields:\n");
                foreach (var ing in recipe.Ingredients)
                {
                    int refund = Mathf.Max(1, Mathf.FloorToInt(ing.Count * SalvageRefundFraction));
                    var ingDef = ItemLibrary.Get(ing.ItemId);
                    string name = ingDef != null ? ingDef.DisplayName : ing.ItemId;
                    sb2.Append("  • ").Append(name).Append(" ×").Append(refund)
                       .Append("  <color=#888888>(was ").Append(ing.Count).Append(")</color>\n");
                }
            }
            detailLabel.text = sb2.ToString();
        }

        private void UpdateActionButton()
        {
            if (actionButton == null) return;
            if (mode == Mode.Forge)
            {
                if (actionButtonLabel != null) actionButtonLabel.text = "Forge";
                actionButton.interactable = selectedRecipe != null && selectedRecipe.CanCraft(Inventory);
            }
            else
            {
                if (actionButtonLabel != null) actionButtonLabel.text = "Salvage";
                actionButton.interactable = selectedSalvage != null && FindRecipeFor(selectedSalvage.Definition.Id) != null;
            }
        }

        private void CreateForgeRow(CraftingRecipe recipe)
        {
            var go = MakeRowGO("Row_" + recipe.Id);
            var bg = go.GetComponent<Image>();
            bg.color = (selectedRecipe != null && selectedRecipe.Id == recipe.Id)
                ? new Color(0.36f, 0.50f, 0.78f, 1f)
                : new Color(0.20f, 0.24f, 0.34f, 1f);

            var btn = go.GetComponent<Button>();
            var captured = recipe;
            btn.onClick.AddListener(() => { selectedRecipe = captured; if (flashLabel != null) flashLabel.text = ""; Refresh(); });

            var tmp = go.GetComponentInChildren<TextMeshProUGUI>();
            bool can = recipe.CanCraft(Inventory);
            string costPart = HubTheme.ColorByAffordable(HubTheme.FormatGold(recipe.GoldCost), can);
            var result = ItemLibrary.Get(recipe.ResultItemId);
            tmp.text = $"{recipe.DisplayName}    {costPart}";
            tmp.color = result != null ? HubItemRowFactory.RarityColor(result.Rarity) : Color.white;
        }

        private void CreateSalvageRow(PlayerInventory.Entry entry)
        {
            var go = MakeRowGO("Salvage_" + entry.Definition.Id);
            var bg = go.GetComponent<Image>();
            bg.color = (selectedSalvage != null && selectedSalvage.Definition.Id == entry.Definition.Id)
                ? new Color(0.36f, 0.50f, 0.78f, 1f)
                : new Color(0.20f, 0.24f, 0.34f, 1f);

            var btn = go.GetComponent<Button>();
            var captured = entry;
            btn.onClick.AddListener(() => { selectedSalvage = captured; if (flashLabel != null) flashLabel.text = ""; Refresh(); });

            var tmp = go.GetComponentInChildren<TextMeshProUGUI>();
            bool salvageable = FindRecipeFor(entry.Definition.Id) != null;
            string countPart = entry.Count > 1 ? $" ×{entry.Count}" : "";
            string suffix = salvageable ? "" : "  <color=#888888>(unsalvageable)</color>";
            tmp.text = $"{entry.Definition.DisplayName}{countPart}{suffix}";
            tmp.color = HubItemRowFactory.RarityColor(entry.Definition.Rarity);
        }

        private GameObject MakeRowGO(string name)
        {
            var go = new GameObject(name);
            go.layer = LayerMask.NameToLayer("UI");
            var rt = go.AddComponent<RectTransform>();
            rt.SetParent(listContent, false);
            rt.sizeDelta = new Vector2(0f, 56f);
            go.AddComponent<CanvasRenderer>();
            var bg = go.AddComponent<Image>();
            bg.raycastTarget = true;
            var btn = go.AddComponent<Button>();
            btn.targetGraphic = bg;
            var le = go.AddComponent<LayoutElement>();
            le.minHeight = 56f; le.preferredHeight = 56f; le.flexibleWidth = 1f;

            var labelGO = new GameObject("Label");
            labelGO.layer = LayerMask.NameToLayer("UI");
            var labelRT = labelGO.AddComponent<RectTransform>();
            labelRT.SetParent(rt, false);
            labelRT.anchorMin = Vector2.zero; labelRT.anchorMax = Vector2.one;
            labelRT.offsetMin = new Vector2(16f, 4f); labelRT.offsetMax = new Vector2(-16f, -4f);
            labelGO.AddComponent<CanvasRenderer>();
            var tmp = labelGO.AddComponent<TextMeshProUGUI>();
            tmp.fontSize = 24;
            tmp.alignment = TextAlignmentOptions.MidlineLeft;
            tmp.enableWordWrapping = false;
            tmp.richText = true;
            tmp.raycastTarget = false;
            return go;
        }

        private void ConfirmAction()
        {
            if (mode == Mode.Forge) ConfirmForge();
            else ConfirmSalvage();
        }

        private void ConfirmForge()
        {
            if (selectedRecipe == null) return;
            if (!selectedRecipe.CanCraft(Inventory)) return;
            selectedRecipe.Execute(Inventory);
            if (flashLabel != null)
            {
                var result = ItemLibrary.Get(selectedRecipe.ResultItemId);
                string name = result != null ? result.DisplayName : selectedRecipe.ResultItemId;
                flashLabel.text = $"<color=#66cc88>Forged {name} ×{selectedRecipe.ResultCount}!</color>";
            }
            PersistInventory();
            Refresh();
        }

        private void ConfirmSalvage()
        {
            if (selectedSalvage == null) return;
            var item = selectedSalvage.Definition;
            if (Inventory.CountOf(item.Id) < 1) return;

            var recipe = FindRecipeFor(item.Id);
            if (recipe == null) return;

            Inventory.Remove(item.Id, 1);

            var refunded = new System.Text.StringBuilder();
            bool first = true;
            foreach (var ing in recipe.Ingredients)
            {
                int refund = Mathf.Max(1, Mathf.FloorToInt(ing.Count * SalvageRefundFraction));
                var ingDef = ItemLibrary.Get(ing.ItemId);
                if (ingDef != null) Inventory.Add(ingDef, refund);
                if (!first) refunded.Append(", ");
                refunded.Append(ingDef != null ? ingDef.DisplayName : ing.ItemId).Append(" ×").Append(refund);
                first = false;
            }

            if (flashLabel != null)
                flashLabel.text = $"<color=#66cc88>Salvaged {item.DisplayName} → {refunded}</color>";

            // If this was the last copy, clear the selection so the row goes away on rebuild.
            if (Inventory.CountOf(item.Id) == 0) selectedSalvage = null;

            PersistInventory();
            Refresh();
        }

        private static CraftingRecipe FindRecipeFor(string resultItemId)
            => RecipeLibrary.All().FirstOrDefault(r => r.ResultItemId == resultItemId);

        private static TextMeshProUGUI FindLabel(Transform root, string path)
        {
            var t = root.Find(path);
            return t != null ? t.GetComponent<TextMeshProUGUI>() : null;
        }
    }
}
