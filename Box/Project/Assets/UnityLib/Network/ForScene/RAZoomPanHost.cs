using System;
using System.Linq;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.Networking;

namespace Nettle {

    [Serializable]
    public class RAZoomPanHostEventV2 : UnityEvent<Vector2> { }

    [Serializable]
    public class RAZoomPanHostEventF : UnityEvent<float> { }

    public class RAZoomPanHost : RAStateUpdater {

        public RAZoomPanHostEventV2 RAZoomPanHostMoveEvent;
        public RAZoomPanHostEventF RAZoomPanHostZoomEvent;

        private ZoomPan zoomPan;

        private Vector2 moveDeltaToSend = Vector2.zero;
        private float zoomScaleToSend = 1f;

        private class MoveSnapshot : NetSnapshot {
            public Vector2 Delta;

            public MoveSnapshot(float timeOfFrame, Vector2 delta) {
                TimeOfFrame = timeOfFrame;
                Delta = delta;
            }

            public override NetSnapshot Interpolate(NetSnapshot second, float timeBetweenFrames) {
                MoveSnapshot secondSnapshot = (MoveSnapshot)second;
                Vector2 delta = Vector2.zero;
                secondSnapshot.Delta += Delta;
                Delta = Vector2.zero;
                if (secondSnapshot.Delta != Vector2.zero) {
                    delta = Vector2.Lerp(Vector2.zero, secondSnapshot.Delta, GetLerpValue(second.TimeOfFrame, timeBetweenFrames));
                    TimeOfFrame = timeBetweenFrames;
                    secondSnapshot.Delta -= delta;
                    if (secondSnapshot.Delta.sqrMagnitude < 0.00001f) {
                        secondSnapshot.Delta = Vector2.zero;
                    }
                }
                return new MoveSnapshot(timeBetweenFrames, delta);
            }

            public override void Accumulate(NetSnapshot skippedSnapshot) {
                MoveSnapshot skippedMoveSnapshot = (MoveSnapshot)skippedSnapshot;
                if (skippedMoveSnapshot.Delta != Vector2.zero) {
                    Delta += skippedMoveSnapshot.Delta;
                    TimeOfFrame = skippedMoveSnapshot.TimeOfFrame;
                }
                /* Debug.Log(Time.frameCount+"Accumulate::distance=" + skippedMoveSnapshot.Delta.magnitude + "::time=" + skippedMoveSnapshot.TimeOfFrame
                     + "::Result::distance=" + Delta.magnitude + "::time=" + TimeOfFrame);*/
            }
        }

        private class ZoomSnapshot : NetSnapshot {
            public float Scale;

            public ZoomSnapshot(float timeOfFrame, float scale) {
                TimeOfFrame = timeOfFrame;
                Scale = scale;
            }

            public override NetSnapshot Interpolate(NetSnapshot second, float timeBetweenFrames) {
                ZoomSnapshot secondSnapshot = (ZoomSnapshot)second;
                float scale = 1f;
                secondSnapshot.Scale += Scale - 1;
                Scale = 1f;
                if (secondSnapshot.Scale != 1f) {
                    scale = Mathf.Lerp(1f, secondSnapshot.Scale, GetLerpValue(second.TimeOfFrame, timeBetweenFrames));
                    TimeOfFrame = timeBetweenFrames;
                    // Debug.Log("scale="+ scale+ "::secondSnapshot.Scale=" + secondSnapshot.Scale);
                    secondSnapshot.Scale -= scale - 1;
                    if (Mathf.Abs(secondSnapshot.Scale) < 0.0001f) {
                        secondSnapshot.Scale = 1f;
                    }
                }
                return new ZoomSnapshot(timeBetweenFrames, scale);
            }

