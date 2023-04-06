using UnityEngine;
using UnityEngine.Networking;
using System;

namespace Nettle {

public class RAMotionParallaxDisplayLink : RALink {

    private class DisplayChangeSnapshot : NetSnapshot {

        public Vector3 Position;
        public Quaternion Rotation;
        public Vector3 Scale;

        public DisplayChangeSnapshot(Vector3 position, Quaternion rotation, Vector3 scale, float timeOfFrame = 0f) {
            TimeOfFrame = timeOfFrame;
            Position = position;
            Rotation = rotation;
            Scale = scale;
        }

        public override NetSnapshot Interpolate(NetSnapshot second, float timeBetweenFrames) {
            DisplayChangeSnapshot dcSecond = second as DisplayChangeSnapshot;
            float lerpValue = GetLerpValue(second.TimeOfFrame, timeBetweenFrames);
            Vector3 position = Vector3.Lerp(Position, dcSecond.Position, lerpValue);
            Quaternion rotation = Quaternion.Lerp(Rotation, dcSecond.Rotation, lerpValue);
            Vector3 scale = Vector3.Lerp(Scale, dcSecond.Scale, lerpValue);
            return new DisplayChangeSnapshot(position, rotation, scale, timeBetweenFrames);
        }
    }

    public event Action<Vector3, Quaternion, Vector3> OnRpcDisplayChanged;
    private bool useInterpolation = true;
    private NetInterpolator _interpolator = new NetInterpolator(RAStateUpdater.sendChangesInterval);

   /* void OnEnable() {
        _interpolator = new NetInterpolator(RAStateUpdater.sendChangesInterval);
    }*/

    //[ClientRpc]'s will do on client (call on server)
    [ClientRpc(channel = (int)EQoSChannels.UNRELIABLE_SEQUENCED)]
    public void RpcDisplayChanged(Vector3 position, Quaternion rotation, Vector3 scale, int networkTimestamp) {
        if (useInterpolation) {
            _interpolator.AddSnapshot(new DisplayChangeSnapshot(position, rotation, scale), NetworkManager.singleton.client.connection, networkTimestamp);
        } else {
            if (OnRpcDisplayChanged != null) {
                OnRpcDisplayChanged(position, rotation, scale);
            }
        }
    }

    void LateUpdate() {
        DisplayChangeSnapshot relativeSnapshot = _interpolator.GetSnapshot<DisplayChangeSnapshot>();
        if (relativeSnapshot != null) {
            if (OnRpcDisplayChanged != null) {
                OnRpcDisplayChanged(relativeSnapshot.Position, relativeSnapshot.Rotation, relativeSnapshot.Scale);
            }
        }
    }
}
}
