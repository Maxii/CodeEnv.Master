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

using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using CodeEnv.Master.Common;
using CodeEnv.Master.Common.LocalResources;
using CodeEnv.Master.GameContent;
using UnityEngine;

/// <summary>
///  Abstract base class for UnitElement Items.
/// </summary>
public abstract class AUnitElementItem : AMortalItemStateMachine, IElementItem, ICameraFollowable, IElementTarget {

    public virtual bool IsHQElement { get; set; }

    public new AElementData Data {
        get { return base.Data as AElementData; }
        set { base.Data = value; }
    }

    public AUnitCommandItem Command { get; set; }

    public float minCameraViewDistanceMultiplier = 2.0F;
    public float optimalCameraViewDistanceMultiplier = 2.4F;

    public AudioClip cmdHit;
    public AudioClip attacking;
    public AudioClip repairing;
    public AudioClip refitting;
    public AudioClip disbanding;

    /// <summary>
    /// Weapon Range Monitor lookup table keyed by the Monitor's Guid ID.
    /// </summary>
    protected IDictionary<Guid, WeaponRangeMonitor> _weaponRangeMonitorLookup;
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
        circleScaleFactor = 1.0F;
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

    protected override void OnOwnerChanged() {
        base.OnOwnerChanged();
        if (enabled) {  // acts just like an isInitialized test as enabled results in Start() which calls Initialize 
            _weaponRangeMonitorLookup.Values.ForAll(rt => rt.Owner = Data.Owner);
        }
    }

    protected override void OnNamingChanged() {
        base.OnNamingChanged();
        _weaponRangeMonitorLookup.Values.ForAll(rt => rt.ParentFullName = Data.FullName);
    }

    #region Weapons

    /// <summary>
    /// Adds the weapon to this element, paired with the provided range monitor. Clients wishing to add
    /// a weapon to this element should use UnitFactory.AddWeapon(weapon, element).
    /// </summary>
    /// <param name="weapon">The weapon.</param>
    /// <param name="rangeMonitor">The range monitor to pair with this weapon.</param>
    public void AddWeapon(Weapon weapon, WeaponRangeMonitor rangeMonitor) {
        if (_weaponRangeMonitorLookup == null) {
            _weaponRangeMonitorLookup = new Dictionary<Guid, WeaponRangeMonitor>();
        }
        if (!_weaponRangeMonitorLookup.ContainsKey(rangeMonitor.ID)) {
            // only need to record and setup range trackers once. The same rangeTracker can have more than 1 weapon
            _weaponRangeMonitorLookup.Add(rangeMonitor.ID, rangeMonitor);
            rangeMonitor.ParentFullName = FullName;
            rangeMonitor.Range = weapon.Range;
            rangeMonitor.Owner = Data.Owner;
            rangeMonitor.onEnemyInRange += OnEnemyInRange;
        }
        // rangeMonitors enable themselves

        Data.AddWeapon(weapon, rangeMonitor.ID);
        // IMPROVE how to keep track ranges from overlapping
    }

    /// <summary>
    /// Removes the weapon from this element, destroying any associated range tracker
    /// if it is no longer in use.
    /// </summary>
    /// <param name="weapon">The weapon.</param>
    public void RemoveWeapon(Weapon weapon) {
        bool isRangeTrackerStillInUse = Data.RemoveWeapon(weapon);
        if (!isRangeTrackerStillInUse) {
            WeaponRangeMonitor rangeTracker;
            if (_weaponRangeMonitorLookup.TryGetValue(weapon.TrackerID, out rangeTracker)) {
                _weaponRangeMonitorLookup.Remove(weapon.TrackerID);
                D.Log("{0} is destroying unused {1} as a result of removing {2}.", FullName, typeof(WeaponRangeMonitor).Name, weapon.Name);
                GameObject.Destroy(rangeTracker.gameObject);
                return;
            }
            D.Error("{0} could not find {1} for {2}.", FullName, typeof(WeaponRangeMonitor).Name, weapon.Name);
        }
    }

    #region Weapon Reload System

    private IDictionary<Guid, Job> _weaponReloadJobs = new Dictionary<Guid, Job>();

    private void OnEnemyInRange(bool isInRange, Guid monitorID) {
        D.Log("{0}.OnEnemyInRange(isInRange: {1}, monitorID: {2}).", FullName, isInRange, monitorID);
        var weapons = Data.GetWeapons(monitorID);
        foreach (var weapon in weapons) {
            var weaponID = weapon.ID;
            Job weaponReloadJob;
            if (isInRange) {
                if (!_weaponReloadJobs.TryGetValue(weaponID, out weaponReloadJob)) {
                    D.Log("{0} creating new weaponReloadJob for {1}.", FullName, weapon.Name);
                    weaponReloadJob = new Job(ReloadWeapon(weapon));
                    _weaponReloadJobs.Add(weaponID, weaponReloadJob);
                }
                D.Assert(!weaponReloadJob.IsRunning, "{0}.{1}.WeaponReloadJob should not be running.".Inject(FullName, weapon.Name));
                weaponReloadJob.Start();
            }
            else {
                weaponReloadJob = _weaponReloadJobs[weaponID];
                if (!weaponReloadJob.IsRunning) {
                    D.Warn("{0}.{1}.WeaponReloadJob should be running.".Inject(FullName, weapon.Name));
                }
                weaponReloadJob.Kill();
            }
        }
    }

    private IEnumerator ReloadWeapon(Weapon weapon) {
        while (true) {
            OnWeaponReady(weapon);
            yield return new WaitForSeconds(weapon.ReloadPeriod);
        }
    }

    #endregion

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

    protected override void OnDeath() {
        base.OnDeath();
        collider.enabled = false;
        _weaponRangeMonitorLookup.Values.ForAll(rt => rt.onEnemyInRange -= OnEnemyInRange);
        if (_weaponReloadJobs.Count != Constants.Zero) {
            _weaponReloadJobs.ForAll<KeyValuePair<Guid, Job>>(kvp => kvp.Value.Kill());
        }
        Command.OnSubordinateElementDeath(this);
    }

    #endregion

    # region StateMachine Callbacks

    protected override void OnShowCompletion() {
        RelayToCurrentState();
    }

    void OnDetectedEnemy() {  // TODO connect to sensors when I get them
        RelayToCurrentState();
    }

    /// <summary>
    /// Called when this weapon is ready to fire on a target in range.
    /// </summary>
    /// <param name="weapon">The weapon.</param>
    void OnWeaponReady(Weapon weapon) {
        RelayToCurrentState(weapon);
    }

    #endregion

    // subscriptions contained completely within this gameobject (both subscriber
    // and subscribee) donot have to be cleaned up as all instances are destroyed

    #region IItem Members

    public override string FullName { get { return IsHQElement ? "[HQ]" + base.FullName : base.FullName; } }

    #endregion

    #region ICameraTargetable Members

    public override float MinimumCameraViewingDistance { get { return Radius * minCameraViewDistanceMultiplier; } }

    #endregion

    #region ICameraFocusable Members

    public override float OptimalCameraViewingDistance { get { return Radius * optimalCameraViewDistanceMultiplier; } }

    #endregion

    #region ICameraFollowable Members

    [SerializeField]
    private float cameraFollowDistanceDampener = 3.0F;
    public virtual float CameraFollowDistanceDampener {
        get { return cameraFollowDistanceDampener; }
    }

    [SerializeField]
    private float cameraFollowRotationDampener = 1.0F;
    public virtual float CameraFollowRotationDampener {
        get { return cameraFollowRotationDampener; }
    }

    #endregion


}

