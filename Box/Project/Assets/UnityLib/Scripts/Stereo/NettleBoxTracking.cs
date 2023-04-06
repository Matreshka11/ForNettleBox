using UnityEngine;
using System.Runtime.InteropServices;
using System;
using UnityEngine.SceneManagement;

namespace Nettle {

    public sealed class NettleBoxTracking : MonoBehaviour {
        public float ScreenDelay = 0.035f;
        [Tooltip("If this value is above 0, the camera will rotate at a fixed distance from the target (might be useful for remote display)")]
        public float FixedDistance = 0;
        public float FixedAngle = 0;
        public Vector3 LeftEye = Vector3.up - Vector3.right * 0.065f * 0.5f;
        public Vector3 RightEye = Vector3.up + Vector3.right * 0.065f * 0.5f;
        public Vector3 DefaultEyesCenter = new Vector3(0, 0.5f, -0.5f);
        public bool ManualMode = false;
        
        public float TimeBeforeResetEyes = 120f;

        [HideInInspector]
        public bool DebugMode = false;

        public StereoEyes Eyes;
        public MotionParallaxDisplay Display;

        [Header("TrackingDll")]
        public bool UseTrackingDll = false;

        [HideInInspector]
        public bool Active;

        private float lastGetEyeTime;
        private Vector3 _lastPosition;

        #region Tracking Dll

        [DllImport("Tracking", CallingConvention = CallingConvention.StdCall)]
        private static extern bool GetEyes(float timeShift, out Vector3 leftEye, out Vector3 rigthEye);

        [DllImport("Tracking", CallingConvention = CallingConvention.StdCall)]
        private static extern void TrackingCreate();

        [DllImport("Tracking", CallingConvention = CallingConvention.StdCall)]
        private static extern void TrackingDestroy();

        [DllImport("Tracking", CallingConvention = CallingConvention.StdCall)]
        private static extern bool IsTrackingCreated();

        #endregion

        private void Reset() {
            if (Eyes == null) {
                Eyes = SceneUtils.FindObjectIfSingle<StereoEyes>();
            }
            if (Display == null) {
                Display = SceneUtils.FindObjectIfSingle<MotionParallaxDisplay>();
            }
            ResetEyes();
        }

        public void ResetEyes() {
            var eyesScale = Display == null ? 1.0f : Display.Width / 2;
            Vector3 eyesCenter = Vector3.up;
            if (!Application.isEditor) {
                eyesCenter = DefaultEyesCenter;
            }
            LeftEye = (eyesCenter - Vector3.right * 0.065f * 0.5f) * eyesScale;
            RightEye = (eyesCenter + Vector3.right * 0.065f * 0.5f) * eyesScale;
        }


        private void Awake() {
            if (!ManualMode) {
                ResetEyes();
            }

            try {
                if (!Application.isEditor || (Application.isEditor && UseTrackingDll)) {
                    Debug.Log("Tracking created. Scene: " + SceneManager.GetActiveScene().name);
                    TrackingCreate();
                    ManualMode = false;
                }
                else {
                    ManualMode = true;
                }
            }
            catch (DllNotFoundException ex) {
                ManualMode = true;

                Debug.Log(ex.Message);
            }
        }

        private void OnDestroy() {
            try {
                if (!Application.isEditor || (Application.isEditor && UseTrackingDll)) {
                    Debug.Log("Tracking destroyed. Scene: " + SceneManager.GetActiveScene().name);
                    TrackingDestroy();
                }
            }
            catch (DllNotFoundException ex) {
                Debug.Log(ex.Message);
            }
        }

        private string message;
        private void OnGUI() {
            if (DebugMode) {
                GUILayout.Label(string.Format("Left: {0}; Right: {1}", LeftEye, RightEye));
                GUILayout.Label(message);

                if (Input.GetKeyDown(KeyCode.F7)) {
                    TrackingCreate();
                }

                if (Input.GetKeyDown(KeyCode.F8)) {
                    TrackingDestroy();
                }

                GUILayout.Label("Tracking created: " + IsTrackingCreated().ToString());
                GUILayout.Label("F7 = TrackingCreate\tF8 = TrackingDestroy");
            }
        }

        public void UpdateTracking() {
            UpdateEyes();
            UpdateTransform();
        }

        private void UpdateEyes() {
            if (ManualMode) {
                return;
            }
            float eyesScale = Display.Width / 2;
            try {
                if (!Application.isEditor || (Application.isEditor && UseTrackingDll)) {
                    Vector3 left;
                    Vector3 right;
                    if (GetEyes(ScreenDelay, out left, out right)) {
                        Active = true;

                        LeftEye = new Vector3(left.x, left.z, left.y);
                        RightEye = new Vector3(right.x, right.z, right.y);

                        if (Display != null) {
                            LeftEye *= eyesScale;
                            RightEye *= eyesScale;
                        }

                        Eyes.Separation = Vector3.Distance(LeftEye, RightEye);
                        lastGetEyeTime = Time.time;
                    }
                    else {
                        Active = false;
                        message = "Don't get eyes";
                        if (Eyes.CameraMode == EyesCameraMode.Mono && _lastPosition != transform.localPosition) {
                            _lastPosition = transform.localPosition;
                            lastGetEyeTime = Time.time;
                        }
                        if (lastGetEyeTime + TimeBeforeResetEyes < Time.time) {
                            ResetEyes();
                            lastGetEyeTime = Time.time;
                            _lastPosition = transform.localPosition;
                        }
                    }
                }
                else {
                    ResetEyes();
                }
            }
            catch (DllNotFoundException ex) {
                ManualMode = true;
                ResetEyes();

                Debug.Log(ex.Message);
            }
        }

        private void OnValidate() {
            UpdateTransform();
        }

        private void UpdateTransform() {
            var nose = (LeftEye + RightEye) * 0.5f;
            if (FixedDistance > 0) {
                nose = nose.normalized * FixedDistance;
            }
            if (FixedAngle > 0) {
                Vector3 proj = Vector3.ProjectOnPlane(nose, Vector3.up).normalized;
                float yAngle = Vector3.Angle(Vector3.forward, proj);
                if (proj.x < 0) {
                    yAngle = -yAngle;
                }
                Vector3 noseRotated = Quaternion.Euler(-Mathf.Abs(FixedAngle), yAngle, 0) *Vector3.forward;
                nose = nose.magnitude * noseRotated;
            }
            var separation = Vector3.Distance(LeftEye, RightEye);
            transform.localPosition = nose;
            if (separation > 0.0001f) {
                var forward = (-nose).normalized;
                var right = (RightEye - LeftEye).normalized;
                var localUp = Vector3.Cross(forward, right).normalized;
                forward = Vector3.Cross(right, localUp).normalized;
                transform.localRotation = Quaternion.LookRotation(forward, localUp);
            }
        }
    }
}
