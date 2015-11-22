// --------------------------------------------------------------------------------------------------------------------
// <copyright>
// Copyright © 2012 - 2014 Strategic Forge
//
// Email: jim@strategicforge.com
// </copyright> 
// <summary> 
// File: GameInputManager.cs
// Singleton that manages all mouse, key and screen edge user input allowed in the game scene.
// </summary> 
// -------------------------------------------------------------------------------------------------------------------- 

#define DEBUG_LOG
#define DEBUG_WARN
#define DEBUG_ERROR

// default namespace

using System;
using System.Collections.Generic;
using CodeEnv.Master.Common;
using CodeEnv.Master.Common.LocalResources;
using CodeEnv.Master.GameContent;
using UnityEngine;

/// <summary>
/// Singleton that manages all mouse, key and screen edge user input allowed in the game scene.
/// Mouse events originate from the Ngui event system. Key and screen edge events originate from
/// this Manager with support from Unity's Input class.
/// </summary>
[Obsolete]
public class GameInputManager : AInputManager<GameInputManager>, IInputManager {

    /// <summary>
    /// The layers the UI EventDispatcher (2D) is allowed to 'see' when determining whether to raise an event.
    /// This covers the 'normal' game play case where the fixed UI elements should receive events.
    /// </summary>
    public static LayerMask UIEventDispatcherMask_NormalInput { get { return _uiEventDispatcherMask_NormalInput; } }
    private static LayerMask _uiEventDispatcherMask_NormalInput = LayerMaskExtensions.CreateInclusiveMask(Layers.UI);

    /// <summary>
    /// The layers the World EventDispatcher (3D) is allowed to 'see' when determining whether to raise an event.
    /// This covers the 'normal' game play case where all world 3D objects in the scene should receive events.
    /// </summary>
    public static LayerMask WorldEventDispatcherMask_NormalInput { get { return _worldEventDispatcherMask_NormalInput; } }
    private static LayerMask _worldEventDispatcherMask_NormalInput = LayerMaskExtensions.CreateExclusiveMask(Layers.UniverseEdge,
        Layers.DeepSpace, Layers.UI, Layers.UIPopup, Layers.Vectrosity2D, Layers.ShipTransitBan, Layers.IgnoreRaycast);

    /// <summary>
    /// The EventDispatcher (World or UI) mask that does not allow any events to be raised.
    /// </summary>
    public static LayerMask EventDispatcherMask_NoInput { get { return (LayerMask)Constants.Zero; } }

    /// <summary>
    /// The event dispatcher that sends events to world objects.
    /// WARNING: This value is purposely null during scene transitions as otherwise,
    /// the instance provided would be from the previous scene with no warnings of such.
    /// </summary>
    public UICamera WorldEventDispatcher { get; private set; }

    protected override bool IsPersistentAcrossScenes { get { return true; } }

    private GameInputHelper _inputHelper;
    private PlayerViews _playerViews;
    private GameManager _gameMgr;
    private IList<IDisposable> _subscribers;

    #region Initialization

    protected override void InitializeOnInstance() {
        base.InitializeOnInstance();
        References.InputManager = Instance;
        _inputHelper = GameInputHelper.Instance;
        _playerViews = PlayerViews.Instance;
        _gameMgr = GameManager.Instance;
    }

    protected override void InitializeOnAwake() {
        base.InitializeOnAwake();
        InitializeWorldEventDispatcher();
        InputMode = GameInputMode.NoInput;
        Subscribe();
    }

    protected override void InitializeUIEventDispatcher() {
        base.InitializeUIEventDispatcher();
        /*
                  * Ngui 3.7.1 workaround that makes sure all UICamera event delegates are raised.
                  * Note: 3.7.1 introduced these event delegates, deprecating genericEventHandler. Unfortunately, the delegate
                  * is only fired if there is a 'visible' gameObject (Camera's cullingMask and UICamera's eventRcvrMask don't hide it, 
                  * and the gameObject has an enabled Collider) underneath the cursor. CameraControl (and others) rely on GameInput 
                  * hearing about all events (currently onScroll, onPress, onDragStart, onDrag, onDragEnd). Setting the fallThrough field to any 
                  * gameObject makes sure all event delegates are raised, bypassing the mask and collider requirement. The fallThrough 
                  * gameObject is used in place of a 'visible' gameObject if none is found.
                  * Warning: the gameobject used should not have methods for handling UICamera delegates as SendMessage finds them
                  * and generates an error that the method found does not have the correct number of parameters!
                  */
        UICamera.fallThrough = MainCameraControl.Instance.gameObject;
    }

