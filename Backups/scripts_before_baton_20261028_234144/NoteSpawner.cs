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
    int next;
    bool running;

    public void BeginSong(double leadIn)
    {
        var all = ChartLoader.Load(chart);
        notes = all.FindAll(n => n.owner == playerIndex);
        music.clip = chart.clip;
        next = 0;
        SongClock.Start(music, leadIn);
        running = true;
        if (resultsText != null) resultsText.text = "";
    }

    public void BeginSong() { BeginSong(chart.approachTime + 0.5); }

    void Update()
    {
        if (!running && Keyboard.current != null && Keyboard.current.kKey.wasPressedThisFrame)
            BeginSong();
        if (!running) return;

        while (next < notes.Count && SongClock.Now >= notes[next].time - chart.approachTime)
        {
            Spawn(notes[next]);
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

    void Spawn(ChartNote n)
    {
        Transform spawn = Mine.spawns[n.lane];
        Transform target = Mine.targets[n.lane];
        var go = Instantiate(lanePrefabs[n.lane], spawn.position, Quaternion.identity);
        go.GetComponent<Note>().Init(spawn.position, target.position, n.time, chart.approachTime);
    }
}