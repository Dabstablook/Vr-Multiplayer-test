using System.Collections.Generic;
using MidiPlayerTK;
using UnityEngine;

public struct ChartNote
{
    public float time;      // seconds in the song (already includes chartOffset)
    public int lane;        // 0..3
    public int owner;       // 0 = P1, 1 = P2
    public int pairId;      // baton pair this note belongs to, -1 = plain note
    public bool send;       // true: baton send (cut it to pass), false: the partner's receive note
}

public static class ChartLoader
{
    // Lane pitches per (owner, baton). P2 = P1 + 12, P1 baton = +24, P2 baton = +36 from P2.
    static readonly (int[] pitches, int owner, bool baton)[] tables =
    {
        (new[] { 59, 60, 62, 64 },     0, false),
        (new[] { 71, 72, 74, 76 },     1, false),
        (new[] { 83, 84, 86, 88 },     0, true),
        (new[] { 107, 108, 110, 112 }, 1, true),
    };
    const int Unpaired = -2;   // baton note before PairBatons runs

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
                    pairId = tb.baton ? Unpaired : -1
                });
                break;
            }
        }
        result.Sort((a, b) => a.time.CompareTo(b.time));
        int pairs = PairBatons(result);
        Debug.Log($"ChartLoader: {result.Count} notes, {pairs} baton pairs, tpq {tpq}, first at {(result.Count > 0 ? result[0].time : -1)} s");
        return result;
    }

    // Baton notes come in pairs: the first is the send, the next baton note (from the other
    // player) is its receive. Same result on both clients, since both read the same MIDI.
    static int PairBatons(List<ChartNote> notes)
    {
        int nextPair = 0;
        int pending = -1;   // index of a send still waiting for its receive
        for (int i = 0; i < notes.Count; i++)
        {
            if (notes[i].pairId != Unpaired) continue;

            if (pending >= 0 && notes[pending].owner != notes[i].owner)
            {
                var s = notes[pending]; s.pairId = nextPair; s.send = true; notes[pending] = s;
                var r = notes[i]; r.pairId = nextPair; r.send = false; notes[i] = r;
                nextPair++;
                pending = -1;
            }
            else
            {
                if (pending >= 0) MakePlain(notes, pending);
                pending = i;
            }
        }
        if (pending >= 0) MakePlain(notes, pending);
        return nextPair;
    }

    static void MakePlain(List<ChartNote> notes, int i)
    {
        var n = notes[i];
        n.pairId = -1;
        notes[i] = n;
        Debug.LogWarning($"ChartLoader: baton note at {n.time:F3}s (P{n.owner + 1}, lane {n.lane}) has no partner baton, played as a plain note");
    }
}
