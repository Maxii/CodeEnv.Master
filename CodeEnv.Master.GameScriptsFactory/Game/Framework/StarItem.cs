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
public class StarItem : AIntelItem, IShipOrbitable, IDetectable {

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
        get { return _publisher = _publisher ?? new StarPublisher(Data); }
    }

    protected new StarDisplayManager DisplayMgr { get { return base.DisplayMgr as StarDisplayManager; } }

    private SystemItem _system;
    private DetectionHandler _detectionHandler;
    private ICtxControl _ctxControl;

    #region Initialization

    protected override void InitializeLocalReferencesAndValues() {
        base.InitializeLocalReferencesAndValues();
        var meshRenderer = gameObject.GetComponentInChildren<Renderer>();   // FIXME this gets first, but there is more than one
        Radius = meshRenderer.bounds.size.x / 2F;    // half of the (length, width or height, all the same surrounding a sphere)
        collider.enabled = false;
        collider.isTrigger = false;
        (collider as SphereCollider).radius = Radius;
        InitializeShipOrbitSlot();
        InitializeKeepoutZone();
        //D.Log("{0}.Radius set to {1}.", FullName, Radius);
    }

    private void InitializeShipOrbitSlot() {
        float innerOrbitRadius = Radius * TempGameValues.KeepoutRadiusMultiplier;
        float outerOrbitRadius = innerOrbitRadius + TempGameValues.DefaultShipOrbitSlotDepth;
        ShipOrbitSlot = new ShipOrbitSlot(innerOrbitRadius, outerOrbitRadius, this);
    }

    private void InitializeKeepoutZone() {
        SphereCollider keepoutZoneCollider = gameObject.GetComponentInImmediateChildren<SphereCollider>();
        D.Assert(keepoutZoneCollider.gameObject.layer == (int)Layers.CelestialObjectKeepout);
        keepoutZoneCollider.isTrigger = true;
        keepoutZoneCollider.radius = ShipOrbitSlot.InnerRadius;
    }

    protected override void InitializeModelMembers() {
        D.Assert(category == Data.Category);
        _system = gameObject.GetSafeMonoBehaviourComponentInParents<SystemItem>();
        _detectionHandler = new DetectionHandler(Data);
    }

    protected override void InitializeViewMembersOnDiscernible() {
        base.InitializeViewMembersOnDiscernible();
        InitializeContextMenu(Owner);
    }

    protected override HudManager InitializeHudManager() {
        var hudManager = new HudManager(Publisher);
        hudManager.AddContentToUpdate(HudManager.UpdatableLabelContentID.IntelState);
        return hudManager;
    }

    private void InitializeContextMenu(Player owner) {
        _ctxControl = new StarCtxControl(this);
    }

    protected override ADisplayManager InitializeDisplayManager() {
        var displayMgr = new StarDisplayManager(gameObject);
        displayMgr.Icon = InitializeIcon();
        return displayMgr;
    }

    private ResponsiveTrackingSprite InitializeIcon() {
        var icon = TrackingWidgetFactory.Instance.CreateResponsiveTrackingSprite(this, TrackingWidgetFactory.IconAtlasID.Contextual,
            new Vector2(16, 16), WidgetPlacement.Over);
        icon.Set("Icon01");
        icon.Color = Owner.Color;
        var iconEventListener = icon.EventListener;
        iconEventListener.onHover += (iconGo, isOver) => OnHover(isOver);
        iconEventListener.onClick += (iconGo) => OnClick();
        iconEventListener.onDoubleClick += (iconGo) => OnDoubleClick();
        iconEventListener.onPress += (iconGo, isDown) => OnPress(isDown);
        return icon;
    }

    #endregion

    #region Model Methods

    public override void CommenceOperations() {
        base.CommenceOperations();
        collider.enabled = true;
    }

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

    #endregion

    #region Mouse Events

    protected override void OnLeftClick() {
        base.OnLeftClick();
        if (_system.IsDiscernible) {
            _system.IsSelected = true;
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
        if (DisplayMgr != null && DisplayMgr.Icon != null) {
            var iconEventListener = DisplayMgr.Icon.EventListener;
            iconEventListener.onHover -= (iconGo, isOver) => OnHover(isOver);
            iconEventListener.onClick -= (iconGo) => OnClick();
            iconEventListener.onDoubleClick -= (iconGo) => OnDoubleClick();
            iconEventListener.onPress -= (iconGo, isDown) => OnPress(isDown);
        }
    }

    #endregion

    public override string ToString() {
        return new ObjectAnalyzer().ToString(this);
    }

    #region IShipOrbitable Members

    public ShipOrbitSlot ShipOrbitSlot { get; private set; }

    #endregion

    #region ICameraTargetable Members

    public override float MinimumCameraViewingDistance { get { return Radius * minViewDistanceFactor; } }

    #endregion

    #region ICameraFocusable Members

    public override float OptimalCameraViewingDistance { get { return Radius * optViewDistanceFactor; } }

    #endregion

    #region IDetectable Members

    public void OnDetection(ICommandItem cmdItem, DistanceRange sensorRange) {
        _detectionHandler.OnDetection(cmdItem, sensorRange);
    }

    public void OnDetectionLost(ICommandItem cmdItem, DistanceRange sensorRange) {
        _detectionHandler.OnDetectionLost(cmdItem, sensorRange);
    }

    #endregion

    #region IHighlightable Members

    public override float HighlightRadius { get { return Radius * Screen.height * 1.5F; } }

    #endregion

}

