using UnityEngine;
using System;
using UnityEngine.Networking;
using System.Collections.Generic;
using System.Linq;

namespace Nettle {

public class ServerPart : UNetCoreBehaviour {

    public event Action<GameObject> SpawnObjectEvent;
    public event Action<NetworkConnection> ClientConnectedEvent;
    public event Action<NetworkConnection> ClientDisconnectedEvent;

    public int ActiveConnectionsCount {
        get {
            return (from cntn in NetworkServer.connections
                    where cntn != null
                    select cntn).Count();
        }
    }

    public ServerPart(NettleNetworkManager _networkManager, NettleNetworkDiscovery _networkDiscovery, UNetPrefabsList[] _unetPrefabs) : base(_networkManager, _networkDiscovery, _unetPrefabs) {
    }
    

    public void StartServer() {
        bool started = networkManager.StartServer();
        if (started) {
            networkManager.ListenServerAddPlayerEvent(HandleServerAddPlayerEvent);
            networkManager.ListenClientDisconnectsFromServerEvent(HandleClientDisconnectsFromServerEvent);
        }
    }

    public void StartDiscoveryBroadcast() {
        networkDiscovery.Initialize();
        networkDiscovery.StartAsServer();
    }

    public void SetDiscoveryInterval(int miliseconds) {
        networkDiscovery.broadcastInterval = miliseconds;
    }

    public void StopDiscovery() {
        networkDiscovery.StopBroadcast();
    }

    private void HandleServerAddPlayerEvent(NetworkConnection conn) {
        if (ClientConnectedEvent != null)
            ClientConnectedEvent(conn);
    }

    private void HandleClientDisconnectsFromServerEvent(NetworkConnection conn) {
        if (ClientDisconnectedEvent != null)
            ClientDisconnectedEvent(conn);
    }

    

    public GameObject SpawnNetObject(Type componentType, NetworkConnection connection) {
        GameObject instance = UnityEngine.Object.Instantiate(UNetCore.Instance.FindLinkPrefab(componentType));
        (instance.GetComponent<RALink>()).connection = connection;
        NetworkServer.SpawnWithClientAuthority(instance, connection);
        if (SpawnObjectEvent != null)
            SpawnObjectEvent.Invoke(instance);
        return instance;
    }

    
    

    public GameObject[] SpawnNetObjects(Type componentType) {
        List<GameObject> createdObjects = new List<GameObject>();
        NetworkConnection[] connections = NetworkServer.connections.ToArray();
        foreach (NetworkConnection conn in connections) {
            if (conn != null)
                createdObjects.Add(SpawnNetObject(componentType, conn));
        }
        return createdObjects.ToArray();
    }    
}

}
