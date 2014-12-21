// --------------------------------------------------------------------------------------------------------------------
// <copyright>
// Copyright © 2012 - 2014 Strategic Forge
//
// Email: jim@strategicforge.com
// </copyright> 
// <summary> 
// File: UniverseCenterItem.cs
// Item class for the Universe Center.
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
/// Item class for the Universe Center.
/// </summary>
public class UniverseCenterItem : AItem, IShipOrbitable {

    public new UniverseCenterData Data {
        get { return base.Data as UniverseCenterData; }
        set { base.Data = value; }
    }

    [Range(0.5F, 3.0F)]
    [Tooltip("Minimum Camera View Distance Multiplier")]
    public float minViewDistanceFactor = 2F;

    private ICtxControl _ctxControl;

    #region Initialization

    protected override void InitializeLocalReferencesAndValues() {
        base.InitializeLocalReferencesAndValues();
        var meshRenderer = gameObject.GetComponentInImmediateChildren<Renderer>();
        Radius = meshRenderer.bounds.size.x / 2F;    // half of the (length, width or height, all the same surrounding a sphere)
        D.Assert(Mathfx.Approx(Radius, TempGameValues.UniverseCenterRadius, 1F));    // 50
        collider.isTrigger = false;
        (collider as SphereCollider).radius = Radius;
        InitializeShipOrbitSlot();
        InitializeKeepoutZone();
    }

    protected override AIntel InitializePlayerIntel() { return new FixedIntel(IntelCoverage.Comprehensive); }

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

    protected override void InitializeModelMembers() { }

    protected override void InitializeViewMembers() {
        base.InitializeViewMembers();
        AssessDiscernability(); // needed as FixedIntel gets set early and never changes
    }

    protected override IGuiHudPublisher InitializeHudPublisher() {
        return new GuiHudPublisher<UniverseCenterData>(Data);
    }

    protected override void InitializeViewMembersOnDiscernible() {
        InitializeContextMenu(Owner);

        var meshRenderer = gameObject.GetComponentInChildren<MeshRenderer>();
        meshRenderer.castShadows = false;
        meshRenderer.receiveShadows = false;
        meshRenderer.enabled = true;

        var animation = meshRenderer.gameObject.GetComponent<Animation>();
        animation.cullingType = AnimationCullingType.BasedOnRenderers; // aka, disabled when not visible
        animation.enabled = true;

        var cameraLosChgdListener = gameObject.GetSafeInterfaceInChildren<ICameraLosChangedListener>();
        cameraLosChgdListener.onCameraLosChanged += (go, inCameraLOS) => InCameraLOS = inCameraLOS;
        cameraLosChgdListener.enabled = true;
    }

    private void InitializeContextMenu(Player owner) {
        _ctxControl = new UniverseCenterCtxControl(this);
    }

    #endregion

    #region Model Methods

    protected override void OnOwnerChanged() {
        throw new System.NotSupportedException("{0}.Owner is not allowed to change.".Inject(GetType().Name));
    }

    #endregion

    #region View Methods
    #endregion

    #region Mouse Events

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
    }

    #endregion

    public override string ToString() {
        return new ObjectAnalyzer().ToString(this);
    }

    #region IDestinationTarget Members

    public override bool IsMobile { get { return false; } }

    #endregion

    #region IShipOrbitable Members

    public ShipOrbitSlot ShipOrbitSlot { get; private set; }

    #endregion

    #region ICameraTargetable Members

    public override float MinimumCameraViewingDistance { get { return Radius * minViewDistanceFactor; } }

    #endregion

    #region ICameraFocusable Members

    public override bool IsRetainedFocusEligible { get { return true; } }

    public override float OptimalCameraViewingDistance { get { return gameObject.DistanceToCamera(); } }

    #endregion

}

