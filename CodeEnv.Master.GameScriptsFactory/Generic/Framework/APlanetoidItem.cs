// --------------------------------------------------------------------------------------------------------------------
// <copyright>
// Copyright © 2012 - 2014 Strategic Forge
//
// Email: jim@strategicforge.com
// </copyright> 
// <summary> 
// File: APlanetoidItem.cs
// Abstract class for AMortalItems that are Planetoid (Planet and Moon) Items.
// </summary> 
// -------------------------------------------------------------------------------------------------------------------- 

////#define DEBUG_LOG
////#define DEBUG_WARN
////#define DEBUG_ERROR

// default namespace

using System;
using System.Collections.Generic;
using System.Linq;
using CodeEnv.Master.Common;
using CodeEnv.Master.Common.LocalResources;
using CodeEnv.Master.GameContent;
using UnityEngine;
using UnityEngine.Profiling;

/// <summary>
/// Abstract class for AMortalItems that are Planetoid (Planet and Moon) Items.
/// </summary>
public abstract class APlanetoidItem : AMortalItem, IPlanetoid, IPlanetoid_Ltd, ICameraFollowable, IFleetNavigableDestination, /*IUnitAttackable,*/
    IShipBombardable, ISensorDetectable, IAvoidableObstacle, IShipOrbitable {

    // 8.21.16 Removed static, unused MaxOrbitalSpeed as not worthwhile maintaining once SystemCreator static values were removed

    [Tooltip("The category of planetoid")]
    public PlanetoidCategory category = PlanetoidCategory.None;

    private IOrbitSimulator _celestialOrbitSimulator;
    public IOrbitSimulator CelestialOrbitSimulator {
        get {
            if (_celestialOrbitSimulator == null) { // moons have 2 IOrbitSims in parents so can't use GetSafeInterfaceInParents
                _celestialOrbitSimulator = transform.parent.GetComponent<IOrbitSimulator>();
            }
            return _celestialOrbitSimulator;
        }
    }

    public new PlanetoidData Data {
        get { return base.Data as PlanetoidData; }
        set { base.Data = value; }
    }

    public PlanetoidReport UserReport { get { return Data.Publisher.GetUserReport(); } }

    public override float Radius { get { return Data.Radius; } }

    public new FollowableItemCameraStat CameraStat {
        protected get { return base.CameraStat as FollowableItemCameraStat; }
        set { base.CameraStat = value; }
    }

    /// <summary>
    /// The distance from the obstacle's center required to clear it when navigating around it.
    /// <remarks>The clearance a ship requires from its own position to avoid having its collision detection zone
    /// encounter the obstacle's zone must be added to this value to successfully avoid a collision between the zones.</remarks>
    /// </summary>
    protected abstract float ObstacleClearanceDistance { get; }
    protected SystemItem ParentSystem { get; private set; }

    protected SphereCollider _obstacleZoneCollider;

    private DetourGenerator _obstacleDetourGenerator;
    private DetourGenerator ObstacleDetourGenerator {
        get {
            if (_obstacleDetourGenerator == null) {
                InitializeObstacleDetourGenerator();
            }
            return _obstacleDetourGenerator;
        }
    }

    private DetectionHandler _detectionHandler;
    private SphereCollider _primaryCollider;
    private IList<IShip_Ltd> _shipsInHighOrbit;

    #region Initialization

    protected sealed override bool InitializeDebugLog() {
        return _debugCntls.ShowPlanetoidDebugLogs;
    }

    protected override void InitializeOnData() {
        base.InitializeOnData();
        InitializePrimaryCollider();
        InitializeObstacleZone();
        D.AssertEqual(category, Data.Category);
        ParentSystem = gameObject.GetSingleComponentInParents<SystemItem>();

        _detectionHandler = new DetectionHandler(this);
        CurrentState = PlanetoidState.None;
    }

    private void InitializePrimaryCollider() {
        _primaryCollider = UnityUtility.ValidateComponentPresence<SphereCollider>(gameObject);
        _primaryCollider.enabled = false;
        _primaryCollider.isTrigger = false;
        _primaryCollider.radius = Data.Radius;
    }

    private void InitializeObstacleZone() {
        _obstacleZoneCollider = gameObject.GetComponentsInImmediateChildren<SphereCollider>().Where(c => c.gameObject.layer == (int)Layers.AvoidableObstacleZone).Single();
        _obstacleZoneCollider.enabled = false;
        _obstacleZoneCollider.isTrigger = true;
        _obstacleZoneCollider.radius = InitializeObstacleZoneRadius();
        //D.Log(ShowDebugLog, "{0}'s ObstacleZoneCollider radius set to {1:0.##}.", DebugName, _obstacleZoneCollider.radius);
        if (_obstacleZoneCollider.radius > TempGameValues.LargestPlanetoidObstacleZoneRadius) {
            D.Warn("{0}'s ObstacleZoneCollider radius {1:0.##} > Max {2:0.##}.", DebugName, _obstacleZoneCollider.radius, TempGameValues.LargestPlanetoidObstacleZoneRadius);
        }

        Profiler.BeginSample("Editor-only GC allocation (GetComponent returns null)", gameObject);
        var rigidbody = _obstacleZoneCollider.gameObject.GetComponent<Rigidbody>();
        Profiler.EndSample();

        // Static trigger collider (no rigidbody) is OK as the ship's CollisionDetectionZone Collider has a kinematic rigidbody
        if (rigidbody != null) {
            D.Warn("{0}.ObstacleZone has a Rigidbody it doesn't need.", DebugName);
        }
        // 2.7.17 Lazy instantiated //InitializeObstacleDetourGenerator();    
        InitializeDebugShowObstacleZone();
    }

    protected abstract float InitializeObstacleZoneRadius();

    private void InitializeObstacleDetourGenerator() {
        if (IsMobile) {
            Reference<Vector3> obstacleZoneCenter = new Reference<Vector3>(() => _obstacleZoneCollider.transform.TransformPoint(_obstacleZoneCollider.center));
            _obstacleDetourGenerator = new DetourGenerator(DebugName, obstacleZoneCenter, _obstacleZoneCollider.radius, ObstacleClearanceDistance);
        }
        else {
            Vector3 obstacleZoneCenter = _obstacleZoneCollider.transform.TransformPoint(_obstacleZoneCollider.center);
            _obstacleDetourGenerator = new DetourGenerator(DebugName, obstacleZoneCenter, _obstacleZoneCollider.radius, ObstacleClearanceDistance);
        }
    }


    protected override void InitializeOnFirstDiscernibleToUser() {
        base.InitializeOnFirstDiscernibleToUser();
        Data.OrbitalSpeed = CelestialOrbitSimulator.RelativeOrbitSpeed;
    }

    protected override ItemHoveredHudManager InitializeHoveredHudManager() {
        return new ItemHoveredHudManager(Data.Publisher);
    }

    protected override ICtxControl InitializeContextMenu(Player owner) {
        return new PlanetoidCtxControl(this);
    }

    protected sealed override CircleHighlightManager InitializeCircleHighlightMgr() {
        float circleRadius = Radius * Screen.height * 3F;   // HACK
        return new CircleHighlightManager(transform, circleRadius);
    }

    public override void FinalInitialize() {
        base.FinalInitialize();
        // 7.11.16 Moved from CommenceOperations to be consistent with Cmds. Cmds need to be Idling to receive initial 
        // events once sensors are operational. Events include initial discovery of players which result in Relationship changes
        CurrentState = PlanetoidState.Idling;
    }

    #endregion

    public override void CommenceOperations() {
        base.CommenceOperations();
        _primaryCollider.enabled = true;
        _obstacleZoneCollider.enabled = true;
    }

    public PlanetoidReport GetReport(Player player) { return Data.Publisher.GetReport(player); }

    protected override void ShowSelectedItemHud() {
        if (Owner.IsUser) {
            InteractibleHudWindow.Instance.Show(FormID.UserPlanetoid, Data);
        }
        else {
            InteractibleHudWindow.Instance.Show(FormID.NonUserPlanetoid, UserReport);
        }
    }

    protected sealed override void HandleInfoAccessChangedFor(Player player) {
        base.HandleInfoAccessChangedFor(player);
        ParentSystem.AssessWhetherToFireInfoAccessChangedEventFor(player);
    }

    protected sealed override void AssignDeadState() {
        D.Log(ShowDebugLog, "{0} is setting Dead state.", DebugName);
        CurrentState = PlanetoidState.Dead;
    }

    protected override void PrepareForDeathEffect() {
        base.PrepareForDeathEffect();
        // Note: Keep the primaryCollider enabled until destroyed or returned to the pool as this allows 
        // in-route ordnance to show its impact effect while the item is showing its death.
        // Also keep the ObstacleZoneCollider enabled to keep ships from flying through the exploding planetoid.
        CelestialOrbitSimulator.IsActivated = false;
        ParentSystem.RemovePlanetoid(this);
    }

    /// <summary>
    /// Connects the high orbit rigidbody to the provided ship orbit joint,
    /// thereby placing the ship in high orbit around this planetoid.
    /// <remarks>If this planetoid is a moon, the ship(s) go into high orbit around
    /// the moon which itself orbits its parent planet. This means the ship(s) are in motion
    /// tracking the moon around the planet. UNCLEAR whether this motion around the planet 
    /// could cause the ship(s) to encounter other ships in high orbit around other planets
    /// and/or other moons.</remarks>
    /// </summary>
    /// <param name="shipOrbitJoint">The ship orbit joint.</param>
    protected virtual void ConnectHighOrbitRigidbodyToShipOrbitJoint(FixedJoint shipOrbitJoint) {
        shipOrbitJoint.connectedBody = CelestialOrbitSimulator.OrbitRigidbody;
    }

    protected override void HandleNameChanged() {
        base.HandleNameChanged();
        if (CelestialOrbitSimulator != null) {
            CelestialOrbitSimulator.transform.name = Name + GameConstants.OrbitSimulatorNameExtension;
        }
    }

    protected override void HandleOwnerChanging(Player newOwner) {
        base.HandleOwnerChanging(newOwner);
        if (Owner != TempGameValues.NoPlayer) {
            // Owner is about to lose ownership of item so reset owner and allies IntelCoverage of item to what they should know
            ResetBasedOnCurrentDetection(Owner);

            IEnumerable<Player> allies;
            if (TryGetAllies(out allies)) {
                allies.ForAll(ally => {
                    if (ally != newOwner && !ally.IsRelationshipWith(newOwner, DiplomaticRelationship.Alliance)) {
                        // 5.18.17 no point assessing current detection for newOwner or a newOwner ally
                        // as HandleOwnerChgd will assign Comprehensive to them all. 
                        ResetBasedOnCurrentDetection(ally);
                    }
                });
            }
        }
        // Note: A System will assess its IntelCoverage for a player anytime a member's IntelCoverage changes for that player
    }

    private void CurrentStatePropChangedHandler() {
        HandleStateChange();
    }

    #region Event and Property Change Handlers

    #endregion

    #region StateMachine - Simple Alternative

    private PlanetoidState _currentState;
    public PlanetoidState CurrentState {
        get { return _currentState; }
        protected set { SetProperty<PlanetoidState>(ref _currentState, value, "CurrentState", CurrentStatePropChangedHandler); }
    }

    private void HandleStateChange() {
        //D.Log(ShowDebugLog, "{0}.CurrentState changed to {1}.", Data.Name, CurrentState.GetValueName());
        switch (CurrentState) {
            case PlanetoidState.Idling:
                break;
            case PlanetoidState.Dead:
                PrepareForDeathEffect();
                StartEffectSequence(EffectSequenceID.Dying);
                HandleDeathEffectBegun();
                break;
            case PlanetoidState.None:
            default:
                throw new NotImplementedException(ErrorMessages.UnanticipatedSwitchValue.Inject(CurrentState));
        }
    }

    #region State Machine Support Methods

    /// <summary>
    /// Destroys this Planetoid, including the parent IOrbitSimulator and any children.
    /// </summary>
    /// <param name="delayInHours"></param>
    /// <param name="onCompletion">Optional delegate that fires onCompletion.</param>
    protected override void DestroyMe(float delayInHours = Constants.ZeroF, Action onCompletion = null) {
        D.Log(ShowDebugLog, "{0}.DestroyMe called.", DebugName);
        IOrbitSimulator parentOrbitSimulator = transform.parent.GetSafeInterface<IOrbitSimulator>();
        GameUtility.DestroyIfNotNullOrAlreadyDestroyed<IOrbitSimulator>(parentOrbitSimulator, delayInHours, onCompletion);
    }

    #endregion

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

    #region Debug

    /// <summary>
    /// Debug test method used by PlanetoidCtxControl to kill planets and moons.
    /// </summary>
    public void __Die() {
        IsDead = true;
    }

    #region Debug Show Obstacle Zones

    private void InitializeDebugShowObstacleZone() {
        _debugCntls.showObstacleZones += ShowDebugObstacleZonesChangedEventHandler;
        if (_debugCntls.ShowObstacleZones) {
            EnableDebugShowObstacleZone(true);
        }
    }

    private void EnableDebugShowObstacleZone(bool toEnable) {

        Profiler.BeginSample("Proper AddComponent allocation", gameObject);
        DrawColliderGizmo drawCntl = _obstacleZoneCollider.gameObject.AddMissingComponent<DrawColliderGizmo>();
        Profiler.EndSample();

        drawCntl.Color = Color.red;
        drawCntl.enabled = toEnable;
    }

    private void ShowDebugObstacleZonesChangedEventHandler(object sender, EventArgs e) {
        EnableDebugShowObstacleZone(_debugCntls.ShowObstacleZones);
    }

    private void CleanupDebugShowObstacleZone() {
        if (_debugCntls != null) {
            _debugCntls.showObstacleZones -= ShowDebugObstacleZonesChangedEventHandler;
        }
        if (_obstacleZoneCollider != null) {

            Profiler.BeginSample("Editor-only GC allocation (GetComponent returns null)", gameObject);
            DrawColliderGizmo drawCntl = _obstacleZoneCollider.gameObject.GetComponent<DrawColliderGizmo>();
            Profiler.EndSample();

            if (drawCntl != null) {
                Destroy(drawCntl);
            }
        }
    }

    #endregion

    #endregion

    #region IAssemblySupported Members

    /// <summary>
    /// A collection of assembly stations that are local to the item.
    /// </summary>
    public abstract IList<StationaryLocation> LocalAssemblyStations { get; }

    #endregion

    #region IShipOrbitable Members

    public bool IsHighOrbitAllowedBy(Player player) { return true; }

    public bool IsInHighOrbit(IShip_Ltd ship) {
        if (_shipsInHighOrbit == null || !_shipsInHighOrbit.Contains(ship)) {
            return false;
        }
        return true;
    }

    public void AssumeHighOrbit(IShip_Ltd ship, FixedJoint shipOrbitJoint) {
        if (_shipsInHighOrbit == null) {
            _shipsInHighOrbit = new List<IShip_Ltd>();
        }
        _shipsInHighOrbit.Add(ship);
        ConnectHighOrbitRigidbodyToShipOrbitJoint(shipOrbitJoint);
    }

    public virtual void HandleBrokeOrbit(IShip_Ltd ship) {
        if (IsInHighOrbit(ship)) {
            var isRemoved = _shipsInHighOrbit.Remove(ship);
            D.Assert(isRemoved);
            D.Log(ShowDebugLog, "{0} has left high orbit around {1}.", ship.DebugName, DebugName);
            return;
        }
        D.Error("{0}.HandleBrokeOrbit() called, but {1} not in orbit.", DebugName, ship.DebugName);
    }

    #endregion

    #region IElementAttackable Members

    public override void TakeHit(DamageStrength damagePotential) {
        if (_debugSettings.AllPlayersInvulnerable) {
            return;
        }
        D.Assert(!IsDead);  ////D.Assert(IsOperational);
        LogEvent();
        DamageStrength damage = damagePotential - Data.DamageMitigation;
        if (damage.Total == Constants.ZeroF) {
            D.Log(ShowDebugLog, "{0} has been hit but incurred no damage.", DebugName);
            return;
        }
        D.Log(ShowDebugLog, "{0} has been hit. Taking {1:0.#} damage.", DebugName, damage.Total);

        float unusedDamageSeverity;
        bool isAlive = ApplyDamage(damage, out unusedDamageSeverity);
        if (!isAlive) {
            IsDead = true;  ////IsOperational = false;
            return;
        }
        StartEffectSequence(EffectSequenceID.Hit);
    }

    #endregion

    #region IShipBombardable Members

    public ApBesiegeDestinationProxy GetApBesiegeTgtProxy(ValueRange<float> desiredWeaponsRangeEnvelope, IShip ship) {
        float shortestDistanceFromTgtToTgtSurface = Radius;
        float innerProxyRadius = desiredWeaponsRangeEnvelope.Minimum + shortestDistanceFromTgtToTgtSurface;
        float minInnerProxyRadiusToAvoidCollision = _obstacleZoneCollider.radius + ship.CollisionDetectionZoneRadius;
        if (innerProxyRadius < minInnerProxyRadiusToAvoidCollision) {
            innerProxyRadius = minInnerProxyRadiusToAvoidCollision;
        }
        float outerProxyRadius = desiredWeaponsRangeEnvelope.Maximum + shortestDistanceFromTgtToTgtSurface;
        D.Assert(outerProxyRadius > innerProxyRadius);

        ApBesiegeDestinationProxy attackProxy = new ApBesiegeDestinationProxy(this, ship, innerProxyRadius, outerProxyRadius);
        D.Log(ShowDebugLog, "{0} has constructed an AttackProxy with an ArrivalWindowDepth of {1:0.#} units.", DebugName, attackProxy.ArrivalWindowDepth);
        return attackProxy;
    }

    #endregion

    #region ICameraFollowable Members

    public float FollowDistanceDampener { get { return CameraStat.FollowDistanceDampener; } }

    public float FollowRotationDampener { get { return CameraStat.FollowRotationDampener; } }

    #endregion

    #region ISensorDetectable Members

    public void HandleDetectionBy(ISensorDetector detector, RangeCategory sensorRangeCat) {
        _detectionHandler.HandleDetectionBy(detector, sensorRangeCat);
    }

    public void HandleDetectionLostBy(ISensorDetector detector, Player detectorOwner, RangeCategory sensorRangeCat) {
        _detectionHandler.HandleDetectionLostBy(detector, detectorOwner, sensorRangeCat);
    }

    /// <summary>
    /// Resets the ISensorDetectable item based on current detection levels of the provided player.
    /// <remarks>8.2.16 Currently used
    /// 1) when player has lost the Alliance relationship with the owner of this item, and
    /// 2) when the owner of the item is about to be replaced by another player.</remarks>
    /// </summary>
    /// <param name="player">The player.</param>
    public void ResetBasedOnCurrentDetection(Player player) {
        _detectionHandler.ResetBasedOnCurrentDetection(player);
    }

    #endregion

    #region INavigableDestination Members

    public override bool IsMobile { get { return true; } }

    #endregion

    #region IFleetNavigableDestination Members

    public float GetObstacleCheckRayLength(Vector3 fleetPosition) {
        return Vector3.Distance(fleetPosition, Position) - _obstacleZoneCollider.radius - TempGameValues.ObstacleCheckRayLengthBuffer;
    }

    #endregion

    #region IAvoidableObstacle Members

    public float __ObstacleZoneRadius { get { return _obstacleZoneCollider.radius; } }

    public Vector3 GetDetour(Vector3 shipOrFleetPosition, RaycastHit zoneHitInfo, float shipOrFleetClearanceRadius) {
        DetourGenerator detourGenerator = ObstacleDetourGenerator;
        Vector3 detour = detourGenerator.GenerateDetourFromObstacleZoneHit(shipOrFleetPosition, zoneHitInfo.point, shipOrFleetClearanceRadius);
        if (!detourGenerator.IsDetourCleanlyReachable(detour, shipOrFleetPosition, shipOrFleetClearanceRadius)) {
            DetourGenerator.ApproachPath approachPath = detourGenerator.GetApproachPath(shipOrFleetPosition, zoneHitInfo.point);
            switch (approachPath) {
                case DetourGenerator.ApproachPath.Polar:
                    detour = detourGenerator.GenerateDetourFromZoneHitAroundBelt(shipOrFleetPosition, zoneHitInfo.point, shipOrFleetClearanceRadius);
                    if (!detourGenerator.IsDetourCleanlyReachable(detour, shipOrFleetPosition, shipOrFleetClearanceRadius)) {
                        detour = detourGenerator.GenerateDetourFromZoneHitAroundPoles(shipOrFleetPosition, zoneHitInfo.point, shipOrFleetClearanceRadius);
                        if (!detourGenerator.IsDetourCleanlyReachable(detour, shipOrFleetPosition, shipOrFleetClearanceRadius)) {
                            detour = detourGenerator.GenerateDetourAroundObstaclePoles(shipOrFleetPosition, shipOrFleetClearanceRadius);
                            D.Assert(detourGenerator.IsDetourReachable(detour, shipOrFleetPosition, shipOrFleetClearanceRadius),
                                "{0} detour {1} not reachable. Ship/Fleet.Position = {2}, ClearanceRadius = {3:0.##}. Position = {4}."
                                .Inject(DebugName, detour, shipOrFleetPosition, shipOrFleetClearanceRadius, Position));
                        }
                    }
                    break;
                case DetourGenerator.ApproachPath.Belt:
                    detour = detourGenerator.GenerateDetourFromZoneHitAroundPoles(shipOrFleetPosition, zoneHitInfo.point, shipOrFleetClearanceRadius);
                    if (!detourGenerator.IsDetourCleanlyReachable(detour, shipOrFleetPosition, shipOrFleetClearanceRadius)) {
                        detour = detourGenerator.GenerateDetourFromZoneHitAroundBelt(shipOrFleetPosition, zoneHitInfo.point, shipOrFleetClearanceRadius);
                        if (!detourGenerator.IsDetourCleanlyReachable(detour, shipOrFleetPosition, shipOrFleetClearanceRadius)) {
                            detour = detourGenerator.GenerateDetourAroundObstaclePoles(shipOrFleetPosition, shipOrFleetClearanceRadius);
                            D.Assert(detourGenerator.IsDetourReachable(detour, shipOrFleetPosition, shipOrFleetClearanceRadius),
                                "{0} detour {1} not reachable. Ship/Fleet.Position = {2}, ClearanceRadius = {3:0.##}. Position = {4}."
                                .Inject(DebugName, detour, shipOrFleetPosition, shipOrFleetClearanceRadius, Position));
                        }
                    }
                    break;
                case DetourGenerator.ApproachPath.None:
                default:
                    throw new NotImplementedException(ErrorMessages.UnanticipatedSwitchValue.Inject(approachPath));
            }
        }
        return detour;
    }

    #endregion

    #region Nested Classes

    /// <summary>
    /// Enum defining the states a Planetoid can operate in.
    /// </summary>
    public enum PlanetoidState {

        None,

        Idling,

        Dead

    }

    #endregion


}

