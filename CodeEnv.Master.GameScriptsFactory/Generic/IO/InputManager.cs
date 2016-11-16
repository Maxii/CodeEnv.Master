// --------------------------------------------------------------------------------------------------------------------
// <copyright>
// Copyright © 2012 - 2014 Strategic Forge
//
// Email: jim@strategicforge.com
// </copyright> 
// <summary> 
// File: InputManager.cs
// Singleton that manages all mouse, key and screen edge user input allowed in the game.
// </summary> 
// -------------------------------------------------------------------------------------------------------------------- 

#define DEBUG_LOG
#define DEBUG_WARN
#define DEBUG_ERROR

// default namespace

using System;
using System.Linq;
using CodeEnv.Master.Common;
using CodeEnv.Master.Common.LocalResources;
using CodeEnv.Master.GameContent;
using UnityEngine;

/// <summary>
/// Singleton that manages all mouse, key and screen edge user input allowed in the game.
/// Mouse events originate from the Ngui event system. Key and screen edge events originate from
/// this Manager with support from Unity's Input class.
/// </summary>
public class InputManager : AMonoSingleton<InputManager>, IInputManager {

    /// <summary>
    /// The layers the UI EventDispatcher (2D) is allowed to 'see' when determining whether to raise an event.
    /// This covers the 'normal' game play case where the fixed UI elements should receive events.
    /// </summary>
    public static LayerMask UIEventDispatcherMask_NormalInput { get { return _uiEventDispatcherMask_NormalInput; } }
    private static LayerMask _uiEventDispatcherMask_NormalInput = LayerMaskUtility.CreateInclusiveMask(Layers.UI);

    /// <summary>
    /// The layers the World EventDispatcher (3D) is allowed to 'see' when determining whether to raise an event.
    /// This covers the 'normal' game play case where all world 3D objects in the scene should receive events.
    /// OPTIMIZE For now I'm keeping all CullingLayers receiving events although I don't think they need too.
    /// </summary>
    public static LayerMask WorldEventDispatcherMask_NormalInput { get { return _worldEventDispatcherMask_NormalInput; } }
    private static LayerMask _worldEventDispatcherMask_NormalInput = LayerMaskUtility.CreateExclusiveMask(
    Layers.DeepSpace, Layers.UI, Layers.AvoidableObstacleZone, Layers.CollisionDetectionZone, Layers.Shields, Layers.IgnoreRaycast,
    Layers.Water, Layers.Collide_DefaultOnly, Layers.Collide_ProjectileOnly);

    /// <summary>
    /// The EventDispatcher (World or UI) mask that does not allow any events to be raised.
    /// </summary>
    public static LayerMask EventDispatcherMask_NoInput { get { return (LayerMask)Constants.Zero; } }

    private GameInputMode _inputMode;
    /// <summary>
    /// The InputMode the game is currently operating in.
    /// </summary>
    public GameInputMode InputMode {
        get { return _inputMode; }
        set { SetProperty<GameInputMode>(ref _inputMode, value, "InputMode", InputModePropChangedHandler); }
    }

    /// <summary>
    /// The event dispatcher that sends events to UI objects.
    /// WARNING: This value is purposely null during scene transitions as otherwise,
    /// the instance provided would be from the previous scene with no warnings of such.
    /// </summary>
    public UICamera UIEventDispatcher { get; private set; }

    /// <summary>
    /// The 2 event dispatchers that send events to world objects.
    /// WARNING: This value is purposely null during scene transitions as otherwise,
    /// the instance provided would be from the previous scene with no warnings of such.
    /// </summary>
    public UICamera[] WorldEventDispatchers { get; private set; }

    public override bool IsPersistentAcrossScenes { get { return true; } }

    private bool _enableScreenEdge;
    private bool _enableArrowKeys;

    private GameInputHelper _inputHelper;
    private PlayerViews _playerViews;
    private GameManager _gameMgr;

    #region Initialization

    protected override void InitializeOnInstance() {
        base.InitializeOnInstance();
        References.InputManager = Instance;
    }

