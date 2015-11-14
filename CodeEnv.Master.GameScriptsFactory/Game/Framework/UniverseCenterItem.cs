// --------------------------------------------------------------------------------------------------------------------
// <copyright>
// Copyright © 2012 - 2014 Strategic Forge
//
// Email: jim@strategicforge.com
// </copyright> 
// <summary> 
// File: UniverseCenterItem.cs
// Class for the ADiscernibleItem that is the UniverseCenter.
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
/// Class for the ADiscernibleItem that is the UniverseCenter.
/// </summary>
public class UniverseCenterItem : AIntelItem, IUniverseCenterItem, IShipOrbitable, ISensorDetectable {

    [Range(1.0F, 3.0F)]
    [Tooltip("Minimum Camera View Distance Multiplier")]
    [SerializeField]
    private float _minViewDistanceFactor = 2F;

    public new UniverseCenterData Data {
        get { return base.Data as UniverseCenterData; }
        set { base.Data = value; }
    }

    private UniverseCenterPublisher _publisher;
    public UniverseCenterPublisher Publisher {
        get { return _publisher = _publisher ?? new UniverseCenterPublisher(Data, this); }
    }

    private DetectionHandler _detectionHandler;
    private ICtxControl _ctxControl;
    private SphereCollider _collider;

    #region Initialization

    protected override void InitializeLocalReferencesAndValues() {
        base.InitializeLocalReferencesAndValues();
        var meshRenderer = gameObject.GetSingleComponentInChildren<MeshRenderer>();
        Radius = meshRenderer.bounds.size.x / 2F;    // half of the (length, width or height, all the same surrounding a sphere)
        D.Assert(Mathfx.Approx(Radius, TempGameValues.UniverseCenterRadius, 1F));    // 50
        _collider = UnityUtility.ValidateComponentPresence<SphereCollider>(gameObject);
        _collider.enabled = false;
        _collider.isTrigger = false;
        _collider.radius = Radius;
        InitializeKeepoutZone();
        InitializeShipOrbitSlot();
    }

    private void InitializeKeepoutZone() {
        SphereCollider keepoutZoneCollider = gameObject.GetSingleComponentInChildren<SphereCollider>(excludeSelf: true);
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
        _ctxControl = new UniverseCenterCtxControl(this);
    }

    protected override ADisplayManager InitializeDisplayManager() {
        return new UniverseCenterDisplayManager(gameObject);
    }

    #endregion

    #region Model Methods

    public override void CommenceOperations() {
        base.CommenceOperations();
        _collider.enabled = true;
    }

    public UniverseCenterReport GetUserReport() { return Publisher.GetUserReport(); }

    public UniverseCenterReport GetReport(Player player) { return Publisher.GetReport(player); }

    protected override float InitializeOptimalCameraViewingDistance() {
        return gameObject.DistanceToCamera();
    }

    protected override void OnOwnerChanged() {
        throw new System.NotSupportedException("{0}.Owner is not allowed to change.".Inject(GetType().Name));
    }

    #endregion

    #region View Methods

    #endregion

    #region Events

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

    #endregion

    public override string ToString() {
        return new ObjectAnalyzer().ToString(this);
    }

    #region IShipOrbitable Members

    public float KeepoutRadius { get; private set; }

    public ShipOrbitSlot ShipOrbitSlot { get; private set; }

    #endregion

    #region ICameraTargetable Members

    public override float MinimumCameraViewingDistance { get { return Radius * _minViewDistanceFactor; } }

    #endregion

    #region ICameraFocusable Members

    public override bool IsRetainedFocusEligible { get { return true; } }

    #endregion

    #region IDetectable Members

    public void OnDetection(IUnitCmdItem cmdItem, RangeCategory sensorRange) {
        _detectionHandler.OnDetection(cmdItem, sensorRange);
    }

    public void OnDetectionLost(IUnitCmdItem cmdItem, RangeCategory sensorRange) {
        _detectionHandler.OnDetectionLost(cmdItem, sensorRange);
    }

    #endregion

}

