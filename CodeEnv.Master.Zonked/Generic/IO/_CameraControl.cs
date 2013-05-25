// --------------------------------------------------------------------------------------------------------------------
// <copyright>
// Copyright © 2012 - 2013 Strategic Forge
//
// Email: jim@strategicforge.com
// </copyright> 
// <summary> 
// File: _CameraControl.cs
// In Game Camera Controller with Mouse, ScreenEdge and ArrowKey controls.
// </summary> 
// -------------------------------------------------------------------------------------------------------------------- 

//#define DEBUG_LEVEL_LOG
#define DEBUG_LEVEL_WARN
#define DEBUG_LEVEL_ERROR
#define DEBUG_LOG

// default namespace

using System;
using System.Linq;
using CodeEnv.Master.Common;
using CodeEnv.Master.Common.LocalResources;
using CodeEnv.Master.Common.Unity;
using UnityEngine;

[RequireComponent(typeof(Camera))]
/// <summary>
/// In Game Camera Controller with Mouse, ScreenEdge and ArrowKey controls.
/// 
/// NOTE: No reason that this class needs to be serializable as it has no state that I need to restore when loading a saved game.
/// The camera will always start back up looking at the initial planet of the player with no rotation in world space. The nested classes
/// are serializable so that their settings are isTargetVisibleThisFrame in the inspector. Otherwise, they also don't need to be serializable.
/// </summary>
[SerializeAll] //This is redundant as this Object already has a StoreInformation script on it. It causes duplication of referenced SIngletons when saving
public class _CameraControl : AMonoBehaviourBaseSingleton<_CameraControl>, IDisposable, IInstanceIdentity {

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
    public ArrowKeyboardConfiguration keyAllPan = new ArrowKeyboardConfiguration { keyboardAxis = KeyboardAxis.Horizontal, sensitivity = 0.5F, activate = true };
    public ArrowKeyboardConfiguration keyAllTilt = new ArrowKeyboardConfiguration { keyboardAxis = KeyboardAxis.Vertical, modifiers = new Modifiers { ctrlKeyReqd = true, shiftKeyReqd = true }, sensitivity = 0.5F, activate = true };
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
    public ArrowKeyboardConfiguration keyAllRoll = new ArrowKeyboardConfiguration { keyboardAxis = KeyboardAxis.Horizontal, modifiers = new Modifiers { ctrlKeyReqd = true, shiftKeyReqd = true }, activate = true };

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
    // How should zooming toward cursor combine with an object in focusTarget? Should the zoom add an offset creating a new defacto focusTarget point, ala Star Ruler?
    // Dragging the mouse with any button held down works offscreen OK, but upon release offscreen, immediately enables edge scrolling and panning
    // Implement Camera controls such as clip planes, FieldOfView, RenderSettings.[flareStrength, haloStrength, ambientLight]

    public bool IsResetOnFocusEnabled { get; private set; }
    // ScrollWheel always zooms IN on cursor, zooming OUT with the ScrollWheel is directly backwards by default
    public bool IsScrollZoomOutOnCursorEnabled { get; private set; }

    private bool isRollEnabled;
    public bool IsRollEnabled {
        get { return isRollEnabled; }
        private set { isRollEnabled = value; dragFocusRoll.activate = value; dragFreeRoll.activate = value; keyAllRoll.activate = value; }
    }

    public Settings settings = new Settings { minimumDistanceFromTarget = 3.0F, optimalDistanceFromTarget = 5.0F, activeScreenEdge = 10F };

    // static so it is available to nested classes
    public static float universeRadius;

    // AValues held outside LateUpdate() so they retain the last mouseInputValue that was set 
    // when the movement instruction that was setting it is no longer being called
    public float positionSmoothingDampener = 4.0F;
    public float rotationSmoothingDampener = 4.0F;

    // Cached references
    [DoNotSerialize]    // Serializing this creates duplicates of this object on Save
    private GameEventManager eventMgr;
    [DoNotSerialize]    // Serializing this creates duplicates of this object on Save
    private PlayerPrefsManager playerPrefsMgr;

