using System;
using System.Collections.Generic;
using UnityEngine;

public class HitboxController : MonoBehaviour
{
    public LayerMask targetLayer;

    public static event Action<FighterController, FighterController, AttackData, int> OnGlobalAttackHit;
    public static event Action<FighterController, FighterController, AttackData> OnGlobalAttackBlocked;

    private FighterController ownerController;
    private HashSet<FighterController> hitTargets = new HashSet<FighterController>();

    void Start()
    {
        ownerController = GetComponentInParent<FighterController>();

        if (ownerController == null)
        {
            DLog.LogError("FighterController not found in parent of " + gameObject.name);
        }
    }

    void OnEnable()
    {
        hitTargets.Clear();
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        TryHit(other);
    }

    void TryHit(Collider2D other)
    {
        if (ownerController == null) return;

        if (!gameObject.activeInHierarchy) return;
        if (!ownerController.IsAttacking) return;
        if (ownerController.CurrentAttackData == null) return;

        if (((1 << other.gameObject.layer) & targetLayer) == 0)
            return;

        HurtboxController hurtbox = other.GetComponent<HurtboxController>();
        if (hurtbox == null) return;

        FighterController targetController = hurtbox.OwnerController;
        Health targetHealth = hurtbox.OwnerHealth;

        if (targetController == null || targetHealth == null)
            return;

        if (targetController == ownerController)
            return;

        if (hitTargets.Contains(targetController))
            return;

        hitTargets.Add(targetController);

        AttackData attackData = ownerController.CurrentAttackData;
        int damage = attackData.damage;
        float hitstunTime = attackData.hitstunTime;
        float knockbackForce = attackData.pushbackForce;

        if (targetController.CanBlockAttack(attackData))
        {
            float blockstunTime = hitstunTime * 0.5f;
            if (blockstunTime <= 0f)
            {
                blockstunTime = targetController.defaultBlockstunDuration;
            }

            targetController.ReceiveBlockstun(blockstunTime);
            targetController.ApplyBlockPush(knockbackForce, ownerController.transform);

            OnGlobalAttackBlocked?.Invoke(ownerController, targetController, attackData);

            DLog.Log(
                ownerController.gameObject.name +
                " attack was BLOCKED by " +
                targetController.gameObject.name +
                " using " +
                attackData.attackName +
                " [" + attackData.attackType + "]"
            );

            return;
        }

        targetHealth.TakeDamage(damage);
        ownerController.NotifyAttackHit();

        targetController.ReceiveHitstun(hitstunTime);
        targetController.ApplyKnockback(knockbackForce, ownerController.transform);
        targetController.RegisterHit();

        OnGlobalAttackHit?.Invoke(ownerController, targetController, attackData, damage);

        DLog.Log(
            ownerController.gameObject.name +
            " hit " +
            targetController.gameObject.name +
            " at " +
            hurtbox.HurtboxType +
            " with " +
            attackData.attackName +
            " [" + attackData.attackType + "] for " +
            damage +
            " damage."
        );
    }
}