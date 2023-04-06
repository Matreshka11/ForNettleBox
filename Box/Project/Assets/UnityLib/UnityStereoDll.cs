using UnityEngine;
using System.Collections;
using System.Runtime.InteropServices;
using System;

namespace Nettle {

public static class UnityStereoDll  {
    [DllImport("UnityStereo")]
    public static extern void Dummy();
    [DllImport("UnityStereo")]
    public static extern IntPtr GetRenderEventFunc();
    
    public enum GraphicsEvent : int {
        SetLeftEye = 0,
        SetRightEye = 1
    };
}


}
