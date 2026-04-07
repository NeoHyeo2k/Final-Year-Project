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
    [Tooltip("每多少局打印一次 summary")]
    public int summaryEveryNRounds = 20;

    [Tooltip("是否打印每局详细结果")]
    public bool logEachRound = true;

    [Tooltip("是否打印阶段性 summary")]
    public bool logSummary = true;

    // Lifetime totals
    private int totalRounds = 0;
    private int totalWins = 0;
    private int totalLosses = 0;
    private int totalDraws = 0;

    private float totalRoundDuration = 0f;
    private float totalRewardProxy = 0f; // 这里只做占位，不直接从 agent 读 reward

    private int totalHitsDealt = 0;
    private int totalHitsTaken = 0;
    private int totalBlocksPerformed = 0;   // 自己成功防住次数
    private int totalAttacksBlocked = 0;    // 自己打出去但被对面防住次数
    private int totalAttackAttempts = 0;
    private int totalWhiffs = 0;

    // Window totals
    private int windowRounds = 0;
    private int windowWins = 0;
    private int windowLosses = 0;
    private int windowDraws = 0;

    private float windowRoundDuration = 0f;
    private int windowHitsDealt = 0;
    private int windowHitsTaken = 0;
    private int windowBlocksPerformed = 0;
    private int windowAttacksBlocked = 0;
    private int windowAttackAttempts = 0;
    private int windowWhiffs = 0;

    // Current round
    private bool roundTrackingActive = false;
    private float roundStartTime = 0f;

    private int roundHitsDealt = 0;
    private int roundHitsTaken = 0;
    private int roundBlocksPerformed = 0;
    private int roundAttacksBlocked = 0;
    private int roundAttackAttempts = 0;
    private int roundWhiffs = 0;

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
        // 临时调试;
        // Debug.Log("[RoundStatsTracker] HandleRoundStarted fired.");

        roundTrackingActive = true;
        roundStartTime = Time.time;

        roundHitsDealt = 0;
        roundHitsTaken = 0;
        roundBlocksPerformed = 0;
        roundAttacksBlocked = 0;
        roundAttackAttempts = 0;
        roundWhiffs = 0;
    }

    private void HandleRoundEnded(FighterController winner, FighterController loser, bool draw)
    {
        // 临时调试;
        // Debug.Log("[RoundStatsTracker] HandleRoundEnded fired.");

        if (!roundTrackingActive)
            return;

        roundTrackingActive = false;

        float duration = Time.time - roundStartTime;
        totalRounds++;
        windowRounds++;

        totalRoundDuration += duration;
        windowRoundDuration += duration;

        totalHitsDealt += roundHitsDealt;
        totalHitsTaken += roundHitsTaken;
        totalBlocksPerformed += roundBlocksPerformed;
        totalAttacksBlocked += roundAttacksBlocked;
        totalAttackAttempts += roundAttackAttempts;
        totalWhiffs += roundWhiffs;

        windowHitsDealt += roundHitsDealt;
        windowHitsTaken += roundHitsTaken;
        windowBlocksPerformed += roundBlocksPerformed;
        windowAttacksBlocked += roundAttacksBlocked;
        windowAttackAttempts += roundAttackAttempts;
        windowWhiffs += roundWhiffs;

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
                $"BlocksPerformed={roundBlocksPerformed} | AttacksBlocked={roundAttacksBlocked} | " +
                $"AttackAttempts={roundAttackAttempts} | Whiffs={roundWhiffs}"
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
        }

        if (attacker == trackedFighter)
        {
            roundAttacksBlocked++;
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
        float avgBlocksPerformed = windowRounds > 0 ? (float)windowBlocksPerformed / windowRounds : 0f;
        float avgAttacksBlocked = windowRounds > 0 ? (float)windowAttacksBlocked / windowRounds : 0f;
        float avgAttackAttempts = windowRounds > 0 ? (float)windowAttackAttempts / windowRounds : 0f;
        float avgWhiffs = windowRounds > 0 ? (float)windowWhiffs / windowRounds : 0f;
        float whiffRate = windowAttackAttempts > 0 ? (float)windowWhiffs / windowAttackAttempts : 0f;

        Debug.Log(
            $"[RoundStats Summary] Last {windowRounds} rounds | " +
            $"W={windowWins} L={windowLosses} D={windowDraws} | " +
            $"WinRate={winRate:P1} | AvgDuration={avgDuration:F2}s | " +
            $"AvgHitsDealt={avgHitsDealt:F2} | AvgHitsTaken={avgHitsTaken:F2} | " +
            $"AvgBlocksPerformed={avgBlocksPerformed:F2} | AvgAttacksBlocked={avgAttacksBlocked:F2} | " +
            $"AvgAttackAttempts={avgAttackAttempts:F2} | AvgWhiffs={avgWhiffs:F2} | WhiffRate={whiffRate:P1}"
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
        windowAttacksBlocked = 0;
        windowAttackAttempts = 0;
        windowWhiffs = 0;
    }
}