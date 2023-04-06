using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif
namespace Nettle {

    public sealed class MotionParallaxDisplay : MonoBehaviour {

        public float Width = 2.0f;
#if UNITY_EDITOR
        public AspectRatio editorAspect = AspectRatio.Aspect16by9;
#endif
        public static Vector3[] GetLocalScreenCorners(float virtualScreenWidth, float screenAspect) {
            var halfWidth = virtualScreenWidth * 0.5f;
            var halfHeigth = halfWidth * screenAspect;
            return new[]
            {
            new Vector3(-halfWidth, 0, halfHeigth),
            new Vector3(halfWidth, 0, halfHeigth),
            new Vector3(halfWidth, 0, -halfHeigth),
            new Vector3(-halfWidth, 0, -halfHeigth)
        };
        }

        private float GetAspect() {
#if UNITY_EDITOR
            switch (editorAspect) {
                case AspectRatio.Aspect5by4:
                    return 4.0f / 5.0f;
                case AspectRatio.Aspect16by10:
                    return 10.0f / 16.0f;
                case AspectRatio.Aspect16by9:
                    return 9.0f / 16.0f;
                case AspectRatio.Aspect4by3:
                    return 3.0f / 4.0f;
            }
#endif
            //TODO: Fix it!
            return 9.0f / 16.0f;
            return (float)Screen.height / (float)Screen.width;
        }

        public Vector3[] GetLocalScreenCorners() {
            return GetLocalScreenCorners(Width, GetAspect());
        }

        public Vector3[] GetWorldScreenCorners() {
            var corners = GetLocalScreenCorners();
            for (var i = 0; i < corners.Length; ++i) {
                corners[i] = transform.TransformPoint(corners[i]);
            }
            return corners;
        }

        public float PixelsPerUnit() {
            return (float)Screen.width / (Width * transform.localScale.x);
        }

        private void OnDrawGizmos() {
#if UNITY_EDITOR
            var screenAspect = GetAspect();

            var sideLineLength = Width * 0.1f;
            var sideLineOffset = Vector3.forward * sideLineLength;

            var oldMatrix = Handles.matrix;
            Handles.matrix = transform.localToWorldMatrix;
            var rightOffset = Vector3.right * Width * 0.5f;

            //Real gizmo
            var coordsysLen = Width * 0.1f;
            Handles.color = Color.blue;
            //horizontal line
            Handles.DrawLine(-rightOffset, rightOffset);
            //side lines
            Handles.DrawLine(-rightOffset + sideLineOffset, -rightOffset - sideLineOffset);
            Handles.DrawLine(rightOffset + sideLineOffset, rightOffset - sideLineOffset);
            //coordsys
            Handles.DrawLine(Vector3.zero, Vector3.up * coordsysLen);
            Handles.DrawLine(Vector3.zero, Vector3.forward * coordsysLen);

            //Abstract gizmo
            var lineSpacing = 4.0f;
            var screenTopOffset = Vector3.forward * Width * 0.5f * screenAspect;

            Handles.color = new Color(0, 0, 1, 0.65f);
            //Left size
            Handles.DrawDottedLine(-rightOffset + sideLineOffset, -rightOffset + screenTopOffset, lineSpacing);
            Handles.DrawDottedLine(-rightOffset - sideLineOffset, -rightOffset - screenTopOffset, lineSpacing);
            //Right side
            Handles.DrawDottedLine(rightOffset + sideLineOffset, rightOffset + screenTopOffset, lineSpacing);
            Handles.DrawDottedLine(rightOffset - sideLineOffset, rightOffset - screenTopOffset, lineSpacing);
            //Top + Bottom
            Handles.DrawDottedLine(-rightOffset + screenTopOffset, rightOffset + screenTopOffset, lineSpacing);
            Handles.DrawDottedLine(-rightOffset - screenTopOffset, rightOffset - screenTopOffset, lineSpacing);

            Handles.matrix = oldMatrix;

            var corners = GetWorldScreenCorners();

            Handles.color = Color.red;
            Handles.DrawWireDisc(corners[0], transform.up, 0.1f);
            Handles.color = Color.green;
            Handles.DrawWireDisc(corners[1], transform.up, 0.1f);
            Handles.color = Color.yellow;
            Handles.DrawWireDisc(corners[2], transform.up, 0.1f);
            Handles.color = Color.blue;
            Handles.DrawWireDisc(corners[3], transform.up, 0.1f);
#endif
        }
    }
}
