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
using CodeEnv.Master.Common.Unity;
using UnityEngine;

/// <summary>
/// Command entity that receives and executes orders for the Fleet. FleetCommand is automatically destroyed
/// when the health of the fleet reaches Zero.
/// </summary>
public class FleetCommand : FollowableItem, IHasContextMenu/*, IDisposable*/ {

    public new FleetData Data {
        get { return base.Data as FleetData; }
        set { base.Data = value; }
    }

    public float minFleetViewingDistance = 4.0F;
    public float optimalFleetViewingDistance = 10F;

    private FleetGraphics _fleetGraphics;
    private FleetManager _fleetMgr;

    protected override void Awake() {
        base.Awake();
        _fleetMgr = gameObject.GetSafeMonoBehaviourComponentInParents<FleetManager>();
        _fleetGraphics = gameObject.GetSafeMonoBehaviourComponentInParents<FleetGraphics>();
    }

    protected override void Start() {
        base.Start();
        __ValidateCtxObjectSettings();
    }

    protected override IGuiHudPublisher InitializeHudPublisher() {
        return new GuiHudPublisher<FleetData>(Data);
    }

    public void __GetFleetUnderway() {
        ChangeFleetHeading(_transform.forward);
        ChangeFleetSpeed(2.0F);
    }

    public void ChangeFleetHeading(Vector3 newHeading) {
        foreach (var shipCaptain in _fleetMgr.ShipCaptains) {   // OPTIMIZE with local field?
            shipCaptain.ChangeHeading(newHeading);
        }
    }

    public void ChangeFleetSpeed(float newSpeed) {
        foreach (var shipCaptain in _fleetMgr.ShipCaptains) {   // OPTIMIZE with local field?
            shipCaptain.ChangeSpeed(newSpeed);
        }
    }

    public void ReportShipLost(ShipCaptain shipCaptain) {
        Logger.Log("{0} acknowledging {1} has been lost.", this.GetType().Name, shipCaptain.name);
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
        Logger.Log("{0} Health = {1}.", Data.Name, Data.Health);
        if (Data.Health <= Constants.ZeroF) {
            __Die();
        }
    }

    private void __Die() {
        Logger.Log("{0} has Died!", Data.Name);
        Destroy(gameObject);
        Destroy(_fleetMgr.gameObject);
    }

    protected override void OnIsFocusChanged() {
        base.OnIsFocusChanged();
        _fleetGraphics.ChangeHighlighting();
    }

    protected override void OnDestroy() {
        base.OnDestroy();
        Data.Dispose();
    }

    public override string ToString() {
        return new ObjectAnalyzer().ToString(this);
    }

    #region ICameraTargetable Members

    /// <summary>
    /// Overridden because the default implementation returns a value that
    /// is a factor of the collider bounds which doesn't work for colliders whos
    /// size changes based on the distance to the camera.
    /// </summary>
    public override float MinimumCameraViewingDistance { get { return minFleetViewingDistance; } }

    #endregion

    #region ICameraFocusable Members

    /// <summary>
    /// Overridden because the default implementation returns a value that
    /// is a factor of the collider bounds which doesn't work for colliders whos
    /// size changes based on the distance to the camera.
    /// </summary>
    public override float OptimalCameraViewingDistance { get { return optimalFleetViewingDistance; } }

    #endregion

    #region IHasContextMenu Members

    public void __ValidateCtxObjectSettings() {
        CtxObject ctxObject = gameObject.GetSafeMonoBehaviourComponent<CtxObject>();
        D.Assert(ctxObject.contextMenu != null, "{0}.contextMenu on {1} is null.".Inject(typeof(CtxObject).Name, gameObject.name));
        UnityUtility.ValidateComponentPresence<Collider>(gameObject);
    }

    public void OnPress(bool isDown) {
        if (_fleetMgr.IsSelected) {
            //Logger.Log("{0}.OnPress({1}) called.", this.GetType().Name, isPressed);
            CameraControl.Instance.TryShowContextMenuOnPress(isDown);
        }
    }

    #endregion

}

