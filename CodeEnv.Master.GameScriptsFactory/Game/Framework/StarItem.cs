// --------------------------------------------------------------------------------------------------------------------
// <copyright>
// Copyright © 2012 - 2014 Strategic Forge
//
// Email: jim@strategicforge.com
// </copyright> 
// <summary> 
// File: StarItem.cs
// Class for AIntelItems that are Stars.
// </summary> 
// -------------------------------------------------------------------------------------------------------------------- 

#define DEBUG_LOG
#define DEBUG_WARN
#define DEBUG_ERROR

// default namespace

using System;
using CodeEnv.Master.Common;
using CodeEnv.Master.GameContent;
using UnityEngine;

/// <summary>
/// Class for AIntelItems that are Stars.
/// </summary>
public class StarItem : AIntelItem, IStarItem, IShipOrbitable, ISensorDetectable {

    public StarCategory category = StarCategory.None;

    public new StarData Data {
        get { return base.Data as StarData; }
        set { base.Data = value; }
    }

    public override float Radius { get { return Data.Radius; } }

    private StarPublisher _publisher;
    public StarPublisher Publisher {
        get { return _publisher = _publisher ?? new StarPublisher(Data, this); }
    }

    public new StarDisplayManager DisplayMgr { get { return base.DisplayMgr as StarDisplayManager; } }

    public ISystemItem System { get; private set; }

    public Index3D SectorIndex { get { return Data.SectorIndex; } }

    private DetectionHandler _detectionHandler;
    private ICtxControl _ctxControl;
    private SphereCollider _collider;

    #region Initialization

    protected override void InitializeOnData() {
        InitializePrimaryCollider();
        InitializeShipOrbitSlot();
        InitializeTransitBanZone();
    }

    private void InitializePrimaryCollider() {
        _collider = UnityUtility.ValidateComponentPresence<SphereCollider>(gameObject);
        _collider.enabled = false;
        _collider.isTrigger = false;
        _collider.radius = Data.Radius;
    }

    private void InitializeShipOrbitSlot() {
        ShipOrbitSlot = new ShipOrbitSlot(Data.LowOrbitRadius, Data.HighOrbitRadius, this);
    }

    private void InitializeTransitBanZone() {
        SphereCollider transitBanZoneCollider = gameObject.GetSingleComponentInChildren<SphereCollider>(excludeSelf: true);
        D.Assert(transitBanZoneCollider.gameObject.layer == (int)Layers.TransitBan);
        transitBanZoneCollider.isTrigger = true;
        transitBanZoneCollider.radius = Data.HighOrbitRadius;
    }

    protected override void InitializeModelMembers() {
        D.Assert(category == Data.Category);
        System = gameObject.GetSingleComponentInParents<SystemItem>();
        _detectionHandler = new DetectionHandler(this);
    }

    protected override void InitializeViewMembersWhenFirstDiscernibleToUser() {
        base.InitializeViewMembersWhenFirstDiscernibleToUser();
        InitializeContextMenu(Owner);
    }

    protected override ItemHudManager InitializeHudManager() {
        return new ItemHudManager(Publisher);
    }

    private void InitializeContextMenu(Player owner) {
        _ctxControl = new StarCtxControl(this);
    }

    protected override ADisplayManager InitializeDisplayManager() {
        var displayMgr = new StarDisplayManager(this, MakeIconInfo());
        SubscribeToIconEvents(displayMgr.Icon);
        return displayMgr;
    }

    private void SubscribeToIconEvents(IResponsiveTrackingSprite icon) {
        var iconEventListener = icon.EventListener;
        iconEventListener.onHover += (go, isOver) => OnHover(isOver);
        iconEventListener.onClick += (go) => OnClick();
        iconEventListener.onDoubleClick += (go) => OnDoubleClick();
        iconEventListener.onPress += (go, isDown) => OnPress(isDown);
    }

    #endregion

    #region Model Methods

