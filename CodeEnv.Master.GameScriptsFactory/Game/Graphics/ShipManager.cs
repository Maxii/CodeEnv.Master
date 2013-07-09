// --------------------------------------------------------------------------------------------------------------------
// <copyright>
// Copyright © 2012 - 2013 Strategic Forge
//
// Email: jim@strategicforge.com
// </copyright> 
// <summary> 
// File: ShipPropulsionManager.cs
// Manages a ship.
// </summary> 
// -------------------------------------------------------------------------------------------------------------------- 

#define DEBUG_LOG
#define DEBUG_LEVEL_WARN
#define DEBUG_LEVEL_ERROR

// default namespace

using System;
using CodeEnv.Master.Common;
using CodeEnv.Master.Common.LocalResources;
using CodeEnv.Master.Common.Unity;
using UnityEngine;

/// <summary>
/// Manages  a ship.
/// </summary>
public class ShipManager : AMonoBehaviourBase, ICameraFollowable, IZoomToClosest, IOnVisibleRelayTarget {

    public IntelLevel HumanPlayerIntelLevel { get; set; }
    public Navigator Navigator { get; private set; }
    public Visibility VisibilityState { get; set; }
    public ShipData Data { get; set; }

    public int maxAnimateDistance = TempGameValues.MaxShipAnimateDistance;
    public int maxShowDistance = TempGameValues.MaxShipShowDistance;

    private BoxCollider _collider;
    private GameEventManager _eventMgr;
    private Transform _transform;
    private Renderer _renderer;
    private GuiCursorHud _cursorHud;
    private GuiCursorHudText _guiCursorHudText;
    private GuiCursorHudTextFactory_Ship _factory;

    private Color _originalShipColor;
    private Color _hiddenShipColor;

    // start true so first ShowShip(false) will toggle Renderer off if out of show range
    private bool _isShipShowing = true;

    void Awake() {
        UnityUtility.ValidateComponentPresence<Collider>(gameObject);
        UnityUtility.ValidateComponentPresence<Rigidbody>(gameObject);
        _transform = transform;
        _collider = gameObject.GetComponent<BoxCollider>();
        _renderer = gameObject.GetComponentInChildren<Renderer>();
        _eventMgr = GameEventManager.Instance;
        _cursorHud = GuiCursorHud.Instance;
        VisibilityState = Visibility.Visible;
        UpdateRate = UpdateFrequency.Rare;

        __InitializeShipData();
        __InitializeNavigator();
    }

    void Start() {
        HumanPlayerIntelLevel = IntelLevel.LongRangeSensors;
        _originalShipColor = _renderer.material.color;
        _hiddenShipColor = new Color(_originalShipColor.r, _originalShipColor.g, _originalShipColor.b, Constants.ZeroF);
    }

    private void __InitializeShipData() {
        Data = Data ?? new ShipData(_transform);
        Data.Name = gameObject.name;
        Data.CombatStrength = new CombatStrength(1f, 2f, 3f, 4f, 5f, 6f);
        Data.DateHumanPlayerExplored = new GameDate(1, TempGameValues.StartingGameYear);
        Data.Health = 38F;
        Data.MaxHitPoints = 50F;
        Data.Owner = Players.Opponent_3;
        Data.MaxTurnRate = 1.0F;
        Data.MaxThrust = Data.Mass * Data.Drag * 2F;    // MaxThrust = MaxSpeed * Mass * Drag
    }

    private void __InitializeNavigator() {
        Navigator = new Navigator(_transform, Data);
    }

    void OnHover(bool isOver) {
        //Debug.Log("Ship.OnHover() called.");
        if (isOver && _isShipShowing) {
            UpdateSpeedData();
            DisplayCursorHUD();
        }
        else {
            ClearCursorHUD();
        }
    }

    private void UpdateSpeedData() {
        Data.UpdateSpeedReadout(Navigator.CurrentSpeed);
    }

    public void DisplayCursorHUD() {
        if (_factory == null) {
            _factory = new GuiCursorHudTextFactory_Ship(Data);
        }
        if (_guiCursorHudText != null && _guiCursorHudText.IntelLevel == HumanPlayerIntelLevel) {
            // TODO this only updates the ship's speed at this stage. Other values will also be dynamic and need updating
            UpdateGuiCursorHudText(GuiCursorHudDisplayLineKeys.Speed, GuiCursorHudDisplayLineKeys.Distance);
        }
        else {
            _guiCursorHudText = _factory.MakeInstance_GuiCursorHudText(HumanPlayerIntelLevel);
        }
        _cursorHud.Set(_guiCursorHudText);
    }

    private void UpdateGuiCursorHudText(params GuiCursorHudDisplayLineKeys[] keys) {
        if (HumanPlayerIntelLevel != _guiCursorHudText.IntelLevel) {
            Debug.LogError("{0} {1} and {2} must be the same.".Inject(typeof(IntelLevel), HumanPlayerIntelLevel.GetName(), _guiCursorHudText.IntelLevel.GetName()));
            return;
        }

        foreach (var key in keys) {
            IColoredTextList coloredTextList = _factory.MakeInstance_ColoredTextList(HumanPlayerIntelLevel, key);
            _guiCursorHudText.Replace(key, coloredTextList);
        }
    }

