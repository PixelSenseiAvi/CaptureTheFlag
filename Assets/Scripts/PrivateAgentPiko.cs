using UnityEngine;
using Unity.MLAgents;
using Unity.MLAgents.Sensors;
using Unity.MLAgents.Actuators;

public class PrivateAgentPiko : Agent
{
    [Header("Movement Settings")]
    public float moveSpeed = 5f;
    public float rotationSpeed = 100f;
    public float collisionRepulsionForce = 5f;

    [Header("Team Settings")]
    public string teamId = "TeamA"; // Can be "TeamA" or "TeamB"

    [Header("References")]
    public Transform flagTransform;
    public Transform homeBaseTransform;

    private Rigidbody rb;
    private bool hasFlag = false;
    private Vector3 initialPosition;
    private Quaternion initialRotation;
    private GameObject flagObject;

    public override void Initialize()
    {
        rb = GetComponent<Rigidbody>();
        initialPosition = transform.position;
        initialRotation = transform.rotation;

        // Set the tag based on the team
        gameObject.tag = teamId;

        // Find the flag object
        flagObject = GameObject.FindGameObjectWithTag("Flag");
    }

    public override void OnEpisodeBegin()
    {
        // Reset agent position and rotation
        transform.position = initialPosition;
        transform.rotation = initialRotation;
        rb.linearVelocity = Vector3.zero;
        rb.angularVelocity = Vector3.zero;
        hasFlag = false;
    }

    public override void CollectObservations(VectorSensor sensor)
    {
        // Add agent's position
        sensor.AddObservation(transform.localPosition);

        // Add agent's rotation
        sensor.AddObservation(transform.localRotation.eulerAngles.y);

        // Add flag position
        sensor.AddObservation(flagTransform.localPosition);

        // Add home base position
        sensor.AddObservation(homeBaseTransform.localPosition);

        // Add whether agent has flag
        sensor.AddObservation(hasFlag);

        // Add agent's velocity
        sensor.AddObservation(rb.linearVelocity);

        // Add team information
        sensor.AddObservation(teamId == "TeamA" ? 1 : 0);
    }

    public override void OnActionReceived(ActionBuffers actions)
    {
        // Get continuous actions
        float moveInput = actions.ContinuousActions[0];
        float rotateInput = actions.ContinuousActions[1];

        // Move the agent
        Vector3 movement = transform.forward * moveInput * moveSpeed;
        rb.linearVelocity = movement;

        // Rotate the agent
        transform.Rotate(0f, rotateInput * rotationSpeed * Time.fixedDeltaTime, 0f);

        // Add small negative reward for each step to encourage efficiency
        AddReward(-0.001f);
    }

    public override void Heuristic(in ActionBuffers actionsOut)
    {
        var continuousActionsOut = actionsOut.ContinuousActions;
        continuousActionsOut[0] = Input.GetAxis("Vertical");
        continuousActionsOut[1] = Input.GetAxis("Horizontal");
    }

    private void OnCollisionEnter(Collision collision)
    {
        if (collision.gameObject.CompareTag("Flag") && !hasFlag)
        {
            // Capture flag
            hasFlag = true;
            AddReward(1.0f);
            collision.gameObject.SetActive(false);

            // Notify game manager
            GameManager gameManager = FindObjectOfType<GameManager>();
            if (gameManager != null)
            {
                gameManager.OnFlagCaptured();
            }
        }
        else if (collision.gameObject.CompareTag("HomeBase") && hasFlag)
        {
            // Score point
            AddReward(5.0f);
            hasFlag = false;

            // Notify game manager
            GameManager gameManager = FindObjectOfType<GameManager>();
            if (gameManager != null)
            {
                gameManager.OnPointScored();
            }

            EndEpisode();
        }
        // Handle collision with opposite team player
        else if ((collision.gameObject.CompareTag("TeamA") && teamId == "TeamB") ||
                 (collision.gameObject.CompareTag("TeamB") && teamId == "TeamA"))
        {
            // Apply repulsive force
            Vector3 awayFromOtherPlayer = transform.position - collision.transform.position;
            awayFromOtherPlayer.y = 0; // Keep repulsion force horizontal

            if (awayFromOtherPlayer.magnitude > 0)
            {
                // Normalize and apply force
                awayFromOtherPlayer.Normalize();
                rb.AddForce(awayFromOtherPlayer * collisionRepulsionForce, ForceMode.Impulse);

                // Small negative reward for colliding with opponent
                AddReward(-0.2f);

                // Check if this agent has the flag and got hit
                if (hasFlag)
                {
                    // Drop the flag (penalty for losing the flag)
                    AddReward(-1.0f);

                    // Reset flag to its original position
                    if (flagObject != null)
                    {
                        Flag flag = flagObject.GetComponent<Flag>();
                        if (flag != null)
                        {
                            flag.RespawnFlag();
                        }
                        else
                        {
                            // Fallback in case Flag component is not available
                            flagObject.transform.position = flagTransform.position;
                            flagObject.SetActive(true);
                        }
                    }

                    // Reward opponent for stealing the flag
                    PrivateAgentPiko opponentAgent = collision.gameObject.GetComponent<PrivateAgentPiko>();
                    if (opponentAgent != null)
                    {
                        opponentAgent.AddReward(1.5f);
                    }

                    // Player no longer has the flag
                    hasFlag = false;

                    // Notify game manager
                    GameManager gameManager = FindObjectOfType<GameManager>();
                    if (gameManager != null)
                    {
                        gameManager.OnFlagDropped(transform.position);
                    }
                }
            }
        }
    }

    private void OnCollisionStay(Collision collision)
    {
        if (collision.gameObject.CompareTag("Wall"))
        {
            // Small penalty for hitting walls
            AddReward(-0.1f);
        }
    }

    public bool HasFlag()
    {
        return hasFlag;
    }
}
