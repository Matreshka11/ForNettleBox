using UnityEngine;
using System;
using System.Collections;
#if UNITY_EDITOR
using UnityEditor;
#endif
namespace Nettle {


public enum EyesCameraMode {
    Stereo,
    Mono,
    Left,
    Right
}

public enum StereoType {
    NVidia3DVision,
    FramePacking
}
    
public sealed class StereoEyes : MonoBehaviour {
    public NettleBoxTracking NettleBoxTracking;
    public MotionParallax3D2 MotionParallax3D2;
    public bool RenderTwoEyesPerFrame = true;
    public Action BeforeRenderEvent;
    public StereoType stereoType;
    private const uint framePackingVsyncSize = 45;
    private IntPtr _renderEventHandler;
    public float Separation = 0.065f;
    public Camera EyeCamera;
    private bool _leftEyeActive = true;
    public bool FlipEyes = false;
    public EyesCameraMode CameraMode;

    public bool EditorFullFrameMode = true;

    public bool EditorStereo;

    public bool LeftEyeActive {
        get {
            if (CameraMode == EyesCameraMode.Left) {
                return true;
            } else if (CameraMode == EyesCameraMode.Right) {
                return false;
            } else {
                return _leftEyeActive;
            }
        }
    }

    public Vector3 LeftEyeLocal {
        get { return Vector3.right * Separation * -0.5f; }
    }

    public Vector3 RightEyeLocal {
        get { return Vector3.right * Separation * 0.5f; }
    }

    public Vector3 LeftEyeWorld {
        get { return transform.TransformPoint(LeftEyeLocal); }
    }

    public Vector3 RightEyeWorld {
        get { return transform.TransformPoint(RightEyeLocal); }
    }

    private bool _cameraModeGUI = false;

    private void OnValidate() {
        EditorInit();
        UpdateCameraTransform();
    }

    void Reset() {
        EditorInit();
    }

    void EditorInit() {
        if (!NettleBoxTracking) {
            NettleBoxTracking = FindObjectOfType<NettleBoxTracking>();
        }
        if (!MotionParallax3D2) {
            MotionParallax3D2 = FindObjectOfType<MotionParallax3D2>();
        }
    }

    private void Start() {
            if (EyeCamera == null) {
                EyeCamera = GetComponentInChildren<Camera>();
            }
        Application.onBeforeRender += OnBeforeRender;
#if !UNITY_EDITOR
        if (stereoType == StereoType.NVidia3DVision) {
            StartCoroutine(DelayedSetRenderEventHandler());
        }
#else
        if (!EditorStereo) {
            CameraMode = EyesCameraMode.Mono;
            RenderTwoEyesPerFrame = false;
        }
#endif
    }

    private IEnumerator DelayedSetRenderEventHandler() {
        yield return null;
        SetRenderEventHandler();
    }

    private void SetRenderEventHandler() {
        _renderEventHandler = UnityStereoDll.GetRenderEventFunc();
    }

    void OnDestroy() {
        Application.onBeforeRender -= OnBeforeRender;
    }

    public void ToggleTwoEyesPerFrame() {
        RenderTwoEyesPerFrame = !RenderTwoEyesPerFrame;
    }

    private void OnBeforeRender() {
        if (RenderTwoEyesPerFrame) {
            PrepareCameraRender();
            if (BeforeRenderEvent != null) {
                BeforeRenderEvent.Invoke();
            }
            EyeCamera.Render();
        }
        PrepareCameraRender();
        if (BeforeRenderEvent != null) {
            BeforeRenderEvent.Invoke();
        }
    }

    private void PrepareCameraRender() {
        Toggle3DVisionEye();
        if (NettleBoxTracking != null) {
            NettleBoxTracking.UpdateTracking();
        }
        UpdateCameraTransform();
        if (MotionParallax3D2 != null) {
            MotionParallax3D2.LateUpdateManual();
        }
    }

        private void UpdateCameraTransform() {
            if (EyeCamera != null) {



                switch (CameraMode) {
                    case EyesCameraMode.Stereo:
                        EyeCamera.transform.position = _leftEyeActive ? LeftEyeWorld : RightEyeWorld;
                        break;
                    case EyesCameraMode.Mono:
                        EyeCamera.transform.position = transform.position;
                        break;
                    case EyesCameraMode.Left:
                        EyeCamera.transform.position = LeftEyeWorld;
                        break;
                    case EyesCameraMode.Right:
                        EyeCamera.transform.position = RightEyeWorld;
                        break;
                }

                EyeCamera.transform.rotation = transform.rotation;
            }
        }