    public void ClearCursorHUD() {
        _cursorHud.Clear();
    }

    void Update() {
        if (ToUpdate()) {
            OptimizeDisplayPerformance();
            Navigator.TryProcessHeadingChange((int)UpdateRate);
        }
    }

    void FixedUpdate() {
        Navigator.ApplyThrust();
    }

    private void OptimizeDisplayPerformance() {
        bool toEnableAnimation = false;
        bool toShowShip = false;
        int distanceToCamera = _transform.DistanceToCameraInt();
        //Debug.Log("{0}, CameraDistance = {1}.".Inject(VisibilityState.GetName(), distanceToCamera));
        switch (VisibilityState) {
            case Visibility.Visible:
                if (distanceToCamera < maxShowDistance) {
                    toShowShip = true;
                    if (distanceToCamera < maxAnimateDistance) {
                        toEnableAnimation = true;
                    }
                }
                break;
            case Visibility.Invisible:
                // rendering and animation are automatically disabled
                break;
            case Visibility.None:
            default:
                throw new NotImplementedException(ErrorMessages.UnanticipatedSwitchValue.Inject(VisibilityState));
        }

        ShowShip(toShowShip);
        EnableAnimation(toEnableAnimation);
    }

    private void ShowShip(bool toShow) {
        //Debug.Log("ShowShip({0}) called.".Inject(toShow));
        if (_isShipShowing != toShow) {
            _isShipShowing = toShow;
            //ShowShipViaRenderer(toShow);
            ShowShipViaMaterialColor(toShow);
        }
    }

    /// <summary>
    /// Controls whether the ship can be seen by controlling the alpha value of the material color. 
    /// <remarks>This approach to hiding the ship works, but it requires use of a shader capable of transparency,
    /// which doesn't show up well with the current material.</remarks>
    /// </summary>
    /// <param name="toShow">if set to <c>true</c> material.alpha = 1.0, otherwise material.alpha = 0.0</param>
    private void ShowShipViaMaterialColor(bool toShow) {
        _renderer.material.color = toShow ? _originalShipColor : _hiddenShipColor;
        //Debug.Log("ShowShipViaMaterialColor({0}) called.".Inject(toShow));

    }

    /// <summary>
    /// Controls whether the ship can be seen via the activation state of the renderer gameobject. 
    /// <remarks>This approach to hiding the ship works, but we lose any benefit of knowing
    /// when the ship is invisible since, when deactivated, OnBecameVisible/Invisible is not called.</remarks>
    /// </summary>
    /// <param name="toShow">if set to <c>true</c> [to activate].</param>
    private void ShowShipViaRenderer(bool toShow) {
        _renderer.transform.gameObject.SetActive(toShow);
        //Debug.Log("ShowShipViaRenderer({0}) called.".Inject(toShow));
    }


    private void EnableAnimation(bool toEnable) {
        // TODO currently there is no animation
        //Debug.Log("Ship Animation is enabled = {0}".Inject(toEnable));
    }

    void OnDestroy() {
        Navigator.Dispose();
    }

    public override string ToString() {
        return new ObjectAnalyzer().ToString(this);
    }

    #region ICameraTargetable Members

    public bool IsTargetable {
        get { return _isShipShowing; }
    }

    [SerializeField]
    private float minimumCameraViewingDistanceMultiplier = 5.0F;

    private float _minimumCameraViewingDistance;
    public float MinimumCameraViewingDistance {
        get {
            if (_minimumCameraViewingDistance == Constants.ZeroF) {
                _minimumCameraViewingDistance = _collider.size.magnitude * minimumCameraViewingDistanceMultiplier;
            }
            return _minimumCameraViewingDistance;
        }
    }

    #endregion

    #region ICameraFocusable Members

    public void OnClick() {
        if (_isShipShowing && NguiGameInput.IsMiddleMouseButtonClick()) {
            _eventMgr.Raise<FocusSelectedEvent>(new FocusSelectedEvent(this, _transform));
        }
    }

    [SerializeField]
    private float optimalCameraViewingDistanceMultiplier = 20.0F;

    private float _optimalCameraViewingDistance;
    public float OptimalCameraViewingDistance {
        get {
            if (_optimalCameraViewingDistance == Constants.ZeroF) {
                _optimalCameraViewingDistance = _collider.size.magnitude * optimalCameraViewingDistanceMultiplier;
            }
            return _optimalCameraViewingDistance;
        }
    }

    #endregion

    #region ICameraFollowable Members

    [SerializeField]
    private float cameraFollowDistanceDampener = 2.0F;
    public float CameraFollowDistanceDampener {
        get { return cameraFollowDistanceDampener; }
    }

    [SerializeField]
    private float cameraFollowRotationDampener = 1.0F;
    public float CameraFollowRotationDampener {
        get { return cameraFollowRotationDampener; }
    }

    #endregion

    #region IOnVisible Members

    public void OnBecameVisible() {
        //Debug.Log("{0} has become visible.".Inject(gameObject.name));
        VisibilityState = Visibility.Visible;
        OptimizeDisplayPerformance();
    }

    public void OnBecameInvisible() {
        //Debug.Log("{0} has become invisible.".Inject(gameObject.name));
        VisibilityState = Visibility.Invisible;
        OptimizeDisplayPerformance();
    }

    #endregion
}

