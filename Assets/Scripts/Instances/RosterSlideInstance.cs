using Scripts.Helpers;
using UnityEngine;
using UnityEngine.UI;
using Scripts.Canvas;
using Scripts.Data.Actor;
using Scripts.Data.Items;
using Scripts.Data.Skills;
using Scripts.Effects;
using Scripts.Factories;
using Scripts.Hub;
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

namespace Scripts.Instances
{
/// <summary>
/// ROSTERSLIDEINSTANCE - Hero carousel slide component.
/// 
/// PURPOSE:
/// Represents a single hero slide in the party roster carousel,
/// displaying the hero sprite and handling selection.
/// 
/// VISUAL APPEARANCE:
/// ```
/// ┌─────────────────┐
/// │                 │
/// │  [Hero Sprite]  │
/// │                 │
/// │       ✓         │ ← Checkmark if in party
/// └─────────────────┘
/// ```
/// 
/// COMPONENTS:
/// - image: Hero sprite display
/// - imageButton: Full-slide tap detection
/// - centerButton: Center region tap for selection
/// - checkmark: Party membership indicator
/// 
/// INITIALIZATION:
/// Configured via Initialize() with character class, sprite,
/// dimensions, click handler, and party state.
/// 
/// RELATED FILES:
/// - RosterSlideFactory.cs: Creates slides
/// - PartyManager.cs: Manages carousel
/// - ActorLibrary.cs: Hero sprite data
/// </summary>
public class RosterSlideInstance : MonoBehaviour
{
    [HideInInspector] public CharacterClass CharacterClass;
    [HideInInspector] public float Width;
    [HideInInspector] public float Height;

    [HideInInspector] public RectTransform rectTransform;
    private Image image;
    private Button imageButton;

    private RectTransform centerButtonRect; // New center button
    private Button centerButton; // New center button

    private RectTransform checkmark;
  


    private float alphaThreshold;
    private float centerButtonWidth;


    /// <summary>Initializes component references and state.</summary>
    public void Awake()
    {
        rectTransform = GetComponent<RectTransform>();
        image = rectTransform.GetComponent<Image>();
        imageButton = rectTransform.GetComponent<Button>();
        centerButton = rectTransform.Find("CenterButton").GetComponent<Button>();
        checkmark = rectTransform.Find("Checkmark").GetComponent<RectTransform>();

        rectTransform.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, Width);
        rectTransform.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, Height);


        centerButtonRect = centerButton.GetComponent<RectTransform>();
        centerButtonRect.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, Width);
        centerButtonRect.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, Height);

        centerButtonRect.sizeDelta = new Vector2(centerButtonWidth, Height); // 1/3th width, full height
        centerButtonRect.anchoredPosition = Vector2.zero; // Center it on the image

        alphaThreshold = 0.1f;
        centerButtonWidth = Width * 0.33f;
    }

    /// <summary>Initializes initialize.</summary>
    public void Initialize(CharacterClass characterClass, Sprite sprite, float width, float height, System.Action onClick, bool isInParty)
    {
        // Show key and dimensions
        CharacterClass = characterClass;
        Width = width;
        Height = height;

        // Show the image sprite
        image.alphaHitTestMinimumThreshold = alphaThreshold;
        image.sprite = sprite;

        // Show the onClick event to the image button
        imageButton.onClick.AddListener(() => onClick?.Invoke());

        // Show the onClick event to the center button
        centerButton.onClick.AddListener(() => onClick?.Invoke());

        // Configure the checkmark
        checkmark.sizeDelta = new Vector2(Height / 10, Height / 10);
        SetCheckmark(isInParty);
    }

    /// <summary>Sets the checkmark.</summary>
    public void SetCheckmark(bool isInParty)
    {
        checkmark.gameObject.SetActive(isInParty);
    }
}
}
