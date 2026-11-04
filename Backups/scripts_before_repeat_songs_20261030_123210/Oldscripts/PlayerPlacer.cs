using System.Collections;
using Unity.Netcode;
using Unity.XR.CoreUtils;
using UnityEngine;
using XRMultiplayer;

public class PlayerPlacer : MonoBehaviour
{
    public Transform player1Spot;   // empty object in front of Lanes_P1
    public Transform player2Spot;   // empty object in front of Lanes_P2
    public float heightOffset = 0f;

    XROrigin m_Origin;

    void Awake() { m_Origin = GetComponent<XROrigin>(); }
    void Start() { StartCoroutine(Place(player1Spot)); }   // offline / single player
    void OnEnable() { XRINetworkGameManager.Connected.Subscribe(OnConnected); }
    void OnDisable() { XRINetworkGameManager.Connected.Unsubscribe(OnConnected); }

    void OnConnected(bool connected)
    {
        if (!connected) { StartCoroutine(Place(player1Spot)); return; }
        bool isP1 = NetworkManager.Singleton.LocalClient.IsSessionOwner;
        StartCoroutine(Place(isP1 ? player1Spot : player2Spot));
    }

    IEnumerator Place(Transform spot)
    {
        yield return new WaitForSeconds(0.5f); // let headset tracking settle
        if (spot == null || m_Origin == null) yield break;
        m_Origin.MatchOriginUpCameraForward(spot.up, spot.forward);
        Vector3 p = spot.position; p.y += heightOffset;
        m_Origin.MoveCameraToWorldLocation(p);
    }
}