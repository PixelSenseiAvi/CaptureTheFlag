using UnityEngine;
using System.Collections;

public class FlagCaptureEnvironment : MonoBehaviour
{
    [Header("Environment Settings")]
    public FlagCaptureAgent agent1;
    public FlagCaptureAgent agent2;
    public Transform flag;
    public float arenaSize = 10f;
    
    private Vector3 flagStartPosition;
    
    void Start()
    {
        flagStartPosition = flag.position;
        
        // Set up agent references
        agent1.otherAgent = agent2.transform;
        agent1.environment = this;
        agent1.isPlayer1 = true;
        
        agent2.otherAgent = agent1.transform;
        agent2.environment = this;
        agent2.isPlayer1 = false;
    }
    
    public void AgentReachedFlag(FlagCaptureAgent winningAgent)
    {
        // Determine which agent won and give appropriate rewards
        if (winningAgent == agent1)
        {
            agent1.OnWon();
            agent2.OnOtherAgentWon();
        }
        else
        {
            agent2.OnWon();
            agent1.OnOtherAgentWon();
        }
        
        // Reset flag position for next episode
        StartCoroutine(ResetEnvironment());
    }
    
    IEnumerator ResetEnvironment()
    {
        yield return new WaitForSeconds(0.1f);
        
        // Randomize flag position
        Vector3 randomPos = new Vector3(
            Random.Range(-arenaSize/2, arenaSize/2),
            flagStartPosition.y,
            Random.Range(-arenaSize/2, arenaSize/2)
        );
        
        flag.position = randomPos;
    }
}



