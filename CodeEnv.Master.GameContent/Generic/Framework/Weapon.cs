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

#define DEBUG_LOG
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

        static private string _toStringFormat = "{0}: Name[{1}], Operational[{2}], Strength[{3:0.#}], Range[{4:0.#}], ReloadPeriod[{5:0.#}], Size[{6:0.#}], Power[{7:0.#}]";

        private static string _nameFormat = "{0}_{1:0.#}";

        public event Action<Weapon> onIsOperationalChanged;

        public event Action<Weapon> onReadyToFireOnEnemyChanged;

        private bool _isReadyToFireOnEnemy;
        public bool IsReadyToFireOnEnemy {
            get { return _isReadyToFireOnEnemy; }
            set { SetProperty<bool>(ref _isReadyToFireOnEnemy, value, "IsReadyToFireOnEnemy", OnIsReadyToFireOnEnemyChanged); }
        }

        private bool _isAnyEnemyInRange;
        public bool IsAnyEnemyInRange {
            get { return _isAnyEnemyInRange; }
            set { SetProperty<bool>(ref _isAnyEnemyInRange, value, "IsAnyEnemyInRange", OnIsAnyEnemyInRangeChanged); }
        }

        public IWeaponRangeMonitor RangeMonitor { get; set; }

        private bool _isOperational;
        public bool IsOperational {
            get { return _isOperational; }
            set { SetProperty<bool>(ref _isOperational, value, "IsOperational", OnIsOperationalChanged); }
        }

        public DistanceRange Range { get { return _stat.Range; } }

        public string Name { get { return _nameFormat.Inject(_stat.RootName, _stat.Strength.Combined); } }

        public int ReloadPeriod { get { return _stat.ReloadPeriod; } }

        public CombatStrength Strength { get { return _stat.Strength; } }

        public float PhysicalSize { get { return _stat.PhysicalSize; } }

        public float PowerRequirement { get { return _stat.PowerRequirement; } }

        private bool _isLoaded;
        private WeaponStat _stat;
        private WaitJob _reloadJob;

        /// <summary>
        /// Initializes a new instance of the <see cref="Weapon" /> class.
        /// </summary>
        /// <param name="stat">The stat.</param>
        public Weapon(WeaponStat stat) {
            _stat = stat;
        }

        public bool Fire(IElementAttackableTarget target) {
            D.Assert(IsReadyToFireOnEnemy);
            if (target == null) {
                return FireOnTargetOfOpportunity();
            }

            if (!RangeMonitor.EnemyTargets.Contains(target)) {
                D.Warn("Target {0} is not present among tracked enemy targets: {1}{2}.",
                    target.FullName, Constants.NewLine, RangeMonitor.EnemyTargets.Select(et => et.FullName).Concatenate());
                return false;
            }

            D.Log("{0}.{1} is firing on {2}.", RangeMonitor.ParentElement.FullName, Name, target.FullName);
            target.TakeHit(Strength);
            _isLoaded = false;
            AssessReadinessToFireOnEnemy();
            UnityUtility.WaitOneToExecute(onWaitFinished: delegate {
                // give time for _reloadJob to exit before starting another
                InitiateReloadCycle();
            });
            return true;
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
            AssessReadinessToFireOnEnemy();
            if (onIsOperationalChanged != null) {
                onIsOperationalChanged(this);
            }
        }

        private void OnIsAnyEnemyInRangeChanged() {
            AssessReadinessToFireOnEnemy();
        }

        private void OnIsReadyToFireOnEnemyChanged() {
            if (onReadyToFireOnEnemyChanged != null) {
                onReadyToFireOnEnemyChanged(this);
            }
        }

        private void InitiateReloadCycle() {
            if (_reloadJob != null && _reloadJob.IsRunning) {
                D.Warn("{0}.{1}.InitiateReloadCycle() called while already Running.", RangeMonitor.ParentElement.FullName, Name);  // UNCLEAR can this happen?
                return;
            }
            _reloadJob = GameUtility.WaitForHours(ReloadPeriod, onWaitFinished: (jobWasKilled) => {
                OnReloaded();
            });
        }

        private void OnReloaded() {
            //D.Log("{0}.{1} completed reload on {2}.", RangeMonitor.ParentElement.FullName, Name, GameTime.Instance.CurrentDate);
            _isLoaded = true;
            AssessReadinessToFireOnEnemy();
        }

        private void AssessReadinessToFireOnEnemy() {
            IsReadyToFireOnEnemy = IsAnyEnemyInRange && _isLoaded && IsOperational;
        }

        private bool FireOnTargetOfOpportunity() {
            IElementAttackableTarget enemyTarget;
            if (RangeMonitor.TryGetRandomEnemyTarget(out enemyTarget)) {
                Fire(enemyTarget);
                return true;
            }
            D.Warn("{0}.{1} has no target of opportunity to fire on.", RangeMonitor.ParentElement.FullName, Name);
            return false;
        }

        private void Cleanup() {
            _reloadJob.Dispose();
            // other cleanup here including any tracking Gui2D elements
        }

        public override string ToString() {
            return _toStringFormat.Inject(GetType().Name, Name, IsOperational, Strength.Combined, Range.GetName(), ReloadPeriod, PhysicalSize, PowerRequirement);
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

