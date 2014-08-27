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
public class GameInput : AGenericSingleton<GameInput>, IGameInput, IDisposable {

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

    #region ScrollWheel

    public bool isScrollValueWaiting;
    private float _scrollWheelDelta;

    private void OnScroll(GameObject go, float delta) {
        if (UICamera.isOverUI) { return; }
        Arguments.ValidateNotNull(go);  // scroll events should not occur except over a game object
        WriteMessage(go.name + Constants.Space + delta);
        RecordScrollWheelMovement(delta);
    }

    private void RecordScrollWheelMovement(float delta) {
        _scrollWheelDelta = delta;
        isScrollValueWaiting = true;
    }

    public float GetScrollWheelMovement() {
        if (!isScrollValueWaiting) {
            D.Warn("Mouse ScrollWheel inquiry made with no scroll value waiting.");
        }
        isScrollValueWaiting = false;
        float delta = _scrollWheelDelta;
        _scrollWheelDelta = Constants.ZeroF;
        return delta;
    }

    // Unlike ClearDrag, ClearScrollWheel not needed as all scroll wheel movement
    // events recorded here will always be used. 

    #endregion

    #region Dragging

    public bool IsDragging { get; set; }

    public bool isDragValueWaiting;
    private Vector2 _dragDelta;

    private void OnDragStart(GameObject go) {
        if (UICamera.isOverUI) { return; }
        string msg = go != null ? go.name : "No GameObject";
        WriteMessage(msg);
        // turn off the click notification that would normally occur once the drag is complete
        //UICamera.currentTouch.clickNotification = UICamera.ClickNotification.None;
        IsDragging = true;
    }

    private void OnDrag(GameObject go, Vector2 delta) {
        if (UICamera.isOverUI) { return; }
        string msg = go != null ? go.name + Constants.Space + delta : "No GameObject " + delta.ToString();
        WriteMessage(msg);
        RecordDrag(delta);
    }

    private void OnDragEnd(GameObject go) {
        if (UICamera.isOverUI) { return; }
        string msg = go != null ? go.name : "No GameObject";
        WriteMessage(msg);
        NotifyDragEnded();
    }

    private void RecordDrag(Vector2 delta) {
        _dragDelta = delta;
        isDragValueWaiting = true;
    }

    public Vector2 GetDragDelta() {
        if (!isDragValueWaiting) {
            D.Warn("Drag inquiry made with no delta value waiting.");
        }
        Vector2 delta = _dragDelta;
        ClearDragValue();
        return delta;
    }

    /// <summary>
    /// Tells GameInput that the drag that was occuring has ended.
    /// </summary>
    private void NotifyDragEnded() {
        // the drag has ended so clear any residual drag values that
        // may not have qualified for use by Camera Control due to wrong button, etc.
        ClearDragValue();
        IsDragging = false;
    }

    private void ClearDragValue() {
        _dragDelta = Vector2.zero;
        isDragValueWaiting = false;
    }

    #endregion

    #region Clicked

    // used by SelectionManager to clear the Selection when an unconsumed click occurs
    public event Action<NguiMouseButton> onUnconsumedClick;

    private void OnClick(GameObject go) {
        if (UICamera.isOverUI) { return; }
        Arguments.ValidateNotNull(go);  // click events should not occur except over a game object
        WriteMessage(go.name);

        // FIXME can't derive unconsumed click as onClick events donot occur when not over a game object
        //if (UICamera.hoveredObject == gameObject) {
        //    // the target of the click is this generic event handler, so this click wasn't consumed by another gameobject
        //    OnUnconsumedClick();
        //}
    }

    /// <summary>
    /// Called when a mouse button click event occurs but no collider consumes it.
    /// </summary>
    private void OnUnconsumedClick() {
        if (onUnconsumedClick != null) {
            onUnconsumedClick(References.InputHelper.GetMouseButton());
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


    #region SpecialKeys

    // Ngui KeyEvents didn't work as only one event is sent when keys are held down
    // and even then, only selected keys were included

    // activates ViewMode in PlayerViews
    public event Action<ViewModeKeys> onViewModeKeyPressed;

    public void CheckForKeyActivity() {
        IGameInputHelper inputHelper = References.InputHelper;
        if (inputHelper.IsAnyKeyOrMouseButtonDown()) {
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
        //if (LogEvents) {
        System.Diagnostics.StackFrame stackFrame = new System.Diagnostics.StackTrace().GetFrame(1);
        NguiMouseButton? button = Enums<NguiMouseButton>.CastOrNull(UICamera.currentTouchID);
        string touchID = (button ?? NguiMouseButton.None).GetName();
        string hoveredObject = UICamera.hoveredObject.name;
        string camera = UICamera.currentCamera.name;
        string screenPosition = UICamera.lastTouchPosition.ToString();
        UICamera.lastHit = new RaycastHit();    // clears any gameobject that was hit. Otherwise it is cached until the next hit
        string msg = @"{0}.{1}({2}) event. MouseButton = {3}, HoveredObject = {4}, Camera = {5}, ScreenPosition = {6}."
            .Inject(this.GetType().Name, stackFrame.GetMethod().Name, arg, touchID, hoveredObject, camera, screenPosition);
        Debug.Log(msg);
        //}
    }

    private void Cleanup() {
        Unsubscribe();
    }

    private void Unsubscribe() {
        UICamera.onScroll -= OnScroll;
        UICamera.onDragStart -= OnDragStart;
        UICamera.onDrag -= OnDrag;
        UICamera.onDragEnd -= OnDragEnd;
        UICamera.onClick -= OnClick;
    }

    public override string ToString() {
        return new ObjectAnalyzer().ToString(this);
    }

    #region IDisposable

    [DoNotSerialize]
    private bool _alreadyDisposed = false;
    protected bool _isDisposing = false;

    /// <summary>
    /// Performs application-defined tasks associated with freeing, releasing, or resetting unmanaged resources.
    /// </summary>
    public void Dispose() {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    /// <summary>
    /// Releases unmanaged and - optionally - managed resources. Derived classes that need to perform additional resource cleanup
    /// should override this Dispose(isDisposing) method, using its own alreadyDisposed flag to do it before calling base.Dispose(isDisposing).
    /// </summary>
    /// <param name="isDisposing"><c>true</c> to release both managed and unmanaged resources; <c>false</c> to release only unmanaged resources.</param>
    protected virtual void Dispose(bool isDisposing) {
        // Allows Dispose(isDisposing) to be called more than once
        if (_alreadyDisposed) {
            return;
        }

        _isDisposing = true;
        if (isDisposing) {
            // free managed resources here including unhooking events
            Cleanup();
        }
        // free unmanaged resources here

        _alreadyDisposed = true;
    }

    // Example method showing check for whether the object has been disposed
    //public void ExampleMethod() {
    //    // throw Exception if called on object that is already disposed
    //    if(alreadyDisposed) {
    //        throw new ObjectDisposedException(ErrorMessages.ObjectDisposed);
    //    }

    //    // method content here
    //}
    #endregion

}