            public override void Accumulate(NetSnapshot skippedSnapshot) {
                ZoomSnapshot skippedMoveSnapshot = (ZoomSnapshot)skippedSnapshot;
                if (skippedMoveSnapshot.Scale != 1f) {
                    Scale += skippedMoveSnapshot.Scale - 1;
                    TimeOfFrame = skippedMoveSnapshot.TimeOfFrame;
                }

            }
        }
        private NetInterpolator _moveNetInterpolator = new NetInterpolator(RAStateUpdater.sendChangesInterval, true, true);
        private NetInterpolator _zoomNetInterpolator = new NetInterpolator(RAStateUpdater.sendChangesInterval, true, true);

        private void Reset() {
            identificator = "ZoomPan";
        }

        void Awake() {
            zoomPan = GetComponent<ZoomPan>();
        }

        void Update() {
            if (netBehaviour == NetBehaviour.SERVER) {
                MoveSnapshot moveSnapshot = _moveNetInterpolator.GetSnapshot<MoveSnapshot>();
                if (moveSnapshot != null && moveSnapshot.Delta != Vector2.zero) {
                    Move(moveSnapshot.Delta);
                }

                ZoomSnapshot zoomSnapshot = _zoomNetInterpolator.GetSnapshot<ZoomSnapshot>();
                if (zoomSnapshot != null && zoomSnapshot.Scale != 1f) {
                    Zoom(zoomSnapshot.Scale);
                }
            }
        }

        protected override void OnSetNetBehaviour() {
            if (netBehaviour == NetBehaviour.CLIENT) {
                RepeatSendChanges();
            }
        }

        protected override void OnAddLink(RALink raLink) {
            if (netBehaviour == NetBehaviour.SERVER) {
                RABytesLink tempRcNavigation = (raLink as RABytesLink);
                tempRcNavigation.CmdSetBytesEvent += CommandReceived;
            }
        }

        protected override void SendChanges() {
            if (moveDeltaToSend != Vector2.zero) {
                byte[] timestamp = BitConverter.GetBytes(NetworkTransport.GetNetworkTimestamp());
                byte[] vector = VectorsByteConverter.VectorToBytes(moveDeltaToSend);
                SendBytes(timestamp.Concat(vector).ToArray());
                moveDeltaToSend.Set(0f, 0f);
            }
            if (zoomScaleToSend != 1) {
                byte[] timestamp = BitConverter.GetBytes(NetworkTransport.GetNetworkTimestamp());
                byte[] scale = BitConverter.GetBytes(zoomScaleToSend);
                SendBytes(timestamp.Concat(scale).ToArray());
                zoomScaleToSend = 1f;
            }
        }

        private void CommandReceived(byte[] bytes, NetworkConnection connection) {
            float time = NetUtils.GetMsgTime(connection, BitConverter.ToInt32(bytes, 0));
            if (bytes.Length > sizeof(int) + sizeof(float)) {
                //Debug.Log(Time.frameCount + "Receive::distance=" + delta.magnitude + "; time=" + time);
                _moveNetInterpolator.AddSnapshot(new MoveSnapshot(time, VectorsByteConverter.BytesToVector2(bytes.Skip(sizeof(int)).ToArray())), connection);
            } else {
                _zoomNetInterpolator.AddSnapshot(new ZoomSnapshot(time, BitConverter.ToSingle(bytes, sizeof(int))), connection);
            }
        }

        public void Move(Vector2 delta) {
            // Debug.Log(Time.frameCount + "distance=" + delta.magnitude);
            if (zoomPan != null)
                zoomPan.Move(delta.x, delta.y);
            else {
                if (RAZoomPanHostMoveEvent != null) {
                    RAZoomPanHostMoveEvent.Invoke(delta);
                }
                if (netBehaviour == NetBehaviour.CLIENT) {
                    moveDeltaToSend += delta;
                }
            }
        }

        public void Zoom(float scale) {
            if (zoomPan != null)
                zoomPan.DoZoomDirect(scale);
            else {
                if (RAZoomPanHostZoomEvent != null) {
                    RAZoomPanHostZoomEvent.Invoke(scale);
                }
                if (netBehaviour == NetBehaviour.CLIENT) {
                    zoomScaleToSend *= scale;
                }
            }
        }



    }
}
