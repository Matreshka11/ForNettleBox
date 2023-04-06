using UnityEngine;
using System.Collections.Generic;
using UnityEngine.Networking;

namespace Nettle {

public abstract class NetSnapshot {
    public float TimeOfFrame;

    public abstract NetSnapshot Interpolate(NetSnapshot next, float timeBetweenFrames);

    protected float GetLerpValue(float timeOfSecondSnapshot, float timeBetweenFrames) {
        return (timeBetweenFrames - TimeOfFrame) / (timeOfSecondSnapshot - TimeOfFrame);
    }

    public virtual void Accumulate(NetSnapshot skippedSnapshot) { //for delta interpolation

    }
}

public class NetInterpolator {
    protected LinkedList<NetSnapshot> packets = new LinkedList<NetSnapshot>();
    protected float _sendChangesInterval;
    private bool _accumulateSnapshots;
    private bool _updateLastSnapshotTime;
    private LatencyCalculator _latencyCalculator;
    private NetworkConnection _connection;

    public NetInterpolator(float sendChangesInterval, bool accumulateSnapshots = false, bool updateLastSnapshotTime = false) {
        _sendChangesInterval = sendChangesInterval;
        _accumulateSnapshots = accumulateSnapshots;
        _updateLastSnapshotTime = updateLastSnapshotTime;

    }

    public void AddSnapshot(NetSnapshot newSnapshot, NetworkConnection connection, int networkTimestamp) {
        newSnapshot.TimeOfFrame = NetUtils.GetMsgTime(connection, networkTimestamp);
        AddSnapshot(newSnapshot, connection);
    }

    public void AddSnapshot(NetSnapshot newSnapshot, NetworkConnection connection) {
        packets.AddLast(newSnapshot);
        if (connection != _connection) {
            _latencyCalculator = LatencyCalculator.GetLatencyCalculator(connection);
        }
    }


    public virtual T GetSnapshot<T>() where T : NetSnapshot {
        if (packets.Count == 0 || _latencyCalculator == null) {
            return null;
        }

        float curTime = Time.unscaledTime - _latencyCalculator.SmoothOwd - _sendChangesInterval;

        if (packets.First.Value.TimeOfFrame > curTime) {
            return null;
        }

        LinkedListNode<NetSnapshot> firstSnapshot = packets.First;
        LinkedListNode<NetSnapshot> nextSnapshot = firstSnapshot.Next;

        while (packets.Count >= 2 && curTime > nextSnapshot.Value.TimeOfFrame) {
            if (_accumulateSnapshots) {
                nextSnapshot.Value.Accumulate(firstSnapshot.Value);
            }

            packets.RemoveFirst();
            firstSnapshot = nextSnapshot;
            nextSnapshot = firstSnapshot.Next;
        }

        /*if (packets.Count == 2 && curTime - nextSnapshot.Value.TimeOfFrame > 0) {
            Debug.LogErrorFormat("{4}::Snapshots={0}::Lag={1}::deltaTime={2}::smoothOwd={3}::totalFrameLatency={5}",
                packets.Count, curTime - nextSnapshot.Value.TimeOfFrame,
                Time.deltaTime, LatencyCalculator.SmoothOwd, Time.frameCount, LatencyCalculator.SmoothOwd + _sendChangesInterval);
        } else {
            Debug.LogFormat("{4}::Snapshots={0}::Left={1}::deltaTime={2}::smoothOwd={3}::totalFrameLatency={5}",
                packets.Count, packets.Last.Value.TimeOfFrame - curTime,
               Time.deltaTime, LatencyCalculator.SmoothOwd, Time.frameCount, LatencyCalculator.SmoothOwd + _sendChangesInterval);
        }*/


        if (nextSnapshot == null) {
            nextSnapshot = firstSnapshot;
            if (_updateLastSnapshotTime) {
                packets.Last.Value.TimeOfFrame = curTime;
            }
        }


        return (T)firstSnapshot.Value.Interpolate(nextSnapshot.Value, curTime);
    }
}
}
