using UnityEngine;

public class AgentInput : MonoBehaviour
{
    private FighterController controller;
    private FighterCommand command;

    [Header("Attack Repeat")]
    [Tooltip("持续选择攻击动作时，允许再次请求攻击的最小间隔")]
    public float repeatedAttackInterval = 0.18f;

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

    private void BuildCommandFromBranchActions()
    {
        FighterCommand next = FighterCommand.Empty;

        bool postureChanged = currentPostureAction != previousPostureAction;
        bool combatChanged = currentCombatAction != previousCombatAction;

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
                if (ShouldRequestLightAttack(combatChanged))
                {
                    next.lightAttackPressed = true;
                    lightAttackTimer = 0f;
                }
                break;

            case 3:
                if (ShouldRequestHeavyAttack(combatChanged))
                {
                    next.heavyAttackPressed = true;
                    heavyAttackTimer = 0f;
                }
                break;
        }

        command = next;
    }

    private bool ShouldRequestLightAttack(bool combatChanged)
    {
        if (controller == null) return false;

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

    private bool ShouldRequestHeavyAttack(bool combatChanged)
    {
        if (controller == null) return false;

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
}