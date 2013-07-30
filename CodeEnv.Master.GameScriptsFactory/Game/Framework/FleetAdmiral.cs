// --------------------------------------------------------------------------------------------------------------------
// <copyright>
// Copyright © 2012 - 2013 Strategic Forge
//
// Email: jim@strategicforge.com
// </copyright> 
// <summary> 
// File: FleetAdmiral.cs
// Runs the operations of a fleet, aka Fleet Command.
// </summary> 
// -------------------------------------------------------------------------------------------------------------------- 

// default namespace

using System;
using System.Collections.Generic;
using CodeEnv.Master.Common;
using CodeEnv.Master.Common.Unity;
using UnityEngine;

/// <summary>
/// Runs the operations of a fleet, aka Fleet Command.
/// </summary>
public class FleetAdmiral : FollowableItem, ISelectable, IDisposable {

    public new FleetData Data {
        get { return base.Data as FleetData; }
        set { base.Data = value; }
    }

    /// <summary>
    /// The separation between the pivot point on the lead ship and the fleet icon,
    ///as a Viewport vector. Viewport vector values vary from 0.0F to 1.0F.
    /// </summary>
    public Vector3 _fleetIconOffsetFromPivot = new Vector3(Constants.ZeroF, 0.02F, Constants.ZeroF);

    public float minIconZoomDistance = 4.0F;
    public float optimalIconFollowDistance = 10F;

    /// <summary>
    /// The offset that determines the point on the lead ship from which
    ///  the Fleet Icon pivots, as a Worldspace vector.
    /// </summary>
    private Vector3 _fleetIconPivotOffset;

    private Transform _fleetIcon;
    private Transform _leadShip;

    private ShipCaptain[] _shipCaptains;
    private IList<IDisposable> _subscribers;

    protected override void InitializeOnAwake() {
        base.InitializeOnAwake();
        gameObject.name = "Borg Fleet";
        _fleetIcon = gameObject.GetSafeMonoBehaviourComponentInChildren<UISprite>().transform.parent;
        Subscribe();
    }

    private void Subscribe() {
        if (_subscribers == null) {
            _subscribers = new List<IDisposable>();
        }
        _subscribers.Add(GameManager.Instance.SubscribeToPropertyChanged<GameManager, GameState>(gs => gs.GameState, OnGameStateChanged));
    }

    protected override void InitializeOnStart() {
        base.InitializeOnStart();
        HumanPlayerIntelLevel = IntelLevel.LongRangeSensors;
        HudPublisher.SetOptionalUpdateKeys(GuiCursorHudLineKeys.Speed);
        InitializeFleet();
    }

    private void InitializeFleet() {
        // overall fleet container gameobject is this FleetManager's parent
        _shipCaptains = _transform.parent.gameObject.GetSafeMonoBehaviourComponentsInChildren<ShipCaptain>();
        AssignLeadShip(_shipCaptains[0].transform);
        _fleetIconPivotOffset = new Vector3(Constants.ZeroF, _leadShip.collider.bounds.extents.y, Constants.ZeroF);
    }

    public void AssignLeadShip(Transform leadShip) {
        _leadShip = leadShip;
        Data.LeadShipData = leadShip.gameObject.GetSafeMonoBehaviourComponent<ShipCaptain>().Data;
    }

    private void OnGameStateChanged() {
        if (GameManager.Instance.GameState == GameState.Running) {
            __GetFleetUnderway();
        }
    }

    private void __GetFleetUnderway() {
        ChangeFleetHeading(_transform.forward);
        ChangeFleetSpeed(2.0F);
    }

    public void ChangeFleetHeading(Vector3 newHeading) {
        foreach (var shipCaptain in _shipCaptains) {
            shipCaptain.ChangeHeading(newHeading);
        }
    }

    public void ChangeFleetSpeed(float newSpeed) {
        foreach (var shipCaptain in _shipCaptains) {
            shipCaptain.ChangeSpeed(newSpeed);
        }
    }

    protected override void OnClick() {
        base.OnClick();
        if (NguiGameInput.IsLeftMouseButtonClick()) {
            OnLeftClick();
        }
    }

    void OnDoubleClick() {
        ChangeFleetHeading(-_transform.right);
    }

    void Update() {
        if (ToUpdate()) {
            TrackLeadShip();
        }
    }

    private void TrackLeadShip() {  // OPTIMIZE?
        Vector3 viewportOffsetLocation = Camera.main.WorldToViewportPoint(_leadShip.position + _fleetIconPivotOffset);
        _transform.position = Camera.main.ViewportToWorldPoint(viewportOffsetLocation + _fleetIconOffsetFromPivot);
        _transform.rotation = _leadShip.rotation;
    }

    private void Unsubscribe() {
        _subscribers.ForAll(d => d.Dispose());
        _subscribers.Clear();
    }

    void OnDestroy() {
        Dispose();
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
    public override float MinimumCameraViewingDistance { get { return minIconZoomDistance; } }

    #endregion

    #region ICameraFocusable Members

    /// <summary>
    /// Overridden because the default implementation returns a value that
    /// is a factor of the collider bounds which doesn't work for colliders whos
    /// size changes based on the distance to the camera.
    /// </summary>
    public override float OptimalCameraViewingDistance { get { return optimalIconFollowDistance; } }

    #endregion

    #region ISelectable Members

    public void OnLeftClick() {
        // TODO does nothing for now
    }

    private bool _isSelected;
    public bool IsSelected {
        get { return _isSelected; }
        set { SetProperty<bool>(ref _isSelected, value, "IsSelected"); }
    }

    #endregion

    #region IDisposable
    [DoNotSerialize]
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
    /// <param name="isDisposing"><c>true</c> to release both managed and unmanaged resources; <c>false</c> to release only unmanaged resources.</param>
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

}

