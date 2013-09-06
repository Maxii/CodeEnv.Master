// --------------------------------------------------------------------------------------------------------------------
// <copyright>
// Copyright © 2012 - 2013 Strategic Forge
//
// Email: jim@strategicforge.com
// </copyright> 
// <summary> 
// File: CameraControl.cs
// Singleton. Camera Control based on Ngui's Event System.
// </summary> 
// -------------------------------------------------------------------------------------------------------------------- 

#define DEBUG_WARN
#define DEBUG_ERROR

// default namespace

using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using CodeEnv.Master.Common;
using CodeEnv.Master.Common.LocalResources;
using CodeEnv.Master.Common.Unity;

/// <summary>
/// Singleton. Camera Control based on Ngui's Event System.
/// </summary>
public class CameraControl : AMonoBehaviourBaseSingleton<CameraControl> {

    #region Camera Control Configurations

    // Focused Zooming: When focused, top and bottom Edge zooming and arrow key zooming cause camera movement in and out from the focused object that is centered on the screen. 
    // ScrollWheel zooming normally does the same if the cursor is pointed at the focused object. If the cursor is pointed somewhere else, scrolling IN moves toward the cursor resulting 
    // in a change to Freeform scrolling. By default, Freeform scrolling OUT is directly opposite the camera's facing. However, there is an option to scroll OUT from the cursor instead. 
    // If this is selected, then scrolling OUT while the cursor is not pointed at the focused object will also result in Freeform scrolling.
    public ScreenEdgeConfiguration edgeFocusZoom = new ScreenEdgeConfiguration { sensitivity = 0.03F, activate = false };
    public MouseScrollWheelConfiguration scrollFocusZoom = new MouseScrollWheelConfiguration { sensitivity = 0.4F, activate = true };
    public ArrowKeyboardConfiguration keyFocusZoom = new ArrowKeyboardConfiguration { keyboardAxis = KeyboardAxis.Vertical, sensitivity = .01F, activate = true };
    public SimultaneousMouseButtonConfiguration dragFocusZoom = new SimultaneousMouseButtonConfiguration { firstMouseButton = NguiMouseButton.Left, secondMouseButton = NguiMouseButton.Right, sensitivity = 0.003F, activate = true };

    // Freeform Zooming: When not focused, top and bottom Edge zooming and arrow key zooming cause camera movement forward or backward along the camera's facing.
    // ScrollWheel zooming on the other hand always moves toward the cursor when scrolling IN. By default, scrolling OUT is directly opposite
    // the camera's facing. However, there is an option to scroll OUT from the cursor instead. 
    public ScreenEdgeConfiguration edgeFreeZoom = new ScreenEdgeConfiguration { sensitivity = 10F, activate = false };
    public ArrowKeyboardConfiguration keyFreeZoom = new ArrowKeyboardConfiguration { sensitivity = 10F, keyboardAxis = KeyboardAxis.Vertical, activate = true };
    public MouseScrollWheelConfiguration scrollFreeZoom = new MouseScrollWheelConfiguration { activate = true };
    public SimultaneousMouseButtonConfiguration dragFreeZoom = new SimultaneousMouseButtonConfiguration { firstMouseButton = NguiMouseButton.Left, secondMouseButton = NguiMouseButton.Right, sensitivity = 0.3F, activate = true };

    // Panning, Tilting and Orbiting: When focused, edge actuation, arrow key pan and tilting and mouse button/movement results in orbiting of the focused object that is centered on the screen. 
    // When not focused the same arrow keys, edge actuation and mouse button/movement results in the camera panning (looking left or right) and tilting (looking up or down) in place.
    public ScreenEdgeConfiguration edgeFreePan = new ScreenEdgeConfiguration { sensitivity = 10F, activate = true };
    public ScreenEdgeConfiguration edgeFreeTilt = new ScreenEdgeConfiguration { sensitivity = 10F, activate = true };
    public ScreenEdgeConfiguration edgeFocusOrbitPan = new ScreenEdgeConfiguration { sensitivity = 10F, activate = true };
    public ScreenEdgeConfiguration edgeFocusOrbitTilt = new ScreenEdgeConfiguration { sensitivity = 10F, activate = true };
    public ArrowKeyboardConfiguration keyFreePan = new ArrowKeyboardConfiguration { keyboardAxis = KeyboardAxis.Horizontal, sensitivity = 0.5F, activate = true };
    public ArrowKeyboardConfiguration keyFreeTilt = new ArrowKeyboardConfiguration { keyboardAxis = KeyboardAxis.Vertical, modifiers = new Modifiers { ctrlKeyReqd = true, shiftKeyReqd = true }, sensitivity = 0.5F, activate = true };
    public ArrowKeyboardConfiguration keyFocusPan = new ArrowKeyboardConfiguration { keyboardAxis = KeyboardAxis.Horizontal, sensitivity = 0.5F, activate = true };
    public ArrowKeyboardConfiguration keyFocusTilt = new ArrowKeyboardConfiguration { keyboardAxis = KeyboardAxis.Vertical, modifiers = new Modifiers { ctrlKeyReqd = true, shiftKeyReqd = true }, sensitivity = 0.5F, activate = true };

    public MouseButtonConfiguration dragFocusOrbit = new MouseButtonConfiguration { mouseButton = NguiMouseButton.Right, sensitivity = 3.0F, activate = true };
    public MouseButtonConfiguration dragFreePanTilt = new MouseButtonConfiguration { mouseButton = NguiMouseButton.Right, sensitivity = 3.0F, activate = true };

    // Truck and Pedestal: Trucking (moving left and right) and Pedestalling (moving up and down) occurs only in Freeform space, repositioning the camera along it's current horizontal and vertical axis'.
    public MouseButtonConfiguration dragFreeTruck = new MouseButtonConfiguration { mouseButton = NguiMouseButton.Middle, modifiers = new Modifiers { altKeyReqd = true }, sensitivity = 0.02F, activate = true };
    public MouseButtonConfiguration dragFreePedestal = new MouseButtonConfiguration { mouseButton = NguiMouseButton.Middle, modifiers = new Modifiers { shiftKeyReqd = true }, sensitivity = 0.02F, activate = true };
    public ArrowKeyboardConfiguration keyFreePedestal = new ArrowKeyboardConfiguration { keyboardAxis = KeyboardAxis.Vertical, modifiers = new Modifiers { ctrlKeyReqd = true }, activate = true };
    public ArrowKeyboardConfiguration keyFreeTruck = new ArrowKeyboardConfiguration { keyboardAxis = KeyboardAxis.Horizontal, modifiers = new Modifiers { ctrlKeyReqd = true }, activate = true };

    // Rolling: Focused and freeform rolling results in the same behaviour, rolling around the camera's current forward axis.
    public MouseButtonConfiguration dragFocusRoll = new MouseButtonConfiguration { mouseButton = NguiMouseButton.Right, modifiers = new Modifiers { altKeyReqd = true }, sensitivity = 10.0F, activate = true };
    public MouseButtonConfiguration dragFreeRoll = new MouseButtonConfiguration { mouseButton = NguiMouseButton.Right, modifiers = new Modifiers { altKeyReqd = true }, sensitivity = 100.0F, activate = true };
    public ArrowKeyboardConfiguration keyFreeRoll = new ArrowKeyboardConfiguration { keyboardAxis = KeyboardAxis.Horizontal, modifiers = new Modifiers { ctrlKeyReqd = true, shiftKeyReqd = true }, activate = true };
    public ArrowKeyboardConfiguration keyFocusRoll = new ArrowKeyboardConfiguration { keyboardAxis = KeyboardAxis.Horizontal, modifiers = new Modifiers { ctrlKeyReqd = true, shiftKeyReqd = true }, activate = true };

    #endregion

    // LEARNINGS
    // Edge-based requested values need to be normalized for framerate using timeSinceLastUpdate as the change per second is the framerate * sensitivity.
    // Key-based requested values DONOT need to be normalized for framerate using timeSinceLastUpdate as Input.GetAxis() is not framerate dependant.
    // Using +/- Mathf.Abs(requestedDistanceToTarget) accelerates/decelerates movement over time.

    // IMPROVE
    // Should Tilt/EdgePan have some Pedastal/Truck added like Star Ruler?
    // Need more elegant rotation and translation functions when selecting a focusTarget - aka Slerp, Mathf.SmoothDamp/Angle, etc. see my Mathfx, Radical's Easing
    // Dragging the mouse with any button held down works offscreen OK, but upon release offscreen, immediately enables edge scrolling and panning
    // Implement Camera controls such as clip planes, FieldOfView, RenderSettings.[flareStrength, haloStrength, ambientLight]

    #region Fields

