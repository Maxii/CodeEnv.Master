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
/// Abstract class for AMortalItem's that are Unit Elements.
/// </summary>
public abstract class AUnitElementItem : AMortalItemStateMachine, IUnitElementItem, ICameraFollowable, IShipAttackable, ISensorDetectable {

    private const string __HQNameAddendum = "[HQ]";

    public event EventHandler isHQChanged;

    /// <summary>
    /// Indicates whether this element is available for a new assignment.
    /// <remarks>Typically, an element that is available is Idling.</remarks>
    /// </summary>
    public abstract bool IsAvailable { get; }

    /// <summary>
    /// Indicates whether this element is capable of attacking an enemy target.
    /// </summary>
    public abstract bool IsAttackCapable { get; }

    public new AUnitElementItemData Data {
        get { return base.Data as AUnitElementItemData; }
        set { base.Data = value; }
    }

    private float _radius;
    public override float Radius {
        get {
            if (_radius == Constants.ZeroF) {
                _radius = Data.HullDimensions.magnitude / 2F;
                //D.Log(ShowDebugLog, "{0} Radius set to {1:0.000}.", FullName, _radius);
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

    private IUnitCmdItem _command;
    public IUnitCmdItem Command {
        get { return _command; }
        set { SetProperty<IUnitCmdItem>(ref _command, value, "Command"); }
    }

    protected new AElementDisplayManager DisplayMgr { get { return base.DisplayMgr as AElementDisplayManager; } }

    protected IList<IWeaponRangeMonitor> _weaponRangeMonitors = new List<IWeaponRangeMonitor>();
    protected IList<IActiveCountermeasureRangeMonitor> _countermeasureRangeMonitors = new List<IActiveCountermeasureRangeMonitor>();
    protected IList<IShield> _shields = new List<IShield>();
    protected Rigidbody _rigidbody;
    protected PlayerKnowledge _ownerKnowledge;

    private DetectionHandler _detectionHandler;
    private BoxCollider _primaryCollider;

    #region Initialization

    protected override void Subscribe() {
        base.Subscribe();
        _subscriptions.Add(PlayerPrefsManager.Instance.SubscribeToPropertyChanged<PlayerPrefsManager, bool>(ppm => ppm.IsElementIconsEnabled, IsElementIconsEnabledPropChangedHandler));
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
        _primaryCollider.isTrigger = false;
        _primaryCollider.enabled = false;
        _primaryCollider.size = Data.HullDimensions;
    }

    protected virtual void InitializePrimaryRigidbody() {
        _rigidbody = UnityUtility.ValidateComponentPresence<Rigidbody>(gameObject);
        _rigidbody.useGravity = false;
        // Note: if physics is allowed to induce rotation, then ChangeHeading behaves unpredictably when HQ, 
        // presumably because Cmd is attached to HQ with a fixed joint?
        _rigidbody.freezeRotation = true;   // covers both Ship and Facility for when I put Facility under physics control
        _rigidbody.isKinematic = true;      // avoid physics affects until CommenceOperations, if at all
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
        if (PlayerPrefsManager.Instance.IsElementIconsEnabled) {
            DisplayMgr.IconInfo = MakeIconInfo();
            SubscribeToIconEvents(DisplayMgr.Icon);
        }
        DisplayMgr.MeshColor = Owner.Color;
    }

    private void SubscribeToIconEvents(IResponsiveTrackingSprite icon) {
        var iconEventListener = icon.EventListener;
        iconEventListener.onHover += HoverEventHandler;
        iconEventListener.onClick += ClickEventHandler;
        iconEventListener.onDoubleClick += DoubleClickEventHandler;
        iconEventListener.onPress += PressEventHandler;
    }

    protected override void FinalInitialize() {
        base.FinalInitialize();
        _ownerKnowledge = _gameMgr.PlayersKnowledge.GetKnowledge(Owner);
        _rigidbody.isKinematic = false;
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
    protected internal virtual void AttachAsChildOf(Transform unitContainer) {
        transform.parent = unitContainer;
    }

    protected override void HandleDeath() {
        base.HandleDeath();
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
        (Command as ICameraFocusable).IsFocus = true;
    }

    /********************************************************************************************************************************************
     * Equipment (Weapons, Sensors and Countermeasures) no longer added or removed while the item is operating. 
     * Changes in an item's equipment can only occur during a refit where a new item is created to replace the item being refitted.
     ********************************************************************************************************************************************/

    #region Weapons

    /*******************************************************************************************************************************************
     * This implementation attempts to calculate a firing solution against every target thought to be in range and leaves it up to the 
     * element to determine which one to use, if any. If the element declines to fire (would be ineffective, not proper state (ie. refitting), target 
     * died or diplo relations changed while weapon being aimed, etc.), then the weapon continues to look for firing solutions to put forward.
     * This approach works best where many weapons or countermeasures may not bear even when the target is in range.
     ********************************************************************************************************************************************/

    /// <summary>
    /// Attaches the weapon and its monitor to this item.
    /// </summary>
    /// <param name="weapon">The weapon.</param>
    private void Attach(AWeapon weapon) {
        D.Assert(weapon.RangeMonitor != null);
        var monitor = weapon.RangeMonitor;
        if (!_weaponRangeMonitors.Contains(monitor)) {
            // only need to record and setup range monitors once. The same monitor can have more than 1 weapon
            _weaponRangeMonitors.Add(monitor);
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
    /// If the conditions for firing the weapon at the target are satisfied (within range, can be beared upon,
    /// no interfering obstacles, etc.), the weapon will be fired.
    /// </summary>
    /// <param name="firingSolution">The firing solution.</param>
    protected void InitiateFiringSequence(WeaponFiringSolution firingSolution) {
        StartEffect(EffectID.Attacking);
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

    private void LaunchOrdnance(AWeapon weapon, IElementAttackable target) {
        var ordnance = GeneralFactory.Instance.MakeOrdnanceInstance(weapon, gameObject);
        var projectileOrdnance = ordnance as AProjectileOrdnance;
        if (projectileOrdnance != null) {
            projectileOrdnance.Launch(target, weapon, Topography);
        }
        else {
            var beamOrdnance = ordnance as Beam;
            D.Assert(beamOrdnance != null);
            beamOrdnance.Launch(target, weapon);
        }
        //D.Log(ShowDebugLog, "{0} has fired {1} against {2} on {3}.", FullName, ordnance.Name, target.FullName, GameTime.Instance.CurrentDate);
        /***********************************************************************************************************************************************
         * Note on Target Death: When a target dies, the fired ordnance detects it and takes appropriate action. All ordnance types will no longer
         * apply damage to a dead target, but the impact effect will still show if applicable. This is so the viewer still sees impacts even while the
         * death cinematic plays out. Once the target is destroyed, its collider becomes disabled, allowing ordnance to pass through and potentially
         * collide with other items until it runs out of range and self terminates. This behaviour holds for both projectile and beam ordnance. In the
         * case of missile ordnance, once its target is dead it self destructs as waiting until the target is destroyed results in 'transform destroyed' errors.
         **************************************************************************************************************************************************/
    }
    //private void LaunchOrdnance(AWeapon weapon, IElementAttackable target) {
    //    var ordnance = GeneralFactory.Instance.MakeOrdnanceInstance(weapon, gameObject);
    //    var projectileOrdnance = ordnance as AProjectileOrdnance;
    //    if (projectileOrdnance != null) {
    //        projectileOrdnance.Launch(target, weapon, Topography, IsVisualDetailDiscernibleToUser);
    //    }
    //    else {
    //        var beamOrdnance = ordnance as Beam;
    //        D.Assert(beamOrdnance != null);
    //        beamOrdnance.Launch(target, weapon, IsVisualDetailDiscernibleToUser);
    //    }
    //    //D.Log(ShowDebugLog, "{0} has fired {1} against {2} on {3}.", FullName, ordnance.Name, target.FullName, GameTime.Instance.CurrentDate);
    //    /***********************************************************************************************************************************************
    //     * Note on Target Death: When a target dies, the fired ordnance detects it and takes appropriate action. All ordnance types will no longer
    //     * apply damage to a dead target, but the impact effect will still show if applicable. This is so the viewer still sees impacts even while the
    //     * death cinematic plays out. Once the target is destroyed, its collider becomes disabled, allowing ordnance to pass through and potentially
    //     * collide with other items until it runs out of range and self terminates. This behaviour holds for both projectile and beam ordnance. In the
    //     * case of missile ordnance, once its target is dead it self destructs as waiting until the target is destroyed results in 'transform destroyed' errors.
    //     **************************************************************************************************************************************************/
    //}

    private void HandleWeaponReadyToFire(IList<WeaponFiringSolution> firingSolutions) {
        bool isMsgReceived = UponWeaponReadyToFire(firingSolutions);
        if (!isMsgReceived) {
            // element in state that doesn't deal with firing weapons
            var weapon = firingSolutions.First().Weapon;    // all the same weapon
            weapon.HandleElementDeclinedToFire();
        }
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
    //        D.Log("{0} did not fire weapon {1}.", FullName, weapon.Name);
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
    //            //D.Log("{0} added Weapon {1} to ReadyWeaponsInventory.", FullName, weapon.Name);
    //        }
    //        else {
    //            //D.Log("{0} properly avoided adding duplicate Weapon {1} to ReadyWeaponsInventory.", FullName, weapon.Name);
    //            // this occurs when a weapon attempts to fire but doesn't (usually due to LOS interference) and therefore remains
    //            // IsReadyToFire. If it had fired, it wouldn't be ready and therefore would have been removed below
    //        }
    //    }
    //    else {
    //        if (_readyWeaponsInventory.Contains(weapon)) {
    //            _readyWeaponsInventory.Remove(weapon);
    //            //D.Log("{0} removed Weapon {1} from ReadyWeaponsInventory.", FullName, weapon.Name);
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
        D.Assert(activeCM.RangeMonitor != null);
        var monitor = activeCM.RangeMonitor;
        if (!_countermeasureRangeMonitors.Contains(monitor)) {
            // only need to record and setup range monitors once. The same monitor can have more than 1 weapon
            _countermeasureRangeMonitors.Add(monitor);
        }
    }

    #endregion

    #region Shield Generators

    /// <summary>
    /// Attaches this shield generator and its shield to this item.
    /// </summary>
    /// <param name="generator">The shield generator.</param>
    private void Attach(ShieldGenerator generator) {
        D.Assert(generator.Shield != null);
        var shield = generator.Shield;
        if (!_shields.Contains(shield)) {
            // only need to record and setup range monitors once. The same monitor can have more than 1 weapon
            _shields.Add(shield);
        }
    }

    #endregion

    #region Sensors

    private void Attach(Sensor sensor) {
        D.Assert(Command == null);  // OPTIMIZE Sensors are only attached to elements when an element is being created so there is no Command yet
    }

    #endregion

    private void AssessIcon() {
        D.Assert(DisplayMgr != null);

        if (PlayerPrefsManager.Instance.IsElementIconsEnabled) {
            var iconInfo = RefreshIconInfo();
            if (DisplayMgr.IconInfo != iconInfo) {    // avoid property not changed warning
                UnsubscribeToIconEvents(DisplayMgr.Icon);
                //D.Log(ShowDebugLog, "{0} changing IconInfo from {1} to {2}.", FullName, DisplayMgr.IconInfo, iconInfo);
                DisplayMgr.IconInfo = iconInfo;
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

    private IconInfo RefreshIconInfo() {
        return MakeIconInfo();
    }

    protected abstract IconInfo MakeIconInfo();

    public override void AssessHighlighting() {
        if (IsDiscernibleToUser) {
            if (IsFocus) {
                if (IsSelected) {
                    ShowHighlights(HighlightID.Focused, HighlightID.Selected);
                    return;
                }
                if (Command.IsSelected) {
                    ShowHighlights(HighlightID.Focused, HighlightID.UnitElement);
                    return;
                }
                ShowHighlights(HighlightID.Focused);
                return;
            }
            if (IsSelected) {
                ShowHighlights(HighlightID.Selected);
                return;
            }
            if (Command.IsSelected) {
                ShowHighlights(HighlightID.UnitElement);
                return;
            }
        }
        ShowHighlights(HighlightID.None);
    }

    private void HandleLosWeaponAimed(LosWeaponFiringSolution firingSolution) {
        var target = firingSolution.EnemyTarget;
        var losWeapon = firingSolution.Weapon;
        D.Assert(losWeapon.IsOperational);  // weapon should not have completed aiming if it lost operation
        if (target.IsOperational && target.Owner.IsEnemyOf(Owner) && losWeapon.ConfirmInRangeForLaunch(target)) {
            LaunchOrdnance(losWeapon, target);
        }
        else {
            // target moved out of range, died or changed diplo during aiming process
            losWeapon.HandleElementDeclinedToFire();
        }
        losWeapon.weaponAimed -= LosWeaponAimedEventHandler;
    }

    #region Event and Property Change Handlers

    protected virtual void IsHQPropChangedHandler() {
        Name = IsHQ ? Name + __HQNameAddendum : Name.Remove(__HQNameAddendum);
        OnIsHQChanged();
    }

    private void OnIsHQChanged() {
        if (isHQChanged != null) {
            isHQChanged(this, new EventArgs());
        }
    }

    protected override void OwnerPropChangedHandler() {
        base.OwnerPropChangedHandler();
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

    protected override void IsVisualDetailDiscernibleToUserPropChangedHandler() {
        base.IsVisualDetailDiscernibleToUserPropChangedHandler();
        Data.Weapons.ForAll(w => w.IsWeaponDiscernibleToUser = IsVisualDetailDiscernibleToUser);
    }
    //protected override void IsVisualDetailDiscernibleToUserPropChangedHandler() {
    //    base.IsVisualDetailDiscernibleToUserPropChangedHandler();
    //    Data.Weapons.ForAll(w => w.ToShowEffects = IsVisualDetailDiscernibleToUser);
    //}

    private void IsElementIconsEnabledPropChangedHandler() {
        if (DisplayMgr != null) {
            AssessIcon();
        }
    }

    #endregion

    protected override void HandleLeftDoubleClick() {
        base.HandleLeftDoubleClick();
        Command.IsSelected = true;
    }

    protected abstract void __ValidateRadius(float radius);

    # region StateMachine Support Methods

    protected void Dead_ExitState() {
        LogEvent();
        D.Error("{0}.Dead_ExitState should not occur.", Data.Name);
    }

    protected void UponEffectFinished(EffectID effectID) { RelayToCurrentState(effectID); }

    protected void UponApTargetDeath(IMortalItem deadTarget) { RelayToCurrentState(deadTarget); }

    private bool UponWeaponReadyToFire(IList<WeaponFiringSolution> firingSolutions) {
        return RelayToCurrentState(firingSolutions);
    }

    protected void UponNewOrderReceived() { RelayToCurrentState(); }

    #endregion

    #region Combat Support Methods

    protected override void AssessCripplingDamageToEquipment(float damageSeverity) {
        base.AssessCripplingDamageToEquipment(damageSeverity);
        var equipDamageChance = damageSeverity;

        var undamagedWeapons = Data.Weapons.Where(w => !w.IsDamaged);
        undamagedWeapons.ForAll(w => {
            w.IsDamaged = RandomExtended.Chance(equipDamageChance);
            //D.Log(ShowDebugLog && w.IsDamaged, "{0}'s weapon {1} has been damaged.", FullName, w.Name);
        });

        var undamagedSensors = Data.Sensors.Where(s => !s.IsDamaged);
        undamagedSensors.ForAll(s => {
            s.IsDamaged = RandomExtended.Chance(equipDamageChance);
            //D.Log(ShowDebugLog && s.IsDamaged, "{0}'s sensor {1} has been damaged.", FullName, s.Name);
        });

        var undamagedActiveCMs = Data.ActiveCountermeasures.Where(cm => !cm.IsDamaged);
        undamagedActiveCMs.ForAll(cm => {
            cm.IsDamaged = RandomExtended.Chance(equipDamageChance);
            //D.Log(ShowDebugLog && cm.IsDamaged, "{0}'s ActiveCM {1} has been damaged.", FullName, cm.Name);
        });

        var undamagedGenerators = Data.ShieldGenerators.Where(gen => !gen.IsDamaged);
        undamagedGenerators.ForAll(gen => {
            gen.IsDamaged = RandomExtended.Chance(equipDamageChance);
            //D.Log(ShowDebugLog && gen.IsDamaged, "{0}'s shield generator {1} has been damaged.", FullName, gen.Name);
        });
    }

    protected abstract void AssessNeedForRepair();

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
        if (DisplayMgr != null) {
            var icon = DisplayMgr.Icon;
            if (icon != null) {
                UnsubscribeToIconEvents(icon);
            }
        }
    }

    private void UnsubscribeToIconEvents(IResponsiveTrackingSprite icon) {
        var iconEventListener = icon.EventListener;
        iconEventListener.onHover -= HoverEventHandler;
        iconEventListener.onClick -= ClickEventHandler;
        iconEventListener.onDoubleClick -= DoubleClickEventHandler;
        iconEventListener.onPress -= PressEventHandler;
    }

    #endregion

    #region Debug Collision Reporting

    protected void __ReportCollision(Collision collision) {
        SphereCollider sphereCollider = collision.collider as SphereCollider;
        BoxCollider boxCollider = collision.collider as BoxCollider;
        string colliderSizeMsg = (sphereCollider != null) ? "radius = " + sphereCollider.radius : ((boxCollider != null) ? "size = " + boxCollider.size.ToPreciseString() : "size unknown");
        D.Log(ShowDebugLog, "While {0}, {1} collided with {2} whose {3}. Velocity after impact = {4}.",
            CurrentState.ToString(), FullName, collision.collider.name, colliderSizeMsg, _rigidbody.velocity.ToPreciseString());
        //D.Log(ShowDebugLog, "{0}: Detail on collision - Distance between collider centers = {1:0.##}", FullName, Vector3.Distance(Position, collision.collider.transform.position));
        // AngularVelocity no longer reported as element's rigidbody.freezeRotation = true
    }

    #endregion

    #region IElementAttackable Members

    public bool IsAttackingAllowedBy(Player player) {
        return Owner.IsEnemyOf(player);
    }

    /// <summary>
    /// Called by the ordnanceFired to notify its target of the launch
    /// of the ordnance. This workaround is necessary in cases where the ordnance is
    /// launched inside the target's ActiveCountermeasureRangeMonitor
    /// collider sphere since GameObjects instantiated inside a collider are
    /// not detected by OnTriggerEnter(). The target will only take action on
    /// this FYI if it determines that the ordnance will not be detected by one or
    /// more of its monitors.
    /// Note: Obsolete as all interceptable ordnance has a rigidbody which is detected by the monitor when the 
    /// ordnance moves, even if it first appears inside the monitor's collider.
    /// </summary>
    /// <param name="ordnanceFired">The ordnance fired.</param>
    [Obsolete]
    public void HandleFiredUponBy(IInterceptableOrdnance ordnanceFired) {
        float ordnanceDistanceFromElement = Vector3.Distance(ordnanceFired.Position, Position);
        _countermeasureRangeMonitors.ForAll(rm => {
            if (rm.RangeDistance > ordnanceDistanceFromElement) {
                // ordance was fired inside the collider
                rm.AddOrdnanceLaunchedFromInsideMonitor(ordnanceFired);
            }
        });
    }

    public override void TakeHit(DamageStrength damagePotential) {
        LogEvent();
        if (_debugSettings.AllPlayersInvulnerable) {
            return;
        }
        D.Assert(IsOperational);
        DamageStrength damage = damagePotential - Data.DamageMitigation;
        if (damage.Total == Constants.ZeroF) {
            //D.Log("{0} has been hit but incurred no damage.", FullName);
            return;
        }
        D.Log(ShowDebugLog, "{0} has been hit. Taking {1:0.#} damage.", FullName, damage.Total);

        bool isCmdHit = false;
        float damageSeverity;
        bool isElementAlive = ApplyDamage(damage, out damageSeverity);
        if (!isElementAlive) {
            IsOperational = false;  // InitiateDeath();    // should immediately propogate thru to Cmd's alive status
        }
        if (IsHQ && Command.IsOperational) {
            isCmdHit = Command.__CheckForDamage(isElementAlive, damage, damageSeverity);
        }

        if (isElementAlive) {
            var hitAnimation = isCmdHit ? EffectID.CmdHit : EffectID.Hit;
            StartEffect(hitAnimation);
            AssessNeedForRepair();
        }
    }

    #endregion

    #region IShipAttackable Members

    public abstract AutoPilotDestinationProxy GetApAttackTgtProxy(float minRangeToTgtSurface, float maxRangeToTgtSurface);

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

    #region IHighlightable Members

    public override float HighlightRadius { get { return Radius * Screen.height * 1F; } }

    #endregion

}

