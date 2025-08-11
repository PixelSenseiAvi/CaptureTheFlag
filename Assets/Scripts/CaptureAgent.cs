using Unity.MLAgents;
using Unity.MLAgents.Sensors;
using Unity.MLAgents.Actuators;
using UnityEngine;
using System.Collections;

[RequireComponent(typeof(Rigidbody))]
public class CaptureAgent : Agent
{
    [Header("Scene References")]
    [SerializeField] private Transform targetTransform;   // opponent flag
    [SerializeField] private Transform homeBase;         // agent's starting area

    [Header("Boundary Settings")]
    [SerializeField] private float minZBoundary = -10f;
    [SerializeField] private float maxZBoundary = 10f;
    [SerializeField] private float minXBoundary = -10f;
    [SerializeField] private float maxXBoundary = 10f;
    [SerializeField] private float boundaryPenalty = 0.1f;

    [Header("Reward Settings")]
    [SerializeField] private float moveSpeed = 2f;
    [SerializeField] private float distanceRewardScale = 1f;   // strength of dense reward
    [SerializeField] private float opponentSideReward = 0.3f;  // reward for reaching opponent territory
    [SerializeField] private float minDistanceReward = 0.01f;  // min reward for distance improvement
    [SerializeField] private float maxDistanceReward = 0.1f;   // max reward for distance improvement

    [Header("Wall bounce settings")]
    [SerializeField] private float bounceForce = 5f;        // how hard we push back
    [SerializeField] private float respawnRadius = 3f;      // max distance from impact point
    [SerializeField] private LayerMask walkableMask = 1;    // layer(s) considered valid floor

    private Vector3 startPosition;
    private float lastDistance;
    private bool hasReachedOpponentSide;

    private Rigidbody rb;
    private Quaternion startRotation;

    public GameObject flagCaptured;
    private float endEpisodeTime = -1f;

    /* ----------------------------------------------------------- */
    /*  Life-cycle                                                 */
    /* ----------------------------------------------------------- */

    private void Awake()
    {
        rb = GetComponent<Rigidbody>(); // Cache Rigidbody
        startPosition = transform.position;
        startRotation = transform.rotation;
        if (!homeBase) homeBase = transform;
    }

    public override void OnEpisodeBegin()
    {
        // Reset position & velocity
        transform.position = GetRandomSpawnPosition();
        transform.rotation = startRotation;

        rb.linearVelocity = Vector3.zero;
        rb.angularVelocity = Vector3.zero;

        lastDistance = Vector3.Distance(transform.position, targetTransform.position);
        hasReachedOpponentSide = false;

        flagCaptured.SetActive(false);
        targetTransform.gameObject.SetActive(true);
    }

    private Vector3 GetRandomSpawnPosition()
    {
        Vector3 randomOffset = new Vector3(
            Random.Range(-1f, 1f),
            0,
            Random.Range(-1f, 1f)
        );

        Vector3 spawnPos = startPosition + randomOffset;
        spawnPos.y = startPosition.y;
        return ClampPositionToBoundary(spawnPos);
    }

    /* ----------------------------------------------------------- */
    /*  Observations                                               */
    /* ----------------------------------------------------------- */

    public override void CollectObservations(VectorSensor sensor)
    {
        Vector3 toGoal = targetTransform.position - transform.position;
        Vector3 local = transform.InverseTransformDirection(toGoal.normalized);
        sensor.AddObservation(local.x);   // left/right
        sensor.AddObservation(local.z);   // forward(+) / backward(-)
        sensor.AddObservation(toGoal.magnitude);
    }

    /* ----------------------------------------------------------- */
    /*  Actions & Rewards                                          */
    /* ----------------------------------------------------------- */

    public override void OnActionReceived(ActionBuffers actions)
    {
        // Handle movement
        float moveX = actions.ContinuousActions[0];
        float moveZ = actions.ContinuousActions[1];

        Vector3 move = new Vector3(moveX, 0f, moveZ).normalized;
        Vector3 newPosition = transform.position + move * Time.deltaTime * moveSpeed;

        // Apply boundary constraints
        newPosition = ClampPositionToBoundary(newPosition);

        // Apply boundary penalty if hitting edge
        if (newPosition.x == transform.position.x || newPosition.z == transform.position.z)
        {
            AddReward(-boundaryPenalty);
        }

        transform.position = newPosition;

        // 1. Calculate distance reward
        float newDistance = Vector3.Distance(transform.position, targetTransform.position);
        float distanceDelta = lastDistance - newDistance;

        // Scale reward based on progress
        float progressReward = Mathf.Clamp(
            distanceDelta * distanceRewardScale,
            minDistanceReward,
            maxDistanceReward
        );
        AddReward(progressReward);
        lastDistance = newDistance;

        // 2. Check for opponent side entry
        if (!hasReachedOpponentSide && IsOnOpponentSide())
        {
            AddReward(opponentSideReward);
            hasReachedOpponentSide = true;
        }
    }

    private Vector3 ClampPositionToBoundary(Vector3 position)
    {
        return new Vector3(
            Mathf.Clamp(position.x, minXBoundary, maxXBoundary),
            position.y,
            Mathf.Clamp(position.z, minZBoundary, maxZBoundary)
        );
    }

    private bool IsOnOpponentSide()
    {
        // Calculate the midpoint of the play area
        float midpointZ = (minZBoundary + maxZBoundary) / 2f;

        // Determine which side is opponent territory based on start position
        bool opponentSideIsTop = startPosition.z < midpointZ;

        // Check if agent has crossed to the opponent's side
        return opponentSideIsTop ?
            transform.position.z > midpointZ :
            transform.position.z < midpointZ;
    }

    /* ----------------------------------------------------------- */
    /*  Heuristic & Events                                         */
    /* ----------------------------------------------------------- */

    public override void Heuristic(in ActionBuffers actionsOut)
    {
        var c = actionsOut.ContinuousActions;
        c[0] = Input.GetAxisRaw("Horizontal");
        c[1] = Input.GetAxisRaw("Vertical");
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.TryGetComponent<Goal>(out _))
        {
            AddReward(1.0f);
            flagCaptured.SetActive(true);
            targetTransform.gameObject.SetActive(false);


            endEpisodeTime = Time.time + 1.5f;
            EndEpisode();
            return;
        }

        if (other.TryGetComponent<Wall>(out _))
        {
            AddReward(-0.25f);
            Vector3 pushDir = (transform.position - other.ClosestPoint(transform.position)).normalized;
            pushDir.y = 0;
            GetComponent<Rigidbody>().AddForce(pushDir * bounceForce, ForceMode.VelocityChange);

            Vector3 newPos = RandomPointNear(transform.position, respawnRadius);
            if (ValidFloor(newPos))
                transform.position = newPos;
            else
                transform.position = startPosition;
        }
    }

    private void Update()
    {
        if (endEpisodeTime > 0 && Time.time > endEpisodeTime)
        {
            EndEpisode();
            endEpisodeTime = -1f;
        }
    }

    /* ----------------------------------------------------------- */
    /*  Utilities                                                  */
    /* ----------------------------------------------------------- */

    private Vector3 RandomPointNear(Vector3 origin, float radius)
    {
        Vector2 rndUnit = Random.insideUnitCircle * radius;
        Vector3 newPos = new Vector3(
            origin.x + rndUnit.x,
            startPosition.y,
            origin.z + rndUnit.y
        );
        return ClampPositionToBoundary(newPos);
    }

    private bool ValidFloor(Vector3 pos)
    {
        return Physics.Raycast(pos + Vector3.up * 2f, Vector3.down, 3f, walkableMask);
    }
}