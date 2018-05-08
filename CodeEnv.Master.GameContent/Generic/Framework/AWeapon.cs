﻿// --------------------------------------------------------------------------------------------------------------------
// <copyright>
// Copyright © 2012 - 2014 Strategic Forge
//
// Email: jim@strategicforge.com
// </copyright> 
// <summary> 
// File: AWeapon.cs
// Abstract base class for an Element's offensive weapon.
// </summary> 
// -------------------------------------------------------------------------------------------------------------------- 

////#define DEBUG_LOG
////#define DEBUG_WARN
////#define DEBUG_ERROR

namespace CodeEnv.Master.GameContent {

    using System;
    using System.Collections;
    using System.Collections.Generic;
    using System.Linq;
    using CodeEnv.Master.Common;
    using UnityEngine;

    /// <summary>
    /// Abstract base class for an Element's offensive weapon.
    /// </summary>
    public abstract class AWeapon : ARangedEquipment, IDebugable, IDisposable, IDateMinderClient, IRecurringDateMinderClient {

        private static readonly GameTimeDuration FiringSolutionCheckPeriod = new GameTimeDuration(TempGameValues.HoursBetweenFiringSolutionChecks);

        /// <summary>
        /// Occurs when this weapon is ready to fire using one of the included firing solutions.
        /// </summary>
        public event EventHandler<WeaponFiringSolutionEventArgs> readytoFire;

        private bool _isWeaponDiscernibleToUser;
        public bool IsWeaponDiscernibleToUser {
            get { return _isWeaponDiscernibleToUser; }
            set {
                //D.Log(ShowDebugLog, "{0}.IsWeaponDiscernibleToUser set to {1}.", DebugName, value);
                SetProperty<bool>(ref _isWeaponDiscernibleToUser, value, "IsWeaponDiscernibleToUser");
            }
        }

        private IWeaponRangeMonitor _rangeMonitor;
        public IWeaponRangeMonitor RangeMonitor {
            get { return _rangeMonitor; }
            set { SetProperty<IWeaponRangeMonitor>(ref _rangeMonitor, value, "RangeMonitor"); }
        }

        private IWeaponMount _weaponMount;
        public IWeaponMount WeaponMount {
            get { return _weaponMount; }
            set {
                D.AssertNull(_weaponMount); // should only happen once
                _weaponMount = value;
                WeaponMountPropSetHandler();
            }
        }

        public override string DebugName {
            get { return RangeMonitor != null ? DebugNameFormat.Inject(RangeMonitor.DebugName, Name) : Name; }
        }

        public DamageStrength OrdnanceDmgPotential { get { return Stat.OrdnanceDmgPotential; } }

        public IUnitElement Element { get { return RangeMonitor.ParentItem; } }

        private GameTimeDuration _reloadPeriod;
        public GameTimeDuration ReloadPeriod {
            get { return _reloadPeriod; }
            private set { SetProperty<GameTimeDuration>(ref _reloadPeriod, value, "ReloadPeriod"); }
        }

        public Player Owner { get { return RangeMonitor.Owner; } }

        public bool ShowDebugLog { get { return RangeMonitor != null ? RangeMonitor.ShowDebugLog : true; } }

        protected new AWeaponStat Stat { get { return base.Stat as AWeaponStat; } }

        private bool _isReady;
        /// <summary>
        /// Indicates whether this weapon is ready to initiate a firing sequence. A weapon is ready when 
        /// it is operational, loaded and is not currently executing a firing sequence. This property is not affected by whether 
        /// there are any enemy targets within range, or whether there are any executable firing solutions.
        /// </summary>
        private bool IsReady {
            get { return _isReady; }
            set { SetProperty<bool>(ref _isReady, value, "IsReady", IsReadyPropChangedHandler); }
        }

        private bool _isAnyEnemyInRange;
        /// <summary>
        /// Indicates whether there are one or more qualified enemy targets within range.
        /// </summary>
        private bool IsAnyEnemyInRange {
            get { return _isAnyEnemyInRange; }
            set { SetProperty<bool>(ref _isAnyEnemyInRange, value, "IsAnyEnemyInRange", IsAnyEnemyInRangePropChangedHandler); }
        }