    private bool _isResetOnFocusEnabled;
    private bool _isZoomOutOnCursorEnabled;    // ScrollWheel always zooms IN on cursor, zooming OUT with the ScrollWheel is directly backwards by default

    public Settings settings = new Settings {
        activeScreenEdge = 5F, smallMovementThreshold = 2F, maxSpeedGovernorDivider = 50F,
        focusingPositionDampener = 2.0F, focusingRotationDampener = 1.0F, focusedPositionDampener = 4.0F,
        focusedRotationDampener = 2.0F, freeformPositionDampener = 3.0F, freeformRotationDampener = 2.0F
    };

    private CtxPickHandler _contextMenuPickHandler;

    // static so it is available to nested classes
    public static float universeRadius;
    public static GameInput gameInput;

    // Cached references
    [DoNotSerialize]    // Serializing this creates duplicates of this object on Save
    private GameEventManager _eventMgr;
    [DoNotSerialize]    // Serializing this creates duplicates of this object on Save
    private PlayerPrefsManager _playerPrefsMgr;

    private IList<IDisposable> _subscribers;

    private Vector3 _targetPoint;
    private Transform _target;
    private Transform _dummyTarget;
    private Transform _transform;

    private string[] keyboardAxesNames = new string[] { UnityConstants.KeyboardAxisName_Horizontal, UnityConstants.KeyboardAxisName_Vertical };
    private LayerMask collideWithUniverseEdgeOnlyLayerMask = LayerMaskExtensions.CreateInclusiveMask(Layers.UniverseEdge);
    private LayerMask collideWithDummyTargetOnlyLayerMask = LayerMaskExtensions.CreateInclusiveMask(Layers.DummyTarget);
    private LayerMask collideWithOnlyCameraTargetsLayerMask = LayerMaskExtensions.CreateExclusiveMask(Layers.UniverseEdge, Layers.DeepSpace);
    private Vector3 _screenCenter = new Vector3(Screen.width / 2, Screen.height / 2, 0F);

    // Continuously calculated, actual Camera values
    private float _distanceFromTarget;
    private Vector3 _targetDirection;

    // Desired Camera values requested via input controls
    private float _xRotation;    // EulerAngles
    private float _yRotation;
    private float _zRotation;
    private float _requestedDistanceFromTarget;

    // Fields used in algorithms that can vary by Target or CameraState
    private float _minimumDistanceFromTarget;
    private float _optimalDistanceFromTarget;
    private float _cameraPositionDampener;
    private float _cameraRotationDampener;

    private CameraState cameraState;

    public enum CameraUpdateMode { LateUpdate = 0, FixedUpdate = 1, Update = 2 }
    public CameraUpdateMode updateMode = CameraUpdateMode.LateUpdate;

    #endregion

    #region Temporary InEditor Edge Movement Controls
    // Temporary workaround that keeps the edge movement controls
    // from operating when I'm in the Editor but outside the game screen

    private bool __debugEdgeFocusOrbitPanEnabled;
    private bool __debugEdgeFocusOrbitTiltEnabled;
    private bool __debugEdgeFreePanEnabled;
    private bool __debugEdgeFreeTiltEnabled;

    [System.Diagnostics.Conditional("UNITY_EDITOR")]
    private void __InitializeDebugEdgeMovementSettings() {
        __debugEdgeFocusOrbitPanEnabled = edgeFocusOrbitPan.activate;
        __debugEdgeFocusOrbitTiltEnabled = edgeFocusOrbitTilt.activate;
        __debugEdgeFreePanEnabled = edgeFreePan.activate;
        __debugEdgeFreeTiltEnabled = edgeFreeTilt.activate;
    }

    // called by OnApplicationFocus()
    [System.Diagnostics.Conditional("UNITY_EDITOR")]
    private void __EnableEdgePanTiltInEditor(bool toEnable) {
        edgeFocusOrbitPan.activate = __debugEdgeFocusOrbitPanEnabled && toEnable;
        edgeFocusOrbitTilt.activate = __debugEdgeFocusOrbitTiltEnabled && toEnable;
        edgeFreePan.activate = __debugEdgeFreePanEnabled && toEnable;
        edgeFreeTilt.activate = __debugEdgeFreeTiltEnabled && toEnable;
    }

    #endregion

    #region Initialization

    /// <summary>
    /// The 1st method called when the script instance is being loaded. Called once and only once in the lifetime of the script
    /// instance. All game objects have already been initialized so references to other scripts may be established here.
    /// </summary>
    protected override void Awake() {
        base.Awake();
        UnityUtility.ValidateComponentPresence<Camera>(gameObject);
        //Logger.Log("CameraControl.Awake() called.");
        InitializeReferences();
        __InitializeDebugEdgeMovementSettings();
    }

    private void InitializeReferences() {
        IncrementInstanceCounter();
        //if (LevelSerializer.IsDeserializing) { return; }
        _eventMgr = GameEventManager.Instance;
        _playerPrefsMgr = PlayerPrefsManager.Instance;
        _transform = transform;    // cache it!transformis actually GetComponent<Transform>()
        gameInput = new GameInput();
        Subscribe();
        ValidateActiveConfigurations();
        // need to raise this event in Awake as Start can be too late, since the true version of this event is called
        // when the GameState changes to Waiting, which can occur before Start. We have to rely on Loader.Awake
        // being called first via ScriptExecutionOrder.
        _eventMgr.Raise<ElementReadyEvent>(new ElementReadyEvent(this, isReady: false));
    }

    private void ValidateActiveConfigurations() {
        bool isValid = true;
        if ((edgeFocusOrbitTilt.activate && edgeFocusZoom.activate) || (edgeFreeTilt.activate && edgeFreeZoom.activate)) {
            isValid = false;
        }
        D.Assert(isValid, "Incompatable Camera Configuration.", pauseOnFail: true);
    }

    private void InitializeMainCamera() {
        //Logger.Log("Camera initializing.");
        SetCameraSettings();
        InitializeCameraPreferences();
        PositionCameraForGame();
        InitializeContextMenuSettings();
    }

    private void SetCameraSettings() {
        universeRadius = GameManager.Settings.UniverseSize.Radius();
        Camera.main.farClipPlane = universeRadius * 2;

        // This camera will see all layers except for the GUI and DeepSpace layers. If I want to add exclusions, I can still do it from the outside
        camera.cullingMask = LayerMaskExtensions.CreateExclusiveMask(Layers.Gui, Layers.DeepSpace);
        UpdateRate = UpdateFrequency.Continuous;
    }

    private void InitializeCameraPreferences() {
        // the initial Camera preference changed events occur earlier than we can subscribe so do it manually
        OnResetOnFocusEnabledChanged();
        OnCameraRollEnabledChanged();
        OnZoomOutOnCursorEnabledChanged();
    }

    private void EnableCameraRoll(bool toEnable) {
        dragFocusRoll.activate = toEnable;
        dragFreeRoll.activate = toEnable;
        keyFreeRoll.activate = toEnable;
    }

    private void PositionCameraForGame() {
        CreateUniverseEdge();
        CreateDummyTarget();
        ResetAtCurrentLocation();
        // UNDONE whether starting or continuing saved game, camera position should be focused on the player's starting planet, no rotation
        //ResetToWorldspace();
    }

    private void CreateUniverseEdge() {
        SphereCollider universeEdge = null;
        SphereCollider universeEdgePrefab = RequiredPrefabs.Instance.UniverseEdgePrefab;
        if (universeEdgePrefab == null) {
            D.Warn("UniverseEdgePrefab on RequiredPrefabs is null.");
            string universeEdgeName = Layers.UniverseEdge.GetName();
            universeEdge = new GameObject(universeEdgeName).AddComponent<SphereCollider>();
            universeEdge.gameObject.layer = (int)Layers.UniverseEdge;
            universeEdge.gameObject.isStatic = true;
        }
        else {
            universeEdge = Instantiate<SphereCollider>(universeEdgePrefab);
        }
        universeEdge.radius = universeRadius;
        universeEdge.transform.parent = DynamicObjects.Folder;
    }

    private void CreateDummyTarget() {
        Transform dummyTargetPrefab = RequiredPrefabs.Instance.CameraDummyTargetPrefab;
        if (dummyTargetPrefab == null) {
            D.Warn("DummyTargetPrefab on RequiredPrefabs is null.");
            string dummyTargetName = Layers.DummyTarget.GetName();
            _dummyTarget = new GameObject(dummyTargetName).transform;
            _dummyTarget.gameObject.layer = (int)Layers.DummyTarget;
            _dummyTarget.gameObject.AddComponent<SphereCollider>();
            _dummyTarget.gameObject.AddComponent<DummyTargetManager>();
        }
        else {
            _dummyTarget = Instantiate<Transform>(dummyTargetPrefab);
        }
        _dummyTarget.parent = DynamicObjects.Folder;
    }

