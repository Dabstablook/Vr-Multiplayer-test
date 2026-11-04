using UnityEngine;

public static class SongClock
{
    static double dspStart;
    static bool running;

    public static void Start(AudioSource music, double leadIn)
    {
        dspStart = AudioSettings.dspTime + leadIn;
        running = true;
        music.PlayScheduled(dspStart);
    }

    // Seconds into the song. Negative during the lead-in.
    public static float Now => running ? (float)(AudioSettings.dspTime - dspStart) : -999f;
}