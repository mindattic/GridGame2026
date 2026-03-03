using Scripts.Helpers;
using Scripts.Helpers;
using Scripts.Factories;
using Scripts.Libraries;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Button = UnityEngine.UI.Button;
using scene = Scripts.Helpers.SceneHelper;
using Scripts.Canvas;
using Scripts.Data.Actor;
using Scripts.Data.Items;
using Scripts.Data.Skills;
using Scripts.Effects;
using Scripts.Hub;
using Scripts.Instances;
using Scripts.Instances.Actor;
using Scripts.Instances.Board;
using Scripts.Instances.SynergyLine;
using Scripts.Inventory;
using Scripts.Models;
using Scripts.Models.Actor;
using Scripts.Overworld;
using Scripts.Sequences;
using Scripts.Serialization;
using Scripts.Utilities;

namespace Scripts.Managers
{
/// <summary>
/// PARTYMANAGER - Manages the party roster/selection screen.
/// 
/// PURPOSE:
/// Displays available heroes and allows the player to add/remove
/// them from their active party for battle.
/// 
/// VISUAL LAYOUT:
/// ```
/// ┌─────────────────────────────────────┐
/// │            Party Select             │
/// ├─────────────────────────────────────┤
/// │                                     │
/// │  ◄ [Hero1] [Hero2] [Hero3] [Hero4] ►│ ← Horizontal scroll
/// │                                     │
/// │  ┌─────────────────────────────┐   │
/// │  │ Paladin - Level 5           │   │ ← Selected hero stats
/// │  │ HP: 120  STR: 45  VIT: 30   │   │
/// │  │ AGI: 25  SPD: 20  STA: 35   │   │
/// │  └─────────────────────────────┘   │
/// │                                     │
/// │  [ Add to Party ] (2/4 members)    │
/// └─────────────────────────────────────┘
/// ```
/// 
/// ROSTER CAROUSEL:
/// - Horizontal scrolling with momentum
/// - Touch/drag to scroll through heroes
/// - Click to select hero for details
/// 
/// PARTY OPERATIONS:
/// - Add hero to party (up to max)
/// - Remove hero from party
/// - View hero stats
/// 
/// RELATED FILES:
/// - RosterSlideFactory.cs: Creates hero slides
/// - RosterSlideInstance.cs: Slide behavior
/// - ProfileHelper.cs: Party data persistence
/// - ActorLibrary.cs: Hero data
/// 
/// ACCESS: Scene-based manager (Party scene)
/// </summary>
public class PartyManager : MonoBehaviour
{
    #region UI References

    private RectTransform title;
    private RectTransform rosterPanel;

    #endregion

    #region Scroll Settings

    private float spacing = 0f;
    private float deceleration = 1250;
    private float maxFocus = 3000f;
    private float dragThreshold = 15f;
    private float wrapThresholdMultiplier = 1.5f;

    #endregion

    #region Runtime State

    private Dictionary<CharacterClass, RosterSlideInstance> slides = new Dictionary<CharacterClass, RosterSlideInstance>();

    private Vector2 touchStart;
    private bool dragging = false;
    private float velocity = 0f;
    private float targetOffset = 0f;
    private bool scrollingToCenter = false;
    private bool clickAllowed = true;

    #endregion

    #region Stats Display

    private RectTransform addRemovePartyMemberButton;
    private TextMeshProUGUI addRemovePartyMemberLabel;
    private TextMeshProUGUI partyMemberCountLabel;

    private Dictionary<RectTransform, Coroutine> barAnimations = new();
    private RectTransform levelRow;
    private RectTransform hpRow;
    private RectTransform strRow;
    private RectTransform vitRow;
    private RectTransform agiRow;
    private RectTransform spdRow;
    private RectTransform staRow;
    private RectTransform intRow;
    private RectTransform wisRow;
    private RectTransform lckRow;
    private float centeredX;

    #endregion

    #region Party Helpers

