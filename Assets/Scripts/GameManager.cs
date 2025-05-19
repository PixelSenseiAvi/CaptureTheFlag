using UnityEngine;
using System.Collections.Generic;

public class GameManager : MonoBehaviour
{
    [Header("Team Settings")]
    public List<PrivateAgentPiko> teamAAgents = new List<PrivateAgentPiko>();
    public List<PrivateAgentPiko> teamBAgents = new List<PrivateAgentPiko>();
    public Transform flagSpawnPoint;
    public Transform[] teamAHomeBaseSpawnPoints;
    public Transform[] teamBHomeBaseSpawnPoints;

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

    private void InitializeGame()
    {
        // Ensure we have players in both teams
        if (teamAAgents.Count == 0 || teamBAgents.Count == 0)
        {
            Debug.LogError("Both teams must have at least one player!");
            return;
        }

        // Set up Team A
        for (int i = 0; i < teamAAgents.Count; i++)
        {
            teamAAgents[i].teamId = "TeamA";
            if (i < teamAHomeBaseSpawnPoints.Length)
            {
                teamAAgents[i].homeBaseTransform = teamAHomeBaseSpawnPoints[i];
            }
        }

        // Set up Team B
        for (int i = 0; i < teamBAgents.Count; i++)
        {
            teamBAgents[i].teamId = "TeamB";
            if (i < teamBHomeBaseSpawnPoints.Length)
            {
                teamBAgents[i].homeBaseTransform = teamBHomeBaseSpawnPoints[i];
            }
        }

        StartNewEpisode();
    }

    public void StartNewEpisode()
    {
        isEpisodeActive = true;
        episodeTimer = 0f;

        // Reset all Team A agents
        foreach (var agent in teamAAgents)
        {
            agent.EndEpisode();
        }

        // Reset all Team B agents
        foreach (var agent in teamBAgents)
        {
            agent.EndEpisode();
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
                // Notify teammate agents to provide support/protection
                foreach (var teammate in teamAAgents)
                {
                    if (teammate != agent)
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

                break;
            }
        }

        foreach (var agent in teamBAgents)
        {
            if (agent.HasFlag())
            {
                // Notify teammate agents to provide support/protection
                foreach (var teammate in teamBAgents)
                {
                    if (teammate != agent)
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

                break;
            }
        }
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
        bool teamAScored = false;
        bool teamBScored = false;

        foreach (var agent in teamAAgents)
        {
            if (agent.HasFlag())
            {
                teamAScored = true;
                teamAScore++;
                break;
            }
        }

        if (!teamAScored)
        {
            foreach (var agent in teamBAgents)
            {
                if (agent.HasFlag())
                {
                    teamBScored = true;
                    teamBScore++;
                    break;
                }
            }
        }

        // Add team rewards
        if (teamAScored)
        {
            foreach (var agent in teamAAgents)
            {
                agent.AddReward(2.0f);
            }

            foreach (var agent in teamBAgents)
            {
                agent.AddReward(-1.0f);
            }
        }
        else if (teamBScored)
        {
            foreach (var agent in teamBAgents)
            {
                agent.AddReward(2.0f);
            }

            foreach (var agent in teamAAgents)
            {
                agent.AddReward(-1.0f);
            }
        }

        // Reset the episode after a successful capture
        StartNewEpisode();
    }
}