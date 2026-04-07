using UnityEngine;

public class FighterController : MonoBehaviour
{
    [Header("Movement")]
    public float moveSpeed = 5f;
    public float jumpForce = 8f;

    [Header("Jump")]
    public int maxJumpCount = 2;

    [Header("Dash")]
    public float doubleTapWindow = 0.25f;
    public float dashSpeed = 12f;
    public float dashDuration = 0.15f;

    [Header("Air Dash")]
    public bool allowAirDash = true;
    public float airDashWindow = 0.15f;
    public int maxAirDashCount = 1;

    [Header("Crouch")]
    [Range(0.1f, 1f)]
    public float crouchHeightMultiplier = 0.5f;

    [Header("Attacks")]
    public AttackData lightAttack;
    public AttackData heavyAttack;

    [Header("Cancel Settings")]
    public bool allowLightAttackCancel = true;

    [Header("Block")]
    public float defaultBlockstunDuration = 0.2f;
    public float blockPushMultiplier = 0.35f;

    [Header("Input Buffer")]
    public bool enableInputBuffer = true;
    public float inputBufferTime = 0.15f;

    [Header("Ground Check")]
    public Transform groundCheck;
    public float groundCheckRadius = 0.2f;
    public LayerMask groundLayer;

    [Header("Modules")]
    public CharacterMotor motor;
    public CharacterStateMachine stateMachine;
    public CharacterCombat combat;
    public HurtboxManager hurtboxManager;

    private Vector3 originalScale;
    private CharacterState lastState;
    private AttackPhase lastAttackPhase;

    public string attackName;

    public bool FacingRight => motor != null && motor.FacingRight;
    public bool IsGrounded => motor != null && motor.IsGrounded;
    public bool IsDashing => motor != null && motor.IsDashing;

    public bool IsCrouching => stateMachine != null && stateMachine.IsCrouching;
    public bool IsInHitstun => stateMachine != null && stateMachine.IsInHitstun;
    public bool IsInBlockstun => stateMachine != null && stateMachine.IsInBlockstun;
    public bool IsStandingBlocking => stateMachine != null && stateMachine.IsStandingBlocking;
    public bool IsCrouchBlocking => stateMachine != null && stateMachine.IsCrouchBlocking;
    public bool IsBlocking => stateMachine != null && stateMachine.IsBlocking;

    public bool IsAttacking => combat != null && combat.IsAttacking;
    public CharacterState CurrentState => stateMachine != null ? stateMachine.CurrentState : CharacterState.Idle;
    public AttackPhase CurrentAttackPhase => combat != null ? combat.CurrentAttackPhase : AttackPhase.None;
    public AttackData CurrentAttackData => combat != null ? combat.CurrentAttackData : null;

    public BufferedAction CurrentBufferedAction => combat != null ? combat.CurrentBufferedAction : BufferedAction.None;
    public bool HasBufferedAction => combat != null && combat.HasBufferedAction;

    public int comboCount => combat != null ? combat.comboCount : 0;
    public Vector2 Velocity => motor != null ? motor.Velocity : Vector2.zero;

    void Reset()
    {
        motor = GetComponent<CharacterMotor>();
        stateMachine = GetComponent<CharacterStateMachine>();
        combat = GetComponent<CharacterCombat>();
        hurtboxManager = GetComponent<HurtboxManager>();
    }

    void Awake()
    {
        if (motor == null) motor = GetComponent<CharacterMotor>();
        if (stateMachine == null) stateMachine = GetComponent<CharacterStateMachine>();
        if (combat == null) combat = GetComponent<CharacterCombat>();
        if (hurtboxManager == null) hurtboxManager = GetComponent<HurtboxManager>();

        if (motor != null) motor.Initialize(this);
        if (stateMachine != null) stateMachine.Initialize(this);
        if (combat != null) combat.Initialize(this);
        if (hurtboxManager != null) hurtboxManager.Initialize(this);

        originalScale = transform.localScale;
    }

