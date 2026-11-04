using System.Collections.Generic;
using MidiPlayerTK;
using UnityEngine;

public struct ChartNote
{
    public float time;  // seconds in the song (already includes chartOffset)
    public int lane;    // 0..3
    public int owner;   // 0 = P1, 1 = P2
}

public static class ChartLoader
{
    static readonly int[] basePitches = { 59, 60, 62, 64 };
    const int P2Shift = 12;

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

            int owner = 0, pitch = e.Value;
            if (pitch >= 59 + P2Shift - 2) { owner = 1; pitch -= P2Shift; }

            int lane = System.Array.IndexOf(basePitches, pitch);
            if (lane < 0) continue;

            result.Add(new ChartNote
            {
                time = (float)e.Tick / tpq * secPerBeat + chart.chartOffset,
                lane = lane,
                owner = owner
            });
        }
        result.Sort((a, b) => a.time.CompareTo(b.time));
        Debug.Log($"ChartLoader: {result.Count} notes, tpq {tpq}, first at {(result.Count > 0 ? result[0].time : -1)} s");
        return result;
    }
}