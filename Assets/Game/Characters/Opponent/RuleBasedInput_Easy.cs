using UnityEngine;

public class RuleBasedInput_Easy : MonoBehaviour
{
    [Header("Target")]
    public FighterController target;

    [Header("Ranges")]
    public float approachDistance = 2.6f;
    public float attackDistance = 1.1f;

    [Header("Timing")]
    public float thinkInterval = 0.35f;

    [Header("Behavior")]
    [Range(0f, 1f)]
    public float blockChanceWhenClose = 0.05f;

    [Range(0f, 1f)]
    public float heavyAttackChance = 0.15f;

    [Range(0f, 1f)]
    public float lightAttackChance = 0.35f;

    [Range(0f, 1f)]
    public float jumpChance = 0.02f;

    [Range(0f, 1f)]
    public float idleChanceWhenClose = 0.45f;

    [Tooltip("Disable heavy attacks for the rule-based opponent during training.")]
    public bool disableHeavyAttack = false;

    private FighterController controller;
    private float thinkTimer;
    private FighterCommand currentCommand;

    private void Start()
    {
        controller = GetComponent<FighterController>();

        if (controller == null)
        {
            DLog.LogError("FighterController not found on " + gameObject.name);
        }

        thinkTimer = 0f;
        currentCommand = FighterCommand.Empty;
    }

    private void Update()
    {
        if (controller == null || target == null)
            return;

        thinkTimer -= Time.deltaTime;
        if (thinkTimer <= 0f)
        {
            thinkTimer = thinkInterval;
            currentCommand = DecideCommand();
        }

        controller.Move(currentCommand.move);

        if (currentCommand.jumpPressed)
        {
            controller.Jump();
        }

        controller.SetBlock(currentCommand.blockHeld, currentCommand.crouchHeld);
        controller.SetCrouch(currentCommand.crouchHeld);

        if (currentCommand.lightAttackPressed)
        {
            controller.RequestLightAttack();
        }

        if (!disableHeavyAttack && currentCommand.heavyAttackPressed)
        {
            controller.RequestHeavyAttack();
        }
    }

    private FighterCommand DecideCommand()
    {
        FighterCommand cmd = FighterCommand.Empty;

        float dx = target.transform.position.x - transform.position.x;
        float distance = Mathf.Abs(dx);

        if (distance > approachDistance)
        {
            cmd.move = Mathf.Sign(dx);
            return cmd;
        }

        if (distance > attackDistance)
        {
            cmd.move = Mathf.Sign(dx);
            return cmd;
        }

        float roll = Random.value;

        if (roll < idleChanceWhenClose)
        {
            return cmd;
        }

        roll -= idleChanceWhenClose;
        if (roll < blockChanceWhenClose)
        {
            cmd.blockHeld = true;
            return cmd;
        }

        roll -= blockChanceWhenClose;
        if (roll < jumpChance)
        {
            cmd.jumpPressed = true;
            return cmd;
        }

        roll -= jumpChance;
        if (!disableHeavyAttack && roll < heavyAttackChance)
        {
            cmd.heavyAttackPressed = true;
            return cmd;
        }

        if (!disableHeavyAttack)
        {
            roll -= heavyAttackChance;
        }

        if (roll < lightAttackChance)
        {
            cmd.lightAttackPressed = true;
        }

        return cmd;
    }
}
