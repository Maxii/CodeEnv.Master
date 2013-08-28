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
public class FleetAdmiral : FollowableItem, ISelectable, IHasContextMenu, IDisposable {

    public new FleetData Data {
        get { return base.Data as FleetData; }
        set { base.Data = value; }
    }

    /// <summary>
    /// The separation between the pivot point on the lead ship and the fleet icon,
    ///as a Viewport vector. Viewport vector values vary from 0.0F to 1.0F.
    /// </summary>
    public Vector3 _fleetIconOffsetFromPivot = new Vector3(Constants.ZeroF, 0.03F, Constants.ZeroF);

    public float minFleetViewingDistance = 4.0F;
    public float optimalFleetViewingDistance = 10F;

    private Transform _leadShip;
    public Transform LeadShip {
        get { return _leadShip; }
        set { SetProperty<Transform>(ref _leadShip, value, "LeadShip", OnLeadShipChanged); }
    }

    private ShipCaptain[] _shipCaptains;
    private IList<IDisposable> _subscribers;

    /// <summary>
    /// The offset that determines the point on the lead ship from which
    ///  the Fleet Icon pivots, as a Worldspace vector.
    /// </summary>
    private Vector3 _fleetIconPivotOffset;
    private FleetGraphics _fleetGraphics;

    protected override void Awake() {
        base.Awake();
        gameObject.name = "Borg Fleet";
        _fleetGraphics = gameObject.GetSafeMonoBehaviourComponentInParents<FleetGraphics>();
        Subscribe();
    }

    private void Subscribe() {
        if (_subscribers == null) {
            _subscribers = new List<IDisposable>();
        }
        _subscribers.Add(GameManager.Instance.SubscribeToPropertyChanged<GameManager, GameState>(gs => gs.GameState, OnGameStateChanged));
    }

    protected override void Start() {
        base.Start();
        __ValidateCtxObjectSettings();
        PlayerIntelLevel = IntelLevel.LongRangeSensors;
        HudPublisher.SetOptionalUpdateKeys(GuiHudLineKeys.Speed);
        InitializeFleet();
    }

    private void InitializeFleet() {
        // overall fleet container gameobject is this FleetManager's parent
        _shipCaptains = _transform.parent.gameObject.GetSafeMonoBehaviourComponentsInChildren<ShipCaptain>();
        LeadShip = _shipCaptains[0].transform;
        _fleetIconPivotOffset = new Vector3(Constants.ZeroF, LeadShip.collider.bounds.extents.y, Constants.ZeroF);
    }

    private void OnLeadShipChanged() {
        Data.LeadShipData = LeadShip.gameObject.GetSafeMonoBehaviourComponent<ShipCaptain>().Data;
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
        if (NguiGameInput.IsLeftMouseButtonClick()) {
            ChangeFleetHeading(-_transform.right);  // turn left
        }
    }

    void Update() {
        if (ToUpdate()) {
            TrackLeadShip();
        }
    }

    private void TrackLeadShip() {  // OPTIMIZE?
        Vector3 viewportOffsetLocation = Camera.main.WorldToViewportPoint(LeadShip.position + _fleetIconPivotOffset);
        _transform.position = Camera.main.ViewportToWorldPoint(viewportOffsetLocation + _fleetIconOffsetFromPivot);
        _transform.rotation = LeadShip.rotation;
    }

    private void OnIsSelectedChanged() {
        _fleetGraphics.ChangeHighlighting();
        if (IsSelected) {
            _eventMgr.Raise<SelectionEvent>(new SelectionEvent(this, gameObject));
        }
    }

    protected override void OnIsFocusChanged() {
        base.OnIsFocusChanged();
        _fleetGraphics.ChangeHighlighting();
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

    #region ISelectable Members

    private bool _isSelected;
    public bool IsSelected {
        get { return _isSelected; }
        set { SetProperty<bool>(ref _isSelected, value, "IsSelected", OnIsSelectedChanged); }
    }


    public void OnLeftClick() {
        IsSelected = true;
    }

    #endregion

    #region IHasContextMenu Members

    public void __ValidateCtxObjectSettings() {
        CtxObject ctxObject = gameObject.GetSafeMonoBehaviourComponent<CtxObject>();
        D.Assert(ctxObject.contextMenu != null, "{0}.contextMenu on {1} is null.".Inject(typeof(CtxObject).Name, gameObject.name));
        UnityUtility.ValidateComponentPresence<Collider>(gameObject);
    }

    public void OnPress(bool isPressed) {
        if (IsSelected) {
            //Logger.Log("{0}.OnPress({1}) called.", this.GetType().Name, isPressed);
            CameraControl.Instance.ContextMenuPickHandler.OnPress(isPressed);
        }
    }

    #endregion

    #region IDisposable derived class
    [NonSerialized]
    private bool alreadyDisposed = false;

    /// <summary>
    /// Releases unmanaged and - optionally - managed resources. Derived classes that need to perform additional resource cleanup
    /// should override this Dispose(isDisposing) method, using its own alreadyDisposed flag to do it before calling base.Dispose(isDisposing).
    /// </summary>
    /// <param name="isDisposing"><c>true</c> to release both managed and unmanaged resources; <c>false</c> to release only unmanaged resources.</param>
    protected override void Dispose(bool isDisposing) {
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

        // Let the base class free its resources.
        // Base class is reponsible for calling GC.SuppressFinalize()
        base.Dispose(isDisposing);
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

