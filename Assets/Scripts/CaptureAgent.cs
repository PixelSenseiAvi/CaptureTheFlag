using Unity.MLAgents;
using Unity.MLAgents.Sensors;
using Unity.MLAgents.Actuators;
using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
public class CaptureAgent : Agent
{
    [Header("Scene References")]
    [SerializeField] private Transform targetTransform;   // opponent flag

    [Header("Tuning")]
    [SerializeField] private float moveSpeed = 2f;
    [SerializeField] private float distanceRewardScale = 1f;   // strength of dense reward

    private Vector3 startPosition;
    private float lastDistance;

    [Header("Wall bounce settings")]
    [SerializeField] private float bounceForce = 5f;        // how hard we push back
    [SerializeField] private float respawnRadius = 3f;      // max distance from impact point
    [SerializeField] private LayerMask walkableMask = 1;    // layer(s) considered valid floor

    /* ----------------------------------------------------------- */
    /*  Life-cycle                                                 */
    /* ----------------------------------------------------------- */

    private void Awake()
    {
        startPosition = transform.position;
    }

    public override void OnEpisodeBegin()
    {
        // Reset position & velocity
        transform.position = startPosition + Random.insideUnitSphere * 0.5f;
        transform.position = new Vector3(transform.position.x, startPosition.y, transform.position.z);
        var rb = GetComponent<Rigidbody>();
        rb.linearVelocity        = Vector3.zero;
        rb.angularVelocity = Vector3.zero;

        lastDistance = Vector3.Distance(transform.position, targetTransform.position);
    }

    /* ----------------------------------------------------------- */
    /*  Observations                                               */
    /*  (RayPerceptionSensorComponent3D adds rays automatically)   */
    /* ----------------------------------------------------------- */

    // Replace CollectObservations with this
    public override void CollectObservations(VectorSensor sensor)
    {
        Vector3 toGoal = targetTransform.position - transform.position;
        Vector3 local = transform.InverseTransformDirection(toGoal.normalized);
        sensor.AddObservation(local.x);   // left/right
        sensor.AddObservation(local.z);   // forward(+) / backward(-)
        sensor.AddObservation(toGoal.magnitude);
    }

    /* ----------------------------------------------------------- */
    /*  Actions                                                    */
    /* ----------------------------------------------------------- */

    public override void OnActionReceived(ActionBuffers actions)
    {
        float moveX = actions.ContinuousActions[0];
        float moveZ = actions.ContinuousActions[1];

        Vector3 move = new Vector3(moveX, 0f, moveZ).normalized;
        transform.position += move * Time.deltaTime * moveSpeed;

        // Dense reward: positive when closer
        float newDistance = Vector3.Distance(transform.position, targetTransform.position);
        float delta = lastDistance - newDistance;   // >0 == closer
        AddReward(delta * distanceRewardScale * 0.01f);
        lastDistance = newDistance;
    }

    public override void Heuristic(in ActionBuffers actionsOut)
    {
        var c = actionsOut.ContinuousActions;
        c[0] = Input.GetAxisRaw("Horizontal");
        c[1] = Input.GetAxisRaw("Vertical");
    }

    /* ----------------------------------------------------------- */
    /*  Events                                                     */
    /* ----------------------------------------------------------- */

    private void OnTriggerEnter(Collider other)
    {
        if (other.TryGetComponent<Goal>(out _))
        {
            AddReward(1.0f);
            EndEpisode();
            return;
        }

        if (other.TryGetComponent<Wall>(out _))
        {
            /* ---- 1. small penalty ---- */
            AddReward(-0.25f);

            /* ---- 2. bounce away from wall ---- */
            Vector3 pushDir = (transform.position - other.ClosestPoint(transform.position)).normalized;
            pushDir.y = 0;
            GetComponent<Rigidbody>().AddForce(pushDir * bounceForce, ForceMode.VelocityChange);

            /* ---- 3. teleport to a nearby safe spot ---- */
            Vector3 newPos = RandomPointNear(transform.position, respawnRadius);
            if (ValidFloor(newPos))
                transform.position = newPos;
            else
                transform.position = startPosition;   // fallback
        }
    }

    private Vector3 RandomPointNear(Vector3 origin, float radius)
    {
        Vector2 rndUnit = Random.insideUnitCircle * radius;
        return new Vector3(
            origin.x + rndUnit.x,
            startPosition.y,
            origin.z + rndUnit.y
        );

    }

    private bool ValidFloor(Vector3 pos)
    {
        return Physics.Raycast(pos + Vector3.up * 2f, Vector3.down, 3f, walkableMask);
    }
}