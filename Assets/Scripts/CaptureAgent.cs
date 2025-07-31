using UnityEngine;
using Unity.MLAgents;
using Unity.MLAgents.Sensors;
using Unity.MLAgents.Actuators;
using UnityEngine.AI;

public class CaptureAgent : Agent
{
    [Header("Scene refs")]
    public Transform flag;            // opponent flag
    public Transform myBase;          // agent's own base
    public float moveSpeed = 2f;

    [Header("NavMesh Settings")]
    public float stoppingDistance = 0.5f;
    public float pathUpdateInterval = 0.5f; // How often to recalculate path

    private NavMeshAgent navAgent;
    private Rigidbody rb;
    private float lastPathUpdateTime;
    private bool hasFlag = false;

    void Start()
    {
        rb = GetComponent<Rigidbody>();
        navAgent = GetComponent<NavMeshAgent>();

        // Configure NavMeshAgent
        if (navAgent != null)
        {
            navAgent.speed = moveSpeed;
            navAgent.stoppingDistance = stoppingDistance;
            navAgent.angularSpeed = 360f; // Quick turning
            navAgent.acceleration = 8f;
        }
    }

    public override void CollectObservations(VectorSensor sensor)
    {
        // Agent position and velocity (6 floats)
        sensor.AddObservation(transform.localPosition);
        sensor.AddObservation(rb.linearVelocity);

        // Flag position (3 floats)
        sensor.AddObservation(flag.localPosition);

        // Base position (3 floats)
        sensor.AddObservation(myBase.localPosition);

        // Has flag? (1 float)
        sensor.AddObservation(hasFlag ? 1f : 0f);

        // Distance to target (1 float)
        float distanceToTarget = hasFlag ?
            Vector3.Distance(transform.position, myBase.position) :
            Vector3.Distance(transform.position, flag.position);
        sensor.AddObservation(distanceToTarget);

        // NavMesh path status (1 float)
        sensor.AddObservation(navAgent.hasPath ? 1f : 0f);

        // Total: 15 observations
    }

    public override void OnActionReceived(ActionBuffers actions)
    {
        // Action 0: Move strategy (0 = direct control, 1 = navmesh to flag, 2 = navmesh to base)
        int moveStrategy = Mathf.RoundToInt(actions.DiscreteActions[0]);

        // Actions 1-2: Direct movement input (for when not using NavMesh)
        float moveX = actions.ContinuousActions[0];
        float moveZ = actions.ContinuousActions[1];

        // Apply movement based on strategy
        switch (moveStrategy)
        {
            case 0: // Direct control (useful for exploration or obstacle avoidance)
                UseDirectControl(moveX, moveZ);
                break;
            case 1: // Navigate to flag
                if (!hasFlag && flag.gameObject.activeSelf)
                {
                    NavigateToTarget(flag.position);
                }
                else
                {
                    UseDirectControl(moveX, moveZ);
                }
                break;
            case 2: // Navigate to base
                if (hasFlag)
                {
                    NavigateToTarget(myBase.position);
                }
                else
                {
                    UseDirectControl(moveX, moveZ);
                }
                break;
        }

        // Small time penalty to encourage efficiency
        AddReward(MaxStep > 0 ? -1f / MaxStep : -0.001f);

        // Additional rewards for good behavior
        if (navAgent.hasPath && !navAgent.pathPending)
        {
            // Reward for having a valid path
            AddReward(0.001f);

            // Reward for making progress along path
            if (navAgent.remainingDistance < navAgent.stoppingDistance)
            {
                AddReward(0.01f);
            }
        }
    }

    private void UseDirectControl(float moveX, float moveZ)
    {
        // Disable NavMeshAgent when using direct control
        if (navAgent.enabled)
        {
            navAgent.ResetPath();
            navAgent.enabled = false;
        }

        float moveThreshold = 0.1f;
        Vector3 moveInput = new Vector3(moveX, 0, moveZ);

        if (moveInput.magnitude > moveThreshold)
        {
            Vector3 move = moveInput.normalized * moveSpeed;
            rb.linearVelocity = new Vector3(move.x, rb.linearVelocity.y, move.z);

            // Rotate to face movement direction
            if (move != Vector3.zero)
            {
                transform.rotation = Quaternion.LookRotation(move);
            }
        }
        else
        {
            rb.linearVelocity = new Vector3(0, rb.linearVelocity.y, 0);
        }
    }

    private void NavigateToTarget(Vector3 targetPosition)
    {
        // Enable NavMeshAgent for pathfinding
        if (!navAgent.enabled)
        {
            navAgent.enabled = true;
            rb.linearVelocity = Vector3.zero; // Clear any manual velocity
        }

        // Update path periodically or if target moved significantly
        if (Time.time - lastPathUpdateTime > pathUpdateInterval ||
            !navAgent.hasPath ||
            navAgent.destination != targetPosition)
        {
            navAgent.SetDestination(targetPosition);
            lastPathUpdateTime = Time.time;
        }
    }

    public override void Heuristic(in ActionBuffers actionsOut)
    {
        var discreteActions = actionsOut.DiscreteActions;
        var continuousActions = actionsOut.ContinuousActions;

        // Keyboard controls for testing
        if (Input.GetKey(KeyCode.Space))
        {
            // Use NavMesh navigation
            discreteActions[0] = hasFlag ? 2 : 1; // Go to base if has flag, else go to flag
        }
        else
        {
            // Direct control
            discreteActions[0] = 0;
            continuousActions[0] = Input.GetAxis("Horizontal");
            continuousActions[1] = Input.GetAxis("Vertical");
        }
    }

    void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("YellowFlag") && other.gameObject.activeSelf)
        {
            AddReward(1f);          // picked up opponent flag
            hasFlag = true;
            other.gameObject.SetActive(false);

            // Immediately start navigating to base
            if (navAgent.enabled)
            {
                navAgent.SetDestination(myBase.position);
            }
        }

        if (other.CompareTag("YellowBase") && other.transform == myBase && hasFlag)
        {
            AddReward(2f);          // returned flag to my base — win
            EndEpisode();
        }
    }

    public override void OnEpisodeBegin()
    {
        // Reset agent state
        hasFlag = false;

        // Random start position
        Vector3 randomStart = new Vector3(Random.Range(-4, 4), 0.5f, Random.Range(-4, 4));

        // Ensure starting position is on NavMesh
        NavMeshHit hit;
        if (NavMesh.SamplePosition(randomStart, out hit, 5f, NavMesh.AllAreas))
        {
            transform.position = hit.position;
        }
        else
        {
            transform.position = randomStart;
        }

        // Reset physics
        rb.linearVelocity = Vector3.zero;
        rb.angularVelocity = Vector3.zero;

        // Reset NavMeshAgent
        if (navAgent.enabled)
        {
            navAgent.ResetPath();
        }

        // Reset flag
        flag.gameObject.SetActive(true);
    }

    void OnDrawGizmos()
    {
        if (navAgent != null && navAgent.enabled && navAgent.hasPath)
        {
            // Draw the NavMesh path
            Gizmos.color = Color.yellow;
            var path = navAgent.path;
            for (int i = 0; i < path.corners.Length - 1; i++)
            {
                Gizmos.DrawLine(path.corners[i], path.corners[i + 1]);
            }
        }
    }
}