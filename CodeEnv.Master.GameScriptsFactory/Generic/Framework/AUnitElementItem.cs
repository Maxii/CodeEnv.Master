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
///  Abstract class for AMortalItem's that are Unit Elements.
/// </summary>
public abstract class AUnitElementItem : AMortalItemStateMachine, IUnitElementItem, ICameraFollowable, IElementAttackableTarget, ISensorDetectable {

    public event Action<IUnitElementItem> onIsHQChanged;

    [Range(1.0F, 3.0F)]
    [Tooltip("Minimum Camera View Distance Multiplier")]
    public float minViewDistanceFactor = 2.0F;

    [Range(1.5F, 5.0F)]
    [Tooltip("Optimal Camera View Distance Multiplier")]
    public float optViewDistanceFactor = 2.4F;

    public new AUnitElementItemData Data {
        get { return base.Data as AUnitElementItemData; }
        set { base.Data = value; }
    }

    public bool IsHQ { get { return Data.IsHQ; } }

    public IUnitCmdItem Command { get; set; }

    protected new AElementDisplayManager DisplayMgr { get { return base.DisplayMgr as AElementDisplayManager; } }

    protected IList<IWeaponRangeMonitor> _weaponRangeMonitors = new List<IWeaponRangeMonitor>();
    protected IList<IActiveCountermeasureRangeMonitor> _countermeasureRangeMonitors = new List<IActiveCountermeasureRangeMonitor>();
    protected IList<IShield> _shields = new List<IShield>();

    private IList<ActiveCountermeasure> _readyCountermeasuresInventory = new List<ActiveCountermeasure>();
    private DetectionHandler _detectionHandler;
    protected Collider _collider;

    #region Initialization

    protected override void InitializeLocalReferencesAndValues() {
        base.InitializeLocalReferencesAndValues();
        // Note: Radius is set in derived classes due to the difference in meshes
        _collider = UnityUtility.ValidateComponentPresence<Collider>(gameObject);
        _collider.isTrigger = false;
        _collider.enabled = false;
    }

    protected override void Subscribe() {
        base.Subscribe();
        _subscriptions.Add(PlayerPrefsManager.Instance.SubscribeToPropertyChanged<PlayerPrefsManager, bool>(ppm => ppm.IsElementIconsEnabled, OnElementIconsEnabledOptionChanged));
    }

    protected override void InitializeModelMembers() {
        _detectionHandler = new DetectionHandler(this);
    }

    protected override void SubscribeToDataValueChanges() {
        base.SubscribeToDataValueChanges();
        _subscriptions.Add(Data.SubscribeToPropertyChanged<AUnitElementItemData, bool>(data => data.IsHQ, OnIsHQChanged));
        //TODO: Weapon values don't change but weapons do so I need to know when that happens
    }

    protected sealed override ADisplayManager InitializeDisplayManager() {
        var displayMgr = MakeDisplayManager();
        if (PlayerPrefsManager.Instance.IsElementIconsEnabled) {
            displayMgr.IconInfo = MakeIconInfo();
            SubscribeToIconEvents(displayMgr.Icon);
        }
        return displayMgr;
    }

    protected abstract AIconDisplayManager MakeDisplayManager();

    private void SubscribeToIconEvents(IResponsiveTrackingSprite icon) {
        var iconEventListener = icon.EventListener;
        iconEventListener.onHover += (go, isOver) => OnHover(isOver);
        iconEventListener.onClick += (go) => OnClick();
        iconEventListener.onDoubleClick += (go) => OnDoubleClick();
        iconEventListener.onPress += (go, isDown) => OnPress(isDown);
    }

    #endregion

    #region Model Methods

    public override void CommenceOperations() {
        base.CommenceOperations();
        _collider.enabled = true;
        Data.Weapons.ForAll(w => w.IsOperational = true);
        Data.Sensors.ForAll(s => s.IsOperational = true);
        Data.ActiveCountermeasures.ForAll(cm => cm.IsOperational = true);
        Data.ShieldGenerators.ForAll(gen => gen.IsOperational = true);
    }

