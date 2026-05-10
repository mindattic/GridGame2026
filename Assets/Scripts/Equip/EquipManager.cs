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

namespace Scripts.Vendor.Equip
{
    /// <summary>
    /// EQUIPMANAGER - Runtime controller for the Equip scene.
    /// <para>PURPOSE: Equips items into the 5 equipment slots (Weapon / Armor / 3× Relic)
    /// for the hero handed off via <see cref="HeroHandoff.Pending"/>. Equipping moves the
    /// item out of the shared inventory; unequipping returns it. Persists to
    /// <see cref="HeroEquipmentSave"/> on every change.</para>
    /// <para>UX (mirrors Abilities slice):
    /// <list type="bullet">
    /// <item>Click an inventory item → equips into its natural slot. Relics fill the first
    /// empty Relic1/2/3.</item>
    /// <item>Click a filled slot → unequips, returning the item to inventory.</item>
    /// <item>If the natural slot is already filled when equipping, the previous item is
    /// returned to inventory (swap).</item>
    /// </list>
    /// Class-vs-weapon proficiency is NOT enforced in this slice — combat (slice 7) will
    /// surface that warning. The slice-5 goal is just "the user can equip a sword and see
    /// it persist."</para>
    /// <para>RELATED FILES: EquipScaffold.cs, HeroHandoff.cs, HeroEquipmentSave (Profile.cs)</para>
    /// </summary>
    public class EquipManager : MonoBehaviour
    {
        public const string TitleLabelName = "Header/Title";
        public const string SlotsContainerName = "Body/SlotsRow";
        public const string InventoryContentPath = "Body/InventoryList/Viewport/Content";
        public const string FlashLabelName = "Body/FlashLabel";
        public const string BackButtonName = "BackButton";

        private static readonly EquipmentSlot[] AllSlots =
        {
            EquipmentSlot.Weapon,
            EquipmentSlot.Armor,
            EquipmentSlot.Relic1,
            EquipmentSlot.Relic2,
            EquipmentSlot.Relic3,
        };

        private CharacterClass hero = CharacterClass.None;
        private TextMeshProUGUI titleLabel;
        private TextMeshProUGUI flashLabel;
        private RectTransform slotsContainer;
        private RectTransform inventoryContent;
        private PlayerInventory inventory;

