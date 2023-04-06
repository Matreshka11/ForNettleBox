using UnityEngine;
using System.Collections;

namespace Nettle {

public class SetCameraClippingPlanes : MonoBehaviour {

    public bool Enabled = true;
    public Camera Cam;
    public StereoEyes Eyes;
        
    public float MinNearClip = 1;
    public float MaxSceneDepth = 0.0f;

    void Reset() {
        if (!Cam) {
            Cam = GetComponent<Camera>();
        }
        if (!Eyes) {
            SceneUtils.FindObjectIfSingle(ref Eyes);
        }
    }

    void Start () {
        if (!Cam) {
            Cam = GetComponent<Camera>();
        }
	}
	
	// Update is called once per frame
	void Update () {
	    if (!Enabled || !Cam || !Eyes) { return; }

	    var farClip = Eyes.transform.position.y - MaxSceneDepth;
	    farClip += farClip * 0.25f / 0.75f;
        var nearClip = farClip / 100.0f * MinNearClip;

	    Cam.farClipPlane = farClip;
	    Cam.nearClipPlane = nearClip;

	}
}
}
