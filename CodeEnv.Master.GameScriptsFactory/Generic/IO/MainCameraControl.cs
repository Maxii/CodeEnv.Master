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
public class MainCameraControl : AFSMSingleton_NoCall<MainCameraControl, MainCameraControl.CameraState>, ICameraControl {

    /// <summary>
    /// The camera's default field of view in degrees.
    /// </summary>
    private const float CameraFieldOfView_Default = 50F;

    private const float DummyTargetColliderRadius = 0.5F;

    private static readonly float DummyTargetOffsetInsideUniverseEdge = DummyTargetColliderRadius + 0.1F;

    private const float CameraMaxClippingPlaneRatio = 10000F;  // OPTIMIZE up to 30000? http://forum.unity3d.com/threads/how-to-avoid-z-fighting.56418/

    /// <summary>
    /// 360 degrees in one rotation.
    /// </summary>
    private static int _degreesPerRotation = Constants.DegreesPerRotation;

    #region Camera Control Configurations
    // WARNING: Initializing non-Mono classes declared in a Mono class, outside of Awake or Start causes them to be instantiated by Unity AT EDITOR TIME (aka before runtime). 
    //This means that their constructors will be called before ANYTHING else is called. Script execution order is irrelevant.

    // Focused and Follow Zooming: When focused or following, top and bottom Edge zooming, arrow key zooming and scrollWheel zooming cause camera movement in and out 
    // from the focused/followed object that is centered on the screen. 
    public ScreenEdgeConfiguration edgeFocusZoom = new ScreenEdgeConfiguration { screenEdgeAxis = ScreenEdgeAxis.TopBottom, sensitivity = 2F, activate = false };
    public MouseScrollWheelConfiguration scrollFocusZoom = new MouseScrollWheelConfiguration { sensitivity = 1F, activate = true };
    public ArrowKeyboardConfiguration keyFocusZoom = new ArrowKeyboardConfiguration { keyboardAxis = KeyboardAxis.Vertical, sensitivity = 1F, activate = true };
    public SimultaneousMouseButtonDragConfiguration dragFocusZoom = new SimultaneousMouseButtonDragConfiguration { firstMouseButton = NguiMouseButton.Left, secondMouseButton = NguiMouseButton.Right, sensitivity = 1F, activate = true };

    public ScreenEdgeConfiguration edgeFollowZoom = new ScreenEdgeConfiguration { screenEdgeAxis = ScreenEdgeAxis.TopBottom, sensitivity = 2F, activate = false };
    public MouseScrollWheelConfiguration scrollFollowZoom = new MouseScrollWheelConfiguration { sensitivity = 1F, activate = true };
    public ArrowKeyboardConfiguration keyFollowZoom = new ArrowKeyboardConfiguration { keyboardAxis = KeyboardAxis.Vertical, sensitivity = 1F, activate = true };
    public SimultaneousMouseButtonDragConfiguration dragFollowZoom = new SimultaneousMouseButtonDragConfiguration { firstMouseButton = NguiMouseButton.Left, secondMouseButton = NguiMouseButton.Right, sensitivity = 1F, activate = true };

    // Freeform Zooming: When not focused, top and bottom Edge zooming and arrow key zooming cause camera movement forward or backward along the camera's facing.
    // ScrollWheel zooming on the other hand always moves toward the cursor when scrolling IN. By default, scrolling OUT is directly opposite
    // the camera's facing. However, there is an option to scroll OUT from the cursor instead. 
    public ScreenEdgeConfiguration edgeFreeZoom = new ScreenEdgeConfiguration { screenEdgeAxis = ScreenEdgeAxis.TopBottom, sensitivity = 2F, activate = false };
    public ArrowKeyboardConfiguration keyFreeZoom = new ArrowKeyboardConfiguration { sensitivity = 1F, keyboardAxis = KeyboardAxis.Vertical, activate = true };
    public MouseScrollWheelConfiguration scrollFreeZoom = new MouseScrollWheelConfiguration { sensitivity = 1F, activate = true };
    public SimultaneousMouseButtonDragConfiguration dragFreeZoom = new SimultaneousMouseButtonDragConfiguration { firstMouseButton = NguiMouseButton.Left, secondMouseButton = NguiMouseButton.Right, sensitivity = 1F, activate = true };

    // Panning, Tilting and Orbiting: When focused or following, edge actuation, arrow key pan and tilting and mouse button/movement results in orbiting of the focused/followed object
    // that is centered on the screen. When not focused or following the same arrow keys, edge actuation and mouse button/movement results in the camera panning (looking left or right)
    // and tilting (looking up or down) in place.
    public ScreenEdgeConfiguration edgeFreePan = new ScreenEdgeConfiguration { screenEdgeAxis = ScreenEdgeAxis.LeftRight, sensitivity = 10F, activate = true };
    public ScreenEdgeConfiguration edgeFreeTilt = new ScreenEdgeConfiguration { screenEdgeAxis = ScreenEdgeAxis.TopBottom, sensitivity = 10F, activate = true };
    public ScreenEdgeConfiguration edgeFocusPan = new ScreenEdgeConfiguration { screenEdgeAxis = ScreenEdgeAxis.LeftRight, sensitivity = 10F, activate = true };
    public ScreenEdgeConfiguration edgeFocusTilt = new ScreenEdgeConfiguration { screenEdgeAxis = ScreenEdgeAxis.TopBottom, sensitivity = 10F, activate = true };
    public ScreenEdgeConfiguration edgeFollowPan = new ScreenEdgeConfiguration { screenEdgeAxis = ScreenEdgeAxis.LeftRight, sensitivity = 10F, activate = true };
    public ScreenEdgeConfiguration edgeFollowTilt = new ScreenEdgeConfiguration { screenEdgeAxis = ScreenEdgeAxis.TopBottom, sensitivity = 10F, activate = true };
    public ArrowKeyboardConfiguration keyFreePan = new ArrowKeyboardConfiguration { keyboardAxis = KeyboardAxis.Horizontal, sensitivity = 1F, activate = true };
    public ArrowKeyboardConfiguration keyFreeTilt = new ArrowKeyboardConfiguration { keyboardAxis = KeyboardAxis.Vertical, modifiers = new KeyModifiers { ctrlKeyReqd = true, shiftKeyReqd = true }, sensitivity = 1F, activate = true };
    public ArrowKeyboardConfiguration keyFocusPan = new ArrowKeyboardConfiguration { keyboardAxis = KeyboardAxis.Horizontal, sensitivity = 1F, activate = true };
    public ArrowKeyboardConfiguration keyFocusTilt = new ArrowKeyboardConfiguration { keyboardAxis = KeyboardAxis.Vertical, modifiers = new KeyModifiers { ctrlKeyReqd = true, shiftKeyReqd = true }, sensitivity = 1F, activate = true };
    public ArrowKeyboardConfiguration keyFollowPan = new ArrowKeyboardConfiguration { keyboardAxis = KeyboardAxis.Horizontal, sensitivity = 1F, activate = true };
    public ArrowKeyboardConfiguration keyFollowTilt = new ArrowKeyboardConfiguration { keyboardAxis = KeyboardAxis.Vertical, modifiers = new KeyModifiers { ctrlKeyReqd = true, shiftKeyReqd = true }, sensitivity = 1F, activate = true };

    public MouseButtonDragConfiguration dragFocusOrbit = new MouseButtonDragConfiguration { mouseButton = NguiMouseButton.Right, sensitivity = 5F, activate = true };
    public MouseButtonDragConfiguration dragFollowOrbit = new MouseButtonDragConfiguration { mouseButton = NguiMouseButton.Right, sensitivity = 5F, activate = true };
    public MouseButtonDragConfiguration dragFreePanTilt = new MouseButtonDragConfiguration { mouseButton = NguiMouseButton.Right, sensitivity = 3F, activate = true };

    // Truck and Pedestal: Trucking (moving left and right) and Pedestalling (moving up and down) occurs only in Freeform space, repositioning the camera along it's current horizontal and vertical axis'.
    // Attempting to Truck or Pedestal while focused or following is not allowed (as it makes no sense) and will immediately cause a change to the Freeform state.
    public MouseButtonDragConfiguration dragFreeTruck = new MouseButtonDragConfiguration { mouseButton = NguiMouseButton.Middle, modifiers = new KeyModifiers { altKeyReqd = true }, sensitivity = 1F, activate = true };
    public MouseButtonDragConfiguration dragFreePedestal = new MouseButtonDragConfiguration { mouseButton = NguiMouseButton.Middle, modifiers = new KeyModifiers { shiftKeyReqd = true }, sensitivity = 1F, activate = true };
    public ArrowKeyboardConfiguration keyFreePedestal = new ArrowKeyboardConfiguration { keyboardAxis = KeyboardAxis.Vertical, modifiers = new KeyModifiers { ctrlKeyReqd = true }, activate = true };
    public ArrowKeyboardConfiguration keyFreeTruck = new ArrowKeyboardConfiguration { keyboardAxis = KeyboardAxis.Horizontal, modifiers = new KeyModifiers { ctrlKeyReqd = true }, activate = true };

    // Rolling: Focused, following and freeform rolling results in the same behaviour, rolling around the camera's current forward axis.
    public MouseButtonDragConfiguration dragFocusRoll = new MouseButtonDragConfiguration { mouseButton = NguiMouseButton.Right, modifiers = new KeyModifiers { altKeyReqd = true }, sensitivity = 5F, activate = true };
    public MouseButtonDragConfiguration dragFollowRoll = new MouseButtonDragConfiguration { mouseButton = NguiMouseButton.Right, modifiers = new KeyModifiers { altKeyReqd = true }, sensitivity = 5F, activate = true };
    public MouseButtonDragConfiguration dragFreeRoll = new MouseButtonDragConfiguration { mouseButton = NguiMouseButton.Right, modifiers = new KeyModifiers { altKeyReqd = true }, sensitivity = 5F, activate = true };
    public ArrowKeyboardConfiguration keyFreeRoll = new ArrowKeyboardConfiguration { keyboardAxis = KeyboardAxis.Horizontal, modifiers = new KeyModifiers { ctrlKeyReqd = true, shiftKeyReqd = true }, activate = true };
    public ArrowKeyboardConfiguration keyFocusRoll = new ArrowKeyboardConfiguration { keyboardAxis = KeyboardAxis.Horizontal, modifiers = new KeyModifiers { ctrlKeyReqd = true, shiftKeyReqd = true }, activate = true };
    public ArrowKeyboardConfiguration keyFollowRoll = new ArrowKeyboardConfiguration { keyboardAxis = KeyboardAxis.Horizontal, modifiers = new KeyModifiers { ctrlKeyReqd = true, shiftKeyReqd = true }, activate = true };

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