    /// <summary>
    /// Resets the camera to its base state without changing its location
    /// or facing. Base state is Freeform with its target set as the
    /// DummyTarget positioned directly ahead. 
    /// </summary>
    private void ResetAtCurrentLocation() {
        _dummyTarget.collider.enabled = false;
        // the collider is disabled so the placement algorithm doesn't accidently find it already in front of the camera
        TryPlaceDummyTargetAtUniverseEdgeInDirection(_transform.forward);
        _dummyTarget.collider.enabled = true;
        SyncRotation();
        ChangeState(CameraState.Freeform);
    }

    private void SyncRotation() {
        Quaternion startingRotation = _transform.rotation;
        Vector3 startingEulerRotation = startingRotation.eulerAngles;
        // don't understand why y and x are reversed
        _xRotation = startingEulerRotation.y;
        _yRotation = startingEulerRotation.x;
        _zRotation = startingEulerRotation.z;
    }

    private void InitializeContextMenuSettings() {
        _contextMenuPickHandler = gameObject.GetSafeMonoBehaviourComponent<CtxPickHandler>();
        _contextMenuPickHandler.dontUseFallThrough = true;
        if (_contextMenuPickHandler.menuButton != NguiMouseButton.Right.ToUnityMouseButton()) {
            D.Warn("Context Menu actuator button not set to Right Mouse Button.");
        }
        // IMPROVE I should be able to use UIEventListener to subscribe to all objects with a CtxObject
        // using FindObjectsOfType<CtxObject>(), but I can't figure out how to assign ContextMenuPickHandler.OnPress
        // (a method on a different object) to the UIEventListener delegate. For now, I'm just implementing
        // OnPress(isPressed) in each object implementing ISelectable, ie. all ISelectable by definition will be able
        // to receive manual orders and therefore should support context menus.
    }

    #endregion

    #region Event Handlers

    private void Subscribe() {
        if (_subscribers == null) {
            _subscribers = new List<IDisposable>();
        }
        _eventMgr.AddListener<FocusSelectedEvent>(this, OnFocusSelected);
        _subscribers.Add(GameManager.Instance.SubscribeToPropertyChanged<GameManager, GameState>(gm => gm.GameState, OnGameStateChanged));
        _subscribers.Add(_playerPrefsMgr.SubscribeToPropertyChanged<PlayerPrefsManager, bool>(pm => pm.IsCameraRollEnabled, OnCameraRollEnabledChanged));
        _subscribers.Add(_playerPrefsMgr.SubscribeToPropertyChanged<PlayerPrefsManager, bool>(pm => pm.IsResetOnFocusEnabled, OnResetOnFocusEnabledChanged));
        _subscribers.Add(_playerPrefsMgr.SubscribeToPropertyChanged<PlayerPrefsManager, bool>(pm => pm.IsZoomOutOnCursorEnabled, OnZoomOutOnCursorEnabledChanged));
    }

    private void OnZoomOutOnCursorEnabledChanged() {
        _isZoomOutOnCursorEnabled = _playerPrefsMgr.IsZoomOutOnCursorEnabled;
    }

    private void OnResetOnFocusEnabledChanged() {
        _isResetOnFocusEnabled = _playerPrefsMgr.IsResetOnFocusEnabled;
    }

    private void OnCameraRollEnabledChanged() {
        EnableCameraRoll(_playerPrefsMgr.IsCameraRollEnabled);
    }

    [DoNotSerialize]
    private bool _restoredGameFlag = false;
    private void OnGameStateChanged() {
        GameState state = GameManager.Instance.GameState;
        switch (state) {
            case GameState.Restoring:
                // only saved games that are being restored enter Restoring state
                _restoredGameFlag = true;
                break;
            case GameState.Waiting:
                if (_restoredGameFlag) {
                    // for a restored game, Waiting state is gauranteed to occur after OnDeserialized so we must be ready to proceed
                    _restoredGameFlag = false;
                }
                else {
                    InitializeMainCamera();
                }
                _eventMgr.Raise<ElementReadyEvent>(new ElementReadyEvent(this, isReady: true));
                break;
            case GameState.Building:
            case GameState.Loading:
            case GameState.RunningCountdown_3:
            case GameState.RunningCountdown_2:
            case GameState.RunningCountdown_1:
            case GameState.Running:
                // do nothing
                break;
            case GameState.None:
            default:
                throw new NotImplementedException(ErrorMessages.UnanticipatedSwitchValue.Inject(state));
        }
    }

    private void OnFocusSelected(FocusSelectedEvent e) {
        Logger.Log("FocusSelectedEvent received. Focus is {0}.".Inject(e.FocusTransform.name));
        SetFocus(e.FocusTransform);
    }

    private void Unsubscribe() {
        _eventMgr.RemoveListener<FocusSelectedEvent>(this, OnFocusSelected);
        _subscribers.ForAll<IDisposable>(s => s.Dispose());
        _subscribers.Clear();
    }

    #endregion

    #region Standard MonoBehaviour Events

    /// <summary>
    /// Called when application goes in/out of focus, this method controls the
    /// enabled state of edge pan and tilt so the camera doesn't respond to edge 
    /// movement commands when the mouse is clicked outside the editor game window.
    /// </summary>
    /// <arg item="isFocus">if set to <c>true</c> [is focusTarget].</arg>
    void OnApplicationFocus(bool isFocus) {
        Logger.Log("Camera OnApplicationFocus({0}) called.", isFocus);
        __EnableEdgePanTiltInEditor(isFocus);
    }

    /// <summary>
    /// Called when the application is minimized/resumed, this method controls the enabled
    /// state of the camera so it doesn't move when I use the mouse to minimize Unity.
    /// </summary>
    /// <arg item="isPausing">if set to <c>true</c> [is toPause].</arg>
    void OnApplicationPause(bool isPaused) {
        //Logger.Log("Camera OnApplicationPause(" + isPaused + ") called.");
        enabled = !isPaused;
    }

    protected override void OnDestroy() {
        base.OnDestroy();
        Dispose();
    }

    #endregion

    #region State

    /// <summary>
    /// Assign the focus object to be the Target and changes the CameraState based on
    /// what interface the object supports.
    /// </summary>
    /// <arg item="focus">The transform of the GO selected as the focus.</arg>
    private void SetFocus(Transform focus) {
        // any object that can be focused on has the focus's position as the targetPoint
        ChangeTarget(focus, focus.position);

        ICameraFocusable qualifiedCameraFocusTarget = focus.GetInterface<ICameraFollowable>();
        if (qualifiedCameraFocusTarget != null) {
            qualifiedCameraFocusTarget.IsFocus = true;
            ChangeState(CameraState.Follow);
            return;
        }

        qualifiedCameraFocusTarget = focus.GetInterface<ICameraFocusable>();
        if (qualifiedCameraFocusTarget != null) {
            qualifiedCameraFocusTarget.IsFocus = true;
            if (!_isResetOnFocusEnabled) {
                // if not resetting world coordinates on focus, the camera just turns to look at the focus
                ChangeState(CameraState.Focusing);
                return;
            }

            ResetToWorldspace();
            ChangeState(CameraState.Focused);
        }
        else {
            D.Error("Attempting to SetFocus on object that does not implement either ICameraFollowable or ICameraFocusable.");
        }
    }