    /// <summary>Checks if a hero is in the current party.</summary>
    private bool IsInParty(CharacterClass characterClass)
    {
        return ProfileHelper.CurrentProfile.CurrentSave.Party.Members.Any(x => x.CharacterClass == characterClass);
    }

    /// <summary>Current party member count.</summary>
    private int partyMemberCount => ProfileHelper.CurrentProfile.CurrentSave.Party.Members.Count;

    #endregion
    /// <summary>Initializes component references and state.</summary>
    private void Awake()
    {
        if (!ProfileHelper.HasProfiles())
            return;

        //Validate a current profile exists
        if (!ProfileHelper.HasCurrentProfile)
        {
            Debug.LogError("No current profile selected.");
            scene.Fade.ToProfileCreate();
            return;
        }

        //Validate a current save exists
        if (!ProfileHelper.HasCurrentSave)
        {
            Debug.LogError("No current save selected.");
            scene.Fade.ToSaveFileSelect();
            return;
        }

        title = GameObject.Find(GameObjectHelper.PartyManager.Title).GetComponent<RectTransform>();
        rosterPanel = GameObject.Find(GameObjectHelper.PartyManager.RosterPanel).GetComponent<RectTransform>();
        addRemovePartyMemberButton = GameObject.Find(GameObjectHelper.PartyManager.AddRemovePartyMemberButton).GetComponent<RectTransform>();
        addRemovePartyMemberLabel = GameObject.Find(GameObjectHelper.PartyManager.AddRemovePartyMemberButtonLabel).GetComponent<TextMeshProUGUI>();
        partyMemberCountLabel = GameObject.Find(GameObjectHelper.PartyManager.PartyMemberCountLabel).GetComponent<TextMeshProUGUI>();
        //statsDisplay = GameObject.Find(GameObjectHelper.PartyManager.StatsDisplay).GetComponent<StatsDisplay>();


        var statsDisplay = GameObject.Find(GameObjectHelper.PartyManager.StatsDisplay).GetComponent<RectTransform>();
        var panel = statsDisplay.transform.GetChild("Panel").GetComponent<RectTransform>();
        levelRow = panel.transform.GetChild("LVL").GetComponent<RectTransform>();
        hpRow = panel.transform.GetChild("HP").GetComponent<RectTransform>();
        strRow = panel.transform.GetChild("STR").GetComponent<RectTransform>();
        vitRow = panel.transform.GetChild("VIT").GetComponent<RectTransform>();
        agiRow = panel.transform.GetChild("AGI").GetComponent<RectTransform>();
        spdRow = panel.transform.GetChild("SPD").GetComponent<RectTransform>();
        staRow = panel.transform.GetChild("STA").GetComponent<RectTransform>();
        intRow = panel.transform.GetChild("INT").GetComponent<RectTransform>();
        wisRow = panel.transform.GetChild("WIS").GetComponent<RectTransform>();
        lckRow = panel.transform.GetChild("LCK").GetComponent<RectTransform>();

        float parentWidth = statsDisplay.rect.width;
        float barBackWidth = levelRow.rect.width;
        centeredX = (parentWidth - barBackWidth) / 2;

        UpdatePartyMemberCountLabel();
        LoadRosterSlides();
    }

    /// <summary>Performs initial setup after all Awake calls complete.</summary>
    private void Start()
    {
        scene.FadeIn();
    }

    /// <summary>Runs per-frame update logic.</summary>
    private void Update()
    {
        HandleTouch();

        if (!dragging && Mathf.Abs(velocity) > 0.1f)
        {
            float delta = velocity * Time.deltaTime;
            MoveSlides(delta);
            float decel = deceleration * Time.deltaTime;
            velocity = velocity > 0 ? Mathf.Max(0, velocity - decel) : Mathf.Min(0, velocity + decel);
        }

        if (scrollingToCenter)
        {
            float move = Mathf.Lerp(0, targetOffset, 10f * Time.deltaTime);
            MoveSlides(move);
            targetOffset -= move;

            if (Mathf.Abs(targetOffset) < 0.5f)
            {
                scrollingToCenter = false;
                targetOffset = 0f;
            }
        }

        WrapSlides();
    }