    void Update()
    {
        if (motor == null || stateMachine == null || combat == null)
            return;

        motor.Tick();
        stateMachine.TickTimers();
        combat.Tick();

        if (hurtboxManager != null)
        {
            bool footOnly = IsCrouching || IsCrouchBlocking;
            hurtboxManager.UpdateHurtboxes(footOnly);
        }

        UpdateCrouchVisual();
        stateMachine.UpdateState();

        if (CurrentState != lastState)
        {
            DLog.Log("State: " + CurrentState);
            lastState = CurrentState;
        }

        if (CurrentAttackPhase != lastAttackPhase)
        {
            DLog.Log("Attack Phase: " + CurrentAttackPhase);
            lastAttackPhase = CurrentAttackPhase;
        }
    }

    void UpdateCrouchVisual()
    {
        Vector3 targetScale = originalScale;

        if (IsCrouching || IsCrouchBlocking)
        {
            targetScale.y = originalScale.y * crouchHeightMultiplier;
        }

        transform.localScale = targetScale;
    }

    public bool CanMove() => stateMachine != null && stateMachine.CanMove();
    public bool CanJump() => stateMachine != null && stateMachine.CanJump();
    public bool CanCrouch() => stateMachine != null && stateMachine.CanCrouch();
    public bool CanAttack() => stateMachine != null && stateMachine.CanAttack();

    public void Move(float direction)
    {
        if (motor != null)
            motor.Move(direction);
    }

    public void Jump()
    {
        if (motor == null || combat == null) return;

        if (CanJump())
        {
            motor.TryJumpImmediate();
        }
        else
        {
            combat.BufferAction(BufferedAction.Jump);
        }
    }

    public void SetCrouch(bool crouch)
    {
        if (stateMachine != null)
            stateMachine.SetCrouch(crouch);
    }

    public void SetBlock(bool block, bool crouchHeld)
    {
        if (stateMachine != null)
            stateMachine.SetBlock(block, crouchHeld);
    }

    public void StartAttack(AttackData attackData)
    {
        if (combat != null){
            DLog.Log($"{name} StartAttack: {attackData.attackName}");
            combat.StartAttack(attackData);
        }
    }

    public void RequestLightAttack()
    {
        DLog.Log($"{name} RequestLightAttack received by FighterController");
        if (combat != null)
            combat.RequestLightAttack();
    }

    public void RequestHeavyAttack()
    {
        DLog.Log($"{name} RequestHeavyAttack received by FighterController");
        if (combat != null)
            combat.RequestHeavyAttack();
    }

    public void ReceiveHitstun(float duration)
    {
        if (stateMachine != null)
            stateMachine.ReceiveHitstun(duration);
    }

    public void ReceiveBlockstun(float duration)
    {
        if (stateMachine != null)
            stateMachine.ReceiveBlockstun(duration);
    }

    public void RegisterHit()
    {
        if (combat != null)
            combat.RegisterHit();
    }

    public void NotifyAttackHit()
    {
        if (combat != null)
            combat.NotifyAttackHit();
    }

    public void ApplyKnockback(float force, Transform attacker)
    {
        if (motor != null)
            motor.ApplyKnockback(force, attacker);
    }

    public void ApplyBlockPush(float attackPushForce, Transform attacker)
    {
        if (motor != null)
            motor.ApplyBlockPush(attackPushForce, attacker);
    }

    public bool CanBlockAttack(AttackData attackData)
    {
        return stateMachine != null && stateMachine.CanBlockAttack(attackData);
    }

    public Rigidbody2D GetRigidbody()
    {
        return motor != null ? motor.Rigidbody : null;
    }

    public void ResetFighter(Vector3 worldPosition, bool faceRight)
    {
        transform.position = worldPosition;

        if (motor != null)
            motor.ResetMotor(faceRight);

        if (stateMachine != null)
            stateMachine.ResetStateMachine();

        if (combat != null)
            combat.ResetCombat();

        transform.localScale = originalScale;
    }
}