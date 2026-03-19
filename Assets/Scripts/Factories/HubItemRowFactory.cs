using TMPro;
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
/// HUBITEMROWFACTORY - Creates standardized hub list rows.
/// 
/// PURPOSE:
/// Creates a reusable row element for hub item lists, recipe
/// lists, hero selection, and training lists. Provides a
/// consistent look-and-feel across all hub screens.
/// 
/// CREATED HIERARCHY:
/// ```
/// HubItemRow (root)
/// ├── Image (row background, dark semi-transparent)
/// ├── Button (click handler)
/// ├── LayoutElement (scroll view sizing)
/// └── Label (TMP - row text)
/// ```
/// 
/// RELATED FILES:
/// - ShopSectionController.cs: Buy/sell rows
/// - BlacksmithSectionController.cs: Recipe rows
/// - TrainingSectionController.cs: Training rows
/// - PartySectionController.cs: Hero rows
/// </summary>
public static class HubItemRowFactory
{
    private static readonly Color RowBackgroundColor = new Color(0.12f, 0.12f, 0.16f, 0.85f);
    private static readonly Color SelectedRowColor = new Color(0.20f, 0.22f, 0.30f, 0.92f);

    private static readonly ColorBlock RowButtonColors = new ColorBlock
    {
        normalColor = Color.white,
        highlightedColor = new Color(0.85f, 0.85f, 0.95f, 1f),
        pressedColor = new Color(0.65f, 0.65f, 0.75f, 1f),
        selectedColor = new Color(0.85f, 0.85f, 0.95f, 1f),
        disabledColor = new Color(0.5f, 0.5f, 0.5f, 0.6f),
        colorMultiplier = 1f,
        fadeDuration = 0.1f
    };

    /// <summary>Returns a color based on item rarity for label tinting.</summary>
    public static Color RarityColor(Scripts.Data.Items.ItemRarity rarity)
    {
        switch (rarity)
        {
            case Scripts.Data.Items.ItemRarity.Uncommon: return new Color(0.30f, 0.85f, 0.30f, 1f);
            case Scripts.Data.Items.ItemRarity.Rare: return new Color(0.30f, 0.55f, 1.00f, 1f);
            case Scripts.Data.Items.ItemRarity.Epic: return new Color(0.70f, 0.30f, 0.90f, 1f);
            case Scripts.Data.Items.ItemRarity.Legendary: return new Color(1.00f, 0.65f, 0.00f, 1f);
            default: return Color.white;
        }
    }

    private const float IconSize = 56f;
    private const float IconPadding = 8f;
    private const float LabelLeftOffset = IconSize + IconPadding + 12f;

    /// <summary>Creates a new hub list row.</summary>
    public static GameObject Create(Transform parent = null)
    {
        // === ROOT ===
        var root = new GameObject("HubItemRow");
        root.layer = LayerMask.NameToLayer("UI");

        var rootRT = root.AddComponent<RectTransform>();
        rootRT.anchorMin = new Vector2(0f, 0.5f);
        rootRT.anchorMax = new Vector2(1f, 0.5f);
        rootRT.anchoredPosition = Vector2.zero;
        rootRT.sizeDelta = new Vector2(0f, 72f);
        rootRT.pivot = new Vector2(0.5f, 0.5f);

        root.AddComponent<CanvasRenderer>();

        // Image
        var image = root.AddComponent<Image>();
        image.color = RowBackgroundColor;
        image.raycastTarget = true;
        image.type = Image.Type.Simple;

        // Button
        var button = root.AddComponent<Button>();
        button.interactable = true;
        button.targetGraphic = image;
        button.transition = Selectable.Transition.ColorTint;
        button.colors = RowButtonColors;
        button.navigation = new Navigation { mode = Navigation.Mode.None };

        // LayoutElement
        var layout = root.AddComponent<LayoutElement>();
        layout.preferredHeight = 72f;
        layout.flexibleWidth = 1f;

        // === ICON (left-aligned square) ===
        var icon = new GameObject("Icon");
        icon.layer = LayerMask.NameToLayer("UI");

        var iconRT = icon.AddComponent<RectTransform>();
        iconRT.SetParent(rootRT, false);
        iconRT.anchorMin = new Vector2(0f, 0.5f);
        iconRT.anchorMax = new Vector2(0f, 0.5f);
        iconRT.pivot = new Vector2(0f, 0.5f);
        iconRT.anchoredPosition = new Vector2(IconPadding, 0f);
        iconRT.sizeDelta = new Vector2(IconSize, IconSize);

        icon.AddComponent<CanvasRenderer>();
        var iconImg = icon.AddComponent<Image>();
        iconImg.raycastTarget = false;
        iconImg.preserveAspect = true;
        iconImg.color = Color.white;
        // Default: no sprite (hidden until SetIcon is called)
        iconImg.enabled = false;

        // === LABEL (offset right of icon) ===
        var label = new GameObject("Label");
        label.layer = LayerMask.NameToLayer("UI");

        var labelRT = label.AddComponent<RectTransform>();
        labelRT.SetParent(rootRT, false);
        labelRT.anchorMin = Vector2.zero;
        labelRT.anchorMax = new Vector2(1f, 1f);
        labelRT.offsetMin = new Vector2(LabelLeftOffset, 20f);
        labelRT.offsetMax = new Vector2(-16f, -4f);
        labelRT.pivot = new Vector2(0.5f, 0.5f);

        label.AddComponent<CanvasRenderer>();

        var tmp = label.AddComponent<TextMeshProUGUI>();
        tmp.text = "Item";
        tmp.fontSize = 26;
        tmp.color = Color.white;
        tmp.alignment = TextAlignmentOptions.MidlineLeft;
        tmp.enableWordWrapping = false;
        tmp.overflowMode = TextOverflowModes.Ellipsis;
        tmp.richText = true;
        tmp.raycastTarget = false;

        // === SUB-LABEL (detail line beneath main text) ===
        var sub = new GameObject("SubLabel");
        sub.layer = LayerMask.NameToLayer("UI");

        var subRT = sub.AddComponent<RectTransform>();
        subRT.SetParent(rootRT, false);
        subRT.anchorMin = Vector2.zero;
        subRT.anchorMax = new Vector2(1f, 0f);
        subRT.offsetMin = new Vector2(LabelLeftOffset, 2f);
        subRT.offsetMax = new Vector2(-16f, 22f);
        subRT.pivot = new Vector2(0f, 0f);

        sub.AddComponent<CanvasRenderer>();

        var subTmp = sub.AddComponent<TextMeshProUGUI>();
        subTmp.text = "";
        subTmp.fontSize = 18;
        subTmp.color = new Color(0.65f, 0.65f, 0.70f, 1f);
        subTmp.alignment = TextAlignmentOptions.BottomLeft;
        subTmp.enableWordWrapping = false;
        subTmp.overflowMode = TextOverflowModes.Ellipsis;
        subTmp.richText = true;
        subTmp.raycastTarget = false;

        // Parent
        if (parent != null) rootRT.SetParent(parent, false);

        return root;
    }

