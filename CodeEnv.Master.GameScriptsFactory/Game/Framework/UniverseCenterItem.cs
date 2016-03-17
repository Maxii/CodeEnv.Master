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
using System.Collections.Generic;
using CodeEnv.Master.Common;
using CodeEnv.Master.GameContent;
using UnityEngine;

/// <summary>
/// Class for the ADiscernibleItem that is the UniverseCenter.
/// </summary>
public class UniverseCenterItem : AIntelItem, IUniverseCenterItem, IShipOrbitable, ISensorDetectable, IAvoidableObstacle,
    IPatrollable, IFleetExplorable, IShipExplorable, IGuardable {

    public new UniverseCenterData Data {
        get { return base.Data as UniverseCenterData; }
        set { base.Data = value; }
    }

    public override float Radius { get { return Data.Radius; } }

    private UniverseCenterPublisher _publisher;
    public UniverseCenterPublisher Publisher {
        get { return _publisher = _publisher ?? new UniverseCenterPublisher(Data, this); }
    }

    private DetectionHandler _detectionHandler;
    private SphereCollider _primaryCollider;
    private SphereCollider _obstacleZoneCollider;
    private DetourGenerator _detourGenerator;

    #region Initialization

    protected override void InitializeOnData() {
        base.InitializeOnData();
        InitializePrimaryCollider();
        InitializeObstacleZone();
        _detectionHandler = new DetectionHandler(this);
    }

    private void InitializePrimaryCollider() {
        _primaryCollider = UnityUtility.ValidateComponentPresence<SphereCollider>(gameObject);
        _primaryCollider.enabled = false;
        _primaryCollider.isTrigger = false;
        _primaryCollider.radius = Data.Radius;
    }

    private void InitializeObstacleZone() {
        _obstacleZoneCollider = gameObject.GetSingleComponentInChildren<SphereCollider>(excludeSelf: true);
        D.Assert(_obstacleZoneCollider.gameObject.layer == (int)Layers.AvoidableObstacleZone);
        _obstacleZoneCollider.enabled = false;
        _obstacleZoneCollider.isTrigger = true;
        _obstacleZoneCollider.radius = Data.LowOrbitRadius;
        // Static trigger collider (no rigidbody) is OK as the ship's CollisionDetectionZone collider has a kinematic rigidbody
        D.Warn(_obstacleZoneCollider.gameObject.GetComponent<Rigidbody>() != null, "{0}.ObstacleZone has a Rigidbody it doesn't need.", FullName);
        Vector3 obstacleZoneCenter = Position + _obstacleZoneCollider.center;
        _detourGenerator = new DetourGenerator(obstacleZoneCenter, _obstacleZoneCollider.radius, Data.HighOrbitRadius);
        InitializeDebugShowObstacleZone();
    }

    protected override ItemHudManager InitializeHudManager() {
        return new ItemHudManager(Publisher);
    }

    protected override ICtxControl InitializeContextMenu(Player owner) {
        return new UniverseCenterCtxControl(this);
    }

    protected override ADisplayManager InitializeDisplayManager() {
        return new UniverseCenterDisplayManager(gameObject);
    }

    private IList<StationaryLocation> InitializePatrolStations() {
        float radiusOfSphereContainingPatrolStations = Data.HighOrbitRadius * 2F;
        var stationLocations = MyMath.CalcVerticesOfInscribedBoxInsideSphere(Position, radiusOfSphereContainingPatrolStations);
        var patrolStations = new List<StationaryLocation>(8);
        foreach (Vector3 loc in stationLocations) {
            patrolStations.Add(new StationaryLocation(loc));
        }
        return patrolStations;
    }

    private IList<StationaryLocation> InitializeGuardStations() {
        var guardStations = new List<StationaryLocation>(2);
        float distanceFromPosition = Data.HighOrbitRadius * 2F; // HACK
        var localPointAbovePosition = new Vector3(Constants.ZeroF, distanceFromPosition, Constants.ZeroF);
        var localPointBelowPosition = new Vector3(Constants.ZeroF, -distanceFromPosition, Constants.ZeroF);
        guardStations.Add(new StationaryLocation(Position + localPointAbovePosition));
        guardStations.Add(new StationaryLocation(Position + localPointBelowPosition));
        return guardStations;
    }


    private ShipOrbitSlot InitializeShipOrbitSlot() {
        return new ShipOrbitSlot(Data.LowOrbitRadius, Data.HighOrbitRadius, this);
    }

    #endregion

    public override void CommenceOperations() {
        base.CommenceOperations();
        _primaryCollider.enabled = true;
        _obstacleZoneCollider.enabled = true;
    }

    public UniverseCenterReport GetUserReport() { return Publisher.GetUserReport(); }

    public UniverseCenterReport GetReport(Player player) { return Publisher.GetReport(player); }

    protected override void ShowSelectedItemHud() {
        SelectedItemHudWindow.Instance.Show(FormID.SelectedUniverseCenter, GetUserReport());
    }

    #region Event and Property Change Handlers

    protected override void OwnerPropChangingHandler(Player newOwner) {
        throw new System.NotSupportedException("{0}.Owner is not allowed to change.".Inject(GetType().Name));
    }

    protected override void OwnerPropChangedHandler() {
        throw new System.NotSupportedException("{0}.Owner is not allowed to change.".Inject(GetType().Name));
    }

    #endregion

    #region Cleanup

    protected override void Cleanup() {
        base.Cleanup();
        if (_detectionHandler != null) {
            _detectionHandler.Dispose();
        }
        CleanupDebugShowObstacleZone();
    }

    #endregion

    public override string ToString() {
        return new ObjectAnalyzer().ToString(this);
    }

    #region Debug Show Obstacle Zones

    private void InitializeDebugShowObstacleZone() {
        DebugValues debugValues = DebugValues.Instance;
        debugValues.showObstacleZonesChanged += ShowDebugObstacleZonesChangedEventHandler;
        if (debugValues.ShowObstacleZones) {
            EnableDebugShowObstacleZone(true);
        }
    }

    private void EnableDebugShowObstacleZone(bool toEnable) {
        DrawColliderGizmo drawCntl = _obstacleZoneCollider.gameObject.AddMissingComponent<DrawColliderGizmo>();
        drawCntl.Color = Color.red;
        drawCntl.enabled = toEnable;
    }

    private void ShowDebugObstacleZonesChangedEventHandler(object sender, EventArgs e) {
        EnableDebugShowObstacleZone(DebugValues.Instance.ShowObstacleZones);
    }

    private void CleanupDebugShowObstacleZone() {
        var debugValues = DebugValues.Instance;
        if (debugValues != null) {
            debugValues.showObstacleZonesChanged -= ShowDebugObstacleZonesChangedEventHandler;
        }
        DrawColliderGizmo drawCntl = _obstacleZoneCollider.gameObject.GetComponent<DrawColliderGizmo>();
        if (drawCntl != null) {
            Destroy(drawCntl);
        }
    }

    #endregion

    #region IShipOrbitable Members

    private ShipOrbitSlot _shipOrbitSlot;
    public ShipOrbitSlot ShipOrbitSlot {
        get {
            if (_shipOrbitSlot == null) { _shipOrbitSlot = InitializeShipOrbitSlot(); }
            return _shipOrbitSlot;
        }
    }

    public IList<StationaryLocation> LocalAssemblyStations { get { return GuardStations; } }

    public bool IsOrbitingAllowedBy(Player player) {
        return !Owner.IsAtWarWith(player);
    }

    #endregion

    #region ICameraFocusable Members

    public override bool IsRetainedFocusEligible { get { return true; } }

    #endregion

    #region IDetectable Members

    public void HandleDetectionBy(IUnitCmdItem cmdItem, RangeCategory sensorRange) {
        _detectionHandler.HandleDetectionBy(cmdItem, sensorRange);
    }

    public void HandleDetectionLostBy(IUnitCmdItem cmdItem, RangeCategory sensorRange) {
        _detectionHandler.HandleDetectionLostBy(cmdItem, sensorRange);
    }

    #endregion

    #region INavigableTarget Members

    public override float RadiusAroundTargetContainingKnownObstacles { get { return _obstacleZoneCollider.radius; } }

    public override float GetShipArrivalDistance(float shipCollisionAvoidanceRadius) {
        return Data.HighOrbitRadius + shipCollisionAvoidanceRadius; // OPTIMIZE want shipRadius value as AvoidableObstacleZone ends at LowOrbitRadius?
    }

    #endregion

    #region IAvoidableObstacle Members

    public Vector3 GetDetour(Vector3 shipOrFleetPosition, RaycastHit zoneHitInfo, float fleetRadius, Vector3 formationOffset) {
        return _detourGenerator.GenerateDetourFromObstacleZoneHit(shipOrFleetPosition, zoneHitInfo.point, fleetRadius, formationOffset);
    }

    #endregion

    #region IHighlightable Members

    public override float HoverHighlightRadius { get { return Radius + 10F; } }

    #endregion

    #region IPatrollable Members

    private IList<StationaryLocation> _patrolStations;
    public IList<StationaryLocation> PatrolStations {
        get {
            if (_patrolStations == null) {
                _patrolStations = InitializePatrolStations();
            }
            return new List<StationaryLocation>(_patrolStations);
        }
    }

    // EmergencyGatherStations - see IShipOrbitable

    public bool IsPatrollingAllowedBy(Player player) {
        return !player.IsEnemyOf(Owner);
    }

    #endregion

    #region IGuardable

    private IList<StationaryLocation> _guardStations;
    public IList<StationaryLocation> GuardStations {
        get {
            if (_guardStations == null) {
                _guardStations = InitializeGuardStations();
            }
            return new List<StationaryLocation>(_guardStations);
        }
    }

    public bool IsGuardingAllowedBy(Player player) {
        return !player.IsEnemyOf(Owner);
    }

    #endregion

    #region IFleetExplorable, IShipExplorable Members

    public bool IsFullyExploredBy(Player player) {
        return GetIntelCoverage(player) == IntelCoverage.Comprehensive;
    }

    // EmergencyGatherStations - see IShipOrbitable

    public bool IsExploringAllowedBy(Player player) {
        // currently owner can only be NoPlayer which by definition is not at war with anyone
        return !Owner.IsAtWarWith(player);
    }

    public void RecordExplorationCompletedBy(Player player) {
        SetIntelCoverage(player, IntelCoverage.Comprehensive);
    }

    #endregion


}

