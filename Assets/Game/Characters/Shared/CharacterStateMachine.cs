using UnityEngine;

public class CharacterStateMachine : MonoBehaviour
{
    private FighterController owner;

    private float hitstunTimer = 0f;
    private float blockstunTimer = 0f;

    private bool isCrouching = false;
    private bool isBlocking = false;
    private bool isCrouchBlocking = false;

    public CharacterState CurrentState { get; private set; } = CharacterState.Idle;

    public bool IsInHitstun => hitstunTimer > 0f;
    public bool IsInBlockstun => blockstunTimer > 0f;

    public bool IsCrouching => isCrouching;
    public bool IsStandingBlocking => isBlocking && !isCrouchBlocking && !IsInHitstun;
    public bool IsCrouchBlocking => isBlocking && isCrouchBlocking && !IsInHitstun;
    public bool IsBlocking => isBlocking && !IsInHitstun;

    public void Initialize(FighterController fighter)
    {
        owner = fighter;
    }

    public void TickTimers()
    {
        UpdateHitstun();
        UpdateBlockstun();
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

    public void UpdateState()
    {
        if (owner == null || owner.motor == null || owner.combat == null) return;

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

        if (owner.IsAttacking)
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

        if (owner.IsDashing)
        {
            CurrentState = CharacterState.Dash;
            return;
        }

        if (isCrouching && owner.IsGrounded)
        {
            CurrentState = CharacterState.Crouch;
            return;
        }

        if (!owner.IsGrounded)
        {
            if (owner.Velocity.y > 0.1f)
            {
                CurrentState = CharacterState.Jump;
            }
            else
            {
                CurrentState = CharacterState.Fall;
            }

            return;
        }

        if (Mathf.Abs(owner.Velocity.x) > 0.1f)
        {
            CurrentState = CharacterState.Move;
        }
        else
        {
            CurrentState = CharacterState.Idle;
        }
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
        if (owner == null) return false;

        return !owner.IsAttacking
            && !owner.IsDashing
            && CurrentState != CharacterState.Hitstun
            && CurrentState != CharacterState.Block
            && CurrentState != CharacterState.CrouchBlock
            && CurrentState != CharacterState.Blockstun;
    }

    public void SetCrouch(bool crouch)
    {
        if (owner == null) return;
        if (isBlocking) return;

        if (!CanCrouch())
        {
            isCrouching = false;
            return;
        }

        isCrouching = crouch && owner.IsGrounded && !owner.IsDashing;
    }

    public void SetBlock(bool block, bool crouchHeld)
    {
        if (owner == null) return;

        if (IsInHitstun || IsInBlockstun)
        {
            return;
        }

        if (!owner.IsGrounded)
        {
            isBlocking = false;
            isCrouchBlocking = false;
            return;
        }

        if (owner.IsAttacking || owner.IsDashing)
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

    public void ForceClearPosture()
    {
        isCrouching = false;
        isBlocking = false;
        isCrouchBlocking = false;
    }

    public void ReceiveHitstun(float duration)
    {
        if (owner == null) return;
        if (duration <= 0f) return;

        if (owner.combat != null)
            owner.combat.InterruptAttack();

        if (owner.motor != null)
            owner.motor.CancelDash();

        ForceClearPosture();
        blockstunTimer = 0f;
        hitstunTimer = duration;

        DLog.Log(gameObject.name + " entered Hitstun for " + duration + " seconds.");
    }

    public void ReceiveBlockstun(float duration)
    {
        if (owner == null) return;

        if (duration <= 0f)
        {
            duration = owner.defaultBlockstunDuration;
        }

        if (owner.combat != null)
            owner.combat.InterruptAttack();

        if (owner.motor != null)
            owner.motor.CancelDash();

        isCrouching = false;
        blockstunTimer = duration;

        DLog.Log(gameObject.name + " entered Blockstun for " + duration + " seconds.");
    }

    public bool CanBlockAttack(AttackData attackData)
    {
        if (attackData == null) return false;
        if (!IsBlocking) return false;
        if (IsInHitstun) return false;
        if (owner == null || !owner.IsGrounded) return false;

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

    public void ResetStateMachine()
    {
        hitstunTimer = 0f;
        blockstunTimer = 0f;
        isCrouching = false;
        isBlocking = false;
        isCrouchBlocking = false;
        CurrentState = CharacterState.Idle;
    }
}