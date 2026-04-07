using UnityEngine;

public class AttackInput : MonoBehaviour
{
    public GameObject hitboxObject;

    private FighterController controller;
    private BoxCollider2D hitboxCollider;

    void Start()
    {
        controller = GetComponent<FighterController>();

        if (controller == null)
        {
            DLog.LogError("FighterController not found on " + gameObject.name);
        }

        if (hitboxObject == null)
        {
            DLog.LogError("Hitbox object is not assigned on " + gameObject.name);
            return;
        }

        hitboxCollider = hitboxObject.GetComponent<BoxCollider2D>();

        if (hitboxCollider == null)
        {
            DLog.LogError("BoxCollider2D not found on hitboxObject: " + hitboxObject.name);
        }

        hitboxObject.SetActive(false);
    }

    void Update()
    {
        if (controller == null || hitboxObject == null || hitboxCollider == null)
            return;

        UpdateHitboxTransform();
        UpdateHitboxState();
    }

    void UpdateHitboxTransform()
    {
        AttackData attackData = controller.CurrentAttackData;
        if (attackData == null) return;

        Vector2 offset = attackData.GetHitboxOffset();
        Vector2 size = attackData.GetHitboxSize();

        Vector3 localPos = hitboxObject.transform.localPosition;

        if (controller.FacingRight)
        {
            localPos.x = Mathf.Abs(offset.x);
        }
        else
        {
            localPos.x = -Mathf.Abs(offset.x);
        }

        localPos.y = offset.y;
        localPos.z = 0f;

        hitboxObject.transform.localPosition = localPos;
        hitboxCollider.size = size;
    }

    void UpdateHitboxState()
    {
        bool shouldBeActive =
            controller.IsAttacking &&
            controller.CurrentAttackPhase == AttackPhase.Active;

        if (hitboxObject.activeSelf != shouldBeActive)
        {
            hitboxObject.SetActive(shouldBeActive);
        }
    }

    void OnDisable()
    {
        if (hitboxObject != null)
        {
            hitboxObject.SetActive(false);
        }
    }
}