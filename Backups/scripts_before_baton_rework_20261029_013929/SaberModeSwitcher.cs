using UnityEngine;
using XRMultiplayer;

public class SaberModeSwitcher : MonoBehaviour
{
    public static SaberModeSwitcher Instance;

    public GameObject[] sabers;          // on only during the song
    public GameObject[] pointerObjects;  // Poke + Near-Far interactors (and visuals if needed), hidden during the song
    public GameObject[] alwaysOff;       // Teleport interactors etc., never on

    bool inGame;
    bool[] savedPointerState;            // what the rig had on before the song, restored after it

    void Awake() { Instance = this; }

    // Leave the controllers as the rig set them up (its input manager decides
    // what is visible, e.g. the teleport line only while the button is held)
    void Start()
    {
        foreach (var s in sabers) if (s) s.SetActive(false);
        foreach (var o in alwaysOff) if (o) o.SetActive(false);
    }

    void OnEnable() { XRINetworkGameManager.Connected.Subscribe(OnConnected); }
    void OnDisable() { XRINetworkGameManager.Connected.Unsubscribe(OnConnected); }

    void OnConnected(bool connected) { SetGameMode(false); }

    public void SetGameMode(bool game)
    {
        if (game == inGame) return;
        inGame = game;

        foreach (var s in sabers) if (s) s.SetActive(game);

        if (game)
        {
            savedPointerState = new bool[pointerObjects.Length];
            for (int i = 0; i < pointerObjects.Length; i++)
                if (pointerObjects[i]) savedPointerState[i] = pointerObjects[i].activeSelf;
            foreach (var p in pointerObjects) if (p) p.SetActive(false);
        }
        else if (savedPointerState != null)
        {
            for (int i = 0; i < pointerObjects.Length; i++)
                if (pointerObjects[i]) pointerObjects[i].SetActive(savedPointerState[i]);
        }

        foreach (var o in alwaysOff) if (o) o.SetActive(false);
    }
}
