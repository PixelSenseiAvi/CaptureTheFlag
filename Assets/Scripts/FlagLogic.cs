// FlagLogic.cs

using Unity.MLAgents;
using UnityEngine;

public class FlagLogic : MonoBehaviour
{
    public Team flagTeam;               // Enum: Red / Blue
    public Transform homeBase;          // Where it should respawn
    public GameObject carriedFlag;      // Visuals while carried (optional)

    private bool _isHome = true;
    private Transform _carrier;

    public void OnTriggerEnter(Collider other)
    {
        var agent = other.GetComponent<MinionAgent>();
        if (!agent) return;

        // Enemy picks it up
        if (agent.team != flagTeam && _isHome)
        {
            _carrier = other.transform;
            transform.SetParent(_carrier);
            transform.localPosition = Vector3.up * 1.5f; // offset above head
            _isHome = false;
        }

        // Ally captures
        if (agent.team == flagTeam && !_isHome && _carrier != null)
        {
            Respawn();
            agent.AddReward(1f);        // reward for capture
            agent.EndEpisode();         // or use TeamManager to end round
        }
    }

    public void Respawn()
    {
        transform.SetParent(null);
        transform.position = homeBase.position;
        _isHome = true;
        _carrier = null;
    }
}