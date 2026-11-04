using UnityEngine;

public class Note : MonoBehaviour
{
    public float missWindow = 0.25f;   // seconds after hit time before it counts as missed
    public Color chargedColor = new Color(1f, 0.85f, 0.2f);

    [System.NonSerialized] public int batonId = -1;    // >= 0: baton send note
    [System.NonSerialized] public int chargedBy = -1;  // >= 0: becomes charged if that baton is caught
    public bool Charged { get; private set; }

    // Solo testing (keys in NoteSpawner): auto-cut every note, optionally dropping batons
    public static bool AutoPlay, AutoDropBatons;

    bool cut;

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

        // Catch can arrive while this note is already flying, so check every frame
        if (!Charged && chargedBy >= 0 && RhythmNetwork.Instance != null && RhythmNetwork.Instance.IsBatonCaught(chargedBy))
            MakeCharged();

        if (AutoPlay && SongClock.Now >= hitTime && !(AutoDropBatons && batonId >= 0))
        {
            Cut();
            return;
        }

        if (SongClock.Now > hitTime + missWindow)
        {
            // A missed send note is a dropped baton: the miss resets the shared combo
            if (RhythmNetwork.Instance != null && RhythmNetwork.Instance.IsSpawned)
                RhythmNetwork.Instance.ReportMissRpc();
            Destroy(gameObject);
        }
    }

    // Report the hit, and pass the baton if this is a send note
    public void Cut()
    {
        if (cut) return;
        cut = true;

        var r = RhythmNetwork.Instance;
        if (r != null && r.IsSpawned)
        {
            r.ReportHitRpc(Charged);
            if (batonId >= 0)
                r.BatonCaughtRpc(batonId, r.spawner.playerIndex, transform.position);
        }
        Destroy(gameObject);
    }

    void MakeCharged()
    {
        Charged = true;
        foreach (var r in GetComponentsInChildren<Renderer>())
            r.material.color = chargedColor;
    }
}
