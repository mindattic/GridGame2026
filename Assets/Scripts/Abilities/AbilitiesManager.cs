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

namespace Scripts.Vendor.Abilities
{
    /// <summary>
    /// ABILITIESMANAGER - Runtime controller for the Abilities scene.
    /// <para>PURPOSE: Configures the 5-slot ability bar for the hero handed off via
    /// <see cref="HeroHandoff.Pending"/>. The bar binds consumables (e.g. healing
    /// potions) so they can be triggered from slots 1–5 in combat (combat wiring
    /// lands in slice 7). Each slot stores a consumable item ID; the actual item
    /// stack stays in the shared inventory and is consumed at use time.</para>
    /// <para>UX: Click a consumable in the right pane to assign it to the first
    /// empty slot. Click a filled slot to clear it. If all slots are full and the
    /// player clicks a consumable, the flash label tells them to clear a slot first.</para>
    /// <para>RELATED FILES: AbilitiesBuilder.cs, HeroHandoff.cs, Profile.HeroEquipmentSave</para>
    /// </summary>
    public class AbilitiesManager : MonoBehaviour
    {
        public const int SlotCount = 5; // Mirrors HeroLoadout.MaxAbilitySlots

        public const string TitleLabelName = "Header/Title";
        public const string SlotsContainerName = "Body/SlotsRow";
        public const string ConsumablesContentPath = "Body/ConsumablesList/Viewport/Content";
        public const string FlashLabelName = "Body/FlashLabel";
        public const string BackButtonName = "BackButton";
        public const string SlotButtonNamePrefix = "Slot";

        private CharacterClass hero = CharacterClass.None;
        private TextMeshProUGUI titleLabel;
        private TextMeshProUGUI flashLabel;
        private RectTransform slotsContainer;
        private RectTransform consumablesContent;

        private void Awake()
        {
            BootstrapProfile();
            ResolveHero();
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
            // Prefer the explicit handoff from PartyManager. Fall back to first party member
            // so booting straight into Abilities for dev-iteration still has something to show.
            if (HeroHandoff.Pending != CharacterClass.None)
            {
                hero = HeroHandoff.Pending;
                HeroHandoff.Pending = CharacterClass.None; // consume so we don't latch stale state
                return;
            }
            var party = ProfileHelper.CurrentProfile?.CurrentSave?.Party?.Members;
            if (party != null && party.Count > 0) hero = party[0].CharacterClass;
        }

        // ---------- UI lookups ----------

