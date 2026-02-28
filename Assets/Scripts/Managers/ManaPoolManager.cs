using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using g = Assets.Helpers.GameHelper;
using Assets.Helper;

/// <summary>
/// MANAPOOLMANAGER - Hero and enemy mana resource system.
/// 
/// PURPOSE:
/// Manages mana pools for both teams, handling accumulation,
/// spending, and UI display.
/// 
/// MANA ACCUMULATION:
/// - Passive gain: Mana accumulates while timeline advances
/// - Bank bonus: Grants bonus mana equal to time skipped
/// - Rate: manaPerSecond (default 5/sec)
/// 
/// MANA USAGE:
/// - Abilities spend mana when cast
/// - Each ability has a ManaCost
/// - Cannot cast if insufficient mana
/// 
/// UI ELEMENTS:
/// - Hero mana fill bar (blue/cyan HDR)
/// - Enemy mana fill bar (optional)
/// - Bank button for time skip
/// 
/// RELATED FILES:
/// - AbilityManager.cs: Spends mana on ability cast
/// - TimelineBarInstance.cs: Triggers mana gain
/// - AbilityButton.cs: Shows mana requirements
/// 
/// ACCESS: g.ManaPoolManager
/// </summary>
public class ManaPoolManager : MonoBehaviour
{
    [Header("Config")]
    public float maxMana = 100f;
    [SerializeField] private float _heroMana = 0f;
    public float enemyMana = 0f;

    public float heroMana
    {
        get => _heroMana;
        set => _heroMana = value;
    }

    [Header("Passive Gain")]
    [Tooltip("Mana gained per second while timeline advances.")]
    public float manaPerSecond = 5f;

    [Header("UI")]
    public Color manaHdrColor = new Color(0.2f, 1.8f, 3.2f, 1f);
    public bool showEnemyMana = false;

    private Button BankButton;
    private Image HeroFill;
    private Image EnemyFill;

    private void Awake()
    {
        _heroMana = 0f;
        enemyMana = 0f;

        BankButton = GameObjectHelper.Game.ManaPool.BankButton;
        HeroFill = GameObjectHelper.Game.ManaPool.HeroFill;
        EnemyFill = GameObjectHelper.Game.ManaPool.EnemyFill;

        BankButton.onClick.AddListener(OnBankButtonClicked);

        ApplyBloomColor();
        RefreshUI();
    }

    private void OnDestroy()
    {
        BankButton.onClick.RemoveListener(OnBankButtonClicked);
    }

    private void Update()
    {
        // Accumulate mana at a constant rate while the timeline is advancing
        if (g.TimelineBar.IsAdvancing)
        {
            float gain = manaPerSecond * Time.deltaTime;
            heroMana = Mathf.Clamp(heroMana + gain, 0f, maxMana);
            RefreshUI();
        }
    }

    /// <summary>
    /// Bank button: skip to the next enemy trigger and gain mana equal to the time skipped.
    /// The hero sacrifices their movement/turn to instantly accumulate mana and trigger the next enemy.
    /// </summary>
    public void OnBankButtonClicked()
    {
        if (!g.TurnManager.IsHeroTurn)
            return;

        // Get the next bank target
        var (arrivingEnemy, secondsSkipped) = g.TimelineBar.GetNextBankTarget();
        if (arrivingEnemy == null)
            return;

        // Advance the timeline visually
        g.TimelineBar.AdvanceToNextTrigger(arrivingEnemy, secondsSkipped);

        // Grant mana for the time skipped
        float gain = secondsSkipped * manaPerSecond;
        heroMana = Mathf.Clamp(heroMana + gain, 0f, maxMana);
        
        RefreshUI();
        g.AbilityButtonManager?.UpdateAllInteractables(heroMana);

        // Disable input during transition
        g.InputManager.InputMode = InputMode.None;

        // Queue the timeline trigger sequence which handles drop, pincer, and enemy turn
        g.SequenceManager.Add(new Assets.Scripts.Sequences.TimelineTriggerSequence(arrivingEnemy));
        g.SequenceManager.Execute();
    }

    /// <summary>
    /// Spend mana for an ability. Returns true if successful, false if insufficient mana.
    /// </summary>
    public bool Spend(Team team, float cost)
    {
        cost = Mathf.Max(0f, cost);

        if (team == Team.Hero)
        {
            if (heroMana < cost)
                return false;
            heroMana -= cost;
        }
        else
        {
            if (enemyMana < cost)
                return false;
            enemyMana -= cost;
        }

        RefreshUI();
        g.AbilityButtonManager.UpdateAllInteractables(heroMana);
        return true;
    }

    /// <summary>
    /// Add mana directly (for special effects, pickups, etc.).
    /// </summary>
    public void AddMana(Team team, float amount)
    {
        amount = Mathf.Max(0f, amount);

        if (team == Team.Hero)
            heroMana = Mathf.Clamp(heroMana + amount, 0f, maxMana);
        else
            enemyMana = Mathf.Clamp(enemyMana + amount, 0f, maxMana);

        RefreshUI();
        g.AbilityButtonManager.UpdateAllInteractables(heroMana);
    }

    /// <summary>
    /// Update the UI fill bars to reflect current mana values.
    /// </summary>
    public void RefreshUI()
    {
        float heroT = Mathf.Clamp01(heroMana / maxMana);
        HeroFill.fillAmount = heroT;

        EnemyFill.gameObject.SetActive(showEnemyMana);
        if (showEnemyMana)
        {
            float enemyT = Mathf.Clamp01(enemyMana / maxMana);
            EnemyFill.fillAmount = enemyT;
        }
    }

    private void ApplyBloomColor()
    {
        HeroFill.color = manaHdrColor;
        EnemyFill.color = manaHdrColor;
    }

    /// <summary>
    /// Legacy hook called by TurnManager at turn start. 
    /// </summary>
    public void OnTurnStarted(Team team)
    {
        RefreshUI();
    }
}