        private bool _isFiringSequenceUnderway;
        /// <summary>
        /// Indicates whether this weapon is in the process of executing its firing sequence. Execution of
        /// a firing sequence begins when one or more firing solutions have been found and ends when firing has completed
        /// or the element declined to fire. As executing a firing sequence can take time, a weapon
        /// is not ready to generate another firing solution while executing its firing sequence. In particular, LosWeapons take time to
        /// aim so this process can take awhile. MissileWeapons do not aim, so this period is very short.
        /// </summary>
        private bool IsFiringSequenceUnderway {
            get { return _isFiringSequenceUnderway; }
            set { SetProperty<bool>(ref _isFiringSequenceUnderway, value, "IsFiringSequenceUnderway", IsFiringSequenceUnderwayPropChangedHandler); }
        }

        /// <summary>
        /// The list of enemy targets in range that qualify as targets of this weapon.
        /// </summary>
        private HashSet<IElementAttackable> _qualifiedEnemyTargets;

        /// <summary>
        /// Lookup table for CombatResults keyed by the target of this weapon.
        /// <remarks>The target itself is used as the key. If the target dies,
        /// the key is removed before the target is destroyed.</remarks>
        /// </summary>
        private IDictionary<IElementAttackable, CombatResult> _combatResults;

        /// <summary>
        /// The date this weapon will be reloaded.
        /// Once reloaded, this date will be default(GameDate) until the Weapon initiates reloading again.
        /// </summary>
        private GameDate _reloadedDate;
        private bool _isLoaded;

        /// <summary>
        /// The DateMinderDuration used to continuously check for firing solutions when none are initially found.
        /// <remarks>Warning: RecurringDateMinder requires UNIQUE instances of DateMinderDuration. Accordingly,
        /// this reference must be nulled and re-instantiated each time a checkForFiringSolutions process is ended/begun.
        /// Use of the same instance when Removing followed by Adding can result in the added instance 
        /// being removed before it is ever checked against a date.</remarks>
        /// </summary>
        private DateMinderDuration _firingSolutionsCheckProcessRecurringDuration;
        private GameTime _gameTime;

        /// <summary>
        /// Initializes a new instance of the <see cref="AWeapon" /> class.
        /// </summary>
        /// <param name="stat">The stat.</param>
        /// <param name="name">The optional unique name for this equipment. If not provided, the name embedded in the stat will be used.</param>
        public AWeapon(AWeaponStat stat, string name = null)
            : base(stat, name) {
            _gameTime = GameTime.Instance;
            _qualifiedEnemyTargets = new HashSet<IElementAttackable>();
            _combatResults = new Dictionary<IElementAttackable, CombatResult>();
        }

        // Copy Constructor makes no sense when a RangeMonitor must be attached

        /*****************************************************************************************************************************
        * This weapon does not need to track Owner changes. When the owner of the item with this weapon changes, the weapon's 
        * RangeMonitor drops and then reacquires all detectedItems. As a result, all reacquired items are categorized correctly. In addition,
        * the RangeMonitor tells each weapon to check its active (fired, currently in route) ordnance and any in-process firing 
        * solution generation via CheckTargeting(). When the owner of an item detected by this weapon changes, the Monitor re-categorizes 
        * the detectedItem into the right list - enemy or non-enemy, and then, depending on the circumstances, either tells the weapon to 
        * CheckTargeting(), calls HandleEnemyTargetInRangeChanged(), neither or both.
        *******************************************************************************************************************************/

        /***********************************************************************************************************************************************
        * ParentDeath Note: No need to track it as the parent element will turn off the operational state of all equipment when it initiates dying.
        **********************************************************************************************************************************************/

