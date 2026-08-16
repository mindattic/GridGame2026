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
using Scripts.Services;
using Scripts.Utilities;
using scene = Scripts.Helpers.SceneHelper;

namespace Scripts.Vendor.Summon
{
    /// <summary>
    /// SUMMONMANAGER - Runtime controller for the Summon Circle scene (US-132 / GG-A5).
    /// <para>PURPOSE: Roster growth as a vendor: lists the summonable hero classes
    /// (SummonService.Pool) with the rising recruit cost; a recruit deducts gold, appends the
    /// class to the save roster, and persists — the new hero then appears in Party's carousel.
    /// Deliberate purchase, never a pull (GG "not a gacha").</para>
    /// <para>UX: rows follow the Abilities-scene pattern (name + status/cost, click to act);
    /// footer shows live gold; recruited classes render dimmed "Recruited"; unaffordable rows
    /// disabled. Feedback via the flash label, same as Abilities.</para>
    /// <para>RELATED FILES: SummonBuilder.cs, SummonService.cs, PartyManager.cs,
    /// VendorNavBar.cs.</para>
    /// </summary>
    public class SummonManager : MonoBehaviour
    {
        public const string TitleLabelName = "Header/Title";
        public const string ListContentPath = "Body/SummonList/Viewport/Content";
        public const string GoldLabelName = "Body/GoldLabel";
        public const string FlashLabelName = "Body/FlashLabel";

        private RectTransform listContent;
        private TextMeshProUGUI goldLabel;
        private TextMeshProUGUI flashLabel;

        private void Awake()
        {
            BootstrapProfile();
            CacheUiReferences();
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

        private void CacheUiReferences()
        {
            var canvas = GameObject.Find("Canvas");
            if (canvas == null) { Debug.LogError("[SummonManager] Canvas not found."); return; }

            var contentT = canvas.transform.Find(ListContentPath);
            listContent = contentT != null ? contentT.GetComponent<RectTransform>() : null;
            var vlg = listContent?.GetComponent<VerticalLayoutGroup>();
            if (vlg != null) vlg.childControlWidth = false;
            var viewport = listContent?.parent as RectTransform;
            if (viewport != null)
            {
                var stencilMask = viewport.GetComponent<Mask>();
                if (stencilMask != null) stencilMask.enabled = false;
                if (viewport.GetComponent<RectMask2D>() == null)
                    viewport.gameObject.AddComponent<RectMask2D>();
            }

            goldLabel = FindLabel(canvas.transform, GoldLabelName);
            flashLabel = FindLabel(canvas.transform, FlashLabelName);
            if (flashLabel != null) flashLabel.text = "";
        }

        private void Refresh()
        {
            var save = ProfileHelper.CurrentProfile?.CurrentSave;
            if (save == null || listContent == null) return;

            for (int i = listContent.childCount - 1; i >= 0; i--)
                Object.Destroy(listContent.GetChild(i).gameObject);

            var inventory = new PlayerInventory();
            inventory.LoadFromSaveData(save.Inventory);
            int nextCost = SummonService.NextCost(save);

            if (goldLabel != null)
                goldLabel.text = $"Gold: {HubTheme.FormatGold(inventory.Gold)}    Next summon: {HubTheme.FormatGold(nextCost)}";

            foreach (var characterClass in SummonService.Pool)
            {
                bool owned = SummonService.IsOwned(save, characterClass);
                bool affordable = inventory.Gold >= nextCost;
                CreateRow(characterClass, owned, affordable, nextCost);
            }

            LayoutRebuilder.ForceRebuildLayoutImmediate(listContent);
        }

        private void CreateRow(CharacterClass characterClass, bool owned, bool affordable, int cost)
        {
            var go = new GameObject("Row_" + characterClass);
            go.layer = LayerMask.NameToLayer("UI");
            var rt = go.AddComponent<RectTransform>();
            rt.SetParent(listContent, false);
            rt.anchorMin = new Vector2(0f, 1f);
            rt.anchorMax = new Vector2(1f, 1f);
            rt.pivot = new Vector2(0.5f, 1f);
            rt.sizeDelta = new Vector2(0f, 64f);

            go.AddComponent<CanvasRenderer>();
            var bg = go.AddComponent<Image>();
            bg.color = owned ? HubTheme.RowLocked : HubTheme.RowBg;
            bg.raycastTarget = !owned;

            if (!owned)
            {
                var btn = go.AddComponent<Button>();
                btn.targetGraphic = bg;
                btn.interactable = affordable;
                var captured = characterClass;
                btn.onClick.AddListener(() => OnRecruitClicked(captured));
            }

            var le = go.AddComponent<LayoutElement>();
            le.minHeight = 64f; le.preferredHeight = 64f; le.flexibleWidth = 1f;

            var labelGO = new GameObject("Label");
            labelGO.layer = LayerMask.NameToLayer("UI");
            var labelRT = labelGO.AddComponent<RectTransform>();
            labelRT.SetParent(rt, false);
            labelRT.anchorMin = Vector2.zero; labelRT.anchorMax = Vector2.one;
            labelRT.offsetMin = new Vector2(16f, 4f); labelRT.offsetMax = new Vector2(-16f, -4f);
            labelGO.AddComponent<CanvasRenderer>();
            var tmp = labelGO.AddComponent<TextMeshProUGUI>();
            tmp.font = UiFonts.Body;
            string status = owned
                ? "<color=#888888>Recruited</color>"
                : HubTheme.ColorByAffordable(HubTheme.FormatGold(cost), affordable);
            tmp.text = $"{characterClass}    {status}";
            tmp.fontSize = 26;
            tmp.color = owned ? HubTheme.TextMuted : HubTheme.TextLight;
            tmp.alignment = TextAlignmentOptions.MidlineLeft;
            tmp.enableWordWrapping = false;
            tmp.richText = true;
            tmp.raycastTarget = false;
        }

        private void OnRecruitClicked(CharacterClass characterClass)
        {
            var save = ProfileHelper.CurrentProfile?.CurrentSave;
            if (save == null) return;

            var inventory = new PlayerInventory();
            inventory.LoadFromSaveData(save.Inventory);

            if (SummonService.TryRecruit(save, inventory, characterClass))
            {
                save.Inventory = inventory.ToSaveData();
                ProfileHelper.Save(overwrite: true);
                if (flashLabel != null)
                    flashLabel.text = $"<color=#66cc88>{characterClass} joins the roster!</color>";
            }
            else if (flashLabel != null)
            {
                flashLabel.text = "<color=#e57878>Cannot summon — not enough gold.</color>";
            }
            Refresh();
        }

        private static TextMeshProUGUI FindLabel(Transform root, string path)
        {
            var t = root.Find(path);
            return t != null ? t.GetComponent<TextMeshProUGUI>() : null;
        }
    }
}
