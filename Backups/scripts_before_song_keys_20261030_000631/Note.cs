using UnityEngine;

public class Note : MonoBehaviour
{
    public float missWindow = 0.25f;   // seconds after hit time before it counts as missed

    [System.NonSerialized] public int pairId = -1;   // baton pair, -1 = plain note
    [System.NonSerialized] public bool send;         // baton send: cutting it passes the baton
    [System.NonSerialized] public Material glowMaterial;   // see-through halo, set by NoteSpawner
    public bool Charged { get; private set; }

    // Solo testing (keys in NoteSpawner): auto-cut every note, optionally dropping batons
    public static bool AutoPlay, AutoDropBatons;

    // Seconds notes run behind the song clock so they cross the hit points on the beat (set from NoteSpawner.noteDelay)
    public static float NoteDelay;

    bool cut;
    Transform halo;

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
        if (halo != null) halo.localScale = Vector3.one * (2f + 0.3f * Mathf.Sin(Time.time * 10f));   // pulse

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

    // Send note, called once at spawn
    public void MarkBaton() { Glow(); }

    public void MakeCharged()
    {
        Charged = true;
        Glow();
    }

    // Glowing orb in the note's own colour: self-lit core plus a pulsing see-through halo (no collider)
    void Glow()
    {
        if (halo != null) return;
        var core = GetComponentInChildren<Renderer>();
        var m = core.material;
        Color c = m.color;
        m.EnableKeyword("_EMISSION");
        m.SetColor("_EmissionColor", c * 1.5f);
        if (glowMaterial == null) return;

        var h = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        DestroyImmediate(h.GetComponent<Collider>());   // must not join the note's hit collider
        h.name = "Glow";
        halo = h.transform;
        halo.SetParent(core.transform, false);
        halo.localScale = Vector3.one * 2f;
        var hr = h.GetComponent<Renderer>();
        hr.sharedMaterial = glowMaterial;
        hr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        var block = new MaterialPropertyBlock();
        block.SetColor("_BaseColor", new Color(c.r, c.g, c.b, 0.45f));
        hr.SetPropertyBlock(block);
    }
}
