// --------------------------------------------------------------------------------------------------------------------
// <copyright>
// Copyright © 2012 - 2013 Strategic Forge
//
// Email: jim@strategicforge.com
// </copyright> 
// <summary> 
// File: GameInput.cs
// Singleton Game Input class that receives and records Mouse and special Key events not intended for the Gui.
// </summary> 
// -------------------------------------------------------------------------------------------------------------------- 

#define DEBUG_LOG
#define DEBUG_WARN
#define DEBUG_ERROR

// default namespace

using CodeEnv.Master.Common;
using UnityEngine;
using System.Linq;
using CodeEnv.Master.GameContent;
using System;

/// <summary>
/// Singleton Game Input class that receives and records Mouse and special Key events not intended for the Gui.
/// The mouse events come from the Ngui event system and the key events come from Unity's Input class.
/// </summary>
[Obsolete]
public class GameInput : AGenericSingleton<GameInput>, IInputManager {

    // WARNING: This class is referenced in CameraControl's Configuration classes which are initialized outside of Awake or Start. 
    // As a result, GameInput is instantiated at EDITOR TIME, before runtime even starts. Therefore, this call to References to provide
    // a GameInputHelper reference will return null, as References will only be initialized at the beginning of Runtime. I work around
    // this by using the reference held by References directly in the code, code that will only be executed during runtime.
    //public IGameInputHelper _inputHelper = References.InputHelper

    private KeyCode[] _viewModeKeyCodesToSearch;

    private GameInput() {
        Initialize();
    }

    protected override void Initialize() {
        ViewModeKeys[] _viewModeKeysExcludingDefault = Enums<ViewModeKeys>.GetValues().Except(default(ViewModeKeys)).ToArray();
        _viewModeKeyCodesToSearch = _viewModeKeysExcludingDefault.Select(sk => (KeyCode)sk).ToArray<KeyCode>();
        Subscribe();
    }

    private void Subscribe() {
        UICamera.onScroll += OnScroll;
        UICamera.onDragStart += OnDragStart;
        UICamera.onDrag += OnDrag;
        UICamera.onDragEnd += OnDragEnd;
        UICamera.onClick += OnClick;
    }

    #region ArchivedScrollWheel

    //public bool isScrollValueWaiting;
    //private float _scrollWheelDelta;

    //private void OnScroll(GameObject go, float delta) {
    //    if (UICamera.isOverUI) {
    //        //D.Log("Scroll using GameObject {0} detected over UI.", go.name);
    //        return;
    //    }
    //    WriteMessage(go.name + Constants.Space + delta);
    //    RecordScrollWheelMovement(delta);
    //}

    //private void RecordScrollWheelMovement(float delta) {
    //    _scrollWheelDelta = delta;
    //    isScrollValueWaiting = true;
    //}

    //public float GetScrollWheelMovement() {
    //    if (!isScrollValueWaiting) {
    //        D.Warn("Mouse ScrollWheel inquiry made with no scroll value waiting.");
    //    }
    //    isScrollValueWaiting = false;
    //    float delta = _scrollWheelDelta;
    //    _scrollWheelDelta = Constants.ZeroF;
    //    return delta;
    //}

    // Unlike ClearDrag, ClearScrollWheel not needed as all scroll wheel movement events recorded here will always be used

    #endregion

    #region ScrollWheel

    //private void OnScroll(GameObject go, float delta) {
    //    if (UICamera.isOverUI) {
    //        //D.Log("Scroll using GameObject {0} detected over UI.", go.name);
    //        return;
    //    }
    //    WriteMessage(go.name + Constants.Space + delta);
    //    ICameraTargetable target = null;
    //    Vector3 hitPoint = Vector3.zero;
    //    if (go != UICamera.fallThrough) {
    //        // scroll event hit something so check in self and parents as some colliders are located on a child mesh or sprite
    //        target = go.GetInterfaceInParents<ICameraTargetable>(excludeSelf: false);
    //    }

    //    if (target == null && PlayerViews.Instance.ViewMode == PlayerViewMode.SectorView) {
    //        var sectorExaminer = go.GetComponent<SectorExaminer>();
    //        if (sectorExaminer != null) {
    //            target = SectorGrid.GetSector(sectorExaminer.CurrentSectorIndex);
    //        }
    //    }

    //    if (target != null) {
    //        // scroll event target is an ICameraTargetable
    //        if (!target.IsEligible) {
    //            // the object's collider shouldn't be enabled when not eligible (aka not discernible)
    //            D.Warn("InEligible {0} found while scrolling.", typeof(ICameraTargetable).Name);
    //            return;
    //        }
    //        hitPoint = (target is IZoomToFurthest) ? UICamera.lastHit.point : target.Transform.position;
    //    }
    //    RecordScrollEvent(new ScrollEvent(target, delta, hitPoint));
    //}

