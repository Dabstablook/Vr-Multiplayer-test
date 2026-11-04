using System.Collections.Generic;
using MidiPlayerTK;
using UnityEngine;

public struct ChartNote
{
    public float time;      // seconds in the song (already includes chartOffset)
    public int lane;        // 0..3
    public int owner;       // 0 = P1, 1 = P2
    public int batonId;     // >= 0: baton send note, -1: normal note
    public int chargedBy;   // >= 0: charged if that baton is caught, -1: never charged
}

public static class ChartLoader
{
    // Lane pitches per (owner, send). P2 = P1 + 12, P1 send = +24, P2 send = +36 from P2.
    static readonly (int[] pitches, int owner, bool send)[] tables =
    {
        (new[] { 59, 60, 62, 64 },     0, false),
        (new[] { 71, 72, 74, 76 },     1, false),
        (new[] { 83, 84, 86, 88 },     0, true),
        (new[] { 107, 108, 110, 112 }, 1, true),
    };
    const int ChargedPerBaton = 3;

    public static List<ChartNote> Load(SongChart chart)
    {
        var result = new List<ChartNote>();
        var midi = new MidiLoad();
        midi.MPTK_Load(chart.midiName);                      // check: may also be MPTK_Load(int index)
        List<MPTKEvent> events = midi.MPTK_ReadMidiEvents();
        float tpq = midi.MPTK_DeltaTicksPerQuarterNote;
        float secPerBeat = 60f / chart.bpm;

        foreach (var e in events)
        {
            if (e.Command != MPTKCommand.NoteOn || e.Velocity == 0) continue;

            foreach (var tb in tables)
            {
                int lane = System.Array.IndexOf(tb.pitches, e.Value);
                if (lane < 0) continue;

                result.Add(new ChartNote
                {
                    time = (float)e.Tick / tpq * secPerBeat + chart.chartOffset,
                    lane = lane,
                    owner = tb.owner,
                    batonId = tb.send ? 0 : -1,   // numbered after sorting
                    chargedBy = -1
                });
                break;
            }
        }
        result.Sort((a, b) => a.time.CompareTo(b.time));
        AssignBatons(result);
        Debug.Log($"ChartLoader: {result.Count} notes, tpq {tpq}, first at {(result.Count > 0 ? result[0].time : -1)} s");
        return result;
    }

    // Number the send notes in time order (same on both clients, since both read the same MIDI),
    // then mark the other player's next 3 normal notes after each send as charged by it.
    static void AssignBatons(List<ChartNote> notes)
    {
        int nextId = 0;
        for (int i = 0; i < notes.Count; i++)
        {
            if (notes[i].batonId < 0) continue;
            var send = notes[i];
            send.batonId = nextId++;
            notes[i] = send;

            int marked = 0;
            for (int j = i + 1; j < notes.Count && marked < ChargedPerBaton; j++)
            {
                var n = notes[j];
                if (n.owner == send.owner || n.batonId >= 0 || n.time <= send.time) continue;
                if (n.chargedBy < 0) { n.chargedBy = send.batonId; notes[j] = n; }
                marked++;
            }
        }
    }
}
