using System.Collections;
using System.Collections.Generic;
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
    public float orbDuration = 1.2f;
    public float orbArcHeight = 1f;

    readonly HashSet<int> caughtBatons = new HashSet<int>();
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
        caughtBatons.Clear();
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

    public bool IsBatonCaught(int batonId) => caughtBatons.Contains(batonId);

    [Rpc(SendTo.Everyone)]
    public void BatonCaughtRpc(int batonId, int fromPlayer, Vector3 cutPosition)
    {
        if (!caughtBatons.Add(batonId)) return;
        Debug.Log($"baton {batonId} caught");
        StartCoroutine(BatonOrb(fromPlayer, cutPosition));
    }

    // Local only: an orb arcs from where the send note was cut to the other player's hit points
    IEnumerator BatonOrb(int fromPlayer, Vector3 a)
    {
        Vector3 b = LaneCentre(fromPlayer == 0 ? spawner.p2Lanes : spawner.p1Lanes);

        GameObject orb;
        if (batonOrbPrefab != null) orb = Instantiate(batonOrbPrefab, a, Quaternion.identity);
        else
        {
            orb = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            Destroy(orb.GetComponent<Collider>());
            orb.transform.localScale = Vector3.one * 0.3f;
            orb.GetComponent<Renderer>().material.color = new Color(1f, 0.85f, 0.2f);
        }

        Vector3 size = orb.transform.localScale;
        for (float t = 0; t < 1f; t += Time.deltaTime / orbDuration)
        {
            float e = Mathf.SmoothStep(0f, 1f, t);
            orb.transform.position = Vector3.Lerp(a, b, e) + Vector3.up * Mathf.Sin(e * Mathf.PI) * orbArcHeight;
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
}