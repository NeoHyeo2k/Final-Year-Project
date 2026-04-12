using UnityEngine;

[RequireComponent(typeof(FTGAgent))]
public class ShapingReward : MonoBehaviour
{
    [Header("References")]
    public FTGAgent agent;
    public FighterController self;
    public MatchManager matchManager;

    [Header("Policy Reward")]
    [Tooltip("Policy-level guidance reward for a successful defensive block.")]
    public float successfulBlockReward = 0.10f;

    [Tooltip("If true, only reward blocks while the round is active.")]
    public bool requireRoundActive = true;

    [Header("Debug")]
    public bool debugLog = false;

    private void Awake()
    {
        if (agent == null)
            agent = GetComponent<FTGAgent>();

        if (self == null)
            self = GetComponent<FighterController>();
    }

    private void OnEnable()
    {
        HitboxController.OnGlobalAttackBlocked -= HandleGlobalAttackBlocked;
        HitboxController.OnGlobalAttackBlocked += HandleGlobalAttackBlocked;
    }

    private void OnDisable()
    {
        HitboxController.OnGlobalAttackBlocked -= HandleGlobalAttackBlocked;
    }

    private void OnDestroy()
    {
        HitboxController.OnGlobalAttackBlocked -= HandleGlobalAttackBlocked;
    }

    private void HandleGlobalAttackBlocked(FighterController attacker, FighterController defender, AttackData attackData)
    {
        if (agent == null || self == null)
            return;

        if (defender != self)
            return;

        if (requireRoundActive && matchManager != null && !matchManager.RoundActive)
            return;

        agent.AddReward(successfulBlockReward);

        if (debugLog)
        {
            string attackName = attackData != null ? attackData.attackName : "UnknownAttack";
            DLog.Log($"{name} successful block reward: {successfulBlockReward:F4}, blocked={attackName}");
        }
    }
}
