using UnityEngine;

public class RoundStatsTracker : MonoBehaviour
{
    [Header("References")]
    public MatchManager matchManager;
    public FighterController trackedFighter;
    public FighterController opponentFighter;
    public Health trackedHealth;
    public Health opponentHealth;

    [Header("Logging")]
    public int summaryEveryNRounds = 20;
    public bool logEachRound = true;
    public bool logSummary = true;

    [Header("Derived Metrics")]
    [Tooltip("A hit landed by the tracked fighter within this window after a successful block counts as a post-block punish.")]
    public float postBlockPunishWindow = 0.5f;

    private int totalRounds = 0;
    private int totalWins = 0;
    private int totalLosses = 0;
    private int totalDraws = 0;

    private float totalRoundDuration = 0f;
    private int totalHitsDealt = 0;
    private int totalHitsTaken = 0;
    private int totalBlocksPerformed = 0;
    private int totalOwnAttacksBlockedByOpponent = 0;
    private int totalAttackAttempts = 0;
    private int totalWhiffs = 0;
    private int totalPostBlockPunishes = 0;

    private int windowRounds = 0;
    private int windowWins = 0;
    private int windowLosses = 0;
    private int windowDraws = 0;

    private float windowRoundDuration = 0f;
    private int windowHitsDealt = 0;
    private int windowHitsTaken = 0;
    private int windowBlocksPerformed = 0;
    private int windowOwnAttacksBlockedByOpponent = 0;
    private int windowAttackAttempts = 0;
    private int windowWhiffs = 0;
    private int windowPostBlockPunishes = 0;

    private bool roundTrackingActive = false;
    private float roundStartTime = 0f;

    private int roundHitsDealt = 0;
    private int roundHitsTaken = 0;
    private int roundBlocksPerformed = 0;
    private int roundOwnAttacksBlockedByOpponent = 0;
    private int roundAttackAttempts = 0;
    private int roundWhiffs = 0;
    private int roundPostBlockPunishes = 0;

    private bool postBlockPunishWindowActive = false;
    private float postBlockPunishTimer = 0f;

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

    private void Update()
    {
        if (!postBlockPunishWindowActive)
            return;

        postBlockPunishTimer -= Time.deltaTime;

        if (postBlockPunishTimer <= 0f)
        {
            ClearPostBlockPunishWindow();
        }
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

        HitboxController.OnGlobalAttackHit -= HandleGlobalAttackHit;
        HitboxController.OnGlobalAttackHit += HandleGlobalAttackHit;

        HitboxController.OnGlobalAttackBlocked -= HandleGlobalAttackBlocked;
        HitboxController.OnGlobalAttackBlocked += HandleGlobalAttackBlocked;

        CharacterCombat.OnGlobalAttackAttempt -= HandleGlobalAttackAttempt;
        CharacterCombat.OnGlobalAttackAttempt += HandleGlobalAttackAttempt;

        CharacterCombat.OnGlobalAttackWhiff -= HandleGlobalAttackWhiff;
        CharacterCombat.OnGlobalAttackWhiff += HandleGlobalAttackWhiff;
    }

    private void Unsubscribe()
    {
        if (matchManager != null)
        {
            matchManager.OnRoundStarted -= HandleRoundStarted;
            matchManager.OnRoundEnded -= HandleRoundEnded;
        }

        HitboxController.OnGlobalAttackHit -= HandleGlobalAttackHit;
        HitboxController.OnGlobalAttackBlocked -= HandleGlobalAttackBlocked;
        CharacterCombat.OnGlobalAttackAttempt -= HandleGlobalAttackAttempt;
        CharacterCombat.OnGlobalAttackWhiff -= HandleGlobalAttackWhiff;
    }

    private void HandleRoundStarted()
    {
        roundTrackingActive = true;
        roundStartTime = Time.time;

        roundHitsDealt = 0;
        roundHitsTaken = 0;
        roundBlocksPerformed = 0;
        roundOwnAttacksBlockedByOpponent = 0;
        roundAttackAttempts = 0;
        roundWhiffs = 0;
        roundPostBlockPunishes = 0;

        ClearPostBlockPunishWindow();
    }

    private void HandleRoundEnded(FighterController winner, FighterController loser, bool draw)
    {
        if (!roundTrackingActive)
            return;

        roundTrackingActive = false;
        ClearPostBlockPunishWindow();

        float duration = Time.time - roundStartTime;
        totalRounds++;
        windowRounds++;

        totalRoundDuration += duration;
        windowRoundDuration += duration;

        totalHitsDealt += roundHitsDealt;
        totalHitsTaken += roundHitsTaken;
        totalBlocksPerformed += roundBlocksPerformed;
        totalOwnAttacksBlockedByOpponent += roundOwnAttacksBlockedByOpponent;
        totalAttackAttempts += roundAttackAttempts;
        totalWhiffs += roundWhiffs;
        totalPostBlockPunishes += roundPostBlockPunishes;

        windowHitsDealt += roundHitsDealt;
        windowHitsTaken += roundHitsTaken;
        windowBlocksPerformed += roundBlocksPerformed;
        windowOwnAttacksBlockedByOpponent += roundOwnAttacksBlockedByOpponent;
        windowAttackAttempts += roundAttackAttempts;
        windowWhiffs += roundWhiffs;
        windowPostBlockPunishes += roundPostBlockPunishes;

        string result;
        if (draw)
        {
            totalDraws++;
            windowDraws++;
            result = "Draw";
        }
        else if (winner == trackedFighter)
        {
            totalWins++;
            windowWins++;
            result = "Win";
        }
        else
        {
            totalLosses++;
            windowLosses++;
            result = "Lose";
        }

        int selfHp = trackedHealth != null ? trackedHealth.currentHealth : -1;
        int oppHp = opponentHealth != null ? opponentHealth.currentHealth : -1;

        if (logEachRound)
        {
            Debug.Log(
                $"[RoundStats] Round {totalRounds} | Result={result} | Duration={duration:F2}s | " +
                $"SelfHP={selfHp} | OppHP={oppHp} | " +
                $"HitsDealt={roundHitsDealt} | HitsTaken={roundHitsTaken} | " +
                $"BlocksPerformed={roundBlocksPerformed} | OwnAttacksBlockedByOpponent={roundOwnAttacksBlockedByOpponent} | " +
                $"PostBlockPunishes={roundPostBlockPunishes} | AttackAttempts={roundAttackAttempts} | Whiffs={roundWhiffs}"
            );
        }

        if (logSummary && summaryEveryNRounds > 0 && windowRounds >= summaryEveryNRounds)
        {
            PrintWindowSummary();
            ResetWindowStats();
        }
    }