    private Transform target;
    private Transform dummyTarget;
    private Transform cameraTransform;
    private Transform focusTarget;   // can be null

    private string[] keyboardAxesNames = new string[] { UnityConstants.KeyboardAxisName_Horizontal, UnityConstants.KeyboardAxisName_Vertical };
    private LayerMask collideWithUniverseEdgeOnlyLayerMask = LayerMaskExtensions.CreateInclusiveMask(Layers.UniverseEdge);
    private LayerMask collideWithDummyTargetOnlyLayerMask = LayerMaskExtensions.CreateInclusiveMask(Layers.DummyTarget);

    // Calculated Positional fields    
    private float _requestedDistanceFromTarget;
    private float _distanceFromTarget;

    // Continuously calculated, accurate EulerAngles
    private float _xRotation;
    private float _yRotation;
    private float _zRotation;

    // State
    private enum CameraState { None = 0, Focused = 1, Freeform = 2 }
    private CameraState cameraState;

    public enum CameraUpdateMode { LateUpdate = 0, FixedUpdate = 1, Update = 2 }
    public CameraUpdateMode updateMode = CameraUpdateMode.LateUpdate;

    /// <summary>
    /// The 1st method called when the script instance is being loaded. Called once and only once in the lifetime of the script
    /// instance. All game objects have already been initialized so references to other scripts may be established here.
    /// </summary>
    void Awake() {
        Debug.Log("_CameraControl.Awake() called.");
        InitializeReferences();
    }

