using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ScreenResolution : MonoBehaviour
{
    [SerializeField]
    private int _width = 1920;
    [SerializeField]
    private int _height = 2205;
    [SerializeField]
    private FullScreenMode _fullScreenMode = FullScreenMode.ExclusiveFullScreen;

    private void Start()
    {
        SetResolution();
    }

    private void OnApplicationFocus(bool focused)
    {
        if (focused)
        {
            SetResolution();
        }
    }

    private void SetResolution()
    {

        Screen.SetResolution(_width, _height, _fullScreenMode);
    }
}
