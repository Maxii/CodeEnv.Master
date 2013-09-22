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

using CodeEnv.Master.Common;
using CodeEnv.Master.GameContent;
using UnityEngine;

/// <summary>
/// Manages the operation of a ship within a fleet.
/// </summary>
public class ShipCaptain : FollowableItem, ISelectable, IHasContextMenu {

    public new ShipData Data {
        get { return base.Data as ShipData; }
        set { base.Data = value; }
    }

    public Navigator Navigator { get; private set; }

    private ShipGraphics _shipGraphics;
    private FleetManager _fleetMgr;
    private FleetCommand _fleetCmd;

    protected override void Awake() {
        base.Awake();
        UnityUtility.ValidateComponentPresence<Rigidbody>(gameObject);
        _shipGraphics = gameObject.GetSafeMonoBehaviourComponent<ShipGraphics>();
        _fleetMgr = gameObject.GetSafeMonoBehaviourComponentInParents<FleetManager>();
        _fleetCmd = _fleetMgr.gameObject.GetSafeMonoBehaviourComponentInChildren<FleetCommand>();
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
    }

    public void ChangeSpeed(float newRequestedSpeed) {
        if (Navigator.ChangeSpeed(newRequestedSpeed)) {
            // IMPROVE ChangeSpeed currently executes this as ApplyThrust is called from FixedUpdate here. Change to execute speed change coroutine like changeHeading?
        }
    }

    void FixedUpdate() {
        Navigator.ApplyThrust();
    }

    private void OnHealthChanged() {
        D.Log("{0} Health = {1}.", Data.Name, Data.Health);
        if (Data.Health <= Constants.ZeroF) {
            __Die();
        }
    }

    private void __Die() {
        _fleetCmd.ReportShipLost(this);
        Destroy(gameObject);
    }

    private void OnIsSelectedChanged() {
        _shipGraphics.AssessHighlighting();
        if (IsSelected) {
            _eventMgr.Raise<SelectionEvent>(new SelectionEvent(this));
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
            __OnHit(Random.Range(1.0F, Data.MaxHitPoints));
        }
    }

    private void __OnHit(float damage) {
        Data.Health = Data.Health - damage;
    }

    protected override void OnDestroy() {
        base.OnDestroy();
        if (!_isApplicationQuiting) {
            if (!Application.isLoadingLevel) {
                // game item has been destroyed in normal play
                _eventMgr.Raise<GameItemDestroyedEvent>(new GameItemDestroyedEvent(this));
            }
            // we aren't quiting so cleanup
            Navigator.Dispose();
            Data.Dispose();
        }
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
        if (GameInputHelper.IsKeyDown(KeyCode.LeftAlt, KeyCode.RightAlt)) {
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
            CameraControl.Instance.TryShowContextMenuOnPress(isDown);
        }
    }

    #endregion

}

