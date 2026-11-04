using UnityEngine;

public class Note : MonoBehaviour
{
    public float missWindow = 0.25f;   // seconds after hit time before it counts as missed
    public Color chargedColor = new Color(1f, 0.85f, 0.2f);

    [System.NonSerialized] public int pairId = -1;   // baton pair, -1 = plain note
    [System.NonSerialized] public bool send;         // baton send: cutting it passes the baton
    public bool Charged { get; private set; }

    // Solo testing (keys in NoteSpawner): auto-cut every note, optionally dropping batons
    public static bool AutoPlay, AutoDropBatons;

    // Seconds notes run behind the song clock so they cross the hit points on the beat (set from NoteSpawner.noteDelay)
    public static float NoteDelay;

    bool cut;

    Vector3 from, to;
    float hitTime, approach;

    public void Init(Vector3 spawn, Vector3 target, float hitTime, float approachTime)
    {
        from = spawn; to = target; this.hitTime = hitTime; approach = approachTime;
    }

    void Update()
    {
        float now = SongClock.Now - NoteDelay;
        float t = (now - (hitTime - approach)) / approach;   // 0 at spawn, 1 at hit
        transform.position = Vector3.LerpUnclamped(from, to, t);

        if (AutoPlay && now >= hitTime && !(AutoDropBatons && send))
        {
            Cut();
            return;
        }

        if (now > hitTime + missWindow)
        {
            // A missed send note is a dropped baton: the miss resets the shared combo
            if (send && pairId >= 0)
                Debug.Log($"baton pair {pairId} dropped");
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

        if (Charged)
            Debug.Log($"Charged note cut at {hitTime:F3}s");

        var r = RhythmNetwork.Instance;
        if (r != null && r.IsSpawned)
        {
            r.ReportHitRpc(Charged);
            if (send && pairId >= 0)   // receive notes never send
                r.BatonCaughtRpc(pairId, r.spawner.playerIndex);
        }
        else if (r != null && send && pairId >= 0)
        {
            // Offline both-sides test: pass the baton locally instead of over the network
            Debug.Log($"baton pair {pairId} caught (local)");
            r.spawner.ChargeReceive(pairId);
        }
        Destroy(gameObject);
    }

    // Called once at spawn: keep the lane colour but make it glow
    public void MarkBaton()
    {
        foreach (var r in GetComponentsInChildren<Renderer>())
        {
            var m = r.material;
            Color c = Color.Lerp(m.color, Color.white, 0.4f);
            m.color = c;
            m.EnableKeyword("_EMISSION");
            m.SetColor("_EmissionColor", c * 2f);
        }
    }

    public void MakeCharged()
    {
        Charged = true;
        foreach (var r in GetComponentsInChildren<Renderer>())
            r.material.color = chargedColor;
    }
}
