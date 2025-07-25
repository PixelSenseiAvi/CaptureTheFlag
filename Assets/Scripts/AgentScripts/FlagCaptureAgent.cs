using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.MLAgents;
using Unity.MLAgents.Actuators;
using Unity.MLAgents.Sensors;

public class FlagCaptureAgent : Agent
{
    [Header("Agent Settings")]
    public float moveSpeed = 5f;
    public float rotationSpeed = 300f;
    public bool isPlayer1 = true;
    
    [Header("Environment References")]
    public Transform flag;
    public Transform otherAgent;
    public FlagCaptureEnvironment environment;
    
    private Rigidbody rb;
    private Vector3 startPosition;
    private Quaternion startRotation;
    
    public override void Initialize()
    {
        rb = GetComponent<Rigidbody>();
        startPosition = transform.position;
        startRotation = transform.rotation;
    }
    
    public override void OnEpisodeBegin()
    {
        // Reset agent position and rotation
        transform.position = startPosition;
        transform.rotation = startRotation;
        rb.linearVelocity = Vector3.zero;
        rb.angularVelocity = Vector3.zero;
    }
    
    public override void CollectObservations(VectorSensor sensor)
    {
        // Agent's position and rotation
        sensor.AddObservation(transform.position);
        sensor.AddObservation(transform.rotation);
        
        // Agent's velocity
        sensor.AddObservation(rb.linearVelocity);
        
        // Flag position
        sensor.AddObservation(flag.position);
        
        // Other agent's position and velocity
        sensor.AddObservation(otherAgent.position);
        sensor.AddObservation(otherAgent.GetComponent<Rigidbody>().linearVelocity);
        
        // Distance to flag
        float distanceToFlag = Vector3.Distance(transform.position, flag.position);
        sensor.AddObservation(distanceToFlag);
        
        // Distance to other agent
        float distanceToOther = Vector3.Distance(transform.position, otherAgent.position);
        sensor.AddObservation(distanceToOther);
        
        // Relative position to flag
        Vector3 relativePos = flag.position - transform.position;
        sensor.AddObservation(relativePos.normalized);
    }
    
    public override void OnActionReceived(ActionBuffers actionBuffers)
    {
        // Get actions
        float moveX = actionBuffers.ContinuousActions[0];
        float moveZ = actionBuffers.ContinuousActions[1];
        float rotate = actionBuffers.ContinuousActions[2];
        
        // Debug the received actions
        Debug.Log($"Agent {gameObject.name} - Actions: X={moveX}, Z={moveZ}, Rot={rotate}");
        
        // Apply movement - using transform for more direct control
        Vector3 movement = new Vector3(moveX, 0, moveZ).normalized * moveSpeed * Time.fixedDeltaTime;
        transform.Translate(movement, Space.World);
        
        // Apply rotation
        transform.Rotate(0, rotate * rotationSpeed * Time.fixedDeltaTime, 0);
        
        // Alternative Rigidbody movement (comment out transform movement above if using this)
        /*
        Vector3 force = new Vector3(moveX, 0, moveZ) * moveSpeed;
        rb.AddForce(force, ForceMode.Force);
        rb.AddTorque(0, rotate * rotationSpeed, 0);
        */
        
        // Small negative reward for time passing (encourages faster completion)
        AddReward(-0.001f);
        
        // Reward for getting closer to flag
        float distanceToFlag = Vector3.Distance(transform.position, flag.position);
        float normalizedDistance = distanceToFlag / 20f; // Assuming max distance is ~20 units
        AddReward(-normalizedDistance * 0.01f);
    }
    
    public override void Heuristic(in ActionBuffers actionsOut)
    {
        // Manual control for testing - WASD for movement, QE for rotation
        var continuousActionsOut = actionsOut.ContinuousActions;
        
        if (isPlayer1)
        {
            // Player 1 uses WASD + QE
            continuousActionsOut[0] = Input.GetAxis("Horizontal"); // A/D
            continuousActionsOut[1] = Input.GetAxis("Vertical");   // W/S
            continuousActionsOut[2] = (Input.GetKey(KeyCode.E) ? 1f : 0f) - (Input.GetKey(KeyCode.Q) ? 1f : 0f);
        }
        else
        {
            // Player 2 uses Arrow Keys + Numpad
            continuousActionsOut[0] = (Input.GetKey(KeyCode.RightArrow) ? 1f : 0f) - (Input.GetKey(KeyCode.LeftArrow) ? 1f : 0f);
            continuousActionsOut[1] = (Input.GetKey(KeyCode.UpArrow) ? 1f : 0f) - (Input.GetKey(KeyCode.DownArrow) ? 1f : 0f);
            continuousActionsOut[2] = (Input.GetKey(KeyCode.Keypad6) ? 1f : 0f) - (Input.GetKey(KeyCode.Keypad4) ? 1f : 0f);
        }
        
        Debug.Log($"Heuristic Agent {gameObject.name} - Actions: {continuousActionsOut[0]}, {continuousActionsOut[1]}, {continuousActionsOut[2]}");
    }
    
    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Flag"))
        {
            // Agent reached the flag - big positive reward
            AddReward(10f);
            
            // Notify environment that this agent won
            environment.AgentReachedFlag(this);
        }
    }
    
    public void OnOtherAgentWon()
    {
        // Other agent reached flag first - negative reward
        AddReward(-5f);
        EndEpisode();
    }
    
    public void OnWon()
    {
        // This agent won - end episode
        EndEpisode();
    }
}