    public Vector2 GetEyeRTSize() {
#if !UNITY_EDITOR
        EditorFullFrameMode = false;
#endif

        if (stereoType == StereoType.NVidia3DVision || EditorFullFrameMode) {
            return new Vector2(Screen.width, Screen.height);
        } else if (stereoType == StereoType.FramePacking) {
            var vsync = Screen.width == 1280 ? 30 : framePackingVsyncSize;
            int eyeTargetHeight = (int)(Screen.height - vsync) / 2;
            return new Vector2(Screen.width, eyeTargetHeight);
        } else {
            throw new Exception("Unknown stereo mode");
        }
    }

    public Rect GetCameraViewportRect() {
        return GetCameraViewportRect(_leftEyeActive);
    }

    public Rect GetCameraViewportRect(bool leftEye) {
        if (stereoType == StereoType.NVidia3DVision) {
            return new Rect(0, 0, 1, 1);
        } else {
            float normalizedEyeHeight = GetEyeRTSize().y / (float)Screen.height;
            return new Rect(0, (leftEye || FlipEyes) ? (1.0f - normalizedEyeHeight) : 0.0f, 1.0f, normalizedEyeHeight);
        }
    }

    public void Toggle3DVisionEye() {
        try {
            if (_renderEventHandler == IntPtr.Zero) {
                Debug.Log("Render event handler was null");
                SetRenderEventHandler();
            }
            _leftEyeActive = !_leftEyeActive;
            if (stereoType == StereoType.NVidia3DVision) {
#if !UNITY_EDITOR
            if(!FlipEyes){
                GL.IssuePluginEvent(_renderEventHandler, _leftEyeActive ? (int)UnityStereoDll.GraphicsEvent.SetLeftEye : (int)UnityStereoDll.GraphicsEvent.SetRightEye);
            }else{
                GL.IssuePluginEvent(_renderEventHandler, _leftEyeActive ? (int)UnityStereoDll.GraphicsEvent.SetRightEye : (int)UnityStereoDll.GraphicsEvent.SetLeftEye);
            }
#endif
            }
            EyeCamera.rect = GetCameraViewportRect();
        }
        catch (Exception ex){
            Debug.LogError(ex.Message + "\n" + ex.StackTrace);
        }
    }

    private void OnDrawGizmos() {
        Gizmos.matrix = transform.localToWorldMatrix;
        Gizmos.DrawLine(LeftEyeLocal, RightEyeLocal);
        Gizmos.DrawLine(Vector3.zero, Vector3.forward * Separation * 0.2f);
#if UNITY_EDITOR
        Handles.Label(LeftEyeWorld, "L");
        Handles.Label(RightEyeWorld, "R");
#endif
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(LeftEyeLocal, 0.01f);
        Gizmos.color = Color.green;
        Gizmos.DrawWireSphere(RightEyeLocal, 0.01f);
    }

    private void Update() {
        if (Input.GetKey(KeyCode.LeftAlt) && Input.GetKey(KeyCode.LeftControl) && Input.GetKeyUp(KeyCode.C)) {
            _cameraModeGUI = !_cameraModeGUI;
        }
    }

        public void SetMonoMode(bool mono) {
            CameraMode = mono? EyesCameraMode.Mono : EyesCameraMode.Stereo; 
        }

    private void OnGUI() {
        if (!_cameraModeGUI) { return; }

        var btnWidth = 50.0f;
        var btnHeigth = 20.0f;

        GUI.Label(new Rect(25, 25, 200, 25), "Select screen mode:");

        if (GUI.Button(new Rect(25, 50, btnWidth, btnHeigth), "Stereo")) {
            CameraMode = EyesCameraMode.Stereo;
            _cameraModeGUI = false;
        }
        if (GUI.Button(new Rect(25, 75, btnWidth, btnHeigth), "Mono")) {
            CameraMode = EyesCameraMode.Mono;
            _cameraModeGUI = false;
        }
        if (GUI.Button(new Rect(25, 100, btnWidth, btnHeigth), "Left")) {
            CameraMode = EyesCameraMode.Left;
            _cameraModeGUI = false;
        }
        if (GUI.Button(new Rect(25, 125, btnWidth, btnHeigth), "Right")) {
            CameraMode = EyesCameraMode.Right;
            _cameraModeGUI = false;
        }
    }
}
}