    private void InitializeReferences() {
        IncrementInstanceCounter();
        //if (LevelSerializer.IsDeserializing) { return; }
        eventMgr = GameEventManager.Instance;
        playerPrefsMgr = PlayerPrefsManager.Instance;
        cameraTransform = transform;    // cache it! t is actually GetComponent<Transform>()
        AddListeners();
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
    /// <summary>
    /// Called once and only once after all objects have been awoken. Start will not be 
    /// called if the script is not enabled. All scripts have been awoken so it is OK to start talking to them.
    /// </summary>
    void Start() {
        Debug.Log("_CameraControl.Start() called.");
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

        // This camera will see all layers except for the GUI layer. If I want to add exclusions, I can still do it from the outside
        camera.cullingMask = LayerMaskExtensions.CreateExclusiveMask(Layers.Gui);
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

        // UNDONE whether starting or continuing saved game, camera _location should be focused on the player's starting planet, no rotation
        ResetToWorldspace();

        focusTarget = null;
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
        PlaceDummyTargetAtUniverseEdgeInDirection(cameraTransform.forward);
        dummyTarget.parent = DynamicObjects.Folder;
        dummyTarget.collider.enabled = true;
    }

    void OnDeserialized() {
        Debug.Log("Camera.OnDeserialized() called.");
        //ConnectDummyTargetOnDeserialized();
    }

    [Obsolete]
    private void ConnectDummyTargetOnDeserialized() {
        SphereCollider[] sphereColliders = DynamicObjects.Folder.GetComponentsInChildren<SphereCollider>();
        if (sphereColliders.Length != 0) {
            var dt = from c in sphereColliders where c.gameObject.layer == (int)Layers.DummyTarget select c;
            if (!dt.IsNullOrEmpty<SphereCollider>()) {
                Debug.Log("Found DummyTarget under DynamicObjects!");
                dummyTarget = dt.Single<SphereCollider>().transform;
                target = dummyTarget;
            }
        }
    }

    private void OnOptionChange(OptionChangeEvent e) {
        OptionSettings settings = e.Settings;
        IsResetOnFocusEnabled = settings.IsResetOnFocusEnabled;
        IsRollEnabled = settings.IsCameraRollEnabled;
        IsScrollZoomOutOnCursorEnabled = settings.IsZoomOutOnCursorEnabled;
    }

    private void OnFocusSelected(FocusSelectedEvent e) {
        //Debug.Log("FocusSelectedEvent received by Camera.");
        SetFocus(e.FocusTransform);
    }

    /// <summary>
    /// Sets the focusTarget for the camera.
    /// </summary>
    /// <arg item="focusTarget">The t of the GO to focusTarget on.</arg>
    private void SetFocus(Transform focus) {
        focusTarget = focus;
        ChangeState(CameraState.Focused);
    }

    /// <summary>
    /// Changes the CameraState.
    /// </summary>
    /// <arg item="newState">The new state.</arg>
    /// <exception cref="System.NotImplementedException"></exception>
    private void ChangeState(CameraState newState) {
        Arguments.ValidateNotNull(target);

        cameraState = newState;
        switch (newState) {
            case CameraState.Focused:
                Arguments.ValidateNotNull(focusTarget);

                target = focusTarget;

                _distanceFromTarget = Vector3.Distance(target.position, cameraTransform.position);
                _requestedDistanceFromTarget = settings.optimalDistanceFromTarget;
                // face the selected focusTarget
                _xRotation = Vector3.Angle(cameraTransform.right, target.right);
                _yRotation = Vector3.Angle(cameraTransform.up, target.up);
                _zRotation = Vector3.Angle(cameraTransform.forward, target.forward);

                if (IsResetOnFocusEnabled) {
                    ResetToWorldspace();
                }
                break;
            case CameraState.Freeform:
                focusTarget = null;
                _distanceFromTarget = Vector3.Distance(target.position, cameraTransform.position);
                _requestedDistanceFromTarget = _distanceFromTarget;
                // no facing change
                break;
            case CameraState.None:
            default:
                throw new NotImplementedException(ErrorMessages.UnanticipatedSwitchValue.Inject(newState));
        }
        //Debug.Log("CameraState changed to " + cameraState);
    }

    /// <summary>
    /// Resets the camera rotation to that of worldspace, no rotation.
    /// </summary>
    public void ResetToWorldspace() {
        // current and requested distance to followTarget already set
        cameraTransform.rotation = Quaternion.identity;
        _xRotation = Vector3.Angle(Vector3.right, cameraTransform.right); // same as 0.0F
        _yRotation = Vector3.Angle(Vector3.up, cameraTransform.up);   // same as 0.0F
        _zRotation = Vector3.Angle(Vector3.forward, cameraTransform.forward); // same as 0.0F
        //Debug.Log("ResetToWorldSpace called. Worldspace Camera Rotation = " + _transform.rotation);
        //Debug.Log("FollowTarget location = " + followTarget._location);
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
            bool toLockCursor = false;
            float mouseInputValue = 0F;
            Vector3 targetDirection = (target.position - cameraTransform.position).normalized;

            switch (cameraState) {
                case CameraState.Focused:
                    if (dragFreeTruck.IsActivated() || dragFreePedestal.IsActivated()) {
                        ChangeState(CameraState.Freeform);
                        return;
                    }
                    if (dragFocusOrbit.IsActivated()) {
                        toLockCursor = true;
                        if (GameInput.IsHorizontalMouseMovement(out mouseInputValue)) {
                            _xRotation += mouseInputValue * dragFocusOrbit.sensitivity * timeSinceLastUpdate;
                        }
                        if (GameInput.IsVerticalMouseMovement(out mouseInputValue)) {
                            _yRotation -= mouseInputValue * dragFocusOrbit.sensitivity * timeSinceLastUpdate;
                        }
                        //_cameraRotationDampener = dragFocusOrbit.dampener;
                    }
                    if (edgeFocusOrbit.IsActivated()) {
                        float xMousePosition = Input.mousePosition.x;
                        if (xMousePosition <= settings.activeScreenEdge) {
                            _xRotation -= edgeFocusOrbit.sensitivity * timeSinceLastUpdate;
                            //_cameraRotationDampener = edgeFocusOrbit.dampener;
                        }
                        else if (xMousePosition >= Screen.width - settings.activeScreenEdge) {
                            _xRotation += edgeFocusOrbit.sensitivity * timeSinceLastUpdate;
                            //_cameraRotationDampener = edgeFocusOrbit.dampener;
                        }
                    }
                    if (dragFocusRoll.IsActivated()) {
                        toLockCursor = true;
                        if (GameInput.IsHorizontalMouseMovement(out mouseInputValue)) {
                            _zRotation -= mouseInputValue * dragFocusRoll.sensitivity * timeSinceLastUpdate;
                        }
                        //_cameraRotationDampener = dragFocusRoll.dampener;
                    }
                    if (scrollFocusZoom.IsActivated()) {
                        if (GameInput.IsScrollWheelMovement(out mouseInputValue)) {
                            if (mouseInputValue > 0 || (mouseInputValue < 0 && IsScrollZoomOutOnCursorEnabled)) {
                                // Scroll ZoomIN Command or ZoomOUT with ZoomOutOnCursorEnabled
                                TrySetNewTargetAtCursor();
                                if (target != focusTarget) {
                                    // new followTarget was selected so change state and startover to reset tValues
                                    ChangeState(CameraState.Freeform);
                                    return;
                                }
                            }
                            float positionAccelerationFactor = Mathf.Clamp(Mathf.Abs(_requestedDistanceFromTarget), 0F, settings.MaxSpeedGoverner);
                            _requestedDistanceFromTarget -= mouseInputValue * positionAccelerationFactor * scrollFocusZoom.sensitivity * scrollFocusZoom.TranslationSpeedNormalizer * timeSinceLastUpdate;
                            //_cameraPositionDampener = scrollFocusZoom.dampener;
                            //Debug.Log("MaxSpeedGovernor = {0}, mouseInputValue = {1}".Inject(settings.MaxSpeedGovernor, mouseInputValue));
                            //Debug.Log("positionAccelerationFactor = {0}, _requestedDistanceFromTarget = {1}".Inject(positionAccelerationFactor, _requestedDistanceFromTarget));
                        }
                    }
                    if (edgeFocusZoom.IsActivated()) {
                        float yMousePosition = Input.mousePosition.y;
                        if (yMousePosition <= settings.activeScreenEdge) {
                            float positionAccelerationFactor = Mathf.Clamp(Mathf.Abs(_requestedDistanceFromTarget), 0F, settings.MaxSpeedGoverner);
                            _requestedDistanceFromTarget += positionAccelerationFactor * edgeFocusZoom.sensitivity * edgeFocusZoom.TranslationSpeedNormalizer * timeSinceLastUpdate;
                            //_cameraPositionDampener = edgeFocusZoom.dampener;
                        }
                        else if (yMousePosition >= Screen.height - settings.activeScreenEdge) {
                            float positionAccelerationFactor = Mathf.Clamp(Mathf.Abs(_requestedDistanceFromTarget), 0F, settings.MaxSpeedGoverner);
                            _requestedDistanceFromTarget -= positionAccelerationFactor * edgeFocusZoom.sensitivity * edgeFocusZoom.TranslationSpeedNormalizer * timeSinceLastUpdate;
                            //_cameraPositionDampener = edgeFocusZoom.dampener;
                        }
                    }
                    if (keyFocusZoom.IsActivated()) {
                        float positionAccelerationFactor = Mathf.Clamp(Mathf.Abs(_requestedDistanceFromTarget), 0F, settings.MaxSpeedGoverner);
                        _requestedDistanceFromTarget -= Input.GetAxis(keyboardAxesNames[(int)keyFocusZoom.keyboardAxis]) * positionAccelerationFactor * keyFocusZoom.TranslationSpeedNormalizer * keyFocusZoom.sensitivity;
                        //_cameraPositionDampener = keyFocusZoom.dampener;
                    }
                    if (dragFocusZoom.IsActivated()) {
                        toLockCursor = true;
                        if (GameInput.IsVerticalMouseMovement(out mouseInputValue)) {
                            //Debug.Log("MouseFocusZoom Vertical Mouse Movement detected.");
                            float positionAccelerationFactor = Mathf.Clamp(Mathf.Abs(_requestedDistanceFromTarget), 0F, settings.MaxSpeedGoverner);
                            _requestedDistanceFromTarget -= mouseInputValue * positionAccelerationFactor * dragFocusZoom.sensitivity * dragFocusZoom.TranslationSpeedNormalizer * timeSinceLastUpdate;
                        }
                        //_cameraPositionDampener = dragFocusZoom.dampener;
                    }

                    // t.forward is the camera's current definition of 'forward', ie. WorldSpace's absolute forward adjusted by the camera's rotation (Vector.forward * cameraRotation )   
                    // this is the key that keeps the camera pointed at the followTarget when focused
                    if (targetDirection != cameraTransform.forward) {
                        targetDirection = Vector3.Lerp(targetDirection, cameraTransform.forward, rotationSmoothingDampener * timeSinceLastUpdate);
                    }

                    break;

                case CameraState.Freeform:
                    if (edgeFreePan.IsActivated()) {
                        float xMousePosition = Input.mousePosition.x;
                        if (xMousePosition <= settings.activeScreenEdge) {
                            _xRotation -= edgeFreePan.sensitivity * timeSinceLastUpdate;
                            //_cameraRotationDampener = edgeFreePan.dampener;
                        }
                        else if (xMousePosition >= Screen.width - settings.activeScreenEdge) {
                            _xRotation += edgeFreePan.sensitivity * timeSinceLastUpdate;
                            //_cameraRotationDampener = edgeFreePan.dampener;
                        }
                    }
                    if (dragFreeTruck.IsActivated()) {
                        toLockCursor = true;
                        if (GameInput.IsHorizontalMouseMovement(out mouseInputValue)) {
                            PlaceDummyTargetAtUniverseEdgeInDirection(cameraTransform.right);
                            _requestedDistanceFromTarget -= mouseInputValue * dragFreeTruck.sensitivity * dragFreeTruck.TranslationSpeedNormalizer * timeSinceLastUpdate;
                        }
                        //_cameraPositionDampener = dragFreeTruck.dampener;
                    }
                    if (dragFreePedestal.IsActivated()) {
                        toLockCursor = true;
                        if (GameInput.IsVerticalMouseMovement(out mouseInputValue)) {
                            PlaceDummyTargetAtUniverseEdgeInDirection(cameraTransform.up);
                            _requestedDistanceFromTarget -= mouseInputValue * dragFreePedestal.sensitivity * dragFreePedestal.TranslationSpeedNormalizer * timeSinceLastUpdate;
                        }
                        //_cameraPositionDampener = dragFreePedestal.dampener;
                    }

                    if (dragFreeRoll.IsActivated()) {
                        toLockCursor = true;
                        if (GameInput.IsHorizontalMouseMovement(out mouseInputValue)) {
                            _zRotation -= mouseInputValue * dragFreeRoll.sensitivity * timeSinceLastUpdate;
                        }
                        //_cameraRotationDampener = dragFreeRoll.dampener;
                    }
                    if (dragFreePanTilt.IsActivated()) {
                        toLockCursor = true;
                        if (GameInput.IsHorizontalMouseMovement(out mouseInputValue)) {
                            _xRotation += mouseInputValue * dragFreePanTilt.sensitivity * timeSinceLastUpdate;
                        }
                        if (GameInput.IsVerticalMouseMovement(out mouseInputValue)) {
                            _yRotation -= mouseInputValue * dragFreePanTilt.sensitivity * timeSinceLastUpdate;
                        }
                        //_cameraRotationDampener = dragFreePanTilt.dampener;
                    }
                    if (scrollFreeZoom.IsActivated()) {
                        if (GameInput.IsScrollWheelMovement(out mouseInputValue)) {
                            if (mouseInputValue > 0) {
                                // Scroll ZoomIN command
                                TrySetNewTargetAtCursor();
                            }
                            if (mouseInputValue < 0) {
                                // Scroll ZoomOUT command
                                if (IsScrollZoomOutOnCursorEnabled) {
                                    TrySetNewTargetAtCursor();
                                }
                                else {
                                    PlaceDummyTargetAtUniverseEdgeInDirection(cameraTransform.forward);
                                }
                            }
                            float positionAccelerationFactor = Mathf.Clamp(Mathf.Abs(_requestedDistanceFromTarget), 0F, settings.MaxSpeedGoverner);
                            _requestedDistanceFromTarget -= mouseInputValue * positionAccelerationFactor * scrollFreeZoom.sensitivity * scrollFreeZoom.TranslationSpeedNormalizer * timeSinceLastUpdate;
                            //Debug.Log("ScrollFreeZoom RequestedDistanceFromTarget = " + _requestedDistanceFromTarget);
                            //_cameraPositionDampener = scrollFreeZoom.dampener;
                        }
                    }
                    if (edgeFreeZoom.IsActivated()) {
                        float yMousePosition = Input.mousePosition.y;
                        if (yMousePosition <= settings.activeScreenEdge) {
                            PlaceDummyTargetAtUniverseEdgeInDirection(-cameraTransform.forward);
                            _requestedDistanceFromTarget -= edgeFreeZoom.sensitivity * edgeFreeZoom.TranslationSpeedNormalizer * timeSinceLastUpdate;
                            //Debug.Log("EdgeFreeZoom _requestedDistanceFromTarget = " + _requestedDistanceFromTarget);
                            //_cameraPositionDampener = edgeFreeZoom.dampener;
                        }
                        else if (yMousePosition >= Screen.height - settings.activeScreenEdge) {
                            PlaceDummyTargetAtUniverseEdgeInDirection(cameraTransform.forward);
                            _requestedDistanceFromTarget -= edgeFreeZoom.sensitivity * edgeFreeZoom.TranslationSpeedNormalizer * timeSinceLastUpdate;
                            //_cameraPositionDampener = edgeFreeZoom.dampener;
                        }
                    }
                    if (dragFreeZoom.IsActivated()) {
                        toLockCursor = true;
                        if (GameInput.IsVerticalMouseMovement(out mouseInputValue)) {
                            PlaceDummyTargetAtUniverseEdgeInDirection(cameraTransform.forward);
                            _requestedDistanceFromTarget -= mouseInputValue * dragFreeZoom.sensitivity * dragFreeZoom.TranslationSpeedNormalizer * timeSinceLastUpdate;
                        }
                        //_cameraPositionDampener = dragFreeZoom.dampener;
                    }

                    // Freeform Arrow Keyboard Configurations. Mouse Buttons supercede Arrow Keys. Only Arrow Keys are used as IsActivated() must be governed by 
                    //whether the appropriate key is down to keep the configurations from interfering with each other. 
                    if (keyFreeZoom.IsActivated()) {
                        PlaceDummyTargetAtUniverseEdgeInDirection(cameraTransform.forward);
                        _requestedDistanceFromTarget -= Input.GetAxis(keyboardAxesNames[(int)keyFreeZoom.keyboardAxis]) * keyFreeZoom.sensitivity * keyFreeZoom.TranslationSpeedNormalizer;
                        //_cameraPositionDampener = keyFreeZoom.dampener;
                    }
                    if (keyFreeTruck.IsActivated()) {
                        PlaceDummyTargetAtUniverseEdgeInDirection(cameraTransform.right);
                        _requestedDistanceFromTarget -= Input.GetAxis(keyboardAxesNames[(int)keyFreeTruck.keyboardAxis]) * keyFreeTruck.sensitivity * keyFreeTruck.TranslationSpeedNormalizer;
                        //_cameraPositionDampener = keyFreeTruck.dampener;
                    }
                    if (keyFreePedestal.IsActivated()) {
                        PlaceDummyTargetAtUniverseEdgeInDirection(cameraTransform.up);
                        _requestedDistanceFromTarget -= Input.GetAxis(keyboardAxesNames[(int)keyFreePedestal.keyboardAxis]) * keyFreePedestal.sensitivity * keyFreePedestal.TranslationSpeedNormalizer;
                        //_cameraPositionDampener = keyFreePedestal.dampener;
                    }

                    targetDirection = (target.position - cameraTransform.position).normalized;  // needed when another followTarget is picked when in scrollFreeZoom
                    //Debug.Log("FollowTarget Direction is: " + _targetDirection);
                    break;
                case CameraState.None:
                default:
                    throw new NotImplementedException(ErrorMessages.UnanticipatedSwitchValue.Inject(cameraState));
            }
            // These Arrow Key configurations apply to both freeform and focused states, and at this stage, don't need seperate sensitivity tValues
            if (keyAllPan.IsActivated()) {
                _xRotation += Input.GetAxis(keyboardAxesNames[(int)keyAllPan.keyboardAxis]) * keyAllPan.sensitivity;
                //_cameraRotationDampener = keyFreePan.dampener;
            }
            if (keyAllTilt.IsActivated()) {
                _yRotation -= Input.GetAxis(keyboardAxesNames[(int)keyAllTilt.keyboardAxis]) * keyAllTilt.sensitivity;
                //_cameraRotationDampener = keyFreeTilt.dampener;
            }
            if (keyAllRoll.IsActivated()) {
                _zRotation -= Input.GetAxis(keyboardAxesNames[(int)keyAllRoll.keyboardAxis]) * keyAllRoll.sensitivity;
                //_cameraRotationDampener = keyFreeRoll.dampener;
            }

            cameraTransform.rotation = CalculateCameraRotation(_xRotation, _yRotation, _zRotation, rotationSmoothingDampener * timeSinceLastUpdate);

            _requestedDistanceFromTarget = Mathf.Clamp(_requestedDistanceFromTarget, settings.minimumDistanceFromTarget, Mathf.Infinity);
            _distanceFromTarget = Mathfx.Lerp(_distanceFromTarget, _requestedDistanceFromTarget, positionSmoothingDampener * timeSinceLastUpdate);

            //_distanceFromTarget = Mathf.Lerp(_distanceFromTarget, _requestedDistanceFromTarget, _cameraPositionDampener * timeSinceLastUpdate);
            //Debug.Log("Actual DistanceFromTarget = " + _distanceFromTarget);
            Vector3 proposedPosition = target.position - (targetDirection * _distanceFromTarget);
            //Debug.Log("Resulting Camera location = " + _transform._location);

            cameraTransform.position = ValidatePosition(proposedPosition);

            ManageCursorDisplay(toLockCursor);
        }
    }

