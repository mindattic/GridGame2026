using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using g = Assets.Helpers.GameHelper;
using Assets.Helper;

/// <summary>
/// Manages hero and enemy mana pools.
/// - Mana accumulates at a constant rate while the timeline is advancing (hero is moving).
/// - Bank button grants bonus mana equal to time skipped.
/// - Abilities spend mana when cast.
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
    [Tooltip("Mana gained per second while the timeline is advancing.")]
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

        float secondsSkipped;
        var arrivingEnemy = g.TimelineBar.BankToNextTrigger(out secondsSkipped);

        if (arrivingEnemy == null)
            return;

        // Grant mana for the time skipped
        float gain = secondsSkipped * manaPerSecond;
        heroMana = Mathf.Clamp(heroMana + gain, 0f, maxMana);
        
        RefreshUI();
        g.AbilityButtonManager.UpdateAllInteractables(heroMana);

        // Begin the enemy turn after a brief delay to allow UI to update
        StartCoroutine(BeginEnemyTurnAfterBank(arrivingEnemy));
    }

    private IEnumerator BeginEnemyTurnAfterBank(ActorInstance enemy)
    {
        g.InputManager.InputMode = InputMode.None;
        g.SelectionManager.Drop();

        yield return null;

        if (enemy.IsPlaying)
            g.TurnManager.ForceBeginEnemyTurn(enemy);
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
