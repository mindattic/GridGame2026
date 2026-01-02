using Assets.Helper;
using Assets.Helpers;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using static Intermission.Before;

public class AbilityButton : MonoBehaviour
{
    public Button button;                         // Show in Inspector or dynamically
    public TMP_Text label;                        // Show in Inspector or dynamically
    private Ability ability;

    private void Awake()
    {
        // Show missing references if not set in Inspector
        if (button == null)
            button = GetComponent<Button>();

        if (label == null)
            label = GetComponentInChildren<TMP_Text>();
    }

    public void Initialize(Ability ability, System.Action onClick)
    {
        if (label != null)
            label.text = ability.name;
        else
            Debug.LogError("AbilityButton.label is null");

        // store ability for later use (cost checks, etc.)
        this.ability = ability;

        if (button != null)
        {
            button.onClick.RemoveAllListeners();
            button.onClick.AddListener(() => onClick?.Invoke());
        }
        else
        {
            Debug.LogError("AbilityButton.button is null");
        }
    }

    public void UpdateInteractable(float currentMana)
    {
        if (ability == null || button == null) return;
        button.interactable = currentMana >= ability.ManaCost;
    }


    public Vector3 WorldPosition()
    {
        return UnitConversionHelper.Canvas.ToWorld(button.transform, button.transform.position.z);
    }
}

public enum AbilityEffect
{
    None,
    Heal,
    ShieldRush,
    Trap,
    Smite
}

public enum AbilityTargetingMode
{
    AnyActor,
    Linear
}

public class Ability
{
    public string name;
    public AbilityType type;
    public Sprite button;

    // New: how many distinct targets this ability can select (default 1)
    public int TotalNumberOfTargets = 1;

    // New: semantic effect and targeting mode
    public AbilityEffect Effect = AbilityEffect.None;
    public AbilityTargetingMode TargetingMode = AbilityTargetingMode.AnyActor;

    // New: mana cost to cast this ability once
    public int ManaCost = 0;

    // New: description to display on the card when selected
    public string Description;

    public bool requiresTarget =>
        type == AbilityType.TargetAlly || type == AbilityType.TargetOpponent || type == AbilityType.TargetAny;

    public void Activate(ActorInstance user, ActorInstance target)
    {
        // Implement actual ability logic
        Debug.Log($"{user.name} used {name} on {(target ? target.name : "no target")}");
    }
}
