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
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using CodeEnv.Master.Common;
using CodeEnv.Master.GameContent;
using UnityEngine;

/// <summary>
///  Abstract class for AMortalItem's that are Unit Elements.
/// </summary>
public abstract class AUnitElementItem : AMortalItemStateMachine, IElementItem, ICameraFollowable, IElementAttackableTarget, IDetectable {

    [Range(1.0F, 3.0F)]
    [Tooltip("Minimum Camera View Distance Multiplier")]
    public float minViewDistanceFactor = 2.0F;

    [Range(1.5F, 5.0F)]
    [Tooltip("Optimal Camera View Distance Multiplier")]
    public float optViewDistanceFactor = 2.4F;

    public AudioClip cmdHit;
    public AudioClip attacking;
    public AudioClip repairing;
    public AudioClip refitting;
    public AudioClip disbanding;

    public new AUnitElementItemData Data {
        get { return base.Data as AUnitElementItemData; }
        set { base.Data = value; }
    }

    public AUnitCmdItem Command { get; set; }

    protected override float ItemTypeCircleScale { get { return 1.0F; } }

    protected IList<IWeaponRangeMonitor> _weaponRangeMonitors = new List<IWeaponRangeMonitor>();
    protected float _gameSpeedMultiplier;

    private Color _originalMeshColor_Main;
    private Color _originalMeshColor_Specular;
    private Color _hiddenMeshColor = GameColor.Clear.ToUnityColor();
    private Renderer _meshRenderer;
    private Animation _animation;
    private ITrackingWidget _icon;
    private DetectionHandler _detectionHandler;

    #region Initialization

    protected override void InitializeLocalReferencesAndValues() {
        base.InitializeLocalReferencesAndValues();
        _gameSpeedMultiplier = GameTime.Instance.GameSpeed.SpeedMultiplier();
        // Note: Radius is set in derived classes due to the difference in meshes
        collider.enabled = false;
        collider.isTrigger = false;
    }

    protected override void Subscribe() {
        base.Subscribe();
        _subscribers.Add(GameTime.Instance.SubscribeToPropertyChanged<GameTime, GameClockSpeed>(gt => gt.GameSpeed, OnGameSpeedChanged));
        _subscribers.Add(PlayerPrefsManager.Instance.SubscribeToPropertyChanged<PlayerPrefsManager, bool>(ppm => ppm.IsElementIconsEnabled, OnElementIconsEnabledChanged));
    }

    protected override void InitializeModelMembers() {
        //D.Log("{0}.InitializeModelMembers() called.", FullName);
        _detectionHandler = new DetectionHandler(Data);
    }

    protected override void SubscribeToDataValueChanges() {
        base.SubscribeToDataValueChanges();
        //TODO: Weapon values don't change but weapons do so I need to know when that happens
    }

    protected override void InitializeViewMembersOnDiscernible() {
        base.InitializeViewMembersOnDiscernible();
        _meshRenderer = gameObject.GetComponentInChildren<Renderer>();
        _meshRenderer.castShadows = true;
        _meshRenderer.receiveShadows = true;
        _originalMeshColor_Main = _meshRenderer.material.GetColor(UnityConstants.MaterialColor_Main);
        _originalMeshColor_Specular = _meshRenderer.material.GetColor(UnityConstants.MaterialColor_Specular);
        _meshRenderer.enabled = true;

        _animation = _meshRenderer.gameObject.GetComponent<Animation>();
        _animation.cullingType = AnimationCullingType.BasedOnRenderers; // aka, disabled when not visible
        _animation.enabled = true;

        var meshCameraLosChgdListener = _meshRenderer.gameObject.GetSafeInterface<ICameraLosChangedListener>();
        meshCameraLosChgdListener.onCameraLosChanged += (go, inCameraLOS) => InCameraLOS = inCameraLOS;
        meshCameraLosChgdListener.enabled = true;

        if (PlayerPrefsManager.Instance.IsElementIconsEnabled) {
            InitializeIcon();
        }
    }

    private void InitializeIcon() {
        D.Assert(PlayerPrefsManager.Instance.IsElementIconsEnabled);
        _icon = TrackingWidgetFactory.Instance.CreateConstantSizeTrackingSprite(this, new Vector2(12, 12), WidgetPlacement.Below);
        _icon.Set("FleetIcon_Unknown");  // HACK 
        ChangeIconColor(Owner.Color);
        //D.Log("{0} initialized its Icon.", FullName);
        // icon enabled state controlled by _icon.Show()
    }

    #endregion

    #region Model Methods

    public override void CommenceOperations() {
        base.CommenceOperations();
        collider.enabled = true;
        Data.Weapons.ForAll(w => w.IsOperational = true);
        Data.Sensors.ForAll(s => s.IsOperational = true);
    }

