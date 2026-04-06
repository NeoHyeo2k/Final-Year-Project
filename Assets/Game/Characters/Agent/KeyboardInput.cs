using UnityEngine;

public class KeyboardInput : MonoBehaviour
{
    private FighterController controller;

    void Start()
    {
        controller = GetComponent<FighterController>();

        if (controller == null)
        {
            Debug.LogError("FighterController not found on " + gameObject.name);
        }
    }

    void Update()
    {
        if (controller == null) return;

        float move = Input.GetAxisRaw("Horizontal");
        controller.Move(move);

        if (Input.GetKeyDown(KeyCode.Space))
        {
            controller.Jump();
        }

        bool block =
            Input.GetKey(KeyCode.LeftShift) ||
            Input.GetKey(KeyCode.RightShift);

        bool crouchInput =
            Input.GetKey(KeyCode.S) ||
            Input.GetKey(KeyCode.DownArrow);

        controller.SetBlock(block, crouchInput);
        controller.SetCrouch(crouchInput);

        if (Input.GetKeyDown(KeyCode.J))
        {
            controller.RequestLightAttack();
        }

        if (Input.GetKeyDown(KeyCode.K))
        {
            controller.RequestHeavyAttack();
        }
    }
}