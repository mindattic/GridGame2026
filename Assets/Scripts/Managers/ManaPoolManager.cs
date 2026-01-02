using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using g = Assets.Helpers.GameHelper;
using Assets.Helper;
using Assets.Scripts.Canvas;

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
    
    // Property to track heroMana changes
    public float heroMana
    {
        get => _heroMana;
        set
        {
            if (!Mathf.Approximately(_heroMana, value))
            {
                // Debug.Log($"[ManaPool] heroMana changed: {_heroMana:F2} -> {value:F2}");
            }
            _heroMana = value;
        }
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

        ApplyBloomColor();
        RefreshUI();
    }

    private void OnDestroy()
    {

    }

    private void Update()
    {
        // Accumulate mana at a constant rate while the timeline is advancing
        var timeline = g.TimelineBar;
        if (timeline != null && timeline.IsAdvancing)
        {
            float gain = manaPerSecond * Time.deltaTime;
            heroMana = Mathf.Clamp(heroMana + gain, 0f, maxMana);
            RefreshUI();
        }
        
        // Debug: detect if fillAmount was changed externally
        if (HeroFill != null)
        {
            float expectedFill = maxMana <= 0f ? 0f : Mathf.Clamp01(heroMana / maxMana);
            if (!Mathf.Approximately(HeroFill.fillAmount, expectedFill))
            {
                Debug.LogWarning($"[ManaPool] Fill mismatch detected! fillAmount={HeroFill.fillAmount:F3}, expected={expectedFill:F3}, heroMana={heroMana:F2}");
                // Force correction
                HeroFill.fillAmount = expectedFill;
            }
        }
    }

    /// <summary>
    /// Bank button: skip to the next enemy trigger and gain mana equal to the time skipped.
    /// The hero sacrifices their movement/turn to instantly accumulate mana and trigger the next enemy.
    /// </summary>
    public void OnBankButtonClicked()
    {
        Debug.Log("[ManaPool] Bank button clicked");
        
        if (g.TurnManager == null || !g.TurnManager.IsHeroTurn)
        {
            Debug.Log("[ManaPool] Bank rejected: not hero turn");
            return;
        }

        var timeline = g.TimelineBar;
        if (timeline == null)
        {
            Debug.Log("[ManaPool] Bank rejected: no timeline");
            return;
        }

        float secondsSkipped;
        var arrivingEnemy = timeline.BankToNextTrigger(out secondsSkipped);

        if (arrivingEnemy == null)
        {
            Debug.Log("[ManaPool] Bank rejected: no arriving enemy returned");
            return;
        }

        // Store the mana before adding for debug
        float manaBefore = heroMana;
        
        // Grant mana for the time skipped (even if small, give at least something)
        float gain = secondsSkipped * manaPerSecond;
        heroMana = Mathf.Clamp(heroMana + gain, 0f, maxMana);
        
        Debug.Log($"[ManaPool] Bank SUCCESS: secondsSkipped={secondsSkipped:F2}, gain={gain:F2}, manaBefore={manaBefore:F2}, manaAfter={heroMana:F2}");
        
        RefreshUI();

        // Update ability buttons with new mana amount
        g.AbilityButtonManager?.UpdateAllInteractables(heroMana);

        // Begin the enemy turn after a brief delay to allow UI to update
        StartCoroutine(BeginEnemyTurnAfterBank(arrivingEnemy));
    }

    /// <summary>
    /// Begin the enemy turn after banking. Waits a frame for UI updates.
    /// </summary>
    private IEnumerator BeginEnemyTurnAfterBank(ActorInstance enemy)
    {
        // Disable input during transition
        g.InputManager.InputMode = InputMode.None;
        g.SelectionManager.Drop();

        // Wait a frame for visual updates to settle
        yield return null;

        // Start the enemy turn - ForceBeginEnemyTurn handles everything
        if (enemy != null && enemy.IsPlaying && g.TurnManager != null)
        {
            g.TurnManager.ForceBeginEnemyTurn(enemy);
        }
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
        g.AbilityButtonManager?.UpdateAllInteractables(heroMana);
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
        g.AbilityButtonManager?.UpdateAllInteractables(heroMana);
    }

    /// <summary>
    /// Update the UI fill bars to reflect current mana values.
    /// </summary>
    public void RefreshUI()
    {
        // Re-fetch HeroFill if null (might have been destroyed/recreated)
        if (HeroFill == null)
            HeroFill = GameObjectHelper.Game.ManaPool.HeroFill;
            
        if (HeroFill != null)
        {
            float heroT = maxMana <= 0f ? 0f : Mathf.Clamp01(heroMana / maxMana);
            HeroFill.fillAmount = heroT;
        }

        // Re-fetch EnemyFill if null
        if (EnemyFill == null)
            EnemyFill = GameObjectHelper.Game.ManaPool.EnemyFill;
            
        if (EnemyFill != null)
        {
            EnemyFill.gameObject.SetActive(showEnemyMana);
            if (showEnemyMana)
            {
                float enemyT = maxMana <= 0f ? 0f : Mathf.Clamp01(enemyMana / maxMana);
                EnemyFill.fillAmount = enemyT;
            }
        }
    }

    /// <summary>
    /// Apply HDR color to fill images for bloom effect.
    /// </summary>
    private void ApplyBloomColor()
    {
        if (HeroFill != null)
            HeroFill.color = manaHdrColor;

        if (EnemyFill != null)
            EnemyFill.color = manaHdrColor;
    }

    /// <summary>
    /// Legacy hook called by TurnManager at turn start. 
    /// No longer needed since mana now accumulates continuously while timeline moves.
    /// </summary>
    public void OnTurnStarted(Team team)
    {
        // No-op: mana is now gained continuously in Update() while timeline.IsAdvancing
        RefreshUI();
    }
}
