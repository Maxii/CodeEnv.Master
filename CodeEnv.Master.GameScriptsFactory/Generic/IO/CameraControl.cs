// --------------------------------------------------------------------------------------------------------------------
// <copyright>
// Copyright © 2012 - 2013 Strategic Forge
//
// Email: jim@strategicforge.com
// </copyright> 
// <summary> 
// File: CameraControl.cs
//  In Game Camera Controller with Mouse, ScreenEdge and ArrowKey controls enabling Freeform, Focus and Follow capabilities.
// </summary> 
// -------------------------------------------------------------------------------------------------------------------- 

#define DEBUG_LOG
#define DEBUG_LEVEL_WARN
#define DEBUG_LEVEL_ERROR

// default namespace

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
using CodeEnv.Master.Common;
using CodeEnv.Master.Common.Unity;
using CodeEnv.Master.Common.LocalResources;

[RequireComponent(typeof(Camera))]
/// <summary>
/// In Game Camera Controller with Mouse, ScreenEdge and ArrowKey controls enabling Freeform, Focus and Follow capabilities.
/// 
///The nested classes are serializable so that their settings are visible in the inspector. Otherwise, they also don't need to be serializable.
/// </summary>
[SerializeAll]
public class CameraControl : MonoBehaviourBaseSingleton<CameraControl> {

    public class CameraTarget {

        private Vector3 position;
        public Vector3 Position {
            get {
                if (position == Vector3.zero) {
                    return transform.position;
                }
                return position;
            }
            set { position = value; }
        }

        public Transform transform;

        public CameraTarget(Transform _transform) {
            transform = _transform;
        }
    }

    // Focused Zooming: When focused, top and bottom Edge zooming and arrow key zooming cause camera movement in and out from the focused object that is centered on the screen. 
    // ScrollWheel zooming normally does the same if the cursor is pointed at the focused object. If the cursor is pointed somewhere else, scrolling IN moves toward the cursor resulting 
    // in a change to Freeform scrolling. By default, Freeform scrolling OUT is directly opposite the camera's facing. However, there is an option to scroll OUT from the cursor instead. 
    // If this is selected, then scrolling OUT while the cursor is not pointed at the focused object will also result in Freeform scrolling.
    public ScreenEdgeConfiguration edgeFocusZoom = new ScreenEdgeConfiguration { sensitivity = 0.03F, activate = true };
    public MouseScrollWheelConfiguration scrollFocusZoom = new MouseScrollWheelConfiguration { sensitivity = 0.5F, activate = true };
    public ArrowKeyboardConfiguration keyFocusZoom = new ArrowKeyboardConfiguration { keyboardAxis = KeyboardAxis.Vertical, sensitivity = 0.1F, activate = true };

    // Freeform Zooming: When not focused, top and bottom Edge zooming and arrow key zooming cause camera movement forward or backward along the camera's facing.
    // ScrollWheel zooming on the other hand always moves toward the cursor when scrolling IN. By default, scrolling OUT is directly opposite
    // the camera's facing. However, there is an option to scroll OUT from the cursor instead. 
    public ScreenEdgeConfiguration edgeFreeZoom = new ScreenEdgeConfiguration { activate = true };
    public ArrowKeyboardConfiguration keyFreeZoom = new ArrowKeyboardConfiguration { keyboardAxis = KeyboardAxis.Vertical, activate = true };
    public MouseScrollWheelConfiguration scrollFreeZoom = new MouseScrollWheelConfiguration { activate = true };

    // Panning, Tilting and Orbiting: When focused, side edge actuation, arrow key pan and tilting and mouse button/movement results in orbiting of the focused object that is centered on the screen. 
    // When not focused the same arrow keys, edge actuation and mouse button/movement results in the camera panning (looking left or right) and tilting (looking up or down) in place.
    public ScreenEdgeConfiguration edgeFreePan = new ScreenEdgeConfiguration { sensitivity = 10F, activate = true };
    public ScreenEdgeConfiguration edgeFocusOrbit = new ScreenEdgeConfiguration { sensitivity = 10F, activate = true };
    public ArrowKeyboardConfiguration keyFreePan = new ArrowKeyboardConfiguration { keyboardAxis = KeyboardAxis.Horizontal, sensitivity = 0.5F, activate = true };
    public ArrowKeyboardConfiguration keyFocusPan = new ArrowKeyboardConfiguration { keyboardAxis = KeyboardAxis.Horizontal, sensitivity = 0.5F, activate = true };
    public ArrowKeyboardConfiguration keyFreeTilt = new ArrowKeyboardConfiguration { keyboardAxis = KeyboardAxis.Vertical, modifiers = new Modifiers { ctrlKeyReqd = true, shiftKeyReqd = true }, sensitivity = 0.5F, activate = true };
    public ArrowKeyboardConfiguration keyFocusTilt = new ArrowKeyboardConfiguration { keyboardAxis = KeyboardAxis.Vertical, modifiers = new Modifiers { ctrlKeyReqd = true, shiftKeyReqd = true }, sensitivity = 0.5F, activate = true };

    public MouseButtonConfiguration dragFocusOrbit = new MouseButtonConfiguration { mouseButton = MouseButton.Right, sensitivity = 40.0F, activate = true };
    public MouseButtonConfiguration dragFreePanTilt = new MouseButtonConfiguration { mouseButton = MouseButton.Right, sensitivity = 28.0F, activate = true };

    // Truck and Pedestal: Trucking (moving left and right) and Pedestalling (moving up and down) occurs only in Freeform space, repositioning the camera along it's current horizontal and vertical
    // axis'.
    public MouseButtonConfiguration dragFreeTruck = new MouseButtonConfiguration { mouseButton = MouseButton.Middle, modifiers = new Modifiers { altKeyReqd = true }, sensitivity = 0.3F, activate = true };
    public MouseButtonConfiguration dragFreePedestal = new MouseButtonConfiguration { mouseButton = MouseButton.Middle, modifiers = new Modifiers { shiftKeyReqd = true }, sensitivity = 0.3F, activate = true };
    public ArrowKeyboardConfiguration keyFreePedestal = new ArrowKeyboardConfiguration { keyboardAxis = KeyboardAxis.Vertical, modifiers = new Modifiers { ctrlKeyReqd = true }, activate = true };
    public ArrowKeyboardConfiguration keyFreeTruck = new ArrowKeyboardConfiguration { keyboardAxis = KeyboardAxis.Horizontal, modifiers = new Modifiers { ctrlKeyReqd = true }, activate = true };

    // Rolling: Focused and freeform rolling results in the same behaviour, rolling around the camera's current forward axis.
    public MouseButtonConfiguration dragFocusRoll = new MouseButtonConfiguration { mouseButton = MouseButton.Right, modifiers = new Modifiers { altKeyReqd = true }, sensitivity = 40.0F, activate = true };
    public MouseButtonConfiguration dragFreeRoll = new MouseButtonConfiguration { mouseButton = MouseButton.Right, modifiers = new Modifiers { altKeyReqd = true }, sensitivity = 40.0F, activate = true };
    public ArrowKeyboardConfiguration keyFreeRoll = new ArrowKeyboardConfiguration { keyboardAxis = KeyboardAxis.Horizontal, modifiers = new Modifiers { ctrlKeyReqd = true, shiftKeyReqd = true }, activate = true };
    public ArrowKeyboardConfiguration keyFocusRoll = new ArrowKeyboardConfiguration { keyboardAxis = KeyboardAxis.Horizontal, modifiers = new Modifiers { ctrlKeyReqd = true, shiftKeyReqd = true }, activate = true };

