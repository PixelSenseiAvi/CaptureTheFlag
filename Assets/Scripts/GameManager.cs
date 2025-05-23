using UnityEngine;
using Unity.MLAgents;
using Unity.MLAgents.Sensors;
using Unity.MLAgents.Actuators;

public class GameManager : MonoBehaviour
{
    [Header("Team Agents (Phase 1)")]
    public PrivateAgentPiko teamAAgent;
    public PrivateAgentPiko teamBAgent;
    
    [Header("Spawn Points")]
    public Transform teamASpawnPoint;
    public Transform teamBSpawnPoint;
    public Transform flagSpawnPoint;

    [Header("Game Settings")]
    public float episodeTimeout = 300f; // 5 minutes per episode
    private float episodeTimer;
    private bool isEpisodeActive = false;

    [Header("Score Tracking")]
    public int teamAScore = 0;
    public int teamBScore = 0;
    public int maxScore = 10; // Game ends when a team reaches this score

    [Header("Training Settings")]
    public bool autoResetOnScore = true;
    public float resetDelay = 2f;

    // Reference to the flag
    private GameObject flagObject;
    private Flag flagComponent;

    // Training statistics
    private int totalEpisodes = 0;
    
    private void Start()
    {
        ValidateSetup();
        InitializeGame();
    }

    private void Update()
    {
        if (isEpisodeActive)
        {
            episodeTimer += Time.deltaTime;
            if (episodeTimer >= episodeTimeout)
            {
                EndEpisodeTimeout();
            }
        }
    }

    private void ValidateSetup()
    {
        bool hasErrors = false;

        // Check agents
        if (teamAAgent == null)
        {
            Debug.LogError("Team A Agent is not assigned!");
            hasErrors = true;
        }

        if (teamBAgent == null)
        {
            Debug.LogError("Team B Agent is not assigned!");
            hasErrors = true;
        }

        // Check spawn points
        if (teamASpawnPoint == null)
        {
            Debug.LogError("Team A spawn point is not assigned!");
            hasErrors = true;
        }

        if (teamBSpawnPoint == null)
        {
            Debug.LogError("Team B spawn point is not assigned!");
            hasErrors = true;
        }

        if (flagSpawnPoint == null)
        {
            Debug.LogError("Flag spawn point is not assigned!");
            hasErrors = true;
        }

        // Find flag object
        flagObject = GameObject.FindGameObjectWithTag("Flag");
        if (flagObject == null)
        {
            Debug.LogError("No flag object found with tag 'Flag'!");
            hasErrors = true;
        }
        else
        {
            flagComponent = flagObject.GetComponent<Flag>();
            if (flagComponent == null)
            {
                Debug.LogWarning("Flag object doesn't have Flag component. Basic respawn will be used.");
            }
        }

        if (hasErrors)
        {
            Debug.LogError("GameManager setup has errors. Please fix them before training.");
            enabled = false;
        }
    }

    private void InitializeGame()
    {
        Debug.Log("Initializing CTF Game - Phase 1 (Two Teams)");
        
        // Set up team A agent references
        if (teamAAgent != null)
        {
            teamAAgent.gameObject.tag = "TeamA";
            
            // Set home base reference
            if (teamAAgent.homeBaseTransform == null)
                teamAAgent.homeBaseTransform = teamASpawnPoint;
                
            // Set flag reference
            if (teamAAgent.flagTransform == null)
                teamAAgent.flagTransform = flagSpawnPoint;
        }
        
        // Set up team B agent references
        if (teamBAgent != null)
        {
            teamBAgent.gameObject.tag = "TeamB";
            
            // Set home base reference
            if (teamBAgent.homeBaseTransform == null)
                teamBAgent.homeBaseTransform = teamBSpawnPoint;
                
            // Set flag reference
            if (teamBAgent.flagTransform == null)
                teamBAgent.flagTransform = flagSpawnPoint;
        }

        StartNewEpisode();
    }

    public void StartNewEpisode()
    {
        Debug.Log($"Starting new episode #{totalEpisodes + 1}");
        isEpisodeActive = true;
        episodeTimer = 0f;
        totalEpisodes++;

        // Reset Team A agent
        if (teamAAgent != null)
        {
            ResetAgent(teamAAgent, teamASpawnPoint);
        }

        // Reset Team B agent
        if (teamBAgent != null)
        {
            ResetAgent(teamBAgent, teamBSpawnPoint);
        }

        // Reset flag
        ResetFlag();
    }

    private void ResetAgent(Agent agent, Transform spawnPoint)
    {
        if (agent == null || spawnPoint == null) return;

        // Position agent at spawn point
        agent.transform.position = spawnPoint.position;
        agent.transform.rotation = spawnPoint.rotation;

        // Reset physics
        Rigidbody rb = agent.GetComponent<Rigidbody>();
        if (rb != null)
        {
            rb.linearVelocity = Vector3.zero;
            rb.angularVelocity = Vector3.zero;
        }

        // Reset agent state
        agent.EndEpisode();
        
        Debug.Log($"Reset {agent.name} to {spawnPoint.position}");
    }