    /// <summary>Load roster slides.</summary>
    private void LoadRosterSlides()
    {
        var rosterMembers = ProfileHelper.CurrentProfile.CurrentSave.Roster.Members;
        foreach (var member in rosterMembers)
        {
            if (member == null || member.CharacterClass == CharacterClass.None)
                continue;

            var actorData = ActorLibrary.Get(member.CharacterClass);
            if (actorData == null)
            {
                Debug.LogWarning($"Skipping roster member with invalid class: {member.CharacterClass}");
                continue;
            }

            // Use factory instead of Instantiate(prefab)
            GameObject slide = RosterSlideFactory.Create(rosterPanel);
            var instance = slide.GetComponent<RosterSlideInstance>();

            // Show the slide name
            slide.name = $"RosterSlide_{member.CharacterClass}";

            // Load the instance with all required variables
            instance.Initialize(
                characterClass: member.CharacterClass,
                sprite: actorData.Portrait,
                width: 512f,
                height: 512f,
                onClick: () => CenterOn(instance),
                isInParty: IsInParty(member.CharacterClass)
            );

            // Add the instance to the roster
            AddItem(instance);
        }

        RepositionSlides();
    }

    /// <summary>Handle touch.</summary>
    private void HandleTouch()
    {
        if (Input.touchCount > 0)
        {
            Touch touch = Input.GetTouch(0);

            Vector2 localPoint = UnitConversionHelper.Screen.ToCanvas(rosterPanel, touch.position);

            if (!rosterPanel.rect.Contains(localPoint)) return;

            if (touch.phase == TouchPhase.Began)
            {
                touchStart = touch.position;
                dragging = true;
                velocity = 0f;
                clickAllowed = true;
            }
            else if (touch.phase == TouchPhase.Moved && dragging)
            {
                Vector2 current = touch.position;
                float deltaX = current.x - touchStart.x;

                if (Mathf.Abs(deltaX) > dragThreshold)
                    clickAllowed = false;

                MoveSlides(deltaX);
                velocity = Mathf.Clamp(deltaX / Time.deltaTime, -maxFocus, maxFocus);
                touchStart = current;
            }
            else if (touch.phase == TouchPhase.Ended)
            {
                dragging = false;
            }
        }
    }

    /// <summary>Move slides.</summary>
    private void MoveSlides(float deltaX)
    {
        foreach (var item in slides.Values)
        {
            Vector3 pos = item.rectTransform.anchoredPosition;
            pos.x += deltaX;
            item.rectTransform.anchoredPosition = pos;
        }
    }

    /// <summary>Wrap slides.</summary>
    private void WrapSlides()
    {
        var itemList = slides.Values.ToList();

        for (int i = 0; i < itemList.Count; i++)
        {
            var item = itemList[i];
            float width = item.Width;
            float totalWidth = width + spacing;
            Vector3 pos = item.rectTransform.anchoredPosition;

            if (pos.x < -totalWidth * wrapThresholdMultiplier)
            {
                float rightMostX = GetRightmostX();
                pos.x = rightMostX + totalWidth;
                item.rectTransform.anchoredPosition = pos;
            }
            else if (pos.x > totalWidth * (itemList.Count - wrapThresholdMultiplier))
            {
                float leftMostX = GetLeftmostX();
                pos.x = leftMostX - totalWidth;
                item.rectTransform.anchoredPosition = pos;
            }
        }
    }

    /// <summary>Gets the leftmost x.</summary>
    private float GetLeftmostX()
    {
        float min = float.MaxValue;
        foreach (var item in slides.Values)
            min = Mathf.Min(min, item.rectTransform.anchoredPosition.x);
        return min;
    }

    /// <summary>Gets the rightmost x.</summary>
    private float GetRightmostX()
    {
        float max = float.MinValue;
        foreach (var item in slides.Values)
            max = Mathf.Max(max, item.rectTransform.anchoredPosition.x);
        return max;
    }

