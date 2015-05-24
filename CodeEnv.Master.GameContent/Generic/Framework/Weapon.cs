// --------------------------------------------------------------------------------------------------------------------
// <copyright>
// Copyright © 2012 - 2014 Strategic Forge
//
// Email: jim@strategicforge.com
// </copyright> 
// <summary> 
// File: Weapon.cs
// An Element's offensive Weapon.
// </summary> 
// -------------------------------------------------------------------------------------------------------------------- 

//#define DEBUG_LOG
#define DEBUG_WARN
#define DEBUG_ERROR

namespace CodeEnv.Master.GameContent {

    using System;
    using System.Linq;
    using System.Collections;
    using System.Collections.Generic;
    using CodeEnv.Master.Common;
    using UnityEngine;

    /// <summary>
    /// An Element's offensive Weapon.
    /// </summary>
    public class Weapon : APropertyChangeTracking, IDisposable {

        //static private string _toStringFormat = "{0}: Name[{1}], Operational[{2}], Strength[{3:0.#}], Range[{4:0.#}], ReloadPeriod[{5:0.#}], Size[{6:0.#}], Power[{7:0.#}]";

        private static string _nameFormat = "{0}_Range({1:0.#})";

        /// <summary>
        /// Occurs when IsOperational changes.
        /// </summary>
        public event Action<Weapon> onIsOperationalChanged;

        /// <summary>
        /// Occurs when IsReady changes.
        /// </summary>
        public event Action<Weapon> onReadinessChanged;

        /// <summary>
        /// Occurs when a qualified enemy target enters this operational 
        /// weapon's range. Only raised when the weapon IsOperational.
        /// </summary>
        public event Action<Weapon> onEnemyTargetEnteringRange;

        private bool _isReady;
        /// <summary>
        /// Indicates whether this weapon is ready to fire. A weapon is ready when 
        /// it is both operational and loaded. This property is not affected by whether 
        /// there are any enemy targets within range.
        /// </summary>
        public bool IsReady {
            get { return _isReady; }
            private set { SetProperty<bool>(ref _isReady, value, "IsReady", OnIsReadyChanged); }
        }

        /// <summary>
        /// Indicates whether there are one or more qualified enemy targets within range.
        /// </summary>
        public bool IsEnemyInRange { get { return _qualifiedEnemyTargets.Any(); } }

        public IWeaponRangeMonitor RangeMonitor { get; set; }

        private bool _isOperational;
        /// <summary>
        /// Indicates whether this weapon is operational (undamaged). Being operational does
        /// not mean it is ready to fire. Not being operational definitely means it is not ready to fire.
        /// The IsReady property reflects both its operational state as well as whether it is loaded.
        /// </summary>
        public bool IsOperational {
            get { return _isOperational; }
            set { SetProperty<bool>(ref _isOperational, value, "IsOperational", OnIsOperationalChanged); }
        }

        public DistanceRange Range { get { return _stat.Range; } }

        public string Name {
            get {
                var owner = RangeMonitor != null ? RangeMonitor.Owner : TempGameValues.NoPlayer;
                return _nameFormat.Inject(_stat.RootName, _stat.Range.GetWeaponRange(owner));
            }
        }

        public ArmamentCategory Category { get { return _stat.Category; } }

        public float Accuracy { get { return _stat.Accuracy; } }

        public float ReloadPeriod { get { return _stat.ReloadPeriod; } }

        public CombatStrength Strength { get { return _stat.Strength; } }

        public float PhysicalSize { get { return _stat.PhysicalSize; } }

        public float PowerRequirement { get { return _stat.PowerRequirement; } }

        /// <summary>
        /// The list of enemy targets in range that qualify as targets of this weapon.
        /// </summary>
        private IList<IElementAttackableTarget> _qualifiedEnemyTargets;

        private IList<IOrdnance> _activeFiredOrdnance;
        private bool _isLoaded;
        private WeaponStat _stat;
        private WaitJob _reloadJob;

        /// <summary>
        /// Initializes a new instance of the <see cref="Weapon" /> class.
        /// </summary>
        /// <param name="stat">The stat.</param>
        public Weapon(WeaponStat stat) {
            _stat = stat;
            _qualifiedEnemyTargets = new List<IElementAttackableTarget>();
            _activeFiredOrdnance = new List<IOrdnance>();
        }

        // Copy Constructor makes no sense when a RangeMonitor must be attached.

        /// <summary>
        /// Called when this Weapon should first start operating. 
        /// Note: Done this way rather than just set IsOperational = true so the Weapon can start loaded.
        /// </summary>
        public void CommenceOperations() {
            _isLoaded = true;
            IsOperational = true;
        }

        /****************************************************************************************
                  * No need to worry about Owner changes as WeaponRangeMonitor tracks it and will 
                  * remove/add enemyTargets via OnEnemyTargetInRangeChanged() when it happens
                  * *************************************************************************************/

        /// <summary>
        /// Called by this weapon's RangeMonitor when an enemy target enters or exits
        /// its range.
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

        public void OnParentElementDeath() {
            if (Category == ArmamentCategory.Beam) {
                // we are about to die so turn off all active Beams
                _activeFiredOrdnance.ForAll(ord => ord.Terminate());
            }
        }