    // TODO
    public SimultaneousMouseButtonConfiguration dragFocusZoom = new SimultaneousMouseButtonConfiguration { firstMouseButton = MouseButton.Left, secondMouseButton = MouseButton.Right, sensitivity = 0.2F, activate = true };
    public SimultaneousMouseButtonConfiguration dragFreeZoom = new SimultaneousMouseButtonConfiguration { firstMouseButton = MouseButton.Left, secondMouseButton = MouseButton.Right, sensitivity = 0.2F, activate = true };

    // LEARNINGS
    // Edge-based requested tValues need to be normalized for framerate using timeSinceLastUpdate as the change per second is the framerate * sensitivity.
    // Key-based requested tValues DONOT need to be normalized for framerate using timeSinceLastUpdate as Input.GetAxis() is not framerate dependant.
    // Using +/- Mathf.Abs(requestedDistanceToTarget) accelerates/decelerates movement over time.

    // IMPROVE
    // Should Tilt/EdgePan have some Pedastal/Truck added like Star Ruler?
    // Need more elegant rotation and translation functions when selecting a focusTarget - aka Slerp, Mathf.SmoothDamp/Angle, etc. see my Mathfx
    // Dragging the mouse with any button held down works offscreen OK, but upon release offscreen, immediately enables edge scrolling and panning
    // Implement Camera controls such as clip planes, FieldOfView, RenderSettings.[flareStrength, haloStrength, ambientLight]

    public bool IsResetOnFocusEnabled { get; private set; }
    // ScrollWheel always zooms IN on cursor, zooming OUT with the ScrollWheel is directly backwards by default
    public bool IsScrollZoomOutOnCursorEnabled { get; private set; }

    private bool isRollEnabled;
    public bool IsRollEnabled {
        get { return isRollEnabled; }
        private set { isRollEnabled = value; dragFocusRoll.activate = value; dragFreeRoll.activate = value; keyFreeRoll.activate = value; }
    }

    public Settings settings = new Settings {
        minimumDistanceFromDummyTarget = 50F, activeScreenEdge = 5F,
        followTargetPositionDampener = 1.0F, followTargetLookatDampener = 4.0F, focusingPositionDampener = 6.0F, focusingLookatDampener = 2.0F,
        focusedPositionDampener = 4.0F, focusedRotationDampener = 2.0F, freeformPositionDampener = 3.0F, freeformRotationDampener = 2.0F
    };

    // static so it is available to nested classes
    public static float universeRadius;

    // Values held outside LateUpdate() so they retain the last value that was set 
    // when the movement instruction that was setting it is no longer being called
    private float positionSmoothingDampener = 4.0F;
    private float rotationSmoothingDampener = 4.0F;

    // Cached references
    [DoNotSerialize]    // Serializing this creates duplicates of this object on Save
    private GameEventManager eventMgr;
    [DoNotSerialize]    // Serializing this creates duplicates of this object on Save
    private PlayerPrefsManager playerPrefsMgr;

    private Transform target;
    private Transform dummyTarget;
    private Transform _transform;

    private string[] keyboardAxesNames = new string[] { UnityConstants.KeyboardAxisName_Horizontal, UnityConstants.KeyboardAxisName_Vertical };
    private LayerMask collideWithUniverseEdgeOnlyLayerMask = LayerMaskExtensions.CreateInclusiveMask(Layers.UniverseEdge);
    private LayerMask collideWithDummyTargetOnlyLayerMask = LayerMaskExtensions.CreateInclusiveMask(Layers.DummyTarget);
    private LayerMask collideWithOnlyGameObjectCameraTargetsLayerMask = LayerMaskExtensions.CreateExclusiveMask(Layers.UniverseEdge, Layers.DummyTarget, Layers.DeepSpace);
    private LayerMask collideWithOnlyCameraTargetsLayerMask = LayerMaskExtensions.CreateExclusiveMask(Layers.UniverseEdge, Layers.DeepSpace);
    private Vector3 screenCenter = new Vector3(Screen.width / 2, Screen.height / 2, 0);

    // Calculated Positional fields    
    private float _requestedDistanceFromTarget;
    private float _distanceFromTarget;
    private float _minimumDistanceFromTarget;
    private float _optimalDistanceFromFocusTarget;
    private Vector3 _targetDirection;

    // Continuously calculated, accurate EulerAngles
    private float _xRotation;
    private float _yRotation;
    private float _zRotation;

    // State
    private enum CameraState { None = 0, Focusing = 1, Focused = 2, Freeform = 3, Follow = 4 }
    private CameraState cameraState;

    public enum CameraUpdateMode { LateUpdate = 0, FixedUpdate = 1, Update = 2 }
    public CameraUpdateMode updateMode = CameraUpdateMode.LateUpdate;

    /// <summary>
    /// The 1st method called when the script instance is being loaded. Called once and only once in the lifetime of the script
    /// instance. All game objects have already been initialized so references to other scripts may be established here.
    /// </summary>
    void Awake() {
        Debug.Log("CameraControl.Awake() called.");
        InitializeReferences();
    }

    private void InitializeReferences() {
        IncrementInstanceCounter();
        //if (LevelSerializer.IsDeserializing) { return; }
        eventMgr = GameEventManager.Instance;
        playerPrefsMgr = PlayerPrefsManager.Instance;
        _transform = transform;    // cache it! transform is actually GetComponent<Transform>()
        AddListeners();
        // need to raise this event in Awake as Start can be too late, since the true version of this event is called
        // when the GameState changes to Waiting, which can occur before Start. We have to rely on Loader.Awake
        // being called first via ScriptExecutionOrder.
        eventMgr.Raise<ElementReadyEvent>(new ElementReadyEvent(this, isReady: false));
    }

    private void AddListeners() {
        eventMgr.AddListener<FocusSelectedEvent>(this, OnFocusSelected);
        eventMgr.AddListener<OptionChangeEvent>(this, OnOptionChange);
        eventMgr.AddListener<GameStateChangedEvent>(this, OnGameStateChange);
    }

    [DoNotSerialize]
    private bool _restoredGameFlag = false;
    private void OnGameStateChange(GameStateChangedEvent e) {
        switch (e.NewState) {
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
                eventMgr.Raise<ElementReadyEvent>(new ElementReadyEvent(this, isReady: true));
                break;
            case GameState.Running:
            case GameState.Building:
            case GameState.Loading:
                // do nothing
                break;
            case GameState.None:
            default:
                throw new NotImplementedException(ErrorMessages.UnanticipatedSwitchValue.Inject(e.NewState));
        }
    }

    #region Little used Unity Events

    void Start() {
        //Debug.Log("CameraControl.Start() called.");
        //CheckScriptCompilerSettings();
        //InitializeMainCamera();
    }

    private void CheckScriptCompilerSettings() {
        Debug.Log("Compiler Preprocessor Settings in {0} follow:".Inject(Instance.GetType().Name) + Constants.NewLine);
#if DEBUG
        Debug.Log("DEBUG, ");
#endif

#if UNITY_EDITOR
            Debug.Log("UNITY_EDITOR, ");
#endif

#if DEBUG_LEVEL_LOG
        Debug.Log("DEBUG_LEVEL_LOG, ");
#endif

#if DEBUG_LEVEL_WARN
        Debug.Log("DEBUG_LEVEL_WARN, ");
#endif

#if DEBUG_LEVEL_ERROR
        Debug.Log("DEBUG_LEVEL_ERROR, ");
#endif

#if DEBUG_LOG
        Debug.Log("DEBUG_LOG, ");
#endif
    }

    /// <summary>
    /// Called when enabled set to true after the script has been loaded, including after DeSerialization.
    /// </summary>
    void OnEnable() {
        //Debug.Log("Camera OnEnable() called. Enabled = " + enabled);
    }