    /// <summary>Reposition slides.</summary>
    private void RepositionSlides()
    {
        float x = 0;
        foreach (var slide in slides.Values)
        {
            float width = slide.Width;
            slide.rectTransform.anchoredPosition = new Vector2(x, 0);
            x += width + spacing;
        }
    }

    /// <summary>Center on.</summary>
    public void CenterOn(RosterSlideInstance slide)
    {
        if (!clickAllowed) return;

        float offset = slide.rectTransform.anchoredPosition.x;
        if (slide.rectTransform.parent != rosterPanel)
            offset += slide.rectTransform.parent.GetComponent<RectTransform>().anchoredPosition.x;

        targetOffset = -offset;
        scrollingToCenter = true;

        // Update the title
        title.GetComponent<TextMeshProUGUI>().text = slide.CharacterClass.ToString();

        // Update the button text and functionality
        UpdateAddRemoveButton(slide.CharacterClass);

        // Update the Stats display
        UpdateStatsDisplay(slide.CharacterClass);
    }

    /// <summary>Updates the stats display.</summary>
    private void UpdateStatsDisplay(CharacterClass characterClass)
    {
        var rosterMember = ProfileHelper.CurrentProfile.CurrentSave.Roster.Members.Where(x => x.CharacterClass == characterClass).First();
        var derived = ExperienceHelper.DeriveFromTotalXP(Mathf.Max(0, rosterMember.TotalXP));
        Load(rosterMember.CharacterClass, Mathf.Max(1, derived.level));
    }

    /// <summary>Updates the party member label.</summary>
    private void UpdatePartyMemberLabel(bool isInParty)
    {
        addRemovePartyMemberLabel.text = isInParty ? "Remove from Party" : "Add to Party";
    }

    /// <summary>Updates the party member count label.</summary>
    private void UpdatePartyMemberCountLabel()
    {
        partyMemberCountLabel.text = $"{partyMemberCount}/{Common.MaxPartyMemberCount}";
    }

    /// <summary>Updates the slide checkmark.</summary>
    private void UpdateSlideCheckmark(CharacterClass characterClass, bool isInParty)
    {
        // Update the checkmark for the slide
        if (slides.TryGetValue(characterClass, out var slide))
        {
            slide.SetCheckmark(isInParty);
        }
    }

    /// <summary>Updates the add remove button.</summary>
    private void UpdateAddRemoveButton(CharacterClass characterClass)
    {
        bool isInParty = IsInParty(characterClass);
        UpdatePartyMemberLabel(isInParty);
        UpdatePartyMemberCountLabel();
        UpdateSlideCheckmark(characterClass, isInParty);

        // Update the button functionality
        var button = addRemovePartyMemberButton.GetComponent<Button>();
        button.onClick.RemoveAllListeners(); // Hide previous listeners
        if (isInParty)
        {
            button.onClick.AddListener(() => RemoveFromParty(characterClass));
        }
        else
        {
            button.onClick.AddListener(() => AddToParty(characterClass));
        }
    }

    /// <summary>Add to party.</summary>
    private void AddToParty(CharacterClass characterClass)
    {
        if (partyMemberCount >= Common.MaxPartyMemberCount)
        {
            Debug.LogWarning($"Cannot add more than {Common.MaxPartyMemberCount} members to the party.");
            return;
        }

        ProfileHelper.AddToParty(characterClass);
        UpdateAddRemoveButton(characterClass); // Refresh button state
    }



    /// <summary> remove from party..Groups[0].Value.ToUpper() emove from party.</summary>
    private void RemoveFromParty(CharacterClass characterClass)
    {
        ProfileHelper.RemoveFromParty(characterClass);
        UpdateAddRemoveButton(characterClass); // Refresh button state
    }

    /// <summary>Add item.</summary>
    public void AddItem(RosterSlideInstance slide)
    {
        if (!slides.ContainsKey(slide.CharacterClass))
        {
            slides.Add(slide.CharacterClass, slide);
        }
    }