        /// <summary>
        /// Called when an ownership change of either the ParentElement or a tracked target requires 
        /// a check to see if any active ordnance or in-process firing solution generation is targeting a non-enemy.
        /// <remarks>5.7.17 Added to account for in-process firing solution generation that could result in a firing
        /// solution targeting a non-enemy, usually caused by a ownership or relationship change.</remarks>
        /// <remarks>Obsoleted 5.9.17 as I found root cause of the problem I was having. Could still be useful so left in place
        /// to use in place of CheckActiveOrdnanceTargeting.</remarks>
        /// </summary>
        [Obsolete]
        public void CheckTargeting() {
            CheckActiveOrdnanceTargeting();
            AssessReadinessToFire();
        }

        /// <summary>
        /// Checks to see if any active ordnance is currently targeted on a non-enemy
        /// and corrects if found.
        /// <remarks>Called when an ownership change of the ParentElement occurs which requires 
        /// a check to see if any active ordnance is targeting a non-enemy.</remarks>
        /// <remarks>5.8.17 An ownership change in a tracked target is handled by 
        /// HandleAttackableEnemyTargetInRangeChanged if needed.</remarks>
        /// </summary>
        public abstract void CheckActiveOrdnanceTargeting();

        /// <summary>
        /// Called by this weapon's RangeMonitor when an enemy target enters or exits the weapon's range.
        /// <remarks>A target that is no longer in range can be a dead enemy target or a former enemy target 
        /// that is no longer an enemy.</remarks>
        /// </summary>
        /// <param name="enemyTarget">The enemy target.</param>
        /// <param name="isInRange">if set to <c>true</c> [is in range].</param>
        public void HandleAttackableEnemyTargetInRangeChanged(IElementAttackable enemyTarget, bool isInRange) {

            __enemyInRangeChgdLogs.Add("{0} received HandleAttackableEnemyTargetInRangeChanged. EnemyTarget: {1}, InRange: {2}. Frame: {3}."
                .Inject(DebugName, enemyTarget.DebugName, isInRange, Time.frameCount));

            if (isInRange) {
                if (IsQualifiedEnemyTarget(enemyTarget)) {
                    bool isAdded = _qualifiedEnemyTargets.Add(enemyTarget);
                    if (!isAdded) {
                        // 5.16.17 Unexpectedly occurred. Added more logging.
                        D.Error("{0} found {1} already being tracked as enemy.".Inject(DebugName, enemyTarget.DebugName));
                        D.Error("{0}: EnemyInRangeChgdLog including error not added: {1}.", DebugName, __enemyInRangeChgdLogs.Concatenate());
                    }

                    __TryAddToPeakEnemiesInRange();
                }
            }
            else {
                IElementAttackable target = enemyTarget;
                bool isPresent = _qualifiedEnemyTargets.Remove(target);
                if (isPresent) {
                    // Note: Some targets going out of range may not have been qualified as targets for this Weapon so they won't be present
                    // Also, a qualified target can die (goes out of range) from other Weapons fire before it is ever added
                    // to this one, so if it is not present, it was never added to this Weapon because it was immediately killed
                    // by other Weapons as it was being added to them.
                    HandleTargetOutOfRange(target);
                    ReportCombatResults(target);
                    // IMPROVE report at later time (combat over?) as not all results are available when the target moves out of the 
                    // Monitor's range, aka missile range can be larger or smaller than monitor range
                }
            }
            IsAnyEnemyInRange = _qualifiedEnemyTargets.Any();
        }

        protected abstract void HandleTargetOutOfRange(IElementAttackable target);

        /// <summary>
        /// Confirms the provided enemyTarget is in range PRIOR to launching the weapon's ordnance.
        /// <remarks>12.15.16 Got a HandleFiringInitiated error so added _qualifiedEnemyTgt criteria as 
        /// WeaponMount range confirmation is not exactly the same thing. I'm theorizing that the target was
        /// barely in range of the mount (measured from the mount's hub), but just outside of the Weapon's 
        /// Monitor range implying that the target had already been removed via OnTriggerExit().</remarks>
        /// </summary>
        /// <param name="enemyTarget">The target.</param>
        /// <returns></returns>
        public bool ConfirmInRangeForLaunch(IElementAttackable enemyTarget) {
            return _qualifiedEnemyTargets.Contains(enemyTarget) && WeaponMount.ConfirmInRangeForLaunch(enemyTarget);
        }

