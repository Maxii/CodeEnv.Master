// --------------------------------------------------------------------------------------------------------------------
// <copyright>
// Copyright © 2012 - 2014 Strategic Forge
//
// Email: jim@strategicforge.com
// </copyright> 
// <summary> 
// File: StarItem.cs
// COMMENT - one line to give a brief idea of what this file does.
// </summary> 
// -------------------------------------------------------------------------------------------------------------------- 

#define DEBUG_LOG
#define DEBUG_WARN
#define DEBUG_ERROR

// default namespace

using System;
using System.Collections.Generic;
using System.Linq;
using CodeEnv.Master.Common;
using CodeEnv.Master.Common.LocalResources;
using CodeEnv.Master.GameContent;
using UnityEngine;

/// <summary>
/// COMMENT 
/// </summary>
public class StarItem : AItem, IDestinationTarget, IShipOrbitable {


    private static LayerMask _starLightCullingMask = LayerMaskExtensions.CreateInclusiveMask(Layers.Default, Layers.TransparentFX,
    Layers.Ship, Layers.Facility, Layers.Planetoid, Layers.Star);

    public new StarData Data {
        get { return base.Data as StarData; }
        set { base.Data = value; }
    }

    public StarCategory category;
    public float minCameraViewDistanceMultiplier = 2F;
    public float optimalCameraViewDistanceMultiplier = 8F;

    private Billboard _billboard;
    private IViewable _systemView;


    protected override void Awake() {
        base.Awake();
        circleScaleFactor = 1.0F;

        Subscribe();
    }

    protected override void Start() {
        base.Start();
        _systemView = gameObject.GetSafeInterfaceInParents<IViewable>(excludeSelf: true);

        AssessDiscernability(); // needed as FixedIntel gets set early and never changes
    }


    protected override void InitializeRadiiComponents() {
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

    protected override IGuiHudPublisher InitializeHudPublisher() {
        return new GuiHudPublisher<StarData>(Data);
    }

    protected override void InitializeVisualMembers() {
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
        starLight.range = GameManager.Settings.UniverseSize.Radius();
        starLight.intensity = 0.5F;
        starLight.cullingMask = _starLightCullingMask;
        starLight.enabled = true;

        _billboard = gameObject.GetSafeMonoBehaviourComponentInChildren<Billboard>();
        _billboard.enabled = true;

        var animation = gameObject.GetComponentInChildren<Animation>();
        animation.cullingType = AnimationCullingType.BasedOnRenderers; // aka, disabled when not visible
        animation.enabled = true;
        // TODO animation settings and distance controls

        var revolvers = gameObject.GetSafeMonoBehaviourComponentsInChildren<Revolver>();
        revolvers.ForAll(r => r.enabled = true);
        // TODO Revolver settings and distance controls, Revolvers control their own enabled state based on visibility

        var cameraLosChgdListener = gameObject.GetSafeMonoBehaviourComponentInChildren<CameraLosChangedListener>();
        cameraLosChgdListener.onCameraLosChanged += (go, inCameraLOS) => InCameraLOS = inCameraLOS;
        cameraLosChgdListener.enabled = true;
    }

    protected override IIntel InitializePlayerIntel() {
        return new FixedIntel(IntelCoverage.Comprehensive);
    }


    protected override void Initialize() {
        D.Assert(category == Data.Category);
    }

    #region Mouse Events

    protected override void OnLeftClick() {
        base.OnLeftClick();
        if (_systemView.IsDiscernible) {
            (_systemView as ISelectable).IsSelected = true;
        }

    }

    #endregion

    protected override void OnIsDiscernibleChanged() {
        base.OnIsDiscernibleChanged();
        _billboard.enabled = IsDiscernible;
    }

    protected override void SubscribeToPlayerIntelCoverageChanged() {
        // no reason to subscribe as Coverage does not change
    }

    public override string ToString() {
        return new ObjectAnalyzer().ToString(this);
    }

    #region IDestinationTarget Members

    public SpaceTopography Topography { get { return Data.Topography; } }

    #endregion

    #region IShipOrbitable Members

    public ShipOrbitSlot ShipOrbitSlot { get; private set; }

    #endregion

    #region ICameraTargetable Members

    public override float MinimumCameraViewingDistance { get { return Radius * minCameraViewDistanceMultiplier; } }

    #endregion

    #region ICameraFocusable Members

    public override float OptimalCameraViewingDistance { get { return Radius * optimalCameraViewDistanceMultiplier; } }

    #endregion

}

