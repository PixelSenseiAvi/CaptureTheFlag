// CTFAgent.cs
using Unity.MLAgents;
using Unity.MLAgents.Actuators;
using Unity.MLAgents.Sensors;
using UnityEngine;
using UnityEngine.AI;

public enum Team { Yellow, Blue }

public class MinionAgent : Agent
{
    [Header("Team")]
    public Team team;

    [Header("Refs")]
    public NavMeshAgent navAgent;
    public Transform myFlag;
    public Transform enemyFlag;
    public Transform spawnPoint;

    private Vector3 _startPos;

    public override void Initialize()
    {
        _startPos = transform.position;
    }

    public override void OnEpisodeBegin()
    {
        transform.position = spawnPoint.position;
        navAgent.ResetPath();
    }

    public override void CollectObservations(VectorSensor sensor)
    {
        // 3 × 3 = 9 floats
        sensor.AddObservation(transform.position);     // 3
        sensor.AddObservation(myFlag.position);        // 3
        sensor.AddObservation(enemyFlag.position);     // 3
    }

    public override void OnActionReceived(ActionBuffers actions)
    {
        // var moveX = actions.ContinuousActions[0];
        // var moveZ = actions.ContinuousActions[1];
        //
        // Vector3 target = transform.position + new Vector3(moveX, 0, moveZ) * 5f;
        // navAgent.SetDestination(target);
        //
        // // Small time penalty to encourage speed
        // AddReward(-1f / MaxStep);
        
        Vector3 target = transform.position + transform.forward * 5f;
        NavMeshHit hit;
        if (NavMesh.SamplePosition(target, out hit, 3f, NavMesh.AllAreas))
            navAgent.SetDestination(hit.position);
        else
            navAgent.SetDestination(transform.position + transform.forward); 
    }

    public override void Heuristic(in ActionBuffers actionsOut)
    {
        var c = actionsOut.ContinuousActions;
        c[0] = Input.GetAxis("Horizontal");
        c[1] = Input.GetAxis("Vertical");
    }
}