    /// <summary>
    /// Validates the proposed new _location of the camera to be within the universe.
    /// </summary>
    /// <arg item="newPosition">The new _location.</arg>
    /// <returns>if validated, returns newPosition. If not, return the current _location.</returns>
    private Vector3 ValidatePosition(Vector3 newPosition) {
        if ((newPosition - TempGameValues.UniverseOrigin).magnitude >= universeRadius) {
            Debug.LogError("Camera proposed new _location not valid at " + newPosition);
            return cameraTransform.position;
        }
        return newPosition;
    }

    /// <summary>
    /// Manages the display of the cursor during certain movement actions.
    /// </summary>
    /// <arg item="_toLockCursor">if set to <c>true</c> [to lock cursor].</arg>
    private void ManageCursorDisplay(bool toLockCursor) {
        if (Input.GetKeyDown(UnityConstants.Key_Escape)) {
            Screen.lockCursor = false;
        }

        if (toLockCursor && !Screen.lockCursor) {
            Screen.lockCursor = true;
        }
        else if (Screen.lockCursor && !toLockCursor) {
            Screen.lockCursor = false;
        }
    }

    /// <summary>
    /// Attempts to assign any object found under the cursor as the new followTarget. If the existing followTarget is the object encountered, 
    /// then no change is made. If no object is encountered, the DummyTarget is moved to the edge of the universe along the line
    /// to the cursor and assigned as the new followTarget.
    /// </summary>
    private void TrySetNewTargetAtCursor() {
        Ray ray = camera.ScreenPointToRay(Input.mousePosition);
        // this LayerMask is not really needed as no collision is possible with inside of UniverseEdge sphere
        LayerMask collideWithAllExceptUniverseEdgeLayerMask = collideWithUniverseEdgeOnlyLayerMask.Inverse();
        RaycastHit targetHit;
        if (Physics.Raycast(ray, out targetHit, Mathf.Infinity, collideWithAllExceptUniverseEdgeLayerMask.value)) {
            if (targetHit.transform == target) {
                // the followTarget under the cursor is the current followTarget object so do nothing
                //Debug.Log("Existing FollowTarget under cursor found. Name = " + followTarget.name);
                return;
            }
            // else I've got a new followTarget object
            target = targetHit.transform;
            //Debug.Log("New non-DummyTarget acquired. Name = " + followTarget.name);
            _requestedDistanceFromTarget = Vector3.Distance(target.position, cameraTransform.position);
            _distanceFromTarget = _requestedDistanceFromTarget;
        }
        else {
            // no followTarget under cursor so move the dummy to the edge of the universe
            PlaceDummyTargetAtUniverseEdgeInDirection(ray.direction);
        }
    }

