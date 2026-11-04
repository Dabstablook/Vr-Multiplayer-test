using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;

public class NoteSpawner : MonoBehaviour
{
    [System.Serializable]
    public class LaneSet
    {
        public GameObject root;
        public Transform[] spawns = new Transform[4];
        public Transform[] targets = new Transform[4];
    }

    public SongChart chart;
    public AudioSource music;
    public int playerIndex = 0;

    public LaneSet p1Lanes;
    public LaneSet p2Lanes;
    public GameObject[] lanePrefabs = new GameObject[4];

    [Header("Song end")]
    public TMP_Text resultsText;        // optional: shown when the song finishes
    public float endPadding = 2f;       // seconds after the last note / audio end

    LaneSet Mine => playerIndex == 0 ? p1Lanes : p2Lanes;

    List<ChartNote> notes = new List<ChartNote>();
    bool[] charged;     // per chart note: charged by a caught baton
    Note[] live;        // per chart note: spawned object (null once cut or missed)
    int next;
    bool running;

    public void BeginSong(double leadIn)
    {
        var all = ChartLoader.Load(chart);
        notes = all.FindAll(n => n.owner == playerIndex);
        charged = new bool[notes.Count];
        live = new Note[notes.Count];
        music.clip = chart.clip;
        next = 0;
        SongClock.Start(music, leadIn);
        running = true;
        if (resultsText != null) resultsText.text = "";
    }

    public void BeginSong() { BeginSong(chart.approachTime + 0.5); }

    void Update()
    {
        var kb = Keyboard.current;
        if (!running && kb != null && kb.kKey.wasPressedThisFrame)
            BeginSong();

        // Solo testing: P = autoplay this player, O = autoplay drops batons
        if (kb != null && kb.pKey.wasPressedThisFrame)
        {
            Note.AutoPlay = !Note.AutoPlay;
            Debug.Log($"Autoplay {(Note.AutoPlay ? "ON" : "OFF")}");
        }
        if (kb != null && kb.oKey.wasPressedThisFrame)
        {
            Note.AutoDropBatons = !Note.AutoDropBatons;
            Debug.Log($"Autoplay drops batons {(Note.AutoDropBatons ? "ON" : "OFF")}");
        }

        // Audio sync: ] = notes later, [ = notes earlier, until they hit the hit point on the heard beat
        if (kb != null && kb.rightBracketKey.wasPressedThisFrame)
        {
            Note.AudioLatency += 0.01f;
            Debug.Log($"Audio latency {Note.AudioLatency:F2}s");
        }
        if (kb != null && kb.leftBracketKey.wasPressedThisFrame)
        {
            Note.AudioLatency -= 0.01f;
            Debug.Log($"Audio latency {Note.AudioLatency:F2}s");
        }
        if (!running) return;

        while (next < notes.Count && SongClock.Now - Note.AudioLatency >= notes[next].time - chart.approachTime)
        {
            Spawn(next);
            next++;
        }

        // Song finished: all notes spawned and the clock is past the end of the audio
        if (next >= notes.Count && SongClock.Now > chart.clip.length + endPadding)
            EndSong();
    }

    void EndSong()
    {
        running = false;
        music.Stop();

        if (SaberModeSwitcher.Instance != null)
            SaberModeSwitcher.Instance.SetGameMode(false);

        var r = RhythmNetwork.Instance;
        if (resultsText != null && r != null && r.IsSpawned)
            resultsText.text = $"FINISHED!\nTeam score: {r.teamScore.Value}\nMisses: {r.misses.Value}";
    }

    // Partner cut the send of this pair: charge my receive note of the same pair (exactly one)
    public void ChargeReceive(int pairId)
    {
        if (charged == null) return;
        for (int i = 0; i < notes.Count; i++)
        {
            if (notes[i].pairId != pairId || notes[i].send) continue;
            if (charged[i]) return;
            if (i < next && live[i] == null)
            {
                Debug.LogWarning($"Baton pair {pairId}: receive note at {notes[i].time:F3}s was already played, not charged");
                return;
            }

            charged[i] = true;
            if (i < next) live[i].MakeCharged();
            Debug.Log($"Charged note: lane {notes[i].lane}, time {notes[i].time:F3}s");
            return;
        }
    }

    void Spawn(int i)
    {
        ChartNote n = notes[i];
        Transform spawn = Mine.spawns[n.lane];
        Transform target = Mine.targets[n.lane];
        var go = Instantiate(lanePrefabs[n.lane], spawn.position, Quaternion.identity);
        var note = go.GetComponent<Note>();
        note.Init(spawn.position, target.position, n.time, chart.approachTime);
        note.pairId = n.pairId;
        note.send = n.send;
        if (n.pairId >= 0 && n.send) note.MarkBaton();
        if (charged[i]) note.MakeCharged();
        live[i] = note;
    }
}