        /// <summary>
        /// Called by WeaponRangeMonitor when an ownership change of either the
        /// ParentElement or a tracked target requires a check to see if any active ordnance
        /// is currently targeted on a non-enemy.
        /// </summary>
        public void CheckActiveOrdnanceTargeting() {
            Player owner = RangeMonitor.Owner;
            var ordnanceTargetingNonEnemies = _activeFiredOrdnance.Where(ord => !ord.Target.Owner.IsEnemyOf(owner));
            if (ordnanceTargetingNonEnemies.Any()) {
                ordnanceTargetingNonEnemies.ForAll(ord => ord.Terminate());
            }
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
        public bool TryPickBestTarget(IElementAttackableTarget hint, out IElementAttackableTarget enemyTgt) {
            if (hint != null && _qualifiedEnemyTargets.Contains(hint)) {
                enemyTgt = hint;
                return true;
            }
            return TryPickBestTarget(out enemyTgt);
        }

        private bool TryPickBestTarget(out IElementAttackableTarget enemyTgt) {
            if (_qualifiedEnemyTargets.Count > Constants.Zero) {
                enemyTgt = _qualifiedEnemyTargets.First();
                return true;
            }
            enemyTgt = null;
            return false;
        }

        private bool CheckIfQualified(IElementAttackableTarget enemyTarget) {
            return true;    // UNDONE
        }

        /// <summary>
        /// Called when this weapon's firing process against <c>targetFiredOn</c> has begun.
        /// </summary>
        /// <param name="targetFiredOn">The target fired on.</param>
        /// <param name="ordnanceFired">The ordnance fired.</param>
        public void OnFiringInitiated(IElementAttackableTarget targetFiredOn, IOrdnance ordnanceFired) {
            D.Assert(IsOperational, "{0} fired at {1} while not operational.".Inject(Name, targetFiredOn.FullName));
            D.Assert(_qualifiedEnemyTargets.Contains(targetFiredOn));
            D.Assert(ordnanceFired.Category == Category);

            D.Log("{0}.OnFiringInitiated(Target: {1}, Ordnance: {2}) called.", Name, targetFiredOn.FullName, ordnanceFired.Name);
            _activeFiredOrdnance.Add(ordnanceFired);
            ordnanceFired.onDeathOneShot += OnOrdnanceDeath;

            _isLoaded = false;
            AssessReadiness();
        }

        /// <summary>
        /// Called when this weapon's firing process against <c>targetFiredOn</c> is complete. Projectile
        /// and Missile Weapons initiate and complete the firing process at the same time. Beam Weapons
        /// don't complete the firing process until their Beam is terminated.
        /// </summary>
        /// <param name="targetFiredOn">The target fired on.</param>
        public void OnFiringComplete(IOrdnance ordnanceFired) {
            D.Assert(ordnanceFired.Category == Category);
            D.Assert(_activeFiredOrdnance.Contains(ordnanceFired));
            D.Assert(!_isLoaded);
            D.Log("{0}.OnFiringComplete({1}) called.", Name, ordnanceFired.Name);

            UnityUtility.WaitOneToExecute(onWaitFinished: () => {
                // give time for _reloadJob to exit before starting another
                InitiateReloadCycle();
            });
        }

        public void OnVisualDetailDiscernibleToUserChanged(bool isDetailVisible) {
            if (_activeFiredOrdnance.Any()) {
                _activeFiredOrdnance.ForAll(ord => ord.ToShowEffects = isDetailVisible);
            }
        }

        private void OnOrdnanceDeath(IOrdnance ordnance) {
            D.Log("{0}.OnOrdnanceDeath({1}) called.", Name, ordnance.Name);

            D.Assert(_activeFiredOrdnance.Contains(ordnance));
            _activeFiredOrdnance.Remove(ordnance);
        }

        private void OnIsOperationalChanged() {
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
            if (onIsOperationalChanged != null) {
                onIsOperationalChanged(this);
            }
        }

        private void OnIsReadyChanged() {
            if (onReadinessChanged != null) {
                onReadinessChanged(this);
            }
        }

        private void InitiateReloadCycle() {
            D.Log("{0} is initiating its reload cycle. Duration: {1} hours.", Name, ReloadPeriod);
            if (_reloadJob != null && _reloadJob.IsRunning) {
                // UNCLEAR can this happen?
                D.Warn("{0}.{1}.InitiateReloadCycle() called while already Running.", RangeMonitor.FullName, Name);
            }
            _reloadJob = GameUtility.WaitForHours(ReloadPeriod, onWaitFinished: (jobWasKilled) => {
                OnReloaded();
            });
        }

        private void OnReloaded() {
            D.Log("{0}.{1} completed reload.", RangeMonitor.FullName, Name);
            _isLoaded = true;
            AssessReadiness();
        }

        private void AssessReadiness() {
            IsReady = IsOperational && _isLoaded;
        }

        private void Cleanup() {
            if (_reloadJob != null) {   // can be null if element is destroyed before Running
                _reloadJob.Dispose();
            }
            // other cleanup here including any tracking Gui2D elements
        }

        public override string ToString() {
            return _stat.ToString();
            //return _toStringFormat.Inject(GetType().Name, Name, IsOperational, Strength.Combined, Range.GetName(), ReloadPeriod, PhysicalSize, PowerRequirement);
        }

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

