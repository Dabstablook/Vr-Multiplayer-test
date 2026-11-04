using MidiPlayerTK;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

public class MPTKSpawner : MonoBehaviour
{
    [Header("MPTK Reference")]
    public MidiFilePlayer midiFilePlayer;

    private bool hasStarted = false;

    [Header("Spawn Transforms")]
    public Transform lane1Spawn;
    public Transform lane2Spawn;
    public Transform lane3Spawn;
    public Transform lane4Spawn;

    [Header("Optional per-lane targets (empty = straight lane)")]
    public Transform lane1Target;
    public Transform lane2Target;
    public Transform lane3Target;
    public Transform lane4Target;

    [Header("Note Prefabs (lane 1 to 4)")]
    public GameObject redNotePrefab;     // lane 1
    public GameObject yellowNotePrefab;  // lane 2
    public GameObject blueNotePrefab;    // lane 3
    public GameObject purpleNotePrefab;  // lane 4

    [Header("Audio & Timing Sync")]
    public AudioSource backgroundMusic;

    [Header("Note Timing")]
    public float approachTime = 2f;   // seconds from spawn to cut plane (smaller = faster notes)
    public float songOffset = 5.77f;  // per-song constant
    public float hitZ = 0f;           // world Z where the sabers cut

    [Header("MIDI Pitch Mapping")]
    public int lane1Pitch = 59;
    public int lane2Pitch = 60;
    public int lane3Pitch = 62;
    public int lane4Pitch = 64;

    void Start()
    {
        if (midiFilePlayer == null)
            midiFilePlayer = GetComponent<MidiFilePlayer>();

        if (midiFilePlayer != null)
            midiFilePlayer.OnEventNotesMidi.AddListener(OnMidiNotesReceived);
    }

    public void StartGameSequence()
    {
        StartCoroutine(SyncMusicAndMidi());
    }

    System.Collections.IEnumerator SyncMusicAndMidi()
    {
        // Positive: MIDI first, music after. Negative: music first, MIDI after.
        float delta = approachTime - songOffset;

        if (delta >= 0f)
        {
            midiFilePlayer.MPTK_Play();
            yield return new WaitForSeconds(delta);
            if (backgroundMusic != null) backgroundMusic.Play();
        }
        else
        {
            if (backgroundMusic != null) backgroundMusic.Play();
            yield return new WaitForSeconds(-delta);
            midiFilePlayer.MPTK_Play();
        }
    }

    void Update()
    {
        if (!hasStarted && Keyboard.current != null && Keyboard.current.kKey.wasPressedThisFrame)
        {
            hasStarted = true;
            StartGameSequence();
        }
    }

    void OnMidiNotesReceived(List<MPTKEvent> midiEvents)
    {
        foreach (MPTKEvent noteEvent in midiEvents)
        {
            if (noteEvent.Command == MPTKCommand.NoteOn)
                SpawnNoteFromMidi(noteEvent.Value);
        }
    }

    void SpawnNoteFromMidi(int midiPitch)
    {
        if (midiPitch == lane1Pitch) SpawnPrefab(redNotePrefab, lane1Spawn, lane1Target);
        else if (midiPitch == lane2Pitch) SpawnPrefab(yellowNotePrefab, lane2Spawn, lane2Target);
        else if (midiPitch == lane3Pitch) SpawnPrefab(blueNotePrefab, lane3Spawn, lane3Target);
        else if (midiPitch == lane4Pitch) SpawnPrefab(purpleNotePrefab, lane4Spawn, lane4Target);
    }

    void SpawnPrefab(GameObject prefab, Transform spawn, Transform target)
    {
        if (prefab == null || spawn == null) return;

        Vector3 end = target != null
            ? target.position
            : new Vector3(spawn.position.x, spawn.position.y, hitZ); // straight lane

        Vector3 toHit = end - spawn.position;
        float dist = Mathf.Max(0.1f, toHit.magnitude);
        Vector3 dir = toHit / dist;

        var go = Instantiate(prefab, spawn.position, Quaternion.LookRotation(dir));
        var mv = go.GetComponent<NoteMovement>();
        if (mv != null)
        {
            mv.direction = dir;
            mv.speed = dist / approachTime;
        }
    }
}