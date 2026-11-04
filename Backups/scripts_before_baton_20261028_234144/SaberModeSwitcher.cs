using UnityEngine;
using XRMultiplayer;

public class SaberModeSwitcher : MonoBehaviour
{
    public static SaberModeSwitcher Instance;

    public GameObject[] sabers;          // on only during the song
    public GameObject[] pointerObjects;  // Poke + Near-Far interactors (and visuals if needed), on only in the lobby
    public GameObject[] alwaysOff;       // Teleport interactors etc., never on

    void Awake() { Instance = this; }
    void Start() { SetGameMode(false); }

    void OnEnable() { XRINetworkGameManager.Connected.Subscribe(OnConnected); }
    void OnDisable() { XRINetworkGameManager.Connected.Unsubscribe(OnConnected); }

    void OnConnected(bool connected) { SetGameMode(false); }

    public void SetGameMode(bool game)
    {
        foreach (var s in sabers) if (s) s.SetActive(game);
        foreach (var p in pointerObjects) if (p) p.SetActive(!game);
        foreach (var o in alwaysOff) if (o) o.SetActive(false);
    }
}