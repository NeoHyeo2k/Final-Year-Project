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
    public float preferredRangeRewardPerSecond = 0.018f;

    [Tooltip("成功进入理想距离带时，一次性奖励")]
    public float enterPreferredBandReward = 0.02f;

    [Tooltip("每步 spacing reward 的最大绝对值")]
    public float maxSpacingRewardPerStep = 0.02f;

    [Header("Anti Face-Hug")]
    [Tooltip("极近距离持续停留时，每秒惩罚，用于抑制贴脸乱打")]
    public float faceHugPenaltyPerSecond = -0.018f;

    [Tooltip("小于该距离时认为是过度贴脸")]
    public float faceHugDistance = 0.7f;

    [Header("Attack Shaping")]
    [Tooltip("只有在较合理距离发起攻击，才给极小奖励")]
    public float attackAttemptReward = 0.015f;

    [Tooltip("一次攻击结束没命中则判为空挥")]
    public float whiffPenalty = -0.035f;

    [Tooltip("攻击尝试奖励允许的下界容差")]
    public float attackAttemptLowerTolerance = 0.0f;

    [Tooltip("攻击尝试奖励允许的上界容差")]
    public float attackAttemptUpperTolerance = 0.15f;

    [Header("Disengage / Tempo Reset")]
    [Tooltip("受到伤害后，若成功拉开距离，则给予一次奖励")]
    public float disengageReward = 0.08f;

    [Tooltip("从受击瞬间开始，累计拉开至少这么多距离才算成功 reset")]
    public float disengageRequiredDistanceGain = 0.35f;

    [Tooltip("受到伤害后，最多在这个时间窗口内触发 reset reward")]
    public float disengageWindow = 1.2f;

    [Header("Inactivity")]
    public float inactivityThreshold = 1.0f;
    public float inactivityPenaltyInterval = 0.5f;
    public float inactivityPenalty = -0.015f;
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

    private bool disengageTrackingActive = false;
    private float disengageStartDistance = 0f;
    private float disengageTimer = 0f;

    private bool wasInPreferredBandLastFrame = false;
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
        hitConfirmedThisAttack = false;

        disengageTrackingActive = false;
        disengageStartDistance = 0f;
        disengageTimer = 0f;
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

        disengageTrackingActive = false;
        disengageStartDistance = lastDistance;
        disengageTimer = 0f;

        wasInPreferredBandLastFrame =
            lastDistance >= preferredMinDistance &&
            lastDistance <= preferredMaxDistance;

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

        bool tookDamageThisFrame = selfHealth.currentHealth < lastSelfHealth;
        bool dealtDamageThisFrame = opponentHealth.currentHealth < lastOpponentHealth;

        if (tookDamageThisFrame)
        {
            StartDisengageTracking(currentDistance);
        }

        ApplySpacingReward(currentDistance, dt);
        UpdateAttackSequence(currentDistance);
        UpdateDisengageReward(currentDistance, dt, tookDamageThisFrame, dealtDamageThisFrame);
        UpdateInactivity(dt, dealtDamageThisFrame, tookDamageThisFrame);

        wasInPreferredBandLastFrame =
            currentDistance >= preferredMinDistance &&
            currentDistance <= preferredMaxDistance;

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

        if (!wasInPreferredBandLastFrame && inPreferredBand)
        {
            reward += enterPreferredBandReward;
        }

        if (currentDistance < faceHugDistance)
        {
            reward += faceHugPenaltyPerSecond * dt;
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

            bool goodAttackDistance =
                currentDistance >= preferredMinDistance - attackAttemptLowerTolerance &&
                currentDistance <= preferredMaxDistance + attackAttemptUpperTolerance;

            if (goodAttackDistance)
            {
                agent.AddReward(attackAttemptReward);

                if (debugLog)
                {
                    DLog.Log($"{name} attack attempt reward: {attackAttemptReward:F4}, dist={currentDistance:F2}");
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

    private void StartDisengageTracking(float currentDistance)
    {
        disengageTrackingActive = true;
        disengageStartDistance = currentDistance;
        disengageTimer = 0f;

        if (debugLog)
        {
            DLog.Log($"{name} disengage tracking started. startDist={disengageStartDistance:F2}");
        }
    }

    private void UpdateDisengageReward(float currentDistance, float dt, bool tookDamageThisFrame, bool dealtDamageThisFrame)
    {
        if (!disengageTrackingActive)
            return;

        disengageTimer += dt;

        float distanceGain = currentDistance - disengageStartDistance;

        if (distanceGain >= disengageRequiredDistanceGain)
        {
            agent.AddReward(disengageReward);

            if (debugLog)
            {
                DLog.Log($"{name} disengage reward: {disengageReward:F4}, gain={distanceGain:F2}");
            }

            disengageTrackingActive = false;
            return;
        }

        if (dealtDamageThisFrame && !tookDamageThisFrame)
        {
            disengageTrackingActive = false;

            if (debugLog)
            {
                DLog.Log($"{name} disengage tracking cancelled by re-engage hit.");
            }

            return;
        }

        if (disengageTimer >= disengageWindow)
        {
            disengageTrackingActive = false;

            if (debugLog)
            {
                DLog.Log($"{name} disengage window expired.");
            }
        }
    }

    private void UpdateInactivity(float dt, bool dealtDamageThisFrame, bool tookDamageThisFrame)
    {
        bool movedEnough = Vector3.Distance(self.transform.position, lastPosition) > movementEpsilon;
        bool isAttacking = self.IsAttacking;

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