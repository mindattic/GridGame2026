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

namespace Scripts.Vendor.Party
{
    /// <summary>
    /// PARTYMANAGER - Runtime controller for the Party scene.
    /// <para>PURPOSE: Lists every unlocked roster hero on the left, shows the selected
    /// hero's stats on the right. Tap Add/Remove to toggle party membership (party caps
    /// at <see cref="MaxPartySize"/>). Routes to Equip / Abilities scenes for that hero
    /// once those slices ship.</para>
    /// <para>SAVE ROUND-TRIP: ProfileHelper.AddToParty / RemoveFromParty already handles
    /// XP-preserving roster ⇄ party transitions and persists; this scene only refreshes
    /// the UI from the resulting save state.</para>
    /// <para>RELATED FILES: PartyScaffold.cs, ProfileHelper.cs, ActorLibrary.cs, ExperienceHelper.cs</para>
    /// </summary>
    public class PartyManager : MonoBehaviour
    {
        public const int MaxPartySize = 4;

        // Object names — scaffold must match.
        public const string RosterContentPath = "Body/RosterList/Viewport/Content";
        public const string DetailLabelName = "Body/DetailLabel";
        public const string ActionButtonName = "Body/ActionButton";
        public const string EquipButtonName = "Body/EquipButton";
        public const string AbilitiesButtonName = "Body/AbilitiesButton";
        public const string PartyCountLabelName = "Header/PartyCountLabel";
        public const string BackButtonName = "BackButton";

        private CharacterClass selected = CharacterClass.None;
        private TextMeshProUGUI detailLabel;
        private TextMeshProUGUI partyCountLabel;
        private RectTransform rosterContent;
        private Button actionButton;
        private TextMeshProUGUI actionButtonLabel;
        private Button equipButton;
        private Button abilitiesButton;

        private void Awake()
        {
            BootstrapProfile();
            CacheUiReferences();
            WireButtons();
        }

        private void Start()
        {
            scene.FadeIn();
            Refresh();
        }

        // ---------- Boot ----------

        private static void BootstrapProfile()
        {
            if (ProfileHelper.CurrentProfile == null) ProfileHelper.Load();
            if (!ProfileHelper.HasCurrentSave) ProfileHelper.CreateProfile("Dev");
        }

        // ---------- UI lookups & wiring ----------

        private void CacheUiReferences()
        {
            var canvas = GameObject.Find("Canvas");
            if (canvas == null) { Debug.LogError("[PartyManager] Canvas not found."); return; }

            detailLabel = FindLabel(canvas.transform, DetailLabelName);
            partyCountLabel = FindLabel(canvas.transform, PartyCountLabelName);

            var rosterT = canvas.transform.Find(RosterContentPath);
            rosterContent = rosterT != null ? rosterT.GetComponent<RectTransform>() : null;
            if (rosterContent == null) Debug.LogError("[PartyManager] Roster Content not found at " + RosterContentPath);

            var actionT = canvas.transform.Find(ActionButtonName);
            actionButton = actionT != null ? actionT.GetComponent<Button>() : null;
            actionButtonLabel = actionT != null ? actionT.GetComponentInChildren<TextMeshProUGUI>() : null;

            var equipT = canvas.transform.Find(EquipButtonName);
            equipButton = equipT != null ? equipT.GetComponent<Button>() : null;

            var abilT = canvas.transform.Find(AbilitiesButtonName);
            abilitiesButton = abilT != null ? abilT.GetComponent<Button>() : null;
        }

        private void WireButtons()
        {
            if (actionButton != null)
            {
                actionButton.onClick.RemoveAllListeners();
                actionButton.onClick.AddListener(ToggleSelectedInParty);
            }
            if (abilitiesButton != null)
            {
                abilitiesButton.onClick.RemoveAllListeners();
                abilitiesButton.onClick.AddListener(() =>
                {
                    if (selected == CharacterClass.None) return;
                    HeroHandoff.Pending = selected;
                    scene.Fade.ToAbilities();
                });
            }
            if (equipButton != null)
            {
                equipButton.onClick.RemoveAllListeners();
                equipButton.onClick.AddListener(() =>
                {
                    if (selected == CharacterClass.None) return;
                    HeroHandoff.Pending = selected;
                    scene.Fade.ToEquip();
                });
            }

            var canvas = GameObject.Find("Canvas");
            var backT = canvas != null ? canvas.transform.Find(BackButtonName) : null;
            var backBtn = backT != null ? backT.GetComponent<Button>() : null;
            if (backBtn != null)
            {
                backBtn.onClick.RemoveAllListeners();
                backBtn.onClick.AddListener(() => scene.Fade.ToOverworld());
            }
        }

