using TMPro;
using UnityEngine;
using g = Assets.Helpers.GameHelper;
using Assets.Helper;
using UnityEngine.UI;
using Assets.Scripts.Canvas; // for TimelineBarInstance
using System.Collections; // for coroutine

public class ManaPoolManager : MonoBehaviour
{
    [Header("Config")]
    public float maxMana = 100f;
    public float heroMana = 0f;
    public float enemyMana = 0f;

    [Header("Gain Rates")]
    public float perTurnGain = 5f;     // passive gain each turn start
    public float onAttackGain = 5f;    // gain when dealing damage
    public float onHitGain = 3f;       // gain when taking damage

    private Button BankButton;
    private TextMeshProUGUI HeroLabel;
    private TextMeshProUGUI EnemyLabel;

    private void Awake()
    {
        heroMana = 0f; enemyMana = 0f; // ensure start at zero
        BankButton = GameObjectHelper.Game.ManaPool.BankButton;
        HeroLabel = GameObjectHelper.Game.ManaPool.HeroMana;
        EnemyLabel = GameObjectHelper.Game.ManaPool.EnemyMana;

        if (BankButton != null) BankButton.onClick.AddListener(OnBankButtonClicked);

        RefreshUI();
    }

    private void OnDestroy()
    {
        if (BankButton != null)
            BankButton.onClick.RemoveListener(OnBankButtonClicked);
    }

    private TimelineBarInstance GetTimeline() => g.TimelineBar;

    public void OnBankButtonClicked()
    {
        // Only usable during hero planning window
        if (!g.TurnManager.IsHeroTurn) return;
        var bar = GetTimeline();
        if (bar == null) return;

        // Bank: move all tags left by the exact remaining time of the nearest tag
        float secondsUsed;
        var arrivingEnemy = bar.BankToNextTrigger(out secondsUsed);
        if (arrivingEnemy == null || secondsUsed <= 0f)
            return;

        // Add MP to hero based on seconds used
        Accumulate(Team.Hero, secondsUsed);
        RefreshUI();

        // Handoff on the next frame to avoid colliding with any in-flight sequences
        StartCoroutine(BeginEnemyTurnNextFrame(arrivingEnemy));
    }

    private IEnumerator BeginEnemyTurnNextFrame(ActorInstance enemy)
    {
        // Mirror tag trigger behavior
        g.InputManager.InputMode = InputMode.None;
        g.SelectionManager.Drop();
        // wait one frame to settle any UI/sequence
        yield return null;

        // Primary: start enemy turn immediately
        if (enemy != null && g.TurnManager != null)
        {
            g.TurnManager.ForceBeginEnemyTurn(enemy);
        }

        // Fallback: if still in hero turn for any reason, use queued-advance path
        yield return null; // give BeginEnemyTurn a frame to flip state
        if (g.TurnManager != null && g.TurnManager.IsHeroTurn)
        {
            if (enemy != null) g.TurnManager.QueueEnemyAfterHero(enemy);
            g.TurnManager.NextTurn();
        }
    }

    public bool Spend(Team team, float cost)
    {
        cost = Mathf.Max(0, cost);
        if (team == Team.Hero)
        {
            if (heroMana < cost) return false;
            heroMana -= cost;
        }
        else
        {
            if (enemyMana < cost) return false;
            enemyMana -= cost;
        }
        RefreshUI();
        return true;
    }

    public void Accumulate(Team team, float amount)
    {
        amount = Mathf.Max(0, amount);
        if (team == Team.Hero)
            heroMana = Mathf.Clamp(heroMana + amount, 0, maxMana);
        else
            enemyMana = Mathf.Clamp(enemyMana + amount, 0, maxMana);
        RefreshUI();
    }

    public void OnTurnStarted(Team team)
    {
        // Skip the very first hero window so MP starts at 0.00
        if (team == Team.Hero && g.TurnManager != null && g.TurnManager.CurrentTurn == 0)
        {
            RefreshUI();
            return;
        }
        Accumulate(team, perTurnGain);
    }

    // Optional hooks you can call where appropriate
    public void OnDealtDamage(Team team) => Accumulate(team, onAttackGain);
    public void OnTookDamage(Team team) => Accumulate(team, onHitGain);

    public void RefreshUI()
    {
        if (HeroLabel != null) HeroLabel.text = $"MP: {heroMana.ToString("0.00")}";
        if (EnemyLabel != null) EnemyLabel.text = $"MP: {enemyMana.ToString("0.00")}";
    }
}
