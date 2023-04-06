using UnityEngine;
using System;
using System.Linq;
using UnityEngine.Events;
using System.Collections;

namespace Nettle {

[Serializable]
public class OnShowZone : UnityEvent<VisibilityZone> { }

public enum NettleDeviceType {
    NettleBox, NettleDesk
}


public class VisibilityZoneViewer : MonoBehaviour {
    public string StartupZone = "Start";
    public AnimationCurve SpeedCurve = AnimationCurve.EaseInOut(0, 0, 1, 1);
    public bool ParabolicTransition = false;
    public float ParabolicTransitionHeight = 300;
    [Range(0, 1)]
    public float CurvePeakWidth = 0.3f;
    public float MinTransitionHeight = 100f;
    public float TransitionSpeedToMinHeight = 0.1f;
    public float MinTransitionHeightApplyDistance = 10;
    [Space(10)]
    public static NettleDeviceType DeviceType = NettleDeviceType.NettleBox;
    //TODO: OnShowZone, ZoneChanged and ActiveZoneChanged? 
    public event Action ZonesChanged;
    public event Action ActiveZoneChanged;
    private VisibilityZone[] _zones;
    private VisibilityZone _activeZone;
    public VisibilityManager Manager;
    //public TexturesStreaming Streamer;
    private NTransform _startTransform;
    private NTransform _endTransform;
    public GameObject Display;

    public NettleBoxTracking Tracking;
        
    public float TransitionSpeed = 2.5f;

    public bool AdaptiveTransitionSpeed = false;
    public float MinTransitionDistance = 250;
    public float MaxTransitionDistance = 1000;
    public float MinDistanceTransitionSpeed = 2.5f;
    public float MaxDistanceTransitionSpeed = 5f;

    public bool SpeedInMetersPerSecond = false;



    private float _currentTransitionSpeed = 2.5f;

    [HideInInspector]
    public bool TransitionRunning;
    private bool _applyTransform;

    public ZoomPan Zoompan;
    public ZoomPanZoneController ZoneControl;

    public UnityEvent TransitionEnd;
    public UnityEvent TransitionBegin;

    private float _transitionTime;

    private float _startTransitionSpeed = 0.0f;

    private float _maxZoneScale = 0;

    private bool _startTransitionModeMpS = false;
    private AnimationCurve _scaleTransitionCurve = null;
    public OnShowZone OnShowZone = new OnShowZone();
    private float _currentMinTransitionHeight = 0;

    public void ResetView() {
        if (ActiveZone != null) {
            ShowZone(ActiveZone.name);
        }
    }

    public VisibilityZone[] Zones {
        get { return _zones; }
        set { _zones = value; OnZonesChanged(); }
    }

    public VisibilityZone ActiveZone {
        get { return _activeZone; }
        set { _activeZone = value; OnActiveZoneChanged(); }
    }

    public void SetTransitionSpeed(float speed) {
        TransitionSpeed = speed;
    }

    public void SetTransitionModeMpS(bool state) {
        SpeedInMetersPerSecond = state;
    }

    public void ResetTransitionSpeed() {
        TransitionSpeed = _startTransitionSpeed;
    }

    public void ResetTransitionMode() {
        SpeedInMetersPerSecond = _startTransitionModeMpS;
    }

    private static VisibilityZoneViewer _instance;
    public static VisibilityZoneViewer Instance {
        get {
            return _instance;
        }
    }

    void Awake() {
        _instance = this;
        if (TransitionEnd == null) {
            TransitionEnd = new UnityEvent();
        }

        if (TransitionBegin == null) {
            TransitionBegin = new UnityEvent();
        }

        FindZones();

        _startTransitionSpeed = TransitionSpeed;
        _startTransitionModeMpS = SpeedInMetersPerSecond;
    }

    void Reset() {
        SceneUtils.FindObjectIfSingle(ref ZoneControl);
        SceneUtils.FindObjectIfSingle(ref Zoompan);
        //SceneUtils.FindObjectIfSingle(ref Streamer);
        SceneUtils.FindObjectIfSingle(ref Manager);
    }

    public bool NotUseStartUpZone = false;
    private void Start() {
        if (!String.IsNullOrEmpty(StartupZone) && !NotUseStartUpZone) {
            var zone = GetZoneByName(StartupZone);
            if (zone != null) {
                zone.GetNTransform().Apply(Display.transform);
            }

            ShowZone(StartupZone);
        }
    }


    protected void OnZonesChanged() {
        if (ZonesChanged != null) {
            ZonesChanged();
        }
    }

    protected void OnActiveZoneChanged() {
        if (ActiveZoneChanged != null) {
            ActiveZoneChanged();
        }
    }

    public VisibilityZone GetZoneByName(string zoneName) {
        VisibilityZone zone = null;

        if (Zones != null)
            zone = Zones.FirstOrDefault(v => v != null && v.name == zoneName);

        return zone;
    }

