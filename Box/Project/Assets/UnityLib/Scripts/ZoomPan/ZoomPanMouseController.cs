using System;
using UnityEngine;

namespace Nettle {

    public class ZoomPanMouseController : MonoBehaviour {
        public ZoomPan ZoomPan;
        public bool PanEnable = true;
        //[ConfigField("PanWithLeftMouseButton")]
        public bool ZoomEnable = true;
        public float PanSpeed = 1.0f;

        void Reset() {
            ZoomPan = SceneUtils.FindObjectIfSingle<ZoomPan>();
        }

        void Update() {
            if (ZoomPan != null && ZoomPan.enabled) {
                if (PanEnable) {
                    ZoomPan.Move(Input.GetAxis("Mouse X") * PanSpeed, Input.GetAxis("Mouse Y") * PanSpeed);
                }

                if (ZoomEnable) {
                    float scrollValue = Input.GetAxis("Mouse ScrollWheel");

                    if (Math.Abs(scrollValue) > 0.001f) {
                        ZoomPan.DoZoom(scrollValue);
                    }
                }
            }
        }

        public void SetPanEnable(bool enabled) {
            PanEnable = enabled;
        }

        public void SetZoomEnable(bool enabled) {
            ZoomEnable = enabled;
        }
    }
}
