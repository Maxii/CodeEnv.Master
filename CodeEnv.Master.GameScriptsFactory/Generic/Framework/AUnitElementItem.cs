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
using CodeEnv.Master.Common.LocalResources;
using CodeEnv.Master.GameContent;
using PathologicalGames;
using UnityEngine;
using UnityEngine.Profiling;

/// <summary>
/// Abstract class for AMortalItem's that are Unit Elements.
/// </summary>
public abstract class AUnitElementItem : AMortalItemStateMachine, IUnitElement, IUnitElement_Ltd, ICameraFollowable, IShipAttackable, ISensorDetectable {

    private const string __HQNameAddendum = "[HQ]";

    public event EventHandler isHQChanged;

    public event EventHandler isAvailableChanged;

    /// <summary>
    /// Indicates whether this element is available for a new assignment.
    /// <remarks>Typically, an element that is available is Idling.</remarks>
    /// </summary>
    private bool _isAvailable;
    public bool IsAvailable {
        get { return _isAvailable; }
        protected set { SetProperty<bool>(ref _isAvailable, value, "IsAvailable", IsAvailablePropChangedHandler); }
    }

    /// <summary>
    /// Indicates whether this element is capable of attacking an enemy target.
    /// </summary>
    public abstract bool IsAttackCapable { get; }

