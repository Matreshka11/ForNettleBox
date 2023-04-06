using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;

namespace Nettle {

abstract public class RemoteActionsUser : MonoBehaviour {
    /// <summary>
    /// Set true if the spawned link should not be destroyed when a new scene is loaded
    /// </summary>
    public bool DontDestroyLinkOnLoad = false;
    /// <summary>
    /// The id name of this object which enables remote network entities to find their matches
    /// </summary>
    [SerializeField]
    protected string identificator = string.Empty;
    public string GetIdentificator {
        get {
            return identificator;
        }
    }
    [SerializeField]
    private List<RALink> raLinks = new List<RALink>();
    protected NetBehaviour netBehaviour = NetBehaviour.NONE;

    public void HandleLinkSpawned(GameObject spawnedObj) {
        RALink raLink = spawnedObj.GetComponentInChildren(GetTypeOfLink()) as RALink;
        if (raLink != null) {
            if (DontDestroyLinkOnLoad && CompareIdentificators(raLink)) {
                DontDestroyOnLoad(spawnedObj);
            }
            if (netBehaviour == NetBehaviour.SERVER) {
                if (CompareIdentificators(raLink)) {
                    raLink.OnCmdRequestSync += Synchronize;
                    raLinks.Add(raLink);
                    OnAddLink(raLink);
                }
            } else if (netBehaviour == NetBehaviour.CLIENT) {
                raLink.ExecuteMethodWhenRpcIsReady(ClientHandleLinkReady);
            }
        }
    }

    public virtual Type GetTypeOfLink() {
        return typeof(RABytesLink);
    }

    protected abstract void OnAddLink(RALink raLink);

    void OnDestroy() {
        foreach (var link in raLinks) {
            UnsubscribeFromLink(link);
        }
    }

    protected virtual void UnsubscribeFromLink(RALink link) {

    }



    // Synchronizes the object on the server with linked object on the clients
    protected virtual void Synchronize() { }

    void Awake() {
        if (raLinks.Count > 0) {
            Debug.LogError("Available links on awake on object: " + name);
        }
    }



    protected RALink[] GetLinks() {
        return raLinks.ToArray();
    }

    public void ClearLinks() {
        raLinks.Clear();
    }

    private bool CompareIdentificators(RALink other) {
        return (GetIdentificator.Equals(other.Identificator));
    }

    //client
    public bool ClientHandleLinkReady(GameObject linkObject) {
        bool added = false;
        if (raLinks.Count < 1 || raLinks[0] == null) {
            if (linkObject.GetComponent<NetworkIdentity>().hasAuthority) {
                RALink raLink = linkObject.GetComponent<RALink>();
                if (raLink != null && (raLink.GetType()).Equals(GetTypeOfLink())) {
                    if (CompareIdentificators(raLink)) {
                        if (raLinks.Count < 1) {
                            raLinks.Add(raLink);
                        } else {
                            raLinks[0] = raLink;
                        }
                    }

                    if (raLinks.Count > 0 && raLinks[0] != null) {
                        OnAddLink(raLinks[0]);
                        raLinks[0].CmdRequestSync();
                        added = true;
                    }
                }
            }
        }
        return added;
    }

    public void SetNetBehaviour(NetBehaviour behaviour) {
        if (netBehaviour == NetBehaviour.NONE) {
            netBehaviour = behaviour;
            OnSetNetBehaviour();
        }
    }

    public void RequestSync() {
        if (netBehaviour == NetBehaviour.CLIENT) {
            if (raLinks.Count > 0) {
                raLinks[0].CmdRequestSync();
            }
        }
    }

    protected virtual void OnSetNetBehaviour() {
    }

    /// <summary>
    /// Use this to send byte array.
    /// </summary>
    /// <param name="data">Byte array to send</param>
    /// <param name="excludeConnection">The connection that should be ignored when sending from server to clients</param>
    /// <param name="channel">Determines which channel will be used to send bytes</param>
    protected void SendBytes(byte[] data, NetworkConnection excludeConnection = null, EQoSChannels channel = EQoSChannels.RELIABLE_SEQUENCED) {
        RALink[] links = GetLinks();
        foreach (RABytesLink link in links) {
            if (link != null) {
                if (excludeConnection == null || !excludeConnection.Equals(link.connection)) {
                    link.SendBytes(data, channel);
                }
            }
        }
    }
}
}
