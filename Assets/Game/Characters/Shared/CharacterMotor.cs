using UnityEngine;

public class CharacterMotor : MonoBehaviour
{
    private FighterController owner;
    private Rigidbody2D rb;

    private bool isGrounded;
    private bool wasGrounded;

    private bool isDashing = false;
    private float dashTimer = 0f;
    private float dashDirection = 0f;

    private float lastLeftTapTime = -999f;
    private float lastRightTapTime = -999f;
    private int previousInputSign = 0;

    private float timeSinceJumpStart = 999f;
    private int currentJumpCount = 0;
    private int currentAirDashCount = 0;

    public bool FacingRight { get; private set; } = true;
    public bool IsGrounded => isGrounded;
    public bool WasGrounded => wasGrounded;
    public bool IsDashing => isDashing;
    public Vector2 Velocity => rb != null ? rb.linearVelocity : Vector2.zero;
    public Rigidbody2D Rigidbody => rb;

    public void Initialize(FighterController fighter)
    {
        owner = fighter;
        rb = GetComponent<Rigidbody2D>();
    }

    public void Tick()
    {
        CheckGround();
        UpdateTimers();
        HandleLandingReset();
        UpdateDash();
    }

    void CheckGround()
    {
        wasGrounded = isGrounded;

        if (owner == null || owner.groundCheck == null)
        {
            isGrounded = false;
            return;
        }

        isGrounded = Physics2D.OverlapCircle(
            owner.groundCheck.position,
            owner.groundCheckRadius,
            owner.groundLayer
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

    void UpdateDash()
    {
        if (!isDashing || rb == null) return;

        dashTimer -= Time.deltaTime;

        if (dashTimer > 0f)
        {
            rb.linearVelocity = new Vector2(dashDirection * owner.dashSpeed, rb.linearVelocity.y);
        }
        else
        {
            isDashing = false;
        }
    }

    public void Move(float direction)
    {
        if (owner == null || rb == null || owner.stateMachine == null) return;

        if (!owner.stateMachine.CanMove())
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

        rb.linearVelocity = new Vector2(direction * owner.moveSpeed, rb.linearVelocity.y);

        if (direction > 0f)
        {
            FacingRight = true;
        }
        else if (direction < 0f)
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
            if (Time.time - lastLeftTapTime <= owner.doubleTapWindow)
            {
                TryStartDash(-1f);
            }
            lastLeftTapTime = Time.time;
        }
        else if (inputSign == 1)
        {
            if (Time.time - lastRightTapTime <= owner.doubleTapWindow)
            {
                TryStartDash(1f);
            }
            lastRightTapTime = Time.time;
        }
    }

    void TryStartDash(float direction)
    {
        if (owner == null || owner.stateMachine == null) return;

        if (
            isDashing ||
            owner.IsAttacking ||
            owner.IsInHitstun ||
            owner.IsInBlockstun ||
            owner.IsBlocking
        )
        {
            return;
        }

        if (isGrounded)
        {
            StartDash(direction);
            return;
        }

        if (
            owner.allowAirDash &&
            timeSinceJumpStart <= owner.airDashWindow &&
            currentAirDashCount < owner.maxAirDashCount
        )
        {
            StartDash(direction);
            currentAirDashCount++;
        }
    }

    void StartDash(float direction)
    {
        if (rb == null) return;

        isDashing = true;
        dashTimer = owner.dashDuration;
        dashDirection = direction;

        FacingRight = direction > 0f;
        rb.linearVelocity = new Vector2(dashDirection * owner.dashSpeed, rb.linearVelocity.y);
    }

    public bool TryJumpImmediate()
    {
        if (owner == null || rb == null) return false;
        if (currentJumpCount >= owner.maxJumpCount) return false;

        currentJumpCount++;

        rb.linearVelocity = new Vector2(rb.linearVelocity.x, owner.jumpForce);

        isGrounded = false;
        timeSinceJumpStart = 0f;

        if (owner.stateMachine != null)
            owner.stateMachine.ForceClearPosture();

        if (owner.combat != null)
            owner.combat.ClearBufferedAction();

        return true;
    }

    public void CancelDash()
    {
        isDashing = false;
        dashTimer = 0f;
    }

    public void StopHorizontalMovement()
    {
        if (rb == null) return;
        rb.linearVelocity = new Vector2(0f, rb.linearVelocity.y);
    }

    public void ApplyKnockback(float force, Transform attacker)
    {
        if (rb == null || force <= 0f || attacker == null) return;

        float direction = transform.position.x >= attacker.position.x ? 1f : -1f;
        rb.linearVelocity = new Vector2(direction * force, rb.linearVelocity.y);

        FacingRight = direction > 0f;
    }

    public void ApplyBlockPush(float attackPushForce, Transform attacker)
    {
        if (rb == null || owner == null || attacker == null) return;

        float finalPush = attackPushForce * owner.blockPushMultiplier;
        if (finalPush <= 0f)
        {
            finalPush = 1.5f;
        }

        float direction = transform.position.x >= attacker.position.x ? 1f : -1f;
        rb.linearVelocity = new Vector2(direction * finalPush, rb.linearVelocity.y);

        FacingRight = direction > 0f;
    }

    public void ResetMotor(bool faceRight)
    {
        isGrounded = false;
        wasGrounded = false;

        isDashing = false;
        dashTimer = 0f;
        dashDirection = 0f;

        lastLeftTapTime = -999f;
        lastRightTapTime = -999f;
        previousInputSign = 0;

        timeSinceJumpStart = 999f;
        currentJumpCount = 0;
        currentAirDashCount = 0;

        FacingRight = faceRight;

        if (rb != null)
        {
            rb.linearVelocity = Vector2.zero;
            rb.angularVelocity = 0f;
        }
    }

    void OnDrawGizmosSelected()
    {
        FighterController fc = owner != null ? owner : GetComponent<FighterController>();
        if (fc == null || fc.groundCheck == null) return;

        Gizmos.color = Color.green;
        Gizmos.DrawWireSphere(fc.groundCheck.position, fc.groundCheckRadius);
    }
}