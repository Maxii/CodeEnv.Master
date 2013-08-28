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
using CodeEnv.Master.Common.Unity;
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

    protected override void Awake() {
        base.Awake();
        UnityUtility.ValidateComponentPresence<Rigidbody>(gameObject);
        _shipGraphics = gameObject.GetSafeMonoBehaviourComponent<ShipGraphics>();
        UpdateRate = UpdateFrequency.Infrequent;
    }

    protected override void Start() {
        base.Start();
        __ValidateCtxObjectSettings();
        __InitializeNavigator();
        PlayerIntelLevel = IntelLevel.ShortRangeSensors;
        HudPublisher.SetOptionalUpdateKeys(GuiHudLineKeys.Speed);
    }


    private void __InitializeNavigator() {
        Navigator = new Navigator(_transform, Data);
    }

    public void ChangeHeading(Vector3 newHeading) {
        if (Data.RequestedHeading != newHeading) {
            Navigator.ChangeHeading(newHeading);
        }
    }

    public void ChangeSpeed(float newRequestedSpeed) {
        if (Data.RequestedSpeed != newRequestedSpeed) {
            //Logger.Log("Current Requested Speed = {0}, New Requested Speed = {1}.", Data.RequestedSpeed, newRequestedSpeed);
            Navigator.ChangeSpeed(newRequestedSpeed);
        }
    }

    void Update() {
        if (ToUpdate()) {
            bool isTurnUnderway = Navigator.TryProcessHeadingChange((int)UpdateRate);   // IMPROVE isTurnUnderway useful as a field?
        }
    }

    void FixedUpdate() {
        Navigator.ApplyThrust();
    }

    private void OnIsSelectedChanged() {
        _shipGraphics.ChangeHighlighting();
        if (IsSelected) {
            _eventMgr.Raise<SelectionEvent>(new SelectionEvent(this, gameObject));
        }
    }

    protected override void OnIsFocusChanged() {
        base.OnIsFocusChanged();
        _shipGraphics.ChangeHighlighting();
    }

    protected override void OnClick() {
        base.OnClick();
        if (NguiGameInput.IsLeftMouseButtonClick()) {
            OnLeftClick();
        }
    }

    void OnDestroy() {
        Navigator.Dispose();
        Data.Dispose();
    }

    public override string ToString() {
        return new ObjectAnalyzer().ToString(this);
    }

    #region ICameraTargetable Members

    public override bool IsTargetable {
        get {
            return _shipGraphics.IsShipShowing;
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
}