    private void ResetFlag()
    {
        if (flagObject == null || flagSpawnPoint == null) return;

        if (flagComponent != null)
        {
            flagComponent.RespawnFlag();
        }
        else
        {
            flagObject.transform.position = flagSpawnPoint.position;
            flagObject.transform.rotation = flagSpawnPoint.rotation;
            flagObject.SetActive(true);
        }

        Debug.Log($"Flag respawned at {flagSpawnPoint.position}");
    }

    private void EndEpisodeTimeout()
    {
        Debug.Log("Episode ended due to timeout");
        isEpisodeActive = false;

        // Small penalty for timeout (encourages more aggressive play)
        if (teamAAgent != null)
            teamAAgent.AddReward(-0.1f);
            
        if (teamBAgent != null)
            teamBAgent.AddReward(-0.1f);

        // End episode for both agents
        EndEpisodeForBothAgents();

        if (autoResetOnScore)
        {
            Invoke(nameof(StartNewEpisode), resetDelay);
        }
    }

    private void EndEpisodeForBothAgents()
    {
        if (teamAAgent != null)
            teamAAgent.EndEpisode();
            
        if (teamBAgent != null)
            teamBAgent.EndEpisode();
    }

    // Called when flag is captured
    public void OnFlagCaptured()
    {
        // Determine which agent captured the flag
        string capturingTeam = "";
        
        if (teamAAgent != null && teamAAgent.HasFlag())
        {
            capturingTeam = "TeamA";
        }
        else if (teamBAgent != null && teamBAgent.HasFlag())
        {
            capturingTeam = "TeamB";
        }

        Debug.Log($"Flag captured by {capturingTeam}");
    }

    // Called when flag is dropped
    public void OnFlagDropped(Vector3 dropPosition)
    {
        Debug.Log($"Flag dropped at {dropPosition}");
        
        // If a team collides with another team, flag goes to original point
        if (flagComponent != null)
        {
            flagComponent.RespawnFlag();
        }
        else
        {
            Invoke(nameof(ResetFlag), 1f); // 1 second delay
        }
    }

    // Called when a point is scored
    public void OnPointScored()
    {
        // Determine which team scored
        if (teamAAgent != null && teamAAgent.HasFlag())
        {
            OnPointScored("TeamA");
        }
        else if (teamBAgent != null && teamBAgent.HasFlag())
        {
            OnPointScored("TeamB");
        }
    }

    private void OnPointScored(string scoringTeam)
    {
        isEpisodeActive = false;

        // Update scores
        if (scoringTeam == "TeamA")
        {
            teamAScore++;
            Debug.Log($"Team A scored! Score: A:{teamAScore} - B:{teamBScore}");
        }
        else if (scoringTeam == "TeamB")
        {
            teamBScore++;
            Debug.Log($"Team B scored! Score: A:{teamAScore} - B:{teamBScore}");
        }

        // Check if game is complete
        if (teamAScore >= maxScore || teamBScore >= maxScore)
        {
            OnGameComplete();
        }
        else
        {
            // End current episode
            EndEpisodeForBothAgents();
            
            if (autoResetOnScore)
            {
                Invoke(nameof(StartNewEpisode), resetDelay);
            }
        }
    }

    private void OnGameComplete()
    {
        string winner = teamAScore >= maxScore ? "Team A" : "Team B";
        Debug.Log($"Game Complete! {winner} wins with score A:{teamAScore} - B:{teamBScore}");

        // Reset scores
        teamAScore = 0;
        teamBScore = 0;

        // End episodes
        EndEpisodeForBothAgents();
        
        if (autoResetOnScore)
        {
            Invoke(nameof(StartNewEpisode), resetDelay);
        }
    }

    // Public methods for external control
    public void ForceResetEpisode()
    {
        CancelInvoke(); // Cancel any pending resets
        StartNewEpisode();
    }

    public void PauseTraining()
    {
        isEpisodeActive = false;
        CancelInvoke();
    }

    public void ResumeTraining()
    {
        if (!isEpisodeActive)
        {
            StartNewEpisode();
        }
    }

    // Training statistics
    public float GetWinRate(string team)
    {
        if (totalEpisodes == 0) return 0f;
        
        if (team == "TeamA")
            return (float)teamAScore / totalEpisodes;
        else if (team == "TeamB")
            return (float)teamBScore / totalEpisodes;
        
        return 0f;
    }

    public void PrintTrainingStats()
    {
        Debug.Log($"Training Stats - Episodes: {totalEpisodes}, A Wins: {teamAScore}, B Wins: {teamBScore}");
    }

    // Gizmos for editor visualization
    private void OnDrawGizmos()
    {
        // Draw spawn points
        if (teamASpawnPoint != null)
        {
            Gizmos.color = Color.blue;
            Gizmos.DrawWireSphere(teamASpawnPoint.position, 1f);
            Gizmos.DrawLine(teamASpawnPoint.position, teamASpawnPoint.position + teamASpawnPoint.forward * 2f);
        }

        if (teamBSpawnPoint != null)
        {
            Gizmos.color = Color.red;
            Gizmos.DrawWireSphere(teamBSpawnPoint.position, 1f);
            Gizmos.DrawLine(teamBSpawnPoint.position, teamBSpawnPoint.position + teamBSpawnPoint.forward * 2f);
        }

        if (flagSpawnPoint != null)
        {
            Gizmos.color = Color.yellow;
            Gizmos.DrawWireSphere(flagSpawnPoint.position, 0.5f);
        }
    }
}