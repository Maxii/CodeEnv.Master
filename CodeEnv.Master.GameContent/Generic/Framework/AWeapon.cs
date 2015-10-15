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

//#define DEBUG_LOG
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

        private static string _nameFormat = "{0}.{1}";

        /// <summary>
        /// Occurs when this weapon is ready to fire using one of the included firing solutions.
        /// </summary>
        public event Action<IList<WeaponFiringSolution>> onReadyToFire;

        private bool _isReady;
        /// <summary>
        /// Indicates whether this weapon is ready to execute a firing solution. A weapon is ready when 
        /// it is both operational and loaded. This property is not affected by whether 
        /// there are any enemy targets within range, or whether there are any executable firing solutions.
        /// </summary>
        private bool IsReady {
            get { return _isReady; }
            set { SetProperty<bool>(ref _isReady, value, "IsReady", OnIsReadyChanged); }
        }

        private bool _isAnyEnemyInRange;
        /// <summary>
        /// Indicates whether there are one or more qualified enemy targets within range.
        /// </summary>
        private bool IsAnyEnemyInRange {
            get { return _isAnyEnemyInRange; }
            set { SetProperty<bool>(ref _isAnyEnemyInRange, value, "IsAnyEnemyInRange", OnIsAnyEnemyInRangeChanged); }
        }

        private bool _toShowEffects;
        /// <summary>
        /// Indicates whether this weapon and its fired ordnance should show their audio and visual effects.
        /// </summary>
        public bool ToShowEffects {
            get { return _toShowEffects; }
            set { SetProperty<bool>(ref _toShowEffects, value, "ToShowEffects", OnToShowEffectsChanged); }
        }

        public IWeaponRangeMonitor RangeMonitor { get; set; }

        private IWeaponMount _weaponMount;
        public IWeaponMount WeaponMount {
            get { return _weaponMount; }
            set {
                D.Assert(_weaponMount == null); // should only happen once
                _weaponMount = value;
                OnWeaponMountChanged();
            }
        }

        public override string Name {
            get {
                return RangeMonitor != null ? _nameFormat.Inject(RangeMonitor.Name, base.Name) : base.Name;
            }
        }

        public WDVCategory DeliveryVehicleCategory { get { return DeliveryVehicleStrength.Category; } }

        public WDVStrength DeliveryVehicleStrength { get { return Stat.DeliveryVehicleStrength; } }

        public DamageStrength DamagePotential { get { return Stat.DamagePotential; } }

        public float Accuracy { get { return Stat.Accuracy; } }

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
        private bool _isLoaded;
        private WaitJob _reloadJob;
        private Job _checkForFiringSolutionsJob;

        /// <summary>
        /// Initializes a new instance of the <see cref="AWeapon" /> class.
        /// </summary>
        /// <param name="stat">The stat.</param>
        /// <param name="name">The optional unique name for this equipment. If not provided, the name embedded in the stat will be used.</param>
        public AWeapon(AWeaponStat stat, string name = null)
            : base(stat, name) {
            _qualifiedEnemyTargets = new List<IElementAttackableTarget>();
        }

        // Copy Constructor makes no sense when a RangeMonitor must be attached

        /*****************************************************************************************************************************
                    * This weapon does not need to track Owner changes. When the owner of the item with this weapon changes, the weapon's 
                    * RangeMonitor drops and then reacquires all detectedItems. As a result, all reacquired items are categorized correctly. In addition,
                    * the RangeMonitor tells each weapon to check its active (fired, currently in route) ordnance via CheckActiveOrdnanceTargeting().
                    * When the owner of an item detected by this weapon changes, the Monitor recategorizes the detectedItem into the right list - 
                    * enemy or non-enemy, and then, depending on the circumstances, either tells the weapon to CheckActiveOrdnanceTargeting(), 
                    * calls OnEnemyTargetInRangeChanged(), niether or both.
                    *******************************************************************************************************************************/

        /***********************************************************************************************************************************************
                     * ParentDeath Note: No need to track it as the parent element will turn off the operational state of all  equipment when it initiates dying.
                     **********************************************************************************************************************************************/

        /// <summary>
        /// Called when an ownership change of either the ParentElement or a tracked target requires 
        /// a check to see if any active ordnance is currently targeted on a non-enemy.
        /// </summary>
        public abstract void CheckActiveOrdnanceTargeting();

        private void OnWeaponMountChanged() {
            WeaponMount.Weapon = this;
        }

        /// <summary>
        /// Called by this weapon's RangeMonitor when an enemy target enters or exits the weapon's range.
        /// </summary>
        /// <param name="enemyTarget">The enemy target.</param>
        /// <param name="isInRange">if set to <c>true</c> [is in range].</param>
        public void OnEnemyTargetInRangeChanged(IElementAttackableTarget enemyTarget, bool isInRange) {
            //D.Log("{0} received OnEnemyTargetInRangeChanged. EnemyTarget: {1}, InRange: {2}.", Name, enemyTarget.FullName, isInRange);
            if (isInRange) {
                if (CheckIfQualified(enemyTarget)) {
                    D.Assert(!_qualifiedEnemyTargets.Contains(enemyTarget));
                    _qualifiedEnemyTargets.Add(enemyTarget);
                }
            }
            else {
                // some targets going out of range may not have been qualified as targets for this weapon
                if (_qualifiedEnemyTargets.Contains(enemyTarget)) {
                    _qualifiedEnemyTargets.Remove(enemyTarget);
                }
            }
            IsAnyEnemyInRange = _qualifiedEnemyTargets.Any();
        }

        private void OnIsAnyEnemyInRangeChanged() {
            if (!IsAnyEnemyInRange) {
                KillFiringSolutionsCheckJob();
            }
            AssessReadinessToFire();
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
        public void OnElementDeclinedToFire() {
            LaunchFiringSolutionsCheckJob();
        }

        private void OnReadyToFire(IList<WeaponFiringSolution> firingSolutions) {
            D.Assert(firingSolutions.Any());    // must have one or more firingSolutions to generate the event
            if (onReadyToFire != null) {
                onReadyToFire(firingSolutions);
            }
        }

        /// <summary>
        /// Called by the weapon's ordnance when this weapon's firing process against <c>targetFiredOn</c> has begun.
        /// </summary>
        /// <param name="targetFiredOn">The target fired on.</param>
        /// <param name="ordnanceFired">The ordnance fired.</param>
        public virtual void OnFiringInitiated(IElementAttackableTarget targetFiredOn, IOrdnance ordnanceFired) {
            D.Assert(IsOperational, "{0} fired at {1} while not operational.".Inject(Name, targetFiredOn.FullName));
            D.Assert(_qualifiedEnemyTargets.Contains(targetFiredOn), "{0} fired at {1} but not in list of targets.".Inject(Name, targetFiredOn.FullName));

            //D.Log("{0}.OnFiringInitiated(Target: {1}, Ordnance: {2}) called.", Name, targetFiredOn.FullName, ordnanceFired.Name);
            RecordFiredOrdnance(ordnanceFired);
            ordnanceFired.onDeathOneShot += OnOrdnanceDeath;

            _isLoaded = false;
            AssessReadiness();
        }

        /// <summary>
        /// Called when this weapon's firing process launching the provided ordnance is complete. Projectile
        /// and Missile Weapons initiate and complete the firing process at the same time. Beam Weapons
        /// don't complete the firing process until their Beam is terminated.
        /// </summary>
        /// <param name="ordnanceFired">The ordnance fired.</param>
        public virtual void OnFiringComplete(IOrdnance ordnanceFired) {
            D.Assert(!_isLoaded);
            //D.Log("{0}.OnFiringComplete({1}) called.", Name, ordnanceFired.Name);

            UnityUtility.WaitOneToExecute(onWaitFinished: () => {
                // give time for _reloadJob to exit before starting another
                InitiateReloadCycle();
            });
        }

        private void OnOrdnanceDeath(IOrdnance terminatedOrdnance) {
            //D.Log("{0}.OnOrdnanceDeath({1}) called.", Name, terminatedOrdnance.Name);
            RemoveFiredOrdnanceFromRecord(terminatedOrdnance);
        }

        protected override void OnIsOperationalChanged() {
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
            NotifyIsOperationalChanged();
        }

        private void OnIsReadyChanged() {
            if (!IsReady) {
                KillFiringSolutionsCheckJob();
            }
            AssessReadinessToFire();
        }

        private void OnReloaded() {
            //D.Log("{0} completed reload.", Name);
            _isLoaded = true;
            AssessReadiness();
        }

        protected abstract void OnToShowEffectsChanged();

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
            _reloadJob = GameUtility.WaitForHours(ReloadPeriod, onWaitFinished: (jobWasKilled) => {
                OnReloaded();
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
            _checkForFiringSolutionsJob = new Job(CheckForFiringSolutions(), toStart: true, onJobComplete: (jobWasKilled) => {
                // TODO
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
                yield return new WaitForSeconds(1);
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

        private void Cleanup() {
            if (_reloadJob != null) {   // can be null if element is destroyed before Running
                _reloadJob.Dispose();
            }
            if (_checkForFiringSolutionsJob != null) {
                _checkForFiringSolutionsJob.Dispose();
            }
        }

        public sealed override string ToString() { return Stat.ToString(); }

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
        [DoNotSerialize]
        private bool _alreadyDisposed = false;
        protected bool _isDisposing = false;

        /// <summary>
        /// Performs application-defined tasks associated with freeing, releasing, or resetting unmanaged resources.
        /// </summary>
        public void Dispose() {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        /// <summary>
        /// Releases unmanaged and - optionally - managed resources. Derived classes that need to perform additional resource cleanup
        /// should override this Dispose(isDisposing) method, using its own alreadyDisposed flag to do it before calling base.Dispose(isDisposing).
        /// </summary>
        /// <param name="isDisposing"><c>true</c> to release both managed and unmanaged resources; <c>false</c> to release only unmanaged resources.</param>
        protected virtual void Dispose(bool isDisposing) {
            // Allows Dispose(isDisposing) to be called more than once
            if (_alreadyDisposed) {
                D.Warn("{0} has already been disposed.", GetType().Name);
                return;
            }

            _isDisposing = true;
            if (isDisposing) {
                // free managed resources here including unhooking events
                Cleanup();
            }
            // free unmanaged resources here

            _alreadyDisposed = true;
        }

        // Example method showing check for whether the object has been disposed
        //public void ExampleMethod() {
        //    // throw Exception if called on object that is already disposed
        //    if(alreadyDisposed) {
        //        throw new ObjectDisposedException(ErrorMessages.ObjectDisposed);
        //    }

        //    // method content here
        //}
        #endregion

    }
}