    [SerializeField]    // OPTIMIZE
    private CameraUpdateMode _focusingUpdateMode = CameraUpdateMode.LateUpdate;
    [SerializeField]
    private CameraUpdateMode _focusedUpdateMode = CameraUpdateMode.LateUpdate;
    [SerializeField]
    private CameraUpdateMode _freeformUpdateMode = CameraUpdateMode.LateUpdate;
    [SerializeField]
    private CameraUpdateMode _followUpdateMode = CameraUpdateMode.FixedUpdate;


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
        get { return transform.position; }
        private set { transform.position = value; }
    }

    /// <summary>
    /// The MainCamera for objects that are close.
    /// NearClipPlane: .01, FarClipPlane: 1 - 10.
    /// </summary>
    public Camera MainCamera_Near { get; private set; }

    /// <summary>
    /// The MainCamera for objects that are far.
    /// NearClipPlane: 1 - 10, FarClipPlane: UniverseDiameter, up to 100K
    /// </summary>
    public Camera MainCamera_Far { get; private set; }

    /// <summary>
    /// The distance from the camera's target point to the camera's focal plane.
    /// </summary>
    public float DistanceToCameraTarget { get { return _targetPoint.DistanceToCamera(); } }

    private ICameraFocusable _currentFocus;
    /// <summary>
    /// The object the camera is currently focused on if it has one.
    /// </summary>
    public ICameraFocusable CurrentFocus {
        get { return _currentFocus; }
        set { SetProperty<ICameraFocusable>(ref _currentFocus, value, "CurrentFocus", CurrentFocusPropChangedHandler, CurrentFocusPropChangingHandler); }
    }

    public Settings settings = new Settings {
        smallMovementThreshold = 2F,
        focusingDistanceDampener = 2.0F, focusingRotationDampener = 1.0F, focusedDistanceDampener = 4.0F,
        focusedRotationDampener = 2.0F, freeformDistanceDampener = 3.0F, freeformRotationDampener = 2.0F
    };

    /// <summary>
    /// Indicates whether the Camera (in Follow mode)  is in the process of zooming.
    /// </summary>
    private bool _isFollowingCameraZooming;
    private bool _isResetOnFocusEnabled;
    private bool _isZoomOutOnCursorEnabled;    // ScrollWheel always zooms IN on cursor, zooming OUT with the ScrollWheel is directly backwards by default

    // Cached references
    //[DoNotSerialize]    // Serializing this creates duplicates of this object on Save
    private PlayerPrefsManager _playerPrefsMgr;
    private InputManager _inputMgr;
    private SectorGrid _sectorGrid;
    private GameManager _gameMgr;
    private GameTime _gameTime;

    private Camera[] _mainCameras;

    private IList<IDisposable> _subscriptions;

    private Vector3 _targetPoint;
    private Transform _target;
    private Transform _dummyTarget;
    private SphereCollider _universeEdgeCollider;

    private float _universeRadius;

    // not currently used
    //private string[] keyboardAxesNames = new string[] { UnityConstants.KeyboardAxisName_Horizontal, UnityConstants.KeyboardAxisName_Vertical };

    /// <summary>
    /// The layers the main 3DCameras are allowed to render.
    /// </summary>
    private LayerMask _mainCamerasCullingMask = LayerMaskUtility.CreateInclusiveMask(Layers.Default, Layers.TransparentFX, Layers.Cull_Tiny,
        Layers.Cull_1, Layers.Cull_2, Layers.Cull_3, Layers.Cull_4, Layers.Cull_8, Layers.Cull_15, Layers.Cull_200, Layers.Cull_400, Layers.Cull_1000, Layers.Cull_3000,
        Layers.SystemOrbitalPlane, Layers.Projectiles, Layers.Shields);

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
    private float _cameraDistanceDampener;
    private float _cameraRotationDampener;

    #endregion

    #region Temporary UnityEditor Controls
    // Temporary workaround that keeps the edge movement controls
    // from operating when I'm in the Editor but outside the game screen

    private bool __debugEdgeFocusPanEnabled;
    private bool __debugEdgeFocusTiltEnabled;
    private bool __debugEdgeFollowPanEnabled;
    private bool __debugEdgeFollowTiltEnabled;
    private bool __debugEdgeFreePanEnabled;
    private bool __debugEdgeFreeTiltEnabled;

    [System.Diagnostics.Conditional("UNITY_EDITOR")]
    private void __InitializeDebugEdgeMovementSettings() {
        __debugEdgeFocusPanEnabled = edgeFocusPan.activate;
        __debugEdgeFocusTiltEnabled = edgeFocusTilt.activate;
        __debugEdgeFollowPanEnabled = edgeFollowPan.activate;
        __debugEdgeFollowTiltEnabled = edgeFollowTilt.activate;
        __debugEdgeFreePanEnabled = edgeFreePan.activate;
        __debugEdgeFreeTiltEnabled = edgeFreeTilt.activate;
    }

    // called by OnApplicationFocus()
    [System.Diagnostics.Conditional("UNITY_EDITOR")]
    private void __EnableEdgePanTiltInEditor(bool toEnable) {
        edgeFocusPan.activate = __debugEdgeFocusPanEnabled && toEnable;
        edgeFocusTilt.activate = __debugEdgeFocusTiltEnabled && toEnable;
        edgeFollowPan.activate = __debugEdgeFollowPanEnabled && toEnable;
        edgeFollowTilt.activate = __debugEdgeFollowTiltEnabled && toEnable;
        edgeFreePan.activate = __debugEdgeFreePanEnabled && toEnable;
        edgeFreeTilt.activate = __debugEdgeFreeTiltEnabled && toEnable;
        //D.Log("{0}: EdgePan and EdgeTilt enabled = {1}.", GetType().Name, toEnable);
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
        MainCamera_Near = UnityUtility.ValidateComponentPresence<Camera>(gameObject);
        MainCamera_Far = gameObject.GetSingleComponentInChildren<Camera>(excludeSelf: true);
        _mainCameras = new Camera[] { MainCamera_Near, MainCamera_Far };
        _playerPrefsMgr = PlayerPrefsManager.Instance;
        _inputMgr = InputManager.Instance;
        _sectorGrid = SectorGrid.Instance;
        _gameMgr = GameManager.Instance;
        _gameTime = GameTime.Instance;
    }

    private void Subscribe() {
        _subscriptions = new List<IDisposable>();
        _subscriptions.Add(_playerPrefsMgr.SubscribeToPropertyChanged<PlayerPrefsManager, bool>(ppm => ppm.IsCameraRollEnabled, IsCameraRollEnabledPropChangedHandler));
        _subscriptions.Add(_playerPrefsMgr.SubscribeToPropertyChanged<PlayerPrefsManager, bool>(ppm => ppm.IsResetOnFocusEnabled, IsResetOnFocusEnabledPropChangedHandler));
        _subscriptions.Add(_playerPrefsMgr.SubscribeToPropertyChanged<PlayerPrefsManager, bool>(ppm => ppm.IsZoomOutOnCursorEnabled, IsZoomOutOnCursorEnabledPropChangedHandler));
        _subscriptions.Add(_gameMgr.SubscribeToPropertyChanged<GameManager, bool>(gs => gs.IsRunning, IsRunningPropChangedHandler));
        _gameMgr.gameStateChanged += GameStateChangedEventHandler;
        _inputMgr.unconsumedPress += UnconsumedPressEventHandler;
    }

    private void ValidateActiveConfigurations() {
        bool isValid = true;
        if ((edgeFocusTilt.activate && edgeFocusZoom.activate) || (edgeFreeTilt.activate && edgeFreeZoom.activate) || (edgeFollowTilt.activate && edgeFollowZoom.activate)) {
            isValid = false;
        }
        D.Assert(isValid, "Incompatable Camera Configuration.");
    }

    private void InitializeMainCamera() {   // called from OnGameStateChanged()
        InitializeFields();
        SetCameraSettings();
        InitializeCameraLight();
        InitializeCameraPreferences();
        PositionCameraForGame();
    }

    private void InitializeFields() {
        _universeRadius = _gameMgr.GameSettings.UniverseSize.Radius();
    }

    private void SetCameraSettings() {
        // Note: multiple camera system to eliminate Z fighting  http://www.davenewson.com/dev/unity-notes-on-rendering-the-big-and-the-small

        // DEPTH IS IMPORTANT Camera with lower depth draws first
        // Ngui GuiCamera depth = 1
        MainCamera_Near.depth = -1F;
        MainCamera_Far.depth = -2F;
        // SpaceUnity skybox camera depth = -3F

        // MainCamera_Near will always be set to depth only. 
        MainCamera_Near.clearFlags = CameraClearFlags.Depth;
        // MainCamera_Far must be set manually during development. Set it to Depth also if SpaceUnity skybox is active, otherwise Skybox or SolidColor if not

        // IMPORTANT Max far to near clip plane ratio <= 10,000
        MainCamera_Far.farClipPlane = _universeRadius * 2F;
        MainCamera_Far.nearClipPlane = (MainCamera_Far.farClipPlane / CameraMaxClippingPlaneRatio);
        MainCamera_Near.farClipPlane = MainCamera_Far.nearClipPlane + 1F;   // HACK 1F is overlap
        MainCamera_Near.nearClipPlane = 0.01F;

        RefreshCamerasFOV();

        //_camera.layerCullSpherical = true;    // UNCLEAR
        float[] cullDistances = new float[32];
        cullDistances[(int)Layers.Cull_Tiny] = TempGameValues.CullDistance_Tiny;
        cullDistances[(int)Layers.Cull_1] = TempGameValues.CullDistance_1;
        cullDistances[(int)Layers.Cull_2] = TempGameValues.CullDistance_2;
        cullDistances[(int)Layers.Cull_3] = TempGameValues.CullDistance_3;
        cullDistances[(int)Layers.Cull_4] = TempGameValues.CullDistance_4;
        cullDistances[(int)Layers.Cull_8] = TempGameValues.CullDistance_8;
        cullDistances[(int)Layers.Cull_15] = TempGameValues.CullDistance_15;
        cullDistances[(int)Layers.Cull_200] = TempGameValues.CullDistance_200;
        cullDistances[(int)Layers.Cull_400] = TempGameValues.CullDistance_400;
        cullDistances[(int)Layers.Cull_1000] = TempGameValues.CullDistance_1000;
        cullDistances[(int)Layers.Cull_3000] = TempGameValues.CullDistance_3000;

        cullDistances[(int)Layers.SystemOrbitalPlane] = TempGameValues.CullDistance_3000;

        _mainCameras.ForAll(cam => {
            cam.orthographic = false;
            cam.cullingMask = _mainCamerasCullingMask;
            cam.layerCullDistances = cullDistances;
        });
    }

    private void InitializeCameraLight() {
        Light directionalLight = gameObject.GetComponentInChildren<Light>(includeInactive: true);
        directionalLight.type = LightType.Directional;
        directionalLight.color = GameColor.White.ToUnityColor();
        directionalLight.cullingMask = _mainCamerasCullingMask;
        directionalLight.intensity = 1F;
    }

    /// <summary>
    /// Refreshes the MainCameras FieldOfView to the provided value in degrees.
    /// Default is 50 degrees if not provided.
    /// </summary>
    /// <param name="fov">The fov.</param>
    private void RefreshCamerasFOV(float fov = CameraFieldOfView_Default) {
        if (!MainCamera_Near.fieldOfView.ApproxEquals(fov)) {
            _mainCameras.ForAll(cam => cam.fieldOfView = fov);
        }
    }

    private void InitializeCameraPreferences() {
        // the initial Camera preference changed events occur earlier than we can subscribe so do it manually
        IsResetOnFocusEnabledPropChangedHandler();
        IsCameraRollEnabledPropChangedHandler();
        IsZoomOutOnCursorEnabledPropChangedHandler();
    }

    private void EnableCameraRoll(bool toEnable) {
        dragFocusRoll.activate = toEnable;
        dragFollowRoll.activate = toEnable;
        dragFreeRoll.activate = toEnable;
        keyFreeRoll.activate = toEnable;
        keyFocusRoll.activate = toEnable;
        keyFollowRoll.activate = toEnable;
    }

    private void PositionCameraForGame() {
        _universeEdgeCollider = CreateUniverseEdge();
        _dummyTarget = CreateDummyTarget();

        // HACK start looking down from a far distance
        float yElevation = _universeRadius * 0.3F;
        float zDistance = -_universeRadius * 0.75F;
        Position = new Vector3(0F, yElevation, zDistance);
        _sectorIndex = _sectorGrid.GetSectorIndex(Position);
        transform.rotation = Quaternion.Euler(new Vector3(20F, 0F, 0F));

        ResetAtCurrentLocation();
        // UNDONE whether starting or continuing saved game, camera position should be focused on the player's starting planet, no rotation
        //ResetToWorldspace();
    }

    private SphereCollider CreateUniverseEdge() {
        GameObject universeEdge = new GameObject("UniverseEdge");
        universeEdge.isStatic = true;
        SphereCollider universeEdgeCollider = universeEdge.AddComponent<SphereCollider>();
        UnityUtility.AttachChildToParent(universeEdge, UniverseFolder.Instance.Folder.gameObject);
        D.Assert(universeEdge.layer == (int)Layers.Default);
        universeEdgeCollider.isTrigger = true;
        universeEdgeCollider.radius = _universeRadius;
        return universeEdgeCollider;
    }

    private Transform CreateDummyTarget() {
        GameObject dummyTgt = new GameObject("DummyTarget");
        SphereCollider dummyTgtCollider = dummyTgt.AddComponent<SphereCollider>();
        dummyTgtCollider.isTrigger = true;
        dummyTgtCollider.radius = DummyTargetColliderRadius;
        dummyTgt.AddComponent<DummyTargetManager>();
        UnityUtility.AttachChildToParent(dummyTgt, DynamicObjectsFolder.Instance.gameObject);
        D.Assert(dummyTgt.layer == (int)Layers.Default);
        return dummyTgt.transform;
    }

    /// <summary>
    /// Resets the camera to its base state without changing its location
    /// or facing. Base state is Freeform with its target set as the
    /// DummyTarget positioned directly ahead. 
    /// </summary>
    private void ResetAtCurrentLocation() {
        SphereCollider dummyTargetCollider = _dummyTarget.GetComponent<SphereCollider>();
        dummyTargetCollider.enabled = false;
        // the collider is disabled so the placement algorithm doesn't accidently find it already in front of the camera
        PlaceDummyTargetAtUniverseEdgeInDirection(transform.forward);
        dummyTargetCollider.enabled = true;
        SyncRotation();
        CurrentState = CameraState.Freeform;
    }

    private void SyncRotation() {
        Quaternion startingRotation = transform.rotation;
        Vector3 startingEulerRotation = startingRotation.eulerAngles;
        _xAxisRotation = startingEulerRotation.x;
        _yAxisRotation = startingEulerRotation.y;
        _zAxisRotation = startingEulerRotation.z;
    }

    #endregion

    #region Events and Property Change Handlers

    private void IsZoomOutOnCursorEnabledPropChangedHandler() {
        _isZoomOutOnCursorEnabled = _playerPrefsMgr.IsZoomOutOnCursorEnabled;
    }

    private void IsResetOnFocusEnabledPropChangedHandler() {
        _isResetOnFocusEnabled = _playerPrefsMgr.IsResetOnFocusEnabled;
    }

    private void IsCameraRollEnabledPropChangedHandler() {
        EnableCameraRoll(_playerPrefsMgr.IsCameraRollEnabled);
    }

    //[DoNotSerialize]
    private bool __restoredGameFlag = false;

    private void GameStateChangedEventHandler(object sender, EventArgs e) {
        GameState state = _gameMgr.CurrentState;
        //D.Log("{0}{1} received GameState changed to {2}.", GetType().Name, InstanceCount, state.GetValueName());
        switch (state) {
            case GameState.Restoring:
                // only saved games that are being restored enter Restoring state
                __restoredGameFlag = true;
                break;
            case GameState.Waiting:
                _gameMgr.RecordGameStateProgressionReadiness(this, GameState.Waiting, isReady: false);
                if (__restoredGameFlag) {
                    // for a restored game, Waiting state is guaranteed to occur after OnDeserialized so we must be ready to proceed
                    __restoredGameFlag = false;
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

    private void IsRunningPropChangedHandler() {
        __AssessEnabled();
    }

    // Not currently used. Keep this for now as I expect there will be other reasons to modify camera behaviour during special modes.
    private void ViewModePropChangedHandler() {
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
                throw new NotImplementedException(ErrorMessages.UnanticipatedSwitchValue.Inject(PlayerViews.Instance.ViewMode.GetValueName()));
        }

        // edgeFocusZoom already false
        // edgeFollowZoom already false
        // scrollFocusZoom keep true
        // scrollFollowZoom keep true
        // keyFocusZoom keep true
        // keyFollowZoom keep true
        dragFocusZoom.activate = toActivateDragging;
        dragFollowZoom.activate = toActivateDragging;
        // edgeFreeZoom already false
        // keyFreeZoom keep true
        // scrollFreeZoom keep true
        dragFreeZoom.activate = toActivateDragging;

        // edgeFreePan keep true
        // edgeFreeTilt keep true
        // edgeFocusPan keep true
        // edgeFocusTilt keep true
        // edgeFollowPan keep true
        // edgeFollowTilt keep true
        // keyFreePan keep true
        // keyFreeTilt keep true
        // keyFocusPan keep true
        // keyFocusTilt keep true
        // keyFollowPan keep true
        // keyFollowTilt keep true
        dragFocusOrbit.activate = toActivateDragging;
        dragFollowOrbit.activate = toActivateDragging;
        dragFreePanTilt.activate = toActivateDragging;
        dragFreeTruck.activate = toActivateDragging;
        dragFreePedestal.activate = toActivateDragging;
        // keyFreePedestal keep true
        // keyFreeTruck keep true
        dragFocusRoll.activate = toActivateDragging;
        dragFollowRoll.activate = toActivateDragging;
        dragFreeRoll.activate = toActivateDragging;
        // keyFreeRoll keep true
        // keyFocusRoll keep true
        // keyFollowRoll keep true
    }

    private void UnconsumedPressEventHandler(object sender, EventArgs e) {
        if (GameInputHelper.Instance.IsMiddleMouseButton) {
            // pressing the middle mouse button over nothing is the primary way of exiting focused or follow mode
            CurrentFocus = null;
        }
    }

    private void CurrentFocusPropChangingHandler(ICameraFocusable newFocus) {
        if (CurrentFocus != null) {
            CurrentFocus.IsFocus = false;
            //CurrentFocus.OptimalCameraViewingDistance = _optimalDistanceFromTarget;
        }
    }

    private void CurrentFocusPropChangedHandler() {
        if (CurrentFocus != null) {
            Transform newFocus = CurrentFocus.transform;
            D.Log("New Focus is now {0}.", newFocus.name);
            SetFocusAsTarget(newFocus);
        }
        else if (CurrentState != CameraState.Freeform) {
            // CurrentFocus set to null while focused or following so switch to freeform
            ResetAtCurrentLocation();
        }
    }

    /// <summary>
    /// Called when application goes in/out of focus, this method controls the
    /// enabled state of edge pan and tilt so the camera doesn't respond to edge 
    /// movement commands when the mouse is clicked outside the editor game window.
    /// </summary>
    /// <arg item="isFocus">if set to <c>true</c> [is focusTarget].</arg>
    void OnApplicationFocus(bool isFocus) {
        //D.Log("Camera ApplicationFocusEventHandler({0}) called.", isFocus);
        __EnableEdgePanTiltInEditor(isFocus);
    }

    /// <summary>
    /// Called when the application is minimized/resumed, this method controls the enabled
    /// state of the camera so it doesn't move when I use the mouse to minimize Unity.
    /// </summary>
    /// <param name="isPaused">if set to <c>true</c> [is paused].</param>
    void OnApplicationPause(bool isPaused) {
        //D.Log("Camera ApplicationPauseEventHandler({0}) called.", isPaused);
        __AssessEnabled();
    }

    #endregion

    #region Camera StateMachine

    #region Focusing

    void Focusing_EnterState() {
        LogEvent();
        Utility.ValidateNotNull(_targetPoint);

        _distanceFromTarget = Vector3.Distance(_targetPoint, Position);
        _requestedDistanceFromTarget = _optimalDistanceFromTarget;
        _targetDirection = (_targetPoint - Position).normalized;

        // face the selected Target
        Vector3 lookAtVector = Quaternion.LookRotation(_targetDirection).eulerAngles;
        _yAxisRotation = lookAtVector.y;
        _xAxisRotation = lookAtVector.x;
        _zAxisRotation = lookAtVector.z;

        _cameraRotationDampener = settings.focusingRotationDampener;
        _cameraDistanceDampener = settings.focusingDistanceDampener;

        LockCursor(true);
    }

    void Focusing_Update() {
        if (_focusingUpdateMode == CameraUpdateMode.Update) {
            Focusing_UpdateCamera();
        }
    }

    void Focusing_LateUpdate() {
        if (_focusingUpdateMode == CameraUpdateMode.LateUpdate) {
            Focusing_UpdateCamera();
        }
    }

    void Focusing_FixedUpdate() {
        if (_focusingUpdateMode == CameraUpdateMode.FixedUpdate) {
            Focusing_UpdateCamera();
        }
    }

    private void Focusing_UpdateCamera() {
        // transition process to allow lookAt to complete. Only entered from OnFocusSelected, when !IsResetOnFocus
        if (_targetDirection.IsSameDirection(transform.forward, 1F)) {
            // exits when the lookAt rotation is complete
            CurrentState = CameraState.Focused;
            return;
        }

        // The desired (x,y,z) rotation to LookAt the Target and the requested distance from the Target
        // is set in EnterState and does not need to be updated to get there as the Target doesn't move

        // no other functionality active 
        ProcessChanges(GetTimeSinceLastUpdate(_focusingUpdateMode));
    }

    void Focusing_ExitState() {
        LogEvent();
        LockCursor(false);
    }

    #endregion

    #region Focused

    void Focused_EnterState() {
        LogEvent();
        Utility.ValidateNotNull(_targetPoint);
        // entered via OnFocusSelected AND IsResetOnFocusEnabled, OR after Focusing has completed
        _distanceFromTarget = Vector3.Distance(_targetPoint, Position);
        _requestedDistanceFromTarget = _optimalDistanceFromTarget;
        // x,y,z rotation has already been established before entering ??? FIXME where???

        _cameraRotationDampener = settings.focusedRotationDampener;
        _cameraDistanceDampener = settings.focusedDistanceDampener;
    }

    void Focused_Update() {
        if (_focusedUpdateMode == CameraUpdateMode.Update) {
            Focused_UpdateCamera();
        }
    }

    void Focused_LateUpdate() {
        if (_focusedUpdateMode == CameraUpdateMode.LateUpdate) {
            Focused_UpdateCamera();
        }
    }

    void Focused_FixedUpdate() {
        if (_focusedUpdateMode == CameraUpdateMode.FixedUpdate) {
            Focused_UpdateCamera();
        }
    }

    private void Focused_UpdateCamera() {
        if (dragFreeTruck.IsActivated || dragFreePedestal.IsActivated) {
            // Can also exit Focused/Follow on MiddleButton Press. No longer can exit on Scroll In on dummy Target
            CurrentState = CameraState.Freeform;
            return;
        }

        float timeSinceLastUpdate = GetTimeSinceLastUpdate(_focusedUpdateMode);
        // the input value determined by number of mouseWheel ticks, drag movement delta, screen edge presence or arrow key events
        float inputValue = 0F;
        // the clamping value used to constrain distanceChgAllowedPerUnitInput
        float distanceChgClamp = Mathf.Min(_requestedDistanceFromTarget * 0.5F, settings.MaxDistanceChgAllowedPerUnitInput);
        // distanceChgAllowedPerUnitInput defines the distanceChange value associated with a normalized unit of input
        float distanceChgAllowedPerUnitInput = Mathf.Clamp(Mathf.Abs(_requestedDistanceFromTarget), 0F, distanceChgClamp);
        // the distance change value used to modify _requestedDistanceFromTarget as determined by inputValue and distanceChgAllowedPerUnitInput
        float distanceChange = 0F;

        if (dragFocusOrbit.IsActivated) {
            Vector2 dragDelta = _inputMgr.GetDragDelta();
            inputValue = dragDelta.x;
            _yAxisRotation += inputValue * dragFocusOrbit.sensitivity * timeSinceLastUpdate;
            inputValue = dragDelta.y;
            _xAxisRotation -= inputValue * dragFocusOrbit.sensitivity * timeSinceLastUpdate;
        }
        if (edgeFocusPan.IsActivated) {
            var activeEdge = _inputMgr.GetScreenEdgeEvent(edgeFocusPan.screenEdgeAxis);
            if (activeEdge == InputManager.ActiveScreenEdge.Left) {
                _yAxisRotation += edgeFocusPan.sensitivity * timeSinceLastUpdate;
            }
            else if (activeEdge == InputManager.ActiveScreenEdge.Right) {
                _yAxisRotation -= edgeFocusPan.sensitivity * timeSinceLastUpdate;
            }
        }
        if (edgeFocusTilt.IsActivated) {
            var activeEdge = _inputMgr.GetScreenEdgeEvent(edgeFocusTilt.screenEdgeAxis);
            if (activeEdge == InputManager.ActiveScreenEdge.Bottom) {
                _xAxisRotation -= edgeFocusTilt.sensitivity * timeSinceLastUpdate;
            }
            else if (activeEdge == InputManager.ActiveScreenEdge.Top) {
                _xAxisRotation += edgeFocusTilt.sensitivity * timeSinceLastUpdate;
            }
        }
        if (dragFocusRoll.IsActivated) {
            inputValue = _inputMgr.GetDragDelta().x;
            _zAxisRotation += inputValue * dragFocusRoll.sensitivity * timeSinceLastUpdate;
        }
        if (scrollFocusZoom.IsActivated) {
            var scrollEvent = _inputMgr.GetScrollEvent();
            inputValue = scrollEvent.delta;
            /**************************************************************************************************************************
             * Note: Scrolling on something besides the focused object no longer exits Focused state.
             * Instead, it simply scrolls in/out on the current focus no matter where the cursor is
             *************************************************************************************************************************/
            distanceChange = inputValue * scrollFocusZoom.InputTypeNormalizer * scrollFocusZoom.sensitivity * distanceChgAllowedPerUnitInput;
            _optimalDistanceFromTarget -= distanceChange;
        }

        #region Archived ScrollFocusZoom
        //if (scrollFocusZoom.IsActivated()) {
        //    var scrollEvent = _inputMgr.GetScrollEvent();
        //    inputValue = scrollEvent.delta;
        //      if (inputValue > 0 || (inputValue < 0 && _isZoomOutOnCursorEnabled)) { 
        //           if (TrySetScrollZoomTarget(scrollEvent, Input.mousePosition)) { 
        //              // there is a new Target so it can't be the old focus Target
        //              CurrentState = CameraState.Freeform;
        //              return;
        //           }
        //      }
        //    distanceChange = inputValue * scrollFocusZoom.InputTypeNormalizer * scrollFocusZoom.sensitivity * distanceChgAllowedPerUnitInput;
        //    _requestedDistanceFromTarget -= distanceChange;
        //}
        #endregion

        if (edgeFocusZoom.IsActivated) {
            inputValue = 1F;
            var activeEdge = _inputMgr.GetScreenEdgeEvent(edgeFocusZoom.screenEdgeAxis);
            if (activeEdge == InputManager.ActiveScreenEdge.Bottom) {
                distanceChange = inputValue * edgeFocusZoom.sensitivity * edgeFocusZoom.InputTypeNormalizer * timeSinceLastUpdate * distanceChgAllowedPerUnitInput;
                _optimalDistanceFromTarget += distanceChange;
            }
            else if (activeEdge == InputManager.ActiveScreenEdge.Top) {
                distanceChange = inputValue * edgeFocusZoom.sensitivity * edgeFocusZoom.InputTypeNormalizer * timeSinceLastUpdate * distanceChgAllowedPerUnitInput;
                _optimalDistanceFromTarget -= distanceChange;
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

        if (keyFocusZoom.IsActivated) {
            inputValue = _inputMgr.GetArrowKeyEventValue(keyFocusZoom.keyboardAxis);
            distanceChange = inputValue * keyFocusZoom.InputTypeNormalizer * keyFocusZoom.sensitivity * distanceChgAllowedPerUnitInput;
            _optimalDistanceFromTarget -= distanceChange;
        }

        #region Archived KeyFocusZoom

        //if (keyFocusZoom.IsActivated()) {
        //    inputValue = Input.GetAxis(keyboardAxesNames[(int)keyFocusZoom.keyboardAxis]);
        //    distanceChange = inputValue * keyFocusZoom.InputTypeNormalizer * keyFocusZoom.sensitivity * distanceChgAllowedPerUnitInput;
        //    _requestedDistanceFromTarget -= distanceChange;
        //}

        #endregion

        if (dragFocusZoom.IsActivated) {
            inputValue = _inputMgr.GetDragDelta().y;
            distanceChange = inputValue * dragFocusZoom.InputTypeNormalizer * dragFocusZoom.sensitivity * distanceChgAllowedPerUnitInput;
            _optimalDistanceFromTarget -= distanceChange;
        }

        if (keyFocusPan.IsActivated) {
            _yAxisRotation += _inputMgr.GetArrowKeyEventValue(keyFocusPan.keyboardAxis) * keyFocusPan.sensitivity;
        }
        if (keyFocusTilt.IsActivated) {
            _xAxisRotation -= _inputMgr.GetArrowKeyEventValue(keyFocusTilt.keyboardAxis) * keyFocusTilt.sensitivity;
        }
        if (keyFocusRoll.IsActivated) {
            _zAxisRotation -= _inputMgr.GetArrowKeyEventValue(keyFocusRoll.keyboardAxis) * keyFocusRoll.sensitivity;
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

        // this is the key to re-positioning the already rotated camera so that it is looking at the target
        _targetDirection = transform.forward;

        // clamp here so optimalDistance never goes below minimumDistance as optimalDistance gets assigned to the focus when focus changes. Also effectively clamps requestedDistance
        _optimalDistanceFromTarget = Mathf.Clamp(_optimalDistanceFromTarget, _minimumDistanceFromTarget, Mathf.Infinity);
        _requestedDistanceFromTarget = _optimalDistanceFromTarget;

        // OPTIMIZE lets me change the values on the fly in the inspector
        _cameraRotationDampener = settings.focusedRotationDampener;
        _cameraDistanceDampener = settings.focusedDistanceDampener;

        ProcessChanges(timeSinceLastUpdate);
    }

    void Focused_ExitState() {
        LogEvent();
    }

    #endregion

    #region Freeform

    void Freeform_EnterState() {
        LogEvent();
        Utility.ValidateNotNull(_targetPoint);
        _distanceFromTarget = Vector3.Distance(_targetPoint, Position);
        _requestedDistanceFromTarget = _distanceFromTarget;
        // no facing change

        _cameraRotationDampener = settings.freeformRotationDampener;
        _cameraDistanceDampener = settings.freeformDistanceDampener;

        RefreshCamerasFOV();

        CurrentFocus = null;    // will tell the previous focus it is no longer in focus
    }

    void Freeform_Update() {
        if (_freeformUpdateMode == CameraUpdateMode.Update) {
            Freeform_UpdateCamera();
        }
    }

    void Freeform_LateUpdate() {
        if (_freeformUpdateMode == CameraUpdateMode.LateUpdate) {
            Freeform_UpdateCamera();
        }
    }

    void Freeform_FixedUpdate() {
        if (_freeformUpdateMode == CameraUpdateMode.FixedUpdate) {
            Freeform_UpdateCamera();
        }
    }

    private void Freeform_UpdateCamera() {
        // the only exit condition out of Freeform is the user clicking to follow or focus an object
        // the event that is generated causes the CameraState to change

        float timeSinceLastUpdate = GetTimeSinceLastUpdate(_freeformUpdateMode);
        // the input value determined by number of mouseWheel ticks, drag movement delta, screen edge presence or arrow key events
        float inputValue = 0F;
        // the clamping value used to constrain distanceChgAllowedPerUnitInput
        float distanceChgClamp = Mathf.Min(_requestedDistanceFromTarget * 0.5F, settings.MaxDistanceChgAllowedPerUnitInput);
        // distanceChgAllowedPerUnitInput defines the distanceChange value associated with a normalized unit of input
        float distanceChgAllowedPerUnitInput = Mathf.Clamp(Mathf.Abs(_requestedDistanceFromTarget), 0F, distanceChgClamp);
        // the distance change value used to modify _requestedDistanceFromTarget as determined by inputValue and distanceChgAllowedPerUnitInput
        float distanceChange = 0F;

        if (edgeFreePan.IsActivated) {
            var activeEdge = _inputMgr.GetScreenEdgeEvent(edgeFreePan.screenEdgeAxis);
            if (activeEdge == InputManager.ActiveScreenEdge.Left) {
                _yAxisRotation -= edgeFreePan.sensitivity * timeSinceLastUpdate;
            }
            else if (activeEdge == InputManager.ActiveScreenEdge.Right) {
                _yAxisRotation += edgeFreePan.sensitivity * timeSinceLastUpdate;
            }
            //D.Log("EdgeFreePan rotation = {0}.", _yAxisRotation);
        }
        if (edgeFreeTilt.IsActivated) {
            var activeEdge = _inputMgr.GetScreenEdgeEvent(edgeFreeTilt.screenEdgeAxis);
            if (activeEdge == InputManager.ActiveScreenEdge.Bottom) {
                _xAxisRotation += edgeFreeTilt.sensitivity * timeSinceLastUpdate;
            }
            else if (activeEdge == InputManager.ActiveScreenEdge.Top) {
                _xAxisRotation -= edgeFreeTilt.sensitivity * timeSinceLastUpdate;
            }
            //D.Log("EdgeFreeTilt rotation = {0}.", _xAxisRotation);
        }
        if (dragFreeTruck.IsActivated) {
            inputValue = _inputMgr.GetDragDelta().x;
            PlaceDummyTargetAtUniverseEdgeInDirection(transform.right);
            distanceChange = inputValue * dragFreeTruck.InputTypeNormalizer * dragFreeTruck.sensitivity * distanceChgAllowedPerUnitInput;
            _requestedDistanceFromTarget += distanceChange;
        }
        if (dragFreePedestal.IsActivated) {
            inputValue = _inputMgr.GetDragDelta().y;
            PlaceDummyTargetAtUniverseEdgeInDirection(transform.up);
            distanceChange = inputValue * dragFreePedestal.InputTypeNormalizer * dragFreePedestal.sensitivity * distanceChgAllowedPerUnitInput;
            _requestedDistanceFromTarget += distanceChange;
        }
        if (dragFreeRoll.IsActivated) {
            inputValue = _inputMgr.GetDragDelta().x;
            _zAxisRotation += inputValue * dragFreeRoll.sensitivity * timeSinceLastUpdate;
        }
        if (dragFreePanTilt.IsActivated) {
            Vector2 dragDelta = _inputMgr.GetDragDelta();
            inputValue = dragDelta.x;
            _yAxisRotation -= inputValue * dragFreePanTilt.sensitivity * timeSinceLastUpdate;
            inputValue = dragDelta.y;
            _xAxisRotation += inputValue * dragFreePanTilt.sensitivity * timeSinceLastUpdate;
        }
        if (scrollFreeZoom.IsActivated) {
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

        if (edgeFreeZoom.IsActivated) {
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
        if (dragFreeZoom.IsActivated) {
            inputValue = _inputMgr.GetDragDelta().y;
            if (TrySetZoomTargetAt(_screenCenter)) {
                _requestedDistanceFromTarget = _distanceFromTarget;
            }
            distanceChange = inputValue * dragFreeZoom.InputTypeNormalizer * dragFreeZoom.sensitivity * distanceChgAllowedPerUnitInput;
            _requestedDistanceFromTarget -= distanceChange;
        }

        // Freeform Arrow Keyboard Configurations. Only Arrow Keys are used as IsActivated must be governed by 
        // whether the appropriate key is down to keep the configurations from interfering with each other. 
        if (keyFreeZoom.IsActivated) {
            if (TrySetZoomTargetAt(_screenCenter)) {
                _requestedDistanceFromTarget = _distanceFromTarget;
            }
            inputValue = _inputMgr.GetArrowKeyEventValue(keyFreeZoom.keyboardAxis);
            distanceChange = inputValue * keyFreeZoom.InputTypeNormalizer * keyFreeZoom.sensitivity * distanceChgAllowedPerUnitInput;
            _requestedDistanceFromTarget -= distanceChange;
        }
        if (keyFreeTruck.IsActivated) {
            PlaceDummyTargetAtUniverseEdgeInDirection(transform.right);
            inputValue = _inputMgr.GetArrowKeyEventValue(keyFreeTruck.keyboardAxis);
            distanceChange = inputValue * keyFreeTruck.InputTypeNormalizer * keyFreeTruck.sensitivity * distanceChgAllowedPerUnitInput;
            _requestedDistanceFromTarget -= distanceChange;
        }
        if (keyFreePedestal.IsActivated) {
            PlaceDummyTargetAtUniverseEdgeInDirection(transform.up);
            inputValue = _inputMgr.GetArrowKeyEventValue(keyFreePedestal.keyboardAxis);
            distanceChange = inputValue * keyFreePedestal.InputTypeNormalizer * keyFreePedestal.sensitivity * distanceChgAllowedPerUnitInput;
            _requestedDistanceFromTarget -= distanceChange;
        }
        if (keyFreePan.IsActivated) {
            _yAxisRotation += _inputMgr.GetArrowKeyEventValue(keyFreePan.keyboardAxis) * keyFreePan.sensitivity;
        }
        if (keyFreeTilt.IsActivated) {
            _xAxisRotation -= _inputMgr.GetArrowKeyEventValue(keyFreeTilt.keyboardAxis) * keyFreeTilt.sensitivity;
        }
        if (keyFreeRoll.IsActivated) {
            _zAxisRotation -= _inputMgr.GetArrowKeyEventValue(keyFreeRoll.keyboardAxis) * keyFreeRoll.sensitivity;
        }

        _targetDirection = (_targetPoint - Position).normalized;
        // clamp requestedDistance here instead of in ProcessChanges() to avoid duplication
        _requestedDistanceFromTarget = Mathf.Clamp(_requestedDistanceFromTarget, _minimumDistanceFromTarget, Mathf.Infinity);

        // OPTIMIZE lets me change the values on the fly in the inspector
        _cameraRotationDampener = settings.freeformRotationDampener;
        _cameraDistanceDampener = settings.freeformDistanceDampener;

        ProcessChanges(timeSinceLastUpdate);
    }

    void Freeform_ExitState() {
        LogEvent();
    }

    #endregion

    #region Follow

    //private Quaternion _previousFollowLookAt;

    void Follow_EnterState() {
        LogEvent();
        // some values are continuously recalculated in update as the target moves so they don't need to be here too

        D.Log("Follow Target is now {0}.", _target.gameObject.GetSafeComponent<ADiscernibleItem>().FullName);
        ICameraFollowable icfTarget = _target.gameObject.GetSafeInterface<ICameraFollowable>();
        _cameraRotationDampener = icfTarget.FollowRotationDampener;
        _cameraDistanceDampener = icfTarget.FollowDistanceDampener;

        // initial camera view angle determined by direction of target relative to camera when Follow initiated, aka where camera is approaching from
        var initialTargetDirection = (_targetPoint - Position).normalized;
        Quaternion lookAt = Quaternion.LookRotation(initialTargetDirection);
        Vector3 lookAtEuler = lookAt.eulerAngles;
        _yAxisRotation = lookAtEuler.y;
        _xAxisRotation = lookAtEuler.x;
        _zAxisRotation = lookAtEuler.z;
        //_previousFollowLookAt = lookAt;
    }

    void Follow_Update() {
        if (_followUpdateMode == CameraUpdateMode.Update) {
            Follow_UpdateCamera();
        }
    }

    void Follow_LateUpdate() {
        if (_followUpdateMode == CameraUpdateMode.LateUpdate) {
            Follow_UpdateCamera();
        }
    }

    void Follow_FixedUpdate() {
        if (_followUpdateMode == CameraUpdateMode.FixedUpdate) {    // testing shows minimum jitter 
            Follow_UpdateCamera();
        }
    }

    private void Follow_UpdateCamera() {
        if (dragFreeTruck.IsActivated || dragFreePedestal.IsActivated) {
            // Can also exit Focused/Follow on MiddleButton Press. No longer can exit on Scroll In on dummy Target
            CurrentState = CameraState.Freeform;
            return;
        }

        float timeSinceLastUpdate = GetTimeSinceLastUpdate(_followUpdateMode);
        // the input value determined by number of mouseWheel ticks, drag movement delta, screen edge presence or arrow key events
        float inputValue = Constants.ZeroF;
        // the clamping value used to constrain distanceChgAllowedPerUnitInput
        float distanceChgClamp = Mathf.Min(_requestedDistanceFromTarget * 0.5F, settings.MaxDistanceChgAllowedPerUnitInput);
        // distanceChgAllowedPerUnitInput defines the distanceChange value associated with a normalized unit of input
        float distanceChgAllowedPerUnitInput = Mathf.Clamp(Mathf.Abs(_requestedDistanceFromTarget), 0F, distanceChgClamp);
        // the distance change value used to modify _optimalDistanceToTarget as determined by inputValue and distanceChgAllowedPerUnitInput
        float distanceChange = Constants.ZeroF;

        #region Realtime Tracking of Target Direction Archive

        // Doesn't appear to be needed as the delta each frame is very small, probably because
        // the camera is continuously repositioned to face the target, aka _targetDirection = transform.forward

        //Vector3 targetDirectionForCameraRotation = (_targetPoint - Position).normalized;
        //Quaternion lookAt = Quaternion.LookRotation(targetDirectionForCameraRotation);
        //Quaternion lookAtDelta = Math3D.SubtractRotation(lookAt, _previousFollowLookAt);
        //Vector3 lookAtDeltaEuler = lookAtDelta.eulerAngles;
        //_yAxisRotation += lookAtDeltaEuler.y;
        //_xAxisRotation += lookAtDeltaEuler.x;
        //_zAxisRotation += lookAtDeltaEuler.z;
        //D.Log("LookAtDeltaEuler = {0}.", lookAtDeltaEuler);
        //_previousFollowLookAt = lookAt;

        #endregion

        //bool isActivatedFound = false;    // debug help for detecting whether more than one config isActivated during a frame

        if (dragFollowOrbit.IsActivated) {
            Vector2 dragDelta = _inputMgr.GetDragDelta();
            inputValue = dragDelta.x;
            _yAxisRotation += inputValue * dragFollowOrbit.sensitivity * timeSinceLastUpdate;
            inputValue = dragDelta.y;
            _xAxisRotation -= inputValue * dragFollowOrbit.sensitivity * timeSinceLastUpdate;
            //__ValidateConfigIsOnlyActivated(ref isActivatedFound);
        }
        if (edgeFollowPan.IsActivated) {
            var activeEdge = _inputMgr.GetScreenEdgeEvent(edgeFollowPan.screenEdgeAxis);
            if (activeEdge == InputManager.ActiveScreenEdge.Left) {
                _yAxisRotation += edgeFollowPan.sensitivity * timeSinceLastUpdate;
            }
            else if (activeEdge == InputManager.ActiveScreenEdge.Right) {
                _yAxisRotation -= edgeFollowPan.sensitivity * timeSinceLastUpdate;
            }
            //D.Log(activeEdge != InputManager.ActiveScreenEdge.None, "{0}: Panning using edge {1}.", GetType().Name, activeEdge.GetValueName());
            // edge pan and tilt can be activated on same frame
        }
        if (edgeFollowTilt.IsActivated) {
            var activeEdge = _inputMgr.GetScreenEdgeEvent(edgeFollowTilt.screenEdgeAxis);
            if (activeEdge == InputManager.ActiveScreenEdge.Bottom) {
                _xAxisRotation -= edgeFollowTilt.sensitivity * timeSinceLastUpdate;
            }
            else if (activeEdge == InputManager.ActiveScreenEdge.Top) {
                _xAxisRotation += edgeFollowTilt.sensitivity * timeSinceLastUpdate;
            }
            //D.Log(activeEdge != InputManager.ActiveScreenEdge.None, "{0}: Tilting using edge {1}.", GetType().Name, activeEdge.GetValueName());
            // edge pan and tilt can be activated on same frame
        }
        if (dragFollowRoll.IsActivated) {
            inputValue = _inputMgr.GetDragDelta().x;
            _zAxisRotation += inputValue * dragFollowRoll.sensitivity * timeSinceLastUpdate;
            //__ValidateConfigIsOnlyActivated(ref isActivatedFound);
        }

        // All Zooms must adjust optimalDistance rather than requestedDistance as requestedDistance gets adjusted below to allow spectator-like viewing
        if (scrollFollowZoom.IsActivated) {
            var scrollEvent = _inputMgr.GetScrollEvent();
            inputValue = scrollEvent.delta;
            distanceChange = inputValue * scrollFollowZoom.InputTypeNormalizer * scrollFollowZoom.sensitivity * distanceChgAllowedPerUnitInput;
            //D.Log("ScrollFollowZoom: distance change = {0:0.#}, inputValue = {1:0.#}, normalizer = {2:0.#}, sensitivity = {3:0.##}, allowedChg = {4:0.##}.", distanceChange, inputValue, scrollFollowZoom.InputTypeNormalizer, scrollFollowZoom.sensitivity, distanceChgAllowedPerUnitInput);
            _optimalDistanceFromTarget -= distanceChange;
            _isFollowingCameraZooming = true;
            //__ValidateConfigIsOnlyActivated(ref isActivatedFound);
        }
        if (edgeFollowZoom.IsActivated) {
            inputValue = 1F;
            var activeEdge = _inputMgr.GetScreenEdgeEvent(edgeFollowZoom.screenEdgeAxis);
            if (activeEdge == InputManager.ActiveScreenEdge.Bottom) {
                distanceChange = inputValue * edgeFollowZoom.sensitivity * edgeFollowZoom.InputTypeNormalizer * timeSinceLastUpdate * distanceChgAllowedPerUnitInput;
                _optimalDistanceFromTarget += distanceChange;
            }
            else if (activeEdge == InputManager.ActiveScreenEdge.Top) {
                distanceChange = inputValue * edgeFollowZoom.sensitivity * edgeFollowZoom.InputTypeNormalizer * timeSinceLastUpdate * distanceChgAllowedPerUnitInput;
                _optimalDistanceFromTarget -= distanceChange;
            }
            _isFollowingCameraZooming = true;
            //__ValidateConfigIsOnlyActivated(ref isActivatedFound);
        }
        if (keyFollowZoom.IsActivated) {
            inputValue = _inputMgr.GetArrowKeyEventValue(keyFollowZoom.keyboardAxis);
            distanceChange = inputValue * keyFollowZoom.InputTypeNormalizer * keyFollowZoom.sensitivity * distanceChgAllowedPerUnitInput;
            _optimalDistanceFromTarget -= distanceChange;
            _isFollowingCameraZooming = true;
            //__ValidateConfigIsOnlyActivated(ref isActivatedFound);
        }
        if (dragFollowZoom.IsActivated) {
            inputValue = _inputMgr.GetDragDelta().y;
            distanceChange = inputValue * dragFollowZoom.InputTypeNormalizer * dragFollowZoom.sensitivity * distanceChgAllowedPerUnitInput;
            _optimalDistanceFromTarget -= distanceChange;
            _isFollowingCameraZooming = true;
            //__ValidateConfigIsOnlyActivated(ref isActivatedFound);
        }

        if (keyFollowPan.IsActivated) {
            _yAxisRotation += _inputMgr.GetArrowKeyEventValue(keyFollowPan.keyboardAxis) * keyFollowPan.sensitivity;
            //__ValidateConfigIsOnlyActivated(ref isActivatedFound);
        }
        if (keyFollowTilt.IsActivated) {
            _xAxisRotation -= _inputMgr.GetArrowKeyEventValue(keyFollowTilt.keyboardAxis) * keyFollowTilt.sensitivity;
            //__ValidateConfigIsOnlyActivated(ref isActivatedFound);
        }
        if (keyFollowRoll.IsActivated) {
            _zAxisRotation -= _inputMgr.GetArrowKeyEventValue(keyFollowRoll.keyboardAxis) * keyFollowRoll.sensitivity;
            //__ValidateConfigIsOnlyActivated(ref isActivatedFound);
        }

        // These values must be continuously updated as the Target and camera are moving
        _targetPoint = _target.position;
        _distanceFromTarget = Vector3.Distance(_targetPoint, Position);

        _targetDirection = transform.forward;   // this is the key to re-positioning the rotated camera so that it is always looking at the target

        // clamp here so optimalDistance never goes below minimumDistance as optimalDistance gets assigned to the focus when focus changes. Also effectively clamps requestedDistance
        _optimalDistanceFromTarget = Mathf.Clamp(_optimalDistanceFromTarget, _minimumDistanceFromTarget, Mathf.Infinity);

        // Note: Smooth follow interpolation as spectator avoids moving away from the Target if it turns inside our optimal follow distance. 
        // When the Target turns and breaks inside the optimal follow distance, we stop the camera from adjusting its position by making 
        // the requested distance the same as the actual distance. As soon as the Target moves outside of the optimal distance, we start following again.
        // This algorithm does not work when the following camera is zooming as zooming changes optimalDistanceFromTarget. If zooming in makes
        // optimalDistance < distanceToTarget the algorithm would think the target has moved when it was the camera that moved.
        if (!_isFollowingCameraZooming) {
            // camera is not zooming so a change in the targets distance is caused by the target, not the camera
            if (_distanceFromTarget < _optimalDistanceFromTarget) {
                // target has moved inside optimal distance so stay put and watch it pass by
                if (_distanceFromTarget < _minimumDistanceFromTarget) {
                    // maintain minimumDistance from target if it gets that close
                    _requestedDistanceFromTarget = _minimumDistanceFromTarget;
                }
                else {
                    _requestedDistanceFromTarget = _distanceFromTarget;
                }
            }
            else {
                // target has moved outside of optimal distance so move camera to follow it
                _requestedDistanceFromTarget = _optimalDistanceFromTarget;
            }
            //D.Log("Camera is not zooming. RequestedDistance is {0:0.#}.", _requestedDistanceFromTarget);
        }
        else {
            //D.Log("Camera is zooming. RequestedDistance is {0:0.#}.", _requestedDistanceFromTarget);
            // camera is zooming so keep requested distance from target at optimal distance
            _requestedDistanceFromTarget = _optimalDistanceFromTarget;
        }

        ProcessChanges(timeSinceLastUpdate);

        if (Mathfx.Approx(_distanceFromTarget, _requestedDistanceFromTarget, .1F)) {
            // Following Camera has completed zooming
            _isFollowingCameraZooming = false;
        }
    }

    #region UpdateCamera_Follow Archive

    //private void Follow_UpdateCamera() {
    //    if (dragFreePanTilt.IsActivated || scrollFreeZoom.IsActivated) {
    //        CurrentState = CameraState.Freeform;
    //        return;
    //    }

    //    // Smooth lookAt interpolation rotates the camera to continue to lookAt the moving Target. These
    //    // values must be continuously updated as the Target and camera are moving
    //    _targetPoint = _target.position;
    //    _targetDirection = (_targetPoint - Position).normalized;
    //    // This places camera directly above target. I want it above and behind...
    //    //_targetDirection = ((_targetPoint - Position) - _target.up * 0.5F).normalized; 

    //    Vector3 lookAt = Quaternion.LookRotation(_targetDirection).eulerAngles;
    //    _yAxisRotation = lookAt.y;
    //    _xAxisRotation = lookAt.x;
    //    _zAxisRotation = lookAt.z;

    //    // Smooth follow interpolation as spectator avoids moving away from the Target if it turns inside our optimal 
    //    // follow distance. When the Target turns and breaks inside the optimal follow distance, stop the camera 
    //    // from adjusting its position by making the requested distance the same as the actual distance. 
    //    // As soon as the Target moves outside of the optimal distance, start following again.
    //    _distanceFromTarget = Vector3.Distance(_targetPoint, Position);
    //    if (_distanceFromTarget < _optimalDistanceFromTarget) {
    //        _requestedDistanceFromTarget = _distanceFromTarget;
    //    }
    //    else {
    //        _requestedDistanceFromTarget = _optimalDistanceFromTarget;
    //    }

    //    Follow_ProcessChanges(GetTimeSinceLastUpdate());
    //}

    #endregion

    void Follow_ExitState() {
        LogEvent();
        _isFollowingCameraZooming = false;
    }

    #endregion

    /// <summary>
    /// Assign the focus object to be the Target and changes the CameraState based on
    /// what interface the object supports.
    /// </summary>
    /// <arg item="focus">The transform of the GO selected as the focus.</arg>
    private void SetFocusAsTarget(Transform focus) {
        // any object that can be focused on has the focus's position as the targetPoint
        ChangeTarget(focus, focus.position);

        if (_isResetOnFocusEnabled) {
            ResetToWorldspace();
        }

        ICameraFocusable qualifiedCameraFocusTarget = focus.GetComponent<ICameraFollowable>();
        if (qualifiedCameraFocusTarget != null) {
            CurrentState = CameraState.Follow;
        }
        else {
            qualifiedCameraFocusTarget = focus.GetComponent<ICameraFocusable>();
            D.Assert(qualifiedCameraFocusTarget != null, "Attempting to SetFocus on object that does not implement either ICameraFollowable or ICameraFocusable.");

            // if not resetting world coordinates on focus, the camera just turns to look at the focus
            CurrentState = _isResetOnFocusEnabled ? CameraState.Focused : CameraState.Focusing;
        }

        float fov = qualifiedCameraFocusTarget.FieldOfView;
        RefreshCamerasFOV(fov);
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
        ICameraTargetable qualifiedCameraTarget = newTarget.GetComponent<ICameraTargetable>();
        if (qualifiedCameraTarget != null) {
            if (!qualifiedCameraTarget.IsCameraTargetEligible) {
                return;
            }
            _minimumDistanceFromTarget = qualifiedCameraTarget.MinimumCameraViewingDistance;
            //D.Log("Target {0} _minimumDistanceFromTarget set to {1:0.#}.".Inject(newTarget.name, _minimumDistanceFromTarget));

            ICameraFocusable qualifiedCameraFocusTarget = newTarget.GetComponent<ICameraFocusable>();
            if (qualifiedCameraFocusTarget != null) {
                _optimalDistanceFromTarget = qualifiedCameraFocusTarget.OptimalCameraViewingDistance;
                //D.Log("Target {0} _optimalDistanceFromTarget set to {1:0.#}.".Inject(newTarget.name, _optimalDistanceFromTarget));
            }
            // no reason to know whether the Target is followable or not for these values for now
        }
        else {
            D.ErrorContext(this, "New Target {0} is not {1}.", newTarget.name, typeof(ICameraTargetable).Name);
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
        transform.rotation = zeroRotation;
        Vector3 zeroRotationVector = zeroRotation.eulerAngles;
        _xAxisRotation = zeroRotationVector.x;
        _yAxisRotation = zeroRotationVector.y;
        _zAxisRotation = zeroRotationVector.z;
        //D.Log("ResetToWorldSpace called. Worldspace Camera Rotation = {0}.".Inject(new Vector3(_xRotation, _yRotation, _zRotation)));
    }

    #endregion

    #region Camera Updating Support

    private float GetTimeSinceLastUpdate(CameraUpdateMode updateMode) {
        return updateMode == CameraUpdateMode.FixedUpdate ? Time.fixedDeltaTime : _gameTime.DeltaTime;
    }

    private void LockCursor(bool toLockCursor) {
        if (toLockCursor) {
            // cursor locked to center of screen and disappears
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
        }
        else {
            // cursor reappears in the center of the screen
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
        }
    }

    private void ProcessChanges(float deltaTime) {
        transform.rotation = CalculateCameraRotation(_cameraRotationDampener * deltaTime);
        //transform.localRotation = CalculateCameraRotation(_cameraRotationDampener * deltaTime);

        // Moved min distance from target clamp of requestedDistance into states so both requested and optimal distance are clamped
        // This way, optimal distance will never be less than minimum distance when assigned to the previous focus
        //_requestedDistanceFromTarget = Mathf.Clamp(_requestedDistanceFromTarget, _minimumDistanceFromTarget, Mathf.Infinity);

        //D.Log("RequestedDistanceFromTarget = {0}.".Inject(_requestedDistanceFromTarget));

        //_distanceFromTarget = Mathfx.Hermite(_distanceFromTarget, _requestedDistanceFromTarget, _cameraDistanceDampener * deltaTime);
        _distanceFromTarget = Mathfx.Lerp(_distanceFromTarget, _requestedDistanceFromTarget, _cameraDistanceDampener * deltaTime);
        //_distanceFromTarget = Mathfx.Sinerp(_distanceFromTarget, _requestedDistanceFromTarget, _cameraDistanceDampener * deltaTime);
        //_distanceFromTarget = Mathfx.Coserp(_distanceFromTarget, _requestedDistanceFromTarget, _cameraDistanceDampener * deltaTime);

        Vector3 proposedPosition = _targetPoint - (_targetDirection * _distanceFromTarget);
        //D.Log("Adjusting position to {0}.", proposedPosition);
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
        if (currentPosition.IsSameAs(proposedPosition) || !ValidatePosition(proposedPosition)) {
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
            var proposedZoomTarget = scrollEvent.target.transform;
            if (proposedZoomTarget == _dummyTarget) {
                // the stationary, existing DummyTarget
                return false;
            }

            if (scrollEvent.target is IZoomToFurthest) {
                // this is a IZoomToFurthest, aka SystemOrbitalPlane so we have to select the target based on what is behind it
                return TrySetZoomTargetAt(screenPoint);
            }
            // scrollEvent.target is not IZoomToFurthest so it is the zoom target
            var proposedZoomPoint = scrollEvent.hitPoint;
            return TryChangeZoomTarget(proposedZoomTarget, proposedZoomPoint);
        }

        // scroll event with a null target so position the dummyTarget
        return PlaceDummyTargetAtUniverseEdgeInDirection(MainCamera_Near.ScreenPointToRay(screenPoint).direction);
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
        Ray ray = MainCamera_Near.ScreenPointToRay(screenPoint);   // Use _mainCamera_Near as ray is cast starting at camera's nearClipPlane
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
                if (proposedZoomTarget.GetComponent<IZoomToFurthest>() == null) {
                    proposedZoomPoint = proposedZoomTarget.position;
                }
                else {
                    proposedZoomPoint = hit.point;
                }
                return TryChangeZoomTarget(proposedZoomTarget, proposedZoomPoint);
            }

            // NOTE: As Rigidbodies consume child collider events, a hit on a child collider when there is a rigidbody parent 
            // involved will return the transform of the parent, not the child. 

            // there are multiple eligibleIctHits
            //var zoomToFurthestHits = from h in eligibleIctHits where h.transform.GetInterface<IZoomToFurthest>() != null select h;
            var zoomToFurthestHits = eligibleIctHits.Where(ictHit => ictHit.transform.GetComponent<IZoomToFurthest>() != null);
            var remainingHits = eligibleIctHits.Except(zoomToFurthestHits);
            if (remainingHits.Any()) {
                // there is a hit that isn't a IZoomToFurthest, so pick the closest and done
                var closestHit = remainingHits.OrderBy(h => (h.transform.position - Position).sqrMagnitude).First();
                proposedZoomTarget = closestHit.transform;
                proposedZoomPoint = proposedZoomTarget.position;
                if (proposedZoomTarget != _dummyTarget) {
                    // rare case where the dummyTarget is hit along with a IZoomToFurthest so not filtered out at top
                    return TryChangeZoomTarget(proposedZoomTarget, proposedZoomPoint);
                }
            }
            // otherwise, all hits are IZoomToFurthest, so pick the furthest and done
            var furthestHit = zoomToFurthestHits.OrderBy(h => (h.transform.position - Position).sqrMagnitude).Last();
            proposedZoomTarget = furthestHit.transform;
            proposedZoomPoint = furthestHit.point;
            //D.Log("IZoomToFurthest furthest hit at {0}.", proposedZoomPoint);
            return TryChangeZoomTarget(proposedZoomTarget, proposedZoomPoint);
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
            ICameraTargetable ict = hit.transform.GetComponent<ICameraTargetable>();
            if (ict != null) {
                if (ict.IsCameraTargetEligible) {
                    eligibleIctHit = new SimpleRaycastHit(hit.transform, hit.point);
                }
            }
            else {
                ict = hit.transform.gameObject.GetComponentInParent<ICameraTargetable>();
                if (ict != null && ict.IsCameraTargetEligible) {
                    Transform ictTransform = (ict as Component).transform;
                    eligibleIctHit = new SimpleRaycastHit(ictTransform, hit.point);
                }
            }

            if (eligibleIctHit != default(SimpleRaycastHit)) {
                eligibleIctHits.Add(eligibleIctHit);
            }
        }
        return eligibleIctHits;
    }

    /// <summary>
    /// Attempts to change the Target to the proposedZoomTarget. If the existing Target is the same
    /// Target but the target point is different, then the change is made to the target point but 
    /// the method returns false as the Target itself wasn't changed.
    /// </summary>
    /// <param name="proposedZoomTarget">The proposed Zoom Target. Logs an error if the DummyTarget.</param>
    /// <param name="proposedZoomPoint">The proposed Zoom Target point.</param>
    /// <returns>
    /// true if the Target itself is changed, otherwise false.
    /// </returns>
    private bool TryChangeZoomTarget(Transform proposedZoomTarget, Vector3 proposedZoomPoint) {
        D.Assert(proposedZoomTarget != _dummyTarget, "TryChangeZoomTarget must not be used to change to the DummyTarget.");

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
        //D.Log("{0}{1}.PlaceDummyTargetAtUniverseEdgeInDirection({2}) called.", GetType().Name, InstanceCount, direction);
        direction.ValidateNormalized();

        Vector3 currentDirectionToDummyTgt = (_dummyTarget.position - Position).normalized;
        if (direction.IsSameDirection(currentDirectionToDummyTgt)) {
            // DummyTarget is already there
            float dummyTgtDistanceToOrigin = Vector3.Distance(_dummyTarget.position, GameConstants.UniverseOrigin); // OPTIMIZE values too big to use SqrMagnitude
            float expectedDummyTgtDistanceToOrigin = _universeRadius - DummyTargetOffsetInsideUniverseEdge;
            D.Assert(Mathfx.Approx(dummyTgtDistanceToOrigin, expectedDummyTgtDistanceToOrigin, 0.5F), "{0} != {1}.", dummyTgtDistanceToOrigin, expectedDummyTgtDistanceToOrigin);
            return false;   // Note 4.29.16: Using large 0.5F tolerance as saw 49999.65 vs expected 49999.4 when trucking on Gigantic
        }

        Ray ray = new Ray(Position, direction);
        RaycastHit targetHit;

        Vector3 pointOutsideUniverse = ray.GetPoint(_universeRadius * 2F);
        if (Physics.Raycast(pointOutsideUniverse, -ray.direction, out targetHit, _universeRadius * 2F)) {
            D.Assert(targetHit.collider == _universeEdgeCollider, "Expected to hit UniverseEdgeCollider. Instead hit {0}!", targetHit.collider.name);
            // Place dummyTgt just inside UniverseEdge so its collider doesn't extend outside the UniverseEdge collider,
            // otherwise we might hit the dummyTgt collider rather than the UniverseEdge collider
            Vector3 hitPtOnUniverseEdgeCollider = targetHit.point;
            _dummyTarget.position = hitPtOnUniverseEdgeCollider - ray.direction * DummyTargetOffsetInsideUniverseEdge;
            ChangeTarget(_dummyTarget, _dummyTarget.position);
            return true;
        }

        D.Error("Camera has not found a Universe Edge point!");
        return false;
    }

    /// <summary>
    /// Calculates a new rotation derived from the current EulerAngles.
    /// </summary>
    /// <arg name="dampenedTimeSinceLastUpdate">The dampened adjusted time since last update.</arg>
    /// <returns></returns>
    private Quaternion CalculateCameraRotation(float dampenedTimeSinceLastUpdate) {
        // keep rotation values exact as a substitute for the unreliable? accuracy that comes from reading EulerAngles from the Quaternion
        _xAxisRotation %= _degreesPerRotation;
        _yAxisRotation %= _degreesPerRotation;
        _zAxisRotation %= _degreesPerRotation;

        Vector3 desiredFacingDirection = new Vector3(_xAxisRotation, _yAxisRotation, _zAxisRotation);

        //D.Log("Desired Facing = {0}.", desiredFacingDirection);

        Quaternion startingRotation = transform.rotation;

        // This approach DOES generate a desired local rotation from the angles BUT it continues to change,
        // always staying in front of the changes from the slerp. This is because .right, .up and .forward continuously 
        // change. This results in a slow and continuous movement across the screen
        //Quaternion xQuaternion = Quaternion.AngleAxis(Mathf.Deg2Rad * _xAxisRotation, transform.right);
        //Quaternion yQuaternion = Quaternion.AngleAxis(Mathf.Deg2Rad * _yAxisRotation, transform.up);
        //Quaternion zQuaternion = Quaternion.AngleAxis(Mathf.Deg2Rad * _zAxisRotation, transform.forward);
        //Quaternion desiredRotation = startingRotation * xQuaternion * yQuaternion * zQuaternion;

        Quaternion desiredRotation = Quaternion.Euler(desiredFacingDirection);
        //D.Log("Desired Rotation: {0}.", desiredRotation.eulerAngles);

        Quaternion resultingRotation = Quaternion.Slerp(startingRotation, desiredRotation, dampenedTimeSinceLastUpdate);
        // OPTIMIZE Lerp is faster but not as pretty when the rotation changes are far apart
        return resultingRotation;
    }

    #endregion

    #region Debug

    /// <summary>
    /// Validates that this activated configuration is the only one that is activated during this update.
    /// Detects whether another active config is overwriting a previous one.
    /// </summary>
    /// <param name="isActivatedAlreadyFound">if set to <c>true</c> [is activated already found].</param>
    private void __ValidateConfigIsOnlyActivated(ref bool isActivatedAlreadyFound) {
        D.Assert(!isActivatedAlreadyFound);
        isActivatedAlreadyFound = true;
    }

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
        _subscriptions.ForAll<IDisposable>(s => s.Dispose());
        _subscriptions.Clear();
        _gameMgr.gameStateChanged -= GameStateChangedEventHandler;
        _inputMgr.unconsumedPress -= UnconsumedPressEventHandler;
    }

    public override string ToString() {
        return new ObjectAnalyzer().ToString(this);
    }

    #region Nested Classes

    /// <summary>
    /// Substitute for Unity's RaycastHit whose constructors are not accessible to me.
    /// Note: Changed from a struct as per Jon Skeet: mutable references in a immutable struct are sneakily evil, aka Transform
    /// </summary>
    private class SimpleRaycastHit {

        private static string _toStringFormat = "Transform hit: {0}, HitPoint: {1}";

        public readonly Transform transform;
        public readonly Vector3 point;

        public SimpleRaycastHit(Transform transform, Vector3 point) {
            this.transform = transform;
            this.point = point;
        }

        public override string ToString() {
            return _toStringFormat.Inject(transform.name, point);
        }

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

    public enum CameraUpdateMode {
        LateUpdate = 0, // Default
        Update = 1,
        FixedUpdate = 2
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
        public float focusingDistanceDampener;
        public float focusedRotationDampener;
        public float focusedDistanceDampener;
        public float freeformRotationDampener;
        public float freeformDistanceDampener;
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

        public override bool IsActivated {
            get {
                return base.IsActivated && InputManager.Instance.IsDragValueWaiting && _inputHelper.IsMouseButtonDown(mouseButton)
                    && !_inputHelper.IsAnyMouseButtonDownBesides(mouseButton);
            }
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

        public override bool IsActivated {
            get {
                return base.IsActivated && InputManager.Instance.IsDragValueWaiting && _inputHelper.IsMouseButtonDown(firstMouseButton)
                    && _inputHelper.IsMouseButtonDown(secondMouseButton);
            }
        }
    }

    [Serializable]
    // Defines Screen Edge Camera controls
    public class ScreenEdgeConfiguration : CameraConfigurationBase {

        // defacto edge value per frame is 1F

        public ScreenEdgeAxis screenEdgeAxis;

        public override bool IsActivated {
            get {
                return base.IsActivated && InputManager.Instance.IsScreenEdgeEventWaiting(screenEdgeAxis);
            }
        }
    }

    [Serializable]
    // Defines Mouse Scroll Wheel Camera Controls
    public class MouseScrollWheelConfiguration : CameraConfigurationBase {

        public override float InputTypeNormalizer {
            // typ scroll tick value is 0.1F per frame
            get { return 10F; }
        }

        public override bool IsActivated {
            get {
                return base.IsActivated && InputManager.Instance.IsScrollEventWaiting;
            }
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
        public override float InputTypeNormalizer { get { return 0.3F; } }

        // Warning: Using Ngui Key events for key control did not work as it doesn't fire continuously when held down

        public override bool IsActivated {
            get {
                return base.IsActivated && InputManager.Instance.IsArrowKeyEventWaiting(keyboardAxis);
            }
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
    ///// NOTE: This is a preprocess method for ContextMenuPickHandler.PressEventHandler(isDown) which is designed to show
    ///// the context menu if the method is called both times (isDown = true, then isDown = false) over the same object.
    ///// Unfortunately, that also means the context menu will show if a drag starts and ends over the same 
    ///// ISelectable object. Therefore, this preprocess method is here to detect whether a drag is occurring before 
    ///// passing it on to show the context menu.
    ///// </summary>
    ///// <param name="isDown">if set to <c>true</c> [is down].</param>
    //public void ShowContextMenuOnPress(bool isDown) {
    //    if (!_gameInput.IsDragging) {
    //        _contextMenuPickHandler.PressEventHandler(isDown);
    //        //D.Log("ContextMenu requested.");
    //    }
    //}

    #endregion

}


