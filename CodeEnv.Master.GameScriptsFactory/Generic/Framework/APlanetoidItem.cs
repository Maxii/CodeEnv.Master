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
public abstract class APlanetoidItem : AMortalItem, ICameraFollowable, IShipOrbitable, IUnitAttackableTarget, IElementAttackableTarget, IDetectable {

    [Tooltip("The type of planetoid")]
    public PlanetoidCategory category;

    [Range(0.5F, 3.0F)]
    [Tooltip("Minimum Camera View Distance Multiplier")]
    public float minViewDistanceFactor = 2F;

    [Range(3.0F, 15.0F)]
    [Tooltip("Optimal Camera View Distance Multiplier")]
    public float optViewDistanceFactor = 8F;

    public new PlanetoidItemData Data {
        get { return base.Data as PlanetoidItemData; }
        set { base.Data = value; }
    }

    private PlanetoidPublisher _publisher;
    public PlanetoidPublisher Publisher {
        get { return _publisher = _publisher ?? new PlanetoidPublisher(Data); }
    }

    private ICtxControl _ctxControl;

    #region Initialization

    protected override void InitializeLocalReferencesAndValues() {
        base.InitializeLocalReferencesAndValues();
        var meshRenderers = gameObject.GetComponentsInImmediateChildren<Renderer>();    // some planetoids have an atmosphere
        Radius = meshRenderers.First().bounds.size.x / 2F;    // half of the (length, width or height, all the same surrounding a sphere)
        (collider as SphereCollider).radius = Radius;
        collider.isTrigger = false;
        collider.enabled = false;

        InitializeShipOrbitSlot();
        InitializeKeepoutZone();
    }

    private void InitializeShipOrbitSlot() {
        float innerOrbitRadius = Radius * TempGameValues.KeepoutRadiusMultiplier;
        float outerOrbitRadius = innerOrbitRadius + TempGameValues.DefaultShipOrbitSlotDepth;
        ShipOrbitSlot = new ShipOrbitSlot(innerOrbitRadius, outerOrbitRadius, this);
    }

    private void InitializeKeepoutZone() {
        SphereCollider keepoutZoneCollider = gameObject.GetComponentsInImmediateChildren<SphereCollider>().Where(c => c.isTrigger).Single();
        D.Assert(keepoutZoneCollider.gameObject.layer == (int)Layers.CelestialObjectKeepout);
        keepoutZoneCollider.isTrigger = true;
        keepoutZoneCollider.radius = ShipOrbitSlot.InnerRadius;
    }

    protected override void InitializeModelMembers() {
        D.Assert(category == Data.Category);
        CurrentState = PlanetoidState.None;
    }

    protected override void InitializeViewMembersOnDiscernible() {
        base.InitializeViewMembersOnDiscernible();
        InitializeContextMenu(Owner);
        // Once the player initially discerns the planet, he will always be able to discern it
        var meshRenderers = gameObject.GetComponentsInImmediateChildren<MeshRenderer>();
        meshRenderers.ForAll(mr => {
            mr.castShadows = true;
            mr.receiveShadows = true;
            mr.enabled = true;
        });

        var animations = gameObject.GetComponentsInImmediateChildren<Animation>();
        animations.ForAll(anim => {
            anim.cullingType = AnimationCullingType.BasedOnRenderers; // aka, disabled when not visible
            anim.enabled = true;
        });
        // TODO animation settings and distance controls

        // TODO Revolver settings and distance controls, Revolvers control their own enabled state based on visibility

        var cameraLosChgdListener = gameObject.GetSafeInterfaceInImmediateChildren<ICameraLosChangedListener>();
        cameraLosChgdListener.onCameraLosChanged += (go, inCameraLOS) => InCameraLOS = inCameraLOS;
        cameraLosChgdListener.enabled = true;

        float orbitalRadius = _transform.localPosition.magnitude;
        Data.OrbitalSpeed = gameObject.GetSafeMonoBehaviourComponentInParents<Orbiter>().GetRelativeOrbitSpeed(orbitalRadius);
    }

    protected override HudManager InitializeHudManager() {
        var hudManager = new HudManager(Publisher);
        hudManager.AddContentToUpdate(AHudManager.UpdatableLabelContentID.IntelState);
        return hudManager;
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

    public PlanetoidReport GetReport(Player player) { return Publisher.GetReport(player); }

    protected override void InitiateDeath() {
        base.InitiateDeath();
        CurrentState = PlanetoidState.Dead;
    }

    protected override void OnDeath() {
        base.OnDeath();
        collider.enabled = false;
        PlaceParentOrbiterInMotion(false);
    }

    private void PlaceParentOrbiterInMotion(bool toOrbit) {
        _transform.parent.GetInterface<IOrbiter>().IsOrbiterInMotion = toOrbit;
    }

    protected override void OnOwnerChanging(Player newOwner) {
        base.OnOwnerChanging(newOwner);
        // there is only 1 type of ContextMenu for Planetoids so no need to generate a new one
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
                //OnDeath();
                ShowAnimation(MortalAnimations.Dying);
                break;
            case PlanetoidState.None:
            default:
                throw new NotImplementedException(ErrorMessages.UnanticipatedSwitchValue.Inject(CurrentState));
        }
    }

    protected override void OnShowCompletion() {
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
    }

    #endregion

    #region IElementAttackableTarget Members

    public override void TakeHit(CombatStrength attackerWeaponStrength) {
        if (!IsAliveAndOperating) {
            return;
        }
        LogEvent();
        CombatStrength damage = attackerWeaponStrength - Data.DefensiveStrength;
        if (damage.Combined == Constants.ZeroF) {
            D.Log("{0} has been hit but incurred no damage.", FullName);
            return;
        }
        D.Log("{0} has been hit. Taking {1:0.#} damage.", FullName, damage.Combined);

        float unusedDamageSeverity;
        bool isAlive = ApplyDamage(damage, out unusedDamageSeverity);
        if (!isAlive) {
            InitiateDeath();
            return;
        }
        ShowAnimation(MortalAnimations.Hit);
    }

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

    #region IDetectableItem Members

    public void OnDetectionGained(ICommandItem cmdItem, DistanceRange sensorRange) {
        throw new NotImplementedException();
    }

    public void OnDetectionLost(ICommandItem cmdItem, DistanceRange sensorRange) {
        throw new NotImplementedException();
    }

    #endregion

    #region INavigableTarget Members

    public override bool IsMobile { get { return true; } }

    #endregion

}

