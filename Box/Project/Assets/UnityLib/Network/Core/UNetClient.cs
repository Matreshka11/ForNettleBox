using System;
using System.Linq;
using UnityEngine;
using UnityEngine.Networking;

namespace Nettle {

public class ClientPart : UNetCoreBehaviour {

    private enum ClientConnectionState {
        Disconnected,
        Connecting,
        Connected
    }

    public event Action<string, string> AvailableConnectionEvent;
    public event Action ConnectedToServerEvent;
    public event Action DisconnectedFromServerEvent;
    public event Action<GameObject> SpawnObjectEvent;
    public event Action CreateLocalPlayerEvent;

    public ClientPart(NettleNetworkManager _networkManager, NettleNetworkDiscovery _networkDiscovery, UNetPrefabsList[] _unetPrefabs) : base(_networkManager, _networkDiscovery, _unetPrefabs) {
        networkDiscovery.BroadcastReceivedEvent += HandleBroadcastReceivedEvent;
    }


    public void ListenDiscoveryBroadcast() {
        networkDiscovery.Initialize();
        networkDiscovery.StartAsClient();
    }

    public void StopDiscovery() {
            networkDiscovery.StopBroadcast();
    }

    public void ConnectToServer(string address) {
        networkManager.ListenConnectedToServerEvent(HandleConnectedToServerEvent);
        networkManager.ListenDisconnectedFromServerEvent(HandleDisconnectedFromServerEvent);
        networkManager.ListenErrorOnClientEvent(HandleErrorOnClientEvent);

        //register player prefab to be able spawn it
        RegisterPrefabAndHandlers(networkManager.playerPrefab, SpawnObjectEvent);
        //register all prefabs of objects, which will be able to spawn
        foreach (var prefabToRegister in unetPrefabs.SelectMany(v=>v.GetObjectsPrefabs())) {
            RegisterPrefabAndHandlers(prefabToRegister, SpawnObjectEvent);
        }

        networkManager.networkAddress = address;
        //here connects to the server (using networkManager.networkAddress)
        networkManager.StartClient();
    }

    /// <summary>
    /// Register prefab to spawn net objects on scene
    /// </summary>
    /// <param name="prefab"></param>
    /// <param name="onSpawnFunction"> Will be called when object spawns </param>
    private void RegisterPrefabAndHandlers(GameObject prefab, Action<GameObject> onSpawnFunction) {
        if (ClientScene.prefabs.ContainsKey(prefab.GetComponent<NetworkIdentity>().assetId)) {
            return;
        }
        SpawnDelegate spawnDelegate = (position, networkHash) => {
            Debug.Log("Spawn link: " + prefab.GetComponentInChildren<RALink>().GetType().Name + " at time " + Time.realtimeSinceStartup); 
            GameObject instance = UnityEngine.Object.Instantiate(prefab, position, Quaternion.identity) as GameObject;
            UnityEngine.Object.DontDestroyOnLoad(instance);
            onSpawnFunction(instance);
            return instance;
        };
        UnSpawnDelegate unspawnDelegate = (obj) => {
            UnityEngine.Object.Destroy(obj);
        };        
        ClientScene.RegisterPrefab(prefab, spawnDelegate, unspawnDelegate);        
    }

    public bool IsConnected() {
        return networkManager.isNetworkActive;
    }

    public void Disconnect() {
        networkManager.StopHost();
        //call to avoid bug on iPad, when push power button
        NetworkClient.ShutdownAll();
    }

    private void HandleConnectedToServerEvent() {
        RACore.StartLocalPlayerEvent += HandleStartLocalPlayerEvent;

        if (ConnectedToServerEvent != null)
            ConnectedToServerEvent();
    }

    private void HandleDisconnectedFromServerEvent() {
        if (DisconnectedFromServerEvent != null)
            DisconnectedFromServerEvent();
    }

    private void HandleErrorOnClientEvent() {

    }
    
    private void HandleStartLocalPlayerEvent(RACore playersScriptComponent) {
        if (!playersScriptComponent.gameObject.GetComponent<NetworkIdentity>().assetId.Equals(networkManager.playerPrefab.GetComponent<NetworkIdentity>().assetId)) {
            Debug.LogError("Player spawn failed. Other object was spawned");
        } else {
            if (CreateLocalPlayerEvent != null)
                CreateLocalPlayerEvent.Invoke();
        }
        RACore.StartLocalPlayerEvent -= HandleStartLocalPlayerEvent;
    }

    private void HandleBroadcastReceivedEvent(string address, string port) {
        if (AvailableConnectionEvent != null) {
            AvailableConnectionEvent.Invoke(address, port);
        }
    }
}
}
