using UnityEngine;
using Unity.MLAgents;
using Unity.MLAgents.Actuators;
using Unity.MLAgents.Sensors;

[RequireComponent(typeof(AgentInput))]
public class FTGAgent : Agent
{
    private const int ObservationSize = 61;

    [Header("References")]
    public AgentInput agentInput;
    public CombatObservationProvider observationProvider;
    public MatchManager matchManager;
    public FighterController selfController;
    public FighterController opponentController;

    [Header("Episode")]
    public bool endEpisodeOnRoundEnd = true;
    public bool restartRoundFromAgent = false;

    [Header("Reward")]
    public float stepPenalty = 0f;

    [Header("Action Mask")]
    public bool useDistanceAwareAttackMask = true;
    public float lightAttackMinDistance = 0f;
    public float lightAttackMaxDistance = 2.2f;
    public float heavyAttackMinDistance = 0.9f;
    public float heavyAttackMaxDistance = 2.2f;

    [Header("Curriculum")]
    public bool disableHeavyAttack = false;

    [Header("Debug")]
    public bool debugActionReceived = false;

    private bool episodeEnding = false;

    public override void Initialize()
    {
        if (agentInput == null)
            agentInput = GetComponent<AgentInput>();

        if (selfController == null)
            selfController = GetComponent<FighterController>();

        if (observationProvider == null)
            DLog.LogError($"{name}: FTGAgent missing CombatObservationProvider reference.");

        if (matchManager == null)
            DLog.LogError($"{name}: FTGAgent missing MatchManager reference.");

        if (agentInput == null)
            DLog.LogError($"{name}: FTGAgent missing AgentInput reference.");
    }

    public override void OnEpisodeBegin()
    {
        episodeEnding = false;

        if (restartRoundFromAgent && matchManager != null)
        {
            matchManager.StartRound();
        }

        if (agentInput != null)
        {
            agentInput.ResetTemporalState();
        }
    }

    public override void CollectObservations(VectorSensor sensor)
    {
        if (observationProvider == null)
        {
            sensor.AddObservation(new float[ObservationSize]);
            return;
        }

        float[] obs = observationProvider.GetObservationVector();

        if (obs == null || obs.Length == 0)
        {
            sensor.AddObservation(new float[ObservationSize]);
            return;
        }

        sensor.AddObservation(obs);
    }

    public override void WriteDiscreteActionMask(IDiscreteActionMask actionMask)
    {
        if (actionMask == null || selfController == null)
            return;

        if (matchManager == null || !matchManager.RoundActive)
        {
            MaskAllActiveControls(actionMask);
            return;
        }

        // Branch index 1: posture, 0 none, 1 jump, 2 crouch
        if (!selfController.CanJump())
        {
            actionMask.SetActionEnabled(1, 1, false);
        }

        if (!selfController.CanCrouch())
        {
            actionMask.SetActionEnabled(1, 2, false);
        }

        // Branch index 2: combat, 0 none, 1 block, 2 light, 3 heavy
        if (selfController.IsAttacking || selfController.IsDashing)
        {
            actionMask.SetActionEnabled(2, 1, false);
        }

        bool canStartAttack = selfController.CanAttack();
        bool canTryRecoveryCancel =
            selfController.allowLightAttackCancel &&
            selfController.IsAttacking &&
            selfController.CurrentAttackPhase == AttackPhase.Recovery;

        if (!canStartAttack && !canTryRecoveryCancel)
        {
            actionMask.SetActionEnabled(2, 2, false);
            actionMask.SetActionEnabled(2, 3, false);
        }

        if (selfController.HasBufferedAction)
        {
            actionMask.SetActionEnabled(2, 2, false);
            actionMask.SetActionEnabled(2, 3, false);
        }

        if (agentInput != null && agentInput.AttackGapActive)
        {
            actionMask.SetActionEnabled(2, 2, false);
            actionMask.SetActionEnabled(2, 3, false);
        }

        if (useDistanceAwareAttackMask && opponentController != null)
        {
            ApplyDistanceAwareAttackMask(actionMask);
        }

        if (disableHeavyAttack)
        {
            actionMask.SetActionEnabled(2, 3, false);
        }
    }

    private void MaskAllActiveControls(IDiscreteActionMask actionMask)
    {
        actionMask.SetActionEnabled(0, 1, false);
        actionMask.SetActionEnabled(0, 2, false);
        actionMask.SetActionEnabled(1, 1, false);
        actionMask.SetActionEnabled(1, 2, false);
        actionMask.SetActionEnabled(2, 1, false);
        actionMask.SetActionEnabled(2, 2, false);
        actionMask.SetActionEnabled(2, 3, false);
    }

    private void ApplyDistanceAwareAttackMask(IDiscreteActionMask actionMask)
    {
        // Coarse horizontal curriculum gate. This is not a final hitbox-range model.
        float distance = Mathf.Abs(opponentController.transform.position.x - selfController.transform.position.x);

        if (distance < lightAttackMinDistance || distance > lightAttackMaxDistance)
        {
            actionMask.SetActionEnabled(2, 2, false);
        }

        if (distance < heavyAttackMinDistance || distance > heavyAttackMaxDistance)
        {
            actionMask.SetActionEnabled(2, 3, false);
        }
    }

    public override void OnActionReceived(ActionBuffers actions)
    {
        if (episodeEnding)
            return;

        if (matchManager == null || !matchManager.RoundActive)
        {
            if (agentInput != null)
                agentInput.SetBranchActions(0, 0, 0);
            return;
        }

        int moveAction = actions.DiscreteActions[0];
        int postureAction = actions.DiscreteActions[1];
        int combatAction = actions.DiscreteActions[2];

        if (debugActionReceived)
        {
            DLog.Log($"OnActionReceived => move:{moveAction}, posture:{postureAction}, combat:{combatAction}");
        }

        if (agentInput != null)
        {
            agentInput.SetBranchActions(moveAction, postureAction, combatAction);
        }

        AddReward(stepPenalty);

        CheckRoundEnd();
    }

    public override void Heuristic(in ActionBuffers actionsOut)
    {
        var discrete = actionsOut.DiscreteActions;

        // move: 0 neutral, 1 left, 2 right
        discrete[0] = 0;

        // posture: 0 none, 1 jump, 2 crouch
        discrete[1] = 0;

        // combat: 0 none, 1 block, 2 light, 3 heavy
        discrete[2] = 0;

        if (Input.GetKey(KeyCode.A))
        {
            discrete[0] = 1;
        }
        else if (Input.GetKey(KeyCode.D))
        {
            discrete[0] = 2;
        }

        if (Input.GetKeyDown(KeyCode.W))
        {
            discrete[1] = 1;
        }
        else if (Input.GetKey(KeyCode.S))
        {
            discrete[1] = 2;
        }

        if (Input.GetKey(KeyCode.LeftShift))
        {
            discrete[2] = 1;
        }
        else if (Input.GetKeyDown(KeyCode.J))
        {
            discrete[2] = 2;
        }
        else if (Input.GetKeyDown(KeyCode.K))
        {
            discrete[2] = 3;
        }
    }

    private void Update()
    {
        if (episodeEnding)
            return;

        CheckRoundEnd();
    }

    private void CheckRoundEnd()
    {
        if (!endEpisodeOnRoundEnd || matchManager == null)
            return;

        if (matchManager.RoundActive)
            return;

        episodeEnding = true;
        EndEpisode();
    }
}