    private void OnScroll(GameObject go, float delta) {
        if (UICamera.isOverUI) {
            //D.Log("Scroll using GameObject {0} detected over UI.", go.name);
            return;
        }
        WriteMessage(go.name + Constants.Space + delta);
        ICameraTargetable target = null;
        Vector3 hitPoint = Vector3.zero;
        if (go != UICamera.fallThrough) {
            // scroll event hit something so check in self and parents as some colliders are located on a child mesh or sprite
            target = go.GetInterfaceInParents<ICameraTargetable>(excludeSelf: false);

            if (target == null && PlayerViews.Instance.ViewMode == PlayerViewMode.SectorView) {
                var sectorExaminer = go.GetComponent<SectorExaminer>();
                if (sectorExaminer != null) {
                    target = SectorGrid.GetSector(sectorExaminer.CurrentSectorIndex);
                }
            }
        }

        if (target != null) {
            // scroll event target is an ICameraTargetable
            if (!target.IsCameraTargetEligible) {
                // the object's collider shouldn't be enabled when not eligible (aka not discernible)
                D.Warn("InEligible {0} found while scrolling.", typeof(ICameraTargetable).Name);
                return;
            }
            hitPoint = (target is IZoomToFurthest) ? UICamera.lastHit.point : target.Transform.position;
        }
        // even scroll events not over a gameObject (target = null) must be recorded so DummyTarget can be moved
        RecordScrollEvent(new ScrollEvent(target, delta, hitPoint));
    }

    private ScrollEvent _scrollEvent;

    public bool IsScrollEventWaiting { get; private set; }

    public void RecordScrollEvent(ScrollEvent scrollEvent) {
        _scrollEvent = scrollEvent;
        IsScrollEventWaiting = true;
    }

    public ScrollEvent GetScrollEvent() {
        var scrollEvent = _scrollEvent;
        _scrollEvent = default(ScrollEvent);
        IsScrollEventWaiting = false;
        return scrollEvent;
    }

    // Unlike ClearDrag, ClearScrollWheel not needed as all scroll wheel movement events recorded here will always be used

    #endregion

    #region Dragging

    public bool IsDragging { get; private set; }
    public bool IsDragValueWaiting { get; private set; }
    private Vector2 _dragDelta;

    private void OnDragStart(GameObject go) {
        if (UICamera.isOverUI) {
            //D.Log("OnDragStart using GameObject {0} detected over UI.", go.name);
            return;
        }
        WriteMessage(go.name);
        // turn off the click notification that would normally occur once the drag is complete
        UICamera.currentTouch.clickNotification = UICamera.ClickNotification.None;
        IsDragging = true;
    }

    private void OnDrag(GameObject go, Vector2 delta) {
        if (UICamera.isOverUI) {
            //D.Log("OnDrag using GameObject {0} detected over UI.", go.name);
            return;
        }
        WriteMessage(go.name + Constants.Space + delta);
        _dragDelta += delta;
        IsDragValueWaiting = true;
    }

    private void OnDragEnd(GameObject go) {
        if (UICamera.isOverUI) {
            //D.Log("OnDragEnd using GameObject {0} detected over UI.", go.name);
            return;
        }
        WriteMessage(go.name);
        // the drag has ended so clear any residual drag values that may not have qualified for use by CameraControl due to wrong button, etc.
        ClearDragValue();
        IsDragging = false;
    }

    public Vector2 GetDragDelta() {
        D.Assert(IsDragValueWaiting, "Drag inquiry made with no delta value waiting.");
        Vector2 delta = _dragDelta;
        ClearDragValue();
        return delta;
    }

    private void ClearDragValue() {
        _dragDelta = Vector2.zero;
        IsDragValueWaiting = false;
    }

    #endregion

    #region Clicked

    // used by SelectionManager to clear the Selection when an unconsumed click occurs
    public event Action<NguiMouseButton> onUnconsumedClick;

    private void OnClick(GameObject go) {
        if (UICamera.isOverUI) {
            //D.Log("OnClick({0}) detected over UI.", go.name);
            return;
        }
        //Arguments.ValidateNotNull(go);  // click events should not occur except over a game object
        WriteMessage(go.name);

        if (UICamera.fallThrough == go) {
            // the target of the click is the fallThrough event handler, so this click wasn't consumed by another gameobject
            OnUnconsumedClick();
        }
    }

    /// <summary>
    /// Called when a mouse button click event occurs but no collider consumes it.
    /// </summary>
    private void OnUnconsumedClick() {
        if (onUnconsumedClick != null) {
            onUnconsumedClick(References.InputHelper.CurrentMouseButton);
        }
    }

    #endregion

    #region Pressed

