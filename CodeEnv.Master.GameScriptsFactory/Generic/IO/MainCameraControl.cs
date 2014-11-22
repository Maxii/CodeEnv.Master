// --------------------------------------------------------------------------------------------------------------------
// <copyright>
// Copyright © 2012 - 2013 Strategic Forge
//
// Email: jim@strategicforge.com
// </copyright> 
// <summary> 
// File: MainCameraControl.cs
// Singleton. Main Camera movement control.
// </summary> 
// -------------------------------------------------------------------------------------------------------------------- 

#define DEBUG_LOG
#define DEBUG_WARN
#define DEBUG_ERROR

// default namespace

using System;
using System.Collections.Generic;
using System.Linq;
using CodeEnv.Master.Common;
using CodeEnv.Master.Common.LocalResources;
using CodeEnv.Master.GameContent;
using UnityEngine;
using UnityEditor;

/// <summary>
/// Singleton. Main Camera movement control.
/// </summary>
//public class MainCameraControl : AMonoStateMachineSingleton<MainCameraControl, MainCameraControl.CameraState>, ICameraControl {
public class MainCameraControl : AFSMSingleton_NoCall<MainCameraControl, MainCameraControl.CameraState>, ICameraControl {

    #region Camera Control Configurations
    // WARNING: Initializing non-Mono classes declared in a Mono class, outside of Awake or Start causes them to be instantiated by Unity AT EDITOR TIME (aka before runtime). 
    //This means that their constructors will be called before ANYTHING else is called. Script execution order is irrelevant.

    // Focused Zooming: When focused, top and bottom Edge zooming and arrow key zooming cause camera movement in and out from the focused object that is centered on the screen. 
    // ScrollWheel zooming normally does the same if the cursor is pointed at the focused object. If the cursor is pointed somewhere else, scrolling IN moves toward the cursor resulting 
    // in a change to Freeform scrolling. By default, Freeform scrolling OUT is directly opposite the camera's facing. However, there is an option to scroll OUT from the cursor instead. 
    // If this is selected, then scrolling OUT while the cursor is not pointed at the focused object will also result in Freeform scrolling.
    public ScreenEdgeConfiguration edgeFocusZoom = new ScreenEdgeConfiguration { screenEdgeAxis = ScreenEdgeAxis.TopBottom, sensitivity = 2F, activate = false };
    public MouseScrollWheelConfiguration scrollFocusZoom = new MouseScrollWheelConfiguration { sensitivity = 1F, activate = true };
    public ArrowKeyboardConfiguration keyFocusZoom = new ArrowKeyboardConfiguration { keyboardAxis = KeyboardAxis.Vertical, sensitivity = 1F, activate = true };
    public SimultaneousMouseButtonDragConfiguration dragFocusZoom = new SimultaneousMouseButtonDragConfiguration { firstMouseButton = NguiMouseButton.Left, secondMouseButton = NguiMouseButton.Right, sensitivity = 1F, activate = true };

    // Freeform Zooming: When not focused, top and bottom Edge zooming and arrow key zooming cause camera movement forward or backward along the camera's facing.
    // ScrollWheel zooming on the other hand always moves toward the cursor when scrolling IN. By default, scrolling OUT is directly opposite
    // the camera's facing. However, there is an option to scroll OUT from the cursor instead. 
    public ScreenEdgeConfiguration edgeFreeZoom = new ScreenEdgeConfiguration { screenEdgeAxis = ScreenEdgeAxis.TopBottom, sensitivity = 2F, activate = false };
    public ArrowKeyboardConfiguration keyFreeZoom = new ArrowKeyboardConfiguration { sensitivity = 1F, keyboardAxis = KeyboardAxis.Vertical, activate = true };
    public MouseScrollWheelConfiguration scrollFreeZoom = new MouseScrollWheelConfiguration { sensitivity = 1F, activate = true };
    public SimultaneousMouseButtonDragConfiguration dragFreeZoom = new SimultaneousMouseButtonDragConfiguration { firstMouseButton = NguiMouseButton.Left, secondMouseButton = NguiMouseButton.Right, sensitivity = 1F, activate = true };

    // Panning, Tilting and Orbiting: When focused, edge actuation, arrow key pan and tilting and mouse button/movement results in orbiting of the focused object that is centered on the screen. 
    // When not focused the same arrow keys, edge actuation and mouse button/movement results in the camera panning (looking left or right) and tilting (looking up or down) in place.
    public ScreenEdgeConfiguration edgeFreePan = new ScreenEdgeConfiguration { screenEdgeAxis = ScreenEdgeAxis.LeftRight, sensitivity = 10F, activate = true };
    public ScreenEdgeConfiguration edgeFreeTilt = new ScreenEdgeConfiguration { screenEdgeAxis = ScreenEdgeAxis.TopBottom, sensitivity = 10F, activate = true };
    public ScreenEdgeConfiguration edgeFocusOrbitPan = new ScreenEdgeConfiguration { screenEdgeAxis = ScreenEdgeAxis.LeftRight, sensitivity = 10F, activate = true };
    public ScreenEdgeConfiguration edgeFocusOrbitTilt = new ScreenEdgeConfiguration { screenEdgeAxis = ScreenEdgeAxis.TopBottom, sensitivity = 10F, activate = true };
    public ArrowKeyboardConfiguration keyFreePan = new ArrowKeyboardConfiguration { keyboardAxis = KeyboardAxis.Horizontal, sensitivity = 1F, activate = true };
    public ArrowKeyboardConfiguration keyFreeTilt = new ArrowKeyboardConfiguration { keyboardAxis = KeyboardAxis.Vertical, modifiers = new KeyModifiers { ctrlKeyReqd = true, shiftKeyReqd = true }, sensitivity = 1F, activate = true };
    public ArrowKeyboardConfiguration keyFocusPan = new ArrowKeyboardConfiguration { keyboardAxis = KeyboardAxis.Horizontal, sensitivity = 1F, activate = true };
    public ArrowKeyboardConfiguration keyFocusTilt = new ArrowKeyboardConfiguration { keyboardAxis = KeyboardAxis.Vertical, modifiers = new KeyModifiers { ctrlKeyReqd = true, shiftKeyReqd = true }, sensitivity = 1F, activate = true };

    public MouseButtonDragConfiguration dragFocusOrbit = new MouseButtonDragConfiguration { mouseButton = NguiMouseButton.Right, sensitivity = 5F, activate = true };
    public MouseButtonDragConfiguration dragFreePanTilt = new MouseButtonDragConfiguration { mouseButton = NguiMouseButton.Right, sensitivity = 3F, activate = true };

    // Truck and Pedestal: Trucking (moving left and right) and Pedestalling (moving up and down) occurs only in Freeform space, repositioning the camera along it's current horizontal and vertical axis'.
    public MouseButtonDragConfiguration dragFreeTruck = new MouseButtonDragConfiguration { mouseButton = NguiMouseButton.Middle, modifiers = new KeyModifiers { altKeyReqd = true }, sensitivity = 1F, activate = true };
    public MouseButtonDragConfiguration dragFreePedestal = new MouseButtonDragConfiguration { mouseButton = NguiMouseButton.Middle, modifiers = new KeyModifiers { shiftKeyReqd = true }, sensitivity = 1F, activate = true };
    public ArrowKeyboardConfiguration keyFreePedestal = new ArrowKeyboardConfiguration { keyboardAxis = KeyboardAxis.Vertical, modifiers = new KeyModifiers { ctrlKeyReqd = true }, activate = true };
    public ArrowKeyboardConfiguration keyFreeTruck = new ArrowKeyboardConfiguration { keyboardAxis = KeyboardAxis.Horizontal, modifiers = new KeyModifiers { ctrlKeyReqd = true }, activate = true };

    // Rolling: Focused and freeform rolling results in the same behaviour, rolling around the camera's current forward axis.
    public MouseButtonDragConfiguration dragFocusRoll = new MouseButtonDragConfiguration { mouseButton = NguiMouseButton.Right, modifiers = new KeyModifiers { altKeyReqd = true }, sensitivity = 5F, activate = true };
    public MouseButtonDragConfiguration dragFreeRoll = new MouseButtonDragConfiguration { mouseButton = NguiMouseButton.Right, modifiers = new KeyModifiers { altKeyReqd = true }, sensitivity = 5F, activate = true };
    public ArrowKeyboardConfiguration keyFreeRoll = new ArrowKeyboardConfiguration { keyboardAxis = KeyboardAxis.Horizontal, modifiers = new KeyModifiers { ctrlKeyReqd = true, shiftKeyReqd = true }, activate = true };
    public ArrowKeyboardConfiguration keyFocusRoll = new ArrowKeyboardConfiguration { keyboardAxis = KeyboardAxis.Horizontal, modifiers = new KeyModifiers { ctrlKeyReqd = true, shiftKeyReqd = true }, activate = true };

    #endregion

    // LEARNINGS
    // Edge-based requested values need to be normalized for framerate using timeSinceLastUpdate as the changeValue is a defacto 1 per frame by definition.
    // Key-based requested values DONOT need to be normalized for framerate using timeSinceLastUpdate as Input.GetAxis() is not framerate dependant.
    // NguiEvents: The values of dragDelta and scrollWheelDelta increase/decrease with lower/higher framerates so the change per second is not framerate dependant.
    // Using requestedDistanceToTarget as a scaler when determining the requested position change to make increases/decreases the requested movement 
    //     the further/closer the camera is to the target.
    // No need to include UniverseRadius in camera movement calculations as the size of the universe is already accounted for by the distance to the dummyTarget.

    // IMPROVE
    // Should Tilt/EdgePan have some Pedastal/Truck added like Star Ruler?
    // Need more elegant rotation and translation functions when selecting a focusTarget - aka Slerp, Mathf.SmoothDamp/Angle, etc. see my Mathfx, Radical's Easing
    // Dragging the mouse with any button held down works offscreen OK, but upon release offscreen, immediately enables edge scrolling and panning
    // Implement Camera controls such as clip planes, FieldOfView, RenderSettings.[flareStrength, haloStrength, ambientLight]

    #region Fields

