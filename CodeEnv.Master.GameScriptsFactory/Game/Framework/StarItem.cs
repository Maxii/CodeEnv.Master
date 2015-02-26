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

    private static LayerMask _starLightCullingMask = LayerMaskExtensions.CreateInclusiveMask(Layers.Default, Layers.TransparentFX,
    Layers.ShipCull, Layers.FacilityCull, Layers.PlanetoidCull, Layers.StarCull);

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

    protected override float ItemTypeCircleScale { get { return 1.5F; } }

    //private Billboard _billboard;
    private SystemItem _system;
    private DetectionHandler _detectionHandler;
    private ICtxControl _ctxControl;
    //private InteractableTrackingSprite _icon;

    private StarDisplayMgr _displayMgr;

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
    //protected override void InitializeLocalReferencesAndValues() {
    //    base.InitializeLocalReferencesAndValues();
    //    var meshRenderer = gameObject.GetComponentInImmediateChildren<Renderer>();
    //    Radius = meshRenderer.bounds.size.x / 2F;    // half of the (length, width or height, all the same surrounding a sphere)
    //    collider.enabled = false;
    //    collider.isTrigger = false;
    //    (collider as SphereCollider).radius = Radius;
    //    InitializeShipOrbitSlot();
    //    InitializeKeepoutZone();
    //    //D.Log("{0}.Radius set to {1}.", FullName, Radius);
    //}

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

        _displayMgr = gameObject.GetSafeMonoBehaviourComponentInChildren<StarDisplayMgr>();
        _displayMgr.Initialize(trackedItem: this);

        var iconEventListener = _displayMgr.IconEventListener;
        iconEventListener.onHover += (iconGo, isOver) => OnHover(isOver);
        iconEventListener.onClick += (iconGo) => OnClick();
        iconEventListener.onDoubleClick += (iconGo) => OnDoubleClick();
        iconEventListener.onPress += (iconGo, isDown) => OnPress(isDown);

        _subscribers.Add(_displayMgr.SubscribeToPropertyChanged<StarDisplayMgr, bool>(sdm => sdm.InCameraLOS, __OnDisplayMgrInCameraLOSChanged));
        _displayMgr.IconColor = Owner.Color;
        _displayMgr.enabled = true;
    }

    //protected override void InitializeViewMembersOnDiscernible() {
    //    base.InitializeViewMembersOnDiscernible();
    //    InitializeContextMenu(Owner);

    //    _displayMgr = gameObject.GetSafeMonoBehaviourComponentInChildren<StarDisplayMgr>();
    //    _displayMgr.Initialize();

    //    var iconEventListener = _displayMgr.IconEventListener;
    //    iconEventListener.onHover += (iconGo, isOver) => OnHover(isOver);
    //    iconEventListener.onClick += (iconGo) => OnClick();
    //    iconEventListener.onDoubleClick += (iconGo) => OnDoubleClick();
    //    iconEventListener.onPress += (iconGo, isDown) => OnPress(isDown);

    //    _displayMgr.onInCameraLosChanged += (inCameraLOS) => InCameraLOS = inCameraLOS;
    //    _displayMgr.AllowShowing = true;
    //}
    //protected override void InitializeViewMembersOnDiscernible() {
    //    base.InitializeViewMembersOnDiscernible();
    //    InitializeContextMenu(Owner);

    //    var meshRenderer = gameObject.GetComponentInImmediateChildren<MeshRenderer>();
    //    meshRenderer.castShadows = false;
    //    meshRenderer.receiveShadows = false;
    //    meshRenderer.enabled = true;

    //    var glowRenderers = gameObject.GetComponentsInChildren<MeshRenderer>().Except(meshRenderer);
    //    glowRenderers.ForAll(gr => {
    //        gr.castShadows = false;
    //        gr.receiveShadows = false;
    //        gr.enabled = true;
    //    });

    //    var starLight = gameObject.GetComponentInChildren<Light>();
    //    starLight.range = GameManager.Instance.GameSettings.UniverseSize.Radius();
    //    starLight.intensity = 0.5F;
    //    starLight.cullingMask = _starLightCullingMask;
    //    starLight.enabled = true;

    //    _billboard = gameObject.GetSafeMonoBehaviourComponentInChildren<Billboard>();
    //    _billboard.enabled = true;

    //    var animation = gameObject.GetComponentInChildren<Animation>();
    //    animation.cullingType = AnimationCullingType.BasedOnRenderers; // aka, disabled when not visible
    //    animation.enabled = true;
    //    // TODO animation settings and distance controls

    //    // var revolvers = gameObject.GetSafeMonoBehaviourComponentsInChildren<Revolver>();
    //    // revolvers.ForAll(r => r.axisOfRotation = new Vector3(Constants.Zero, Constants.One, Constants.Zero));
    //    // TODO Revolver settings and distance controls, Revolvers control their own enabled state based on visibility

    //    var cameraLosChgdListener = gameObject.GetSafeMonoBehaviourComponentInChildren<CameraLosChangedListener>();
    //    cameraLosChgdListener.onCameraLosChanged += (go, inCameraLOS) => InCameraLOS = inCameraLOS;
    //    cameraLosChgdListener.enabled = true;

    //    InitializeIcon();
    //}

    protected override HudManager InitializeHudManager() {
        var hudManager = new HudManager(Publisher);
        hudManager.AddContentToUpdate(HudManager.UpdatableLabelContentID.IntelState);
        return hudManager;
    }

    private void InitializeContextMenu(Player owner) {
        _ctxControl = new StarCtxControl(this);
    }

    //private void InitializeIcon() {
    //    float minShowDistance = Camera.main.layerCullDistances[(int)Layers.StarCull];
    //    _icon = TrackingWidgetFactory.Instance.CreateInteractableTrackingSprite(this, TrackingWidgetFactory.IconAtlasID.Contextual,
    //        new Vector2(16, 16), WidgetPlacement.Over, minShowDistance);
    //    _icon.Set("Icon01");
    //    ChangeIconColor(Owner.Color);

    //    var cmdIconEventListener = _icon.EventListener;
    //    cmdIconEventListener.onHover += (cmdIconGo, isOver) => OnHover(isOver);
    //    cmdIconEventListener.onClick += (cmdIconGo) => OnClick();
    //    cmdIconEventListener.onDoubleClick += (cmdIconGo) => OnDoubleClick();
    //    cmdIconEventListener.onPress += (cmdIconGo, isDown) => OnPress(isDown);

    //    var cmdIconCameraLosChgdListener = _icon.CameraLosChangedListener;
    //    cmdIconCameraLosChgdListener.onCameraLosChanged += (cmdIconGo, inCameraLOS) => InCameraLOS = inCameraLOS;
    //    cmdIconCameraLosChgdListener.enabled = true;
    //    //D.Log("{0} initialized its Icon.", FullName);
    //    // icon enabled state controlled by _icon.Show()
    //}

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
        if (_displayMgr != null) {
            _displayMgr.IconColor = Owner.Color;
        }
    }
    //protected override void OnOwnerChanged() {
    //    base.OnOwnerChanged();
    //    _displayMgr.ChangeIconColor(Owner.Color);
    //}
    //protected override void OnOwnerChanged() {
    //    base.OnOwnerChanged();
    //    ChangeIconColor(Owner.Color);
    //}

    #endregion

    #region View Methods

    protected override void OnHumanPlayerIntelCoverageChanged() {
        base.OnHumanPlayerIntelCoverageChanged();
        _displayMgr.enabled = GetHumanPlayerIntelCoverage() != IntelCoverage.None;
    }
    //protected override void OnHumanPlayerIntelCoverageChanged() {
    //    base.OnHumanPlayerIntelCoverageChanged();
    //    _displayMgr.AllowShowing = GetHumanPlayerIntelCoverage() != IntelCoverage.None;
    //}

    //protected override void OnIsDiscernibleChanged() {
    //    base.OnIsDiscernibleChanged();
    //    _billboard.enabled = IsDiscernible;
    //    // icon only shows when in front of the camera and beyond the star mesh's culling distance
    //    ShowIcon(!IsDiscernible && UnityUtility.IsWithinCameraViewport(Position));
    //}

    //private void ShowIcon(bool toShow) {
    //    if (_icon != null) {
    //        //D.Log("{0}.ShowIcon({1}) called.", FullName, toShow);
    //        _icon.Show(toShow);
    //    }
    //}

    //private void ChangeIconColor(GameColor color) {
    //    if (_icon != null) {
    //        _icon.Color = color;
    //    }
    //}

    private void __OnDisplayMgrInCameraLOSChanged() {
        InCameraLOS = _displayMgr.InCameraLOS;
    }

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

    // no need to destroy _icon as it is a child of this element

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

}

