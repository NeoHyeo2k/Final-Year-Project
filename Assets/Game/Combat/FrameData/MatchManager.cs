using System;
using UnityEngine;

public class MatchManager : MonoBehaviour
{
    [Header("Fighters")]
    public FighterController fighterA;
    public FighterController fighterB;

    [Header("Health")]
    public Health healthA;
    public Health healthB;

    [Header("Spawn Points")]
    public Transform spawnA;
    public Transform spawnB;

    [Header("Round Settings")]
    public float roundDuration = 30f;
    public bool autoRestartRound = true;
    public float restartDelay = 2f;

    [Header("Debug")]
    public bool logRoundEvents = true;

    public float RoundTimeRemaining { get; private set; }
    public bool RoundActive { get; private set; }
    public bool IsDraw { get; private set; }

    public FighterController Winner { get; private set; }
    public FighterController Loser { get; private set; }

    public event Action OnRoundStarted;
    public event Action<FighterController, FighterController, bool> OnRoundEnded;

    private float restartTimer = 0f;

    void Start()
    {
        StartRound();
    }

    void Update()
    {
        if (RoundActive)
        {
            RoundTimeRemaining -= Time.deltaTime;
            if (RoundTimeRemaining < 0f)
            {
                RoundTimeRemaining = 0f;
            }

            CheckWinCondition();
        }
        else if (autoRestartRound)
        {
            restartTimer -= Time.deltaTime;
            if (restartTimer <= 0f)
            {
                StartRound();
            }
        }
    }

    public void StartRound()
    {
        if (fighterA == null || fighterB == null || healthA == null || healthB == null || spawnA == null || spawnB == null)
        {
            DLog.LogError("MatchManager is missing references.");
            return;
        }

        Winner = null;
        Loser = null;
        IsDraw = false;

        healthA.ResetHealth();
        healthB.ResetHealth();

        fighterA.ResetFighter(spawnA.position, true);
        fighterB.ResetFighter(spawnB.position, false);

        RoundTimeRemaining = roundDuration;
        RoundActive = true;
        restartTimer = 0f;

        if (logRoundEvents)
        {
            DLog.Log("Round Start");
        }

        OnRoundStarted?.Invoke();
    }

    void CheckWinCondition()
    {
        if (!RoundActive) return;

        bool aDead = healthA.IsDead;
        bool bDead = healthB.IsDead;

        if (aDead && bDead)
        {
            EndRound(null, null, true);
            return;
        }

        if (aDead)
        {
            EndRound(fighterB, fighterA, false);
            return;
        }

        if (bDead)
        {
            EndRound(fighterA, fighterB, false);
            return;
        }

        if (RoundTimeRemaining <= 0f)
        {
            if (healthA.currentHealth > healthB.currentHealth)
            {
                EndRound(fighterA, fighterB, false);
            }
            else if (healthB.currentHealth > healthA.currentHealth)
            {
                EndRound(fighterB, fighterA, false);
            }
            else
            {
                EndRound(null, null, true);
            }
        }
    }

    void EndRound(FighterController winner, FighterController loser, bool draw)
    {
        if (!RoundActive) return;

        RoundActive = false;
        Winner = winner;
        Loser = loser;
        IsDraw = draw;
        restartTimer = restartDelay;

        if (logRoundEvents)
        {
            if (draw)
            {
                DLog.Log("Round End: Draw");
            }
            else if (winner != null && loser != null)
            {
                DLog.Log("Round End: Winner = " + winner.gameObject.name + ", Loser = " + loser.gameObject.name);
            }
        }

        OnRoundEnded?.Invoke(winner, loser, draw);
    }
}