    public override void CommenceOperations() {
        base.CommenceOperations();
        _collider.enabled = true;
    }

    public StarReport GetUserReport() { return Publisher.GetUserReport(); }

    public StarReport GetReport(Player player) { return Publisher.GetReport(player); }

    protected override void OnOwnerChanging(Player newOwner) {
        base.OnOwnerChanging(newOwner);
        // there is only 1 type of ContextMenu for Stars so no need to generate a new one
    }

    protected override void OnOwnerChanged() {
        base.OnOwnerChanged();
        if (DisplayMgr != null && DisplayMgr.Icon != null) {
            DisplayMgr.Icon.Color = Owner.Color;
        }
    }

    #endregion

    #region View Methods

    private void AssessIcon() {
        if (DisplayMgr == null) { return; }

        var iconInfo = RefreshIconInfo();
        if (DisplayMgr.IconInfo != iconInfo) {    // avoid property not changed warning
            UnsubscribeToIconEvents(DisplayMgr.Icon);
            D.Log("{0} changing IconInfo from {1} to {2}.", FullName, DisplayMgr.IconInfo, iconInfo);
            DisplayMgr.IconInfo = iconInfo;
            SubscribeToIconEvents(DisplayMgr.Icon);
        }
    }

    private IconInfo RefreshIconInfo() {
        return MakeIconInfo();
    }

    private IconInfo MakeIconInfo() {
        var report = GetUserReport();
        GameColor iconColor = report.Owner != null ? report.Owner.Color : GameColor.White;
        return new IconInfo("Icon01", AtlasID.Contextual, iconColor);
    }

    #endregion

    #region Events

    protected override void OnLeftClick() {
        base.OnLeftClick();
        if (System.IsDiscernibleToUser) {
            (System as ISelectable).IsSelected = true;
            //System.IsSelected = true;
        }
    }

    protected override void OnRightPress(bool isDown) {
        base.OnRightPress(isDown);
        if (!isDown && !_inputMgr.IsDragging) {
            // right press release while not dragging means both press and release were over this object
            _ctxControl.OnRightPressRelease();
        }
    }

    #endregion

    #region Cleanup

    protected override void Cleanup() {
        base.Cleanup();
        if (_ctxControl != null) {
            (_ctxControl as IDisposable).Dispose();
        }
        if (_detectionHandler != null) {
            _detectionHandler.Dispose();
        }
    }

    protected override void Unsubscribe() {
        base.Unsubscribe();
        if (DisplayMgr != null) {
            var icon = DisplayMgr.Icon;
            if (icon != null) {
                UnsubscribeToIconEvents(icon);
            }
        }
    }

    private void UnsubscribeToIconEvents(IResponsiveTrackingSprite icon) {
        var iconEventListener = icon.EventListener;
        iconEventListener.onHover -= (go, isOver) => OnHover(isOver);
        iconEventListener.onClick -= (go) => OnClick();
        iconEventListener.onDoubleClick -= (go) => OnDoubleClick();
        iconEventListener.onPress -= (go, isDown) => OnPress(isDown);
    }

    #endregion

    public override string ToString() {
        return new ObjectAnalyzer().ToString(this);
    }

    #region IShipOrbitable Members

    public float TransitBanRadius { get { return Data.HighOrbitRadius; } }

    public ShipOrbitSlot ShipOrbitSlot { get; private set; }

    #endregion

    #region IDetectable Members

    public void OnDetection(IUnitCmdItem cmdItem, RangeCategory sensorRangeCat) {
        _detectionHandler.OnDetection(cmdItem, sensorRangeCat);
    }

    public void OnDetectionLost(IUnitCmdItem cmdItem, RangeCategory sensorRangeCat) {
        _detectionHandler.OnDetectionLost(cmdItem, sensorRangeCat);
    }

    #endregion

    #region IHighlightable Members

    public override float HighlightRadius { get { return Radius * Screen.height * 1.5F; } }

    #endregion

}

