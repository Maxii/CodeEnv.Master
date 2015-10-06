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
    using System.Collections.Generic;
    using System.Linq;
    using CodeEnv.Master.Common;
    using UnityEngine;

    /// <summary>
    /// Abstract base class for an Element's offensive weapon.
    /// </summary>
    public abstract class AWeapon : ARangedEquipment, IDisposable {

        private static string _editorNameFormat = "{0}[{1}({2:0.})]";

        /// <summary>
        /// Occurs when IsReadyToFire changes.
        /// </summary>
        public event Action<AWeapon> onIsReadyToFireChanged;

        /// <summary>
        /// Occurs when a qualified enemy target enters this operational 
        /// weapon's range. Only raised when the weapon IsOperational.
        /// </summary>
        public event Action<AWeapon> onEnemyTargetEnteringRange;

        private bool _isReadyToFire;
        /// <summary>
        /// Indicates whether this weapon is ready to fire. A weapon is ready to fire when 
        /// it is both operational and loaded. This property is not affected by whether 
        /// there are any enemy targets within range.
        /// </summary>
        public bool IsReadyToFire {
            get { return _isReadyToFire; }
            private set { SetProperty<bool>(ref _isReadyToFire, value, "IsReadyToFire", OnIsReadyToFireChanged); }
        }

        /// <summary>
        /// Indicates whether there are one or more qualified enemy targets within range.
        /// </summary>
        public bool IsEnemyInRange { get { return _qualifiedEnemyTargets.Any(); } }

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
#if UNITY_EDITOR
                return _editorNameFormat.Inject(base.Name, RangeCategory.GetEnumAttributeText(), RangeDistance);
#else 
                return base.Name;
#endif
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

        protected new WeaponStat Stat { get { return base.Stat as WeaponStat; } }

        /// <summary>
        /// The list of enemy targets in range that qualify as targets of this weapon.
        /// </summary>
        protected IList<IElementAttackableTarget> _qualifiedEnemyTargets;
        private bool _isLoaded;
        private WaitJob _reloadJob;

        /// <summary>
        /// Initializes a new instance of the <see cref="AWeapon" /> class.
        /// </summary>
        /// <param name="stat">The stat.</param>
        public AWeapon(WeaponStat stat)
            : base(stat) {
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

        /// <summary>
        /// Gets an estimated firing solution for this weapon on the provided target. The estimate
        /// takes into account the accuracy of the weapon.
        /// </summary>
        /// <param name="target">The target.</param>
        /// <param name="actualTgtBearing">The actual target bearing.</param>
        /// <returns></returns>
        [Obsolete]
        public Vector3 GetFiringSolution(IElementAttackableTarget target, out Vector3 actualTgtBearing) {
            actualTgtBearing = (target.Position - WeaponMount.MuzzleLocation).normalized;
            var inaccuracy = Constants.OneF - Accuracy;
            var xSpread = UnityEngine.Random.Range(-inaccuracy, inaccuracy);
            var ySpread = UnityEngine.Random.Range(-inaccuracy, inaccuracy);
            var zSpread = UnityEngine.Random.Range(-inaccuracy, inaccuracy);
            return new Vector3(actualTgtBearing.x + xSpread, actualTgtBearing.y + ySpread, actualTgtBearing.z + zSpread).normalized;
        }


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
        public virtual bool TryPickBestTarget(IElementAttackableTarget hint, out IElementAttackableTarget enemyTgt) {
            if (hint != null && _qualifiedEnemyTargets.Contains(hint)) {
                if (WeaponMount.CheckFiringSolution(hint)) {
                    enemyTgt = hint;
                    return true;
                }
            }
            var possibleTargets = new List<IElementAttackableTarget>(_qualifiedEnemyTargets);
            return TryPickBestTarget(possibleTargets, out enemyTgt);
        }

        private void OnWeaponMountChanged() {
            WeaponMount.Weapon = this;
        }

        /// <summary>
        /// Called by this weapon's RangeMonitor when an enemy target enters or exits the weapon's range.
        /// </summary>
        /// <param name="enemyTarget">The enemy target.</param>
        /// <param name="isInRange">if set to <c>true</c> [is in range].</param>
        public void OnEnemyTargetInRangeChanged(IElementAttackableTarget enemyTarget, bool isInRange) {
            D.Log("{0} received OnEnemyTargetInRangeChanged. EnemyTarget: {1}, InRange: {2}.", Name, enemyTarget.FullName, isInRange);
            if (isInRange) {
                if (CheckIfQualified(enemyTarget)) {
                    D.Assert(!_qualifiedEnemyTargets.Contains(enemyTarget));
                    _qualifiedEnemyTargets.Add(enemyTarget);
                    if (IsOperational && onEnemyTargetEnteringRange != null) {
                        onEnemyTargetEnteringRange(this);
                    }
                }
            }
            else {
                if (_qualifiedEnemyTargets.Contains(enemyTarget)) {
                    _qualifiedEnemyTargets.Remove(enemyTarget);
                }
            }
        }

        /// <summary>
        /// Called when this weapon's firing process against <c>targetFiredOn</c> has begun.
        /// </summary>
        /// <param name="targetFiredOn">The target fired on.</param>
        /// <param name="ordnanceFired">The ordnance fired.</param>
        public void OnFiringInitiated(IElementAttackableTarget targetFiredOn, IOrdnance ordnanceFired) {
            D.Assert(IsOperational, "{0} fired at {1} while not operational.".Inject(Name, targetFiredOn.FullName));
            D.Assert(_qualifiedEnemyTargets.Contains(targetFiredOn));

            D.Log("{0}.OnFiringInitiated(Target: {1}, Ordnance: {2}) called.", Name, targetFiredOn.FullName, ordnanceFired.Name);
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
        public void OnFiringComplete(IOrdnance ordnanceFired) {
            D.Assert(!_isLoaded);
            D.Log("{0}.OnFiringComplete({1}) called.", Name, ordnanceFired.Name);

            UnityUtility.WaitOneToExecute(onWaitFinished: () => {
                // give time for _reloadJob to exit before starting another
                InitiateReloadCycle();
            });
        }

        private void OnOrdnanceDeath(IOrdnance terminatedOrdnance) {
            D.Log("{0}.OnOrdnanceDeath({1}) called.", Name, terminatedOrdnance.Name);
            RemoveFiredOrdnanceFromRecord(terminatedOrdnance);
        }

        protected override void OnIsOperationalChanged() {
            D.Log("{0}.IsOperational changed to {1}.", Name, IsOperational);
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

        private void OnIsReadyToFireChanged() {
            if (onIsReadyToFireChanged != null) {
                onIsReadyToFireChanged(this);
            }
        }

        private void OnReloaded() {
            D.Log("{0}.{1} completed reload.", RangeMonitor.Name, Name);
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

        /// <summary>
        /// Recursive method that tries to pick a target from a list of possibleTargets. Returns <c>true</c>
        /// if a target was picked, <c>false</c> if not.
        /// </summary>
        /// <param name="possibleTargets">The possible targets.</param>
        /// <param name="enemyTgt">The enemy target returned.</param>
        /// <returns></returns>
        protected virtual bool TryPickBestTarget(IList<IElementAttackableTarget> possibleTargets, out IElementAttackableTarget enemyTgt) {
            enemyTgt = null;
            if (possibleTargets.Count == Constants.Zero) {
                return false;
            }
            var candidateTgt = possibleTargets.First();
            if (WeaponMount.CheckFiringSolution(candidateTgt)) {
                enemyTgt = candidateTgt;
                return true;
            }
            possibleTargets.Remove(candidateTgt);
            return TryPickBestTarget(possibleTargets, out enemyTgt);
        }

        private bool CheckIfQualified(IElementAttackableTarget enemyTarget) {
            return true;    // UNDONE
        }

        private void InitiateReloadCycle() {
            D.Log("{0} is initiating its reload cycle. Duration: {1:0.##} hours.", Name, ReloadPeriod);
            if (_reloadJob != null && _reloadJob.IsRunning) {
                // UNCLEAR can this happen?
                D.Warn("{0}.{1}.InitiateReloadCycle() called while already Running.", RangeMonitor.Name, Name);
            }
            _reloadJob = GameUtility.WaitForHours(ReloadPeriod, onWaitFinished: (jobWasKilled) => {
                OnReloaded();
            });
        }

        private void AssessReadiness() {
            IsReadyToFire = IsOperational && _isLoaded;
        }

        private void Cleanup() {
            if (_reloadJob != null) {   // can be null if element is destroyed before Running
                _reloadJob.Dispose();
            }
        }

        public sealed override string ToString() { return Stat.ToString(); }

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

