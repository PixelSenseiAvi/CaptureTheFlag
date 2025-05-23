using UnityEngine;
using Unity.MLAgents;
using Unity.MLAgents.Sensors;
using Unity.MLAgents.Actuators;

public class PrivateAgentPikachu : Agent
{
    [Header("Movement Settings")]
    public float moveSpeed = 5f;
    public float rotationSpeed = 100f;
    public float collisionRepulsionForce = 5f;

    [Header("References")]
    public Transform flagTransform;
    public Transform homeBaseTransform; // TeamB base
    public Transform enemyBaseTransform; // TeamA base
    public Transform enemyAgentTransform; // Reference to TeamA agent

    private Rigidbody rb;
    private bool hasFlag = false;
    private Vector3 initialPosition;
    private Quaternion initialRotation;
    private GameObject flagObject;
    private Vector3 lastFlagPosition;
    private Vector3 lastEnemyPosition;
    private float lastStepTime;

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
        gameObject.tag = "TeamB";
        flagObject = GameObject.FindGameObjectWithTag("Flag");

        // Find enemy agent if not assigned
        if (enemyAgentTransform == null)
        {
            GameObject enemyAgent = GameObject.FindWithTag("TeamA");
            if (enemyAgent != null)
                enemyAgentTransform = enemyAgent.transform;
        }
    }

    public override void OnEpisodeBegin()
    {
        transform.position = initialPosition;
        transform.rotation = initialRotation;
        rb.linearVelocity = Vector3.zero;
        rb.angularVelocity = Vector3.zero;
        hasFlag = false;
        lastStepTime = Time.time;

        if (flagTransform != null)
            lastFlagPosition = flagTransform.position;
        if (enemyAgentTransform != null)
            lastEnemyPosition = enemyAgentTransform.position;
    }

    public override void CollectObservations(VectorSensor sensor)
    {
        // Agent's position and rotation (4 values)
        sensor.AddObservation(transform.localPosition);
        sensor.AddObservation(transform.localEulerAngles.y / 360f);

        // Agent's velocity (3 values)
        sensor.AddObservation(rb.linearVelocity);

        // Flag information (4 values)
        sensor.AddObservation(flagTransform.localPosition);
        sensor.AddObservation(hasFlag ? 1f : 0f);

        // Base positions (6 values)
        sensor.AddObservation(homeBaseTransform.localPosition);
        sensor.AddObservation(enemyBaseTransform.localPosition);

        // Enemy agent information (4 values)
        if (enemyAgentTransform != null)
        {
            sensor.AddObservation(enemyAgentTransform.localPosition);
            PrivateAgentPiko enemyAgent = enemyAgentTransform.GetComponent<PrivateAgentPiko>();
            sensor.AddObservation(enemyAgent != null && enemyAgent.HasFlag() ? 1f : 0f);
        }
        else
        {
            sensor.AddObservation(Vector3.zero);
            sensor.AddObservation(0f);
        }

        // Directional vectors (9 values)
        Vector3 toFlag = (flagTransform.position - transform.position).normalized;
        Vector3 toHome = (homeBaseTransform.position - transform.position).normalized;
        Vector3 toEnemy = enemyAgentTransform != null ?
            (enemyAgentTransform.position - transform.position).normalized : Vector3.zero;

        sensor.AddObservation(toFlag);
        sensor.AddObservation(toHome);
        sensor.AddObservation(toEnemy);

        // Distances (3 values)
        sensor.AddObservation(Vector3.Distance(transform.position, flagTransform.position) / 20f);
        sensor.AddObservation(Vector3.Distance(transform.position, homeBaseTransform.position) / 20f);
        sensor.AddObservation(enemyAgentTransform != null ?
            Vector3.Distance(transform.position, enemyAgentTransform.position) / 20f : 0f);
    }

    public override void OnActionReceived(ActionBuffers actions)
    {
        float moveInput = actions.ContinuousActions[0];
        float rotateInput = actions.ContinuousActions[1];

        Vector3 movement = transform.forward * moveInput * moveSpeed;
        rb.AddForce(movement, ForceMode.Force);
        transform.Rotate(0f, rotateInput * rotationSpeed * Time.fixedDeltaTime, 0f);

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
            float currentDistToFlag = Vector3.Distance(transform.position, currentFlagPos);
            float lastDistToFlag = Vector3.Distance(transform.position, lastFlagPosition);

            if (currentDistToFlag < lastDistToFlag)
                AddReward(0.001f); // Small reward for moving towards flag
        }
        else
        {
            float distToHome = Vector3.Distance(transform.position, homeBaseTransform.position);
            float maxDist = 20f;
            float progressReward = (maxDist - distToHome) / maxDist * 0.002f;
            AddReward(progressReward);
        }

        lastFlagPosition = currentFlagPos;
        if (enemyAgentTransform != null)
            lastEnemyPosition = enemyAgentTransform.position;
    }

    public override void Heuristic(in ActionBuffers actionsOut)
    {
        var continuousActionsOut = actionsOut.ContinuousActions;
        continuousActionsOut[0] = Input.GetAxisRaw("Vertical");
        continuousActionsOut[1] = Input.GetAxisRaw("Horizontal");
    }

    private void OnCollisionEnter(Collision collision)
    {
        if (collision.gameObject.CompareTag("Flag") && !hasFlag)
        {
            CaptureFlagLogic();
        }
        else if (collision.gameObject.CompareTag("BaseB") && hasFlag) // Check for TeamB base
        {
            ScorePointLogic();
        }
        else if (collision.gameObject.CompareTag("TeamA"))
        {
            HandleEnemyCollision(collision);
        }
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

        GameManager gameManager = FindObjectOfType<GameManager>();
        if (gameManager != null)
            gameManager.OnPointScored();
    }

    private void HandleEnemyCollision(Collision collision)
    {
        // In Phase 1, we don't have tagging, but we'll keep basic collision handling
        Vector3 awayFromEnemy = transform.position - collision.transform.position;
        awayFromEnemy.y = 0;

        if (awayFromEnemy.magnitude > 0)
        {
            awayFromEnemy.Normalize();
            rb.AddForce(awayFromEnemy * collisionRepulsionForce, ForceMode.Impulse);
        }

        if (hasFlag)
        {
            // In Phase 1, we don't drop the flag on collision with enemy
            // This will be implemented in Phase 2
        }
    }

    private void DropFlag()
    {
        // This is for Phase 2 but keeping the structure for later
        hasFlag = false;
        
        // Penalty for dropping flag (from image: -0.1)
        AddReward(-0.1f);

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
