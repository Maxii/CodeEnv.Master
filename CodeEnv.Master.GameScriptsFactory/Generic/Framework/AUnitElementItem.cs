// --------------------------------------------------------------------------------------------------------------------
// <copyright>
// Copyright © 2012 - 2014 Strategic Forge
//
// Email: jim@strategicforge.com
// </copyright> 
// <summary> 
// File: AUnitElementItem.cs
// Abstract class for AMortalItem's that are Unit Elements.
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
using CodeEnv.Master.GameContent;
using UnityEngine;

/// <summary>
/// Abstract class for AMortalItem's that are Unit Elements.
/// </summary>
public abstract class AUnitElementItem : AMortalItemStateMachine, IUnitElement, IUnitElement_Ltd, ICameraFollowable, IShipBlastable,
    ISensorDetectable, ISensorDetector, IFsmEventSubscriptionMgrClient, IAssaultable {

    private const string __HQNameAddendum = "[HQ]";

    public event EventHandler isHQChanged;

    public event EventHandler isAvailableChanged;

    public event EventHandler<SubordinateOwnerChangingEventArgs> subordinateOwnerChanging;

    public event EventHandler<SubordinateDamageIncurredEventArgs> subordinateDamageIncurred;

    public event EventHandler subordinateDeathOneShot;

    /// <summary>
    /// Indicates whether this element is available for a new assignment.
    /// <remarks>Typically, an element that is available is Idling.</remarks>
    /// </summary>
    private bool _isAvailable;
    public bool IsAvailable {
        get { return _isAvailable; }
        protected set {
            if (_isAvailable != value) {
                _isAvailable = value;
                IsAvailablePropChangedHandler();
            }
        }
    }

    /// <summary>
    /// Indicates whether this element is capable of attacking an enemy target.
    /// </summary>
    public abstract bool IsAttackCapable { get; }

    private ReworkingMode _reworkUnderway;
    public ReworkingMode ReworkUnderway {
        get { return _reworkUnderway; }
        protected set { SetProperty<ReworkingMode>(ref _reworkUnderway, value, "ReworkUnderway", ReworkUnderwayPropChangedHandler); }
    }

    public int UnitElementCount { get { return Command.ElementCount; } }

    public new AUnitElementData Data {
        get { return base.Data as AUnitElementData; }
        protected set { base.Data = value; }
    }

    private float _radius;
    public override float Radius {
        get {
            if (_radius == Constants.ZeroF) {
                _radius = Data.HullDimensions.magnitude / 2F;
                //D.Log(ShowDebugLog, "{0} Radius set to {1:0.000}.", DebugName, _radius);
                __ValidateRadius(_radius);
            }
            return _radius;
        }
    }

    private bool _isHQ;
    public bool IsHQ {
        get { return _isHQ; }
        set { SetProperty<bool>(ref _isHQ, value, "IsHQ", IsHQPropChangedHandler); }
    }

    public AUnitCmdItem Command { protected get; set; }

    // OPTIMIZE all elements followable for now to support facilities rotating around bases or stars
    public new FollowableItemCameraStat CameraStat {
        protected get { return base.CameraStat as FollowableItemCameraStat; }
        set { base.CameraStat = value; }
    }

    public AlertStatus AlertStatus {
        get { return Data.AlertStatus; }
        set { Data.AlertStatus = value; }
    }

    public IElementSensorRangeMonitor SRSensorMonitor { get; private set; }

    public new bool IsOwnerChangeUnderway { get { return base.IsOwnerChangeUnderway; } }

    protected new AElementDisplayManager DisplayMgr { get { return base.DisplayMgr as AElementDisplayManager; } }
    protected IList<IWeaponRangeMonitor> WeaponRangeMonitors { get; private set; }
    protected IList<IActiveCountermeasureRangeMonitor> CountermeasureRangeMonitors { get; private set; }
    protected IList<IShield> Shields { get; private set; }
    protected Rigidbody Rigidbody { get; private set; }
    protected FsmEventSubscriptionManager FsmEventSubscriptionMgr { get; private set; }
    protected override bool IsPaused { get { return _gameMgr.IsPaused; } }

    protected Job _repairJob;

    private DetectionHandler _detectionHandler;
    private BoxCollider _primaryCollider;
    private Player _newOwnerAfterPause;

    #region Initialization

    protected override void InitializeValuesAndReferences() {
        base.InitializeValuesAndReferences();
        WeaponRangeMonitors = new List<IWeaponRangeMonitor>();
        CountermeasureRangeMonitors = new List<IActiveCountermeasureRangeMonitor>();
        Shields = new List<IShield>();
    }

    protected override void InitializeOnData() {
        base.InitializeOnData();
        InitializePrimaryCollider();
        InitializePrimaryRigidbody();
        AttachEquipment();
        _detectionHandler = new DetectionHandler(this);
    }

    private void InitializePrimaryCollider() {
        _primaryCollider = UnityUtility.ValidateComponentPresence<BoxCollider>(gameObject);
        // Early detection of colliders that start out enabled can occur when data is added during runtime
        if (_primaryCollider.enabled) {
            D.Warn("{0}'s primary collider should start disabled to avoid early detection by monitors.", DebugName);
        }
        _primaryCollider.isTrigger = false;
        _primaryCollider.enabled = false;
        _primaryCollider.size = Data.HullDimensions;
    }

    protected virtual void InitializePrimaryRigidbody() {
        Rigidbody = UnityUtility.ValidateComponentPresence<Rigidbody>(gameObject);
        Rigidbody.useGravity = false;
        // Note: if physics is allowed to induce rotation, then ChangeHeading behaves unpredictably when HQ, 
        // presumably because Cmd is attached to HQ with a fixed joint?
        Rigidbody.freezeRotation = true;   // covers both Ship and Facility for when I put Facility under physics control
        Rigidbody.isKinematic = true;      // avoid physics affects until CommenceOperations, if at all
    }

    private void AttachEquipment() {
        Data.ActiveCountermeasures.ForAll(cm => Attach(cm));
        Data.Weapons.ForAll(w => Attach(w));
        Data.ShieldGenerators.ForAll(gen => Attach(gen));
        Data.Sensors.ForAll(s => Attach(s));
    }

    protected sealed override void InitializeDisplayMgr() {
        base.InitializeDisplayMgr();
        // 1.16.17 TEMP Replaced User Option/Preference with easily accessible DebugControls setting
        InitializeIcon();
        DisplayMgr.MeshColor = Owner.Color;
    }

    protected sealed override CircleHighlightManager InitializeCircleHighlightMgr() {
        float circleRadius = Radius * Screen.height * 1F;   // HACK
        return new CircleHighlightManager(transform, circleRadius);
    }

    protected sealed override HoverHighlightManager InitializeHoverHighlightMgr() {
        return new HoverHighlightManager(this, Radius);
    }

    public override void FinalInitialize() {
        base.FinalInitialize();
        InitializeRangeMonitors();
        InitializeFsmEventSubscriptionMgr();
        __InitializeFinalRigidbodySettings();
    }

    protected abstract void __InitializeFinalRigidbodySettings();

    private void InitializeRangeMonitors() {
        SRSensorMonitor.InitializeRangeDistance();  // 5.10.17 First as other Monitors validate their range compared to this range
        WeaponRangeMonitors.ForAll(wrm => {
            wrm.InitializeRangeDistance();
            wrm.ToEngageColdWarEnemies = OwnerAIMgr.IsPolicyToEngageColdWarEnemies;
        });
        CountermeasureRangeMonitors.ForAll(crm => crm.InitializeRangeDistance());
        Shields.ForAll(srm => srm.InitializeRangeDistance());
    }

    private void InitializeFsmEventSubscriptionMgr() {
        FsmEventSubscriptionMgr = new FsmEventSubscriptionManager(this);
    }

    /// <summary>
    /// Subscribes to sensor events.
    /// <remarks>Must be called after initial runtime state is set, aka Idling. 
    /// Otherwise events can arrive immediately as sensors activate.</remarks>
    /// <remarks>UNDONE 5.13.17 No use yet in Elements for responding to what their SRSensors detect.</remarks>
    /// </summary>
    protected void __SubscribeToSensorEvents() {
        __ValidateStateForSensorEventSubscription();
    }

    protected abstract void __ValidateStateForSensorEventSubscription();

    #endregion

    public override void CommenceOperations() {  // FIXME new does not hide this
        base.CommenceOperations();
        _primaryCollider.enabled = true;
    }

    /// <summary>
    /// Parents this element to the provided container that holds the entire Unit.
    /// Local position, rotation and scale auto adjust to keep element unchanged in worldspace.
    /// </summary>
    /// <param name="unitContainer">The unit container.</param>
    internal void AttachAsChildOf(Transform unitContainer) {
        if (transform.parent != unitContainer) {
            // In most cases, the element is already a child of the UnitContainer. Conditions where
            // this change is reqd include a ship joining another fleet, creating a fleet from another, etc.
            //D.Log(ShowDebugLog, "{0} is not a child of {1}. Fixing.", DebugName, unitContainer.name);
            transform.parent = unitContainer;
        }
    }

    /// <summary>
    /// Called by this Element's UnitCmd when a change of the Cmd's HQElement has been completed.
    /// <remarks>All the Cmd's Elements are notified of this completed change. Allows each element
    /// to potentially take compensating action based on the CurrentState of the element.</remarks>
    /// </summary>
    public void HandleChangeOfHQStatusCompleted() {
        D.Assert(!IsDead);
        UponHQStatusChangeCompleted();
    }

    private void ChangeOwnerAfterPause() {
        D.AssertNotNull(_newOwnerAfterPause);
        D.Assert(!IsPaused);

        Player newOwner = _newOwnerAfterPause;
        _newOwnerAfterPause = null;
        D.Log(/*ShowDebugLog,*/ "{0} is changing owner to {1} after resuming from pause.", DebugName, newOwner.DebugName);
        Data.Owner = newOwner;
    }

    internal void HandleColdWarEnemyEngagementPolicyChanged() {
        bool toEngageColdWarEnemies = OwnerAIMgr.IsPolicyToEngageColdWarEnemies;
        WeaponRangeMonitors.ForAll(wrm => wrm.ToEngageColdWarEnemies = toEngageColdWarEnemies);
    }

    protected void HandleRefitReplacementCompleted() {
        Data.HandleRefitReplacementCompleted();
        PrepareForDeathSequence();
        // Don't call PrepareForOnDeath() as it fires the subordinateDeath event which will attempt to remove the already removed element
        OnDeath();  // fire the death event
        PrepareForDeadState();
        AssignDeadState();
        // FIXME DeathEffect methods get called after this from Dead_EnterState which we don't want
    }

    protected override void PrepareForDeathSequence() {
        base.PrepareForDeathSequence();
        Data.Weapons.ForAll(weap => weap.readytoFire -= WeaponReadyToFireEventHandler);
        // equipment deactivation handled by Data
    }

    protected sealed override void PrepareForOnDeath() {
        base.PrepareForOnDeath();
        OnSubordinateDeath();
    }

    protected override void PrepareForDeadState() {
        base.PrepareForDeadState();
        ReturnFromCalledStates();
        UponDeath();    // 4.19.17 Do any reqd Callback before exiting current non-Call()ed state
    }

    /********************************************************************************************************************************************
      * Equipment (Weapons, Sensors and Countermeasures) no longer added or removed while the item is operating. 
      * Changes in an item's equipment can only occur during a refit where a new item is created to replace the item being refitted.
      ********************************************************************************************************************************************/

    #region Weapons

    /*******************************************************************************************************************************************
     * This implementation attempts to calculate a firing solution against every target thought to be in range and leaves it up to the 
     * element to determine which one to use, if any. If the element declines to fire (would be ineffective, not proper state (IE. refitting), target 
     * died or diplomatic relations changed while weapon being aimed, etc.), then the weapon continues to look for firing solutions to put forward.
     * This approach works best where many weapons or countermeasures may not bear even when the target is in range.
     ********************************************************************************************************************************************/

    /// <summary>
    /// Attaches the weapon and its monitor to this item.
    /// </summary>
    /// <param name="weapon">The weapon.</param>
    private void Attach(AWeapon weapon) {
        D.AssertNotNull(weapon.RangeMonitor);
        var monitor = weapon.RangeMonitor;
        if (!WeaponRangeMonitors.Contains(monitor)) {
            // only need to record and setup range monitors once. The same monitor can have more than 1 weapon
            WeaponRangeMonitors.Add(monitor);
        }
        weapon.readytoFire += WeaponReadyToFireEventHandler;
    }

    protected WeaponFiringSolution PickBestFiringSolution(IList<WeaponFiringSolution> firingSolutions, IElementAttackable tgtHint = null) {
        int count = firingSolutions.Count;
        D.Assert(count > Constants.Zero);

        WeaponFiringSolution solution = null;
        if (count == Constants.One) {
            solution = firingSolutions[0];
        }
        else if (tgtHint != null) {
            var hintFiringSolution = firingSolutions.SingleOrDefault(fs => fs.EnemyTarget == tgtHint);
            if (hintFiringSolution != null) {
                solution = hintFiringSolution;
            }
        }

        if (solution == null) {
            solution = firingSolutions.Shuffle().First();
        }
        return solution;
    }

    /// <summary>
    /// Initiates the process of firing the provided weapon at the enemy target defined by the provided firing solution.
    /// If the conditions for firing the weapon at the target are satisfied (within range, can be borne upon,
    /// no interfering obstacles, etc.), the weapon will be fired.
    /// </summary>
    /// <param name="firingSolution">The firing solution.</param>
    protected void InitiateFiringSequence(WeaponFiringSolution firingSolution) {
        StartEffectSequence(EffectSequenceID.Attacking);
        LosWeaponFiringSolution losFiringSolution = firingSolution as LosWeaponFiringSolution;
        if (losFiringSolution != null) {
            var losWeapon = losFiringSolution.Weapon;
            losWeapon.weaponAimed += LosWeaponAimedEventHandler;
            losWeapon.AimAt(losFiringSolution);
        }
        else {
            // no aiming reqd, just launch the ordnance
            var weapon = firingSolution.Weapon;
            var target = firingSolution.EnemyTarget;
            LaunchOrdnance(weapon, target);
        }
    }

    /// <summary>
    /// Spawns and launches the ordnance.
    /// <remarks>12.1.16 Fixed in Unity 5.5. Physics.IgnoreCollision below resets the trigger state of each collider, thereby
    /// generating sequential OnTriggerExit and OnTriggerEnter events in any Monitor in the area.</remarks>
    /// <see cref="http://forum.unity3d.com/threads/physics-ignorecollision-that-does-not-reset-trigger-state.340836/"/>
    /// </summary>
    /// <param name="weapon">The weapon.</param>
    /// <param name="target">The target.</param>
    private void LaunchOrdnance(AWeapon weapon, IElementAttackable target) {
        Vector3 launchLoc = weapon.WeaponMount.MuzzleLocation;
        Quaternion launchRotation = Quaternion.LookRotation(weapon.WeaponMount.MuzzleFacing);
        WDVCategory category = weapon.DeliveryVehicleCategory;
        Transform ordnanceTransform;
        if (category == WDVCategory.Beam) {
            ordnanceTransform = GamePoolManager.Instance.Spawn(category, launchLoc, launchRotation, weapon.WeaponMount.Muzzle);
            Beam beam = ordnanceTransform.GetComponent<Beam>();
            beam.Launch(target, weapon);
        }
        else {
            // Projectiles are collected under GamePoolManager in the scene
            ordnanceTransform = GamePoolManager.Instance.Spawn(category, launchLoc, launchRotation);
            Collider ordnanceCollider = UnityUtility.ValidateComponentPresence<Collider>(ordnanceTransform.gameObject);
            D.Assert(ordnanceTransform.gameObject.activeSelf);  // ordnanceGo must be active for IgnoreCollision
            Physics.IgnoreCollision(ordnanceCollider, _primaryCollider);

            if (category == WDVCategory.Missile) {
                Missile missile = ordnanceTransform.GetComponent<Missile>();
                missile.ElementVelocityAtLaunch = Rigidbody.velocity;
                missile.Launch(target, weapon, Topography);
            }
            else if (category == WDVCategory.AssaultVehicle) {
                AssaultVehicle shuttle = ordnanceTransform.GetComponent<AssaultVehicle>();
                shuttle.ElementVelocityAtLaunch = Rigidbody.velocity;
                shuttle.Launch(target, weapon, Topography);
            }
            else {
                D.AssertEqual(WDVCategory.Projectile, category);
                AProjectileOrdnance projectile;
                if (DebugControls.Instance.MovementTech == DebugControls.UnityMoveTech.Kinematic) {
                    projectile = ordnanceTransform.GetComponent<KinematicProjectile>();
                }
                else {
                    projectile = ordnanceTransform.GetComponent<PhysicsProjectile>();
                }
                projectile.Launch(target, weapon, Topography);
            }
        }
        //D.Log(ShowDebugLog, "{0} has fired {1} against {2} on {3}.", DebugName, ordnance.Name, target.DebugName, GameTime.Instance.CurrentDate);
        /***********************************************************************************************************************************************
         * Note on Target Death: When a target dies, the fired ordnance detects it and takes appropriate action. All ordnance types will no longer
         * apply damage to a dead target, but the impact effect will still show if applicable. This is so the viewer still sees impacts even while the
         * death cinematic plays out. Once the target is destroyed, its collider becomes disabled, allowing ordnance to pass through and potentially
         * collide with other items until it runs out of range and self terminates. This behaviour holds for both projectile and beam ordnance. In the
         * case of missile ordnance, once its target is dead it self destructs as waiting until the target is destroyed results in 'transform destroyed' errors.
         **************************************************************************************************************************************************/
    }

    private void HandleWeaponReadyToFire(IList<WeaponFiringSolution> firingSolutions) {
        bool isMsgReceived = UponWeaponReadyToFire(firingSolutions);
        if (!isMsgReceived) {
            // element in state that doesn't deal with firing weapons
            var weapon = firingSolutions.First().Weapon;    // all the same weapon
            weapon.HandleElementDeclinedToFire();
        }
    }

    private void HandleLosWeaponAimed(LosWeaponFiringSolution firingSolution) {
        var target = firingSolution.EnemyTarget;
        var losWeapon = firingSolution.Weapon;
        D.Assert(losWeapon.IsOperational);  // weapon should not have completed aiming if it lost operation
        if (!target.IsDead && target.IsAttackAllowedBy(Owner) && losWeapon.ConfirmInRangeForLaunch(target)) {
            if (losWeapon.__CheckLineOfSight(target)) {
                LaunchOrdnance(losWeapon, target);
            }
            else {
                D.Warn("{0} no longer has a bead on Target {1}. Canceling firing solution!", losWeapon.DebugName, target.DebugName);
                losWeapon.HandleElementDeclinedToFire();
            }
        }
        else {
            // target moved out of range, died or changed relations during aiming process
            losWeapon.HandleElementDeclinedToFire();
        }
        losWeapon.weaponAimed -= LosWeaponAimedEventHandler;
    }

    #region Weapons Firing Archive

    //private IList<AWeapon> _readyWeaponsInventory = new List<AWeapon>();

    //private void Attach(AWeapon weapon) {
    //    D.Assert(weapon.RangeMonitor != null);
    //    var monitor = weapon.RangeMonitor;
    //    if (!_weaponRangeMonitors.Contains(monitor)) {
    //        // only need to record and setup range monitors once. The same monitor can have more than 1 weapon
    //        _weaponRangeMonitors.Add(monitor);
    //    }
    //    weapon.onIsReadyToFireChanged += OnWeaponReadinessChanged;
    //    weapon.onEnemyTargetEnteringRange += OnNewEnemyTargetInRange;
    //    // IsOperational = true is set when item operations commences
    //}


    /// <summary>
    /// Attempts to find a target in range and fire the weapon at it.
    /// </summary>
    /// <param name="weapon">The weapon.</param>
    /// <param name="tgtHint">Optional hint indicating a highly desirable target.</param>
    //protected void FindTargetAndFire(AWeapon weapon, IElementAttackableTarget tgtHint = null) {
    //    D.Assert(weapon.IsReadyToFire);
    //    IElementAttackableTarget enemyTarget;
    //    if (weapon.TryPickBestTarget(tgtHint, out enemyTarget)) {
    //        InitiateFiringSequence(weapon, enemyTarget);
    //    }
    //    else {
    //        D.Log("{0} did not fire weapon {1}.", DebugName, weapon.Name);
    //    }
    //}


    /// <summary>
    /// Called when there is a change in the readiness to fire status of the indicated weapon. 
    /// Readiness to fire does not mean there is an enemy in range to fire at.
    /// </summary>
    /// <param name="weapon">The weapon.</param>
    //private void OnWeaponReadinessChanged(AWeapon weapon) {
    //    if (weapon.IsReadyToFire && weapon.IsEnemyInRange) {
    //        OnWeaponReadyAndEnemyInRange(weapon);
    //    }
    //    UpdateReadyWeaponsInventory(weapon);
    //}

    /// <summary>
    /// Called when a new, qualified enemy target has come within range 
    /// of the indicated weapon. This event is independent of whether the
    /// weapon is ready to fire. However, it does mean the weapon is operational.
    /// </summary>
    /// <param name="weapon">The weapon.</param>
    //private void OnNewEnemyTargetInRange(AWeapon weapon) {
    //    if (_readyWeaponsInventory.Contains(weapon)) {
    //        OnWeaponReadyAndEnemyInRange(weapon);
    //        UpdateReadyWeaponsInventory(weapon);
    //    }
    //}

    /// <summary>
    /// Called when [weapon ready and enemy in range].
    /// </summary>
    /// <param name="weapon">The weapon.</param>
    //private void OnWeaponReadyAndEnemyInRange(AWeapon weapon) {
    //    // the weapon is ready and the enemy is in range
    //    RelayToCurrentState(weapon);
    //}

    /// <summary>
    /// Updates the ready weapons inventory.
    /// </summary>
    /// <param name="weapon">The weapon.</param>
    //private void UpdateReadyWeaponsInventory(AWeapon weapon) {
    //    if (weapon.IsReadyToFire) {
    //        if (!_readyWeaponsInventory.Contains(weapon)) {
    //            _readyWeaponsInventory.Add(weapon);
    //            //D.Log("{0} added Weapon {1} to ReadyWeaponsInventory.", DebugName, weapon.Name);
    //        }
    //        else {
    //            //D.Log("{0} properly avoided adding duplicate Weapon {1} to ReadyWeaponsInventory.", DebugName, weapon.Name);
    //            // this occurs when a weapon attempts to fire but doesn't (usually due to LOS interference) and therefore remains
    //            // IsReadyToFire. If it had fired, it wouldn't be ready and therefore would have been removed below
    //        }
    //    }
    //    else {
    //        if (_readyWeaponsInventory.Contains(weapon)) {
    //            _readyWeaponsInventory.Remove(weapon);
    //            //D.Log("{0} removed Weapon {1} from ReadyWeaponsInventory.", DebugName, weapon.Name);
    //        }
    //    }
    //}

    #endregion

    #endregion

    #region Active Countermeasures

    /********************************************************************************************************************
     * ActiveCountermeasure target selection and firing handled automatically within ActiveCountermeasure class
     * Note: For previous approach to firing ActiveCountermeasures, see Weapons Firing Archive above
     *******************************************************************************************************************/

    /// <summary>
    /// Attaches this active countermeasure and its monitor to this item.
    /// </summary>
    /// <param name="activeCM">The cm.</param>
    private void Attach(ActiveCountermeasure activeCM) {
        D.AssertNotNull(activeCM.RangeMonitor);
        var monitor = activeCM.RangeMonitor;
        if (!CountermeasureRangeMonitors.Contains(monitor)) {
            // only need to record and setup range monitors once. The same monitor can have more than 1 weapon
            CountermeasureRangeMonitors.Add(monitor);
        }
    }

    #endregion

    #region Shield Generators

    /// <summary>
    /// Attaches this shield generator and its shield to this item.
    /// </summary>
    /// <param name="generator">The shield generator.</param>
    private void Attach(ShieldGenerator generator) {
        D.AssertNotNull(generator.Shield);
        var shield = generator.Shield;
        if (!Shields.Contains(shield)) {
            // only need to record and setup range monitors once. The same monitor can have more than 1 weapon
            Shields.Add(shield);
        }
    }

    #endregion

    #region Sensors

    private void Attach(ElementSensor sensor) {
        D.AssertNotNull(sensor.RangeMonitor);
        if (SRSensorMonitor == null) {
            // only need to record and setup range monitor once as there is only one. The monitor can have more than 1 sensor
            SRSensorMonitor = sensor.RangeMonitor;
        }
    }

    #endregion

    #region Event and Property Change Handlers

    private void ReworkUnderwayPropChangedHandler() {
        HandleReworkUnderwayPropChanged();
    }

    private void OnSubordinateDamageIncurred(bool isAlive, DamageStrength damageIncurred, float damageSeverity) {
        if (subordinateDamageIncurred != null) {
            subordinateDamageIncurred(this, new SubordinateDamageIncurredEventArgs(isAlive, damageIncurred, damageSeverity));
        }
    }

    private void OnSubordinateDeath() {
        if (subordinateDeathOneShot != null) {
            subordinateDeathOneShot(this, EventArgs.Empty);
            subordinateDeathOneShot = null;
        }
    }

    private void OnSubordinateOwnerChanging(Player incomingOwner) {
        if (subordinateOwnerChanging != null) {
            subordinateOwnerChanging(this, new SubordinateOwnerChangingEventArgs(incomingOwner));
        }
    }

    private void ChangeOwnerAfterPauseEventHandler(object sender, EventArgs e) {
        D.Log(ShowDebugLog, "{0}.ChangeOwnerAfterPauseEventHandler called.", DebugName);
        _gameMgr.isPausedChanged -= ChangeOwnerAfterPauseEventHandler;
        ChangeOwnerAfterPause();
    }

    private void IsAvailablePropChangedHandler() {
        if (IsAvailable) {
            __ValidateCurrentOrderAndStateWhenAvailable();
        }
        OnIsAvailable();
    }

    private void IsHQPropChangedHandler() {
        HandleIsHQChanged();
    }

    private void OnIsAvailable() {
        if (isAvailableChanged != null) {
            isAvailableChanged(this, EventArgs.Empty);
        }
    }

    private void OnIsHQChanged() {
        if (isHQChanged != null) {
            isHQChanged(this, EventArgs.Empty);
        }
    }

    private void WeaponReadyToFireEventHandler(object sender, AWeapon.WeaponFiringSolutionEventArgs e) {
        HandleWeaponReadyToFire(e.FiringSolutions);
    }

    private void LosWeaponAimedEventHandler(object sender, ALOSWeapon.LosWeaponFiringSolutionEventArgs e) {
        HandleLosWeaponAimed(e.FiringSolution);
    }

    #endregion

    private void HandleReworkUnderwayPropChanged() {
        if (ReworkUnderway == ReworkingMode.None) {
            DisplayMgr.HideReworkingVisuals();
        }
        else {
            DisplayMgr.BeginReworkingVisuals(ReworkUnderway);
        }
    }

    protected sealed override void HandleLeftDoubleClick() {
        base.HandleLeftDoubleClick();
        if (IsSelectable) {   // IMPROVE clearer criteria would be element's (and therefore command's) owner is user
            Command.IsSelected = true;
        }
    }

    protected virtual void HandleIsHQChanged() {
        Name = IsHQ ? Name + __HQNameAddendum : Name.Remove(__HQNameAddendum);
        OnIsHQChanged();
    }

    /// <summary>
    /// Handles a change in relations between players.
    /// <remarks> 7.14.16 Primary responsibility for handling Relations changes (existing relationship with a player changes) in Cmd
    /// and Element state machines rest with the Cmd. They implement HandleRelationsChanged and UponRelationsChanged.
    /// In all cases where the order is issued by either Cmd or User, the element FSM does not need to pay attention to Relations
    /// changes as their orders will be changed if a Relations change requires it, determined by Cmd. When the Captain
    /// overrides an order, those orders typically(so far) entail assuming station in one form or another, and/or repairing
    /// in place, sometimes in combination. A Relations change here should not affect any of these orders...so far.
    /// Upshot: Elements FSMs can ignore Relations changes.
    /// </remarks>
    /// </summary>
    /// <param name="player">The other player.</param>
    public void HandleRelationsChangedWith(Player player) {
        SRSensorMonitor.HandleRelationsChangedWith(player);
        WeaponRangeMonitors.ForAll(wrm => wrm.HandleRelationsChangedWith(player));
        CountermeasureRangeMonitors.ForAll(crm => crm.HandleRelationsChangedWith(player));
        UponRelationsChangedWith(player);
    }

    /// <summary>
    /// Called when this element is removed from the ConstructionQueue before its initial construction or refit is completed.
    /// </summary>
    public void HandleUncompletedRemovalFromConstructionQueue() {
        UponUncompletedRemovalFromConstructionQueue();
    }

    protected override void HandleIsVisualDetailDiscernibleToUserChanged() {
        base.HandleIsVisualDetailDiscernibleToUserChanged();
        Data.Weapons.ForAll(w => w.IsWeaponDiscernibleToUser = IsVisualDetailDiscernibleToUser);
    }

    protected override void HandleUserIntelCoverageChanged() {
        base.HandleUserIntelCoverageChanged();
        if (Command != null) {   // 4.24.17 If Cmd is null, a new LoneCmd is being created which will AssessIcon during FinalInitialize
            Command.AssessIcon();
        }
    }

    protected override void HandleOwnerChanging(Player incomingOwner) {
        base.HandleOwnerChanging(incomingOwner);
        D.AssertNotEqual(TempGameValues.NoPlayer, Owner);
        D.Assert(IsAssaultAllowedBy(incomingOwner));

        OnSubordinateOwnerChanging(incomingOwner);   // 5.17.17 If Cmd is going to chg owner, it must do it BEFORE all these other changes propagate

        // Owner is about to lose ownership of item so reset owner and allies IntelCoverage of item to what they should know
        ResetBasedOnCurrentDetection(Owner);

        IEnumerable<Player> allies;
        if (TryGetAllies(out allies)) {
            allies.ForAll(ally => {
                if (ally != incomingOwner && !ally.IsRelationshipWith(incomingOwner, DiplomaticRelationship.Alliance)) {
                    // 5.18.17 no point assessing current detection for newOwner or a newOwner ally
                    // as HandleOwnerChgd will assign Comprehensive to them all. 
                    ResetBasedOnCurrentDetection(ally);
                }
            });
        }
        // Note: A Cmd will track its HQ Element's IntelCoverage change for a player

        ReturnFromCalledStates();
        UponLosingOwnership();  // 4.20.17 Do any reqd Callback before exiting current non-Call()ed state
        ResetOrderAndState();
    }

    protected override void HandleOwnerChanged() {
        base.HandleOwnerChanged();
        if (DisplayMgr != null) {
            DisplayMgr.MeshColor = Owner.Color;
            AssessIcon();
        }
        // Checking weapon targeting on an OwnerChange is handled by WeaponRangeMonitor
    }

    #region Orders Support Members

    /// <summary>
    /// Cancels the CurrentOrder and (re)initiates Idling state unless its an override order from the Captain. Returns <c>true</c>
    /// if there was no CurrentOrder or the CurrentOrder was canceled, <c>false</c> if the CurrentOrder was not canceled
    /// due to it being issued by the Captain.
    /// <remarks>If CurrentOrder is from the Captain, then StandingOrder within the
    /// Captain's Order is canceled (nulled) as it is, by definition, from the Captain's Superior.
    /// </remarks>
    /// </summary>
    /// <returns></returns>
    internal abstract bool CancelSuperiorsOrder();

    protected abstract void ResetOrderAndState();

    #endregion

    #region StateMachine Support Members

    protected abstract bool IsCurrentStateCalled { get; }

    #region FsmReturnHandler and Callback System

    /// <summary>
    /// Indicates whether an order outcome failure callback to Cmd is allowed.
    /// <remarks>Typically, an order outcome failure callback is allowed until the ExecuteXXXOrder_EnterState
    /// successfully finishes executing, aka it wasn't interrupted by an event.</remarks>
    /// <remarks>4.9.17 Used to filter which OrderOutcome callbacks to events (e.g. XXX_UponNewOrderReceived()) 
    /// should be allowed. Typically, a callback will not occur from an event once the order has 
    /// successfully finished executing.</remarks>
    /// </summary>
    protected bool _allowOrderFailureCallback = true;

    /// <summary>
    /// Stack of FsmReturnHandlers that are currently in use. 
    /// <remarks>Allows use of nested Call()ed states.</remarks>
    /// </summary>
    protected Stack<FsmReturnHandler> _activeFsmReturnHandlers = new Stack<FsmReturnHandler>();

    /// <summary>
    /// Removes the FsmReturnHandler from the top of _activeFsmReturnHandlers. 
    /// Throws an error if not on top.
    /// </summary>
    /// <param name="handlerToRemove">The handler to remove.</param>
    protected void RemoveReturnHandlerFromTopOfStack(FsmReturnHandler handlerToRemove) {
        var topHandler = _activeFsmReturnHandlers.Pop();
        D.AssertEqual(topHandler, handlerToRemove);
    }

    /// <summary>
    /// Gets the FsmReturnHandler for the current Call()ed state.
    /// Throws an error if the CurrentState is not a Call()ed state or if not found.
    /// </summary>
    /// <returns></returns>
    protected FsmReturnHandler GetCurrentCalledStateReturnHandler() {
        D.Assert(IsCurrentStateCalled);
        D.AssertException(_activeFsmReturnHandlers.Count != Constants.Zero);
        string currentStateName = CurrentState.ToString();
        var peekHandler = _activeFsmReturnHandlers.Peek();
        if (peekHandler.CalledStateName != currentStateName) {
            // 4.11.17 When an event occurs in the 1 frame delay between Call()ing a state and processing the results
            D.Warn("{0}: {1} is not correct for state {2}. Replacing.", DebugName, peekHandler.DebugName, currentStateName);
            RemoveReturnHandlerFromTopOfStack(peekHandler);
            return GetCurrentCalledStateReturnHandler();
        }
        return peekHandler;
    }

    /// <summary>
    /// Gets the FsmReturnHandler for the Call()ed state named <c>calledStateName</c>.
    /// Throws an error if not found.
    /// <remarks>TEMP version that allows use in CalledState_ExitState methods where CurrentState has already changed.</remarks>
    /// </summary>
    /// <param name="calledStateName">Name of the Call()ed state.</param>
    /// <returns></returns>
    protected FsmReturnHandler __GetCalledStateReturnHandlerFor(string calledStateName) {
        D.AssertException(_activeFsmReturnHandlers.Count != Constants.Zero);
        var peekHandler = _activeFsmReturnHandlers.Peek();
        if (peekHandler.CalledStateName != calledStateName) {
            // 4.11.17 This can occur in the 1 frame delay between Call()ing a state and processing the results
            D.Warn("{0}: {1} is not correct for state {2}. Replacing.", DebugName, peekHandler.DebugName, calledStateName);
            RemoveReturnHandlerFromTopOfStack(peekHandler);
            return __GetCalledStateReturnHandlerFor(calledStateName);
        }
        return peekHandler;
    }

    #endregion

    /// <summary>
    /// Validates the common starting values of a State that is Call()able.
    /// </summary>
    protected virtual void ValidateCommonCallableStateValues(string calledStateName) {
        D.AssertNotEqual(Constants.Zero, _activeFsmReturnHandlers.Count);
        _activeFsmReturnHandlers.Peek().__Validate(calledStateName);
    }

    /// <summary>
    /// Validates the common starting values of a State that is not Call()able.
    /// </summary>
    protected virtual void ValidateCommonNotCallableStateValues() {
        D.AssertEqual(Constants.Zero, _activeFsmReturnHandlers.Count);
    }

    protected void ReturnFromCalledStates() {
        while (IsCurrentStateCalled) {
            Return();
        }
        D.Assert(!IsCurrentStateCalled);
    }

    protected void KillRepairJob() {
        if (_repairJob != null) {
            _repairJob.Kill();
            _repairJob = null;
        }
    }

    protected sealed override void PreconfigureCurrentState() {
        base.PreconfigureCurrentState();
        UponPreconfigureState();
    }

    protected void PrepareForRefit() {
        Data.DamageEquipment(Constants.OneHundredPercent);

    }

    protected void Dead_ExitState() {
        LogEventWarning();
    }

    #region Relays

    protected void UponEffectSequenceFinished(EffectSequenceID effectSeqID) { RelayToCurrentState(effectSeqID); }

    /// <summary>
    /// Called prior to entering the Dead state, this method notifies the current
    /// state that the element is dying, allowing any current state housekeeping
    /// required before entering the Dead state.
    /// </summary>
    protected void UponDeath() { RelayToCurrentState(); }

    /// <summary>
    /// Called prior to the Owner changing, this method notifies the current
    /// state that the element is losing ownership, allowing any current state housekeeping
    /// required before the Owner is changed.
    /// </summary>
    protected void UponLosingOwnership() { RelayToCurrentState(); }

    protected void UponNewOrderReceived() { RelayToCurrentState(); }

    /// <summary>
    /// Called from the StateMachine just after a state
    /// change and just before state_EnterState() is called. When EnterState
    /// is a coroutine method (returns IEnumerator), the relayed version
    /// of this method provides an opportunity to configure the state
    /// before any other event relay methods can be called during the state.
    /// </summary>
    private void UponPreconfigureState() { RelayToCurrentState(); }

    private void UponRelationsChangedWith(Player player) { RelayToCurrentState(player); }

    /// <summary>
    /// Called when the current target being used by the State Machine dies.
    /// </summary>
    /// <param name="deadFsmTgt">The dead target.</param>
    private void UponFsmTgtDeath(IMortalItem_Ltd deadFsmTgt) { RelayToCurrentState(deadFsmTgt); }

    private void UponFsmTgtInfoAccessChgd(IOwnerItem_Ltd fsmTgt) { RelayToCurrentState(fsmTgt); }

    private void UponFsmTgtOwnerChgd(IOwnerItem_Ltd fsmTgt) { RelayToCurrentState(fsmTgt); }

    private bool UponWeaponReadyToFire(IList<WeaponFiringSolution> firingSolutions) { return RelayToCurrentState(firingSolutions); }

    private void UponDamageIncurred() { RelayToCurrentState(); }

    private void UponUncompletedRemovalFromConstructionQueue() { RelayToCurrentState(); }

    /// <summary>
    /// Called when the HQ status of this element changes.
    /// <remarks>3.21.17 Upon receiving this event, this element either just became the HQ during runtime or is the 
    /// old HQ going back to its normal duty.</remarks>
    /// <remarks>Virtual to allow comments for ships on why this is important.</remarks>
    /// </summary>
    protected virtual void UponHQStatusChangeCompleted() { RelayToCurrentState(); }

    #endregion

    #region Repair Support

    /// <summary>
    /// Assesses this element's need for repair, returning <c>true</c> if immediate repairs are needed, <c>false</c> otherwise.
    /// <remarks>Abstract to simply remind of need for functionality.</remarks>
    /// </summary>
    /// <param name="healthThreshold">The health threshold.</param>
    /// <returns></returns>
    protected abstract bool AssessNeedForRepair(float healthThreshold);

    #endregion

    #region Combat Support

    protected override void AssessCripplingDamageToEquipment(float damageSeverity) {
        base.AssessCripplingDamageToEquipment(damageSeverity);
        var equipDamageChance = damageSeverity;

        var undamagedDamageableWeapons = Data.Weapons.Where(w => w.IsDamageable && !w.IsDamaged);
        undamagedDamageableWeapons.ForAll(w => {
            w.IsDamaged = RandomExtended.Chance(equipDamageChance);
            //D.Log(ShowDebugLog && w.IsDamaged, "{0}'s weapon {1} has been damaged.", DebugName, w.Name);
        });

        var undamagedDamageableSensors = Data.Sensors.Where(s => s.IsDamageable && !s.IsDamaged);
        undamagedDamageableSensors.ForAll(s => {
            s.IsDamaged = RandomExtended.Chance(equipDamageChance);
            //D.Log(ShowDebugLog && s.IsDamaged, "{0}'s sensor {1} has been damaged.", DebugName, s.Name);
        });

        var undamagedDamageableActiveCMs = Data.ActiveCountermeasures.Where(cm => cm.IsDamageable && !cm.IsDamaged);
        undamagedDamageableActiveCMs.ForAll(cm => {
            cm.IsDamaged = RandomExtended.Chance(equipDamageChance);
            //D.Log(ShowDebugLog && cm.IsDamaged, "{0}'s ActiveCM {1} has been damaged.", DebugName, cm.Name);
        });

        var undamagedDamageableGenerators = Data.ShieldGenerators.Where(gen => gen.IsDamageable && !gen.IsDamaged);
        undamagedDamageableGenerators.ForAll(gen => {
            gen.IsDamaged = RandomExtended.Chance(equipDamageChance);
            //D.Log(ShowDebugLog && gen.IsDamaged, "{0}'s shield generator {1} has been damaged.", DebugName, gen.Name);
        });
    }

    #endregion

    #endregion

    #region Show Icon

    private void InitializeIcon() {
        _debugCntls.showElementIcons += ShowElementIconsChangedEventHandler;
        if (_debugCntls.ShowElementIcons) {
            EnableIcon(true);
        }
    }

    private void EnableIcon(bool toEnable) {
        if (toEnable) {
            if (DisplayMgr.TrackingIcon == null) {
                DisplayMgr.IconInfo = MakeIconInfo();
                SubscribeToIconEvents(DisplayMgr.TrackingIcon);
            }
        }
        else {
            if (DisplayMgr.TrackingIcon != null) {
                UnsubscribeToIconEvents(DisplayMgr.TrackingIcon);
                DisplayMgr.IconInfo = null;
            }
        }
    }

    private void AssessIcon() {
        if (DisplayMgr != null) {
            if (DisplayMgr.TrackingIcon != null) {
                var iconInfo = RefreshIconInfo();
                if (DisplayMgr.IconInfo != iconInfo) {    // avoid property not changed warning
                    UnsubscribeToIconEvents(DisplayMgr.TrackingIcon);
                    //D.Log(ShowDebugLog, "{0} changing IconInfo from {1} to {2}.", DebugName, DisplayMgr.IconInfo, iconInfo);
                    DisplayMgr.IconInfo = iconInfo;
                    SubscribeToIconEvents(DisplayMgr.TrackingIcon);
                }
            }
            else {
                D.Assert(!_debugCntls.ShowElementIcons);
            }
        }
    }

    private void SubscribeToIconEvents(IInteractiveWorldTrackingSprite icon) {
        var iconEventListener = icon.EventListener;
        iconEventListener.onHover += HoverEventHandler;
        iconEventListener.onClick += ClickEventHandler;
        iconEventListener.onDoubleClick += DoubleClickEventHandler;
        iconEventListener.onPress += PressEventHandler;
    }

    private TrackingIconInfo RefreshIconInfo() {
        return MakeIconInfo();
    }

    protected abstract TrackingIconInfo MakeIconInfo();

    private void ShowElementIconsChangedEventHandler(object sender, EventArgs e) {
        EnableIcon(_debugCntls.ShowElementIcons);
    }

    /// <summary>
    /// Cleans up any icon subscriptions.
    /// <remarks>The icon itself will be cleaned up when DisplayMgr.Dispose() is called.</remarks>
    /// </summary>
    private void CleanupIconSubscriptions() {
        if (_debugCntls != null) {
            _debugCntls.showElementIcons -= ShowElementIconsChangedEventHandler;
        }
        if (DisplayMgr != null) {
            var icon = DisplayMgr.TrackingIcon;
            if (icon != null) {
                UnsubscribeToIconEvents(icon);
            }
        }
    }

    private void UnsubscribeToIconEvents(IInteractiveWorldTrackingSprite icon) {
        var iconEventListener = icon.EventListener;
        iconEventListener.onHover -= HoverEventHandler;
        iconEventListener.onClick -= ClickEventHandler;
        iconEventListener.onDoubleClick -= DoubleClickEventHandler;
        iconEventListener.onPress -= PressEventHandler;
    }

    #region Element Icon Preference Archive

    // 1.16.17 TEMP Replaced User Option/Preference with easily accessible DebugControls setting
    //  - Graphics Options Menu Window's ElementIcons Checkbox has been deactivated.
    //  - PlayerPrefsMgr's preference value implementation has been commented out

    //protected override void Subscribe() {
    //    base.Subscribe();
    //    _subscriptions.Add(PlayerPrefsManager.Instance.SubscribeToPropertyChanged<PlayerPrefsManager, bool>(ppm => ppm.IsElementIconsEnabled, IsElementIconsEnabledPropChangedHandler));
    //}

    //protected sealed override void InitializeDisplayManager() {
    //    base.InitializeDisplayManager();
    //    if (PlayerPrefsManager.Instance.IsElementIconsEnabled) {
    //        DisplayMgr.IconInfo = MakeIconInfo();
    //        SubscribeToIconEvents(DisplayMgr.Icon);
    //    }
    //    DisplayMgr.MeshColor = Owner.Color;
    //}

    //private void IsElementIconsEnabledPropChangedHandler() {
    //    if (DisplayMgr != null) {
    //        AssessIcon();
    //    }
    //}

    //private void AssessIcon() {
    //    D.AssertNotNull(DisplayMgr);

    //    if (PlayerPrefsManager.Instance.IsElementIconsEnabled) {
    //        var iconInfo = RefreshIconInfo();
    //        if (DisplayMgr.IconInfo != iconInfo) {    // avoid property not changed warning
    //            UnsubscribeToIconEvents(DisplayMgr.Icon);
    //            //D.Log(ShowDebugLog, "{0} changing IconInfo from {1} to {2}.", DebugName, DisplayMgr.IconInfo, iconInfo);
    //            DisplayMgr.IconInfo = iconInfo;
    //            SubscribeToIconEvents(DisplayMgr.Icon);
    //        }
    //    }
    //    else {
    //        if (DisplayMgr.Icon != null) {
    //            UnsubscribeToIconEvents(DisplayMgr.Icon);
    //            DisplayMgr.IconInfo = default(IconInfo);
    //        }
    //    }
    //}

    //protected override void Unsubscribe() {
    //    base.Unsubscribe();
    //    if (DisplayMgr != null) {
    //        var icon = DisplayMgr.Icon;
    //        if (icon != null) {
    //            UnsubscribeToIconEvents(icon);
    //        }
    //    }
    //}

    #endregion

    #endregion

    #region Debug

    public bool __HasCommand { get { return Command != null; } }

    [System.Diagnostics.Conditional("DEBUG")]
    protected abstract void __ValidateCurrentOrderAndStateWhenAvailable();

    public override bool __IsPlayerEntitledToComprehensiveRelationship(Player player) {
        if (_debugCntls.IsAllIntelCoverageComprehensive) {
            return true;
        }
        if (IsOwnerChangeUnderway) {
            return true;
        }
        bool isEntitled = Owner.IsRelationshipWith(player, DiplomaticRelationship.Self, DiplomaticRelationship.Alliance);
        if (!isEntitled) {
            //D.Warn("{0} is not entitled to Comprehensive IntelCoverage. IsOwnerChangeUnderway: {1}.", DebugName, IsOwnerChangeUnderway);
        }
        return isEntitled;
    }

    /// <summary>
    /// Debug flag used to decide when to warn or not warn about Idling state
    /// receiving _fsmTgt-related events like FsmInfoAccessChgd, FsmOwnerChgd and FsmDeath.
    /// <remarks>4.18.17 Idling can receive these events even though it has not subscribed to 
    /// them in some circumstances. When Cmd and its elements have subscribed to a FsmInfoAccessChgd
    /// event for a System, and the event is raised, Cmd is the first to receive the event. Upon
    /// receiving the event, Cmd may decide to exit its current state which will immediately cancel
    /// all element orders and change their state to Idling. Even though each element will 
    /// remove its subscription to the event upon exiting its current state, the event will still
    /// show up in Idling. This flag allows me to ignore events 
    /// arriving in Idling when I expect them, and to warn me when I don't expect them.</remarks>
    /// <remarks>4.18.17 My Theory was this is because the event's InvocationList has been copied to keep
    /// it from being modified while it is iterating. 5.19.17 I now know that the InvocationList is a linked
    /// list so shouldn't need the iteration copy?</remarks>
    /// </summary>
    protected bool __warnWhenIdlingReceivesFsmTgtEvents = true;

    public bool __TryGetIsHQChangedEventSubscribers(out string targetNames) {
        if (isHQChanged != null) {
            targetNames = isHQChanged.GetInvocationList().Select(d => d.Target.ToString()).Concatenate();
            return true;
        }
        targetNames = null;
        return false;
    }

    [System.Diagnostics.Conditional("DEBUG")]
    protected abstract void __ValidateRadius(float radius);

    // 3.7.17 Moved __ReportCollision(Collision collision) to Facility and Ship as needs differ

    /// <summary>
    /// The current speed of the element in Units per hour including any current drift velocity. 
    /// <remarks>For debug. Uses Rigidbody.velocity. Not valid while paused.</remarks>
    /// </summary>
    protected float __ActualSpeedValue {
        get {
            Vector3 velocityPerSec = Rigidbody.velocity;
            float value = velocityPerSec.magnitude / GameTime.Instance.GameSpeedAdjustedHoursPerSecond;
            //D.Log(ShowDebugLog, "{0}.ActualSpeedValue = {1:0.00}.", DebugName, value);
            return value;
        }
    }

    #endregion

    #region Cleanup

    protected override void Cleanup() {
        base.Cleanup();
        if (_detectionHandler != null) {
            _detectionHandler.Dispose();
        }
    }

    protected override void Unsubscribe() {
        base.Unsubscribe();
        CleanupIconSubscriptions();
        _gameMgr.isPausedChanged -= ChangeOwnerAfterPauseEventHandler;
    }

    #endregion

    #region AssaultShuttle Sim Archive

    // 5.20.17 Atomic AssaultShuttle attacks without using Ordnance

    //private void LaunchOrdnance(AWeapon weapon, IElementAttackable target) {
    //    Vector3 launchLoc = weapon.WeaponMount.MuzzleLocation;
    //    Quaternion launchRotation = Quaternion.LookRotation(weapon.WeaponMount.MuzzleFacing);
    //    WDVCategory category = weapon.DeliveryVehicleCategory;
    //    Transform ordnanceTransform;
    //    if (category == WDVCategory.Beam) {
    //        ordnanceTransform = GamePoolManager.Instance.Spawn(category, launchLoc, launchRotation, weapon.WeaponMount.Muzzle);
    //        Beam beam = ordnanceTransform.GetComponent<Beam>();
    //        beam.Launch(target, weapon);
    //    }
    //    else if (category == WDVCategory.AssaultShuttle) {    // 5.20.17 Sim approach prior to using real AssaultShuttles
    //        IAssaultable assaultTgt = target as IAssaultable;
    //        D.AssertNotNull(assaultTgt);
    //        D.Assert(assaultTgt.IsAssaultAllowedBy(Owner));
    //        D.Log(ShowDebugLog, "{0} is assaulting {1} with value {2:0.00}.", DebugName, assaultTgt.DebugName, weapon.DamagePotential.Total);
    //        string prevAssaultTgtName = assaultTgt.DebugName;
    //        bool isAssaultSuccessful = assaultTgt.AttemptAssault(Owner, weapon.DamagePotential);
    //        if (isAssaultSuccessful) {
    //            D.LogBold(/*ShowDebugLog, */"{0} has assaulted {1} and taken it over, creating {2}.", DebugName, prevAssaultTgtName, assaultTgt.DebugName);
    //        }
    //    }
    //    else {
    //        // Projectiles are located under PoolingManager in the scene
    //        ordnanceTransform = GamePoolManager.Instance.Spawn(category, launchLoc, launchRotation);
    //        Collider ordnanceCollider = UnityUtility.ValidateComponentPresence<Collider>(ordnanceTransform.gameObject);
    //        D.Assert(ordnanceTransform.gameObject.activeSelf);  // ordnanceGo must be active for IgnoreCollision
    //        Physics.IgnoreCollision(ordnanceCollider, _primaryCollider);

    //        if (category == WDVCategory.Missile) {
    //            Missile missile = ordnanceTransform.GetComponent<Missile>();
    //            missile.ElementVelocityAtLaunch = Rigidbody.velocity;
    //            missile.Launch(target, weapon, Topography);
    //        }
    //        else {
    //            D.AssertEqual(WDVCategory.Projectile, category);
    //            Projectile projectile = ordnanceTransform.GetComponent<Projectile>();
    //            projectile.Launch(target, weapon, Topography);
    //        }
    //    }
    //    //D.Log(ShowDebugLog, "{0} has fired {1} against {2} on {3}.", DebugName, ordnance.Name, target.DebugName, GameTime.Instance.CurrentDate);
    //}

    #endregion

    #region Nested Classes

    public class SubordinateOwnerChangingEventArgs : EventArgs {

        public Player IncomingOwner { get; private set; }

        public SubordinateOwnerChangingEventArgs(Player incomingOwner) {
            IncomingOwner = incomingOwner;
        }

    }

    public class SubordinateDamageIncurredEventArgs : EventArgs {

        public bool IsAlive { get; private set; }

        public DamageStrength DamageIncurred { get; private set; }

        public float DamageSeverity { get; private set; }

        public SubordinateDamageIncurredEventArgs(bool isAlive, DamageStrength damageIncurred, float damageSeverity) {
            IsAlive = isAlive;
            DamageIncurred = damageIncurred;
            DamageSeverity = damageSeverity;
        }
    }

    #endregion

    #region IElementAttackable Members

    public override void TakeHit(DamageStrength damagePotential) {
        LogEvent();
        if (_debugSettings.AllPlayersInvulnerable) {
            return;
        }
        D.Assert(!IsDead);
        DamageStrength damageSustained = damagePotential - Data.DamageMitigation;
        if (damageSustained.Total == Constants.ZeroF) {
            //D.Log(ShowDebugLog, "{0} has been hit but incurred no damage.", DebugName);
            return;
        }
        D.Log(ShowDebugLog, "{0} has been hit, taking {1:0.#} damage.", DebugName, damageSustained.Total);

        float damageSeverity;
        bool isElementAlive = ApplyDamage(damageSustained, out damageSeverity);
        if (isElementAlive) {
            StartEffectSequence(EffectSequenceID.Hit);
            UponDamageIncurred();
        }

        OnSubordinateDamageIncurred(isElementAlive, damageSustained, damageSeverity);
        if (!isElementAlive) {
            IsDead = true;
        }
    }

    #endregion

    #region IShipBlastable Members

    public abstract ApBesiegeDestinationProxy GetApBesiegeTgtProxy(ValueRange<float> desiredWeaponsRangeEnvelope, IShip ship);

    public abstract ApStrafeDestinationProxy GetApStrafeTgtProxy(ValueRange<float> desiredWeaponsRangeEnvelope, IShip ship);

    /// <summary>
    /// Returns the shortest distance between the element's center (position)
    /// and a surface that could incur a weapon impact.
    /// <remarks>UNCLEAR I believe HullDimensions is derived from the collider 
    /// that was fitted around the hull mesh.</remarks>
    /// </summary>
    /// <returns></returns>
    protected float GetDistanceToClosestWeaponImpactSurface() {
        Vector3 hullDimensions = Data.HullDimensions;
        float shortestHullDimension = hullDimensions.x;
        if (hullDimensions.y < shortestHullDimension) {
            shortestHullDimension = hullDimensions.y;
        }
        if (hullDimensions.z < shortestHullDimension) {
            shortestHullDimension = hullDimensions.z;
        }
        return shortestHullDimension / 2F;
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

    #region IUnitElement Members

    IUnitCmd IUnitElement.Command { get { return Command as IUnitCmd; } }

    #endregion

    #region IUnitElement_Ltd Members

    IUnitCmd_Ltd IUnitElement_Ltd.Command { get { return Command as IUnitCmd_Ltd; } }

    #endregion

    #region IFsmEventSubscriptionMgrClient Members

    void IFsmEventSubscriptionMgrClient.HandleFsmTgtInfoAccessChgd(IOwnerItem_Ltd fsmTgt) {
        D.Log(ShowDebugLog, "{0}'s access to info about FsmTgt {1} has changed.", DebugName, fsmTgt.DebugName);
        UponFsmTgtInfoAccessChgd(fsmTgt);
    }

    void IFsmEventSubscriptionMgrClient.HandleFsmTgtOwnerChgd(IOwnerItem_Ltd fsmTgt) {
        UponFsmTgtOwnerChgd(fsmTgt);
    }

    void IFsmEventSubscriptionMgrClient.HandleFsmTgtDeath(IMortalItem_Ltd deadFsmTgt) {
        UponFsmTgtDeath(deadFsmTgt);
    }

    void IFsmEventSubscriptionMgrClient.HandleAwarenessChgd(IMortalItem_Ltd item) { }

    #endregion

    #region IAssaultable Members

    public virtual bool IsAssaultAllowedBy(Player player) {
        if (!IsAttackAllowedBy(player)) {
            return false;
        }
        if (!InfoAccessCntlr.HasIntelCoverageReqdToAccess(player, ItemInfoID.Defense)) {
            return false;
        }
        // 5.18.17 Can't use dynamically changing factors where a WRM can't subscribe to 
        // changes in as this is used by weapons to place targets into the QualifiedTargets
        // collection which determines whether the target is fired on. Examples that shouldn't
        // be used here include __lastAssaultFrame and __IsActivelyOperating. 
        return true;
    }

    private int __lastAssaultFrame;

    /// <summary>
    /// Attempts to takeover this Element's ownership with player. Returns <c>true</c> if successful, <c>false</c> otherwise.
    /// </summary>
    /// <param name="player">The player.</param>
    /// <param name="damagePotential">The damage potential of the assault.</param>
    /// <returns></returns>
    public bool AttemptAssault(Player player, DamageStrength damagePotential, string __assaulterName) {
        D.Assert(!IsPaused);
        if (!IsAssaultAllowedBy(player)) {
            D.Error("{0} erroneously assaulted by {1} in Frame {2}. IsAttackAllowedBy: {3}, HasAccessToDefense: {4}.",
                DebugName, __assaulterName, Time.frameCount, IsAttackAllowedBy(player), InfoAccessCntlr.HasIntelCoverageReqdToAccess(player, ItemInfoID.Defense));
        }

        int currentFrame = Time.frameCount;
        if (IsOwnerChangeUnderway) {
            // 5.17.17 Multiple assaults in the same frame by the same or other players can occur even if 
            // AssaultShuttles aren't instantaneous.
            // 5.22.17 Changed to warn to reconfirm this takes place with real AssaultVehicles
            D.Warn(/*ShowDebugLog, */"{0} assault is not allowed by {1} in Frame {2} when owner change underway.",
                DebugName, __assaulterName, currentFrame);
            return false;
        }

        if (!Command.__IsActivelyOperating) {
            // 5.22.17 Changed to Error to see if this still occurs with real AssaultVehicles. I know with instantaneous
            // Assaults it did occur on LoneFleetCmds
            D.Error("FYI. {0} assault is not allowed by {1} in Frame {2} when {3} has not yet CommencedOperations.",
                DebugName, __assaulterName, currentFrame, Command.DebugName);
            return false;
        }

        if (__lastAssaultFrame == currentFrame) {
            // 5.22.17 FIXME Multiple assaults in the same frame by the same or other players can occur even if 
            // AssaultShuttles aren't instantaneous. Confirmed this does occur. I think multiple changes in the same frame 
            // may have something to do with the infrequent but bad behaviour I see with SensorMonitor's OnTriggerExit warnings
            // (items exiting at wrong distance). I'm speculating that multiple collider enable/disable in the same frame 
            // creates some instability.
            D.Log(/*ShowDebugLog,*/ "{0} assault is not allowed by {1} in Frame {2} when previously assaulted in same frame.",
                DebugName, __assaulterName, currentFrame);
            return false;
        }
        __lastAssaultFrame = currentFrame;

        if (_debugSettings.AllPlayersInvulnerable) {
            return false;
        }
        if (_debugCntls.AreAssaultsAlwaysSuccessful) {
            Data.Owner = player;
            return true;
        }

        DamageStrength damageSustained = damagePotential - Data.DamageMitigation;
        if (damageSustained.Total == Constants.ZeroF) {
            //D.Log(ShowDebugLog, "{0} has been assaulted but incurred no damage.", DebugName);
            return false;
        }
        D.Log(ShowDebugLog, "{0} has been assaulted, taking {1:0.#} damage.", DebugName, damageSustained.Total);

        float damageSeverity;
        bool isElementAlive = ApplyDamage(damageSustained, out damageSeverity);
        if (isElementAlive) {
            StartEffectSequence(EffectSequenceID.Hit);
            UponDamageIncurred();
        }
        // 11.4.17 Eliminated Cmd 'taking hit' if this IsHQ. Can reinstate if desired in Cmd's SubordinateDamageIncurredHandler
        OnSubordinateDamageIncurred(isElementAlive, damageSustained, damageSeverity);

        if (isElementAlive) {
            if (damageSustained.GetValue(DamageCategory.Incursion) > Constants.ZeroF) {
                // assault was successful
                Data.Owner = player;
                return true;
            }
        }
        else {
            IsDead = true;
        }
        return false;
    }

    public void __ChangeOwner(Player newOwner) {
        D.AssertNotEqual(Owner, newOwner, DebugName);
        if (IsPaused) {
            _newOwnerAfterPause = newOwner;
            // deal with multiple changes all while paused
            _gameMgr.isPausedChanged -= ChangeOwnerAfterPauseEventHandler;
            _gameMgr.isPausedChanged += ChangeOwnerAfterPauseEventHandler;
            D.Log(/*ShowDebugLog, */"{0}.__ChangeOwner({1}) called while paused.", DebugName, newOwner.DebugName);
        }
        else {
            D.AssertNull(_newOwnerAfterPause);
            Data.Owner = newOwner;
        }
    }

    #endregion

}

