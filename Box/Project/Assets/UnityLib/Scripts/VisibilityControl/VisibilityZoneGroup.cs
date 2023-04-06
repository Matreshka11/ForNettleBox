using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class VisibilityZoneGroup : MonoBehaviour {
    [Tooltip("If true, zone transition will be skipped if switching between zones inside group")]
    public bool SwitchWithoutTransition = true;
}
