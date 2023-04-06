using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Nettle {

public abstract class RAStateUpdater : RemoteActionsUser {
    public const float sendChangesInterval = 0.016f;
    private bool usingSendChangesCoroutine;
    private bool _sendAtEndOfFrame;
    private float _lastSendTime;

    /// <summary>
    /// Repeatedly calls Send Changes method
    /// </summary>
    protected void RepeatSendChanges(bool sendAtEndOfFrame = false) {
        _sendAtEndOfFrame = sendAtEndOfFrame;
        usingSendChangesCoroutine = true;
        if (_sendAtEndOfFrame) {
            StartCoroutine(SendAtEndOfFrameCoroutine());
        } else {
            StartCoroutine(SendChangesCoroutine());
        }
    }

    private IEnumerator SendAtEndOfFrameCoroutine() {
        while (true) {
            yield return new WaitForEndOfFrame();

            if (Time.realtimeSinceStartup - _lastSendTime >= sendChangesInterval) {
                SendChanges();
                _lastSendTime = Time.realtimeSinceStartup;
            }
        }
    }

    private IEnumerator SendChangesCoroutine() {
        while (true) {
            yield return new WaitForSecondsRealtime(sendChangesInterval);
            SendChanges();
        }
    }

    private void OnEnable() {
        if (usingSendChangesCoroutine) {
            RepeatSendChanges(_sendAtEndOfFrame);
        }
    }
    /// <summary>
    /// Override this to check for changes in state of the object and send them to clients. Use RepeatSendChanges to call repeatedly
    /// </summary>
    protected abstract void SendChanges();
}
}
