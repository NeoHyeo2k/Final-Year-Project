using UnityEngine;

public enum CharacterState
{
    Idle,
    Move,
    Jump,
    Fall,
    Dash,
    Crouch,
    Attack,
    Hitstun,
    Block,
    CrouchBlock,
    Blockstun
}

public enum AttackPhase
{
    None,
    Startup,
    Active,
    Recovery
}

public enum BufferedAction
{
    None,
    Jump,
    LightAttack,
    HeavyAttack
}

public class CharacterController2D : MonoBehaviour
{
    [Header("Movement")]
    public float moveSpeed = 5f;
    public float jumpForce = 8f;

    [Header("Jump")]
    public int maxJumpCount = 2;
    private int currentJumpCount = 0;

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

    [Header("Hurtboxes")]
    public HurtboxController headHurtbox;
    public HurtboxController bodyHurtbox;
    public HurtboxController footHurtbox;

    [Header("Combo")]
    public int comboCount = 0;
    public float comboResetTime = 1.0f;
    private float lastHitTime = -999f;

    private Rigidbody2D rb;

    private bool isGrounded;
    private bool wasGrounded;

    public bool FacingRight { get; private set; } = true;
    public bool IsGrounded => isGrounded;
    public bool IsDashing => isDashing;
    public bool IsCrouching => isCrouching;
    public bool IsAttacking => isAttacking;
    public bool IsInHitstun => hitstunTimer > 0f;
    public bool IsInBlockstun => blockstunTimer > 0f;

    public bool IsStandingBlocking => isBlocking && !isCrouchBlocking && !IsInHitstun;
    public bool IsCrouchBlocking => isBlocking && isCrouchBlocking && !IsInHitstun;
    public bool IsBlocking => isBlocking && !IsInHitstun;

    public BufferedAction CurrentBufferedAction => bufferedAction;
    public bool HasBufferedAction => bufferedAction != BufferedAction.None;

    public CharacterState CurrentState { get; private set; } = CharacterState.Idle;
    public AttackPhase CurrentAttackPhase { get; private set; } = AttackPhase.None;
    public AttackData CurrentAttackData { get; private set; }

    private bool isDashing = false;
    private float dashTimer = 0f;
    private float dashDirection = 0f;

    private float lastLeftTapTime = -999f;
    private float lastRightTapTime = -999f;
    private int previousInputSign = 0;

    private float timeSinceJumpStart = 999f;
    private int currentAirDashCount = 0;

    private bool isCrouching = false;
    private bool isBlocking = false;
    private bool isCrouchBlocking = false;

    private Vector3 originalScale;

    private CharacterState lastState;
    private AttackPhase lastAttackPhase;

    private bool isAttacking = false;
    private float attackTimer = 0f;

    private float hitstunTimer = 0f;
    private float blockstunTimer = 0f;

    private BufferedAction bufferedAction = BufferedAction.None;
    private float bufferedActionTimer = 0f;

    private bool currentAttackHasHit = false;
    private bool cancelUsedThisAttack = false;

    void Start()
    {
        rb = GetComponent<Rigidbody2D>();
        originalScale = transform.localScale;
    }

    void Update()
    {
        CheckGround();
        UpdateTimers();
        HandleLandingReset();
        UpdateHitstun();
        UpdateBlockstun();
        UpdateDash();
        UpdateAttack();
        UpdateCombo();
        UpdateInputBuffer();
        UpdateHurtboxes();
        UpdateCrouchVisual();
        UpdateState();

        if (CurrentState != lastState)
        {
            Debug.Log("State: " + CurrentState);
            lastState = CurrentState;
        }

        if (CurrentAttackPhase != lastAttackPhase)
        {
            Debug.Log("Attack Phase: " + CurrentAttackPhase);
            lastAttackPhase = CurrentAttackPhase;
        }
    }

