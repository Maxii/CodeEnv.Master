// --------------------------------------------------------------------------------------------------------------------
// <copyright>
// Copyright © 2012 - 2014 Strategic Forge
//
// Email: jim@strategicforge.com
// </copyright> 
// <summary> 
// File: SystemItem.cs
// Class for ADiscernibleItems that are Systems.
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
using CodeEnv.Master.GameContent;
using UnityEngine;

/// <summary>
/// Class for ADiscernibleItems that are Systems.
/// </summary>
public class SystemItem : ADiscernibleItem, ISystemItem, IZoomToFurthest, IPatrollable, IFleetExplorable {

    public bool IsTrackingLabelEnabled { private get; set; }

    /// <summary>
    ///  The orbit slot within this system that any current or future settlement can occupy. 
    /// </summary>
    public CelestialOrbitSlot SettlementOrbitSlot { get; set; }

    public new SystemData Data {
        get { return base.Data as SystemData; }
        set { base.Data = value; }
    }

    private SettlementCmdItem _settlement;
    public SettlementCmdItem Settlement {
        get { return _settlement; }
        set {
            if (_settlement != null && value != null) {
                D.Error("{0} cannot assign {1} when {2} is already present.", FullName, value.FullName, _settlement.FullName);
            }
            SetProperty<SettlementCmdItem>(ref _settlement, value, "Settlement", SettlementPropChangedHandler);
        }
    }

    private StarItem _star;
    public StarItem Star {
        get { return _star; }
        set {
            D.Assert(_star == null, "{0}'s Star can only be set once.".Inject(FullName));
            _star = value;
            StarPropSetHandler();
        }
    }

    public IList<PlanetItem> Planets { get; private set; }

    public IList<MoonItem> Moons { get; private set; }

    public IList<APlanetoidItem> Planetoids { get; private set; }

    private SystemPublisher _publisher;
    public SystemPublisher Publisher {
        get { return _publisher = _publisher ?? new SystemPublisher(Data, this); }
    }

    public override float Radius { get { return Data.Radius; } }

    public Index3D SectorIndex { get { return Data.SectorIndex; } }

    private ITrackingWidget _trackingLabel;
    private MeshCollider _orbitalPlaneCollider;

    #region Initialization

    protected override void InitializeOnAwake() {
        base.InitializeOnAwake();
        Planetoids = new List<APlanetoidItem>();    // OPTIMIZE size of each of these lists
        Planets = new List<PlanetItem>();
        Moons = new List<MoonItem>();
        // there is no collider associated with a SystemItem implementation. The collider used for interaction is located on the orbital plane
    }

    protected override void InitializeOnData() {
        // no primary collider that needs data, no ship transit ban zone, no ship orbit slot
    }

    protected override void InitializeOnFirstDiscernibleToUser() {
        base.InitializeOnFirstDiscernibleToUser();
        __InitializeOrbitalPlaneMeshCollider();
    }

    private void __InitializeOrbitalPlaneMeshCollider() {
        _orbitalPlaneCollider = gameObject.GetComponentInChildren<MeshCollider>();
        _orbitalPlaneCollider.convex = true;    // must preceed isTrigger = true as Trigger's aren't supported on concave meshColliders
        _orbitalPlaneCollider.isTrigger = true;
        _orbitalPlaneCollider.enabled = true;
    }

    protected override ItemHudManager InitializeHudManager() {
        return new ItemHudManager(Publisher);
    }

    protected override ICtxControl InitializeContextMenu(Player owner) {
        ICtxControl ctxControl;
        if (owner == TempGameValues.NoPlayer) {
            ctxControl = new SystemCtxControl(this);
        }
        else {
            ctxControl = owner.IsUser ? new SystemCtxControl_User(this) as ICtxControl : new SystemCtxControl_AI(this);
        }
        return ctxControl;
    }

    private ITrackingWidget InitializeTrackingLabel() {
        float minShowDistance = TempGameValues.MinTrackingLabelShowDistance;
        var trackingLabel = TrackingWidgetFactory.Instance.MakeUITrackingLabel(this, WidgetPlacement.Above, minShowDistance);
        trackingLabel.Set(DisplayName);
        trackingLabel.Color = Owner.Color;
        return trackingLabel;
    }

    protected override ADisplayManager InitializeDisplayManager() {
        return new SystemDisplayManager(gameObject);
    }

    private IList<StationaryLocation> InitializePatrolPoints() {
        float radiusOfSphereContainingPatrolPoints = Radius / 2F;
        var points = MyMath.CalcVerticesOfInscribedBoxInsideSphere(Position, radiusOfSphereContainingPatrolPoints);
        var patrolPoints = new List<StationaryLocation>(8);
        foreach (Vector3 point in points) {
            patrolPoints.Add(new StationaryLocation(point));
        }
        return patrolPoints;
    }

    #endregion

    public override void CommenceOperations() {
        base.CommenceOperations();
        if (Settlement != null) {
            SettlementOrbitSlot.OrbitSimulator.IsActivated = true;
        }
    }

    public void AddPlanetoid(APlanetoidItem planetoid) {
        D.Assert(!planetoid.IsOperational);
        Planetoids.Add(planetoid);
        var planet = planetoid as PlanetItem;
        if (planet != null) {
            Planets.Add(planet);
        }
        else {
            Moons.Add(planetoid as MoonItem);
        }
        Data.AddPlanetoid(planetoid.Data);
    }

