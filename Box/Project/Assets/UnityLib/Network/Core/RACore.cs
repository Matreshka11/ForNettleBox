using UnityEngine;
using System;
using System.Collections;
using JetBrains.Annotations;
using UnityEngine.Networking;

namespace Nettle {

//Remote actions
public class RACore: NetworkBehaviour {
    /// <summary>
    /// called when player object is created on client
    /// </summary>
    public static event Action<RACore> StartLocalPlayerEvent;

    public LatencyCalculator LatencyCalculator;

    void Awake() {
        DontDestroyOnLoad(this);
    }

    [Command(channel = (int)EQoSChannels.UNRELIABLE)]
    public void CmdPing(int networkTimestamp) {
        if (Time.unscaledDeltaTime + (Time.realtimeSinceStartup - Time.unscaledTime) < LatencyCalculator.OwdDeltatimeCutoff) {
            byte errorByte;
            LatencyCalculator.AccumulateOWD(NetworkTransport.GetRemoteDelayTimeMS(connectionToClient.hostId, connectionToClient.connectionId, networkTimestamp, out errorByte));
        }
    }


    [TargetRpc(channel = (int)EQoSChannels.UNRELIABLE)]
    public void TargetPing(NetworkConnection target, int networkTimestamp) {
        if (Time.unscaledDeltaTime + (Time.realtimeSinceStartup - Time.unscaledTime) < LatencyCalculator.OwdDeltatimeCutoff) {
            byte errorByte;
            int connectionId = NetworkManager.singleton.client.connection.connectionId;
            int hostId = NetworkManager.singleton.client.connection.hostId;
            LatencyCalculator.AccumulateOWD(NetworkTransport.GetRemoteDelayTimeMS(hostId, connectionId, networkTimestamp, out errorByte));
        }
    }

    //invokes on client
    public override void OnStartLocalPlayer() {
        base.OnStartLocalPlayer();
        if (StartLocalPlayerEvent != null)
            StartLocalPlayerEvent(this);

        UNetCore.localPlayer = this;
        StopAllCoroutines();
        StartCoroutine(CmdPingCoroutine());
    }

    public override void OnStartServer() {
        base.OnStartServer();
        StopAllCoroutines();
        StartCoroutine(RpcPingCoroutine());
    }

    IEnumerator RpcPingCoroutine() {
        while (true) {
            if (connectionToClient != null && connectionToClient.isConnected) {
                TargetPing(connectionToClient, NetworkTransport.GetNetworkTimestamp());
            }
            yield return new WaitForSeconds(LatencyCalculator.OwdInterval);
        }
    }

    IEnumerator CmdPingCoroutine() {
        while (true) {
            if (NetworkManager.singleton.client != null && NetworkManager.singleton.client.connection.isConnected) {
                CmdPing(NetworkTransport.GetNetworkTimestamp());
            }
            yield return new WaitForSeconds(LatencyCalculator.OwdInterval);


        }
    }

}
}
