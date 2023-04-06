using UnityEngine;
using System.Linq;
using System.Collections.Generic;

namespace Nettle {

    public class VisibilityFactor {
        public bool State = true;
        public string Name;
    }

    public class VisibilityControl : MonoBehaviour {
        private List<VisibilityFactor> _factors = new List<VisibilityFactor>();
        [Tooltip("Array contains all tags that make this object active. If empty, will be active at any tag.")]
        public string[] Tags;
        public string ObjectName;
        private Renderer _attachedRenderer;

        public List<Collider> Colliders;

        public void Reset() {
            FillColliders();
        }

        public void OnValidate() {
            FillColliders();
        }

        public void FillColliders() {
            Colliders = new List<Collider>();
            Collider collider = GetComponent<Collider>();
            if (collider != null) {
                Colliders.Add(collider);
            }
            AddChildrenColliders(transform);

        }

        private void AddChildrenColliders(Transform parentTransform) {
            for (int i = 0; i < parentTransform.childCount; i++) {
                if (parentTransform.GetChild(i).GetComponent<VisibilityControl>() == null) {
                    Collider collider = parentTransform.GetChild(i).GetComponent<Collider>();
                    if (collider != null) {
                        Colliders.Add(collider);
                    }
                    AddChildrenColliders(parentTransform.GetChild(i));
                }
            }
        }

        public Renderer Renderer {
            get {
                if (_attachedRenderer == null) {
                    _attachedRenderer = GetComponent<Renderer>();
                }
                return _attachedRenderer;
            }
        }

        public VisibilityFactor[] Factors {
            get {
                return _factors.ToArray();
            }
        }

        public virtual bool HasTag(string newTag) {
            return Tags.Length == 0 || Tags.Any(t => t == newTag);
        }

        public virtual void EnableVisibilityFactor(string name) {
            SetVisibilityFactor(name, true);
        }

        public void DisableVisibilityFactor(string name) {
            SetVisibilityFactor(name, false);
        }

        public void SetVisibilityFactor(string name, bool state) {
            
            VisibilityFactor factor = GetFactor(name);
            if (factor == null) {
                factor = new VisibilityFactor() { State = state, Name = name };
                _factors.Add(factor);
            }
            factor.State = state;
            bool targetState = true;
            if (state) {
                foreach (VisibilityFactor f in _factors) {
                    if (!f.State) {
                        targetState = false;
                        break;
                    }
                }
            } else {
                targetState = false;
            }
			if (Renderer != null) {
				Renderer.enabled = targetState;
			}
            Colliders.ForEach(v => v.enabled = targetState);
        }

        public bool IsFactorEnabled(string name) {
            VisibilityFactor factor = GetFactor(name);
            return factor == null || factor.State;

        }

        private VisibilityFactor GetFactor(string name) {
            return _factors.Where(x => x.Name == name).FirstOrDefault();
        }

    }
}