    void CheckGround()
    {
        wasGrounded = isGrounded;

        isGrounded = Physics2D.OverlapCircle(
            groundCheck.position,
            groundCheckRadius,
            groundLayer
        );
    }

    void UpdateTimers()
    {
        if (!isGrounded)
        {
            timeSinceJumpStart += Time.deltaTime;
        }
        else
        {
            timeSinceJumpStart = 999f;
        }
    }

    void HandleLandingReset()
    {
        if (isGrounded && !wasGrounded)
        {
            currentJumpCount = 0;
            currentAirDashCount = 0;
        }
    }

    void UpdateHitstun()
    {
        if (hitstunTimer <= 0f) return;

        hitstunTimer -= Time.deltaTime;

        if (hitstunTimer < 0f)
        {
            hitstunTimer = 0f;
        }
    }

    void UpdateBlockstun()
    {
        if (blockstunTimer <= 0f) return;

        blockstunTimer -= Time.deltaTime;

        if (blockstunTimer < 0f)
        {
            blockstunTimer = 0f;
        }
    }

    void UpdateDash()
    {
        if (!isDashing) return;

        dashTimer -= Time.deltaTime;

        if (dashTimer > 0f)
        {
            rb.linearVelocity = new Vector2(dashDirection * dashSpeed, rb.linearVelocity.y);
        }
        else
        {
            isDashing = false;
        }
    }

    void UpdateAttack()
    {
        if (!isAttacking || CurrentAttackData == null) return;

        attackTimer -= Time.deltaTime;

        if (attackTimer > 0f) return;

        switch (CurrentAttackPhase)
        {
            case AttackPhase.Startup:
                CurrentAttackPhase = AttackPhase.Active;
                attackTimer = CurrentAttackData.activeTime;
                break;

            case AttackPhase.Active:
                CurrentAttackPhase = AttackPhase.Recovery;
                attackTimer = CurrentAttackData.recoveryTime;
                break;

            case AttackPhase.Recovery:
                EndAttack();
                break;
        }
    }

    void UpdateCombo()
    {
        if (comboCount > 0 && Time.time - lastHitTime > comboResetTime)
        {
            comboCount = 0;
        }
    }

    void UpdateInputBuffer()
    {
        if (!enableInputBuffer) return;
        if (bufferedAction == BufferedAction.None) return;

        bufferedActionTimer -= Time.deltaTime;

        if (TryExecuteBufferedAction())
        {
            ClearBufferedAction();
            return;
        }

        if (bufferedActionTimer <= 0f)
        {
            ClearBufferedAction();
        }
    }

    void UpdateHurtboxes()
    {
        bool footOnly = isCrouching || isCrouchBlocking;

        if (headHurtbox != null)
            headHurtbox.SetHurtboxEnabled(!footOnly);

        if (bodyHurtbox != null)
            bodyHurtbox.SetHurtboxEnabled(!footOnly);

        if (footHurtbox != null)
            footHurtbox.SetHurtboxEnabled(true);
    }

    void UpdateCrouchVisual()
    {
        Vector3 targetScale = originalScale;

        if (isCrouching || isCrouchBlocking)
        {
            targetScale.y = originalScale.y * crouchHeightMultiplier;
        }

        transform.localScale = targetScale;
    }

    void UpdateState()
    {
        if (IsInHitstun)
        {
            CurrentState = CharacterState.Hitstun;
            return;
        }

        if (IsInBlockstun)
        {
            CurrentState = CharacterState.Blockstun;
            return;
        }

        if (isAttacking)
        {
            CurrentState = CharacterState.Attack;
            return;
        }

        if (IsCrouchBlocking)
        {
            CurrentState = CharacterState.CrouchBlock;
            return;
        }

        if (IsStandingBlocking)
        {
            CurrentState = CharacterState.Block;
            return;
        }

        if (isDashing)
        {
            CurrentState = CharacterState.Dash;
            return;
        }

        if (isCrouching && isGrounded)
        {
            CurrentState = CharacterState.Crouch;
            return;
        }

        if (!isGrounded)
        {
            if (rb.linearVelocity.y > 0.1f)
            {
                CurrentState = CharacterState.Jump;
            }
            else
            {
                CurrentState = CharacterState.Fall;
            }

            return;
        }

        if (Mathf.Abs(rb.linearVelocity.x) > 0.1f)
        {
            CurrentState = CharacterState.Move;
        }
        else
        {
            CurrentState = CharacterState.Idle;
        }
    }