    /// <summary>
    /// Changes the current Target and targetPoint to the provided newTarget and newTargetPoint.
    /// Adjusts any minimum, optimal and actual camera distance settings. 
    /// </summary>
    /// <param name="newTarget">The new Target.</param>
    /// <param name="newTargetPoint">The new Target point.</param>
    private void ChangeTarget(Transform newTarget, Vector3 newTargetPoint) {
        if (newTarget == _target && newTarget != _dummyTarget) {
            if (Mathfx.Approx(newTargetPoint, _targetPoint, settings.smallMovementThreshold)) {
                // the desired move of the Target point on the existing Target is too small to respond too
                return;
            }
        }

        // NOTE: As Rigidbodies consume child collider events, a hit on a child collider when there is a rigidbody parent 
        // involved, will return the transform of the parent, not the child. By not including inspection of the children for this interface,
        // I am requiring that the interface be present with the Rigidbody.
        ICameraTargetable qualifiedCameraTarget = newTarget.GetInterface<ICameraTargetable>();
        if (qualifiedCameraTarget != null) {
            if (!qualifiedCameraTarget.IsTargetable) {
                return;
            }
            _minimumDistanceFromTarget = qualifiedCameraTarget.MinimumCameraViewingDistance;
            //Logger.Log("Target {0} _minimumDistanceFromTarget set to {1}.".Inject(newTarget.name, _minimumDistanceFromTarget));

            ICameraFocusable qualifiedCameraFocusTarget = newTarget.GetInterface<ICameraFocusable>();
            if (qualifiedCameraFocusTarget != null) {
                _optimalDistanceFromTarget = qualifiedCameraFocusTarget.OptimalCameraViewingDistance;
                //Logger.Log("Target {0} _optimalDistanceFromTarget set to {1}.".Inject(newTarget.name, _optimalDistanceFromTarget));
            }
            // no reason to know whether the Target is followable or not for these values for now
        }
        else {
            D.Error("New Target {0} is not an ICameraTargetable.".Inject(newTarget.name));
            return;
        }

        Transform previousTarget = _target;
        if (previousTarget != null) {  // _target is null the first time around
            // if the previous target is IFocusable play it safe and tell it it is not the focus
            ICameraFocusable previousTargetIsFocusable = previousTarget.GetInterface<ICameraFocusable>();
            if (previousTargetIsFocusable != null) {
                previousTargetIsFocusable.IsFocus = false;
            }
        }

        _target = newTarget;
        _targetPoint = newTargetPoint;
        // anytime the Target changes, the actual distance to the Target should also be reset
        _distanceFromTarget = Vector3.Distance(_targetPoint, _transform.position);
        // the requested distance to the Target will vary depending on where the change was initiated from
    }

    /// <summary>
    /// Changes the CameraState and sets calculated values to reflect the new state.
    /// </summary>
    /// <arg item="newState">The new state.</arg>
    /// <exception cref="System.NotImplementedException"></exception>
    private void ChangeState(CameraState newState) {
        Arguments.ValidateNotNull(_targetPoint);
        cameraState = newState;
        switch (newState) {
            case CameraState.Focusing:
                _distanceFromTarget = Vector3.Distance(_targetPoint, _transform.position);
                _requestedDistanceFromTarget = _optimalDistanceFromTarget;
                _targetDirection = (_targetPoint - _transform.position).normalized;

                // face the selected Target
                //Logger.Log("Rotation values before ChangeState {0}.".Inject(new Vector3(_xRotation, _yRotation, _zRotation)));
                Quaternion lookAt = Quaternion.LookRotation(_targetDirection);
                Vector3 lookAtVector = lookAt.eulerAngles;
                _xRotation = lookAtVector.y;
                _yRotation = lookAtVector.x;
                _zRotation = lookAtVector.z;
                //Logger.Log("Rotation values after ChangeState {0}.".Inject(new Vector3(_xRotation, _yRotation, _zRotation)));
                _cameraRotationDampener = settings.focusingRotationDampener;
                _cameraPositionDampener = settings.focusingPositionDampener;
                break;
            case CameraState.Focused:
                // entered via OnFocusSelected AND IsResetOnFocusEnabled, OR after Focusing has completed
                _distanceFromTarget = Vector3.Distance(_targetPoint, _transform.position);
                _requestedDistanceFromTarget = _optimalDistanceFromTarget;
                // x,y,z rotation has already been established before entering

                _cameraRotationDampener = settings.focusedRotationDampener;
                _cameraPositionDampener = settings.focusedPositionDampener;
                break;
            case CameraState.Freeform:
                _distanceFromTarget = Vector3.Distance(_targetPoint, _transform.position);
                _requestedDistanceFromTarget = _distanceFromTarget;
                // no facing change

                _cameraRotationDampener = settings.freeformRotationDampener;
                _cameraPositionDampener = settings.freeformPositionDampener;
                break;
            case CameraState.Follow:
                // no need to update distance or rotation calculated values as UpdateCamera must
                // update these every frame when following as the Target moves

                ICameraFollowable icfTarget = _target.GetInterface<ICameraFollowable>();
                _cameraRotationDampener = icfTarget.CameraFollowRotationDampener;
                _cameraPositionDampener = icfTarget.CameraFollowDistanceDampener;
                break;
            case CameraState.None:
            default:
                throw new NotImplementedException(ErrorMessages.UnanticipatedSwitchValue.Inject(newState));
        }
        _UpdateDebugHud(DebugHudLineKeys.CameraMode, cameraState.GetName());
    }

    /// <summary>
    /// Resets the camera rotation to that of worldspace, no rotation.
    /// </summary>
    public void ResetToWorldspace() {
        // current and requested distance to Target already set
        Quaternion zeroRotation = Quaternion.identity;
        _transform.rotation = zeroRotation;
        Vector3 zeroRotationVector = zeroRotation.eulerAngles;
        _xRotation = zeroRotationVector.y;
        _yRotation = zeroRotationVector.x;
        _zRotation = zeroRotationVector.z;
        //Logger.Log("ResetToWorldSpace called. Worldspace Camera Rotation = {0}.".Inject(new Vector3(_xRotation, _yRotation, _zRotation)));
    }

    #endregion

    /// <summary>
    /// Tries to show the context menu. 
    /// NOTE: This is a preprocess method for
    /// ContextMenuPickHandler.OnPress(isDown) which is designed to show
    /// the context menu if the method is called both times from the same object.
    /// Unfortunately, that also means the context menu will show if a drag
    /// starts and ends over the same ISelectable object. Therefore, this 
    /// preprocess method is here to detect whether a drag is occuring before 
    /// passing it on to show the context menu.
    /// FIXME unfortunately, UICamera.isDragging = false when OnPress(false)
    /// occurs at the end of a drag that started and ended over the same object.
    /// </summary>
    /// <param name="isDown">if set to <c>true</c> [is down].</param>
    public void TryShowContextMenuOnPress(bool isDown) {
        //if (!UICamera.isDragging) {
        _contextMenuPickHandler.OnPress(isDown);
        //}
    }

    #region Update Camera

    void Update() {
        if (updateMode == CameraUpdateMode.Update) { UpdateCamera(); }
    }

    void LateUpdate() {
        if (updateMode == CameraUpdateMode.LateUpdate) { UpdateCamera(); }
    }

    void FixedUpdate() {
        if (updateMode == CameraUpdateMode.FixedUpdate) { UpdateCamera(); }
    }

