using UnityEngine;

[RequireComponent(typeof(FTGAgent))]
public class WinLoseReward : MonoBehaviour
{
    [Header("References")]
    public FTGAgent agent;
    public MatchManager matchManager;
    public FighterController selfController;

    [Header("Reward Settings")]
    public float winReward = 10f;
    public float losePenalty = -10f;
    public float drawReward = 0f;

    private bool rewardGivenThisRound = false;
    private bool lastRoundActive = false;

    private void Awake()
    {
        if (agent == null)
            agent = GetComponent<FTGAgent>();

        if (selfController == null)
            selfController = GetComponent<FighterController>();
    }

    private void Start()
    {
        if (matchManager != null)
        {
            lastRoundActive = matchManager.RoundActive;
        }
    }

    private void Update()
    {
        if (matchManager == null || agent == null || selfController == null)
            return;

        bool currentRoundActive = matchManager.RoundActive;

        if (currentRoundActive)
        {
            rewardGivenThisRound = false;
        }
        else if (lastRoundActive && !rewardGivenThisRound)
        {
            ResolveRoundReward();
            rewardGivenThisRound = true;
        }

        lastRoundActive = currentRoundActive;
    }

    private void ResolveRoundReward()
    {
        if (matchManager.IsDraw)
        {
            agent.AddReward(drawReward);
            return;
        }

        if (matchManager.Winner == selfController)
        {
            agent.AddReward(winReward);
            return;
        }

        if (matchManager.Loser == selfController)
        {
            agent.AddReward(losePenalty);
            return;
        }
    }
}