        /// <summary>
        /// Called when the element this weapon is attached too declines to fire the weapon. This can occur 
        /// for numerous reasons: 1) the element is not in a state where it is allowed to fire, or for LosWeapons 
        /// that must wait for the aiming process to complete: 2) the target has moved out of range, 3) the 
        /// target has died, 4) the target is no longer an enemy, the weapon is no longer operational or, in the future 
        /// 5) the element decides to not waste a shot on any of the provided firing solutions (e.g. the weapon may not 
        /// be able to damage the target).
        /// <remarks>Notifying the weapon of this decision is necessary so that the weapon can begin looking for 
        /// other firing solutions which would otherwise only occur once the weapon was fired.</remarks>
        /// </summary>
        public void HandleElementDeclinedToFire() {
            D.Assert(IsFiringSequenceUnderway);
            IsFiringSequenceUnderway = false;
            // 5.7.17 A change to IsFiringSequenceUnderway will result in AssessReadiness() which should make IsReady = true, 
            // and call AssessReadinessToFire(). This re-assessment will look for targets again, and if it can't find any 
            // firing solutions, will call InitiateFiringSolutionsCheckProcess.
        }

        /// <summary>
        /// Called by the weapon's ordnance when this weapon's firing process against <c>targetFiredOn</c> has begun.
        /// </summary>
        /// <param name="targetFiredOn">The target fired on.</param>
        /// <param name="ordnanceFired">The ordnance fired.</param>
        public virtual void HandleFiringInitiated(IElementAttackable targetFiredOn, IOrdnance ordnanceFired) {
            if (!IsOperational) {
                D.Error("{0} fired at {1} while not operational.", Name, targetFiredOn.DebugName);
            }

            if (!_qualifiedEnemyTargets.Contains(targetFiredOn)) {
                D.Error("{0} fired at {1} but not in list of targets.", Name, targetFiredOn.DebugName);
            }

            //D.Log(ShowDebugLog, "{0}.HandleFiringInitiated(Target: {1}, Ordnance: {2}) called.", DebugName, targetFiredOn.DebugName, ordnanceFired.Name);
            RecordFiredOrdnance(ordnanceFired);
            ordnanceFired.terminationOneShot += OrdnanceTerminationEventHandler;

            RecordShotFired(targetFiredOn);

            _isLoaded = false;
            AssessReadiness();
        }

        /// <summary>
        /// Called when this weapon's firing process launching the provided ordnance is complete. Projectile
        /// and Missile Weapons initiate and complete the firing process at the same time. Beam Weapons
        /// don't complete the firing process until their Beam is terminated.
        /// </summary>
        /// <param name="ordnanceFired">The ordnance fired.</param>
        public virtual void HandleFiringComplete(IOrdnance ordnanceFired) {
            D.Assert(!_isLoaded);
            //D.Log(ShowDebugLog, "{0}.HandleFiringComplete({1}) called.", DebugName, ordnanceFired.Name);
            IsFiringSequenceUnderway = false;

            if (IsOperational) {
                // Beams call this when they terminate due to this Weapon's loss of operations
                InitiateReloadProcess();
            }
        }

        protected override void HandleInitialActivation() {
            base.HandleInitialActivation();
            // IMPROVE If/when Owner's ReloadPeriodMultiplier can change, ReloadPeriod will need to change with it
            ReloadPeriod = new GameTimeDuration(Stat.ReloadPeriod * Owner.WeaponReloadPeriodMultiplier);
        }

        protected override void HandleRangeDistanceChanged() {
            base.HandleRangeDistanceChanged();
            if (RangeDistance < TempGameValues.__MinWeaponsRangeDistance) {
                D.Warn("{0}.RangeDistance of {1:0.##} < Min {2:0.##}.", DebugName, RangeDistance, TempGameValues.__MinWeaponsRangeDistance);
            }
        }

        #region Event and Property Change Handlers

