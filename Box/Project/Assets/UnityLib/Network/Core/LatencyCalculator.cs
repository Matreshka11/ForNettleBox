using System;
using UnityEngine;
using UnityEngine.Networking;

namespace Nettle {


public class LatencyCalculator : MonoBehaviour {
    private const float OWD_SMOOTH_SPEED = 0.02f;
    private const float OWD_SMOOTH_SPEED_UP_MULTIPLIER = 2f;

    [Header("Constants")]
    [SerializeField]
    private float _defaultOwd = 0.04f;
    [SerializeField]
    private float maxOwdLimit = 1f;

    [Space(10)]
    public float OwdInterval = 0.1f;
    [Tooltip("Don't accumulate ping if deltaTime > PING_DELTATIME_CUTOFF")]
    public float OwdDeltatimeCutoff = 0.1f;

    public float DecreaseOwdPercent = 0.001f; // 1 = 100% 
    public float IncreaseStep = 0.04f; //max seconds for add

    public float MaxOwdLifeTime = 15;
    public float MaxOwdDiffForAdd = 0.75f;


    [HideInInspector]
    public float MaxOwd;
    [HideInInspector]
    public float SmoothOwd;

    private float _curOwdLifeTime;

    void OnEnable() {
        _curOwdLifeTime = MaxOwdLifeTime;
        MaxOwd = _defaultOwd;
        SmoothOwd = _defaultOwd;
    }

    public void AccumulateOWD(int remoteDelayTimeMs) {
        float owd = remoteDelayTimeMs * 0.001f;
        if (owd > maxOwdLimit) {
            owd = maxOwdLimit;
        }

        if (owd > MaxOwd) {
            if (owd - MaxOwd > IncreaseStep) {
                MaxOwd += IncreaseStep;
            } else {
                MaxOwd = owd;
            }
            _curOwdLifeTime = MaxOwdLifeTime;
        } else if (owd >= MaxOwd * MaxOwdDiffForAdd) {
            if (_curOwdLifeTime < MaxOwdLifeTime) {
                _curOwdLifeTime += MaxOwdLifeTime * OwdInterval * (MaxOwd - owd) / (MaxOwd * (1 - MaxOwdDiffForAdd)); //addLifeTime
            }
        } else if (_curOwdLifeTime > 0) {
            _curOwdLifeTime -= OwdInterval;
        } else {
            MaxOwd -= MaxOwd * DecreaseOwdPercent;
        }
    }

    void Update() {
        float owdChangeStep = OWD_SMOOTH_SPEED * Time.unscaledDeltaTime;
        if (SmoothOwd + owdChangeStep * OWD_SMOOTH_SPEED_UP_MULTIPLIER < MaxOwd) {
            SmoothOwd += owdChangeStep * OWD_SMOOTH_SPEED_UP_MULTIPLIER;
        } else if (SmoothOwd - owdChangeStep > MaxOwd) {
            SmoothOwd -= owdChangeStep;
        } else {
            SmoothOwd = MaxOwd;
        }
    }

    public static LatencyCalculator GetLatencyCalculator(NetworkConnection connection) {
        return connection.playerControllers[0].gameObject.GetComponent<RACore>().LatencyCalculator;
    }
}
}
