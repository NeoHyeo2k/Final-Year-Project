using UnityEngine;

[RequireComponent(typeof(FTGAgent))]
public class WinLoseReward : MonoBehaviour
{
    [Header("References")]
    public FTGAgent agent;
    public MatchManager matchManager;
    public FighterController selfController;

    [Header("Reward Settings")]
    public float winReward = 8f;
    public float losePenalty = -8f;
    public float drawReward = 0f;

    private bool rewardGivenThisRound = false;

    private void Awake()
    {
        if (agent == null)
            agent = GetComponent<FTGAgent>();

        if (selfController == null)
            selfController = GetComponent<FighterController>();
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

        rewardGivenThisRound = true;
    }
}