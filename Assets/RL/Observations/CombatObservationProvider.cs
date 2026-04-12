using System.Collections.Generic;
using UnityEngine;

public class CombatObservationProvider : MonoBehaviour
{
    public FighterController self;
    public FighterController opponent;
    public Health selfHealth;
    public Health opponentHealth;
    public MatchManager matchManager;

    [Header("Normalization")]
    public float positionScale = 10f;
    public float velocityScale = 10f;
    public float timeScale = 30f;
    public float damageRecencyScale = 2f;

    private float timeSinceSelfDamaged = 999f;
    private float timeSinceOpponentDamaged = 999f;

    private void OnEnable()
    {
        Subscribe();
        ResetDamageRecency();
    }

    private void Start()
    {
        Subscribe();
        ResetDamageRecency();
    }

    private void OnDisable()
    {
        Unsubscribe();
    }

    private void OnDestroy()
    {
        Unsubscribe();
    }

    private void Update()
    {
        timeSinceSelfDamaged += Time.deltaTime;
        timeSinceOpponentDamaged += Time.deltaTime;
    }

    private void Subscribe()
    {
        if (selfHealth != null)
        {
            selfHealth.OnDamaged -= HandleSelfDamaged;
            selfHealth.OnDamaged += HandleSelfDamaged;
        }

        if (opponentHealth != null)
        {
            opponentHealth.OnDamaged -= HandleOpponentDamaged;
            opponentHealth.OnDamaged += HandleOpponentDamaged;
        }

        if (matchManager != null)
        {
            matchManager.OnRoundStarted -= HandleRoundStarted;
            matchManager.OnRoundStarted += HandleRoundStarted;
        }
    }

    private void Unsubscribe()
    {
        if (selfHealth != null)
            selfHealth.OnDamaged -= HandleSelfDamaged;

        if (opponentHealth != null)
            opponentHealth.OnDamaged -= HandleOpponentDamaged;

        if (matchManager != null)
            matchManager.OnRoundStarted -= HandleRoundStarted;
    }

    private void HandleSelfDamaged(int damage)
    {
        timeSinceSelfDamaged = 0f;
    }

    private void HandleOpponentDamaged(int damage)
    {
        timeSinceOpponentDamaged = 0f;
    }

    private void HandleRoundStarted()
    {
        ResetDamageRecency();
    }

    private void ResetDamageRecency()
    {
        timeSinceSelfDamaged = damageRecencyScale;
        timeSinceOpponentDamaged = damageRecencyScale;
    }

    public float[] GetObservationVector()
    {
        List<float> obs = new List<float>(32);

        if (self == null || opponent == null || selfHealth == null || opponentHealth == null)
        {
            return obs.ToArray();
        }

        Vector3 selfPos = self.transform.position;
        Vector3 oppPos = opponent.transform.position;

        Vector2 selfVel = self.Velocity;
        Vector2 oppVel = opponent.Velocity;

        obs.Add(selfPos.x / positionScale);
        obs.Add(selfPos.y / positionScale);
        obs.Add(selfVel.x / velocityScale);
        obs.Add(selfVel.y / velocityScale);
        obs.Add((float)selfHealth.currentHealth / Mathf.Max(1, selfHealth.maxHealth));
        obs.Add(self.IsGrounded ? 1f : 0f);
        obs.Add(self.IsCrouching ? 1f : 0f);
        obs.Add(self.IsBlocking ? 1f : 0f);
        obs.Add(self.FacingRight ? 1f : 0f);
        AddStateOneHot(obs, self.CurrentState);
        AddAttackPhaseOneHot(obs, self.CurrentAttackPhase);

        obs.Add(oppPos.x / positionScale);
        obs.Add(oppPos.y / positionScale);
        obs.Add(oppVel.x / velocityScale);
        obs.Add(oppVel.y / velocityScale);
        obs.Add((float)opponentHealth.currentHealth / Mathf.Max(1, opponentHealth.maxHealth));
        obs.Add(opponent.IsGrounded ? 1f : 0f);
        obs.Add(opponent.IsCrouching ? 1f : 0f);
        obs.Add(opponent.IsBlocking ? 1f : 0f);
        obs.Add(opponent.FacingRight ? 1f : 0f);
        AddStateOneHot(obs, opponent.CurrentState);
        AddAttackPhaseOneHot(obs, opponent.CurrentAttackPhase);

        obs.Add((oppPos.x - selfPos.x) / positionScale);
        obs.Add((oppPos.y - selfPos.y) / positionScale);
        obs.Add(Mathf.Abs(oppPos.x - selfPos.x) / positionScale);

        if (matchManager != null)
        {
            obs.Add(matchManager.RoundTimeRemaining / timeScale);
            obs.Add(matchManager.RoundActive ? 1f : 0f);
        }
        else
        {
            obs.Add(0f);
            obs.Add(0f);
        }

        obs.Add(self.CanAttack() ? 1f : 0f);
        obs.Add(opponent.CanAttack() ? 1f : 0f);
        obs.Add(opponent.CurrentAttackPhase == AttackPhase.Active ? 1f : 0f);
        obs.Add(opponent.CurrentAttackPhase == AttackPhase.Recovery ? 1f : 0f);
        obs.Add(Mathf.Clamp01(timeSinceSelfDamaged / Mathf.Max(0.01f, damageRecencyScale)));
        obs.Add(Mathf.Clamp01(timeSinceOpponentDamaged / Mathf.Max(0.01f, damageRecencyScale)));
        obs.Add(self.IsInHitstun ? 1f : 0f);
        obs.Add(opponent.IsInHitstun ? 1f : 0f);

        return obs.ToArray();
    }

    void AddStateOneHot(List<float> obs, CharacterState state)
    {
        CharacterState[] states = new CharacterState[]
        {
            CharacterState.Idle,
            CharacterState.Move,
            CharacterState.Jump,
            CharacterState.Fall,
            CharacterState.Dash,
            CharacterState.Crouch,
            CharacterState.Attack,
            CharacterState.Hitstun,
            CharacterState.Block,
            CharacterState.CrouchBlock,
            CharacterState.Blockstun
        };

        for (int i = 0; i < states.Length; i++)
        {
            obs.Add(state == states[i] ? 1f : 0f);
        }
    }

    void AddAttackPhaseOneHot(List<float> obs, AttackPhase phase)
    {
        AttackPhase[] phases = new AttackPhase[]
        {
            AttackPhase.None,
            AttackPhase.Startup,
            AttackPhase.Active,
            AttackPhase.Recovery
        };

        for (int i = 0; i < phases.Length; i++)
        {
            obs.Add(phase == phases[i] ? 1f : 0f);
        }
    }
}
