using UnityEngine;
using System.Collections;
using JetBrains.Annotations;

public static class TransformEx {
    public static Transform[] GetChildren(this Transform t) {
        var children = new Transform[t.childCount];
        for (int i = 0; i < t.childCount; ++i) {
            children[i] = t.GetChild(i);
        }
        return children;
    }

    public static void Reset(this Transform t) {
        t.localRotation = Quaternion.identity;
        t.localPosition = Vector3.zero;
        t.localScale = Vector3.one;
    }

    public static void Copy(this RectTransform t, RectTransform target) {
        target.pivot = t.pivot;
        target.anchorMin = t.anchorMin;
        target.anchorMax = t.anchorMax;
        target.anchoredPosition = t.anchoredPosition;
        target.offsetMin = t.offsetMin;
        target.offsetMax = t.offsetMax;
        target.sizeDelta = t.sizeDelta;
    }
}