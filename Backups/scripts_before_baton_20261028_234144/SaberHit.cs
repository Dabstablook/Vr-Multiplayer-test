using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Inputs.Haptics;

public class SaberHit : MonoBehaviour
{
    [Header("Audio")]
    public AudioSource audioSource;
    public AudioClip hitSound;

    [Header("Visual Effects")]
    public GameObject particlePrefab;

    [Header("Haptics")]
    [Range(0f, 1f)] public float hapticAmplitude = 0.5f;
    public float hapticDuration = 0.1f;

    void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Red") || other.CompareTag("Yellow") ||
            other.CompareTag("Blue") || other.CompareTag("Purple"))
        {
            // 1. Play hit sound instantly
            if (hitSound != null && audioSource != null)
            {
                audioSource.PlayOneShot(hitSound);
            }

            // 2. Spawn particle explosion at note position
            if (particlePrefab != null)
            {
                Instantiate(particlePrefab, other.transform.position, Quaternion.identity);
            }

            // 3. Trigger controller vibration rumble
            TriggerHaptics();

            if (RhythmNetwork.Instance != null && RhythmNetwork.Instance.IsSpawned)
            {
                RhythmNetwork.Instance.ReportHitRpc();
            }

            // 4. Destroy the note
            Destroy(other.gameObject);
        }
    }

    HapticImpulsePlayer m_Haptics;

    void Awake()
    {
        m_Haptics = GetComponentInParent<HapticImpulsePlayer>();
    }

    void TriggerHaptics()
    {
        if (m_Haptics != null)
            m_Haptics.SendHapticImpulse(hapticAmplitude, hapticDuration);
    }
}