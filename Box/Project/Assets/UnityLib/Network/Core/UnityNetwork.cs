using UnityEngine;
using System.Collections.Generic;
using UnityEngine.Networking;
using System;
using System.Linq;
#if UNITY_EDITOR
using UnityEditor;
#endif
using UnityEngine.SceneManagement;

namespace Nettle {

public enum NetBehaviour {
    NONE,
    SERVER,
    CLIENT
};

[RequireComponent(typeof(UNetCore))]
public class UnityNetwork : MonoBehaviour {

    private event Action<GameObject> NetworkObjectSpawnEvent;
    public bool reconnectOnLostFocus = true;
    /// <summary>
    /// if true, net behaviour is automatically set to CLIENT
    /// </summary>
    public bool isRemoteDisplay = false;
    [SerializeField]
    private NetBehaviour netBehaviour = NetBehaviour.NONE;
    public NetBehaviour NetBehaviour {
        get {
            return netBehaviour;
        }
    }
    /// <summary>
    /// true if ReassignListenersOnScene has been called at least once
    /// </summary>
    private static bool listenersInitialized = false;
    /// <summary>
    /// Fix for OnLevelFinishedLoading. Old OnLevelWasLoaded function used to be called only when a new scene was loaded, not at the starting scene, so we have to skip the first sceneLoaded delegate call.
    /// </summary>
    private bool sceneLoadedFirstTime = true;
    private bool _sceneLoadedAdditive = false;

    void Awake() {
        if (UNetCore.Instance == null) {
            Debug.LogError("Can't find UNetCore component ");
        }
    }

    void OnLevelFinishedLoading(Scene scene, LoadSceneMode mode) {
        _sceneLoadedAdditive = mode == LoadSceneMode.Additive;
        
        if (!sceneLoadedFirstTime) {
            ReassignListenersOnScene();
            if (netBehaviour == NetBehaviour.SERVER) {
                CreateObjects();
            } else if (netBehaviour == NetBehaviour.CLIENT) {
                AssignNetToSceneObjects();
            }
        } else {
            sceneLoadedFirstTime = false;
        }
        _sceneLoadedAdditive = false;
    }

    private void OnEnable() {
        SceneManager.sceneLoaded += OnLevelFinishedLoading;
    }

    private void OnDisable() {
        SceneManager.sceneLoaded -= OnLevelFinishedLoading;
    }

    bool wasPaused = false;

    void OnApplicationFocus(bool focused) {
        if (!reconnectOnLostFocus || netBehaviour != NetBehaviour.CLIENT) {
            return;
        }

        if (!focused) {
            wasPaused = true;
            UNetCore.Instance.clientPart.StopDiscovery();
            UNetCore.Instance.clientPart.Disconnect();
        } else {
            if (wasPaused) {
                wasPaused = false;
                UNetCore.Instance.clientPart.ListenDiscoveryBroadcast();
            }
        }
    }

    void Start() {

        if (isRemoteDisplay) {
            netBehaviour = NetBehaviour.CLIENT;
        }
        UNetCore.Instance.Init(netBehaviour);
        if (netBehaviour == NetBehaviour.SERVER) {
            UNetCore.Instance.serverPart.ClientConnectedEvent += HandleClientConnect;
            UNetCore.Instance.serverPart.ClientDisconnectedEvent += HandleClientDisconnect;
            InitServer();
        } else if (netBehaviour == NetBehaviour.CLIENT) {
            UNetCore.Instance.clientPart.SpawnObjectEvent += HandleObjectCreation;
            UNetCore.Instance.clientPart.CreateLocalPlayerEvent += ReassignListenersOnScene;
            UNetCore.Instance.clientPart.AvailableConnectionEvent += HandleAvailableConnectionEvent;
            UNetCore.Instance.clientPart.ListenDiscoveryBroadcast();
        }
    }

    private void HandleAvailableConnectionEvent(string address, string data) {
        if (netBehaviour == NetBehaviour.CLIENT) {
            if (!UNetCore.Instance.clientPart.IsConnected()) {
                UNetCore.Instance.clientPart.ConnectToServer(address);
            }
        }
    }

    void OnDestroy() {
        if (netBehaviour == NetBehaviour.SERVER) {
            UNetCore.Instance.serverPart.ClientConnectedEvent -= HandleClientConnect;
            UNetCore.Instance.serverPart.ClientDisconnectedEvent -= HandleClientDisconnect;
        } else if (netBehaviour == NetBehaviour.CLIENT) {
            UNetCore.Instance.clientPart.SpawnObjectEvent -= HandleObjectCreation;
            UNetCore.Instance.clientPart.CreateLocalPlayerEvent -= ReassignListenersOnScene;
            UNetCore.Instance.clientPart.AvailableConnectionEvent -= HandleAvailableConnectionEvent;
        }
    }

    private void InitServer() {
        UNetCore.Instance.serverPart.StartServer();
        UNetCore.Instance.serverPart.StartDiscoveryBroadcast();
    }

    private void HandleClientConnect(NetworkConnection newConnection) {
        if (!listenersInitialized) {
            ReassignListenersOnScene();
        }
        CreateObjects(newConnection);
    }

    private void HandleClientDisconnect(NetworkConnection newConnection) {
        //ChangeDiscoveryInterval();
    }

    private void HandleObjectCreation(GameObject obj) {
        if (NetworkObjectSpawnEvent != null) {
            NetworkObjectSpawnEvent(obj);
        }
    }

