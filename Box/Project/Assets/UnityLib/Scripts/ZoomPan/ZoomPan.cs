using System;
using UnityEngine;
using UnityEngine.Events;

namespace Nettle {

public class ZoomPan : MonoBehaviour {
    public Transform Target;
    public NettleBoxTracking Tracking;
    public bool MoveEnabled = true;
    public bool FlipXAxis = false;
    public bool FlipZAxis = false;
    public bool SwapXZ = false;

    public bool ZoomEnabled = true;

    public float ZoomSpeed = 0.8f;
    public float MinZoom = 0.1f;
    public float MaxZoom = 10.0f;
    public bool MoveRelativeToUser = false;
    public bool LocalSpace = true;

    private Vector3 _startScale;
    public float StartScale {
        get {
            return _startScale.x;
        }
    }

    void Reset() {
        if (!Target) {
            var mpDisplay = SceneUtils.FindObjectIfSingle<MotionParallaxDisplay>();
            if (mpDisplay) {
                Target = mpDisplay.transform;
            }
        }

        if (!Tracking) {
            Tracking = SceneUtils.FindObjectIfSingle<NettleBoxTracking>();
        }
    }


    public void SetStartScale(Vector3 startScale, bool resetZoom = true) {
        _startScale = startScale;
        if (resetZoom) {
            CurrentZoom = 1.0f;
            //_previousZoom = _currentZoom;
        }
    }

    public Vector3 Position {
        set { Target.transform.position = value; }
        get { return Target.transform.position; }
    }

    /* public float Zoom {
         set {
             _currentZoom = Mathf.Clamp(value, MinZoom, MaxZoom);
             _previousZoom = _currentZoom;
             Target.transform.localScale = _startScale * _previousZoom;
         }
         get { return _currentZoom;}
     }*/

    
    [HideInInspector]
    public float CurrentZoom = 1.0f;
    //private float _previousZoom = 1.0f;

    //private float _zoomSmoothSpeed = 2.0f;

    public bool ViewUnits = true;
    
    public event Action<Vector3> OnMoved;
    public event Action<float> OnZoomed;

    private bool _waitForApply;

    void Start() {
        _startScale = Target.transform.lossyScale;
    }

    public void MoveRelativeToUserSwitch() {
        MoveRelativeToUser = !MoveRelativeToUser;
    }

    public void Move(float axisX, float axisY) {
        if (MoveEnabled) {
            var offset = new Vector3 {
                x = (SwapXZ ? axisY : axisX) * (FlipXAxis ? 1.0f : -1.0f),
                z = (SwapXZ ? axisX : axisY) * (FlipXAxis ? 1.0f : -1.0f)
            };

            if (MoveRelativeToUser) {
                var right = Vector3.Normalize(Tracking.RightEye - Tracking.LeftEye);
                var front = Vector3.Normalize(Vector3.Cross(right, Vector3.up));
                right = Vector3.Normalize(Vector3.Cross(Vector3.up, front));
                offset = right * offset.x + front * offset.z;
            } else if (LocalSpace) {
                var right = Target.transform.right;
                var front = Target.transform.forward;
                offset = right * offset.x + front * offset.z;
            }

            //float unitScale = ViewUnits ? transform.lossyScale.z * 2 : 1;

            if (offset != Vector3.zero) {
                Target.transform.position += offset * Target.transform.localScale.x * 2;
                if (OnMoved != null)
                    OnMoved(Target.transform.position);
                // Target.transform.position += offset /* unitScale*/ * Target.transform.localScale.x / 100;
                //Target.transform.Translate(offset* unitScale, LocalSpace ? Space.Self : Space.World);
            }
        }
    }

    public void DoZoom(float axis) {
        if (ZoomEnabled) {
            CurrentZoom = Mathf.Clamp(CurrentZoom * Mathf.Pow(ZoomSpeed, axis), MinZoom, MaxZoom);

           // _previousZoom = Mathf.Lerp(_previousZoom, _currentZoom, _zoomSmoothSpeed * Time.deltaTime);
            Target.transform.localScale =  _startScale * CurrentZoom;
            if (OnZoomed != null)
                OnZoomed(CurrentZoom);
        }
    }

    public void SetMoveEnabled(bool value) {
        MoveEnabled = value;
    }

    public void SetZoomEnabled(bool value) {
        ZoomEnabled = value;
    }

    public void DoZoomDirect(float scale) {
        if (ZoomEnabled) {
            CurrentZoom = Mathf.Clamp(CurrentZoom * scale, MinZoom, MaxZoom);
            Target.transform.localScale = _startScale * CurrentZoom;

            if (OnZoomed != null)
                OnZoomed(CurrentZoom);
        }
    }
}
}
