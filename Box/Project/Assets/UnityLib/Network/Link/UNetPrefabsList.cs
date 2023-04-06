using UnityEngine;
using System.Collections.Generic;
using UnityEngine.Networking;
using System;

namespace Nettle {

/// <summary>
/// List of RemoteCall Prefabs, used by client and server 
/// </summary>
public class UNetPrefabsList : MonoBehaviour {

    public List<GameObject> netCallPrefabs;

    public GameObject GetPrefab(NetworkHash128 netHash) {
        GameObject outPrefab = null;

        foreach (GameObject prefab in netCallPrefabs) {
            if (prefab.GetComponent<NetworkIdentity>().assetId.Equals(netHash)) {
                outPrefab = prefab;
                break;
            }
        }

        if (outPrefab == null)
            Debug.LogError("GetPrefab: can't find object with hash " + netHash.ToString());

        return outPrefab;
    }

    public GameObject GetPrefab(Type componentType) {
        GameObject outPrefab = null;

        foreach (GameObject prefab in netCallPrefabs) {
            if (componentType.Equals((prefab.GetComponent<RALink>()).GetType())) {
                outPrefab = prefab;
                break;
            }
        }

        if (outPrefab == null)
            Debug.LogError("GetPrefab: can't find object with type " + componentType.ToString());

        return outPrefab;
    }

    public List<GameObject> GetObjectsPrefabs() {
        return netCallPrefabs;
    }
}
}