    private void UpdateCamera() {
        if (ToUpdate()) {
            //Logger.Log("___________________New Frame______________________");
            float timeSinceLastUpdate = GameTime.DeltaTime * (int)UpdateRate;
            bool _toLockCursor = false;
            float mouseInputValue = 0F;
            bool showDistanceDebugLog = false;
            Vector3 lookAt = Vector3.zero;

            if (_target == null || _targetPoint == null) {
                ResetAtCurrentLocation();
            }

            switch (cameraState) {
                case CameraState.Focusing:
                    // transition state to allow lookAt to complete. Only entered from OnFocusSelected, when !IsResetOnFocus
                    //Logger.Log("Focusing. RequestedDistanceFromTarget = {0}.".Inject(_requestedDistanceFromTarget));
                    //showDistanceDebugLog = true;
                    if (CheckExitConditions()) {
                        // exits to Focused when the lookAt rotation is complete, ie. _targetDirection 'equals' _transform.forward
                        ChangeState(CameraState.Focused);
                        return;
                    }
                    _toLockCursor = true;

                    // The desired (x,y,z) rotation to LookAt the Target and the requested distance from the Target
                    // is set in ChangeState and does not need to be updated to get there as the Target doesn't move

                    // OPTIMIZE lets me change the values on the fly in the inspector
                    _cameraRotationDampener = settings.focusingRotationDampener;
                    _cameraPositionDampener = settings.focusingPositionDampener;
                    // no other functionality active 
                    break;
                case CameraState.Focused:
                    //Logger.Log("Focused. RequestedDistanceFromTarget = {0}.".Inject(_requestedDistanceFromTarget));
                    //showDistanceDebugLog = true;
                    if (CheckExitConditions()) {
                        ChangeState(CameraState.Freeform);
                        return;
                    }
                    if (dragFocusOrbit.IsActivated()) {
                        Vector2 dragDelta = gameInput.GetDragDelta();
                        mouseInputValue = dragDelta.x;
                        _xRotation += mouseInputValue * dragFocusOrbit.sensitivity * timeSinceLastUpdate;
                        mouseInputValue = dragDelta.y;
                        _yRotation -= mouseInputValue * dragFocusOrbit.sensitivity * timeSinceLastUpdate;
                    }
                    if (edgeFocusOrbitPan.IsActivated()) {
                        float xMousePosition = Input.mousePosition.x;
                        if (xMousePosition <= settings.activeScreenEdge) {
                            _xRotation += edgeFocusOrbitPan.sensitivity * timeSinceLastUpdate;
                        }
                        else if (xMousePosition >= Screen.width - settings.activeScreenEdge) {
                            _xRotation -= edgeFocusOrbitPan.sensitivity * timeSinceLastUpdate;
                        }
                    }
                    if (edgeFocusOrbitTilt.IsActivated()) {
                        float yMousePosition = Input.mousePosition.y;
                        if (yMousePosition <= settings.activeScreenEdge) {
                            _yRotation -= edgeFocusOrbitPan.sensitivity * timeSinceLastUpdate;
                        }
                        else if (yMousePosition >= Screen.height - settings.activeScreenEdge) {
                            _yRotation += edgeFocusOrbitPan.sensitivity * timeSinceLastUpdate;
                        }
                    }
                    if (dragFocusRoll.IsActivated()) {
                        mouseInputValue = gameInput.GetDragDelta().x;
                        _zRotation += mouseInputValue * dragFocusRoll.sensitivity * timeSinceLastUpdate;
                    }
                    if (scrollFocusZoom.IsActivated()) {
                        mouseInputValue = gameInput.GetScrollWheelMovement();
                        if (mouseInputValue > 0 || (mouseInputValue < 0 && _isZoomOutOnCursorEnabled)) {
                            if (TrySetTargetAtScreenPoint(Input.mousePosition)) {
                                // there is a new Target so it can't be the old focus Target
                                ChangeState(CameraState.Freeform);
                                return;
                            }
                        }
                        float translationSpeedGoverner = Mathf.Clamp(Mathf.Abs(_requestedDistanceFromTarget), 0F, settings.MaxSpeedGovernor);
                        _requestedDistanceFromTarget -= mouseInputValue * translationSpeedGoverner * scrollFocusZoom.sensitivity * scrollFocusZoom.InputControlSpeedNormalizer * timeSinceLastUpdate;
                        //Logger.Log("MaxSpeedGovernor = {0}, mouseInputValue = {1}".Inject(settings.MaxSpeedGovernor, mouseInputValue));
                        //Logger.Log("ScrollFocusZoom translationSpeedGoverner = {0}, _requestedDistanceFromTarget = {1}".Inject(translationSpeedGoverner, _requestedDistanceFromTarget));
                    }
                    if (edgeFocusZoom.IsActivated()) {
                        float yMousePosition = Input.mousePosition.y;
                        if (yMousePosition <= settings.activeScreenEdge) {
                            float translationSpeedGoverner = Mathf.Clamp(Mathf.Abs(_requestedDistanceFromTarget), 0F, settings.MaxSpeedGovernor);
                            _requestedDistanceFromTarget += translationSpeedGoverner * edgeFocusZoom.sensitivity * edgeFocusZoom.InputControlSpeedNormalizer * timeSinceLastUpdate;
                            //Logger.Log("edgeFocusZoom translationSpeedGoverner = {0}, _requestedDistanceFromTarget = {1}".Inject(translationSpeedGoverner, _requestedDistanceFromTarget));
                        }
                        else if (yMousePosition >= Screen.height - settings.activeScreenEdge) {
                            float translationSpeedGoverner = Mathf.Clamp(Mathf.Abs(_requestedDistanceFromTarget), 0F, settings.MaxSpeedGovernor);
                            _requestedDistanceFromTarget -= translationSpeedGoverner * edgeFocusZoom.sensitivity * edgeFocusZoom.InputControlSpeedNormalizer * timeSinceLastUpdate;
                            //Logger.Log("edgeFocusZoom translationSpeedGoverner = {0}, _requestedDistanceFromTarget = {1}".Inject(translationSpeedGoverner, _requestedDistanceFromTarget));
                        }
                    }
                    if (keyFocusZoom.IsActivated()) {
                        float translationSpeedGoverner = Mathf.Clamp(Mathf.Abs(_requestedDistanceFromTarget), 0F, settings.MaxSpeedGovernor);
                        _requestedDistanceFromTarget -= Input.GetAxis(keyboardAxesNames[(int)keyFocusZoom.keyboardAxis]) * translationSpeedGoverner * keyFocusZoom.InputControlSpeedNormalizer * keyFocusZoom.sensitivity;
                        //Logger.Log("keyFocusZoom translationSpeedGoverner = {0}, _requestedDistanceFromTarget = {1}".Inject(translationSpeedGoverner, _requestedDistanceFromTarget));
                    }
                    if (dragFocusZoom.IsActivated()) {
                        mouseInputValue = gameInput.GetDragDelta().y;
                        //Logger.Log("MouseFocusZoom Vertical Mouse Movement detected.");
                        float translationSpeedGoverner = Mathf.Clamp(Mathf.Abs(_requestedDistanceFromTarget), 0F, settings.MaxSpeedGovernor);
                        _requestedDistanceFromTarget -= mouseInputValue * translationSpeedGoverner * dragFocusZoom.sensitivity * dragFocusZoom.InputControlSpeedNormalizer * timeSinceLastUpdate;
                        //Logger.Log("dragFocusZoom translationSpeedGoverner = {0}, _requestedDistanceFromTarget = {1}".Inject(translationSpeedGoverner, _requestedDistanceFromTarget));
                    }
                    if (keyFocusPan.IsActivated()) {
                        _xRotation += Input.GetAxis(keyboardAxesNames[(int)keyFreePan.keyboardAxis]) * keyFreePan.sensitivity;
                    }
                    if (keyFocusTilt.IsActivated()) {
                        _yRotation -= Input.GetAxis(keyboardAxesNames[(int)keyFreeTilt.keyboardAxis]) * keyFreeTilt.sensitivity;
                    }
                    if (keyFocusRoll.IsActivated()) {
                        _zRotation -= Input.GetAxis(keyboardAxesNames[(int)keyFreeRoll.keyboardAxis]) * keyFreeRoll.sensitivity;
                    }

                    // t.forward is the camera's current definition of 'forward', ie. WorldSpace's absolute forward adjusted by the camera's rotation (Vector.forward * cameraRotation )   
                    // this is the key that keeps the camera pointed at the Target when focused
                    _targetDirection = _transform.forward;

                    // OPTIMIZE lets me change the values on the fly in the inspector
                    _cameraRotationDampener = settings.focusedRotationDampener;
                    _cameraPositionDampener = settings.focusedPositionDampener;
                    break;
                case CameraState.Freeform:
                    if (edgeFreePan.IsActivated()) {
                        float xMousePosition = Input.mousePosition.x;
                        if (xMousePosition <= settings.activeScreenEdge) {
                            _xRotation -= edgeFreePan.sensitivity * timeSinceLastUpdate;
                        }
                        else if (xMousePosition >= Screen.width - settings.activeScreenEdge) {
                            _xRotation += edgeFreePan.sensitivity * timeSinceLastUpdate;
                        }
                    }
                    if (edgeFreeTilt.IsActivated()) {
                        float yMousePosition = Input.mousePosition.y;
                        if (yMousePosition <= settings.activeScreenEdge) {
                            _yRotation += edgeFreeTilt.sensitivity * timeSinceLastUpdate;
                        }
                        else if (yMousePosition >= Screen.height - settings.activeScreenEdge) {
                            _yRotation -= edgeFreeTilt.sensitivity * timeSinceLastUpdate;
                        }
                    }
                    if (dragFreeTruck.IsActivated()) {
                        mouseInputValue = gameInput.GetDragDelta().x;
                        TryPlaceDummyTargetAtUniverseEdgeInDirection(_transform.right);
                        _requestedDistanceFromTarget += mouseInputValue * dragFreeTruck.sensitivity * dragFreeTruck.InputControlSpeedNormalizer * timeSinceLastUpdate;
                    }
                    if (dragFreePedestal.IsActivated()) {
                        mouseInputValue = gameInput.GetDragDelta().y;
                        TryPlaceDummyTargetAtUniverseEdgeInDirection(_transform.up);
                        _requestedDistanceFromTarget += mouseInputValue * dragFreePedestal.sensitivity * dragFreePedestal.InputControlSpeedNormalizer * timeSinceLastUpdate;
                    }
                    if (dragFreeRoll.IsActivated()) {
                        mouseInputValue = gameInput.GetDragDelta().y;
                        _zRotation += mouseInputValue * dragFreeRoll.sensitivity * timeSinceLastUpdate;
                    }
                    if (dragFreePanTilt.IsActivated()) {
                        Vector2 dragDelta = gameInput.GetDragDelta();
                        mouseInputValue = dragDelta.x;
                        _xRotation -= mouseInputValue * dragFreePanTilt.sensitivity * timeSinceLastUpdate;
                        mouseInputValue = dragDelta.y;
                        _yRotation += mouseInputValue * dragFreePanTilt.sensitivity * timeSinceLastUpdate;
                    }
                    if (scrollFreeZoom.IsActivated()) {
                        mouseInputValue = gameInput.GetScrollWheelMovement();
                        if (mouseInputValue > 0) {
                            // Scroll ZoomIN command
                            if (TrySetTargetAtScreenPoint(Input.mousePosition)) {
                                // Target was changed so reset requested distance to actual distance
                                _requestedDistanceFromTarget = _distanceFromTarget;
                            }
                        }
                        if (mouseInputValue < 0) {
                            // Scroll ZoomOUT command
                            if (_isZoomOutOnCursorEnabled) {
                                if (TrySetTargetAtScreenPoint(Input.mousePosition)) {
                                    // Target was changed so reset requested distance to actual distance
                                    _requestedDistanceFromTarget = _distanceFromTarget;
                                }
                            }
                            else {
                                if (TrySetTargetAtScreenPoint(_screenCenter)) {
                                    // Target was changed so reset requested distance to actual distance
                                    _requestedDistanceFromTarget = _distanceFromTarget;
                                }
                            }
                        }
                        float translationSpeedGoverner = Mathf.Clamp(Mathf.Abs(_requestedDistanceFromTarget), 0F, settings.MaxSpeedGovernor);
                        _requestedDistanceFromTarget -= mouseInputValue * translationSpeedGoverner * scrollFreeZoom.sensitivity * scrollFreeZoom.InputControlSpeedNormalizer * timeSinceLastUpdate;
                        //Logger.Log("ScrollFreeZoom translationSpeedGoverner = {0}, _requestedDistanceFromTarget = {1}".Inject(translationSpeedGoverner, _requestedDistanceFromTarget));
                        //showDistanceDebugLog = true;
                    }
                    if (edgeFreeZoom.IsActivated()) {
                        float yMousePosition = Input.mousePosition.y;
                        if (yMousePosition <= settings.activeScreenEdge) {
                            // Edge ZoomOUT
                            if (TrySetTargetAtScreenPoint(_screenCenter)) {
                                // Target was changed so reset requested distance to actual distance
                                _requestedDistanceFromTarget = _distanceFromTarget;
                            }
                            _requestedDistanceFromTarget += edgeFreeZoom.sensitivity * edgeFreeZoom.InputControlSpeedNormalizer * timeSinceLastUpdate;
                            //Logger.Log("EdgeFreeZoom _requestedDistanceFromTarget = " + _requestedDistanceFromTarget);
                            //showDistanceDebugLog = true;
                        }
                        else if (yMousePosition >= Screen.height - settings.activeScreenEdge) {
                            // Edge ZoomIN
                            if (TrySetTargetAtScreenPoint(_screenCenter)) {
                                // Target was changed so reset requested distance to actual distance
                                _requestedDistanceFromTarget = _distanceFromTarget;
                            }
                            _requestedDistanceFromTarget -= edgeFreeZoom.sensitivity * edgeFreeZoom.InputControlSpeedNormalizer * timeSinceLastUpdate;
                            //showDistanceDebugLog = true;
                        }
                    }
                    if (dragFreeZoom.IsActivated()) {
                        mouseInputValue = gameInput.GetDragDelta().y;
                        if (TrySetTargetAtScreenPoint(_screenCenter)) {
                            _requestedDistanceFromTarget = _distanceFromTarget;
                        }
                        _requestedDistanceFromTarget -= mouseInputValue * dragFreeZoom.sensitivity * dragFreeZoom.InputControlSpeedNormalizer * timeSinceLastUpdate;
                        //showDistanceDebugLog = true;
                    }

                    // Freeform Arrow Keyboard Configurations. Mouse Buttons supercede Arrow Keys. Only Arrow Keys are used as IsActivated() must be governed by 
                    // whether the appropriate key is down to keep the configurations from interfering with each other. 
                    if (keyFreeZoom.IsActivated()) {
                        if (TrySetTargetAtScreenPoint(_screenCenter)) {
                            _requestedDistanceFromTarget = _distanceFromTarget;
                        }
                        //showDistanceDebugLog = true;
                        _requestedDistanceFromTarget -= Input.GetAxis(keyboardAxesNames[(int)keyFreeZoom.keyboardAxis]) * keyFreeZoom.sensitivity * keyFreeZoom.InputControlSpeedNormalizer;
                    }
                    if (keyFreeTruck.IsActivated()) {
                        TryPlaceDummyTargetAtUniverseEdgeInDirection(_transform.right);
                        _requestedDistanceFromTarget -= Input.GetAxis(keyboardAxesNames[(int)keyFreeTruck.keyboardAxis]) * keyFreeTruck.sensitivity * keyFreeTruck.InputControlSpeedNormalizer;
                    }
                    if (keyFreePedestal.IsActivated()) {
                        TryPlaceDummyTargetAtUniverseEdgeInDirection(_transform.up);
                        _requestedDistanceFromTarget -= Input.GetAxis(keyboardAxesNames[(int)keyFreePedestal.keyboardAxis]) * keyFreePedestal.sensitivity * keyFreePedestal.InputControlSpeedNormalizer;
                    }
                    if (keyFreePan.IsActivated()) {
                        _xRotation += Input.GetAxis(keyboardAxesNames[(int)keyFreePan.keyboardAxis]) * keyFreePan.sensitivity;
                    }
                    if (keyFreeTilt.IsActivated()) {
                        _yRotation -= Input.GetAxis(keyboardAxesNames[(int)keyFreeTilt.keyboardAxis]) * keyFreeTilt.sensitivity;
                    }
                    if (keyFreeRoll.IsActivated()) {
                        _zRotation -= Input.GetAxis(keyboardAxesNames[(int)keyFreeRoll.keyboardAxis]) * keyFreeRoll.sensitivity;
                    }

                    _targetDirection = (_targetPoint - _transform.position).normalized;

                    // OPTIMIZE lets me change the values on the fly in the inspector
                    _cameraRotationDampener = settings.freeformRotationDampener;
                    _cameraPositionDampener = settings.freeformPositionDampener;
                    break;
                case CameraState.Follow:    // Follow as Spectator, not Chase
                    if (CheckExitConditions()) {
                        // exit on Mouse Scroll, Pan/Tilt or Key Escape
                        ChangeState(CameraState.Freeform);
                        return;
                    }

                    //showDistanceDebugLog = true;

                    // Smooth lookAt interpolation rotates the camera to continue to lookAt the moving Target. These
                    // values must be continuously updated as the Target and camera are moving
                    _targetPoint = _target.position;
                    _targetDirection = (_targetPoint - _transform.position).normalized;
                    lookAt = Quaternion.LookRotation(_targetDirection).eulerAngles;
                    _xRotation = lookAt.y;
                    _yRotation = lookAt.x;
                    _zRotation = lookAt.z;

                    // Smooth follow interpolation as spectator avoids moving away from the Target if it turns inside our optimal 
                    // follow distance. When the Target turns and breaks inside the optimal follow distance, stop the camera 
                    // from adjusting its position by making the requested distance the same as the actual distance. 
                    // As soon as the Target moves outside of the optimal distance, start following again.
                    _distanceFromTarget = Vector3.Distance(_targetPoint, _transform.position);
                    if (_distanceFromTarget > _optimalDistanceFromTarget) {
                        _requestedDistanceFromTarget = _optimalDistanceFromTarget;
                    }
                    else {
                        _requestedDistanceFromTarget = _distanceFromTarget;
                    }

                    // OPTIMIZE lets me change the values on the fly in the inspector
                    ICameraFollowable icfTarget = _target.GetInterface<ICameraFollowable>();
                    _cameraRotationDampener = icfTarget.CameraFollowRotationDampener;
                    _cameraPositionDampener = icfTarget.CameraFollowDistanceDampener;
                    break;
                case CameraState.None:
                default:
                    throw new NotImplementedException(ErrorMessages.UnanticipatedSwitchValue.Inject(cameraState));
            }

            _transform.rotation = CalculateCameraRotation(_cameraRotationDampener * timeSinceLastUpdate);
            //Logger.Log("RequestedDistanceFromTarget = {0}, MinimumDistanceFromTarget = {1}.".Inject(_requestedDistanceFromTarget, settings.minimumDistanceFromTarget));
            _requestedDistanceFromTarget = Mathf.Clamp(_requestedDistanceFromTarget, _minimumDistanceFromTarget, Mathf.Infinity);
            //Logger.Log("RequestedDistanceFromTarget = {0}.".Inject(_requestedDistanceFromTarget));

            _distanceFromTarget = Mathfx.Lerp(_distanceFromTarget, _requestedDistanceFromTarget, _cameraPositionDampener * timeSinceLastUpdate);
            if (showDistanceDebugLog) {
                Logger.Log("RequestedDistanceFromTarget = {0}, Actual DistanceFromTarget = {1}.".Inject(_requestedDistanceFromTarget, _distanceFromTarget));
                showDistanceDebugLog = false;
            }

            Vector3 _proposedPosition = _targetPoint - (_targetDirection * _distanceFromTarget);
            _transform.position = ValidatePosition(_proposedPosition);

            ManageCursorDisplay(_toLockCursor);
        }
    }

