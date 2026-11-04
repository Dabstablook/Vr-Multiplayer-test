using System.Collections.Generic;
using MidiPlayerTK;
using UnityEngine;

public struct ChartNote
{
    public float time;      // seconds in the song (already includes chartOffset)
    public int lane;        // 0..3
    public int owner;       // 0 = P1, 1 = P2
    public bool baton;      // baton send note (spawns in the sender's own lane)
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
                    baton = tb.send
                });
                break;
            }
        }
        result.Sort((a, b) => a.time.CompareTo(b.time));
        Debug.Log($"ChartLoader: {result.Count} notes, tpq {tpq}, first at {(result.Count > 0 ? result[0].time : -1)} s");
        return result;
    }
}