    // Previously used by SectorExaminer to open a contextMenu without using a collider.
    // This works but I replaced the Wireframe Mouse hot spot with a small collider instead
    // as the same approach using onUnconsumedHover was unreliable due to the way Ngui
    // spams onHover events

    //public event Action<NguiMouseButton, bool> onUnconsumedPress;

    //public void RecordUnconsumedPress(bool isDown) {
    //    if (!IsDragging) {  // if dragging, the press shouldn't have any meaning except related to terminating a drag
    //        var d = onUnconsumedPress;
    //        if (d != null) {
    //            d(GameInputHelper.GetMouseButton(), isDown);
    //        }
    //    }
    //}

    #endregion

    #region Ngui Key Events

    // Ngui KeyEvents are only sent when a GameObject is selected from Ngui's perspective - aka the gameObject has been clicked.
    // The KeyCodes sent are limited to the arrow keys, along with the UICamera-designated submit and cancel keys.
    // As I use arrow keys to move the camera without a requirement for a gameObject to be under the mouse, I can't use this.

    #endregion

    #region SpecialKeys

    // activates ViewMode in PlayerViews
    public event Action<ViewModeKeys> onViewModeKeyPressed;

    public void CheckForKeyActivity() {
        IGameInputHelper inputHelper = References.InputHelper;
        if (inputHelper.IsAnyKeyOrMouseButtonDown) {
            KeyCode keyPressed;
            if (inputHelper.TryIsKeyDown(out keyPressed, _viewModeKeyCodesToSearch)) {
                if (onViewModeKeyPressed != null) {
                    onViewModeKeyPressed((ViewModeKeys)keyPressed);
                }
            }
        }
    }

    #endregion

    private void WriteMessage(string arg = "") {
        if (DebugSettings.Instance.EnableEventLogging) {
            System.Diagnostics.StackFrame stackFrame = new System.Diagnostics.StackTrace().GetFrame(1);
            NguiMouseButton? button = Enums<NguiMouseButton>.CastOrNull(UICamera.currentTouchID);
            string touchID = (button ?? NguiMouseButton.None).GetValueName();
            string hoveredObject = UICamera.hoveredObject.name;
            string camera = UICamera.currentCamera.name;
            string screenPosition = UICamera.lastTouchPosition.ToString();
            UICamera.lastHit = new RaycastHit();    // clears any gameobject that was hit. Otherwise it is cached until the next hit
            string msg = @"{0}.{1}({2}) event. MouseButton = {3}, HoveredObject = {4}, Camera = {5}, ScreenPosition = {6}."
                .Inject(this.GetType().Name, stackFrame.GetMethod().Name, arg, touchID, hoveredObject, camera, screenPosition);
            Debug.Log(msg);
        }
    }

    // Note: No need to unsubscribe the UICamera events as GameInput is never deleted during runtime

    public override string ToString() {
        return new ObjectAnalyzer().ToString(this);
    }

    #region Nested classes

    public struct ScrollEvent : IEquatable<ScrollEvent> {

        #region Comparison Operators Override

        // see C# 4.0 In a Nutshell, page 254

        public static bool operator ==(ScrollEvent left, ScrollEvent right) {
            return left.Equals(right);
        }

        public static bool operator !=(ScrollEvent left, ScrollEvent right) {
            return !left.Equals(right);
        }

        #endregion

        private static string _toStringFormat = "Target: {0}, Delta: {1}, HitPoint: {2}";

        public readonly ICameraTargetable target;
        public readonly float delta;
        public readonly Vector3 hitPoint;

        public ScrollEvent(ICameraTargetable target, float delta, Vector3 hitPoint) {
            this.target = target;
            this.delta = delta;
            this.hitPoint = hitPoint;
        }

        #region Object.Equals and GetHashCode Override

        public override bool Equals(object obj) {
            if (!(obj is ScrollEvent)) { return false; }
            return Equals((ScrollEvent)obj);
        }

        /// <summary>
        /// Returns a hash code for this instance.
        /// See "Page 254, C# 4.0 in a Nutshell."
        /// </summary>
        /// <returns>
        /// A hash code for this instance, suitable for use in hashing algorithms and data structures like a hash table. 
        /// </returns>
        public override int GetHashCode() {
            int hash = 17;  // 17 = some prime number
            hash = hash * 31 + target.GetHashCode(); // 31 = another prime number
            hash = hash * 31 + delta.GetHashCode();
            hash = hash * 31 + hitPoint.GetHashCode();
            return hash;
        }

        #endregion

        public override string ToString() {
            return _toStringFormat.Inject(target, delta, hitPoint);
        }

        #region IEquatable<ScrollEvent> Members

        public bool Equals(ScrollEvent other) {
            return target == other.target && delta == other.delta && hitPoint == other.hitPoint;
        }

        #endregion

    }

    #endregion

}



