using System.Collections;
using TMPro;
using UnityEngine;
using Scripts.Data.Actor;
using Scripts.Data.Config;
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

namespace Scripts.Canvas
{
    /// <summary>
    /// ACTIONTITLE - Top-center banner that announces the current action.
    /// <para>PURPOSE: FF6-style action announcement. Shows a transient label like "Casting Flames",
    /// "Using Cure Potion", or "Equipping Thunder Sword" centered at the top of the Game scene.
    /// Fades after a beat so the next action's title can take its place. Fires for hero AND
    /// enemy actions — players need to know what the enemy is doing too.</para>
    /// <para>VISUAL APPEARANCE:</para>
    /// <para><code>
    /// ┌───────────────────────────────┐
    /// │       Casting Flames          │  ← navy panel, white text, top-center
    /// └───────────────────────────────┘
    ///               ↓
    ///         fades after a beat
    /// </code></para>
    /// <para>USAGE:</para>
    /// <para><c>g.ActionTitle.Cast(ability)</c> → "Casting Flames"</para>
    /// <para><c>g.ActionTitle.Use(item)</c> → "Using Cure Potion"</para>
    /// <para><c>g.ActionTitle.Equip(weapon)</c> → "Equipping Thunder Sword"</para>
    /// <para><c>g.ActionTitle.Show(rawText)</c> → free-form passthrough</para>
    /// <para>NOT THE ABILITY BAR. The bottom 5-slot UI that holds abilities/items/weapons is a
    /// different concept (see AbilityBarSlot* in save data). This component is a top transient
    /// title strip, not an interactive bar.</para>
    /// <para>RELATED FILES: AbilityManager.cs, UseItemSequence.cs, EnemyAttackSequence.cs,
    /// ChangeEquippedWeaponSequence.cs (planned), GameBuilder.cs (placement), HubTheme.cs (palette)</para>
    /// <para>ACCESS: <c>g.ActionTitle</c></para>
    /// </summary>
    [DisallowMultipleComponent]
    [RequireComponent(typeof(CanvasGroup))]
    public class ActionTitle : MonoBehaviour
    {
        public const string GameObjectName = "ActionTitle";

        private TextMeshProUGUI label;
        private CanvasGroup canvasGroup;
        private Coroutine hideCoroutine;
        private WorldSpaceUiPanel panel;

        private void Awake()
        {
            BuildWorldSpaceUI();
            Hide();
        }

        /// <summary>
        /// First consumer of the world-space UI rig: builds the action banner on a
        /// WorldSpaceUiPanel (UI sorting layer, sized off the visible world rect, placed in the
        /// top negative-space band) instead of the ScreenSpaceOverlay canvas — so VFX/portraits
        /// can render in front of it and it lives in the same coordinate system as the board.
        /// Show/Hide/FadeOut drive the panel's CanvasGroup, so they work unchanged.
        /// </summary>
        private void BuildWorldSpaceUI()
        {
            int uiLayer = LayerMask.NameToLayer("UI");

            // This component's own GameObject is still the scene-baked overlay node; clear any
            // leftover baked children + background so the only visual is the world-space panel.
            var selfRt = transform as RectTransform;
            if (selfRt != null)
                for (int i = selfRt.childCount - 1; i >= 0; i--)
                    Destroy(selfRt.GetChild(i).gameObject);
            var selfImage = GetComponent<UnityEngine.UI.Image>();
            if (selfImage != null) selfImage.enabled = false;

            var vr = UnitConversionHelper.World.VisibleRect();

            // Transient top-center banner sized in world units (not screen pixels).
            panel = WorldSpaceUiPanel.Create("ActionTitleWS", vr.width * 0.6f, vr.height * 0.08f, sortingOrder: 30);
            panel.PlaceInTopBand();

            var root = panel.Content;
            canvasGroup = root.GetComponent<CanvasGroup>();
            if (canvasGroup == null) canvasGroup = root.gameObject.AddComponent<CanvasGroup>();

            // Navy panel background filling the panel.
            var bg = root.gameObject.AddComponent<UnityEngine.UI.Image>();
            bg.color = new Color(0.07f, 0.10f, 0.22f, 0.92f);
            bg.raycastTarget = false;

            // Centered label, authored in the panel's reference-pixel space (~1000 wide).
            var labelGo = new GameObject("Label");
            labelGo.layer = uiLayer;
            var labelRT = labelGo.AddComponent<RectTransform>();
            labelRT.SetParent(root, false);
            labelRT.anchorMin = Vector2.zero;
            labelRT.anchorMax = Vector2.one;
            labelRT.offsetMin = new Vector2(20f, 12f);
            labelRT.offsetMax = new Vector2(-20f, -12f);
            labelRT.pivot = new Vector2(0.5f, 0.5f);

            label = labelGo.AddComponent<TextMeshProUGUI>();
            label.text = string.Empty;
            label.fontSize = 140;
            label.fontStyle = FontStyles.Bold;
            label.color = Color.white;
            label.alignment = TextAlignmentOptions.Center;
            label.enableWordWrapping = false;
            label.overflowMode = TextOverflowModes.Overflow;
            label.raycastTarget = false;
        }

        // ---------- Verb-dispatched API (FF6-style, no actor prefix) ----------

        /// <summary>"Casting {Ability.name}". Used for spells with a cast bar.</summary>
        public void Cast(Ability ability)
        {
            if (ability == null) return;
            Show($"Casting {ability.name}");
        }

        /// <summary>"Using {Item.DisplayName}". Used for consumables / item-sourced abilities.</summary>
        public void Use(ItemDefinition item)
        {
            if (item == null) return;
            Show($"Using {item.DisplayName}");
        }

        /// <summary>"Equipping {Weapon.DisplayName}". Used by ChangeEquippedWeaponSequence
        /// when a bar-slot weapon swaps into the wielder's equipped slot.</summary>
        public void Equip(ItemDefinition weapon)
        {
            if (weapon == null) return;
            Show($"Equipping {weapon.DisplayName}");
        }

        /// <summary>Generic passthrough — used for enemy attacks ("{EnemyName} attacks!") and
        /// any case where the verb-dispatched overloads don't apply.</summary>
        public void Show(string text)
        {
            if (string.IsNullOrEmpty(text)) return;
            CancelHide();
            label.text = text;
            canvasGroup.alpha = 1f;
            gameObject.SetActive(true);
            hideCoroutine = StartCoroutine(AutoHideRoutine());
        }

        public void Hide()
        {
            CancelHide();
            canvasGroup.alpha = 0f;
        }

        public void FadeOut()
        {
            CancelHide();
            hideCoroutine = StartCoroutine(FadeOutRoutine());
        }

        private void CancelHide()
        {
            if (hideCoroutine != null)
            {
                StopCoroutine(hideCoroutine);
                hideCoroutine = null;
            }
        }

        private IEnumerator AutoHideRoutine()
        {
            yield return new WaitForSeconds(ActionTitleConfig.DisplayDuration);
            yield return FadeOutRoutine();
        }

        private IEnumerator FadeOutRoutine()
        {
            float startAlpha = canvasGroup.alpha;
            float elapsed = 0f;
            while (elapsed < ActionTitleConfig.FadeDuration)
            {
                elapsed += Time.deltaTime;
                canvasGroup.alpha = Mathf.Lerp(startAlpha, 0f, elapsed / ActionTitleConfig.FadeDuration);
                yield return null;
            }
            canvasGroup.alpha = 0f;
            hideCoroutine = null;
        }
    }
}
