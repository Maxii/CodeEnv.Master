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
[System.Obsolete]
public class FleetCommand : FollowableItem, IFleetCommand {

    public float minFleetViewingDistance = 4F;
    public float optimalFleetViewingDistance = 6F;

    private FleetGraphics _fleetGraphics;
    private FleetUnitCreator _fleetMgr;

    protected override void Awake() {
        base.Awake();
        _fleetMgr = gameObject.GetSafeMonoBehaviourComponentInParents<FleetUnitCreator>();
        _fleetGraphics = gameObject.GetSafeMonoBehaviourComponentInParents<FleetGraphics>();
    }

    protected override void Start() {
        base.Start();
        __ValidateCtxObjectSettings();
    }

    private void __ValidateCtxObjectSettings() {
        CtxObject ctxObject = gameObject.GetSafeMonoBehaviourComponent<CtxObject>();
        D.Assert(ctxObject.contextMenu != null, "{0}.contextMenu on {1} is null.".Inject(typeof(CtxObject).Name, gameObject.name));
        UnityUtility.ValidateComponentPresence<Collider>(gameObject);
    }

    protected override IGuiHudPublisher InitializeHudPublisher() {
        return new GuiHudPublisher<FleetCmdData>(Data);
    }

    public void __GetFleetUnderway() {
        ChangeFleetHeading(Random.onUnitSphere);
        ChangeFleetSpeed(2.0F);
        _fleetGraphics.AssessHighlighting();    // temporary initialization location as IsVisible no longer changes initially to force it
    }

    public void ReportShipLost(ShipCaptain shipCaptain) {
        D.Log("{0} acknowledging {1} has been lost.", this.GetType().Name, shipCaptain.name);
        _fleetMgr.ProcessShipRemoval(shipCaptain);
    }

    protected override void OnPlayerIntelLevelChanged() {
        base.OnPlayerIntelLevelChanged();
        _fleetGraphics.IsDetectable = PlayerIntelLevel != IntelLevel.Nil;
    }

    void OnPress(bool isDown) {
        if (_fleetMgr.IsSelected) {
            //D.Log("{0}.OnPress({1}) called.", this.GetType().Name, isPressed);
            CameraControl.Instance.ShowContextMenuOnPress(isDown);
        }
    }

    protected override void OnClick() {
        base.OnClick();
        if (GameInputHelper.IsLeftMouseButton()) {
            _fleetMgr.OnLeftClick();
        }
    }

    void OnDoubleClick() {
        if (GameInputHelper.IsLeftMouseButton()) {
            ChangeFleetHeading(Random.insideUnitSphere.normalized);
            ChangeFleetSpeed(Random.Range(Constants.ZeroF, 2.5F));
        }
    }

    protected override void OnDataChanged() {
        base.OnDataChanged();
        HudPublisher.SetOptionalUpdateKeys(GuiHudLineKeys.Speed);
        SubscribeToDataValueChanges();
    }

    private void SubscribeToDataValueChanges() {
        Data.SubscribeToPropertyChanged<FleetCmdData, float>(fd => fd.UnitHealth, OnHealthChanged);
    }

    private void OnHealthChanged() {
        D.Log("{0} Health = {1}.", Data.Name, Data.UnitHealth);
        if (Data.UnitHealth <= Constants.ZeroF) {
            Die();
        }
    }

    private void Die() {
        D.Log("{0} has Died!", Data.Name);
        if (IsFocus) {
            CameraControl.Instance.CurrentFocus = null;
        }
        _eventMgr.Raise<MortalItemDeathEvent>(new MortalItemDeathEvent(this));
        Destroy(gameObject);
        _fleetMgr.Die();
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

    public new FleetCmdData Data {
        get { return base.Data as FleetCmdData; }
        set { base.Data = value; }
    }

    public void ChangeFleetHeading(Vector3 newHeading) {
        D.Log("Heading was {0}, now {1}.", Data.RequestedHeading, newHeading);
        foreach (var shipCaptain in _fleetMgr.ShipCaptains) {   // OPTIMIZE with local field?
            shipCaptain.ChangeHeading(newHeading);
        }
    }

    public void ChangeFleetSpeed(float newSpeed) {
        if (DebugSettings.Instance.StopShipMovement) {
            newSpeed = Constants.ZeroF;
        }
        foreach (var shipCaptain in _fleetMgr.ShipCaptains) {   // OPTIMIZE with local field?
            shipCaptain.ChangeSpeed(newSpeed);
        }
    }

    #endregion

    #region ICameraTargetable Members

    public override bool IsEligible {
        get {
            return _fleetGraphics.IsDetectable;
        }
    }

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

    public override bool IsRetainedFocusEligible {
        get { return _fleetGraphics.IsDetectable; }
    }

    /// <summary>
    /// Overridden because the default implementation returns a value that
    /// is a factor of the collider bounds which doesn't work for colliders whos
    /// size changes based on the distance to the camera.
    /// </summary>
    protected override float CalcOptimalCameraViewingDistance() {
        return optimalFleetViewingDistance;
    }

    #endregion

}

