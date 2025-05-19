using UnityEngine;

public class Flag : MonoBehaviour
{
    private Vector3 initialPosition;
    private Quaternion initialRotation;
    private float respawnTimer = 0f;
    private bool isRespawned = true;
    private const float RESPAWN_DELAY = 5f;

    private void Start()
    {
        initialPosition = transform.position;
        initialRotation = transform.rotation;
    }

    private void Update()
    {
        if (!isRespawned)
        {
            respawnTimer += Time.deltaTime;
            if (respawnTimer >= RESPAWN_DELAY)
            {
                RespawnFlag();
            }
        }
    }

    public void CaptureFlag()
    {
        gameObject.SetActive(false);
        isRespawned = false;
        respawnTimer = 0f;
    }

    public void RespawnFlag()
    {
        transform.position = initialPosition;
        transform.rotation = initialRotation;
        gameObject.SetActive(true);
        isRespawned = true;
    }

    // Force immediate respawn with no delay
    public void ForceRespawn()
    {
        RespawnFlag();
    }
}