        private void CacheUiReferences()
        {
            var canvas = GameObject.Find("Canvas");
            if (canvas == null) { Debug.LogError("[AbilitiesManager] Canvas not found."); return; }

            titleLabel = FindLabel(canvas.transform, TitleLabelName);
            flashLabel = FindLabel(canvas.transform, FlashLabelName);
            if (flashLabel != null) flashLabel.text = "";

            var slotsT = canvas.transform.Find(SlotsContainerName);
            slotsContainer = slotsT != null ? slotsT.GetComponent<RectTransform>() : null;

            var contentT = canvas.transform.Find(ConsumablesContentPath);
            consumablesContent = contentT != null ? contentT.GetComponent<RectTransform>() : null;
            var vlg = consumablesContent?.GetComponent<VerticalLayoutGroup>();
            if (vlg != null) vlg.childControlWidth = false;
            var viewport = consumablesContent?.parent as RectTransform;
            if (viewport != null)
            {
                var stencilMask = viewport.GetComponent<Mask>();
                if (stencilMask != null) stencilMask.enabled = false;
                if (viewport.GetComponent<RectMask2D>() == null)
                    viewport.gameObject.AddComponent<RectMask2D>();
            }
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

        private static HeroEquipmentSave Equipment(CharacterClass c)
        {
            var save = ProfileHelper.CurrentProfile?.CurrentSave;
            if (save == null) return null;
            if (save.Equipment == null) save.Equipment = new EquipmentSaveData();
            return save.Equipment.GetOrCreate(c);
        }

        private static List<AbilityBarSlotSave> SlotsFor(CharacterClass c)
        {
            var eq = Equipment(c);
            if (eq == null) return new List<AbilityBarSlotSave>();
            if (eq.AbilityBarSlots == null) eq.AbilityBarSlots = new List<AbilityBarSlotSave>();
            // Pad up to SlotCount with empty entries so the UI can index 0..SlotCount-1.
            while (eq.AbilityBarSlots.Count < SlotCount) eq.AbilityBarSlots.Add(new AbilityBarSlotSave());
            return eq.AbilityBarSlots;
        }

        private static void Persist()
        {
            ProfileHelper.Save(overwrite: true);
        }

        // ---------- Refresh ----------

        public void Refresh()
        {
            UpdateTitle();
            RebuildSlotsRow();
            RebuildConsumables();
        }

        private void UpdateTitle()
        {
            if (titleLabel == null) return;
            titleLabel.text = hero != CharacterClass.None
                ? $"Abilities — {hero}"
                : "Abilities";
        }

        private void RebuildSlotsRow()
        {
            if (slotsContainer == null) return;
            for (int i = slotsContainer.childCount - 1; i >= 0; i--)
                Object.Destroy(slotsContainer.GetChild(i).gameObject);

            if (hero == CharacterClass.None)
            {
                var emptyMsg = MakeEmptyMessage(slotsContainer, "Add a hero to your Party first.");
                return;
            }

            var slots = SlotsFor(hero);
            int unlocked = Scripts.Services.AbilitySlotProgression.UnlockedSlotsForCurrentSave();
            for (int i = 0; i < SlotCount; i++) CreateSlotButton(i, slots[i], locked: i >= unlocked);
        }

        private void RebuildConsumables()
        {
            if (consumablesContent == null) return;
            for (int i = consumablesContent.childCount - 1; i >= 0; i--)
                Object.Destroy(consumablesContent.GetChild(i).gameObject);

            // Section 1: the hero's own active skills & spells (ActorData.Abilities) —
            // assignable to bar slots by name; combat resolves them via AbilityLibrary.Get
            // (HeroLoadout.LoadFromSave handles the IsAbility slot kind already).
            if (hero != CharacterClass.None)
            {
                var actorData = ActorLibrary.Get(hero);
                var known = actorData?.Abilities;
                if (known != null && known.Count > 0)
                {
                    CreateSectionHeader("Skills & Spells");
                    foreach (var ability in known)
                    {
                        if (ability == null || !ability.IsActive) continue;
                        CreateAbilityRow(ability);
                    }
                }
            }

            // Section 2: consumables from the live inventory — same source of truth as
            // Vendor / Alchemist.
            var save = ProfileHelper.CurrentProfile?.CurrentSave;
            if (save?.Inventory?.Items != null)
            {
                CreateSectionHeader("Items");
                foreach (var entry in save.Inventory.Items)
                {
                    if (entry.Count <= 0) continue;
                    var def = ItemLibrary.Get(entry.ItemId);
                    if (def == null || def.Type != ItemType.Consumable) continue;
                    CreateConsumableRow(def, entry.Count);
                }
            }

            LayoutRebuilder.ForceRebuildLayoutImmediate(consumablesContent);
        }

        // ---------- UI factories ----------

        private void CreateSlotButton(int index, AbilityBarSlotSave slot, bool locked = false)
        {
            var go = new GameObject($"{SlotButtonNamePrefix}{index}");
            go.layer = LayerMask.NameToLayer("UI");
            var rt = go.AddComponent<RectTransform>();
            rt.SetParent(slotsContainer, false);

            go.AddComponent<CanvasRenderer>();
            var bg = go.AddComponent<Image>();
            bg.color = locked
                ? HubTheme.RowLocked
                : slot.IsEmpty ? HubTheme.RowBg : HubTheme.RowSelected;
            bg.raycastTarget = !locked;

            if (!locked)
            {
                var btn = go.AddComponent<Button>();
                btn.targetGraphic = bg;
                int captured = index;
                btn.onClick.AddListener(() => OnSlotClicked(captured));
            }

            var le = go.AddComponent<LayoutElement>();
            le.minWidth = 120f; le.preferredWidth = 140f; le.flexibleWidth = 1f;
            le.minHeight = 100f; le.preferredHeight = 100f; le.flexibleHeight = 0f;

            // Slot index badge (top-left)
            var badgeGO = new GameObject("Index");
            badgeGO.layer = LayerMask.NameToLayer("UI");
            var badgeRT = badgeGO.AddComponent<RectTransform>();
            badgeRT.SetParent(rt, false);
            badgeRT.anchorMin = new Vector2(0f, 1f); badgeRT.anchorMax = new Vector2(0f, 1f);
            badgeRT.pivot = new Vector2(0f, 1f);
            badgeRT.sizeDelta = new Vector2(40f, 30f);
            badgeRT.anchoredPosition = new Vector2(8f, -4f);
            badgeGO.AddComponent<CanvasRenderer>();
            var badgeTmp = badgeGO.AddComponent<TextMeshProUGUI>();
            badgeTmp.font = UiFonts.Body;
            badgeTmp.text = (index + 1).ToString();
            badgeTmp.fontSize = 22;
            badgeTmp.color = HubTheme.Accent;
            badgeTmp.alignment = TextAlignmentOptions.TopLeft;
            badgeTmp.fontStyle = FontStyles.Bold;
            badgeTmp.raycastTarget = false;

            // Slot content label
            var labelGO = new GameObject("Label");
            labelGO.layer = LayerMask.NameToLayer("UI");
            var labelRT = labelGO.AddComponent<RectTransform>();
            labelRT.SetParent(rt, false);
            labelRT.anchorMin = Vector2.zero; labelRT.anchorMax = Vector2.one;
            labelRT.offsetMin = new Vector2(8f, 8f); labelRT.offsetMax = new Vector2(-8f, -8f);
            labelGO.AddComponent<CanvasRenderer>();
            var tmp = labelGO.AddComponent<TextMeshProUGUI>();
            tmp.font = UiFonts.Body;
            string content;
            if (locked)
            {
                int gate = Scripts.Services.AbilitySlotProgression.GateForSlot(index);
                content = gate >= 0
                    ? $"<color=#666666>Locked\nclear stage {gate + 1}</color>"
                    : "<color=#666666>Locked</color>";
            }
            else if (slot.IsEmpty) content = "<color=#888888>Empty</color>";
            else if (slot.IsItem)
            {
                var def = ItemLibrary.Get(slot.ItemId);
                content = def != null ? def.DisplayName : slot.ItemId;
            }
            else content = slot.AbilityName;
            tmp.text = content;
            tmp.fontSize = 20;
            tmp.color = HubTheme.TextLight;
            tmp.alignment = TextAlignmentOptions.Center;
            tmp.enableWordWrapping = true;
            tmp.richText = true;
            tmp.raycastTarget = false;
        }

        /// <summary>Non-clickable section divider in the assignables list.</summary>
        private void CreateSectionHeader(string text)
        {
            var go = new GameObject("Section_" + text);
            go.layer = LayerMask.NameToLayer("UI");
            var rt = go.AddComponent<RectTransform>();
            rt.SetParent(consumablesContent, false);
            rt.anchorMin = new Vector2(0f, 1f);
            rt.anchorMax = new Vector2(1f, 1f);
            rt.pivot = new Vector2(0.5f, 1f);
            rt.sizeDelta = new Vector2(0f, 44f);

            go.AddComponent<CanvasRenderer>();
            var le = go.AddComponent<LayoutElement>();
            le.minHeight = 44f; le.preferredHeight = 44f; le.flexibleWidth = 1f;

            var tmp = go.AddComponent<TextMeshProUGUI>();
            tmp.font = UiFonts.Body;
            tmp.text = text;
            tmp.fontSize = 22;
            tmp.fontStyle = FontStyles.Bold;
            tmp.color = HubTheme.Accent;
            tmp.alignment = TextAlignmentOptions.MidlineLeft;
            tmp.raycastTarget = false;
        }

        /// <summary>Clickable row for one of the hero's known active abilities.</summary>
        private void CreateAbilityRow(Ability ability)
        {
            var go = new GameObject("Ability_" + ability.name);
            go.layer = LayerMask.NameToLayer("UI");
            var rt = go.AddComponent<RectTransform>();
            rt.SetParent(consumablesContent, false);
            rt.anchorMin = new Vector2(0f, 1f);
            rt.anchorMax = new Vector2(1f, 1f);
            rt.pivot = new Vector2(0.5f, 1f);
            rt.sizeDelta = new Vector2(0f, 56f);

            go.AddComponent<CanvasRenderer>();
            var bg = go.AddComponent<Image>();
            bg.color = HubTheme.RowBg;
            bg.raycastTarget = true;

            var btn = go.AddComponent<Button>();
            btn.targetGraphic = bg;
            var captured = ability;
            btn.onClick.AddListener(() => OnAbilityClicked(captured));

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
            tmp.font = UiFonts.Body;
            string cost = ability.ManaCost > 0 ? $"    <color=#7db8e8>{ability.ManaCost} mana</color>" : "";
            tmp.text = $"{ability.name}{cost}";
            tmp.fontSize = 22;
            tmp.color = HubTheme.TextLight;
            tmp.alignment = TextAlignmentOptions.MidlineLeft;
            tmp.enableWordWrapping = false;
            tmp.richText = true;
            tmp.raycastTarget = false;
        }

        private void CreateConsumableRow(ItemDefinition def, int owned)
        {
            var go = new GameObject("Row_" + def.Id);
            go.layer = LayerMask.NameToLayer("UI");
            var rt = go.AddComponent<RectTransform>();
            rt.SetParent(consumablesContent, false);
            rt.anchorMin = new Vector2(0f, 1f);
            rt.anchorMax = new Vector2(1f, 1f);
            rt.pivot = new Vector2(0.5f, 1f);
            rt.sizeDelta = new Vector2(0f, 56f);

            go.AddComponent<CanvasRenderer>();
            var bg = go.AddComponent<Image>();
            bg.color = HubTheme.RowBg;
            bg.raycastTarget = true;

            var btn = go.AddComponent<Button>();
            btn.targetGraphic = bg;
            var captured = def;
            btn.onClick.AddListener(() => OnConsumableClicked(captured));

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
            tmp.font = UiFonts.Body;
            tmp.text = $"{def.DisplayName}    <color=#cccccc>×{owned}</color>";
            tmp.fontSize = 22;
            tmp.color = HubItemRowFactory.RarityColor(def.Rarity);
            tmp.alignment = TextAlignmentOptions.MidlineLeft;
            tmp.enableWordWrapping = false;
            tmp.richText = true;
            tmp.raycastTarget = false;
        }

        private static RectTransform MakeEmptyMessage(RectTransform parent, string text)
        {
            var go = new GameObject("EmptyMessage");
            go.layer = LayerMask.NameToLayer("UI");
            var rt = go.AddComponent<RectTransform>();
            rt.SetParent(parent, false);
            go.AddComponent<CanvasRenderer>();
            var tmp = go.AddComponent<TextMeshProUGUI>();
            tmp.font = UiFonts.Body;
            tmp.text = text;
            tmp.fontSize = 24;
            tmp.color = HubTheme.TextMuted;
            tmp.alignment = TextAlignmentOptions.Center;
            tmp.enableWordWrapping = true;
            tmp.richText = true;
            tmp.raycastTarget = false;
            return rt;
        }

        // ---------- Click handlers ----------

        private void OnSlotClicked(int index)
        {
            if (hero == CharacterClass.None) return;
            var slots = SlotsFor(hero);
            if (slots[index].IsEmpty) return; // nothing to clear
            var cleared = slots[index];
            slots[index] = new AbilityBarSlotSave(); // reset to empty
            Persist();
            if (flashLabel != null)
                flashLabel.text = $"<color=#cccccc>Cleared slot {index + 1}.</color>";
            Refresh();
            // suppress unused warning
            _ = cleared;
        }

        /// <summary>Assigns a known skill/spell to the first empty bar slot (by name — the
        /// IsAbility slot kind; combat resolves it via AbilityLibrary.Get at loadout time).</summary>
        private void OnAbilityClicked(Ability ability)
        {
            if (hero == CharacterClass.None || ability == null) return;
            var slots = SlotsFor(hero);

            for (int i = 0; i < slots.Count; i++)
            {
                if (!slots[i].IsEmpty && slots[i].AbilityName == ability.name)
                {
                    if (flashLabel != null)
                        flashLabel.text = $"<color=#e5c878>{ability.name} is already on the bar.</color>";
                    return;
                }
            }

            int unlockedForAbility = Scripts.Services.AbilitySlotProgression.UnlockedSlotsForCurrentSave();
            int firstEmpty = -1;
            for (int i = 0; i < Mathf.Min(slots.Count, unlockedForAbility); i++)
            {
                if (slots[i].IsEmpty) { firstEmpty = i; break; }
            }
            if (firstEmpty < 0)
            {
                if (flashLabel != null)
                    flashLabel.text = "<color=#e57878>No open slot — clear one, or unlock more by clearing stages.</color>";
                return;
            }

            slots[firstEmpty] = new AbilityBarSlotSave(abilityName: ability.name, itemId: null);
            Persist();
            if (flashLabel != null)
                flashLabel.text = $"<color=#66cc88>Assigned {ability.name} to slot {firstEmpty + 1}.</color>";
            Refresh();
        }

        private void OnConsumableClicked(ItemDefinition def)
        {
            if (hero == CharacterClass.None || def == null) return;
            var slots = SlotsFor(hero);
            int unlockedForItem = Scripts.Services.AbilitySlotProgression.UnlockedSlotsForCurrentSave();
            int firstEmpty = -1;
            for (int i = 0; i < Mathf.Min(slots.Count, unlockedForItem); i++)
            {
                if (slots[i].IsEmpty) { firstEmpty = i; break; }
            }
            if (firstEmpty < 0)
            {
                if (flashLabel != null)
                    flashLabel.text = "<color=#e57878>No open slot — clear one, or unlock more by clearing stages.</color>";
                return;
            }
            slots[firstEmpty] = new AbilityBarSlotSave(abilityName: null, itemId: def.Id);
            Persist();
            if (flashLabel != null)
                flashLabel.text = $"<color=#66cc88>Assigned {def.DisplayName} to slot {firstEmpty + 1}.</color>";
            Refresh();
        }

        // ---------- Helpers ----------

        private static TextMeshProUGUI FindLabel(Transform root, string path)
        {
            var t = root.Find(path);
            return t != null ? t.GetComponent<TextMeshProUGUI>() : null;
        }
    }
}
