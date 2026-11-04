using UnityEngine;
using TMPro;

public class MissZone : MonoBehaviour
{
    [Header("UI Reference")]
    public TMP_Text missText;

    private int missCount = 0;

    void OnTriggerEnter(Collider other)
    {
        // Debug log to check if anything hits the wall
        Debug.Log("Hit detected with: " + other.gameObject.name + " | Tag: " + other.tag);

        if (other.CompareTag("Red") || other.CompareTag("Blue") || other.CompareTag("Yellow") || other.CompareTag("Purple"))
        {
            missCount++;
            Debug.Log("Miss confirmed! Total Misses: " + missCount);

            if (missText != null)
            {
                missText.text = "MISSES: " + missCount;
            }

            if (RhythmNetwork.Instance != null && RhythmNetwork.Instance.IsSpawned)
                RhythmNetwork.Instance.ReportHitRpc();

            Destroy(other.gameObject);
        }
    }
}