    protected override void InitializeOnAwake() {
        base.InitializeOnAwake();
        InitializeLocalReferencesAndValues();
        InitializeNonpersistentReferences();
        InputMode = GameInputMode.NoInput;
        Subscribe();
    }

    private void InitializeLocalReferencesAndValues() {
        _inputHelper = GameInputHelper.Instance;
        _gameMgr = GameManager.Instance;
    }

    private void InitializeNonpersistentReferences() {
        //D.Log("{0}_{1} is [re]initializing non-persistent references.", GetType().Name, InstanceCount);
        InitializeUIEventDispatcher();
        if (_gameMgr.CurrentSceneID == GameManager.SceneID.GameScene) {
            _playerViews = PlayerViews.Instance;
            InitializeWorldEventDispatchers();
        }
    }

    private void InitializeUIEventDispatcher() {
        UIEventDispatcher = UICamera.first;

        //UIEventDispatcher.eventType = UICamera.EventType.UI_2D;
        UIEventDispatcher.eventType = UICamera.EventType.UI_3D;

        UIEventDispatcher.useKeyboard = true;
        UIEventDispatcher.useMouse = true;
        UIEventDispatcher.eventsGoToColliders = true;
    }

    private void InitializeWorldEventDispatchers() {
        //D.Log("{0}_{1} is [re]initializing WorldEventDispatchers.", GetType().Name, InstanceCount);
        WorldEventDispatchers = MainCameraControl.Instance.gameObject.GetComponentsInChildren<UICamera>();
        WorldEventDispatchers.ForAll(wed => {
            wed.eventType = UICamera.EventType.World_3D;
            wed.useKeyboard = true;
            wed.useMouse = true;
            wed.eventsGoToColliders = true;
        });
        /*
                  * Ngui 3.7.1 workaround that makes sure all UICamera event delegates are raised.
                  * Note: 3.7.1 introduced these event delegates, deprecating genericEventHandler. Unfortunately, the delegate
                  * is only fired if there is a 'visible' gameObject (Camera's cullingMask and UICamera's eventRcvrMask don't hide it, 
                  * and the gameObject has an enabled Collider) underneath the cursor. CameraControl (and others) rely on GameInput 
                  * hearing about all events (currently onScroll, onPress, onDragStart, onDrag, onDragEnd). Setting the fallThrough field to any 
                  * gameObject makes sure all event delegates are raised, bypassing the mask and collider requirement. The fallThrough 
                  * gameObject is used in place of a 'visible' gameObject if none is found.
                  * Warning: the gameObject used should not have methods for handling UICamera delegates as SendMessage finds them
                  * and generates an error that the method found does not have the correct number of parameters!
                  */
        UICamera.fallThrough = MainCameraControl.Instance.gameObject;
    }

    private void Subscribe() {
        _gameMgr.gameStateChanging += GameStateChangingEventHandler;
        _gameMgr.gameStateChanged += GameStateChangedEventHandler;
        _gameMgr.sceneLoading += SceneLoadingEventHandler;
        _gameMgr.sceneLoaded += SceneLoadedEventHandler;
    }

    /// <summary>
    /// Subscribes to world mouse events. 
    /// Note: Subscribing (and unsubscribing) to these mouse events via their delegate is reqd to 
    /// control which events occur as changing the UICamera.eventRcvrMask does not affect the delegate
    /// being fired. The assignment of the fall-through gameObject makes sure the delegate fires whether the
    /// UICamera sees a gameObject or not.
    /// </summary>
    private void SubscribeToWorldMouseEvents() {
        UICamera.onScroll += ScrollEventHandler;
        UICamera.onDragStart += DragStartEventHandler;
        UICamera.onDrag += DraggingEventHandler;
        UICamera.onDragEnd += DragEndEventHandler;
        UICamera.onPress += PressEventHandler;
    }

    #endregion

    #region Event and Property Change Handlers

    private void SceneLoadingEventHandler(object sender, EventArgs e) {
        InvalidateNonpersistentReferences();
    }

    private void SceneLoadedEventHandler(object sender, EventArgs e) {
        InitializeNonpersistentReferences();
    }

