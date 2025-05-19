using UnityEngine;
using System.Collections.Generic;

public class GameManager : MonoBehaviour
{
    [Header("Team Settings")]
    public List<PrivateAgentPiko> teamAAgents = new List<PrivateAgentPiko>();
    public List<PrivateAgentPiko> teamBAgents = new List<PrivateAgentPiko>();
    public Transform flagSpawnPoint;
    
    // Spawn planes for team bases
    public Transform teamAHomeBaseSpawnPlane;
    public Transform teamBHomeBaseSpawnPlane;
    
    // Define spawn area size (assumes the spawn plane's scale determines the spawn area)
    public Vector2 spawnAreaSize = new Vector2(10f, 10f);

    [SerializeField] private uint teamSize = 3;

    [Header("Game Settings")]
    public float episodeTimeout = 300f; // 5 minutes per episode
    private float episodeTimer;
    private bool isEpisodeActive = false;

    [Header("Score")]
    public int teamAScore = 0;
    public int teamBScore = 0;

    // Reference to the flag
    private GameObject flagObject;

    private void Start()
    {
        // Find the flag object
        flagObject = GameObject.FindGameObjectWithTag("Flag");
        InitializeGame();
    }

    private void Update()
    {
        if (isEpisodeActive)
        {
            episodeTimer += Time.deltaTime;
            if (episodeTimer >= episodeTimeout)
            {
                EndEpisode();
            }
        }
    }

    //Randomly spawn Agents at Spawn Area
    private void InitializeGame()
    {
        // Ensure spawn planes are assigned
        if (teamAHomeBaseSpawnPlane == null)
        {
            Debug.LogError("Team A home base spawn plane is not assigned!");
            return;
        }

        if (teamBHomeBaseSpawnPlane == null)
        {
            Debug.LogError("Team B home base spawn plane is not assigned!");
            return;
        }
        
        // Set up Team A
        for (int i = 0; i < teamAAgents.Count; i++)
        {
            if (i < teamSize)
            {
                teamAAgents[i].teamId = "TeamA";
                
                // Generate random position within spawn plane
                Vector3 randomSpawnPosition = GetRandomPositionOnPlane(teamAHomeBaseSpawnPlane);
                teamAAgents[i].transform.position = randomSpawnPosition;
                
                // Set home base reference
                teamAAgents[i].homeBaseTransform = teamAHomeBaseSpawnPlane;
            }
        }

        // Set up Team B
        for (int i = 0; i < teamBAgents.Count; i++)
        {
            if (i < teamSize)
            {
                teamBAgents[i].teamId = "TeamB";
                
                // Generate random position within spawn plane
                Vector3 randomSpawnPosition = GetRandomPositionOnPlane(teamBHomeBaseSpawnPlane);
                teamBAgents[i].transform.position = randomSpawnPosition;
                
                // Set home base reference
                teamBAgents[i].homeBaseTransform = teamBHomeBaseSpawnPlane;
            }
        }

        StartNewEpisode();
    }

    // Helper method to get a random position on a plane
    private Vector3 GetRandomPositionOnPlane(Transform plane)
    {
        // Calculate bounds based on plane scale
        float halfWidth = plane.localScale.x * spawnAreaSize.x * 0.5f;
        float halfLength = plane.localScale.z * spawnAreaSize.y * 0.5f;
        
        // Get random position within bounds
        float randomX = Random.Range(-halfWidth, halfWidth);
        float randomZ = Random.Range(-halfLength, halfLength);
        
        // Calculate world position
        Vector3 localPosition = new Vector3(randomX, 0f, randomZ);
        Vector3 worldPosition = plane.TransformPoint(localPosition);
        
        // Adjust Y position to be slightly above the plane to prevent clipping
        worldPosition.y = plane.position.y + 0.5f;
        
        return worldPosition;
    }

    public void StartNewEpisode()
    {
        isEpisodeActive = true;
        episodeTimer = 0f;

        // Reset all Team A agents
        foreach (var agent in teamAAgents)
        {
            agent.EndEpisode();
            
            // Reposition agent to a random location on their spawn plane
            if (teamAHomeBaseSpawnPlane != null)
            {
                agent.transform.position = GetRandomPositionOnPlane(teamAHomeBaseSpawnPlane);
            }
        }

        // Reset all Team B agents
        foreach (var agent in teamBAgents)
        {
            agent.EndEpisode();
            
            // Reposition agent to a random location on their spawn plane
            if (teamBHomeBaseSpawnPlane != null)
            {
                agent.transform.position = GetRandomPositionOnPlane(teamBHomeBaseSpawnPlane);
            }
        }

        // Reset flag position
        if (flagSpawnPoint != null && flagObject != null)
        {
            Flag flag = flagObject.GetComponent<Flag>();
            if (flag != null)
            {
                flag.RespawnFlag();
            }
            else
            {
                flagObject.transform.position = flagSpawnPoint.position;
                flagObject.SetActive(true);
            }
        }
        else
        {
            Debug.LogError("Flag spawn point or flag object is missing!");
        }
    }

