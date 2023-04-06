using System;
using System.Collections;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.Events;

namespace Nettle {

[Serializable]
public class RANettleBoxTrackingClientEvent : UnityEvent<bool> { }
[Serializable]
public class RANettleBoxTrackingClientEvent2 : UnityEvent<Vector3, Vector3> { }

public class RANettleBoxTrackingHost : RAStateUpdater {
    public RANettleBoxTrackingClientEvent ChangeMonoModeEvent;
    public RANettleBoxTrackingClientEvent2 ChangeEyesPositionEvent;

    private NettleBoxTracking nettleBoxTracking;

    private Vector3 lastLeftEyeSent = new Vector3(0f, 1f, 0f);
    private Vector3 lastRightEyeSent = new Vector3(0f, 1f, 0f);

    private void Reset() {
        identificator = "NettleBoxTracking";
    }

    void Awake() {
        nettleBoxTracking = GetComponent<NettleBoxTracking>();
    }

    protected override void OnSetNetBehaviour() {
        if (netBehaviour == NetBehaviour.SERVER) {
            RepeatSendChanges();
        }
    }

    protected override void OnAddLink(RALink _raLink) {
        if (netBehaviour == NetBehaviour.SERVER) {
            (_raLink as RABytesLink).CmdSetBytesEvent += ReceiveBytesCmd;
        } else if (netBehaviour == NetBehaviour.CLIENT) {
            (_raLink as RABytesLink).RpcSetBytesEvent += ReceiveBytesRpc;
        }
    }

    
    private void ReceiveBytesCmd(byte[] bytes, NetworkConnection exConn) {
        if (bytes.Length == sizeof(bool)) {
            bool on = BitConverter.ToBoolean(bytes, 0);
            RemoteSetManualMode(on);
            SendManualModeState(on, exConn);
        }        
    }

    private void ReceiveBytesRpc(byte[] bytes) {        
        if (bytes.Length == sizeof(bool)) {
            bool on = BitConverter.ToBoolean(bytes, 0);
            RemoteSetManualMode(on);
        }
        else if (bytes.Length == sizeof(float) * 6) {
            byte[] serialized = new byte[sizeof(float) * 3];
            Array.Copy(bytes, 0, serialized, 0, sizeof(float) * 3);
            Vector3 left = VectorsByteConverter.BytesToVector3(serialized);
            Array.Copy(bytes, sizeof(float) * 3, serialized, 0, sizeof(float) * 3);
            Vector3 right = VectorsByteConverter.BytesToVector3(serialized);
            SetEyesPositionRpc(left, right);
            if (ChangeEyesPositionEvent != null) {
                ChangeEyesPositionEvent.Invoke(left, right);
            }
        }
    }

    private void RemoteSetManualMode(bool on) {
        if (ChangeMonoModeEvent != null)
            ChangeMonoModeEvent.Invoke(on);
        if (nettleBoxTracking != null) {
            nettleBoxTracking.ManualMode = on;
            nettleBoxTracking.ResetEyes();
        }
    }

    public void ToggleManualMode(bool on) {
        SendManualModeState(on, null);
    }

    //server
    protected override void Synchronize() {
        if (netBehaviour == NetBehaviour.SERVER) {
            SendManualModeState(nettleBoxTracking.ManualMode, null);
        }
    }
    

    //client
    private void SetEyesPositionRpc(Vector3 positionLeft, Vector3 positionRight) {
        if (nettleBoxTracking != null) {
            nettleBoxTracking.LeftEye = positionLeft;
            nettleBoxTracking.RightEye = positionRight;

        }
    }

    private void SendEyesPosition() {
        byte[] bytes = new byte[sizeof(float) * 6];
        byte[] serialized = VectorsByteConverter.VectorToBytes(nettleBoxTracking.LeftEye);
        Array.Copy(serialized, 0, bytes, 0, sizeof(float) * 3);
        serialized = VectorsByteConverter.VectorToBytes(nettleBoxTracking.RightEye);
        Array.Copy(serialized, 0, bytes, sizeof(float) * 3, sizeof(float) * 3);
        SendBytes(bytes);
    }

    private void SendManualModeState(bool on, NetworkConnection exceptConnection) {
        SendBytes(BitConverter.GetBytes(on),exceptConnection);
    }

    protected override void SendChanges() {
        if (nettleBoxTracking.LeftEye != lastLeftEyeSent || nettleBoxTracking.RightEye != lastRightEyeSent) {
            SendEyesPosition();
            if (ChangeEyesPositionEvent != null) {
                ChangeEyesPositionEvent.Invoke(nettleBoxTracking.LeftEye, nettleBoxTracking.RightEye);
            }
            lastLeftEyeSent = nettleBoxTracking.LeftEye;
            lastRightEyeSent = nettleBoxTracking.RightEye;
        }
    }    
}
}
