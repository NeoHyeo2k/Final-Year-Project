using UnityEngine;

public class AgentInput : MonoBehaviour
{
    private FighterController controller;
    private FighterCommand command;

    private int currentDiscreteAction = 0;
    private int previousDiscreteAction = 0;

    void Start()
    {
        controller = GetComponent<FighterController>();

        if (controller == null)
        {
            Debug.LogError("FighterController not found on " + gameObject.name);
        }

        command = FighterCommand.Empty;
    }

    void Update()
    {
        if (controller == null) return;

        BuildCommandFromCurrentAction();

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

        previousDiscreteAction = currentDiscreteAction;
    }

    public void SetCommand(FighterCommand newCommand)
    {
        command = newCommand;
    }

    public void SetDiscreteAction(int actionId)
    {
        Debug.Log("SetDiscreteAction: " + actionId);
        currentDiscreteAction = actionId;
    }

    private void BuildCommandFromCurrentAction()
    {
        FighterCommand next = FighterCommand.Empty;

        bool isNewAction = currentDiscreteAction != previousDiscreteAction;

        switch (currentDiscreteAction)
        {
            case 0:
                break;

            case 1:
                next.move = -1f;
                break;

            case 2:
                next.move = 1f;
                break;

            case 3:
                if (isNewAction)
                {
                    next.jumpPressed = true;
                }
                break;

            case 4:
                next.crouchHeld = true;
                break;

            case 5:
                next.blockHeld = true;
                break;

            case 6:
                if (isNewAction)
                {
                    next.lightAttackPressed = true;
                }
                break;

            case 7:
                if (isNewAction)
                {
                    next.heavyAttackPressed = true;
                }
                break;
        }

        command = next;
    }
}