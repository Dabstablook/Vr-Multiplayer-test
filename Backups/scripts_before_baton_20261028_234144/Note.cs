using UnityEngine;

public class Note : MonoBehaviour
{
    public float missWindow = 0.25f;   // seconds after hit time before it counts as missed

    Vector3 from, to;
    float hitTime, approach;

    public void Init(Vector3 spawn, Vector3 target, float hitTime, float approachTime)
    {
        from = spawn; to = target; this.hitTime = hitTime; approach = approachTime;
    }

    void Update()
    {
        float t = (SongClock.Now - (hitTime - approach)) / approach;   // 0 at spawn, 1 at hit
        transform.position = Vector3.LerpUnclamped(from, to, t);

        if (SongClock.Now > hitTime + missWindow)
        {
            if (RhythmNetwork.Instance != null && RhythmNetwork.Instance.IsSpawned)
                RhythmNetwork.Instance.ReportMissRpc();
            Destroy(gameObject);
        }
    }
}