    void BufferAction(BufferedAction action)
    {
        if (!enableInputBuffer) return;
        if (action == BufferedAction.None) return;

        bufferedAction = action;
        bufferedActionTimer = inputBufferTime;

        Debug.Log(gameObject.name + " buffered action: " + bufferedAction);
    }

    void ClearBufferedAction()
    {
        bufferedAction = BufferedAction.None;
        bufferedActionTimer = 0f;
    }

    bool TryExecuteBufferedAction()
    {
        switch (bufferedAction)
        {
            case BufferedAction.Jump:
                if (CanJump())
                {
                    DoJump();
                    return true;
                }
                break;

            case BufferedAction.LightAttack:
                if (CanAttack())
                {
                    StartAttack(lightAttack);
                    return true;
                }
                break;

            case BufferedAction.HeavyAttack:
                if (CanAttack())
                {
                    StartAttack(heavyAttack);
                    return true;
                }
                break;
        }

        return false;
    }

    public bool CanMove()
    {
        return CurrentState != CharacterState.Dash
            && CurrentState != CharacterState.Attack
            && CurrentState != CharacterState.Hitstun
            && CurrentState != CharacterState.Block
            && CurrentState != CharacterState.CrouchBlock
            && CurrentState != CharacterState.Blockstun;
    }

    public bool CanJump()
    {
        return CurrentState != CharacterState.Dash
            && CurrentState != CharacterState.Attack
            && CurrentState != CharacterState.Hitstun
            && CurrentState != CharacterState.Block
            && CurrentState != CharacterState.CrouchBlock
            && CurrentState != CharacterState.Blockstun;
    }

    public bool CanCrouch()
    {
        return CurrentState != CharacterState.Dash
            && CurrentState != CharacterState.Attack
            && CurrentState != CharacterState.Hitstun
            && CurrentState != CharacterState.Block
            && CurrentState != CharacterState.CrouchBlock
            && CurrentState != CharacterState.Blockstun;
    }

    public bool CanAttack()
    {
        return !isAttacking
            && !isDashing
            && CurrentState != CharacterState.Hitstun
            && CurrentState != CharacterState.Block
            && CurrentState != CharacterState.CrouchBlock
            && CurrentState != CharacterState.Blockstun;
    }

    public void Move(float direction)
    {
        if (!CanMove())
        {
            int blockedInputSign = 0;
            if (direction > 0.1f) blockedInputSign = 1;
            else if (direction < -0.1f) blockedInputSign = -1;

            previousInputSign = blockedInputSign;
            return;
        }

        int inputSign = 0;

        if (direction > 0.1f) inputSign = 1;
        else if (direction < -0.1f) inputSign = -1;

        DetectDoubleTap(inputSign);

        if (isDashing)
        {
            previousInputSign = inputSign;
            return;
        }

        rb.linearVelocity = new Vector2(direction * moveSpeed, rb.linearVelocity.y);

        if (direction > 0)
        {
            FacingRight = true;
        }
        else if (direction < 0)
        {
            FacingRight = false;
        }

        previousInputSign = inputSign;
    }

    void DetectDoubleTap(int inputSign)
    {
        bool isFreshTap = (inputSign != 0 && previousInputSign == 0);

        if (!isFreshTap) return;

        if (inputSign == -1)
        {
            if (Time.time - lastLeftTapTime <= doubleTapWindow)
            {
                TryStartDash(-1f);
            }
            lastLeftTapTime = Time.time;
        }
        else if (inputSign == 1)
        {
            if (Time.time - lastRightTapTime <= doubleTapWindow)
            {
                TryStartDash(1f);
            }
            lastRightTapTime = Time.time;
        }
    }

