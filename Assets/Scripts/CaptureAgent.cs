using Unity.MLAgents;
using Unity.MLAgents.Actuators;
using Unity.MLAgents.Sensors;
using UnityEngine;

public class CaptureAgent : Agent
{
    [Header("Scene refs")]
    public Transform flag;            // opponent flag
    public Transform myBase;          // agent�s own base
    public float moveSpeed = 5f;

    private Rigidbody rb;

    void Start() => rb = GetComponent<Rigidbody>();

    public override void CollectObservations(VectorSensor sensor)
    {
        // 3 + 3 = 6 floats
        sensor.AddObservation(transform.localPosition);
        sensor.AddObservation(flag.localPosition);
    }

    public override void OnActionReceived(ActionBuffers actions)
    {
        // Continuous actions: [x, z]
        Vector3 move = new Vector3(actions.ContinuousActions[0], 0,
                                   actions.ContinuousActions[1]).normalized;
        rb.linearVelocity = move * moveSpeed;

        // Tiny time penalty so it learns to finish quickly
        AddReward(-1f / MaxStep);
    }

    public override void Heuristic(in ActionBuffers a)
    {
        var c = a.ContinuousActions;
        c[0] = Input.GetAxis("Horizontal");
        c[1] = Input.GetAxis("Vertical");
    }

    void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Flag"))
        {
            AddReward(1f);          // picked up opponent flag
            other.gameObject.SetActive(false);
        }

        if (other.CompareTag("Base") && !other.gameObject.CompareTag(gameObject.tag))
        {
            AddReward(2f);          // returned flag to my base ? win
            EndEpisode();
        }
    }

    public override void OnEpisodeBegin()
    {
        // Random start
        transform.localPosition = new Vector3(Random.Range(-4, 4), 0.5f, Random.Range(-4, 4));
        flag.gameObject.SetActive(true);
        rb.linearVelocity = Vector3.zero;
    }
}