    public void RemovePlanetoid(APlanetoidItem planetoid) {
        D.Assert(!planetoid.IsOperational);
        bool isRemoved = Planetoids.Remove(planetoid);
        var planet = planetoid as PlanetItem;
        if (planet != null) {
            isRemoved = isRemoved & Planets.Remove(planet);
        }
        else {
            isRemoved = isRemoved & Moons.Remove(planetoid as MoonItem);
        }
        isRemoved = isRemoved & Data.RemovePlanetoid(planetoid.Data);
        D.Assert(isRemoved);
    }

    public SystemReport GetUserReport() { return Publisher.GetUserReport(); }

    public SystemReport GetReport(Player player) { return Publisher.GetReport(player); }

    public StarReport GetStarReport(Player player) { return Star.GetReport(player); }

    public PlanetoidReport[] GetPlanetoidReports(Player player) {
        return Planetoids.Select(p => p.GetReport(player)).ToArray();
    }

    /// <summary>
    /// Gets the settlement report if a settlement is present. Can be null.
    /// </summary>
    /// <param name="player">The player.</param>
    /// <returns></returns>
    public SettlementReport GetSettlementReport(Player player) {
        return Settlement != null ? Settlement.GetReport(player) : null;
    }

    private void AttachSettlement(SettlementCmdItem settlementCmd) {
        Transform settlementUnit = settlementCmd.UnitContainer;
        SettlementOrbitSlot.AssumeOrbit(settlementUnit);
        if (IsOperational) { // don't activate until operational, otherwise Assert(IsRunning) will fail in AOrbitSlot
            SettlementOrbitSlot.OrbitSimulator.IsActivated = true;
        }
        //D.Log("{0} has been deployed to {1}.", settlementCmd.DisplayName, FullName);
    }

    protected override void AssessIsDiscernibleToUser() {
        // all players including User are now aware of the existence of all systems just like stars
        var isInMainCameraLOS = DisplayMgr != null ? DisplayMgr.IsInMainCameraLOS : true;
        IsDiscernibleToUser = isInMainCameraLOS;
    }

    protected override void ShowSelectedItemHud() {
        SelectedItemHudWindow.Instance.Show(FormID.SelectedSystem, GetUserReport());
    }

    private void ShowTrackingLabel(bool toShow) {
        if (IsTrackingLabelEnabled) {
            _trackingLabel = _trackingLabel ?? InitializeTrackingLabel();
            _trackingLabel.Show(toShow);
        }
    }

    #region Event and Property Change Handlers

    private void StarPropSetHandler() {
        Data.StarData = Star.Data;
    }

    private void SettlementPropChangedHandler() {
        if (Settlement != null) {
            Settlement.ParentSystem = this;
            Data.SettlementData = Settlement.Data;
            AttachSettlement(Settlement);
        }
        else {
            // The existing Settlement has been destroyed, so cleanup the orbit slot in prep for a future Settlement
            // Choose to deactivate or destroy but not both
            //SettlementOrbitSlot.OrbitSimulator.IsActivated = false;   // TODO make a choice between the two
            SettlementOrbitSlot.DestroyOrbitSimulator();
            Data.SettlementData = null;
        }
        // The owner of a system and all it's celestial objects is determined by the ownership of the Settlement, if any
    }

    protected override void OwnerPropChangedHandler() {
        base.OwnerPropChangedHandler();
        if (_trackingLabel != null) {
            _trackingLabel.Color = Owner.Color;
        }
    }

    protected override void IsDiscernibleToUserPropChangedHandler() {
        base.IsDiscernibleToUserPropChangedHandler();
        ShowTrackingLabel(IsDiscernibleToUser);
        _orbitalPlaneCollider.enabled = IsDiscernibleToUser;
    }

    #endregion

    #region Cleanup

    protected override void Cleanup() {
        base.Cleanup();
        UnityUtility.DestroyIfNotNullOrAlreadyDestroyed(_trackingLabel);
        Data.Dispose();
    }

    #endregion

    public override string ToString() {
        return new ObjectAnalyzer().ToString(this);
    }

    #region ICameraFocusable Members

    public override bool IsRetainedFocusEligible { get { return true; } }

    #endregion

    #region IHighlightable Members

    public override float HighlightRadius { get { return Radius * Screen.height * 1F; } }

    #endregion

    #region INavigableTarget Members

    public override float RadiusAroundTargetContainingKnownObstacles { get { return Radius; } }

    public override float GetShipArrivalDistance(float shipCollisionAvoidanceRadius) {
        return Radius + shipCollisionAvoidanceRadius;   // keeps ship outside of gravity well, aka Topography.System
    }

    #endregion

    #region IPatrollable Members

    private IList<StationaryLocation> _patrolPoints;
    public IList<StationaryLocation> PatrolPoints {
        get {
            if (_patrolPoints == null) {
                _patrolPoints = InitializePatrolPoints();
            }
            return new List<StationaryLocation>(_patrolPoints);
        }
    }

    #endregion

    #region IFleetExplorable Members

    public bool IsFullyExploredBy(Player player) {
        bool isStarExplored = Star.IsFullyExploredBy(player);
        bool areAllPlanetsExplored = Planets.All(p => p.IsFullyExploredBy(player));
        return isStarExplored && areAllPlanetsExplored;
    }

    public bool IsExplorationAllowedBy(Player player) {
        return !Owner.IsAtWarWith(player);
    }

    #endregion

}