    public void FindZones() {
        _zones = (VisibilityZone[])FindObjectsOfType(typeof(VisibilityZone));
        _maxZoneScale = _zones.Max(x => x.transform.localScale.x);
    }

    public void OnTransitionEnd() {
        if (TransitionEnd != null) {
            TransitionEnd.Invoke();
        }
    }


    public void OnTransitionBegin() {
        if (TransitionBegin != null) {
            TransitionBegin.Invoke();
        }

    }

    public void ShowZoneWithoutTransition(string zoneName) {
        /* VisibilityZone zone = GetZoneByName(zoneName);
         if (zone != null) {
             Manager.BeginSwitch(zone.VisibilityTag);

             ActiveZone = zone;

             zone.Show();

             if (OnShowZone != null)
                 OnShowZone.Invoke(zone);

             OnTransitionEnd();
         }*/

        ShowZone(GetZoneByName(zoneName), false);
    }

    public void ShowZone(string zoneName) {
        bool applyTransform = true;
        VisibilityZone zone = GetZoneByName(zoneName);
        if (ActiveZone != null) {
            var settings = ActiveZone.GetTransitionSettings(zone);
            if (settings != null) {
                applyTransform = settings.affectTransform;
            }
        }
        ShowZone(zone, applyTransform);
    }

    private void ShowZone(VisibilityZone zone, bool applyTransform) {
        if (!TransitionRunning || applyTransform) {
            /*if (_applyTransform) {
                if (Zoompan != null) {
                    Zoompan.ZoomEnabled = true;
                    Zoompan.MoveEnabled = true;
                }
            }*/
            _applyTransform = applyTransform;
        }
        //print("ShowZone " +zoneName);
        //if (zone == null || ActiveZone == zone) {
        //	return;
        //}

        if (zone == null) {
            return;
        }
        Debug.Log("ShowZone " + zone.name);

        var oldZone = ActiveZone;
        ActiveZone = zone;

        if (zone.IsStaticMP3D && Tracking != null) {
            Tracking.LeftEye = zone.StaticLeftEye;
            Tracking.RightEye = zone.StaticRightEye;
        } else {
            ActiveZone.OnChangeStaticState += (v) => {
                if (v && Tracking != null) {
                    Tracking.LeftEye = zone.StaticLeftEye;
                    Tracking.RightEye = zone.StaticRightEye;
                }
            };
        }
        _startTransform = new NTransform(Display.transform);
        _endTransform = new NTransform(zone.GetTransform());

        //Calculate distance between target camera positions, taking scale into account
        float groundLevelDistance = (new Vector2(_startTransform.Position.x, _startTransform.Position.z) - new Vector2(_endTransform.Position.x, _endTransform.Position.z)).magnitude;
        float distance = Mathf.Sqrt(groundLevelDistance * groundLevelDistance + Mathf.Pow(_startTransform.Scale.x + _startTransform.Position.y - _endTransform.Scale.x - _startTransform.Position.y, 2));

        if (distance >= MinTransitionHeightApplyDistance) {
            _currentMinTransitionHeight = MinTransitionHeight;
        }
        else {
            _currentMinTransitionHeight = 0;
        }

        if (!AdaptiveTransitionSpeed) {
            _currentTransitionSpeed = TransitionSpeed;
        } else {

            float t = Mathf.Clamp01((distance - MinTransitionDistance) / (MaxTransitionDistance - MinTransitionDistance));
            _currentTransitionSpeed = Mathf.Lerp(MinDistanceTransitionSpeed, MaxDistanceTransitionSpeed, t);
        }

        if (SpeedInMetersPerSecond) {
            _currentTransitionSpeed = distance / _currentTransitionSpeed;
        }

        if (ParabolicTransition) {
            float transitionHeight = Mathf.Max(_startTransform.Scale.x, _endTransform.Scale.x) + ParabolicTransitionHeight;
            if (transitionHeight > _maxZoneScale) {
                transitionHeight = _maxZoneScale;
            }
            if (_startTransform.Scale.x < transitionHeight && _endTransform.Scale.x < transitionHeight) {
                _scaleTransitionCurve = new AnimationCurve();
                _scaleTransitionCurve.AddKey(new Keyframe(0, _startTransform.Scale.x));

                _scaleTransitionCurve.AddKey(new Keyframe(0.5f - CurvePeakWidth / 2, transitionHeight));
                if (CurvePeakWidth > 0) {
                    _scaleTransitionCurve.AddKey(new Keyframe(0.5f + CurvePeakWidth / 2, transitionHeight));
                }
                _scaleTransitionCurve.AddKey(new Keyframe(1, _endTransform.Scale.x));
            } else {
                _scaleTransitionCurve = null;
            }
        } else {
            _scaleTransitionCurve = null;
        }

        /* var middle = NTransform.Lerp(_startTransform, _endTransform, 0.5f);
         Debug.Log("Start transform: " + _startTransform.ToString());
         Debug.Log("End transform: " + _endTransform.ToString());
         Debug.Log("Mid transform: " + middle.ToString());*/

        /*Vector3 a = new Vector3(129, -3, 55);
        Vector3 b = new Vector3(95, 15, 18);
        var m0 = Vector3.Lerp(a, b, 0.5f);
        var m1 = Vector3.Lerp(b, a, 0.5f);

        Debug.Log("m0: " + m0);
        Debug.Log("m1: " + m1);*/

        //_endTransform = new NTransform(zone.transform, zoompan.Zoom);
        

        if (_applyTransform) {
            if (Zoompan != null) {
                Zoompan.ZoomEnabled = false;
                Zoompan.MoveEnabled = false;
            }
        }

        if (ZoneControl != null) {
            ZoneControl.enabled = false;
        }

        if (oldZone == null || oldZone.FastSwitchingFrom || zone.FastSwitchingTo) {
            _transitionTime = _currentTransitionSpeed;
        } else {
            _transitionTime = 0;
        }

            if (oldZone!=null && zone.Group!=null && oldZone.Group == zone.Group && zone.Group.SwitchWithoutTransition) {
                _transitionTime = _currentTransitionSpeed;
                _endTransform = new NTransform(Display.transform);
            }

        if (oldZone != null) {
            oldZone.Hide();
        }
        zone.Show();
        if (OnShowZone != null)
            OnShowZone.Invoke(zone);
        if (Manager != null) {
            Manager.EndShowTag += StartTransition;
            if (!Manager.BeginSwitch(zone.VisibilityTag)) {
                Manager.EndShowTag -= StartTransition;
                StartTransition();
            }
        }
        else {
            StartTransition();
        }        
    }

