using UnityEngine;

[RequireComponent(typeof(FTGAgent))]
public class DamageReward : MonoBehaviour
{
    [Header("References")]
    public FTGAgent agent;
    public Health selfHealth;
    public Health opponentHealth;

    [Header("Reward Settings")]
    public float rewardPerHitEvent = 0.4f;
    public float penaltyPerHitTakenEvent = -0.45f;

    [Header("Optional Damage Scaling")]
    public bool scaleByDamageAmount = false;
    public float rewardPerDamagePoint = 0f;
    public float penaltyPerDamagePointTaken = 0f;

    private void Awake()
    {
        if (agent == null)
            agent = GetComponent<FTGAgent>();
    }

    private void OnEnable()
    {
        Subscribe();
    }

    private void Start()
    {
        Subscribe();
    }

    private void OnDisable()
    {
        Unsubscribe();
    }

    private void OnDestroy()
    {
        Unsubscribe();
    }

    private void Subscribe()
    {
        if (selfHealth != null)
        {
            selfHealth.OnDamaged -= OnSelfDamaged;
            selfHealth.OnDamaged += OnSelfDamaged;
        }

        if (opponentHealth != null)
        {
            opponentHealth.OnDamaged -= OnOpponentDamaged;
            opponentHealth.OnDamaged += OnOpponentDamaged;
        }
    }

    private void Unsubscribe()
    {
        if (selfHealth != null)
            selfHealth.OnDamaged -= OnSelfDamaged;

        if (opponentHealth != null)
            opponentHealth.OnDamaged -= OnOpponentDamaged;
    }

    private void OnSelfDamaged(int damage)
    {
        if (agent == null)
            return;

        float reward = penaltyPerHitTakenEvent;

        if (scaleByDamageAmount)
        {
            reward += penaltyPerDamagePointTaken * damage;
        }

        agent.AddReward(reward);
    }

    private void OnOpponentDamaged(int damage)
    {
        if (agent == null)
            return;

        float reward = rewardPerHitEvent;

        if (scaleByDamageAmount)
        {
            reward += rewardPerDamagePoint * damage;
        }

        agent.AddReward(reward);
    }
}