    //server

    /// <summary>
    /// Create objects for all clients
    /// </summary>
    private void CreateObjects() {
        CreateObjects(null);
    }
    /// <summary>
    /// Create objects for client, or for all clients
    /// </summary>
    private void CreateObjects(NetworkConnection connection) {
        GameObject[] createdObjects;

        //Find all gameobjects, including disabled ones

        SceneUtils.GetAllScenes().ForEach(v => Debug.Log("Found scene: " + v.name));

        RemoteActionsUser[] usersTemp = SceneUtils.FindSceneObjectsOfTypeAll<RemoteActionsUser>().ToArray();

        foreach (RemoteActionsUser user in usersTemp) {
            //check if dontDestroyObject already exists
            if (_sceneLoadedAdditive || user.DontDestroyLinkOnLoad || UNetCore.Instance.IsDontDestroyLink(user.GetTypeOfLink())) {
                bool skipCreation = false;
                UnityEngine.Object[] dontDestrObj = FindObjectsOfType(user.GetTypeOfLink());
                if (dontDestrObj != null) {
                    RALink link = null;
                    foreach (UnityEngine.Object obj in dontDestrObj) {
                        link = obj as RALink;
                        if (link != null) {
                            if ((connection == null || link.connection.Equals(connection)) && link.Identificator.Equals(user.GetIdentificator)) {
                                skipCreation = true;
                            }
                        }
                    }
                }
                if (skipCreation) {
                    continue;
                }
            }

            if (connection == null) {
                createdObjects = UNetCore.Instance.serverPart.SpawnNetObjects(user.GetTypeOfLink());
            } else {
                createdObjects = new GameObject[] { UNetCore.Instance.serverPart.SpawnNetObject(user.GetTypeOfLink(), connection) };
            }

            foreach (GameObject obj in createdObjects) {
                RALink raLink = obj.GetComponent<RALink>();
                string idForLink = user.GetIdentificator;
                raLink.Identificator = idForLink;
                raLink.RpcReady();
                //invoke creation event after change id, to link with user
                HandleObjectCreation(obj);
            }
        }
    }

    //both
    private void ReassignListenersOnScene() {
        listenersInitialized = true;
        //Remove All Listeners
        if (NetworkObjectSpawnEvent != null) {
            Delegate[] dlgts = NetworkObjectSpawnEvent.GetInvocationList();
            foreach (Delegate dlgt in dlgts) {
                NetworkObjectSpawnEvent -= (dlgt as Action<GameObject>);
            }
        }

        //RemoteActionsUser[] users = FindObjectsOfType<RemoteActionsUser>();
        RemoteActionsUser[] users = Resources.FindObjectsOfTypeAll<RemoteActionsUser>();
        foreach (RemoteActionsUser user in users) {
#if UNITY_EDITOR
            if (CheckPrefabOriginal(user.gameObject)) {
                continue;
            }
#endif
            user.SetNetBehaviour(netBehaviour);
            if (netBehaviour == NetBehaviour.SERVER && !(_sceneLoadedAdditive||user.DontDestroyLinkOnLoad || UNetCore.Instance.IsDontDestroyLink(user.GetTypeOfLink()))) {
                user.ClearLinks();
            }
            NetworkObjectSpawnEvent += user.HandleLinkSpawned;
        }

        CheckEqualIds(users);
    }

    //client
    private void AssignNetToSceneObjects() {
        NetworkIdentity[] netIdentities = FindObjectsOfType<NetworkIdentity>();
        LinkedList<NetworkIdentity> actNetIdentits = new LinkedList<NetworkIdentity>();
        foreach (NetworkIdentity ni in netIdentities) {
            if (ni.hasAuthority)
                actNetIdentits.AddLast(ni);
        }
        LinkedListNode<NetworkIdentity> node;
        //RemoteActionsUser[] users = FindObjectsOfType<RemoteActionsUser>();
        RemoteActionsUser[] users = Resources.FindObjectsOfTypeAll<RemoteActionsUser>();
        foreach (RemoteActionsUser user in users) {
#if UNITY_EDITOR
            if (CheckPrefabOriginal(user.gameObject)) {
                continue;
            }
#endif
            node = actNetIdentits.First;
            while (node != null) {
                if (user.ClientHandleLinkReady(node.Value.gameObject)) {
                    actNetIdentits.Remove(node);
                    break;
                } else {
                    node = node.Next;
                }
            }
        }
    }

    private void CheckEqualIds(RemoteActionsUser[] users) {
        Dictionary<Type, List<string>> allIds = new Dictionary<Type, List<string>>();
        foreach (RemoteActionsUser user in users) {
#if UNITY_EDITOR
            if (CheckPrefabOriginal(user.gameObject)) {
                continue;
            }
#endif
            string idForLink = user.GetIdentificator;
            Type userType = user.GetType();
            if (allIds.ContainsKey(userType)) {
                if (allIds[userType].Contains(idForLink)) {
                    Debug.LogErrorFormat("Object of type '{0}' already contains id '{1}'", userType, idForLink);
                }
                allIds[userType].Add(idForLink);
            } else {
                allIds.Add(user.GetType(), new List<string>() { idForLink });
            }
        }
    }
#if UNITY_EDITOR
    public bool CheckPrefabOriginal(GameObject gameObject) {
        return PrefabUtility.GetCorrespondingObjectFromSource(gameObject) == null && PrefabUtility.GetPrefabObject(gameObject) != null;
    }
#endif
}
}