    private bool CheckExitConditions() {
        switch (cameraState) {
            case CameraState.Focusing:
                if (Mathfx.Approx(_targetDirection, _transform.forward, .01F)) {
                    return true;
                }
                return false;
            case CameraState.Focused:
                if (dragFreeTruck.IsActivated() || dragFreePedestal.IsActivated()) {
                    return true;
                }
                // can also exit on Scroll In on dummy Target
                return false;
            case CameraState.Follow:
                if (Input.GetKey(UnityConstants.Key_Escape)) {
                    return true;
                }
                if (dragFreePanTilt.IsActivated()) {
                    return true;
                }
                if (scrollFreeZoom.IsActivated()) {
                    return true;
                }
                return false;
            case CameraState.Freeform:
            case CameraState.None:
            default:
                throw new NotImplementedException(ErrorMessages.UnanticipatedSwitchValue.Inject(cameraState));
        }
    }

    /// <summary>
    /// Validates the proposed new position of the camera to be within the universe. If 
    /// it is not, the camera stays where it is at.
    /// </summary>
    /// <arg item="newPosition">The new position.</arg>
    /// <returns>if validated, returns newPosition. If not, return the current position.</returns>
    private Vector3 ValidatePosition(Vector3 newPosition) {
        float magnitude = (newPosition - GameConstants.UniverseOrigin).magnitude;
        if (magnitude > universeRadius) {
            return _transform.position;
        }
        return newPosition;
    }

