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
    //  private float endEpisodeTime = -1f;
    public GameObject originalFlag;


    private bool isTurning = false;
    private Quaternion turnStartRotation;
    private Quaternion turnTargetRotation;
    private float turnProgress = 0f;
    private float turnDuration = 1f; // seconds to complete the turn

    [Header("Wall Collision Settings")]
    [SerializeField] private WallCollisionMode collisionMode = WallCollisionMode.Bounce;
    [SerializeField] private float bouncePenalty = 0.25f;     // penalty for bouncing
    [SerializeField] private float respawnPenalty = 0.5f;     // penalty for respawn
    [SerializeField] private int maxWallHitsBeforeRespawn = 3;
    [SerializeField] private float wallHitWindow = 2f; // seconds to count hits

    private int wallHitCount = 0;
    private float lastWallHitTime = 0f;
    private bool hasRotatedAfterCapture = false;

    [SerializeField] private float xDivergenceFactor = 0.3f; // small sidestep influence


    public enum WallCollisionMode
    {
        Bounce,
        Respawn
    }

    /* ----------------------------------------------------------- */
    /*  Life-cycle                                                 */
    /* ----------------------------------------------------------- */

    private void Awake()
    {
        rb = GetComponent<Rigidbody>(); // Cache Rigidbody
        startPosition = transform.localPosition;
        startRotation = transform.localRotation;
        if (!homeBase) homeBase = transform;
    }

    public override void OnEpisodeBegin()
    {
        Debug.Log("OnEpisode begin");
        // Reset position & velocity
        transform.localPosition = GetRandomSpawnPosition();
        transform.localRotation = startRotation;

        rb.linearVelocity = Vector3.zero;
        rb.angularVelocity = Vector3.zero;

        lastDistance = Vector3.Distance(transform.localPosition, targetTransform.localPosition);
        hasReachedOpponentSide = false;

        flagCaptured.SetActive(false);
        targetTransform.gameObject.SetActive(true);

        hasRotatedAfterCapture = false;
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
        Vector3 toGoal = targetTransform.localPosition - transform.localPosition;
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
        float moveX = actions.ContinuousActions[0] * xDivergenceFactor; // scaled divergence
        float moveZ = actions.ContinuousActions[1]; // main forward/back axis

        if (!flagCaptured)
        {
            Vector3 move = new Vector3(moveX, 0f, moveZ).normalized;
            Vector3 newPosition = transform.localPosition + move * Time.deltaTime * moveSpeed;

            // Clamp inside boundaries
            newPosition = ClampPositionToBoundary(newPosition);

            // Boundary penalty
            if (newPosition.x == transform.localPosition.x || newPosition.z == transform.localPosition.z)
            {
                AddReward(-boundaryPenalty);
            }

            transform.localPosition = newPosition;

            // Distance reward
            float newDistance = Vector3.Distance(transform.localPosition, targetTransform.localPosition);
            float distanceDelta = lastDistance - newDistance;

            float progressReward = Mathf.Clamp(
                distanceDelta * distanceRewardScale,
                minDistanceReward,
                maxDistanceReward
            );
            AddReward(progressReward);
            lastDistance = newDistance;

            if (!hasReachedOpponentSide && IsOnOpponentSide())
            {
                AddReward(opponentSideReward);
                hasReachedOpponentSide = true;
            }
        }
        else
        {
            // Return to base
            Vector3 toHome = homeBase.localPosition - transform.localPosition;

            // Prioritize Z movement, allow small X divergence
            float moveXHome = Mathf.Sign(toHome.x) * xDivergenceFactor;
            float moveZHome = Mathf.Sign(toHome.z);

            Vector3 move = new Vector3(-moveXHome, 0f, -moveZHome).normalized;
            Vector3 newPosition = transform.localPosition + move * Time.deltaTime * moveSpeed;

            newPosition = ClampPositionToBoundary(newPosition);

            if (newPosition.x == transform.localPosition.x || newPosition.z == transform.localPosition.z)
            {
                AddReward(-boundaryPenalty);
            }

            transform.localPosition = newPosition;

            float newDistanceHome = Vector3.Distance(transform.localPosition, homeBase.localPosition);
            float distanceDeltaHome = lastDistance - newDistanceHome;

            float progressRewardHome = Mathf.Clamp(
                distanceDeltaHome * distanceRewardScale,
                minDistanceReward,
                maxDistanceReward
            );
            AddReward(progressRewardHome);
            lastDistance = newDistanceHome;

            if (newDistanceHome < 0.5f)
            {
                AddReward(1f);        // bonus for getting home
                EndEpisode();
            }
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
            transform.localPosition.z > midpointZ :
            transform.localPosition.z < midpointZ;
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
            originalFlag.SetActive(false);

            //endEpisodeTime = Time.time + 1.5f;
            if (!hasRotatedAfterCapture)
            {
                StartEpisode2();
                hasRotatedAfterCapture = true;
            }

            return;
        }

        if (Time.time - lastWallHitTime > wallHitWindow)
        {
            wallHitCount = 0;
        }

        wallHitCount++;
        lastWallHitTime = Time.time;

        bool forceRespawn = wallHitCount >= maxWallHitsBeforeRespawn;

        if (collisionMode == WallCollisionMode.Bounce && !forceRespawn)
        {
            // Bounce with small penalty
            AddReward(-bouncePenalty);

            Vector3 pushDir = (transform.localPosition - other.ClosestPoint(transform.localPosition)).normalized;
            pushDir.y = 0;
            rb.AddForce(pushDir * bounceForce, ForceMode.VelocityChange);
        }
        else
        {
            // Respawn with bigger penalty
            AddReward(-respawnPenalty);

            Vector3 newPos = RandomPointNear(startPosition, respawnRadius);
            if (ValidFloor(newPos))
                transform.localPosition = newPos;
            else
                transform.localPosition = startPosition;

            rb.linearVelocity = Vector3.zero;
            rb.angularVelocity = Vector3.zero;

            wallHitCount = 0; // reset counter after respawn
        }
    }


    private void StartEpisode2()
    {
        isTurning = true;
        turnProgress = 0f;

        turnStartRotation = transform.rotation;
        turnTargetRotation = turnStartRotation * Quaternion.Euler(0, 180f, 0);

        //StartCoroutine(ReturnToBase());
    }


    private void Update()
    {
        // Smooth turning
        if (isTurning)
        {
            turnProgress += Time.deltaTime / turnDuration;
            transform.rotation = Quaternion.Slerp(turnStartRotation, turnTargetRotation, turnProgress);

            if (turnProgress >= 1f)
            {
                isTurning = false;
            }
        }

        //if (endEpisodeTime > 0 && Time.time > endEpisodeTime)
        //{
        //    EndEpisode();
        //    endEpisodeTime = -1f;
        //}
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