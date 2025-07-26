using UnityEngine;
using UnityEngine.AI;

public class MoveTester : MonoBehaviour
{
    NavMeshAgent agent;
    void Start() => agent = GetComponent<NavMeshAgent>();
    void FixedUpdate()
    {
        agent.SetDestination(transform.position + Vector3.forward * 2f);
    }
}
