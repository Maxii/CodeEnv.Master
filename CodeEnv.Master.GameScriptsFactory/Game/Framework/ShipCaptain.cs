// --------------------------------------------------------------------------------------------------------------------
// <copyright>
// Copyright © 2012 - 2013 Strategic Forge
//
// Email: jim@strategicforge.com
// </copyright> 
// <summary> 
// File: ShipCaptain.cs
// Manages the operation of a ship within a fleet.
// </summary> 
// -------------------------------------------------------------------------------------------------------------------- 

// default namespace

using System;
using CodeEnv.Master.Common;
using CodeEnv.Master.GameContent;
using UnityEngine;

/// <summary>
/// Manages the operation of a ship within a fleet.
/// </summary>
public class ShipCaptain : FollowableItem, ISelectable, IHasContextMenu, IDisposable {

    public new ShipData Data {
        get { return base.Data as ShipData; }
        set { base.Data = value; }
    }

    public Navigator Navigator { get; private set; }

    private ShipGraphics _shipGraphics;
    private FleetManager _fleetMgr;
    private FleetCommand _fleetCmd;
    private SelectionManager _selectionMgr;

    protected override void Awake() {
        base.Awake();
        UnityUtility.ValidateComponentPresence<Rigidbody>(gameObject);
        _shipGraphics = gameObject.GetSafeMonoBehaviourComponent<ShipGraphics>();
        _fleetMgr = gameObject.GetSafeMonoBehaviourComponentInParents<FleetManager>();
        _fleetCmd = _fleetMgr.gameObject.GetSafeMonoBehaviourComponentInChildren<FleetCommand>();
        _selectionMgr = SelectionManager.Instance;
        UpdateRate = FrameUpdateFrequency.Infrequent;
    }

    protected override void Start() {
        base.Start();
        __ValidateCtxObjectSettings();
    }

    protected override IGuiHudPublisher InitializeHudPublisher() {
        return new GuiHudPublisher<ShipData>(Data);
    }

    protected override void OnDataChanged() {
        base.OnDataChanged();
        __InitializeNavigator();
        HudPublisher.SetOptionalUpdateKeys(GuiHudLineKeys.Speed);
        SubscribeToDataValueChanges();
    }

    private void __InitializeNavigator() {
        Navigator = new Navigator(_transform, Data);
    }

    private void SubscribeToDataValueChanges() {
        Data.SubscribeToPropertyChanged<ShipData, float>(sd => sd.Health, OnHealthChanged);
    }

    public void ChangeHeading(Vector3 newHeading) {
        if (Navigator.ChangeHeading(newHeading)) {
            StartCoroutine(Navigator.ExecuteHeadingChange());
        }
        // else TODO
    }

    public void ChangeSpeed(float newRequestedSpeed) {
        if (Navigator.ChangeSpeed(newRequestedSpeed)) {
            StartCoroutine(Navigator.ExecuteSpeedChange());
        }
        // else TODO
    }

    private void OnHealthChanged() {
        D.Log("{0} Health = {1}.", Data.Name, Data.Health);
        if (Data.Health <= Constants.ZeroF) {
            Die();
        }
    }

    private void Die() {
        if (IsSelected) {
            _selectionMgr.CurrentSelection = null;
        }
        _fleetCmd.ReportShipLost(this);
        _eventMgr.Raise<ItemDeathEvent>(new ItemDeathEvent(this));
        Destroy(gameObject);
    }

    private void OnIsSelectedChanged() {
        _shipGraphics.AssessHighlighting();
        if (IsSelected) {
            _selectionMgr.CurrentSelection = this;
        }
    }

    protected override void OnIsFocusChanged() {
        base.OnIsFocusChanged();
        _shipGraphics.AssessHighlighting();
    }

    protected override void OnClick() {
        base.OnClick();
        if (GameInputHelper.IsLeftMouseButton()) {
            OnLeftClick();
        }
    }

    void OnDoubleClick() {
        if (GameInputHelper.IsLeftMouseButton()) {
            OnLeftDoubleClick();
        }
    }

    private void OnLeftDoubleClick() {
        _fleetMgr.OnLeftClick();
    }

    private void __SimulateAttacked() {
        if (!DebugSettings.Instance.MakePlayerInvincible) {
            __OnHit(UnityEngine.Random.Range(1.0F, Data.MaxHitPoints));
        }
    }

    private void __OnHit(float damage) {
        Data.Health = Data.Health - damage;
    }

    protected override void OnDestroy() {
        base.OnDestroy();
        Dispose();
    }

    private void Cleanup() {
        // other cleanup here including any tracking Gui2D elements
        Navigator.Dispose();
        Data.Dispose();
    }

    public override string ToString() {
        return new ObjectAnalyzer().ToString(this);
    }

    #region ICameraTargetable Members

    public override bool IsTargetable {
        get {
            return _shipGraphics.IsShowShip;
        }
    }

    #endregion

    #region ISelectable Members

    private bool _isSelected;
    public bool IsSelected {
        get { return _isSelected; }
        set { SetProperty<bool>(ref _isSelected, value, "IsSelected", OnIsSelectedChanged); }
    }

    public void OnLeftClick() {
        KeyCode notUsed;
        if (GameInputHelper.TryIsKeyHeldDown(out notUsed, KeyCode.LeftAlt, KeyCode.RightAlt)) {
            __SimulateAttacked();
            return;
        }
        IsSelected = true;
    }

    #endregion

    #region IHasContextMenu Members

    public void __ValidateCtxObjectSettings() {
        CtxObject ctxObject = gameObject.GetSafeMonoBehaviourComponent<CtxObject>();
        D.Assert(ctxObject.contextMenu != null, "{0}.contextMenu on {1} is null.".Inject(typeof(CtxObject).Name, gameObject.name));
        UnityUtility.ValidateComponentPresence<Collider>(gameObject);
    }

    public void OnPress(bool isDown) {
        if (IsSelected) {
            //D.Log("{0}.OnPress({1}) called.", this.GetType().Name, isPressed);
            CameraControl.Instance.ShowContextMenuOnPress(isDown);
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
            Cleanup();
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