    private Index3D _sectorIndex;
    /// <summary>
    /// Readonly. The location of the camera in sector space.
    /// </summary>
    public Index3D SectorIndex {
        get { return _sectorIndex; }
        private set { SetProperty<Index3D>(ref _sectorIndex, value, "SectorIndex"); }
    }

    /// <summary>
    /// The position of the camera in world space.
    /// </summary>
    public Vector3 Position {
        get { return _transform.position; }
        private set { _transform.position = value; }
    }

    /// <summary>
    /// The distance from the camera's target point to the camera's focal plane.
    /// </summary>
    public float DistanceToCamera { get { return _targetPoint.DistanceToCamera(); } }

    private ICameraFocusable _currentFocus;
    /// <summary>
    /// The object the camera is currently focused on if it has one.
    /// </summary>
    public ICameraFocusable CurrentFocus {
        get { return _currentFocus; }
        set { SetProperty<ICameraFocusable>(ref _currentFocus, value, "CurrentFocus", OnCurrentFocusChanged, OnCurrentFocusChanging); }
    }

    public Settings settings = new Settings {
        smallMovementThreshold = 2F,
        focusingPositionDampener = 2.0F, focusingRotationDampener = 1.0F, focusedPositionDampener = 4.0F,
        focusedRotationDampener = 2.0F, freeformPositionDampener = 3.0F, freeformRotationDampener = 2.0F
    };

    private bool _isResetOnFocusEnabled;
    private bool _isZoomOutOnCursorEnabled;    // ScrollWheel always zooms IN on cursor, zooming OUT with the ScrollWheel is directly backwards by default

    // Cached references
    [DoNotSerialize]    // Serializing this creates duplicates of this object on Save
    private PlayerPrefsManager _playerPrefsMgr;
    private Camera _camera;
    private InputManager _inputMgr;
    private SectorGrid _sectorGrid;
    private GameManager _gameMgr;

    private IList<IDisposable> _subscribers;

    private Vector3 _targetPoint;
    private Transform _target;
    private Transform _dummyTarget;

    private float _universeRadius;

    // not currently used
    //private string[] keyboardAxesNames = new string[] { UnityConstants.KeyboardAxisName_Horizontal, UnityConstants.KeyboardAxisName_Vertical };

    /// <summary>
    /// The layers the main 3DCamera is allowed to render.
    /// </summary>
    private LayerMask _mainCameraCullingMask = LayerMaskExtensions.CreateInclusiveMask(Layers.Default, Layers.TransparentFX,
        Layers.DummyTarget, Layers.UniverseEdge, Layers.Ship, Layers.Facility, Layers.Planetoid, Layers.Star, Layers.SystemOrbitalPlane);

    private LayerMask _universeEdgeOnlyMask = LayerMaskExtensions.CreateInclusiveMask(Layers.UniverseEdge);
    private LayerMask _dummyTargetOnlyMask = LayerMaskExtensions.CreateInclusiveMask(Layers.DummyTarget);

    private Vector3 _screenCenter = new Vector3(Screen.width / 2, Screen.height / 2, 0F);

    // Continuously calculated, actual Camera values
    private float _distanceFromTarget;
    private Vector3 _targetDirection;

    // Desired Camera values requested via input controls
    private float _xAxisRotation;    // EulerAngles
    private float _yAxisRotation;
    private float _zAxisRotation;
    private float _requestedDistanceFromTarget;

    // Fields used in algorithms that can vary by Target or CameraState
    private float _minimumDistanceFromTarget;
    private float _optimalDistanceFromTarget;
    private float _cameraPositionDampener;
    private float _cameraRotationDampener;

    #endregion

