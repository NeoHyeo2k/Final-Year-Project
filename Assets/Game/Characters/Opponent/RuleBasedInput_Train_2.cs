using UnityEngine;

public class RuleBasedInput_Train_2 : MonoBehaviour
{
    [Header("Target")]
    public FighterController target;

    [Header("Ranges")]
    [Tooltip("If farther than this, keep walking toward the target.")]
    public float approachDistance = 2.4f;

    [Tooltip("Primary training range where the opponent starts creating attack/block samples.")]
    public float attackDistance = 1.25f;

    [Tooltip("Heavy attack is only sampled inside this conservative range.")]
    public float heavyAttackMinDistance = 0.9f;

    [Tooltip("Heavy attack is only sampled inside this conservative range.")]
    public float heavyAttackMaxDistance = 1.25f;

    [Header("Timing")]
    [Tooltip("How often the opponent refreshes its high-level decision.")]
    public float thinkInterval = 0.26f;

    [Header("Behavior")]
    [Range(0f, 1f)]
    [Tooltip("Chance to hold block when both fighters are in close range.")]
    public float blockChanceWhenClose = 0.22f;

    [Range(0f, 1f)]
    [Tooltip("Chance to stay idle briefly when close. Kept modest to reduce dead air.")]
    public float idleChanceWhenClose = 0.28f;

    [Range(0f, 1f)]
    [Tooltip("Chance to use a light attack when close and not blocking/idling.")]
    public float lightAttackChance = 0.38f;

    [Range(0f, 1f)]
    [Tooltip("Small chance to use a heavy attack in range. Kept low so Train_2 stays close to Train_1.")]
    public float heavyAttackChance = 0.03f;

    [Range(0f, 1f)]
    [Tooltip("Chance to jump when close. Keep low to preserve grounded timing samples.")]
    public float jumpChance = 0.03f;

    [Tooltip("Disable heavy attacks for this training opponent.")]
    public bool disableHeavyAttack = false;

    [Header("Pressure Response")]
    [Tooltip("When the target is attacking in range, prefer blocking instead of scrambling.")]
    public bool reactiveBlockAgainstActiveAttack = true;

    [Tooltip("Extra chance to block while the target is in active attack frames nearby.")]
    [Range(0f, 1f)]
    public float extraBlockChanceVsActiveAttack = 0.22f;

    private FighterController controller;
    private FighterCommand currentCommand;
    private float thinkTimer;

    private void Start()
    {
        controller = GetComponent<FighterController>();

        if (controller == null)
        {
            DLog.LogError("FighterController not found on " + gameObject.name);
        }

        currentCommand = FighterCommand.Empty;
        thinkTimer = 0f;
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

        if (ShouldReactiveBlock(distance))
        {
            cmd.blockHeld = true;
            return cmd;
        }

        float roll = Random.value;

        if (roll < blockChanceWhenClose)
        {
            cmd.blockHeld = true;
            return cmd;
        }

        roll -= blockChanceWhenClose;
        if (roll < idleChanceWhenClose)
        {
            return cmd;
        }

        roll -= idleChanceWhenClose;
        if (roll < jumpChance)
        {
            cmd.jumpPressed = true;
            return cmd;
        }

        roll -= jumpChance;
        if (roll < lightAttackChance)
        {
            cmd.lightAttackPressed = true;
            return cmd;
        }

        roll -= lightAttackChance;
        if (CanSampleHeavyAttack(distance) && roll < heavyAttackChance)
        {
            cmd.heavyAttackPressed = true;
            return cmd;
        }

        cmd.move = Mathf.Sign(dx);
        return cmd;
    }

    private bool ShouldReactiveBlock(float distance)
    {
        if (!reactiveBlockAgainstActiveAttack)
            return false;

        if (distance > attackDistance)
            return false;

        if (target.CurrentAttackPhase != AttackPhase.Active)
            return false;

        return Random.value < extraBlockChanceVsActiveAttack;
    }

    private bool CanSampleHeavyAttack(float distance)
    {
        if (disableHeavyAttack)
            return false;

        if (target == null)
            return false;

        if (target.CurrentAttackPhase != AttackPhase.None || target.IsInHitstun || target.IsInBlockstun)
            return false;

        return distance >= heavyAttackMinDistance && distance <= heavyAttackMaxDistance;
    }
}
