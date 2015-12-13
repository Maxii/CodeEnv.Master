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

#define DEBUG_LOG
#define DEBUG_WARN
#define DEBUG_ERROR

// default namespace

using System;
using System.Linq;
using CodeEnv.Master.Common;
using CodeEnv.Master.Common.LocalResources;
using CodeEnv.Master.GameContent;
using UnityEngine;

/// <summary>
/// Abstract class for AMortalItems that are Planetoid (Planet and Moon) Items.
/// </summary>
public abstract class APlanetoidItem : AMortalItem, IPlanetoidItem, ICameraFollowable, IShipTransitBanned, IUnitAttackableTarget, IElementAttackableTarget, ISensorDetectable {

    /// <summary>
    /// Gets the maximum possible orbital speed of a planetoid in Units per hour, 
    /// aka the max speed of any planet plus the max speed of any moon.
    /// </summary>
    public static float MaxOrbitalSpeed { get { return SystemCreator.AllPlanets.Max(p => p.Data.OrbitalSpeed) + SystemCreator.AllMoons.Max(m => m.Data.OrbitalSpeed); } }

    [Tooltip("The category of planetoid")]
    public PlanetoidCategory category = PlanetoidCategory.None;

    public new APlanetoidData Data {
        get { return base.Data as APlanetoidData; }
        set { base.Data = value; }
    }

    private PlanetoidPublisher _publisher;
    public PlanetoidPublisher Publisher {
        get { return _publisher = _publisher ?? new PlanetoidPublisher(Data, this); }
    }

    public override float Radius { get { return Data.Radius; } }

    public ISystemItem System { get; private set; }

    private DetectionHandler _detectionHandler;
    private ICtxControl _ctxControl;
    private SphereCollider _collider;

    #region Initialization

    protected override void InitializeOnData() {
        base.InitializeOnData();
        InitializePrimaryCollider();
        InitializeTransitBanCollider();
        D.Assert(category == Data.Category);
        System = gameObject.GetComponentInParent<SystemItem>();
        _detectionHandler = new DetectionHandler(this);
        CurrentState = PlanetoidState.None;
    }

    private void InitializePrimaryCollider() {
        _collider = UnityUtility.ValidateComponentPresence<SphereCollider>(gameObject);
        _collider.enabled = false;
        _collider.isTrigger = false;
        _collider.radius = Data.Radius;
    }

    private void InitializeTransitBanCollider() {
        SphereCollider shipTransitBanCollider = gameObject.GetComponentsInImmediateChildren<SphereCollider>().Where(c => c.isTrigger).Single();
        D.Assert(shipTransitBanCollider.gameObject.layer == (int)Layers.ShipTransitBan);
        shipTransitBanCollider.isTrigger = true;
        shipTransitBanCollider.radius = ShipTransitBanRadius;
    }

    protected override void InitializeOnFirstDiscernibleToUser() {
        base.InitializeOnFirstDiscernibleToUser();
        InitializeContextMenu(Owner);

        float orbitalRadius = transform.localPosition.magnitude;
        // moons will have 2 OrbitSimulators as parents, 1 for the moon and 1 for the planet        
        Data.OrbitalSpeed = gameObject.GetSafeFirstComponentInParents<OrbitSimulator>().GetRelativeOrbitSpeed(orbitalRadius);
    }

    protected override ItemHudManager InitializeHudManager() {
        return new ItemHudManager(Publisher);
    }

    private void InitializeContextMenu(Player owner) {
        _ctxControl = new PlanetoidCtxControl(this);
    }

    #endregion

    public override void CommenceOperations() {
        base.CommenceOperations();
        _collider.enabled = true;
        PlaceParentOrbiterInMotion(true);
        CurrentState = PlanetoidState.Idling;
    }

    public PlanetoidReport GetUserReport() { return Publisher.GetUserReport(); }

    public PlanetoidReport GetReport(Player player) { return Publisher.GetReport(player); }

    protected override void SetDeadState() {
        CurrentState = PlanetoidState.Dead;
    }

