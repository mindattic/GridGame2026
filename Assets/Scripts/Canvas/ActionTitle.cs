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

        private void Awake()
        {
            if (canvasGroup == null) canvasGroup = GetComponent<CanvasGroup>();
            BuildUI();
            Hide();
        }

        /// <summary>
        /// Self-building Lego brick: anchors itself as a top-center band (responsive to any aspect
        /// ratio via anchor fractions) and constructs its own navy panel + centered label, instead
        /// of relying on a layout baked into Game.unity. Drop this component on a bare GameObject
        /// under the Canvas and it builds itself.
        /// </summary>
        private void BuildUI()
        {
            int uiLayer = LayerMask.NameToLayer("UI");
            gameObject.layer = uiLayer;

            // Anchor a top-center band: 56% of width, fixed height, inset from the top edge.
            var rt = GetComponent<RectTransform>();
            if (rt == null) rt = gameObject.AddComponent<RectTransform>();
            rt.anchorMin = new Vector2(0.22f, 1f);
            rt.anchorMax = new Vector2(0.78f, 1f);
            rt.pivot = new Vector2(0.5f, 1f);
            rt.offsetMin = new Vector2(0f, -200f);  // band height ~120 below the top inset
            rt.offsetMax = new Vector2(0f, -80f);

            // Remove any pre-existing baked children so the code-built look is authoritative.
            for (int i = rt.childCount - 1; i >= 0; i--)
                Destroy(rt.GetChild(i).gameObject);

            // Navy panel background on this object.
            var panel = GetComponent<UnityEngine.UI.Image>();
            if (panel == null) panel = gameObject.AddComponent<UnityEngine.UI.Image>();
            panel.color = new Color(0.07f, 0.10f, 0.22f, 0.92f);
            panel.raycastTarget = false;

            // Centered label child stretched to fill the band.
            var labelGo = new GameObject("Label");
            labelGo.layer = uiLayer;
            var labelRT = labelGo.AddComponent<RectTransform>();
            labelRT.SetParent(rt, false);
            labelRT.anchorMin = Vector2.zero;
            labelRT.anchorMax = Vector2.one;
            labelRT.offsetMin = new Vector2(16f, 8f);
            labelRT.offsetMax = new Vector2(-16f, -8f);
            labelRT.pivot = new Vector2(0.5f, 0.5f);

            label = labelGo.AddComponent<TextMeshProUGUI>();
            label.text = string.Empty;
            label.fontSize = 44;
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
