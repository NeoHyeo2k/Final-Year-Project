using UnityEngine;

[RequireComponent(typeof(FTGAgent))]
public class ShapingReward : MonoBehaviour
{
    [Header("References")]
    public FTGAgent agent;
    public FighterController self;
    public FighterController opponent;
    public Health selfHealth;
    public Health opponentHealth;
    public MatchManager matchManager;

    [Header("Spacing")]
    [Tooltip("理想交战距离下界。小于这个值视为贴太近")]
    public float preferredMinDistance = 1.0f;

    [Tooltip("理想交战距离上界。大于这个值视为太远")]
    public float preferredMaxDistance = 2.2f;

    [Tooltip("超过理想最大距离时，缩短距离给予奖励")]
    public float approachRewardScale = 0.012f;

    [Tooltip("超过理想最大距离时，继续远离给予惩罚")]
    public float retreatPenaltyScale = 0.008f;

    [Tooltip("小于理想最小距离时，进一步贴近给予惩罚")]
    public float tooClosePenaltyScale = 0.012f;

    [Tooltip("处于理想距离带时，每秒奖励")]
    public float preferredRangeRewardPerSecond = 0.01f;

    [Tooltip("每步 spacing reward 的最大绝对值")]
    public float maxSpacingRewardPerStep = 0.02f;

    [Header("Attack Shaping")]
    [Tooltip("只有在理想距离附近尝试攻击，才给极小奖励")]
    public float attackAttemptReward = 0.008f;

    [Tooltip("一次攻击结束没命中则判为空挥")]
    public float whiffPenalty = -0.06f;

    [Tooltip("允许攻击尝试奖励的最大距离")]
    public float maxAttackAttemptDistance = 2.4f;

    [Header("Inactivity")]
    public float inactivityThreshold = 1.0f;
    public float inactivityPenaltyInterval = 0.5f;
    public float inactivityPenalty = -0.03f;
    public float movementEpsilon = 0.03f;

    [Header("Debug")]
    public bool debugLog = false;

    private float lastDistance;
    private Vector3 lastPosition;
    private int lastSelfHealth;
    private int lastOpponentHealth;

    private AttackPhase lastAttackPhase = AttackPhase.None;
    private bool inAttackSequence = false;
    private bool hitConfirmedThisAttack = false;

    private float inactivityTimer = 0f;
    private float inactivityPenaltyTimer = 0f;

    private bool initialized = false;

    private void Awake()
    {
        if (agent == null)
            agent = GetComponent<FTGAgent>();

        if (self == null)
            self = GetComponent<FighterController>();
    }

    private void OnEnable()
    {
        SubscribeMatchEvents();
    }

    private void Start()
    {
        SubscribeMatchEvents();
        ResetInternalState();
    }

    private void OnDisable()
    {
        UnsubscribeMatchEvents();
    }

    private void OnDestroy()
    {
        UnsubscribeMatchEvents();
    }

    private void SubscribeMatchEvents()
    {
        if (matchManager != null)
        {
            matchManager.OnRoundStarted -= HandleRoundStarted;
            matchManager.OnRoundStarted += HandleRoundStarted;

            matchManager.OnRoundEnded -= HandleRoundEnded;
            matchManager.OnRoundEnded += HandleRoundEnded;
        }
    }

    private void UnsubscribeMatchEvents()
    {
        if (matchManager != null)
        {
            matchManager.OnRoundStarted -= HandleRoundStarted;
            matchManager.OnRoundEnded -= HandleRoundEnded;
        }
    }

    private void HandleRoundStarted()
    {
        ResetInternalState();
    }

    private void HandleRoundEnded(FighterController winner, FighterController loser, bool draw)
    {
        inAttackSequence = false;
    }

    private void ResetInternalState()
    {
        if (self == null || opponent == null || selfHealth == null || opponentHealth == null)
            return;

        lastDistance = GetHorizontalDistance();
        lastPosition = self.transform.position;
        lastSelfHealth = selfHealth.currentHealth;
        lastOpponentHealth = opponentHealth.currentHealth;

        lastAttackPhase = self.CurrentAttackPhase;
        inAttackSequence = self.IsAttacking;
        hitConfirmedThisAttack = false;

        inactivityTimer = 0f;
        inactivityPenaltyTimer = 0f;

        initialized = true;
    }