        private void IsFiringSequenceUnderwayPropChangedHandler() {
            AssessReadiness();
        }

        private void IsAnyEnemyInRangePropChangedHandler() {
            if (!IsAnyEnemyInRange) {
                KillFiringSolutionsCheckProcess();
            }
            AssessReadinessToFire();
        }

        private void WeaponMountPropSetHandler() {
            WeaponMount.Weapon = this;
        }

        private void OrdnanceTerminationEventHandler(object sender, EventArgs e) {
            IOrdnance terminatedOrdnance = sender as IOrdnance;
            D.AssertNotNull(terminatedOrdnance, sender.ToString());
            RemoveFiredOrdnanceFromRecord(terminatedOrdnance);
        }

        protected override void IsOperationalPropChangedHandler() {
            //D.Log(ShowDebugLog, "{0}.IsOperational changed to {1}.", DebugName, IsOperational);
            if (IsOperational) {
                // just became operational so if not already loaded, reload
                if (!_isLoaded) {
                    InitiateReloadProcess();
                }
            }
            else {
                // just lost operational status so kill any reload in process
                KillReloadProcess();
            }
            AssessReadiness();
            OnIsOperationalChanged();
        }

        private void IsReadyPropChangedHandler() {
            if (!IsReady) {
                KillFiringSolutionsCheckProcess();
            }
            AssessReadinessToFire();
        }

        private void OnReadyToFire(IList<WeaponFiringSolution> firingSolutions) {
            D.Assert(firingSolutions.Any());    // must have one or more firingSolutions to generate the event
            if (readytoFire != null) {
                readytoFire(this, new WeaponFiringSolutionEventArgs(firingSolutions));
            }
        }

        #endregion

        private void HandleReloaded() {
            //D.Log(ShowDebugLog, "{0} completed reload.", DebugName);
            _isLoaded = true;
            AssessReadiness();
        }

        /// <summary>
        /// Records the provided ordnance as having been fired.
        /// </summary>
        /// <param name="ordnanceFired">The ordnance fired.</param>
        protected abstract void RecordFiredOrdnance(IOrdnance ordnanceFired);

        /// <summary>
        /// Removes the fired ordnance from the record as having been fired.
        /// </summary>
        /// <param name="terminatedOrdnance">The terminated ordnance.</param>
        protected abstract void RemoveFiredOrdnanceFromRecord(IOrdnance terminatedOrdnance);

        protected virtual bool IsQualifiedEnemyTarget(IElementAttackable enemyTarget) {
            // 4.12.17 Handles case where 'InRange' enemy targets provided by the WeaponRangeMonitor include ColdWarEnemies.
            // ColdWarEnemy targets would only be included if policy is to allow engagement, and they are in range.
            // However, ColdWarEnemy targets can only be attacked when they are located in Owner's territory. Policy can
            // allow them to be engaged, and they can be in range, but that doesn't mean they are in our territory.
            // WarEnemies can be attacked anywhere. IsAttackAllowedBy incorporates the location of the Item.
            return enemyTarget.IsAttackAllowedBy(Owner);
        }

        private void InitiateReloadProcess() {
            //D.Log(ShowDebugLog, "{0} is initiating its reload process. Duration: {1}.", DebugName, ReloadPeriod);
            D.AssertDefault(_reloadedDate);
            _reloadedDate = new GameDate(ReloadPeriod);
            _gameTime.DateMinder.Add(_reloadedDate, this);
        }

        private void KillReloadProcess() {
            if (_reloadedDate != default(GameDate)) {
                _gameTime.DateMinder.Remove(_reloadedDate, this);
                _reloadedDate = default(GameDate);
            }
        }

        private void AssessReadiness() {
            IsReady = IsOperational && _isLoaded && !IsFiringSequenceUnderway;
        }

