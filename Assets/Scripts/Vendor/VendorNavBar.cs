using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
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
    /// VENDORNAVBAR - Hamburger-menu navigation shared by every vendor scene AND by StageSelect.
    /// <para>PURPOSE: Lets the player hop Vendor ⇄ Alchemist ⇄ ... ⇄ StageSelect without
    /// any intermediate scene. A floating hamburger button at the upper-right opens a dropdown
    /// listing every scene; the active scene's row is highlighted and inert.</para>
    /// <para>WIRING: Each builder calls VendorNavBarBuilder.Build(canvas) which creates the
    /// hamburger, backdrop, and dropdown, then attaches this script. On Awake the script binds
    /// click handlers by GameObject name (no SerializeField) and hoists itself to the top of
    /// the render stack so the dropdown paints above the Body.</para>
    /// <para>RELATED FILES: VendorNavBarBuilder.cs, SceneHelper.cs, StageSelectManager.cs</para>
    /// </summary>
    public class VendorNavBar : MonoBehaviour
    {
        // GameObject names — Builder and runtime must agree.
        public const string RootName = "VendorNavBar";
        public const string HamburgerButtonName = "VendorNavBar_Hamburger";
        public const string DropdownName = "VendorNavBar_Dropdown";
        public const string BackdropName = "VendorNavBar_Backdrop";
        public const string VendorButtonName = "VendorNavBar_VendorButton";
        public const string AlchemistButtonName = "VendorNavBar_AlchemistButton";
        public const string PartyButtonName = "VendorNavBar_PartyButton";
        public const string AbilitiesButtonName = "VendorNavBar_AbilitiesButton";
        public const string EquipButtonName = "VendorNavBar_EquipButton";
        public const string BlacksmithButtonName = "VendorNavBar_BlacksmithButton";
        public const string StageSelectButtonName = "VendorNavBar_StageSelectButton";

        // Single source of truth for the buttons that exist in the dropdown.
        // When new vendor scenes ship, append to this list and re-builder.
        public static readonly List<(string buttonName, string sceneName, string label)> Entries = new()
        {
            (VendorButtonName,      scene.Vendor,      "Merchant"),
            (AlchemistButtonName,   scene.Alchemist,   "Alchemist"),
            (BlacksmithButtonName,  scene.Blacksmith,  "Blacksmith"),
            (PartyButtonName,       scene.Party,       "Party"),
            (AbilitiesButtonName,   scene.Abilities,   "Abilities"),
            (EquipButtonName,       scene.Equip,       "Equip"),
            (StageSelectButtonName, scene.StageSelect, "Campaign"),
        };

        private static readonly Color ActiveTint = new Color(0.28f, 0.42f, 0.70f, 1f);
        private static readonly Color IdleTint   = new Color(0.14f, 0.18f, 0.28f, 1f);
        private static readonly Color HomeTint   = new Color(0.36f, 0.50f, 0.78f, 1f);

        private GameObject dropdown;
        private GameObject backdrop;

        private void Awake()
        {
            HoistAboveBody();

            dropdown = transform.Find(DropdownName)?.gameObject;
            backdrop = transform.Find(BackdropName)?.gameObject;

            WireHamburger();
            WireBackdrop();

            string activeScene = SceneManager.GetActiveScene().name;
            foreach (var entry in Entries) WireEntry(entry.buttonName, entry.sceneName, activeScene);

            SetOpen(false);
        }

        // Move this nav under FadeOverlay (or to the end if there isn't one) so the dropdown
        // paints above Body/BackButton/etc. without each builder needing to reorder siblings.
        private void HoistAboveBody()
        {
            var parent = transform.parent;
            if (parent == null) return;
            var fade = parent.Find("FadeOverlay");
            if (fade != null) transform.SetSiblingIndex(fade.GetSiblingIndex());
            else              transform.SetAsLastSibling();
        }

        private void WireHamburger()
        {
            var btnT = transform.Find(HamburgerButtonName);
            var btn = btnT != null ? btnT.GetComponent<Button>() : null;
            if (btn == null) return;
            btn.onClick.RemoveAllListeners();
            btn.onClick.AddListener(Toggle);
        }

        private void WireBackdrop()
        {
            if (backdrop == null) return;
            var btn = backdrop.GetComponent<Button>();
            if (btn == null) return;
            btn.onClick.RemoveAllListeners();
            btn.onClick.AddListener(() => SetOpen(false));
        }

        private void WireEntry(string buttonName, string targetScene, string activeScene)
        {
            if (dropdown == null) return;
            var t = dropdown.transform.Find(buttonName);
            if (t == null) return;
            var btn = t.GetComponent<Button>();
            if (btn == null) return;

            var img = btn.GetComponent<Image>();
            bool isCampaignHome = targetScene == scene.StageSelect;
            if (img != null) img.color = (targetScene == activeScene) ? ActiveTint
                                       : isCampaignHome              ? HomeTint
                                                                     : IdleTint;

            if (targetScene == activeScene)
            {
                btn.interactable = false;
                return;
            }

            btn.onClick.RemoveAllListeners();
            btn.onClick.AddListener(() =>
            {
                SetOpen(false);
                scene.Fade.To(targetScene);
            });
        }

        public void Toggle() => SetOpen(dropdown != null && !dropdown.activeSelf);

        public void SetOpen(bool open)
        {
            if (dropdown != null) dropdown.SetActive(open);
            if (backdrop != null) backdrop.SetActive(open);
        }
    }
}