    public new AUnitElementData Data {
        get { return base.Data as AUnitElementData; }
        set { base.Data = value; }
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

    private AUnitCmdItem _command;
    public AUnitCmdItem Command {
        get { return _command; }
        set { SetProperty<AUnitCmdItem>(ref _command, value, "Command"); }
    }

    // OPTIMIZE all elements followable for now to support facilities rotating around bases or stars
    public new FollowableItemCameraStat CameraStat {
        protected get { return base.CameraStat as FollowableItemCameraStat; }
        set { base.CameraStat = value; }
    }

    protected new AElementDisplayManager DisplayMgr { get { return base.DisplayMgr as AElementDisplayManager; } }
    protected IList<IWeaponRangeMonitor> WeaponRangeMonitors { get; private set; }
    protected IList<IActiveCountermeasureRangeMonitor> CountermeasureRangeMonitors { get; private set; }
    protected IList<IShield> Shields { get; private set; }
    protected Rigidbody Rigidbody { get; private set; }

    protected Job _repairJob;

    private DetectionHandler _detectionHandler;
    private BoxCollider _primaryCollider;

    #region Initialization

    protected override void InitializeOnAwake() {
        base.InitializeOnAwake();
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

    protected override void SubscribeToDataValueChanges() {
        base.SubscribeToDataValueChanges();
        //TODO: Weapon values don't change but weapons do so I need to know when that happens
    }

    protected sealed override void InitializeDisplayManager() {
        base.InitializeDisplayManager();
        // 1.16.17 TEMP Replaced User Option/Preference with easily accessible DebugControls setting
        InitializeIcon();
        DisplayMgr.MeshColor = Owner.Color;
    }

    protected sealed override CircleHighlightManager InitializeCircleHighlightMgr() {
        float circleRadius = Radius * Screen.height * 1F;
        return new CircleHighlightManager(transform, circleRadius);
    }

    protected sealed override HoverHighlightManager InitializeHoverHighlightMgr() {
        return new HoverHighlightManager(this, Radius);
    }

    public override void FinalInitialize() {
        base.FinalInitialize();
        InitializeMonitorRanges();
        Rigidbody.isKinematic = false;
    }

    private void InitializeMonitorRanges() {
        CountermeasureRangeMonitors.ForAll(crm => crm.InitializeRangeDistance());
        WeaponRangeMonitors.ForAll(wrm => wrm.InitializeRangeDistance());
        Shields.ForAll(srm => srm.InitializeRangeDistance());
    }

    #endregion

    public override void CommenceOperations() {
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
            // this change is reqd include a ship 'joins' another fleet, a facility 'joins?' a base
            transform.parent = unitContainer;
        }
    }

    /// <summary>
    /// Called when the local position of this element has been manually changed by its Command.
    /// <remarks>1.21.17 Currently called when the FormationMgr manually repositions
    /// the element. For ships, manual repositioning only occurs when the formation is 
    /// initially changed before it becomes operational. IMPROVE For facilities, manual repositioning
    /// occurs even if operational which of course will have to be improved.</remarks>
    /// </summary>
    public abstract void HandleLocalPositionManuallyChanged();

    protected override void HandleDeathBeforeBeginningDeathEffect() {
        base.HandleDeathBeforeBeginningDeathEffect();
        // Note: Keep the primaryCollider enabled until destroyed or returned to the pool as this allows 
        // in-route ordnance to show its impact effect while the item is showing its death.
        Data.Weapons.ForAll(w => {
            w.readytoFire -= WeaponReadyToFireEventHandler;
            w.IsActivated = false;
        });
        Data.ActiveCountermeasures.ForAll(cm => cm.IsActivated = false);
        Data.Sensors.ForAll(s => s.IsActivated = false);
        Data.ShieldGenerators.ForAll(gen => gen.IsActivated = false);

        Command.HandleSubordinateElementDeath(this);
    }

    /// <summary>
    /// Assigns its Command as the focus to replace it. 
    /// <remarks>If the last element to die then Command will shortly die 
    /// after HandleSubordinateElementDeath() called. This in turn
    /// will null the MainCameraControl.CurrentFocus property.
    /// </remarks>
    /// </summary>
    protected override void AssignAlternativeFocusOnDeath() {
        base.AssignAlternativeFocusOnDeath();
        Command.IsFocus = true;
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
        if (tgtHint == null) {
            return firingSolutions.First();
        }
        var hintFiringSolution = firingSolutions.SingleOrDefault(fs => fs.EnemyTarget == tgtHint);
        if (hintFiringSolution == null) {
            return firingSolutions.First();
        }
        return hintFiringSolution;
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
            ordnanceTransform = MyPoolManager.Instance.Spawn(category, launchLoc, launchRotation, weapon.WeaponMount.Muzzle);
            Beam beam = ordnanceTransform.GetComponent<Beam>();
            beam.Launch(target, weapon);
        }
        else {
            // Projectiles are located under PoolManager in the scene
            ordnanceTransform = MyPoolManager.Instance.Spawn(category, launchLoc, launchRotation);
            Collider ordnanceCollider = UnityUtility.ValidateComponentPresence<Collider>(ordnanceTransform.gameObject);
            ////D.Assert(!ordnanceCollider.enabled);
            D.Assert(ordnanceTransform.gameObject.activeSelf);  // ordnanceGo must be active for IgnoreCollision
            Physics.IgnoreCollision(ordnanceCollider, _primaryCollider);
            ////D.Assert(!ordnanceCollider.enabled);    // makes sure IgnoreCollision doesn't enable collider

            if (category == WDVCategory.Missile) {
                Missile missile = ordnanceTransform.GetComponent<Missile>();
                ////D.Assert(!missile.enabled);
                missile.ElementVelocityAtLaunch = Rigidbody.velocity;
                missile.Launch(target, weapon, Topography);
            }
            else {
                D.AssertEqual(WDVCategory.Projectile, category);
                Projectile projectile = ordnanceTransform.GetComponent<Projectile>();
                ////D.Assert(!projectile.enabled);
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
        if (target.IsOperational && target.IsAttackByAllowed(Owner) && losWeapon.ConfirmInRangeForLaunch(target)) {
            LaunchOrdnance(losWeapon, target);
        }
        else {
            // target moved out of range, died or changed diplomatic during aiming process
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

    private void Attach(Sensor sensor) {
        D.AssertNull(Command);  // OPTIMIZE Sensors are only attached to elements when an element is being created so there is no Command yet
    }

    #endregion

    #region Highlighting

    public override void AssessCircleHighlighting() {
        if (IsDiscernibleToUser) {
            if (IsFocus) {
                if (IsSelected) {
                    ShowCircleHighlights(CircleHighlightID.Focused, CircleHighlightID.Selected);
                    return;
                }
                if (Command.IsSelected) {
                    ShowCircleHighlights(CircleHighlightID.Focused, CircleHighlightID.UnitElement);
                    return;
                }
                ShowCircleHighlights(CircleHighlightID.Focused);
                return;
            }
            if (IsSelected) {
                ShowCircleHighlights(CircleHighlightID.Selected);
                return;
            }
            if (Command.IsSelected) {
                ShowCircleHighlights(CircleHighlightID.UnitElement);
                return;
            }
        }
        ShowCircleHighlights(CircleHighlightID.None);
    }

    #endregion

    #region Event and Property Change Handlers

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
    /// <param name="chgdRelationsPlayer">The other player.</param>
    public void HandleRelationsChanged(Player chgdRelationsPlayer) {
        WeaponRangeMonitors.ForAll(wrm => wrm.HandleRelationsChanged(chgdRelationsPlayer));
        CountermeasureRangeMonitors.ForAll(crm => crm.HandleRelationsChanged(chgdRelationsPlayer));
        UponRelationsChanged(chgdRelationsPlayer);
    }

    private void IsAvailablePropChangedHandler() {
        OnIsAvailable();
    }

    private void OnIsAvailable() {
        if (isAvailableChanged != null) {
            isAvailableChanged(this, EventArgs.Empty);
        }
    }

    private void IsHQPropChangedHandler() {
        HandleIsHQChanged();
    }

    protected virtual void HandleIsHQChanged() {
        Name = IsHQ ? Name + __HQNameAddendum : Name.Remove(__HQNameAddendum);
        OnIsHQChanged();
    }

    private void OnIsHQChanged() {
        if (isHQChanged != null) {
            isHQChanged(this, EventArgs.Empty);
        }
    }

    protected override void HandleOwnerChanged() {
        base.HandleOwnerChanged();
        if (DisplayMgr != null) {
            DisplayMgr.MeshColor = Owner.Color;
            AssessIcon();
        }
        // Checking weapon targeting on an OwnerChange is handled by WeaponRangeMonitor
    }

    private void WeaponReadyToFireEventHandler(object sender, AWeapon.WeaponFiringSolutionEventArgs e) {
        HandleWeaponReadyToFire(e.FiringSolutions);
    }

    private void LosWeaponAimedEventHandler(object sender, ALOSWeapon.LosWeaponFiringSolutionEventArgs e) {
        HandleLosWeaponAimed(e.FiringSolution);
    }

    protected override void HandleIsVisualDetailDiscernibleToUserChanged() {
        base.HandleIsVisualDetailDiscernibleToUserChanged();
        Data.Weapons.ForAll(w => w.IsWeaponDiscernibleToUser = IsVisualDetailDiscernibleToUser);
    }

    protected override void HandleUserIntelCoverageChanged() {
        base.HandleUserIntelCoverageChanged();
        Command.AssessIcon();
    }

    protected override void HandleLeftDoubleClick() {
        base.HandleLeftDoubleClick();
        Command.IsSelected = true;
    }

    private void FsmTgtDeathEventHandler(object sender, EventArgs e) {
        IMortalItem_Ltd deadFsmTgt = sender as IMortalItem_Ltd;
        UponFsmTgtDeath(deadFsmTgt);
    }

    private void FsmTgtInfoAccessChgdEventHandler(object sender, InfoAccessChangedEventArgs e) {
        IItem_Ltd fsmTgt = sender as IItem_Ltd;
        HandleFsmTgtInfoAccessChgd(e.Player, fsmTgt);
    }

    private void HandleFsmTgtInfoAccessChgd(Player playerWhoseInfoAccessChgd, IItem_Ltd fsmTgt) {
        if (playerWhoseInfoAccessChgd == Owner) {
            UponFsmTgtInfoAccessChgd(fsmTgt);
        }
    }

    private void FsmTgtOwnerChgdEventHandler(object sender, EventArgs e) {
        IItem_Ltd fsmTgt = sender as IItem_Ltd;
        HandleFsmTgtOwnerChgd(fsmTgt);
    }

    private void HandleFsmTgtOwnerChgd(IItem_Ltd fsmTgt) {
        UponFsmTgtOwnerChgd(fsmTgt);
    }

    #endregion

    protected sealed override void HandleAIMgrLosingOwnership() {
        base.HandleAIMgrLosingOwnership();
        ResetBasedOnCurrentDetection(Owner);
    }

    #region StateMachine Support Members

    /// <summary>
    /// The reported cause of a failure to complete execution of an Order.
    /// </summary>
    protected UnitItemOrderFailureCause _orderFailureCause;

    protected sealed override void PreconfigureCurrentState() {
        base.PreconfigureCurrentState();
        UponPreconfigureState();
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

    protected void UponNewOrderReceived() { RelayToCurrentState(); }

    /// <summary>
    /// Called from the StateMachine just after a state
    /// change and just before state_EnterState() is called. When EnterState
    /// is a coroutine method (returns IEnumerator), the relayed version
    /// of this method provides an opportunity to configure the state
    /// before any other event relay methods can be called during the state.
    /// </summary>
    private void UponPreconfigureState() { RelayToCurrentState(); }

    private void UponRelationsChanged(Player chgdRelationsPlayer) { RelayToCurrentState(chgdRelationsPlayer); }

    /// <summary>
    /// Called when the current target being used by the State Machine dies.
    /// </summary>
    /// <param name="deadFsmTgt">The dead target.</param>
    private void UponFsmTgtDeath(IMortalItem_Ltd deadFsmTgt) { RelayToCurrentState(deadFsmTgt); }

    private void UponFsmTgtInfoAccessChgd(IItem_Ltd fsmTgt) { RelayToCurrentState(fsmTgt); }

    private void UponFsmTgtOwnerChgd(IItem_Ltd fsmTgt) { RelayToCurrentState(fsmTgt); }

    private bool UponWeaponReadyToFire(IList<WeaponFiringSolution> firingSolutions) { return RelayToCurrentState(firingSolutions); }

    private void UponDamageIncurred() { RelayToCurrentState(); }

    #endregion

    #region Combat Support

    protected override void AssessCripplingDamageToEquipment(float damageSeverity) {
        base.AssessCripplingDamageToEquipment(damageSeverity);
        var equipDamageChance = damageSeverity;

        var undamagedWeapons = Data.Weapons.Where(w => !w.IsDamaged);
        undamagedWeapons.ForAll(w => {
            w.IsDamaged = RandomExtended.Chance(equipDamageChance);
            //D.Log(ShowDebugLog && w.IsDamaged, "{0}'s weapon {1} has been damaged.", DebugName, w.Name);
        });

        var undamagedSensors = Data.Sensors.Where(s => !s.IsDamaged);
        undamagedSensors.ForAll(s => {
            s.IsDamaged = RandomExtended.Chance(equipDamageChance);
            //D.Log(ShowDebugLog && s.IsDamaged, "{0}'s sensor {1} has been damaged.", DebugName, s.Name);
        });

        var undamagedActiveCMs = Data.ActiveCountermeasures.Where(cm => !cm.IsDamaged);
        undamagedActiveCMs.ForAll(cm => {
            cm.IsDamaged = RandomExtended.Chance(equipDamageChance);
            //D.Log(ShowDebugLog && cm.IsDamaged, "{0}'s ActiveCM {1} has been damaged.", DebugName, cm.Name);
        });

        var undamagedGenerators = Data.ShieldGenerators.Where(gen => !gen.IsDamaged);
        undamagedGenerators.ForAll(gen => {
            gen.IsDamaged = RandomExtended.Chance(equipDamageChance);
            //D.Log(ShowDebugLog && gen.IsDamaged, "{0}'s shield generator {1} has been damaged.", DebugName, gen.Name);
        });
    }

    #endregion

    #endregion

    protected void KillRepairJob() {
        if (_repairJob != null) {
            _repairJob.Kill();
            _repairJob = null;
        }
    }

    #region Show Icon

    private void InitializeIcon() {
        DebugControls debugControls = DebugControls.Instance;
        debugControls.showElementIcons += ShowElementIconsChangedEventHandler;
        if (debugControls.ShowElementIcons) {
            EnableIcon(true);
        }
    }

    private void EnableIcon(bool toEnable) {
        if (toEnable) {
            if (DisplayMgr.Icon == null) {
                DisplayMgr.IconInfo = MakeIconInfo();
                SubscribeToIconEvents(DisplayMgr.Icon);
            }
        }
        else {
            if (DisplayMgr.Icon != null) {
                UnsubscribeToIconEvents(DisplayMgr.Icon);
                DisplayMgr.IconInfo = default(IconInfo);
            }
        }
    }

    private void AssessIcon() {
        if (DisplayMgr != null) {
            if (DisplayMgr.Icon != null) {
                var iconInfo = RefreshIconInfo();
                if (DisplayMgr.IconInfo != iconInfo) {    // avoid property not changed warning
                    UnsubscribeToIconEvents(DisplayMgr.Icon);
                    //D.Log(ShowDebugLog, "{0} changing IconInfo from {1} to {2}.", DebugName, DisplayMgr.IconInfo, iconInfo);
                    DisplayMgr.IconInfo = iconInfo;
                    SubscribeToIconEvents(DisplayMgr.Icon);
                }
            }
            else {
                D.Assert(!DebugControls.Instance.ShowElementIcons);
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

    private IconInfo RefreshIconInfo() {
        return MakeIconInfo();
    }

    protected abstract IconInfo MakeIconInfo();

    private void ShowElementIconsChangedEventHandler(object sender, EventArgs e) {
        EnableIcon(DebugControls.Instance.ShowElementIcons);
    }

    /// <summary>
    /// Cleans up any icon subscriptions.
    /// <remarks>The icon itself will be cleaned up when DisplayMgr.Dispose() is called.</remarks>
    /// </summary>
    private void CleanupIconSubscriptions() {
        var debugControls = DebugControls.Instance;
        if (debugControls != null) {
            debugControls.showElementIcons -= ShowElementIconsChangedEventHandler;
        }
        if (DisplayMgr != null) {
            var icon = DisplayMgr.Icon;
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
    }

    #endregion

    #region Debug

    protected abstract void __ValidateRadius(float radius);

    private IDictionary<FsmTgtEventSubscriptionMode, bool> __subscriptionStatusLookup =
        new Dictionary<FsmTgtEventSubscriptionMode, bool>(FsmTgtEventSubscriptionModeEqualityComparer.Default) {
        {FsmTgtEventSubscriptionMode.TargetDeath, false },
        {FsmTgtEventSubscriptionMode.InfoAccessChg, false },
        {FsmTgtEventSubscriptionMode.OwnerChg, false }
    };

    /// <summary>
    /// Attempts subscribing or unsubscribing to <c>fsmTgt</c> in the mode provided.
    /// Returns <c>true</c> if the indicated subscribe action was taken, <c>false</c> if not.
    /// <remarks>Issues a warning if attempting to create a duplicate subscription.</remarks>
    /// </summary>
    /// <param name="subscriptionMode">The subscription mode.</param>
    /// <param name="fsmTgt">The target used by the State Machine.</param>
    /// <param name="toSubscribe">if set to <c>true</c> subscribe, otherwise unsubscribe.</param>
    /// <returns></returns>
    /// <exception cref="NotImplementedException"></exception>
    protected bool __AttemptFsmTgtSubscriptionChg(FsmTgtEventSubscriptionMode subscriptionMode, IShipNavigable fsmTgt, bool toSubscribe) {
        Utility.ValidateNotNull(fsmTgt);
        bool isSubscribeActionTaken = false;
        bool isDuplicateSubscriptionAttempted = false;
        IItem_Ltd itemFsmTgt = null;
        bool isSubscribed = __subscriptionStatusLookup[subscriptionMode];
        switch (subscriptionMode) {
            case FsmTgtEventSubscriptionMode.TargetDeath:
                var mortalFsmTgt = fsmTgt as IMortalItem_Ltd;
                if (mortalFsmTgt != null) {
                    if (!toSubscribe) {
                        mortalFsmTgt.deathOneShot -= FsmTgtDeathEventHandler;
                        isSubscribeActionTaken = true;
                    }
                    else if (!isSubscribed) {
                        mortalFsmTgt.deathOneShot += FsmTgtDeathEventHandler;
                        isSubscribeActionTaken = true;
                    }
                    else {
                        isDuplicateSubscriptionAttempted = true;
                    }
                }
                break;
            case FsmTgtEventSubscriptionMode.InfoAccessChg:
                itemFsmTgt = fsmTgt as IItem_Ltd;
                if (itemFsmTgt != null) {    // fsmTgt can be a StationaryLocation
                    if (!toSubscribe) {
                        itemFsmTgt.infoAccessChgd -= FsmTgtInfoAccessChgdEventHandler;
                        isSubscribeActionTaken = true;
                    }
                    else if (!isSubscribed) {
                        itemFsmTgt.infoAccessChgd += FsmTgtInfoAccessChgdEventHandler;
                        isSubscribeActionTaken = true;
                    }
                    else {
                        isDuplicateSubscriptionAttempted = true;
                    }
                }
                break;
            case FsmTgtEventSubscriptionMode.OwnerChg:
                itemFsmTgt = fsmTgt as IItem_Ltd;
                if (itemFsmTgt != null) {    // fsmTgt can be a StationaryLocation
                    if (!toSubscribe) {
                        itemFsmTgt.ownerChanged -= FsmTgtOwnerChgdEventHandler;
                        isSubscribeActionTaken = true;
                    }
                    else if (!isSubscribed) {
                        itemFsmTgt.ownerChanged += FsmTgtOwnerChgdEventHandler;
                        isSubscribeActionTaken = true;
                    }
                    else {
                        isDuplicateSubscriptionAttempted = true;
                    }
                }
                break;
            case FsmTgtEventSubscriptionMode.None:
            default:
                throw new NotImplementedException(ErrorMessages.UnanticipatedSwitchValue.Inject(subscriptionMode));
        }
        if (isDuplicateSubscriptionAttempted) {
            D.Warn("{0}: Attempting to subscribe to {1}'s {2} when already subscribed.", DebugName, fsmTgt.DebugName, subscriptionMode.GetValueName());
        }
        if (isSubscribeActionTaken) {
            __subscriptionStatusLookup[subscriptionMode] = toSubscribe;
        }
        return isSubscribeActionTaken;
    }

    protected void __ReportCollision(Collision collision) {
        if (ShowDebugLog) {

            Profiler.BeginSample("Editor-only GC allocation (GetComponent returns null)");
            var ordnance = collision.transform.GetComponent<AOrdnance>();
            Profiler.EndSample();

            if (ordnance == null) {
                // its not ordnance we collided with so report it
                SphereCollider sphereCollider = collision.collider as SphereCollider;
                BoxCollider boxCollider = collision.collider as BoxCollider;
                string colliderSizeMsg = (sphereCollider != null) ? "radius = " + sphereCollider.radius : ((boxCollider != null) ? "size = " + boxCollider.size.ToPreciseString() : "size unknown");
                D.Log("While {0}, {1} collided with {2} whose {3}. Velocity after impact = {4}.",
                    CurrentState.ToString(), DebugName, collision.collider.name, colliderSizeMsg, Rigidbody.velocity.ToPreciseString());
                //D.Log("{0}: Detail on collision - Distance between collider centers = {1:0.##}", DebugName, Vector3.Distance(Position, collision.collider.transform.position));
                // AngularVelocity no longer reported as element's rigidbody.freezeRotation = true
            }
        }
    }

    #endregion

    #region IElementAttackable Members

    public override void TakeHit(DamageStrength damagePotential) {
        LogEvent();
        if (_debugSettings.AllPlayersInvulnerable) {
            return;
        }
        D.Assert(IsOperational);
        DamageStrength damage = damagePotential - Data.DamageMitigation;
        if (damage.Total == Constants.ZeroF) {
            //D.Log("{0} has been hit but incurred no damage.", DebugName);
            return;
        }
        D.Log(ShowDebugLog, "{0} has been hit. Taking {1:0.#} damage.", DebugName, damage.Total);

        bool isCmdHit = false;
        float damageSeverity;
        bool isElementAlive = ApplyDamage(damage, out damageSeverity);
        if (!isElementAlive) {
            IsOperational = false;  // should immediately propagate thru to Cmd's alive status
        }
        if (IsHQ && Command.IsOperational) {
            isCmdHit = Command.__CheckForDamage(isElementAlive, damage, damageSeverity);
        }

        if (isElementAlive) {
            var hitAnimation = isCmdHit ? EffectSequenceID.CmdHit : EffectSequenceID.Hit;
            StartEffectSequence(hitAnimation);
            UponDamageIncurred();
        }
    }

    #endregion

    #region IShipAttackable Members

    public abstract AutoPilotDestinationProxy GetApAttackTgtProxy(float minDesiredDistanceToTgtSurface, float maxDesiredDistanceToTgtSurface);

    #endregion

    #region ICameraFollowable Members

    public float FollowDistanceDampener { get { return CameraStat.FollowDistanceDampener; } }

    public float FollowRotationDampener { get { return CameraStat.FollowRotationDampener; } }

    #endregion

    #region ISensorDetectable Members

    public void HandleDetectionBy(Player detectingPlayer, IUnitCmd_Ltd cmdItem, RangeCategory sensorRangeCat) {
        _detectionHandler.HandleDetectionBy(detectingPlayer, cmdItem, sensorRangeCat);
    }

    public void HandleDetectionLostBy(Player detectingPlayer, IUnitCmd_Ltd cmdItem, RangeCategory sensorRangeCat) {
        _detectionHandler.HandleDetectionLostBy(detectingPlayer, cmdItem, sensorRangeCat);
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

}

