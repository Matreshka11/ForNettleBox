using System;
using UnityEngine;
using System.Linq;
using UnityEngine.Events;
using UnityEngine.Serialization;

namespace Nettle {

    public class VisibilityZone : MonoBehaviour {
        public VisibilityZoneGroup Group;
        public string VisibilityTag;
        public bool FastSwitchingTo;
        public bool FastSwitchingFrom;

        public VisibilityZoneTransitionRules[] TransitionRules;

        [Space(20)]
        public bool DisableZoompan;
        public float MinZoom = 0.5f, MaxZoom = 2.0f;
        [FormerlySerializedAs("PanInside")]
        public bool ZoomPanInside = false;
        [Space(20)]
        public bool ShowGizmo = true;
        public bool ShowGizmoIfNotSelected;
        public bool ShowZoomGizmo = true;
        public enum Aspect { Aspect_Free, Aspect_16x9, Aspect_16x10, Aspect_4x3, Aspect_1x1 };
        public Aspect ScreenAspect = Aspect.Aspect_16x9;
        public float[] HorizontalLines = new float[0];
        public float[] VerticalLines = new float[0];

        public UnityEvent OnShowed = new UnityEvent();
        public UnityEvent OnHided = new UnityEvent();

        [SerializeField]
        private bool _isGeneralPlan = false;

        public bool IsStaticMP3D {
            get { return IsStatic; }
            set {
                IsStatic = value;
                if (OnChangeStaticState != null)
                    OnChangeStaticState.Invoke(value);
            }
        }
        public bool IsStatic = false;

        public event Action<bool> OnChangeStaticState;

        public Vector3 StaticLeftEye;
        public Vector3 StaticRightEye;


        public VisibilityZoneTransition GetTransitionSettings(VisibilityZone target) {
            if (TransitionRules != null) {
                return (from rule in TransitionRules where rule.Match(target) select rule.transitionSettings).FirstOrDefault();
            }
            return null;
        }

        public void Show() {
            if (OnShowed != null)
                OnShowed.Invoke();
        }

        public void Hide() {
            if (OnHided != null)
                OnHided.Invoke();
        }

        void OnValidate() {
            EditorInit();
        }

        void Reset() {
            EditorInit();
        }

        void EditorInit() {

            MaxZoom = MaxZoom < 1f ? 1f : MaxZoom;
            MinZoom = Mathf.Clamp01(MinZoom);
        }

        public virtual Transform GetTransform() {
            return transform;
        }

        public float GetAspect() {
            if (ScreenAspect == Aspect.Aspect_Free) {
                return (float)Screen.width / (float)Screen.height;
            } else if (ScreenAspect == Aspect.Aspect_16x9) {
                return 16.0f / 9.0f;
            } else if (ScreenAspect == Aspect.Aspect_16x10) {
                return 16.0f / 10.0f;
            } else if (ScreenAspect == Aspect.Aspect_4x3) {
                return 4.0f / 3.0f;
            }
            return 1.0f;
        }

        void DrawRect(Vector3[] points, Color color) {
            Gizmos.color = color;
            for (int i = 0; i < 4; ++i) {
                Gizmos.DrawLine(points[i], points[(i + 1) % 4]);
            }
        }

        public static Vector3[] GetLocalSpaceRect(float aspect, float zoom = 1.0f) {
            return new[]{
            new Vector3(-zoom, 0f, zoom / aspect),
            new Vector3(zoom, 0f, zoom / aspect),
            new Vector3(zoom, 0f, -zoom / aspect),
            new Vector3(-zoom, 0f, -zoom / aspect)
            };
        }

        public Vector3[] LocalToWorldSpacePoints(Vector3[] points) {
            return points.Select(v => GetTransform().localToWorldMatrix.MultiplyPoint3x4(v)).ToArray();
        }

        public Vector3[] GetMaxViewRectWorldSpace() {
            return LocalToWorldSpacePoints(GetLocalSpaceRect(GetAspect(), MaxZoom));
        }

        public NTransform GetNTransform() {
            return new NTransform(GetTransform());
        }

        void DrawGizmos() {
            if (!ShowGizmo) return;
            var oldMatrix = Gizmos.matrix;
            Gizmos.matrix = GetTransform().localToWorldMatrix;

            DrawRect(GetLocalSpaceRect(GetAspect(), 1.0f), Color.blue);

            if (ShowZoomGizmo) {
                if (MinZoom != 0f) {
                    DrawRect(GetLocalSpaceRect(GetAspect(), MinZoom), Color.red);
                }

                if (MaxZoom != 0f) {
                    DrawRect(GetLocalSpaceRect(GetAspect(), MaxZoom), Color.yellow);
                }
            }
            if (HorizontalLines.Length > 0) {
                foreach (var horizontalLine in HorizontalLines) {
                    Gizmos.DrawLine(new Vector3(-1.0f, 0.0f, horizontalLine), new Vector3(1.0f, 0.0f, horizontalLine));
                }
            }
            if (VerticalLines.Length > 0) {
                foreach (var verticalLine in VerticalLines) {
                    Gizmos.DrawLine(new Vector3(verticalLine, 0.0f, -1.0f / GetAspect()),
                        new Vector3(verticalLine, 0.0f, 1.0f / GetAspect()));
                }
            }

            Gizmos.matrix = oldMatrix;
        }

        void OnDrawGizmos() {
            if (ShowGizmoIfNotSelected) {
                DrawGizmos();
            }
        }

        void OnDrawGizmosSelected() {
            DrawGizmos();
        }


    }
}
