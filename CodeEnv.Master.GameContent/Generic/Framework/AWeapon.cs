// --------------------------------------------------------------------------------------------------------------------
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

#define DEBUG_LOG
#define DEBUG_WARN
#define DEBUG_ERROR

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
    public abstract class AWeapon : ARangedEquipment, IDisposable {

        /// <summary>
        /// Occurs when this weapon is ready to fire using one of the included firing solutions.
        /// </summary>
        public event EventHandler<WeaponFiringSolutionEventArgs> readytoFire;

        private bool _isReady;
        /// <summary>
        /// Indicates whether this weapon is ready to execute a firing solution. A weapon is ready when 
        /// it is both operational and loaded. This property is not affected by whether 
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

        private bool _toShowEffects;
        /// <summary>
        /// Indicates whether this weapon and its fired ordnance should show their audio and visual effects.
        /// </summary>
        public bool ToShowEffects {
            get { return _toShowEffects; }
            set { SetProperty<bool>(ref _toShowEffects, value, "ToShowEffects", ToShowEffectsPropChangedHandler); }
        }

        public IWeaponRangeMonitor RangeMonitor { get; set; }

        private IWeaponMount _weaponMount;
        public IWeaponMount WeaponMount {
            get { return _weaponMount; }
            set {
                D.Assert(_weaponMount == null); // should only happen once
                _weaponMount = value;
                WeaponMountPropSetHandler();
            }
        }

        public override string FullName {
            get { return RangeMonitor != null ? _fullNameFormat.Inject(RangeMonitor.Name, Name) : Name; }
        }

        public WDVCategory DeliveryVehicleCategory { get { return DeliveryVehicleStrength.Category; } }

        public WDVStrength DeliveryVehicleStrength { get { return Stat.DeliveryVehicleStrength; } }

        public DamageStrength DamagePotential { get { return Stat.DamagePotential; } }

        public float ReloadPeriod {
            get {
                float reloadPeriodMultiplier = RangeMonitor != null ? Owner.WeaponReloadPeriodMultiplier : Constants.OneF;
                return Stat.ReloadPeriod * reloadPeriodMultiplier;
            }
        }

        public Player Owner { get { return RangeMonitor.Owner; } }

        protected override float RangeMultiplier {
            get { return RangeMonitor != null ? Owner.WeaponRangeMultiplier : Constants.OneF; }
        }

        protected new AWeaponStat Stat { get { return base.Stat as AWeaponStat; } }

        /// <summary>
        /// The list of enemy targets in range that qualify as targets of this weapon.
        /// </summary>
        protected IList<IElementAttackableTarget> _qualifiedEnemyTargets;

        private IDictionary<string, CombatResult> _combatResults;
        private bool _isLoaded;
        private WaitJob _reloadJob;
        private Job _checkForFiringSolutionsJob;
        private GameTime _gameTime;

        /// <summary>
        /// Initializes a new instance of the <see cref="AWeapon" /> class.
        /// </summary>
        /// <param name="stat">The stat.</param>
        /// <param name="name">The optional unique name for this equipment. If not provided, the name embedded in the stat will be used.</param>
        public AWeapon(AWeaponStat stat, string name = null)
            : base(stat, name) {
            _gameTime = GameTime.Instance;
            _qualifiedEnemyTargets = new List<IElementAttackableTarget>();
            _combatResults = new Dictionary<string, CombatResult>();
        }

        // Copy Constructor makes no sense when a RangeMonitor must be attached

        /*****************************************************************************************************************************
                    * This weapon does not need to track Owner changes. When the owner of the item with this weapon changes, the weapon's 
                    * RangeMonitor drops and then reacquires all detectedItems. As a result, all reacquired items are categorized correctly. In addition,
                    * the RangeMonitor tells each weapon to check its active (fired, currently in route) ordnance via CheckActiveOrdnanceTargeting().
                    * When the owner of an item detected by this weapon changes, the Monitor recategorizes the detectedItem into the right list - 
                    * enemy or non-enemy, and then, depending on the circumstances, either tells the weapon to CheckActiveOrdnanceTargeting(), 
                    * calls HandleEnemyTargetInRangeChanged(), niether or both.
                    *******************************************************************************************************************************/

        /***********************************************************************************************************************************************
                     * ParentDeath Note: No need to track it as the parent element will turn off the operational state of all  equipment when it initiates dying.
                     **********************************************************************************************************************************************/

        /// <summary>
        /// Called when an ownership change of either the ParentElement or a tracked target requires 
        /// a check to see if any active ordnance is currently targeted on a non-enemy.
        /// </summary>
        public abstract void CheckActiveOrdnanceTargeting();

        /// <summary>
        /// Called by this weapon's RangeMonitor when an enemy target enters or exits the weapon's range.
        /// </summary>
        /// <param name="enemyTarget">The enemy target.</param>
        /// <param name="isInRange">if set to <c>true</c> [is in range].</param>
        public void HandleEnemyTargetInRangeChanged(IElementAttackableTarget enemyTarget, bool isInRange) {
            //D.Log("{0} received HandleEnemyTargetInRangeChanged. EnemyTarget: {1}, InRange: {2}.", Name, enemyTarget.FullName, isInRange);
            if (isInRange) {
                if (CheckIfQualified(enemyTarget)) {
                    D.Assert(!_qualifiedEnemyTargets.Contains(enemyTarget));
                    _qualifiedEnemyTargets.Add(enemyTarget);
                }
            }
            else {
                // Note: Some targets going out of range may not have been qualified as targets for this Weapon.
                // Also, a qualified target can be destroyed (goes out of range) by other Weapons before it is ever added
                // to this one, so if it is not present, it was never added to this Weapon because it was immediately destroyed
                // by other Weapons as it was being added to them.
                if (_qualifiedEnemyTargets.Contains(enemyTarget)) {
                    _qualifiedEnemyTargets.Remove(enemyTarget);
                    ReportCombatResults(enemyTarget);
                }
            }
            IsAnyEnemyInRange = _qualifiedEnemyTargets.Any();
        }

        /// <summary>
        /// Confirms the provided enemyTarget is in range prior to launching the weapon's ordnance.
        /// </summary>
        /// <param name="enemyTarget">The target.</param>
        /// <returns></returns>
        public bool ConfirmInRange(IElementAttackableTarget enemyTarget) {
            return WeaponMount.ConfirmInRange(enemyTarget);
        }

        /// <summary>
        /// Called when the element this weapon is attached too declines to fire
        /// the weapon. This can occur when the target dies while the aiming process
        /// is underway, or if the element decides to not waste a shot on any of the
        /// provided firing solutions (e.g. the weapon may not be able to damage the target).
        /// <remarks>Notifying the weapon of this decision is necessary so that the weapon
        /// can begin looking for other firing solutions which would otherwise only occur once
        /// the weapon was fired.</remarks>
        /// </summary>
        public void HandleElementDeclinedToFire() {
            LaunchFiringSolutionsCheckJob();
        }

        /// <summary>
        /// Called by the weapon's ordnance when this weapon's firing process against <c>targetFiredOn</c> has begun.
        /// </summary>
        /// <param name="targetFiredOn">The target fired on.</param>
        /// <param name="ordnanceFired">The ordnance fired.</param>
        public virtual void HandleFiringInitiated(IElementAttackableTarget targetFiredOn, IOrdnance ordnanceFired) {
            D.Assert(IsOperational, "{0} fired at {1} while not operational.".Inject(Name, targetFiredOn.FullName));
            D.Assert(_qualifiedEnemyTargets.Contains(targetFiredOn), "{0} fired at {1} but not in list of targets.".Inject(Name, targetFiredOn.FullName));

            //D.Log("{0}.HandleFiringInitiated(Target: {1}, Ordnance: {2}) called.", Name, targetFiredOn.FullName, ordnanceFired.Name);
            RecordFiredOrdnance(ordnanceFired);
            ordnanceFired.deathOneShot += OrdnanceDeathEventHandler;

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
            //D.Log("{0}.HandleFiringComplete({1}) called.", Name, ordnanceFired.Name);

            UnityUtility.WaitOneToExecute(onWaitFinished: () => {
                // give time for _reloadJob to exit before starting another
                InitiateReloadCycle();
            });
        }

        #region Event and Property Change Handlers

        private void IsAnyEnemyInRangePropChangedHandler() {
            if (!IsAnyEnemyInRange) {
                KillFiringSolutionsCheckJob();
            }
            AssessReadinessToFire();
        }

        private void WeaponMountPropSetHandler() {
            WeaponMount.Weapon = this;
        }

        private void OrdnanceDeathEventHandler(object sender, EventArgs e) {
            IOrdnance terminatedOrdnance = sender as IOrdnance;
            RemoveFiredOrdnanceFromRecord(terminatedOrdnance);
        }

        protected override void IsOperationalPropChangedHandler() {
            //D.Log("{0}.IsOperational changed to {1}.", Name, IsOperational);
            if (IsOperational) {
                // just became operational so if not already loaded, reload
                if (!_isLoaded) {
                    InitiateReloadCycle();
                }
            }
            else {
                // just lost operational status so kill any reload in process
                if (_reloadJob != null && _reloadJob.IsRunning) {
                    _reloadJob.Kill();
                }
            }
            AssessReadiness();
            OnIsOperationalChanged();
        }

        private void IsReadyPropChangedHandler() {
            if (!IsReady) {
                KillFiringSolutionsCheckJob();
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
            //D.Log("{0} completed reload.", Name);
            _isLoaded = true;
            AssessReadiness();
        }

        protected abstract void ToShowEffectsPropChangedHandler();

        /// <summary>
        /// Records the provided ordnance as having been fired.
        /// </summary>
        /// <param name="ordnanceFired">The ordnance fired.</param>
        protected abstract void RecordFiredOrdnance(IOrdnance ordnanceFired);

        /// <summary>
        /// Removes the fired ordnance from the record as having been fired.
        /// </summary>
        /// <param name="terminatedOrdnance">The dead ordnance.</param>
        protected abstract void RemoveFiredOrdnanceFromRecord(IOrdnance terminatedOrdnance);

        private bool CheckIfQualified(IElementAttackableTarget enemyTarget) {
            return true;    // UNDONE
        }

        private void InitiateReloadCycle() {
            //D.Log("{0} is initiating its reload cycle. Duration: {1:0.##} hours.", Name, ReloadPeriod);
            if (_reloadJob != null && _reloadJob.IsRunning) {
                // UNCLEAR can this happen?
                D.Warn("{0}.InitiateReloadCycle() called while already Running.", Name);
            }
            _reloadJob = WaitJobUtility.WaitForHours(ReloadPeriod, onWaitFinished: (jobWasKilled) => {
                if (!jobWasKilled) {
                    HandleReloaded();
                }
            });
        }

        private void AssessReadiness() {
            IsReady = IsOperational && _isLoaded;
        }

        private void AssessReadinessToFire() {
            if (!IsReady || !IsAnyEnemyInRange) {
                return;
            }

            IList<WeaponFiringSolution> firingSolutions;
            if (TryGetFiringSolutions(out firingSolutions)) {
                OnReadyToFire(firingSolutions);
            }
            else {
                LaunchFiringSolutionsCheckJob();
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
        private void LaunchFiringSolutionsCheckJob() {
            KillFiringSolutionsCheckJob();
            D.Assert(IsReady);
            D.Assert(IsAnyEnemyInRange);
            //D.Log("{0}: Launching FiringSolutionsCheckJob.", Name);
            _checkForFiringSolutionsJob = new Job(CheckForFiringSolutions(), toStart: true, jobCompleted: (jobWasKilled) => {
                //TODO
            });
        }

        /// <summary>
        /// Continuously checks for firing solutions against any target in range. When it finds
        /// one or more, it signals the weapon's readiness to fire and the job terminates.
        /// </summary>
        /// <returns></returns>
        private IEnumerator CheckForFiringSolutions() {
            bool hasFiringSolutions = false;
            while (!hasFiringSolutions) {
                IList<WeaponFiringSolution> firingSolutions;
                if (TryGetFiringSolutions(out firingSolutions)) {
                    hasFiringSolutions = true;
                    //D.Log("{0}.CheckForFiringSolutions() Job has uncovered one or more firing solutions.", Name);
                    OnReadyToFire(firingSolutions);
                }
                // OPTIMIZE can also handle this changeable waitDuration by subscribing to a GameSpeed change
                var waitDuration = TempGameValues.HoursBetweenFiringSolutionChecks / _gameTime.GameSpeedAdjustedHoursPerSecond;
                yield return new WaitForSeconds(waitDuration);
            }
        }

        private void KillFiringSolutionsCheckJob() {
            if (_checkForFiringSolutionsJob != null && _checkForFiringSolutionsJob.IsRunning) {
                //D.Log("{0} FiringSolutionsCheckJob is being killed.", Name);
                _checkForFiringSolutionsJob.Kill();
            }
        }

        private bool TryGetFiringSolutions(out IList<WeaponFiringSolution> firingSolutions) {
            int enemyTgtCount = _qualifiedEnemyTargets.Count;
            D.Assert(enemyTgtCount > Constants.Zero);
            firingSolutions = new List<WeaponFiringSolution>(enemyTgtCount);
            foreach (var enemyTgt in _qualifiedEnemyTargets) {
                WeaponFiringSolution firingSolution;
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
        /// <param name="targetFiredOn">The target.</param>
        private void RecordShotFired(IElementAttackableTarget targetFiredOn) {
            string targetName = targetFiredOn.Name;
            CombatResult combatResult;
            if (!_combatResults.TryGetValue(targetName, out combatResult)) {
                combatResult = new CombatResult(FullName, targetName);
                _combatResults.Add(targetName, combatResult);
            }
            combatResult.ShotsTaken++;
        }

        /// <summary>
        /// Called by fired ordnance when it hits its intended target.
        /// </summary>
        /// <param name="target">The target.</param>
        public void HandleTargetHit(IElementAttackableTarget target) {
            var combatResult = _combatResults[target.Name];
            combatResult.Hits++;
        }

        /// <summary>
        /// Called by fired ordnance when it misses its intended target without being fatally interdicted.
        /// </summary>
        /// <param name="target">The target.</param>
        public void HandleTargetMissed(IElementAttackableTarget target) {
            var combatResult = _combatResults[target.Name];
            combatResult.Misses++;
        }

        /// <summary>
        /// Called by fired ordnance when it is fatally interdicted by a Countermeasure
        /// (ActiveCM or Shield) or some other obstacle that was not its target.
        /// </summary>
        /// <param name="target">The target.</param>
        public void HandleOrdnanceInterdicted(IElementAttackableTarget target) {
            var combatResult = _combatResults[target.Name];
            combatResult.Interdictions++;
        }

        private void ReportCombatResults(IElementAttackableTarget target) {
            CombatResult combatResult;
            if (_combatResults.TryGetValue(target.Name, out combatResult)) {    // if the weapon never fired, there won't be a combat result
                D.Log(combatResult);
            }
        }

        #endregion

        private void Cleanup() {
            if (_reloadJob != null) {   // can be null if element is destroyed before Running
                _reloadJob.Dispose();
            }
            if (_checkForFiringSolutionsJob != null) {
                _checkForFiringSolutionsJob.Dispose();
            }
        }

        public sealed override string ToString() { return Stat.ToString(); }

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


    }
}

