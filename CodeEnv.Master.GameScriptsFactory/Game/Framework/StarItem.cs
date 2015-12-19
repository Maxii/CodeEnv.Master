﻿// --------------------------------------------------------------------------------------------------------------------
// <copyright>
// Copyright © 2012 - 2014 Strategic Forge
//
// Email: jim@strategicforge.com
// </copyright> 
// <summary> 
// File: StarItem.cs
// AIntelItems that are Stars.
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
/// AIntelItems that are Stars.
/// </summary>
public class StarItem : AIntelItem, IStarItem, IShipOrbitable, ISensorDetectable, IShipTransitBanned {

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
    private SphereCollider _collider;

    #region Initialization

    protected override void InitializeOnData() {
        base.InitializeOnData();
        InitializePrimaryCollider();
        InitializeShipOrbitSlot();
        InitializeTransitBanCollider();
        D.Assert(category == Data.Category);
        System = gameObject.GetSingleComponentInParents<SystemItem>();
        _detectionHandler = new DetectionHandler(this);
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

    private void InitializeTransitBanCollider() {
        SphereCollider shipTransitBanCollider = gameObject.GetSingleComponentInChildren<SphereCollider>(excludeSelf: true);
        D.Assert(shipTransitBanCollider.gameObject.layer == (int)Layers.ShipTransitBan);
        shipTransitBanCollider.isTrigger = true;
        shipTransitBanCollider.radius = ShipTransitBanRadius;
    }

    protected override ItemHudManager InitializeHudManager() {
        return new ItemHudManager(Publisher);
    }

    protected override ICtxControl InitializeContextMenu(Player owner) {
        return new StarCtxControl(this);
    }

    protected override ADisplayManager InitializeDisplayManager() {
        var dMgr = new StarDisplayManager(this, MakeIconInfo());
        SubscribeToIconEvents(dMgr.Icon);
        return dMgr;
    }
    private void SubscribeToIconEvents(IResponsiveTrackingSprite icon) {
        var iconEventListener = icon.EventListener;
        iconEventListener.onHover += (go, isOver) => HoverEventHandler(isOver);
        iconEventListener.onClick += (go) => ClickEventHandler();
        iconEventListener.onDoubleClick += (go) => DoubleClickEventHandler();
        iconEventListener.onPress += (go, isDown) => PressEventHandler(isDown);
    }

    #endregion

    public override void CommenceOperations() {
        base.CommenceOperations();
        _collider.enabled = true;
    }

    public StarReport GetUserReport() { return Publisher.GetUserReport(); }

    public StarReport GetReport(Player player) { return Publisher.GetReport(player); }

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

    protected override void ShowSelectedItemHud() {
        SelectedItemHudWindow.Instance.Show(FormID.SelectedStar, GetUserReport());
    }

    #region Event and Property Change Handlers

    protected override void OwnerPropChangedHandler() {
        base.OwnerPropChangedHandler();
        if (DisplayMgr != null && DisplayMgr.Icon != null) {
            DisplayMgr.Icon.Color = Owner.Color;
        }
    }

    // Selecting a Star used to select the System for convenience. Stars are now selectable.

    #endregion

    #region Cleanup

    protected override void Cleanup() {
        base.Cleanup();
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
        iconEventListener.onHover -= (go, isOver) => HoverEventHandler(isOver);
        iconEventListener.onClick -= (go) => ClickEventHandler();
        iconEventListener.onDoubleClick -= (go) => DoubleClickEventHandler();
        iconEventListener.onPress -= (go, isDown) => PressEventHandler(isDown);
    }

    #endregion

    public override string ToString() {
        return new ObjectAnalyzer().ToString(this);
    }

    #region IShipOrbitable Members

    public ShipOrbitSlot ShipOrbitSlot { get; private set; }

    #endregion

    #region IShipTransitBanned Members

    public float ShipTransitBanRadius { get { return Data.HighOrbitRadius; } }

    #endregion

    #region IDetectable Members

    public void HandleDetectionBy(IUnitCmdItem cmdItem, RangeCategory sensorRangeCat) {
        _detectionHandler.HandleDetectionBy(cmdItem, sensorRangeCat);
    }

    public void HandleDetecionLostBy(IUnitCmdItem cmdItem, RangeCategory sensorRangeCat) {
        _detectionHandler.HandleDetectionLostBy(cmdItem, sensorRangeCat);
    }

    #endregion

    #region IHighlightable Members

    public override float HighlightRadius { get { return Radius * Screen.height * 1.5F; } }

    #endregion

    #region INavigableTarget Members

    public override float GetCloseEnoughDistance(ICanNavigate navigatingItem) {
        return ShipTransitBanRadius + 1F;
    }

    #endregion

}

