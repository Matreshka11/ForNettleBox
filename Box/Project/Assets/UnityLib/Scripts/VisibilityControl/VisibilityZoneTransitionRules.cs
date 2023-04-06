using UnityEngine;
using System;
using System.Linq;

namespace Nettle {

[Serializable]
public class VisibilityZoneTransitionRules {
    [HideInInspector]
    public string inspectorText = "Transition rule";
    public VisibilityZone[] targetZones;
    public VisibilityZoneTransition transitionSettings;

    public bool Match(VisibilityZone zone) {
        return targetZones != null && targetZones.Any(v => v == zone);
    }
}
}
