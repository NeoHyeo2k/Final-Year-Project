using UnityEngine;
using Unity.MLAgents;
using Unity.MLAgents.Actuators;
using Unity.MLAgents.Sensors;

[RequireComponent(typeof(AgentInput))]
public class FTGAgent : Agent
{
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
    // public float stepPenalty = -0.0005f;

    private bool episodeEnding = false;

    public override void Initialize()
    {
        if (agentInput == null)
            agentInput = GetComponent<AgentInput>();

        if (selfController == null)
            selfController = GetComponent<FighterController>();

        if (observationProvider == null)
            Debug.LogError($"{name}: FTGAgent missing CombatObservationProvider reference.");

        if (matchManager == null)
            Debug.LogError($"{name}: FTGAgent missing MatchManager reference.");

        if (agentInput == null)
            Debug.LogError($"{name}: FTGAgent missing AgentInput reference.");
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
            agentInput.SetDiscreteAction(0);
        }
    }

    public override void CollectObservations(VectorSensor sensor)
    {
        if (observationProvider == null)
        {
            sensor.AddObservation(new float[45]);
            return;
        }

        float[] obs = observationProvider.GetObservationVector();

        if (obs == null || obs.Length == 0)
        {
            sensor.AddObservation(new float[45]);
            return;
        }

        sensor.AddObservation(obs);
    }

    public override void OnActionReceived(ActionBuffers actions)
    {
        Debug.Log("OnActionReceived called: " + actions.DiscreteActions[0]);

        if (episodeEnding)
            return;

        if (matchManager == null || !matchManager.RoundActive)
        {
            if (agentInput != null)
                agentInput.SetDiscreteAction(0);
            return;
        }

        int actionId = actions.DiscreteActions[0];

        if (agentInput != null)
        {
            agentInput.SetDiscreteAction(actionId);
        }

        AddReward(stepPenalty);

        CheckRoundEnd();
    }

    public override void Heuristic(in ActionBuffers actionsOut)
    {
        var discrete = actionsOut.DiscreteActions;
        discrete[0] = 0;

        if (Input.GetKey(KeyCode.A))
        {
            discrete[0] = 1;
        }
        else if (Input.GetKey(KeyCode.D))
        {
            discrete[0] = 2;
        }
        else if (Input.GetKeyDown(KeyCode.W))
        {
            discrete[0] = 3;
        }
        else if (Input.GetKey(KeyCode.S))
        {
            discrete[0] = 4;
        }
        else if (Input.GetKey(KeyCode.LeftShift))
        {
            discrete[0] = 5;
        }
        else if (Input.GetKeyDown(KeyCode.J))
        {
            discrete[0] = 6;
        }
        else if (Input.GetKeyDown(KeyCode.K))
        {
            discrete[0] = 7;
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

        bool isDraw = matchManager.IsDraw;
        bool isWinner = matchManager.Winner != null && selfController != null && matchManager.Winner == selfController;
        bool isLoser = matchManager.Loser != null && selfController != null && matchManager.Loser == selfController;

        if (isDraw)
        {
            EndEpisode();
            return;
        }

        if (isWinner)
        {
            EndEpisode();
            return;
        }

        if (isLoser)
        {
            EndEpisode();
            return;
        }

        EndEpisode();
    }
}