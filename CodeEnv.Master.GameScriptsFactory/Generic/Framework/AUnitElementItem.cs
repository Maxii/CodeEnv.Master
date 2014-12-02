// --------------------------------------------------------------------------------------------------------------------
// <copyright>
// Copyright © 2012 - 2014 Strategic Forge
//
// Email: jim@strategicforge.com
// </copyright> 
// <summary> 
// File: AUnitElementItem.cs
// Abstract base class for UnitElement Items.
// </summary> 
// -------------------------------------------------------------------------------------------------------------------- 

#define DEBUG_LOG
#define DEBUG_WARN
#define DEBUG_ERROR

// default namespace

using System.Collections;
using System.Collections.Generic;
using CodeEnv.Master.Common;
using CodeEnv.Master.GameContent;
using UnityEngine;

/// <summary>
///  Abstract base class for UnitElement Items.
/// </summary>
public abstract class AUnitElementItem : AMortalItemStateMachine, IElementItem, ICameraFollowable, IElementAttackableTarget {

    public virtual bool IsHQElement { get; set; }

    public new AElementData Data {
        get { return base.Data as AElementData; }
        set { base.Data = value; }
    }

    public override string FullName { get { return IsHQElement ? "[HQ]" + base.FullName : base.FullName; } }

    public AUnitCommandItem Command { get; set; }

    protected override float ItemTypeCircleScale { get { return 1.0F; } }

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

    protected IList<IWeaponRangeMonitor> _weaponRangeMonitors = new List<IWeaponRangeMonitor>();
    protected float _gameSpeedMultiplier;
    protected Rigidbody __rigidbody;

    private Color _originalMeshColor_Main;
    private Color _originalMeshColor_Specular;
    private Color _hiddenMeshColor = GameColor.Clear.ToUnityColor();
    private Renderer _meshRenderer;
    private Animation _animation;

    #region Initialization

    protected override void InitializeLocalReferencesAndValues() {
        base.InitializeLocalReferencesAndValues();
        _gameSpeedMultiplier = GameTime.Instance.GameSpeed.SpeedMultiplier();
        // Note: Radius is set in derived classes due to the difference in meshes
        collider.isTrigger = false;
    }

    protected override void InitializeModelMembers() {
        __rigidbody = UnityUtility.ValidateComponentPresence<Rigidbody>(gameObject);
        __rigidbody.mass = Data.Mass;
    }

    protected override void Subscribe() {
        base.Subscribe();
        _subscribers.Add(GameTime.Instance.SubscribeToPropertyChanged<GameTime, GameClockSpeed>(gt => gt.GameSpeed, OnGameSpeedChanged));
    }

    protected override void SubscribeToDataValueChanges() {
        base.SubscribeToDataValueChanges();
        //TODO: Weapon values don't change but weapons do so I need to know when that happens
    }

    #endregion

    #region Model Methods

    public override void CommenceOperations() {
        base.CommenceOperations();
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
        if (IsAliveAndOperating) {
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
        if (IsAliveAndOperating) {
            // we have already commenced operations so start the new sensor
            // sensors added before operations have commenced are started when operations commence
            sensor.IsOperational = true;
        }
    }

    public void RemoveSensor(Sensor sensor) {
        D.Assert(Command != null);
        D.Assert(IsAliveAndOperating);
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
        D.Assert(IsAliveAndOperating);
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

    protected override void InitializeViewMembersOnDiscernible() {
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
    }

    protected override void OnIsDiscernibleChanged() {
        base.OnIsDiscernibleChanged();
        ShowMesh(IsDiscernible);
        _animation.enabled = IsDiscernible;
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

    /// <summary>
    /// Called when this weapon is ready to fire on a target in range.
    /// </summary>
    /// <param name="weapon">The weapon.</param>
    void OnWeaponReady(Weapon weapon) { RelayToCurrentState(weapon); }

    protected bool Fire(Weapon weapon, IElementAttackableTarget target = null) {
        if (weapon.Fire(target)) {
            ShowAnimation(MortalAnimations.Attacking);
            return true;
        }
        D.Log("{0}.{1} did not fire.", FullName, weapon.Name);
        return false;
    }

    #endregion

    // subscriptions contained completely within this gameobject (both subscriber
    // and subscribee) donot have to be cleaned up as all instances are destroyed

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

}