    #region Temporary UnityEditor Controls
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
        //D.Log("Edge Pan.active = {0}.", edgeFreePan.activate);
    }

    private void __AssessEnabled() {
        enabled = _gameMgr.IsRunning && !EditorApplication.isPaused;
    }

    #endregion

    #region Initialization

    protected override void InitializeOnInstance() {
        base.InitializeOnInstance();
        References.MainCameraControl = Instance;
    }

    protected override void InitializeOnAwake() {
        base.InitializeOnAwake();
        InitializeReferences();
        Subscribe();
        ValidateActiveConfigurations();
        __InitializeDebugEdgeMovementSettings();
        enabled = false;
    }

    private void InitializeReferences() {
        //if (LevelSerializer.IsDeserializing) { return; }
        _camera = UnityUtility.ValidateComponentPresence<Camera>(gameObject);
        _playerPrefsMgr = PlayerPrefsManager.Instance;
        _inputMgr = InputManager.Instance;
        _sectorGrid = SectorGrid.Instance;
        _gameMgr = GameManager.Instance;
    }

    private void Subscribe() {
        _subscribers = new List<IDisposable>();
        _subscribers.Add(_gameMgr.SubscribeToPropertyChanged<GameManager, GameState>(gm => gm.CurrentState, OnGameStateChanged));
        _subscribers.Add(_playerPrefsMgr.SubscribeToPropertyChanged<PlayerPrefsManager, bool>(pm => pm.IsCameraRollEnabled, OnCameraRollEnabledChanged));
        _subscribers.Add(_playerPrefsMgr.SubscribeToPropertyChanged<PlayerPrefsManager, bool>(pm => pm.IsResetOnFocusEnabled, OnResetOnFocusEnabledChanged));
        _subscribers.Add(_playerPrefsMgr.SubscribeToPropertyChanged<PlayerPrefsManager, bool>(pm => pm.IsZoomOutOnCursorEnabled, OnZoomOutOnCursorEnabledChanged));
        _subscribers.Add(_gameMgr.SubscribeToPropertyChanged<GameManager, bool>(gs => gs.IsRunning, OnIsRunningChanged));
        //_subscribers.Add(PlayerViews.Instance.SubscribeToPropertyChanged<PlayerViews, PlayerViewMode>(pv => pv.ViewMode, OnViewModeChanged));
    }

    private void ValidateActiveConfigurations() {
        bool isValid = true;
        if ((edgeFocusOrbitTilt.activate && edgeFocusZoom.activate) || (edgeFreeTilt.activate && edgeFreeZoom.activate)) {
            isValid = false;
        }
        D.Assert(isValid, "Incompatable Camera Configuration.", pauseOnFail: true);
    }

    private void InitializeMainCamera() {   // called from OnGameStateChanged()
        InitializeFields();
        SetCameraSettings();
        InitializeCameraPreferences();
        PositionCameraForGame();
    }

    private void InitializeFields() {
        _universeRadius = _gameMgr.GameSettings.UniverseSize.Radius();
        UpdateRate = FrameUpdateFrequency.Continuous;
    }

    private void SetCameraSettings() {
        // assumes radius of universe is twice that of the galaxy so the furthest system in the galaxy should be at a distance1.5 times the radius of the universe
        _camera.orthographic = false;
        _camera.fieldOfView = 50F;
        _camera.nearClipPlane = 0.02F;
        _camera.farClipPlane = _universeRadius * 2F;
        _camera.depth = -1F;    // important as this determines which camera is the UICamera.eventHandler, uiCamera = 1F

        _camera.cullingMask = _mainCameraCullingMask;

        //_camera.layerCullSpherical = true;
        float[] cullDistances = new float[32];
        cullDistances[(int)Layers.Ship] = TempGameValues.ShipMaxRadius * AnimationSettings.Instance.ShipLayerCullingDistanceFactor;   // 5
        cullDistances[(int)Layers.Facility] = TempGameValues.FacilityMaxRadius * AnimationSettings.Instance.FacilityLayerCullingDistanceFactor;  // 12.5
        cullDistances[(int)Layers.Planetoid] = TempGameValues.PlanetoidMaxRadius * AnimationSettings.Instance.PlanetoidLayerCullingDistanceFactor;   // 500
        cullDistances[(int)Layers.Star] = TempGameValues.StarMaxRadius * AnimationSettings.Instance.StarLayerCullingDistanceFactor; // 3000
        _camera.layerCullDistances = cullDistances;
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
        keyFocusRoll.activate = toEnable;
    }

    private void PositionCameraForGame() {
        CreateUniverseEdge();
        CreateDummyTarget();

        // HACK start looking down from a far distance
        float yElevation = _universeRadius * 0.3F;
        float zDistance = -_universeRadius * 0.75F;
        Position = new Vector3(0F, yElevation, zDistance);
        _sectorIndex = _sectorGrid.GetSectorIndex(Position);
        _transform.rotation = Quaternion.Euler(new Vector3(20F, 0F, 0F));

        ResetAtCurrentLocation();
        // UNDONE whether starting or continuing saved game, camera position should be focused on the player's starting planet, no rotation
        //ResetToWorldspace();
    }

    private void CreateUniverseEdge() {
        GameObject universeEdge = null;
        SphereCollider universeEdgePrefab = RequiredPrefabs.Instance.universeEdge;
        if (universeEdgePrefab == null) {
            D.Warn("UniverseEdgePrefab on RequiredPrefabs is null.");
            string universeEdgeName = Layers.UniverseEdge.GetName();
            universeEdge = new GameObject(universeEdgeName);
            universeEdge.AddComponent<SphereCollider>();
            universeEdge.isStatic = true;
            UnityUtility.AttachChildToParent(universeEdge, UniverseFolder.Instance.Folder.gameObject);
        }
        else {
            universeEdge = NGUITools.AddChild(UniverseFolder.Instance.Folder.gameObject, universeEdgePrefab.gameObject);
        }
        (universeEdge.collider as SphereCollider).radius = _universeRadius;
        universeEdge.layer = (int)Layers.UniverseEdge;
    }

    private void CreateDummyTarget() {
        Transform dummyTargetPrefab = RequiredPrefabs.Instance.cameraDummyTarget;
        GameObject dummyTarget;
        if (dummyTargetPrefab == null) {
            D.Warn("DummyTargetPrefab on RequiredPrefabs is null.");
            string dummyTargetName = Layers.DummyTarget.GetName();
            dummyTarget = new GameObject(dummyTargetName);
            dummyTarget.AddComponent<SphereCollider>();
            dummyTarget.AddComponent<DummyTargetManager>();
        }
        else {
            dummyTarget = NGUITools.AddChild(DynamicObjectsFolder.Instance.Folder.gameObject, dummyTargetPrefab.gameObject);
        }
        dummyTarget.layer = (int)Layers.DummyTarget;
        _dummyTarget = dummyTarget.transform;
    }

    /// <summary>
    /// Resets the camera to its base state without changing its location
    /// or facing. Base state is Freeform with its target set as the
    /// DummyTarget positioned directly ahead. 
    /// </summary>
    private void ResetAtCurrentLocation() {
        _dummyTarget.collider.enabled = false;
        // the collider is disabled so the placement algorithm doesn't accidently find it already in front of the camera
        PlaceDummyTargetAtUniverseEdgeInDirection(_transform.forward);
        _dummyTarget.collider.enabled = true;
        SyncRotation();
        CurrentState = CameraState.Freeform;
    }

    private void SyncRotation() {
        Quaternion startingRotation = _transform.rotation;
        Vector3 startingEulerRotation = startingRotation.eulerAngles;
        _xAxisRotation = startingEulerRotation.x;
        _yAxisRotation = startingEulerRotation.y;
        _zAxisRotation = startingEulerRotation.z;
    }

    #endregion

    #region Event Handlers

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
        GameState state = _gameMgr.CurrentState;
        switch (state) {
            case GameState.Restoring:
                // only saved games that are being restored enter Restoring state
                _restoredGameFlag = true;
                break;
            case GameState.Waiting:
                _gameMgr.RecordGameStateProgressionReadiness(this, GameState.Waiting, isReady: false);
                if (_restoredGameFlag) {
                    // for a restored game, Waiting state is guaranteed to occur after OnDeserialized so we must be ready to proceed
                    _restoredGameFlag = false;
                }
                else {
                    InitializeMainCamera(); // deferred init until clear game is new
                }
                _gameMgr.RecordGameStateProgressionReadiness(this, GameState.Waiting, isReady: true);
                break;
            case GameState.Lobby:
            case GameState.Building:
            case GameState.Loading:
            case GameState.BuildAndDeploySystems:
            case GameState.GeneratingPathGraphs:
            case GameState.PrepareUnitsForDeployment:
            case GameState.DeployingUnits:
            case GameState.RunningCountdown_1:
            case GameState.Running:
                // do nothing
                break;
            case GameState.None:
            default:
                throw new NotImplementedException(ErrorMessages.UnanticipatedSwitchValue.Inject(state));
        }
    }

    private void OnIsRunningChanged() {
        __AssessEnabled();
    }

    // Not currently used. Keep this for now as I expect there will be other reasons to modify camera behaviour during special modes.
    private void OnViewModeChanged() {
        bool toActivateDragging;
        switch (PlayerViews.Instance.ViewMode) {
            case PlayerViewMode.SectorView:
                toActivateDragging = false;
                break;
            case PlayerViewMode.NormalView:
                toActivateDragging = true;
                break;
            case PlayerViewMode.None:
            default:
                throw new NotImplementedException(ErrorMessages.UnanticipatedSwitchValue.Inject(PlayerViews.Instance.ViewMode.GetName()));
        }

        // edgeFocusZoom already false
        // scrollFocusZoom keep true
        // keyFocusZoom keep true
        dragFocusZoom.activate = toActivateDragging;
        // edgeFreeZoom already false
        // keyFreeZoom keep true
        // scrollFreeZoom keep true
        dragFreeZoom.activate = toActivateDragging;
        // edgeFreePan keep true
        // edgeFreeTilt keep true
        // edgeFocusOrbitPan keep true
        // edgeFocusOrbitTilt keep true
        // keyFreePan keep true
        // keyFreeTilt keep true
        // keyFocusPan keep true
        // keyFocusTilt keep true
        dragFocusOrbit.activate = toActivateDragging;
        dragFreePanTilt.activate = toActivateDragging;
        dragFreeTruck.activate = toActivateDragging;
        dragFreePedestal.activate = toActivateDragging;
        // keyFreePedestal keep true
        // keyFreeTruck keep true
        dragFocusRoll.activate = toActivateDragging;
        dragFreeRoll.activate = toActivateDragging;
        // keyFreeRoll keep true
        // keyFocusRoll keep true
    }

    private void OnCurrentFocusChanging(ICameraFocusable newFocus) {
        if (CurrentFocus != null) {
            CurrentFocus.IsFocus = false;
        }
    }

    private void OnCurrentFocusChanged() {
        if (CurrentFocus != null) {
            Transform newFocus = CurrentFocus.Transform;
            D.Log("New Focus is now {0}.", newFocus.name);
            SetFocus(newFocus);
        }
        else if (CurrentState != CameraState.Freeform) {
            // CurrentFocus set to null while focused or following so switch to freeform
            ResetAtCurrentLocation();
        }
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
        D.Log("Camera OnApplicationFocus({0}) called.", isFocus);
        __EnableEdgePanTiltInEditor(isFocus);
    }

    /// <summary>
    /// Called when the application is minimized/resumed, this method controls the enabled
    /// state of the camera so it doesn't move when I use the mouse to minimize Unity.
    /// </summary>
    /// <param name="isPaused">if set to <c>true</c> [is paused].</param>
    void OnApplicationPause(bool isPaused) {
        //D.Log("Camera OnApplicationPause(" + isPaused + ") called.");
        __AssessEnabled();
    }

    #endregion

    #region Camera StateMachine

    #region Focusing

    void Focusing_EnterState() {
        LogEvent();
        Arguments.ValidateNotNull(_targetPoint);

        _distanceFromTarget = Vector3.Distance(_targetPoint, Position);
        _requestedDistanceFromTarget = _optimalDistanceFromTarget;
        _targetDirection = (_targetPoint - Position).normalized;

        // face the selected Target
        Quaternion lookAt = Quaternion.LookRotation(_targetDirection);
        Vector3 lookAtVector = lookAt.eulerAngles;
        _yAxisRotation = lookAtVector.y;
        _xAxisRotation = lookAtVector.x;
        _zAxisRotation = lookAtVector.z;

        _cameraRotationDampener = settings.focusingRotationDampener;
        _cameraPositionDampener = settings.focusingPositionDampener;

        LockCursor(true);
    }

    //void Focusing_Update() {
    //    UpdateCamera_Focusing();
    //}

    void Focusing_LateUpdate() {
        UpdateCamera_Focusing();
    }

    //void Focusing_FixedUpdate() {
    //    UpdateCamera_Focusing();
    //}

    private void UpdateCamera_Focusing() {
        // transition process to allow lookAt to complete. Only entered from OnFocusSelected, when !IsResetOnFocus
        if (_targetDirection.IsSameDirection(_transform.forward, 1F)) {
            // exits when the lookAt rotation is complete
            CurrentState = CameraState.Focused;
            return;
        }

        // The desired (x,y,z) rotation to LookAt the Target and the requested distance from the Target
        // is set in EnterState and does not need to be updated to get there as the Target doesn't move

        // no other functionality active 
        ProcessChanges(GetTimeSinceLastUpdate());
    }

    void Focusing_ExitState() {
        LogEvent();
        LockCursor(false);
    }

    #endregion

    #region Focused

    void Focused_EnterState() {
        LogEvent();
        Arguments.ValidateNotNull(_targetPoint);
        // entered via OnFocusSelected AND IsResetOnFocusEnabled, OR after Focusing has completed
        _distanceFromTarget = Vector3.Distance(_targetPoint, Position);
        _requestedDistanceFromTarget = _optimalDistanceFromTarget;
        // x,y,z rotation has already been established before entering ??? FIXME where???

        _cameraRotationDampener = settings.focusedRotationDampener;
        _cameraPositionDampener = settings.focusedPositionDampener;
    }

    //void Focused_Update() {
    //    UpdateCamera_Focused();
    //}

    void Focused_LateUpdate() {
        UpdateCamera_Focused();
    }

    //void Focused_FixedUpdate() {
    //    UpdateCamera_Focused();
    //}

    private void UpdateCamera_Focused() {
        if (dragFreeTruck.IsActivated() || dragFreePedestal.IsActivated()) {
            // can also exit on Scroll In on dummy Target
            CurrentState = CameraState.Freeform;
            return;
        }

        float timeSinceLastUpdate = GetTimeSinceLastUpdate();
        // the input value determined by number of mouseWheel ticks, drag movement delta, screen edge presence or arrow key events
        float inputValue = 0F;
        // the clamping value used to constrain distanceChgAllowedPerUnitInput
        float distanceChgClamp = Mathf.Min(_requestedDistanceFromTarget * 0.5F, settings.MaxDistanceChgAllowedPerUnitInput);
        // distanceChgAllowedPerUnitInput defines the distanceChange value associated with a normalized unit of input
        float distanceChgAllowedPerUnitInput = Mathf.Clamp(Mathf.Abs(_requestedDistanceFromTarget), 0F, distanceChgClamp);
        // the distance change value used to modify _requestedDistanceFromTarget as determined by inputValue and distanceChgAllowedPerUnitInput
        float distanceChange = 0F;

        if (dragFocusOrbit.IsActivated()) {
            Vector2 dragDelta = _inputMgr.GetDragDelta();
            inputValue = dragDelta.x;
            _yAxisRotation += inputValue * dragFocusOrbit.sensitivity * timeSinceLastUpdate;
            inputValue = dragDelta.y;
            _xAxisRotation -= inputValue * dragFocusOrbit.sensitivity * timeSinceLastUpdate;
        }
        if (edgeFocusOrbitPan.IsActivated()) {
            var activeEdge = _inputMgr.GetScreenEdgeEvent(edgeFocusOrbitPan.screenEdgeAxis);
            if (activeEdge == InputManager.ActiveScreenEdge.Left) {
                _yAxisRotation += edgeFocusOrbitPan.sensitivity * timeSinceLastUpdate;
            }
            else if (activeEdge == InputManager.ActiveScreenEdge.Right) {
                _yAxisRotation -= edgeFocusOrbitPan.sensitivity * timeSinceLastUpdate;
            }
        }
        if (edgeFocusOrbitTilt.IsActivated()) {
            var activeEdge = _inputMgr.GetScreenEdgeEvent(edgeFocusOrbitTilt.screenEdgeAxis);
            if (activeEdge == InputManager.ActiveScreenEdge.Bottom) {
                _xAxisRotation -= edgeFocusOrbitPan.sensitivity * timeSinceLastUpdate;
            }
            else if (activeEdge == InputManager.ActiveScreenEdge.Top) {
                _xAxisRotation += edgeFocusOrbitPan.sensitivity * timeSinceLastUpdate;
            }
        }
        if (dragFocusRoll.IsActivated()) {
            inputValue = _inputMgr.GetDragDelta().x;
            _zAxisRotation += inputValue * dragFocusRoll.sensitivity * timeSinceLastUpdate;
        }
        if (scrollFocusZoom.IsActivated()) {
            var scrollEvent = _inputMgr.GetScrollEvent();
            inputValue = scrollEvent.delta;
            if (inputValue > 0 || (inputValue < 0 && _isZoomOutOnCursorEnabled)) {
                if (TrySetScrollZoomTarget(scrollEvent, Input.mousePosition)) {
                    // there is a new Target so it can't be the old focus Target
                    CurrentState = CameraState.Freeform;
                    return;
                }
            }
            distanceChange = inputValue * scrollFocusZoom.InputTypeNormalizer * scrollFocusZoom.sensitivity * distanceChgAllowedPerUnitInput;
            _requestedDistanceFromTarget -= distanceChange;
        }

        #region Archived ScrollFocusZoom
        //if (scrollFocusZoom.IsActivated()) {
        //    inputValue = _gameInput.GetScrollWheelMovement();
        //    if (inputValue > 0 || (inputValue < 0 && _isZoomOutOnCursorEnabled)) {
        //        if (TrySetTargetAtScreenPoint(Input.mousePosition)) {
        //            // there is a new Target so it can't be the old focus Target
        //            CurrentState = CameraState.Freeform;
        //            return;
        //        }
        //    }
        //    distanceChange = inputValue * scrollFocusZoom.InputTypeNormalizer * scrollFocusZoom.sensitivity * distanceChgAllowedPerUnitInput;
        //    _requestedDistanceFromTarget -= distanceChange;
        //}
        #endregion

        if (edgeFocusZoom.IsActivated()) {
            inputValue = 1F;
            var activeEdge = _inputMgr.GetScreenEdgeEvent(edgeFocusZoom.screenEdgeAxis);
            if (activeEdge == InputManager.ActiveScreenEdge.Bottom) {
                distanceChange = inputValue * edgeFocusZoom.sensitivity * edgeFocusZoom.InputTypeNormalizer * timeSinceLastUpdate * distanceChgAllowedPerUnitInput;
                _requestedDistanceFromTarget += distanceChange;
            }
            else if (activeEdge == InputManager.ActiveScreenEdge.Top) {
                distanceChange = inputValue * edgeFocusZoom.sensitivity * edgeFocusZoom.InputTypeNormalizer * timeSinceLastUpdate * distanceChgAllowedPerUnitInput;
                _requestedDistanceFromTarget -= distanceChange;
            }
        }

        #region Archived EdgeFocusZoom

        //if (edgeFocusZoom.IsActivated()) {
        //    inputValue = 1F;
        //    float yMousePosition = Input.mousePosition.y;
        //    if (yMousePosition <= settings.activeScreenEdge) {
        //        distanceChange = inputValue * edgeFocusZoom.sensitivity * edgeFocusZoom.InputTypeNormalizer * timeSinceLastUpdate * distanceChgAllowedPerUnitInput;
        //        _requestedDistanceFromTarget += distanceChange;
        //    }
        //    else if (yMousePosition >= Screen.height - settings.activeScreenEdge) {
        //        distanceChange = inputValue * edgeFocusZoom.sensitivity * edgeFocusZoom.InputTypeNormalizer * timeSinceLastUpdate * distanceChgAllowedPerUnitInput;
        //        _requestedDistanceFromTarget -= distanceChange;
        //    }
        //}

        #endregion

        if (keyFocusZoom.IsActivated()) {
            inputValue = _inputMgr.GetArrowKeyEventValue(keyFocusZoom.keyboardAxis);
            distanceChange = inputValue * keyFocusZoom.InputTypeNormalizer * keyFocusZoom.sensitivity * distanceChgAllowedPerUnitInput;
            _requestedDistanceFromTarget -= distanceChange;
        }

        #region Archived KeyFocusZoom

        //if (keyFocusZoom.IsActivated()) {
        //    inputValue = Input.GetAxis(keyboardAxesNames[(int)keyFocusZoom.keyboardAxis]);
        //    distanceChange = inputValue * keyFocusZoom.InputTypeNormalizer * keyFocusZoom.sensitivity * distanceChgAllowedPerUnitInput;
        //    _requestedDistanceFromTarget -= distanceChange;
        //}

        #endregion

        if (dragFocusZoom.IsActivated()) {
            inputValue = _inputMgr.GetDragDelta().y;
            distanceChange = inputValue * dragFocusZoom.InputTypeNormalizer * dragFocusZoom.sensitivity * distanceChgAllowedPerUnitInput;
            _requestedDistanceFromTarget -= distanceChange;
        }

        if (keyFocusPan.IsActivated()) {
            _yAxisRotation += _inputMgr.GetArrowKeyEventValue(keyFocusPan.keyboardAxis) * keyFreePan.sensitivity;
        }
        if (keyFocusTilt.IsActivated()) {
            _xAxisRotation -= _inputMgr.GetArrowKeyEventValue(keyFocusTilt.keyboardAxis) * keyFreeTilt.sensitivity;
        }
        if (keyFocusRoll.IsActivated()) {
            _zAxisRotation -= _inputMgr.GetArrowKeyEventValue(keyFocusRoll.keyboardAxis) * keyFreeRoll.sensitivity;
        }

        #region Archived KeyFocusPan

        //if (keyFocusPan.IsActivated()) {
        //    _yAxisRotation += Input.GetAxis(keyboardAxesNames[(int)keyFreePan.keyboardAxis]) * keyFreePan.sensitivity;
        //}
        //if (keyFocusTilt.IsActivated()) {
        //    _xAxisRotation -= Input.GetAxis(keyboardAxesNames[(int)keyFreeTilt.keyboardAxis]) * keyFreeTilt.sensitivity;
        //}
        //if (keyFocusRoll.IsActivated()) {
        //    _zAxisRotation -= Input.GetAxis(keyboardAxesNames[(int)keyFreeRoll.keyboardAxis]) * keyFreeRoll.sensitivity;
        //}

        #endregion

        // this is the key that keeps the camera pointed at the Target when focused
        _targetDirection = _transform.forward;

        // OPTIMIZE lets me change the values on the fly in the inspector
        _cameraRotationDampener = settings.focusedRotationDampener;
        _cameraPositionDampener = settings.focusedPositionDampener;

        ProcessChanges(timeSinceLastUpdate);
    }

    void Focused_ExitState() {
        LogEvent();
    }

    #endregion

    #region Freeform

    void Freeform_EnterState() {
        LogEvent();
        Arguments.ValidateNotNull(_targetPoint);
        _distanceFromTarget = Vector3.Distance(_targetPoint, Position);
        _requestedDistanceFromTarget = _distanceFromTarget;
        // no facing change

        _cameraRotationDampener = settings.freeformRotationDampener;
        _cameraPositionDampener = settings.freeformPositionDampener;

        CurrentFocus = null;    // will tell the previous focus it is no longer in focus
    }

    //void Freeform_Update() {
    //    UpdateCamera_Freeform();
    //}

    void Freeform_LateUpdate() {
        UpdateCamera_Freeform();
    }

    //void Freeform_FixedUpdate() {
    //    UpdateCamera_Freeform();
    //}

    private void UpdateCamera_Freeform() {
        // the only exit condition out of Freeform is the user clicking to follow or focus an object
        // the event that is generated causes the CameraState to change

        float timeSinceLastUpdate = GetTimeSinceLastUpdate();
        // the input value determined by number of mouseWheel ticks, drag movement delta, screen edge presence or arrow key events
        float inputValue = 0F;
        // the clamping value used to constrain distanceChgAllowedPerUnitInput
        float distanceChgClamp = Mathf.Min(_requestedDistanceFromTarget * 0.5F, settings.MaxDistanceChgAllowedPerUnitInput);
        // distanceChgAllowedPerUnitInput defines the distanceChange value associated with a normalized unit of input
        float distanceChgAllowedPerUnitInput = Mathf.Clamp(Mathf.Abs(_requestedDistanceFromTarget), 0F, distanceChgClamp);
        // the distance change value used to modify _requestedDistanceFromTarget as determined by inputValue and distanceChgAllowedPerUnitInput
        float distanceChange = 0F;

        if (edgeFreePan.IsActivated()) {
            var activeEdge = _inputMgr.GetScreenEdgeEvent(edgeFreePan.screenEdgeAxis);
            if (activeEdge == InputManager.ActiveScreenEdge.Left) {
                _yAxisRotation -= edgeFreePan.sensitivity * timeSinceLastUpdate;
            }
            else if (activeEdge == InputManager.ActiveScreenEdge.Right) {
                _yAxisRotation += edgeFreePan.sensitivity * timeSinceLastUpdate;
            }
            //D.Log("EdgeFreePan rotation = {0}.", _yAxisRotation);
        }
        if (edgeFreeTilt.IsActivated()) {
            var activeEdge = _inputMgr.GetScreenEdgeEvent(edgeFreeTilt.screenEdgeAxis);
            if (activeEdge == InputManager.ActiveScreenEdge.Bottom) {
                _xAxisRotation += edgeFreeTilt.sensitivity * timeSinceLastUpdate;
            }
            else if (activeEdge == InputManager.ActiveScreenEdge.Top) {
                _xAxisRotation -= edgeFreeTilt.sensitivity * timeSinceLastUpdate;
            }
            //D.Log("EdgeFreeTilt rotation = {0}.", _xAxisRotation);
        }
        if (dragFreeTruck.IsActivated()) {
            inputValue = _inputMgr.GetDragDelta().x;
            PlaceDummyTargetAtUniverseEdgeInDirection(_transform.right);
            distanceChange = inputValue * dragFreeTruck.InputTypeNormalizer * dragFreeTruck.sensitivity * distanceChgAllowedPerUnitInput;
            _requestedDistanceFromTarget += distanceChange;
        }
        if (dragFreePedestal.IsActivated()) {
            inputValue = _inputMgr.GetDragDelta().y;
            PlaceDummyTargetAtUniverseEdgeInDirection(_transform.up);
            distanceChange = inputValue * dragFreePedestal.InputTypeNormalizer * dragFreePedestal.sensitivity * distanceChgAllowedPerUnitInput;
            _requestedDistanceFromTarget += distanceChange;
        }
        if (dragFreeRoll.IsActivated()) {
            inputValue = _inputMgr.GetDragDelta().x;
            _zAxisRotation += inputValue * dragFreeRoll.sensitivity * timeSinceLastUpdate;
        }
        if (dragFreePanTilt.IsActivated()) {
            Vector2 dragDelta = _inputMgr.GetDragDelta();
            inputValue = dragDelta.x;
            _yAxisRotation -= inputValue * dragFreePanTilt.sensitivity * timeSinceLastUpdate;
            inputValue = dragDelta.y;
            _xAxisRotation += inputValue * dragFreePanTilt.sensitivity * timeSinceLastUpdate;
        }
        if (scrollFreeZoom.IsActivated()) {
            var scrollEvent = _inputMgr.GetScrollEvent();
            inputValue = scrollEvent.delta;
            if (inputValue > 0) {
                // Scroll ZoomIN command
                if (TrySetScrollZoomTarget(scrollEvent, Input.mousePosition)) {
                    // Target was changed 
                    _requestedDistanceFromTarget = _distanceFromTarget;
                }
            }
            if (inputValue < 0) {
                // Scroll ZoomOUT command
                if (_isZoomOutOnCursorEnabled) {
                    if (TrySetScrollZoomTarget(scrollEvent, Input.mousePosition)) {
                        // Target was changed
                        _requestedDistanceFromTarget = _distanceFromTarget;
                    }
                }
                else {
                    if (TrySetScrollZoomTarget(scrollEvent, _screenCenter)) {
                        // Target was changed
                        _requestedDistanceFromTarget = _distanceFromTarget;
                    }
                }
            }
            distanceChange = inputValue * scrollFreeZoom.InputTypeNormalizer * scrollFreeZoom.sensitivity * distanceChgAllowedPerUnitInput;
            _requestedDistanceFromTarget -= distanceChange;
        }

        #region Archived ScrollFreeZoom
        //if (scrollFreeZoom.IsActivated()) {
        //    inputValue = _gameInput.GetScrollWheelMovement();
        //    if (inputValue > 0) {
        //        // Scroll ZoomIN command
        //        if (TrySetTargetAtScreenPoint(Input.mousePosition)) {
        //            // Target was changed 
        //            _requestedDistanceFromTarget = _distanceFromTarget;
        //        }
        //    }
        //    if (inputValue < 0) {
        //        // Scroll ZoomOUT command
        //        if (_isZoomOutOnCursorEnabled) {
        //            if (TrySetTargetAtScreenPoint(Input.mousePosition)) {
        //                // Target was changed
        //                _requestedDistanceFromTarget = _distanceFromTarget;
        //            }
        //        }
        //        else {
        //            if (TrySetTargetAtScreenPoint(_screenCenter)) {
        //                // Target was changed
        //                _requestedDistanceFromTarget = _distanceFromTarget;
        //            }
        //        }
        //    }
        //    distanceChange = inputValue * scrollFreeZoom.InputTypeNormalizer * scrollFreeZoom.sensitivity * distanceChgAllowedPerUnitInput;
        //    _requestedDistanceFromTarget -= distanceChange;
        //}
        #endregion

        if (edgeFreeZoom.IsActivated()) {
            inputValue = 1F;
            var activeEdge = _inputMgr.GetScreenEdgeEvent(edgeFreeZoom.screenEdgeAxis);
            if (activeEdge == InputManager.ActiveScreenEdge.Bottom) {
                // Edge ZoomOUT
                if (TrySetZoomTargetAt(_screenCenter)) {
                    // Target was changed
                    _requestedDistanceFromTarget = _distanceFromTarget;
                }
                distanceChange = inputValue * edgeFreeZoom.sensitivity * edgeFreeZoom.InputTypeNormalizer * timeSinceLastUpdate * distanceChgAllowedPerUnitInput;
                _requestedDistanceFromTarget += distanceChange;
            }
            else if (activeEdge == InputManager.ActiveScreenEdge.Top) {
                // Edge ZoomIN
                if (TrySetZoomTargetAt(_screenCenter)) {
                    // Target was changed
                    _requestedDistanceFromTarget = _distanceFromTarget;
                }
                distanceChange = inputValue * edgeFreeZoom.sensitivity * edgeFreeZoom.InputTypeNormalizer * timeSinceLastUpdate * distanceChgAllowedPerUnitInput;
                _requestedDistanceFromTarget -= distanceChange;
            }
        }
        if (dragFreeZoom.IsActivated()) {
            inputValue = _inputMgr.GetDragDelta().y;
            if (TrySetZoomTargetAt(_screenCenter)) {
                _requestedDistanceFromTarget = _distanceFromTarget;
            }
            distanceChange = inputValue * dragFreeZoom.InputTypeNormalizer * dragFreeZoom.sensitivity * distanceChgAllowedPerUnitInput;
            _requestedDistanceFromTarget -= distanceChange;
        }

        // Freeform Arrow Keyboard Configurations. Only Arrow Keys are used as IsActivated() must be governed by 
        // whether the appropriate key is down to keep the configurations from interfering with each other. 
        if (keyFreeZoom.IsActivated()) {
            if (TrySetZoomTargetAt(_screenCenter)) {
                _requestedDistanceFromTarget = _distanceFromTarget;
            }
            inputValue = _inputMgr.GetArrowKeyEventValue(keyFreeZoom.keyboardAxis);
            distanceChange = inputValue * keyFreeZoom.InputTypeNormalizer * keyFreeZoom.sensitivity * distanceChgAllowedPerUnitInput;
            _requestedDistanceFromTarget -= distanceChange;
        }
        if (keyFreeTruck.IsActivated()) {
            PlaceDummyTargetAtUniverseEdgeInDirection(_transform.right);
            inputValue = _inputMgr.GetArrowKeyEventValue(keyFreeTruck.keyboardAxis);
            distanceChange = inputValue * keyFreeTruck.InputTypeNormalizer * keyFreeTruck.sensitivity * distanceChgAllowedPerUnitInput;
            _requestedDistanceFromTarget -= distanceChange;
        }
        if (keyFreePedestal.IsActivated()) {
            PlaceDummyTargetAtUniverseEdgeInDirection(_transform.up);
            inputValue = _inputMgr.GetArrowKeyEventValue(keyFreePedestal.keyboardAxis);
            distanceChange = inputValue * keyFreePedestal.InputTypeNormalizer * keyFreePedestal.sensitivity * distanceChgAllowedPerUnitInput;
            _requestedDistanceFromTarget -= distanceChange;
        }
        if (keyFreePan.IsActivated()) {
            _yAxisRotation += _inputMgr.GetArrowKeyEventValue(keyFreePan.keyboardAxis) * keyFreePan.sensitivity;
        }
        if (keyFreeTilt.IsActivated()) {
            _xAxisRotation -= _inputMgr.GetArrowKeyEventValue(keyFreeTilt.keyboardAxis) * keyFreeTilt.sensitivity;
        }
        if (keyFreeRoll.IsActivated()) {
            _zAxisRotation -= _inputMgr.GetArrowKeyEventValue(keyFreeRoll.keyboardAxis) * keyFreeRoll.sensitivity;
        }

        _targetDirection = (_targetPoint - Position).normalized;

        // OPTIMIZE lets me change the values on the fly in the inspector
        _cameraRotationDampener = settings.freeformRotationDampener;
        _cameraPositionDampener = settings.freeformPositionDampener;

        ProcessChanges(timeSinceLastUpdate);
    }

    void Freeform_ExitState() {
        LogEvent();
    }

    #endregion

    #region Follow

    void Follow_EnterState() {
        LogEvent();
        // some values are continuously recalculated in update as the target moves so they don't need to be here too

        //D.Log("Follow Target is now {0}.", _target.gameObject.GetSafeMonoBehaviourComponent<AItemModel>().FullName);
        D.Log("Follow Target is now {0}.", _target.gameObject.GetSafeMonoBehaviourComponent<AItem>().FullName);
        ICameraFollowable icfTarget = _target.GetInterface<ICameraFollowable>();
        _cameraRotationDampener = icfTarget.FollowRotationDampener;
        _cameraPositionDampener = icfTarget.FollowDistanceDampener;
    }

    //void Follow_Update() {
    //    UpdateCamera_Follow();
    //}

    void Follow_LateUpdate() {
        UpdateCamera_Follow();
    }

    //void Follow_FixedUpdate() {
    //    UpdateCamera_Follow();
    //}

    private void UpdateCamera_Follow() {
        //if (ACtxControl.IsAnyContextMenuShowing) {
        //    // avoid camera movement when a context menu is showing 
        //    return;
        //}

        if (dragFreePanTilt.IsActivated() || scrollFreeZoom.IsActivated()) {
            CurrentState = CameraState.Freeform;
            return;
        }

        // Smooth lookAt interpolation rotates the camera to continue to lookAt the moving Target. These
        // values must be continuously updated as the Target and camera are moving
        _targetPoint = _target.position;
        _targetDirection = (_targetPoint - Position).normalized;
        // This places camera directly above target. I want it above and behind...
        //_targetDirection = ((_targetPoint - Position) - _target.up * 0.5F).normalized; 

        Vector3 lookAt = Quaternion.LookRotation(_targetDirection).eulerAngles;
        _yAxisRotation = lookAt.y;
        _xAxisRotation = lookAt.x;
        _zAxisRotation = lookAt.z;

        // Smooth follow interpolation as spectator avoids moving away from the Target if it turns inside our optimal 
        // follow distance. When the Target turns and breaks inside the optimal follow distance, stop the camera 
        // from adjusting its position by making the requested distance the same as the actual distance. 
        // As soon as the Target moves outside of the optimal distance, start following again.
        _distanceFromTarget = Vector3.Distance(_targetPoint, Position);
        if (_distanceFromTarget > _optimalDistanceFromTarget) {
            _requestedDistanceFromTarget = _optimalDistanceFromTarget;
        }
        else {
            _requestedDistanceFromTarget = _distanceFromTarget;
        }

        // OPTIMIZE lets me change the values on the fly in the inspector
        ICameraFollowable icfTarget = _target.GetInterface<ICameraFollowable>();
        _cameraRotationDampener = icfTarget.FollowRotationDampener;
        _cameraPositionDampener = icfTarget.FollowDistanceDampener;

        ProcessChanges(GetTimeSinceLastUpdate());
    }

    void Follow_ExitState() {
        LogEvent();
    }

    #endregion

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
            CurrentState = CameraState.Follow;
            return;
        }

        qualifiedCameraFocusTarget = focus.GetInterface<ICameraFocusable>();
        if (qualifiedCameraFocusTarget != null) {
            if (!_isResetOnFocusEnabled) {
                // if not resetting world coordinates on focus, the camera just turns to look at the focus
                CurrentState = CameraState.Focusing;
                return;
            }

            ResetToWorldspace();
            CurrentState = CameraState.Focused;
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
            // its the same target, it's not the dummy and the target point is different so just change the target point
            // and avoid the checks that follow, including losing the focus on the previous (same as the new) target
            if (CurrentState == CameraState.Focused) {
                // making targetPoint changes while focused causes disconcerting movement to the object
                // targetPoint changes on the orbital plane are meant to be used while in freeform
                return;
            }
            //D.Log("Camera target {0} targetPoint moved from {1} to {2}.", _target.name, _targetPoint, newTargetPoint);
            AssignTarget(newTarget, newTargetPoint);
            return;
        }

        // NOTE: As Rigidbodies consume child collider events, a hit on a child collider when there is a rigidbody parent 
        // involved, will return the transform of the parent, not the child. By not including inspection of the children for this interface,
        // I am requiring that the interface be present with the Rigidbody.
        ICameraTargetable qualifiedCameraTarget = newTarget.GetInterface<ICameraTargetable>();
        if (qualifiedCameraTarget != null) {
            if (!qualifiedCameraTarget.IsEligible) {
                return;
            }
            _minimumDistanceFromTarget = qualifiedCameraTarget.MinimumCameraViewingDistance;
            //D.Log("Target {0} _minimumDistanceFromTarget set to {1}.".Inject(newTarget.name, _minimumDistanceFromTarget));

            ICameraFocusable qualifiedCameraFocusTarget = newTarget.GetInterface<ICameraFocusable>();
            if (qualifiedCameraFocusTarget != null) {
                _optimalDistanceFromTarget = qualifiedCameraFocusTarget.OptimalCameraViewingDistance;
                //D.Log("Target {0} _optimalDistanceFromTarget set to {1}.".Inject(newTarget.name, _optimalDistanceFromTarget));
            }
            // no reason to know whether the Target is followable or not for these values for now
        }
        else {
            D.ErrorContext("New Target {0} is not {1}.".Inject(newTarget.name, typeof(ICameraTargetable).Name), this);
            return;
        }

        AssignTarget(newTarget, newTargetPoint);
        //D.Log("Camera target changed to {0}.", _target.name);
    }

    private void AssignTarget(Transform newTarget, Vector3 newTargetPoint) {
        _target = newTarget;
        _targetPoint = newTargetPoint;
        // anytime the Target changes, the actual distance to the Target should also be reset
        _distanceFromTarget = Vector3.Distance(_targetPoint, Position);
        _requestedDistanceFromTarget = _distanceFromTarget;
    }

    /// <summary>
    /// Resets the camera rotation to that of worldspace, no rotation.
    /// </summary>
    public void ResetToWorldspace() {
        // current and requested distance to Target already set
        Quaternion zeroRotation = Quaternion.identity;
        _transform.rotation = zeroRotation;
        Vector3 zeroRotationVector = zeroRotation.eulerAngles;
        _xAxisRotation = zeroRotationVector.x;
        _yAxisRotation = zeroRotationVector.y;
        _zAxisRotation = zeroRotationVector.z;
        //D.Log("ResetToWorldSpace called. Worldspace Camera Rotation = {0}.".Inject(new Vector3(_xRotation, _yRotation, _zRotation)));
    }

    #endregion

    #region Camera Updating Support

    private float GetTimeSinceLastUpdate() {
        return GameTime.Instance.DeltaTime * (int)UpdateRate;
    }

    private void LockCursor(bool toLockCursor) {
        if (toLockCursor && !Screen.lockCursor) {
            Screen.lockCursor = true;   // cursor disappears
        }
        else if (Screen.lockCursor && !toLockCursor) {
            Screen.lockCursor = false;  // cursor reappears in the center of the screen
        }
    }

    private void ProcessChanges(float deltaTime) {
        _transform.rotation = CalculateCameraRotation(_cameraRotationDampener * deltaTime);
        //_transform.localRotation = CalculateCameraRotation(_cameraRotationDampener * deltaTime);

        //D.Log("RequestedDistanceFromTarget = {0}, MinimumDistanceFromTarget = {1}.".Inject(_requestedDistanceFromTarget, settings.minimumDistanceFromTarget));
        _requestedDistanceFromTarget = Mathf.Clamp(_requestedDistanceFromTarget, _minimumDistanceFromTarget, Mathf.Infinity);
        //D.Log("RequestedDistanceFromTarget = {0}.".Inject(_requestedDistanceFromTarget));

        _distanceFromTarget = Mathfx.Lerp(_distanceFromTarget, _requestedDistanceFromTarget, _cameraPositionDampener * deltaTime);

        Vector3 proposedPosition = _targetPoint - (_targetDirection * _distanceFromTarget);
        ExecutePositionChange(proposedPosition);
    }

    /// <summary>
    /// Processes the position change, implementing it if the position has changed and it is within
    /// the boundaries of the universe. Also updates SectorIndex if the new position has crossed a 
    /// sector boundary.
    /// </summary>
    /// <param name="proposedPosition">The proposed position.</param>
    private void ExecutePositionChange(Vector3 proposedPosition) {
        Vector3 currentPosition = Position;
        if (currentPosition.IsSame(proposedPosition) || !ValidatePosition(proposedPosition)) {
            return;
        }
        Index3D proposedSectorIndex = _sectorGrid.GetSectorIndex(proposedPosition);
        if (!proposedSectorIndex.Equals(SectorIndex)) {
            SectorIndex = proposedSectorIndex;
        }
        Position = proposedPosition;
    }

    /// <summary>
    /// Validates whether the proposed position of the camera is contained within the universe.
    /// </summary>
    /// <param name="proposedPosition">The new position.</param>
    /// <returns> </returns>
    private bool ValidatePosition(Vector3 proposedPosition) {
        float sqrMagnitude = (proposedPosition - GameConstants.UniverseOrigin).sqrMagnitude;
        if (sqrMagnitude > _universeRadius * _universeRadius) {
            return false;
        }
        return true;
    }

    /// <summary>
    /// Evaluates the scroll event to see if the target it contains should be the new target. If the event target is an IZoomToFurthest,
    /// then what is behind it (if anything) is evaluated and target set using TrySetZoomTargetAt(screenPoint).
    /// </summary>
    /// <param name="scrollEvent">The scroll event.</param>
    /// <param name="screenPoint">The screen point.</param>
    /// <returns>
    /// <c>true</c> if the Target is changed, or if the dummyTarget has its location changed.
    /// <c>false</c> if the Target remains the same (or if the dummyTarget, its location remains the same).
    /// </returns>
    private bool TrySetScrollZoomTarget(InputManager.ScrollEvent scrollEvent, Vector3 screenPoint) {
        if (scrollEvent.target != null) {
            var proposedZoomTarget = scrollEvent.target.Transform;
            var proposedZoomPoint = scrollEvent.hitPoint;
            if (proposedZoomTarget == _dummyTarget) {
                // the stationary, existing DummyTarget
                return false;
            }

            if (scrollEvent.target is IZoomToFurthest) {
                // this is a IZoomToFurthest, aka SystemView so we have to select the target based on what is behind it
                return TrySetZoomTargetAt(screenPoint);
            }
            // scrollEvent.target is not IZoomToFurthest so it is the zoom target
            return TryChangeTarget(proposedZoomTarget, proposedZoomPoint);
        }

        // scroll event with a null target so position the dummyTarget
        return PlaceDummyTargetAtUniverseEdgeInDirection(_camera.ScreenPointToRay(screenPoint).direction);
    }


    /// <summary>
    /// Attempts to assign an eligible object implementing ICameraTargetable, found under the provided screenPoint as the new camera target. 
    /// If more than one object is found, then the closest object that doesn't implement IZoomFurthest becomes the Target. If all objects
    /// found implement IZoomFurthest, then the furthest object is used. If the DummyTarget is the only object found, or no 
    /// object at all is found, then the DummyTarget becomes the Target at the edge of the universe.
    /// </summary>
    /// <param name="screenPoint">The screen point.</param>
    /// <returns> <c>true</c> if the Target is changed, or if the dummyTarget has its location changed. 
    /// <c>false</c> if the Target remains the same (or if the dummyTarget, its location remains the same).
    /// </returns>
    private bool TrySetZoomTargetAt(Vector3 screenPoint) {
        Ray ray = _camera.ScreenPointToRay(screenPoint);
        RaycastHit[] hits = Physics.RaycastAll(ray, Mathf.Infinity, InputManager.WorldEventDispatcherMask_NormalInput);

        var eligibleIctHits = ConvertToEligibleICameraTargetableHits(hits);
        // Example of from and let use
        //var eligibleIctHits = (from h in hits
        //                       // allows collider to be present in child of ICameraTargetable parent
        //                       let ict = h.transform.GetInterfaceInParents<ICameraTargetable>(excludeSelf: false)
        //                       where ict != null && ict.IsEligible
        //                       select h);

        //D.Log("Eligible {0} RaycastHits on Zoom: {1}.", typeof(ICameraTargetable).Name, eligibleIctHits.Select(h => h.transform.name).Concatenate());
        if (eligibleIctHits.Any()) {
            // one or more ICameraTargetable objects under cursor encountered
            Transform proposedZoomTarget;
            Vector3 proposedZoomPoint;
            if (eligibleIctHits.Count() == 1) {
                // only one eligibleIct object encountered is likely to be the dummyTarget
                var hit = eligibleIctHits.First();
                proposedZoomTarget = hit.transform;
                if (proposedZoomTarget == _dummyTarget) {
                    // the stationary, existing DummyTarget
                    return false;
                }

                // there is only one hit so determine the proposedZoomPoint and done
                if (proposedZoomTarget.GetInterface<IZoomToFurthest>() == null) {
                    proposedZoomPoint = proposedZoomTarget.position;
                }
                else {
                    proposedZoomPoint = hit.point;
                }
                return TryChangeTarget(proposedZoomTarget, proposedZoomPoint);
            }

            // NOTE: As Rigidbodies consume child collider events, a hit on a child collider when there is a rigidbody parent 
            // involved will return the transform of the parent, not the child. 

            // there are multiple eligibleIctHits
            var zoomToFurthestHits = from h in eligibleIctHits where h.transform.GetInterface<IZoomToFurthest>() != null select h;
            var remainingHits = eligibleIctHits.Except(zoomToFurthestHits);
            if (remainingHits.Any()) {
                // there is a hit that isn't a IZoomToFurthest, so pick the closest and done
                var closestHit = remainingHits.OrderBy(h => (h.transform.position - Position).sqrMagnitude).First();
                proposedZoomTarget = closestHit.transform;
                proposedZoomPoint = proposedZoomTarget.position;
                if (proposedZoomTarget != _dummyTarget) {
                    // rare case where the dummyTarget is hit along with a IZoomToFurthest so not filtered out at top
                    return TryChangeTarget(proposedZoomTarget, proposedZoomPoint);
                }
            }
            // otherwise, all hits are IZoomToFurthest, so pick the furthest and done
            var furthestHit = zoomToFurthestHits.OrderBy(h => (h.transform.position - Position).sqrMagnitude).Last();
            proposedZoomTarget = furthestHit.transform;
            proposedZoomPoint = furthestHit.point;
            //D.Log("IZoomToFurthest furthest hit at {0}.", proposedZoomPoint);
            return TryChangeTarget(proposedZoomTarget, proposedZoomPoint);
        }

        // no eligibleIctHits encountered under cursor so move the dummy to the edge of the universe and designate it as the Target
        return PlaceDummyTargetAtUniverseEdgeInDirection(ray.direction);
    }

    /// <summary>
    /// Converts RaycastHit[] to a collection of eligible hits that implement the ICameraTargetable interface. 
    /// Hits that don't implement ICameraTargetable (eg. OrbitalPlane, Icons, etc.) have their parents searched 
    /// for the interface. If found, that transform is substituted as the transform that was hit.
    /// </summary>
    /// <param name="hits">The hits.</param>
    /// <returns></returns>
    private IEnumerable<SimpleRaycastHit> ConvertToEligibleICameraTargetableHits(RaycastHit[] hits) {
        var eligibleIctHits = new List<SimpleRaycastHit>();
        foreach (var hit in hits) {
            SimpleRaycastHit eligibleIctHit = default(SimpleRaycastHit);
            ICameraTargetable ict = hit.transform.GetInterface<ICameraTargetable>();
            if (ict != null) {
                if (ict.IsEligible) {
                    eligibleIctHit = new SimpleRaycastHit(hit.transform, hit.point);
                }
            }
            else {
                Transform t = hit.transform.GetTransformWithInterfaceInParents<ICameraTargetable>(out ict);
                if (t != null) {
                    if (ict.IsEligible) {
                        eligibleIctHit = new SimpleRaycastHit(t, hit.point);
                    }
                }
            }

            if (eligibleIctHit != default(SimpleRaycastHit)) {
                eligibleIctHits.Add(eligibleIctHit);
            }
        }
        return eligibleIctHits;
    }

    /// <summary>
    /// Attempts to change the Target to the proposedTarget. If the existing Target is the same
    /// Target but the target point is different, then the change is made to the target point but 
    /// the method returns false as the Target itself wasn't changed.
    /// </summary>
    /// <param name="proposedZoomTarget">The proposed Target. Logs an error if the DummyTarget.</param>
    /// <param name="proposedZoomPoint">The proposed Target point.</param>
    /// <returns>
    /// true if the Target itself is changed, otherwise false.
    /// </returns>
    private bool TryChangeTarget(Transform proposedZoomTarget, Vector3 proposedZoomPoint) {
        if (proposedZoomTarget == _dummyTarget) {
            D.Error("TryChangeTarget must not be used to change to the DummyTarget.");
            return false;
        }

        if (proposedZoomTarget == _target) {
            //D.Log("Proposed Target {0} is already the existing Target.".Inject(proposedZoomTarget.name));
            //D.Log("TargetPoint movement proposed is {0}, Threshold is {1}..", Vector3.Distance(proposedZoomPoint, _targetPoint), settings.smallMovementThreshold);
            if (!Mathfx.Approx(proposedZoomPoint, _targetPoint, settings.smallMovementThreshold)) {
                // the desired move of the Target point on the existing Target is large enough to respond too
                ChangeTarget(proposedZoomTarget, proposedZoomPoint);
            }
            return false;
        }
        ChangeTarget(proposedZoomTarget, proposedZoomPoint);
        return true;
    }

    /// <summary>
    /// Attempts to place the dummy Target at the edge of the universe located in the direction provided.
    /// </summary>
    /// <param name="direction">The direction.</param>
    /// <returns>true if the DummyTarget was placed in a new location. False if it was not moved since it was already there.</returns>
    private bool PlaceDummyTargetAtUniverseEdgeInDirection(Vector3 direction) {
        direction.ValidateNormalized();
        Ray ray = new Ray(Position, direction);
        RaycastHit targetHit;
        if (Physics.Raycast(ray, out targetHit, Mathf.Infinity, _dummyTargetOnlyMask.value)) {
            if (_dummyTarget != targetHit.transform) {
                D.Error("Camera should find DummyTarget, but it is: " + targetHit.transform.name);
                return false;
            }

            float distanceToUniverseOrigin = Vector3.Distance(_dummyTarget.position, GameConstants.UniverseOrigin);
            //D.Log("Dummy Target distance to origin = {0}.".Inject(distanceToUniverseOrigin));
            if (!distanceToUniverseOrigin.CheckRange(_universeRadius, allowedPercentageVariation: 0.1F)) {
                D.Error("Camera's Dummy Target is not located on UniverseEdge! Position = " + _dummyTarget.position);
                return false;
            }
            // the dummy Target is already there
            //D.Log("DummyTarget already present at " + _dummyTarget.position + ". TargetHit at " + targetHit.transform.position);
            return false;
        }

        Vector3 pointOutsideUniverse = ray.GetPoint(_universeRadius * 2);
        if (Physics.Raycast(pointOutsideUniverse, -ray.direction, out targetHit, Mathf.Infinity, _universeEdgeOnlyMask.value)) {
            Vector3 universeEdgePoint = targetHit.point;
            _dummyTarget.position = universeEdgePoint;
            ChangeTarget(_dummyTarget, _dummyTarget.position);
            //D.Log("New DummyTarget location = " + universeEdgePoint);
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
        _xAxisRotation %= 360;
        _yAxisRotation %= 360;
        _zAxisRotation %= 360;

        Vector3 desiredFacingDirection = new Vector3(_xAxisRotation, _yAxisRotation, _zAxisRotation);
        //desiredFacingDirection = Quaternion.LookRotation(desiredFacingDirection, _transform.up).eulerAngles;
        //desiredFacingDirection = Quaternion.LookRotation(desiredFacingDirection).eulerAngles;
        //desiredFacingDirection = _transform.InverseTransformDirection(desiredFacingDirection);

        //D.Log("Desired Facing: {0}.", desiredFacingDirection);

        Quaternion startingRotation = _transform.rotation;

        // This approach DOES generate a desired local rotation from the angles BUT it continues to change,
        // always staying in front of the changes from the slerp. This is because .right, .up and .forward continuously 
        // change. This results in a slow and continuous movement across the screen
        //Quaternion xQuaternion = Quaternion.AngleAxis(Mathf.Deg2Rad * _xAxisRotation, _transform.right);
        //Quaternion yQuaternion = Quaternion.AngleAxis(Mathf.Deg2Rad * _yAxisRotation, _transform.up);
        //Quaternion zQuaternion = Quaternion.AngleAxis(Mathf.Deg2Rad * _zAxisRotation, _transform.forward);
        //Quaternion desiredRotation = startingRotation * xQuaternion * yQuaternion * zQuaternion;

        Quaternion desiredRotation = Quaternion.Euler(desiredFacingDirection);
        //Quaternion desiredRotation = startingRotation * Quaternion.FromToRotation(_transform.forward, desiredFacingDirection);
        //D.Log("Desired Rotation: {0}.", desiredRotation.eulerAngles);

        Quaternion resultingRotation = Quaternion.Slerp(startingRotation, desiredRotation, dampenedTimeSinceLastUpdate);
        // OPTIMIZE Lerp is faster but not as pretty when the rotation changes are far apart
        return resultingRotation;
    }

    #endregion

    #region Debug

    private void CheckScriptCompilerSettings() {
        D.Log("Compiler Preprocessor Settings in {0} follow:".Inject(Instance.GetType().Name) + Constants.NewLine);
#if DEBUG
        D.Log("DEBUG, ");
#endif

#if UNITY_EDITOR
            D.Log("UNITY_EDITOR, ");
#endif

#if DEBUG_WARN
        D.Log("DEBUG_WARN, ");
#endif

#if DEBUG_ERROR
        D.Log("DEBUG_ERROR, ");
#endif

#if DEBUG_LOG
        D.Log("DEBUG_LOG, ");
#endif
    }

    #endregion

    protected override void Cleanup() {
        References.MainCameraControl = null;
        Unsubscribe();
    }

    private void Unsubscribe() {
        _subscribers.ForAll<IDisposable>(s => s.Dispose());
        _subscribers.Clear();
    }

    public override string ToString() {
        return new ObjectAnalyzer().ToString(this);
    }

    #region Nested Classes

    /// <summary>
    /// Substitute for RaycastHit struct whose constructors are not accessible to me.
    /// </summary>
    private struct SimpleRaycastHit : IEquatable<SimpleRaycastHit> {

        #region Comparison Operators Override

        // see C# 4.0 In a Nutshell, page 254

        public static bool operator ==(SimpleRaycastHit left, SimpleRaycastHit right) {
            return left.Equals(right);
        }

        public static bool operator !=(SimpleRaycastHit left, SimpleRaycastHit right) {
            return !left.Equals(right);
        }

        #endregion

        private static string _toStringFormat = "TransformHit: {0}, HitPoint: {1}";

        public readonly Transform transform;
        public readonly Vector3 point;

        public SimpleRaycastHit(Transform transform, Vector3 point) {
            this.transform = transform;
            this.point = point;
        }

        #region Object.Equals and GetHashCode Override

        public override bool Equals(object obj) {
            if (!(obj is SimpleRaycastHit)) { return false; }
            return Equals((SimpleRaycastHit)obj);
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
            hash = hash * 31 + transform.GetHashCode(); // 31 = another prime number
            hash = hash * 31 + point.GetHashCode();
            return hash;
        }

        #endregion

        public override string ToString() {
            return _toStringFormat.Inject(transform.name, point);
        }

        #region IEquatable<IctHit> Members

        public bool Equals(SimpleRaycastHit other) {
            return transform == other.transform && point == other.point;
        }

        #endregion
    }

    public enum CameraState {
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
    ///      Keyboard Axis values as defined in Unity.
    /// </summary>
    public enum KeyboardAxis {
        Horizontal = 0,
        Vertical = 1,
        None = 3
    }

    public enum ScreenEdgeAxis {
        None,
        /// <summary>
        /// The left and right edges of the screen.
        /// </summary>
        LeftRight,
        /// <summary>
        /// The top and bottom edges of the screen.
        /// </summary>
        TopBottom
    }

    [Serializable]
    // Settings isTargetVisibleThisFrame in the Inspector so they can be tweaked
    public class Settings {
        //public float activeScreenEdge;
        public float smallMovementThreshold;
        // damping
        public float focusingRotationDampener;
        public float focusingPositionDampener;
        public float focusedRotationDampener;
        public float focusedPositionDampener;
        public float freeformRotationDampener;
        public float freeformPositionDampener;
        /// <summary>
        /// The maximum amount of requested distance change allowed
        /// per 'unit' input value. 
        /// Scroll Ticks: 10 scroll ticks x 0.1 value/tick = 1 Scroll Tick unit
        /// Dragging: 1 drag movement x 10 value/drag = 10 Drag units
        /// </summary>
        internal float MaxDistanceChgAllowedPerUnitInput {
            get {
                return TempGameValues.SectorSideLength;
            }
        }
    }

    [Serializable]
    // Defines Camera Controls using 1Mouse Button
    public class MouseButtonDragConfiguration : CameraConfigurationBase {
        public NguiMouseButton mouseButton;

        public override float InputTypeNormalizer {
            // typ dragDelta is 10F per frame
            get { return 0.1F; }
        }

        public override bool IsActivated() {
            return base.IsActivated() && InputManager.Instance.IsDragValueWaiting && _inputHelper.IsMouseButtonDown(mouseButton)
                && !_inputHelper.IsAnyMouseButtonDownBesides(mouseButton);
        }
    }

    [Serializable]
    // Defines Camera Controls using 2 simultaneous Mouse Buttons
    public class SimultaneousMouseButtonDragConfiguration : CameraConfigurationBase {
        public NguiMouseButton firstMouseButton;
        public NguiMouseButton secondMouseButton;

        public override float InputTypeNormalizer {
            // typ dragDelta is 10F per frame
            get { return 0.1F; }
        }

        public override bool IsActivated() {
            return base.IsActivated() && InputManager.Instance.IsDragValueWaiting && _inputHelper.IsMouseButtonDown(firstMouseButton)
                && _inputHelper.IsMouseButtonDown(secondMouseButton);
        }
    }

    [Serializable]
    // Defines Screen Edge Camera controls
    public class ScreenEdgeConfiguration : CameraConfigurationBase {

        // defacto edge value per frame is 1F

        public ScreenEdgeAxis screenEdgeAxis;

        public override bool IsActivated() {
            return base.IsActivated() && InputManager.Instance.IsScreenEdgeEventWaiting(screenEdgeAxis);
        }
    }

    [Serializable]
    // Defines Mouse Scroll Wheel Camera Controls
    public class MouseScrollWheelConfiguration : CameraConfigurationBase {

        public override float InputTypeNormalizer {
            // typ scroll tick value is 0.1F per frame
            get { return 10F; }
        }

        public override bool IsActivated() {
            return base.IsActivated() && InputManager.Instance.IsScrollEventWaiting;
        }
    }

    [Serializable]
    // Defines the movement associated with the Arrow Keys on the Keyboard
    public class ArrowKeyboardConfiguration : CameraConfigurationBase {

        public KeyboardAxis keyboardAxis;

        // typ Key value is 1F per frame, but it is very easy to hold down a key and get a movement command every frame
        // Over a very short period of time (< 1 second), the effective movement value of the accumulated commands is
        // roughly the same between key, scroll and drag. Over a longer period though (say 3 seconds), the value of 
        // movement commands is much greater with a key as there is no need to reposition the hand to continue.
        // Accordingly, this InputTypeNormalizer must be reduced in value to make the effect roughly the same.
        public override float InputTypeNormalizer {
            get {
                return 0.3F;
            }
        }

        // Using Ngui Key events for key control did not work as it doesn't fire continuously when held down
        //private bool IsAxisKeyInUse() {
        //    KeyCode notUsed;
        //    switch (keyboardAxis) {
        //        case KeyboardAxis.Horizontal:
        //            return GameInputHelper.Instance.TryIsKeyHeldDown(out notUsed, KeyCode.LeftArrow, KeyCode.RightArrow);
        //        case KeyboardAxis.Vertical:
        //            return GameInputHelper.Instance.TryIsKeyHeldDown(out notUsed, KeyCode.UpArrow, KeyCode.DownArrow);
        //        case KeyboardAxis.None:
        //        default:
        //            throw new NotImplementedException(ErrorMessages.UnanticipatedSwitchValue.Inject(keyboardAxis));
        //    }
        //}

        public override bool IsActivated() {
            return base.IsActivated() && InputManager.Instance.IsArrowKeyEventWaiting(keyboardAxis);
        }
    }

    [Serializable]
    public abstract class CameraConfigurationBase : AInputConfigurationBase {

        // Can't use an initializer or Constructor to assign a Singleton MonoBehaviour to a field
        //protected static GameInputManager _gameInput = GameInputManager.Instance;

        protected static GameInputHelper _inputHelper = GameInputHelper.Instance;

        /// <summary>
        /// This factor is used to normalize the translation movement speed of different input controls (keys, screen edge and mouse dragging)
        /// so that roughly the same distance is covered by a typical application of the control over a set period of time.  
        /// 
        /// Keys are normally held down providing a mouseInput value of 1 every frame. A single key press however provides a value between 
        /// 0.1 and 0.3 every frame, depending on how long it is pressed.
        /// 
        /// The screen edge provides a defacto value of 1 every frame.
        /// 
        /// Dragging a mouse provides a value of about 10 every frame.
        /// 
        /// The mouse scrollwheel provides a value of 0.1 - 0.3 every frame, depending on how fast the scroll wheel is being rolled. A
        /// single tick of the scroll wheel provides a value of 0.1.
        /// 
        /// At this time, this factor is not used to normalize rotation gameSpeed.
        /// 
        /// </summary>
        public virtual float InputTypeNormalizer {
            get { return 1F; }
        }
    }

    #endregion

    #region ContextMenu Archive

    //private CtxPickHandler _contextMenuPickHandler; // not currently used

    //private void InitializeContextMenuSettings() {  // not currently used
    //    _contextMenuPickHandler = gameObject.GetSafeMonoBehaviourComponent<CtxPickHandler>();
    //    _contextMenuPickHandler.dontUseFallThrough = true;
    //    //_contextMenuPickHandler.pickLayers = LayerMaskExtensions.CreateInclusiveMask(Layers.Default, Layers.SectorView);
    //    _contextMenuPickHandler.pickLayers = LayerMaskExtensions.CreateInclusiveMask(Layers.Default);
    //    if (_contextMenuPickHandler.menuButton != NguiMouseButton.Right.ToUnityMouseButton()) {
    //        D.Warn("Context Menu actuator button not set to Right Mouse Button.");
    //    }
    //}

    ///// <summary>
    ///// Tries to show the context menu. 
    ///// NOTE: This is a preprocess method for ContextMenuPickHandler.OnPress(isDown) which is designed to show
    ///// the context menu if the method is called both times (isDown = true, then isDown = false) over the same object.
    ///// Unfortunately, that also means the context menu will show if a drag starts and ends over the same 
    ///// ISelectable object. Therefore, this preprocess method is here to detect whether a drag is occurring before 
    ///// passing it on to show the context menu.
    ///// </summary>
    ///// <param name="isDown">if set to <c>true</c> [is down].</param>
    //public void ShowContextMenuOnPress(bool isDown) {
    //    if (!_gameInput.IsDragging) {
    //        _contextMenuPickHandler.OnPress(isDown);
    //        //D.Log("ContextMenu requested.");
    //    }
    //}

    #endregion

}


