using System;
using UnityEngine;
using UnityEngine.Networking;

namespace Nettle {

public class NettleNetworkManager : NetworkManager {


    //server
    private event Action<NetworkConnection> ServerAddPlayerEvent;
    private event Action<NetworkConnection> ClientDisconnectsFromServerEvent;

    //client
    private event Action ConnectedToServerEvent;
    private event Action DisconnectedFromServerEvent;
    private event Action ErrorOnClientEvent;

   

    //Called on the server when a client adds a new player with ClientScene.AddPlayer.
    override public void OnServerAddPlayer(NetworkConnection conn, short playerControllerId) {
        //The default implementation for this function creates a new player object from the playerPrefab.
        base.OnServerAddPlayer(conn, playerControllerId);

        if (ServerAddPlayerEvent != null) {
            ServerAddPlayerEvent(conn);
        }
    }

    //Called on the server when a client disconnects.
    override public void OnServerDisconnect(NetworkConnection conn) {
        base.OnServerDisconnect(conn);

        if (ClientDisconnectsFromServerEvent != null) {
            ClientDisconnectsFromServerEvent(conn);
        }
    }

    //Called on the server when a network error occurs for a client connection.
    override public void OnServerError(NetworkConnection conn, int errorCode) {
        base.OnServerError(conn, errorCode);

        Debug.Log("Connecting error code: " + errorCode);
    }

    //Called on the client when connected to a server.
    override public void OnClientConnect(NetworkConnection conn) {
        //The default implementation of this function sets the client as ready and adds a player.
        base.OnClientConnect(conn);

        if (ConnectedToServerEvent != null) {
            ConnectedToServerEvent();
        }
    }

    //Called on clients when disconnected from a server.
    override public void OnClientDisconnect(NetworkConnection conn) {
        base.OnClientDisconnect(conn);

        if (DisconnectedFromServerEvent != null) {
            DisconnectedFromServerEvent();
        }
    }

    //Called on clients when a network error occurs.
    override public void OnClientError(NetworkConnection conn, int errorCode) {
        base.OnClientError(conn, errorCode);

        Debug.Log("Connecting error code: " + errorCode);
        if (ErrorOnClientEvent != null) {
            ErrorOnClientEvent();
        }
    }

    //server
    public void ListenServerAddPlayerEvent(Action<NetworkConnection> listener) {
        AddSingleListener(ref ServerAddPlayerEvent, ref listener);
    }

    public void ListenClientDisconnectsFromServerEvent(Action<NetworkConnection> listener) {
        AddSingleListener(ref ClientDisconnectsFromServerEvent, ref listener);
    }

    private void AddSingleListener(ref Action<NetworkConnection> observable, ref Action<NetworkConnection> observer) {
        //Remove All Listeners
        if (observable != null) {
            Delegate[] dlgts = observable.GetInvocationList();
            foreach (Delegate dlgt in dlgts) {
                observable -= (dlgt as Action<NetworkConnection>);
            }
        }
        //Add listener
        observable += observer;
    }

    //client
    public void ListenConnectedToServerEvent(Action listener) {
        AddSingleListener(ref ConnectedToServerEvent, ref listener);
    }

    public void ListenDisconnectedFromServerEvent(Action listener) {
        AddSingleListener(ref DisconnectedFromServerEvent, ref listener);
    }

    public void ListenErrorOnClientEvent(Action listener) {
        AddSingleListener(ref ErrorOnClientEvent, ref listener);
    }

    private void AddSingleListener(ref Action observable, ref Action observer) {
        //Remove All Listeners
        if (observable != null) {
            Delegate[] dlgts = observable.GetInvocationList();
            foreach (Delegate dlgt in dlgts) {
                observable -= (dlgt as Action);
            }
        }
        //Add listener
        observable += observer;
    }
}
}
