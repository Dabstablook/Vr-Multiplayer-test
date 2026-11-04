using UnityEngine;

[CreateAssetMenu(menuName = "Rhythm/Song Chart")]
public class SongChart : ScriptableObject
{
    public AudioClip clip;
    public string midiName = "RAMtest1";   // name as shown in the MIDI DB
    public float bpm = 120f;
    public float chartOffset = 0f;         // seconds, MIDI time -> audio time (can be negative)
    public float approachTime = 2f;        // seconds from spawn to hit
}