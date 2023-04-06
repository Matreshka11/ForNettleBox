using UnityEngine.Networking;
using System.Collections.Generic;
using System;
using System.Linq;

namespace Nettle {

public class BroadcastSource{
    public string address;
    public int count;
    public BroadcastSource(string address) {
        this.address = address;
        count = 0;
    }
}

public class NettleNetworkDiscovery : NetworkDiscovery {
    public event Action<string, string> BroadcastReceivedEvent;

    private List<BroadcastSource> broadcastsSources = new List<BroadcastSource>();

    public override void OnReceivedBroadcast(string fromAddress, string data) {
        var src = broadcastsSources.FirstOrDefault(v => v.address == fromAddress);
        if(src == null) {
            src = new BroadcastSource(fromAddress);
            broadcastsSources.Add(src);
        }

        src.count++;

        if (BroadcastReceivedEvent != null) {
            BroadcastReceivedEvent.Invoke(fromAddress, data);
        }
    }
}
}
