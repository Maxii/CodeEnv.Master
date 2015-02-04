﻿// --------------------------------------------------------------------------------------------------------------------
// <copyright>
// Copyright © 2012 - 2014 Strategic Forge
//
// Email: jim@strategicforge.com
// </copyright> 
// <summary> 
// File: StarItem.cs
// Item class for Stars.
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
/// Item class for Stars.
/// </summary>
public class StarItem : AItem, IShipOrbitable {

    private static LayerMask _starLightCullingMask = LayerMaskExtensions.CreateInclusiveMask(Layers.Default, Layers.TransparentFX,
    Layers.Ship, Layers.Facility, Layers.Planetoid, Layers.Star);

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

    public override bool IsHudShowing {
        get { return _hudManager != null && _hudManager.IsHudShowing; }
    }

    protected override float ItemTypeCircleScale { get { return 1.5F; } }

    private HudManager<StarPublisher> _hudManager;
    private Billboard _billboard;
    private SystemItem _system;
    private ICtxControl _ctxControl;

    #region Initialization

    protected override void InitializeLocalReferencesAndValues() {
        base.InitializeLocalReferencesAndValues();
        var meshRenderer = gameObject.GetComponentInImmediateChildren<Renderer>();
        Radius = meshRenderer.bounds.size.x / 2F;    // half of the (length, width or height, all the same surrounding a sphere)
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
    }

    protected override void InitializeViewMembers() {
        base.InitializeViewMembers();
        _system = gameObject.GetSafeMonoBehaviourComponentInParents<SystemItem>();
        AssessDiscernability(); // needed as FixedIntel gets set early and never changes
    }

    //protected override IGuiHudPublisher InitializeHudPublisher() {
    //    return new GuiHudPublisher<StarData>(Data);
    //}

    protected override void InitializeViewMembersOnDiscernible() {
        base.InitializeViewMembersOnDiscernible();
        InitializeContextMenu(Owner);

        var meshRenderer = gameObject.GetComponentInImmediateChildren<MeshRenderer>();
        meshRenderer.castShadows = false;
        meshRenderer.receiveShadows = false;
        meshRenderer.enabled = true;

        var glowRenderers = gameObject.GetComponentsInChildren<MeshRenderer>().Except(meshRenderer);
        glowRenderers.ForAll(gr => {
            gr.castShadows = false;
            gr.receiveShadows = false;
            gr.enabled = true;
        });

        var starLight = gameObject.GetComponentInChildren<Light>();
        starLight.range = GameManager.Instance.GameSettings.UniverseSize.Radius();
        starLight.intensity = 0.5F;
        starLight.cullingMask = _starLightCullingMask;
        starLight.enabled = true;

        _billboard = gameObject.GetSafeMonoBehaviourComponentInChildren<Billboard>();
        _billboard.enabled = true;

        var animation = gameObject.GetComponentInChildren<Animation>();
        animation.cullingType = AnimationCullingType.BasedOnRenderers; // aka, disabled when not visible
        animation.enabled = true;
        // TODO animation settings and distance controls

        // var revolvers = gameObject.GetSafeMonoBehaviourComponentsInChildren<Revolver>();
        // revolvers.ForAll(r => r.axisOfRotation = new Vector3(Constants.Zero, Constants.One, Constants.Zero));
        // TODO Revolver settings and distance controls, Revolvers control their own enabled state based on visibility

        var cameraLosChgdListener = gameObject.GetSafeMonoBehaviourComponentInChildren<CameraLosChangedListener>();
        cameraLosChgdListener.onCameraLosChanged += (go, inCameraLOS) => InCameraLOS = inCameraLOS;
        cameraLosChgdListener.enabled = true;
    }

    protected override void InitializeHudPublisher() {
        _hudManager = new HudManager<StarPublisher>(Publisher);
    }

    private void InitializeContextMenu(Player owner) {
        _ctxControl = new StarCtxControl(this);
    }

