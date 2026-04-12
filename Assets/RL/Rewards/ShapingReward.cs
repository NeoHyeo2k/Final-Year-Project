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
    public float successfulBlockReward = 0.05f;

    [Tooltip("Additional reward when the agent lands a hit shortly after a successful block.")]
    public float postBlockPunishReward = 0.10f;

    [Tooltip("Time window after a successful block where a hit counts as a post-block punish.")]
    public float postBlockPunishWindow = 0.5f;

    [Tooltip("If true, only reward blocks while the round is active.")]
    public bool requireRoundActive = true;

    [Header("Debug")]
    public bool debugLog = false;

    private bool postBlockPunishWindowActive = false;
    private float postBlockPunishTimer = 0f;

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

        HitboxController.OnGlobalAttackHit -= HandleGlobalAttackHit;
        HitboxController.OnGlobalAttackHit += HandleGlobalAttackHit;
    }

    private void OnDisable()
    {
        HitboxController.OnGlobalAttackBlocked -= HandleGlobalAttackBlocked;
        HitboxController.OnGlobalAttackHit -= HandleGlobalAttackHit;
    }

    private void OnDestroy()
    {
        HitboxController.OnGlobalAttackBlocked -= HandleGlobalAttackBlocked;
        HitboxController.OnGlobalAttackHit -= HandleGlobalAttackHit;
    }

    private void Update()
    {
        if (!postBlockPunishWindowActive)
            return;

        if (requireRoundActive && matchManager != null && !matchManager.RoundActive)
        {
            ClearPostBlockPunishWindow();
            return;
        }

        postBlockPunishTimer -= Time.deltaTime;

        if (postBlockPunishTimer <= 0f)
        {
            ClearPostBlockPunishWindow();
        }
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
        postBlockPunishWindowActive = true;
        postBlockPunishTimer = postBlockPunishWindow;

        if (debugLog)
        {
            string attackName = attackData != null ? attackData.attackName : "UnknownAttack";
            DLog.Log($"{name} successful block reward: {successfulBlockReward:F4}, blocked={attackName}");
        }
    }

    private void HandleGlobalAttackHit(FighterController attacker, FighterController defender, AttackData attackData, int damage)
    {
        if (agent == null || self == null)
            return;

        if (!postBlockPunishWindowActive)
            return;

        if (attacker != self)
            return;

        if (requireRoundActive && matchManager != null && !matchManager.RoundActive)
            return;

        agent.AddReward(postBlockPunishReward);

        if (debugLog)
        {
            string attackName = attackData != null ? attackData.attackName : "UnknownAttack";
            DLog.Log($"{name} post-block punish reward: {postBlockPunishReward:F4}, hit={attackName}");
        }

        ClearPostBlockPunishWindow();
    }

    private void ClearPostBlockPunishWindow()
    {
        postBlockPunishWindowActive = false;
        postBlockPunishTimer = 0f;
    }
}
