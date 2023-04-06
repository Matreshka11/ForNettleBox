using System;
using System.Linq;
using UnityEngine;

namespace Nettle {

public class UNetCore : MonoBehaviour {
    [SerializeField]
    private UNetPrefabsList[] unetPrefabLists;
    [SerializeField]
    private GameObject playerPrefab;
    public ServerPart serverPart {
        get {
            return behaviour as ServerPart;
        }
    }
    public ClientPart clientPart {
        get {
            return behaviour as ClientPart;
        }
    }
    private UNetCoreBehaviour behaviour;
    
    public static RACore localPlayer = null;

    [HideInInspector]
    public NettleNetworkManager NetworkManager;
    private NettleNetworkDiscovery networkDiscovery;

    private static UNetCore instance;
    public static UNetCore Instance {
        get {
            if (instance == null) {
                instance = SceneUtils.FindObjectIfSingle<UNetCore>();
            }
            return instance;
        }
    }


    public void Init(NetBehaviour netBehaviour) {
        NetworkManager = GetComponent<NettleNetworkManager>();
        if (NetworkManager == null) {
            Debug.LogError("Can't find NettleNetworkManager component");
        }
        NetworkCustomConfig.ConfigureNetworkManager(ref NetworkManager);
        networkDiscovery = GetComponent<NettleNetworkDiscovery>();
        if (networkDiscovery == null) {
            Debug.LogError("Can't find NettleNetworkDiscovery component");
        }
        if (unetPrefabLists == null || unetPrefabLists.Length == 0) {
            Debug.LogWarning("No network prefabs specified!!!");
        }
        NetworkManager.autoCreatePlayer = true;
        NetworkManager.playerPrefab = playerPrefab;
        if (netBehaviour == NetBehaviour.SERVER) {
            behaviour = new ServerPart(NetworkManager, networkDiscovery, unetPrefabLists);
        }
        else if (netBehaviour == NetBehaviour.CLIENT) {
            behaviour = new ClientPart(NetworkManager, networkDiscovery, unetPrefabLists);
        }
    }

    public bool IsDontDestroyLink(Type objType) {
        return (FindLinkPrefab(objType).GetComponent<RALink>()).dontDestroyOnLoad;
    }

    public GameObject FindLinkPrefab(Type componentType) {
        return unetPrefabLists.Where(v => v != null).Select(v => v.GetPrefab(componentType)).FirstOrDefault();
    }

}
}