    // temp workaround for funny behaviour with application focusTarget starting GameScene
    private bool _ignore = true;
    /// <summary>
    /// Called when application goes in/out of focusTarget, this method controls the
    /// enabled state of the camera so it doesn't move when I use the mouse outside of 
    /// the editor window.
    /// </summary>
    /// <arg item="isFocus">if set to <c>true</c> [is focusTarget].</arg>
    void OnApplicationFocus(bool isFocus) {
        //Debug.Log("Camera OnApplicationFocus(" + isFocus + ") called.");
        if (_ignore) {
            _ignore = false;
            return;
        }
        enabled = isFocus;
    }

    /// <summary>
    /// Called when the application is minimized/resumed, this method controls the enabled
    /// state of the camera so it doesn't move when I use the mouse to minimize Unity.
    /// </summary>
    /// <arg item="isPausing">if set to <c>true</c> [is toPause].</arg>
    void OnApplicationPause(bool isPaused) {
        //Debug.Log("Camera OnApplicationPause(" + isPaused + ") called.");
        enabled = !isPaused;
    }

    /// <summary>
    /// Called when enabled set to false. It is also called when the object is destroyed and when
    /// scripts are reloaded after compilation has finished.
    /// </summary>
    void OnDisable() {
        // Debug.Log("Camera OnDisable() called.");
    }

    protected override void OnApplicationQuit() {
        instance = null;
    }

    #endregion

    private void InitializeMainCamera() {
        Debug.Log("Camera initializing.");
        SetCameraSettings();
        SetPlayerPrefs();
        PositionCameraForGame();
    }

    private void SetCameraSettings() {
        universeRadius = GameManager.Settings.SizeOfUniverse.GetUniverseRadius();
        Camera.main.farClipPlane = universeRadius * 2;

        // This camera will see all layers except for the GUI and DeepSpace layers. If I want to add exclusions, I can still do it from the outside
        camera.cullingMask = LayerMaskExtensions.CreateExclusiveMask(Layers.Gui, Layers.DeepSpace);
        UpdateRate = UpdateFrequency.Continuous;
    }

    private void SetPlayerPrefs() {
        IsResetOnFocusEnabled = playerPrefsMgr.IsResetOnFocusEnabled;
        IsRollEnabled = playerPrefsMgr.IsCameraRollEnabled;
        IsScrollZoomOutOnCursorEnabled = playerPrefsMgr.IsZoomOutOnCursorEnabled;
    }

    private void PositionCameraForGame() {
        CreateUniverseEdge();
        CreateAndPositionDummyTarget();    // do it here as the camera is the only user of the object

        // UNDONE whether starting or continuing saved game, camera position should be focused on the player's starting planet, no rotation
        ResetToWorldspace();
        ChangeState(CameraState.Freeform);
    }

