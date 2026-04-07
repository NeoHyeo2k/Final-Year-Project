using UnityEngine;

public class CharacterCombat : MonoBehaviour
{
    private FighterController owner;

    public int comboCount = 0;

    private float comboResetTime = 1.0f;
    private float lastHitTime = -999f;

    private bool isAttacking = false;
    private float attackTimer = 0f;

    private BufferedAction bufferedAction = BufferedAction.None;
    private float bufferedActionTimer = 0f;

    private bool currentAttackHasHit = false;
    private bool cancelUsedThisAttack = false;

    public bool IsAttacking => isAttacking;
    public BufferedAction CurrentBufferedAction => bufferedAction;
    public bool HasBufferedAction => bufferedAction != BufferedAction.None;

    public AttackPhase CurrentAttackPhase { get; private set; } = AttackPhase.None;
    public AttackData CurrentAttackData { get; private set; }

    public void Initialize(FighterController fighter)
    {
        owner = fighter;
    }

    public void Tick()
    {
        UpdateAttack();
        UpdateCombo();
        UpdateInputBuffer();
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
        if (owner == null) return;
        if (!owner.enableInputBuffer) return;
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

    bool TryExecuteBufferedAction()
    {
        if (owner == null) return false;

        switch (bufferedAction)
        {
            case BufferedAction.Jump:
                if (owner.CanJump() && owner.motor != null)
                {
                    return owner.motor.TryJumpImmediate();
                }
                break;

            case BufferedAction.LightAttack:
                if (owner.CanAttack())
                {
                    StartAttack(owner.lightAttack);
                    return true;
                }
                break;

            case BufferedAction.HeavyAttack:
                if (owner.CanAttack())
                {
                    StartAttack(owner.heavyAttack);
                    return true;
                }
                break;
        }

        return false;
    }

    public void BufferAction(BufferedAction action)
    {
        if (owner == null) return;
        if (!owner.enableInputBuffer) return;
        if (action == BufferedAction.None) return;

        if (bufferedAction == action){
            return;
        }

        bufferedAction = action;
        bufferedActionTimer = owner.inputBufferTime;

        DLog.Log(gameObject.name + " buffered action: " + bufferedAction);

    }

    public void ClearBufferedAction()
    {
        bufferedAction = BufferedAction.None;
        bufferedActionTimer = 0f;
    }

    public void StartAttack(AttackData attackData)
    {
        if (owner == null || owner.stateMachine == null || owner.motor == null) return;
        if (!owner.CanAttack()) return;
        if (attackData == null) return;

        isAttacking = true;

        owner.stateMachine.ForceClearPosture();

        CurrentAttackData = attackData;
        CurrentAttackPhase = AttackPhase.Startup;
        attackTimer = CurrentAttackData.startupTime;

        currentAttackHasHit = false;
        cancelUsedThisAttack = false;

        owner.motor.StopHorizontalMovement();
        ClearBufferedAction();

        DLog.Log("Start Attack: " + CurrentAttackData.attackName + " [" + CurrentAttackData.attackType + "]");
    }

    public void EndAttack()
    {
        isAttacking = false;
        CurrentAttackPhase = AttackPhase.None;
        attackTimer = 0f;
        CurrentAttackData = null;

        currentAttackHasHit = false;
        cancelUsedThisAttack = false;
    }

    public void InterruptAttack()
    {
        EndAttack();
    }

    public void NotifyAttackHit()
    {
        if (!isAttacking) return;
        if (CurrentAttackData == null) return;

        currentAttackHasHit = true;
    }

    public bool TryCancelInto(AttackData nextAttack)
    {
        if (owner == null) return false;
        if (!owner.allowLightAttackCancel) return false;
        if (!isAttacking) return false;
        if (CurrentAttackData == null) return false;
        if (nextAttack == null) return false;

        if (CurrentAttackData != owner.lightAttack) return false;
        if (!currentAttackHasHit) return false;
        if (CurrentAttackPhase != AttackPhase.Recovery) return false;
        if (cancelUsedThisAttack) return false;
        if (nextAttack != owner.lightAttack && nextAttack != owner.heavyAttack) return false;

        cancelUsedThisAttack = true;

        isAttacking = true;
        if (owner.stateMachine != null)
            owner.stateMachine.ForceClearPosture();

        CurrentAttackData = nextAttack;
        CurrentAttackPhase = AttackPhase.Startup;
        attackTimer = CurrentAttackData.startupTime;

        currentAttackHasHit = false;
        cancelUsedThisAttack = false;

        if (owner.motor != null)
            owner.motor.StopHorizontalMovement();

        ClearBufferedAction();

        DLog.Log("Cancel into: " + CurrentAttackData.attackName);
        return true;
    }

    public void RequestLightAttack()
    {
        if (owner == null) return;

        if (TryCancelInto(owner.lightAttack)) return;

        if (owner.CanAttack())
        {
            StartAttack(owner.lightAttack);
        }
        //else
        //{
        //    BufferAction(BufferedAction.LightAttack);
        //}
    }

    public void RequestHeavyAttack()
    {
        if (owner == null) return;

        if (TryCancelInto(owner.heavyAttack)) return;

        if (owner.CanAttack())
        {
            StartAttack(owner.heavyAttack);
        }
        //else
        //{
        //    BufferAction(BufferedAction.HeavyAttack);
        //}
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

        DLog.Log(gameObject.name + " combo count = " + comboCount);
    }

    public void ResetCombat()
    {
        comboCount = 0;
        lastHitTime = -999f;

        isAttacking = false;
        attackTimer = 0f;

        bufferedAction = BufferedAction.None;
        bufferedActionTimer = 0f;

        currentAttackHasHit = false;
        cancelUsedThisAttack = false;

        CurrentAttackPhase = AttackPhase.None;
        CurrentAttackData = null;
    }
}