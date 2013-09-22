// --------------------------------------------------------------------------------------------------------------------
// <copyright>
// Copyright © 2012 - 2013 Strategic Forge
//
// Email: jim@strategicforge.com
// </copyright> 
// <summary> 
// File: FleetCommand.cs
// Command entity that receives and executes orders for the Fleet.
// </summary> 
// -------------------------------------------------------------------------------------------------------------------- 

// default namespace

using CodeEnv.Master.Common;
using CodeEnv.Master.GameContent;
using UnityEngine;

/// <summary>
/// Command entity that receives and executes orders for the Fleet. FleetCommand is automatically destroyed
/// when the health of the fleet reaches Zero.
/// </summary>
public class FleetCommand : FollowableItem, IFleetCommand, IHasContextMenu {

    public float minFleetViewingDistance = 4.0F;
    public float optimalFleetViewingDistance = 10F;

    private BoxCollider _boxCollider;
    private Vector3 _initialColliderSize;

    private FleetGraphics _fleetGraphics;
    private FleetManager _fleetMgr;
    private ScaleRelativeToCamera _fleetIconScaler;

    protected override void Awake() {
        base.Awake();
        _fleetMgr = gameObject.GetSafeMonoBehaviourComponentInParents<FleetManager>();
        _fleetGraphics = gameObject.GetSafeMonoBehaviourComponentInParents<FleetGraphics>();
        _fleetIconScaler = gameObject.GetSafeMonoBehaviourComponentInChildren<ScaleRelativeToCamera>();
        _boxCollider = collider as BoxCollider;
        _initialColliderSize = _boxCollider.size;
        UpdateRate = FrameUpdateFrequency.Infrequent;
    }

    protected override void Start() {
        base.Start();
        __ValidateCtxObjectSettings();
    }

    protected override IGuiHudPublisher InitializeHudPublisher() {
        return new GuiHudPublisher<FleetData>(Data);
    }

    void Update() {
        if (ToUpdate()) {
            KeepColliderSizeCurrent();
        }
    }

    /// <summary>
    /// Keeps the size (scale) of the collider current to match the
    /// scale of the FleetIcon that constantly changes as the camera moves.
    /// </summary>
    private void KeepColliderSizeCurrent() {
        _boxCollider.size = _initialColliderSize * _fleetIconScaler.Scale.magnitude;
    }

    public void __GetFleetUnderway() {
        ChangeFleetHeading(Random.onUnitSphere);
        ChangeFleetSpeed(2.0F);
    }

    public void ReportShipLost(ShipCaptain shipCaptain) {
        D.Log("{0} acknowledging {1} has been lost.", this.GetType().Name, shipCaptain.name);
        _fleetMgr.ProcessShipRemoval(shipCaptain);
    }

    protected override void OnClick() {
        base.OnClick();
        if (GameInputHelper.IsLeftMouseButton()) {
            _fleetMgr.OnLeftClick();
        }
    }

    void OnDoubleClick() {
        if (GameInputHelper.IsLeftMouseButton()) {
            ChangeFleetHeading(-_transform.right);  // turn left
        }
    }

    protected override void OnDataChanged() {
        base.OnDataChanged();
        HudPublisher.SetOptionalUpdateKeys(GuiHudLineKeys.Speed);
        SubscribeToDataValueChanges();
    }

    private void SubscribeToDataValueChanges() {
        Data.SubscribeToPropertyChanged<FleetData, float>(fd => fd.Health, OnHealthChanged);
    }

    private void OnHealthChanged() {
        D.Log("{0} Health = {1}.", Data.Name, Data.Health);
        if (Data.Health <= Constants.ZeroF) {
            __Die();
        }
    }

    private void __Die() {
        D.Log("{0} has Died!", Data.Name);
        Destroy(gameObject);
        Destroy(_fleetMgr.gameObject);
    }

    protected override void OnIsFocusChanged() {
        base.OnIsFocusChanged();
        _fleetGraphics.AssessHighlighting();
    }

    protected override void OnDestroy() {
        base.OnDestroy();
        Data.Dispose();
    }

    public override string ToString() {
        return new ObjectAnalyzer().ToString(this);
    }

    #region IFleetCommand Members

    public new FleetData Data {
        get { return base.Data as FleetData; }
        set { base.Data = value; }
    }

    public void ChangeFleetHeading(Vector3 newHeading) {
        D.Log("Heading was {0}, now {1}.", Data.RequestedHeading, newHeading);
        foreach (var shipCaptain in _fleetMgr.ShipCaptains) {   // OPTIMIZE with local field?
            shipCaptain.ChangeHeading(newHeading);
        }
    }

    public void ChangeFleetSpeed(float newSpeed) {
        foreach (var shipCaptain in _fleetMgr.ShipCaptains) {   // OPTIMIZE with local field?
            shipCaptain.ChangeSpeed(newSpeed);
        }
    }

    #endregion

    #region ICameraTargetable Members

    /// <summary>
    /// Overridden because the default implementation returns a value that
    /// is a factor of the collider bounds which doesn't work for colliders whos
    /// size changes based on the distance to the camera.
    /// </summary>
    protected override float CalcMinimumCameraViewingDistance() {
        return minFleetViewingDistance;
    }

    #endregion

    #region ICameraFocusable Members

    public override bool IsRetainedFocusEligible { get { return true; } }

    /// <summary>
    /// Overridden because the default implementation returns a value that
    /// is a factor of the collider bounds which doesn't work for colliders whos
    /// size changes based on the distance to the camera.
    /// </summary>
    protected override float CalcOptimalCameraViewingDistance() {
        return optimalFleetViewingDistance;
    }

    #endregion

    #region IHasContextMenu Members

    public void __ValidateCtxObjectSettings() {
        CtxObject ctxObject = gameObject.GetSafeMonoBehaviourComponent<CtxObject>();
        D.Assert(ctxObject.contextMenu != null, "{0}.contextMenu on {1} is null.".Inject(typeof(CtxObject).Name, gameObject.name));
        UnityUtility.ValidateComponentPresence<Collider>(gameObject);
    }

    public void OnPress(bool isDown) {
        if (_fleetMgr.IsSelected) {
            //D.Log("{0}.OnPress({1}) called.", this.GetType().Name, isPressed);
            CameraControl.Instance.TryShowContextMenuOnPress(isDown);
        }
    }

    #endregion

}

