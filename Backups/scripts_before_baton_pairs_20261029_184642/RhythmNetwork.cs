using System.Collections;
using Unity.Netcode;
using UnityEngine;
using TMPro;

public class RhythmNetwork : NetworkBehaviour
{
    public static RhythmNetwork Instance;

    public NoteSpawner spawner;
    public TMP_Text countdownText;      // optional: world-space text near the lanes
    public float countdownSeconds = 5f; // must be bigger than the song's approachTime

    public NetworkVariable<int> teamScore = new NetworkVariable<int>();
    public NetworkVariable<int> combo = new NetworkVariable<int>();
    public NetworkVariable<int> misses = new NetworkVariable<int>();
    NetworkVariable<double> startTime = new NetworkVariable<double>(-1);

    [Header("Baton")]
    public int chargedBonus = 200;          // extra points for cutting a charged note
    public GameObject batonOrbPrefab;       // optional, a plain sphere is used if empty
    public float orbDuration = 1f;
    public float orbClearance = 0.75f;      // metres above the highest lane point

    bool started;

    void Awake() { Instance = this; }

    public override void OnNetworkSpawn()
    {
        spawner.playerIndex = NetworkManager.LocalClient.IsSessionOwner ? 0 : 1;
        startTime.OnValueChanged += OnStartTimeChanged;
        if (startTime.Value > 0) OnStartTimeChanged(-1, startTime.Value);
    }

    public override void OnNetworkDespawn()
    {
        startTime.OnValueChanged -= OnStartTimeChanged;
    }

    // Wire this to the host's Start button. Only works on the host, with 2 players connected.
    public void HostStartSong()
    {
        if (!IsSpawned) { Debug.LogWarning("RhythmNetwork is not spawned."); return; }
        if (!NetworkManager.LocalClient.IsSessionOwner) { Debug.LogWarning("Only P1 (session owner) can start."); return; }
        if (started) { Debug.LogWarning("Song already started."); return; }

        int count = NetworkManager.ConnectedClientsIds.Count;
        if (count < 2) { Debug.LogWarning($"Need 2 players, have {count}."); return; }

        float lead = Mathf.Max(countdownSeconds, spawner.chart.approachTime + 1f);
        startTime.Value = NetworkManager.ServerTime.Time + lead;
        Debug.Log("Start time set, countdown begins.");
    }

    void OnStartTimeChanged(double _, double t)
    {
        if (t <= 0 || started) return;
        started = true;
        if (SaberModeSwitcher.Instance != null) SaberModeSwitcher.Instance.SetGameMode(true);
        double remaining = t - NetworkManager.ServerTime.Time;
        spawner.BeginSong(System.Math.Max(0.5, remaining));
    }

    void Update()
    {
        if (countdownText == null) return;
        if (startTime.Value > 0 && SongClock.Now < 0)
            countdownText.text = Mathf.CeilToInt(-SongClock.Now).ToString();
        else
            countdownText.text = "";
    }

    [Rpc(SendTo.Authority)]
    public void ReportHitRpc(bool charged)
    {
        combo.Value++;
        teamScore.Value += 100 * (1 + combo.Value / 10) + (charged ? chargedBonus : 0);
    }

    [Rpc(SendTo.Authority)]
    public void ReportMissRpc()
    {
        combo.Value = 0;
        misses.Value++;
    }

    [Rpc(SendTo.Everyone)]
    public void BatonCaughtRpc(float batonTime, int fromPlayer)
    {
        Debug.Log($"baton at {batonTime:F3}s caught");
        StartCoroutine(BatonOrb(fromPlayer));
        if (fromPlayer != spawner.playerIndex)
            spawner.ChargeNextAfter(batonTime);
    }

    // Local only, purely visual: an orb flies above both lane sets from the sender's side to the partner's
    IEnumerator BatonOrb(int fromPlayer)
    {
        var from = fromPlayer == 0 ? spawner.p1Lanes : spawner.p2Lanes;
        var to = fromPlayer == 0 ? spawner.p2Lanes : spawner.p1Lanes;
        float y = Mathf.Max(LaneTop(from), LaneTop(to)) + orbClearance;
        Vector3 a = LaneCentre(from); a.y = y;
        Vector3 b = LaneCentre(to); b.y = y;

        GameObject orb;
        if (batonOrbPrefab != null) orb = Instantiate(batonOrbPrefab, a, Quaternion.identity);
        else
        {
            orb = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            orb.transform.localScale = Vector3.one * 0.3f;
            orb.GetComponent<Renderer>().material.color = new Color(1f, 0.85f, 0.2f);
        }
        foreach (var c in orb.GetComponentsInChildren<Collider>()) DestroyImmediate(c);

        Vector3 size = orb.transform.localScale;
        float step = 1f / Mathf.Max(orbDuration, 0.05f);
        for (float t = 0; t < 1f; t += Time.deltaTime * step)
        {
            float e = Mathf.SmoothStep(0f, 1f, t);
            orb.transform.position = Vector3.Lerp(a, b, e) + Vector3.up * Mathf.Sin(e * Mathf.PI) * 0.3f;
            yield return null;
        }

        // Short pop on arrival
        for (float t = 0; t < 1f; t += Time.deltaTime / 0.25f)
        {
            orb.transform.localScale = size * (1f + t) * (1f - t);
            yield return null;
        }
        Destroy(orb);
    }

    static Vector3 LaneCentre(NoteSpawner.LaneSet set)
    {
        Vector3 sum = Vector3.zero;
        int count = 0;
        foreach (var t in set.targets) if (t) { sum += t.position; count++; }
        return count > 0 ? sum / count : Vector3.zero;
    }

    static float LaneTop(NoteSpawner.LaneSet set)
    {
        float top = float.MinValue;
        foreach (var t in set.spawns) if (t) top = Mathf.Max(top, t.position.y);
        foreach (var t in set.targets) if (t) top = Mathf.Max(top, t.position.y);
        return top == float.MinValue ? 0f : top;
    }
}