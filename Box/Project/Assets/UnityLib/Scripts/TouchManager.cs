using UnityEngine;
using System;
using UnityEngine.Events;
using System.Collections.Generic;

[Serializable]
public class TouchManagerMoveEvent : UnityEvent<Vector2> { }
[Serializable]
public class TouchManagerZoomEvent : UnityEvent<float> { }

public class TouchManager : MonoBehaviour {
    public float MouseMoveScale = 1.0f;
    public float TouchMoveScale = 1.0f;
    public float ScrollZoomMultiplier = 0.1f;
    //public bool flipTouchX = true;
	//public bool flipTouchY = true;

    public TouchManagerMoveEvent MoveEvent = new TouchManagerMoveEvent();
    public TouchManagerZoomEvent ZoomEvent = new TouchManagerZoomEvent();

    //if canvas render overlay(canvas with fieldsOfTouch), then this field is null
    public Camera CameraForFieldsOfTouch = null;

    public List<RectTransform> FieldsOfTouch;
    private bool _enableTouchProcess = false;
    private bool _noInput = true;
    private bool _useTouch;
    private Vector2 _lastMousePosition;
    private bool _drag;

    private void Awake() {		
		_useTouch = SystemInfo.deviceType == DeviceType.Handheld;
	}

	private Vector2 GetMouseMoveDelta() {
		var delta = Vector2.zero;
		Vector2 mousePosition = Input.mousePosition;     

        if (Input.GetMouseButtonDown(0)) {
			_drag = true;
			_lastMousePosition = mousePosition;
        }
		
		if (Input.GetMouseButtonUp(0)) {
			_drag = false;
		}
		
		if (_drag) {
			if (mousePosition != _lastMousePosition) {
				delta = (_lastMousePosition - mousePosition);
				_lastMousePosition = mousePosition;
			}
		}
		
		return delta * MouseMoveScale;
	}
	
	private float GetMouseScrollDelta() { 
		return Input.GetAxis("Mouse ScrollWheel");
	}
	
	private Vector2 GetTouchMoveDelta() {
		if (Input.touchCount == 1) {
			var touch = Input.GetTouch(0);
			if (touch.phase == TouchPhase.Moved) {
				return touch.deltaPosition * TouchMoveScale;
			}
		}
        return Vector2.zero;
	}

	private float GetTouchPinchScale() {
		float delta = 1.0f;
		
		if (Input.touchCount == 2) {
			var touchA = Input.GetTouch(0);
			var touchB = Input.GetTouch(1);
			var touchPositionA = touchA.position;
			var touchPositionB = touchB.position;
			
			if (touchA.phase == TouchPhase.Moved || touchB.phase == TouchPhase.Moved) {
				var currentDistance = Vector2.Distance(touchPositionA, touchPositionB);
				var previousDistance = Vector2.Distance(touchPositionA - touchA.deltaPosition, touchPositionB - touchB.deltaPosition);

			    delta = previousDistance/currentDistance;
			}
		}
		
		return delta;
	}

	
	private Vector2 ToNormalizedZoomPanSpace(Vector2 pos) {
		var screenSize = new Vector2((float)Screen.width, (float)Screen.height);
		var normalized = new Vector2(pos.x / screenSize.x, pos.y / screenSize.y);
		var aspect = screenSize.x / screenSize.y;
		normalized.y /= aspect;
		return normalized;
	}

	private void Update () {
		Vector2 delta = Vector2.zero;
		float scale = 1.0f;

        UpdateInput(ref _enableTouchProcess);

        if (_enableTouchProcess) {
            if (_useTouch) {
                delta = GetTouchMoveDelta();
                delta.x *= 1f;
                delta.y *= 1f;

                scale = GetTouchPinchScale();
            } else {
                delta = GetMouseMoveDelta();
                delta.x *= -1f;
                delta.y *= -1f;

                var scroll_delta = GetMouseScrollDelta();
                if (scroll_delta >= 0) {
                    //scale = 1.0f + scroll_delta * scrollZoomMultiplier;
                    scale = 1.0f / (1.0f + scroll_delta * ScrollZoomMultiplier);
                } else {
                    //scale = 1.0f / (1.0f - scroll_delta * scrollZoomMultiplier);
                    scale = 1.0f - scroll_delta * ScrollZoomMultiplier;
                }

            }
        }

		if (delta.x != 0f || delta.y != 0f) {
			var offset = ToNormalizedZoomPanSpace(delta);

            if (MoveEvent != null)
                MoveEvent.Invoke(offset);
        }

		if (scale != 1) {

            if (ZoomEvent != null)
                ZoomEvent.Invoke(scale);
            scale = 1;

		}
	}

    private void UpdateInput(ref bool refEnable) {

        if (_useTouch) {
            if (_noInput && Input.touchCount > 0) {

                _noInput = false;              
                refEnable = IsPositionInRectsOfTouch(Input.GetTouch(0).position);
            } else if (!_noInput && Input.touchCount == 0) {
                _noInput = true;
            }
        } else {
            if (Input.GetMouseButtonDown(0)) {
                //withoutInput = false;
                Vector2 mousePstn = new Vector2(Input.mousePosition.x, Input.mousePosition.y);
                refEnable = IsPositionInRectsOfTouch(mousePstn);
                //Debug.Log("ZoomPan Enable Updated " + refEnable);
            }
            if (Input.GetMouseButtonUp(0)) {
                //withoutInput = true;
            }
        }
    }

    private bool IsPositionInRectsOfTouch(Vector2 postn) {

        bool inRects = false;

        if (FieldsOfTouch.Count > 0) {
            foreach (RectTransform rt in FieldsOfTouch) {
                if (rt.gameObject.activeInHierarchy) {
                    if (RectTransformUtility.RectangleContainsScreenPoint(rt, postn, CameraForFieldsOfTouch)) {            
                        inRects = true;
                        break;
                    }
                }
            }
        } else {
            inRects = true;
        }

        return inRects;
    }
}
