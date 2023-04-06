using UnityEngine.Networking;
using System;
using UnityEngine;

namespace Nettle {

////Remote actions
public class RABytesLink : RALink {

    public event Action<byte[], NetworkConnection> CmdSetBytesEvent;
    public event Action<byte[]> RpcSetBytesEvent;

    //[Command]'s will do on server (call on client)
    [Command(channel = (int)EQoSChannels.RELIABLE_SEQUENCED)]
    private void CmdSetBytes_Sequenced(byte[] data) {
        //Debug.LogFormat("Cmd {0} | {1}", val, GetType());
        if (CmdSetBytesEvent != null)
            CmdSetBytesEvent(data, connection);
    }

    [Command(channel = (int)EQoSChannels.RELIABLE_STATE_UPDATE)]
    private void CmdSetBytes_StateUpdate(byte[] data) {
        //Debug.LogFormat("Cmd {0} | {1}", val, GetType());
        if (CmdSetBytesEvent != null)
            CmdSetBytesEvent(data, connection);
    }

    //[ClientRpc]'s will do on client (call on server)
    [ClientRpc(channel = (int)EQoSChannels.RELIABLE_SEQUENCED)]
    private void RpcSetBytes_Sequenced(byte[] data) {

        //Debug.LogFormat("Rpc {0} | {1}", val, GetType());
        if (RpcSetBytesEvent != null)
            RpcSetBytesEvent(data);
    }

    [ClientRpc(channel = (int)EQoSChannels.RELIABLE_STATE_UPDATE)]
    private void RpcSetBytes_StateUpdate(byte[] data) {
        
        //Debug.LogFormat("Rpc {0} | {1}", val, GetType());
        if (RpcSetBytesEvent != null)
            RpcSetBytesEvent(data);
    }

    public void SendBytes(byte[] data, EQoSChannels channel) {
        if (channel == EQoSChannels.RELIABLE_SEQUENCED) {
            if (isClient) {
                CmdSetBytes_Sequenced(data);
            }
            else {
                RpcSetBytes_Sequenced(data);
            }
        }
        else if (channel == EQoSChannels.RELIABLE_STATE_UPDATE) {
            if (isClient) {
                CmdSetBytes_StateUpdate(data);
            }
            else {
                RpcSetBytes_StateUpdate(data);
            }
        }
    }
}
}