        /// <summary>
        /// Assesses readiness to fire of this weapon. 
        /// <remarks>5.7.17 This is the primary way of terminating any in-process FiringSolution generation
        /// process and restarting it, if needed.</remarks>
        /// </summary>
        private void AssessReadinessToFire() {
            if (!IsReady || !IsAnyEnemyInRange) {
                D.AssertNull(_firingSolutionsCheckProcessRecurringDuration);
                return;
            }

            IList<WeaponFiringSolution> firingSolutions;
            if (TryGetFiringSolutions(out firingSolutions)) {
                IsFiringSequenceUnderway = true;
                OnReadyToFire(firingSolutions);
            }
            else {
                InitiateFiringSolutionsCheckProcess();
            }
        }

        /// <summary>
        /// Launches a process to continuous check for newly uncovered firing solutions
        /// against targets in range. Only initiated when the weapon is ready to fire with
        /// enemy targets in range. If either of these conditions change, the job is immediately
        /// killed using KillFiringSolutionsCheckJob().
        /// <remarks>This fill-in check job is needed as firing solution checks otherwise
        /// occur only when 1) the weapon becomes ready to fire, or 2) the first enemy comes
        /// into range. If a firing solution is not discovered during these event checks, no more
        /// checks would take place until another event condition arises. This process fills that
        /// gap, continuously looking for newly uncovered firing solutions which are to be
        /// expected, given movement and attitude changes of both the firing element and
        /// the targets.</remarks>
        /// </summary>
        private void InitiateFiringSolutionsCheckProcess() {
            KillFiringSolutionsCheckProcess();
            D.Assert(IsReady);
            D.Assert(IsAnyEnemyInRange);
            //D.Log(ShowDebugLog, "{0}: Initiating FiringSolutionsCheckProcess.", DebugName);
            _firingSolutionsCheckProcessRecurringDuration = new DateMinderDuration(FiringSolutionCheckPeriod, this);
            _gameTime.RecurringDateMinder.Add(_firingSolutionsCheckProcessRecurringDuration);
        }

        private void KillFiringSolutionsCheckProcess() {
            if (_firingSolutionsCheckProcessRecurringDuration != null) {
                _gameTime.RecurringDateMinder.Remove(_firingSolutionsCheckProcessRecurringDuration);
                _firingSolutionsCheckProcessRecurringDuration = null;
            }
        }

        private bool TryGetFiringSolutions(out IList<WeaponFiringSolution> firingSolutions) {
            int enemyTgtCount = _qualifiedEnemyTargets.Count;
            D.Assert(enemyTgtCount > Constants.Zero);
            firingSolutions = new List<WeaponFiringSolution>(enemyTgtCount);
            foreach (var enemyTgt in _qualifiedEnemyTargets) {
                WeaponFiringSolution firingSolution;
                if (!enemyTgt.IsAttackAllowedBy(Owner)) {
                    __LogErrorNoLongerAttackable(enemyTgt);
                    return false;
                }
                if (WeaponMount.TryGetFiringSolution(enemyTgt, out firingSolution)) {
                    firingSolutions.Add(firingSolution);
                }
            }
            return firingSolutions.Any();
        }

        #region CombatResults System

        /// <summary>
        /// Records a shot was fired for purposes of tracking CombatResults.
        /// </summary>
        /// <param name="target">The target.</param>
        private void RecordShotFired(IElementAttackable target) {
            string targetName = target.DebugName;
            CombatResult combatResult;
            if (!_combatResults.TryGetValue(target, out combatResult)) {
                combatResult = new CombatResult(DebugName, targetName);
                _combatResults.Add(target, combatResult);
            }
            combatResult.ShotsTaken++;
        }

        /// <summary>
        /// Called by fired ordnance when it hits its intended target.
        /// </summary>
        /// <param name="target">The target.</param>
        public void HandleTargetHit(IElementAttackable target) {
            var combatResult = _combatResults[target];
            combatResult.Hits++;
        }

        /// <summary>
        /// Called by fired ordnance when it misses its intended target without being fatally interdicted.
        /// </summary>
        /// <param name="target">The target.</param>
        public void HandleTargetMissed(IElementAttackable target) {
            var combatResult = _combatResults[target];
            combatResult.Misses++;
        }

