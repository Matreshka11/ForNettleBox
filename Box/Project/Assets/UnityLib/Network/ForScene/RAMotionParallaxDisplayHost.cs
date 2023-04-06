using System;
using System.Collections;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.Networking;

namespace Nettle {

    [Serializable]
    public class RAMotionParallaxDisplayClientEvent : UnityEvent<Vector3, Quaternion, Vector3> { }

    public class RAMotionParallaxDisplayHost : RAStateUpdater {

        public RAMotionParallaxDisplayClientEvent rpcDisplayChangedEvent = new RAMotionParallaxDisplayClientEvent();

        private MotionParallaxDisplay display;
        private Transform dispTransform;
        private Vector3 oldPosition = Vector3.zero;
        private Quaternion oldRotation = Quaternion.identity;
        private Vector3 oldLocalScale = Vector3.one;
        //client
        private RAMotionParallaxDisplayLink raLink = null;
        private Vector3 incomePosition = Vector3.zero;
        private Quaternion incomeRotation = Quaternion.identity;
        private Vector3 incomeLocalScale = Vector3.one;


        private void Reset() {
            identificator = "MotionParallaxDisplay";
        }

        void Awake() {
            display = GetComponent<MotionParallaxDisplay>();
            if (display != null) {
                dispTransform = display.transform;
            }
        }

        protected override void UnsubscribeFromLink(RALink link) {
            ((RAMotionParallaxDisplayLink)link).OnRpcDisplayChanged -= TransformDisplay;
        }

        protected override void OnSetNetBehaviour() {

            if (netBehaviour == NetBehaviour.SERVER) {
                RepeatSendChanges(true);
            }
        }
        protected override void OnAddLink(RALink _raLink) {
            if (netBehaviour == NetBehaviour.CLIENT) {
                raLink = (_raLink as RAMotionParallaxDisplayLink);
                raLink.OnRpcDisplayChanged += TransformDisplay;
            }
        }

        protected override void Synchronize() {
            if (netBehaviour == NetBehaviour.SERVER && dispTransform != null) {
                RpcDisplayChanged(dispTransform.position, dispTransform.rotation, dispTransform.localScale);
            }
        }

        //client
        private void TransformDisplay(Vector3 position, Quaternion rotation, Vector3 scale) {
            if (IsNanAny(position)) {
                Debug.LogError("Display RPC position is NaN!");
                return;
            }
            if (IsNanAny(scale)) {
                Debug.LogError("Display RPC scale is NaN!");
                return;
            }
            if (scale == Vector3.zero) {
                Debug.LogError("Display RPC scale is zero!");
                return;
            }
            incomePosition = position;
            incomeRotation = rotation;
            incomeLocalScale = scale;
            if (dispTransform != null) {
                dispTransform.position = position;
                dispTransform.rotation = rotation;
                dispTransform.localScale = scale;
            }
            if (rpcDisplayChangedEvent != null) {
                rpcDisplayChangedEvent.Invoke(position, rotation, scale);
            }
        }

        protected override void SendChanges() {
            if (dispTransform != null && (
                dispTransform.position != oldPosition ||
                dispTransform.rotation != oldRotation ||
                dispTransform.localScale != oldLocalScale)) {

                oldPosition = dispTransform.position;
                oldRotation = dispTransform.rotation;
                oldLocalScale = dispTransform.localScale;
            }
            RpcDisplayChanged(oldPosition, oldRotation, oldLocalScale);
        }

        private void RpcDisplayChanged(Vector3 position, Quaternion rotation, Vector3 scale) {
            RALink[] links = GetLinks();
            foreach (RAMotionParallaxDisplayLink link in links) {
                if (link != null) {
                    link.RpcDisplayChanged(position, rotation, scale, NetworkTransport.GetNetworkTimestamp() - (int)Mathf.Round(Time.realtimeSinceStartup - Time.unscaledTime));
                }
            }
        }

        public override Type GetTypeOfLink() {
            return typeof(RAMotionParallaxDisplayLink);
        }

        //public void RequestSynchronization() {
        //    raLink.CmdRequestSync();
        //}

        public void SetLastDisplayTransform() {
            if (dispTransform != null && enabled) {
                dispTransform.position = incomePosition;
                dispTransform.rotation = incomeRotation;
                dispTransform.localScale = incomeLocalScale;
            }
        }

        public bool IsNanAny(Vector3 v) {
            return float.IsNaN(v.x) || float.IsNaN(v.y) || float.IsNaN(v.z);
        }
    }
}
