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

    [Header("References")]
    public Transform flagTransform;
    public Transform homeBaseTransform; // Home base for this agent

    private Rigidbody rb;
    private bool hasFlag = false;
    private Vector3 initialPosition;
    private Quaternion initialRotation;
    private GameObject flagObject;
    private Vector3 lastFlagPosition;
    private float lastStepTime;
    private string teamTag; // "TeamA" or "TeamB"
    private int collisionCount = 0;
    private const int MAX_COLLISIONS = 2;

    public override void Initialize()
    {
        rb = GetComponent<Rigidbody>();

        if (rb != null)
        {
            rb.isKinematic = false;
            rb.constraints = RigidbodyConstraints.FreezeRotationX | RigidbodyConstraints.FreezeRotationZ | RigidbodyConstraints.FreezePositionY;
        }

        initialPosition = transform.position;
        initialRotation = transform.rotation;
        
        // Check which team we belong to
        teamTag = gameObject.tag;
        if (teamTag != "TeamA" && teamTag != "TeamB")
        {
            Debug.LogWarning("Agent does not have a valid team tag (TeamA or TeamB). Defaulting to TeamA.");
            teamTag = "TeamA";
            gameObject.tag = teamTag;
        }
        
        flagObject = GameObject.FindGameObjectWithTag("Flag");
    }

    public override void OnEpisodeBegin()
    {
        transform.position = initialPosition;
        transform.rotation = initialRotation;
        rb.linearVelocity = Vector3.zero;
        rb.angularVelocity = Vector3.zero;
        hasFlag = false;
        lastStepTime = Time.time;
        collisionCount = 0;

        if (flagTransform != null)
            lastFlagPosition = flagTransform.position;
    }

    public override void CollectObservations(VectorSensor sensor)
    {
        // Agent's position and rotation (4 values)
        sensor.AddObservation(transform.localPosition);
        sensor.AddObservation(transform.localEulerAngles.y / 360f); // Normalized angle

        // Agent's velocity (3 values)
        sensor.AddObservation(rb.linearVelocity);

        // Flag information (4 values)
        sensor.AddObservation(flagTransform.localPosition);
        sensor.AddObservation(hasFlag ? 1f : 0f);

        // Base positions (3 values)
        sensor.AddObservation(homeBaseTransform.localPosition);

        // Directional vectors for better spatial understanding (6 values)
        Vector3 toFlag = (flagTransform.position - transform.position).normalized;
        Vector3 toHome = (homeBaseTransform.position - transform.position).normalized;

        sensor.AddObservation(toFlag);
        sensor.AddObservation(toHome);

        // Distances (2 values)
        sensor.AddObservation(Vector3.Distance(transform.position, flagTransform.position) / 20f); // Normalized
        sensor.AddObservation(Vector3.Distance(transform.position, homeBaseTransform.position) / 20f);
    }

    public override void OnActionReceived(ActionBuffers actionBuffers)
    {
        // Using discrete actions as specified in the rules
        // Branch0: 3 values (0: do nothing, 1: move forward, 2: move backward)
        // Branch1: 3 values (0: do nothing, 1: rotate left, 2: rotate right)
        int moveAction = actionBuffers.DiscreteActions[0];
        int rotateAction = actionBuffers.DiscreteActions[1];

        // Handle movement
        float moveAmount = 0f;
        switch (moveAction)
        {
            case 1: moveAmount = 1.0f; break;  // Forward
            case 2: moveAmount = -1.0f; break; // Backward
        }

        // Handle rotation
        float rotateAmount = 0f;
        switch (rotateAction)
        {
            case 1: rotateAmount = -1.0f; break; // Left
            case 2: rotateAmount = 1.0f; break;  // Right
        }

        // Apply movement
        Vector3 movement = transform.forward * moveAmount * moveSpeed;
        rb.AddForce(movement, ForceMode.Force);

        // Apply rotation
        transform.Rotate(0f, rotateAmount * rotationSpeed * Time.fixedDeltaTime, 0f);

        // Reward shaping for better learning
        RewardShaping();

        // Small time penalty to encourage efficiency (from image: -0.01 per step)
        float timeSinceLastStep = Time.time - lastStepTime;
        AddReward(-0.01f * timeSinceLastStep);
        lastStepTime = Time.time;
    }

    private void RewardShaping()
    {
        if (flagTransform == null) return;

        Vector3 currentFlagPos = flagTransform.position;

        if (!hasFlag)
        {
            // Reward for moving towards flag
            float currentDistToFlag = Vector3.Distance(transform.position, currentFlagPos);
            float lastDistToFlag = Vector3.Distance(transform.position, lastFlagPosition);

            if (currentDistToFlag < lastDistToFlag)
                AddReward(0.001f); // Small reward for moving towards flag
        }
        else
        {
            // Reward for moving towards home base with flag
            float distToHome = Vector3.Distance(transform.position, homeBaseTransform.position);
            float maxDist = 20f; // Adjust based on your arena size
            float progressReward = (maxDist - distToHome) / maxDist * 0.002f;
            AddReward(progressReward);
        }

        lastFlagPosition = currentFlagPos;
    }

    public override void Heuristic(in ActionBuffers actionsOut)
    {
        var discreteActionsOut = actionsOut.DiscreteActions;
        
        // Movement: Forward/Backward (Branch0)
        if (Input.GetKey(KeyCode.W) || Input.GetKey(KeyCode.UpArrow))
            discreteActionsOut[0] = 1; // Forward
        else if (Input.GetKey(KeyCode.S) || Input.GetKey(KeyCode.DownArrow))
            discreteActionsOut[0] = 2; // Backward
        else
            discreteActionsOut[0] = 0; // Do nothing

        // Rotation: Left/Right (Branch1)
        if (Input.GetKey(KeyCode.A) || Input.GetKey(KeyCode.LeftArrow))
            discreteActionsOut[1] = 1; // Left
        else if (Input.GetKey(KeyCode.D) || Input.GetKey(KeyCode.RightArrow))
            discreteActionsOut[1] = 2; // Right
        else
            discreteActionsOut[1] = 0; // Do nothing
    }

    private void OnCollisionEnter(Collision collision)
    {
        if (collision.gameObject.CompareTag("Flag") && !hasFlag)
        {
            CaptureFlagLogic();
        }
        else if (IsHomeBase(collision) && hasFlag)
        {
            ScorePointLogic();
        }
        else if (collision.gameObject.CompareTag("TeamA") || collision.gameObject.CompareTag("TeamB"))
        {
            // Check if the collision is with an agent from the other team
            if (collision.gameObject.tag != gameObject.tag)
            {
                HandleTeamCollision(collision);
            }
        }
    }

    private void HandleTeamCollision(Collision collision)
    {
        // Apply displacement effect (small push away)
        Vector3 awayFromEnemy = transform.position - collision.transform.position;
        awayFromEnemy.y = 0;

        if (awayFromEnemy.magnitude > 0)
        {
            awayFromEnemy.Normalize();
            rb.AddForce(awayFromEnemy * collisionRepulsionForce, ForceMode.Impulse);
        }

        // For Phase 1, we track collisions but only apply basic displacement
        if (hasFlag)
        {
            collisionCount++;
            
            // If hit twice, drop the flag (as per the rules)
            if (collisionCount >= MAX_COLLISIONS)
            {
                DropFlag();
                collisionCount = 0;
            }
        }
    }

    private bool IsHomeBase(Collision collision)
    {
        // Check if this is our home base
        if (teamTag == "TeamA" && collision.gameObject.CompareTag("BaseA"))
            return true;
            
        if (teamTag == "TeamB" && collision.gameObject.CompareTag("BaseB"))
            return true;
            
        return false;
    }

    private void CaptureFlagLogic()
    {
        hasFlag = true;
        // Reward for picking up flag (from image: +0.5)
        AddReward(0.5f);
        flagObject.SetActive(false);

        GameManager gameManager = FindObjectOfType<GameManager>();
        if (gameManager != null)
            gameManager.OnFlagCaptured();
    }

    private void ScorePointLogic()
    {
        // Reward for returning flag to base (from image: +1.0)
        AddReward(1.0f);
        hasFlag = false;
        collisionCount = 0;

        GameManager gameManager = FindObjectOfType<GameManager>();
        if (gameManager != null)
            gameManager.OnPointScored();
    }

    private void DropFlag()
    {
        hasFlag = false;
        collisionCount = 0;

        if (flagObject != null)
        {
            Flag flag = flagObject.GetComponent<Flag>();
            if (flag != null)
            {
                flag.RespawnFlag();
            }
            else
            {
                flagObject.transform.position = flagTransform.position;
                flagObject.SetActive(true);
            }
        }

        GameManager gameManager = FindObjectOfType<GameManager>();
        if (gameManager != null)
            gameManager.OnFlagDropped(transform.position);
    }

    private void OnCollisionStay(Collision collision)
    {
        if (collision.gameObject.CompareTag("OuterWall"))
        {
            AddReward(-0.05f);
        }
    }

    public bool HasFlag()
    {
        return hasFlag;
    }

    private void FixedUpdate()
    {
        // Velocity limiting and stability
        if (rb.linearVelocity.magnitude > moveSpeed)
        {
            rb.linearVelocity = rb.linearVelocity.normalized * moveSpeed;
        }

        if (transform.up.y < 0.5f)
        {
            transform.rotation = Quaternion.Euler(0, transform.eulerAngles.y, 0);
        }
    }
}