    private void HandleGlobalAttackHit(FighterController attacker, FighterController defender, AttackData attackData, int damage)
    {
        if (!roundTrackingActive)
            return;

        if (attacker == trackedFighter)
        {
            roundHitsDealt++;

            if (postBlockPunishWindowActive)
            {
                roundPostBlockPunishes++;
                ClearPostBlockPunishWindow();
            }
        }

        if (defender == trackedFighter)
        {
            roundHitsTaken++;
        }
    }

    private void HandleGlobalAttackBlocked(FighterController attacker, FighterController defender, AttackData attackData)
    {
        if (!roundTrackingActive)
            return;

        if (defender == trackedFighter)
        {
            roundBlocksPerformed++;
            postBlockPunishWindowActive = true;
            postBlockPunishTimer = postBlockPunishWindow;
        }

        if (attacker == trackedFighter)
        {
            roundOwnAttacksBlockedByOpponent++;
        }
    }

    private void HandleGlobalAttackAttempt(FighterController attacker, AttackData attackData)
    {
        if (!roundTrackingActive)
            return;

        if (attacker == trackedFighter)
        {
            roundAttackAttempts++;
        }
    }

    private void HandleGlobalAttackWhiff(FighterController attacker, AttackData attackData)
    {
        if (!roundTrackingActive)
            return;

        if (attacker == trackedFighter)
        {
            roundWhiffs++;
        }
    }

    private void PrintWindowSummary()
    {
        float winRate = windowRounds > 0 ? (float)windowWins / windowRounds : 0f;
        float avgDuration = windowRounds > 0 ? windowRoundDuration / windowRounds : 0f;
        float avgHitsDealt = windowRounds > 0 ? (float)windowHitsDealt / windowRounds : 0f;
        float avgHitsTaken = windowRounds > 0 ? (float)windowHitsTaken / windowRounds : 0f;
        float hitDelta = avgHitsDealt - avgHitsTaken;
        float avgBlocksPerformed = windowRounds > 0 ? (float)windowBlocksPerformed / windowRounds : 0f;
        float avgOwnAttacksBlockedByOpponent = windowRounds > 0 ? (float)windowOwnAttacksBlockedByOpponent / windowRounds : 0f;
        float avgPostBlockPunishes = windowRounds > 0 ? (float)windowPostBlockPunishes / windowRounds : 0f;
        float avgAttackAttempts = windowRounds > 0 ? (float)windowAttackAttempts / windowRounds : 0f;
        float avgWhiffs = windowRounds > 0 ? (float)windowWhiffs / windowRounds : 0f;
        float whiffRate = windowAttackAttempts > 0 ? (float)windowWhiffs / windowAttackAttempts : 0f;

        Debug.Log(
            $"[RoundStats Summary] Last {windowRounds} rounds | " +
            $"W={windowWins} L={windowLosses} D={windowDraws} | " +
            $"WinRate={winRate:P1} | AvgDuration={avgDuration:F2}s | " +
            $"AvgHitsDealt={avgHitsDealt:F2} | AvgHitsTaken={avgHitsTaken:F2} | HitDelta={hitDelta:F2} | " +
            $"AvgBlocksPerformed={avgBlocksPerformed:F2} | AvgOwnAttacksBlockedByOpponent={avgOwnAttacksBlockedByOpponent:F2} | " +
            $"AvgPostBlockPunishes={avgPostBlockPunishes:F2} | AvgAttackAttempts={avgAttackAttempts:F2} | " +
            $"AvgWhiffs={avgWhiffs:F2} | WhiffRate={whiffRate:P1}"
        );
    }

    private void ResetWindowStats()
    {
        windowRounds = 0;
        windowWins = 0;
        windowLosses = 0;
        windowDraws = 0;

        windowRoundDuration = 0f;
        windowHitsDealt = 0;
        windowHitsTaken = 0;
        windowBlocksPerformed = 0;
        windowOwnAttacksBlockedByOpponent = 0;
        windowAttackAttempts = 0;
        windowWhiffs = 0;
        windowPostBlockPunishes = 0;
    }

    private void ClearPostBlockPunishWindow()
    {
        postBlockPunishWindowActive = false;
        postBlockPunishTimer = 0f;
    }
}
