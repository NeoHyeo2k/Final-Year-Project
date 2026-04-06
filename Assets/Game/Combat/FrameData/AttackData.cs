using UnityEngine;

public enum AttackType
{
    Mid,
    Low,
    Overhead
}

[System.Serializable]
public class AttackData
{
    [Header("Basic Info")]
    public string attackName = "New Attack";
    public AttackType attackType = AttackType.Mid;

    [Header("Frame Data")]
    public float startupTime = 0.12f;
    public float activeTime = 0.08f;
    public float recoveryTime = 0.18f;

    [Header("Combat Data")]
    public int damage = 1;
    public float hitstunTime = 0.15f;
    public float pushbackForce = 2f;

    [Header("Head Hitbox (Overhead)")]
    public Vector2 headHitboxOffset = new Vector2(1f, 1.0f);
    public Vector2 headHitboxSize = new Vector2(1f, 1f);

    [Header("Body Hitbox (Mid)")]
    public Vector2 bodyHitboxOffset = new Vector2(1f, 0.4f);
    public Vector2 bodyHitboxSize = new Vector2(1f, 1f);

    [Header("Foot Hitbox (Low)")]
    public Vector2 footHitboxOffset = new Vector2(1f, -0.6f);
    public Vector2 footHitboxSize = new Vector2(1f, 0.8f);

    public Vector2 GetHitboxOffset()
    {
        switch (attackType)
        {
            case AttackType.Overhead:
                return headHitboxOffset;
            case AttackType.Low:
                return footHitboxOffset;
            case AttackType.Mid:
            default:
                return bodyHitboxOffset;
        }
    }

    public Vector2 GetHitboxSize()
    {
        switch (attackType)
        {
            case AttackType.Overhead:
                return headHitboxSize;
            case AttackType.Low:
                return footHitboxSize;
            case AttackType.Mid:
            default:
                return bodyHitboxSize;
        }
    }
}