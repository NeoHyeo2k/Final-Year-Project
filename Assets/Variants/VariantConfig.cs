using UnityEngine;

[CreateAssetMenu(menuName = "Variants/VariantConfig")]
public class VariantConfig : ScriptableObject
{
    [Header("Mechanics")]
    public bool enableRC;
    public bool enablePersona;

    [Header("Tempo")]
    public float moveSpeedMultiplier = 1f;
    public int inputDelayFrames = 0;

    [Header("Rewards")]
    public float rewardScale = 1f;
}
