using UnityEngine;
using System.Collections;

namespace Nettle {

public sealed class MotionParallax3D2 : MonoBehaviour {
    public MotionParallaxDisplay Display;
    public StereoEyes eyes;
    public bool UseStereoProjection = true;
    public float FOVScale = 2.0f;

    private Camera _camera;

    private void Reset() {
        if (Display == null) {
            Display = SceneUtils.FindObjectIfSingle<MotionParallaxDisplay>();
        }
    }

    private void Awake() {
        _camera = GetComponent<Camera>();
    }

    private float CalculateCameraNoseFov() {
        float maxY = 0;
        var currentScreenAspect = (float)Screen.width / (float)Screen.height;
        var worldScreenCorners = Display.GetWorldScreenCorners();
        foreach (var corner in worldScreenCorners) {
            var cameraLocalCorner = transform.InverseTransformPoint(corner);
            cameraLocalCorner /= cameraLocalCorner.z;
            maxY = Mathf.Max(maxY, Mathf.Abs(cameraLocalCorner.y));
            maxY = Mathf.Max(maxY, Mathf.Abs(cameraLocalCorner.x * currentScreenAspect));
        }
        return Mathf.Atan(maxY / currentScreenAspect) * Mathf.Rad2Deg * 2;
    }

    public void LateUpdateManual() {
        var rotation = new Quaternion();
        rotation.SetLookRotation(Vector3.down);
        transform.rotation = Display.transform.rotation * rotation;

        if (UseStereoProjection) {
            UpdateProjection();
        } else {
            transform.LookAt(Display.transform.position, Display.transform.up);
            var fov = CalculateCameraNoseFov();
            _camera.fieldOfView = fov / FOVScale;
            _camera.ResetProjectionMatrix();
            _camera.ResetAspect();
        }
    }

    private void UpdateProjection() {
        NettleProjectionMatrix(_camera, Display.transform.InverseTransformPoint(_camera.transform.position));
    }

    private void NettleProjectionMatrix(Camera C, Vector3 Eye) {
        /*Vector3 WorldScaleEye = Eye * screenWidth * 0.5f;
        C.gameObject.transform.localPosition = WorldScaleEye;
        Quaternion Rotation = new Quaternion();
        Rotation.SetLookRotation(Vector3.down);

        C.gameObject.transform.localRotation = Rotation;

        float FC = farClip + WorldScaleEye.y * transform.localScale.y;
        float NC = Mathf.Max(relNearClip * WorldScaleEye.y * transform.localScale.y, minNearClip);*/

        var eyesScale = Display == null ? 1.0f : Display.Width / 2.0f;
        Eye /= eyesScale;

        var fc = C.farClipPlane; // 1000.0f;
        var nc = C.nearClipPlane; // 0.03f;

        var shift = new Vector2(0.5f * Eye.x, 0.5f * Eye.z);

        var M = 1f / Eye.y * (2f * nc);

        var eyeRtSize = eyes.GetEyeRTSize();
        var aspect = eyeRtSize.y/eyeRtSize.x; //(float)Screen.height / (float)Screen.width;

        var right = (-shift.x + 0.5f) * M;
        var left = (-shift.x - 0.5f) * M;
        var top = (-shift.y + aspect * 0.5f) * M;
        var bottom = (-shift.y - aspect * 0.5f) * M;

        C.aspect = aspect; //width/height;
        var fov = (2 * Mathf.Atan(1f / Mathf.Max(Eye.y, 0.01f))) * Mathf.Rad2Deg; // 57.2957795f;
        C.fieldOfView = fov;
        C.nearClipPlane = nc;
        C.farClipPlane = fc;

        var m = PerspectiveOffCenter(left, right, bottom, top, nc, fc);
        C.projectionMatrix = m;

    }

    private static Matrix4x4 PerspectiveOffCenter(float left, float right, float bottom, float top, float near,
        float far) {
        var x = 2.0F * near / (right - left);
        var y = 2.0F * near / (top - bottom);
        var a = (right + left) / (right - left);
        var b = (top + bottom) / (top - bottom);
        var c = -(far + near) / (far - near);
        var d = -(2.0F * far * near) / (far - near);
        var e = -1.0F;
        var m = new Matrix4x4();
        m[0, 0] = x;
        m[0, 1] = 0;
        m[0, 2] = a;
        m[0, 3] = 0;
        m[1, 0] = 0;
        m[1, 1] = y;
        m[1, 2] = b;
        m[1, 3] = 0;
        m[2, 0] = 0;
        m[2, 1] = 0;
        m[2, 2] = c;
        m[2, 3] = d;
        m[3, 0] = 0;
        m[3, 1] = 0;
        m[3, 2] = e;
        m[3, 3] = 0;
        return m;
    }
}
}