    /// <summary>
    /// Places the dummy followTarget at the edge of the universe in the direction provided.
    /// </summary>
    /// <arg item="direction">The direction.</arg>
    private void PlaceDummyTargetAtUniverseEdgeInDirection(Vector3 direction) {
        if (direction.magnitude == 0F) {
            Debug.LogWarning("Camera Direction Vector to place DummyTarget has no magnitude: " + direction);
            return;
        }
        Ray ray = new Ray(cameraTransform.position, direction.normalized);
        RaycastHit targetHit;
        if (Physics.Raycast(ray, out targetHit, Mathf.Infinity, collideWithDummyTargetOnlyLayerMask.value)) {
            if (dummyTarget != targetHit.transform) {
                Debug.LogError("Camera should find DummyTarget, but it is: " + targetHit.transform.name);
                return;
            }
            else {
                float distanceToUniverseOrigin = (dummyTarget.position - TempGameValues.UniverseOrigin).magnitude;
                if (!distanceToUniverseOrigin.CheckRange(universeRadius, allowedPercentageVariation: 1.0F)) {
                    Debug.LogError("Camera's Dummy FollowTarget is not located on UniverseEdge! location = " + dummyTarget.position);
                    return;
                }
                // the dummy followTarget is already there
                //Debug.Log("DummyTarget already present at " + dummyTarget._location + ". TargetHit at " + targetHit.t._location);
                return;
            }
        }
        Vector3 pointOutsideUniverse = ray.GetPoint(universeRadius * 2);
        if (Physics.Raycast(pointOutsideUniverse, -ray.direction, out targetHit, Mathf.Infinity, collideWithUniverseEdgeOnlyLayerMask.value)) {
            Vector3 universeEdgePoint = targetHit.point;
            dummyTarget.position = universeEdgePoint;
            target = dummyTarget;
            //Debug.Log("New DummyTarget location = " + universeEdgePoint);
            _requestedDistanceFromTarget = Vector3.Distance(target.position, cameraTransform.position);
            _distanceFromTarget = _requestedDistanceFromTarget;
        }
        else {
            Debug.LogError("Camera has not found a Universe Edge point! PointOutsideUniverse = " + pointOutsideUniverse + "ReturnDirection = " + -ray.direction);
        }
    }