    /// <summary>Sets the main label text.</summary>
    public static void SetLabel(GameObject row, string text)
    {
        if (row == null) return;
        var label = row.transform.Find("Label")?.GetComponent<TextMeshProUGUI>();
        if (label != null) label.text = text;
    }

    /// <summary>Sets the sub-label text.</summary>
    public static void SetSubLabel(GameObject row, string text)
    {
        if (row == null) return;
        var sub = row.transform.Find("SubLabel")?.GetComponent<TextMeshProUGUI>();
        if (sub != null) sub.text = text;
    }

    /// <summary>Sets the main label color (e.g. rarity tint).</summary>
    public static void SetLabelColor(GameObject row, Color color)
    {
        if (row == null) return;
        var label = row.transform.Find("Label")?.GetComponent<TextMeshProUGUI>();
        if (label != null) label.color = color;
    }

    /// <summary>Sets the background to the selected highlight color and shows a ▶ arrow cursor.</summary>
    public static void SetSelected(GameObject row, bool selected)
    {
        if (row == null) return;
        var img = row.GetComponent<Image>();
        if (img != null) img.color = selected ? SelectedRowColor : RowBackgroundColor;

        // Show/hide selection arrow indicator
        var arrowTr = row.transform.Find("SelectArrow");
        if (selected && arrowTr == null)
        {
            var arrow = new GameObject("SelectArrow");
            arrow.layer = LayerMask.NameToLayer("UI");
            var arrowRT = arrow.AddComponent<RectTransform>();
            arrowRT.SetParent(row.transform, false);
            arrowRT.anchorMin = new Vector2(0f, 0.5f);
            arrowRT.anchorMax = new Vector2(0f, 0.5f);
            arrowRT.pivot = new Vector2(0f, 0.5f);
            arrowRT.anchoredPosition = new Vector2(2f, 0f);
            arrowRT.sizeDelta = new Vector2(20f, 24f);
            arrow.AddComponent<CanvasRenderer>();
            var arrowTmp = arrow.AddComponent<TextMeshProUGUI>();
            arrowTmp.text = "\u25B6";
            arrowTmp.fontSize = 18;
            arrowTmp.color = new Color(1f, 0.85f, 0.35f, 1f);
            arrowTmp.alignment = TextAlignmentOptions.Center;
            arrowTmp.raycastTarget = false;
        }
        else if (!selected && arrowTr != null)
        {
            Object.Destroy(arrowTr.gameObject);
        }
    }

    /// <summary>Sets the icon sprite from an ItemDefinition (uses PlaceholderIconFactory).</summary>
    public static void SetIcon(GameObject row, Scripts.Data.Items.ItemDefinition item)
    {
        if (row == null) return;
        var iconImg = row.transform.Find("Icon")?.GetComponent<Image>();
        if (iconImg == null) return;

        var sprite = PlaceholderIconFactory.GetIcon(item);
        if (sprite != null)
        {
            iconImg.sprite = sprite;
            iconImg.enabled = true;
        }
    }

    /// <summary>Sets the icon to a direct sprite (for hero portraits, etc.).</summary>
    public static void SetIconSprite(GameObject row, Sprite sprite)
    {
        if (row == null) return;
        var iconImg = row.transform.Find("Icon")?.GetComponent<Image>();
        if (iconImg == null) return;

        if (sprite != null)
        {
            iconImg.sprite = sprite;
            iconImg.enabled = true;
        }
        else
        {
            iconImg.enabled = false;
        }
    }

    /// <summary>Sets the icon to a solid color placeholder (for non-item rows like heroes).</summary>
    public static void SetIconColor(GameObject row, Color color)
    {
        if (row == null) return;
        var iconImg = row.transform.Find("Icon")?.GetComponent<Image>();
        if (iconImg == null) return;

        iconImg.sprite = PlaceholderIconFactory.GetFallback();
        iconImg.color = color;
        iconImg.enabled = true;
    }
}

}