    private void CreateUniverseEdge() {
        SphereCollider universeEdge = null;
        SphereCollider universeEdgePrefab = RequiredPrefabs.Instance.UniverseEdgePrefab;
        if (universeEdgePrefab == null) {
            Debug.LogWarning("UniverseEdgePrefab on RequiredPrefabs is null.");
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

    private void CreateAndPositionDummyTarget() {
        Transform dummyTargetPrefab = RequiredPrefabs.Instance.CameraDummyTargetPrefab;
        if (dummyTargetPrefab == null) {
            Debug.LogWarning("DummyTargetPrefab on RequiredPrefabs is null.");
            string dummyTargetName = Layers.DummyTarget.GetName();
            dummyTarget = new GameObject(dummyTargetName).transform;
            dummyTarget.gameObject.layer = (int)Layers.DummyTarget;
            dummyTarget.gameObject.AddComponent<SphereCollider>();
        }
        else {
            dummyTarget = Instantiate<Transform>(dummyTargetPrefab);
        }
        dummyTarget.collider.enabled = false;
        // the collider is disabled so the placement algorithm doesn't accidently find it already in front of the camera
        TryPlaceDummyTargetAtUniverseEdgeInDirection(_transform.forward);
        dummyTarget.parent = DynamicObjects.Folder;
        dummyTarget.collider.enabled = true;
    }

    void OnDeserialized() {
        Debug.Log("Camera.OnDeserialized() called.");
    }

    private void OnOptionChange(OptionChangeEvent e) {
        OptionSettings settings = e.Settings;
        IsResetOnFocusEnabled = settings.IsResetOnFocusEnabled;
        IsRollEnabled = settings.IsCameraRollEnabled;
        IsScrollZoomOutOnCursorEnabled = settings.IsZoomOutOnCursorEnabled;
        //Debug.Log("Option Change Event received. IsResetOnFocusEnabled = {0}.".Inject(IsResetOnFocusEnabled));
    }

    private void OnFocusSelected(FocusSelectedEvent e) {
        Debug.Log("FocusSelectedEvent received by Camera.");
        SetFocus(e.FocusTransform);
    }

    /// <summary>
    /// Assign the focus object to be the target and changes the CameraState based on
    /// what interface the object supports.
    /// </summary>
    /// <arg item="focus">The transform of the GO selected as the focus.</arg>
    private void SetFocus(Transform focus) {
        ChangeTarget(focus);

        if (focus.gameObject.GetInterface<IFollow>() != null) {
            ChangeState(CameraState.Follow);
            return;
        }

        if (focus.gameObject.GetInterface<IFocus>() != null) {
            if (!IsResetOnFocusEnabled) {
                // if not resetting world coordinates on focus, the camera just turns to look at the focus
                ChangeState(CameraState.Focusing);
                return;
            }

            ResetToWorldspace();
            ChangeState(CameraState.Focused);
        }
        else {
            Debug.LogError("Attempting to SetFocus on object that does not implement either IFollow or IFocus.");
        }
    }

    /// <summary>
    /// Changes the current target to the provided new target along with 
    /// appropriate minimum, optimal and actual camera distance settings. If the newTarget is
    /// already the existing target and its not the dummyTarget, then the method simply returns.
    /// </summary>
    /// <param name="newTarget">The new target.</param>
    private void ChangeTarget(Transform newTarget) {
        if (newTarget == target && newTarget != dummyTarget) {
            Debug.LogWarning("Attempt to change the existing (non-dummy) target {0} to itself.".Inject(newTarget.name));
            return;
        }

        target = newTarget;
        // anytime the target changes, the actual distance to the target should also be reset
        _distanceFromTarget = Vector3.Distance(target.position, _transform.position);
        // the requested distance to the target will vary depending on where the change was initiated from

        if (newTarget == dummyTarget) {
            _minimumDistanceFromTarget = settings.minimumDistanceFromDummyTarget;
            //Debug.Log("New Target is the DummyTarget, _minimumDistanceFromTarget = {0}.".Inject(_minimumDistanceFromTarget));
            // optimal distance settings not used with dummy target
            return;
        }

        ICameraTarget qualifiedCameraTarget = newTarget.gameObject.GetInterface<ICameraTarget>();
        if (qualifiedCameraTarget != null) {
            _minimumDistanceFromTarget = qualifiedCameraTarget.MinimumCameraApproachDistance;
            Debug.Log("New Target {0} _minimumDistanceFromTarget = {1}.".Inject(newTarget.name, _minimumDistanceFromTarget));

            IFocus qualifiedCameraFocusTarget = newTarget.gameObject.GetInterface<IFocus>();
            if (qualifiedCameraFocusTarget != null) {
                _optimalDistanceFromFocusTarget = qualifiedCameraFocusTarget.OptimalCameraApproachDistance;
                Debug.Log("New Target {0} _optimalDistanceFromFocusTarget set to {1}.".Inject(newTarget.name, _optimalDistanceFromFocusTarget));
            }
            // no reason to know whether the target is followable or not for these values for now
        }
        else {
            Debug.LogError("New Target {0} is not an ICameraTarget.".Inject(newTarget.name));
        }
    }


    /// <summary>
    /// Changes the CameraState and sets calculated values to reflect the new state.
    /// </summary>
    /// <arg item="newState">The new state.</arg>
    /// <exception cref="System.NotImplementedException"></exception>
    private void ChangeState(CameraState newState) {
        Arguments.ValidateNotNull(target);
        cameraState = newState;
        switch (newState) {
            case CameraState.Focusing:
                _distanceFromTarget = Vector3.Distance(target.position, _transform.position);
                _requestedDistanceFromTarget = _optimalDistanceFromFocusTarget;
                _targetDirection = (target.position - _transform.position).normalized;

                // face the selected target
                //Debug.Log("Rotation values before ChangeState {0}.".Inject(new Vector3(_xRotation, _yRotation, _zRotation)));
                Quaternion lookAt = Quaternion.LookRotation(_targetDirection);
                Vector3 lookAtVector = lookAt.eulerAngles;
                _xRotation = lookAtVector.y;
                _yRotation = lookAtVector.x;
                _zRotation = lookAtVector.z;
                //Debug.Log("Rotation values after ChangeState {0}.".Inject(new Vector3(_xRotation, _yRotation, _zRotation)));
                rotationSmoothingDampener = settings.focusingLookatDampener;
                positionSmoothingDampener = settings.focusingPositionDampener;
                break;
            case CameraState.Focused:
                // entered via OnFocusSelected AND IsResetOnFocusEnabled, OR after Focusing has completed
                _distanceFromTarget = Vector3.Distance(target.position, _transform.position);
                _requestedDistanceFromTarget = _optimalDistanceFromFocusTarget;
                // x,y,z rotation has already been established before entering

                rotationSmoothingDampener = settings.focusedRotationDampener;
                positionSmoothingDampener = settings.focusedPositionDampener;
                break;
            case CameraState.Freeform:
                _distanceFromTarget = Vector3.Distance(target.position, _transform.position);
                _requestedDistanceFromTarget = _distanceFromTarget;
                // no facing change

                rotationSmoothingDampener = settings.freeformRotationDampener;
                positionSmoothingDampener = settings.freeformPositionDampener;
                break;
            case CameraState.Follow:
                _distanceFromTarget = Vector3.Distance(target.position, _transform.position);
                _requestedDistanceFromTarget = _optimalDistanceFromFocusTarget;

                rotationSmoothingDampener = settings.followTargetLookatDampener;
                positionSmoothingDampener = settings.followTargetPositionDampener;
                break;
            case CameraState.None:
            default:
                throw new NotImplementedException(ErrorMessages.UnanticipatedSwitchValue.Inject(newState));
        }
        Debug.Log("CameraState changed to " + cameraState);
    }

    /// <summary>
    /// Resets the camera rotation to that of worldspace, no rotation.
    /// </summary>
    public void ResetToWorldspace() {
        // current and requested distance to target already set
        Quaternion zeroRotation = Quaternion.identity;
        _transform.rotation = zeroRotation;
        Vector3 zeroRotationVector = zeroRotation.eulerAngles;
        _xRotation = zeroRotationVector.y;
        _yRotation = zeroRotationVector.x;
        _zRotation = zeroRotationVector.z;
        //Debug.Log("ResetToWorldSpace called. Worldspace Camera Rotation = {0}.".Inject(new Vector3(_xRotation, _yRotation, _zRotation)));
    }



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
            //Debug.Log("___________________New Frame______________________");
            float timeSinceLastUpdate = GameTime.DeltaTime * (int)UpdateRate;
            bool _toLockCursor = false;
            float mouseInputValue = 0F;
            bool showDistanceDebugLog = false;

            switch (cameraState) {
                case CameraState.Focusing:
                    // transition state to allow lookAt to complete. Only entered from OnFocusSelected, when !IsResetOnFocus
                    //Debug.Log("Focusing. RequestedDistanceFromTarget = {0}.".Inject(_requestedDistanceFromTarget));
                    showDistanceDebugLog = true;
                    if (CheckExitConditions()) {
                        ChangeState(CameraState.Focused);
                        return;
                    }
                    // exits to Focused when the lookAt rotation is complete, ie. _targetDirection 'equals' _transform.forward
                    _toLockCursor = true;

                    // lets me change the values on the fly in the inspector
                    rotationSmoothingDampener = settings.focusingLookatDampener;
                    positionSmoothingDampener = settings.focusingPositionDampener;
                    // no other functionality active 
                    break;
                case CameraState.Focused:
                    //Debug.Log("Focused. RequestedDistanceFromTarget = {0}.".Inject(_requestedDistanceFromTarget));
                    showDistanceDebugLog = true;
                    if (CheckExitConditions()) {
                        ChangeState(CameraState.Freeform);
                        return;
                    }
                    if (dragFocusOrbit.IsActivated()) {
                        _toLockCursor = true;
                        if (GameInput.IsHorizontalMouseMovement(out mouseInputValue)) {
                            _xRotation += mouseInputValue * dragFocusOrbit.sensitivity * timeSinceLastUpdate;
                        }
                        if (GameInput.IsVerticalMouseMovement(out mouseInputValue)) {
                            _yRotation -= mouseInputValue * dragFocusOrbit.sensitivity * timeSinceLastUpdate;
                        }
                        //rotationSmoothingDampener = dragFocusOrbit.dampener;
                    }
                    if (edgeFocusOrbit.IsActivated()) {
                        float xMousePosition = Input.mousePosition.x;
                        if (xMousePosition <= settings.activeScreenEdge) {
                            _xRotation -= edgeFocusOrbit.sensitivity * timeSinceLastUpdate;
                            //rotationSmoothingDampener = edgeFocusOrbit.dampener;
                        }
                        else if (xMousePosition >= Screen.width - settings.activeScreenEdge) {
                            _xRotation += edgeFocusOrbit.sensitivity * timeSinceLastUpdate;
                            //rotationSmoothingDampener = edgeFocusOrbit.dampener;
                        }
                    }
                    if (dragFocusRoll.IsActivated()) {
                        _toLockCursor = true;
                        if (GameInput.IsHorizontalMouseMovement(out mouseInputValue)) {
                            _zRotation -= mouseInputValue * dragFocusRoll.sensitivity * timeSinceLastUpdate;
                        }
                        //rotationSmoothingDampener = dragFocusRoll.dampener;
                    }
                    if (scrollFocusZoom.IsActivated()) {
                        if (GameInput.IsScrollWheelMovement(out mouseInputValue)) {
                            if (mouseInputValue > 0 || (mouseInputValue < 0 && IsScrollZoomOutOnCursorEnabled)) {
                                if (TrySetTargetAtScreenPoint(Input.mousePosition)) {
                                    // there is a new target so it can't be the old focus target
                                    ChangeState(CameraState.Freeform);
                                    return;
                                }
                            }
                            float translationSpeedGoverner = Mathf.Clamp(Mathf.Abs(_requestedDistanceFromTarget), 0F, settings.MaxSpeedGoverner);
                            _requestedDistanceFromTarget -= mouseInputValue * translationSpeedGoverner * scrollFocusZoom.sensitivity * scrollFocusZoom.TranslationSpeedNormalizer * timeSinceLastUpdate;
                            //positionSmoothingDampener = scrollFocusZoom.dampener;
                            //Debug.Log("MaxSpeedGoverner = {0}, mouseInputValue = {1}".Inject(settings.MaxSpeedGoverner, mouseInputValue));
                            Debug.Log("ScrollFocusZoom translationSpeedGoverner = {0}, _requestedDistanceFromTarget = {1}".Inject(translationSpeedGoverner, _requestedDistanceFromTarget));
                        }
                    }
                    if (edgeFocusZoom.IsActivated()) {
                        float yMousePosition = Input.mousePosition.y;
                        if (yMousePosition <= settings.activeScreenEdge) {
                            float translationSpeedGoverner = Mathf.Clamp(Mathf.Abs(_requestedDistanceFromTarget), 0F, settings.MaxSpeedGoverner);
                            _requestedDistanceFromTarget += translationSpeedGoverner * edgeFocusZoom.sensitivity * edgeFocusZoom.TranslationSpeedNormalizer * timeSinceLastUpdate;
                            Debug.Log("edgeFocusZoom translationSpeedGoverner = {0}, _requestedDistanceFromTarget = {1}".Inject(translationSpeedGoverner, _requestedDistanceFromTarget));
                            //positionSmoothingDampener = edgeFocusZoom.dampener;
                        }
                        else if (yMousePosition >= Screen.height - settings.activeScreenEdge) {
                            float translationSpeedGoverner = Mathf.Clamp(Mathf.Abs(_requestedDistanceFromTarget), 0F, settings.MaxSpeedGoverner);
                            _requestedDistanceFromTarget -= translationSpeedGoverner * edgeFocusZoom.sensitivity * edgeFocusZoom.TranslationSpeedNormalizer * timeSinceLastUpdate;
                            Debug.Log("edgeFocusZoom translationSpeedGoverner = {0}, _requestedDistanceFromTarget = {1}".Inject(translationSpeedGoverner, _requestedDistanceFromTarget));
                            //positionSmoothingDampener = edgeFocusZoom.dampener;
                        }
                    }
                    if (keyFocusZoom.IsActivated()) {
                        float translationSpeedGoverner = Mathf.Clamp(Mathf.Abs(_requestedDistanceFromTarget), 0F, settings.MaxSpeedGoverner);
                        _requestedDistanceFromTarget -= Input.GetAxis(keyboardAxesNames[(int)keyFocusZoom.keyboardAxis]) * translationSpeedGoverner * keyFocusZoom.TranslationSpeedNormalizer * keyFocusZoom.sensitivity;
                        Debug.Log("keyFocusZoom translationSpeedGoverner = {0}, _requestedDistanceFromTarget = {1}".Inject(translationSpeedGoverner, _requestedDistanceFromTarget));
                        //positionSmoothingDampener = keyFocusZoom.dampener;
                    }
                    if (dragFocusZoom.IsActivated()) {
                        _toLockCursor = true;
                        if (GameInput.IsVerticalMouseMovement(out mouseInputValue)) {
                            //Debug.Log("MouseFocusZoom Vertical Mouse Movement detected.");
                            float translationSpeedGoverner = Mathf.Clamp(Mathf.Abs(_requestedDistanceFromTarget), 0F, settings.MaxSpeedGoverner);
                            _requestedDistanceFromTarget -= mouseInputValue * translationSpeedGoverner * dragFocusZoom.sensitivity * dragFocusZoom.TranslationSpeedNormalizer * timeSinceLastUpdate;
                            Debug.Log("dragFocusZoom translationSpeedGoverner = {0}, _requestedDistanceFromTarget = {1}".Inject(translationSpeedGoverner, _requestedDistanceFromTarget));
                        }
                        //positionSmoothingDampener = dragFocusZoom.dampener;
                    }
                    if (keyFocusPan.IsActivated()) {
                        _xRotation += Input.GetAxis(keyboardAxesNames[(int)keyFreePan.keyboardAxis]) * keyFreePan.sensitivity;
                        //rotationSmoothingDampener = keyFocusPan.dampener;
                    }
                    if (keyFocusTilt.IsActivated()) {
                        _yRotation -= Input.GetAxis(keyboardAxesNames[(int)keyFreeTilt.keyboardAxis]) * keyFreeTilt.sensitivity;
                        //rotationSmoothingDampener = keyFocusTilt.dampener;
                    }
                    if (keyFocusRoll.IsActivated()) {
                        _zRotation -= Input.GetAxis(keyboardAxesNames[(int)keyFreeRoll.keyboardAxis]) * keyFreeRoll.sensitivity;
                        //rotationSmoothingDampener = keyFocusRoll.dampener;
                    }

                    // transform.forward is the camera's current definition of 'forward', ie. WorldSpace's absolute forward adjusted by the camera's rotation (Vector.forward * cameraRotation )   
                    // this is the key that keeps the camera pointed at the target when focused
                    _targetDirection = _transform.forward;

                    // lets me change the values on the fly in the inspector
                    rotationSmoothingDampener = settings.focusedRotationDampener;
                    positionSmoothingDampener = settings.focusedPositionDampener;
                    break;
                case CameraState.Freeform:
                    if (edgeFreePan.IsActivated()) {
                        float xMousePosition = Input.mousePosition.x;
                        if (xMousePosition <= settings.activeScreenEdge) {
                            _xRotation -= edgeFreePan.sensitivity * timeSinceLastUpdate;
                            //rotationSmoothingDampener = edgeFreePan.dampener;
                        }
                        else if (xMousePosition >= Screen.width - settings.activeScreenEdge) {
                            _xRotation += edgeFreePan.sensitivity * timeSinceLastUpdate;
                            //rotationSmoothingDampener = edgeFreePan.dampener;
                        }
                    }
                    if (dragFreeTruck.IsActivated()) {
                        _toLockCursor = true;
                        if (GameInput.IsHorizontalMouseMovement(out mouseInputValue)) {
                            TryPlaceDummyTargetAtUniverseEdgeInDirection(_transform.right);
                            _requestedDistanceFromTarget -= mouseInputValue * dragFreeTruck.sensitivity * dragFreeTruck.TranslationSpeedNormalizer * timeSinceLastUpdate;
                        }
                        //positionSmoothingDampener = dragFreeTruck.dampener;
                    }
                    if (dragFreePedestal.IsActivated()) {
                        _toLockCursor = true;
                        if (GameInput.IsVerticalMouseMovement(out mouseInputValue)) {
                            TryPlaceDummyTargetAtUniverseEdgeInDirection(_transform.up);
                            _requestedDistanceFromTarget -= mouseInputValue * dragFreePedestal.sensitivity * dragFreePedestal.TranslationSpeedNormalizer * timeSinceLastUpdate;
                        }
                        //positionSmoothingDampener = dragFreePedestal.dampener;
                    }
                    if (dragFreeRoll.IsActivated()) {
                        _toLockCursor = true;
                        if (GameInput.IsHorizontalMouseMovement(out mouseInputValue)) {
                            _zRotation -= mouseInputValue * dragFreeRoll.sensitivity * timeSinceLastUpdate;
                        }
                        //rotationSmoothingDampener = dragFreeRoll.dampener;
                    }
                    if (dragFreePanTilt.IsActivated()) {
                        _toLockCursor = true;
                        if (GameInput.IsHorizontalMouseMovement(out mouseInputValue)) {
                            _xRotation += mouseInputValue * dragFreePanTilt.sensitivity * timeSinceLastUpdate;
                        }
                        if (GameInput.IsVerticalMouseMovement(out mouseInputValue)) {
                            _yRotation -= mouseInputValue * dragFreePanTilt.sensitivity * timeSinceLastUpdate;
                        }
                        //rotationSmoothingDampener = dragFreePanTilt.dampener;
                    }
                    if (scrollFreeZoom.IsActivated()) {
                        if (GameInput.IsScrollWheelMovement(out mouseInputValue)) {
                            // Debug.LogWarning("Mouse ScrollWheel is non-zero at {0:0.000000}.".Inject(mouseInputValue));
                            if (mouseInputValue > 0) {
                                // Scroll ZoomIN command
                                if (TrySetTargetAtScreenPoint(Input.mousePosition)) {
                                    // target was changed so reset requested distance to actual distance
                                    _requestedDistanceFromTarget = _distanceFromTarget;
                                }
                            }
                            if (mouseInputValue < 0) {
                                // Scroll ZoomOUT command
                                if (IsScrollZoomOutOnCursorEnabled) {
                                    if (TrySetTargetAtScreenPoint(Input.mousePosition)) {
                                        // target was changed so reset requested distance to actual distance
                                        _requestedDistanceFromTarget = _distanceFromTarget;
                                    }
                                }
                                else {
                                    if (TrySetTargetAtScreenPoint(screenCenter)) {
                                        // target was changed so reset requested distance to actual distance
                                        _requestedDistanceFromTarget = _distanceFromTarget;
                                    }
                                }
                            }
                            float translationSpeedGoverner = Mathf.Clamp(Mathf.Abs(_requestedDistanceFromTarget), 0F, settings.MaxSpeedGoverner);
                            _requestedDistanceFromTarget -= mouseInputValue * translationSpeedGoverner * scrollFreeZoom.sensitivity * scrollFreeZoom.TranslationSpeedNormalizer * timeSinceLastUpdate;
                            //Debug.Log("ScrollFreeZoom translationSpeedGoverner = {0}, _requestedDistanceFromTarget = {1}".Inject(translationSpeedGoverner, _requestedDistanceFromTarget));
                            //positionSmoothingDampener = scrollFreeZoom.dampener;
                            showDistanceDebugLog = true;
                        }
                    }
                    if (edgeFreeZoom.IsActivated()) {
                        float yMousePosition = Input.mousePosition.y;
                        if (yMousePosition <= settings.activeScreenEdge) {
                            // Edge ZoomOUT
                            if (TrySetTargetAtScreenPoint(screenCenter)) {
                                // target was changed so reset requested distance to actual distance
                                _requestedDistanceFromTarget = _distanceFromTarget;
                            }
                            _requestedDistanceFromTarget += edgeFreeZoom.sensitivity * edgeFreeZoom.TranslationSpeedNormalizer * timeSinceLastUpdate;
                            //Debug.Log("EdgeFreeZoom _requestedDistanceFromTarget = " + _requestedDistanceFromTarget);
                            //positionSmoothingDampener = edgeFreeZoom.dampener;
                        }
                        else if (yMousePosition >= Screen.height - settings.activeScreenEdge) {
                            // Edge ZoomIN
                            if (TrySetTargetAtScreenPoint(screenCenter)) {
                                // target was changed so reset requested distance to actual distance
                                _requestedDistanceFromTarget = _distanceFromTarget;
                            }
                            _requestedDistanceFromTarget -= edgeFreeZoom.sensitivity * edgeFreeZoom.TranslationSpeedNormalizer * timeSinceLastUpdate;
                            //positionSmoothingDampener = edgeFreeZoom.dampener;
                        }
                    }
                    if (dragFreeZoom.IsActivated()) {
                        _toLockCursor = true;
                        if (GameInput.IsVerticalMouseMovement(out mouseInputValue)) {
                            if (TrySetTargetAtScreenPoint(screenCenter)) {
                                _requestedDistanceFromTarget = _distanceFromTarget;
                            }
                            _requestedDistanceFromTarget -= mouseInputValue * dragFreeZoom.sensitivity * dragFreeZoom.TranslationSpeedNormalizer * timeSinceLastUpdate;
                        }
                        //positionSmoothingDampener = dragFreeZoom.dampener;
                    }

                    // Freeform Arrow Keyboard Configurations. Mouse Buttons supercede Arrow Keys. Only Arrow Keys are used as IsActivated() must be governed by 
                    // whether the appropriate key is down to keep the configurations from interfering with each other. 
                    if (keyFreeZoom.IsActivated()) {
                        if (TrySetTargetAtScreenPoint(screenCenter)) {
                            _requestedDistanceFromTarget = _distanceFromTarget;
                        }
                        showDistanceDebugLog = true;
                        _requestedDistanceFromTarget -= Input.GetAxis(keyboardAxesNames[(int)keyFreeZoom.keyboardAxis]) * keyFreeZoom.sensitivity * keyFreeZoom.TranslationSpeedNormalizer;
                        //positionSmoothingDampener = keyFreeZoom.dampener;
                    }
                    if (keyFreeTruck.IsActivated()) {
                        TryPlaceDummyTargetAtUniverseEdgeInDirection(_transform.right);
                        _requestedDistanceFromTarget -= Input.GetAxis(keyboardAxesNames[(int)keyFreeTruck.keyboardAxis]) * keyFreeTruck.sensitivity * keyFreeTruck.TranslationSpeedNormalizer;
                        //positionSmoothingDampener = keyFreeTruck.dampener;
                    }
                    if (keyFreePedestal.IsActivated()) {
                        TryPlaceDummyTargetAtUniverseEdgeInDirection(_transform.up);
                        _requestedDistanceFromTarget -= Input.GetAxis(keyboardAxesNames[(int)keyFreePedestal.keyboardAxis]) * keyFreePedestal.sensitivity * keyFreePedestal.TranslationSpeedNormalizer;
                        //positionSmoothingDampener = keyFreePedestal.dampener;
                    }
                    if (keyFreePan.IsActivated()) {
                        _xRotation += Input.GetAxis(keyboardAxesNames[(int)keyFreePan.keyboardAxis]) * keyFreePan.sensitivity;
                        //rotationSmoothingDampener = keyFreePan.dampener;
                    }
                    if (keyFreeTilt.IsActivated()) {
                        _yRotation -= Input.GetAxis(keyboardAxesNames[(int)keyFreeTilt.keyboardAxis]) * keyFreeTilt.sensitivity;
                        //rotationSmoothingDampener = keyFreeTilt.dampener;
                    }
                    if (keyFreeRoll.IsActivated()) {
                        _zRotation -= Input.GetAxis(keyboardAxesNames[(int)keyFreeRoll.keyboardAxis]) * keyFreeRoll.sensitivity;
                        //rotationSmoothingDampener = keyFreeRoll.dampener;
                    }

                    _targetDirection = (target.position - _transform.position).normalized;

                    // lets me change the values on the fly in the inspector
                    rotationSmoothingDampener = settings.freeformRotationDampener;
                    positionSmoothingDampener = settings.freeformPositionDampener;
                    break;
                case CameraState.Follow:    // Follow as Spectator, not Chase
                    if (CheckExitConditions()) {
                        // exit on Mouse Scroll, Pan/Tilt or Key Escape
                        ChangeState(CameraState.Freeform);
                        return;
                    }
                    // direction, resulting rotation and target distance must be updated continuously as the target moves
                    // smooth lookAt interpolation
                    _targetDirection = (target.position - _transform.position).normalized;
                    Quaternion lookAt = Quaternion.LookRotation(_targetDirection);
                    Vector3 lookAtVector = lookAt.eulerAngles;
                    _xRotation = lookAtVector.y;
                    _yRotation = lookAtVector.x;
                    _zRotation = lookAtVector.z;

                    // Smooth follow interpolation as spectator avoids moving away from the target if it turns inside our optimal 
                    // follow distance. When the target turns and breaks inside the optimal follow distance, stop the camera 
                    // from adjusting its position by making the requested distance the same as the actual distance. 
                    // As soon as the target moves outside of the optimal distance, start following again.
                    _distanceFromTarget = Vector3.Distance(target.position, _transform.position);
                    if (_distanceFromTarget > _optimalDistanceFromFocusTarget) {
                        _requestedDistanceFromTarget = _optimalDistanceFromFocusTarget;
                    }
                    else {
                        _requestedDistanceFromTarget = _distanceFromTarget;
                    }

                    // lets me change the values on the fly in the inspector
                    rotationSmoothingDampener = settings.followTargetLookatDampener;
                    positionSmoothingDampener = settings.followTargetPositionDampener;
                    break;
                case CameraState.None:
                default:
                    throw new NotImplementedException(ErrorMessages.UnanticipatedSwitchValue.Inject(cameraState));
            }

            _transform.rotation = CalculateCameraRotation(rotationSmoothingDampener * timeSinceLastUpdate);
            //Debug.Log("RequestedDistanceFromTarget = {0}, MinimumDistanceFromTarget = {1}.".Inject(_requestedDistanceFromTarget, settings.minimumDistanceFromTarget));
            _requestedDistanceFromTarget = Mathf.Clamp(_requestedDistanceFromTarget, _minimumDistanceFromTarget, Mathf.Infinity);
            //Debug.Log("RequestedDistanceFromTarget = {0}.".Inject(_requestedDistanceFromTarget));

            _distanceFromTarget = Mathfx.Lerp(_distanceFromTarget, _requestedDistanceFromTarget, positionSmoothingDampener * timeSinceLastUpdate);
            if (showDistanceDebugLog) {
                Debug.Log("RequestedDistanceFromTarget = {0}, Actual DistanceFromTarget = {1}.".Inject(_requestedDistanceFromTarget, _distanceFromTarget));
                showDistanceDebugLog = false;
            }

            Vector3 _proposedPosition = target.position - (_targetDirection * _distanceFromTarget);
            _transform.position = ValidatePosition(_proposedPosition);

            ManageCursorDisplay(_toLockCursor);
        }
    }