        /// <summary>
        /// Called by fired ordnance when it is fatally interdicted by a Countermeasure
        /// (ActiveCM or Shield) or some other obstacle that was not its target.
        /// </summary>
        /// <param name="target">The target.</param>
        public void HandleOrdnanceInterdicted(IElementAttackable target) {
            var combatResult = _combatResults[target];
            combatResult.Interdictions++;
        }

        private void ReportCombatResults(IElementAttackable target) {
            if (DebugSettings.Instance.EnableCombatResultLogging) {
                CombatResult combatResult;
                if (_combatResults.TryGetValue(target, out combatResult)) {    // if the weapon never fired, there won't be a combat result
                    D.Log(combatResult.ToString());
                    //_combatResults.Remove(target);    // for now let these accumulate so logging of hits and misses after the target
                }                                       // is out of the monitor's range doesn't encounter a Dictionary key not found error
            }
        }

        #endregion

        // 8.12.16 Handling pausing for all Jobs moved to JobManager

        #region Cleanup

        private void Cleanup() {
            // 12.8.16 Job Disposal centralized in JobManager
            KillReloadProcess();
            KillFiringSolutionsCheckProcess();
        }

        #endregion

        #region Debug

        private IList<string> __enemyInRangeChgdLogs = new List<string>();

        private void __LogErrorNoLongerAttackable(IElementAttackable target) {
            D.Warn("{0}: The following are a listing of the enemyInRangeChgd calls received for this weapon before this error.", DebugName);
            D.Warn(__enemyInRangeChgdLogs.Concatenate());
            D.Error("{0}: Tgt {1} is no longer attackable. Frame: {2}.", DebugName, target.DebugName, Time.frameCount);
        }

        private static int __peakEnemiesInRangeCount;

        private void __TryAddToPeakEnemiesInRange() {
            if (_qualifiedEnemyTargets.Count > __peakEnemiesInRangeCount) {
                __peakEnemiesInRangeCount = _qualifiedEnemyTargets.Count;
            }
        }

        public static void __ReportPeakEnemiesInRange() {
            Debug.LogFormat("Weapons report PeakEnemiesInRange = {0}.", __peakEnemiesInRangeCount);
        }

        #endregion

        #region Nested Classes

        public class WeaponFiringSolutionEventArgs : EventArgs {

            public IList<WeaponFiringSolution> FiringSolutions { get; private set; }

            public WeaponFiringSolutionEventArgs(IList<WeaponFiringSolution> firingSolutions) {
                FiringSolutions = firingSolutions;
            }
        }


        #endregion

        #region Archive

        /// <summary>
        /// Gets an estimated firing solution for this weapon on the provided target. The estimate
        /// takes into account the accuracy of the weapon.
        /// </summary>
        /// <param name="target">The target.</param>
        /// <param name="actualTgtBearing">The actual target bearing.</param>
        /// <returns></returns>
        //[Obsolete]
        //public Vector3 GetFiringSolution(IElementAttackableTarget target, out Vector3 actualTgtBearing) {
        //    actualTgtBearing = (target.Position - WeaponMount.MuzzleLocation).normalized;
        //    var inaccuracy = Constants.OneF - Accuracy;
        //    var xSpread = UnityEngine.Random.Range(-inaccuracy, inaccuracy);
        //    var ySpread = UnityEngine.Random.Range(-inaccuracy, inaccuracy);
        //    var zSpread = UnityEngine.Random.Range(-inaccuracy, inaccuracy);
        //    return new Vector3(actualTgtBearing.x + xSpread, actualTgtBearing.y + ySpread, actualTgtBearing.z + zSpread).normalized;
        //}

