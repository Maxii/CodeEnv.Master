// --------------------------------------------------------------------------------------------------------------------
// <copyright>
// Copyright © 2012 - 2013 Strategic Forge
//
// Email: jim@strategicforge.com
// </copyright> 
// <summary> 
// File: ShipManager.cs
// Manages a ship.
// </summary> 
// -------------------------------------------------------------------------------------------------------------------- 

// default namespace

using System;
using CodeEnv.Master.Common;
using CodeEnv.Master.Common.LocalResources;
using CodeEnv.Master.Common.Unity;
using UnityEngine;

/// <summary>
/// Manages  a ship.
/// </summary>
public class ShipManager : AGameObjectManager<ShipData>, ICameraFollowable, IZoomToClosest {

    public override float CurrentSpeed { get { return Data.CurrentSpeed; } }

    public Navigator Navigator { get; private set; }

    public int maxAnimateDistance;  // must be initialized in Awake or Start as it calls UnityEngine.Application.dataPath
    public int maxShowDistance;    // must be initialized in Awake or Start as it calls UnityEngine.Application.dataPath

    private BoxCollider _collider;
    private Renderer _renderer;
    private GameEventManager _eventMgr;

    public Color _originalShipColor;
    public Color _hiddenShipColor;

    // start true so first ShowShip(false) will toggle Renderer off if out of show range
    private bool _isShipShowing = true;

    protected override void InitializeOnAwake() {
        base.InitializeOnAwake();
        UnityUtility.ValidateComponentPresence<Collider>(gameObject);
        UnityUtility.ValidateComponentPresence<Rigidbody>(gameObject);
        _collider = gameObject.GetComponent<BoxCollider>();
        _renderer = gameObject.GetComponentInChildren<Renderer>();
        _eventMgr = GameEventManager.Instance;
        UpdateRate = UpdateFrequency.Infrequent;
        maxAnimateDistance = AnimationSettings.Instance.MaxShipAnimateDistance;
        maxShowDistance = AnimationSettings.Instance.MaxShipShowDistance;

        __InitializeData();
        __InitializeNavigator();
    }

    protected override void InitializeOnStart() {
        base.InitializeOnStart();
        _originalShipColor = _renderer.material.color;
        _hiddenShipColor = new Color(_originalShipColor.r, _originalShipColor.g, _originalShipColor.b, Constants.ZeroF);
    }

    protected override IntelLevel __InitializeIntelLevel() {
        return IntelLevel.ShortRangeSensors;
    }

    protected override void __InitializeData() {
        Data = new ShipData(_transform);
        Data.Name = gameObject.name;
        // Ship's PieceName gets set when it gets attached to a fleet
        Data.Hull = ShipHull.Destroyer;
        Data.Strength = new CombatStrength(1f, 2f, 3f, 4f, 5f, 6f);
        Data.LastHumanPlayerIntelDate = new GameDate();
        Data.Health = 38F;
        Data.MaxHitPoints = 50F;
        Data.Owner = GameManager.Instance.HumanPlayer;
        Data.MaxTurnRate = 1.0F;
        Data.RequestedHeading = _transform.forward;
        Data.MaxThrust = Data.Mass * Data.Drag * 2F;    // MaxThrust = MaxSpeed * Mass * Drag
    }

    private void __InitializeNavigator() {
        Navigator = new Navigator(_transform, Data);
    }

    void OnHover(bool isOver) {
        Logger.Log("Ship.OnHover() called.");
        if (isOver && _isShipShowing) {
            DisplayCursorHUD();
        }
        else {
            ClearCursorHUD();
        }
    }

    public void ChangeHeading(Vector3 newHeading) {
        if (Data.RequestedHeading != newHeading) {
            Navigator.ChangeHeading(newHeading);
        }
    }

    public void ChangeSpeed(float newSpeed) {
        if (Data.RequestedSpeed != newSpeed) {
            Navigator.ChangeSpeed(newSpeed);
        }
    }

    protected override GuiHudLineKeys[] OptionalCursorHudLinesToUpdate() {
        return new GuiHudLineKeys[1] {
            GuiHudLineKeys.Speed
        };
    }

    protected override void UpdateGuiCursorHudText(params GuiHudLineKeys[] keys) {
        if (HumanPlayerIntelLevel != _guiCursorHudText.IntelLevel) {
            D.Error("{0} {1} and {2} must be the same.".Inject(typeof(IntelLevel), HumanPlayerIntelLevel.GetName(), _guiCursorHudText.IntelLevel.GetName()));
            return;
        }
        IColoredTextList coloredTextList;
        foreach (var key in keys) {
            coloredTextList = GuiHudTextFactory.MakeInstance(key, HumanPlayerIntelLevel, Data);
            _guiCursorHudText.Replace(key, coloredTextList);
        }
    }

    protected override void OnToUpdate() {
        base.OnToUpdate();
        bool isTurnUnderway = Navigator.TryProcessHeadingChange((int)UpdateRate);   // IMPROVE isTurnUnderway useful as a field?
    }

    void FixedUpdate() {
        Navigator.ApplyThrust();
    }

    protected override void OptimizeDisplay() {
        bool toEnableAnimation = false;
        bool toShowShip = false;
        int distanceToCamera = _transform.DistanceToCameraInt();
        if (_isVisible) {
            if (distanceToCamera < maxShowDistance) {
                toShowShip = true;
                if (distanceToCamera < maxAnimateDistance) {
                    toEnableAnimation = true;
                }
            }
        }
        ShowShip(toShowShip);
        EnableAnimation(toEnableAnimation);
    }

    private void ShowShip(bool toShow) {
        //Logger.Log("ShowShip({0}) called.".Inject(toShow));
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
        //Logger.Log("ShowShipViaMaterialColor({0}) called.".Inject(toShow));

    }

    /// <summary>
    /// Controls whether the ship can be seen via the activation state of the renderer gameobject. 
    /// <remarks>This approach to hiding the ship works, but we lose any benefit of knowing
    /// when the ship is invisible since, when deactivated, OnBecameVisible/Invisible is not called.</remarks>
    /// </summary>
    /// <param name="toShow">if set to <c>true</c> [to activate].</param>
    private void ShowShipViaRenderer(bool toShow) {
        _renderer.transform.gameObject.SetActive(toShow);
        //Logger.Log("ShowShipViaRenderer({0}) called.".Inject(toShow));
    }

    private void EnableAnimation(bool toEnable) {
        // TODO currently there is no animation
        //Logger.Log("Ship Animation is enabled = {0}".Inject(toEnable));
    }

    void OnDestroy() {
        Navigator.Dispose();
        Data.Dispose();
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

    public void OnClick() {
        if (_isShipShowing && GameInputHelper.IsMiddleMouseButton()) {
            _eventMgr.Raise<FocusSelectedEvent>(new FocusSelectedEvent(this, _transform));
        }
    }

    public void IsFocus() {
        // TODO
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
}

