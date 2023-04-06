using System.Collections.Generic;
using UnityEngine.Networking;
using System;

namespace Nettle {

public enum EQoSChannels {
    RELIABLE_SEQUENCED,
    RELIABLE_STATE_UPDATE,
    UNRELIABLE_SEQUENCED,
    UNRELIABLE
}

public static class NetworkCustomConfig {

    public static Dictionary<EQoSChannels, QosType> EnumToQoSType = new Dictionary<EQoSChannels, QosType>() {
        {EQoSChannels.RELIABLE_SEQUENCED, QosType.ReliableSequenced},
        {EQoSChannels.RELIABLE_STATE_UPDATE, QosType.ReliableStateUpdate},
        {EQoSChannels.UNRELIABLE_SEQUENCED, QosType.UnreliableSequenced},
        {EQoSChannels.UNRELIABLE, QosType.Unreliable}
    };

    public static void ConfigureNetworkManager(ref NettleNetworkManager netmanager) {
        netmanager.customConfig = true;
        netmanager.channels.Clear();
        EQoSChannels[] newChannels = ((EQoSChannels[])Enum.GetValues(typeof(EQoSChannels)));
        foreach (EQoSChannels qch in newChannels) {
            netmanager.channels.Add(EnumToQoSType[qch]);
        }

        //netmanager.connectionConfig.AckDelay = 100u; //33u
        //netmanager.connectionConfig.AllCostTimeout = 20u; //20u
        //netmanager.connectionConfig.DisconnectTimeout = 1000u; //2000u
        //netmanager.connectionConfig.FragmentSize = 2000; //500
        netmanager.connectionConfig.MaxCombinedReliableMessageCount = 100; //10
        //netmanager.connectionConfig.MaxCombinedReliableMessageSize = 100; //100
        //netmanager.connectionConfig.MaxSentMessageQueueSize = 256; //128
        ///interval between sends
        netmanager.connectionConfig.MinUpdateTimeout = 2u; //10u
        //netmanager.connectionConfig.NetworkDropThreshold = 10; //5
        //netmanager.connectionConfig.OverflowDropThreshold = 10; //5
        //netmanager.connectionConfig.PacketSize = 3000; //1500
        netmanager.connectionConfig.PingTimeout = 600u; //500u
        //netmanager.connectionConfig.ReducedPingTimeout = 100u; //100u
        //netmanager.connectionConfig.ResendTimeout = 700u; //1200u
    }
}
}
