using UnityEngine;

[RequireComponent(typeof(FTGAgent))]
public class WinLoseReward : MonoBehaviour
{
    [Header("References")]
    public FTGAgent agent;
    public MatchManager matchManager;
    public FighterController selfController;
    public Health selfHealth;
    public Health opponentHealth;

    [Header("Reward Settings")]
    public float winReward = 8f;
    public float losePenalty = -8f;
    public float drawReward = 0f;

    [Header("Terminal Health Difference")]
    [Tooltip("Scale applied to normalized terminal health difference: selfHealthRatio - opponentHealthRatio.")]
    public float terminalHealthDiffScale = 1f;

    [Tooltip("If true, add terminal health difference reward once at round end.")]
    public bool useTerminalHealthDiffReward = true;

    private bool rewardGivenThisRound = false;

    private void Awake()
    {
        if (agent == null)
            agent = GetComponent<FTGAgent>();

        if (selfController == null)
            selfController = GetComponent<FighterController>();

        ResolveHealthReferences();
    }

    private void OnEnable()
    {
        Subscribe();
    }

    private void Start()
    {
        Subscribe();
    }

    private void OnDisable()
    {
        Unsubscribe();
    }

    private void OnDestroy()
    {
        Unsubscribe();
    }

    private void Subscribe()
    {
        if (matchManager != null)
        {
            matchManager.OnRoundStarted -= HandleRoundStarted;
            matchManager.OnRoundStarted += HandleRoundStarted;

            matchManager.OnRoundEnded -= HandleRoundEnded;
            matchManager.OnRoundEnded += HandleRoundEnded;
        }
    }

    private void Unsubscribe()
    {
        if (matchManager != null)
        {
            matchManager.OnRoundStarted -= HandleRoundStarted;
            matchManager.OnRoundEnded -= HandleRoundEnded;
        }
    }

    private void HandleRoundStarted()
    {
        rewardGivenThisRound = false;
    }

    private void HandleRoundEnded(FighterController winner, FighterController loser, bool draw)
    {
        if (rewardGivenThisRound || agent == null || selfController == null)
            return;

        if (draw)
        {
            agent.AddReward(drawReward);
        }
        else if (winner == selfController)
        {
            agent.AddReward(winReward);
        }
        else if (loser == selfController)
        {
            agent.AddReward(losePenalty);
        }

        if (useTerminalHealthDiffReward)
        {
            agent.AddReward(GetTerminalHealthDifferenceReward());
        }

        rewardGivenThisRound = true;
    }

    private void ResolveHealthReferences()
    {
        if (matchManager == null || selfController == null)
            return;

        if (selfController == matchManager.fighterA)
        {
            if (selfHealth == null)
                selfHealth = matchManager.healthA;

            if (opponentHealth == null)
                opponentHealth = matchManager.healthB;
        }
        else if (selfController == matchManager.fighterB)
        {
            if (selfHealth == null)
                selfHealth = matchManager.healthB;

            if (opponentHealth == null)
                opponentHealth = matchManager.healthA;
        }
    }

    private float GetTerminalHealthDifferenceReward()
    {
        ResolveHealthReferences();

        if (selfHealth == null || opponentHealth == null)
            return 0f;

        float selfRatio = (float)selfHealth.currentHealth / Mathf.Max(1, selfHealth.maxHealth);
        float opponentRatio = (float)opponentHealth.currentHealth / Mathf.Max(1, opponentHealth.maxHealth);
        float normalizedDiff = selfRatio - opponentRatio;

        return normalizedDiff * terminalHealthDiffScale;
    }
}