        // ---------- Refresh ----------

        public void Refresh()
        {
            UpdatePartyCount();
            RebuildRoster();
            UpdateDetail();
            UpdateActionButton();
            UpdateRouteButtons();
        }

        private void UpdatePartyCount()
        {
            if (partyCountLabel == null) return;
            int count = PartyMembers().Count;
            partyCountLabel.text = $"Party: {count}/{MaxPartySize}";
            partyCountLabel.color = count == 0 ? HubTheme.Danger : HubTheme.Accent;
        }

        private void RebuildRoster()
        {
            if (rosterContent == null) return;
            for (int i = rosterContent.childCount - 1; i >= 0; i--)
                Object.Destroy(rosterContent.GetChild(i).gameObject);

            // Show party members first (highlighted), then bench.
            var party = PartyMembers();
            var partyClasses = new HashSet<CharacterClass>(party.Select(p => p.CharacterClass));

            foreach (var member in party) CreateRow(member, inParty: true);
            foreach (var member in RosterMembers())
            {
                if (partyClasses.Contains(member.CharacterClass)) continue;
                CreateRow(member, inParty: false);
            }
        }

        private void UpdateDetail()
        {
            if (detailLabel == null) return;
            if (selected == CharacterClass.None)
            {
                detailLabel.text = "<b>Party</b>\nClick a hero to see their stats.\nUse Add/Remove to manage your party (max 4).";
                return;
            }
            var data = ActorLibrary.Get(selected);
            if (data == null) { detailLabel.text = $"<b>{selected}</b>\n(no actor data)"; return; }

            int totalXP = TotalXPFor(selected);
            var (level, xpInLevel) = ExperienceHelper.DeriveFromTotalXP(totalXP);
            var stats = data.GetStats(level);

            var sb = new System.Text.StringBuilder();
            sb.Append("<b>").Append(selected).Append("</b>  Lv ").Append(level).Append('\n');
            sb.Append('\n');
            sb.Append("HP  ").Append(stats.MaxHP.ToString("0")).Append('\n');
            sb.Append("STR ").Append(stats.Strength.ToString("0"))
              .Append("    VIT ").Append(stats.Vitality.ToString("0")).Append('\n');
            sb.Append("AGI ").Append(stats.Agility.ToString("0"))
              .Append("    STA ").Append(stats.Stamina.ToString("0")).Append('\n');
            sb.Append("INT ").Append(stats.Intelligence.ToString("0"))
              .Append("    WIS ").Append(stats.Wisdom.ToString("0")).Append('\n');
            sb.Append("LCK ").Append(stats.Luck.ToString("0")).Append('\n');
            sb.Append('\n');
            sb.Append("XP this level: ").Append(xpInLevel).Append('\n');

            bool inParty = IsInParty(selected);
            sb.Append('\n');
            sb.Append(inParty
                ? "<color=#66cc88>In active party.</color>"
                : "<color=#cccccc>On the bench.</color>");

            detailLabel.text = sb.ToString();
        }

        private void UpdateActionButton()
        {
            if (actionButton == null) return;
            bool hasSelection = selected != CharacterClass.None;
            actionButton.gameObject.SetActive(hasSelection);
            if (!hasSelection) return;

            bool inParty = IsInParty(selected);
            bool partyFull = PartyMembers().Count >= MaxPartySize;
            actionButton.interactable = inParty || !partyFull;
            if (actionButtonLabel != null)
                actionButtonLabel.text = inParty ? "Remove from Party" : "Add to Party";
        }

        private void UpdateRouteButtons()
        {
            bool hasSel = selected != CharacterClass.None;
            if (equipButton != null)
            {
                equipButton.interactable = hasSel;
                var lbl = equipButton.GetComponentInChildren<TextMeshProUGUI>();
                if (lbl != null) lbl.text = "Equip";
            }
            if (abilitiesButton != null)
            {
                abilitiesButton.interactable = hasSel;
                var lbl = abilitiesButton.GetComponentInChildren<TextMeshProUGUI>();
                if (lbl != null) lbl.text = "Abilities";
            }
        }

        // ---------- Roster row factory ----------

