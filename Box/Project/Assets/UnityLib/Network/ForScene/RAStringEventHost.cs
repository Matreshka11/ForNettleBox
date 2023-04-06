using System;
using System.Text;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.Networking;

namespace Nettle {

/// <summary>
/// Sends an event which takes string as a parameter
/// </summary>
public class RAStringEventHost : RemoteActionsUser {
    [Serializable]
    public class StringEvent : UnityEvent<string> {
    }

    public StringEvent Event = new StringEvent();
    
    private NetworkConnection _currentSenderConnection = null;
    [SerializeField]
    private EQoSChannels channel = EQoSChannels.RELIABLE_SEQUENCED;
        public string CurrentStringValue {
            get {
                return currentStringValue;
            }
        }
    [SerializeField]
    private string currentStringValue = "";
    
    protected override void OnAddLink(RALink raLink) {
        if (netBehaviour == NetBehaviour.SERVER) {
            (raLink as RABytesLink).CmdSetBytesEvent += EventCmd;
        } else if (netBehaviour == NetBehaviour.CLIENT) {
            (raLink as RABytesLink).RpcSetBytesEvent += EventRpc;
        }
    }

    public void SendStringEvent(string str) {
        if (netBehaviour == NetBehaviour.SERVER && currentStringValue == str) {
            SendEvent(str, _currentSenderConnection);
            _currentSenderConnection = null;
        }
        else {
            SendEvent(str, null);
        }
    }

    protected override void Synchronize() {
        if (netBehaviour == NetBehaviour.SERVER) {
            SendEvent(currentStringValue, null);
        }
    }

    private void EventCmd(byte[] bytes, NetworkConnection senderConnection) {
        string str = "";
        if (bytes != null) {
            str = Encoding.ASCII.GetString(bytes);
        }
        SendEvent(str, senderConnection);
        _currentSenderConnection = senderConnection;
        currentStringValue = str;
        Event.Invoke(currentStringValue);

    }

    private void EventRpc(byte[] bytes) {
        string str = "";
        if (bytes != null) {
            str = Encoding.ASCII.GetString(bytes);
        }
        currentStringValue = str;
        Event.Invoke(currentStringValue);
    }
      

    private void SendEvent(string str, NetworkConnection exceptConnection) {
        currentStringValue = str;
        SendBytes(Encoding.ASCII.GetBytes(str),exceptConnection, channel);
    }
}
}