    /// <summary>
    /// Calculates a new rotation derived from the current rotation and the provided EulerAngle arguments.
    /// </summary>
    /// <arg item="xDeg">The x deg.</arg>
    /// <arg item="yDeg">The y deg.</arg>
    /// <arg item="zDeg">The z deg.</arg>
    /// <arg item="adjustedTime">The  elapsed time to use with the Slerp function. Can be adjusted for effect.</arg>
    /// <returns></returns>
    private Quaternion CalculateCameraRotation(float xDeg, float yDeg, float zDeg, float adjustedTime) {
        // keep rotation tValues exact as a substitute for the unreliable accuracy that comes from reading EulerAngles from the Quaternion
        _xRotation = xDeg % 360;
        _yRotation = yDeg % 360; //        ClampAngle(yDeg % 360, -80, 80);
        _zRotation = zDeg % 360;
        Quaternion desiredRotation = Quaternion.Euler(_yRotation, _xRotation, _zRotation);

        return Quaternion.Slerp(cameraTransform.rotation, desiredRotation, adjustedTime);
        // OPTIMIZE Lerp is faster but not as pretty when the rotation changes are far apart
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
    // Settings isTargetVisibleThisFrame in the Inspector so they can be tweaked
    public class Settings {
        public float minimumDistanceFromTarget;
        public float optimalDistanceFromTarget;
        public float activeScreenEdge;
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

