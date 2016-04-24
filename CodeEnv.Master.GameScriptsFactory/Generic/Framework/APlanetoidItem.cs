﻿// --------------------------------------------------------------------------------------------------------------------
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
/// Abstract class for AMortalItems that are Planetoid (Planet and Moon) Items.
/// </summary>
public abstract class APlanetoidItem : AMortalItem, IPlanetoidItem, ICameraFollowable, IFleetNavigable, IUnitAttackableTarget, IElementAttackableTarget,
    ISensorDetectable, IAvoidableObstacle, IShipOrbitable {

    /// <summary>
    /// Gets the maximum possible orbital speed of a planetoid in Units per hour, 
    /// aka the max speed of any planet plus the max speed of any moon.
    /// </summary>
    public static float MaxOrbitalSpeed { get { return SystemCreator.AllPlanets.Max(p => p.Data.OrbitalSpeed) + SystemCreator.AllMoons.Max(m => m.Data.OrbitalSpeed); } }

    [Tooltip("The category of planetoid")]
    public PlanetoidCategory category = PlanetoidCategory.None;

    private IOrbitSimulator _celestialOrbitSimulator;
    public IOrbitSimulator CelestialOrbitSimulator {
        get {
            if (_celestialOrbitSimulator == null) { // moons have 2 IOrbitSims in parents so can't use GetSafeInterfaceInParents
                _celestialOrbitSimulator = transform.parent.gameObject.GetSafeInterface<IOrbitSimulator>();
            }
            return _celestialOrbitSimulator;
        }
    }

    public new PlanetoidData Data {
        get { return base.Data as PlanetoidData; }
        set { base.Data = value; }
    }

    private PlanetoidPublisher _publisher;
    public PlanetoidPublisher Publisher {
        get { return _publisher = _publisher ?? new PlanetoidPublisher(Data, this); }
    }

    public override float Radius { get { return Data.Radius; } }

    public float ObstacleZoneRadius { get { return _obstacleZoneCollider.radius; } }

    private ISystemItem _parentSystem;
    public ISystemItem ParentSystem {
        get { return _parentSystem; }
        private set { SetProperty<ISystemItem>(ref _parentSystem, value, "ParentSystem"); }
    }

    protected SphereCollider _obstacleZoneCollider;

    private DetectionHandler _detectionHandler;
    private SphereCollider _primaryCollider;
    private IList<IShipItem> _shipsInHighOrbit;

    #region Initialization

    protected override void InitializeOnData() {
        base.InitializeOnData();
        InitializePrimaryCollider();
        InitializeObstacleZone();
        D.Assert(category == Data.Category);
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

    protected virtual void InitializeObstacleZone() {
        _obstacleZoneCollider = gameObject.GetComponentsInImmediateChildren<SphereCollider>().Where(c => c.gameObject.layer == (int)Layers.AvoidableObstacleZone).Single();
        _obstacleZoneCollider.enabled = false;
        _obstacleZoneCollider.isTrigger = true;
        // Static trigger collider (no rigidbody) is OK as the ship's CollisionDetectionZone Collider has a kinematic rigidbody
        D.Warn(_obstacleZoneCollider.gameObject.GetComponent<Rigidbody>() != null, "{0}.ObstacleZone has a Rigidbody it doesn't need.", FullName);
        InitializeDebugShowObstacleZone();
    }

    protected override void InitializeOnFirstDiscernibleToUser() {
        base.InitializeOnFirstDiscernibleToUser();
        Data.OrbitalSpeed = CelestialOrbitSimulator.RelativeOrbitSpeed;
    }

    protected override ItemHudManager InitializeHudManager() {
        return new ItemHudManager(Publisher);
    }

    protected override ICtxControl InitializeContextMenu(Player owner) {
        return new PlanetoidCtxControl(this);
    }

    #endregion

    public override void CommenceOperations() {
        base.CommenceOperations();
        _primaryCollider.enabled = true;
        _obstacleZoneCollider.enabled = true;
        ActivateParentOrbitSimulator(true);
        CurrentState = PlanetoidState.Idling;
    }

    public PlanetoidReport GetUserReport() { return Publisher.GetUserReport(); }

    public PlanetoidReport GetReport(Player player) { return Publisher.GetReport(player); }

    protected override void ShowSelectedItemHud() {
        SelectedItemHudWindow.Instance.Show(FormID.SelectedPlanetoid, GetUserReport());
    }

    /// <summary>
    /// Debug test method used by PlanetoidCtxControl to kill planets and moons.
    /// </summary>
    public void __Die() {
        IsOperational = false;
    }

    protected sealed override void SetDeadState() {
        D.Log(ShowDebugLog, "{0} is setting Dead state.", FullName);
        CurrentState = PlanetoidState.Dead;
    }

    protected override void HandleDeath() {
        base.HandleDeath();
        // Note: Keep the primaryCollider enabled until destroyed or returned to the pool as this allows 
        // in-route ordnance to show its impact effect while the item is showing its death.
        // Also keep the ObstacleZoneCollider enabled to keep ships from flying through the exploding planetoid.
        ActivateParentOrbitSimulator(false);
        ParentSystem.RemovePlanetoid(this);
    }

    protected virtual void ConnectHighOrbitRigidbodyToShipOrbitJoint(FixedJoint shipOrbitJoint) {
        shipOrbitJoint.connectedBody = CelestialOrbitSimulator.OrbitRigidbody;
    }

    private void ActivateParentOrbitSimulator(bool toActivate) {
        CelestialOrbitSimulator.IsActivated = toActivate;
    }

    #region Event and Property Change Handlers

    private void CurrentStatePropChangedHandler() {
        HandleStateChange();
    }

    #endregion

    #region StateMachine - Simple Alternative

    private PlanetoidState _currentState;
    public PlanetoidState CurrentState {
        get { return _currentState; }
        protected set { SetProperty<PlanetoidState>(ref _currentState, value, "CurrentState", CurrentStatePropChangedHandler); }
    }

    private void HandleStateChange() {
        D.Log(ShowDebugLog, "{0}.CurrentState changed to {1}.", Data.Name, CurrentState.GetValueName());
        switch (CurrentState) {
            case PlanetoidState.Idling:
                break;
            case PlanetoidState.Dead:
                HandleDeath();
                StartEffect(EffectID.Dying);
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
        D.Log(ShowDebugLog, "{0}.DestroyMe called.", FullName);
        IOrbitSimulator parentOrbitSimulator = transform.parent.gameObject.GetSafeInterface<IOrbitSimulator>();
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

    public bool IsHighOrbitAllowedBy(Player player) { return true; }

    public bool IsInHighOrbit(IShipItem ship) {
        if (_shipsInHighOrbit == null || !_shipsInHighOrbit.Contains(ship)) {
            return false;
        }
        return true;
    }

    public void AssumeHighOrbit(IShipItem ship, FixedJoint shipOrbitJoint) {
        if (_shipsInHighOrbit == null) {
            _shipsInHighOrbit = new List<IShipItem>();
        }
        _shipsInHighOrbit.Add(ship);
        ConnectHighOrbitRigidbodyToShipOrbitJoint(shipOrbitJoint);
    }

    public virtual void HandleBrokeOrbit(IShipItem ship) {
        if (IsInHighOrbit(ship)) {
            var isRemoved = _shipsInHighOrbit.Remove(ship);
            D.Assert(isRemoved);
            D.Log(ShowDebugLog, "{0} has left high orbit around {1}.", ship.FullName, FullName);
            return;
        }
        D.Error("{0}.HandleBrokeOrbit() called, but {1} not in orbit.", FullName, ship.FullName);
    }

    #endregion

    #region  IUnitAttackableTarget Members

    public bool IsAttackingAllowedBy(Player player) {
        return Owner.IsEnemyOf(player);
    }

    #endregion

    #region IElementAttackableTarget Members

    public AutoPilotTarget GetAttackTarget(float innerRadius, float outerRadius) {
        return new AutoPilotTarget(this, Vector3.zero, innerRadius, outerRadius);
    }

    // IsAttackingAllowedBy(Player) see IUnitAttackableTarget Members

    [System.Obsolete]
    public void HandleFiredUponBy(IInterceptableOrdnance ordnanceFired) {
        // does nothing as planetoids have no activeCMs to attempt to intercept
    }

    public override void TakeHit(DamageStrength damagePotential) {
        if (_debugSettings.AllPlayersInvulnerable) {
            return;
        }
        D.Assert(IsOperational);
        LogEvent();
        DamageStrength damage = damagePotential - Data.DamageMitigation;
        if (damage.Total == Constants.ZeroF) {
            D.Log(ShowDebugLog, "{0} has been hit but incurred no damage.", FullName);
            return;
        }
        D.Log(ShowDebugLog, "{0} has been hit. Taking {1:0.#} damage.", FullName, damage.Total);

        float unusedDamageSeverity;
        bool isAlive = ApplyDamage(damage, out unusedDamageSeverity);
        if (!isAlive) {
            //__GenerateExplosionMedia();
            IsOperational = false;
            return;
        }
        StartEffect(EffectID.Hit);
        //__GenerateHitImpactMedia();
    }

    //private void __GenerateHitImpactMedia() {
    //    var impactPrefab = RequiredPrefabs.Instance.hitImpact;
    //    var impactClone = UnityUtility.AddChild(gameObject, impactPrefab);
    //    D.Warn("{0} showing impact.", FullName);
    //}

    //private void __GenerateExplosionMedia() {
    //    var explosionPrefab = RequiredPrefabs.Instance.explosion;
    //    var explosionClone = UnityUtility.AddChild(gameObject, explosionPrefab);
    //    D.Warn("{0} showing explosion.", FullName);
    //}

    #endregion

    #region ICameraFollowable Members

    public float FollowDistanceDampener { get { return Data.CameraStat.FollowDistanceDampener; } }

    public float FollowRotationDampener { get { return Data.CameraStat.FollowRotationDampener; } }

    #endregion

    #region IDetectable Members

    public void HandleDetectionBy(IUnitCmdItem cmdItem, RangeCategory sensorRange) {
        _detectionHandler.HandleDetectionBy(cmdItem, sensorRange);
    }

    public void HandleDetectionLostBy(IUnitCmdItem cmdItem, RangeCategory sensorRange) {
        _detectionHandler.HandleDetectionLostBy(cmdItem, sensorRange);
    }

    #endregion

    #region INavigable Members

    public override bool IsMobile { get { return true; } }

    #endregion

    #region IFleetNavigable Members

    public abstract float GetObstacleCheckRayLength(Vector3 fleetPosition);

    #endregion

    #region IAvoidableObstacle Members

    public abstract Vector3 GetDetour(Vector3 shipOrFleetPosition, RaycastHit zoneHitInfo, float fleetRadius);

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

