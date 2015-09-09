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
public abstract class APlanetoidItem : AMortalItem, IPlanetoidItem, ICameraFollowable, IShipOrbitable, IUnitAttackableTarget, IElementAttackableTarget, ISensorDetectable {

    /// <summary>
    /// Gets the maximum possible orbital speed of a planetoid in Units per hour, aka the max speed of any planet plus the max speed of any moon.
    /// </summary>
    public static float MaxOrbitalSpeed { get { return SystemCreator.AllPlanets.Max(p => p.Data.OrbitalSpeed) + SystemCreator.AllMoons.Max(m => m.Data.OrbitalSpeed); } }

    [Tooltip("The type of planetoid")]
    public PlanetoidCategory category;

    [Range(0.5F, 3.0F)]
    [Tooltip("Minimum Camera View Distance Multiplier")]
    public float minViewDistanceFactor = 2F;

    [Range(3.0F, 15.0F)]
    [Tooltip("Optimal Camera View Distance Multiplier")]
    public float optViewDistanceFactor = 8F;

    public new PlanetoidData Data {
        get { return base.Data as PlanetoidData; }
        set { base.Data = value; }
    }

    private PlanetoidPublisher _publisher;
    public PlanetoidPublisher Publisher {
        get { return _publisher = _publisher ?? new PlanetoidPublisher(Data, this); }
    }

    public ISystemItem System { get; private set; }

    private DetectionHandler _detectionHandler;
    private ICtxControl _ctxControl;

    #region Initialization

    protected override void InitializeLocalReferencesAndValues() {
        base.InitializeLocalReferencesAndValues();
        var meshRenderers = gameObject.GetComponentsInImmediateChildrenOnly<Renderer>();    // some planetoids have an atmosphere
        Radius = meshRenderers.First().bounds.size.x / 2F;    // half of the (length, width or height, all the same surrounding a sphere)
        collider.enabled = false;
        collider.isTrigger = false;
        (collider as SphereCollider).radius = Radius;

        InitializeKeepoutZone();
        InitializeShipOrbitSlot();
    }

    private void InitializeKeepoutZone() {
        SphereCollider keepoutZoneCollider = gameObject.GetComponentsInImmediateChildrenOnly<SphereCollider>().Where(c => c.isTrigger).Single();
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
        D.Assert(category == Data.Category);
        System = gameObject.GetComponentInParent<SystemItem>();
        _detectionHandler = new DetectionHandler(this);
        CurrentState = PlanetoidState.None;
    }

    protected override void InitializeViewMembersWhenFirstDiscernibleToUser() {
        base.InitializeViewMembersWhenFirstDiscernibleToUser();
        InitializeContextMenu(Owner);

        float orbitalRadius = _transform.localPosition.magnitude;
        Data.OrbitalSpeed = gameObject.GetSafeFirstMonoBehaviourInParents<OrbitSimulator>().GetRelativeOrbitSpeed(orbitalRadius);
    }

    protected override ItemHudManager InitializeHudManager() {
        return new ItemHudManager(Publisher);
    }

    private void InitializeContextMenu(Player owner) {
        _ctxControl = new PlanetoidCtxControl(this);
    }

    #endregion

    #region Model Methods

    public override void CommenceOperations() {
        base.CommenceOperations();
        collider.enabled = true;
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
        // _collider.enabled = false;   // keep the collider on until destroyed or returned to the pool
        // this allows in-route ordnance to show its impact effect while the item is showing its death
        PlaceParentOrbiterInMotion(false);
    }

    private void PlaceParentOrbiterInMotion(bool toOrbit) {
        _transform.parent.GetInterface<IOrbitSimulator>().IsActive = toOrbit;
    }

    protected override void OnOwnerChanging(Player newOwner) {
        base.OnOwnerChanging(newOwner);
        // there is only 1 type of ContextMenu for Planetoids so no need to generate a new one
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

    #region StateMachine - Simple Alternative

    private PlanetoidState _currentState;
    public PlanetoidState CurrentState {
        get { return _currentState; }
        protected set { SetProperty<PlanetoidState>(ref _currentState, value, "CurrentState", OnCurrentStateChanged); }
    }

    private void OnCurrentStateChanged() {
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

    public override void OnEffectFinished(EffectID effectID) {
        base.OnEffectFinished(effectID);
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
            InitiateDeath();
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

    #region IShipOrbitable Members

    public float KeepoutRadius { get; private set; }

    public ShipOrbitSlot ShipOrbitSlot { get; private set; }

    #endregion

    #region ICameraTargetable Members

    public override float MinimumCameraViewingDistance { get { return Radius * minViewDistanceFactor; } }

    #endregion

    #region ICameraFocusable Members

    public override float OptimalCameraViewingDistance { get { return Radius * optViewDistanceFactor; } }

    #endregion

    #region ICameraFollowable Members

    [SerializeField]
    private float cameraFollowDistanceDampener = 3.0F;
    public virtual float FollowDistanceDampener {
        get { return cameraFollowDistanceDampener; }
    }

    [SerializeField]
    private float cameraFollowRotationDampener = 1.0F;
    public virtual float FollowRotationDampener {
        get { return cameraFollowRotationDampener; }
    }

    #endregion

    #region IDetectable Members

    public void OnDetection(IUnitCmdItem cmdItem, RangeCategory sensorRange) {
        _detectionHandler.OnDetection(cmdItem, sensorRange);
    }

    public void OnDetectionLost(IUnitCmdItem cmdItem, RangeCategory sensorRange) {
        _detectionHandler.OnDetectionLost(cmdItem, sensorRange);
    }

    #endregion

    #region INavigableTarget Members

    public override bool IsMobile { get { return true; } }

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

