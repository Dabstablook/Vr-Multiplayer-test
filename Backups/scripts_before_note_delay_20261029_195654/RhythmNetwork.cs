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

    // Send note of a pair was cut: the partner's receive note of that pair becomes charged
    [Rpc(SendTo.Everyone)]
    public void BatonCaughtRpc(int pairId, int fromPlayer)
    {
        Debug.Log($"baton pair {pairId} caught");
        if (fromPlayer != spawner.playerIndex)
            spawner.ChargeReceive(pairId);
    }
}