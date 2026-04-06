using UnityEngine;

[System.Serializable]
public struct FighterCommand
{
    public float move;
    public bool jumpPressed;
    public bool crouchHeld;
    public bool blockHeld;
    public bool lightAttackPressed;
    public bool heavyAttackPressed;

    public static FighterCommand Empty => new FighterCommand
    {
        move = 0f,
        jumpPressed = false,
        crouchHeld = false,
        blockHeld = false,
        lightAttackPressed = false,
        heavyAttackPressed = false
    };
}