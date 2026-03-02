using UnityEngine;
using UnityEngine.UI;
using Scripts.Canvas;
using Scripts.Data.Actor;
using Scripts.Data.Items;
using Scripts.Data.Skills;
using Scripts.Effects;
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

namespace Scripts.Factories
{
    /// <summary>
    /// ROSTERSLIDEFACTORY - Creates hero roster slide GameObjects.
    /// 
    /// PURPOSE:
    /// Creates individual hero slides for the party selection carousel.
    /// Each slide shows a hero portrait with selection state.
    /// 
    /// VISUAL APPEARANCE:
    /// ```
    /// ???????????????????
    /// ?                 ?
    /// ?   [Portrait]    ?
    /// ?                 ?
    /// ?       ?         ? ? Checkmark if in party
    /// ???????????????????
    /// ```
    /// 
    /// CREATED HIERARCHY:
    /// ```
    /// RosterSlide (root)
    /// ??? Image (portrait background)
    /// ??? Button (selection)
    /// ??? RosterSlideInstance (behavior)
    /// ??? CenterButton (invisible click target)
    /// ??? Checkmark (party membership indicator)
    /// ```
    /// 
    /// CONFIGURATION:
    /// - Default size: 100x100 (resized by manager)
    /// - ColorTint transition for button states
    /// - Checkmark visible when hero in party
    /// 
    /// CALLED BY:
    /// - PartyManager.BuildSlides()
    /// 
    /// RELATED FILES:
    /// - RosterSlideInstance.cs: Slide behavior
    /// - PartyManager.cs: Carousel management
    /// - ActorLibrary.cs: Hero portraits
    /// </summary>
    public static class RosterSlideFactory
    {
        private static readonly ColorBlock DefaultButtonColors = new ColorBlock
        {
            normalColor = Color.white,
            highlightedColor = new Color(0.9607843f, 0.9607843f, 0.9607843f, 1f),
            pressedColor = new Color(0.78431374f, 0.78431374f, 0.78431374f, 1f),
            selectedColor = new Color(0.9607843f, 0.9607843f, 0.9607843f, 1f),
            disabledColor = new Color(0.78431374f, 0.78431374f, 0.78431374f, 0.5019608f),
            colorMultiplier = 1f,
            fadeDuration = 0.1f
        };

        /// <summary>Creates a new roster slide for a hero.</summary>
        public static GameObject Create(Transform parent = null)
        {
            // === ROOT: RosterSlide ===
            var root = new GameObject("RosterSlide");
            root.layer = LayerMask.NameToLayer("Default");

            var rootRT = root.AddComponent<RectTransform>();
            rootRT.anchorMin = new Vector2(0.5f, 0.5f);
            rootRT.anchorMax = new Vector2(0.5f, 0.5f);
            rootRT.anchoredPosition = Vector2.zero;
            rootRT.sizeDelta = new Vector2(100f, 100f);
            rootRT.pivot = new Vector2(0.5f, 0.5f);

            root.AddComponent<CanvasRenderer>();

            // Image (portrait)
            var rootImage = root.AddComponent<Image>();
            rootImage.color = Color.white;
            rootImage.raycastTarget = true;
            rootImage.maskable = true;
            rootImage.type = Image.Type.Simple;

            // Button
            var rootButton = root.AddComponent<Button>();
            rootButton.interactable = true;
            rootButton.targetGraphic = rootImage;
            rootButton.transition = Selectable.Transition.ColorTint;
            rootButton.colors = DefaultButtonColors;
            rootButton.navigation = new Navigation { mode = Navigation.Mode.Automatic };

            // RosterSlideInstance
            var rosterSlide = root.AddComponent<RosterSlideInstance>();
            rosterSlide.Width = 1000f;
            rosterSlide.Height = 1000f;

            // === CHILD: CenterButton ===
            var centerButton = new GameObject("CenterButton");
            centerButton.layer = LayerMask.NameToLayer("Default");

            var centerButtonRT = centerButton.AddComponent<RectTransform>();
            centerButtonRT.SetParent(rootRT, false);
            centerButtonRT.anchorMin = new Vector2(0.5f, 0.5f);
            centerButtonRT.anchorMax = new Vector2(0.5f, 0.5f);
            centerButtonRT.anchoredPosition = Vector2.zero;
            centerButtonRT.sizeDelta = new Vector2(160f, 30f);
            centerButtonRT.pivot = new Vector2(0.5f, 0.5f);

            centerButton.AddComponent<CanvasRenderer>();

            // Image (disabled by default)
            var centerImage = centerButton.AddComponent<Image>();
            centerImage.enabled = false;
            centerImage.color = Color.white;
            centerImage.raycastTarget = true;
            centerImage.maskable = true;
            centerImage.type = Image.Type.Sliced;

            // Button
            var centerBtn = centerButton.AddComponent<Button>();
            centerBtn.interactable = true;
            centerBtn.targetGraphic = centerImage;
            centerBtn.transition = Selectable.Transition.ColorTint;
            centerBtn.colors = DefaultButtonColors;
            centerBtn.navigation = new Navigation { mode = Navigation.Mode.Automatic };

            // === CHILD: Checkmark ===
            var checkmark = new GameObject("Checkmark");
            checkmark.layer = LayerMask.NameToLayer("Default");

            var checkmarkRT = checkmark.AddComponent<RectTransform>();
            checkmarkRT.SetParent(rootRT, false);
            checkmarkRT.anchorMin = new Vector2(0.5f, 1f);
            checkmarkRT.anchorMax = new Vector2(0.5f, 1f);
            checkmarkRT.anchoredPosition = Vector2.zero;
            checkmarkRT.sizeDelta = new Vector2(10f, 10f);
            checkmarkRT.pivot = new Vector2(0.5f, 0.5f);

            checkmark.AddComponent<CanvasRenderer>();

            var checkmarkImage = checkmark.AddComponent<Image>();
            checkmarkImage.color = Color.white;
            checkmarkImage.raycastTarget = true;
            checkmarkImage.maskable = true;
            checkmarkImage.type = Image.Type.Simple;

            // Parent if specified
            if (parent != null)
            {
                rootRT.SetParent(parent, false);
            }

            return root;
        }
    }
}
