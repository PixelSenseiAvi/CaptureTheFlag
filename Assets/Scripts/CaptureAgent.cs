using UnityEngine;
using Unity.MLAgents;
using Unity.MLAgents.Sensors;
using Unity.MLAgents.Actuators;
using UnityEngine.AI;

public class CaptureAgent : Agent
{
    [Header("Scene refs")]
    [SerializeField] private Transform targetTransform;   // opponent flag

    [Header("Tuning")]
    [SerializeField] private float moveSpeed = 2f;
    [SerializeField] private float distanceRewardScale = 1f;  // how strong the shaped reward is

    private Vector3 startPosition;
    private float lastDistance;        // distance to flag at previous step

    /* ----------------------------------------------------------- */
    /*  Unity / ML-Agents life-cycle                               */
    /* ----------------------------------------------------------- */

    private void Awake()
    {
        startPosition = transform.position;
    }

    public override void OnEpisodeBegin()
    {
        transform.position = startPosition;
        lastDistance = Vector3.Distance(transform.position, targetTransform.position);
    }

    public override void CollectObservations(VectorSensor sensor)
    {
        Vector3 toTarget = targetTransform.position - transform.position;
        Vector3 local = transform.InverseTransformDirection(toTarget);

        sensor.AddObservation(local.normalized);   // direction (x, z)
        sensor.AddObservation(local.magnitude);    // distance
    }

    public override void OnActionReceived(ActionBuffers actions)
    {
        float moveX = actions.ContinuousActions[0];
        float moveZ = actions.ContinuousActions[1];

        Vector3 move = transform.right * moveX + transform.forward * moveZ;
        transform.position += move * Time.deltaTime * moveSpeed;

        /* ----- distance-based dense reward ----- */
        float newDistance = Vector3.Distance(transform.position, targetTransform.position);
        float delta = lastDistance - newDistance;   // >0 if closer
        AddReward(delta * distanceRewardScale);
        lastDistance = newDistance;
    }

    public override void Heuristic(in ActionBuffers actionsOut)
    {
        var c = actionsOut.ContinuousActions;
        c[0] = Input.GetAxisRaw("Horizontal");
        c[1] = Input.GetAxisRaw("Vertical");
    }

    /* ----------------------------------------------------------- */
    /*  Collision events                                           */
    /* ----------------------------------------------------------- */

    private void OnTriggerEnter(Collider other)
    {
        if (other.TryGetComponent<Goal>(out _))
        {
            AddReward(1.0f);   // flag reached
            EndEpisode();
        }

        if (other.TryGetComponent<Wall>(out _))
        {
            AddReward(-1.0f);  // hit wall
            EndEpisode();
        }
    }

}