    void TryStartDash(float direction)
    {
        if (isDashing || isAttacking || IsInHitstun || IsInBlockstun || isBlocking) return;

        if (isGrounded)
        {
            StartDash(direction);
            return;
        }

        if (allowAirDash && timeSinceJumpStart <= airDashWindow && currentAirDashCount < maxAirDashCount)
        {
            StartDash(direction);
            currentAirDashCount++;
        }
    }

    void StartDash(float direction)
    {
        isDashing = true;
        dashTimer = dashDuration;
        dashDirection = direction;

        FacingRight = direction > 0;
        rb.linearVelocity = new Vector2(dashDirection * dashSpeed, rb.linearVelocity.y);
    }

    public void Jump()
    {
        if (CanJump())
        {
            DoJump();
        }
        else
        {
            BufferAction(BufferedAction.Jump);
        }
    }

    void DoJump()
    {
        if (currentJumpCount >= maxJumpCount) return;

        currentJumpCount++;

        rb.linearVelocity = new Vector2(rb.linearVelocity.x, jumpForce);

        isGrounded = false;
        timeSinceJumpStart = 0f;
        isCrouching = false;
        isBlocking = false;
        isCrouchBlocking = false;

        ClearBufferedAction();
    }

    public void SetCrouch(bool crouch)
    {
        if (isBlocking) return;

        if (!CanCrouch())
        {
            isCrouching = false;
            return;
        }

        isCrouching = crouch && isGrounded && !isDashing;
    }

    public void SetBlock(bool block, bool crouchHeld)
    {
        if (IsInHitstun || IsInBlockstun)
        {
            return;
        }

        if (!isGrounded)
        {
            isBlocking = false;
            isCrouchBlocking = false;
            return;
        }

        if (isAttacking || isDashing)
        {
            isBlocking = false;
            isCrouchBlocking = false;
            return;
        }

        if (!block)
        {
            isBlocking = false;
            isCrouchBlocking = false;
            return;
        }

        isBlocking = true;
        isCrouchBlocking = crouchHeld;
        isCrouching = false;
    }

    public void StartAttack(AttackData attackData)
    {
        if (!CanAttack()) return;
        if (attackData == null) return;

        isAttacking = true;
        isCrouching = false;
        isBlocking = false;
        isCrouchBlocking = false;
        CurrentAttackData = attackData;

        CurrentAttackPhase = AttackPhase.Startup;
        attackTimer = CurrentAttackData.startupTime;

        currentAttackHasHit = false;
        cancelUsedThisAttack = false;

        rb.linearVelocity = new Vector2(0f, rb.linearVelocity.y);

        ClearBufferedAction();

        Debug.Log("Start Attack: " + CurrentAttackData.attackName + " [" + CurrentAttackData.attackType + "]");
    }

    void EndAttack()
    {
        isAttacking = false;
        CurrentAttackPhase = AttackPhase.None;
        attackTimer = 0f;
        CurrentAttackData = null;

        currentAttackHasHit = false;
        cancelUsedThisAttack = false;
    }

    public void ReceiveHitstun(float duration)
    {
        if (duration <= 0f) return;

        EndAttack();

        isDashing = false;
        dashTimer = 0f;
        isCrouching = false;
        isBlocking = false;
        isCrouchBlocking = false;
        blockstunTimer = 0f;

        hitstunTimer = duration;

        Debug.Log(gameObject.name + " entered Hitstun for " + duration + " seconds.");
    }

    public void ReceiveBlockstun(float duration)
    {
        if (duration <= 0f)
        {
            duration = defaultBlockstunDuration;
        }

        EndAttack();

        isDashing = false;
        dashTimer = 0f;
        isCrouching = false;

        blockstunTimer = duration;

        Debug.Log(gameObject.name + " entered Blockstun for " + duration + " seconds.");
    }