    /// <summary>
    /// Parents this element to the provided container that holds the entire Unit.
    /// Local position, rotation and scale auto adjust to keep element unchanged in worldspace.
    /// </summary>
    /// <param name="unitContainer">The unit container.</param>
    protected internal virtual void AttachAsChildOf(Transform unitContainer) {
        _transform.parent = unitContainer;
    }

    protected override void OnDataSet() {
        base.OnDataSet();
        Data.ActiveCountermeasures.ForAll(cm => Attach(cm));
        Data.Weapons.ForAll(w => Attach(w));
        Data.ShieldGenerators.ForAll(gen => Attach(gen));
        Data.Sensors.ForAll(s => Attach(s));
    }

    protected override void OnOwnerChanged() {
        base.OnOwnerChanged();
        AssessIcon();
        // Checking weapon targeting on an OwnerChange is handled by WeaponRangeMonitor
    }

    private void OnIsHQChanged() {
        if (onIsHQChanged != null) {
            onIsHQChanged(this);
        }
    }

    protected override void PrepareForOnDeathNotification() {
        base.PrepareForOnDeathNotification();
        // _collider.enabled = false;   // keep the collider on until destroyed or returned to the pool
        // this allows in-route ordnance to show its impact effect while the item is showing its death
        Data.Weapons.ForAll(w => {
            w.onReadyToFire -= OnWeaponReadyToFire;
            w.IsOperational = false;
        });
        Data.ActiveCountermeasures.ForAll(cm => {
            cm.onIsReadyToInterceptAThreatChanged -= OnCountermeasureReadyToInterceptAThreatChanged;
            cm.onThreatEnteringRange -= OnNewThreatInRange;
            cm.IsOperational = false;
        });
        Data.Sensors.ForAll(s => s.IsOperational = false);
        Data.ShieldGenerators.ForAll(gen => gen.IsOperational = false);
    }

    protected override void CleanupAfterOnDeathNotification() {
        base.CleanupAfterOnDeathNotification();
        Command.OnSubordinateElementDeath(this);
    }

    #region Weapons

