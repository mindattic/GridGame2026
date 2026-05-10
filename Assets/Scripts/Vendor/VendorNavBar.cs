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
    /// VENDORNAVBAR - Persistent top-edge navigation strip shared by every vendor scene.
    /// <para>PURPOSE: Lets the player hop Store ⇄ Alchemist ⇄ ... without going through the
    /// Overworld, so traversing the town feels like one continuous space. The active scene's
    /// button is highlighted; clicking another fades to that scene.</para>
    /// <para>WIRING: Each vendor scaffold calls VendorNavBarScaffold.Build(canvas) which
    /// creates the strip + buttons and attaches this script. On Awake the script binds onClick
    /// handlers by GameObject name (no SerializeField).</para>
    /// <para>RELATED FILES: VendorNavBarScaffold.cs, SceneHelper.cs</para>
    /// </summary>
    public class VendorNavBar : MonoBehaviour
    {
        // GameObject names — Scaffold and runtime must agree.
        public const string RootName = "VendorNavBar";
        public const string StoreButtonName = "VendorNavBar_StoreButton";
        public const string AlchemistButtonName = "VendorNavBar_AlchemistButton";
        public const string PartyButtonName = "VendorNavBar_PartyButton";
        public const string AbilitiesButtonName = "VendorNavBar_AbilitiesButton";
        public const string EquipButtonName = "VendorNavBar_EquipButton";
        public const string BlacksmithButtonName = "VendorNavBar_BlacksmithButton";
        public const string OverworldButtonName = "VendorNavBar_OverworldButton";
        public const string BattleButtonName = "VendorNavBar_BattleButton";

        // Single source of truth for the buttons that exist on the bar.
        // When new vendor scenes ship, append to this list and re-scaffold.
        public static readonly List<(string buttonName, string sceneName, string label)> Entries = new()
        {
            (StoreButtonName,      scene.Store,      "Store"),
            (AlchemistButtonName,  scene.Alchemist,  "Alchemist"),
            (BlacksmithButtonName, scene.Blacksmith, "Blacksmith"),
            (PartyButtonName,      scene.Party,      "Party"),
            (AbilitiesButtonName,  scene.Abilities,  "Abilities"),
            (EquipButtonName,      scene.Equip,      "Equip"),
            (OverworldButtonName,  scene.Overworld,  "Overworld"),
            (BattleButtonName,     scene.Game,       "Battle"),
        };

        private static readonly Color ActiveTint = new Color(0.28f, 0.42f, 0.70f, 1f);
        private static readonly Color IdleTint   = new Color(0.14f, 0.18f, 0.28f, 1f);

        private void Awake()
        {
            string activeScene = SceneManager.GetActiveScene().name;
            foreach (var entry in Entries) Wire(entry.buttonName, entry.sceneName, activeScene);
        }

        private void Wire(string buttonName, string targetScene, string activeScene)
        {
            var t = transform.Find(buttonName);
            if (t == null) return;
            var btn = t.GetComponent<Button>();
            if (btn == null) return;

            var img = btn.GetComponent<Image>();
            bool isBattle = targetScene == scene.Game;
            // Battle is an action button (not lateral nav) — give it the gold accent so it
            // reads as the "go fight" affordance instead of just another vendor link.
            if (img != null) img.color = isBattle
                ? new Color(1f, 0.78f, 0.28f, 1f)
                : ((targetScene == activeScene) ? ActiveTint : IdleTint);

            // The active scene's button stays inert — clicking it would fade to itself.
            if (targetScene == activeScene)
            {
                btn.interactable = false;
                return;
            }

            btn.onClick.RemoveAllListeners();
            if (isBattle)
                btn.onClick.AddListener(LaunchBattle);
            else
                btn.onClick.AddListener(() => scene.Fade.To(targetScene));
        }

        /// <summary>Pre-flight + fade to the Game scene. Refuses to launch combat with an
        /// empty party (would lose instantly) — bounces to Party scene instead. Sets the
        /// post-battle return target so the player lands back in the same vendor afterwards.</summary>
        private static void LaunchBattle()
        {
            var party = ProfileHelper.CurrentProfile?.CurrentSave?.Party?.Members;
            if (party == null || party.Count == 0)
            {
                Debug.LogWarning("[VendorNavBar] Battle aborted — empty party. Routing to Party scene.");
                scene.Fade.ToParty();
                return;
            }
            // Persist current state before leaving — vendor managers each persist on their
            // own actions, but the bar itself doesn't know which manager is active.
            ProfileHelper.Save(overwrite: true);
            // Return the player to whichever vendor scene they launched from after the post-battle
            // screen finishes its XP awards.
            ExperienceTracker.NextSceneAfterPostBattleScreen = SceneManager.GetActiveScene().name;
            scene.Fade.ToGame();
        }
    }
}