    private void GameStateChangingEventHandler(object sender, EventArgs e) {
        var previousState = _gameMgr.CurrentState;
        //D.Log("{0}_{1} received a gameStateChanging event. Previous GameState = {2}.", GetType().Name, InstanceCount, previousState.GetValueName());
        if (previousState == GameState.Lobby || previousState == GameState.Running) {
            InputMode = GameInputMode.NoInput;
        }
    }

    private void GameStateChangedEventHandler(object sender, EventArgs e) {
        var gameState = _gameMgr.CurrentState;
        //D.Log("{0}_{1} received a gameStateChanged event. New GameState = {2}.", GetType().Name, InstanceCount, gameState.GetValueName());
        if (gameState == GameState.Lobby) {
            InputMode = GameInputMode.Lobby;
        }
        if (gameState == GameState.Running) {
            InputMode = GameInputMode.Normal;
        }
    }

    /// <summary>
    /// Called when the InputMode changes.
    /// Notes: [Un]subscribing to world mouse events covers camera movement from dragging and scrolling, 
    /// and pressing on empty space (for SelectionManager). Camera movement from arrow keys and the screen edge 
    /// are covered by their specific bool as is other key detection used by PlayerViews. Changing the eventReceiverMask 
    /// of the _worldEventDispatchers covers all OnHover, OnClick, OnDoubleClick, PressEventHandler events embedded 
    /// in world objects. Changing the eventReceiverMask of the _uiEventDispatcher covers all OnHover, 
    /// OnTooltip, OnClick, DraggingEventHandler and PressEventHandler events for UI elements.
    /// </summary>
    /// <exception cref="System.NotImplementedException"></exception>
    private void InputModePropChangedHandler() {
        D.Log("{0}_{1}.{2} is now {3}.", GetType().Name, InstanceCount, typeof(GameInputMode).Name, InputMode.GetValueName());
        __ValidateEventDispatchersNotDestroyed();

        switch (InputMode) {
            case GameInputMode.NoInput:
                UIEventDispatcher.eventReceiverMask = EventDispatcherMask_NoInput;
                _enableArrowKeys = false;
                _enableScreenEdge = false;
                UnsubscribeToViewModeKeyEvents();
                if (_gameMgr.CurrentSceneID == GameManager.SceneID.GameScene) {
                    WorldEventDispatchers.ForAll(wed => wed.eventReceiverMask = EventDispatcherMask_NoInput);
                    UnsubscribeToWorldMouseEvents();
                }
                break;
            case GameInputMode.Lobby:
                //D.Assert(_gameMgr.CurrentScene == _gameMgr.LobbyScene);   // fails during Startup simulation
                UIEventDispatcher.eventReceiverMask = UIEventDispatcherMask_NormalInput;
                _enableArrowKeys = false;
                _enableScreenEdge = false;
                UnsubscribeToViewModeKeyEvents();
                break;
            case GameInputMode.PartialPopup:
                D.AssertEqual(GameManager.SceneID.GameScene, _gameMgr.CurrentSceneID);
                UIEventDispatcher.eventReceiverMask = UIEventDispatcherMask_NormalInput;
                _enableArrowKeys = true;
                _enableScreenEdge = true;
                UnsubscribeToViewModeKeyEvents();
                WorldEventDispatchers.ForAll(wed => wed.eventReceiverMask = EventDispatcherMask_NoInput);
                UnsubscribeToWorldMouseEvents();
                break;
            case GameInputMode.FullPopup:
                D.AssertEqual(GameManager.SceneID.GameScene, _gameMgr.CurrentSceneID);
                UIEventDispatcher.eventReceiverMask = UIEventDispatcherMask_NormalInput;
                _enableArrowKeys = false;
                _enableScreenEdge = false;
                UnsubscribeToViewModeKeyEvents();
                WorldEventDispatchers.ForAll(wed => wed.eventReceiverMask = EventDispatcherMask_NoInput);
                UnsubscribeToWorldMouseEvents();
                break;
            case GameInputMode.Normal:
                D.AssertEqual(GameManager.SceneID.GameScene, _gameMgr.CurrentSceneID);
                UIEventDispatcher.eventReceiverMask = UIEventDispatcherMask_NormalInput;
                _enableArrowKeys = true;
                _enableScreenEdge = true;
                SubscribeToViewModeKeyEvents();
                WorldEventDispatchers.ForAll(wed => wed.eventReceiverMask = WorldEventDispatcherMask_NormalInput);
                SubscribeToWorldMouseEvents();
                break;
            case GameInputMode.None:
            default:
                throw new NotImplementedException(ErrorMessages.UnanticipatedSwitchValue.Inject(InputMode));
        }
    }

