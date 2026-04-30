using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using g = Scripts.Helpers.GameHelper;
using scene = Scripts.Helpers.SceneHelper;
using Scripts.Canvas;
using Scripts.Data.Actor;
using Scripts.Data.Items;
using Scripts.Data.Skills;
using Scripts.Effects;
using Scripts.Factories;
using Scripts.Helpers;
using Scripts.Hub.Sections;
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

namespace Scripts.Hub
{
    /// <summary>
    /// HUBMANAGER - Slim scene controller for the town Hub.
    /// <para>PURPOSE: Owns the live <see cref="PlayerInventory"/> and <see cref="PartyLoadout"/>
    /// hydrated from the current save on Awake, discovers every <see cref="HubSection"/> in the
    /// scene at startup, and routes NavBar clicks to the matching section.</para>
    /// <para>FLOW:
    /// <list type="bullet">
    /// <item>Awake → LoadFromSave → FindAndBindSections → Show(default).</item>
    /// <item>Section mutates Inventory / Loadout → calls PersistAndRefresh.</item>
    /// <item>PersistAndRefresh → save-to-disk → current section Refresh().</item>
    /// <item>ExitToOverworld / ExitToBattle → persist → SceneHelper.Fade.ToX().</item>
    /// </list></para>
    /// <para>RELATED FILES: HubSection.cs, HubTheme.cs, ProfileHelper.cs, SceneHelper.cs</para>
    /// </summary>
    public class HubManager : MonoBehaviour
    {
        public PlayerInventory Inventory { get; private set; }
        public PartyLoadout Loadout { get; private set; }

        /// <summary>Cross-section handoff: when PartySection routes the player to EquipSection,
        /// it stuffs the chosen hero here. EquipSection reads + clears on its next OnActivated.</summary>
        public CharacterClass PendingEquipHero = CharacterClass.None;

        private readonly List<HubSection> sections = new List<HubSection>();
        private HubSection current;

        // NavBar button cache (populated on Awake via Find)
        private readonly Dictionary<System.Type, Button> navButtons = new Dictionary<System.Type, Button>();

        // Hamburger menu dropdown — hidden by default; toggled by MenuButton, closed by backdrop or item click.
        private GameObject menuDropdown;

        private void Awake()
        {
            LoadFromSave();
            FindAndBindSections();
            WireMenuDropdown();
            // Section buttons live inside MenuDropdown which the scaffold starts inactive.
            // Activate it so GameObject.Find resolves the buttons during wiring, then close.
            if (menuDropdown != null) menuDropdown.SetActive(true);
            WireNavBar();
            WireExitButtons();
            if (menuDropdown != null) menuDropdown.SetActive(false);
            // Default section on scene entry
            Show<PartySection>();
        }

        private void Start()
        {
            scene.FadeIn();
            // Drain post-battle notifications (broken weapons, etc.) into the toast strip so the
            // player sees them when they get back to town instead of having them silently swallowed.
            // Toast overwrites on rapid Show() calls, so collapse the batch into one multi-line toast.
            if (BattleEventTracker.HasMessages)
            {
                HubToast.Show(string.Join("\n", BattleEventTracker.Drain()));
            }
        }

        // ---------- Save Round-trip ----------

        private void LoadFromSave()
        {
            var save = ProfileHelper.CurrentProfile?.CurrentSave;
            Inventory = new PlayerInventory();
            Loadout = new PartyLoadout();
            if (save == null) return;
            Inventory.LoadFromSaveData(save.Inventory);
            Loadout.LoadFromSave(save.Equipment);
        }

        /// <summary>Writes inventory + loadout to save, persists to disk, then refreshes the current section.</summary>
        public void PersistAndRefresh()
        {
            var save = ProfileHelper.CurrentProfile?.CurrentSave;
            if (save != null)
            {
                save.Inventory = Inventory.ToSaveData();
                save.Equipment = Loadout.ToSave();
                ProfileHelper.Save(overwrite: true);
            }
            current?.Refresh();
            UpdateGoldLabels();
        }

        // ---------- Section Discovery + Routing ----------

        private void FindAndBindSections()
        {
            sections.Clear();
            var all = FindObjectsOfType<HubSection>(includeInactive: true);
            foreach (var s in all)
            {
                s.Bind(this);
                s.gameObject.SetActive(false);
                sections.Add(s);
            }
        }

        public void Show<TSection>() where TSection : HubSection
        {
            // While most sections are flagged off, redirect to Party rather than warning.
            if (!HubFeatureFlags.IsEnabled<TSection>() && typeof(TSection) != typeof(PartySection))
            {
                ShowParty();
                return;
            }
            var next = FindSection<TSection>();
            if (next == null)
            {
                Debug.LogWarning($"[HubManager] No section of type {typeof(TSection).Name} in scene.");
                return;
            }
            if (current == next) { current.Refresh(); return; }

            current?.Activate(false);
            current = next;
            current.Activate(true);
            HighlightNav(typeof(TSection));
            UpdateGoldLabels();
        }

        private TSection FindSection<TSection>() where TSection : HubSection
        {
            foreach (var s in sections) if (s is TSection t) return t;
            return null;
        }

        // ---------- NavBar wiring ----------