    public void RegisterHit()
    {
        float currentTime = Time.time;

        if (currentTime - lastHitTime <= comboResetTime)
        {
            comboCount++;
        }
        else
        {
            comboCount = 1;
        }

        lastHitTime = currentTime;

        Debug.Log(gameObject.name + " combo count = " + comboCount);
    }

    public void ApplyKnockback(float force, Transform attacker)
    {
        if (force <= 0f || attacker == null) return;

        float direction = transform.position.x >= attacker.position.x ? 1f : -1f;
        rb.linearVelocity = new Vector2(direction * force, rb.linearVelocity.y);

        if (direction > 0f)
        {
            FacingRight = true;
        }
        else
        {
            FacingRight = false;
        }
    }

    public void ApplyBlockPush(float attackPushForce, Transform attacker)
    {
        if (attacker == null) return;

        float finalPush = attackPushForce * blockPushMultiplier;
        if (finalPush <= 0f)
        {
            finalPush = 1.5f;
        }

        float direction = transform.position.x >= attacker.position.x ? 1f : -1f;
        rb.linearVelocity = new Vector2(direction * finalPush, rb.linearVelocity.y);

        if (direction > 0f)
        {
            FacingRight = true;
        }
        else
        {
            FacingRight = false;
        }
    }

    public bool CanBlockAttack(AttackData attackData)
    {
        if (attackData == null) return false;
        if (!IsBlocking) return false;
        if (IsInHitstun) return false;
        if (!isGrounded) return false;

        switch (attackData.attackType)
        {
            case AttackType.Mid:
                return IsStandingBlocking || IsCrouchBlocking;

            case AttackType.Low:
                return IsCrouchBlocking;

            case AttackType.Overhead:
                return IsStandingBlocking;
        }

        return false;
    }

    public void NotifyAttackHit()
    {
        if (!isAttacking) return;
        if (CurrentAttackData == null) return;

        currentAttackHasHit = true;
    }

    public bool TryCancelInto(AttackData nextAttack)
    {
        if (!allowLightAttackCancel) return false;
        if (!isAttacking) return false;
        if (CurrentAttackData == null) return false;
        if (nextAttack == null) return false;

        if (CurrentAttackData != lightAttack) return false;
        if (!currentAttackHasHit) return false;
        if (CurrentAttackPhase != AttackPhase.Recovery) return false;
        if (cancelUsedThisAttack) return false;
        if (nextAttack != lightAttack && nextAttack != heavyAttack) return false;

        cancelUsedThisAttack = true;

        isAttacking = true;
        isCrouching = false;
        isBlocking = false;
        isCrouchBlocking = false;
        CurrentAttackData = nextAttack;

        CurrentAttackPhase = AttackPhase.Startup;
        attackTimer = CurrentAttackData.startupTime;

        currentAttackHasHit = false;
        cancelUsedThisAttack = false;

        rb.linearVelocity = new Vector2(0f, rb.linearVelocity.y);

        ClearBufferedAction();

        Debug.Log("Cancel into: " + CurrentAttackData.attackName);
        return true;
    }

    public void RequestLightAttack()
    {
        if (TryCancelInto(lightAttack)) return;

        if (CanAttack())
        {
            StartAttack(lightAttack);
        }
        else
        {
            BufferAction(BufferedAction.LightAttack);
        }
    }

    public void RequestHeavyAttack()
    {
        if (TryCancelInto(heavyAttack)) return;

        if (CanAttack())
        {
            StartAttack(heavyAttack);
        }
        else
        {
            BufferAction(BufferedAction.HeavyAttack);
        }
    }

    void OnDrawGizmosSelected()
    {
        if (groundCheck == null) return;

        Gizmos.color = Color.green;
        Gizmos.DrawWireSphere(groundCheck.position, groundCheckRadius);
    }
}