    void Update() {
        CheckForArrowKeyActivity();
        CheckForScreenEdgeActivity();
    }

    #endregion

    #region ScrollWheel Events

    private void ScrollEventHandler(GameObject go, float delta) {
        if (UICamera.isOverUI) {
            //D.Log("Scroll using GameObject {0} detected over UI.", go.name);
            return;
        }
        if (_inputHelper.IsAnyMouseButtonDown) {
            return;
        }
        LogEventArg(go.name + Constants.Space + delta);
        ICameraTargetable target = null;
        Vector3 hitPoint = Vector3.zero;
        if (go != UICamera.fallThrough) {
            // scroll event hit something so check in self and parents as some colliders are located on a child mesh or sprite
            target = go.GetComponentInParent<ICameraTargetable>();

            /****************************************************************************************************************
                         * UNDONE Below is code that makes a Sector the ICameraTargetable target (if so designated) using the collider of the
                         * SectorExaminer. This allows a slow approach to the sector highlighted by the SectorExaminer. Without this, the
                         * Zoom target is the DummyTarget which might make the approach much too fast. For now, Sectors are not
                         * ICameraTargetable. Going forward, if I want to be able to approach a sector slowly with the camera, I should 
                         * consider making SectorExaminer : ICameraTargetable. Also, what about being able to focus on a Sector?
                          *****************************************************************************************************************/
            //if (target == null && _playerViews.ViewMode == PlayerViewMode.SectorView) {
            //    var sectorExaminer = go.GetComponent<SectorExaminer>();
            //    if (sectorExaminer != null) {
            //        target = SectorGrid.Instance.GetSector(sectorExaminer.CurrentSectorIndex);
            //    }
            //}
        }

        if (target != null) {
            // scroll event target is an ICameraTargetable
            if (!target.IsCameraTargetEligible) {
                // Note: This is OK, albeit infrequent. Stars, Planetoids, Ships and Facilities all keep their collider on, even when not discernible. 
                // All can lose discernibility <- distance culling
                D.Log("InEligible {0} {1} found while scrolling.", typeof(ICameraTargetable).Name, target.transform.name);
                return;
            }
            hitPoint = (target is IZoomToFurthest) ? UICamera.lastHit.point : target.transform.position;
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

    private void DragStartEventHandler(GameObject go) {
        if (UICamera.isOverUI) {
            //D.Log("DragStartEventHandler using GameObject {0} detected over UI.", go.name);
            return;
        }
        LogEventArg(go.name);
        // turn off the click notification that would normally occur once the drag is complete
        UICamera.currentTouch.clickNotification = UICamera.ClickNotification.None;
        IsDragging = true;
    }

    private void DraggingEventHandler(GameObject go, Vector2 delta) {
        if (UICamera.isOverUI) {
            //D.Log("DraggingEventHandler using GameObject {0} detected over UI.", go.name);
            return;
        }
        LogEventArg(go.name + Constants.Space + delta);
        _dragDelta += delta;
        IsDragValueWaiting = true;
    }

    private void DragEndEventHandler(GameObject go) {
        if (UICamera.isOverUI) {
            //D.Log("DragEndEventHandler using GameObject {0} detected over UI.", go.name);
            return;
        }
        LogEventArg(go.name);
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
    // while in the pop up inputMode. The upshot is that the SelectionManager will not lose its selection when
    // randomly clicking on open space to get out of a context menu. Using onClick didn't work as the onClick
    // delegate isn't fired until the completion of the click action, which is way after the input mode is changed
    // back to normal, thereby firing onUnconsumedClick which undesirably clears the SelectionManager.

    /// <summary>
    /// Occurs when a mouse button is pressed down, but not over a gameObject.
    /// This event does not fire when the button is released.
    /// </summary>
    public event EventHandler unconsumedPress;
    //public event Action<NguiMouseButton> onUnconsumedPressDown;

    private void PressEventHandler(GameObject go, bool isDown) {
        if (UICamera.isOverUI) {
            //D.Log("PressEventHandler({0}) detected over UI.", go.name);
            return;
        }
        //WriteMessage(go.name);    // FIXME: UICamera.onPress bug? go can be null, posted on Ngui support 11/7/14

        if (isDown && UICamera.fallThrough == go) {
            // the target of the press is the fallThrough event handler, so this press wasn't consumed by another gameObject
            //OnUnconsumedPress(isDown);
            OnUnconsumedPress();
        }
    }

    /// <summary>
    /// Called when a mouse button press event occurs but no collider consumes it.
    /// </summary>
    private void OnUnconsumedPress() {
        if (unconsumedPress != null) {
            unconsumedPress(this, EventArgs.Empty);
        }
    }

    #endregion

    #region ScreenEdge Events

    /// <summary>
    /// The depth in pixels at the edge of the screen that qualifies for edge movement.
    /// </summary>
    [Range(0, 20)]
    [Tooltip("Active edge of screen in pixels.")]
    public int activeScreenEdgeDepth = 5;

    private void CheckForScreenEdgeActivity() {
        if (_enableScreenEdge) {
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

    // Beginning with Ngui 3.9, all KeyCodes are now supported by the onKey delegate.
    // However, the onKey delegate still only fires on individual presses and doesn't recognize being held down.
    // Accordingly, I can use it for ViewModeKey detection but not for ArrowKeys whose typical use case is holding down.

    private KeyCode[] _arrowKeyCodes = new KeyCode[] { KeyCode.RightArrow, KeyCode.LeftArrow, KeyCode.UpArrow, KeyCode.DownArrow };
    private bool _isHorizontalAxisEventWaiting;
    private bool _isVerticalAxisEventWaiting;

    private void CheckForArrowKeyActivity() {
        if (_enableArrowKeys) {
            if (_inputHelper.IsAnyMouseButtonDown) { return; }

            if (_inputHelper.IsAnyKeyOrMouseButtonDown) {
                KeyCode keyPressed;
                if (_inputHelper.TryIsKeyHeldDown(out keyPressed, _arrowKeyCodes)) {
                    RecordArrowKeyEvent(keyPressed);
                }
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

    // Beginning with Ngui 3.9, all KeyCodes are now supported by the onKey delegate.
    // However, the onKey delegate still only fires on individual presses and doesn't recognize being held down.
    // Accordingly, I can use it for ViewModeKey detection but not for ArrowKeys whose typical use case is holding down.

    private void SubscribeToViewModeKeyEvents() {
        UICamera.onKey += KeyEventHandler;
    }

    private void UnsubscribeToViewModeKeyEvents() {
        UICamera.onKey -= KeyEventHandler;
    }

    private void KeyEventHandler(GameObject go, KeyCode key) {
        if (_playerViews.ViewModeKeyCodes.Contains(key)) {
            _playerViews.HandleViewModeKeyPressed(key);
        }
    }

    #endregion

    #region Cleanup

    protected override void Cleanup() {
        References.InputManager = null;
        InvalidateNonpersistentReferences();
        Unsubscribe();
    }

    private void InvalidateNonpersistentReferences() {
        UIEventDispatcher = null;
        if (_gameMgr.CurrentSceneID == GameManager.SceneID.GameScene) {
            if (!IsApplicationQuiting) { // GameManager.DisposeOfGlobals() handles this when Quiting
                _playerViews.Dispose();
            }
            _playerViews = null;
            WorldEventDispatchers.ForAll(wed => wed = null);    // UNCLEAR is this needed with the collection set to null?
            WorldEventDispatchers = null;
        }
    }

    private void Unsubscribe() {
        _gameMgr.gameStateChanging -= GameStateChangingEventHandler;
        _gameMgr.gameStateChanged -= GameStateChangedEventHandler;
        _gameMgr.sceneLoading -= SceneLoadingEventHandler;
        _gameMgr.sceneLoaded -= SceneLoadedEventHandler;
        UnsubscribeToWorldMouseEvents();
    }

    /// <summary>
    /// Unsubscribes to world mouse events. 
    /// Note: Subscribing (and unsubscribing) to these mouse events via their delegate is reqd to 
    /// control which events occur as changing the UICamera.eventRcvrMask does not affect the delegate
    /// being fired. The assignment of the fall-through gameObject makes sure the delegate fires whether the
    /// UICamera sees a gameObject or not.
    /// </summary>
    private void UnsubscribeToWorldMouseEvents() {
        UICamera.onScroll -= ScrollEventHandler;
        UICamera.onDragStart -= DragStartEventHandler;
        UICamera.onDrag -= DraggingEventHandler;
        UICamera.onDragEnd -= DragEndEventHandler;
        UICamera.onPress -= PressEventHandler;
    }

    #endregion

    public override string ToString() {
        return new ObjectAnalyzer().ToString(this);
    }

    #region Debug

    private void LogEventArg(string arg = "") {
        if (_debugSettings.EnableEventLogging) {
            var stackFrame = new System.Diagnostics.StackFrame(1);
            NguiMouseButton? button = Enums<NguiMouseButton>.CastOrNull(UICamera.currentTouchID);
            string touchID = (button ?? NguiMouseButton.None).GetValueName();
            string hoveredObject = UICamera.hoveredObject.name;
            string camera = UICamera.currentCamera.name;
            string screenPosition = UICamera.lastEventPosition.ToString();  // UICamera.lastTouchPosition.ToString();
            UICamera.lastHit = new RaycastHit();    // clears any gameObject that was hit. Otherwise it is cached until the next hit
            string msg = @"{0}.{1}({2}) event. MouseButton = {3}, HoveredObject = {4}, Camera = {5}, ScreenPosition = {6}."
                .Inject(this.GetType().Name, stackFrame.GetMethod().Name, arg, touchID, hoveredObject, camera, screenPosition);
            Debug.Log(msg);
        }
    }

    /// <summary>
    ///Validates the 3 event dispatcher (aka UICamera) instances have not been destroyed.
    ///This is accomplished by trying to access their gameObject. If they are destroyed, Unity will throw an error.
    /// </summary>
    private void __ValidateEventDispatchersNotDestroyed() {
        if (_gameMgr.CurrentSceneID == GameManager.SceneID.GameScene) {
#pragma warning disable 0168
            var dummy = UIEventDispatcher.gameObject;
            WorldEventDispatchers.ForAll(wed => dummy = wed.gameObject);
            //D.Log("{0} has validated all EventDispatchers are alive.", GetType().Name);
#pragma warning restore 0168
        }
    }

    #endregion

    #region Nested classes

    public enum ActiveScreenEdge {
        None,

        Left,
        Right,
        Top,
        Bottom
    }

    /// <summary>
    /// Simple container for a scroll event so it can be retrieved when desired by MainCameraControl.
    /// Note: Changed from a struct as per Jon Skeet: mutable references in a immutable struct are sneakily evil, aka ICameraTargetable
    /// </summary>
    public class ScrollEvent {

        private static string _toStringFormat = "Target: {0}, Delta: {1}, HitPoint: {2}";

        public readonly ICameraTargetable target;
        public readonly float delta;
        public readonly Vector3 hitPoint;

        public ScrollEvent(ICameraTargetable target, float delta, Vector3 hitPoint) {
            this.target = target;
            this.delta = delta;
            this.hitPoint = hitPoint;
        }

        public override string ToString() {
            return _toStringFormat.Inject(target, delta, hitPoint);
        }

    }

    #endregion

    #region Pressed Archive

    // Previously used by SectorExaminer to open a contextMenu without using a collider.
    // This works but I replaced the Wireframe Mouse hot spot with a small collider instead
    // as the same approach using onUnconsumedHover was unreliable due to the way Ngui
    // spam onHover events

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
    //        // the target of the click is the fallThrough event handler, so this click wasn't consumed by another gameObject
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

    //private void ScrollEventHandler(GameObject go, float delta) {
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

