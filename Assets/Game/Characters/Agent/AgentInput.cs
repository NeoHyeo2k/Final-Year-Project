using UnityEngine;

public class AgentInput : MonoBehaviour
{
    private FighterController controller;
    private FighterCommand command;

    [Header("Attack Repeat")]
    [Tooltip("持续选择攻击动作时，允许再次请求攻击的最小间隔")]
    public float repeatedAttackInterval = 0.18f;
    public bool edgeTriggeredAttacksOnly = true;
    public bool useAttackGap = true;
    public float minAttackGap = 0.22f;

    [Header("Debug")]
    public bool debugActions = false;

    // Branch actions
    private int currentMoveAction = 0;
    private int currentPostureAction = 0;
    private int currentCombatAction = 0;

    private int previousMoveAction = 0;
    private int previousPostureAction = 0;
    private int previousCombatAction = 0;

    private float lightAttackTimer = 0f;
    private float heavyAttackTimer = 0f;
    private float attackGapTimer = 0f;

    public bool AttackGapActive => useAttackGap && attackGapTimer > 0f;

    void Start()
    {
        controller = GetComponent<FighterController>();

        if (controller == null)
        {
            DLog.LogError("FighterController not found on " + gameObject.name);
        }

        command = FighterCommand.Empty;
    }

    void Update()
    {
        if (controller == null) return;

        lightAttackTimer += Time.deltaTime;
        heavyAttackTimer += Time.deltaTime;

        if (attackGapTimer > 0f)
        {
            attackGapTimer -= Time.deltaTime;
        }

        BuildCommandFromBranchActions();

        controller.Move(command.move);

        if (command.jumpPressed)
        {
            controller.Jump();
        }

        controller.SetBlock(command.blockHeld, command.crouchHeld);
        controller.SetCrouch(command.crouchHeld);

        if (command.lightAttackPressed)
        {
            controller.RequestLightAttack();
        }

        if (command.heavyAttackPressed)
        {
            controller.RequestHeavyAttack();
        }

        previousMoveAction = currentMoveAction;
        previousPostureAction = currentPostureAction;
        previousCombatAction = currentCombatAction;
    }

    public void SetBranchActions(int moveAction, int postureAction, int combatAction)
    {
        currentMoveAction = moveAction;
        currentPostureAction = postureAction;
        currentCombatAction = combatAction;

        if (debugActions)
        {
            DLog.Log($"{name} actions => move:{moveAction}, posture:{postureAction}, combat:{combatAction}");
        }
    }

    public void ResetTemporalState()
    {
        command = FighterCommand.Empty;

        currentMoveAction = 0;
        currentPostureAction = 0;
        currentCombatAction = 0;

        previousMoveAction = 0;
        previousPostureAction = 0;
        previousCombatAction = 0;

        lightAttackTimer = 0f;
        heavyAttackTimer = 0f;
        attackGapTimer = 0f;
    }

    private void BuildCommandFromBranchActions()
    {
        FighterCommand next = FighterCommand.Empty;

        bool postureChanged = currentPostureAction != previousPostureAction;
        bool combatChanged = currentCombatAction != previousCombatAction;
        bool attackStartedThisFrame =
            IsAttackAction(currentCombatAction) &&
            !IsAttackAction(previousCombatAction);

        // Branch 1: Move
        switch (currentMoveAction)
        {
            case 0:
                next.move = 0f;
                break;
            case 1:
                next.move = -1f;
                break;
            case 2:
                next.move = 1f;
                break;
        }

        // Branch 2: Jump / Crouch
        switch (currentPostureAction)
        {
            case 0:
                break;
            case 1:
                if (postureChanged)
                {
                    next.jumpPressed = true;
                }
                break;
            case 2:
                next.crouchHeld = true;
                break;
        }

        // Branch 3: Block / Light / Heavy
        switch (currentCombatAction)
        {
            case 0:
                break;

            case 1:
                next.blockHeld = true;
                break;

            case 2:
                if (ShouldRequestLightAttack(attackStartedThisFrame, combatChanged))
                {
                    next.lightAttackPressed = true;
                    lightAttackTimer = 0f;
                    StartAttackGap();
                }
                break;

            case 3:
                if (ShouldRequestHeavyAttack(attackStartedThisFrame, combatChanged))
                {
                    next.heavyAttackPressed = true;
                    heavyAttackTimer = 0f;
                    StartAttackGap();
                }
                break;
        }

        command = next;
    }

    private bool ShouldRequestLightAttack(bool attackStartedThisFrame, bool combatChanged)
    {
        if (controller == null) return false;

        if (AttackGapActive)
            return false;

        if (edgeTriggeredAttacksOnly)
            return attackStartedThisFrame;

        if (combatChanged)
            return true;

        if (lightAttackTimer < repeatedAttackInterval)
            return false;

        if (controller.IsAttacking)
            return false;

        if (controller.HasBufferedAction)
            return false;

        return true;
    }

    private bool ShouldRequestHeavyAttack(bool attackStartedThisFrame, bool combatChanged)
    {
        if (controller == null) return false;

        if (AttackGapActive)
            return false;

        if (edgeTriggeredAttacksOnly)
            return attackStartedThisFrame;

        if (combatChanged)
            return true;

        if (heavyAttackTimer < repeatedAttackInterval)
            return false;

        if (controller.IsAttacking)
            return false;

        if (controller.HasBufferedAction)
            return false;

        return true;
    }

    private bool IsAttackAction(int combatAction)
    {
        return combatAction == 2 || combatAction == 3;
    }

    private void StartAttackGap()
    {
        if (!useAttackGap)
            return;

        attackGapTimer = minAttackGap;
    }
}