    private void InitializeWorldEventDispatcher() {
        WorldEventDispatcher = MainCameraControl.Instance.gameObject.GetSafeFirstMonoBehaviourInChildren<UICamera>();
        WorldEventDispatcher.eventType = UICamera.EventType.World_3D;
        WorldEventDispatcher.useKeyboard = true;
        WorldEventDispatcher.useMouse = true;
        WorldEventDispatcher.eventsGoToColliders = true;
    }

    private void Subscribe() {
        _subscribers = new List<IDisposable>();
        _subscribers.Add(_gameMgr.SubscribeToPropertyChanging<GameManager, GameState>(gm => gm.CurrentState, OnGameStateChanging));
        _subscribers.Add(_gameMgr.SubscribeToPropertyChanged<GameManager, GameState>(gm => gm.CurrentState, OnGameStateChanged));
        _gameMgr.onSceneLoading += OnSceneLoading;
        _gameMgr.onSceneLoaded += OnSceneLoaded;
    }


    private void ReinitializeEventDispatchers() {
        D.Log("{0} is reinitializing the new EventDispatcher instances.", GetType().Name);
        InitializeUIEventDispatcher();
        InitializeWorldEventDispatcher();
    }

    #endregion

    private void OnSceneLoading(SceneLevel newScene) {
        InvalidateEventDispatchers();
    }

    private void OnSceneLoaded() {
        ReinitializeEventDispatchers();
    }

    private void OnGameStateChanging(GameState incomingState) {
        var previousState = GameManager.Instance.CurrentState;
        if (previousState == GameState.Lobby) {
            // TODO GameInputManager, which exists only in the GameScene receives this Lobby state change from the startup simulation
            //D.Warn("{0} received a GameState exit event from {1}.", GetType().Name, previousState.GetName());
            InputMode = GameInputMode.NoInput;
        }
        if (previousState == GameState.Running) {
            InputMode = GameInputMode.NoInput;
        }
    }

    private void OnGameStateChanged() {
        var enteringGameState = GameManager.Instance.CurrentState;
        if (enteringGameState == GameState.Running) {
            InputMode = GameInputMode.Normal;
        }
    }

    protected override void Update() {
        base.Update();
        CheckForArrowKeyActivity();
        CheckForScreenEdgeActivity();
        if (InputMode != GameInputMode.PartialPopup) {
            // Update runs during PartialScreenPopup mode as arrow key and screen edge camera movement
            // is desired, but VIewModeKey inputs are not supported during this mode
            CheckForViewModeKeyActivity();
        }
    }

    /// <summary>
    /// Called when the GameInputMode changes.
    /// Notes: [Un]subscribing to world mouse events covers camera movement from dragging and scrolling, and pressing on empty space (for SelectionManager).
    /// Enabling/disabling this script covers camera movement from arrow keys and the screen edge. It also covers other key detection used by PlayerViews.
    /// Changing the eventReceiverMask of the _worldEventDispatcher covers all OnHover, OnClick, OnDoubleClick, OnPress events embedded in world objects.
    /// Changing the eventReceiverMask of the _uiEventDispatcher covers all OnHover, OnClick, OnDrag and OnPress events for UI elements.
    /// </summary>
    /// <exception cref="System.NotImplementedException"></exception>
    protected override void OnInputModeChanged() {
        D.Log("{0}_{1}.{2} is now {3}.", GetType().Name, InstanceCount, typeof(GameInputMode).Name, InputMode.GetValueName());
        __ValidateEventDispatchersNotDestroyed();

        switch (InputMode) {
            case GameInputMode.NoInput:
                UIEventDispatcher.eventReceiverMask = EventDispatcherMask_NoInput;
                WorldEventDispatcher.eventReceiverMask = EventDispatcherMask_NoInput;
                UnsubscribeToWorldMouseEvents();
                enabled = false;
                break;
            case GameInputMode.PartialPopup:
                UIEventDispatcher.eventReceiverMask = UIEventDispatcherMask_PopupInputOnly;
                WorldEventDispatcher.eventReceiverMask = EventDispatcherMask_NoInput;
                UnsubscribeToWorldMouseEvents();
                enabled = true;
                break;
            case GameInputMode.FullPopup:
                UIEventDispatcher.eventReceiverMask = UIEventDispatcherMask_PopupInputOnly;
                WorldEventDispatcher.eventReceiverMask = EventDispatcherMask_NoInput;
                UnsubscribeToWorldMouseEvents();
                enabled = false;
                break;
            case GameInputMode.Normal:
                UIEventDispatcher.eventReceiverMask = UIEventDispatcherMask_NormalInput;
                WorldEventDispatcher.eventReceiverMask = WorldEventDispatcherMask_NormalInput;
                SubscribeToWorldMouseEvents();
                enabled = true;
                break;
            case GameInputMode.None:
            default:
                throw new NotImplementedException(ErrorMessages.UnanticipatedSwitchValue.Inject(InputMode));
        }
    }