    #endregion

    #region Model Methods

    public StarReport GetReport(Player player) { return Publisher.GetReport(player); }

    protected override void OnOwnerChanging(Player newOwner) {
        base.OnOwnerChanging(newOwner);
        // there is only 1 type of ContextMenu for Stars so no need to generate a new one
    }

    #endregion

    #region View Methods

    public override void ShowHud(bool toShow) {
        if (_hudManager != null) {
            if (toShow) {
                _hudManager.Show(Position);
            }
            else {
                _hudManager.Hide();
            }
        }
    }

    protected override void OnIsDiscernibleChanged() {
        base.OnIsDiscernibleChanged();
        _billboard.enabled = IsDiscernible;
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

    //private HudPublisher<StarPublisher> _hudPublisher3;
    //public HudPublisher<StarPublisher> HudPublisher3 {
    //    get { return _hudPublisher3 = _hudPublisher3 ?? new HudPublisher<StarPublisher>(Publisher); }
    //}

    //private StarHudPublisher _hudPublisher2;
    //public StarHudPublisher HudPublisher2 {
    //    get { return _hudPublisher2 = _hudPublisher2 ?? new StarHudPublisher(Publisher); }
    //}

    //private StarPublisher _publisher;
    //public StarPublisher Publisher {
    //    get { return _publisher = _publisher ?? new StarPublisher(Data); }
    //}
    //private StarHudPublisher2 _reportGenerator;
    //public StarHudPublisher2 ReportGenerator {
    //    get {
    //        return _reportGenerator = _reportGenerator ?? new StarHudPublisher2(Data);
    //    }
    //}
    //private StarReportGenerator _reportGenerator;
    //public StarReportGenerator ReportGenerator {
    //    get {
    //        return _reportGenerator = _reportGenerator ?? new StarReportGenerator(Data);
    //    }
    //}


    //public StarReport GetReport(Player player) {
    //    return Publisher.GetReport(player);
    //}
    //public StarReport GetReport(Player player) {
    //    return ReportGenerator.GetReport(player);
    //}
    //public StarReport GetReport(Player player) {
    //    return ReportGenerator.GetReport(player, Data.GetPlayerIntel(player));
    //}

    //protected override void OnHover(bool isOver) {
    //    HudPublisher3.ShowHud(isOver, Position);
    //}
    //protected override void OnHover(bool isOver) {
    //    HudPublisher2.ShowHud(isOver, Position);
    //}
    //protected override void OnHover(bool isOver) {
    //        ReportGenerator.ShowHud(isOver, Position);
    //}
    //protected override void OnHover(bool isOver) {
    //    if (isOver) {
    //        string hudText = ReportGenerator.GetCursorHudText(Data.GetHumanPlayerIntel());
    //        //string hudText = _starReportGenerator.GetCursorHudText(IntelCoverage.Comprehensive);
    //        GuiCursorHud.Instance.Set(hudText, Position);
    //    }
    //    else {
    //        GuiCursorHud.Instance.Clear();
    //    }
    //}

    #endregion

    #region Cleanup

    protected override void Cleanup() {
        base.Cleanup();
        if (_ctxControl != null) {
            (_ctxControl as IDisposable).Dispose();
        }

        if (_hudManager != null) {
            _hudManager.Dispose();
        }
    }

    #endregion

    public override string ToString() {
        return new ObjectAnalyzer().ToString(this);
    }

    #region INavigableTarget Members

    public override bool IsMobile { get { return false; } }

    #endregion

    #region IShipOrbitable Members

    public ShipOrbitSlot ShipOrbitSlot { get; private set; }

    #endregion

    #region ICameraTargetable Members

    public override float MinimumCameraViewingDistance { get { return Radius * minViewDistanceFactor; } }

    #endregion

    #region ICameraFocusable Members

    public override float OptimalCameraViewingDistance { get { return Radius * optViewDistanceFactor; } }

    #endregion

}

