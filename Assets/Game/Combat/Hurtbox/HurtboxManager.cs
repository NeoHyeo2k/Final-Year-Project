using UnityEngine;

public class HurtboxManager : MonoBehaviour
{
    private FighterController owner;

    [Header("Hurtboxes")]
    public HurtboxController headHurtbox;
    public HurtboxController bodyHurtbox;
    public HurtboxController footHurtbox;

    public void Initialize(FighterController fighter)
    {
        owner = fighter;
    }

    public void UpdateHurtboxes(bool footOnly)
    {
        if (headHurtbox != null)
            headHurtbox.SetHurtboxEnabled(!footOnly);

        if (bodyHurtbox != null)
            bodyHurtbox.SetHurtboxEnabled(!footOnly);

        if (footHurtbox != null)
            footHurtbox.SetHurtboxEnabled(true);
    }
}