    /// <summary>
    /// Subscribes to world mouse events. 
    /// Note: Subscribing (and unsubscribing) to these mouse events via their delegate is req'd to 
    /// control which events occur as changing the UICamera.eventRcvrMask does not affect the delegate
    /// being fired. The assignment of the fallthrough gameObject makes sure the delegate fires whether the
    /// UICamera sees a gameObject or not.
    /// </summary>
    private void SubscribeToWorldMouseEvents() {
        UICamera.onScroll += OnScroll;
        UICamera.onDragStart += OnDragStart;
        UICamera.onDrag += OnDrag;
        UICamera.onDragEnd += OnDragEnd;
        UICamera.onPress += OnPress;
    }

    #region ScrollWheel Events

    private void OnScroll(GameObject go, float delta) {
        if (UICamera.isOverUI) {
            //D.Log("Scroll using GameObject {0} detected over UI.", go.name);
            return;
        }
        if (_inputHelper.IsAnyMouseButtonDown) {
            return;
        }
        WriteMessage(go.name + Constants.Space + delta);
        ICameraTargetable target = null;
        Vector3 hitPoint = Vector3.zero;
        if (go != UICamera.fallThrough) {
            // scroll event hit something so check in self and parents as some colliders are located on a child mesh or sprite
            target = go.GetInterfaceInParents<ICameraTargetable>(excludeSelf: false);

            if (target == null && _playerViews.ViewMode == PlayerViewMode.SectorView) {
                var sectorExaminer = go.GetComponent<SectorExaminer>();
                if (sectorExaminer != null) {
                    target = SectorGrid.Instance.GetSector(sectorExaminer.CurrentSectorIndex);
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

    private void RecordScrollEvent(ScrollEvent scrollEvent) {
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

    #region Dragging Events

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

    #region Pressed Events

    // Note: Changed SelectionManager to use onUnconsumedPressDown rather than onClick as this way
    // an open ContextMenu hides itself (and changes the inputMode back to normal) AFTER the onPress
    // delegate would have been received. The delegate isn't received as there is no subscription active
    // while in the popup inputMode. The upshot is that the SelectionManager will not lose its selection when
    // randomly clicking on open space to get out of a context menu. Using onClick didn't work as the onClick
    // delegate isn't fired until the completion of the click action, which is way after the input mode is changed
    // back to normal, thereby firing onUnconsumedClick which undesireably clears the SelectionManager.

    public event Action<NguiMouseButton> onUnconsumedPressDown;

    private void OnPress(GameObject go, bool isDown) {
        if (UICamera.isOverUI) {
            //D.Log("OnPress({0}) detected over UI.", go.name);
            return;
        }
        //WriteMessage(go.name);    // FIXME: UICamera.onPress bug? go can be null, posted on Ngui support 11/7/14

        if (UICamera.fallThrough == go) {
            // the target of the press is the fallThrough event handler, so this press wasn't consumed by another gameobject
            OnUnconsumedPress(isDown);
        }
    }

    /// <summary>
    /// Called when a mouse button press event occurs but no collider consumes it.
    /// </summary>
    private void OnUnconsumedPress(bool isDown) {
        if (isDown) {
            if (onUnconsumedPressDown != null) {
                onUnconsumedPressDown(_inputHelper.CurrentMouseButton);
            }
        }
    }

    #endregion

    #region ScreenEdge Events

    /// <summary>
    /// The depth in pixels at the edge of the screen that qualifies for edge movement.
    /// </summary>
    public int activeScreenEdgeDepth = 5;

    private void CheckForScreenEdgeActivity() {
        if (_inputHelper.IsAnyKeyOrMouseButtonDown) {
            return;
        }

        var activeEdge = ActiveScreenEdge.None;
        Vector2 mPos = Input.mousePosition;
        if (mPos.x <= activeScreenEdgeDepth) {
            activeEdge = ActiveScreenEdge.Left;
        }
        else if (mPos.x >= Screen.width - activeScreenEdgeDepth) {
            activeEdge = ActiveScreenEdge.Right;
        }
        else if (mPos.y <= activeScreenEdgeDepth) {
            activeEdge = ActiveScreenEdge.Bottom;
        }
        else if (mPos.y >= Screen.height - activeScreenEdgeDepth) {
            activeEdge = ActiveScreenEdge.Top;
        }

        if (activeEdge != ActiveScreenEdge.None) {
            RecordScreenEdgeEvent(activeEdge);
        }
    }

    private ActiveScreenEdge _screenEdgeEvent;

    private bool _isLeftRightScreenEdgeEventWaiting;
    private bool _isTopBottomScreenEdgeEventWaiting;

    public bool IsScreenEdgeEventWaiting(MainCameraControl.ScreenEdgeAxis screenEdgeAxis) {
        switch (screenEdgeAxis) {
            case MainCameraControl.ScreenEdgeAxis.LeftRight:
                return _isLeftRightScreenEdgeEventWaiting;
            case MainCameraControl.ScreenEdgeAxis.TopBottom:
                return _isTopBottomScreenEdgeEventWaiting;
            case MainCameraControl.ScreenEdgeAxis.None:
            default:
                throw new NotImplementedException(ErrorMessages.UnanticipatedSwitchValue.Inject(screenEdgeAxis));
        }
    }

    private void RecordScreenEdgeEvent(ActiveScreenEdge activeEdge) {
        switch (activeEdge) {
            case ActiveScreenEdge.Left:
            case ActiveScreenEdge.Right:
                _isLeftRightScreenEdgeEventWaiting = true;
                break;
            case ActiveScreenEdge.Top:
            case ActiveScreenEdge.Bottom:
                _isTopBottomScreenEdgeEventWaiting = true;
                break;
            case ActiveScreenEdge.None:
            default:
                throw new NotImplementedException(ErrorMessages.UnanticipatedSwitchValue.Inject(activeEdge));
        }
        _screenEdgeEvent = activeEdge;
    }

    public ActiveScreenEdge GetScreenEdgeEvent(MainCameraControl.ScreenEdgeAxis screenEdgeAxis) {
        switch (screenEdgeAxis) {
            case MainCameraControl.ScreenEdgeAxis.LeftRight:
                _isLeftRightScreenEdgeEventWaiting = false;
                break;
            case MainCameraControl.ScreenEdgeAxis.TopBottom:
                _isTopBottomScreenEdgeEventWaiting = false;
                break;
            case MainCameraControl.ScreenEdgeAxis.None:
            default:
                throw new NotImplementedException(ErrorMessages.UnanticipatedSwitchValue.Inject(screenEdgeAxis));
        }
        var screenEdge = _screenEdgeEvent;
        _screenEdgeEvent = ActiveScreenEdge.None;
        return screenEdge;
    }

    #endregion

    #region ArrowKey Events

    private KeyCode[] _arrowKeyCodesToSearch = new KeyCode[] { KeyCode.RightArrow, KeyCode.LeftArrow, KeyCode.UpArrow, KeyCode.DownArrow };
    private bool _isHorizontalAxisEventWaiting;
    private bool _isVerticalAxisEventWaiting;

    private void CheckForArrowKeyActivity() {
        if (_inputHelper.IsAnyMouseButtonDown) { return; }

        if (_inputHelper.IsAnyKeyOrMouseButtonDown) {
            KeyCode keyPressed;
            if (_inputHelper.TryIsKeyHeldDown(out keyPressed, _arrowKeyCodesToSearch)) {
                RecordArrowKeyEvent(keyPressed);
            }
        }
    }

    public bool IsArrowKeyEventWaiting(MainCameraControl.KeyboardAxis axis) {
        switch (axis) {
            case MainCameraControl.KeyboardAxis.Horizontal:
                return _isHorizontalAxisEventWaiting;
            case MainCameraControl.KeyboardAxis.Vertical:
                return _isVerticalAxisEventWaiting;
            case MainCameraControl.KeyboardAxis.None:
            default:
                throw new NotImplementedException(ErrorMessages.UnanticipatedSwitchValue.Inject(axis));
        }
    }

    private void RecordArrowKeyEvent(KeyCode arrowKeyCode) {
        switch (arrowKeyCode) {
            case KeyCode.LeftArrow:
            case KeyCode.RightArrow:
                _isHorizontalAxisEventWaiting = true;
                break;
            case KeyCode.UpArrow:
            case KeyCode.DownArrow:
                _isVerticalAxisEventWaiting = true;
                break;
            default:
                throw new NotImplementedException(ErrorMessages.UnanticipatedSwitchValue.Inject(arrowKeyCode));
        }
    }

    public float GetArrowKeyEventValue(MainCameraControl.KeyboardAxis axis) {
        switch (axis) {
            case MainCameraControl.KeyboardAxis.Horizontal:
                _isHorizontalAxisEventWaiting = false;
                break;
            case MainCameraControl.KeyboardAxis.Vertical:
                _isVerticalAxisEventWaiting = false;
                break;
            case MainCameraControl.KeyboardAxis.None:
            default:
                throw new NotImplementedException(ErrorMessages.UnanticipatedSwitchValue.Inject(axis));
        }
        return Input.GetAxis(axis.GetValueName());
    }

    #endregion

    #region ViewModeKey Events

    private void CheckForViewModeKeyActivity() {
        if (_inputHelper.IsAnyMouseButtonDown) { return; }

        if (_inputHelper.IsAnyKeyOrMouseButtonDown) {
            // a key has been pressed with no mouseButton also pressed
            KeyCode keyPressed;
            if (_inputHelper.TryIsKeyDown(out keyPressed, _playerViews.ViewModeKeyCodes)) {
                _playerViews.OnViewModeKeyPressed(keyPressed);
            }
        }
    }

    #endregion

    private void WriteMessage(string arg = "") {
        if (DebugSettings.Instance.EnableEventLogging) {
            var stackFrame = new System.Diagnostics.StackFrame(1);
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

    /// <summary>
    ///Validates the two event dispatcher instances we currently have have not been destroyed.
    ///This is accomplished by trying to access their gameObject. If they are destroyed, Unity will 
    ///throw an error.
    /// </summary>
    private void __ValidateEventDispatchersNotDestroyed() {
#pragma warning disable 0168
        var dummy = UIEventDispatcher.gameObject;
        dummy = WorldEventDispatcher.gameObject;
#pragma warning restore 0168
    }

    private void InvalidateEventDispatchers() {
        UIEventDispatcher = null;
        WorldEventDispatcher = null;
    }

    protected override void Cleanup() {
        References.InputManager = null;
        Unsubscribe();
    }

    private void Unsubscribe() {
        _subscribers.ForAll(d => d.Dispose());
        _subscribers.Clear();
        _gameMgr.onSceneLoading -= OnSceneLoading;
        _gameMgr.onSceneLoaded -= OnSceneLoaded;
        UnsubscribeToWorldMouseEvents();
    }

    /// <summary>
    /// Unsubscribes to world mouse events. 
    /// Note: Subscribing (and unsubscribing) to these mouse events via their delegate is req'd to 
    /// control which events occur as changing the UICamera.eventRcvrMask does not affect the delegate
    /// being fired. The assignment of the fallthrough gameObject makes sure the delegate fires whether the
    /// UICamera sees a gameObject or not.
    /// </summary>
    private void UnsubscribeToWorldMouseEvents() {
        UICamera.onScroll -= OnScroll;
        UICamera.onDragStart -= OnDragStart;
        UICamera.onDrag -= OnDrag;
        UICamera.onDragEnd -= OnDragEnd;
        UICamera.onPress -= OnPress;
    }

    public override string ToString() {
        return new ObjectAnalyzer().ToString(this);
    }

    #region Nested classes

    public enum ActiveScreenEdge {

        None,

        Left,

        Right,

        Top,

        Bottom

    }

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

    #region Pressed Archive

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

    #region Clicked Events Archive

    //// used by SelectionManager to clear the Selection when an unconsumed click occurs
    //public event Action<NguiMouseButton> onUnconsumedClick;

    //private void OnClick(GameObject go) {
    //    if (UICamera.isOverUI) {
    //        //D.Log("OnClick({0}) detected over UI.", go.name);
    //        return;
    //    }
    //    //Arguments.ValidateNotNull(go);  // click events should not occur except over a game object
    //    WriteMessage(go.name);

    //    if (UICamera.fallThrough == go) {
    //        // the target of the click is the fallThrough event handler, so this click wasn't consumed by another gameobject
    //        OnUnconsumedClick();
    //    }
    //}

    ///// <summary>
    ///// Called when a mouse button click event occurs but no collider consumes it.
    ///// </summary>
    //private void OnUnconsumedClick() {
    //    if (onUnconsumedClick != null) {
    //        onUnconsumedClick(_inputHelper.CurrentMouseButton);
    //    }
    //}

    #endregion

    #region ScrollWheel Archive

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

    #region Ngui Key Event Archive

    // Ngui KeyEvents are only sent when a GameObject is selected from Ngui's perspective - aka the gameObject has been clicked.
    // The KeyCodes sent are limited to the arrow keys, along with the UICamera-designated submit and cancel keys.
    // As I use arrow keys to move the camera without a requirement for a gameObject to be under the mouse, I can't use this.

    #endregion

}