    /********************************************************************************************************************************************
           * Equipment (Weapons, Sensors and Countermeasures) no longer added or removed while the item is operating. 
           * Changes in an item's equipment can only occur during a refit where a new item is created to replace the item being refitted.
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
        weapon.onReadyToFire += OnWeaponReadyToFire;
        // IsOperational = true is set when item operations commences
    }

    protected FiringSolution PickBestFiringSolution(IList<FiringSolution> firingSolutions, IElementAttackableTarget tgtHint = null) {
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
    protected void InitiateFiringSequence(FiringSolution firingSolution) {
        StartEffect(EffectID.Attacking);
        LosFiringSolution losFiringSolution = firingSolution as LosFiringSolution;
        if (losFiringSolution != null) {
            var losWeapon = losFiringSolution.Weapon;
            losWeapon.onWeaponAimedAtTarget += OnLosWeaponAimedAtTarget;
            losWeapon.AimAtTarget(losFiringSolution);
        }
        else {
            // no aiming reqd, just launch the ordnance
            var weapon = firingSolution.Weapon;
            var target = firingSolution.EnemyTarget;
            LaunchOrdnance(weapon, target);
        }
    }

    /// <summary>
    /// Called when a LOS Weapon has completed its aiming process at a target.
    /// </summary>
    /// <param name="firingSolution">The firing solution.</param>
    private void OnLosWeaponAimedAtTarget(LosFiringSolution firingSolution) {
        var target = firingSolution.EnemyTarget;
        var losWeapon = firingSolution.Weapon;
        if (target.IsOperational) {
            LaunchOrdnance(losWeapon, target);
        }
        else {  // target died during aiming process
            losWeapon.OnElementDeclinedToFire();
        }
        losWeapon.onWeaponAimedAtTarget -= OnLosWeaponAimedAtTarget;
    }

    private void LaunchOrdnance(AWeapon weapon, IElementAttackableTarget target) {
        var ordnance = GeneralFactory.Instance.MakeOrdnanceInstance(weapon, gameObject);
        ordnance.Initiate(target, weapon, IsVisualDetailDiscernibleToUser);
        D.Log("{0} has fired {1} against {2} on {3}.", FullName, ordnance.Name, target.FullName, GameTime.Instance.CurrentDate);
        /***********************************************************************************************************************************************
               * Note on Target Death: When a target dies, the fired ordnance detects it and takes appropriate action. All ordnance types will no longer
               * apply damage to a dead target, but the impact effect will still show if applicable. This is so the viewer still sees impacts even while the
               * death cinematic plays out. Once the target is destroyed, its collider becomes disabled, allowing ordnance to pass through and potentially
               * collide with other items until it runs out of range and self terminates. This behaviour holds for both projectile and beam ordnance. In the
               * case of missile ordnance, once its target is dead it self destructs as waiting until the target is destroyed results in 'transform destroyed' errors.
               **************************************************************************************************************************************************/
    }

    private void OnWeaponReadyToFire(IList<FiringSolution> firingSolutions) {
        RelayToCurrentState(firingSolutions);
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
        activeCM.onIsReadyToInterceptAThreatChanged += OnCountermeasureReadyToInterceptAThreatChanged;
        activeCM.onThreatEnteringRange += OnNewThreatInRange;
        // IsOperational = true is set when item operations commences
    }

    /// <summary>
    /// Attempts to find an incoming ordnance threat in range and intercept it.
    /// </summary>
    /// <param name="activeCM">The active countermeasure.</param>
    protected void FindIncomingThreatAndIntercept(ActiveCountermeasure activeCM) {
        D.Assert(activeCM.IsReadyToInterceptAThreat);
        IInterceptableOrdnance ordnanceThreat;
        if (activeCM.TryPickMostDangerousThreat(out ordnanceThreat)) {
            bool isThreatHit = activeCM.Fire(ordnanceThreat);
            //D.Log(!isThreatHit, "{0}'s {1} missed intercept on {2}.", FullName, activeCM.Name, ordnanceThreat.Name);
        }
        else {
            //D.Log("{0} did not find a threat to use countermeasure {1} against.", FullName, activeCM.Name);
        }
    }

    /// <summary>
    /// Called when there is a change in the readiness to intercept a threat status of the provided countermeasure. 
    /// Readiness to intercept does not mean there is a threat in range to intercept.
    /// </summary>
    /// <param name="activeCM">The active countermeasure.</param>
    private void OnCountermeasureReadyToInterceptAThreatChanged(ActiveCountermeasure activeCM) {
        //D.Log("{0}.OnCountermeasureReadyToInterceptAThreatChange() called by {1}. Ready = {2}, ThreatInRange = {3}.", FullName, activeCM.Name, activeCM.IsReadyToInterceptAThreat, activeCM.IsThreatInRange);
        if (activeCM.IsReadyToInterceptAThreat && activeCM.IsThreatInRange) {
            OnCountermeasureReadyAndThreatInRange(activeCM);
        }
        UpdateReadyCountermeasuresInventory(activeCM);
    }

    /// <summary>
    /// Called when a new, qualified incoming ordnance threat has come within range 
    /// of the provided countermeasure. This event is independent of whether the
    /// countermeasure is ready to intercept. However, it does mean the countermeasure is operational.
    /// </summary>
    /// <param name="activeCM">The active countermeasure.</param>
    private void OnNewThreatInRange(ActiveCountermeasure activeCM) {
        //D.Log("{0}.OnNewThreatInRange() called by {1} event.", FullName, activeCM.Name);
        if (_readyCountermeasuresInventory.Contains(activeCM)) {
            OnCountermeasureReadyAndThreatInRange(activeCM);
            UpdateReadyCountermeasuresInventory(activeCM);
        }
    }

    /// <summary>
    /// Called when the provided countermeasure is ready to intercept and there
    /// is a threat in range.
    /// </summary>
    /// <param name="activeCM">The countermeasure.</param>
    private void OnCountermeasureReadyAndThreatInRange(ActiveCountermeasure activeCM) {
        //D.Log("{0}.OnCountermeasureReadyAndThreatInRange() called by {1}.", FullName, activeCM.Name);
        RelayToCurrentState(activeCM);
    }

    /// <summary>
    /// Updates the ready countermeasures inventory.
    /// </summary>
    /// <param name="activeCM">The countermeasure.</param>
    private void UpdateReadyCountermeasuresInventory(ActiveCountermeasure activeCM) {
        if (activeCM.IsReadyToInterceptAThreat) {
            if (!_readyCountermeasuresInventory.Contains(activeCM)) {
                _readyCountermeasuresInventory.Add(activeCM);
                //D.Log("{0} added Countermeasure {1} to ReadyCountermeasuresInventory.", FullName, activeCM.Name);
            }
            else {
                //D.Log("{0} properly avoided adding duplicate Countermeasure {1} to ReadyCountermeasuresInventory.", FullName, activeCM.Name);
                // this occurs when a countermeasure attempts to intercept but doesn't (doesn't currently occur) and therefore remains
                // IsReadyToInterceptAThreat. If it had intercepted, it would no longer be ready and therefore would have been removed below
            }
        }
        else {
            if (_readyCountermeasuresInventory.Contains(activeCM)) {
                _readyCountermeasuresInventory.Remove(activeCM);
                //D.Log("{0} removed Countermeasure {1} from ReadyCountermeasuresInventory.", FullName, activeCM.Name);
            }
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
        // IsOperational = true is set when item operations commences
    }

    #endregion

    #region Sensors

    private void Attach(Sensor sensor) {
        D.Assert(Command == null);  // Sensors are only attached to elements when an element is being created so there is no Command yet
        //if (Command != null) {
        //    // Command exists so the new sensor can be attached to the Command's SensorRangeMonitor now
        //    Command.AttachSensorsToMonitors(sensor);
        //}
        //else {
        //    // Note: During startup and ingame building, sensors are added to Elements by Creators before the element has been assigned to a Command.
        //    // As a result, the sensors are initially present without an attached RangeMonitor as SensorRangeMonitors go with Commands not elements.
        //    // When the element is added to a command, unattached sensors are then attached to a RangeMonitor. Weapons and Countermeasures don't
        //    // have this problem as their RangeMonitors can be attached when they are added since the RangeMonitor goes with the element, not the Cmd.
        //    // D.Warn("{0}.Command not yet set. Sensor {1} not attached to monitor.", FullName, sensor.Name);
        //}
        // IsOperational = true is set when item operations commences
    }

    #endregion

    #endregion

    #region View Methods

    protected override void OnIsVisualDetailDiscernibleToUserChanged() {
        base.OnIsVisualDetailDiscernibleToUserChanged();
        Data.Weapons.ForAll(w => w.ToShowEffects = IsVisualDetailDiscernibleToUser);
    }

    private void OnElementIconsEnabledOptionChanged() {
        AssessIcon();
    }

    private void AssessIcon() {
        if (DisplayMgr == null) { return; }

        if (PlayerPrefsManager.Instance.IsElementIconsEnabled) {
            var iconInfo = RefreshIconInfo();
            if (DisplayMgr.IconInfo != iconInfo) {    // avoid property not changed warning
                UnsubscribeToIconEvents(DisplayMgr.Icon);
                //D.Log("{0} changing IconInfo from {1} to {2}.", FullName, DisplayMgr.IconInfo, iconInfo);
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

    #endregion

    #region Events

    protected override void OnLeftDoubleClick() {
        base.OnLeftDoubleClick();
        Command.IsSelected = true;
    }

    protected override void OnCollisionEnter(Collision collision) {
        base.OnCollisionEnter(collision);
        //D.Log("{0}.OnCollisionEnter() called. Colliding object = {1}.", FullName, collision.collider.name);
    }

    #endregion

    # region StateMachine Support Methods

    protected void Dead_ExitState() {
        LogEvent();
        D.Error("{0}.Dead_ExitState should not occur.", Data.Name);
    }

    void OnDetectedEnemy() { RelayToCurrentState(); }   // TODO connect to sensors when I get them

    protected abstract void AssessNeedForRepair();

    #endregion

    #region Combat Support Methods

    protected override void AssessCripplingDamageToEquipment(float damageSeverity) {
        base.AssessCripplingDamageToEquipment(damageSeverity);
        var equipmentSurvivalChance = Constants.OneHundredPercent - damageSeverity;

        var operationalWeapons = Data.Weapons.Where(w => w.IsOperational);
        operationalWeapons.ForAll(w => {
            w.IsOperational = RandomExtended.Chance(equipmentSurvivalChance);
            //D.Log(!w.IsOperational, "{0}'s weapon {1} has been damaged.", FullName, w.Name);
        });
        var operationalSensors = Data.Sensors.Where(s => s.IsOperational);
        operationalSensors.ForAll(s => {
            s.IsOperational = RandomExtended.Chance(equipmentSurvivalChance);
            //D.Log(!s.IsOperational, "{0}'s sensor {1} has been damaged.", FullName, s.Name);
        });
        var operationalActiveCMs = Data.ActiveCountermeasures.Where(cm => cm.IsOperational);
        operationalActiveCMs.ForAll(cm => {
            cm.IsOperational = RandomExtended.Chance(equipmentSurvivalChance);
            //D.Log(!cm.IsOperational, "{0}'s sensor {1} has been damaged.", FullName, cm.Name);
        });
        var operationalGenerators = Data.ShieldGenerators.Where(gen => gen.IsOperational);
        operationalGenerators.ForAll(gen => {
            gen.IsOperational = RandomExtended.Chance(equipmentSurvivalChance);
            D.Log(!gen.IsOperational, "{0}'s shield generator {1} has been damaged.", FullName, gen.Name);
        });
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
        if (DisplayMgr != null) {
            var icon = DisplayMgr.Icon;
            if (icon != null) {
                UnsubscribeToIconEvents(icon);
            }
        }
    }

    private void UnsubscribeToIconEvents(IResponsiveTrackingSprite icon) {
        var iconEventListener = icon.EventListener;
        iconEventListener.onHover -= (go, isOver) => OnHover(isOver);
        iconEventListener.onClick -= (go) => OnClick();
        iconEventListener.onDoubleClick -= (go) => OnDoubleClick();
        iconEventListener.onPress -= (go, isDown) => OnPress(isDown);
    }

    #endregion

    // subscriptions contained completely within this gameobject (both subscriber
    // and subscribee) donot have to be cleaned up as all instances are destroyed

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

        bool isCmdHit = false;
        float damageSeverity;
        bool isElementAlive = ApplyDamage(damage, out damageSeverity);
        if (!isElementAlive) {
            InitiateDeath();    // should immediately propogate thru to Cmd's alive status
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

    #region ICameraTargetable Members

    public override float MinimumCameraViewingDistance { get { return Radius * minViewDistanceFactor; } }

    #endregion

    #region ICameraFocusable Members

    public override float OptimalCameraViewingDistance { get { return Radius * optViewDistanceFactor; } }

    #endregion

    #region ICameraFollowable Members

    [SerializeField]
    [Range(1.0F, 10F)]
    [Tooltip("Dampens Camera Follow Distance Behaviour")]
    private float _followDistanceDampener = 3.0F;
    public virtual float FollowDistanceDampener {
        get { return _followDistanceDampener; }
    }

    [SerializeField]
    [Range(0.5F, 3.0F)]
    [Tooltip("Dampens Camera Follow Rotation Behaviour")]
    private float _followRotationDampener = 1.0F;
    public virtual float FollowRotationDampener {
        get { return _followRotationDampener; }
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

    #region IHighlightable Members

    public override float HighlightRadius { get { return Radius * Screen.height * 1F; } }

    #endregion

}

