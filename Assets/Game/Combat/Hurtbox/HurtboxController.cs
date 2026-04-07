using UnityEngine;

public enum HurtboxType
{
    Head,
    Body,
    Foot
}

public class HurtboxController : MonoBehaviour
{
    public HurtboxType hurtboxType;

    private FighterController ownerController;
    private Health ownerHealth;
    private Collider2D hurtboxCollider;

    public FighterController OwnerController => ownerController;
    public Health OwnerHealth => ownerHealth;
    public HurtboxType HurtboxType => hurtboxType;

    void Awake()
    {
        ownerController = GetComponentInParent<FighterController>();
        ownerHealth = GetComponentInParent<Health>();
        hurtboxCollider = GetComponent<Collider2D>();

        if (ownerController == null)
        {
            DLog.LogError("FighterController not found in parent of " + gameObject.name);
        }

        if (ownerHealth == null)
        {
            DLog.LogError("Health not found in parent of " + gameObject.name);
        }

        if (hurtboxCollider == null)
        {
            DLog.LogError("Collider2D not found on hurtbox " + gameObject.name);
        }
    }

    public void SetHurtboxEnabled(bool enabled)
    {
        if (hurtboxCollider != null)
        {
            hurtboxCollider.enabled = enabled;
        }
    }
}