        private void WireNavBar()
        {
            WireNav<PartySection>(GameObjectHelper.Hub.PartyButton);
            WireNav<GeneralStoreSection>(GameObjectHelper.Hub.ShopButton);
            WireNav<AlchemistSection>(GameObjectHelper.Hub.AlchemistButton);
            WireNav<InnSection>(GameObjectHelper.Hub.ResidenceButton);
            WireNav<BlacksmithSection>(GameObjectHelper.Hub.BlacksmithButton);
            WireNav<TrainingSection>(GameObjectHelper.Hub.TrainingButton);
            WireNav<EquipSection>(GameObjectHelper.Hub.EquipButton);
            WireNav<InventorySection>(GameObjectHelper.Hub.InventoryButton);
            WireNav<EnchantSection>(GameObjectHelper.Hub.EnchanterButton);
            WireNav<SalvageSection>(GameObjectHelper.Hub.SalvageButton);
            WireNav<PlacesSection>(GameObjectHelper.Hub.PlacesButton);
            WireNav<BountySection>(GameObjectHelper.Hub.BountyButton);
        }

        private void WireNav<TSection>(string buttonName) where TSection : HubSection
        {
            var go = GameObject.Find(buttonName);
            if (go == null) return;
            var btn = go.GetComponent<Button>();
            if (btn == null) return;
            btn.onClick.RemoveAllListeners();
            btn.onClick.AddListener(() => { Show<TSection>(); CloseMenu(); });
            navButtons[typeof(TSection)] = btn;
        }

        // ---------- Hamburger Menu ----------

        private void WireMenuDropdown()
        {
            menuDropdown = GameObject.Find(GameObjectHelper.Hub.MenuDropdown);
            // The dropdown starts inactive (set by the scaffold) so Find won't return it after Awake.
            // Resolve via the canvas instead.
            if (menuDropdown == null)
            {
                var canvas = GameObject.Find("Canvas");
                if (canvas != null)
                {
                    var t = canvas.transform.Find(GameObjectHelper.Hub.MenuDropdown);
                    if (t != null) menuDropdown = t.gameObject;
                }
            }

            var menuBtnGO = GameObject.Find(GameObjectHelper.Hub.MenuButton);
            if (menuBtnGO != null)
            {
                var btn = menuBtnGO.GetComponent<Button>();
                if (btn != null)
                {
                    btn.onClick.RemoveAllListeners();
                    btn.onClick.AddListener(ToggleMenu);
                }
            }

            if (menuDropdown != null)
            {
                var backdrop = menuDropdown.transform.Find(GameObjectHelper.Hub.MenuBackdrop);
                if (backdrop != null)
                {
                    var btn = backdrop.GetComponent<Button>();
                    if (btn != null)
                    {
                        btn.onClick.RemoveAllListeners();
                        btn.onClick.AddListener(CloseMenu);
                    }
                }
            }
        }

        private void ToggleMenu()
        {
            if (menuDropdown == null) return;
            menuDropdown.SetActive(!menuDropdown.activeSelf);
        }

        private void CloseMenu()
        {
            if (menuDropdown != null) menuDropdown.SetActive(false);
        }

        private void HighlightNav(System.Type activeType)
        {
            foreach (var kvp in navButtons)
            {
                var img = kvp.Value.GetComponent<Image>();
                if (img == null) continue;
                img.color = kvp.Key == activeType ? HubTheme.NavActive : HubTheme.NavIdle;
            }
        }

        // ---------- Exit buttons ----------

        private void WireExitButtons()
        {
            var ow = GameObject.Find(GameObjectHelper.Hub.OverworldButton);
            if (ow != null)
            {
                var btn = ow.GetComponent<Button>();
                if (btn != null)
                {
                    btn.onClick.RemoveAllListeners();
                    btn.onClick.AddListener(ExitToOverworld);
                }
            }
            var battle = GameObject.Find(GameObjectHelper.Hub.BattleButton);
            if (battle != null)
            {
                var btn = battle.GetComponent<Button>();
                if (btn != null)
                {
                    btn.onClick.RemoveAllListeners();
                    btn.onClick.AddListener(ExitToBattle);
                }
            }
        }

        public void ExitToOverworld()
        {
            PersistAndRefresh();
            scene.Fade.ToOverworld();
        }

        public void ExitToBattle()
        {
            // Refuse to enter combat with an empty party — the player would lose instantly with
            // nothing to control. Bounce them to Party so they can fix it.
            var party = ProfileHelper.CurrentProfile?.CurrentSave?.Party?.Members;
            if (party == null || party.Count == 0)
            {
                HubToast.Show("Add at least one hero to your party before heading to battle.");
                Show<PartySection>();
                return;
            }
            PersistAndRefresh();
            // After victory or defeat, the post-battle screen routes back here so XP/levels are
            // visible on the same Hub the player came from.
            ExperienceTracker.NextSceneAfterPostBattleScreen = scene.Hub;
            scene.Fade.ToGame();
        }

        // ---------- Shared label updates ----------

        private void UpdateGoldLabels()
        {
            var goldText = HubTheme.FormatGold(Inventory.Gold);
            foreach (var label in FindObjectsOfType<TMP_Text>(includeInactive: true))
            {
                if (label != null && label.gameObject.name == GameObjectHelper.Hub.GoldLabel)
                    label.text = $"Gold: {goldText}";
            }
        }

        // ---------- Nav button helpers (wired by scaffold via UnityEvent for inspector compat) ----------

        public void ShowParty() => Show<PartySection>();
        public void ShowGeneralStore() => Show<GeneralStoreSection>();
        public void ShowAlchemist() => Show<AlchemistSection>();
        public void ShowInn() => Show<InnSection>();
        public void ShowBlacksmith() => Show<BlacksmithSection>();
        public void ShowTraining() => Show<TrainingSection>();
        public void ShowEquip() => Show<EquipSection>();
        public void ShowInventory() => Show<InventorySection>();
        public void ShowEnchanter() => Show<EnchantSection>();
        public void ShowSalvage() => Show<SalvageSection>();
        public void ShowPlaces() => Show<PlacesSection>();
        public void ShowBounty() => Show<BountySection>();
    }
}
