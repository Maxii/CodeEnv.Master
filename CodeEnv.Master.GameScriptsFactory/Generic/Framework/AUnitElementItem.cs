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

//#define DEBUG_LOG
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
///  Abstract class for AMortalItem's that are Unit Elements.
/// </summary>
public abstract class AUnitElementItem : AMortalItemStateMachine, IUnitElementItem, ICameraFollowable, IElementAttackableTarget, IDetectable {

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

    private IList<Weapon> _readyWeaponsInventory = new List<Weapon>();
    private DetectionHandler _detectionHandler;
    private Collider _collider;

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
        Data.Weapons.ForAll(w => w.CommenceOperations());
        Data.Sensors.ForAll(s => s.CommenceOperations());
    }

    /// <summary>
    /// Parents this element to the provided container that holds the entire Unit.
    /// Local position, rotation and scale auto adjust to keep element unchanged in worldspace.
    /// </summary>
    /// <param name="unitContainer">The unit container.</param>
    protected internal virtual void AttachAsChildOf(Transform unitContainer) {
        _transform.parent = unitContainer;
    }

    protected override void OnOwnerChanged() {
        base.OnOwnerChanged();
        AssessIcon();
    }

    private void OnIsHQChanged() {
        if (onIsHQChanged != null) {
            onIsHQChanged(this);
        }
    }

    protected override void OnDeath() {
        base.OnDeath();
        _collider.enabled = false;
        Data.Weapons.ForAll(w => {
            w.onReadinessChanged -= OnWeaponReadinessChanged;
            w.onEnemyTargetEnteringRange -= OnNewEnemyTargetInRange;
        });
    }

    #region Weapons

    /// <summary>
    /// Adds a weapon based on the WeaponStat provided to this element.
    /// </summary>
    /// <param name="weaponStat">The weapon stat.</param>
    public void AddWeapon(WeaponStat weaponStat) {
        //D.Log("{0}.AddWeapon() called. WeaponStat = {1}, Range = {2}.", FullName, weaponStat, weaponStat.Range.GetWeaponRange(Owner));
        Weapon weapon;
        switch (weaponStat.Category) {
            case ArmamentCategory.Beam:
                weapon = new BeamWeapon(weaponStat as BeamWeaponStat);
                break;
            case ArmamentCategory.Missile:
            case ArmamentCategory.Projectile:
                weapon = new Weapon(weaponStat);
                break;
            default:
                throw new NotImplementedException(ErrorMessages.UnanticipatedSwitchValue.Inject(weaponStat.Category));
        }

        var monitor = UnitFactory.Instance.MakeMonitorInstance(weapon, this);
        if (!_weaponRangeMonitors.Contains(monitor)) {
            // only need to record and setup range monitors once. The same monitor can have more than 1 weapon
            _weaponRangeMonitors.Add(monitor);
        }
        Data.AddWeapon(weapon);
        weapon.onReadinessChanged += OnWeaponReadinessChanged;
        weapon.onEnemyTargetEnteringRange += OnNewEnemyTargetInRange;
        if (IsOperational) {
            // we have already commenced operations so start the new weapon
            // weapons added before operations have commenced are started when operations commence
            weapon.IsOperational = true;
        }
    }

    /// <summary>
    /// Removes the weapon from this element, destroying any associated 
    /// range monitor no longer in use.
    /// </summary>
    /// <param name="weapon">The weapon.</param>
    public void RemoveWeapon(Weapon weapon) {
        D.Assert(IsOperational);
        var monitor = weapon.RangeMonitor;
        bool isRangeMonitorStillInUse = monitor.Remove(weapon);

        if (!isRangeMonitorStillInUse) {
            _weaponRangeMonitors.Remove(monitor);
            D.Log("{0} is destroying unused {1} as a result of removing {2}.", FullName, typeof(WeaponRangeMonitor).Name, weapon.Name);
            UnityUtility.DestroyIfNotNullOrAlreadyDestroyed(monitor);
        }
        weapon.IsOperational = false;
        Data.RemoveWeapon(weapon);
        weapon.onReadinessChanged -= OnWeaponReadinessChanged;
        weapon.onEnemyTargetEnteringRange -= OnNewEnemyTargetInRange;
    }

    /// <summary>
    /// Attempts to find a target in range and fire the weapon at it.
    /// </summary>
    /// <param name="weapon">The weapon.</param>
    /// <param name="tgtHint">Optional hint indicating a highly desirable target.</param>
    protected void FindTargetAndFire(Weapon weapon, IElementAttackableTarget tgtHint = null) {
        D.Assert(weapon.IsReady);
        IElementAttackableTarget enemyTarget;
        if (weapon.TryPickBestTarget(tgtHint, out enemyTarget)) {
            Fire(weapon, enemyTarget);
        }
    }

    /// <summary>
    /// Fires the provided weapon at the provided enemy target and
    /// lets the weapon know it has been fired.
    /// </summary>
    /// <param name="weapon">The weapon.</param>
    /// <param name="target">The target.</param>
    private void Fire(Weapon weapon, IElementAttackableTarget target) {
        StartEffect(EffectID.Attacking);
        IOrdnance ordnance = null;
        if (weapon.Category == ArmamentCategory.Beam) {
            var beamWeapon = weapon as BeamWeapon;
            ordnance = FireBeam(beamWeapon, target);
        }
        else if (weapon.Category == ArmamentCategory.Projectile) {
            ordnance = FireProjectile(weapon, target);
        }
        else if (weapon.Category == ArmamentCategory.Missile) {
            ordnance = FireMissile(weapon, target);
        }
        D.Log("{0} has fired {1} against {2} on {3}.", FullName, ordnance.Name, target.DisplayName, GameTime.Instance.CurrentDate);
    }

    private IOrdnance FireProjectile(Weapon weapon, IElementAttackableTarget target) {
        var targetBearing = (target.Position - Position).normalized;
        var muzzleLocation = Position + targetBearing * Radius; // IMPROVE
        var projectile = GeneralFactory.Instance.MakeOrdnanceInstance(weapon.Category, gameObject, muzzleLocation);
        projectile.Initiate(target, weapon, IsVisualDetailDiscernibleToUser);
        return projectile;
    }

    private IOrdnance FireMissile(Weapon weapon, IElementAttackableTarget target) {
        var targetBearing = (target.Position - Position).normalized;
        var muzzleLocation = Position + targetBearing * Radius; // IMPROVE
        var missile = GeneralFactory.Instance.MakeOrdnanceInstance(weapon.Category, gameObject, muzzleLocation);
        missile.Initiate(target, weapon, IsVisualDetailDiscernibleToUser);
        return missile;
    }

    private IOrdnance FireBeam(BeamWeapon weapon, IElementAttackableTarget target) {
        var targetBearing = (target.Position - Position).normalized;
        var muzzleLocation = Position + targetBearing * Radius;   // IMPROVE
        var beam = GeneralFactory.Instance.MakeOrdnanceInstance(weapon.Category, gameObject, muzzleLocation);
        beam.Initiate(target, weapon, IsVisualDetailDiscernibleToUser);
        return beam;
    }

    /// <summary>
    /// Called when there is a change in the readiness to fire 
    /// status of the indicated weapon. Readiness to fire does not
    /// mean there is an enemy in range to fire at.
    /// </summary>
    /// <param name="weapon">The weapon.</param>
    private void OnWeaponReadinessChanged(Weapon weapon) {
        if (weapon.IsReady && weapon.IsEnemyInRange) {
            OnWeaponReadyAndEnemyInRange(weapon);
        }
        UpdateReadyWeaponsInventory(weapon);
    }

    /// <summary>
    /// Called when a new, qualified enemy target has come within range 
    /// of the indicated weapon. This event is independent of whether the
    /// weapon is ready to fire. However, it does mean the weapon is operational.
    /// </summary>
    /// <param name="weapon">The weapon.</param>
    private void OnNewEnemyTargetInRange(Weapon weapon) {
        if (_readyWeaponsInventory.Contains(weapon)) {
            OnWeaponReadyAndEnemyInRange(weapon);
            UpdateReadyWeaponsInventory(weapon);
        }
    }

    /// <summary>
    /// Called when [weapon ready and enemy in range].
    /// </summary>
    /// <param name="weapon">The weapon.</param>
    private void OnWeaponReadyAndEnemyInRange(Weapon weapon) {
        // the weapon is ready and the enemy is in range
        RelayToCurrentState(weapon);
    }

    /// <summary>
    /// Updates the ready weapons inventory.
    /// </summary>
    /// <param name="weapon">The weapon.</param>
    private void UpdateReadyWeaponsInventory(Weapon weapon) {
        if (weapon.IsReady) {
            if (!_readyWeaponsInventory.Contains(weapon)) {
                _readyWeaponsInventory.Add(weapon);
                //D.Log("{0} added Weapon {1} to ReadyWeaponsInventory.", FullName, weapon.Name);
            }
            else {
                //D.Assert(!_readyWeaponsInventory.Contains(weapon));   // adding a ready weapon
                D.Warn("{0} attempted to add duplicate Weapon {1} to ReadyWeaponsInventory.", FullName, weapon.Name);
            }
        }
        else {
            if (_readyWeaponsInventory.Contains(weapon)) {
                _readyWeaponsInventory.Remove(weapon);
                //D.Log("{0} removed Weapon {1} from ReadyWeaponsInventory.", FullName, weapon.Name);
            }
        }
    }

    #endregion

    #region Sensors

    public void AddSensor(SensorStat sensorStat) {
        Sensor sensor = new Sensor(sensorStat);
        if (Command != null) {
            // Command exists so the new sensor can be attached to the Command's SensorRangeMonitor now
            Command.AttachSensorsToMonitors(sensor);
        }
        Data.AddSensor(sensor);
        if (IsOperational) {
            // we have already commenced operations so start the new sensor
            // sensors added before operations have commenced are started when operations commence
            sensor.IsOperational = true;
        }
    }

    public void RemoveSensor(Sensor sensor) {
        D.Assert(Command != null);
        D.Assert(IsOperational);
        Command.DetachSensorsFromMonitors(sensor);
        sensor.IsOperational = false;
        Data.RemoveSensor(sensor);
    }

    #endregion

    #endregion

    #region View Methods

    protected override void OnIsVisualDetailDiscernibleToUserChanged() {
        base.OnIsVisualDetailDiscernibleToUserChanged();
        Data.Weapons.ForAll(w => w.OnVisualDetailDiscernibleToUserChanged(IsVisualDetailDiscernibleToUser));
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
        D.Log("{0}.OnCollisionEnter() called. Colliding object = {1}.", FullName, collision.collider.name);
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
        var operationalWeapons = Data.Weapons.Where(w => w.IsOperational);
        operationalWeapons.ForAll(w => w.IsOperational = RandomExtended<bool>.Chance(damageSeverity));
        var operationalSensors = Data.Sensors.Where(s => s.IsOperational);
        operationalSensors.ForAll(s => s.IsOperational = RandomExtended<bool>.Chance(damageSeverity));
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

    public override void TakeHit(CombatStrength attackerWeaponStrength) {
        if (DebugSettings.Instance.AllPlayersInvulnerable) { return; }

        D.Assert(IsOperational);
        CombatStrength damageSustained = attackerWeaponStrength - Data.DefensiveStrength;
        if (damageSustained.Combined == Constants.ZeroF) {
            D.Log("{0} has been hit but incurred no damage.", FullName);
            return;
        }
        D.Log("{0} has been hit. Taking {1:0.#} damage.", FullName, damageSustained.Combined);
        bool isCmdHit = false;
        float damageSeverity;
        bool isElementAlive = ApplyDamage(damageSustained, out damageSeverity);
        if (!isElementAlive) {
            InitiateDeath();    // should immediately propogate thru to Cmd's alive status
        }
        if (IsHQ && Command.IsOperational) {
            isCmdHit = Command.__CheckForDamage(isElementAlive, damageSustained, damageSeverity);
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

    public void OnDetection(IUnitCmdItem cmdItem, DistanceRange sensorRange) {
        _detectionHandler.OnDetection(cmdItem, sensorRange);
    }

    public void OnDetectionLost(IUnitCmdItem cmdItem, DistanceRange sensorRange) {
        _detectionHandler.OnDetectionLost(cmdItem, sensorRange);
    }

    #endregion

    #region IHighlightable Members

    public override float HighlightRadius { get { return Radius * Screen.height * 1F; } }

    #endregion

}

