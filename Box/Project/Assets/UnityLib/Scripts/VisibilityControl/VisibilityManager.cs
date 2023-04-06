using System.Linq;
using UnityEngine;
using System.Collections.Generic;
using System;
using UnityEngine.Events;

namespace Nettle {

public delegate void VisibilityListDelegate(List<VisibilityControl> list, string oldTag, string newTag);
public delegate void VisibilityHideShowDelegate(List<VisibilityControl> hide, List<VisibilityControl> show, string oldTag, string newTag);

[Serializable]
public class OnTagChangedEvent: UnityEvent<string> { }

[ExecuteInEditMode]
public class VisibilityManager: MonoBehaviour {
    public List<VisibilityControl> Controls { get; private set; }

    public List<VisibilityControl> HideList {
        get { return _hideList; }
        private set { _hideList = value; }
    }

    public List<VisibilityControl> ShowList {
        get { return _showList; }
        private set { _showList = value; }
    }

    public GameObject Display;

    private bool _firstSwitch = true;

    [HideInInspector]
    public bool EditorModeSlice = false;
    
    private static VisibilityManager Instance {
        get {
            if (_instance == null) {
                _instance = FindObjectOfType<VisibilityManager>();
            }
            return _instance;
        }
    }
    private static VisibilityManager _instance;

    private const string BorderNameStart = "border";

    public event VisibilityListDelegate AppliedHideList;
    public event VisibilityListDelegate AppliedShowList;

    public event Action BeginShowTag;
    public event Action EndShowTag;

    public event Action<string> OnBeginSwitch;
    private const string NoTag = "";

    private string _oldTag = "";
    public string OldTag {
        get { return _oldTag; }
        set { _oldTag = value; }
    }
    private string _currentTag = "";
    public string CurrentTag {
        get { return _currentTag; }
        private set {
            _currentTag = value;
            OnTagChanged();
        }
    }
    private string _newTag = NoTag;
    public string NewTag {
        get { return _newTag; }
        set { _newTag = value; }
    }
    private bool _waitForApply;

    protected virtual void OnBeginShowTag() {
        if (BeginShowTag != null) {
            BeginShowTag.Invoke();
        }
    }

    public OnTagChangedEvent TagChanged = new OnTagChangedEvent();
    /// <summary>
    /// Enlists all visibility tags that exist in the scene
    /// </summary>
    public List<string> AllTags { get; private set; }
    private List<VisibilityControl> _showList;
    private List<VisibilityControl> _hideList;

    protected virtual void OnTagChanged() {
        if (TagChanged != null) {
            TagChanged.Invoke(_currentTag);
        }
    }

    void Awake() {
        Initialize();
    }

    protected virtual void Start() {
#if !UNITY_EDITOR
        if (Display != null) {
            SetDisplayObjectEnabled(false);
        } else {
            Debug.LogWarning("VisibilityManagerStreaming: mp3d is not set!");
        }
#endif
    }

    void SetDisplayObjectEnabled(bool enable) {
        if (Display != null) {
            Display.gameObject.SetActive(enable);
        }
    }

    protected virtual void Initialize() {
        ShowList = new List<VisibilityControl>();
        HideList = new List<VisibilityControl>();
        UpdateTargets();

#if UNITY_EDITOR
        if (!Application.isPlaying) {
            string noSliceTag = AllTags.Find(x => x.ToLower() == "no_slice");
            if (!string.IsNullOrEmpty(noSliceTag)) {
                BeginSwitch(noSliceTag);
            }
        }
#endif
    }

    public void UpdateTargets() {
        Controls = FindTargets();
#if UNITY_EDITOR
        AllTags = new List<string>();
        VisibilityZone[] zones = FindObjectsOfType<VisibilityZone>();
        foreach (VisibilityZone zone in zones) {
            if (!AllTags.Contains(zone.VisibilityTag)) {
                AllTags.Add(zone.VisibilityTag);
            }
        }
        AllTags.Sort();
#endif
    }

    public static List<VisibilityControl> FindTargets() {
        return FindObjectsOfType(typeof(VisibilityControl)).Cast<VisibilityControl>().ToList();
    }

    public static void QueryObjects(string visibilityTag, List<VisibilityControl> objects, ref List<VisibilityControl> showList, ref List<VisibilityControl> hideList) {
        showList.Clear();
        hideList.Clear();
        if (objects == null) return;
#if UNITY_EDITOR
        if (!Application.isPlaying && !Instance.EditorModeSlice) {
            showList.AddRange(objects);
            return;
        }
#endif
            foreach (var c in objects) {
            if (c.HasTag(visibilityTag)) {
                showList.Add(c);
            } else
                hideList.Add(c);
        }
    }

    public void BeginSwitchUnsafe(string newTag) {
        BeginSwitch(newTag);
    }

    public virtual bool BeginSwitch(string newTag, bool forced = false) {
        if (!forced) {
            if ((CurrentTag == newTag) && (_newTag == NoTag)) {
                return false;
            }

            if (_newTag == newTag) {
                return false;
            }
        }
        _newTag = newTag;

        if(OnBeginSwitch != null)
            OnBeginSwitch.Invoke(_newTag);


        //TODO: hide/show lists by ref
        QueryObjects(newTag, Controls, ref _showList, ref _hideList);

        OnBeginShowTag();

        _waitForApply = true;
        return true;
    }

    public virtual void Update() {
        if (_waitForApply && CanSwitchTag()) {
            if (_firstSwitch) {
                SetDisplayObjectEnabled(true);
                _firstSwitch = false;
            }

            ApplyHideList();
            ApplyShowList();
            OldTag = _currentTag;
            CurrentTag = _newTag;
            _newTag = NoTag;
            ShowList.Clear();
            HideList.Clear();
            _waitForApply = false;
            OnEndShowTag();
        }
    }

    public void ResetTag() {
        _currentTag = "";
    }

    protected virtual bool CanSwitchTag() {
        return true;
    }

    protected virtual void ApplyHideList() {
        if (HideList == null)
            return;
        foreach (VisibilityControl t in HideList.Where(t => t != null)) {
            t.DisableVisibilityFactor("tagVisibility");
        }

        if (AppliedHideList != null) {
            AppliedHideList.Invoke(HideList, OldTag, NewTag);
        }
    }

    protected virtual void ApplyShowList() {
        if (ShowList != null) {
            foreach (VisibilityControl t in ShowList.Where(t => t != null)) {
                t.EnableVisibilityFactor("tagVisibility");
            }
            if (AppliedShowList != null) {
                AppliedShowList.Invoke(ShowList, _oldTag, _newTag);
            }
        }
    }

    protected virtual void OnEndShowTag() {
        if (EndShowTag != null) EndShowTag();
    }
}


}