    /// <summary>Load.</summary>
    public void Load(CharacterClass characterClass, int level)
    {
        if (characterClass == CharacterClass.None) return;
        var actorData = ActorLibrary.Get(characterClass);
        if (actorData == null) return;
        var stats = actorData.GetStats(level);

        // Update each stat row
        UpdateStatRow(levelRow, "LVL", stats.Level); // Assuming max level is 100
        UpdateStatRow(hpRow, "HP", stats.HP, stats.MaxHP);
        UpdateStatRow(strRow, "STR", stats.Strength); // Assuming max stat value is 100
        UpdateStatRow(vitRow, "VIT", stats.Vitality);
        UpdateStatRow(agiRow, "SPD", stats.Agility);
        UpdateStatRow(spdRow, "SPD", stats.Speed);
        UpdateStatRow(staRow, "STA", stats.Stamina);
        UpdateStatRow(intRow, "INT", stats.Intelligence);
        UpdateStatRow(wisRow, "WIS", stats.Wisdom);
        UpdateStatRow(lckRow, "LCK", stats.Luck);
    }



    /// <summary>Updates the stat row.</summary>
    private void UpdateStatRow(RectTransform row, string label, float value, float maxValue = 99)
    {
        var labelComponent = row.Find("Label").GetComponent<TextMeshProUGUI>();
        labelComponent.text = label;

        var backImage = row.Find("Bar/Back").GetComponent<Image>();
        var fillImage = row.Find("Bar/Fill").GetComponent<Image>();
        var valueComponent = row.Find("Value").GetComponent<TextMeshProUGUI>();
        valueComponent.text = $"{value}";

        float targetWidth = backImage.rectTransform.rect.width * (value / maxValue);

        // Cancel any existing Animation on this row
        if (barAnimations.TryGetValue(row, out Coroutine runningCoroutine))
            StopCoroutine(runningCoroutine);

        // BounceRoutine new Animation
        barAnimations[row] = StartCoroutine(AnimateBarFillRoutine(row, fillImage.rectTransform, targetWidth));

    }

    /// <summary>Coroutine that executes the animate bar fill sequence.</summary>
    private IEnumerator AnimateBarFillRoutine(RectTransform row, RectTransform bar, float targetWidth)
    {
        float duration = 0.4f;
        float elapsed = 0f;
        float startWidth = bar.sizeDelta.x;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / duration);
            float newWidth = Mathf.Lerp(startWidth, targetWidth, t);
            bar.sizeDelta = new Vector2(newWidth, bar.sizeDelta.y);
            yield return Wait.None();
        }

        bar.sizeDelta = new Vector2(targetWidth, bar.sizeDelta.y);

        //Remove from dictionary
        barAnimations.Remove(row);
    }




    /// <summary>Handles the back button clicked event.</summary>
    public void OnBackButtonClicked()
    {
        scene.Fade.ToGame();
    }


    /// <summary>Handles the equipment tooltip anchor clicked event.</summary>
    public void OnEquipmentTooltipAnchorClicked()
    {

        var message = "Lorem ipsum dolor sit amet, consectetur adipiscing elit. In tellus augue, iaculis quis neque at, malesuada consectetur leo. Vestibulum id nulla lobortis, ullamcorper nibh at, vestibulum justo. Etiam sed ipsum eget odio consequat vulputate. Phasellus consequat neque quam, ac pulvinar nulla consectetur quis. Nulla facilisi. Nulla semper, justo eget volutpat tempor, elit mauris convallis mi, nec pellentesque neque dui eu metus. Mauris non facilisis sapien, lacinia consequat nibh.";
        var tt = new TooltipSettings()
        {
            message = message,
            target = GameObject.Find("TestTooltip").GetComponent<RectTransform>(),
            placement = TooltipPlacement.Top,
            useFade = true,
            useTypewriter = true,
            autoDestroy = false,
            autoDestroyDelay = 2.5f,
            textAlignment = TooltipTextAlignment.TopLeft
        };
        Tooltip.Show(tt);
    }


}

}
