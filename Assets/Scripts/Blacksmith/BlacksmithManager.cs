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
    /// <para>PURPOSE: Lists every Equipment recipe (RecipeLibrary entries whose ResultItemId
    /// resolves to <see cref="ItemType.Equipment"/>). Click "Forge" to consume ingredients
    /// + gold and add the result to inventory. Forging never fails (deterministic) — that
    /// is the contrast with the Alchemist's Wisdom-driven mix roll.</para>
    /// <para>RELATED FILES: BlacksmithScaffold.cs, RecipeLibrary.cs, AlchemistManager.cs (parallel)</para>
    /// </summary>
    public class BlacksmithManager : MonoBehaviour
    {
        public const string GoldLabelName = "GoldLabel";
        public const string ItemListContentPath = "Body/ItemList/Viewport/Content";
        public const string DetailLabelName = "Body/DetailLabel";
        public const string ForgeButtonName = "Body/ForgeButton";
        public const string FlashLabelName = "Body/FlashLabel";
        public const string BackButtonName = "BackButton";

        public PlayerInventory Inventory { get; private set; }

        private CraftingRecipe selected;
        private TextMeshProUGUI goldLabel;
        private TextMeshProUGUI detailLabel;
        private TextMeshProUGUI flashLabel;
        private RectTransform listContent;
        private Button forgeButton;

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

            var forgeT = canvas.transform.Find(ForgeButtonName);
            forgeButton = forgeT != null ? forgeT.GetComponent<Button>() : null;
        }

        private void WireButtons()
        {
            if (forgeButton != null)
            {
                forgeButton.onClick.RemoveAllListeners();
                forgeButton.onClick.AddListener(ConfirmForge);
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

        private static IEnumerable<CraftingRecipe> EquipmentRecipes()
        {
            return RecipeLibrary.All().Where(r =>
            {
                var result = ItemLibrary.Get(r.ResultItemId);
                return result != null && result.Type == ItemType.Equipment;
            });
        }

        public void Refresh()
        {
            if (goldLabel != null) goldLabel.text = "Gold: " + HubTheme.FormatGold(Inventory.Gold);
            RebuildList();
            UpdateDetail();
            if (forgeButton != null) forgeButton.interactable = selected != null && selected.CanCraft(Inventory);
        }

        private void RebuildList()
        {
            if (listContent == null) return;
            for (int i = listContent.childCount - 1; i >= 0; i--)
                Object.Destroy(listContent.GetChild(i).gameObject);

            foreach (var recipe in EquipmentRecipes())
                CreateRow(recipe);
        }

        private void UpdateDetail()
        {
            if (detailLabel == null) return;
            if (selected == null)
            {
                detailLabel.text = "<b>Blacksmith</b>\nForge weapons + armor from raw materials.\nClick a recipe to see the requirements.";
                return;
            }

            var result = ItemLibrary.Get(selected.ResultItemId);
            string resultLine = result != null ? $"{result.DisplayName} ×{selected.ResultCount}" : selected.ResultItemId;

            var sb = new System.Text.StringBuilder();
            sb.Append("<b>").Append(selected.DisplayName).Append("</b>\n");
            sb.Append("Result: ").Append(resultLine).Append('\n');
            if (result != null && !string.IsNullOrEmpty(result.Description))
                sb.Append("<i>").Append(result.Description).Append("</i>\n");
            sb.Append('\n');
            sb.Append("Cost: ").Append(HubTheme.FormatGold(selected.GoldCost)).Append('\n');
            sb.Append("Ingredients:\n");
            foreach (var ing in selected.Ingredients)
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
        }

        private void CreateRow(CraftingRecipe recipe)
        {
            var go = new GameObject("Row_" + recipe.Id);
            go.layer = LayerMask.NameToLayer("UI");
            var rt = go.AddComponent<RectTransform>();
            rt.SetParent(listContent, false);
            rt.sizeDelta = new Vector2(0f, 56f);

            go.AddComponent<CanvasRenderer>();
            var bg = go.AddComponent<Image>();
            bg.color = (selected != null && selected.Id == recipe.Id)
                ? new Color(0.36f, 0.50f, 0.78f, 1f)
                : new Color(0.20f, 0.24f, 0.34f, 1f);
            bg.raycastTarget = true;

            var btn = go.AddComponent<Button>();
            btn.targetGraphic = bg;
            var captured = recipe;
            btn.onClick.AddListener(() => { selected = captured; if (flashLabel != null) flashLabel.text = ""; Refresh(); });

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
            bool can = recipe.CanCraft(Inventory);
            string costPart = HubTheme.ColorByAffordable(HubTheme.FormatGold(recipe.GoldCost), can);
            var result = ItemLibrary.Get(recipe.ResultItemId);
            tmp.text = $"{recipe.DisplayName}    {costPart}";
            tmp.fontSize = 24;
            tmp.color = result != null ? HubItemRowFactory.RarityColor(result.Rarity) : Color.white;
            tmp.alignment = TextAlignmentOptions.MidlineLeft;
            tmp.enableWordWrapping = false;
            tmp.richText = true;
            tmp.raycastTarget = false;
        }

        private void ConfirmForge()
        {
            if (selected == null) return;
            if (!selected.CanCraft(Inventory)) return;
            selected.Execute(Inventory);
            if (flashLabel != null)
            {
                var result = ItemLibrary.Get(selected.ResultItemId);
                string name = result != null ? result.DisplayName : selected.ResultItemId;
                flashLabel.text = $"<color=#66cc88>Forged {name} ×{selected.ResultCount}!</color>";
            }
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
