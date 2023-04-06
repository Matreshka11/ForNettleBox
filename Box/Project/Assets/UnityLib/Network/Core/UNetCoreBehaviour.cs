using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Nettle {

public abstract class UNetCoreBehaviour {
    protected NettleNetworkManager networkManager;
    protected NettleNetworkDiscovery networkDiscovery;
    protected UNetPrefabsList[] unetPrefabs;

    public UNetCoreBehaviour(NettleNetworkManager _networkManager, NettleNetworkDiscovery _networkDiscovery, UNetPrefabsList[] _unetPrefabs) {
        networkManager = _networkManager;
        networkDiscovery = _networkDiscovery;
        unetPrefabs = _unetPrefabs;
    }
}
}