    private void Update()
    {
        if (!initialized)
        {
            ResetInternalState();
        }

        if (agent == null || self == null || opponent == null || selfHealth == null || opponentHealth == null)
            return;

        if (matchManager != null && !matchManager.RoundActive)
            return;

        float currentDistance = GetHorizontalDistance();
        float dt = Time.deltaTime;

        ApplySpacingReward(currentDistance, dt);
        UpdateAttackSequence(currentDistance);
        UpdateInactivity(dt);

        lastDistance = currentDistance;
        lastPosition = self.transform.position;
        lastSelfHealth = selfHealth.currentHealth;
        lastOpponentHealth = opponentHealth.currentHealth;
        lastAttackPhase = self.CurrentAttackPhase;
    }

    private float GetHorizontalDistance()
    {
        return Mathf.Abs(opponent.transform.position.x - self.transform.position.x);
    }

    private void ApplySpacingReward(float currentDistance, float dt)
    {
        float reward = 0f;
        float delta = lastDistance - currentDistance;

        bool tooFar = currentDistance > preferredMaxDistance;
        bool tooClose = currentDistance < preferredMinDistance;
        bool inPreferredBand = !tooFar && !tooClose;

        if (tooFar)
        {
            if (delta > 0f)
            {
                reward += delta * approachRewardScale;
            }
            else if (delta < 0f)
            {
                reward += delta * retreatPenaltyScale;
            }
        }
        else if (tooClose)
        {
            if (delta > 0f)
            {
                reward -= Mathf.Abs(delta) * tooClosePenaltyScale;
            }
            else if (delta < 0f)
            {
                reward += Mathf.Abs(delta) * tooClosePenaltyScale * 0.5f;
            }
        }

        if (inPreferredBand)
        {
            reward += preferredRangeRewardPerSecond * dt;
        }

        reward = Mathf.Clamp(reward, -maxSpacingRewardPerStep, maxSpacingRewardPerStep);

        if (Mathf.Abs(reward) > 0f)
        {
            agent.AddReward(reward);

            if (debugLog)
            {
                DLog.Log($"{name} spacing reward: {reward:F4}, dist={currentDistance:F2}");
            }
        }
    }

    private void UpdateAttackSequence(float currentDistance)
    {
        AttackPhase currentPhase = self.CurrentAttackPhase;

        bool attackStartedThisFrame =
            !inAttackSequence &&
            self.IsAttacking &&
            currentPhase != AttackPhase.None;

        if (attackStartedThisFrame)
        {
            inAttackSequence = true;
            hitConfirmedThisAttack = false;

            if (currentDistance >= preferredMinDistance * 0.8f &&
                currentDistance <= maxAttackAttemptDistance)
            {
                agent.AddReward(attackAttemptReward);

                if (debugLog)
                {
                    DLog.Log($"{name} attack attempt reward: {attackAttemptReward:F4}");
                }
            }
        }

        if (inAttackSequence)
        {
            if (opponentHealth.currentHealth < lastOpponentHealth)
            {
                hitConfirmedThisAttack = true;

                if (debugLog)
                {
                    DLog.Log($"{name} hit confirmed.");
                }
            }

            bool attackEndedThisFrame =
                !self.IsAttacking &&
                lastAttackPhase != AttackPhase.None &&
                currentPhase == AttackPhase.None;

            if (attackEndedThisFrame)
            {
                if (!hitConfirmedThisAttack)
                {
                    agent.AddReward(whiffPenalty);

                    if (debugLog)
                    {
                        DLog.Log($"{name} whiff penalty: {whiffPenalty:F4}");
                    }
                }

                inAttackSequence = false;
                hitConfirmedThisAttack = false;
            }
        }
    }

    private void UpdateInactivity(float dt)
    {
        bool movedEnough = Vector3.Distance(self.transform.position, lastPosition) > movementEpsilon;
        bool isAttacking = self.IsAttacking;
        bool dealtDamageThisFrame = opponentHealth.currentHealth < lastOpponentHealth;
        bool tookDamageThisFrame = selfHealth.currentHealth < lastSelfHealth;

        bool meaningfulInteraction =
            movedEnough ||
            isAttacking ||
            dealtDamageThisFrame ||
            tookDamageThisFrame;

        if (meaningfulInteraction)
        {
            inactivityTimer = 0f;
            inactivityPenaltyTimer = 0f;
            return;
        }

        inactivityTimer += dt;

        if (inactivityTimer >= inactivityThreshold)
        {
            inactivityPenaltyTimer += dt;

            if (inactivityPenaltyTimer >= inactivityPenaltyInterval)
            {
                agent.AddReward(inactivityPenalty);
                inactivityPenaltyTimer = 0f;

                if (debugLog)
                {
                    DLog.Log($"{name} inactivity penalty: {inactivityPenalty:F4}");
                }
            }
        }
    }
}