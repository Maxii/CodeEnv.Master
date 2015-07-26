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

    private IList<AWeapon> _readyWeaponsInventory = new List<AWeapon>();
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
        Data.Weapons.ForAll(w => w.IsOperational = true);
        Data.Sensors.ForAll(s => s.IsOperational = true);
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
            w.onIsReadyToFireChanged -= OnWeaponReadinessChanged;
            w.onEnemyTargetEnteringRange -= OnNewEnemyTargetInRange;
            w.IsOperational = false;
        });
        Data.Sensors.ForAll(s => s.IsOperational = false);
    }

    protected override void CleanupAfterOnDeathNotification() {
        base.CleanupAfterOnDeathNotification();
        Command.OnSubordinateElementDeath(this);
    }

    #region Weapons

    /// <summary>
    /// Adds a weapon based on the WeaponStat provided to this element.
    /// </summary>
    /// <param name="weaponStat">The weapon stat.</param>
    public void AddWeapon(WeaponStat weaponStat) {
        //D.Log("{0}.AddWeapon() called. WeaponStat = {1}, Range = {2}.", FullName, weaponStat, weaponStat.Range.GetWeaponRange(Owner));
        AWeapon weapon;
        switch (weaponStat.ArmamentCategory) {
            case ArmamentCategory.Beam:
                weapon = new BeamProjector(weaponStat);
                break;
            case ArmamentCategory.Projectile:
                weapon = new ProjectileLauncher(weaponStat);
                break;
            case ArmamentCategory.Missile:
                weapon = new MissileLauncher(weaponStat);
                break;
            case ArmamentCategory.None:
            default:
                throw new NotImplementedException(ErrorMessages.UnanticipatedSwitchValue.Inject(weaponStat.ArmamentCategory));
        }

        var monitor = UnitFactory.Instance.MakeMonitorInstance(weapon, this);
        if (!_weaponRangeMonitors.Contains(monitor)) {
            // only need to record and setup range monitors once. The same monitor can have more than 1 weapon
            _weaponRangeMonitors.Add(monitor);
        }
        Data.AddWeapon(weapon);
        weapon.onIsReadyToFireChanged += OnWeaponReadinessChanged;
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
    public void RemoveWeapon(AWeapon weapon) {
        weapon.IsOperational = false;
        var monitor = weapon.RangeMonitor;
        bool isRangeMonitorStillInUse = monitor.Remove(weapon);

        if (!isRangeMonitorStillInUse) {
            monitor.Reset();
            _weaponRangeMonitors.Remove(monitor);
            //D.Log("{0} is destroying unused {1} as a result of removing {2}.", FullName, typeof(WeaponRangeMonitor).Name, weapon.Name);
            UnityUtility.DestroyIfNotNullOrAlreadyDestroyed(monitor);
        }
        Data.RemoveWeapon(weapon);
        weapon.onIsReadyToFireChanged -= OnWeaponReadinessChanged;
        weapon.onEnemyTargetEnteringRange -= OnNewEnemyTargetInRange;
    }

    /// <summary>
    /// Attempts to find a target in range and fire the weapon at it.
    /// </summary>
    /// <param name="weapon">The weapon.</param>
    /// <param name="tgtHint">Optional hint indicating a highly desirable target.</param>
    protected void FindTargetAndFire(AWeapon weapon, IElementAttackableTarget tgtHint = null) {
        D.Assert(weapon.IsReadyToFire);
        IElementAttackableTarget enemyTarget;
        if (weapon.TryPickBestTarget(tgtHint, out enemyTarget)) {
            Fire(weapon, enemyTarget);
        }
        else {
            D.Log("{0} did not fire weapon {1}.", FullName, weapon.Name);
        }
    }

    /// <summary>
    /// Fires the provided weapon at the provided enemy target and lets the weapon know it has been fired.
    /// </summary>
    /// <param name="weapon">The weapon.</param>
    /// <param name="target">The target.</param>
    private void Fire(AWeapon weapon, IElementAttackableTarget target) {
        StartEffect(EffectID.Attacking);
        var targetBearing = (target.Position - Position).normalized;
        var muzzleLocation = Position + targetBearing * Radius; // IMPROVE
        var ordnance = GeneralFactory.Instance.MakeOrdnanceInstance(weapon.ArmamentCategory, gameObject, muzzleLocation);
        ordnance.Initiate(target, weapon, IsVisualDetailDiscernibleToUser);
        //D.Log("{0} has fired {1} against {2} on {3}.", FullName, ordnance.Name, target.FullName, GameTime.Instance.CurrentDate);
        /***********************************************************************************************************************************************
               * Note on Target Death: When a target dies, the fired ordnance detects it and takes appropriate action. All ordnance types will no longer
               * apply damage to a dead target, but the impact effect will still show if applicable. This is so the viewer still sees impacts even while the
               * death cinematic plays out. Once the target is destroyed, its collider becomes disabled, allowing ordnance to pass through and potentially
               * collide with other items until it runs out of range and self terminates. This behaviour holds for both projectile and beam ordnance. In the
               * case of missile ordnance, once its target is dead it self destructs as waiting until the target is destroyed results in 'transform destroyed' errors.
               **************************************************************************************************************************************************/
    }

    /// <summary>
    /// Called when there is a change in the readiness to fire 
    /// status of the indicated weapon. Readiness to fire does not
    /// mean there is an enemy in range to fire at.
    /// </summary>
    /// <param name="weapon">The weapon.</param>
    private void OnWeaponReadinessChanged(AWeapon weapon) {
        if (weapon.IsReadyToFire && weapon.IsEnemyInRange) {
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
    private void OnNewEnemyTargetInRange(AWeapon weapon) {
        if (_readyWeaponsInventory.Contains(weapon)) {
            OnWeaponReadyAndEnemyInRange(weapon);
            UpdateReadyWeaponsInventory(weapon);
        }
    }

    /// <summary>
    /// Called when [weapon ready and enemy in range].
    /// </summary>
    /// <param name="weapon">The weapon.</param>
    private void OnWeaponReadyAndEnemyInRange(AWeapon weapon) {
        // the weapon is ready and the enemy is in range
        RelayToCurrentState(weapon);
    }

    /// <summary>
    /// Updates the ready weapons inventory.
    /// </summary>
    /// <param name="weapon">The weapon.</param>
    private void UpdateReadyWeaponsInventory(AWeapon weapon) {
        if (weapon.IsReadyToFire) {
            if (!_readyWeaponsInventory.Contains(weapon)) {
                _readyWeaponsInventory.Add(weapon);
                //D.Log("{0} added Weapon {1} to ReadyWeaponsInventory.", FullName, weapon.Name);
            }
            else {
                //D.Log("{0} properly avoided adding duplicate Weapon {1} to ReadyWeaponsInventory.", FullName, weapon.Name);
                // this occurs when a weapon attempts to fire but doesn't (usually due to LOS interference) and therefore remains
                // IsReadyToFire. If it had fired, it wouldn't be ready and therefore would have been removed below
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
        sensor.IsOperational = false;
        Command.DetachSensorsFromMonitors(sensor);
        Data.RemoveSensor(sensor);
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
        var operationalWeapons = Data.Weapons.Where(w => w.IsOperational);
        operationalWeapons.ForAll(w => {
            var isDamaged = !RandomExtended<bool>.Chance(damageSeverity);
            if (isDamaged) {
                w.IsOperational = false;
                D.Log("{0}'s weapon {1} has been damaged.", FullName, w.Name);
            }
        });
        var operationalSensors = Data.Sensors.Where(s => s.IsOperational);
        operationalSensors.ForAll(s => {
            var isDamaged = !RandomExtended<bool>.Chance(damageSeverity);
            if (isDamaged) {
                s.IsOperational = false;
                D.Log("{0}'s sensor {1} has been damaged.", FullName, s.Name);
            }
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

    public override void TakeHit(CombatStrength attackerWeaponStrength) {
        if (DebugSettings.Instance.AllPlayersInvulnerable) { return; }

        D.Assert(IsOperational);
        CombatStrength damageSustained = attackerWeaponStrength - Data.DefensiveStrength;
        if (damageSustained.Combined == Constants.ZeroF) {
            //D.Log("{0} has been hit but incurred no damage.", FullName);
            return;
        }
        //D.Log("{0} has been hit. Taking {1:0.#} damage.", FullName, damageSustained.Combined);
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

    public void OnDetection(IUnitCmdItem cmdItem, RangeDistanceCategory sensorRange) {
        _detectionHandler.OnDetection(cmdItem, sensorRange);
    }

    public void OnDetectionLost(IUnitCmdItem cmdItem, RangeDistanceCategory sensorRange) {
        _detectionHandler.OnDetectionLost(cmdItem, sensorRange);
    }

    #endregion

    #region IHighlightable Members

    public override float HighlightRadius { get { return Radius * Screen.height * 1F; } }

    #endregion

}

