using UnityEngine;
using System.Collections;
#if UNITY_EDITOR
using UnityEditor;
#endif
namespace Nettle {
public class FullHDStereoFix : MonoBehaviour
{
    public StereoEyes eyes;
    private Texture2D leftEyeMarker;
    private Texture2D rightEyeMarker;

    void Start () {
        if (eyes.stereoType == StereoType.NVidia3DVision)
        {
            leftEyeMarker = CreateMarkerTexture(new Color32(0x00, 0x00, 0x00, 0xff));
            rightEyeMarker = CreateMarkerTexture(new Color32(0xff, 0xff, 0xff, 0xff));

            StartCoroutine(DrawMarkerCoroutine());
        }
    
    }

    Texture2D CreateMarkerTexture(Color32 color)
    {
        var result = new Texture2D(1, 1, TextureFormat.ARGB32, false);

        var pixels = new Color32[result.width * result.height];

        for (int i = 0; i < pixels.Length; ++i)
        {
            pixels[i] = color;
        }

        result.SetPixels32(pixels);
        result.Apply(false);

        return result;
    }

#if UNITY_EDITOR
    int GetExecutionOrder(MonoBehaviour behaviour)
    {
        MonoScript behaviourScript = MonoScript.FromMonoBehaviour(behaviour);
        return MonoImporter.GetExecutionOrder(behaviourScript);
    }
#endif

    void DrawMarker(Texture2D tex)
    {
        GL.PushMatrix();
        GL.LoadPixelMatrix(0, Screen.width, Screen.height, 0);

        Graphics.DrawTexture(new Rect(0, 0, tex.width, tex.height), tex);
        GL.PopMatrix();
    }

    IEnumerator DrawMarkerCoroutine() {
        while (true) {
            yield return new WaitForEndOfFrame();
            DrawMarker(eyes.LeftEyeActive ? leftEyeMarker : rightEyeMarker);
        }
    }


}
}