    private void StartTransition() {
        if (Manager != null) {
            Manager.EndShowTag -= StartTransition;
        }
        TransitionRunning = true;
        OnTransitionBegin();
    }

    void LateUpdate() {
        const float epsilon = 0.001f;
        if (TransitionRunning) {
            if ((_transitionTime + epsilon) >= _currentTransitionSpeed) {
                if (_applyTransform) {
                    _endTransform.Apply(Display.transform);
                    _startTransform = _endTransform;

                    if (Zoompan != null) {
                        Zoompan.ZoomEnabled = true;
                        Zoompan.MoveEnabled = true;
                    }
                }
                if (!_applyTransform && Zoompan != null) {
                    Zoompan.DoZoom(0); //Set correct zoom
                }

                if (ZoneControl != null) {
                    ZoneControl.enabled = true;
                }

                TransitionRunning = false;
                OnTransitionEnd();

                if (ZoneControl != null) {
                    ZoneControl.ViewZone = ActiveZone;
                    if (Zoompan != null) {
                        //lossyScale - we have zones linked to other zones
                        Zoompan.SetStartScale(ActiveZone.transform.lossyScale, true);
                    }
                } else {
                    if (Application.isEditor) {
                        Debug.LogWarning("VisibilityZoneViewer: zoneControl is not assigned");
                    }
                }
            } else {
                if (_applyTransform) {
                    float kLepr = Mathf.Clamp01(SpeedCurve.Evaluate(_transitionTime / _currentTransitionSpeed));
                    NTransform.Lerp(_startTransform, _endTransform, kLepr).Apply(Display.transform);
                    if (_scaleTransitionCurve != null) {
                        float scale = _scaleTransitionCurve.Evaluate(_transitionTime / _currentTransitionSpeed);
                        Display.transform.localScale = new Vector3(scale, scale, scale);
                    } else if (_currentMinTransitionHeight > 0 && _startTransform.Scale.x < _currentMinTransitionHeight) {
                        float speedToMinHeight = ((_currentMinTransitionHeight - _startTransform.Scale.x) / _currentMinTransitionHeight) * TransitionSpeedToMinHeight;
                        float scale = Mathf.LerpUnclamped(_startTransform.Scale.x, _currentMinTransitionHeight, (_transitionTime / speedToMinHeight));
                        if (_transitionTime > speedToMinHeight) {
                            scale = Mathf.Lerp(_currentMinTransitionHeight, _endTransform.Scale.x, (_transitionTime - speedToMinHeight) / (_currentTransitionSpeed - speedToMinHeight));
                        }
                        float passed = Mathf.InverseLerp(0, _currentTransitionSpeed, _transitionTime);
                        Display.transform.localScale = new Vector3(scale, scale, scale) * (1 - passed) + Display.transform.localScale * passed;
                    }
                }
                _transitionTime += Time.deltaTime;
            }
        }
    }
}
}
