using UnityEngine;

public class BackgroundAudioPlayer : MonoBehaviour
{
    [Header("Audio Reference")]
    public AudioSource audioSource;

    void Start()
    {
        // Automatically fetch the AudioSource on this object if left unassigned
        if (audioSource == null)
        {
            audioSource = GetComponent<AudioSource>();
        }

        // Play the audio source on start
        if (audioSource != null)
        {
            audioSource.Play();
        }
    }

    // Call this method from another script (e.g. MPTKSpawner) to play audio manually
    public void PlayAudio()
    {
        if (audioSource != null)
        {
            audioSource.Play();
        }
    }
}