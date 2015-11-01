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

    public StarCategory category;

    [Range(0.5F, 3.0F)]
    [Tooltip("Minimum Camera View Distance Multiplier")]
    public float minViewDistanceFactor = 2F;

    [Range(3.0F, 15.0F)]
    [Tooltip("Optimal Camera View Distance Multiplier")]
    public float optViewDistanceFactor = 8F;

    public new StarData Data {
        get { return base.Data as StarData; }
        set { base.Data = value; }
    }

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

    protected override void InitializeLocalReferencesAndValues() {
        base.InitializeLocalReferencesAndValues();
        var primaryMeshRenderer = gameObject.GetFirstComponentInImmediateChildrenOnly<MeshRenderer>();
        //D.Log("Star renderer name = {0}, bounds size = {1}.", primaryMeshRenderer.name, primaryMeshRenderer.bounds.size);
        Radius = primaryMeshRenderer.bounds.size.x / 2F;    // half of the length, width or height, all the same surrounding a sphere
        _collider = UnityUtility.ValidateComponentPresence<SphereCollider>(gameObject);
        _collider.enabled = false;
        _collider.isTrigger = false;
        _collider.radius = Radius;
        InitializeKeepoutZone();
        InitializeShipOrbitSlot();
        //D.Log("{0}.Radius set to {1}.", FullName, Radius);
    }
    //protected override void InitializeLocalReferencesAndValues() {
    //    base.InitializeLocalReferencesAndValues();
    //    var primaryMeshRenderer = gameObject.GetFirstComponentInImmediateChildrenOnly<MeshRenderer>();
    //    //D.Log("Star renderer name = {0}, bounds size = {1}.", primaryMeshRenderer.name, primaryMeshRenderer.bounds.size);
    //    Radius = primaryMeshRenderer.bounds.size.x / 2F;    // half of the length, width or height, all the same surrounding a sphere
    //    collider.enabled = false;
    //    collider.isTrigger = false;
    //    (collider as SphereCollider).radius = Radius;
    //    InitializeKeepoutZone();
    //    InitializeShipOrbitSlot();
    //    //D.Log("{0}.Radius set to {1}.", FullName, Radius);
    //}

    private void InitializeKeepoutZone() {
        SphereCollider keepoutZoneCollider = gameObject.GetFirstComponentInImmediateChildrenOnly<SphereCollider>();
        D.Assert(keepoutZoneCollider.gameObject.layer == (int)Layers.CelestialObjectKeepout);
        keepoutZoneCollider.isTrigger = true;
        keepoutZoneCollider.radius = Radius * TempGameValues.KeepoutRadiusMultiplier;
        KeepoutRadius = keepoutZoneCollider.radius;
    }

    private void InitializeShipOrbitSlot() {
        float innerOrbitRadius = KeepoutRadius;
        float outerOrbitRadius = innerOrbitRadius + TempGameValues.DefaultShipOrbitSlotDepth;
        ShipOrbitSlot = new ShipOrbitSlot(innerOrbitRadius, outerOrbitRadius, this);
    }

    protected override void InitializeModelMembers() {
        D.Assert(category == Data.Category);
        System = gameObject.GetSafeFirstMonoBehaviourInParents<SystemItem>();
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
    //public override void CommenceOperations() {
    //    base.CommenceOperations();
    //    collider.enabled = true;
    //}

    public StarReport GetUserReport() { return Publisher.GetUserReport(); }

    public StarReport GetReport(Player player) { return Publisher.GetReport(player); }

    protected override float InitializeOptimalCameraViewingDistance() {
        return Radius * optViewDistanceFactor;
    }

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

    public float KeepoutRadius { get; private set; }

    public ShipOrbitSlot ShipOrbitSlot { get; private set; }

    #endregion

    #region ICameraTargetable Members

    public override float MinimumCameraViewingDistance { get { return Radius * minViewDistanceFactor; } }

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

