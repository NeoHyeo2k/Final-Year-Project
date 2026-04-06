using UnityEngine;

public class RuleBasedInput : MonoBehaviour
{
    [Header("Target")]
    public FighterController target;

    [Header("Ranges")]
    public float approachDistance = 2.5f;
    public float attackDistance = 1.2f;

    [Header("Timing")]
    public float thinkInterval = 0.15f;

    [Header("Behavior")]
    [Range(0f, 1f)]
    public float blockChanceWhenClose = 0.2f;

    [Range(0f, 1f)]
    public float heavyAttackChance = 0.35f;

    [Range(0f, 1f)]
    public float jumpChance = 0.08f;

    private FighterController controller;
    private float thinkTimer;

    private FighterCommand currentCommand;

    void Start()
    {
        controller = GetComponent<FighterController>();

        if (controller == null)
        {
            Debug.LogError("FighterController not found on " + gameObject.name);
        }

        thinkTimer = 0f;
        currentCommand = FighterCommand.Empty;
    }

    void Update()
    {
        if (controller == null || target == null) return;

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

        if (currentCommand.heavyAttackPressed)
        {
            controller.RequestHeavyAttack();
        }
    }

    FighterCommand DecideCommand()
    {
        FighterCommand cmd = FighterCommand.Empty;

        float dx = target.transform.position.x - transform.position.x;
        float distance = Mathf.Abs(dx);

        if (distance > approachDistance)
        {
            cmd.move = Mathf.Sign(dx);
            return cmd;
        }

        if (distance <= attackDistance)
        {
            float r = Random.value;

            if (r < blockChanceWhenClose)
            {
                cmd.blockHeld = true;
                return cmd;
            }

            if (Random.value < jumpChance)
            {
                cmd.jumpPressed = true;
                return cmd;
            }

            if (Random.value < heavyAttackChance)
            {
                cmd.heavyAttackPressed = true;
            }
            else
            {
                cmd.lightAttackPressed = true;
            }

            return cmd;
        }

        cmd.move = Mathf.Sign(dx);
        return cmd;
    }
}