using UnityEngine;
using UnityEngine.UI;
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

namespace Scripts.Hub
{
    /// <summary>
    /// HUBMANAGER - Central vendor launcher.
    ///
    /// <para>PURPOSE: Backs the Hub.unity scene (US-112) — a simple 6-button grid that routes the
    /// player to each vendor scene (Vendor, Blacksmith, Alchemist, Equip, Party, Abilities).
    /// No shop logic lives here; each vendor scene owns its own inventory flow.</para>
    ///
    /// <para>LIFECYCLE: Wires every navigation button at runtime in Awake (persistent onClick
    /// listeners can't target lambdas or plain delegates — see SceneBuilderHelper.WireOnClick),
    /// then fades the scene in on Start like every other vendor manager.</para>
    ///
    /// <para>RELATED FILES: HubBuilder.cs, SceneHelper.cs, HubTheme.cs</para>
    /// </summary>
    public class HubManager : MonoBehaviour
    {
        public const string ButtonGridName = "ButtonGrid";
        public const string BackButtonName = "BackButton";

        private void Awake()
        {
            WireButtons();
        }

        private void Start()
        {
            scene.FadeIn();
        }

        private void WireButtons()
        {
            var canvas = GameObject.Find("Canvas");
            if (canvas == null) { Debug.LogError("[HubManager] Canvas not found."); return; }

            Wire(canvas.transform, $"{ButtonGridName}/Vendor", scene.Fade.ToVendor);
            Wire(canvas.transform, $"{ButtonGridName}/Blacksmith", scene.Fade.ToBlacksmith);
            Wire(canvas.transform, $"{ButtonGridName}/Alchemist", scene.Fade.ToAlchemist);
            Wire(canvas.transform, $"{ButtonGridName}/Equip", scene.Fade.ToEquip);
            Wire(canvas.transform, $"{ButtonGridName}/Party", scene.Fade.ToParty);
            Wire(canvas.transform, $"{ButtonGridName}/Abilities", scene.Fade.ToAbilities);
            Wire(canvas.transform, BackButtonName, scene.Fade.ToStageSelect);
        }

        private static void Wire(Transform canvas, string path, UnityEngine.Events.UnityAction onClick)
        {
            var button = canvas.Find(path)?.GetComponent<Button>();
            if (button == null)
            {
                Debug.LogError($"[HubManager] Button not found at '{path}'.");
                return;
            }
            button.onClick.RemoveAllListeners();
            button.onClick.AddListener(onClick);
        }
    }
}
