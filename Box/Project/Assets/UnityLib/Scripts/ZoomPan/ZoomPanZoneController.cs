using UnityEngine;
using System.Collections.Generic;
using System.Linq;

namespace Nettle {
    public class ZoomPanZoneController : MonoBehaviour {
        public ZoomPan ZoomPan;
        public MotionParallaxDisplay Display;

        public bool UseGlobalBounds = false;
        public VisibilityZone GlobalBoundsSource;

        public bool OnCorrectViewZone = true;
        public VisibilityZone ViewZone;

        void Reset() {
            ZoomPan = SceneUtils.FindObjectIfSingle<ZoomPan>();
        }

        private void Start() {
            if (UseGlobalBounds && !GlobalBoundsSource) {
                Debug.LogWarning("GlobalBoundsSource is not set!");
            }
        }

        void LateUpdate() {
            CorrectView();
        }

        public static void GetAABB(Vector3[] src, out Vector3 bMin, out Vector3 bMax) {
            bMin = src[0];
            bMax = src[0];
            for (var i = 1; i < src.Length; ++i) {
                bMin = Vector3.Min(bMin, src[i]);
                bMax = Vector3.Max(bMax, src[i]);
            }
        }

        void Correct(VisibilityZone zone) {
            if (zone == null) {
                return;
            }

            var displayLocalCorners = Display.GetWorldScreenCorners().Select(v => zone.transform.InverseTransformPoint(v));
            var zoneLocalCorners = VisibilityZone.GetLocalSpaceRect(zone.GetAspect(), zone.MaxZoom);

            Vector3 panBoundsMin;
            Vector3 panBoundsMax;
            GetAABB(zoneLocalCorners, out panBoundsMin, out panBoundsMax);

            Vector3 displayBoundsMin;
            Vector3 displayBoundsMax;
            GetAABB(displayLocalCorners.ToArray(), out displayBoundsMin, out displayBoundsMax);
            var displaySize = displayBoundsMax - displayBoundsMin;


            var viewZoneSize = panBoundsMax - panBoundsMin;

            if ((displaySize.x > (viewZoneSize.x + 0.0001f)) || (displaySize.z > (viewZoneSize.z + 0.0001f))) {
#if UNITY_EDITOR
                Debug.LogWarningFormat("ZoomPan: Mp3d is bigger than desired view zone!::displaySize x={0}, z={1}::viewZoneSize x={2}, z={3}", displaySize.x, displaySize.z, viewZoneSize.x, viewZoneSize.z);
#endif
            } else {
                var dirRight = Vector3.right;
                var dirForward = Vector3.forward;
                var correctionDelta = new Vector3();
                if (displayBoundsMax.z > panBoundsMax.z) {
                    correctionDelta += -dirForward * (displayBoundsMax.z - panBoundsMax.z);
                }
                if (displayBoundsMax.x > panBoundsMax.x) {
                    correctionDelta += -dirRight * (displayBoundsMax.x - panBoundsMax.x);
                }
                if (displayBoundsMin.z < panBoundsMin.z) {
                    correctionDelta += dirForward * (panBoundsMin.z - displayBoundsMin.z);
                }
                if (displayBoundsMin.x < panBoundsMin.x) {
                    correctionDelta += dirRight * (panBoundsMin.x - displayBoundsMin.x);
                }

                var worldOffset = zone.transform.TransformVector(correctionDelta);
                Display.transform.position += worldOffset;
            }
        }

        void CorrectView() {
            if (!enabled || !ZoomPan) {
                return;
            }

            if (ZoomPan.Target == null || ViewZone == null || !ZoomPan.enabled) {
                return;
            }

            ZoomPan.enabled = !ViewZone.DisableZoompan;

            ZoomPan.MinZoom = ViewZone.MinZoom;

            if (ViewZone.ZoomPanInside) {
                ZoomPan.MaxZoom = ViewZone.MaxZoom;
                Correct(ViewZone);
            } else if (UseGlobalBounds && GlobalBoundsSource != null) {
                ZoomPan.MaxZoom = GlobalBoundsSource.MaxZoom * GlobalBoundsSource.transform.lossyScale.x / ViewZone.transform.lossyScale.x;
                Correct(GlobalBoundsSource);
            }
        }
    }
}