    /// <summary>
    /// Parents this element to the provided container that holds the entire Unit.
    /// Local position, rotation and scale auto adjust to keep element unchanged in worldspace.
    /// </summary>
    /// <param name="unitContainer">The unit container.</param>
    protected internal virtual void AttachElementAsChildOfUnitContainer(Transform unitContainer) {
        _transform.parent = unitContainer;
    }

    protected override void OnOwnerChanged() {
        base.OnOwnerChanged();
        ChangeIconColor(Owner.Color);
    }

    private void OnGameSpeedChanged() {
        _gameSpeedMultiplier = GameTime.Instance.GameSpeed.SpeedMultiplier();
    }

    protected override void OnDeath() {
        base.OnDeath();
        collider.enabled = false;
        Data.Weapons.ForAll(w => {
            w.onReadyToFireOnEnemyChanged -= OnWeaponReadyToFireOnEnemyChanged;
        });
        Command.OnSubordinateElementDeath(this);
    }

    #region Weapons and Sensors

    /// <summary>
    /// Adds a weapon based on the WeaponStat provided to this element.
    /// </summary>
    /// <param name="weaponStat">The weapon stat.</param>
    public void AddWeapon(WeaponStat weaponStat) {
        // D.Log("{0}.AddWeapon() called. WeaponStat name = {1}.", FullName, weaponStat.RootName);
        Weapon weapon = new Weapon(weaponStat);
        var monitor = UnitFactory.Instance.MakeMonitorInstance(weapon, this);
        if (!_weaponRangeMonitors.Contains(monitor)) {
            // only need to record and setup range monitors once. The same monitor can have more than 1 weapon
            _weaponRangeMonitors.Add(monitor);
        }
        Data.AddWeapon(weapon);
        weapon.onReadyToFireOnEnemyChanged += OnWeaponReadyToFireOnEnemyChanged;
        if (IsOperational) {
            // we have already commenced operations so start the new weapon
            // weapons added before operations have commenced are started when operations commence
            weapon.IsOperational = true;
        }
    }

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
        weapon.onReadyToFireOnEnemyChanged -= OnWeaponReadyToFireOnEnemyChanged;
    }

    #endregion

    #endregion

    #region View Methods

    protected override void OnIsDiscernibleChanged() {
        base.OnIsDiscernibleChanged();
        ShowMesh(IsDiscernible);
        _animation.enabled = IsDiscernible;
        ShowIcon(IsDiscernible);
    }

    private void OnElementIconsEnabledChanged() {
        if (_icon != null) {
            ShowIcon(false); // accessing destroy gameObject error if we are showing it while destroying it
            UnityUtility.DestroyIfNotNullOrAlreadyDestroyed<ITrackingWidget>(_icon);
            _icon = null;
        }

        if (PlayerPrefsManager.Instance.IsElementIconsEnabled && _isViewMembersOnDiscernibleInitialized) {
            InitializeIcon();
            ShowIcon(IsDiscernible);
        }
    }

    private void ShowMesh(bool toShow) {
        if (toShow) {
            _meshRenderer.material.SetColor(UnityConstants.MaterialColor_Main, _originalMeshColor_Main);
            _meshRenderer.material.SetColor(UnityConstants.MaterialColor_Specular, _originalMeshColor_Specular);
            // TODO audio on goes here
        }
        else {
            _meshRenderer.material.SetColor(UnityConstants.MaterialColor_Main, _hiddenMeshColor);
            _meshRenderer.material.SetColor(UnityConstants.MaterialColor_Specular, _hiddenMeshColor);
            // TODO audio off goes here
        }
    }

    private void ChangeIconColor(GameColor color) {
        if (_icon != null) {
            _icon.Color = color;
        }
    }

    private void ShowIcon(bool toShow) {
        if (_icon != null) {
            //D.Log("{0}.ShowIcon({1}) called.", FullName, toShow);
            _icon.Show(toShow);
        }
    }

    #region Animations

    // these run until finished with no requirement to call OnShowCompletion
    protected override void ShowCmdHit() {
        base.ShowCmdHit();
        if (_showingJob != null && _showingJob.IsRunning) {
            _showingJob.Kill();
        }
        _showingJob = new Job(ShowingCmdHit(), toStart: true);
    }

    protected override void ShowAttacking() {
        base.ShowAttacking();
        if (_showingJob != null && _showingJob.IsRunning) {
            _showingJob.Kill();
        }
        _showingJob = new Job(ShowingAttacking(), toStart: true);
    }

    // these run continuously until they are stopped via StopAnimation() 
    protected override void ShowRepairing() {
        base.ShowRepairing();
        if (_showingJob != null && _showingJob.IsRunning) {
            _showingJob.Kill();
        }
        _showingJob = new Job(ShowingRepairing(), toStart: true);
    }

    protected override void ShowRefitting() {
        base.ShowRefitting();
        if (_showingJob != null && _showingJob.IsRunning) {
            _showingJob.Kill();
        }
        _showingJob = new Job(ShowingRefitting(), toStart: true);
    }

    protected override void ShowDisbanding() {
        base.ShowDisbanding();
        if (_showingJob != null && _showingJob.IsRunning) {
            _showingJob.Kill();
        }
        _showingJob = new Job(ShowingDisbanding(), toStart: true);
    }

    private IEnumerator ShowingCmdHit() {
        if (cmdHit != null) {
            _audioSource.PlayOneShot(cmdHit);
        }
        //animation.Stop();
        //yield return UnityUtility.PlayAnimation(animation, "hit");  
        yield return null;
        // does not use OnShowCompletion
    }

    private IEnumerator ShowingAttacking() {
        if (attacking != null) {
            _audioSource.PlayOneShot(attacking);
        }
        //animation.Stop();
        //yield return UnityUtility.PlayAnimation(animation, "hit");  
        yield return null;
        // does not use OnShowCompletion
    }

    private IEnumerator ShowingRefitting() {
        if (refitting != null) {
            _audioSource.PlayOneShot(refitting);
        }
        //animation.Stop();
        //yield return UnityUtility.PlayAnimation(animation, "hit");  
        yield return null;
        // does not useOnShowCompletion
    }

    private IEnumerator ShowingDisbanding() {
        if (disbanding != null) {
            _audioSource.PlayOneShot(disbanding);
        }
        //animation.Stop();
        //yield return UnityUtility.PlayAnimation(animation, "hit");  
        yield return null;
        // does not use OnShowCompletion
    }

    private IEnumerator ShowingRepairing() {
        if (repairing != null) {
            _audioSource.PlayOneShot(repairing);
        }
        //animation.Stop();
        //yield return UnityUtility.PlayAnimation(animation, "hit");  
        yield return null;
        // does not use OnShowCompletion
    }

    #endregion

    #endregion

    #region Mouse Events

    protected override void OnLeftDoubleClick() {
        base.OnLeftDoubleClick();
        Command.IsSelected = true;
    }

    #endregion

    # region StateMachine Support Methods

    protected void Dead_ExitState() {
        LogEvent();
        D.Error("{0}.Dead_ExitState should not occur.", Data.Name);
    }

    protected override void OnShowCompletion() { RelayToCurrentState(); }

    void OnDetectedEnemy() { RelayToCurrentState(); }   // TODO connect to sensors when I get them

    void OnWeaponReadyToFireOnEnemyChanged(Weapon weapon) {
        if (weapon.IsReadyToFireOnEnemy) {
            OnWeaponReady(weapon);
        }
    }

    protected override void AssessCripplingDamageToEquipment(float damageSeverity) {
        base.AssessCripplingDamageToEquipment(damageSeverity);
        var operationalWeapons = Data.Weapons.Where(w => w.IsOperational);
        operationalWeapons.ForAll(w => w.IsOperational = RandomExtended<bool>.Chance(damageSeverity));
        var operationalSensors = Data.Sensors.Where(s => s.IsOperational);
        operationalSensors.ForAll(s => s.IsOperational = RandomExtended<bool>.Chance(damageSeverity));
    }

    /// <summary>
    /// Called when this weapon is ready to fire on an enemy target in range.
    /// </summary>
    /// <param name="weapon">The weapon.</param>
    void OnWeaponReady(Weapon weapon) { RelayToCurrentState(weapon); }

    /// <summary>
    /// Fires the provided weapon at an enemy target, returning <c>true</c> if the weapon
    /// was fired. If a target is provided, then that target is fired on if in range, returning 
    /// <c>false</c> if not.
    /// </summary>
    /// <param name="weapon">The weapon.</param>
    /// <param name="target">An optional designated target.</param>
    /// <returns></returns>
    protected bool Fire(Weapon weapon, IElementAttackableTarget target = null) {
        if (weapon.FireOnEnemyTarget(target)) {
            ShowAnimation(MortalAnimations.Attacking);
            return true;
        }
        D.Log("{0}.{1} did not fire.", FullName, weapon.Name);
        return false;
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

    // no need to destroy _icon as it is a child of this element

    #endregion

    // subscriptions contained completely within this gameobject (both subscriber
    // and subscribee) donot have to be cleaned up as all instances are destroyed

    #region IElementAttackableTarget Members

    public override void TakeHit(CombatStrength attackerWeaponStrength) {
        if (!IsOperational) { return; }

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
        if (Data.IsHQElement && Command.IsOperational) {
            isCmdHit = Command.__CheckForDamage(isElementAlive, damageSustained, damageSeverity);
        }

        if (isElementAlive) {
            var hitAnimation = isCmdHit ? MortalAnimations.CmdHit : MortalAnimations.Hit;
            ShowAnimation(hitAnimation);
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

    public void OnDetection(ICommandItem cmdItem, DistanceRange sensorRange) {
        _detectionHandler.OnDetection(cmdItem, sensorRange);
    }

    public void OnDetectionLost(ICommandItem cmdItem, DistanceRange sensorRange) {
        _detectionHandler.OnDetectionLost(cmdItem, sensorRange);
    }

    #endregion

}

