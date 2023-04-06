using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;

namespace Nettle {

public abstract class RALink : NetworkBehaviour {
    
    private event Func<GameObject, bool> OnRpcIsReady;
    public event Action OnCmdRequestSync;

    public bool dontDestroyOnLoad = false;
    [SerializeField]
    private string identificator = string.Empty;
    public string Identificator {
        get {
            return identificator;
        }
        set {
            identificator = value;
            gameObject.name = "RA_Link_" + identificator;
            if (isServer) {
                RpcSetId(identificator);
            }
        }
    }
  
    private bool isReady = false;

    public NetworkConnection connection;

    //[Command]'s will do on server (call on client)
    [Command(channel = (int)EQoSChannels.RELIABLE_SEQUENCED)]
    public void CmdRequestSync() {
        if (OnCmdRequestSync != null)
            OnCmdRequestSync.Invoke();
    }

    //[ClientRpc]'s will do on client (call on server)
    [ClientRpc]
    private void RpcSetId(string _id) {
        Identificator = _id;
    }

    public void RpcReady() {
        if (isServer && !isReady) {
            isReady = true;
            RpcIsReady();
        }
    }

    [ClientRpc(channel = (int)EQoSChannels.RELIABLE_SEQUENCED)]
    private void RpcIsReady() {
        isReady = true;
        if (OnRpcIsReady != null)
            OnRpcIsReady(gameObject);
    }

    public void ExecuteMethodWhenRpcIsReady(Func<GameObject, bool> method) {
        if (isReady) {
            method(gameObject);
        }
        else {
            OnRpcIsReady += method;
        }
    }

    private void Awake() {
        if (dontDestroyOnLoad) {
            DontDestroyOnLoad(this);
        }
    }    

    /// <summary>
    /// called on the server on each networked object when a new player enters the game. 
    /// If it returns true, then that player is added to the object’s observers. 
    /// Using to prevent spawn objects(already spawned by other clients) on new connected clients.
    /// </summary>
    /// <returns></returns>
    override public bool OnCheckObserver(NetworkConnection newObserver) {
        return false;
    }

    override public bool OnRebuildObservers(HashSet<NetworkConnection> observers, bool initialize) {
        observers.Add(connection);
        return true;
    }
}
}