    protected override void PrepareForOnDeathNotification() {
        base.PrepareForOnDeathNotification();
        // Note: Keep the collider enabled until destroyed or returned to the pool. This allows in-route ordnance to show its impact effect while the item is showing its death
        PlaceParentOrbiterInMotion(false);
    }

    private void PlaceParentOrbiterInMotion(bool toOrbit) {
        transform.parent.GetComponent<IOrbitSimulator>().IsActivelyOrbiting = toOrbit;
    }

    #region Event and Property Change Handlers

    protected override void OwnerPropChangingHandler(Player newOwner) {
        base.OwnerPropChangingHandler(newOwner);
        // there is only 1 type of ContextMenu for Planetoids so no need to generate a new one
    }

    protected override void HandleRightPressRelease() {
        base.HandleRightPressRelease();
        if (!_inputMgr.IsDragging) {
            // right press release while not dragging means both press and release were over this object
            _ctxControl.TryShowContextMenu();
        }
    }

    #endregion

    #region StateMachine - Simple Alternative

    private PlanetoidState _currentState;
    public PlanetoidState CurrentState {
        get { return _currentState; }
        protected set { SetProperty<PlanetoidState>(ref _currentState, value, "CurrentState", CurrentStatePropChangedHandler); }
    }

    private void CurrentStatePropChangedHandler() {
        //D.Log("{0}.CurrentState changed to {1}.", Data.Name, CurrentState.GetName());
        switch (CurrentState) {
            case PlanetoidState.Idling:
                break;
            case PlanetoidState.Dead:
                StartEffect(EffectID.Dying);
                break;
            case PlanetoidState.None:
            default:
                throw new NotImplementedException(ErrorMessages.UnanticipatedSwitchValue.Inject(CurrentState));
        }
    }

    public override void HandleEffectFinished(EffectID effectID) {
        base.HandleEffectFinished(effectID);
        switch (CurrentState) {
            case PlanetoidState.Dead:
                __DestroyMe(3F);
                break;
            case PlanetoidState.Idling:
                // do nothing
                break;
            case PlanetoidState.None:
            default:
                throw new NotImplementedException(ErrorMessages.UnanticipatedSwitchValue.Inject(CurrentState));
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

    #region IElementAttackableTarget Members

    [System.Obsolete]
    public void HandleFiredUponBy(IInterceptableOrdnance ordnanceFired) {
        // does nothing as planetoids have no activeCMs to attempt to intercept
    }

    public override void TakeHit(DamageStrength damagePotential) {
        if (DebugSettings.Instance.AllPlayersInvulnerable) {
            return;
        }
        D.Assert(IsOperational);
        LogEvent();
        DamageStrength damage = damagePotential - Data.DamageMitigation;
        if (damage.Total == Constants.ZeroF) {
            D.Log("{0} has been hit but incurred no damage.", FullName);
            return;
        }
        D.Log("{0} has been hit. Taking {1:0.#} damage.", FullName, damage.Total);

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

    #region IShipTransitBanned Members

    public abstract float ShipTransitBanRadius { get; }

    #endregion

    #region ICameraFollowable Members

    public float FollowDistanceDampener { get { return Data.CameraStat.FollowDistanceDampener; } }

    public float FollowRotationDampener { get { return Data.CameraStat.FollowRotationDampener; } }

    #endregion

    #region IDetectable Members

    public void HandleDetectionBy(IUnitCmdItem cmdItem, RangeCategory sensorRange) {
        _detectionHandler.HandleDetectionBy(cmdItem, sensorRange);
    }

    public void HandleDetecionLostBy(IUnitCmdItem cmdItem, RangeCategory sensorRange) {
        _detectionHandler.HandleDetectionLostBy(cmdItem, sensorRange);
    }

    #endregion

    #region INavigableTarget Members

    public override bool IsMobile { get { return true; } }

    public override float GetCloseEnoughDistance(ICanNavigate navigatingItem) {
        return ShipTransitBanRadius + 1F;
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

