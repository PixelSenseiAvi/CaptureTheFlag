using UnityEngine;

public class Flag : MonoBehaviour
{
    [Header("Visual Effects")]
    public ParticleSystem captureEffect;
    
    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Agent"))
        {
            // Play capture effect
            if (captureEffect != null)
            {
                captureEffect.Play();
            }
        }
    }
}