    /// <summary>
    /// Manages the display of the cursor during certain movement actions.
    /// </summary>
    /// <param name="toLockCursor">if set to <c>true</c> [automatic lock cursor].</param>
    private void ManageCursorDisplay(bool toLockCursor) {
        if (toLockCursor && !Screen.lockCursor) {
            Screen.lockCursor = true;   // cursor disappears
        }
        else if (Screen.lockCursor && !toLockCursor) {
            Screen.lockCursor = false;  // cursor reappears in the center of the screen
        }
    }

    /// <summary>
    /// Attempts to assign an object found under the provided screenPoint as the new Target. If more than one object is found,
    /// then the closest object implementing iFocus (typically Cellestial Bodies and Ships) becomes the Target. If none of the objects
    /// found implements iFocus, then the farthest object implementing iCameraTarget is used. If the DummyTarget is found, or no 
    /// object at all is found, then the DummyTarget becomes the Target under the screenPoint at universe edge.
    /// </summary>
    /// <param name="screenPoint">The screen point.</param>
    /// <returns>
    /// true if the Target is changed, or if the dummyTarget has its location changed. false if the Target remains the same (or if the dummyTarget, its location remains the same).
    /// </returns>
    private bool TrySetTargetAtScreenPoint(Vector3 screenPoint) {
        Transform proposedZoomTarget;
        Vector3 proposedZoomPoint;
        Ray ray = camera.ScreenPointToRay(screenPoint);
        RaycastHit[] hits = Physics.RaycastAll(ray, Mathf.Infinity, collideWithOnlyCameraTargetsLayerMask);
        hits = (from h in hits where h.transform.GetInterface<ICameraTargetable>() != null && h.transform.GetInterface<ICameraTargetable>().IsTargetable select h).ToArray<RaycastHit>();
        if (!hits.IsNullOrEmpty<RaycastHit>()) {
            // one or more object under cursor encountered
            if (hits.Length == 1) {
                // the object encountered is likely to be the dummyTarget
                proposedZoomTarget = hits[0].transform;
                if (proposedZoomTarget == _dummyTarget) {
                    // the stationary, existing DummyTarget
                    return false;
                }
            }

            // NOTE: As Rigidbodies consume child collider events, a hit on a child collider when there is a rigidbody parent 
            // involved will return the transform of the parent, not the child. By not including inspection of the children for this interface,
            // I am requiring that the interface be present with the Rigidbody.

            var zoomToFurthestHits = from h in hits where h.transform.GetInterface<IZoomToFurthest>() != null select h;
            var remainingHits = hits.Except(zoomToFurthestHits.ToArray());
            if (!remainingHits.IsNullOrEmpty()) {
                // there is a hit that isn't a IZoomToFurthest, so pick the closest and done
                var closestHit = remainingHits.OrderBy(h => (h.transform.position - _transform.position).magnitude).First();
                proposedZoomTarget = closestHit.transform;
                proposedZoomPoint = proposedZoomTarget.position;
                return TryChangeTarget(proposedZoomTarget, proposedZoomPoint);
            }
            // otherwise, all hits are IZoomToFurthest, so pick the furthest and done
            var furthestHit = zoomToFurthestHits.OrderBy(h => (h.transform.position - _transform.position).magnitude).Last();
            proposedZoomTarget = furthestHit.transform;
            proposedZoomPoint = furthestHit.point;
            return TryChangeTarget(proposedZoomTarget, proposedZoomPoint);
        }

        // no game object encountered under cursor so move the dummy to the edge of the universe and designate it as the Target
        return TryPlaceDummyTargetAtUniverseEdgeInDirection(ray.direction);
    }

    /// <summary>
    /// Attempts to change the Target to the proposedTarget. If the existing Target is the same
    /// Target, then the change is aborted and the method returns false.
    /// </summary>
    /// <param name="proposedZoomTarget">The proposed Target. Logs an error if the DummyTarget.</param>
    /// <param name="proposedZoomPoint">The proposed Target point.</param>
    /// <returns>
    /// true if the Target was successfully changed, otherwise false.
    /// </returns>
    private bool TryChangeTarget(Transform proposedZoomTarget, Vector3 proposedZoomPoint) {
        if (proposedZoomTarget == _dummyTarget) {
            D.Error("TryChangeTarget must not be used to change to the DummyTarget.");
            return false;
        }

        if (proposedZoomTarget == _target) {
            //Logger.Log("Proposed Target {0} is already the existing Target.".Inject(Target.name));
            if (Mathfx.Approx(proposedZoomPoint, _targetPoint, settings.smallMovementThreshold)) {
                // the desired move of the Target point on the existing Target is too small to respond too
                return false;
            }
        }
        ChangeTarget(proposedZoomTarget, proposedZoomPoint);
        return true;
    }

    /// <summary>
    /// Attempts to place the dummy Target at the edge of the universe located in the direction provided.
    /// </summary>
    /// <param name="direction">The direction.</param>
    /// <returns>true if the DummyTarget was placed in a new location. False if it was not moved since it was already there.</returns>
    private bool TryPlaceDummyTargetAtUniverseEdgeInDirection(Vector3 direction) {
        direction.ValidateNormalized();
        Ray ray = new Ray(_transform.position, direction);
        RaycastHit targetHit;
        if (Physics.Raycast(ray, out targetHit, Mathf.Infinity, collideWithDummyTargetOnlyLayerMask.value)) {
            if (_dummyTarget != targetHit.transform) {
                D.Error("Camera should find DummyTarget, but it is: " + targetHit.transform.name);
                return false;
            }

            float distanceToUniverseOrigin = Vector3.Distance(_dummyTarget.position, GameConstants.UniverseOrigin);
            //Logger.Log("Dummy Target distance to origin = {0}.".Inject(distanceToUniverseOrigin));
            if (!distanceToUniverseOrigin.CheckRange(universeRadius, allowedPercentageVariation: 0.1F)) {
                D.Error("Camera's Dummy Target is not located on UniverseEdge! Position = " + _dummyTarget.position);
                return false;
            }
            // the dummy Target is already there
            //Logger.Log("DummyTarget already present at " + dummyTarget.position + ". TargetHit at " + targetHit.t.position);
            return false;
        }

        Vector3 pointOutsideUniverse = ray.GetPoint(universeRadius * 2);
        if (Physics.Raycast(pointOutsideUniverse, -ray.direction, out targetHit, Mathf.Infinity, collideWithUniverseEdgeOnlyLayerMask.value)) {
            Vector3 universeEdgePoint = targetHit.point;
            _dummyTarget.position = universeEdgePoint;
            ChangeTarget(_dummyTarget, _dummyTarget.position);
            //Logger.Log("New DummyTarget location = " + universeEdgePoint);
            _requestedDistanceFromTarget = _distanceFromTarget;
            return true;
        }

        D.Error("Camera has not found a Universe Edge point! PointOutsideUniverse = " + pointOutsideUniverse);
        return false;
    }