    private void EndEpisode()
    {
        isEpisodeActive = false;

        // End episode for Team A
        foreach (var agent in teamAAgents)
        {
            agent.EndEpisode();
        }

        // End episode for Team B
        foreach (var agent in teamBAgents)
        {
            agent.EndEpisode();
        }
    }

    // Call this when a flag is captured
    public void OnFlagCaptured()
    {
        // Find which agent has the flag
        foreach (var agent in teamAAgents)
        {
            if (agent.HasFlag())
            {
                OnFlagCaptured(agent);
                return;
            }
        }

        foreach (var agent in teamBAgents)
        {
            if (agent.HasFlag())
            {
                OnFlagCaptured(agent);
                return;
            }
        }
    }

    // Overload that takes the capturing agent directly
    public void OnFlagCaptured(PrivateAgentPiko capturingAgent)
    {
        string capturingTeam = capturingAgent.teamId;
        
        if (capturingTeam == "TeamA")
        {
            // Notify teammate agents to provide support/protection
            foreach (var teammate in teamAAgents)
            {
                if (teammate != capturingAgent)
                {
                    // Add small reward to encourage protection
                    teammate.AddReward(0.2f);
                }
            }

            // Add negative reward to opposing team
            foreach (var opponent in teamBAgents)
            {
                opponent.AddReward(-0.2f);
            }
        }
        else if (capturingTeam == "TeamB")
        {
            // Notify teammate agents to provide support/protection
            foreach (var teammate in teamBAgents)
            {
                if (teammate != capturingAgent)
                {
                    // Add small reward to encourage protection
                    teammate.AddReward(0.2f);
                }
            }

            // Add negative reward to opposing team
            foreach (var opponent in teamAAgents)
            {
                opponent.AddReward(-0.2f);
            }
        }
        
        Debug.Log("Flag captured by team: " + capturingTeam);
    }

    // Called when the flag is dropped (player with flag is hit by opponent)
    public void OnFlagDropped(Vector3 dropPosition)
    {
        Debug.Log("Flag dropped at: " + dropPosition);

        // Inform all agents that flag is back at base
        foreach (var agent in teamAAgents)
        {
            agent.AddReward(-0.1f); // Small penalty for everyone as the flag was dropped
        }

        foreach (var agent in teamBAgents)
        {
            agent.AddReward(-0.1f); // Small penalty for everyone as the flag was dropped
        }

        // Respawn the flag at its original position
        if (flagObject != null)
        {
            Flag flag = flagObject.GetComponent<Flag>();
            if (flag != null)
            {
                flag.ForceRespawn();
            }
            else
            {
                flagObject.transform.position = flagSpawnPoint.position;
                flagObject.SetActive(true);
            }
        }
    }

    // Call this when a point is scored
    public void OnPointScored()
    {
        // Determine which team scored
        foreach (var agent in teamAAgents)
        {
            if (agent.HasFlag())
            {
                OnPointScored(agent);
                return;
            }
        }

        foreach (var agent in teamBAgents)
        {
            if (agent.HasFlag())
            {
                OnPointScored(agent);
                return;
            }
        }
    }

    // Overload that takes the scoring agent directly
    public void OnPointScored(PrivateAgentPiko scoringAgent)
    {
        string scoringTeam = scoringAgent.teamId;
        
        // Update score based on team
        if (scoringTeam == "TeamA")
        {
            teamAScore++;
            
            // Reward team that scored
            foreach (var agent in teamAAgents)
            {
                // Higher reward for the scoring agent
                float reward = (agent == scoringAgent) ? 3.0f : 2.0f;
                agent.AddReward(reward);
            }

            // Penalty for opposing team
            foreach (var agent in teamBAgents)
            {
                agent.AddReward(-1.0f);
            }
            
            Debug.Log("Team A scored! Score: " + teamAScore);
        }
        else if (scoringTeam == "TeamB")
        {
            teamBScore++;
            
            // Reward team that scored
            foreach (var agent in teamBAgents)
            {
                // Higher reward for the scoring agent
                float reward = (agent == scoringAgent) ? 3.0f : 2.0f;
                agent.AddReward(reward);
            }

            // Penalty for opposing team
            foreach (var agent in teamAAgents)
            {
                agent.AddReward(-1.0f);
            }
            
            Debug.Log("Team B scored! Score: " + teamBScore);
        }

        // Reset the episode after a successful capture
        StartNewEpisode();
    }
}
