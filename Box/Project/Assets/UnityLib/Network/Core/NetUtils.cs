using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;

namespace Nettle {

public class NetUtils {

    /// <summary>
    /// Time of sending a message (Receiver time) 
    /// </summary>
    public static float GetMsgTime(NetworkConnection connection, int networkTimestamp) {
        byte errorByte;
        return Time.realtimeSinceStartup - NetworkTransport.GetRemoteDelayTimeMS(connection.hostId, connection.connectionId, networkTimestamp, out errorByte) * 0.001f;
    }
}
}