    /// <summary>
    /// Calculates a new rotation derived from the current EulerAngles.
    /// </summary>
    /// <arg name="dampenedTimeSinceLastUpdate">The dampened adjusted time since last update.</arg>
    /// <returns></returns>
    private Quaternion CalculateCameraRotation(float dampenedTimeSinceLastUpdate) {
        // keep rotation values exact as a substitute for the unreliable? accuracy that comes from reading EulerAngles from the Quaternion
        _xRotation %= 360;
        _yRotation %= 360;
        _zRotation %= 360;
        //Logger.Log("Rotation input values: x = {0}, y = {1}, z = {2}.".Inject(_xRotation, _yRotation, _zRotation));
        Quaternion desiredRotation = Quaternion.Euler(_yRotation, _xRotation, _zRotation);
        //Vector3 lookAtVector = desiredRotation.eulerAngles;
        //float xRotation = lookAtVector.y;
        //float yRotation = lookAtVector.x;
        //float zRotation = lookAtVector.z;
        //Logger.Log("After Quaternion conversion: x = {0}, y = {1}, z = {2}.".Inject(xRotation, yRotation, zRotation));
        Quaternion resultingRotation = Quaternion.Slerp(_transform.rotation, desiredRotation, dampenedTimeSinceLastUpdate);
        // OPTIMIZE Lerp is faster but not as pretty when the rotation changes are far apart
        return resultingRotation;
    }

    #endregion

    #region Debug

    private void CheckScriptCompilerSettings() {
        Logger.Log("Compiler Preprocessor Settings in {0} follow:".Inject(Instance.GetType().Name) + Constants.NewLine);
#if DEBUG
        Logger.Log("DEBUG, ");
#endif

#if UNITY_EDITOR
            Logger.Log("UNITY_EDITOR, ");
#endif

#if DEBUG_WARN
        Logger.Log("DEBUG_WARN, ");
#endif

#if DEBUG_ERROR
        Logger.Log("DEBUG_ERROR, ");
#endif

#if DEBUG_LOG
        Logger.Log("DEBUG_LOG, ");
#endif
    }


    [System.Diagnostics.Conditional("UNITY_EDITOR")]
    private void _UpdateDebugHud(DebugHudLineKeys key, string text) {
        Logger.Log("CameraState changed to " + cameraState);
        DebugHud.Instance.Publish(key, text);
    }

    #endregion

    public override string ToString() {
        return new ObjectAnalyzer().ToString(this);
    }

    #region IDisposable
    private bool alreadyDisposed = false;

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
    /// <arg item="isDisposing"><c>true</c> to release both managed and unmanaged resources; <c>false</c> to release only unmanaged resources.</arg>
    protected virtual void Dispose(bool isDisposing) {
        // Allows Dispose(isDisposing) to be called more than once
        if (alreadyDisposed) {
            return;
        }

        if (isDisposing) {
            // free managed resources here including unhooking events
            Unsubscribe();
        }
        // free unmanaged resources here
        alreadyDisposed = true;
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

    #region Nested Classes

    // State
    private enum CameraState {
        None = 0,
        /// <summary>
        /// Transitional state preceeding Focused allowing the camera's approach to the selected focus 
        /// game object to complete before the input controls are enabled in Focused
        /// </summary>
        Focusing = 1,
        Focused = 2,
        Freeform = 3,
        Follow = 4
    }

    /// <summary>
    /// Keyboard Axis values as defined in Unity.
    /// </summary>
    public enum KeyboardAxis {
        Horizontal = 0,
        Vertical = 1,
        None = 3
    }

    [Serializable]
    // Settings isTargetVisibleThisFrame in the Inspector so they can be tweaked
    public class Settings {
        public float activeScreenEdge;
        public float smallMovementThreshold;
        public float maxSpeedGovernorDivider;
        // damping
        public float focusingRotationDampener;
        public float focusingPositionDampener;
        public float focusedRotationDampener;
        public float focusedPositionDampener;
        public float freeformRotationDampener;
        public float freeformPositionDampener;
        internal float MaxSpeedGovernor {
            get {
                return universeRadius / maxSpeedGovernorDivider;
            }
        }
    }

    [Serializable]
    // Handles modifiers keys (Alt, Ctrl, Shift and Apple)
    public class Modifiers {
        public bool altKeyReqd;
        public bool ctrlKeyReqd;
        public bool shiftKeyReqd;
        public bool appleKeyReqd;

        internal bool confirmModifierKeyState() { // ^ = Exclusive OR
            return (!altKeyReqd ^ (Input.GetKey(KeyCode.LeftAlt) || Input.GetKey(KeyCode.RightAlt))) &&
                (!ctrlKeyReqd ^ (Input.GetKey(KeyCode.LeftControl) || Input.GetKey(KeyCode.RightControl))) &&
                (!shiftKeyReqd ^ (Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift))) &&
                (!appleKeyReqd ^ (Input.GetKey(KeyCode.LeftApple) || Input.GetKey(KeyCode.RightApple)));
        }
    }

    [Serializable]
    // Defines Camera Controls using 1Mouse Button
    public class MouseButtonConfiguration : ConfigurationBase {
        public NguiMouseButton mouseButton;

        internal override float InputControlSpeedNormalizer {
            get { return 4.0F * universeRadius; }
            set { }
        }

        internal override bool IsActivated() {
            return base.IsActivated() && gameInput.isDragging && GameInputHelper.IsMouseButtonDown(mouseButton) && !GameInputHelper.IsAnyMouseButtonDownBesides(mouseButton);
        }
    }

    [Serializable]
    // Defines Camera Controls using 2 simultaneous Mouse Buttons
    public class SimultaneousMouseButtonConfiguration : ConfigurationBase {
        public NguiMouseButton firstMouseButton;
        public NguiMouseButton secondMouseButton;

        internal override float InputControlSpeedNormalizer {
            get { return 0.4F * universeRadius; }
            set { }
        }

        internal override bool IsActivated() {
            return base.IsActivated() && gameInput.isDragging && GameInputHelper.IsMouseButtonDown(firstMouseButton) && GameInputHelper.IsMouseButtonDown(secondMouseButton);
        }
    }

    [Serializable]
    // Defines Screen Edge Camera controls
    public class ScreenEdgeConfiguration : ConfigurationBase {

        internal override float InputControlSpeedNormalizer {
            get { return 0.02F * universeRadius; }
            set { }
        }

        internal override bool IsActivated() {
            return base.IsActivated() && !GameInputHelper.IsAnyKeyOrMouseButtonDown();
        }
    }

    [Serializable]
    // Defines Mouse Scroll Wheel Camera Controls
    public class MouseScrollWheelConfiguration : ConfigurationBase {

        internal override float InputControlSpeedNormalizer {
            get { return 0.1F * universeRadius; }
            set { }
        }

        internal override bool IsActivated() {
            return base.IsActivated() && gameInput.isScrolling && !GameInputHelper.IsAnyKeyOrMouseButtonDown();
        }
    }

    [Serializable]
    // Defines the movement associated with the Arrow Keys on the Keyboard
    public class ArrowKeyboardConfiguration : ConfigurationBase {
        public KeyboardAxis keyboardAxis;
        internal override float InputControlSpeedNormalizer {
            get { return 0.002F * universeRadius; }
            set { }
        }

        private bool IsAxisKeyInUse() {
            KeyCode arrowKey = gameInput.GetArrowKey();
            switch (keyboardAxis) {
                case KeyboardAxis.Horizontal:
                    return arrowKey == KeyCode.LeftArrow || arrowKey == KeyCode.RightArrow;
                case KeyboardAxis.Vertical:
                    return arrowKey == KeyCode.UpArrow || arrowKey == KeyCode.DownArrow;
                case KeyboardAxis.None:
                default:
                    throw new NotImplementedException(ErrorMessages.UnanticipatedSwitchValue.Inject(keyboardAxis));
            }
        }

        internal override bool IsActivated() {
            return base.IsActivated() && gameInput.isArrowKeyPressed && IsAxisKeyInUse();
        }
    }

    [Serializable]
    [HideInInspector]
    public abstract class ConfigurationBase {
        public bool activate;
        public Modifiers modifiers = new Modifiers();
        public float sensitivity = 1.0F;

        /// <summary>
        /// This factor is used to normalize the translation movement speed of different input controls (keys, screen edge and mouse dragging)
        /// so that roughly the same distance is covered in a set period of time. The current implementation is a function of
        /// the size of the universe. At this time, this factor is not used to normalize rotation gameSpeed.
        /// </summary>
        internal abstract float InputControlSpeedNormalizer { get; set; }

        internal virtual bool IsActivated() {
            return activate && modifiers.confirmModifierKeyState();
        }
    }
    #endregion


}