        private void CreateRow(CharacterLevelPair member, bool inParty)
        {
            var go = new GameObject("Row_" + member.CharacterClass);
            go.layer = LayerMask.NameToLayer("UI");
            var rt = go.AddComponent<RectTransform>();
            rt.SetParent(rosterContent, false);
            rt.sizeDelta = new Vector2(0f, 64f);

            go.AddComponent<CanvasRenderer>();
            var bg = go.AddComponent<Image>();
            bg.color = (selected == member.CharacterClass)
                ? new Color(0.36f, 0.50f, 0.78f, 1f)
                : new Color(0.20f, 0.24f, 0.34f, 1f);
            bg.raycastTarget = true;

            var btn = go.AddComponent<Button>();
            btn.targetGraphic = bg;
            var captured = member.CharacterClass;
            btn.onClick.AddListener(() => { selected = captured; Refresh(); });

            var le = go.AddComponent<LayoutElement>();
            le.minHeight = 64f; le.preferredHeight = 64f; le.flexibleWidth = 1f;

            // Left accent — green stripe if in party.
            var accentGO = new GameObject("Accent");
            accentGO.layer = LayerMask.NameToLayer("UI");
            var accentRT = accentGO.AddComponent<RectTransform>();
            accentRT.SetParent(rt, false);
            accentRT.anchorMin = new Vector2(0f, 0f); accentRT.anchorMax = new Vector2(0f, 1f);
            accentRT.pivot = new Vector2(0f, 0.5f);
            accentRT.sizeDelta = new Vector2(8f, 0f);
            accentRT.anchoredPosition = Vector2.zero;
            accentGO.AddComponent<CanvasRenderer>();
            var accentImg = accentGO.AddComponent<Image>();
            accentImg.color = inParty ? HubTheme.Success : new Color(0f, 0f, 0f, 0f);
            accentImg.raycastTarget = false;

            // Label
            var (level, _) = ExperienceHelper.DeriveFromTotalXP(member.TotalXP);
            var labelGO = new GameObject("Label");
            labelGO.layer = LayerMask.NameToLayer("UI");
            var labelRT = labelGO.AddComponent<RectTransform>();
            labelRT.SetParent(rt, false);
            labelRT.anchorMin = Vector2.zero; labelRT.anchorMax = Vector2.one;
            labelRT.offsetMin = new Vector2(20f, 4f); labelRT.offsetMax = new Vector2(-12f, -4f);
            labelGO.AddComponent<CanvasRenderer>();
            var tmp = labelGO.AddComponent<TextMeshProUGUI>();
            tmp.text = $"{member.CharacterClass}    <color=#cccccc>Lv {level}</color>";
            tmp.fontSize = 24;
            tmp.color = HubTheme.TextLight;
            tmp.alignment = TextAlignmentOptions.MidlineLeft;
            tmp.enableWordWrapping = false;
            tmp.richText = true;
            tmp.raycastTarget = false;
        }

        // ---------- Party toggle ----------

        private void ToggleSelectedInParty()
        {
            if (selected == CharacterClass.None) return;
            if (IsInParty(selected))
                ProfileHelper.RemoveFromParty(selected);
            else if (PartyMembers().Count < MaxPartySize)
                ProfileHelper.AddToParty(selected);
            // ProfileHelper.AddToParty / RemoveFromParty persist on success.
            Refresh();
        }

        // ---------- Save accessors ----------

        private static List<CharacterLevelPair> PartyMembers()
        {
            return ProfileHelper.CurrentProfile?.CurrentSave?.Party?.Members ?? new List<CharacterLevelPair>();
        }

        private static List<CharacterLevelPair> RosterMembers()
        {
            return ProfileHelper.CurrentProfile?.CurrentSave?.Roster?.Members ?? new List<CharacterLevelPair>();
        }

        private static bool IsInParty(CharacterClass cls) => PartyMembers().Any(m => m.CharacterClass == cls);

        private static int TotalXPFor(CharacterClass cls)
        {
            // Prefer party XP (kept in sync with combat). Fall back to roster (bench).
            var party = PartyMembers().FirstOrDefault(m => m.CharacterClass == cls);
            if (party != null) return party.TotalXP;
            var roster = RosterMembers().FirstOrDefault(m => m.CharacterClass == cls);
            return roster?.TotalXP ?? 0;
        }

        // ---------- Helpers ----------

        private static TextMeshProUGUI FindLabel(Transform root, string path)
        {
            var t = root.Find(path);
            return t != null ? t.GetComponent<TextMeshProUGUI>() : null;
        }
    }
}