        private void Awake()
        {
            BootstrapProfile();
            ResolveHero();
            HydrateInventory();
            CacheUiReferences();
            WireBackButton();
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

        private void ResolveHero()
        {
            if (HeroHandoff.Pending != CharacterClass.None)
            {
                hero = HeroHandoff.Pending;
                HeroHandoff.Pending = CharacterClass.None;
                return;
            }
            var party = ProfileHelper.CurrentProfile?.CurrentSave?.Party?.Members;
            if (party != null && party.Count > 0) hero = party[0].CharacterClass;
        }

        private void HydrateInventory()
        {
            inventory = new PlayerInventory();
            var save = ProfileHelper.CurrentProfile?.CurrentSave;
            if (save?.Inventory != null) inventory.LoadFromSaveData(save.Inventory);
        }

        private void Persist()
        {
            var save = ProfileHelper.CurrentProfile?.CurrentSave;
            if (save == null) return;
            save.Inventory = inventory.ToSaveData();
            ProfileHelper.Save(overwrite: true);
        }

        // ---------- UI lookups ----------

        private void CacheUiReferences()
        {
            var canvas = GameObject.Find("Canvas");
            if (canvas == null) { Debug.LogError("[EquipManager] Canvas not found."); return; }

            titleLabel = FindLabel(canvas.transform, TitleLabelName);
            flashLabel = FindLabel(canvas.transform, FlashLabelName);
            if (flashLabel != null) flashLabel.text = "";

            var slotsT = canvas.transform.Find(SlotsContainerName);
            slotsContainer = slotsT != null ? slotsT.GetComponent<RectTransform>() : null;

            var contentT = canvas.transform.Find(InventoryContentPath);
            inventoryContent = contentT != null ? contentT.GetComponent<RectTransform>() : null;
        }

        private void WireBackButton()
        {
            var canvas = GameObject.Find("Canvas");
            var backT = canvas != null ? canvas.transform.Find(BackButtonName) : null;
            var backBtn = backT != null ? backT.GetComponent<Button>() : null;
            if (backBtn != null)
            {
                backBtn.onClick.RemoveAllListeners();
                backBtn.onClick.AddListener(() => scene.Fade.ToParty());
            }
        }

        // ---------- Save accessors ----------

        private HeroEquipmentSave Equipment()
        {
            var save = ProfileHelper.CurrentProfile?.CurrentSave;
            if (save == null) return null;
            if (save.Equipment == null) save.Equipment = new EquipmentSaveData();
            return save.Equipment.GetOrCreate(hero);
        }

        private static string LabelForSlot(EquipmentSlot slot)
        {
            return slot switch
            {
                EquipmentSlot.Weapon => "Weapon",
                EquipmentSlot.Armor => "Armor",
                EquipmentSlot.Relic1 => "Relic 1",
                EquipmentSlot.Relic2 => "Relic 2",
                EquipmentSlot.Relic3 => "Relic 3",
                _ => slot.ToString(),
            };
        }

        // ---------- Refresh ----------

        public void Refresh()
        {
            UpdateTitle();
            RebuildSlotsRow();
            RebuildInventory();
        }

        private void UpdateTitle()
        {
            if (titleLabel == null) return;
            titleLabel.text = hero != CharacterClass.None ? $"Equip — {hero}" : "Equip";
        }

        private void RebuildSlotsRow()
        {
            if (slotsContainer == null) return;
            for (int i = slotsContainer.childCount - 1; i >= 0; i--)
                Object.Destroy(slotsContainer.GetChild(i).gameObject);

            if (hero == CharacterClass.None) return;

            var eq = Equipment();
            foreach (var slot in AllSlots) CreateSlotButton(slot, eq?.GetSlot(slot));
        }

        private void RebuildInventory()
        {
            if (inventoryContent == null) return;
            for (int i = inventoryContent.childCount - 1; i >= 0; i--)
                Object.Destroy(inventoryContent.GetChild(i).gameObject);

            if (inventory == null) return;
            // Equipment items only — sort by slot, then rarity desc, then name asc for predictable order.
            var rows = inventory.All()
                .Where(e => e.Definition != null
                    && e.Definition.Type == ItemType.Equipment
                    && e.Definition.Slot != EquipmentSlot.None
                    && e.Count > 0)
                .OrderBy(e => (int)e.Definition.Slot)
                .ThenByDescending(e => (int)e.Definition.Rarity)
                .ThenBy(e => e.Definition.DisplayName);

            foreach (var entry in rows) CreateInventoryRow(entry.Definition, entry.Count);
        }

        // ---------- UI factories ----------

        private void CreateSlotButton(EquipmentSlot slot, string itemId)
        {
            var go = new GameObject("Slot_" + slot);
            go.layer = LayerMask.NameToLayer("UI");
            var rt = go.AddComponent<RectTransform>();
            rt.SetParent(slotsContainer, false);

            go.AddComponent<CanvasRenderer>();
            bool filled = !string.IsNullOrEmpty(itemId);
            var bg = go.AddComponent<Image>();
            bg.color = filled
                ? new Color(0.36f, 0.50f, 0.78f, 1f)
                : new Color(0.20f, 0.24f, 0.34f, 1f);
            bg.raycastTarget = true;

            var btn = go.AddComponent<Button>();
            btn.targetGraphic = bg;
            var capturedSlot = slot;
            btn.onClick.AddListener(() => OnSlotClicked(capturedSlot));

            var le = go.AddComponent<LayoutElement>();
            le.minWidth = 120f; le.preferredWidth = 140f; le.flexibleWidth = 1f;
            le.minHeight = 100f; le.preferredHeight = 100f; le.flexibleHeight = 0f;

            // Slot label (top-left)
            var badgeGO = new GameObject("SlotName");
            badgeGO.layer = LayerMask.NameToLayer("UI");
            var badgeRT = badgeGO.AddComponent<RectTransform>();
            badgeRT.SetParent(rt, false);
            badgeRT.anchorMin = new Vector2(0f, 1f); badgeRT.anchorMax = new Vector2(1f, 1f);
            badgeRT.pivot = new Vector2(0.5f, 1f);
            badgeRT.sizeDelta = new Vector2(0f, 28f);
            badgeRT.anchoredPosition = new Vector2(0f, -4f);
            badgeGO.AddComponent<CanvasRenderer>();
            var badgeTmp = badgeGO.AddComponent<TextMeshProUGUI>();
            badgeTmp.text = LabelForSlot(slot);
            badgeTmp.fontSize = 18;
            badgeTmp.color = HubTheme.Accent;
            badgeTmp.alignment = TextAlignmentOptions.Top;
            badgeTmp.fontStyle = FontStyles.Bold;
            badgeTmp.raycastTarget = false;

            // Item name (centered)
            var labelGO = new GameObject("ItemName");
            labelGO.layer = LayerMask.NameToLayer("UI");
            var labelRT = labelGO.AddComponent<RectTransform>();
            labelRT.SetParent(rt, false);
            labelRT.anchorMin = Vector2.zero; labelRT.anchorMax = Vector2.one;
            labelRT.offsetMin = new Vector2(8f, 8f); labelRT.offsetMax = new Vector2(-8f, -32f);
            labelGO.AddComponent<CanvasRenderer>();
            var tmp = labelGO.AddComponent<TextMeshProUGUI>();
            string content;
            if (!filled) content = "<color=#888888>Empty</color>";
            else
            {
                var def = ItemLibrary.Get(itemId);
                content = def != null ? def.DisplayName : itemId;
            }
            tmp.text = content;
            tmp.fontSize = 18;
            tmp.color = HubTheme.TextLight;
            tmp.alignment = TextAlignmentOptions.Center;
            tmp.enableWordWrapping = true;
            tmp.richText = true;
            tmp.raycastTarget = false;
        }

        private void CreateInventoryRow(ItemDefinition def, int owned)
        {
            var go = new GameObject("Row_" + def.Id);
            go.layer = LayerMask.NameToLayer("UI");
            var rt = go.AddComponent<RectTransform>();
            rt.SetParent(inventoryContent, false);
            rt.sizeDelta = new Vector2(0f, 56f);

            go.AddComponent<CanvasRenderer>();
            var bg = go.AddComponent<Image>();
            bg.color = new Color(0.20f, 0.24f, 0.34f, 1f);
            bg.raycastTarget = true;

            var btn = go.AddComponent<Button>();
            btn.targetGraphic = bg;
            var captured = def;
            btn.onClick.AddListener(() => OnInventoryClicked(captured));

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
            string slotLabel = LabelForSlot(def.Slot);
            tmp.text = $"{def.DisplayName}    <color=#cccccc>{slotLabel} ×{owned}</color>";
            tmp.fontSize = 22;
            tmp.color = HubItemRowFactory.RarityColor(def.Rarity);
            tmp.alignment = TextAlignmentOptions.MidlineLeft;
            tmp.enableWordWrapping = false;
            tmp.richText = true;
            tmp.raycastTarget = false;
        }

        // ---------- Click handlers ----------

        private void OnSlotClicked(EquipmentSlot slot)
        {
            if (hero == CharacterClass.None) return;
            var eq = Equipment();
            if (eq == null) return;
            string itemId = eq.GetSlot(slot);
            if (string.IsNullOrEmpty(itemId))
            {
                if (flashLabel != null)
                    flashLabel.text = $"<color=#cccccc>{LabelForSlot(slot)} is empty.</color>";
                return;
            }

            // Return item to inventory and clear the slot.
            var def = ItemLibrary.Get(itemId);
            if (def != null) inventory.Add(def, 1);
            eq.SetSlot(slot, null);
            Persist();
            if (flashLabel != null)
                flashLabel.text = $"<color=#cccccc>Unequipped {(def != null ? def.DisplayName : itemId)} from {LabelForSlot(slot)}.</color>";
            Refresh();
        }

        private void OnInventoryClicked(ItemDefinition def)
        {
            if (hero == CharacterClass.None || def == null) return;
            if (def.Type != ItemType.Equipment || def.Slot == EquipmentSlot.None)
            {
                if (flashLabel != null) flashLabel.text = "<color=#e57878>Not equippable.</color>";
                return;
            }

            var eq = Equipment();
            if (eq == null) return;

            EquipmentSlot target = ResolveTargetSlot(def, eq);
            // Swap-out: if the target slot already has something, return it to inventory first.
            string previous = eq.GetSlot(target);
            if (!string.IsNullOrEmpty(previous))
            {
                var prevDef = ItemLibrary.Get(previous);
                if (prevDef != null) inventory.Add(prevDef, 1);
            }

            // Move item from inventory into slot.
            if (!inventory.Remove(def.Id, 1))
            {
                if (flashLabel != null) flashLabel.text = "<color=#e57878>Inventory empty.</color>";
                return;
            }
            eq.SetSlot(target, def.Id);
            Persist();
            if (flashLabel != null)
                flashLabel.text = $"<color=#66cc88>Equipped {def.DisplayName} to {LabelForSlot(target)}.</color>";
            Refresh();
        }

        /// <summary>For a non-relic item, the natural slot. For relics, the first empty
        /// Relic1/2/3, falling back to Relic1 (which forces a swap).</summary>
        private static EquipmentSlot ResolveTargetSlot(ItemDefinition def, HeroEquipmentSave eq)
        {
            if (!EquipmentSlotHelper.IsRelicSlot(def.Slot)) return def.Slot;
            if (string.IsNullOrEmpty(eq.GetSlot(EquipmentSlot.Relic1))) return EquipmentSlot.Relic1;
            if (string.IsNullOrEmpty(eq.GetSlot(EquipmentSlot.Relic2))) return EquipmentSlot.Relic2;
            if (string.IsNullOrEmpty(eq.GetSlot(EquipmentSlot.Relic3))) return EquipmentSlot.Relic3;
            return EquipmentSlot.Relic1;
        }

        // ---------- Helpers ----------

        private static TextMeshProUGUI FindLabel(Transform root, string path)
        {
            var t = root.Find(path);
            return t != null ? t.GetComponent<TextMeshProUGUI>() : null;
        }
    }
}