        /// <summary>
        /// Tries to pick the best (most advantageous) qualified target in range.
        /// Returns <c>true</c> if a target was picked, <c>false</c> otherwise.
        /// The hint provided is the initial choice for primary target as determined
        /// by the Element. It is within sensor range (although not necessarily weapons
        /// range) and is associated with the target indicated by Command that our
        /// Element is to attack.
        /// </summary>
        /// <param name="hint">The hint.</param>
        /// <param name="enemyTgt">The enemy target picked.</param>
        /// <returns></returns>
        //public virtual bool TryPickBestTarget(IElementAttackableTarget hint, out IElementAttackableTarget enemyTgt) {
        //    if (hint != null && _qualifiedEnemyTargets.Contains(hint)) {
        //        if (WeaponMount.CheckFiringSolution(hint)) {
        //            enemyTgt = hint;
        //            return true;
        //        }
        //    }
        //    var possibleTargets = new List<IElementAttackableTarget>(_qualifiedEnemyTargets);
        //    return TryPickBestTarget(possibleTargets, out enemyTgt);
        //}

        /// <summary>
        /// Recursive method that tries to pick a target from a list of possibleTargets. Returns <c>true</c>
        /// if a target was picked, <c>false</c> if not.
        /// </summary>
        /// <param name="possibleTargets">The possible targets.</param>
        /// <param name="enemyTgt">The enemy target returned.</param>
        /// <returns></returns>
        //protected virtual bool TryPickBestTarget(IList<IElementAttackableTarget> possibleTargets, out IElementAttackableTarget enemyTgt) {
        //    enemyTgt = null;
        //    if (possibleTargets.Count == Constants.Zero) {
        //        return false;
        //    }
        //    var candidateTgt = possibleTargets.First();
        //    if (WeaponMount.CheckFiringSolution(candidateTgt)) {
        //        enemyTgt = candidateTgt;
        //        return true;
        //    }
        //    possibleTargets.Remove(candidateTgt);
        //    return TryPickBestTarget(possibleTargets, out enemyTgt);
        //}

        #endregion

        #region IDisposable

        private bool _alreadyDisposed = false;

        /// <summary>
        /// Performs application-defined tasks associated with freeing, releasing, or resetting unmanaged resources.
        /// </summary>
        public void Dispose() {

            Dispose(true);

            // This object is being cleaned up by you explicitly calling Dispose() so take this object off
            // the finalization queue and prevent finalization code from 'disposing' a second time
            GC.SuppressFinalize(this);
        }

        /// <summary>
        /// Releases unmanaged and - optionally - managed resources.
        /// </summary>
        /// <param name="isExplicitlyDisposing"><c>true</c> to release both managed and unmanaged resources; <c>false</c> to release only unmanaged resources.</param>
        protected virtual void Dispose(bool isExplicitlyDisposing) {
            if (_alreadyDisposed) { // Allows Dispose(isExplicitlyDisposing) to mistakenly be called more than once
                D.Warn("{0} has already been disposed.", GetType().Name);
                return; //throw new ObjectDisposedException(ErrorMessages.ObjectDisposed);
            }

            if (isExplicitlyDisposing) {
                // Dispose of managed resources here as you have called Dispose() explicitly
                Cleanup();
            }

            // Dispose of unmanaged resources here as either 1) you have called Dispose() explicitly so
            // may as well clean up both managed and unmanaged at the same time, or 2) the Finalizer has
            // called Dispose(false) to cleanup unmanaged resources

            _alreadyDisposed = true;
        }

        #endregion

        #region IDateMinderClient Members

        void IDateMinderClient.HandleDateReached(GameDate date) {
            D.AssertEqual(_reloadedDate, date);
            _reloadedDate = default(GameDate);
            HandleReloaded();
        }

        #endregion

        #region IRecurringDateMinderClient Members

        void IRecurringDateMinderClient.HandleDateReached(DateMinderDuration recurringDuration) {
            D.AssertEqual(_firingSolutionsCheckProcessRecurringDuration, recurringDuration);
            IList<WeaponFiringSolution> firingSolutions;
            if (TryGetFiringSolutions(out firingSolutions)) {
                //D.Log(ShowDebugLog, "{0}'s FiringSolutionsCheckProcess has uncovered one or more firing solutions.", DebugName);
                IsFiringSequenceUnderway = true;
                OnReadyToFire(firingSolutions);
                KillFiringSolutionsCheckProcess();
            }
        }

        #endregion

    }
}

