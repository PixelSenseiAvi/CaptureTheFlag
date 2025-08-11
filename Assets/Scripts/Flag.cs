using UnityEngine;

[RequireComponent(typeof(Collider))]
public class Flag : MonoBehaviour
{
    [Tooltip("Reward given to the agent that picks up this flag")]
    [SerializeField] private float pickupReward = 1.0f;

    [Tooltip("Flag On/Off")]
    [SerializeField] public SkinnedMeshRenderer flagCloth;



    //private void OnTriggerEnter(Collider other)
    //{
    //    // Make sure we hit an agent
    //    if (other.TryGetComponent<CaptureAgent>(out var agent))
    //    {
    //        agent.AddReward(pickupReward);
    //        agent.EndEpisode();

    //        flagCloth.enabled = false;   // invisible
    //        GetComponent<Collider>().enabled = false; // no more collisions
    //    }
    //}
}