    private bool CheckExitConditions() {
        switch (cameraState) {
            case CameraState.Focusing:
                if (Mathfx.Approx(_targetDirection, _transform.forward, .001F)) {
                    return true;
                }
                return false;
            case CameraState.Focused:
                if (dragFreeTruck.IsActivated() || dragFreePedestal.IsActivated()) {
                    return true;
                }
                // can also exit on Scroll In on dummy target
                return false;
            case CameraState.Follow:
                if (Input.GetKey(UnityConstants.Key_Escape)) {
                    return true;
                }
                float mouseInputValue;
                if (dragFreePanTilt.IsActivated()) {
                    if (GameInput.IsHorizontalMouseMovement(out mouseInputValue)) {
                        return true;
                    }
                    if (GameInput.IsVerticalMouseMovement(out mouseInputValue)) {
                        return true;
                    }
                }
                if (scrollFreeZoom.IsActivated()) {
                    if (GameInput.IsScrollWheelMovement(out mouseInputValue)) {
                        return true;
                    }
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
        float magnitude = (newPosition - TempGameValues.UniverseOrigin).magnitude;
        if (magnitude > universeRadius) {
            //Debug.LogWarning("Camera proposed new position not valid at {0}, Distance to Origin is {1}.".Inject(_proposedPosition, magnitude));
            //float currentPositionMagnitude = (_transform.position - TempGameValues.UniverseOrigin).magnitude;
            //Debug.LogWarning("Current position is {0}, Distance to Origin is {1}.".Inject(_transform.position, currentPositionMagnitude));
            //float targetPositionMagnitude = (target.position - TempGameValues.UniverseOrigin).magnitude;
            //Debug.LogWarning("target position is {0}, Distance to Origin is {1}.".Inject(target.position, targetPositionMagnitude));
            //Debug.LogWarning("_distanceFromTarget is {0}.".Inject(_distanceFromTarget));
            return _transform.position;
        }
        return newPosition;
    }

    /// <summary>
    /// Manages the display of the cursor during certain movement actions.
    /// </summary>
    /// <arg item="_toLockCursor">if set to <c>true</c> [to lock cursor].</arg>
    private void ManageCursorDisplay(bool toLockCursor) {
        if (toLockCursor && !Screen.lockCursor) {
            Screen.lockCursor = true;
        }
        else if (Screen.lockCursor && !toLockCursor) {
            Screen.lockCursor = false;
        }
    }



    /// <summary>
    /// Attempts to assign an object found under the provided screenPoint as the new target. If more than one object is found,
    /// then the closest object implementing iFocus (typically Cellestial Bodies and Ships) becomes the target. If none of the objects
    /// found implements iFocus, then the farthest object implementing iCameraTarget is used. If the DummyTarget is found, or no 
    /// object at all is found, then the DummyTarget becomes the target under the screenPoint at universe edge.
    /// </summary>
    /// <param name="screenPoint">The screen point.</param>
    /// <returns>
    /// true if the target is changed, or if the dummyTarget has its location changed. false if the target remains the same (or if the dummyTarget, its location remains the same).
    /// </returns>
    private bool TrySetTargetAtScreenPoint(Vector3 screenPoint) {
        Ray ray = camera.ScreenPointToRay(screenPoint);
        RaycastHit[] hits = Physics.RaycastAll(ray, Mathf.Infinity, collideWithOnlyCameraTargetsLayerMask);
        if (!hits.IsNullOrEmpty<RaycastHit>()) {
            // one or more object under cursor encountered
            if (hits.Length == 1) {
                // the object encountered is either a game object or the dummy target
                Transform proposedTarget = hits[0].transform;
                if (proposedTarget != dummyTarget) {
                    return TryChangeTarget(proposedTarget);
                }

                // the proposedTarget must be the existing, stationary DummyTarget
                return false;
            }

            var iFocusHits = from h in hits where h.transform.GetInterface<IFocus>() != null select h;
            if (!iFocusHits.IsNullOrEmpty()) {
                var closestIFocusHit = iFocusHits.OrderBy(ifh => (ifh.transform.position - _transform.position).magnitude).First();
                return TryChangeTarget(closestIFocusHit.transform);
            }

            // no iFocus game objects under the cursor, so check for ICameraTarget now
            var iCameraHits = from h in hits where h.transform.GetInterface<ICameraTarget>() != null select h;
            if (!iCameraHits.IsNullOrEmpty()) {
                var furthestICameraHit = iCameraHits.OrderBy(ich => (ich.transform.position - _transform.position).magnitude).Last();
                return TryChangeTarget(furthestICameraHit.transform);
            }

            // no iFocus or iCameraTarget game objects under the cursor, yet there are at least 2 hits, so something is wrong
            Debug.LogError("Failed to find an acceptable collider to set as the camera target. Hits: ".Inject(hits));
            return true;
        }

        // no game object encountered under cursor so move the dummy to the edge of the universe and designate it as the target
        return TryPlaceDummyTargetAtUniverseEdgeInDirection(ray.direction);
    }



    /// <summary>
    /// Attempts to change the target to the proposedTarget. If the existing target is the same
    /// target, then the change is aborted and the method returns false.
    /// 
    /// </summary>
    /// <param name="proposedTarget">The proposed target. Logs an error if the DummyTarget.</param>
    /// <returns>true if the target was successfully changed, otherwise false.</returns>
    private bool TryChangeTarget(Transform proposedTarget) {
        if (proposedTarget == dummyTarget) {
            Debug.LogError("TryChangeTarget must not be used to change to the DummyTarget.");
            return false;
        }

        if (proposedTarget == target) {
            //Debug.Log("Proposed Target {0} is already the existing target.".Inject(target.name));
            return false;
        }
        ChangeTarget(proposedTarget);
        return true;
    }


    /// <summary>
    /// Attempts to place the dummy target at the edge of the universe located in the direction provided.
    /// </summary>
    /// <param name="direction">The direction.</param>
    /// <returns>true if the DummyTarget was placed in a new location. False if it was not moved since it was already there.</returns>
    private bool TryPlaceDummyTargetAtUniverseEdgeInDirection(Vector3 direction) {
        if (direction.magnitude == 0F) {
            Debug.LogError("Camera Direction Vector to place DummyTarget has no magnitude: " + direction);
            return false;
        }
        Ray ray = new Ray(_transform.position, direction.normalized);
        RaycastHit targetHit;
        if (Physics.Raycast(ray, out targetHit, Mathf.Infinity, collideWithDummyTargetOnlyLayerMask.value)) {
            if (dummyTarget != targetHit.transform) {
                Debug.LogError("Camera should find DummyTarget, but it is: " + targetHit.transform.name);
                return false;
            }

            float distanceToUniverseOrigin = (dummyTarget.position - TempGameValues.UniverseOrigin).magnitude;
            //Debug.Log("Dummy target distance to origin = {0}.".Inject(distanceToUniverseOrigin));
            if (!distanceToUniverseOrigin.CheckRange(universeRadius, allowedPercentageVariation: 0.1F)) {
                Debug.LogError("Camera's Dummy target is not located on UniverseEdge! Position = " + dummyTarget.position);
                return false;
            }
            // the dummy target is already there
            //Debug.Log("DummyTarget already present at " + dummyTarget.position + ". TargetHit at " + targetHit.transform.position);
            return false;
        }

        Vector3 pointOutsideUniverse = ray.GetPoint(universeRadius * 2);
        if (Physics.Raycast(pointOutsideUniverse, -ray.direction, out targetHit, Mathf.Infinity, collideWithUniverseEdgeOnlyLayerMask.value)) {
            Vector3 universeEdgePoint = targetHit.point;
            dummyTarget.position = universeEdgePoint;
            ChangeTarget(dummyTarget);
            //Debug.Log("New DummyTarget location = " + universeEdgePoint);
            _requestedDistanceFromTarget = _distanceFromTarget;
            return true;
        }

        Debug.LogError("Camera has not found a Universe Edge point! PointOutsideUniverse = " + pointOutsideUniverse + "ReturnDirection = " + -ray.direction);
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
        //Debug.Log("Rotation input values: x = {0}, y = {1}, z = {2}.".Inject(_xRotation, _yRotation, _zRotation));
        Quaternion desiredRotation = Quaternion.Euler(_yRotation, _xRotation, _zRotation);
        //Vector3 lookAtVector = desiredRotation.eulerAngles;
        //float xRotation = lookAtVector.y;
        //float yRotation = lookAtVector.x;
        //float zRotation = lookAtVector.z;
        //Debug.Log("After Quaternion conversion: x = {0}, y = {1}, z = {2}.".Inject(xRotation, yRotation, zRotation));
        Quaternion resultingRotation = Quaternion.Slerp(_transform.rotation, desiredRotation, dampenedTimeSinceLastUpdate);
        // OPTIMIZE Lerp is faster but not as pretty when the rotation changes are far apart
        return resultingRotation;
    }

    private void RemoveListeners() {
        eventMgr.RemoveListener<FocusSelectedEvent>(this, OnFocusSelected);
        eventMgr.RemoveListener<OptionChangeEvent>(this, OnOptionChange);
        eventMgr.RemoveListener<GameStateChangedEvent>(this, OnGameStateChange);
    }

    void OnDestroy() {
        Dispose();
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
            RemoveListeners();
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

    [Serializable]
    // Settings visible in the Inspector so they can be tweaked
    public class Settings {
        public float activeScreenEdge;
        public float minimumDistanceFromDummyTarget;
        // damping
        public float followTargetLookatDampener;
        public float followTargetPositionDampener;
        public float focusingLookatDampener;
        public float focusingPositionDampener;
        public float focusedRotationDampener;
        public float focusedPositionDampener;
        public float freeformRotationDampener;
        public float freeformPositionDampener;
        internal float MaxSpeedGoverner {
            get {
                return universeRadius / 20F;
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
        public MouseButton mouseButton;

        internal override float TranslationSpeedNormalizer {
            get { return 4.0F * universeRadius; }
            set { }
        }

        internal override bool IsActivated() {
            return base.IsActivated() && GameInput.IsMouseButtonDown(mouseButton) && !GameInput.IsAnyMouseButtonDownBesides(mouseButton);
        }
    }

    [Serializable]
    // Defines Camera Controls using 2 simultaneous Mouse Buttons
    public class SimultaneousMouseButtonConfiguration : ConfigurationBase {
        public MouseButton firstMouseButton;
        public MouseButton secondMouseButton;

        internal override float TranslationSpeedNormalizer {
            get { return 4.0F * universeRadius; }
            set { }
        }

        internal override bool IsActivated() {
            return base.IsActivated() && GameInput.IsMouseButtonDown(firstMouseButton) && GameInput.IsMouseButtonDown(secondMouseButton);
        }
    }

    [Serializable]
    // Defines Screen Edge Camera controls
    public class ScreenEdgeConfiguration : ConfigurationBase {

        internal override float TranslationSpeedNormalizer {
            get { return 0.02F * universeRadius; }
            set { }
        }

        internal override bool IsActivated() {
            return base.IsActivated() && !GameInput.IsAnyKeyOrMouseButtonDown();
        }
    }

    [Serializable]
    // Defines Mouse Scroll Wheel Camera Controls
    public class MouseScrollWheelConfiguration : ConfigurationBase {

        internal override float TranslationSpeedNormalizer {
            get { return 0.1F * universeRadius; }
            set { }
        }

        internal override bool IsActivated() {
            return base.IsActivated() && !GameInput.IsAnyKeyOrMouseButtonDown();
        }
    }

    [Serializable]
    // Defines the movement associated with the Arrow Keys on the Keyboard
    public class ArrowKeyboardConfiguration : ConfigurationBase {
        public KeyboardAxis keyboardAxis;
        internal override float TranslationSpeedNormalizer {
            get { return 0.0002F * universeRadius; }
            set { }
        }

        private bool IsAxisKeyInUse() {
            switch (keyboardAxis) {
                case KeyboardAxis.Horizontal:
                    return Input.GetKey(KeyCode.LeftArrow) || Input.GetKey(KeyCode.RightArrow);
                case KeyboardAxis.Vertical:
                    return Input.GetKey(KeyCode.UpArrow) || Input.GetKey(KeyCode.DownArrow);
                case KeyboardAxis.None:
                default:
                    throw new NotImplementedException(ErrorMessages.UnanticipatedSwitchValue.Inject(keyboardAxis));
            }
        }

        internal override bool IsActivated() {
            return base.IsActivated() && !GameInput.IsAnyMouseButtonDown() && IsAxisKeyInUse();
        }
    }

    [Serializable]
    [HideInInspector]
    public abstract class ConfigurationBase {
        public bool activate;
        public Modifiers modifiers = new Modifiers();
        public float sensitivity = 1.0F;
        //public float dampener = 4.0F;

        /// <summary>
        /// This factor is used to normalize the translation gameSpeed of different input mechanisms (keys, screen edge and mouse dragging)
        /// so that roughly the same distance is covered in a set period of time. The current implementation is a function of
        /// the size of the universe. At this time, this factor is not used to normalize rotation gameSpeed.
        /// </summary>
        internal abstract float TranslationSpeedNormalizer { get; set; }

        internal virtual bool IsActivated() {
            return activate && modifiers.confirmModifierKeyState();
        }
    }

    public override string ToString() {
        return new ObjectAnalyzer().ToString(this);
    }


}


