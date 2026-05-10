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

namespace Scripts.Vendor.Alchemy
{
    /// <summary>
    /// ALCHEMISTMANAGER - Runtime controller for the Alchemist scene.
    /// <para>PURPOSE: Lists every consumable recipe (anything whose ResultItemId is an
    /// ItemType.Consumable). Click a row to see ingredient cost vs. owned counts and a
    /// success forecast. Click "Mix" to roll: success consumes ingredients + gold and
    /// adds the result; failure consumes ingredients + gold but produces nothing — the
    /// risk that justifies the alchemist over just buying potions outright.</para>
    /// <para>FAILURE FORMULA: success = clamp(0.4 + 0.04 * partyMaxWisdom, 0.4, 0.95).
    /// Wisdom 0 → 40%, Wisdom 14 → ~96% capped at 95%. Reads max Wisdom across party
    /// members at Level 1 base stats — refine when ExperienceHelper-driven level lookup
    /// is wired (slice 4+).</para>
    /// <para>RELATED FILES: AlchemistScaffold.cs, RecipeLibrary.cs, CraftingRecipe.cs</para>
    /// </summary>
    public class AlchemistManager : MonoBehaviour
    {
        public const string GoldLabelName = "GoldLabel";
        public const string ItemListContentPath = "Body/ItemList/Viewport/Content";
        public const string DetailLabelName = "Body/DetailLabel";
        public const string MixButtonName = "Body/MixButton";
        public const string BackButtonName = "BackButton";
        public const string FlashLabelName = "Body/FlashLabel";

        public PlayerInventory Inventory { get; private set; }

        private CraftingRecipe selected;
        private TextMeshProUGUI goldLabel;
        private TextMeshProUGUI detailLabel;
        private TextMeshProUGUI flashLabel;
        private RectTransform listContent;
        private Button mixButton;

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

        // ---------- UI lookups & wiring ----------

        private void CacheUiReferences()
        {
            var canvas = GameObject.Find("Canvas");
            if (canvas == null) { Debug.LogError("[AlchemistManager] Canvas not found."); return; }

            goldLabel = FindLabel(canvas.transform, "Header/" + GoldLabelName);
            detailLabel = FindLabel(canvas.transform, DetailLabelName);
            flashLabel = FindLabel(canvas.transform, FlashLabelName);
            if (flashLabel != null) flashLabel.text = "";

            var contentT = canvas.transform.Find(ItemListContentPath);
            listContent = contentT != null ? contentT.GetComponent<RectTransform>() : null;
            if (listContent == null) Debug.LogError("[AlchemistManager] ItemList Content not found at " + ItemListContentPath);

            var mixT = canvas.transform.Find(MixButtonName);
            mixButton = mixT != null ? mixT.GetComponent<Button>() : null;
        }

        private void WireButtons()
        {
            if (mixButton != null)
            {
                mixButton.onClick.RemoveAllListeners();
                mixButton.onClick.AddListener(ConfirmMix);
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

        // ---------- Catalogue ----------

        private static IEnumerable<CraftingRecipe> PotionRecipes()
        {
            return RecipeLibrary.All().Where(r =>
            {
                var result = ItemLibrary.Get(r.ResultItemId);
                return result != null && result.Type == ItemType.Consumable;
            });
        }

        // ---------- Refresh ----------

        public void Refresh()
        {
            UpdateGoldLabel();
            RebuildList();
            UpdateDetail();
            UpdateMixButtonInteractable();
        }

        private void UpdateGoldLabel()
        {
            if (goldLabel != null) goldLabel.text = "Gold: " + HubTheme.FormatGold(Inventory.Gold);
        }

        private void RebuildList()
        {
            if (listContent == null) return;
            for (int i = listContent.childCount - 1; i >= 0; i--)
                Object.Destroy(listContent.GetChild(i).gameObject);

            foreach (var recipe in PotionRecipes())
                CreateRow(recipe);
        }

        private void UpdateDetail()
        {
            if (detailLabel == null) return;
            if (selected == null)
            {
                detailLabel.text = "<b>Alchemist</b>\nMix consumables from raw materials.\nClick a recipe to see the requirements.";
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
                string countText = $"{owned}/{ing.Count}";
                sb.Append("  • ")
                  .Append(name)
                  .Append("  ")
                  .Append(HubTheme.ColorByAffordable(countText, enough))
                  .Append('\n');
            }
            sb.Append('\n');
            float pct = SuccessChance() * 100f;
            sb.Append("Success chance: ").Append(pct.ToString("0")).Append('%');

            detailLabel.text = sb.ToString();
        }

        private void UpdateMixButtonInteractable()
        {
            if (mixButton == null) return;
            mixButton.interactable = selected != null && selected.CanCraft(Inventory);
        }

        // ---------- Row factory (inline, mirrors StoreManager.CreateRow) ----------

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

        // ---------- Mix ----------

        private void ConfirmMix()
        {
            if (selected == null) return;
            if (!selected.CanCraft(Inventory)) return;

            float chance = SuccessChance();
            bool success = Random.value <= chance;

            if (success)
            {
                selected.Execute(Inventory);
                if (flashLabel != null)
                {
                    var result = ItemLibrary.Get(selected.ResultItemId);
                    string name = result != null ? result.DisplayName : selected.ResultItemId;
                    flashLabel.text = $"<color=#66cc88>Brewed {name} ×{selected.ResultCount}!</color>";
                }
            }
            else
            {
                // Consume ingredients + gold but produce nothing — the failure tax.
                foreach (var ing in selected.Ingredients) Inventory.Remove(ing.ItemId, ing.Count);
                Inventory.Gold = Mathf.Max(0, Inventory.Gold - selected.GoldCost);
                if (flashLabel != null)
                    flashLabel.text = "<color=#e57878>Mixture failed! Ingredients lost.</color>";
            }

            PersistInventory();
            Refresh();
        }

        // ---------- Success chance ----------

        private float SuccessChance()
        {
            float wisdom = PartyMaxWisdom();
            float raw = 0.4f + 0.04f * wisdom;
            return Mathf.Clamp(raw, 0.4f, 0.95f);
        }

        private static float PartyMaxWisdom()
        {
            var party = ProfileHelper.CurrentProfile?.CurrentSave?.Party?.Members;
            if (party == null || party.Count == 0) return 0f;
            float max = 0f;
            foreach (var member in party)
            {
                var data = ActorLibrary.Get(member.CharacterClass);
                if (data == null) continue;
                var stats = data.GetStats(1); // Level 1 baseline; refine when level lookup is wired in slice 4+.
                if (stats.Wisdom > max) max = stats.Wisdom;
            }
            return max;
        }

        // ---------- Helpers ----------

        private static TextMeshProUGUI FindLabel(Transform root, string path)
        {
            var t = root.Find(path);
            return t != null ? t.GetComponent<TextMeshProUGUI>() : null;
        }
    }
}
